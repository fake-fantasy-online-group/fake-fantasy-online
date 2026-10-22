using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class RiftHazardContract
{
    private static int checks;
    private static string fixture;
    private static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    private static void Reject(Action action, string name)
    {
        try { action(); } catch (FormatException) { checks++; return; }
        throw new Exception("Accepted " + name);
    }
    private static void Isolate(Func<EndgameResponse> action, string name)
    {
        var response = action();
        Check(response.Rift != null && response.Rift.SkippedVisualEntries > 0, "isolated optional row: " + name);
    }
    private static Dictionary<string, object> Object(object value) => (Dictionary<string, object>)value;
    private static List<object> List(object value) => (List<object>)value;
    private static EndgameResponse Edit(Action<Dictionary<string, object>> edit)
    {
        var envelope = Object(BoundedJson.Parse(fixture)); var rift = Object(envelope["rift"]);
        edit(rift); return EndgameResponse.Parse(BoundedJson.Serialize(envelope));
    }
    private static Dictionary<string, object> Hazard(Dictionary<string, object> rift, int index = 4) => Object(List(rift["hazards"])[index]);
    private static Dictionary<string, object> Wall(Dictionary<string, object> rift) => Object(List(Hazard(rift)["walls"])[0]);
    private static EndgameRiftView View(long now = 1000000, bool empty = false, int floor = 1, int map = 7, string revision = "7") => Edit(r =>
    {
        r["serverNowMS"] = now; r["floorIndex"] = (long)floor; r["mapId"] = (long)map; r["revision"] = revision;
        if (empty) r["hazards"] = new List<object>();
        else foreach (var item in List(r["hazards"])) { var h = Object(item); h["remainingMS"] = (long)h["expiresMS"] - now; }
    }).Rift;
    private static EndgameClientState State()
    {
        var state = new EndgameClientState(); state.Begin(new EndgameRequest("hello"));
        state.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-portals.v1\"]}"));
        return state;
    }
    private static void Context(EndgameClientState state, int floor, long now = 1000000, int map = 7)
    {
        state.Apply(EndgameResponse.Parse("{\"kind\":\"rift.context\",\"ok\":true,\"result\":{\"inInstance\":\"true\",\"runId\":\"test-rift\",\"mapId\":\"" + map + "\",\"floorIndex\":\"" + floor + "\",\"serverNowMS\":\"" + now + "\"}}"));
    }
    private static EndgameResponse Snapshot(long now = 1000000, bool empty = false, int floor = 1, int map = 7) => Edit(r =>
    {
        r["serverNowMS"] = now; r["floorIndex"] = (long)floor; r["mapId"] = (long)map;
        if (empty) r["hazards"] = new List<object>();
        else foreach (var item in List(r["hazards"])) { var h = Object(item); h["remainingMS"] = (long)h["expiresMS"] - now; }
    });
    private static EndgameRiftWallView Rectangle(double x, double y, double x2, double y2, double width) => new EndgameRiftWallView(
        new Dictionary<string, object> { {"id","geometry"},{"x",x},{"y",y},{"x2",x2},{"y2",y2},{"width",width} });
    public static int Main(string[] args)
    {
        fixture = File.ReadAllText(Path.Combine(args[0], "elite-wire-v6.json"));
        var original = EndgameResponse.Parse(fixture);
        Check(original.Rift.Hazards.Count == 5 && original.Rift.MapID == 7, "real Go production riftView fixture parses all five mechanisms");
        Check(original.Rift.Auras[0].AbilityIDs[0] == 3 && original.Rift.Hazards[4].Walls.Count == 3, "stable aura IDs and all three exact wall segments");
        string hex = File.ReadAllText(Path.Combine(args[0], "elite-wire-v6.wire.hex")).Trim();
        byte[] wire = new byte[hex.Length / 2]; for (int i=0;i<wire.Length;i++) wire[i]=Convert.ToByte(hex.Substring(i*2,2),16);
        Check(wire[0] == 0xd0 && wire[1] == 0x80, "Go production opcode unchanged");
        byte[] payload = new byte[wire.Length-2]; Buffer.BlockCopy(wire,2,payload,0,payload.Length);
        Check(PrivateServerEndgameCodec.Decode(payload).Rift.Hazards[4].Walls[0].X == original.Rift.Hazards[4].Walls[0].X, "actual Go binary envelope decodes additive JSON");
        var metadata=EndgameResponse.Parse(File.ReadAllText(Path.Combine(args[0], "elite-wire-v6-metadata.json")));
        int liveGold=0,liveClones=0;
        foreach(var aura in metadata.Rift.Auras) {if(aura.GoldenUntilMS>metadata.Rift.ServerNowMS) liveGold++;if(aura.CloneOf.Length>0) liveClones++;}
        Check(liveGold==1 && liveClones==3 && metadata.Rift.Impacts.Count==2, "actual Go gold/clone/confirmed-impact metadata fixture");
        string metadataHex=File.ReadAllText(Path.Combine(args[0],"elite-wire-v6-metadata.wire.hex")).Trim();
        byte[] metadataPayload=new byte[metadataHex.Length/2-2];
        for(int i=0;i<metadataPayload.Length;i++) metadataPayload[i]=Convert.ToByte(metadataHex.Substring((i+2)*2,2),16);
        Check(PrivateServerEndgameCodec.Decode(metadataPayload).Rift.Impacts.Count==2, "actual production metadata wire decodes without binary extension");
        var legacy = EndgameResponse.Parse(File.ReadAllText(Path.Combine(args[0], "full-fields.json")));
        Check(legacy.Rift.Hazards.Count == 0 && legacy.Rift.ServerNowMS == 0 && legacy.Rift.Auras[0].AbilityIDs.Count == 0, "old v5 and floorless payloads still parse");
        Check(Edit(r => Hazard(r)["unknown"] = true).Rift.Hazards.Count == 5, "unknown optional hazard field preserves known effect");
        foreach (string key in new[] {"id","sourceId","abilityId","mechanism","x","y","radius","bornMS","expiresMS","remainingMS"})
            Isolate(() => Edit(r => Hazard(r).Remove(key)), "missing required hazard " + key);
        Isolate(() => Edit(r => Hazard(r)["mechanism"] = "ice_orb"), "mechanism does not match ability ID");
        Isolate(() => Edit(r => Hazard(r)["abilityId"] = 0L), "buff cannot masquerade as hazard");
        foreach (string id in new[] {"0","01","-1","4294967296","1e3"}) Isolate(() => Edit(r => Hazard(r)["sourceId"] = id), "noncanonical native source " + id);
        Isolate(() => Edit(r => Hazard(r)["remainingMS"] = 15000.5), "fractional duration");
        Isolate(() => Edit(r => Hazard(r)["remainingMS"] = 14999L), "deadline/sample discrepancy");
        Isolate(() => Edit(r => Hazard(r)["bornMS"] = 1000001L), "future birth");
        Isolate(() => Edit(r => Hazard(r)["expiresMS"] = 1060001L), "duration over hard cap");
        Isolate(() => Edit(r => Wall(r)["x"] = "1280"), "numeric geometry string");
        Isolate(() => Edit(r => Wall(r)["x"] = 1e8), "coordinate limit");
        Isolate(() => Edit(r => Wall(r)["width"] = 0L), "zero wall width");
        Isolate(() => Edit(r => Wall(r)["width"] = 1025L), "wall width cap");
        Isolate(() => Edit(r => { var w=Wall(r); w["x2"]=w["x"]; w["y2"]=w["y"]; }), "zero centerline");
        Isolate(() => Edit(r => Object(List(Hazard(r)["walls"])[1])["id"] = Wall(r)["id"]), "duplicate wall ID");
        Isolate(() => Edit(r => List(Hazard(r)["walls"]).RemoveAt(0)), "partial wall group");
        Isolate(() => Edit(r => List(r["hazards"]).Add(Hazard(r))), "duplicate owner");
        Reject(() => Edit(r => r.Remove("mapId")), "hazard snapshot without native map identity");
        Reject(() => Edit(r => r.Remove("serverNowMS")), "hazard snapshot without authoritative time");
        Isolate(() => Edit(r => Object(List(r["auras"])[0])["abilityIds"] = new List<object> {3L,3L}), "duplicate aura abilities");
        Check(Edit(r => Object(List(r["auras"])[0])["abilityIds"] = new List<object> {8L}).Rift.Auras[0].AbilityIDs[0] == 8, "unknown aura ability preserves identity without enabling unknown renderer");
        Check(Edit(r => Wall(r)["x"] = 1280.25).Rift.Hazards[4].Walls[0].X == 1280.25, "fractional Go wall coordinates retained");
        var golden=Edit(r => {var aura=Object(List(r["auras"])[0]);aura["abilityIds"]=new List<object>{0L};aura["goldenUntilMS"]=1005000L;}).Rift;
        Check(golden.Auras[0].GoldenUntilMS==1005000, "golden body carries server deadline rather than local timer");
        Isolate(() => Edit(r => Object(List(r["auras"])[0])["goldenUntilMS"]=1005000L), "golden deadline without selected golden ability");
        Isolate(() => Edit(r => {var aura=Object(List(r["auras"])[0]);aura["abilityIds"]=new List<object>{0L};aura["goldenUntilMS"]=1005001L;}), "golden deadline exceeds five seconds");
        Check(Edit(r => List(r["auras"]).Add(new Dictionary<string,object>{{"entityId","134217834"},{"class",0L},{"cloneOf","134217833"}})).Rift.Auras[1].CloneOf=="134217833", "native clone owner is explicit class-zero metadata");
        Isolate(() => Edit(r => Object(List(r["auras"])[0])["cloneOf"]="134217834"), "clone cannot inherit elite class or abilities");
        var impact=new Dictionary<string,object>{{"id","impact-1"},{"mechanism","ice_orb"},{"x",1100L},{"y",1000L},{"radius",120L},{"atMS",1000000L},{"expiresMS",1000700L}};
        Check(Edit(r => r["impacts"]=new List<object>{impact}).Rift.Impacts[0].ExpiresMS==1000700, "server detonation receipt has short fixed deadline");
        Isolate(() => Edit(r => {var wrong=new Dictionary<string,object>(impact);wrong["expiresMS"]=1000701L;r["impacts"]=new List<object>{wrong};}), "impact TTL mismatch");
        Isolate(() => Edit(r => {var wrong=new Dictionary<string,object>(impact);wrong["mechanism"]="walls";r["impacts"]=new List<object>{wrong};}), "wall removal cannot masquerade as detonation");
        Isolate(() => Edit(r => r["impacts"]=new List<object>{impact,impact}), "duplicate impact receipt");
        var oracle=Object(BoundedJson.Parse(File.ReadAllText(Path.Combine(args[0],"elite-wall-geometry-v6.json"))));
        Check(EndgameData.Number(oracle,"clearance")==EndgameRiftWallCollision.ActorClearance, "actual Go actor clearance equals client");
        foreach(var row in List(oracle["cases"]))
        {
            var item=Object(row);var from=Object(item["from"]);var to=Object(item["to"]);
            var wall=new EndgameRiftWallView(Object(item["wall"]));
            bool result=EndgameRiftWallCollision.Blocks(wall,EndgameData.Number(from,"x"),EndgameData.Number(from,"y"),EndgameData.Number(to,"x"),EndgameData.Number(to,"y"));
            Check(result==(bool)item["blocked"], "actual Go geometry oracle: "+item["name"]);
        }
        var rectangle = Rectangle(100,60,100,140,20);
        Check(!EndgameRiftWallCollision.Blocks(rectangle,77,0,77,200), "parallel just outside clearance passes");
        Check(EndgameRiftWallCollision.Blocks(rectangle,78,0,78,200), "parallel exact actor-clearance boundary blocks");
        Check(EndgameRiftWallCollision.Blocks(rectangle,-10000,100,10000,100), "continuous sweep blocks high speed tunneling");
        Check(!EndgameRiftWallCollision.Blocks(rectangle,50,47,150,47), "flat end plus clearance outside passes");
        Check(EndgameRiftWallCollision.Blocks(rectangle,50,48,150,48), "flat-end exact boundary blocks");
        Check(EndgameRiftWallCollision.Blocks(rectangle,100,100,100,100), "stationary overlap blocks");
        Check(EndgameRiftWallCollision.Blocks(rectangle,double.NaN,100,150,100), "nonfinite movement rejected");
        var diagonal=Rectangle(0,0,80,80,20);
        Check(EndgameRiftWallCollision.Blocks(diagonal,0,80,80,0) && !EndgameRiftWallCollision.Blocks(diagonal,0,120,40,120), "rotated geometry uses server axes");
        var clock=new EndgameRiftHazardState(); var initial=View();
        Check(clock.Apply(initial,0) && clock.Blocks(7,1200,1000,1350,1000,0), "snapshot immediately supplies matching map collision");
        Check(!clock.Blocks(8,1200,1000,1350,1000,0), "wrong native map never blocks");
        Check(clock.RemainingMS(initial.Hazards[4],1000)==14000, "remaining lifetime advances monotonically");
        clock.Apply(initial,10000);
        Check(clock.RemainingMS(initial.Hazards[4],10000)==5000, "duplicate snapshot cannot restart duration");
        Check(clock.RemainingMS(initial.Hazards[4],100)==5000, "backward local sample cannot add time");
        Check(clock.Blocks(7,1200,1000,1350,1000,14999) && !clock.Blocks(7,1200,1000,1350,1000,15000), "expiry is exclusive at authoritative deadline");
        clock.Apply(initial,16000); Check(!clock.Blocks(7,1200,1000,1350,1000,16000), "expired duplicate never resurrects");
        clock.Reset(); clock.Apply(initial,0); clock.Apply(View(1000010,true),10); clock.Apply(View(1000020),20);
        Check(!clock.Blocks(7,1200,1000,1350,1000,20), "removed effect tombstone rejects later replay before deadline");
        clock.Reset(); clock.Apply(initial,0); clock.Suspend();
        Check(!clock.Apply(initial,1) && !clock.Blocks(7,1200,1000,1350,1000,1), "suspended scene refuses old identical snapshot");
        Check(clock.Apply(View(1000010,false,2),10) && !clock.Apply(View(1000020,false,1),20), "older floor cannot replace newer floor even with later time");
        Check(!clock.Apply(View(1000001,false,2),30), "out-of-order timestamp rejected");
        clock.Reset(); clock.Apply(initial,0);
        Check(clock.Apply(Edit(r => { r["floorIndex"]=1L; Wall(r)["width"]=21L; }).Rift,1) && clock.CurrentHazards.Count == 4, "same effect ID conflict retires only that effect");
        var two=Edit(r => {r["floorIndex"]=1L; var copied=Object(BoundedJson.Parse(BoundedJson.Serialize(Hazard(r)))); copied["id"]="second-owner"; foreach(var o in List(copied["walls"])) Object(o)["id"] = "second-"+Object(o)["id"]; List(r["hazards"]).Add(copied);}).Rift;
        clock.Reset();clock.Apply(two,0);clock.Apply(initial,1);
        Check(clock.Blocks(7,1200,1000,1350,1000,1), "removing one overlapping owner preserves other owner");
        var state=State();Context(state,1);state.NoteRiftSceneChange();state.Apply(Snapshot());
        var firstRoot = new object();
        Check(state.CanUseRiftHazards(7, firstRoot), "fresh matching context and full snapshot bind to native map");
        Context(state,2,1000010);state.NoteRiftSceneChange();state.Apply(Snapshot(1000020,false,1));
        Check(!state.CanUseRiftHazards(7), "late old-floor packet cannot bind in reused native map");
        state.Apply(Snapshot(1000030,false,2));
        Check(!state.CanUseRiftHazards(7, firstRoot), "future same-map floor snapshot cannot bind to old root before construction");
        Check(state.CanUseRiftHazards(7, new object()), "new floor snapshot matches context identity and new root");
        state.NoteRiftSceneChange();Check(!state.CanUseRiftHazards(7), "context epoch cannot be consumed twice");
        state.Reset();Check(!state.CanUseRiftHazards(7), "disconnect removes wall authority");
        state=State();Context(state,1);state.Apply(Snapshot());var retainedRoot=new object();
        Check(state.CanUseRiftHazards(7,retainedRoot), "initial old root consumes original context");
        state.NoteRiftSceneChange();state.NoteRiftSceneChangeRejected(retainedRoot,7);
        state.Apply(Snapshot(1000010));
        Check(!state.CanUseRiftHazards(7,retainedRoot), "rejected load never restores cached authority from a snapshot alone");
        Context(state,1,1000000);state.Apply(Snapshot(1000011));
        Check(!state.CanUseRiftHazards(7,retainedRoot), "equal-timestamp cached context plus newer snapshot cannot recover rejected root");
        state.NoteRiftSceneChangeRejected(retainedRoot,7);
        Context(state,1,1000005);state.Apply(Snapshot(1000015));
        Check(!state.CanUseRiftHazards(7,retainedRoot), "cached context older than latest snapshot cannot recover rejected root");
        Context(state,1,1000020);state.Apply(Snapshot(1000030));
        Check(state.CanUseRiftHazards(7,retainedRoot), "server-confirmed old floor can resume on exact retained root after rejected load");
        Check(!state.CanUseRiftHazards(7,new object()), "rejection recovery does not authorize another root");
        Context(state,2,1000040);state.NoteRiftSceneChange();state.NoteRiftSceneChangeRejected(retainedRoot,7);
        state.Apply(Snapshot(1000050,false,2));
        Check(!state.CanUseRiftHazards(7,retainedRoot), "client rejection never reinterprets committed new floor as old root");
        Context(state,1,1000060);state.Apply(Snapshot(1000070,false,1));
        Check(!state.CanUseRiftHazards(7,retainedRoot), "late old-floor context cannot rewind a server-committed newer floor");
        NativeMovement();
        Console.WriteLine("PASS " + checks + " rift hazard/parser/clock/native movement contracts (deterministic substitutes, not Unity rendering).");
        return 0;
    }
    private static void NativeMovement()
    {
        var state=State();Context(state,1);state.Apply(Snapshot());
        EndgamePanel.WorldState=state; GameBootstrap.CurMapId=7; GameBootstrap.I=new GameBootstrap();
        var player=new PlayerController();PlayerController.I=player;player.worldX=1200;player.worldY=1000;
        player.SetPathToForIndependentRuntime(1350,1000);
        Check(!player.HasPath, "no-MASK fallback still rejects wall crossing");
        player.SetOpenMask(200,200,10);player.SetPathToForIndependentRuntime(1350,1000);
        Check(player.HasPath && player.PathSegmentsClear(state.RiftHazards), "actual A-star and smoothing route around temporary wall");
        int steps=0;while(player.HasPath && steps++<100) player.AdvanceDisplacementForIndependentRuntime();
        Check(Math.Abs(player.worldX-1350)<.01 && Math.Abs(player.worldY-1000)<.01, "native predicted path reaches destination without tunneling");
        Check(PlayerController.LineOfSight(1200,1000,1350,1000), "temporary wall does not change projectile LOS");
        player.worldX=1200;player.worldY=1000;player.SetPathToForIndependentRuntime(1201,1000);
        player.ForceDirectTarget(1350,1000);player.Step=10000;player.AdvanceDisplacementForIndependentRuntime();
        Check(player.worldX<1258, "actual fast native displacement sweep cannot cross between outside endpoints");
        var clean=State();Context(clean,1);clean.Apply(Snapshot(1000000,true));EndgamePanel.WorldState=clean;
        player=new PlayerController();player.SetOpenMask(200,200,10);player.worldX=1200;player.worldY=1000;
        player.SetPathToForIndependentRuntime(1350,1000);clean.Apply(Snapshot(1000010));
        long navigationOwner=player.NavigationOwner;
        var changed=player.AdvanceDisplacementForIndependentRuntime();
        Check(changed.Replanned && player.PathSegmentsClear(clean.RiftHazards), "new wall snapshot invalidates and replans cached route");
        Check(player.NavigationOwner==navigationOwner, "server wall replan preserves addon/combat movement intent owner");
        clean.NoteRiftSceneChange();GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime=new Transform();
        var replacement=new PlayerController();replacement.worldX=1200;replacement.worldY=1000;replacement.SetPathToForIndependentRuntime(1350,1000);
        Check(replacement.HasPath, "new controller never rebinds old same-map snapshot after scene invalidation");
    }
}
