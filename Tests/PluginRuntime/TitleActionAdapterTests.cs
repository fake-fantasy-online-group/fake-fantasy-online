// Actual title action/projection code with deterministic native owners; no login or server.
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static partial class GameActionAdapterTests
{
    private static Dictionary<string, object> TitleArgs(TestTitleBridge bridge, params object[] extra)
    {
        var args = Map("expectedTitleInstanceId", bridge.State.InstanceId, "expectedTitleRevision", bridge.State.Revision.ToString(System.Globalization.CultureInfo.InvariantCulture));
        for (int i = 0; i < extra.Length; i += 2) args.Add((string)extra[i], extra[i + 1]);
        return args;
    }
    private static PluginGameApi TitleSetup(out TestTitleBridge bridge, string page = "Login")
    {
        var api = Setup(); api.TestSession.State = ClientSessionState.Offline;
        bridge = new TestTitleBridge(); bridge.State.Page = page;
        TitleFlow.PluginBridgeForIndependentRuntime = bridge;
        return api;
    }
    private static void RunTitleTests()
    {
        int initial = checks;
        TestTitleBridge bridge;
        var api = TitleSetup(out bridge);
        Check(Definition(api, "title.submit_login").Available && !Definition(api, "title.select_server").Available, "title discovery follows native page");
        Check(Act(api, "title.select_server", TitleArgs(bridge, "serverId", "server:0")).Code == "action.unavailable" && bridge.Calls == 0, "wrong title stage never invokes native bridge");
        var stale = TitleArgs(bridge); stale["expectedTitleRevision"] = "0";
        Check(Act(api, "title.submit_login", stale).Code == "title.stale", "old title revision rejected");
        stale = TitleArgs(bridge); stale["expectedTitleInstanceId"] = "replaced";
        Check(Act(api, "title.submit_login", stale).Code == "title.stale", "old title instance rejected");
        foreach (string invalid in new[] { "01", "-1", "1.0", "9223372036854775808", " 1" })
        { stale = TitleArgs(bridge); stale["expectedTitleRevision"] = invalid; Check(Act(api, "title.submit_login", stale).Code == "title.invalid_guard", "title revision must be canonical nonnegative Int64"); }
        foreach (string key in new[] { "password", "account", "token", "secret-key-marker" })
        {
            var result = Act(api, "title.submit_login", TitleArgs(bridge, key, "SECRET-MARKER"), "secret-attempt");
            Check(result.Code == "arguments.invalid" && !result.Message.Contains("SECRET-MARKER") && !result.Message.Contains(key), "unexpected sensitive input rejected without reflection");
            Check(api.TestTitleRetainedCount == 8, "secret action is not retained");
        }
        int retainedBeforeGeneric = api.TestTitleRetainedCount;
        Check(Act(api, "unknown.action", Map("password", "SECRET-MARKER")).Code == "arguments.invalid" && api.TestTitleRetainedCount == retainedBeforeGeneric, "ordinary unknown actions cannot retain credential arguments");
        Check(bridge.Calls == 0, "validation failures never call native title");
        var args = TitleArgs(bridge);
        var login = Act(api, "title.submit_login", args, "login-once");
        Check(login.State == GameActionState.Running && bridge.LastAction == "login", "login submission does not fabricate success");
        Check(Act(api, "title.submit_login", args, "login-once").ActionId == login.ActionId && bridge.Calls == 1, "title replay returns same receipt without second submit");
        bridge.State.ResultSequence++; bridge.State.ResultCode = "login_succeeded"; bridge.State.Page = "ServerSelection";
        Check(api.GetAction(login.ActionId).Code == "title.login_succeeded", "new native callback establishes login success");
        bridge.State.Servers = new[] { new PluginTitleServer { DirectoryIndex = 4, Name = "Native" } };
        Check(Act(api, "title.select_server", TitleArgs(bridge, "serverId", "server:04")).Code == "title.invalid_server", "noncanonical server identity rejected");
        Check(Act(api, "title.select_server", TitleArgs(bridge, "serverId", "server:5")).Code == "title.invalid_server", "unobserved server identity rejected");
        Check(Act(api, "title.select_server", TitleArgs(bridge, "serverId", "server:4")).State == GameActionState.Completed && bridge.Server == 4, "observed server routes exact native index");
        bridge.NextPage = "Login";
        var confirm = Act(api, "title.confirm_server", TitleArgs(bridge));
        Check(confirm.State == GameActionState.Completed && (bool)confirm.Data["requiresLogin"], "endpoint switch reports local return to login");
        bridge.NextPage = null; bridge.State.Page = "CharacterSelection";
        bridge.State.Characters = new[] { new PluginTitleCharacter { CharacterId = "Hero", DisplayName = "Native" } };
        Check(Act(api, "title.select_character", TitleArgs(bridge, "characterId", "OtherHero")).Code == "title.invalid_character", "unobserved exact character identity rejected");
        Check(Act(api, "title.select_character", TitleArgs(bridge, "characterId", "Hero")).State == GameActionState.Completed && bridge.Character == "Hero", "character identity preserves exact native name");
        bridge.State.SelectedCharacterId = bridge.Character;
        var entering = Act(api, "title.enter_world", TitleArgs(bridge));
        Check(entering.State == GameActionState.Running && bridge.LastAction == "enter", "world entry waits for observed world");
        api.TestSession.State = ClientSessionState.InWorld; api.TestScene++;
        TitleFlow.PluginBridgeForIndependentRuntime = null; GameBootstrap.I.Window.Frame.Built = false;
        Check(api.GetAction(entering.ActionId).State == GameActionState.Running, "title teardown and scene transition await world construction");
        GameBootstrap.I.Window.Frame = new ReferenceFirstWorldInitialFrame { Built = true }; CharData.name = "Hero";
        api.TestSession.Connected = false;
        Check(api.GetAction(entering.ActionId).State != GameActionState.Completed, "disconnected built frame cannot prove world entry");
        api.TestSession.Connected = true;
        Check(api.GetAction(entering.ActionId).Code == "title.world_entered", "matching built world completes native entry");
        foreach (string code in new[] { "login_failed", "connection_failed", "network_error", "login_timeout" })
        {
            api = TitleSetup(out bridge); login = Act(api, "title.submit_login", TitleArgs(bridge));
            bridge.State.ResultSequence++; bridge.State.ResultCode = code;
            Check(api.GetAction(login.ActionId).Code == "title." + code && bridge.Calls == 1, "native failure/timeout reported without retry");
        }
        api = TitleSetup(out bridge); login = Act(api, "title.submit_login", TitleArgs(bridge)); api.TestExpireAction(login.ActionId);
        Check(api.GetAction(login.ActionId).State == GameActionState.TimedOut && bridge.Calls == 1, "deadline does not cancel or retry native login");
        api = TitleSetup(out bridge); bridge.Error = "SECRET-MARKER";
        var error = Act(api, "title.submit_login", TitleArgs(bridge));
        Check(error.Code == "title.native_result" && !error.Message.Contains("SECRET-MARKER"), "unexpected native errors are allowlist-redacted");
        api = TitleSetup(out bridge); login = Act(api, "title.submit_login", TitleArgs(bridge));
        TitleFlow.PluginBridgeForIndependentRuntime = new TestTitleBridge();
        Check(api.GetAction(login.ActionId).State == GameActionState.Unknown, "owner replacement does not imply native success");
        api = TitleSetup(out bridge); bridge.State.LoginLoading = true;
        Check(!Definition(api, "title.submit_login").Available && Act(api, "title.submit_login", TitleArgs(bridge)).Code == "action.unavailable" && bridge.Calls == 0, "loading blocks second login");
        bridge.State.LoginLoading = false; bridge.State.NeedsUpdate = true;
        Check(!Definition(api, "title.submit_login").Available, "mandatory client update blocks login");
        bridge.State.NeedsUpdate = false; bridge.State.ModalOpen = true;
        Check(!Definition(api, "title.submit_login").Available, "native modal blocks title operations");
        bridge.State.ModalOpen = false;
        int retained = api.TestTitleRetainedCount;
        Check(api.SubmitLogin("ephemeral-user", "SECRET-MARKER", bridge.State.InstanceId, "1").Submitted && bridge.LastAction == "credentials", "sensitive submission uses dedicated native route");
        Check(api.TestTitleRetainedCount == retained, "sensitive submission creates no action retention");
        Check(!api.SubmitLogin(new string('a', 21), "SECRET-MARKER", bridge.State.InstanceId, "1").Submitted && bridge.Calls == 1, "sensitive overlength input rejected before bridge");
        bridge.Error = "SECRET-MARKER";
        Check(api.SubmitLogin("user", "SECRET-MARKER", bridge.State.InstanceId, "1").Code == "not_ready", "sensitive native error remains redacted");
        api = TitleSetup(out bridge, "CharacterSelection"); bridge.State.SelectedCharacterId = "Hero";
        entering = Act(api, "title.enter_world", TitleArgs(bridge));
        api.TestSession.State = ClientSessionState.InWorld; CharData.name = "OtherHero";
        GameBootstrap.I.Window.Frame = new ReferenceFirstWorldInitialFrame { Built = true };
        Check(api.GetAction(entering.ActionId).Code == "title.character_changed", "different active character cannot complete requested entry");
        api = TitleSetup(out bridge, "CharacterSelection"); bridge.State.SelectedCharacterId = "Hero";
        entering = Act(api, "title.enter_world", TitleArgs(bridge));
        api.TestSession.State = ClientSessionState.InWorld; CharData.name = "Hero";
        GameBootstrap.I.Window.Frame = new ReferenceFirstWorldInitialFrame { Built = true };
        TitleFlow.PluginBridgeForIndependentRuntime = new TestTitleBridge();
        Check(api.GetAction(entering.ActionId).Code == "title.owner_changed", "replacement title owner cannot complete same-name world entry");
        api = TitleSetup(out bridge); bridge.State.Revision = 9007199254740993;
        bridge.State.SafeMessage = "SECRET-MARKER"; bridge.State.ResultCode = "SECRET-MARKER";
        bridge.State.HasNativeResultCode = true; bridge.State.NativeResultCode = 7;
        bridge.State.Servers = new[] { new PluginTitleServer { DirectoryIndex = 4, Name = new string('n', 400), WorldName = "World", BusyLevel = 4, Selected = true }, new PluginTitleServer { DirectoryIndex = 5, Name = "Second" } };
        bridge.State.ServerTotalCount = 2;
        bridge.State.Characters = new[] { new PluginTitleCharacter { CharacterId = "Hero", DisplayName = "Hero", Level = 20, Selected = true } };
        bridge.State.CharacterTotalCount = 1;
        var observed = api.TestObserveTitle(0, 1);
        Check((string)observed["expectedTitleRevision"] == "9007199254740993" && (string)observed["expectedTitleInstanceId"] == bridge.State.InstanceId, "title snapshot guard preserves exact revision");
        Check((bool)observed["credentialsReady"] && !(bool)observed["credentialsExposed"] && !observed.ContainsKey("account") && !observed.ContainsKey("password"), "title observation omits credentials");
        Check((bool)observed["hasNativeMessage"] && (string)observed["resultCode"] == "native_result" && (int)observed["nativeResultCode"] == 7 && !observed.ContainsKey("message"), "title observation exposes numeric result without arbitrary native text");
        Check(Rows(observed["servers"]).Count == 1 && (int)observed["serverNextOffset"] == 1 && (bool)observed["serversTruncated"], "title observation pages bounded native server rows");
        Check((string)Object(Rows(observed["servers"])[0])["serverId"] == "server:4" && ((string)Object(Rows(observed["servers"])[0])["name"]).Length == 256 && (bool)Object(Rows(observed["servers"])[0])["maintenance"], "server projection retains exact identity and bounded visible data");
        Check((string)Object(Rows(observed["characters"])[0])["characterId"] == "Hero" && (int)Object(Rows(observed["characters"])[0])["level"] == 20, "character projection uses native identity and level");
        bridge.State.Servers[0].Name = "Changed";
        Check(((string)Object(Rows(observed["servers"])[0])["name"]).Length == 256, "title snapshot detached from native mutation");
        bridge.State.Characters[0].CharacterId = new string('x', 65);
        bridge.State.SelectedCharacterId = bridge.State.Characters[0].CharacterId;
        var oversized = api.TestObserveTitle();
        Check(Object(Rows(oversized["characters"])[0])["characterId"] == null && !(bool)Object(Rows(oversized["characters"])[0])["selectable"] && oversized["selectedCharacterId"] == null, "oversized native identity never becomes actionable truncated ID");
        var beyond = api.TestObserveTitle(99, 1);
        Check(Rows(beyond["servers"]).Count == 0 && beyond["serverNextOffset"] == null && Rows(beyond["characters"]).Count == 0, "title page beyond end is empty terminal page");
        TitleFlow.PluginBridgeForIndependentRuntime = null;
        Check((string)api.TestObserveTitle()["page"] == "Unavailable", "absent title does not imply world entry");
        api.TestSession.State = ClientSessionState.InWorld;
        Check((string)api.TestObserveTitle()["page"] == "InWorld" && (bool)api.TestObserveTitle()["worldEntered"], "built native world remains observable after title destruction");
        TitleFlow.PluginBridgeForIndependentRuntime = null;
        CharData.charId = 0;
        Console.WriteLine("Title adapter passed: " + (checks - initial) + " assertions (production adapter; native substitutes).");
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class TitleFlow { internal static IPluginTitleBridge PluginBridgeForIndependentRuntime; }
    internal sealed class TestTitleBridge : IPluginTitleBridge
    {
        internal PluginTitleSnapshot State = new PluginTitleSnapshot { InstanceId = "test-title", Revision = 1, Page = "Login", CredentialsReady = true,
            Servers = new PluginTitleServer[0], Characters = new PluginTitleCharacter[0] };
        internal int Calls, Server; internal string LastAction, Character, Error, NextPage;
        public PluginTitleSnapshot ReadState() => State;
        private string Invoke(string action) { Calls++; LastAction = action; if (NextPage != null) State.Page = NextPage; return Error; }
        public string SubmitLogin() => Invoke("login");
        public string SubmitCredentials(string account, string password) => Invoke("credentials");
        public string SelectServer(int index) { Server = index; return Invoke("server"); }
        public string ConfirmSelectedServer() => Invoke("confirm");
        public string SelectCharacter(string id) { Character = id; return Invoke("character"); }
        public string EnterWorld() => Invoke("enter");
    }
    public sealed partial class PluginGameApi
    {
        internal int TestTitleRetainedCount => actions.Count;
        private void TrackObservation(string domain, object token) { }
        private static object Token(params object[] values) => values;
        private bool ObservationWorldReady(ClientSessionCoordinator session) => session != null && session.State == ClientSessionState.InWorld && session.Connected && ObservationFrame != null && ObservationFrame.Built;
        internal IReadOnlyDictionary<string, object> TestObserveTitle(int offset = 0, int limit = 128)
        { TrackTitleObservation(); return new GameSnapshot("title", TestScene, 1, ObserveTitle(new ObservationArguments { Offset = offset, Limit = limit })).Data; }
    }
}
