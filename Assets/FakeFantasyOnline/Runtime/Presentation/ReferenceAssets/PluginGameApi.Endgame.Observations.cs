using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private Dictionary<string, object> ObserveEndgame(string domain, ObservationArguments args)
    {
        IEndgameClient owner = EndgameClient;
        EndgameClientState state = owner?.Endgame;
        var data = Values("available", owner != null && owner.EndgameInWorld && state.Negotiated,
            "apiInstanceId", apiInstanceId, "inWorld", owner != null && owner.EndgameInWorld,
            "growthV5", state != null && state.GrowthV5, "negotiated", state != null && state.Negotiated, "generation", state?.Generation ?? 0,
            "expectedRevision", state?.Revision ?? "0", "snapshotRevision", state?.SnapshotRevision ?? "0", "hasSnapshot", state != null && state.HasSnapshot,
            "snapshotLoading", state != null && state.SnapshotLoading, "snapshotId", state?.SnapshotID ?? "",
            "catalogVersion", state?.CatalogVersion ?? "", "error", state?.Error ?? "",
            "message", BoundText(state?.Message ?? "", 2048));
        if (state == null || !owner.EndgameInWorld || !state.Negotiated)
        { data["reason"] = "The active native world has not negotiated endgame.v1."; return data; }
        switch (domain)
        {
            case "endgame":
                data["combatStats"] = ObserveEndgameCombatStats(state.CombatStats);
                data["combatStatsVersion"] = state.CombatStatsVersion; data["combatStatsRevision"] = state.CombatStatsRevision;
                data["combatStatsReceivedUtc"] = state.CombatStatsReceivedUtc.ToString("o", CultureInfo.InvariantCulture);
                data["pending"] = state.Pending;
                data["pendingRequest"] = state.PendingRequest == null ? null : Values("requestId", state.PendingRequest.RequestID, "op", state.PendingRequest.Op,
                    "arguments", BoundedJson.Parse(state.PendingRequest.Json), "sinceUtc", state.PendingSinceUtc.ToString("o", CultureInfo.InvariantCulture));
                data["quotePending"] = state.QuotePending; data["quoteRequestId"] = state.QuoteRequestID;
                data["quote"] = ObserveEndgameQuote(state.Quote);
                data["materials"] = EndgameRows(state.Materials, ObserveEndgameMaterial);
                data["activeFormations"] = EndgameRows(state.ActiveFormations, ObserveEndgameFormation);
                data["rift"] = ObserveEndgameRift(state, args, false);
                data["itemCount"] = state.Items.Count;
                var catalogs = new Dictionary<string, object>();
                foreach (string name in new[] { "uniques", "runes", "formations", "recipes" }) catalogs[name] = EndgameCatalogStatus(state, name);
                data["catalogs"] = catalogs;
                break;
            case "endgame.items":
                var items = new List<object>(); int matched = 0;
                foreach (var item in state.Items)
                {
                    if (args.UID != null && args.UID != item.UID || args.Location != null && args.Location != item.Location ||
                        args.Tab.HasValue && args.Tab.Value != item.Tab || args.ItemId.HasValue && args.ItemId.Value != item.ItemID || !MatchesName(args.Name, item.Name)) continue;
                    if (matched++ < args.Offset || items.Count >= args.Limit) continue;
                    items.Add(ObserveEndgameItem(item));
                }
                EndgamePage(data, "items", items, matched, args); break;
            case "endgame.catalog":
                string catalog = EndgameCatalogName(args.Catalog);
                foreach (var pair in EndgameCatalogStatus(state, catalog)) data[pair.Key] = pair.Value;
                data["catalog"] = catalog;
                var rows = new List<object>(); int total = 0;
                if (catalog == "uniques") { total = state.Uniques.Count; EndgamePageRows(rows, state.Uniques, args, ObserveEndgameItem); }
                if (catalog == "runes") { total = state.Runes.Count; EndgamePageRows(rows, state.Runes, args, ObserveEndgameRune); }
                if (catalog == "formations") { total = state.Formations.Count; EndgamePageRows(rows, state.Formations, args, ObserveEndgameFormation); }
                if (catalog == "recipes") { total = state.Recipes.Count; EndgamePageRows(rows, state.Recipes, args, ObserveEndgameRecipe); }
                EndgamePage(data, "rows", rows, total, args); break;
            case "endgame.rift": data["rift"] = ObserveEndgameRift(state, args, true); break;
            case "endgame.skills":
                data["available"] = state.GrowthV5 && state.HasSkillRanks;
                data["skillRanksRevision"] = state.SkillRanksRevision; data["formulaVersion"] = state.SkillFormulaVersion;
                data["hardCap"] = state.SkillHardCap; data["playerCap"] = state.PlayerLevelCap; data["petCap"] = state.PetLevelCap; data["unspentPoints"] = state.UnspentSkillPoints;
                var ranks = new List<object>(); int count = 0;
                foreach (var rank in state.SkillRanks)
                {
                    if (args.SkillId.HasValue && args.SkillId.Value != rank.ID) continue;
                    if (count++ < args.Offset || ranks.Count >= args.Limit) continue;
                    ranks.Add(ObserveEndgameSkill(rank));
                }
                EndgamePage(data, "skillRanks", ranks, count, args); break;
        }
        return data;
    }
    private static Dictionary<string, object> ObserveEndgameCombatStats(EndgameCombatStatsView row) => row == null ? null : Values(
        "magicFind", row.MagicFind, "physicalMitigationBP", row.PhysicalMitigationBP,
        "elements", EndgameRows(row.Elements, e => Values("element", e.Element, "damageBP", e.DamageBP, "resistBP", e.ResistBP, "pierceBP", e.PierceBP, "mitigationBP", e.MitigationBP, "absorbBP", e.AbsorbBP)),
        "equipmentAuras", EndgameRows(row.EquipmentAuras, a => Values("auraId", a.AuraID, "family", a.Family, "sourceEntityId", a.SourceEntityID, "sourceUID", a.SourceUID, "rank", a.Rank, "assetKey", a.AssetKey, "modifier", a.Modifier, "element", a.Element, "value", a.Value, "leaseMs", a.LeaseMS)),
        "procReadiness", EndgameRows(row.ProcReadiness, p => Values("effectId", p.EffectID, "sourceUID", p.SourceUID, "trigger", p.Trigger, "skillId", p.SkillID, "fixedRank", p.FixedRank, "chanceBP", p.ChanceBP, "icdMs", p.ICDMS, "remainingMs", p.RemainingMS, "globalRemainingMs", p.GlobalRemainingMS, "ready", p.Ready)));
    private static Dictionary<string, object> ObserveEndgameSkill(EndgameSkillRankView row) => Values(
        "skillId", row.ID, "hardRank", row.HardRank, "grantRank", row.GrantRank, "softRank", row.SoftRank, "effectiveRank", row.EffectiveRank,
        "canCast", row.CanCast, "canInvest", row.CanInvest, "blockedReasons", new List<string>(row.BlockedReasons),
        "currentEffectLines", new List<string>(row.CurrentEffectLines), "nextEffectLines", new List<string>(row.NextEffectLines),
        "nextChangeLines", new List<string>(row.NextChangeLines), "nextEffectiveRank", row.NextEffectiveRank, "nextRequiredLevel", row.NextRequiredLevel, "nativeModifierLines", new List<string>(row.NativeModifierLines),
        "trainingSynergies", EndgameRows(row.TrainingSynergies, synergy => Values("sourceId", synergy.SourceID, "sourceName", synergy.SourceName,
            "effect", synergy.Effect, "effectLabel", synergy.EffectLabel, "perPointValue", synergy.PerPointValue, "unit", synergy.Unit,
            "investedHardPoints", synergy.InvestedHardPoints, "contributionValue", synergy.ContributionValue, "equipmentApplies", synergy.EquipmentApplies)),
        "contributions", EndgameRows(row.Contributions, c => Values("sourceId", c.SourceID, "scope", c.Scope, "profession", c.Profession, "branch", c.Branch, "school", c.School, "skillId", c.SkillID, "levels", c.Levels.ToString(CultureInfo.InvariantCulture))));
    private static Dictionary<string, object> EndgameCatalogStatus(EndgameClientState state, string name) => Values(
        "loaded", state.CatalogLoaded(name), "requested", state.CatalogRequested(name), "hasMore", state.CatalogHasMore(name), "nextCursor", state.NextCatalogCursor(name));
    private static void EndgamePage(Dictionary<string, object> data, string key, List<object> rows, int total, ObservationArguments args)
    {
        data[key] = rows; data["offset"] = args.Offset; data["limit"] = args.Limit; data["count"] = rows.Count; data["total"] = total;
        data["truncated"] = args.Offset + rows.Count < total; data["nextOffset"] = args.Offset + rows.Count < total ? (object)(args.Offset + rows.Count) : null;
    }
    private static void EndgamePageRows<T>(List<object> rows, IReadOnlyList<T> source, ObservationArguments args, Func<T, Dictionary<string, object>> copy)
    { for (int i = args.Offset; i < source.Count && rows.Count < args.Limit; i++) rows.Add(copy(source[i])); }
    private static List<object> EndgameRows<T>(IReadOnlyList<T> source, Func<T, Dictionary<string, object>> copy)
    { var rows = new List<object>(); foreach (var row in source) rows.Add(copy(row)); return rows; }
    private static Dictionary<string, object> ObserveEndgameStat(EndgameStatView row) => Values("attr", row.Attr, "value", row.Value, "mode", row.Mode);
    private Dictionary<string, object> ObserveEndgameSocket(EndgameSocketView row) => Values("index", row.Index, "itemId", row.ItemID, "uid", row.UID, "name", BoundText(row.Name));
    private Dictionary<string, object> ObserveEndgameItem(EndgameItemView row) => Values(
        "uid", row.UID, "itemId", row.ItemID, "location", row.Location, "tab", row.Tab, "slot", row.Slot, "revision", row.Revision,
        "uniqueId", row.UniqueID, "uniqueRollBp", row.UniqueRollBP, "name", BoundText(row.Name), "description", BoundText(row.Description, 4096),
        "trait", BoundText(row.Trait, 4096), "level", row.Level, "profession", row.Profession, "socketCount", row.SocketCount,
        "sockets", EndgameRows(row.Sockets, ObserveEndgameSocket), "stats", EndgameRows(row.Stats, ObserveEndgameStat),
        "formationId", row.FormationID, "active", row.Active, "inactiveReason", BoundText(row.InactiveReason), "bound", row.Bound, "locked", row.Locked);
    private Dictionary<string, object> ObserveEndgameRune(EndgameRuneView row) => Values("itemId", row.ItemID, "name", BoundText(row.Name), "description", BoundText(row.Description, 4096),
        "minLevel", row.MinLevel, "quadrant", BoundText(row.Quadrant), "mansion", BoundText(row.Mansion), "stats", EndgameRows(row.Stats, ObserveEndgameStat));
    private Dictionary<string, object> ObserveEndgameFormation(EndgameFormationView row) => Values("id", row.ID, "name", BoundText(row.Name), "description", BoundText(row.Description, 4096),
        "channel", BoundText(row.Channel), "runes", CopyNumbers(row.Runes, 5), "stats", EndgameRows(row.Stats, ObserveEndgameStat), "minLevel", row.MinLevel,
        "professions", row.Professions, "slots", CopyNumbers(row.Slots, 32), "active", row.Active, "inactiveReason", BoundText(row.InactiveReason));
    private Dictionary<string, object> ObserveEndgameMaterial(EndgameMaterialView row) => Values("itemId", row.ItemID, "name", BoundText(row.Name), "count", row.Count, "owned", row.Owned);
    private Dictionary<string, object> ObserveEndgameRecipe(EndgameRecipeView row) => Values("id", row.ID, "name", BoundText(row.Name), "description", BoundText(row.Description, 4096),
        "operation", row.Operation, "money", row.Money, "materials", EndgameRows(row.Materials, ObserveEndgameMaterial), "outputItemId", row.OutputItemID, "targetRequired", row.TargetRequired);
    private Dictionary<string, object> ObserveEndgameQuote(EndgameQuoteView row) => row == null ? null : Values("quoteId", row.QuoteID, "recipeId", row.RecipeID, "targetUid", row.TargetUID,
        "revision", row.Revision, "money", row.Money, "materials", EndgameRows(row.Materials, ObserveEndgameMaterial), "expiresAt", row.ExpiresAt, "description", BoundText(row.Description, 8192));
    private Dictionary<string, object> ObserveEndgameRift(EndgameClientState state, ObservationArguments args, bool includeMarkers)
    {
        EndgameRiftView row = state.Rift; if (row == null) return null;
        var data = Values("runId", row.RunID, "revision", row.Revision, "phase", row.Phase, "name", BoundText(row.Name), "tier", row.Tier,
            "seed", row.Seed, "generatorVersion", row.GeneratorVersion, "floorIndex", row.FloorIndex, "mapName", BoundText(row.MapName),
            "canAdvance", row.CanAdvance, "transitioning", row.Transitioning, "exitX", row.ExitX, "exitY", row.ExitY, "maxTier", row.MaxTier, "unlockedTier", row.UnlockedTier,
            "progress", row.Progress, "progressMax", row.ProgressMax, "remainingSeconds", row.RemainingSeconds,
            "receivedUtc", state.RiftReceivedUtc.ToString("o", CultureInfo.InvariantCulture), "bossName", BoundText(row.BossName),
            "keyCount", row.KeyCount, "keyItemId", row.KeyItemID, "claimable", row.Claimable,
            "members", EndgameRows(row.Members, m => Values("charId", m.CharID, "name", BoundText(m.Name), "level", m.Level, "eligible", m.Eligible, "claimed", m.Claimed)));
        if (!includeMarkers) return data;
        var auras = new List<object>(); var globes = new List<object>(); var telegraphs = new List<object>();
        EndgamePageRows(auras, row.Auras, args, a => Values("entityId", a.EntityID, "targetEntityId", EntityReference("monster", a.NativeEntityID), "nativeEntityId", a.NativeEntityID, "class", a.Class, "affixes", new List<string>(a.Affixes)));
        EndgamePageRows(globes, row.Globes, args, g => Values("entityId", g.EntityID, "x", g.X, "y", g.Y, "value", g.Value));
        EndgamePageRows(telegraphs, row.Telegraphs, args, t => Values("id", t.ID, "entityId", t.EntityID, "ability", BoundText(t.Ability), "shape", t.Shape,
            "x", t.X, "y", t.Y, "endX", t.EndX, "endY", t.EndY, "radius", t.Radius, "innerRadius", t.InnerRadius, "remainingMS", t.RemainingMS));
        data["telegraphs"] = telegraphs; data["telegraphsTotal"] = row.Telegraphs.Count; data["telegraphsTruncated"] = args.Offset + telegraphs.Count < row.Telegraphs.Count;
        data["auras"] = auras; data["aurasTotal"] = row.Auras.Count; data["aurasTruncated"] = args.Offset + auras.Count < row.Auras.Count;
        data["globes"] = globes; data["globesTotal"] = row.Globes.Count; data["globesTruncated"] = args.Offset + globes.Count < row.Globes.Count;
        data["offset"] = args.Offset; data["limit"] = args.Limit;
        data["nextOffset"] = args.Offset + args.Limit < Math.Max(row.Telegraphs.Count, Math.Max(row.Auras.Count, row.Globes.Count)) ? (object)(args.Offset + args.Limit) : null;
        data["globePickupRadius"] = 75; data["coordinateSystem"] = CoordinateSystem();
        return data;
    }
}
}
