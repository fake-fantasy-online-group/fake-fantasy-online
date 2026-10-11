// Exercises the production party observation partial with deterministic native substitutes.
// Generic Observe readiness, schema, argument validation and cache integration live in
// GameObservationTests; this runner requires neither Unity nor server gameplay.
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static partial class GameActionAdapterTests
{
    private static void RunPartyObservationTests()
    {
        int initial = checks;
        var api = Setup();
        var session = api.TestSession;
        CharData.charId = 0;
        const long exactVersion = 9007199254740993L;
        session.PartyStateVersion = exactVersion;
        session.PartyState = new WorldPartyState { LeaderEntityId = 1, PartyName = "Native party" };
        session.PartyState.Members.Add(new WorldPartyMemberState {
            EntityId = 1, DisplayName = "Leader", Level = 30, HitPoints = 350, MaximumHitPoints = 400,
            Gender = 1, HairId = 8, HairColor = 9, Online = true });
        session.PartyState.Members.Add(new WorldPartyMemberState {
            EntityId = 2, DisplayName = "Away", Level = 20, HitPoints = 0, MaximumHitPoints = 250, Online = false });
        session.PartyState.Members.Add(new WorldPartyMemberState {
            EntityId = 3, DisplayName = "Member", Level = 10, HitPoints = 110, MaximumHitPoints = 120, Online = true });
        session.PartyState.CharacterIds.AddRange(new[] { long.MaxValue, exactVersion });
        session.PartyState.Professions.Add(4);

        var data = api.TestObserveParty();
        Check((bool)data["available"] && (bool)data["inParty"] && (bool)data["isLeader"], "party observation derives native membership and leadership");
        Check((string)data["name"] == "Native party" && (long)data["nativeVersion"] == exactVersion &&
            (string)data["expectedPartyRevision"] == "9007199254740993", "party name and native version preserve exact guard above double integer range");
        Check((string)data["leaderEntityId"] == "player:1:1" && (string)data["selfEntityId"] == "player:1:1" &&
            (int)data["leaderNativeId"] == 1 && (int)data["selfNativeId"] == 1, "party observation scene-qualified and raw identities agree");
        Check((int)data["count"] == 3 && (int)data["total"] == 3 && (int)data["offset"] == 0 && (int)data["limit"] == 128 &&
            !(bool)data["truncated"] && data["nextOffset"] == null, "full native party page reports exact counts and terminal cursor");
        var leader = Object(Rows(data["members"])[0]);
        Check((string)leader["entityId"] == "player:1:1" && (int)leader["nativeId"] == 1 && (string)leader["name"] == "Leader", "party member identity and name are copied");
        Check((int)leader["level"] == 30 && (int)leader["hp"] == 350 && (int)leader["maxHp"] == 400 && (bool)leader["online"], "party native level and vitals are copied");
        Check((bool)leader["isSelf"] && (bool)leader["isLeader"] && (int)leader["gender"] == 1 && (int)leader["hairId"] == 8 && (int)leader["hairColor"] == 9, "party member flags and native appearance are copied");
        Check((string)leader["characterId"] == "9223372036854775807" && (int)leader["profession"] == 4, "party optional character ID is exact and profession is native");
        Check(!leader.ContainsKey("mp") && !leader.ContainsKey("x"), "party observation does not invent absent mana or positions");
        CharData.charId = 99;
        var recycled = api.TestObserveParty();
        Check(!(bool)recycled["inParty"] && !(bool)recycled["isLeader"] && !(bool)Object(Rows(recycled["members"])[0])["isSelf"], "party persistent mismatch rejects recycled scene entity identity");
        CharData.charId = long.MaxValue;
        GameBootstrap.I.Window.Frame.Player.EntityId = 99;
        var persistent = api.TestObserveParty();
        Check((bool)persistent["inParty"] && (bool)persistent["isLeader"] && (bool)Object(Rows(persistent["members"])[0])["isSelf"], "party persistent self wins over changed scene entity identity");
        CharData.charId = 99;
        GameBootstrap.I.Window.Frame.Player.EntityId = 3;
        var missingTail = api.TestObserveParty();
        Check((bool)missingTail["inParty"] && (bool)Object(Rows(missingTail["members"])[2])["isSelf"], "party absent character tail falls back to entity identity");
        CharData.charId = 0;

        GameBootstrap.I.Window.Frame.Player.EntityId = 2;
        var page = api.TestObserveParty(1, 1);
        var away = Object(Rows(page["members"])[0]);
        Check((bool)page["inParty"] && !(bool)page["isLeader"] && (bool)away["isSelf"] && !(bool)away["isLeader"], "party membership follows current frame player identity");
        Check((int)page["count"] == 1 && (int)page["total"] == 3 && (int)page["nextOffset"] == 2 && (bool)page["truncated"], "party limit and offset select native receipt order");
        Check(!(bool)away["online"] && (int)away["hp"] == 0 && (string)away["characterId"] == "9007199254740993" && away["profession"] == null, "offline member remains visible and optional tails use absolute row index");
        GameBootstrap.I.Window.Frame.Player.EntityId = 1;
        var last = api.TestObserveParty(2, 1);
        Check((bool)last["inParty"] && (bool)last["isLeader"] && !(bool)last["truncated"] && last["nextOffset"] == null &&
            Object(Rows(last["members"])[0])["characterId"] == null, "self outside page retains membership; final page has no continuation or invented character ID");
        var beyond = api.TestObserveParty(4096, 512);
        Check(Rows(beyond["members"]).Count == 0 && (int)beyond["total"] == 3 && !(bool)beyond["truncated"] && beyond["nextOffset"] == null, "party beyond-end offset is an empty terminal page");
        api.TestScene = 2;
        Check((string)api.TestObserveParty()["leaderEntityId"] == "player:2:1" && (string)data["leaderEntityId"] == "player:1:1", "scene-qualified party references refresh without changing old snapshots");
        GameBootstrap.I.Window.Frame.Player.EntityId = 99;
        var outsider = api.TestObserveParty();
        Check(!(bool)outsider["inParty"] && !(bool)outsider["isLeader"], "party flags reject a local identity absent from full membership");
        GameBootstrap.I.Window.Frame.Player = null;
        var noSelf = api.TestObserveParty();
        Check(noSelf["selfEntityId"] == null && !(bool)noSelf["inParty"] && !(bool)noSelf["isLeader"], "party missing local player identity is explicit");
        GameBootstrap.I.Window.Frame.Player = new LocalPlayer();

        // Test substitutes are mutable so mutation verifies the production DTO boundary.
        session.PartyState.Members[0].DisplayName = "Changed";
        session.PartyState.Members.Clear(); session.PartyState.CharacterIds.Clear(); session.PartyState.Professions.Clear();
        Check(Rows(data["members"]).Count == 3 && (string)leader["name"] == "Leader" && (string)leader["characterId"] == "9223372036854775807", "party returned DTO is detached from later native replacement");
        bool immutable = false;
        try { ((IDictionary<string, object>)leader)["name"] = "Mutated"; } catch (NotSupportedException) { immutable = true; }
        Check(immutable, "party returned member DTO cannot be mutated");
        var absentMember = api.TestObserveParty();
        Check(!(bool)absentMember["inParty"] && !(bool)absentMember["isLeader"], "stale leader ID alone cannot imply local party membership");
        CharData.charId = 99;
        session.PartyState = new WorldPartyState();
        session.PartyState.Members.Add(new WorldPartyMemberState { EntityId = 0, DisplayName = "Self" });
        session.PartyState.CharacterIds.Add(99);
        var noLeader = api.TestObserveParty();
        Check((bool)noLeader["inParty"] && !(bool)noLeader["isLeader"] && !(bool)Object(Rows(noLeader["members"])[0])["isLeader"], "zero leader sentinel cannot confer leadership through persistent self identity");
        CharData.charId = 0;
        session.PartyState = new WorldPartyState(); session.PartyStateVersion = 0;
        var empty = api.TestObserveParty();
        Check((bool)empty["available"] && Rows(empty["members"]).Count == 0 && empty["leaderEntityId"] == null && (string)empty["expectedPartyRevision"] == "0", "received empty party is available with exact zero guard");
        session.PartyState = null;
        Check(!(bool)api.TestObserveParty()["available"] && (string)api.TestObserveParty()["code"] == "not_received", "missing native party receipt is unavailable");

        session.PartyState = new WorldPartyState { PartyName = new string('p', 600) };
        for (int i = 0; i < 600; i++) session.PartyState.Members.Add(new WorldPartyMemberState { EntityId = i + 1, DisplayName = new string('n', 600) });
        session.PartyStateVersion = long.MaxValue;
        var large = api.TestObserveParty(0, 512);
        Check(Rows(large["members"]).Count == 512 && (int)large["total"] == 600 && (int)large["nextOffset"] == 512, "party projection respects normalized row bound");
        Check(((string)large["name"]).Length == 256 && ((string)Object(Rows(large["members"])[0])["name"]).Length == 256 &&
            (string)large["expectedPartyRevision"] == "9223372036854775807", "party names are bounded and maximum native revision stays exact");
        int reads = 0;
        session.PartyVersionReader = () => ++reads == 1 ? 1 : 2;
        Check((string)api.TestObserveParty()["expectedPartyRevision"] == "2" && reads == 4, "party observation retries unstable root/version pair before exposing guard");
        reads = 0; session.PartyVersionReader = () => ++reads;
        var changing = api.TestObserveParty();
        Check(!(bool)changing["available"] && (string)changing["code"] == "party_changing" && reads == 6, "party capture has a bounded three-attempt retry for ongoing publication");
        Check(session.Sends == 0 && session.LastPartyAction == null, "party observations never send native commands");
        Console.WriteLine("Party observation adapter passed: " + (checks - initial) + " assertions (production projection; native substitutes).");
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class PluginGameApi
    {
        private ReferenceFirstWorldInitialFrame ObservationFrame => GameBootstrap.I?.Window?.Frame;
        private static Dictionary<string, object> Unavailable(string code, string reason) =>
            Values("available", false, "code", code, "reason", reason);
        public IReadOnlyDictionary<string, object> TestObserveParty(int offset = 0, int limit = 128) =>
            new GameSnapshot("party", TestScene, 1, ObserveParty(TestSession, new ObservationArguments { Offset = offset, Limit = limit })).Data;
    }
}
