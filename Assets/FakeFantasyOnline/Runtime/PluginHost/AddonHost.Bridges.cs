using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using FakeFantasyOnline.PluginApi;
using MoonSharp.Interpreter;
using MoonSharp.Interpreter.Loaders;

namespace FakeFantasyOnline.PluginHost
{
public sealed partial class AddonHost
{
    private Script CreateScript(Instance addon)
    {
        // No CLR userdata, OS, IO, dynamic loader, metatables, pcall or nested coroutine library.
        // The latter can create execution paths that bypass the host's coroutine instruction counter.
        var script = new Script(CoreModules.Basic | CoreModules.GlobalConsts | CoreModules.TableIterators | CoreModules.Math);
        script.Options.ScriptLoader = new DeniedLoader();
        script.Options.MaximumStringLength = LuaValue.MaxString;
        script.Options.MaximumTableEntries = 8192;
        script.Options.DebugPrint = text => Trace.WriteLine("[Addon " + addon.Id + "] " + (text.Length > 2048 ? text.Substring(0, 2048) : text));
        script.Globals.Set("collectgarbage", DynValue.Nil);
        script.Globals.Get("math").Table.Set("frexp", DynValue.Nil);
        script.Globals.Get("math").Table.Set("ldexp", DynValue.Nil);
        AddBoundedLibraries(script, addon);
        var metadata = new Table(script);
        metadata.Set("id", DynValue.NewString(addon.Id)); metadata.Set("version", DynValue.NewString(addon.Version));
        metadata.Set("yield", DynValue.NewCallback((ctx, args) => DynValue.NewYieldReq(Array.Empty<DynValue>())));
        Bind(metadata, "log", addon, null, args =>
        { script.Options.DebugPrint(LuaValue.String(args[0], "message", 2048)); return DynValue.Nil; });
        script.Globals.Set("addon", DynValue.NewTable(metadata));
        // print must have the same native-call/output bound as explicit logging.
        script.Globals.Set("print", metadata.Get("log"));
        var game = new Table(script);
        Bind(game, "observe", addon, "game.observe", args =>
        {
            var snapshot = _game.Observe(new GameQuery(LuaValue.String(args[0], "domain", 128), LuaValue.Object(args[1])));
            return LuaValue.ToLua(script, new Dictionary<string, object> { { "domain", snapshot.Domain }, { "sceneRevision", snapshot.SceneRevision },
                { "revision", snapshot.Revision }, { "data", snapshot.Data } });
        });
        Bind(game, "actions", addon, "game.observe", args =>
        {
            var items = new List<object>();
            foreach (var action in _game.GetActions()) items.Add(new Dictionary<string, object> { { "name", action.Name }, { "description", action.Description },
                { "arguments", action.Arguments }, { "available", action.Available }, { "unavailableReason", action.UnavailableReason } });
            return LuaValue.ToLua(script, items);
        });
        Bind(game, "queries", addon, "game.observe", args =>
        {
            var catalog = _game as IGameObservationCatalog;
            var items = new List<object>();
            if (catalog != null)
                foreach (var query in catalog.GetQueries()) items.Add(new Dictionary<string, object>
                { { "domain", query.Domain }, { "description", query.Description }, { "arguments", query.Arguments } });
            return LuaValue.ToLua(script, items);
        });
        Bind(game, "act", addon, "game.act", args =>
        {
            if (addon.Stopping) throw new ScriptRuntimeException("Actions are unavailable during addon cleanup.");
            long? revision = args[3].IsNil() ? (long?)null : CheckedLong(args[3]);
            var request = new GameActionRequest(LuaValue.String(args[0], "action", 128), LuaValue.Object(args[1]),
                args[2].IsNil() ? null : addon.Id + ":" + LuaValue.String(args[2], "requestId", 127 - addon.Id.Length), revision);
            foreach (string previous in addon.Actions.ToArray())
            {
                var state = _game.GetAction(previous);
                if (state == null || (state.State != GameActionState.Queued && state.State != GameActionState.Running)) addon.Actions.Remove(previous);
            }
            if (addon.Actions.Count >= 128) throw new ScriptRuntimeException("Too many outstanding addon actions.");
            var receipt = _game.Execute(request);
            if (receipt != null && !string.IsNullOrEmpty(receipt.ActionId) &&
                (receipt.State == GameActionState.Queued || receipt.State == GameActionState.Running || receipt.State == GameActionState.Sent))
                addon.Actions.Add(receipt.ActionId);
            return LuaValue.ToLua(script, ReceiptData(receipt));
        });
        Bind(game, "action_status", addon, "game.observe", args => LuaValue.ToLua(script, ReceiptData(_game.GetAction(LuaValue.String(args[0], "actionId", 128)))));
        Bind(game, "cancel", addon, "game.act", args =>
        {
            string id = LuaValue.String(args[0], "actionId", 128);
            return DynValue.NewBoolean(addon.Actions.Contains(id) && _game.CancelAction(id));
        });
        Bind(game, "events", addon, "game.events", args =>
        {
            long after = args[0].IsNil() ? 0 : CheckedLong(args[0]);
            int maximum = (int)Math.Max(1, Math.Min(64, LuaValue.Number(args[1], 32)));
            var batch = _game.ReadEvents(after, maximum); var events = new List<object>();
            foreach (var item in batch.Events) events.Add(EventData(item));
            return LuaValue.ToLua(script, new Dictionary<string, object> { { "events", events }, { "nextSequence", batch.NextSequence }, { "resyncRequired", batch.ResyncRequired } });
        });
        Bind(game, "subscribe", addon, "game.events", args =>
        {
            if (addon.Stopping || addon.Subscriptions.Count >= 6) throw new ScriptRuntimeException("Event subscription limit exceeded or addon is stopping.");
            if (args[1].Type != DataType.Function) throw new ScriptRuntimeException("Subscription callback must be a Lua function.");
            long id = ++addon.SubscriptionId;
            addon.Subscriptions.Add(id, new Subscription { Name = LuaValue.String(args[0], "event name", 128), Callback = args[1] });
            return DynValue.NewNumber(id);
        });
        Bind(game, "unsubscribe", addon, "game.events", args => DynValue.NewBoolean(addon.Subscriptions.Remove(CheckedLong(args[0]))));
        script.Globals.Set("game", DynValue.NewTable(game));
        var storage = new Table(script);
        Bind(storage, "get", addon, "storage.local", args => LuaValue.ToLua(script, addon.Storage.Get(LuaValue.String(args[0], "key", 128))));
        Bind(storage, "set", addon, "storage.local", args =>
        { addon.Storage.Set(LuaValue.String(args[0], "key", 128), LuaValue.FromLua(args[1])); return DynValue.Nil; });
        script.Globals.Set("storage", DynValue.NewTable(storage));
        AddUi(script, addon);
        return script;
    }
    private void Bind(Table table, string name, Instance addon, string capability, Func<CallbackArguments, DynValue> callback)
    {
        table.Set(name, DynValue.NewCallback((context, args) =>
        {
            if (++addon.NativeCalls > 64) throw new ScriptRuntimeException("Native API call budget exceeded.");
            if (capability != null && !addon.Package.Capabilities.Contains(capability)) throw new ScriptRuntimeException("Missing manifest capability: " + capability);
            try { return callback(args); }
            catch (ScriptRuntimeException) { throw; }
            catch (Exception ex) { throw new ScriptRuntimeException(ShortError(ex)); }
        }));
    }
    private void AddUi(Script script, Instance addon)
    {
        var ui = new Table(script);
        Bind(ui, "panel", addon, "ui.panel", args =>
        {
            var definition = RequireTable(args[0]); string id = PanelId(addon, definition.Get("id"));
            CheckNewPanel(addon, id);
            var handle = _ui.RegisterPanel(new PanelDefinition(id, LuaValue.String(definition.Get("title"), "title", 128),
                definition.Get("text").IsNil() ? "" : LuaValue.String(definition.Get("text"), "text", 16384)));
            addon.Panels.Add(id, handle); return DynValue.Nil;
        });
        Bind(ui, "widget_panel", addon, "ui.panel", args =>
        {
            if (_widgets == null) throw new ScriptRuntimeException("Widget UI is unavailable.");
            var definition = RequireTable(args[0]); string id = PanelId(addon, definition.Get("id")); CheckNewPanel(addon, id);
            var controls = new List<PluginWidgetDefinition>(); var raw = definition.Get("widgets");
            if (!raw.IsNil())
            {
                var children = RequireTable(raw);
                foreach (var pair in children.Pairs)
                {
                    if (controls.Count >= 64) throw new ScriptRuntimeException("A panel supports at most 64 widgets.");
                    var widget = RequireTable(pair.Value);
                    PluginWidgetKind kind;
                    if (!Enum.TryParse(LuaValue.String(widget.Get("kind"), "kind", 32), true, out kind)) throw new ScriptRuntimeException("Unknown widget kind.");
                    controls.Add(new PluginWidgetDefinition(LocalId(widget.Get("id")), kind,
                        widget.Get("text").IsNil() ? "" : LuaValue.String(widget.Get("text"), "text", 16384),
                        Dimension(widget.Get("x"), 0, false), Dimension(widget.Get("y"), 0, false),
                        Dimension(widget.Get("width"), 200, true), Dimension(widget.Get("height"), 24, true),
                        LuaValue.Number(widget.Get("progress")), widget.Get("enabled").IsNil() || widget.Get("enabled").CastToBool()));
                }
            }
            PluginAnchor anchor = PluginAnchor.TopLeft;
            if (!definition.Get("anchor").IsNil() && !Enum.TryParse(LuaValue.String(definition.Get("anchor"), "anchor", 32), true, out anchor))
                throw new ScriptRuntimeException("Unknown panel anchor.");
            var panel = new PluginPanelDefinition(id, LuaValue.String(definition.Get("title"), "title", 128),
                Dimension(definition.Get("width"), 300, true), Dimension(definition.Get("height"), 180, true), anchor,
                Dimension(definition.Get("x"), 24, false), Dimension(definition.Get("y"), 90, false),
                definition.Get("draggable").IsNil() || definition.Get("draggable").CastToBool(), controls);
            addon.Panels.Add(id, _widgets.RegisterWidgetPanel(panel)); return DynValue.Nil;
        });
        Bind(ui, "set_text", addon, "ui.panel", args =>
        { _ui.SetPanelText(OwnedPanel(addon, args[0]), LuaValue.String(args[1], "text", 16384)); return DynValue.Nil; });
        Bind(ui, "set_widget_text", addon, "ui.panel", args =>
        {
            if (_widgets == null) throw new ScriptRuntimeException("Widget UI is unavailable.");
            _widgets.SetWidgetText(OwnedPanel(addon, args[0]), LocalId(args[1]), LuaValue.String(args[2], "text", 16384)); return DynValue.Nil;
        });
        Bind(ui, "set_progress", addon, "ui.panel", args =>
        {
            if (_widgets == null) throw new ScriptRuntimeException("Widget UI is unavailable.");
            _widgets.SetWidgetProgress(OwnedPanel(addon, args[0]), LocalId(args[1]), LuaValue.Number(args[2])); return DynValue.Nil;
        });
        Bind(ui, "show", addon, "ui.panel", args =>
        {
            if (_widgets == null) throw new ScriptRuntimeException("Widget UI is unavailable.");
            _widgets.SetPanelVisible(OwnedPanel(addon, args[0]), args[1].CastToBool()); return DynValue.Nil;
        });
        Bind(ui, "close", addon, "ui.panel", args =>
        { string id = OwnedPanel(addon, args[0]); addon.Panels[id].Dispose(); addon.Panels.Remove(id); return DynValue.Nil; });
        script.Globals.Set("ui", DynValue.NewTable(ui));
    }
    private static void CheckNewPanel(Instance addon, string id)
    {
        if (addon.Stopping || addon.Panels.Count >= 8) throw new ScriptRuntimeException("Panel limit exceeded or addon is stopping.");
        if (addon.Panels.ContainsKey(id)) throw new ScriptRuntimeException("Panel already exists.");
    }
    private static Table RequireTable(DynValue value)
    { if (value.Type != DataType.Table) throw new ScriptRuntimeException("Expected table."); return value.Table; }
    private static string LocalId(DynValue value)
    { string id = LuaValue.String(value, "id", 64); if (!AddonPackage.ValidId(id)) throw new ScriptRuntimeException("Invalid local ID."); return id; }
    private static string PanelId(Instance addon, DynValue value) { return addon.Id + ":" + LocalId(value); }
    private static string OwnedPanel(Instance addon, DynValue value)
    { string id = PanelId(addon, value); if (!addon.Panels.ContainsKey(id)) throw new ScriptRuntimeException("Panel is not owned by this addon."); return id; }
    private static float Dimension(DynValue value, float fallback, bool positive)
    {
        double result = LuaValue.Number(value, fallback);
        if (Math.Abs(result) > 4096 || (positive && result <= 0)) throw new ScriptRuntimeException("UI size or coordinate is out of range.");
        return (float)result;
    }
    private static long CheckedLong(DynValue value)
    {
        if (value.Type == DataType.String)
        {
            long precise;
            if (long.TryParse(value.String, NumberStyles.None, CultureInfo.InvariantCulture, out precise) && precise >= 0) return precise;
            throw new ScriptRuntimeException("Expected nonnegative Int64 decimal string.");
        }
        double number = LuaValue.Number(value);
        if (number < 0 || number > 9007199254740991d || number != Math.Floor(number)) throw new ScriptRuntimeException("Expected nonnegative safe integer.");
        return (long)number;
    }
    private static Dictionary<string, object> ReceiptData(GameActionReceipt value)
    {
        if (value == null) return null;
        return new Dictionary<string, object> { { "actionId", value.ActionId }, { "requestId", value.RequestId }, { "name", value.Name },
            { "state", value.State.ToString().ToLowerInvariant() }, { "code", value.Code }, { "message", value.Message }, { "data", value.Data } };
    }
    private static Dictionary<string, object> EventData(GameEvent value)
    { return new Dictionary<string, object> { { "sequence", value.Sequence }, { "sceneRevision", value.SceneRevision }, { "name", value.Name }, { "data", value.Data } }; }
    private sealed class DeniedLoader : IScriptLoader
    {
        public object LoadFile(string file, Table context) { throw new ScriptRuntimeException("Addon file loading is disabled."); }
        public string ResolveFileName(string file, Table context) { throw new ScriptRuntimeException("Addon file loading is disabled."); }
        public string ResolveModuleName(string module, Table context) { throw new ScriptRuntimeException("Addon module loading is disabled."); }
    }
}
}
