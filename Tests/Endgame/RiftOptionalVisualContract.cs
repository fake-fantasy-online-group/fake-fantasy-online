using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

class RiftOptionalVisualContract
{
    static int checks;
    static string fixture;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static Dictionary<string, object> Obj(object value) => (Dictionary<string, object>)value;
    static List<object> Rows(object value) => (List<object>)value;
    static EndgameResponse Edit(Action<Dictionary<string, object>, Dictionary<string, object>> change)
    {
        var root = Obj(BoundedJson.Parse(fixture)); var rift = Obj(root["rift"]);
        rift["progress"] = 120L; change(root, rift);
        return EndgameResponse.Parse(BoundedJson.Serialize(root));
    }
    static void Reject(Action action, string message)
    {
        try { action(); } catch (FormatException) { checks++; return; }
        throw new Exception("Accepted " + message);
    }
    static EndgameClientState State()
    {
        var state = new EndgameClientState(); state.Begin(new EndgameRequest("hello"));
        state.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"));
        return state;
    }
    static void Main(string[] args)
    {
        fixture = File.ReadAllText(Path.Combine(args[0], "elite-wire-v6.json"));
        var baseline = Edit((root, rift) => { });
        var additive = Edit((root, rift) =>
        {
            root["futureEnvelope"] = "extra"; rift["futureField"] = new List<object>{1L, 2L};
            Obj(Rows(rift["auras"])[0])["futureAura"] = true;
            foreach (var item in Rows(rift["hazards"])) Obj(item)["futureHazard"] = "extra";
            foreach (var item in Rows(Obj(Rows(rift["hazards"])[4])["walls"])) Obj(item)["futureWall"] = true;
        });
        Check(additive.Rift.Progress == 120 && additive.Rift.Hazards.Count == 5 && additive.Rift.SkippedVisualEntries == 0, "additive snapshot and nested visual metadata remain forward compatible");
        var shortAura = Edit((root, rift) => { Obj(Rows(rift["auras"])[0])["class"] = 2L; });
        Check(shortAura.Rift.Auras[0].Class == 2 && shortAura.Rift.Auras[0].AbilityIDs.Count == 1 && shortAura.Rift.Progress == 120, "removed ability does not discard classification or progress");
        var unknownAbility = Edit((root, rift) => { Obj(Rows(rift["auras"])[0])["abilityIds"] = new List<object>{3L, 5L, 42L, 1000L}; });
        Check(unknownAbility.Rift.Auras[0].AbilityIDs.Count == 4 && unknownAbility.Rift.Auras[0].AbilityIDs[1] == 5 && unknownAbility.Rift.Progress == 120, "unknown IDs and independent ability count preserve known agility metadata");
        var unknownEffect = Edit((root, rift) => { Obj(Rows(rift["hazards"])[0])["mechanism"] = "future_storm"; });
        Check(unknownEffect.Rift.Hazards.Count == 4 && unknownEffect.Rift.Progress == 120 && unknownEffect.Rift.Auras.Count == baseline.Rift.Auras.Count && unknownEffect.Rift.SkippedVisualEntries == 1, "unknown mechanism isolates one optional effect");
        var unsafeEffect = Edit((root, rift) => { Obj(Rows(Obj(Rows(rift["hazards"])[4])["walls"])[0])["width"] = 0L; });
        Check(unsafeEffect.Rift.Hazards.Count == 4 && unsafeEffect.Rift.Progress == 120 && unsafeEffect.Rift.SkippedVisualEntries == 1, "unsafe wall geometry is not rendered and cannot veto unrelated progress");
        var unknownClass = Edit((root, rift) => { Obj(Rows(rift["auras"])[0])["class"] = 99L; });
        Check(unknownClass.Rift.Auras.Count == 0 && unknownClass.Rift.Hazards.Count == 5 && unknownClass.Rift.Progress == 120, "unknown actor classification is skipped, not miscolored or misclassified");
        var invalidIdentity = Edit((root, rift) => { Obj(Rows(rift["auras"])[0])["entityId"] = "4294967296"; });
        Check(invalidIdentity.Rift.Auras.Count == 0 && invalidIdentity.Rift.Progress == 120, "overflowing actor identity never reaches world lookup");
        var duplicates = Edit((root, rift) => { Rows(rift["hazards"]).Add(Rows(rift["hazards"])[0]); });
        Check(duplicates.Rift.Hazards.Count == 4 && duplicates.Rift.SkippedVisualEntries == 2, "duplicate optional row never duplicates a world effect");
        var unknownTell = Edit((root, rift) => { rift["telegraphs"] = new List<object>{new Dictionary<string, object>{{"id","future"},{"entityId","134217833"},{"shape","future"},{"radius",100L}}}; });
        Check(unknownTell.Rift.Telegraphs.Count == 0 && unknownTell.Rift.Progress == 120, "unsupported shape cannot masquerade as a supported warning");
        var malformedArrival = Edit((root, rift) => { rift["phase"] = "boss_arriving"; rift["guardianArrival"] = new Dictionary<string,object>{{"durationMS",100L}}; });
        Check(malformedArrival.Rift.GuardianArrival == null && malformedArrival.Rift.Progress == 120, "unsupported arrival rendering is isolated from run state");
        var state = State(); state.Apply(baseline);
        var changed = Edit((root, rift) => { rift["revision"]="8"; rift["progress"]=240L; Obj(Rows(Obj(Rows(rift["hazards"])[4])["walls"])[0])["width"]=21L; });
        Check(state.Apply(changed) && state.Rift.Progress == 240 && state.RiftHazards.CurrentHazards.Count == 4, "immutable effect identity conflict retires that effect but applies progress and known effects");
        Check(state.RiftHazards.CurrentHazards[0].ID == baseline.Rift.Hazards[0].ID, "unrelated effect is not restarted or removed by identity conflict");
        var expiredConflict = Edit((root, rift) =>
        {
            rift["revision"]="9"; rift["progress"]=360L; rift["serverNowMS"]=1015001L;
            var wall=Obj(Rows(rift["hazards"])[4]); rift["hazards"]=new List<object>{wall};
            wall["expiresMS"]=1020001L; wall["remainingMS"]=5000L; Obj(Rows(wall["walls"])[0])["width"]=21L;
        });
        var clock=new EndgameRiftHazardState(); clock.Apply(baseline.Rift,0);
        Check(clock.Apply(expiredConflict.Rift,15001) && clock.CurrentHazards.Count==0, "conflicting expired ID cannot be removed and re-added in the same snapshot");
        Check(clock.Apply(expiredConflict.Rift,15002) && clock.CurrentHazards.Count==0, "replayed conflicting expired ID cannot resurrect");
        var conflictGap=Edit((root,rift)=>{rift["revision"]="9";rift["serverNowMS"]=1015001L;rift["hazards"]=new List<object>();});
        Check(clock.Apply(conflictGap.Rift,15003) && clock.CurrentHazards.Count==0, "conflict tombstone survives an absent snapshot until every observed deadline");
        Check(clock.Apply(expiredConflict.Rift,15004) && clock.CurrentHazards.Count==0, "conflicting ID cannot replay after an absent snapshot");
        foreach(bool reversed in new[]{false,true})
        {
            var conflict=Edit((root,rift)=>
            {
                var rows=Rows(rift["hazards"]); var copy=Obj(BoundedJson.Parse(BoundedJson.Serialize(rows[4])));
                Obj(Rows(copy["walls"])[0])["width"]=21L; rows.Add(copy); if(reversed) rows.Reverse();
            });
            Check(conflict.Rift.Hazards.Count==4 && conflict.Rift.SkippedVisualEntries==2, "both conflicting duplicate IDs are isolated regardless of row order");
            var sharedWall=Edit((root,rift)=>
            {
                var rows=Rows(rift["hazards"]); var copy=Obj(BoundedJson.Parse(BoundedJson.Serialize(rows[4])));
                copy["id"]="other-wall-owner";
                for(int i=1;i<Rows(copy["walls"]).Count;i++) Obj(Rows(copy["walls"])[i])["id"]="other-wall-"+i;
                Obj(Rows(copy["walls"])[0])["width"]=21L; rows.Add(copy); if(reversed) rows.Reverse();
            });
            Check(sharedWall.Rift.Hazards.Count==4 && sharedWall.Rift.SkippedVisualEntries==2, "distinct hazards sharing a wall ID are both isolated regardless of row order");
        }
        var unsupportedWall=Edit((root,rift)=>{rift["revision"]="8";Obj(Rows(rift["hazards"])[4])["mechanism"]="future";});
        clock=new EndgameRiftHazardState();clock.Apply(baseline.Rift,0);clock.Apply(unsupportedWall.Rift,1);
        Check(clock.CurrentHazards.Count==4 && !clock.HasWalls(7,1), "known effect becoming unsupported removes stale collision and preserves unrelated effects");
        Reject(() => Edit((root, rift) => rift["mapId"] = 0L), "invalid native map scope");
        Reject(() => Edit((root, rift) => rift["serverNowMS"] = 0L), "missing authoritative clock");
        Reject(() => Edit((root, rift) => rift["progress"] = -1L), "negative progress");
        Reject(() => Edit((root, rift) => { var rows=Rows(rift["hazards"]); while(rows.Count<=56) rows.Add(rows[0]); }), "effect array resource budget");
        Reject(() => EndgameResponse.Parse("{\"kind\":\"hello\",\"future\":true}"), "unrelated protocol is not weakened");
        Reject(() => EndgameResponse.Parse("{\"kind\":\"rift.context\",\"future\":true}"), "scene context authorization is not weakened");
        Reject(() => EndgameResponse.Parse("{\"kind\":\"rift.state\",\"rift\":{\"progress\":NaN}}"), "non-JSON nonfinite number");
        fixture = File.ReadAllText(Path.Combine(args[0], "elite-wire-v6-metadata.json"));
        var golden = Edit((root, rift) => { Obj(Rows(rift["auras"])[0])["abilityIds"] = new List<object>{0L, 99L}; });
        int clones=0; foreach(var aura in golden.Rift.Auras) if(aura.CloneOf.Length>0) clones++;
        Check(golden.Rift.Auras[0].GoldenUntilMS > golden.Rift.ServerNowMS && clones == 3 && golden.Rift.Impacts.Count == 2, "future abilities do not suppress real golden deadline, clones or impacts");
        Console.WriteLine("PASS " + checks + " optional rift metadata isolation and retained resource/identity safeguards.");
    }
}
