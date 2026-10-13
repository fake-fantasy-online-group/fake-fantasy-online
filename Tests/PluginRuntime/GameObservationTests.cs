using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;

// Host-only invariants against the compiled adapter. Does not connect a server or create a
// GameObject, invoke Unity's player loop, or execute gameplay actions. Native rendering and
// live receipt coverage still require an explicitly authorized engine test session.
public static class GameObservationTests
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception(message);
    }
    private static Dictionary<string, object> Args(params object[] fields)
    {
        var result = new Dictionary<string, object>();
        for (int i = 0; i < fields.Length; i += 2) result.Add((string)fields[i], fields[i + 1]);
        return result;
    }
    private static void Throws<T>(Action action, string message) where T : Exception
    {
        try { action(); } catch (T) { assertions++; return; }
        throw new Exception(message);
    }
    private static void Emit(PluginGameApi api, string name, Dictionary<string, object> data)
    {
        typeof(PluginGameApi).GetMethod("Emit", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(api, new object[] { name, data });
    }
    private static IReadOnlyDictionary<string, object> PartySnapshot(PluginGameApi api, WorldPartyState state,
        long nativeVersion, int selfEntityId, params object[] fields) =>
        PartyIdentitySnapshot(api, state, nativeVersion, selfEntityId, 0, fields);
    private static IReadOnlyDictionary<string, object> PartyIdentitySnapshot(PluginGameApi api, WorldPartyState state,
        long nativeVersion, int selfEntityId, long selfCharacterId, params object[] fields)
    {
        var argumentType = typeof(PluginGameApi).GetNestedType("ObservationArguments", BindingFlags.NonPublic);
        var arguments = Activator.CreateInstance(argumentType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            null, new object[] { Args(fields) }, null);
        typeof(PluginGameApi).GetField("observationTextBudget", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(api, 65536);
        var data = (IReadOnlyDictionary<string, object>)typeof(PluginGameApi)
            .GetMethod("ObservePartySnapshot", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(api, new object[] { state, nativeVersion, selfEntityId, selfCharacterId, arguments });
        return new GameSnapshot("party", 1, 1, data).Data;
    }
    private static IReadOnlyList<object> Members(IReadOnlyDictionary<string, object> data) =>
        (IReadOnlyList<object>)data["members"];
    private static IReadOnlyDictionary<string, object> Member(IReadOnlyDictionary<string, object> data, int index) =>
        (IReadOnlyDictionary<string, object>)Members(data)[index];
    private static void TestPartySnapshots()
    {
        var api = new PluginGameApi(() => null);
        const long exactRevision = 9007199254740993L;
        var scene = typeof(PluginGameApi).GetField("sceneRevision", BindingFlags.Instance | BindingFlags.NonPublic);
        scene.SetValue(api, 27L);
        var members = new List<WorldPartyMemberState>
        {
            new WorldPartyMemberState(41, "Leader", 30, 350, 400, 1, 8, 9, true),
            new WorldPartyMemberState(42, "Away", 20, 0, 250, 0, 3, 4, false),
            new WorldPartyMemberState(43, "Member", 10, 110, 120, 1, 5, 6, true)
        };
        var characterIds = new List<long> { long.MaxValue, exactRevision };
        var professions = new List<byte> { 4 };
        var native = new WorldPartyState(41, "Native party", members, characterIds, professions);
        var data = PartySnapshot(api, native, exactRevision, 41);
        Check((bool)data["available"] && (bool)data["inParty"] && (bool)data["isLeader"], "native leader membership is observed");
        Check((string)data["name"] == "Native party" && (long)data["nativeVersion"] == exactRevision, "native party name and version are copied");
        Check((string)data["expectedPartyRevision"] == "9007199254740993", "party revision preserves exact integer above Lua double range as string");
        Check((string)data["leaderEntityId"] == "player:27:41" && (string)data["selfEntityId"] == "player:27:41", "party leader and self references are scene qualified");
        Check((int)data["leaderNativeId"] == 41 && (int)data["selfNativeId"] == 41, "party native identities are explicit");
        Check((int)data["count"] == 3 && (int)data["total"] == 3 && (int)data["offset"] == 0 && (int)data["limit"] == 128, "party default bounded page counts are explicit");
        Check(!(bool)data["truncated"] && data["nextOffset"] == null, "complete party page has no continuation");
        var leader = Member(data, 0);
        Check((string)leader["entityId"] == "player:27:41" && (int)leader["nativeId"] == 41, "member references preserve scene and native identity");
        Check((string)leader["name"] == "Leader" && (int)leader["level"] == 30 && (int)leader["hp"] == 350 && (int)leader["maxHp"] == 400, "native member name level and vitals are retained");
        Check((bool)leader["isSelf"] && (bool)leader["isLeader"] && (bool)leader["online"], "member self leadership and online flags are explicit");
        Check((int)leader["gender"] == 1 && (int)leader["hairId"] == 8 && (int)leader["hairColor"] == 9, "native member appearance fields are retained");
        Check((string)leader["characterId"] == "9223372036854775807" && (int)leader["profession"] == 4, "optional persistent identity stays exact and profession retains native value");
        Check(!(bool)Member(data, 1)["online"] && (int)Member(data, 1)["hp"] == 0, "offline members remain in native receipt order with received vitals");
        Check(Member(data, 1)["profession"] == null && Member(data, 2)["characterId"] == null, "missing native compatibility tails remain unknown");
        Check(!leader.ContainsKey("mp") && !leader.ContainsKey("x"), "party does not invent absent mana or position fields");

        var page = PartySnapshot(api, native, exactRevision, 42, "offset", 1, "limit", 1);
        Check((bool)page["inParty"] && !(bool)page["isLeader"] && (bool)Member(page, 0)["isSelf"], "nonleader self membership is independent of page offset");
        Check((int)page["total"] == 3 && (int)page["count"] == 1 && (int)page["offset"] == 1 && (int)page["limit"] == 1, "party offset selects a bounded page");
        Check((string)Member(page, 0)["name"] == "Away" && (string)Member(page, 0)["characterId"] == "9007199254740993", "native optional tail indices follow absolute member position");
        Check((bool)page["truncated"] && (int)page["nextOffset"] == 2, "party page provides exact next offset");
        var last = PartySnapshot(api, native, exactRevision, 41, "offset", 2, "limit", 1);
        Check((bool)last["inParty"] && (bool)last["isLeader"] && !(bool)last["truncated"] && last["nextOffset"] == null, "membership and leadership cover full native state even when self is outside page");
        var beyond = PartySnapshot(api, native, exactRevision, 41, "offset", 4096, "limit", 512);
        Check(Members(beyond).Count == 0 && (int)beyond["total"] == 3 && !(bool)beyond["truncated"] && beyond["nextOffset"] == null, "offset beyond native members returns empty terminal page");
        var outsider = PartySnapshot(api, native, exactRevision, 99);
        Check(!(bool)outsider["inParty"] && !(bool)outsider["isLeader"], "unlisted local player is not a party member or leader");
        var recycled = PartyIdentitySnapshot(api, native, exactRevision, 41, 99);
        Check(!(bool)recycled["inParty"] && !(bool)recycled["isLeader"] && !(bool)Member(recycled, 0)["isSelf"], "persistent character mismatch rejects a recycled equal entity ID");
        var sameCharacter = PartyIdentitySnapshot(api, native, exactRevision, 99, long.MaxValue);
        Check((bool)sameCharacter["inParty"] && (bool)sameCharacter["isLeader"] && (bool)Member(sameCharacter, 0)["isSelf"], "persistent character match wins over a changed entity ID");
        var absentTail = PartyIdentitySnapshot(api, native, exactRevision, 43, 99);
        Check((bool)absentTail["inParty"] && (bool)Member(absentTail, 2)["isSelf"], "missing persistent identity tail falls back to native entity ID");
        var unknownSelf = PartySnapshot(api, native, exactRevision, 0);
        Check(unknownSelf["selfEntityId"] == null && !(bool)unknownSelf["inParty"] && !(bool)unknownSelf["isLeader"], "missing local player never acquires party membership");
        var empty = PartySnapshot(api, WorldPartyState.Empty, 0, 41);
        Check((bool)empty["available"] && Members(empty).Count == 0 && empty["leaderEntityId"] == null && !(bool)empty["inParty"] && !(bool)empty["isLeader"], "received empty party remains available and explicitly out of party");
        Check((string)empty["expectedPartyRevision"] == "0", "empty native revision is an exact string");
        var staleLeader = PartySnapshot(api, new WorldPartyState(41, "", new WorldPartyMemberState[0], new long[0]), 1, 41);
        Check(!(bool)staleLeader["inParty"] && !(bool)staleLeader["isLeader"], "leader ID alone cannot imply membership");
        var noLeader = PartyIdentitySnapshot(api, new WorldPartyState(0, "",
            new[] { new WorldPartyMemberState(0, "Self", 1, 1, 1, 0, 0, 0, true) }, new long[] { 99 }), 1, 41, 99);
        Check((bool)noLeader["inParty"] && !(bool)noLeader["isLeader"] && !(bool)Member(noLeader, 0)["isLeader"], "zero leader sentinel never confers leadership on persistent self member");
        var missing = PartySnapshot(api, null, 0, 41);
        Check(!(bool)missing["available"] && (string)missing["code"] == "not_received", "unpublished party differs from received empty party");

        members.Clear(); characterIds.Clear(); professions.Clear();
        Check(native.Members.Count == 3 && native.CharacterIds.Count == 2 && native.Professions.Count == 1, "native party snapshot owns immutable membership and optional tails");
        Check(Members(data).Count == 3 && (string)Member(data, 0)["characterId"] == "9223372036854775807", "returned party snapshot survives source replacement independently");
        Throws<NotSupportedException>(() => ((IList<object>)Members(data)).Clear(), "returned member page is immutable");
        Throws<NotSupportedException>(() => ((IDictionary<string, object>)leader)["name"] = "changed", "returned member fields are immutable");
        scene.SetValue(api, 28L);
        var newScene = PartySnapshot(api, native, exactRevision, 41);
        Check((string)newScene["leaderEntityId"] == "player:28:41" && (string)Member(newScene, 0)["entityId"] == "player:28:41", "new scene replaces party entity references");
        Check((string)data["leaderEntityId"] == "player:27:41", "prior party snapshot retains its original scene identity");
        var largeMembers = new List<WorldPartyMemberState>();
        for (int i = 0; i < 600; i++) largeMembers.Add(new WorldPartyMemberState(i + 1, new string('n', 600), 1, 1, 1, 0, 0, 0, true));
        var large = PartySnapshot(api, new WorldPartyState(1, new string('p', 600), largeMembers, null), long.MaxValue, 1, "limit", 10000);
        Check(Members(large).Count == 512 && (int)large["total"] == 600 && (int)large["nextOffset"] == 512, "party hard row bound applies to oversized native input and requested limit");
        Check((string)large["expectedPartyRevision"] == "9223372036854775807", "maximum signed native party version remains exact");
        int textLength = ((string)large["name"]).Length;
        foreach (object item in Members(large)) textLength += ((string)((IReadOnlyDictionary<string, object>)item)["name"]).Length;
        Check(textLength <= 65536 && ((string)Member(large, 0)["name"]).Length == 256, "party projection honors per-name and aggregate text budgets");
        Check((int)PartySnapshot(api, native, 1, 41, "limit", 0)["count"] == 1, "party shares minimum normalized page size");

        var token = typeof(PluginGameApi).GetMethod("PartyObservationToken", BindingFlags.Static | BindingFlags.NonPublic);
        string original = (string)token.Invoke(null, new object[] { exactRevision, 41, 99L, true });
        Check(original == (string)token.Invoke(null, new object[] { exactRevision, 41, 99L, true }), "unchanged party native version and self retain cache token");
        Check(original != (string)token.Invoke(null, new object[] { exactRevision + 1, 41, 99L, true }), "native party receipt invalidates observation cache token");
        Check(original != (string)token.Invoke(null, new object[] { exactRevision, 42, 99L, true }), "local entity change invalidates membership and leadership cache token");
        Check(original != (string)token.Invoke(null, new object[] { exactRevision, 41, 100L, true }), "local persistent character change invalidates membership and leadership cache token");
        Check(original != (string)token.Invoke(null, new object[] { exactRevision, 41, 99L, false }), "same-frame world readiness change invalidates cached party unavailability");
        api.Dispose();
    }
    public static int Main()
    {
        TestPartySnapshots();
        var api = new PluginGameApi(() => null);
        Check(api.GetQueries().Count == 20, "all supported domains are discoverable");
        var names = new HashSet<string>();
        foreach (var definition in api.GetQueries()) Check(names.Add(definition.Domain), "domains are unique");
        var schemas = new Dictionary<string, GameQueryDefinition>();
        foreach (var definition in api.GetQueries()) schemas.Add(definition.Domain, definition);
        foreach (string domain in new[] { "endgame", "endgame.items", "endgame.catalog", "endgame.skills", "endgame.rift" })
        {
            Check(names.Contains(domain), "endgame domain discoverable: " + domain);
            var queryArgs = domain == "endgame.catalog" ? Args("catalog", "uniques") : Args();
            GameSnapshot offline = api.Observe(new GameQuery(domain, queryArgs));
            Check(!(bool)offline.Data["available"] && !(bool)offline.Data["negotiated"] && !(bool)offline.Data["inWorld"], "offline endgame state explicit: " + domain);
            Check((string)offline.Data["expectedRevision"] == "0" && (string)offline.Data["snapshotId"] == "" && (string)offline.Data["snapshotRevision"] == "0", "offline endgame revisions are empty exact strings: " + domain);
            Check(((string)offline.Data["apiInstanceId"]).Length > 0, "offline endgame exposes adapter restart identity: " + domain);
        }
        foreach (string domain in new[] { "endgame.items", "endgame.catalog", "endgame.skills", "endgame.rift", "party" })
        {
            var properties = (IReadOnlyDictionary<string, object>)schemas[domain].Arguments["properties"];
            var offsetSchema = (IReadOnlyDictionary<string, object>)properties["offset"];
            Check(Convert.ToInt32(offsetSchema["minimum"]) == 0 && Convert.ToInt32(offsetSchema["maximum"]) == 4096, "endgame offset discovery bounds: " + domain);
            var limitSchema = (IReadOnlyDictionary<string, object>)properties["limit"];
            Check(Convert.ToInt32(limitSchema["minimum"]) == 1 && Convert.ToInt32(limitSchema["maximum"]) == 512, "endgame limit discovery bounds: " + domain);
        }
        var catalogSchema = schemas["endgame.catalog"].Arguments;
        Check(((IReadOnlyList<object>)catalogSchema["required"]).Count == 1 && (string)((IReadOnlyList<object>)catalogSchema["required"])[0] == "catalog", "catalog selector is required in discovery");
        var catalogProperties = (IReadOnlyDictionary<string, object>)catalogSchema["properties"];
        var catalogKinds = (IReadOnlyList<object>)((IReadOnlyDictionary<string, object>)catalogProperties["catalog"])["enum"];
        Check(catalogKinds.Count == 3 && (string)catalogKinds[0] == "uniques" && (string)catalogKinds[1] == "runes" && (string)catalogKinds[2] == "formations", "catalog discovery enumerates exact native catalogs");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.catalog", Args("catalog", "recipes"))), "retired recipes catalog is rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.catalog")), "catalog selector required before offline fallback");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.catalog", Args("catalog", "invented"))), "unknown catalog rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.items", Args("offset", -1))), "negative endgame offset rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.items", Args("offset", 4097))), "oversized endgame offset rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.items", Args("offset", 1.0000000000000002))), "fractional endgame offset rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.items", Args("uid", 9007199254740993L))), "numeric UID observation filter rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.items", Args("uid", "01"))), "noncanonical UID observation filter rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.items", Args("location", "invented"))), "unknown item location rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("endgame.rift", Args("uid", "9007199254740993"))), "unrelated UID filter cannot be ignored by rift");
        Check(!(bool)api.Observe(new GameQuery("endgame.items", Args("uid", "9223372036854775807", "offset", 4096, "limit", 512))).Data["available"], "valid maximum string UID and pagination bounds reach offline state");
        Check(names.Contains("party"), "native party domain is discoverable");
        var offlineParty = api.Observe(new GameQuery("party", Args("offset", 4096, "limit", 512)));
        Check(!(bool)offlineParty.Data["available"] && (string)offlineParty.Data["code"] == "not_in_world", "offline party requires active built native world");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("party", Args("offset", -1))), "negative party offset rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("party", Args("offset", 4097))), "oversized party offset rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("party", Args("offset", 1.5))), "fractional party offset rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("party", Args("offset", "1"))), "string party offset rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("party", Args("limit", 1.5))), "fractional party limit rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("party", Args("entityId", "player:1:41"))), "unadvertised party filters cannot be silently ignored");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("party", Args("expectedPartyRevision", "1"))), "party mutation guard is not an observation filter");
        GameSnapshot world = api.Observe(new GameQuery("world"));
        Check((bool)world.Data["available"], "world/session state remains available while offline");
        Check(!(bool)world.Data["worldReady"], "offline world is not ready");
        Check(world.SceneRevision > 0, "initial scene generation exists");
        Check(!world.Data.ContainsKey("eventSequence"), "cached world does not advertise a stale live event cursor");
        var centerKey = typeof(PluginGameApi).GetMethod("WithObservationCenter", BindingFlags.Static | BindingFlags.NonPublic);
        string centered1 = (string)centerKey.Invoke(null, new object[] { "entities|radius=100", true, 10f, 20f });
        string centered2 = (string)centerKey.Invoke(null, new object[] { "entities|radius=100", true, 11f, 20f });
        Check(centered1 != centered2, "implicit radius center invalidates cached query on local movement");
        Check((string)centerKey.Invoke(null, new object[] { "fixed", false, 10f, 20f }) ==
            (string)centerKey.Invoke(null, new object[] { "fixed", false, 11f, 20f }), "explicit radius center is independent of local movement");
        Check(api.Observe(new GameQuery("world")).Revision == world.Revision, "unchanged native state keeps revision");
        GameSnapshot missing = api.Observe(new GameQuery("inventory"));
        Check(!(bool)missing.Data["available"], "offline inventory is explicitly unavailable");
        GameSnapshot unknown = api.Observe(new GameQuery("does-not-exist"));
        Check(!(bool)unknown.Data["available"], "unknown domain is unavailable");
        Check((string)unknown.Data["code"] == "unknown_domain", "unknown domain gets precise code");
        GameSnapshot unchanged = api.Observe(new GameQuery("world", Args("sinceRevision", world.Revision,
            "sinceQueryKey", world.Data["queryKey"], "expectedSceneRevision", world.SceneRevision)));
        Check((bool)unchanged.Data["unchanged"], "same query and revision produce unchanged delta");
        GameSnapshot noQueryKey = api.Observe(new GameQuery("world", Args("sinceRevision", world.Revision)));
        Check(!(bool)noQueryKey.Data["unchanged"], "revision alone cannot reuse different filters");
        GameSnapshot otherQuery = api.Observe(new GameQuery("world", Args("limit", 1, "sinceRevision", world.Revision,
            "sinceQueryKey", world.Data["queryKey"])));
        Check(!(bool)otherQuery.Data["unchanged"], "filter change cannot reuse old query");
        GameSnapshot stale = api.Observe(new GameQuery("world", Args("expectedSceneRevision", world.SceneRevision + 1)));
        Check((bool)stale.Data["resyncRequired"], "scene mismatch requires resync");
        Check(!(bool)api.Observe(new GameQuery("world")).Data["resyncRequired"], "resync response cannot poison snapshot cache");
        var unavailableDelta = api.Observe(new GameQuery("inventory", Args("sinceRevision", missing.Revision,
            "sinceQueryKey", missing.Data["queryKey"])));
        Check(!(bool)unavailableDelta.Data["available"], "unchanged response preserves unavailable status");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("world", Args("itemId", 1))), "irrelevant filters must not be silently ignored");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("npcs", Args("x", 10))), "partial center rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("npcs", Args("radius", -1))), "negative radius rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("inventory", Args("tab", 1.25))), "fractional integer rejected");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("inventory", Args("tab", 1.0000000000000002))), "decimal conversion cannot hide fractional double");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("inventory", Args("tab", 1e-30))), "decimal underflow cannot hide fractional double");
        Throws<ArgumentException>(() => api.Observe(new GameQuery("world", Args("sinceRevision", 9007199254740992d))), "unsafe double revision rejected");
        Throws<ArgumentOutOfRangeException>(() => api.ReadEvents(-1, 1), "negative cursor rejected");
        var first = api.ReadEvents(0, 512);
        Check(first.ResyncRequired && first.Events.Count > 0, "initial scene event requests resync");
        var second = api.ReadEvents(0, 512);
        Check(second.Events.Count == first.Events.Count && second.NextSequence == first.NextSequence,
            "two consumers independently read identical events");
        long cursor = first.NextSequence;
        var payload = Args("value", 1);
        Emit(api, "test", payload);
        payload["value"] = 2;
        var immutable = api.ReadEvents(cursor, 1);
        Check((int)immutable.Events[0].Data["value"] == 1, "event payload is copied before native mutation");
        Check(api.ReadEvents(cursor, 1).NextSequence == immutable.NextSequence, "reading events never consumes them");
        for (int i = 0; i < 1100; i++) Emit(api, "retained", Args("index", i));
        var gap = api.ReadEvents(cursor, 512);
        Check(gap.ResyncRequired, "lagging consumer sees retention gap");
        Check(gap.Events.Count == 512, "event count is bounded");
        var next = api.ReadEvents(gap.NextSequence, 512);
        Check(!next.ResyncRequired && next.Events.Count == 512, "caught-up cursor resumes independent retained stream");
        Check(api.ReadEvents(next.NextSequence, 512).Events.Count == 0, "caught-up cursor has no phantom events");
        Check(api.ReadEvents(long.MaxValue, 1).ResyncRequired, "future cursor after restart requests resync");
        Exception wrongThread = null;
        var thread = new Thread(() => { try { api.Observe(new GameQuery("world")); } catch (Exception e) { wrongThread = e; } });
        thread.Start(); thread.Join();
        Check(wrongThread is InvalidOperationException, "worker thread cannot access Unity owners");
        typeof(PluginGameApi).GetField("lastMapId", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(api, -1);
        GameSnapshot replacement = api.Observe(new GameQuery("world"));
        Check(replacement.SceneRevision > world.SceneRevision, "scene identity transition invalidates prior references");
        Check(replacement.Revision > world.Revision, "scene transition invalidates cached revisions");
        api.Dispose(); api.Dispose();
        Throws<ObjectDisposedException>(() => api.GetQueries(), "disposed adapter rejects access");
        Console.WriteLine("Observation adapter: " + assertions + " assertions passed.");
        return 0;
    }
}
