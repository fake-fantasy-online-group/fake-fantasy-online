using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FakeFantasyOnline.PluginApi;
using MoonSharp.Interpreter;

namespace FakeFantasyOnline.PluginHost
{
/// <summary>Main-thread, isolated Lua addon runtimes. Enabling schedules work; Tick advances it.</summary>
public sealed partial class AddonHost : IDisposable
{
    public const int InstructionSlice = 10000;
    public const int MaximumCallbackSlices = 50;
    public const int MaximumAddons = 64;
    private readonly int _thread = Thread.CurrentThread.ManagedThreadId;
    private readonly string _addonRoot, _configRoot;
    private readonly IGameApi _game;
    private readonly IPluginUi _ui;
    private readonly IPluginWidgetUi _widgets;
    private readonly Dictionary<string, Instance> _addons = new Dictionary<string, Instance>(StringComparer.Ordinal);
    private bool _disposed;
    private int _roundRobin;

    private sealed class Invocation
    {
        internal DynValue Function;
        internal DynValue[] Arguments;
        internal string Name;
        internal int Slices;
        internal Action Completed;
    }
    private sealed class Subscription
    { internal string Name; internal DynValue Callback; }
    private sealed class Instance
    {
        internal AddonPackage Package;
        internal string Id, Name, Version, Error = string.Empty;
        internal AddonState State;
        internal Script Script;
        internal Coroutine Runner;
        internal bool RunnerStarted;
        internal DynValue CompletionYield;
        internal AddonStorage Storage;
        internal Invocation Current;
        internal readonly Queue<Invocation> Queue = new Queue<Invocation>();
        internal readonly HashSet<string> Actions = new HashSet<string>(StringComparer.Ordinal);
        internal readonly Dictionary<string, IDisposable> Panels = new Dictionary<string, IDisposable>(StringComparer.Ordinal);
        internal readonly Dictionary<long, Subscription> Subscriptions = new Dictionary<long, Subscription>();
        internal long SubscriptionId, EventCursor;
        internal int NativeCalls;
        internal bool Stopping;
        internal float Elapsed;
    }

