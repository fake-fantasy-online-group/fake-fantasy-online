using System;
using System.Collections.Generic;
using System.Globalization;

namespace FakeFantasyOnline.Application
{
/// <summary>Ranks are exact canonical decimal strings, never native byte/int levels.</summary>
public sealed class EndgameSkillRankView
{
    // Frozen V5 profession membership. Starter/life skills retain native progression.
    private static readonly HashSet<int> professionSkills = new HashSet<int> {
        10001, 10002, 10003, 10004, 10005, 10006, 10007, 10008, 10009, 10010, 10011, 10012,
        10013, 10014, 10015, 10016, 10017, 10018, 10019, 10020, 10021, 10022, 10023, 10024,
        10025, 10026, 10027, 10028, 10029, 10030, 10101, 10102, 10103, 10104, 10105, 10106,
        10107, 10108, 10109, 10110, 10111, 10112, 10113, 10114, 10115, 10116, 10117, 10118,
        10119, 10120, 10121, 10122, 10123, 10124, 10125, 10126, 10127, 10128, 10129, 10130,
        10201, 10202, 10203, 10204, 10205, 10206, 10207, 10208, 10209, 10210, 10211, 10212,
        10213, 10214, 10215, 10216, 10217, 10218, 10219, 10220, 10221, 10222, 10223, 10224,
        10225, 10226, 10227, 10228, 10229, 10230, 10301, 10302, 10303, 10304, 10305, 10306,
        10307, 10308, 10309, 10310, 10311, 10312, 10313, 10314, 10315, 10316, 10317, 10318,
        10319, 10320, 10321, 10322, 10324, 10325, 10326, 10327, 10328, 10329, 10330, 10401,
        10402, 10405, 10408, 10409, 10410, 10411, 10412, 10413, 10414, 10415, 10416, 10417,
        10418, 10419, 10420, 10421, 10422, 10423, 10424, 10425, 10426, 10427, 10428, 10429,
        10430, 10431, 10432, 10433, 10435
    };
    public static bool IsProfessionSkill(int id) => professionSkills.Contains(id);
    public int ID { get; }
    public int HardRank { get; }
    public string GrantRank { get; }
    public string SoftRank { get; }
    public string EffectiveRank { get; }
    public string NextEffectiveRank { get; }
    public int NextRequiredLevel { get; }
    public IReadOnlyList<EndgameTrainingSynergyView> TrainingSynergies { get; }
    public IReadOnlyList<string> NativeModifierLines { get; }
    public bool CanCast { get; }
    public bool CanInvest { get; }
    public IReadOnlyList<string> BlockedReasons { get; }
    public IReadOnlyList<string> CurrentEffectLines { get; }
    public IReadOnlyList<string> NextEffectLines { get; }
    public IReadOnlyList<string> NextChangeLines { get; }
    public IReadOnlyList<EndgameSkillContribution> Contributions { get; }
    public EndgameSkillContractView Contract { get; }
    internal EndgameSkillRankView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "id", "hardRank", "grantRank", "softRank", "effectiveRank", "canCast", "canInvest", "blockedReasons", "currentEffectLines", "nextEffectLines", "contributions", "nextEffectiveRank", "trainingSynergies", "nativeModifierLines", "nextRequiredLevel", "nextChangeLines", "contract");
        ID = EndgameData.Int(d, "id"); HardRank = EndgameData.Int(d, "hardRank");
        GrantRank = DecimalRank(EndgameData.String(d, "grantRank", EndgameResponse.MaximumBytes)); SoftRank = DecimalRank(EndgameData.String(d, "softRank", EndgameResponse.MaximumBytes)); EffectiveRank = DecimalRank(EndgameData.String(d, "effectiveRank", EndgameResponse.MaximumBytes));
        string nextRank = EndgameData.String(d, "nextEffectiveRank", EndgameResponse.MaximumBytes);
        NextEffectiveRank = nextRank.Length == 0 ? string.Empty : DecimalRank(nextRank);
        NextRequiredLevel = EndgameData.Int(d, "nextRequiredLevel");
        if (NextRequiredLevel < 0 || NextRequiredLevel > 100) throw new FormatException("Invalid skill investment requirement.");
        TrainingSynergies = EndgameData.List(d, "trainingSynergies", 16, v => new EndgameTrainingSynergyView(EndgameData.Object(v)));
        NativeModifierLines = EndgameData.List(d, "nativeModifierLines", 16, v => EndgameData.Text(v, "nativeModifierLines", 512));
        var synergyKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var synergy in TrainingSynergies)
            if (synergy.SourceID == ID || !synergyKeys.Add(synergy.SourceID.ToString(CultureInfo.InvariantCulture) + ":" + synergy.Effect))
                throw new FormatException("Invalid repeated/self training synergy.");
        if (NextEffectiveRank == "0") throw new FormatException("Invalid next skill rank.");
        CanCast = EndgameData.Bool(d, "canCast"); CanInvest = EndgameData.Bool(d, "canInvest");
        BlockedReasons = EndgameData.List(d, "blockedReasons", 16, v => EndgameData.Text(v, "blockedReasons", 512));
        CurrentEffectLines = EndgameData.List(d, "currentEffectLines", 32, v => EndgameData.Text(v, "currentEffectLines", 2048));
        NextEffectLines = EndgameData.List(d, "nextEffectLines", 32, v => EndgameData.Text(v, "nextEffectLines", 2048));
        NextChangeLines = EndgameData.List(d, "nextChangeLines", 32, v => EndgameData.Text(v, "nextChangeLines", 2048));
        Contributions = EndgameData.List(d, "contributions", 256, v => new EndgameSkillContribution(EndgameData.Object(v)));
        Contract = EndgameData.Optional(d, "contract", v => new EndgameSkillContractView(EndgameData.Object(v)));
        if (Contract != null && (EffectiveRank == "0" && Contract.Current != null || NextEffectiveRank.Length == 0 && Contract.Next != null))
            throw new FormatException("Skill behavior has no matching effective rank.");
        if (ID <= 0 || HardRank < 0 || HardRank > 20 || CanCast != (EffectiveRank != "0") || CanInvest && HardRank >= 20) throw new FormatException("Invalid skill rank state.");
    }
    public static string DecimalRank(string value)
    {
        // Payload length is the capacity boundary. No gameplay ceiling or numeric narrowing.
        if (string.IsNullOrEmpty(value) || value.Length > EndgameResponse.MaximumBytes || value.Length > 1 && value[0] == '0') throw new FormatException("Invalid decimal skill rank.");
        foreach (char c in value) if (c < '0' || c > '9') throw new FormatException("Invalid decimal skill rank.");
        return value;
    }
}
/// <summary>Server-defined hard-point synergy. Equipment ranks never feed this view.</summary>
public sealed class EndgameTrainingSynergyView
{
    public int SourceID { get; }
    public string SourceName { get; }
    public string Effect { get; }
    public string EffectLabel { get; }
    public int PerPointValue { get; }
    public string Unit { get; }
    public int InvestedHardPoints { get; }
    public int ContributionValue { get; }
    public bool EquipmentApplies { get; }
    internal EndgameTrainingSynergyView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "sourceId", "sourceName", "effect", "effectLabel", "perPointValue", "unit", "investedHardPoints", "contributionValue", "equipmentApplies");
        SourceID = EndgameData.Int(d, "sourceId"); SourceName = EndgameData.String(d, "sourceName", 128);
        Effect = EndgameData.String(d, "effect", 64); EffectLabel = EndgameData.String(d, "effectLabel", 128);
        PerPointValue = EndgameData.Int(d, "perPointValue"); Unit = EndgameData.String(d, "unit", 32);
        InvestedHardPoints = EndgameData.Int(d, "investedHardPoints"); ContributionValue = EndgameData.Int(d, "contributionValue");
        EquipmentApplies = EndgameData.Bool(d, "equipmentApplies");
        if (!EndgameSkillRankView.IsProfessionSkill(SourceID) || SourceName.Length == 0 || Effect.Length == 0 || EffectLabel.Length == 0 ||
            PerPointValue <= 0 || Unit != "percent_bp" && Unit != "flat" || InvestedHardPoints < 0 || InvestedHardPoints > 20 ||
            EquipmentApplies || ContributionValue < 0 || (long)PerPointValue * InvestedHardPoints != ContributionValue)
            throw new FormatException("Invalid hard-point training synergy.");
    }
}
public sealed class EndgameSkillContribution
{
    public string SourceID { get; }
    public string Scope { get; }
    public int Profession { get; }
    public string Branch { get; }
    public string School { get; }
    public int SkillID { get; }
    public long Levels { get; }
    internal EndgameSkillContribution(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "sourceId", "scope", "profession", "branch", "school", "skillId", "levels");
        SourceID = EndgameData.String(d, "sourceId", 256); Scope = EndgameData.String(d, "scope", 32);
        Profession = EndgameData.Int(d, "profession"); Branch = EndgameData.String(d, "branch", 32); School = EndgameData.String(d, "school", 32); SkillID = EndgameData.Int(d, "skillId");
        Levels = EndgameData.Long(d, "levels");
        if (Levels <= 0) throw new FormatException("Invalid skill contribution.");
    }
}
public sealed partial class EndgameClientState
{
    public bool HasSkillRanks { get; private set; }
    public string SkillRanksRevision { get; private set; } = "0";
    public int SkillHardCap { get; private set; }
    public int PlayerLevelCap { get; private set; }
    public int PetLevelCap { get; private set; }
    public int UnspentSkillPoints { get; private set; }
    public int SkillFormulaVersion { get; private set; }
    public IReadOnlyList<EndgameSkillRankView> SkillRanks { get; private set; } = Array.Empty<EndgameSkillRankView>();
    public EndgameSkillRankView SkillRank(int id)
    { foreach (var row in SkillRanks) if (row.ID == id) return row; return null; }
    /// <summary>Behavior is authoritative only for the currently observed server revision.</summary>
    public EndgameSkillContractView SkillContract(int id) => SkillBehaviorV1 && GrowthV5 && HasSkillRanks && SkillRanksRevision == Revision ? SkillRank(id)?.Contract : null;
    public EndgameSkillVariantView CurrentSkillVariant(int id) => SkillVariantsV1 ? CurrentSkillBehavior(id)?.Variant : null;
    public EndgameSkillBehaviorView CurrentSkillBehavior(int id) => SkillContract(id)?.Current;
    public bool CanInvestSkill(int id) => GrowthV5 && HasSkillRanks && SkillRanksRevision == Revision && !Pending && UnspentSkillPoints > 0 && SkillRank(id)?.CanInvest == true && (!SkillBehaviorV1 || id == 10212 || SkillRank(id).Contract != null);
    private void ResetSkillRanks()
    { HasSkillRanks = false; SkillRanksRevision = "0"; SkillHardCap = PlayerLevelCap = PetLevelCap = UnspentSkillPoints = SkillFormulaVersion = 0; SkillRanks = Array.Empty<EndgameSkillRankView>(); }
    private static int SkillResultInt(EndgameResponse response, string key)
    {
        if (!response.Result.TryGetValue(key, out string text) || !int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out int value) || value < 0 || value.ToString(CultureInfo.InvariantCulture) != text) throw new FormatException("Invalid skill result: " + key);
        return value;
    }
    private bool ApplySkillRanks(EndgameResponse response)
    {
        if (!GrowthV5 || response.Revision.Length == 0 || CompareRevision(response.Revision, Revision) < 0 || CompareRevision(response.Revision, SkillRanksRevision) < 0) return false;
        // Never activate or present a transformed plan without its negotiated contract.
        if (!SkillVariantsV1)
            foreach (var row in response.SkillRanks)
                if (row.Contract?.Current?.Variant != null || row.Contract?.Next?.Variant != null) return false;
        int hard = SkillResultInt(response, "hardCap"), player = SkillResultInt(response, "playerCap"), pet = SkillResultInt(response, "petCap"), points = SkillResultInt(response, "unspentPoints"), formula = SkillResultInt(response, "formulaVersion");
        if (hard != 20 || player != 100 || pet != 80 || points > 99 || formula != 5) throw new FormatException("Unsupported growth rules.");
        SkillRanks = response.SkillRanks; SkillRanksRevision = response.Revision; SkillHardCap = hard; PlayerLevelCap = player; PetLevelCap = pet; UnspentSkillPoints = points; SkillFormulaVersion = formula; HasSkillRanks = true;
        return true;
    }
}
}
