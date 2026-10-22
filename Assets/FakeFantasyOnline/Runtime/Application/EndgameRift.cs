using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
        CharID = EndgameData.ID(d, "charId"); Name = EndgameData.String(d, "name"); Level = EndgameData.Int(d, "level");
        Eligible = EndgameData.Bool(d, "eligible"); Claimed = EndgameData.Bool(d, "claimed");
    }
}
public sealed class EndgameRiftAuraView
{
    public string EntityID { get; }
    public int NativeEntityID { get; }
    public int Class { get; }
    public string OriginalName { get; }
    public IReadOnlyList<string> Affixes { get; }
    public IReadOnlyList<int> AbilityIDs { get; }
    public long GoldenUntilMS { get; }
    public string CloneOf { get; }
    internal EndgameRiftAuraView(IReadOnlyDictionary<string, object> d)
    {
        EntityID = EndgameData.ID(d, "entityId"); Class = EndgameData.Int(d, "class"); OriginalName = EndgameData.String(d, "originalName", 128);
        if (!uint.TryParse(EntityID, NumberStyles.None, CultureInfo.InvariantCulture, out uint id) || id == 0 || Class < 0 || Class > 3) throw new FormatException("Invalid rift aura.");
        NativeEntityID = unchecked((int)id);
        Affixes = EndgameData.List(d, "affixes", 8, v => EndgameData.Text(v, "affixes", 64));
        AbilityIDs = EndgameData.List(d, "abilityIds", 32, v => EndgameData.Integer(v, "abilityIds"));
        // Classification and optional ability metadata evolve independently. Unknown IDs
        // are retained as bounded integers; presentation only enables IDs it understands.
        GoldenUntilMS = EndgameData.Long(d, "goldenUntilMS"); CloneOf = EndgameData.String(d, "cloneOf", 20);
        var ids = new HashSet<int>();
        foreach (int ability in AbilityIDs) if (ability < 0 || !ids.Add(ability)) throw new FormatException("Invalid rift aura ability ID.");
        if (GoldenUntilMS < 0 || GoldenUntilMS > 0 && !ids.Contains(0)) throw new FormatException("Invalid rift golden-body aura.");
        if (CloneOf.Length > 0 && (!EndgameRiftHazardView.ValidEntityID(CloneOf) || CloneOf == EntityID || Class != 0 || Affixes.Count != 0 || AbilityIDs.Count != 0 || GoldenUntilMS != 0))
            throw new FormatException("Invalid rift clone aura.");
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
        ID = EndgameData.String(d, "id", 128); EntityID = EndgameData.ID(d, "entityId");
        Ability = EndgameData.String(d, "ability", 128); Shape = EndgameData.String(d, "shape", 16);
        X = EndgameData.Int(d, "x"); Y = EndgameData.Int(d, "y"); EndX = EndgameData.Int(d, "endX"); EndY = EndgameData.Int(d, "endY");
        Radius = EndgameData.Int(d, "radius"); InnerRadius = EndgameData.Int(d, "innerRadius"); RemainingMS = EndgameData.Int(d, "remainingMS");
        if (ID.Length == 0 || !uint.TryParse(EntityID, NumberStyles.None, CultureInfo.InvariantCulture, out uint id) || id == 0 ||
            (Shape != "circle" && Shape != "ring" && Shape != "line") || Radius <= 0 || InnerRadius < 0 || InnerRadius > Radius || RemainingMS < 0)
            throw new FormatException("Invalid rift telegraph.");
    }
}
/// <summary>Durable server-authored guardian arrival; snapshots never restart its five-second clock.</summary>
public sealed class EndgameRiftGuardianArrivalView
{
    public string ID { get; }
    public string Seed { get; }
    public string AnchorCharID { get; }
    public int X { get; }
    public int Y { get; }
    public long StartedAtMS { get; }
    public long DeadlineMS { get; }
    public int DurationMS { get; }
    public int RemainingMS { get; private set; }
    internal void LimitRemainingMS(int remaining) => RemainingMS = Math.Min(RemainingMS, Math.Max(0, remaining));
    internal EndgameRiftGuardianArrivalView(IReadOnlyDictionary<string, object> d)
    {
        ID = EndgameData.String(d, "id", 128); Seed = EndgameData.String(d, "seed", 20); AnchorCharID = EndgameData.ID(d, "anchorCharId");
        X = EndgameData.Int(d, "x"); Y = EndgameData.Int(d, "y");
        StartedAtMS = EndgameData.Long(d, "startedAtMS"); DeadlineMS = EndgameData.Long(d, "deadlineMS");
        DurationMS = EndgameData.Int(d, "durationMS"); RemainingMS = EndgameData.Int(d, "remainingMS");
        if (ID.Length == 0 || AnchorCharID.Length == 0 || AnchorCharID == "0" || Seed.Length == 0 ||
            (Seed.Length > 1 && Seed[0] == '0') || !ulong.TryParse(Seed, NumberStyles.None, CultureInfo.InvariantCulture, out _) ||
            DurationMS != 5000 || RemainingMS < 0 || RemainingMS > DurationMS || StartedAtMS <= 0 ||
            StartedAtMS > long.MaxValue - DurationMS || DeadlineMS != StartedAtMS + DurationMS)
            throw new FormatException("Invalid guardian arrival.");
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
    public int MapID { get; }
    public long ServerNowMS { get; }
    public IReadOnlyList<EndgameRiftHazardView> Hazards { get; }
    public IReadOnlyList<EndgameRiftImpactView> Impacts { get; }
    public bool CanAdvance { get; }
    public bool Transitioning { get; }
    public int ExitX { get; }
    public int ExitY { get; }
    public int MaxTier { get; }
    public int UnlockedTier { get; }
    public int Progress { get; }
    public int ProgressMax { get; }
    public int RemainingSeconds { get; }
    // Optional on older servers; never infer a run duration from the first received snapshot.
    public int TotalSeconds { get; }
    public string BossName { get; }
    public int KeyCount { get; }
    public int KeyItemID { get; }
    public bool Claimable { get; }
    public int PendingLoot { get; }
    public IReadOnlyList<EndgameRiftMemberView> Members { get; }
    public IReadOnlyList<EndgameRiftAuraView> Auras { get; }
    public IReadOnlyList<EndgameRiftGlobeView> Globes { get; }
    public IReadOnlyList<EndgameRiftTelegraphView> Telegraphs { get; }
    public EndgameRiftGuardianArrivalView GuardianArrival { get; }
    public int SkippedVisualEntries { get; private set; }
    internal EndgameRiftView(IReadOnlyDictionary<string, object> d)
    {
        RunID = EndgameData.String(d, "runId", 128); Revision = EndgameData.ID(d, "revision"); Phase = EndgameData.String(d, "phase", 32); Name = EndgameData.String(d, "name");
        Seed = EndgameData.String(d, "seed", 20); GeneratorVersion = EndgameData.Int(d, "generatorVersion"); FloorIndex = EndgameData.Int(d, "floorIndex");
        MapName = EndgameData.String(d, "mapName", 256); CanAdvance = EndgameData.Bool(d, "canAdvance"); Transitioning = EndgameData.Bool(d, "transitioning");
        ExitX = EndgameData.Int(d, "exitX"); ExitY = EndgameData.Int(d, "exitY");
        MaxTier = EndgameData.Int(d, "maxTier"); UnlockedTier = EndgameData.Int(d, "unlockedTier");
        Tier = EndgameData.Int(d, "tier"); Progress = EndgameData.Int(d, "progress"); ProgressMax = EndgameData.Int(d, "progressMax"); RemainingSeconds = EndgameData.Int(d, "remainingSeconds"); TotalSeconds = EndgameData.Int(d, "totalSeconds");
        BossName = EndgameData.String(d, "bossName"); KeyCount = EndgameData.Int(d, "keyCount"); KeyItemID = EndgameData.Int(d, "keyItemId"); Claimable = EndgameData.Bool(d, "claimable"); PendingLoot = EndgameData.Int(d, "pendingLoot");
        Members = EndgameData.List(d, "members", 5, v => new EndgameRiftMemberView(EndgameData.Object(v)));
        MapID = EndgameData.Int(d, "mapId"); ServerNowMS = EndgameData.Long(d, "serverNowMS");
        bool extended = d.ContainsKey("mapId") || d.ContainsKey("serverNowMS") || d.ContainsKey("hazards") || d.ContainsKey("impacts");
        if (extended && (MapID <= 0 || ServerNowMS <= 0 || RunID.Length == 0 || Revision.Length == 0 || FloorIndex < 0))
            throw new FormatException("Invalid rift hazard snapshot identity.");
        // Optional rendering rows cannot veto authoritative progress or unrelated visuals.
        // Container budgets and the shared JSON/transport limits still fail closed.
        var duplicateAuras = DuplicateVisualIDs(d, "auras", "entityId", int.MaxValue);
        int clones = 0;
        Auras = VisualRows(d, "auras", int.MaxValue, v => new EndgameRiftAuraView(EndgameData.Object(v)), aura =>
        {
            if (duplicateAuras.Contains(aura.EntityID)) return false;
            if (extended && aura.GoldenUntilMS > 0 && (aura.GoldenUntilMS <= ServerNowMS || aura.GoldenUntilMS - ServerNowMS > 5000)) return false;
            if (aura.CloneOf.Length > 0 && ++clones > 96) return false;
            return true;
        });
        Globes = VisualRows(d, "globes", 1024, v => new EndgameRiftGlobeView(EndgameData.Object(v)));
        Telegraphs = VisualRows(d, "telegraphs", 128, v => new EndgameRiftTelegraphView(EndgameData.Object(v)));
        var duplicateHazards = DuplicateVisualIDs(d, "hazards", "id", 56);
        var duplicateWalls = DuplicateWallIDs(d);
        int active = 0, death = 0;
        Hazards = VisualRows(d, "hazards", 56, v => new EndgameRiftHazardView(EndgameData.Object(v)), hazard =>
        {
            if (duplicateHazards.Contains(hazard.ID) || hazard.BornMS > ServerNowMS || hazard.ExpiresMS <= ServerNowMS ||
                hazard.RemainingMS != hazard.ExpiresMS - ServerNowMS) return false;
            foreach (var wall in hazard.Walls) if (duplicateWalls.Contains(wall.ID)) return false;
            if (hazard.Mechanism == "death_lava" ? ++death > 32 : ++active > 24) return false;
            return true;
        });
        var duplicateImpacts = DuplicateVisualIDs(d, "impacts", "id", 40);
        Impacts = VisualRows(d, "impacts", 40, v => new EndgameRiftImpactView(EndgameData.Object(v)), impact =>
            impact.AtMS <= ServerNowMS && impact.ExpiresMS > ServerNowMS && !duplicateImpacts.Contains(impact.ID));
        if (Phase == "boss_arriving")
        {
            try { GuardianArrival = EndgameData.Optional(d, "guardianArrival", v => new EndgameRiftGuardianArrivalView(EndgameData.Object(v))); }
            catch (FormatException) { SkippedVisualEntries++; }
        }
        if (Seed.Length > 0 && (Seed.Length > 1 && Seed[0] == '0' || !ulong.TryParse(Seed, NumberStyles.None, CultureInfo.InvariantCulture, out _))) throw new FormatException("Invalid rift seed.");
        if (GeneratorVersion < 0 || FloorIndex < 0 || MaxTier < 0 || MaxTier > 150 || UnlockedTier < 0 || UnlockedTier > 150) throw new FormatException("Invalid rift floor metadata.");
        if (Tier < 0 || Tier > 150 || Progress < 0 || ProgressMax < 0 || RemainingSeconds < 0 || TotalSeconds < 0 || KeyCount < 0 || PendingLoot < 0) throw new FormatException("Invalid rift state.");
    }
    private static HashSet<string> DuplicateWallIDs(IReadOnlyDictionary<string, object> d)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicate = new HashSet<string>(StringComparer.Ordinal);
        if (d.TryGetValue("hazards", out object item) && item is IReadOnlyList<object> hazards && hazards.Count <= 56)
            foreach (object hazard in hazards)
                if (hazard is IReadOnlyDictionary<string, object> row && row.TryGetValue("walls", out object nested) && nested is IReadOnlyList<object> walls && walls.Count <= 3)
                    foreach (object wall in walls)
                        if (wall is IReadOnlyDictionary<string, object> data && data.TryGetValue("id", out object id) && id is string text && !seen.Add(text))
                            duplicate.Add(text);
        return duplicate;
    }
    private static HashSet<string> DuplicateVisualIDs(IReadOnlyDictionary<string, object> d, string key, string identity, int maximum)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var duplicate = new HashSet<string>(StringComparer.Ordinal);
        if (d.TryGetValue(key, out object item) && item is IReadOnlyList<object> values && values.Count <= maximum)
            foreach (object value in values)
                if (value is IReadOnlyDictionary<string, object> row && row.TryGetValue(identity, out object id) && id is string text && !seen.Add(text))
                    duplicate.Add(text);
        return duplicate; // Ambiguous identities never depend on first/last row order.
    }
    private IReadOnlyList<T> VisualRows<T>(IReadOnlyDictionary<string, object> d, string key, int maximum, Func<object, T> parse, Func<T, bool> valid = null)
    {
        if (!d.TryGetValue(key, out object item) || item == null) return Array.Empty<T>();
        if (!(item is IReadOnlyList<object> values) || values.Count > maximum) throw new FormatException("Invalid list: " + key);
        var rows = new List<T>(values.Count);
        foreach (object value in values)
        {
            try
            {
                T row = parse(value);
                if (valid == null || valid(row)) { rows.Add(row); continue; }
            }
            catch (FormatException) { } // Unsupported or unsafe optional row; never render it.
            SkippedVisualEntries++;
        }
        return new ReadOnlyCollection<T>(rows);
    }
}
}
