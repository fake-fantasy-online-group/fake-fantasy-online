// Standalone fake-game integration tests. No Unity or game service is started.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.PluginHost;

internal static class AgentTransportContract
{
    private static int assertions;
    private static readonly UTF8Encoding Utf8 = new UTF8Encoding(false, true);

    public static int Main(string[] args)
    {
        try
        {
            Source = args[0];
            if (args.Length > 1 && args[1] == "--python-fixture") return RunPythonFixture();
            SensitiveLoginIsEphemeral(); RoundTripsAndIndependentCursors(); LuaLifecycleAndExactNumbers();
            MalformedFramesAndBounds(); TinyResponseBudget(); DispatchTimeoutAndOverload(); ClientAdmissionAndIdleTimeout();
            CancelQueuedLuaCallbacks();
            Console.WriteLine("Lua agent transport contract passed: " + assertions + " assertions."); return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
    private static string Source;
    private static int RunPythonFixture()
    {
        var api = new FakeGame(); for (int i=0;i<6;i++) api.Publish("fixture.event",Map("index",i));
        using (var fixture = new Fixture(api)) {
            fixture.Start(); Console.WriteLine(fixture.Port); Console.Out.Flush();
            var stop = Task.Run(() => Console.ReadLine());
            while (!stop.IsCompleted) { fixture.Pump(); Thread.Sleep(1); }
            Check(!api.WrongThread, "Python calls stay on owning thread");
        }
        return 0;
    }
    private sealed class Ui : IPluginUi {
        public IDisposable RegisterPanel(PanelDefinition panel) { throw new NotSupportedException(); }
        public void SetPanelText(string id, string text) { throw new NotSupportedException(); }
    }
    private sealed class Fixture : IDisposable
    {
        private readonly string root = Path.Combine(Path.GetTempPath(), "ffo-lua-agent-" + Guid.NewGuid().ToString("N"));
        internal readonly AddonHost Host;
        internal readonly int Port;
        internal bool IsRunning => Host.Statuses[0].State == AddonState.Enabled;
        internal Fixture(FakeGame api, LoopbackJsonServerOptions options = null, string removeCapability = null)
        {
            var probe = new TcpListener(IPAddress.Loopback,0); probe.Start(); Port=((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
            var addon = Path.Combine(root,"addons","agent-bridge"); Directory.CreateDirectory(addon);
            var manifest = Object(GameJson.Parse(File.ReadAllText(Path.Combine(Source,"addon.json"))));
            if (removeCapability != null) ((List<object>)manifest["capabilities"]).Remove(removeCapability);
            File.WriteAllText(Path.Combine(addon,"addon.json"),GameJson.Serialize(manifest));
            File.WriteAllText(Path.Combine(addon,"main.lua"),File.ReadAllText(Path.Combine(Source,"main.lua")).Replace("loopback.listen(17845,", "loopback.listen("+Port+","));
            Host = new AddonHost(Path.Combine(root,"addons"),Path.Combine(root,"config"),api,new Ui(),options); Host.Discover();
        }
        internal void Start() { Check(Host.Enable("agent-bridge"), "enable accepted"); for (int i=0;i<20 && !IsRunning;i++) Pump(); Check(IsRunning,"addon enabled: "+Host.Statuses[0].Error); }
        internal void Pump() { Host.Tick(0.01f); }
        internal void Stop() { Host.Disable("agent-bridge"); }
        internal void CheckPortFree() { var probe=new TcpListener(IPAddress.Loopback,Port); probe.Start(); probe.Stop(); Check(true,"disabled port can be rebound"); }
        public void Dispose() { Host.Dispose(); Directory.Delete(root,true); }
    }
    private static void CancelQueuedLuaCallbacks()
    {
        using(var fixture = new Fixture(new FakeGame())) {
            fixture.Start();
            var peers = new List<Peer>();
            for(int i=0;i<5;i++) { var peer=new Peer(fixture.Port); peers.Add(peer); peer.Send("describe",Map()); }
            Thread.Sleep(60); fixture.Pump();
            foreach(var peer in peers) peer.Dispose();
            Thread.Sleep(60);
            for(int i=0;i<10;i++) fixture.Pump();
            Check(fixture.IsRunning,"disconnected queued Lua callbacks must not fault addon: "+fixture.Host.Statuses[0].Error);
            using(var live=new Peer(fixture.Port)) Check((bool)live.Call(fixture,"describe",Map())["ok"],"other clients work after disconnect cancellation");
        }
    }

    private static void LuaLifecycleAndExactNumbers()
    {
        var api = new FakeGame();
        using (var fixture = new Fixture(api)) {
            fixture.Start();
            using (var peer = new Peer(fixture.Port)) {
                var large = 9007199254740993L;
                var sent=Result(peer.Call(fixture,"act",Map("name","fake.step","requestId","exact","expectedSceneRevision",large,"arguments",Map("x",large))));
                Check(api.LastRevision == large && api.LastX == large,"exact Int64 survives Lua opaque request routing");
                Check((string)sent["requestId"]=="exact","external retry identity is not addon-prefixed");
                Check(Error(peer.Call(fixture,"events",Map("afterSequence",0,"maxCount",513)))=="invalid_params","event cap retained");
                Check(Error(peer.Raw(fixture,"{\"id\":\"bad\",\"method\":\"observe\",\"params\":{\"domain\":\"player\"},\"extra\":1}\n"))=="invalid_params","unknown envelope rejected");
                Check(Error(peer.Raw(fixture,"{\"id\":\"bad\",\"id\":\"twice\"}\n"))=="invalid_json","duplicate JSON keys rejected");
                int before=api.Executions; peer.Send("act",Map("name","fake.step","requestId","must-not-run")); Thread.Sleep(30);
                fixture.Stop(); fixture.Pump(); Check(api.Executions==before,"disable cancels unpumped request");
            }
            fixture.CheckPortFree(); fixture.Start();
            using(var peer=new Peer(fixture.Port)) Check((bool)peer.Call(fixture,"describe",Map())["ok"],"reenable opens fresh working listener");
            fixture.Host.Reload("agent-bridge"); for(int i=0;i<20;i++) fixture.Pump();
            using(var peer=new Peer(fixture.Port)) Check((bool)peer.Call(fixture,"describe",Map())["ok"],"reload closes and recreates listener");
        }
        using(var fixture=new Fixture(new FakeGame(),removeCapability:"game.sensitive-login")) {
            fixture.Start(); using(var peer=new Peer(fixture.Port)) {
                Check(!(bool)Result(peer.Call(fixture,"describe",Map()))["sensitiveLoginAvailable"],"login capability absent from discovery");
                Check(!(bool)peer.Call(fixture,"sensitive-login",Map())["ok"],"login denied without permission");
            }
        }
    }
    private static void SensitiveLoginIsEphemeral()
    {
        const string secret = "dummysecret";
        var api = new FakeGame();
        using (var dispatcher = new Fixture(api, new LoopbackJsonServerOptions(dispatchTimeoutMilliseconds: 100)))
        {
            dispatcher.Start();
            using (var peer = new Peer(dispatcher.Port))
            {
                var fields = Map("account", "dummyaccount", "password", secret,
                    "expectedTitleInstanceId", "title1", "expectedTitleRevision", "rev1");
                var sent = peer.Call(dispatcher, "sensitive-login", fields);
                Check((bool)Result(sent)["submitted"] && api.LoginSubmissions == 1, "ephemeral native submission");
                Check(!GameJson.Serialize(sent).Contains(secret) && api.Executions == 0, "no secret response or retained action");
                Check(!api.WrongThread, "sensitive login runs on game thread");
                var ordinary = peer.Call(dispatcher, "act", Map("name", "fake.step", "requestId", "sensitive-act",
                    "arguments", Map("nested", Map("password", secret))));
                Check(Error(ordinary) == "sensitive_arguments" && api.Executions == 0, "act credentials rejected before durable API");
                fields[secret] = secret;
                var invalid = peer.Call(dispatcher, "sensitive-login", fields);
                Check(Error(invalid) == "invalid_params" && !GameJson.Serialize(invalid).Contains(secret), "unknown sensitive field never echoed");
                fields.Remove(secret);
                api.LoginThrows = true;
                var failed = peer.Call(dispatcher, "sensitive-login", fields);
                Check(Error(failed) == "invalid_params" && !GameJson.Serialize(failed).Contains(secret), "native exception is redacted");
                api.LoginThrows = false;
                var submitted = api.LoginSubmissions;
                peer.Send("sensitive-login", fields);
                var expired = ReadResponse(peer.Read(), null);
                Check(Error(expired) == "dispatch_timeout", "sensitive queued request times out");
                dispatcher.Pump();
                Check(api.LoginSubmissions == submitted, "timed-out credential submission never runs later");
                Check(!GameJson.Serialize(peer.Call(dispatcher, "observe", Map("domain", "player"))).Contains(secret), "observations omit credentials");
                Check(!GameJson.Serialize(peer.Call(dispatcher, "events", Map("afterSequence", 0))).Contains(secret), "events omit credentials");
            }
        }
    }

    private static void RoundTripsAndIndependentCursors()
    {
        var api = new FakeGame();
        using (var dispatcher = new Fixture(api))
        {
            Check(!dispatcher.IsRunning, "construction does not listen");
            dispatcher.CheckPortFree();
            dispatcher.Start();
            var port = dispatcher.Port;
            dispatcher.Start();
            Check(port > 0 && dispatcher.Port == port, "Start is explicit and idempotent");
            using (var first = new Peer(port))
            using (var second = new Peer(port))
            {
                var described = Result(first.Call(dispatcher, "describe", Map()));
                Check((string)described["protocol"] == "ffo-agent/1", "protocol discovery");
                Check((bool)described["observationDiscoveryAvailable"] && ((List<object>)described["queries"]).Count == 2, "observation schemas discovered");
                var definitions = (List<object>)described["actions"];
                Check(definitions.Count == 3 && !(bool)Object(definitions[2])["available"], "unimplemented action stays disabled");
                var snapshot = Result(first.Call(dispatcher, "observe", Map("domain", "player")));
                Check(Long(snapshot["sceneRevision"]) == 7 && Long(Object(snapshot["data"])["hp"]) == 84, "real fake snapshot values projected");
                var request = Map("name", "fake.step", "arguments", Map("x", 12), "requestId", "agent-one-step-1", "expectedSceneRevision", 7);
                var sent = Result(first.Call(dispatcher, "act", request));
                var actionId = (string)sent["actionId"];
                Check((string)sent["state"] == "sent", "sent never presented as completed");
                var duplicate = Result(second.Call(dispatcher, "act", request));
                Check((string)duplicate["actionId"] == actionId && api.Executions == 1, "repeat across connections uses same actionId, no duplicate execution");
                var conflict = Result(second.Call(dispatcher, "act", Map("name", "fake.step", "arguments", Map("x", 99), "requestId", "agent-one-step-1", "expectedSceneRevision", 7)));
                Check((string)conflict["code"] == "request_id_conflict" && api.Executions == 1, "changed retry rejected");
                var status = Result(first.Call(dispatcher, "action-status", Map("actionId", actionId)));
                Check((string)status["actionId"] == actionId && (string)status["state"] == "sent", "status is receipt lookup");
                var uncancelled = Result(first.Call(dispatcher, "cancel", Map("actionId", actionId)));
                Check(!(bool)uncancelled["cancelled"] && (string)Object(uncancelled["receipt"])["state"] == "sent", "cancel does not undo a sent action");
                var running = Result(first.Call(dispatcher, "act", Map("name", "fake.wait", "requestId", "wait-1")));
                var cancelled = Result(first.Call(dispatcher, "cancel", Map("actionId", running["actionId"])));
                Check((bool)cancelled["cancelled"] && (string)Object(cancelled["receipt"])["state"] == "cancelled", "cancellable local operation cancelled");
                for (var i = 0; i < 6; i++) api.Publish("fake.tick", Map("index", i));
                var eventsOne = Result(first.Call(dispatcher, "events", Map("afterSequence", 0, "maxCount", 2)));
                var eventsTwo = Result(second.Call(dispatcher, "events", Map("afterSequence", 0, "maxCount", 2)));
                Check((bool)eventsOne["resyncRequired"] && ((List<object>)eventsOne["events"]).Count == 2, "bounded journal overflow reports gap");
                Check(GameJson.Serialize(eventsOne) == GameJson.Serialize(eventsTwo), "event clients have independent explicit cursors");
                var tail = Result(first.Call(dispatcher, "events", Map("afterSequence", eventsOne["nextSequence"], "maxCount", 2)));
                Check(!(bool)tail["resyncRequired"] && Long(tail["nextSequence"]) == 6, "cursor advances without consuming events globally");
                Check(Error(first.Call(dispatcher, "act", Map("name", "fake.step"))) == "invalid_params", "act requires retry identity");
                Check(Error(first.Call(dispatcher, "events", Map("afterSequence", 1.5))) == "invalid_params", "fractional cursor rejected");
                Check(Error(first.Call(dispatcher, "observe", Map("domain", "player", "typo", 1))) == "invalid_params", "unknown parameters rejected");
                Check(Error(first.Call(dispatcher, "invented", Map())) == "unknown_method", "unknown method rejected");
                Check(!api.WrongThread, "every game API access ran on dispatcher owner thread");
            }
            dispatcher.Stop();
            Check(!dispatcher.IsRunning, "Stop clears running state");
            dispatcher.Start();
            using (var peer = new Peer(dispatcher.Port)) Check((bool)peer.Call(dispatcher, "describe", Map())["ok"], "Stop then Start reconnects");
        }
    }

    private static void MalformedFramesAndBounds()
    {
        var api = new FakeGame();
        using (var dispatcher = new Fixture(api, new LoopbackJsonServerOptions(maxRequestBytes: 256, maxResponseBytes: 512)))
        {
            dispatcher.Start();
            using (var peer = new Peer(dispatcher.Port))
            {
                Check(Error(peer.Raw(dispatcher, "{\"id\":\"a\",\"id\":\"b\",\"method\":\"describe\"}\n")) == "invalid_json", "duplicate JSON keys rejected");
                peer.Stream.Write(new byte[] { 0xff, 10 }, 0, 2);
                Check(Error(ReadResponse(peer.Read(), dispatcher)) == "invalid_json", "invalid UTF-8 rejected");
                Check(Error(peer.Raw(dispatcher, "[]\n")) == "invalid_params", "request must be object");
                Check(Error(peer.Raw(dispatcher, "{\"id\":\"fraction\",\"method\":\"events\",\"params\":{\"afterSequence\":1.00000000000000000000000000001}}\n")) == "invalid_params", "rounded high-precision fraction cannot become integer cursor");
                Check(Error(peer.Raw(dispatcher, "{\"id\":\"fraction\",\"method\":\"act\",\"params\":{\"name\":\"fake.step\",\"requestId\":\"bad-integer\",\"arguments\":{\"x\":1.00000000000000000000000000001}}}\n")) == "invalid_params", "integer action schema rejects rounded fractions");
                Check(Error(peer.Raw(dispatcher, "{\"id\":\"fraction\",\"method\":\"observe\",\"params\":{\"domain\":\"player\",\"arguments\":{\"limit\":1.00000000000000000000000000001}}}\n")) == "invalid_params", "integer observation schema rejects rounded fractions");
                var oversized = peer.Raw(dispatcher, new string('x', 257) + "\n");
                Check(Error(oversized) == "frame_too_large", "oversized frame rejected before parsing");
                Check(peer.Reader.ReadLine() == null, "oversized frame disconnects");
            }
            using (var peer = new Peer(dispatcher.Port))
            {
                var response = peer.Call(dispatcher, "observe", Map("domain", "large"));
                Check(Error(response) == "response_too_large", "large response bounded rather than truncated");
                var bytes = Utf8.GetBytes("{\"id\":\"片\",\"method\":\"observe\",\"params\":{\"domain\":\"player\"}}\r\n");
                for (var i = 0; i < bytes.Length; i++) peer.Stream.Write(bytes, i, 1);
                var fragmented = ReadResponse(peer.Read(), dispatcher);
                Check((string)fragmented["id"] == "片" && (bool)fragmented["ok"], "fragmented Unicode and CRLF frame correctly");
            }
        }
    }

    private static void TinyResponseBudget()
    {
        using (var dispatcher = new Fixture(new FakeGame(), new LoopbackJsonServerOptions(maxResponseBytes: 256)))
        {
            dispatcher.Start();
            using (var peer = new Peer(dispatcher.Port))
            {
                var request = Map("id", new string('片', 128), "method", "observe", "params", Map("domain", "large"));
                var bytes = Utf8.GetBytes(GameJson.Serialize(request) + "\n");
                peer.Stream.Write(bytes, 0, bytes.Length);
                var read = peer.Read();
                while (!read.IsCompleted) { dispatcher.Pump(); Thread.Sleep(1); }
                var response = read.GetAwaiter().GetResult();
                Check(Utf8.GetByteCount(response) <= 256, "even Unicode correlation errors fit output budget");
                Check(Error(Object(GameJson.Parse(response))) == "response_too_large", "tiny response budget yields complete error");
            }
        }
    }

    private static void DispatchTimeoutAndOverload()
    {
        var api = new FakeGame();
        using (var dispatcher = new Fixture(api, new LoopbackJsonServerOptions(dispatchTimeoutMilliseconds: 100, maxPendingRequestsPerClient: 1)))
        {
            dispatcher.Start();
            using (var peer = new Peer(dispatcher.Port))
            {
                peer.Send("act", Map("name", "fake.step", "requestId", "timeout"));
                var pending = peer.Read();
                Check(Error(ReadResponse(pending, null)) == "dispatch_timeout", "un-pumped request times out");
                dispatcher.Pump();
                Check(api.Executions == 0, "timed out action never executes later");
            }
            using (var peer = new Peer(dispatcher.Port))
            {
                var text = string.Concat(Enumerable.Repeat(GameJson.Serialize(Map("id", "spam", "method", "act", "params", Map("name", "fake.step", "requestId", "overload"))) + "\n", 32));
                var bytes = Utf8.GetBytes(text);
                peer.Stream.Write(bytes, 0, bytes.Length);
                var error = ReadResponse(peer.Read(), null);
                Check(Error(error) == "queue_full", "per-client backlog is bounded");
                Thread.Sleep(50);
                dispatcher.Pump();
                Check(api.Executions == 0, "overload cancels queued callbacks");
            }
        }
    }
    private static void ClientAdmissionAndIdleTimeout()
    {
        var api = new FakeGame();
        using (var dispatcher = new Fixture(api, new LoopbackJsonServerOptions(maxClients: 1, idleTimeoutMilliseconds: 200)))
        {
            dispatcher.Start();
            using (var first = new Peer(dispatcher.Port))
            {
                Thread.Sleep(30);
                using (var excess = new Peer(dispatcher.Port))
                    Check(excess.Reader.ReadLine() == null, "excess client refused without new worker");
                first.Stream.WriteByte((byte)'{');
                Check(first.Reader.ReadLine() == null, "idle partial frame is disconnected");

            }
        }
    }

    private static Dictionary<string, object> ReadResponse(Task<string> read, Fixture dispatcher)
    {
        var watch = Stopwatch.StartNew();
        while (!read.IsCompleted)
        {
            if (dispatcher != null) dispatcher.Pump();
            if (watch.ElapsedMilliseconds > 5000) throw new Exception("Timed out reading test response.");
            Thread.Sleep(1);
        }
        var text = read.GetAwaiter().GetResult();
        if (text == null) throw new Exception("Connection closed before response.");
        return Object(GameJson.Parse(text));
    }
    private static Dictionary<string, object> Object(object value) { return (Dictionary<string, object>)value; }
    private static Dictionary<string, object> Result(Dictionary<string, object> response)
    {
        Check((bool)response["ok"], "request succeeded: " + GameJson.Serialize(response));
        return Object(response["result"]);
    }
    private static string Error(Dictionary<string, object> response)
    {
        Check(!(bool)response["ok"], "request failed as expected");
        return (string)Object(response["error"])["code"];
    }
    private static long Long(object value) { return Convert.ToInt64(value); }
    private static Dictionary<string, object> Map(params object[] values)
    {
        var map = new Dictionary<string, object>();
        for (var i = 0; i < values.Length; i += 2) map.Add((string)values[i], values[i + 1]);
        return map;
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception("Assertion failed: " + message);
        assertions++;
    }
    private static void Eventually(Func<bool> predicate, string message)
    {
        var watch = Stopwatch.StartNew();
        while (!predicate())
        {
            if (watch.ElapsedMilliseconds > 4000) throw new Exception("Timed out: " + message);
            Thread.Sleep(1);
        }
        assertions++;
    }

    private sealed class Peer : IDisposable
    {
        private readonly TcpClient socket = new TcpClient();
        internal readonly NetworkStream Stream;
        internal readonly StreamReader Reader;
        private int requestNumber;
        internal Peer(int port)
        {
            socket.Connect(IPAddress.Loopback, port);
            socket.ReceiveTimeout = 5000;
            socket.SendTimeout = 5000;
            Stream = socket.GetStream();
            Reader = new StreamReader(Stream, Utf8);
        }
        internal void Send(string method, Dictionary<string, object> args)
        {
            var text = GameJson.Serialize(Map("id", (++requestNumber).ToString(), "method", method, "params", args)) + "\n";
            var bytes = Utf8.GetBytes(text);
            Stream.Write(bytes, 0, bytes.Length);
        }
        internal Task<string> Read() { return Task.Run(() => Reader.ReadLine()); }
        internal Dictionary<string, object> Call(Fixture dispatcher, string method, Dictionary<string, object> args)
        {
            Send(method, args);
            return ReadResponse(Read(), dispatcher);
        }
        internal Dictionary<string, object> Raw(Fixture dispatcher, string frame)
        {
            var bytes = Utf8.GetBytes(frame);
            Stream.Write(bytes, 0, bytes.Length);
            return ReadResponse(Read(), dispatcher);
        }
        public void Dispose() { socket.Close(); Reader.Dispose(); }
    }

    private sealed class FakeGame : IGameApi, IGameObservationCatalog, IGameSensitiveLogin
    {
        private readonly int owner = Thread.CurrentThread.ManagedThreadId;
        private readonly Dictionary<string, GameActionReceipt> actions = new Dictionary<string, GameActionReceipt>();
        private readonly Dictionary<string, string> requests = new Dictionary<string, string>();
        private readonly Dictionary<string, string> fingerprints = new Dictionary<string, string>();
        private readonly Queue<GameEvent> journal = new Queue<GameEvent>();
        private long sequence;
        internal int Executions;
        internal long? LastRevision; internal long LastX;
        internal int LoginSubmissions;
        internal bool LoginThrows;
        public GameSensitiveLoginResult SubmitLogin(string account, string password, string expectedTitleInstanceId, string expectedTitleRevision)
        {
            OnThread();
            if (LoginThrows) throw new ArgumentException(password);
            Check(account == "dummyaccount" && password == "dummysecret", "credentials reach native only");
            Check(expectedTitleInstanceId == "title1" && expectedTitleRevision == "rev1", "title guards preserved");
            LoginSubmissions++;
            return new GameSensitiveLoginResult(true, "submitted");
        }
        internal bool WrongThread;
        private void OnThread() { WrongThread |= owner != Thread.CurrentThread.ManagedThreadId; if (WrongThread) throw new Exception("Not game thread"); }
        public GameSnapshot Observe(GameQuery query)
        {
            OnThread();
            return new GameSnapshot(query.Domain, 7, 11, query.Domain == "large"
                ? Map("large", new string('x', 2048)) : Map("name", "Fake Ranger", "hp", 84, "position", Map("x", 12, "y", 32)));
        }
        public IReadOnlyList<GameQueryDefinition> GetQueries()
        {
            OnThread();
            return new[] { new GameQueryDefinition("player", "Fake player", Map("type", "object", "properties", Map("limit", Map("type", "integer")))), new GameQueryDefinition("large", "Large fake snapshot") };
        }
        public IReadOnlyList<GameActionDefinition> GetActions()
        {
            OnThread();
            return new[] { new GameActionDefinition("fake.step", "Send a fake step", Map("type", "object", "properties", Map("x", Map("type", "integer")))),
                new GameActionDefinition("fake.wait", "Cancellable fake wait"),
                new GameActionDefinition("unsupported", "Not implemented", available: false, unavailableReason: "No native owner") };
        }
        public GameActionReceipt Execute(GameActionRequest request)
        {
            OnThread();
            LastRevision = request.ExpectedSceneRevision; object x; if(request.Arguments.TryGetValue("x",out x)) LastX=Convert.ToInt64(x);
            var fingerprint = request.Name + ":" + GameJson.Serialize(request.Arguments) + ":" + request.ExpectedSceneRevision;
            string id;
            if (requests.TryGetValue(request.RequestId, out id))
            {
                if (fingerprints[request.RequestId] != fingerprint)
                    return new GameActionReceipt(id, request.RequestId, request.Name, GameActionState.Rejected, "request_id_conflict", "Changed retry");
                return actions[id];
            }
            id = "fake-action-" + (++Executions);
            var receipt = new GameActionReceipt(id, request.RequestId, request.Name,
                request.Name == "fake.wait" ? GameActionState.Running : GameActionState.Sent, "accepted", "Fake owner accepted");
            requests.Add(request.RequestId, id);
            fingerprints.Add(request.RequestId, fingerprint);
            actions.Add(id, receipt);
            return receipt;
        }
        public GameActionReceipt GetAction(string actionId)
        {
            OnThread();
            GameActionReceipt receipt;
            return actions.TryGetValue(actionId, out receipt) ? receipt : new GameActionReceipt(actionId, null, null, GameActionState.Unknown, "unknown", "Not retained");
        }
        public bool CancelAction(string actionId)
        {
            OnThread();
            var receipt = GetAction(actionId);
            if (receipt.State != GameActionState.Running) return false;
            actions[actionId] = new GameActionReceipt(actionId, receipt.RequestId, receipt.Name, GameActionState.Cancelled, "cancelled", "Local wait cancelled");
            return true;
        }
        public GameEventBatch ReadEvents(long afterSequence, int maxCount)
        {
            OnThread();
            var gap = journal.Count > 0 && afterSequence < journal.Peek().Sequence - 1;
            var selected = journal.Where(item => item.Sequence > afterSequence).Take(maxCount).ToArray();
            return new GameEventBatch(selected, selected.Length == 0 ? sequence : selected[selected.Length - 1].Sequence, gap);
        }
        internal void Publish(string name, Dictionary<string, object> data)
        {
            journal.Enqueue(new GameEvent(++sequence, 7, name, data));
            while (journal.Count > 4) journal.Dequeue();
        }
    }
}
