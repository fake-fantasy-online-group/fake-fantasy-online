using System;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static partial class GameActionAdapterTests
{
    private static void PortalHello(EndgameNative n)
    { n.Endgame.Begin(new EndgameRequest("hello")); Apply(n, Map("kind", "hello", "ok", true, "capabilities", new object[] { "endgame.v1", "endgame.rift-portals.v1" })); }
    private static void PortalContext(EndgameNative n, bool inside) => Apply(n, Map("kind", "rift.context", "ok", true, "result", Map("inInstance", inside ? "true" : "false")));
    public static int RunRiftRoutingRaces()
    {
        int initial = checks; EndgameNative n; var api = SetupEndgame(out n, false);
        Check(!Definition(api, "endgame.rift.advance").Available, "plugin cannot infer floor before hello");
        PortalHello(n); PublishItems(n); PortalContext(n, true);
        Check(!Definition(api, "endgame.rift.advance").Available && n.Sent.Count == 0, "private context without delayed info does not invent floor navigation");
        PublishRift(n); var args = Map("expectedRevision", TestRevision, "runId", "run-current", "floorIndex", 3);
        var receipt = EndAct(api, "rift.advance", args, "race-advance"); var request = n.Last;
        Check(receipt.State == GameActionState.Sent && request.Op == "rift.advance" && api.TestSession.Sends == 0, "plugin invokes only the sidecar sender, never ordinary movement/teleport");
        for (int i = 0; i < 100; i++) Check(EndAct(api, "rift.advance", args, "race-advance").ActionId == receipt.ActionId, "repeated caller identity reuses immutable advance");
        Check(n.Sent.Count == 1 && EndAct(api, "rift.advance", args).Code == "action.unavailable", "pending distinct caller cannot advance twice");
        foreach (string error in new[] { "pending", "busy", "storage", "storage_unavailable" })
        { ApplyReceipt(n, request, false, error); Check(api.GetAction(receipt.ActionId).State == GameActionState.Unknown && n.Endgame.Pending && !Definition(api, "endgame.rift.advance").Available, "plugin keeps uncertain " + error + " without public fallback"); }
        EndAct(api, "retry", Map("expectedRequestId", request.RequestID));
        Check(ReferenceEquals(n.Last, request) && n.Last.Json == request.Json && api.TestSession.Sends == 0, "retry uses exact original floor and request, never native teleport");
        ApplyReceipt(n, request, false, "exit");
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Rejected && !n.Endgame.Pending, "server permanent rejection settles navigation attempt");
        receipt = EndAct(api, "rift.advance", args); request = n.Last;
        ApplyReceipt(n, request); PublishRift(n, transitioning: true);
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Completed && (string)api.GetAction(receipt.ActionId).Data["completionScope"] == "server", "advance receipt establishes server operation, not local map arrival");
        Check(!Definition(api, "endgame.rift.advance").Available && api.TestSession.Sends == 0, "late native Teleport leaves transition blocked without local fallback");
        n.Endgame.Reset(); Check(api.GetAction(receipt.ActionId).State == GameActionState.Completed, "settled receipt stays historical after reset");
        PortalHello(n); PublishItems(n); PortalContext(n, true);
        Check(!Definition(api, "endgame.rift.advance").Available, "reconnect does not revive old floor projection");
        PublishRift(n, phase: "completed");
        Check(!Definition(api, "endgame.rift.advance").Available, "completed recovery cannot advance");
        var reentry = EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "runId", "run-current", "tier", 2));
        Check(reentry.State == GameActionState.Sent && n.Last.Op == "rift.start" && !n.Last.Json.Contains("npcEntityId") && api.TestSession.Sends == 0, "same-map admitted recovery remains authoritative and NPC-independent");
        ApplyReceipt(n, n.Last); PortalContext(n, false); PublishRift(n);
        // A stale active run may still offer the sidecar action in public context. It cannot
        // load a map locally; the server owns membership/scene validation and rejects it.
        var stale = EndAct(api, "rift.advance", args);
        Check(stale.State == GameActionState.Sent && api.TestSession.Sends == 0, "stale public run projection can only request server validation, not SwitchMap");
        ApplyReceipt(n, n.Last, false, "instance");
        Check(api.GetAction(stale.ActionId).State == GameActionState.Rejected && !n.Endgame.RiftInInstance, "server rejection cannot restore private scene context");
        api = SetupEndgame(out n); PortalHello(n); PortalContext(n, true); PublishRift(n); n.FailAfterBegin = true;
        var uncertain = EndAct(api, "rift.advance", args);
        Check(uncertain.State == GameActionState.Unknown && n.Endgame.Pending && api.TestSession.Sends == 0, "write failure after Begin retains uncertainty without public map fallback");
        Console.WriteLine("PASS " + (checks - initial) + " production plugin Rift race assertions; no Unity or live transport.");
        return 0;
    }
}
internal static class RiftPluginRoutingRaceMain { public static int Main() => GameActionAdapterTests.RunRiftRoutingRaces(); }
