using System;
using System.Collections.Generic;
using System.Globalization;

namespace FakeFantasyOnline.Application
{
/// <summary>Database-owned NPC binding. Native entity IDs are resolved separately from the world.</summary>
public sealed class EndgameRiftEntranceView
{
    public int MapID { get; }
    public int NpcID { get; }
    public string Npc { get; }
    public string Script { get; }
    internal EndgameRiftEntranceView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "mapId", "npcId", "npc", "script");
        MapID = EndgameData.Int(d, "mapId"); NpcID = EndgameData.Int(d, "npcId");
        Npc = EndgameData.String(d, "npc", 128); Script = EndgameData.String(d, "script", 128);
        if (MapID <= 0 || NpcID <= 0 || string.IsNullOrWhiteSpace(Npc) || string.IsNullOrWhiteSpace(Script))
            throw new FormatException("Invalid rift entrance binding.");
    }
    public bool Matches(int mapID, string npc, string script) => MapID == mapID &&
        string.Equals(Npc, npc, StringComparison.Ordinal) && string.Equals(Script, script, StringComparison.Ordinal);
}
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
    public IReadOnlyList<string> Affixes { get; }
    internal EndgameRiftAuraView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "entityId", "class", "affixes");
        EntityID = EndgameData.ID(d, "entityId"); Class = EndgameData.Int(d, "class");
        if (!uint.TryParse(EntityID, NumberStyles.None, CultureInfo.InvariantCulture, out uint id) || id == 0 || Class < 0 || Class > 3) throw new FormatException("Invalid rift aura.");
        NativeEntityID = unchecked((int)id);
        Affixes = EndgameData.List(d, "affixes", 8, v => EndgameData.Text(v, "affixes", 64));
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
/// <summary>Server-authored danger geometry, expressed in native map pixels.</summary>
public sealed class EndgameRiftTelegraphView
{
    public string ID { get; }
    public string EntityID { get; }
    public string Ability { get; }
    public string Shape { get; }
    public int X { get; }
    public int Y { get; }
    public int EndX { get; }
    public int EndY { get; }
    public int Radius { get; }
    public int InnerRadius { get; }
    public int RemainingMS { get; }
    internal EndgameRiftTelegraphView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "id", "entityId", "ability", "shape", "x", "y", "endX", "endY", "radius", "innerRadius", "remainingMS");
        ID = EndgameData.String(d, "id", 128); EntityID = EndgameData.ID(d, "entityId");
        Ability = EndgameData.String(d, "ability", 128); Shape = EndgameData.String(d, "shape", 16);
        X = EndgameData.Int(d, "x"); Y = EndgameData.Int(d, "y"); EndX = EndgameData.Int(d, "endX"); EndY = EndgameData.Int(d, "endY");
        Radius = EndgameData.Int(d, "radius"); InnerRadius = EndgameData.Int(d, "innerRadius"); RemainingMS = EndgameData.Int(d, "remainingMS");
        if (ID.Length == 0 || !uint.TryParse(EntityID, NumberStyles.None, CultureInfo.InvariantCulture, out uint id) || id == 0 ||
            (Shape != "circle" && Shape != "ring" && Shape != "line") || Radius <= 0 || InnerRadius < 0 || InnerRadius > Radius || RemainingMS < 0)
            throw new FormatException("Invalid rift telegraph.");
    }
}
public sealed class EndgameRiftView
{
    public string RunID { get; }
    public string Revision { get; }
    public string Phase { get; }
    public string Name { get; }
    public int Tier { get; }
    public string Seed { get; }
    public int GeneratorVersion { get; }
    public int FloorIndex { get; }
    public string MapName { get; }
    public bool CanAdvance { get; }
    public bool Transitioning { get; }
    public int ExitX { get; }
    public int ExitY { get; }
    public int MaxTier { get; }
    public int UnlockedTier { get; }
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
    public IReadOnlyList<EndgameRiftTelegraphView> Telegraphs { get; }
    internal EndgameRiftView(IReadOnlyDictionary<string, object> d)
    {
        EndgameData.Keys(d, "runId", "revision", "phase", "name", "tier", "progress", "progressMax", "remainingSeconds", "bossName", "keyCount", "keyItemId", "claimable", "members", "auras", "globes", "seed", "generatorVersion", "floorIndex", "mapName", "canAdvance", "transitioning", "exitX", "exitY", "maxTier", "unlockedTier", "telegraphs");
        RunID = EndgameData.String(d, "runId", 128); Revision = EndgameData.ID(d, "revision"); Phase = EndgameData.String(d, "phase", 32); Name = EndgameData.String(d, "name");
        Seed = EndgameData.String(d, "seed", 20); GeneratorVersion = EndgameData.Int(d, "generatorVersion"); FloorIndex = EndgameData.Int(d, "floorIndex");
        MapName = EndgameData.String(d, "mapName", 256); CanAdvance = EndgameData.Bool(d, "canAdvance"); Transitioning = EndgameData.Bool(d, "transitioning");
        ExitX = EndgameData.Int(d, "exitX"); ExitY = EndgameData.Int(d, "exitY");
        MaxTier = EndgameData.Int(d, "maxTier"); UnlockedTier = EndgameData.Int(d, "unlockedTier");
        Tier = EndgameData.Int(d, "tier"); Progress = EndgameData.Int(d, "progress"); ProgressMax = EndgameData.Int(d, "progressMax"); RemainingSeconds = EndgameData.Int(d, "remainingSeconds");
        BossName = EndgameData.String(d, "bossName"); KeyCount = EndgameData.Int(d, "keyCount"); KeyItemID = EndgameData.Int(d, "keyItemId"); Claimable = EndgameData.Bool(d, "claimable");
        Members = EndgameData.List(d, "members", 5, v => new EndgameRiftMemberView(EndgameData.Object(v)));
        Auras = EndgameData.List(d, "auras", 2048, v => new EndgameRiftAuraView(EndgameData.Object(v)));
        Globes = EndgameData.List(d, "globes", 1024, v => new EndgameRiftGlobeView(EndgameData.Object(v)));
        Telegraphs = EndgameData.List(d, "telegraphs", 128, v => new EndgameRiftTelegraphView(EndgameData.Object(v)));
        if (Seed.Length > 0 && (Seed.Length > 1 && Seed[0] == '0' || !ulong.TryParse(Seed, NumberStyles.None, CultureInfo.InvariantCulture, out _))) throw new FormatException("Invalid rift seed.");
        if (GeneratorVersion < 0 || FloorIndex < 0 || MaxTier < 0 || MaxTier > 150 || UnlockedTier < 0 || UnlockedTier > 150) throw new FormatException("Invalid rift floor metadata.");
        if (Tier < 0 || Tier > 150 || Progress < 0 || ProgressMax < 0 || RemainingSeconds < 0 || KeyCount < 0) throw new FormatException("Invalid rift state.");
    }
}
}
