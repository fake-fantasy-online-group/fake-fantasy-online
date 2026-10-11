using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
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
    public static int Main()
    {
        var api = new PluginGameApi(() => null);
        Check(api.GetQueries().Count == 17, "all supported domains are discoverable");
        var names = new HashSet<string>();
        foreach (var definition in api.GetQueries()) Check(names.Add(definition.Domain), "domains are unique");
        var schemas = new Dictionary<string, GameQueryDefinition>();
        foreach (var definition in api.GetQueries()) schemas.Add(definition.Domain, definition);
        foreach (string domain in new[] { "endgame", "endgame.items", "endgame.catalog", "endgame.rift" })
        {
            Check(names.Contains(domain), "endgame domain discoverable: " + domain);
            var queryArgs = domain == "endgame.catalog" ? Args("catalog", "recipes") : Args();
            GameSnapshot offline = api.Observe(new GameQuery(domain, queryArgs));
            Check(!(bool)offline.Data["available"] && !(bool)offline.Data["negotiated"] && !(bool)offline.Data["inWorld"], "offline endgame state explicit: " + domain);
            Check((string)offline.Data["expectedRevision"] == "0" && (string)offline.Data["snapshotId"] == "" && (string)offline.Data["snapshotRevision"] == "0", "offline endgame revisions are empty exact strings: " + domain);
            Check(((string)offline.Data["apiInstanceId"]).Length > 0, "offline endgame exposes adapter restart identity: " + domain);
        }
        foreach (string domain in new[] { "endgame.items", "endgame.catalog", "endgame.rift" })
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
        Check(((IReadOnlyList<object>)((IReadOnlyDictionary<string, object>)catalogProperties["catalog"])["enum"]).Count == 4, "catalog discovery enumerates exact native catalogs");
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
