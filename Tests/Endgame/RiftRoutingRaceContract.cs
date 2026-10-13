using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

// Reuses only deterministic native substitutes from EndgameRiftPortalContract.
// Ownership is established by real hello/context DTO application, never a stub InRift flag.
internal static class RiftRoutingRaceContract
{
    private sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public readonly List<EndgameRequest> Sent = new List<EndgameRequest>();
        public bool SendEndgame(EndgameRequest r) { if (!Endgame.Begin(r)) return false; Sent.Add(r); return true; }
        public bool RetryEndgame() => Endgame.Pending;
        public bool EquipEndgameItem(EndgameItemView i) => false;
        public bool UnequipEndgameItem(EndgameItemView i) => false;
    }
    private static int checks, publicLoads;
    private static void Check(bool v, string label) { if (!v) throw new Exception(label); checks++; }
    private static void Apply(Client c, string json) => c.Endgame.Apply(EndgameResponse.Parse(json));
    private static Client New()
    { var c = new Client(); new EndgamePanel(c); publicLoads = 0; NetClient.I.Commands = 0; DifficultyBox.Shown = 0; return c; }
    private static void Hello(Client c)
    {
        c.Endgame.Begin(new EndgameRequest("hello"));
        Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-portals.v1\"]}");
    }
    private static void Context(Client c, bool inside) => Apply(c, "{\"kind\":\"rift.context\",\"ok\":true,\"result\":{\"inInstance\":\"" + (inside ? "true" : "false") + "\"}}");
    private static void Snapshot(Client c) => Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"revision\":\"2\",\"snapshotId\":\"race\",\"chunk\":0,\"chunks\":1,\"items\":[]}");
    private static void Info(Client c, int floor, string revision = "4", bool transition = false, string phase = "active") => Apply(c,
        "{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"runId\":\"run-race\",\"revision\":\"" + revision + "\",\"phase\":\"" + phase + "\",\"tier\":7,\"floorIndex\":" + floor + ",\"transitioning\":" + (transition ? "true" : "false") + "}}");
    private static void Receipt(Client c, EndgameRequest r, bool ok, string error = "") => Apply(c,
        "{\"kind\":\"rift.advance\",\"ok\":" + (ok ? "true" : "false") + ",\"requestId\":\"" + r.RequestID + "\",\"error\":\"" + error + "\"}");
    private static PlayerController Player(string cmd = null, bool spawnInside = false)
    {
        var p = new PlayerController { worldX = spawnInside ? 15 : 0, worldY = spawnInside ? 15 : 0,
            transferProcs = new List<MapLink.MapProc> { new MapLink.MapProc { poly = new Vector2[3], mapTo = 999, cmd = cmd } },
            onTransfer = (map,x,y) => publicLoads++ };
        p.StartForIndependentRuntime(); return p;
    }
    private static void Enter(PlayerController p) { p.worldX = p.worldY = 15; p.AdvanceTransferForIndependentRuntime(); }
    private static void Reenter(PlayerController p) { p.worldX = p.worldY = 0; p.AdvanceTransferForIndependentRuntime(); Enter(p); }
    private static IReadOnlyDictionary<string, object> Args(EndgameRequest r) => (IReadOnlyDictionary<string, object>)BoundedJson.Parse(r.Json);
    private static int Floor(EndgameRequest r) => Convert.ToInt32(Args(r)["floorIndex"]);
    private static void NoPublic(string reason) => Check(publicLoads == 0 && NetClient.I.Commands == 0 && DifficultyBox.Shown == 0, reason);
    public static int Main()
    {
        // Reconnect: initial context is filtered before capability negotiation; hello repeats it.
        var c = New(); var p = Player(); Context(c, true); Enter(p);
        Check(!c.Endgame.RiftContextKnown && c.Sent.Count == 0, "pre-hello context cannot grant capability or create a request"); NoPublic("unknown reconnect context blocks normal map callback");
        Hello(c); Snapshot(c); Info(c, 1); Reenter(p);
        Check(!c.Endgame.RiftContextKnown && c.Sent.Count == 0, "info arriving before context is insufficient for native portal routing"); NoPublic("late context never causes an optimistic public map load");
        Context(c, true); Reenter(p);
        Check(c.Sent.Count == 1 && Floor(c.Sent[0]) == 1 && c.Sent[0].Op == "rift.advance", "context plus info invokes actual guarded advance sender");

        // Floors 1/2/3 share scenery; native mapTo and special cmd must never decide private routing.
        for (int floor = 1; floor <= 3; floor++) foreach (string cmd in new string[] { null, "taoyu", "taoyu hard", "legacy-script" })
        {
            c = New(); Hello(c); Context(c, true); Snapshot(c); p = Player(cmd); Enter(p);
            Check(c.Sent.Count == 0, "private context without late info waits at floor " + floor); NoPublic("missing info does not execute map/script/difficulty fallback");
            Info(c, floor); Reenter(p); var req = c.Sent[0];
            Check(Floor(req) == floor && (string)Args(req)["runId"] == "run-race" && !req.Json.Contains("\"tier\""), "private floor identity is frozen, unrelated public mapTo ignored");
            for (int i = 0; i < 100; i++) Enter(p); Reenter(p);
            Check(c.Sent.Count == 1 && !p.TransferredForIndependentRuntime, "repeated and pending edges do not duplicate or latch a public transfer");
            foreach (string error in new[] { "pending", "busy", "storage", "storage_unavailable" })
            { Receipt(c, req, false, error); Reenter(p); Check(c.Endgame.Pending && c.Sent.Count == 1, "uncertain " + error + " retains original request"); }
            Receipt(c, req, false, "exit"); Enter(p); Check(c.Sent.Count == 1, "permanent failure cannot auto-retry while remaining inside gate");
            Reenter(p); Check(c.Sent.Count == 2, "leaving and entering after permanent failure makes a new guarded attempt");
            req = c.Sent[1]; Receipt(c, req, true); Info(c, floor + 1, "5", true);
            for (int i = 0; i < 20; i++) Reenter(p);
            Check(c.Sent.Count == 2 && !c.Endgame.Pending, "receipt before late Teleport cannot start another transition from transitioning info"); NoPublic("pending/failure/late-Teleport matrix never reaches public callback");
            Info(c, floor, "4"); Check(c.Endgame.Rift.FloorIndex == floor + 1 && c.Endgame.Rift.Transitioning, "late older floor view cannot replace newer revision");
            Info(c, floor + 1, "6"); var next = Player(cmd, true); Enter(next);
            Check(c.Sent.Count == 2, "same-map private replacement spawn inside gate is suppressed");
            Reenter(next); Check(c.Sent.Count == 3 && Floor(c.Sent[2]) == floor + 1, "same-map replacement re-arms only on a fresh polygon edge");
        }
        c = New(); Hello(c); Context(c, true); Snapshot(c); Info(c, 3, "9", false, "completed"); p = Player(); Enter(p);
        Check(c.Sent.Count == 0, "completed admitted-run recovery cannot advance through scenery"); NoPublic("completed private context remains owned until server leave edge");
        Context(c, false); Info(c, 1, "3"); p = Player(); Enter(p);
        Check(publicLoads == 1 && !c.Endgame.RiftInInstance, "public context, not stale run info, releases ordinary routing");
        c = New(); Hello(c); Context(c, true); Snapshot(c); Info(c, 2); c.Endgame.Reset(); p = Player(); Enter(p);
        NoPublic("disconnect reset and reconnect awaiting hello do not reuse old private info as public authority");
        Console.WriteLine("PASS " + checks + " real state/PlayerController Rift race assertions; no Unity or live transport.");
        return 0;
    }
}
