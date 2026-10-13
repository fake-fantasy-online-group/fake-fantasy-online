using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

// Synthetic protocol examples only: these tests need no Unity or external game resources.
class EndgameSkillBehaviorContract
{
    static int checks;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    static void Reject(Action action, string name)
    {
        try { action(); }
        catch (FormatException) { checks++; return; }
        throw new Exception(name);
    }
    static Dictionary<string, object> Area() => new Dictionary<string, object> {
        { "shape", "cone" }, { "center", "caster" }, { "radius", 50L },
        { "length", 160L }, { "width", 80L }, { "angle", 60L }
    };
    static Dictionary<string, object> Behavior(int mp = 19) => new Dictionary<string, object> {
        { "mpCost", (long)mp }, { "prepareMs", 375L }, { "cooldownMs", 900L },
        { "castDistance", 0L }, { "usable", true }, { "requiresInvisible", false },
        { "requiredWeaponType", 3L }, { "inputMode", "direction" },
        { "targetSelf", false }, { "targetEnemy", true }, { "targetTeam", false },
        { "allowsDeadTarget", false }, { "deadTargetOnly", false }, { "area", Area() },
        { "groundEffect", true }, { "pointRequired", false }, { "selfFallback", true }
    };
    static Dictionary<string, object> Contract() => new Dictionary<string, object> {
        { "name", "服务端技能身份" }, { "icon", 9001L }, { "profession", 5L },
        { "kind", "area" }, { "unlockLevel", 10L },
        { "prerequisiteSkillIds", new object[] { 10402L, 10405L } },
        { "current", Behavior() }, { "next", Behavior(23) }
    };
    static Dictionary<string, object> Rank(Dictionary<string, object> contract, string effective = "8", string next = "9")
    {
        var row = new Dictionary<string, object> {
            { "id", 10401L }, { "hardRank", effective == "0" ? 0L : 1L },
            { "grantRank", "0" }, { "softRank", "7" }, { "effectiveRank", effective },
            { "nextEffectiveRank", next }, { "canCast", effective != "0" }, { "canInvest", true }
        };
        if (contract != null) row.Add("contract", contract);
        return row;
    }
    static EndgameResponse Response(Dictionary<string, object> row, string revision = "1") => EndgameResponse.Parse(BoundedJson.Serialize(new Dictionary<string, object> {
        { "kind", "skill.ranks" }, { "ok", true }, { "revision", revision },
        { "skillRanks", new object[] { row } },
        { "result", new Dictionary<string, object> {
            { "hardCap", "20" }, { "playerCap", "100" }, { "petCap", "80" },
            { "unspentPoints", "3" }, { "formulaVersion", "5" }
        } }
    }));
    static EndgameSkillRankView View(Dictionary<string, object> contract) => Response(Rank(contract)).SkillRanks[0];
    static EndgameResponse Hello(params string[] caps) => EndgameResponse.Parse(BoundedJson.Serialize(new Dictionary<string, object> {
        { "kind", "hello" }, { "ok", true }, { "capabilities", caps }
    }));
    static void Negotiate(EndgameClientState state, params string[] caps)
    { state.Begin(new EndgameRequest("hello")); state.Apply(Hello(caps)); }

