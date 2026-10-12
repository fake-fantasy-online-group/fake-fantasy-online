using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class EndgameGrowthContract
{
    static int checks;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); checks++; }
    static void Reject(Action action, string name) { try { action(); } catch (FormatException) { checks++; return; } catch (ArgumentException) { checks++; return; } throw new Exception(name); }
    static EndgameResponse Parse(string s) => EndgameResponse.Parse(s);
    static string Rank(int hard, string soft, string effective, bool cast = true, bool invest = true) => "{\"id\":10401,\"hardRank\":" + hard + ",\"grantRank\":\"0\",\"softRank\":\"" + soft + "\",\"effectiveRank\":\"" + effective + "\",\"canCast\":" + cast.ToString().ToLowerInvariant() + ",\"canInvest\":" + invest.ToString().ToLowerInvariant() + ",\"currentEffectLines\":[\"服务端当前参数 123.45%\"],\"nextEffectLines\":[\"服务端下一参数 125.45%\"],\"blockedReasons\":[\"测试阻碍\"],\"contributions\":[{\"sourceId\":\"item:1\",\"scope\":\"all\",\"levels\":7}]}";
    static string Ranks(string rank, string revision = "1", int points = 3) => "{\"kind\":\"skill.ranks\",\"ok\":true,\"revision\":\"" + revision + "\",\"skillRanks\":[" + rank + "],\"result\":{\"hardCap\":\"20\",\"playerCap\":\"100\",\"petCap\":\"80\",\"formulaVersion\":\"5\",\"unspentPoints\":\"" + points + "\"}}";
    const string Combat = "{\"magicFind\":9223372036854775807,\"physicalMitigationBP\":1200,\"elements\":[{\"element\":\"fire\",\"damageBP\":250,\"resistBP\":15000,\"pierceBP\":-25,\"mitigationBP\":500,\"absorbBP\":700}],\"equipmentAuras\":[{\"auraId\":\"jade\",\"family\":\"guard\",\"sourceEntityId\":4294967295,\"sourceUID\":\"9007199254740993\",\"rank\":7,\"assetKey\":\"jade_aura\",\"modifier\":\"physical_mitigation_bp\",\"value\":1200,\"leaseMs\":1000}],\"procReadiness\":[{\"effectId\":\"fire-proc\",\"sourceUID\":\"9223372036854775807\",\"trigger\":\"on_attack\",\"skillId\":10401,\"fixedRank\":15,\"chanceBP\":500,\"icdMs\":1500,\"remainingMs\":9223372036854775807,\"globalRemainingMs\":2147483648,\"ready\":false}]}";
    static string CombatSnapshot(string id, int chunk, int chunks, string combat = null) => "{\"kind\":\"snapshot\",\"ok\":true,\"revision\":\"1\",\"snapshotId\":\"" + id + "\",\"chunk\":" + chunk + ",\"chunks\":" + chunks + (combat == null ? "" : ",\"combatStats\":" + combat) + "}";
    static void CombatChecks()
    {
        var state = new EndgameClientState(); state.Begin(new EndgameRequest("hello"));
        state.Apply(Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\"]}"));
        state.Apply(Parse(CombatSnapshot("combat-a", 0, 2, Combat)));
        Check(state.CombatStats == null && state.SnapshotLoading, "partial inventory cannot publish combat stats");
        state.Apply(Parse(CombatSnapshot("combat-a", 1, 2)));
        var stats = state.CombatStats;
        Check(stats.MagicFind == long.MaxValue && stats.Elements[0].ResistBP == 15000 && stats.Elements[0].PierceBP == -25, "aggregate channels retain raw signed and overcap values");
        Check(stats.EquipmentAuras[0].SourceEntityID == uint.MaxValue && stats.EquipmentAuras[0].SourceUID == "9007199254740993", "aura source identity remains exact");
        Check(stats.ProcReadiness[0].RemainingMS == long.MaxValue && stats.ProcReadiness[0].GlobalRemainingMS == 2147483648L && !stats.ProcReadiness[0].Ready, "proc readiness keeps Int64 timing and server boolean");
        long before = state.CombatStatsVersion;
        state.Apply(Parse(CombatSnapshot("combat-b", 0, 2, Combat)));
        string pushed = Combat.Replace("\"magicFind\":9223372036854775807", "\"magicFind\":31");
        state.Apply(Parse("{\"kind\":\"combat.stats\",\"ok\":true,\"revision\":\"1\",\"combatStats\":" + pushed + "}"));
        Check(state.CombatStats.MagicFind == 31 && state.CombatStatsVersion > before, "same inventory revision can publish changed runtime stats");
        state.Apply(Parse(CombatSnapshot("combat-b", 1, 2)));
        Check(state.CombatStats.MagicFind == 31, "older staged inventory view cannot overwrite newer runtime push");
        Check(!state.Apply(Parse("{\"kind\":\"combat.stats\",\"ok\":true,\"revision\":\"0\",\"combatStats\":" + Combat + "}")) && state.CombatStats.MagicFind == 31, "stale runtime push rejected");
        Reject(() => state.Apply(Parse("{\"kind\":\"combat.stats\",\"ok\":true,\"revision\":\"1\"}")), "combat update requires typed body");
        Reject(() => Parse(CombatSnapshot("invalid", 0, 1, Combat.Replace("\"remainingMs\":9223372036854775807", "\"remainingMs\":-1"))), "negative proc remaining interval rejected");
        state.Apply(Parse(CombatSnapshot("legacy", 0, 1)));
        Check(state.CombatStats == null, "complete snapshot with no combat stats clears old authority");
        state.Reset(); Check(state.CombatStats == null && state.CombatStatsRevision == "0", "disconnect clears combat observation");
    }
    static void Main()
    {
        CombatChecks();
        var state = new EndgameClientState();
        Check(EndgameSkillRankView.IsProfessionSkill(10001) && EndgameSkillRankView.IsProfessionSkill(10435), "frozen profession membership includes edge skills");
        Check(!EndgameSkillRankView.IsProfessionSkill(13601) && !EndgameSkillRankView.IsProfessionSkill(13602) && !EndgameSkillRankView.IsProfessionSkill(11005) && !EndgameSkillRankView.IsProfessionSkill(10403), "starter life and undefined IDs retain native paths");
        Check(new EndgameRequest("hello").Json.Contains("endgame.growth.v5"), "hello advertises V5");
        state.Begin(new EndgameRequest("hello"));
        state.Apply(Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\"]}"));
        Check(state.GrowthV5 && !state.HasSkillRanks, "capability alone does not invent ranks");
        foreach (string rank in new[] { "20", "21", "99", "100", "101", "255", "256", "65535", "65536", "184467440737095516170000001" })
        { Check(state.Apply(Parse(Ranks(Rank(19, "7", rank)))) && state.SkillRank(10401).EffectiveRank == rank, "exact rank " + rank); }
        var contribution = Parse(Ranks(Rank(19, "7", "26")).Replace("\"levels\":7", "\"levels\":9223372036854775807")).SkillRanks[0].Contributions[0];
        Check(contribution.Levels == long.MaxValue, "contribution Int64 never narrows to floating point");
        Reject(() => Parse(Ranks(Rank(19, "7", "26")).Replace("\"levels\":7", "\"levels\":1.5")), "reject fractional contribution");
        Reject(() => state.Apply(Parse(Ranks(Rank(1, "0", "1"), "2", 100))), "reject invalid points without publishing partial rank");
        Check(state.SkillRanksRevision == "1" && state.SkillRank(10401).HardRank == 19, "malformed growth snapshot is atomic");
        Check(state.UnspentSkillPoints == 3 && state.SkillHardCap == 20 && state.PlayerLevelCap == 100 && state.PetLevelCap == 80, "authoritative limits and points");
        Check(state.CanInvestSkill(10401) && !state.CanInvestSkill(10402), "server eligibility only");
        foreach (string invalid in new[] { "", "01", "-1", "+1", "1.5", "1e9", " 1" }) Reject(() => Parse(Ranks(Rank(19, "7", invalid))), "reject noncanonical rank " + invalid);
        Reject(() => Parse(Ranks(Rank(21, "7", "28"))), "reject hard rank 21");
        Reject(() => Parse(Ranks(Rank(20, "7", "27", true, true))), "reject invest at hard cap");
        Reject(() => Parse(Ranks(Rank(19, "7", "26")).Replace("\"effectiveRank\":\"26\"", "\"effectiveRank\":26")), "rank must be text");
        Reject(() => Parse(Ranks(Rank(19, "7", "26") + "," + Rank(19, "7", "26"))), "duplicate rank rejected");
        state.Apply(Parse(Ranks(Rank(20, "7", "27", true, false), "2")));
        Check(!state.CanInvestSkill(10401) && state.SkillRank(10401).EffectiveRank == "27", "hard20 disables plus but preserves soft27");
        Check(!state.Apply(Parse(Ranks(Rank(1, "0", "1"), "1"))) && state.SkillRank(10401).HardRank == 20, "stale rank snapshot cannot roll back");
        state.Apply(Parse(Ranks(Rank(0, "7", "0", false, true), "3")));
        Check(!state.SkillRank(10401).CanCast && state.CanInvestSkill(10401), "soft levels do not grant casting");
        EndgamePanel.GrowthState = state;
        string tip = Win05.TestGrowthTip(10401);
        Check(tip.Contains("投入 0/20") && tip.Contains("装备与状态 +7") && tip.Contains("有效等级: 0"), "native tooltip exact hard soft effective display");
        Check(tip.Contains("服务端当前参数 123.45%") && tip.Contains("服务端下一参数 125.45%") && !tip.Contains("旧五级"), "native tooltip uses only authoritative effect lines");
        Check(tip.Contains("需先投入1点") && tip.Contains("剩余技能点 3"), "native eligibility and point explanation");
        var request = new EndgameRequest("skill.upgrade", "3", skillID: 10401);
        Check(request.Mutates && request.Json.Contains("\"skillId\":10401") && request.Json.Contains("\"expectedRevision\":\"3\""), "idempotent skill command fields");
        Reject(() => new EndgameRequest("skill.upgrade", skillID: 10401), "revision required");
        Reject(() => new EndgameRequest("skill.upgrade", "3"), "skill ID required");
        Check(state.Begin(request) && !state.CanInvestSkill(10401) && !state.Begin(new EndgameRequest("skill.upgrade", "3", skillID: 10401)), "pending point blocks repeat mutation");
        state.Apply(Parse("{\"kind\":\"skill.upgrade\",\"requestId\":\"" + request.RequestID + "\",\"ok\":true,\"revision\":\"4\"}"));
        Check(!state.Pending && state.UnspentSkillPoints == 3 && !state.CanInvestSkill(10401), "receipt cannot invent point count or allow stale rank");
        state.Apply(Parse(Ranks(Rank(1, "7", "8"), "4", 2)));
        Check(state.CanInvestSkill(10401) && state.UnspentSkillPoints == 2, "new rank push resumes investment");
        state.Reset(); Check(!state.GrowthV5 && !state.HasSkillRanks && state.SkillRanks.Count == 0, "reset clears V5 data");
        state.Begin(new EndgameRequest("hello")); state.Apply(Parse("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"));
        Check(!state.Begin(new EndgameRequest("skill.ranks")) && !state.Begin(request), "legacy server cannot receive growth commands");
        Console.WriteLine("PASS " + checks + " V5 growth/combat codec/state/native-tooltip checks (minimal presentation substitutes).");
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class EndgamePanel { public static EndgameClientState GrowthState; }
    public static class CharData { public static int skillPoints; }
    public partial class Win05
    {
        public class SkInfo { public int id; public string name; }
        private static bool SkillUsableForHotbar(SkInfo info) => true;
        private static string ComposeTip(List<(string, char, bool)> rows) { var lines = new List<string>(); foreach (var row in rows) lines.Add(row.Item1); return string.Join("\n", lines); }
        public static string TestGrowthTip(int id) => BuildGrowthSkillTip(new SkInfo { id = id, name = "火弹术" }, true);
    }
}
