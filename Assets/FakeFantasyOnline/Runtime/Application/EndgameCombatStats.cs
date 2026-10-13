using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Server equipment aggregation and readiness observed with a complete inventory snapshot.</summary>
public sealed class EndgameCombatStatsView
{
    public long MagicFind { get; }
    public int PhysicalMitigationBP { get; }
    public IReadOnlyList<EndgameElementCombatView> Elements { get; }
    public IReadOnlyList<EndgameEquipmentAuraView> EquipmentAuras { get; }
    public IReadOnlyList<EndgameProcReadinessView> ProcReadiness { get; }
    internal EndgameCombatStatsView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "magicFind", "elements", "physicalMitigationBP", "equipmentAuras", "procReadiness");
        MagicFind = EndgameData.Long(d, "magicFind"); PhysicalMitigationBP = EndgameData.Int(d, "physicalMitigationBP");
        Elements = EndgameData.List(d, "elements", 4, v => new EndgameElementCombatView(EndgameData.Object(v)));
        EquipmentAuras = EndgameData.List(d, "equipmentAuras", 128, v => new EndgameEquipmentAuraView(EndgameData.Object(v)));
        ProcReadiness = EndgameData.List(d, "procReadiness", 256, v => new EndgameProcReadinessView(EndgameData.Object(v)));
        var elements = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in Elements) if (!elements.Add(element.Element)) throw new FormatException("Duplicate combat element.");
    }
}
public sealed class EndgameElementCombatView
{
    public string Element { get; }
    public int DamageBP { get; }
    public int ResistBP { get; }
    public int PierceBP { get; }
    public int MitigationBP { get; }
    public int AbsorbBP { get; }
    internal EndgameElementCombatView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "element", "damageBP", "resistBP", "pierceBP", "mitigationBP", "absorbBP");
        Element = EndgameData.String(d, "element");
        DamageBP = EndgameData.Int(d, "damageBP");
        ResistBP = EndgameData.Int(d, "resistBP");
        PierceBP = EndgameData.Int(d, "pierceBP");
        MitigationBP = EndgameData.Int(d, "mitigationBP");
        AbsorbBP = EndgameData.Int(d, "absorbBP");
    }
}
public sealed class EndgameEquipmentAuraView
{
    public string AuraID { get; }
    public string Family { get; }
    public uint SourceEntityID { get; }
    public string SourceUID { get; }
    public int Rank { get; }
    public string AssetKey { get; }
    public string Modifier { get; }
    public string Element { get; }
    public int Value { get; }
    public int LeaseMS { get; }
    internal EndgameEquipmentAuraView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "auraId", "family", "sourceEntityId", "sourceUID", "rank", "assetKey", "modifier", "element", "value", "leaseMs");
        AuraID = EndgameData.String(d, "auraId");
        Family = EndgameData.String(d, "family");
        SourceEntityID = EndgameData.UInt(d, "sourceEntityId");
        SourceUID = EndgameData.ID(d, "sourceUID");
        Rank = EndgameData.Int(d, "rank");
        AssetKey = EndgameData.String(d, "assetKey");
        Modifier = EndgameData.String(d, "modifier");
        Element = EndgameData.String(d, "element");
        Value = EndgameData.Int(d, "value");
        LeaseMS = EndgameData.Int(d, "leaseMs");
        if (LeaseMS < 0) throw new FormatException("Invalid equipment aura lease.");
    }
}
public sealed class EndgameProcReadinessView
{
    public string EffectID { get; }
    public string SourceUID { get; }
    public string Trigger { get; }
    public int SkillID { get; }
    public int FixedRank { get; }
    public int ChanceBP { get; }
    public int ICDMS { get; }
    public long RemainingMS { get; }
    public long GlobalRemainingMS { get; }
    public bool Ready { get; }
    internal EndgameProcReadinessView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "effectId", "sourceUID", "trigger", "skillId", "fixedRank", "chanceBP", "icdMs", "remainingMs", "globalRemainingMs", "ready");
        EffectID = EndgameData.String(d, "effectId");
        SourceUID = EndgameData.ID(d, "sourceUID");
        Trigger = EndgameData.String(d, "trigger");
        SkillID = EndgameData.Int(d, "skillId");
        FixedRank = EndgameData.Int(d, "fixedRank");
        ChanceBP = EndgameData.Int(d, "chanceBP");
        ICDMS = EndgameData.Int(d, "icdMs");
        RemainingMS = EndgameData.Long(d, "remainingMs");
        GlobalRemainingMS = EndgameData.Long(d, "globalRemainingMs");
        Ready = EndgameData.Bool(d, "ready");
        if (ICDMS < 0 || RemainingMS < 0 || GlobalRemainingMS < 0) throw new FormatException("Invalid proc readiness interval.");
    }
}
}