    static void IdentityAndProjectionChecks()
    {
        var rank = View(Contract()); var c = rank.Contract; var b = c.Current;
        Check(c.Name == "服务端技能身份" && c.Icon == 9001 && c.Profession == 5 && c.Kind == "area" && c.UnlockLevel == 10,
            "authoritative identity is available without local resources");
        Check(c.PrerequisiteSkillIDs.Count == 2 && c.PrerequisiteSkillIDs[1] == 10405, "exact prerequisite skill IDs");
        Check(b.MPCost == 19 && b.PrepareMilliseconds == 375 && b.CooldownMilliseconds == 900 && b.CastDistance == 0,
            "actual MP and timings and literal unbounded distance are preserved");
        Check(b.Usable && !b.RequiresInvisible && b.RequiredWeaponType == 3 && b.InputMode == "direction" && !b.TargetSelf && b.TargetEnemy && !b.TargetTeam &&
            !b.AllowsDeadTarget && !b.DeadTargetOnly && b.GroundEffect && !b.PointRequired && b.SelfFallback,
            "targeting and requirement flags are projected exactly");
        Check(b.Area.Shape == "cone" && b.Area.Center == "caster" && b.Area.Radius == 50 && b.Area.Length == 160 && b.Area.Width == 80 && b.Area.Angle == 60,
            "area geometry uses typed world units");
        Check(c.Next.MPCost == 23 && rank.EffectiveRank == "8" && rank.NextEffectiveRank == "9", "next projection remains separate from current");
        bool immutable = false;
        try { ((IList<int>)c.PrerequisiteSkillIDs)[0] = 1; } catch (NotSupportedException) { immutable = true; }
        Check(immutable, "prerequisites cannot be mutated by a consumer");
        foreach (Type type in new[] { typeof(EndgameSkillContractView), typeof(EndgameSkillBehaviorView), typeof(EndgameSkillAreaView) })
            foreach (var property in type.GetProperties()) Check(!property.CanWrite, "immutable DTO property " + property.Name);

        var unlearned = Contract(); unlearned["current"] = null;
        var row = Response(Rank(unlearned, "0", "8")).SkillRanks[0];
        Check(!row.CanCast && row.Contract.Current == null && row.Contract.Next.MPCost == 23, "unlearned current is absent without borrowing next cost");
        var unavailable = Contract(); unavailable["current"] = unavailable["next"] = null;
        row = View(unavailable);
        Check(row.CanCast && row.Contract.Current == null && row.Contract.Next == null, "engine-unrepresentable positive rank may have absent projections");
        var passive = Contract(); passive["kind"] = "passive"; ((Dictionary<string, object>)passive["current"])["usable"] = false;
        row = View(passive);
        Check(row.CanCast && !row.Contract.Current.Usable, "CanCast remains learned-rank availability independent of active effect usability");
        var maxRank = Response(Rank(Contract(), "184467440737095516170000001", "184467440737095516170000002")).SkillRanks[0];
        Check(maxRank.EffectiveRank == "184467440737095516170000001", "new contract never narrows V5 ranks");
        Reject(() => Response(Rank(Contract(), "0")), "unlearned rank cannot own current behavior");
        Reject(() => Response(Rank(Contract(), "8", "")), "next behavior requires corresponding next rank");
    }
    static void CapabilityAndFreshnessChecks()
    {
        Check(new EndgameRequest("hello").Json.Contains("endgame.skill-behavior.v1"), "hello advertises behavior capability");
        var state = new EndgameClientState();
        Negotiate(state, "endgame.v1", "endgame.skill-behavior.v1");
        Check(state.Negotiated && !state.GrowthV5 && !state.SkillBehaviorV1, "behavior capability requires growth V5");
        state.Reset();
        Negotiate(state, "endgame.growth.v5", "endgame.skill-behavior.v1");
        Check(!state.Negotiated && !state.SkillBehaviorV1, "behavior also requires endgame base capability");
        Negotiate(state, "endgame.v1", "endgame.growth.v5");
        Check(state.GrowthV5 && !state.SkillBehaviorV1 && state.Apply(Response(Rank(null))), "legacy V5 payload remains accepted");
        Check(state.SkillRank(10401).Contract == null && state.SkillContract(10401) == null && state.CanInvestSkill(10401), "legacy omission does not invent behavior or change investment");
        state.Apply(Response(Rank(Contract())));
        Check(state.SkillRank(10401).Contract != null && state.SkillContract(10401) == null, "unnegotiated extra data is not authoritative behavior");
        Negotiate(state, "endgame.v1", "endgame.growth.v5", "endgame.skill-behavior.v1");
        Check(state.SkillBehaviorV1 && state.Apply(Response(Rank(Contract()))), "negotiated behavior accepts typed ranks");
        Check(state.SkillContract(10401).Name == "服务端技能身份" && state.CurrentSkillBehavior(10401).MPCost == 19 && state.SkillContract(99999) == null,
            "current typed identity and behavior lookup does not access resources");
        state.Apply(Response(Rank(null)));
        Check(state.HasSkillRanks && state.SkillContract(10401) == null && state.CurrentSkillBehavior(10401) == null && !state.CanInvestSkill(10401),
            "negotiated missing identity stays unavailable and cannot spend points");
        var held = Rank(null); held["id"] = 10212L; held["contract"] = null;
        state.Apply(Response(held));
        Check(state.SkillRank(10212).CanInvest && state.CanInvestSkill(10212) && state.SkillContract(10212) == null,
            "10212 unresolved identity preserves existing investment eligibility");
        var currentAbsent = Contract(); currentAbsent["current"] = null;
        state.Apply(Response(Rank(currentAbsent, "0", "8")));
        Check(state.SkillContract(10401) != null && state.CurrentSkillBehavior(10401) == null && state.CanInvestSkill(10401), "unlearned identity can invest without borrowing next for casting");
        state.Apply(Response(Rank(Contract())));
        state.Apply(EndgameResponse.Parse("{\"kind\":\"skill.upgrade\",\"ok\":true,\"revision\":\"2\"}"));
        Check(state.SkillRank(10401).Contract != null && state.SkillContract(10401) == null && state.CurrentSkillBehavior(10401) == null,
            "revision change makes cached behavior unavailable");
        Check(!state.Apply(Response(Rank(Contract()))) && state.SkillContract(10401) == null, "stale rank snapshot cannot restore behavior authority");
        state.Apply(Response(Rank(Contract()), "2"));
        Check(state.CurrentSkillBehavior(10401).MPCost == 19, "matching snapshot restores typed behavior");
        state.Begin(new EndgameRequest("hello"));
        state.Apply(EndgameResponse.Parse("{\"kind\":\"hello\",\"ok\":false,\"error\":\"busy\"}"));
        Check(state.SkillBehaviorV1 && state.CurrentSkillBehavior(10401) != null, "failed hello refresh preserves previously negotiated ownership");
        state.Reset();
        Check(!state.SkillBehaviorV1 && !state.GrowthV5 && !state.HasSkillRanks && state.SkillContract(10401) == null, "disconnect clears capability and behavior");
    }
    static void RequiredFieldChecks()
    {
        foreach (string key in Contract().Keys)
        {
            var c = Contract(); c.Remove(key); Reject(() => View(c), "contract requires " + key);
        }
        foreach (string key in Behavior().Keys)
        {
            var c = Contract(); ((Dictionary<string, object>)c["current"]).Remove(key); Reject(() => View(c), "behavior requires " + key);
        }
        foreach (string key in Area().Keys)
        {
            var c = Contract(); ((Dictionary<string, object>)((Dictionary<string, object>)c["current"])["area"]).Remove(key); Reject(() => View(c), "area requires " + key);
        }
        foreach (string level in new[] { "contract", "behavior", "area" })
        {
            var c = Contract(); var b = (Dictionary<string, object>)c["current"]; var a = (Dictionary<string, object>)b["area"];
            (level == "contract" ? c : level == "behavior" ? b : a).Add("unknown", 0L);
            Reject(() => View(c), "unknown field rejected in " + level);
        }
        var noArea = Contract(); ((Dictionary<string, object>)noArea["current"])["area"] = null;
        Reject(() => View(noArea), "null area rejected");
    }
    static void EnumAndNumericChecks()
    {
        foreach (string kind in new[] { "single", "area", "passive", "special" })
        { var c = Contract(); c["kind"] = kind; Check(View(c).Contract.Kind == kind, "kind " + kind); }
        foreach (string mode in new[] { "none", "self", "entity", "point", "direction", "trapEntity" })
        { var c = Contract(); ((Dictionary<string, object>)c["current"])["inputMode"] = mode; Check(View(c).Contract.Current.InputMode == mode, "input mode " + mode); }
        foreach (string shape in new[] { "none", "rectangle", "circle", "cone" })
        { var c = Contract(); ((Dictionary<string, object>)((Dictionary<string, object>)c["current"])["area"])["shape"] = shape; Check(View(c).Contract.Current.Area.Shape == shape, "shape " + shape); }
        foreach (string center in new[] { "none", "caster", "target" })
        { var c = Contract(); ((Dictionary<string, object>)((Dictionary<string, object>)c["current"])["area"])["center"] = center; Check(View(c).Contract.Current.Area.Center == center, "center " + center); }
        foreach (string field in new[] { "kind", "inputMode", "shape", "center" })
            foreach (object bad in new object[] { "", "unknown", "SELF", 1L, null })
            {
                var c = Contract(); var b = (Dictionary<string, object>)c["current"]; var a = (Dictionary<string, object>)b["area"];
                (field == "kind" ? c : field == "inputMode" ? b : a)[field] = bad;
                Reject(() => View(c), "invalid enum " + field);
            }
        foreach (string field in new[] { "icon", "profession", "unlockLevel", "mpCost", "prepareMs", "cooldownMs", "castDistance", "requiredWeaponType", "radius", "length", "width", "angle" })
            foreach (object bad in new object[] { -1L, 2147483648L, 1.5, "1", true, null })
            {
                var c = Contract(); var b = (Dictionary<string, object>)c["current"]; var a = (Dictionary<string, object>)b["area"];
                (c.ContainsKey(field) ? c : b.ContainsKey(field) ? b : a)[field] = bad;
                Reject(() => View(c), "invalid integer " + field);
            }
        foreach (string field in new[] { "usable", "requiresInvisible", "targetSelf", "targetEnemy", "targetTeam", "allowsDeadTarget", "deadTargetOnly", "groundEffect", "pointRequired", "selfFallback" })
            foreach (object bad in new object[] { 0L, "false", null })
            { var c = Contract(); ((Dictionary<string, object>)c["current"])[field] = bad; Reject(() => View(c), "invalid boolean " + field); }
        var max = Contract(); var maximumBehavior = (Dictionary<string, object>)max["current"];
        foreach (string field in new[] { "mpCost", "prepareMs", "cooldownMs", "castDistance", "requiredWeaponType" }) maximumBehavior[field] = (long)int.MaxValue;
        Check(View(max).Contract.Current.CastDistance == int.MaxValue && View(max).Contract.Current.MPCost == int.MaxValue, "full nonnegative int32 range preserved");
        foreach (object bad in new object[] { "", " ", new string('x', 129), 1L, null })
        { var c = Contract(); c["name"] = bad; Reject(() => View(c), "invalid authoritative name"); }
        var profession = Contract(); profession["profession"] = 6L; Reject(() => View(profession), "profession out of range");
        foreach (object bad in new object[] { null, "10402", new object[] { 0L }, new object[] { -1L }, new object[] { 10402L, 10402L }, new object[] { 1.5 }, new object[] { 2147483648L } })
        { var c = Contract(); c["prerequisiteSkillIds"] = bad; Reject(() => View(c), "invalid prerequisite list"); }
        var dead = Contract(); ((Dictionary<string, object>)dead["current"])["deadTargetOnly"] = true;
        Reject(() => View(dead), "dead-only without dead-target permission rejected");
    }
    static void Main()
    {
        IdentityAndProjectionChecks(); CapabilityAndFreshnessChecks(); RequiredFieldChecks(); EnumAndNumericChecks();
        Console.WriteLine("PASS " + checks + " skill-behavior DTO/capability/freshness checks (no Unity or external resources).");
    }
}
