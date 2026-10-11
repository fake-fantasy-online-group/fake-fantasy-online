using System;
using System.Collections.Generic;
using System.Globalization;

namespace FakeFantasyOnline.Application
{
public sealed class EndgameRiftMemberView
{
    public string CharID { get; }
    public string Name { get; }
    public int Level { get; }
    public bool Eligible { get; }
    public bool Claimed { get; }
    internal EndgameRiftMemberView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "charId", "name", "level", "eligible", "claimed");
        CharID = EndgameData.ID(d, "charId"); Name = EndgameData.String(d, "name"); Level = EndgameData.Int(d, "level");
        Eligible = EndgameData.Bool(d, "eligible"); Claimed = EndgameData.Bool(d, "claimed");
    }
}
public sealed class EndgameRiftAuraView
{
    public string EntityID { get; }
    public int NativeEntityID { get; }
    public int Class { get; }
    internal EndgameRiftAuraView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "entityId", "class");
        EntityID = EndgameData.ID(d, "entityId"); Class = EndgameData.Int(d, "class");
        if (!uint.TryParse(EntityID, NumberStyles.None, CultureInfo.InvariantCulture, out uint id) || id == 0 || Class < 0 || Class > 3) throw new FormatException("Invalid rift aura.");
        NativeEntityID = unchecked((int)id);
    }
}
public sealed class EndgameRiftGlobeView
{
    public string EntityID { get; }
    public int X { get; }
    public int Y { get; }
    public int Value { get; }
    internal EndgameRiftGlobeView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "entityId", "x", "y", "value");
        EntityID = EndgameData.ID(d, "entityId"); X = EndgameData.Int(d, "x"); Y = EndgameData.Int(d, "y"); Value = EndgameData.Int(d, "value");
        if (!uint.TryParse(EntityID, NumberStyles.None, CultureInfo.InvariantCulture, out uint id) || id == 0 || Value < 0) throw new FormatException("Invalid rift globe.");
    }
}
public sealed class EndgameRiftView
{
    public string RunID { get; }
    public string Revision { get; }
    public string Phase { get; }
    public string Name { get; }
    public int Tier { get; }
    public int Progress { get; }
    public int ProgressMax { get; }
    public int RemainingSeconds { get; }
    public string BossName { get; }
    public int KeyCount { get; }
    public int KeyItemID { get; }
    public bool Claimable { get; }
    public IReadOnlyList<EndgameRiftMemberView> Members { get; }
    public IReadOnlyList<EndgameRiftAuraView> Auras { get; }
    public IReadOnlyList<EndgameRiftGlobeView> Globes { get; }
    internal EndgameRiftView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "runId", "revision", "phase", "name", "tier", "progress", "progressMax", "remainingSeconds", "bossName", "keyCount", "keyItemId", "claimable", "members", "auras", "globes");
        RunID = EndgameData.String(d, "runId", 128); Revision = EndgameData.ID(d, "revision"); Phase = EndgameData.String(d, "phase", 32); Name = EndgameData.String(d, "name");
        Tier = EndgameData.Int(d, "tier"); Progress = EndgameData.Int(d, "progress"); ProgressMax = EndgameData.Int(d, "progressMax"); RemainingSeconds = EndgameData.Int(d, "remainingSeconds");
        BossName = EndgameData.String(d, "bossName"); KeyCount = EndgameData.Int(d, "keyCount"); KeyItemID = EndgameData.Int(d, "keyItemId"); Claimable = EndgameData.Bool(d, "claimable");
        Members = EndgameData.List(d, "members", 5, v => new EndgameRiftMemberView(EndgameData.Object(v)));
        Auras = EndgameData.List(d, "auras", 2048, v => new EndgameRiftAuraView(EndgameData.Object(v)));
        Globes = EndgameData.List(d, "globes", 1024, v => new EndgameRiftGlobeView(EndgameData.Object(v)));
        if (Tier < 0 || Tier > 50 || Progress < 0 || ProgressMax < 0 || RemainingSeconds < 0 || KeyCount < 0) throw new FormatException("Invalid rift state.");
    }
}
}
