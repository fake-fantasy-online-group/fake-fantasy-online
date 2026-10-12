using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.PluginHost;

internal static class AddonRuntimeTests
{
    private static int _checks;
    private static void Check(bool value, string message)
    { _checks++; if (!value) throw new Exception("FAIL: " + message); }
    private sealed class Ui : IPluginWidgetUi
    {
        internal readonly Dictionary<string, string> Panels = new Dictionary<string, string>();
        internal int Created, Disposed;
        public event Action<PluginUiEvent> EventRaised;
        public IDisposable RegisterPanel(PanelDefinition p) { Panels.Add(p.PanelId, p.InitialText); Created++; return new Handle(this, p.PanelId); }
        public IDisposable RegisterWidgetPanel(PluginPanelDefinition p) { Panels.Add(p.PanelId, p.Title); Created++; return new Handle(this, p.PanelId); }
        public void SetPanelText(string id, string text) { Panels[id] = text; }
        public void SetWidgetText(string id, string widget, string text) { Panels[id] = text; }
        public void SetWidgetProgress(string id, string widget, double progress) { }
        public void SetPanelVisible(string id, bool visible) { }
        internal void Click(string panel) { EventRaised?.Invoke(new PluginUiEvent(panel, "button", "click")); }
        private sealed class Handle : IDisposable
        {
            private Ui _ui; private readonly string _id;
            internal Handle(Ui ui, string id) { _ui = ui; _id = id; }
            public void Dispose() { if (_ui == null) return; _ui.Panels.Remove(_id); _ui.Disposed++; _ui = null; }
        }
    }
    private sealed class Game : IGameApi, IGameObservationCatalog
    {
        internal int Actions, Cancellations;
        internal bool Resync;
        internal GameActionRequest LastRequest;
        internal readonly List<GameEvent> Events = new List<GameEvent>();
        public GameSnapshot Observe(GameQuery q) { return new GameSnapshot(q.Domain, 2, 3,
            new Dictionary<string, object> { { "name", "Test Player" }, { "health", 50 }, { "nested", new Dictionary<string, object> { { "value", 7 } } } }); }
        public IReadOnlyList<GameQueryDefinition> GetQueries() { return new[] { new GameQueryDefinition("player", "Player") }; }
        public IReadOnlyList<GameActionDefinition> GetActions() { return new[] { new GameActionDefinition("target.set", "Target") }; }
        public GameActionReceipt Execute(GameActionRequest request) { Actions++; LastRequest = request; return GetAction("action-1"); }
        public GameActionReceipt GetAction(string id) { return new GameActionReceipt(id, "req", "target.set", GameActionState.Sent, "sent", "Sent only"); }
        public bool CancelAction(string id) { Cancellations++; return true; }
        public GameEventBatch ReadEvents(long after, int max)
        { var events = Events.Where(e => e.Sequence > after).Take(max).ToArray(); return new GameEventBatch(events, events.Length == 0 ? after : events.Last().Sequence, Resync); }
    }
    private static string Manifest(string id, string[] caps, string[] deps = null)
    { return GameJson.Serialize(new Dictionary<string, object> { { "id", id }, { "name", id }, { "version", "1" }, { "apiVersion", "1.0" },
        { "entry", "main.lua" }, { "capabilities", caps }, { "dependencies", deps ?? new string[0] } }); }
    private static void Package(string root, string id, string source, string[] caps = null, string[] deps = null)
    { string path = Path.Combine(root, id); Directory.CreateDirectory(path); File.WriteAllText(Path.Combine(path, "addon.json"), Manifest(id, caps ?? new string[0], deps)); File.WriteAllText(Path.Combine(path, "main.lua"), source); }
    private static AddonStatus Status(AddonHost host, string id) { return host.Statuses.Single(s => s.Id == id); }
    private static void Pump(AddonHost host, int count = 20) { for (int i = 0; i < count; i++) host.Tick(0.016f); }
    private static Dictionary<string, object> Saved(string config, string id)
    { return (Dictionary<string, object>)GameJson.Parse(File.ReadAllText(Path.Combine(config, id, "storage.json"))); }
    public static int Main(string[] args)
    {
        string temporary = Path.Combine(Path.GetTempPath(), "ffo-addon-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temporary);
        try
        {
            LifecycleAndIsolation(temporary); DependenciesAndReload(temporary); BudgetsAndSandbox(temporary);
            var numericScript = new MoonSharp.Interpreter.Script(MoonSharp.Interpreter.CoreModules.None);
            Check(LuaValue.ToLua(numericScript, long.MaxValue).String == long.MaxValue.ToString(), "large Int64 retains precision as decimal string");
            Check(LuaValue.ToLua(numericScript, ulong.MaxValue).String == ulong.MaxValue.ToString(), "large UInt64 retains precision as decimal string");
            Check(LuaValue.ToLua(numericScript, (byte)4).Number == 4, "small primitive numbers retain numeric type");
            BoundaryValidation(temporary); if (args.Length > 0) BundledExamples(args[0], temporary);
            Console.WriteLine("PASS: " + _checks + " addon runtime checks (real Lua interpreter; fake game/UI; no Unity build)"); return 0;
        }
        finally { Directory.Delete(temporary, true); }
    }
    private static void LifecycleAndIsolation(string temporary)
    {
        string root = Path.Combine(temporary, "lifecycle"), config = Path.Combine(temporary, "config");
        Package(root, "first", @"
secret = 42
function on_load() storage.set('phase', 'loaded') end
function on_enable()
 assert(storage.get('phase') == 'loaded')
 local player = game.observe('player').data
 assert(player.name == 'Test Player' and player.nested.value == 7)
 assert(#game.queries() == 1 and game.actions()[1].available)
 local result = game.act('target.set', {entityId='entity-2'}, 'test', 2)
 assert(result.state == 'sent' and game.action_status(result.actionId).state == 'sent')
 assert(game.cancel(result.actionId))
 ui.widget_panel({id='main',title='First',widgets={{id='button',kind='button',text='Go'}}})
 storage.set('phase', 'enabled')
end
function on_event(e) storage.set('event', e.sequence) end
function on_ui(e) storage.set('click', e.panelId .. ':' .. e.name) end
function on_disable() storage.set('phase', 'disabled') end
function on_unload() storage.set('unloaded', true) end
", new[] { "game.observe", "game.act", "game.events", "storage.local", "ui.panel" });
        Package(root, "second", @"
assert(secret == nil)
function on_enable()
 ui.panel({id='main',title='Second',text='isolated'})
 game.subscribe('hello', function(e) storage.set('received', e.sequence) end)
 game.subscribe('events.resync_required', function(e) storage.set('resync', true) end)
end
", new[] { "game.events", "storage.local", "ui.panel" });
        var game = new Game(); var ui = new Ui();
        using (var host = new AddonHost(root, config, game, ui))
        {
            Check(host.Discover().Count == 2, "discovery finds both packages");
            Check(host.Enable("first") && host.Enable("second"), "enable accepted");
            Check(Status(host, "first").State == AddonState.Loading, "enable is asynchronous");
            Pump(host); Check(Status(host, "first").State == AddonState.Enabled, "all lifecycle calls complete: " + Status(host, "first").Error);
            Check(Status(host, "second").State == AddonState.Enabled, "globals are isolated");
            Check(game.Actions == 1 && game.LastRequest.ExpectedSceneRevision == 2 && game.LastRequest.RequestId == "first:test", "action bridge preserves revision and namespaces requests");
            Check(ui.Panels.ContainsKey("first:main") && ui.Panels.ContainsKey("second:main"), "UI IDs isolated");
            game.Events.Add(new GameEvent(1, 2, "hello", new Dictionary<string, object>())); Pump(host);
            Check(Convert.ToInt64(Saved(config, "first")["event"]) == 1, "global event callback delivered");
            Check(Convert.ToInt64(Saved(config, "second")["received"]) == 1, "independent addon event cursor delivered same event");
            ui.Click("first:main"); Pump(host); Check((string)Saved(config, "first")["click"] == "main:click", "UI callback receives local panel ID");
            game.Resync = true; Pump(host); game.Resync = false;
            Check((bool)Saved(config, "second")["resync"], "subscription-only addon receives resync notice");
            int cancelledBefore = game.Cancellations;
            Check(host.Disable("first"), "disable accepted");
            Check(game.Cancellations > cancelledBefore, "disable attempts cancellation of addon-owned actions");
            Check((string)Saved(config, "first")["phase"] == "disabled" && (bool)Saved(config, "first")["unloaded"], "cleanup lifecycle invoked");
            Check(!ui.Panels.ContainsKey("first:main") && ui.Panels.ContainsKey("second:main"), "disable releases only owned UI");
            ui.Click("first:main"); Pump(host); Check(Status(host, "first").State == AddonState.Disabled, "disabled UI callback not invoked");
            Check(host.Enable("first"), "reenable accepted"); Pump(host); Check(Status(host, "first").State == AddonState.Enabled, "reenable completed without duplicate UI");
            Exception error = null; var thread = new Thread(() => { try { host.Tick(0); } catch (Exception e) { error = e; } }); thread.Start(); thread.Join();
            Check(error is InvalidOperationException, "cross-thread runtime calls rejected");
        }
        Check(ui.Panels.Count == 0 && ui.Created == ui.Disposed, "dispose releases every UI handle exactly once");
    }
    private static void DependenciesAndReload(string temporary)
    {
        string root = Path.Combine(temporary, "dependencies"), config = Path.Combine(temporary, "depconfig");
        Package(root, "base", "function on_enable() ui.panel({id='main',title='base',text='old'}) end", new[] { "ui.panel" });
        Package(root, "child", "function on_enable() ui.panel({id='main',title='child',text='child'}) end", new[] { "ui.panel" }, new[] { "base" });
        Package(root, "missing", "", null, new[] { "absent" });
        Package(root, "cycle-a", "", null, new[] { "cycle-b" }); Package(root, "cycle-b", "", null, new[] { "cycle-a" });
        var ui = new Ui();
        using (var host = new AddonHost(root, config, new Game(), ui))
        {
            host.Discover(); Check(host.Enable("child"), "dependency enable accepted"); Pump(host);
            Check(Status(host, "base").State == AddonState.Enabled && Status(host, "child").State == AddonState.Enabled, "dependencies loaded before dependent");
            Check(!host.Enable("missing") && Status(host, "missing").State == AddonState.Faulted, "missing dependency rejected");
            Check(!host.Enable("cycle-a"), "dependency cycle rejected without recursion overflow");
            File.WriteAllText(Path.Combine(root, "base", "main.lua"), "function on_enable() ui.panel({id='main',title='base',text='new'}) end");
            Check(host.Reload("base"), "reload accepted"); Pump(host);
            Check(ui.Panels["base:main"] == "new" && ui.Panels.ContainsKey("child:main"), "reload replaces source and restores active dependents");
            host.Disable("base"); Check(ui.Panels.Count == 0 && Status(host, "child").State == AddonState.Disabled, "disabling dependency stops dependent");
        }
    }
    private static void BudgetsAndSandbox(string temporary)
    {
        string root = Path.Combine(temporary, "budgets"), config = Path.Combine(temporary, "budgetconfig");
        Package(root, "runaway", "while true do end");
        Package(root, "yielding", "while true do addon.yield() end");
        Package(root, "healthy", "function on_enable() ui.panel({id='main',title='ok',text='ok'}) end", new[] { "ui.panel" });
        Package(root, "huge-string", "string.rep('x', 1000000000000)");
        Package(root, "concat-bomb", "local s='x'; while true do s=s..s end");
        Package(root, "table-bomb", "local t={}; for i=1,1000000 do t[i]=i end");
        Package(root, "huge-table", "table.insert({}, 1000000000000, 'x')");
        Package(root, "unsafe", "assert(io == nil and os == nil and require == nil and load == nil and debug == nil and coroutine == nil and pcall == nil and setmetatable == nil and table.sort == nil and string.gsub == nil and unpack == nil and collectgarbage == nil)");
        Package(root, "cleanup", "function on_enable() ui.panel({id='main',title='x'}) end\nfunction on_disable() while true do end end", new[] { "ui.panel" });
        Package(root, "fault-ui", "function on_enable() ui.panel({id='main',title='x'}) end\nfunction on_update() error('isolated failure') end", new[] { "ui.panel" });
        Package(root, "denied", "game.observe('player')");
        Package(root, "bounded", "assert(string.rep('ab',3,'-') == 'ab-ab-ab')\nlocal a={1,2}; table.insert(a,2,3); assert(table.remove(a,2)==3 and table.concat(a,',')=='1,2')");
        var ui = new Ui();
        using (var host = new AddonHost(root, config, new Game(), ui))
        {
            host.Discover(); foreach (var status in host.Statuses) host.Enable(status.Id);
            var timer = Stopwatch.StartNew(); Pump(host, 100); timer.Stop();
            Check(timer.Elapsed.TotalSeconds < 5, "runaway tests return promptly without hanging main thread");
            Check(Status(host, "runaway").State == AddonState.Faulted && Status(host, "runaway").Error.Contains("budget"), "infinite entry instruction budget enforced");
            Check(Status(host, "yielding").State == AddonState.Faulted, "endless explicit yielding callback eventually faults");
            Check(Status(host, "healthy").State == AddonState.Enabled, "runaway script does not starve healthy addon");
            Check(Status(host, "huge-string").State == AddonState.Faulted, "huge repetition rejected before allocation");
            Check(Status(host, "concat-bomb").State == AddonState.Faulted && Status(host, "concat-bomb").Error.Contains("concatenation"), "VM concat growth capped before allocation");
            Check(Status(host, "table-bomb").State == AddonState.Faulted && Status(host, "table-bomb").Error.Contains("entry limit"), "VM table growth capped before allocation");
            Check(Status(host, "huge-table").State == AddonState.Faulted, "huge native table index rejected");
            Check(Status(host, "unsafe").State == AddonState.Enabled, "unsafe libraries not exposed");
            Check(Status(host, "bounded").State == AddonState.Enabled, "bounded string and table helpers work");
            Check(Status(host, "denied").State == AddonState.Faulted && Status(host, "denied").Error.Contains("capability"), "manifest capability enforced");
            Check(Status(host, "fault-ui").State == AddonState.Faulted && !ui.Panels.ContainsKey("fault-ui:main"), "script callback error releases UI");
            host.Disable("cleanup"); Check(!ui.Panels.ContainsKey("cleanup:main") && Status(host, "cleanup").State == AddonState.Disabled, "runaway cleanup still releases UI immediately");
        }
    }
    private static void BoundaryValidation(string temporary)
    {
        string root = Path.Combine(temporary, "validation"), config = Path.Combine(temporary, "validationconfig");
        Package(root, "cycle-data", "local t={};t.self=t;storage.set('x',t)", new[] { "storage.local" });
        Package(root, "mixed-data", "storage.set('x',{[1]='a',name='b'})", new[] { "storage.local" });
        Package(root, "quota", "storage.set('a',string.rep('x',32768)); storage.set('b',string.rep('x',32768))", new[] { "storage.local" });
        Package(root, "syntax", "function invalid(( ");
        Package(root, "large", new string(' ', 131073));
        Package(root, "traversal", "");
        File.WriteAllText(Path.Combine(root, "traversal", "addon.json"), Manifest("traversal", new string[0]).Replace("main.lua", "../main.lua"));
        using (var host = new AddonHost(root, config, new Game(), new Ui()))
        {
            host.Discover(); Check(Status(host, "traversal").State == AddonState.Faulted, "manifest path traversal rejected");
            foreach (var status in host.Statuses) host.Enable(status.Id); Pump(host);
            foreach (var id in new[] { "cycle-data", "mixed-data", "quota", "syntax", "large" })
                Check(Status(host, id).State == AddonState.Faulted, "invalid input isolated: " + id);
            Check(new FileInfo(Path.Combine(config, "quota", "storage.json")).Length <= 65536, "over-quota write preserves valid prior storage");
        }
    }
    private static void BundledExamples(string root, string temporary)
    {
        var game = new Game(); var ui = new Ui();
        using (var host = new AddonHost(root, Path.Combine(temporary, "examples"), game, ui))
        {
            var statuses = host.Discover(); Check(statuses.Count >= 3, "bundled examples discovered");
            foreach (var status in statuses) Check(host.Enable(status.Id), "bundled example accepted: " + status.Id + " " + status.Error);
            Pump(host, 100);
            foreach (var status in host.Statuses) Check(status.State == AddonState.Enabled, "bundled example enabled: " + status.Id + " " + status.Error);
            Check(ui.Panels.Count >= 3, "bundled examples create UI");
        }
    }
}