    public AddonHost(string addonRoot, string configRoot, IGameApi game, IPluginUi ui)
    {
        _addonRoot = Path.GetFullPath(addonRoot ?? throw new ArgumentNullException(nameof(addonRoot)));
        _configRoot = Path.GetFullPath(configRoot ?? throw new ArgumentNullException(nameof(configRoot)));
        AddonPackage.CheckPath(_addonRoot); AddonPackage.CheckPath(_configRoot);
        _game = game ?? throw new ArgumentNullException(nameof(game));
        _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        _widgets = ui as IPluginWidgetUi;
        if (_widgets != null) _widgets.EventRaised += OnUiEvent;
    }
    public IReadOnlyList<AddonStatus> Statuses
    {
        get { Check(); return new ReadOnlyCollection<AddonStatus>(_addons.Values.OrderBy(x => x.Id, StringComparer.Ordinal)
            .Select(x => new AddonStatus(x.Id, x.Name, x.Version, x.State, x.Error)).ToList()); }
    }
    public IReadOnlyList<AddonStatus> Discover()
    {
        Check(); AddonPackage.CheckPath(_addonRoot);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (Directory.Exists(_addonRoot))
        {
            foreach (var directory in Directory.EnumerateDirectories(_addonRoot).OrderBy(x => x, StringComparer.Ordinal).Take(MaximumAddons))
            {
                string id = Path.GetFileName(directory); seen.Add(id);
                Instance existing;
                if (_addons.TryGetValue(id, out existing) && IsActive(existing)) continue;
                try
                {
                    var package = AddonPackage.Read(directory);
                    _addons[id] = new Instance { Package = package, Id = package.Id, Name = package.Name,
                        Version = package.Version, State = existing == null ? AddonState.Discovered : AddonState.Disabled };
                }
                catch (Exception ex)
                { _addons[id] = new Instance { Id = id, Name = id, Version = "", State = AddonState.Faulted, Error = ShortError(ex) }; }
            }
        }
        foreach (var id in _addons.Keys.ToArray()) if (!seen.Contains(id) && !IsActive(_addons[id])) _addons.Remove(id);
        return Statuses;
    }
    public bool Enable(string id)
    {
        Check();
        return Enable(id, new HashSet<string>(StringComparer.Ordinal));
    }
    private bool Enable(string id, HashSet<string> visiting)
    {
        Instance addon;
        if (id == null || !_addons.TryGetValue(id, out addon) || addon.Package == null) return false;
        if (visiting.Contains(id)) { Fault(addon, "Dependency cycle detected: " + id); return false; }
        if (IsActive(addon)) return true;
        visiting.Add(id);
        foreach (string dependency in addon.Package.Dependencies)
        {
            if (!Enable(dependency, visiting))
            { visiting.Remove(id); Fault(addon, "Missing, invalid or cyclic dependency: " + dependency); return false; }
        }
        visiting.Remove(id);
        try
        {
            Release(addon);
            addon.Error = string.Empty; addon.Stopping = false; addon.State = AddonState.Loading;
            addon.Storage = addon.Package.Capabilities.Contains("storage.local") ? new AddonStorage(_configRoot, addon.Id) : null;
            addon.Script = CreateScript(addon);
            // Only this constant host-authored chunk is called synchronously. No addon source runs here.
            var driver = addon.Script.Call(addon.Script.LoadString("return function(done, fn, arg) while true do fn(arg); fn, arg = done() end end"));
            addon.Runner = addon.Script.CreateCoroutine(driver).Coroutine;
            addon.Runner.AutoYieldCounter = InstructionSlice;
            addon.CompletionYield = DynValue.NewCallback((ctx, args) => DynValue.NewYieldReq(new[] { DynValue.NewString("__host_callback_complete") }));
            var entry = addon.Script.LoadString(addon.Package.ReadSource(), null, addon.Id + "/" + addon.Package.Entry);
            Enqueue(addon, "entry", entry, Array.Empty<DynValue>(), () =>
                Lifecycle(addon, "on_load", () => Lifecycle(addon, "on_enable", () => addon.State = AddonState.Enabled)));
            return true;
        }
        catch (Exception ex) { Fault(addon, ShortError(ex)); return false; }
    }
    public bool Disable(string id)
    {
        Check(); Instance addon;
        if (id == null || !_addons.TryGetValue(id, out addon)) return false;
        foreach (var dependent in _addons.Values.Where(x => IsActive(x) && x.Package.Dependencies.Contains(id)).ToArray())
            Disable(dependent.Id);
        Stop(addon); return true;
    }
    public bool Reload(string id)
    {
        Check(); Instance addon;
        if (id == null || !_addons.TryGetValue(id, out addon)) return false;
        var restore = new HashSet<string>(StringComparer.Ordinal) { id };
        bool changed;
        do
        {
            changed = false;
            foreach (var item in _addons.Values)
                if (IsActive(item) && item.Package.Dependencies.Any(restore.Contains) && restore.Add(item.Id)) changed = true;
        } while (changed);
        Disable(id);
        try
        {
            addon.Package = AddonPackage.Read(Path.Combine(_addonRoot, id));
            addon.Name = addon.Package.Name; addon.Version = addon.Package.Version;
        }
        catch (Exception ex) { addon.Package = null; Fault(addon, ShortError(ex)); return false; }
        bool success = Enable(id);
        if (success) foreach (string dependent in restore) if (dependent != id) success = Enable(dependent) && success;
        return success;
    }
    public void Tick(float deltaTime)
    {
        Check();
        if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0) throw new ArgumentOutOfRangeException(nameof(deltaTime));
        var active = _addons.Values.Where(IsActive).ToArray();
        foreach (var addon in active) { addon.NativeCalls = 0; addon.Elapsed = Math.Min(addon.Elapsed + deltaTime, 10); }
        if (active.Length == 0) return;
        var timer = Stopwatch.StartNew(); int start = _roundRobin % active.Length;
        // At most one 10k-instruction slice per addon per frame; soft global 3ms scheduling budget.
        for (int n = 0; n < active.Length; n++)
        {
            int index = (start + n) % active.Length; var addon = active[index];
            _roundRobin = (index + 1) % active.Length;
            try
            {
                bool ready = true;
                foreach (string dependency in addon.Package.Dependencies)
                {
                    Instance d;
                    if (!_addons.TryGetValue(dependency, out d) || !IsActive(d))
                        throw new InvalidOperationException("Dependency is unavailable: " + dependency);
                    if (d.State != AddonState.Enabled) ready = false;
                }
                if (!ready) continue;
                if (addon.State == AddonState.Enabled && addon.Current == null && addon.Queue.Count == 0)
                {
                    PollEvents(addon);
                    float elapsed = addon.Elapsed; addon.Elapsed = 0;
                    QueueNamed(addon, "on_update", DynValue.NewNumber(elapsed));
                }
                Advance(addon);
            }
            catch (Exception ex) { Fault(addon, ShortError(ex)); }
            if (timer.Elapsed.TotalMilliseconds >= 3) break;
        }
    }
    private void Advance(Instance addon)
    {
        if (addon.Current == null && addon.Queue.Count > 0) addon.Current = addon.Queue.Dequeue();
        var work = addon.Current; if (work == null) return;
        if (++work.Slices > MaximumCallbackSlices) throw new InvalidOperationException("Instruction/yield budget exceeded in " + work.Name);
        DynValue result;
        if (!addon.RunnerStarted)
        {
            addon.RunnerStarted = true;
            result = addon.Runner.Resume(addon.CompletionYield, work.Function,
                work.Arguments.Length == 0 ? DynValue.Nil : work.Arguments[0]);
        }
        else if (work.Slices == 1)
            result = addon.Runner.Resume(work.Function, work.Arguments.Length == 0 ? DynValue.Nil : work.Arguments[0]);
        else result = addon.Runner.Resume();
        if (result.Type == DataType.String && result.String == "__host_callback_complete")
        { addon.Current = null; if (work.Completed != null) work.Completed(); }
    }

