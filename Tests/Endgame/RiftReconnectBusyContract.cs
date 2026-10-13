using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

// Actual server-produced busy payload + actual state owner + actual native gate branch.
// Scene-test companion proves a fresh admitted private reconnect sends SelfEntered without
// negotiated context. This fixture does not manufacture a private-context boolean.
internal static class RiftReconnectBusyContract
{
    private sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld => true;
        public bool SendEndgame(EndgameRequest r) => Endgame.Begin(r);
        public bool RetryEndgame() => false;
        public bool EquipEndgameItem(EndgameItemView i) => false;
        public bool UnequipEndgameItem(EndgameItemView i) => false;
    }
    private static int checks;
    private static void Check(bool b, string text) { if (!b) throw new Exception(text); checks++; }
    private static void Apply(Client c, string json) => c.Endgame.Apply(EndgameResponse.Parse(json));
    private static EndgameResponse Packet(string directory, string name) =>
        PrivateServerEndgameCodec.Decode(File.ReadAllBytes(Path.Combine(directory, name + ".payload.bin")));
    private static void BeginFor(Client c, EndgameResponse reply) =>
        c.Endgame.Begin(new EndgameRequest("hello", requestID: reply.RequestID));
    private static void CheckIgnored(Client c, EndgameResponse reply, string message)
    {
        long version = c.Endgame.Version;
        string error = c.Endgame.Error, text = c.Endgame.Message;
        var entrances = c.Endgame.RiftEntrances;
        bool negotiated = c.Endgame.Negotiated, portals = c.Endgame.RiftPortalsV1,
            known = c.Endgame.RiftContextKnown, inside = c.Endgame.RiftInInstance;
        c.Endgame.Apply(reply);
        Check(c.Endgame.Version == version && c.Endgame.Error == error && c.Endgame.Message == text &&
            ReferenceEquals(entrances, c.Endgame.RiftEntrances) && c.Endgame.Negotiated == negotiated &&
            c.Endgame.RiftPortalsV1 == portals && c.Endgame.RiftContextKnown == known &&
            c.Endgame.RiftInInstance == inside, message);
    }
    private static bool Trigger(Client c)
    {
        new EndgamePanel(c); bool publicLoad = false;
        var p = new PlayerController { transferProcs = new List<MapLink.MapProc> { new MapLink.MapProc { poly = new Vector2[3], mapTo = 7 } }, onTransfer = (m,x,y) => publicLoad = true };
        p.StartForIndependentRuntime(); p.worldX = p.worldY = 15; p.AdvanceTransferForIndependentRuntime(); return publicLoad;
    }
    public static int Main(string[] args)
    {
        var busyReplies = new EndgameResponse[3];
        for (int i=0; i<3; i++)
        {
            string path = args[0].Replace("-1.payload.bin", "-" + (i+1) + ".payload.bin");
            busyReplies[i] = PrivateServerEndgameCodec.Decode(File.ReadAllBytes(path));
            Check(busyReplies[i].Kind == "hello" && !busyReplies[i].OK && busyReplies[i].Error == "busy", "fixture must be an actual server hello busy envelope");
        }
        Check(busyReplies[0].RequestID != busyReplies[1].RequestID && busyReplies[1].RequestID != busyReplies[2].RequestID, "three distinct real request receipts preserve captured FIFO order");
        EndgameResponse busy = busyReplies[0];
        bool vulnerable = args.Length > 1 && args[1] == "--expect-vulnerable";
        var c = new Client(); Check(!Trigger(c), "fresh reconnect awaiting hello cannot assume public scene");
        for (int attempt=0; attempt<3; attempt++)
        {
            Check(!c.Endgame.HelloSent, "bounded scheduler exposes exactly the next hello attempt");
            BeginFor(c, busyReplies[attempt]); c.Endgame.Apply(busyReplies[attempt]);
            if (attempt<2) { Check(!Trigger(c), "first two genuine busy responses remain unknown"); c.Endgame.Tick(DateTime.UtcNow.AddSeconds(3)); }
        }
        Check(Trigger(c) == vulnerable, vulnerable ? "known current-server busy path reproduces ordinary public callback" : "exhausted current-server busy path cannot call ordinary public callback");
        if (vulnerable) { Console.WriteLine("REPRODUCED actual-server three-busy reconnect public fallback ("+checks+" assertions)"); return 0; }
        Check(c.Endgame.HelloSent && !c.Endgame.Negotiated && !c.Endgame.RiftContextKnown, "failure remains unknown with no automatic fourth retry");
        Check(c.Endgame.Message.Contains("重新连接"), "exhausted private-unknown failure gives actionable reconnect guidance");
        c.Endgame.Tick(DateTime.UtcNow.AddHours(1)); Check(c.Endgame.HelloSent && !Trigger(c), "time passing cannot turn proven modern server into legacy fallback or infinite retry");

        foreach (string error in new[]{"busy","pending","storage","storage_unavailable"})
        {
            c=new Client(); c.Endgame.Begin(new EndgameRequest("hello")); Apply(c,"{\"kind\":\"hello\",\"ok\":false,\"error\":\""+error+"\"}");
            c.Endgame.Tick(DateTime.UtcNow.AddSeconds(3));
            for(int i=0;i<2;i++) {c.Endgame.Begin(new EndgameRequest("hello")); if(i==0)c.Endgame.NoteTransportFailure("lost hello write",new EndgameRequest("hello")); else c.Endgame.Tick(DateTime.UtcNow.AddSeconds(16)); c.Endgame.Tick(DateTime.UtcNow.AddSeconds(20));}
            Check(!Trigger(c) && !c.Endgame.RiftContextKnown, "explicit "+error+" followed by transport failure/timeout cannot fail open");
        }
        // A successful legacy handshake is positive compatibility evidence, not a timeout guess.
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Check(Trigger(c), "explicit successful old-server hello without portal capability keeps normal gates");
        c.Endgame.Reset();
        for(int i=0;i<3;i++) {c.Endgame.Begin(new EndgameRequest("hello"));c.Endgame.Tick(DateTime.UtcNow.AddSeconds(16));c.Endgame.Tick(DateTime.UtcNow.AddSeconds(20));}
        Check(Trigger(c), "reset clears prior connection evidence and retains silent legacy-server compatibility");
        c.Endgame.Apply(busy);
        Check(!Trigger(c) && !c.Endgame.RiftContextKnown, "late genuine busy retracts a silence-only legacy guess");

        // Real actor/backend test: first hello starts a held Load without replying;
        // retries two/three receive existing pending responses while the same Load waits.
        string directory = Path.GetDirectoryName(args[0]);
        var delayedSuccess = Packet(directory, "delayed-hello-success");
        c=new Client(); BeginFor(c, delayedSuccess);
        c.Endgame.Tick(DateTime.UtcNow.AddSeconds(16)); c.Endgame.Tick(DateTime.UtcNow.AddSeconds(20));
        for(int attempt=2;attempt<=3;attempt++)
        {
            var pending=Packet(directory, "hello-pending-"+attempt);
            BeginFor(c, pending);
            Check(pending.Kind=="hello" && !pending.OK && pending.Error=="pending", "actual held-Load fixture retains original pending response type");
            c.Endgame.Apply(pending); Check(!Trigger(c), "held backend Load cannot exhaust into a public map callback");
            c.Endgame.Tick(DateTime.UtcNow.AddSeconds(3));
        }
        Check(c.Endgame.HelloSent && c.Endgame.Message.Contains("重新连接"), "held Load still has a bounded retry budget and clear failure guidance");
        c.Endgame.Apply(delayedSuccess);
        Check(c.Endgame.Negotiated && c.Endgame.RiftPortalsV1 && !c.Endgame.RiftContextKnown && !Trigger(c),
            "actual held-Load success for the first request recovers exhausted retries but waits for context");
        c.Endgame.Apply(Packet(directory, "delayed-rift.context-success"));
        Check(c.Endgame.RiftContextKnown && c.Endgame.RiftInInstance && !Trigger(c),
            "actual delayed private context never opens the ordinary map callback");
        CheckIgnored(c, Packet(directory, "hello-pending-2"), "old pending cannot alter confirmed private negotiation");
        CheckIgnored(c, busyReplies[0], "old busy cannot alter confirmed private negotiation");
        c.Endgame.Begin(new EndgameRequest("hello"));
        CheckIgnored(c, Packet(directory, "hello-pending-3"), "different old request cannot pollute explicit newer refresh");

        // A silent legacy guess must also be withdrawn when actual current-server
        // success finally arrives. Request IDs are retained from the original fixture.
        c = new Client();
        for (int i=0; i<3; i++)
        {
            if (i==0) BeginFor(c, delayedSuccess); else c.Endgame.Begin(new EndgameRequest("hello"));
            c.Endgame.Tick(DateTime.UtcNow.AddSeconds(16)); c.Endgame.Tick(DateTime.UtcNow.AddSeconds(20));
        }
        Check(Trigger(c), "pure silence retains the existing legacy compatibility branch");
        c.Endgame.Apply(delayedSuccess);
        Check(c.Endgame.Negotiated && !c.Endgame.RiftContextKnown && !Trigger(c), "late actual capability success withdraws silence-only public guess");
        c.Endgame.Apply(Packet(directory, "delayed-rift.context-success"));
        Check(c.Endgame.RiftInInstance && !Trigger(c), "late actual private context keeps ordinary routing closed");
        c.Endgame.Reset();
        CheckIgnored(c, delayedSuccess, "reset rejects previous-connection success before any new hello");

        c=new Client(); c.Endgame.Begin(new EndgameRequest("hello"));c.Endgame.Apply(busy);c.Endgame.Tick(DateTime.UtcNow.AddSeconds(3));
        c.Endgame.Begin(new EndgameRequest("hello"));Apply(c,"{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-portals.v1\"]}");
        Check(!Trigger(c), "late capability success still waits for authoritative context");
        Apply(c,"{\"kind\":\"rift.context\",\"ok\":true,\"result\":{\"inInstance\":\"false\"}}");
        Check(Trigger(c), "real public context legitimately releases normal routing after recovery");
        if (args.Length > 1) AdmissionFixtures(args[1]);
        Console.WriteLine("PASS "+checks+" actual-server busy/timeout/reconnect native-route contracts"); return 0;
    }

    private static void AdmissionFixtures(string directory)
    {
        // After the server fix, one admitted request can produce pending then busy.
        // Replay each actual FIFO pair with its captured request ID, not fabricated tags.
        var c = new Client();
        for (int attempt=1; attempt<=3; attempt++)
        {
            var pending = Packet(directory, "hello-admission-pending-"+attempt);
            var busy = Packet(directory, "hello-busy-"+attempt);
            Check(pending.Kind == "hello" && !pending.OK && pending.Error == "pending" &&
                busy.Error == "busy" && pending.RequestID == busy.RequestID,
                "actual full mailbox sends admission pending before busy for the same request");
            BeginFor(c, pending); c.Endgame.Apply(pending);
            Check(!Trigger(c), "admission pending never authorizes public routing");
            c.Endgame.Apply(busy);
            Check(!Trigger(c), "following busy never authorizes public routing");
            c.Endgame.Tick(DateTime.UtcNow.AddSeconds(3));
        }
        Check(c.Endgame.HelloSent && !c.Endgame.RiftContextKnown && c.Endgame.Message.Contains("重新连接"),
            "actual pending-busy pairs exhaust safely without a fourth automatic request");

        // Here the real actor was stopped with queue space remaining. Its fixture
        // is a public scene; only its eventual context may establish that fact.
        c = new Client();
        var first = Packet(directory, "queued-hello-pending-1");
        for (int attempt=1; attempt<=3; attempt++)
        {
            var pending = Packet(directory, "queued-hello-pending-"+attempt);
            Check(pending.Kind == "hello" && pending.Error == "pending" && !pending.OK,
                "stalled actor receives immediate actual Session admission response");
            BeginFor(c, pending); c.Endgame.Apply(pending);
            c.Endgame.Tick(DateTime.UtcNow.AddSeconds(20));
            Check(!Trigger(c) && !c.Endgame.RiftContextKnown, "actor queue delay cannot masquerade as a silent old server");
        }
        var success = Packet(directory, "queued-hello-success");
        Check(success.OK && success.RequestID == first.RequestID, "actual queued completion belongs to first admitted request");
        c.Endgame.Apply(success);
        Check(c.Endgame.Negotiated && c.Endgame.RiftPortalsV1 && !c.Endgame.RiftContextKnown && !Trigger(c),
            "first queued success after later retries still waits for authoritative context");
        c.Endgame.Apply(Packet(directory, "queued-rift.context-success"));
        Check(c.Endgame.RiftContextKnown && !c.Endgame.RiftInInstance && Trigger(c),
            "actual queued public context legitimately opens its ordinary gates");
        CheckIgnored(c, first, "late same-request admission pending cannot undo success or public context");
        CheckIgnored(c, Packet(directory, "queued-hello-pending-3"), "late later-attempt pending cannot undo success");
        c.Endgame.Begin(new EndgameRequest("hello"));
        CheckIgnored(c, first, "old admission pending cannot pollute newer refresh state");
    }
}