    private void Lifecycle(Instance addon, string name, Action completed)
    {
        var fn = addon.Script.Globals.Get(name);
        if (fn.IsNil()) { completed(); return; }
        Enqueue(addon, name, fn, Array.Empty<DynValue>(), completed);
    }
    private void QueueNamed(Instance addon, string name, params DynValue[] arguments)
    {
        var function = addon.Script.Globals.Get(name);
        if (!function.IsNil()) Enqueue(addon, name, function, arguments, null);
    }
    private static void Enqueue(Instance addon, string name, DynValue function, DynValue[] arguments, Action completed)
    {
        if (function.Type != DataType.Function) throw new ScriptRuntimeException(name + " must be a Lua function.");
        if (addon.Queue.Count >= 128) throw new ScriptRuntimeException("Addon callback queue exceeded.");
        addon.Queue.Enqueue(new Invocation { Name = name, Function = function, Arguments = arguments, Completed = completed });
    }
    private void PollEvents(Instance addon)
    {
        if (!addon.Package.Capabilities.Contains("game.events")) return;
        if (addon.Subscriptions.Count == 0 && addon.Script.Globals.Get("on_event").IsNil()) return;
        var batch = _game.ReadEvents(addon.EventCursor, 16); addon.EventCursor = batch.NextSequence;
        if (batch.ResyncRequired) DispatchEvent(addon, "events.resync_required", LuaValue.ToLua(addon.Script,
            new Dictionary<string, object> { { "name", "events.resync_required" }, { "sequence", batch.NextSequence } }));
        foreach (var item in batch.Events) DispatchEvent(addon, item.Name, LuaValue.ToLua(addon.Script, EventData(item)));
    }
    private void DispatchEvent(Instance addon, string name, DynValue data)
    {
        QueueNamed(addon, "on_event", data);
        foreach (var subscription in addon.Subscriptions.Values.ToArray())
            if (subscription.Name == "*" || subscription.Name == name)
                Enqueue(addon, "event:" + name, subscription.Callback, new[] { data }, null);
    }

    private void OnUiEvent(PluginUiEvent item)
    {
        Check();
        int separator = item.PanelId.IndexOf(':'); if (separator < 1) return;
        Instance addon;
        if (!_addons.TryGetValue(item.PanelId.Substring(0, separator), out addon) || addon.State != AddonState.Enabled || addon.Stopping) return;
        try
        {
            QueueNamed(addon, "on_ui", LuaValue.ToLua(addon.Script, new Dictionary<string, object>
            { { "panelId", item.PanelId.Substring(separator + 1) }, { "widgetId", item.WidgetId }, { "name", item.Name } }));
        }
        catch (Exception ex) { Fault(addon, ShortError(ex)); }
    }
    private void Stop(Instance addon)
    {
        addon.Stopping = true; addon.Queue.Clear(); addon.Current = null; addon.NativeCalls = 0;
        if (addon.Script != null)
        {
            foreach (string name in new[] { "on_disable", "on_unload" })
            {
                try
                {
                    var callback = addon.Script.Globals.Get(name);
                    if (callback.Type != DataType.Function) continue;
                    var coroutine = addon.Script.CreateCoroutine(callback).Coroutine;
                    coroutine.AutoYieldCounter = InstructionSlice; coroutine.Resume();
                    if (coroutine.State != CoroutineState.Dead) addon.Error = "Cleanup callback exceeded its one-slice budget: " + name;
                }
                catch (Exception ex) { addon.Error = ShortError(ex); }
            }
        }
        Release(addon); addon.State = AddonState.Disabled;
    }
    private void Release(Instance addon)
    {
        foreach (string actionId in addon.Actions)
        { try { _game.CancelAction(actionId); } catch { /* Continue releasing other addon resources. */ } }
        addon.Actions.Clear();
        foreach (var handle in addon.Panels.Values) { try { handle.Dispose(); } catch { /* Release every handle even if one fails. */ } }
        addon.Panels.Clear(); addon.Subscriptions.Clear(); addon.Queue.Clear(); addon.Current = null;
        addon.Script = null; addon.Runner = null; addon.RunnerStarted = false; addon.CompletionYield = null; addon.Storage = null; addon.EventCursor = 0; addon.Elapsed = 0;
    }
    private void Fault(Instance addon, string message)
    { addon.Stopping = true; Release(addon); addon.Error = message; addon.State = AddonState.Faulted; }
    private static bool IsActive(Instance addon) { return addon.State == AddonState.Enabled || addon.State == AddonState.Loading; }
    private static string ShortError(Exception exception)
    {
        var script = exception as InterpreterException;
        string text = script == null ? exception.Message : script.DecoratedMessage ?? script.Message;
        return text.Length > 2048 ? text.Substring(0, 2048) : text;
    }
    private void Check()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(AddonHost));
        if (Thread.CurrentThread.ManagedThreadId != _thread) throw new InvalidOperationException("AddonHost must be used on its owning game thread.");
    }
    public void Dispose()
    {
        if (_disposed) return; Check();
        if (_widgets != null) _widgets.EventRaised -= OnUiEvent;
        foreach (var addon in _addons.Values) Stop(addon);
        _addons.Clear(); _disposed = true;
    }
}
}
