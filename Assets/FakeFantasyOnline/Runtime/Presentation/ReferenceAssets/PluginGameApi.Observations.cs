using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private int observationTextBudget;
    private bool observationTextTruncated;
    public IReadOnlyList<GameQueryDefinition> GetQueries()
    {
        RequireMainThread();
        return new List<GameQueryDefinition>
        {
            QueryDefinition("endgame", "Native endgame negotiation/session/revision, pending immutable request, quote, wallet and active formations."),
            QueryDefinition("endgame.items", "Server UID-bearing equipment/runes, exact locations, sockets, rolls and stats. offset pages matching rows.", "uid:string", "location:string", "tab:integer", "itemId:integer", "name:string", "offset:integer"),
            QueryDefinition("endgame.catalog", "Loaded native catalog rows and server page cursor. catalog is required: uniques, runes, formations or recipes. No network request is implicit.", "catalog:string", "offset:integer"),
            QueryDefinition("endgame.rift", "Authoritative rift phase, claim eligibility, members, combat aura targets and map-pixel globes. offset/limit page auras and globes independently.", "offset:integer"),
            QueryDefinition("world", "Connection, scene/map identity, readiness and map-pixel coordinate system."),
            QueryDefinition("player", "Local player position, authoritative vitals, attributes, pet and local movement/combat state."),
            QueryDefinition("entities", "Remote players, monsters, pets and traps from authoritative receipts; positions may predate paths. Bounded server paths are included.", "radius:number", "x:number", "y:number", "kind:string", "entityId:string", "name:string"),
            QueryDefinition("npcs", "NPC identities, positions, names and shop/transport references.", "radius:number", "x:number", "y:number", "entityId:string", "name:string"),
            QueryDefinition("loot", "Ground item identity, position, quantity, quality and ownership at receipt.", "radius:number", "x:number", "y:number", "entityId:string", "name:string", "itemId:integer"),
            QueryDefinition("inventory", "Bag items plus money, currencies, weight and native version. Use tab, slot and itemId for stale-safe item actions.", "tab:integer", "itemId:integer", "name:string"),
            QueryDefinition("equipment", "Equipped slots, item identities, durability and appearance.", "itemId:integer", "name:string"),
            QueryDefinition("skills", "Learned skills, levels, real HUD cooldown seconds and last cast receipt. Learned-skill metadata gaps are explicit.", "skillId:integer"),
            QueryDefinition("buffs", "Local buffs and durations at authoritative receipt, without consuming native queues.", "skillId:integer", "name:string"),
            QueryDefinition("target", "Current local selection, live actor position, target vitals and matching status receipt."),
            QueryDefinition("quests", "Active/completed quests and current NPC offers with expectedQuestRevision and expectedOffersRevision.", "taskId:integer"),
            QueryDefinition("dialogue", "Actual NPC dialogue choices, expectedNpcId and dialogueRevision for stale-safe choice dispatch."),
            QueryDefinition("ui", "Addon panels/widgets, named native window visibility and actual NPC dialogue choices. No complete native widget tree.")
        }.AsReadOnly();
    }

    private static GameQueryDefinition QueryDefinition(string domain, string description, params string[] fields)
    {
        var properties = Values(
            "limit", Values("type", "integer", "minimum", 1, "maximum", MaximumRows, "default", 128),
            "sinceRevision", Values("type", "integer", "minimum", 0, "description", "Previous snapshot revision. Supply sinceQueryKey to receive unchanged data."),
            "sinceQueryKey", Values("type", "string", "description", "Exact previous data.queryKey. Required alongside sinceRevision for unchanged data."),
            "expectedSceneRevision", Values("type", "integer", "minimum", 0, "description", "A mismatch produces a full replacement with resyncRequired=true."));
        foreach (string field in fields)
        {
            string[] parts = field.Split(':');
            var fieldSchema = Values("type", parts[1]);
            if (parts[0] == "offset") { fieldSchema["minimum"] = 0; fieldSchema["maximum"] = 4096; }
            if (parts[0] == "catalog") fieldSchema["enum"] = new object[] { "uniques", "runes", "formations", "recipes" };
            properties.Add(parts[0], fieldSchema);
        }
        return new GameQueryDefinition(domain, description,
            Values("type", "object", "properties", properties, "required", domain == "endgame.catalog" ? new object[] { "catalog" } : new object[0], "additionalProperties", false));
    }

    /// <summary>
    /// A full bounded replacement snapshot, or an unchanged response for the same queryKey.
    /// Query revisions are independent of event consumption. Native state stays on this thread.
    /// </summary>
    public GameSnapshot Observe(GameQuery query)
    {
        RequireMainThread();
        if (query == null) throw new ArgumentNullException(nameof(query));
        RefreshScene();
        RefreshObservationRevisions();
        string domain = (query.Domain ?? string.Empty).Trim().ToLowerInvariant();
        ValidateObservationArguments(domain, query.Arguments);
        var args = new ObservationArguments(query.Arguments);
        if (domain == "endgame.catalog" && args.Catalog == null) throw new ArgumentException("catalog is required.");
        string key = ObservationQueryKey(domain, query.Arguments);
        if (args.Radius.HasValue && !args.X.HasValue)
        {
            var center = ObservationFrame?.Player;
            // Auto-centred filters are different queries after local movement, even when
            // authoritative entity roots have not changed. Never reuse their old row set.
            key = WithObservationCenter(key, true, center?.WorldX ?? 0, center?.WorldY ?? 0);
        }
        // UI can move locally without a server revision; sample it only on demand.
        if (domain == "ui") TrackObservation(domain, Token(ObservationFrameNumber, CurrentDialogueRevision()));
        long domainRevision = observationRevisions.TryGetValue(domain, out long known) ? known : revision;
        bool resync = args.ExpectedSceneRevision.HasValue && args.ExpectedSceneRevision.Value != sceneRevision;
        if (!observationCache.TryGetValue(key, out GameSnapshot snapshot) || snapshot.Revision != domainRevision || snapshot.SceneRevision != sceneRevision)
        {
            Dictionary<string, object> data;
            observationTextBudget = 65536;
            observationTextTruncated = false;
            ClientSessionCoordinator s = Session;
            if (!IsObservationDomain(domain))
                data = Unavailable("unknown_domain", "This observation domain is not implemented.");
            else if (domain != "world" && domain != "ui" && !domain.StartsWith("endgame", StringComparison.Ordinal) && !ObservationWorldReady(s))
                data = Unavailable("not_in_world", "An active, built world is required for this observation.");
            else
            {
                switch (domain)
                {
                    case "endgame": case "endgame.items": case "endgame.catalog": case "endgame.rift": data = ObserveEndgame(domain, args); break;
                    case "world": data = ObserveWorld(s); break;
                    case "player": data = ObservePlayer(s); break;
                    case "entities": data = ObserveEntities(s, args); break;
                    case "npcs": data = ObserveNpcs(s, args); break;
                    case "loot": data = ObserveLoot(s, args); break;
                    case "inventory": data = ObserveInventory(s, args); break;
                    case "equipment": data = ObserveEquipment(s, args); break;
                    case "skills": data = ObserveSkills(s, args); break;
                    case "buffs": data = ObserveBuffs(s, args); break;
                    case "target": data = ObserveTarget(s, args); break;
                    case "quests": data = ObserveQuests(s, args); break;
                    case "dialogue": data = ObserveDialogue(args); break;
                    case "ui": data = ObserveUi(args); break;
                    default: data = Unavailable("unknown_domain", "This observation domain is not implemented."); break;
                }
            }
            data["textTruncated"] = observationTextTruncated;
            data["queryKey"] = key;
            data["unchanged"] = false;
            data["deltaMode"] = "replace";
            data["resyncRequired"] = false;
            snapshot = new GameSnapshot(domain, sceneRevision, domainRevision, data);
            if (observationCache.Count >= MaximumCachedQueries) observationCache.Clear();
            observationCache[key] = snapshot;
        }
        if (!resync && args.SinceRevision == domainRevision && args.SinceQueryKey == key)
            return new GameSnapshot(domain, sceneRevision, domainRevision,
                Values("available", snapshot.Data["available"], "unchanged", true, "queryKey", key,
                    "deltaMode", "unchanged", "resyncRequired", false));
        if (!resync) return snapshot;
        var resyncData = new Dictionary<string, object>(StringComparer.Ordinal);
        foreach (var pair in snapshot.Data) resyncData.Add(pair.Key, pair.Value);
        resyncData["resyncRequired"] = true;
        return new GameSnapshot(domain, sceneRevision, domainRevision, resyncData);
    }

    private bool ObservationWorldReady(ClientSessionCoordinator s) =>
        s != null && s.Connected && s.State == ClientSessionState.InWorld && ObservationFrame != null && ObservationFrame.Built;

    private Dictionary<string, object> ObserveWorld(ClientSessionCoordinator s)
    {
        var frame = ObservationFrame;
        bool ready = ObservationWorldReady(s);
        return Values("available", true, "apiInstanceId", apiInstanceId, "connected", s != null && s.Connected,
            "sessionState", s == null ? "Offline" : s.State.ToString(), "worldReady", ready,
            "mapId", frame == null ? 0 : GameBootstrap.CurMapId,
            "mapName", frame == null ? null : BoundText(GameBootstrap.CurMapName),
            "width", frame == null ? (object)null : frame.MapWidthForIndependentRuntime,
            "height", frame == null ? (object)null : frame.MapHeightForIndependentRuntime,
            "coordinateSystem", CoordinateSystem(),
            "nativeSceneGeneration", lastNativeSceneGeneration);
    }

    private Dictionary<string, object> ObservePlayer(ClientSessionCoordinator s)
    {
        var frame = ObservationFrame;
        var player = frame.Player;
        WorldEntryRecord entry = frame.Character;
        WorldPlayerStatusState status = s.PlayerStatusState;
        if (player == null || entry == null || status == null)
            return Unavailable("player_not_ready", "Local player or authoritative status has not been published.");
        var controller = player.SourceControllerForIndependentRuntime;
        var data = Values("available", true, "entityId", EntityReference("player", player.EntityId),
            "nativeId", player.EntityId, "name", BoundText(CharData.name), "title", BoundText(CharData.title),
            "mapId", GameBootstrap.CurMapId, "coordinateSystem", CoordinateSystem(),
            "x", player.WorldX, "y", player.WorldY, "level", status.Level,
            "hp", status.HitPoints, "maxHp", status.MaximumHitPoints,
            "mp", status.MagicPoints, "maxMp", status.MaximumMagicPoints,
            "experience", status.Experience, "experienceToNext", status.ExperienceToNext,
            "nianli", status.Nianli, "maxNianli", status.MaximumNianli,
            "naili", status.Naili, "maxNaili", status.MaximumNaili,
            "alive", s.LocalPlayerLifeState == null ? (object)!frame.LocalPlayerDead : s.LocalPlayerLifeState.Alive,
            "moving", player.MovementActive, "movementPathIndex", player.MovementPathIndex,
            "movementPathCount", player.MovementPathCount, "acting", player.ActionActive,
            "inCombat", controller == null ? (object)null : controller.InCombat,
            "sitting", controller == null ? (object)null : controller.SittingForIndependentRuntime,
            "chanting", controller == null ? (object)null : controller.ChantingForIndependentRuntime,
            "attributes", CopyNumbers(status.Attributes, MaximumRows),
            "attributeTotals", CopyNumbers(status.AttributeTotals, MaximumRows),
            "statusVersion", s.PlayerStatusStateVersion, "positionSource", "native_local_player");
        var pet = status.Pet;
        data["pet"] = pet == null ? null : Values("name", BoundText(pet.DisplayName), "model", pet.Model,
            "level", pet.Level, "hp", pet.HitPoints, "maxHp", pet.MaximumHitPoints,
            "mp", pet.MagicPoints, "maxMp", pet.MaximumMagicPoints, "show", pet.Show, "ridable", pet.Ridable);
        return data;
    }

    private Dictionary<string, object> ObserveEntities(ClientSessionCoordinator s, ObservationArguments args)
    {
        var rows = new List<object>();
        int matched = 0;
        foreach (var pair in s.RemotePlayers)
        {
            var p = pair.Value;
            if (p == null || !MatchesSpatial(args, "player", p.EntityId, p.DisplayName, p.X, p.Y)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            rows.Add(Values("entityId", EntityReference("player", p.EntityId), "nativeId", p.EntityId,
                "kind", "player", "name", BoundText(p.DisplayName), "level", p.Level,
                "x", p.X, "y", p.Y, "hp", p.HitPoints, "maxHp", p.MaximumHitPoints,
                "dead", p.Dead, "faction", p.Faction, "invisible", p.Invisible,
                "path", ObservePath(s.RemotePaths, p.EntityId)));
        }
        foreach (var pair in s.WorldEntities)
        {
            var p = pair.Value;
            if (p == null) continue;
            string kind = p.Kind.ToString().ToLowerInvariant();
            if (!MatchesSpatial(args, kind, p.EntityId, p.DisplayName, p.X, p.Y)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            rows.Add(Values("entityId", EntityReference(kind, p.EntityId), "nativeId", p.EntityId,
                "kind", kind, "name", BoundText(p.DisplayName), "level", p.Level,
                "x", p.X, "y", p.Y, "hp", p.Kind == WorldEntityKind.Trap ? (object)null : p.HitPointsOrRadiusPixels,
                "radiusPixels", p.Kind == WorldEntityKind.Trap ? (object)p.HitPointsOrRadiusPixels : null,
                "maxHp", p.MaximumHitPoints, "ownerNativeId", p.OwnerEntityId,
                "friendly", p.FriendlyFlag != 0, "eliteClass", p.EliteClass,
                "path", ObservePath(s.MonsterPaths, p.EntityId)));
        }
        var data = ListResult("entities", rows, matched, args.Limit);
        data["coordinateSystem"] = CoordinateSystem();
        data["positionSource"] = "latest_server_entity_snapshot";
        data["positionNote"] = "Spawn/status coordinates and the latest server path are authoritative receipts, not interpolated actor positions.";
        return data;
    }

    private Dictionary<string, object> ObserveNpcs(ClientSessionCoordinator s, ObservationArguments args)
    {
        var rows = new List<object>();
        int matched = 0;
        foreach (var pair in s.Npcs)
        {
            var p = pair.Value;
            if (p == null || !MatchesSpatial(args, "npc", p.EntityId, p.DisplayName, p.X, p.Y)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            rows.Add(Values("entityId", EntityReference("npc", p.EntityId), "nativeId", p.EntityId,
                "name", BoundText(p.DisplayName), "label", BoundText(p.Label), "x", p.X, "y", p.Y,
                "sellList", p.SellList, "transportList", p.TransportList, "direction", p.Direction));
        }
        var data = ListResult("npcs", rows, matched, args.Limit);
        data["coordinateSystem"] = CoordinateSystem();
        return data;
    }

    private Dictionary<string, object> ObserveLoot(ClientSessionCoordinator s, ObservationArguments args)
    {
        var rows = new List<object>();
        int matched = 0;
        foreach (var pair in s.GroundItems)
        {
            var p = pair.Value;
            if (p == null || !MatchesSpatial(args, "loot", p.GroundId, p.DisplayName, p.X, p.Y) ||
                (args.ItemId.HasValue && args.ItemId != p.ItemId)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            rows.Add(Values("entityId", EntityReference("loot", p.GroundId), "nativeId", p.GroundId,
                "itemId", p.ItemId, "name", BoundText(p.DisplayName), "count", p.Count,
                "x", p.X, "y", p.Y, "owned", p.Owned, "lockMillisecondsAtReceipt", p.LockMilliseconds,
                "quality", p.Quality, "kind", p.Kind));
        }
        var data = ListResult("items", rows, matched, args.Limit);
        data["coordinateSystem"] = CoordinateSystem();
        return data;
    }

    private Dictionary<string, object> ObserveInventory(ClientSessionCoordinator s, ObservationArguments args)
    {
        var state = s.InventoryState;
        if (state == null) return Unavailable("not_received", "Inventory has not been published.");
        var rows = new List<object>();
        int matched = 0;
        foreach (var p in state.Items)
        {
            if (p == null || (args.Tab.HasValue && args.Tab != p.Tab) ||
                (args.ItemId.HasValue && args.ItemId != p.ItemId) || !MatchesName(args.Name, p.DisplayName)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            rows.Add(Values("slotId", "bag:" + sceneRevision + ":" + p.Tab + ":" + p.BagIndex,
                "tab", p.Tab, "slot", p.BagIndex, "itemId", p.ItemId, "name", BoundText(p.DisplayName),
                "description", BoundText(p.Description, 512), "count", p.Count, "quality", p.Quality,
                "bound", p.Bound, "locked", p.Locked, "blocked", p.Blocked,
                "durability", p.CurrentDurability, "maxDurability", p.MaximumDurability,
                "equipmentSlot", p.EquipmentSlot, "sellPrice", p.SellPrice));
        }
        var data = ListResult("items", rows, matched, args.Limit);
        data["money"] = state.Money;
        data["caiYu"] = state.CaiYu;
        data["honor"] = state.Honor;
        data["weight"] = state.Weight;
        data["maxWeight"] = state.MaximumWeight;
        data["nativeVersion"] = s.InventoryStateVersion;
        return data;
    }

    private Dictionary<string, object> ObserveEquipment(ClientSessionCoordinator s, ObservationArguments args)
    {
        var state = s.InventoryState;
        if (state == null) return Unavailable("not_received", "Equipment has not been published.");
        var rows = new List<object>();
        int matched = 0;
        foreach (var p in state.EquippedItems)
        {
            if (p == null || (args.ItemId.HasValue && args.ItemId != p.ItemId) || !MatchesName(args.Name, p.DisplayName)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            rows.Add(Values("slot", p.Slot, "itemId", p.ItemId, "name", BoundText(p.DisplayName),
                "description", BoundText(p.Description, 512), "quality", p.Quality,
                "durability", p.CurrentDurability, "maxDurability", p.MaximumDurability,
                "avatar", p.Avatar, "soulAvatar", p.SoulAvatar));
        }
        return ListResult("items", rows, matched, args.Limit);
    }

    private Dictionary<string, object> ObserveSkills(ClientSessionCoordinator s, ObservationArguments args)
    {
        var state = s.SkillBookState;
        if (state == null) return Unavailable("not_received", "The learned skill book has not been published.");
        var rows = new List<object>();
        int matched = 0;
        foreach (var p in state.Skills)
        {
            if (p == null || (args.SkillId.HasValue && args.SkillId != p.Id)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            state.DamageBonuses.TryGetValue(p.Id, out int bonus);
            rows.Add(Values("skillId", p.Id, "level", p.Level, "damageBonus", bonus,
                "cooldownSeconds", GameHud.ObserveSkillCooldownSecondsForPlugins(p.Id)));
        }
        var data = ListResult("skills", rows, matched, args.Limit);
        data["skillPoints"] = state.SkillPoints;
        data["metadataAvailable"] = false;
        data["metadataUnavailableReason"] = "Names, range and targeting requirements are not exposed by this learned-skill receipt.";
        var cast = s.SkillCastState;
        data["lastCast"] = cast == null ? null : Values("skillId", cast.SkillId,
            "casterNativeId", cast.CasterEntityId, "targetNativeId", cast.TargetEntityId,
            "phase", cast.Phase, "chantHoldMilliseconds", cast.ChantHoldMilliseconds,
            "cooldownMilliseconds", cast.CooldownMilliseconds, "nativeRevision", cast.Revision);
        return data;
    }

    private Dictionary<string, object> ObserveBuffs(ClientSessionCoordinator s, ObservationArguments args)
    {
        var state = s.BuffState;
        if (state == null) return Unavailable("not_received", "Buff state has not been published.");
        var rows = new List<object>();
        int matched = 0;
        foreach (var p in state.Rows)
        {
            if (p == null || (args.SkillId.HasValue && args.SkillId != p.SkillId) || !MatchesName(args.Name, p.DisplayName)) continue;
            matched++;
            if (rows.Count >= args.Limit) continue;
            rows.Add(Values("skillId", p.SkillId, "name", BoundText(p.DisplayName),
                "description", BoundText(p.Description, 512), "iconId", p.IconId,
                "remainingSecondsAtReceipt", p.RemainingSeconds, "beneficial", p.Beneficial,
                "invisible", p.Invisible, "immobile", p.Immobile));
        }
        var data = ListResult("buffs", rows, matched, args.Limit);
        data["doubleExperience"] = state.DoubleExperience;
        data["nativeVersion"] = s.BuffStateVersion;
        return data;
    }

    private Dictionary<string, object> ObserveTarget(ClientSessionCoordinator s, ObservationArguments args)
    {
        var frame = ObservationFrame;
        var selection = frame?.TargetSelection;
        int selected = selection?.SelectedEntityId ?? 0;
        var hud = GameBootstrap.I?.Window?.TargetHud?.Snapshot;
        if (selected == 0) return Values("available", true, "selected", false, "target", null);
        string kind = selection.SelectedMonsterEntityId == selected ? "monster" :
            s.Npcs.ContainsKey(selected) ? "npc" : "player";
        var target = Values("entityId", EntityReference(kind, selected), "nativeId", selected,
            "kind", kind, "friendly", selection.CurrentFriendly);
        if (hud != null && hud.EntityId == selected)
        {
            target["name"] = BoundText(hud.DisplayName);
            target["level"] = hud.Level;
            target["hp"] = hud.HitPoints;
            target["maxHp"] = hud.MaximumHitPoints;
        }
        if (frame.MonsterActors.TryGetValue(selected, out var monster))
        {
            target["x"] = monster.WorldX;
            target["y"] = monster.WorldY;
        }
        else if (frame.RemoteActors.TryGetValue(selected, out var remote))
        {
            target["x"] = remote.WorldX;
            target["y"] = remote.WorldY;
        }
        else if (s.Npcs.TryGetValue(selected, out var npc))
        {
            target["x"] = npc.X;
            target["y"] = npc.Y;
        }
        else if (frame.Player != null && frame.Player.EntityId == selected)
        {
            target["x"] = frame.Player.WorldX;
            target["y"] = frame.Player.WorldY;
        }
        var effects = new List<object>();
        var status = s.TargetStatusState;
        bool statusMatches = status != null && status.EntityId == selected;
        if (statusMatches)
            foreach (var p in status.Rows)
            {
                if (effects.Count == args.Limit) break;
                effects.Add(Values("name", BoundText(p.DisplayName), "description", BoundText(p.Description, 512),
                    "beneficial", p.Beneficial, "iconId", p.IconId, "remainingMillisecondsAtReceipt", p.RemainingMilliseconds));
            }
        return Values("available", true, "selected", true, "target", target, "statuses", effects,
            "statusesAvailable", statusMatches, "statusesTruncated", statusMatches && status.Rows.Count > args.Limit,
            "coordinateSystem", CoordinateSystem());
    }

    private Dictionary<string, object> ObserveQuests(ClientSessionCoordinator s, ObservationArguments args)
    {
        var state = s.TaskLogState;
        if (state == null) return Unavailable("not_received", "The task log has not been published.");
        var active = new List<object>();
        int matched = 0;
        foreach (var p in state.Active)
        {
            if (p == null || (args.TaskId.HasValue && args.TaskId != p.TaskId)) continue;
            matched++;
            if (active.Count >= args.Limit) continue;
            active.Add(Values("taskId", p.TaskId, "text", BoundText(p.Text, 256),
                "progress", BoundText(p.Progress, 256), "step", p.Step, "total", p.Total,
                "ready", p.Ready, "terminal", p.IsTerminal, "turnInNpc", BoundText(p.TurnInNpc, 128),
                "remainingSecondsAtReceipt", p.RemainingSeconds));
        }
        var data = ListResult("active", active, matched, args.Limit);
        var done = new List<object>();
        int doneCount = 0;
        foreach (int id in state.Done)
        {
            if (args.TaskId.HasValue && args.TaskId != id) continue;
            doneCount++;
            if (done.Count < args.Limit) done.Add(id);
        }
        data["done"] = done;
        data["doneTotal"] = doneCount;
        data["doneTruncated"] = doneCount > done.Count;
        var offers = new List<object>();
        var offerState = s.NpcTaskOffersState;
        int offerCount = 0;
        if (offerState != null)
            foreach (var p in offerState.Offers)
            {
                if (p == null || (args.TaskId.HasValue && args.TaskId != p.TaskId)) continue;
                offerCount++;
                if (offers.Count >= args.Limit) continue;
                offers.Add(Values("taskId", p.TaskId, "state", p.State, "title", BoundText(p.Title, 256),
                    "description", BoundText(p.Description, 512), "award", BoundText(p.Award, 256)));
            }
        data["expectedOffersRevision"] = s.NpcTaskOffersStateVersion;
        data["expectedQuestRevision"] = s.TaskLogStateVersion;
        data["offerNpc"] = offerState == null ? null : BoundText(offerState.NpcName);
        data["offers"] = offers;
        data["offersTotal"] = offerCount;
        data["offersTruncated"] = offerCount > offers.Count;
        return data;
    }

    private Dictionary<string, object> ObserveDialogue(ObservationArguments args)
    {
        var dialog = DialogUI.I;
        long current = CurrentDialogueRevision();
        if (dialog == null) return Unavailable("dialogue_owner_unavailable", "The native NPC dialogue owner is not initialized.");
        var rows = new List<object>();
        var choices = dialog.Options;
        for (int i = 0; i < Math.Min(args.Limit, choices.Count); i++)
        {
            var choice = choices[i];
            rows.Add(Values("index", i, "label", BoundText(choice.Label, 1024), "leaves", choice.Leaves,
                "taskId", choice.Offer == null ? (object)null : choice.Offer.TaskId,
                "mutation", choice.MutationKind.ToString()));
        }
        var data = ListResult("choices", rows, choices.Count, args.Limit);
        data["visible"] = dialog.Visible;
        data["dialogueRevision"] = current;
        data["npcEntityId"] = dialog.CurrentNpcEntityId == 0 ? null : EntityReference("npc", dialog.CurrentNpcEntityId);
        data["expectedNpcId"] = data["npcEntityId"];
        data["nativeNpcId"] = dialog.CurrentNpcEntityId;
        data["npcName"] = BoundText(dialog.NpcName);
        data["greeting"] = BoundText(dialog.Greeting, 4096);
        data["inSubView"] = dialog.InSubView;
        data["sellList"] = dialog.CurrentSellList;
        data["transportList"] = dialog.CurrentTransportList;
        return data;
    }

    private Dictionary<string, object> ObserveUi(ObservationArguments args)
    {
        var data = Values("available", true, "addons", AddonUiHost.ObserveState(Math.Min(args.Limit, 32)),
            "dialogue", ObserveDialogue(args));
        var window = GameBootstrap.I?.Window;
        data["nativeWindows"] = Values("inventory", InventoryDlg.IsOpen,
            "skills", window?.SkillDialog?.SkillDialogVisible ?? false,
            "npcDialogue", DialogUI.VisibleS,
            "playerMenu", window?.PlayerActionMenu?.Visible ?? false,
            "store", window?.StoreDialog?.Visible ?? false,
            "transport", window?.TransportDialog?.Visible ?? false);
        data["nativeWidgetTreeAvailable"] = false;
        data["nativeWidgetTreeUnavailableReason"] = "The native immediate-mode UI has no complete retained widget tree. Named window states and NPC choices are exposed.";
        return data;
    }

    private static Dictionary<string, object> CoordinateSystem() =>
        Values("units", "map_pixels", "origin", "top_left", "xDirection", "right", "yDirection", "down");
    private static Dictionary<string, object> Unavailable(string code, string reason) =>
        Values("available", false, "code", code, "reason", reason);
    private static Dictionary<string, object> ListResult(string key, List<object> rows, int total, int limit) =>
        Values("available", true, key, rows, "total", total, "count", rows.Count,
            "limit", limit, "truncated", total > rows.Count);
    private static List<object> CopyNumbers(IReadOnlyList<int> source, int limit)
    {
        var result = new List<object>();
        if (source != null) for (int i = 0; i < Math.Min(source.Count, limit); i++) result.Add(source[i]);
        return result;
    }
    private string BoundText(string value, int maximum = 512)
    {
        if (value == null) return null;
        int length = Math.Min(value.Length, Math.Min(maximum, observationTextBudget));
        if (length > 0 && length < value.Length && char.IsHighSurrogate(value[length - 1])) length--;
        observationTextBudget -= length;
        if (length == value.Length) return value;
        observationTextTruncated = true;
        return value.Substring(0, length);
    }
    private static bool MatchesName(string wanted, string actual) =>
        wanted == null || (actual ?? string.Empty).IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0;
    private bool MatchesSpatial(ObservationArguments args, string kind, int nativeId, string name, double x, double y)
    {
        if (args.Kind != null && args.Kind != kind) return false;
        if (args.EntityId != null && args.EntityId != EntityReference(kind, nativeId)) return false;
        if (!MatchesName(args.Name, name)) return false;
        if (!args.Radius.HasValue) return true;
        var player = ObservationFrame?.Player;
        double centerX = args.X ?? player?.WorldX ?? 0;
        double centerY = args.Y ?? player?.WorldY ?? 0;
        double dx = x - centerX, dy = y - centerY;
        return dx * dx + dy * dy <= args.Radius.Value * args.Radius.Value;
    }

    private static object ObservePath(IReadOnlyDictionary<int, WorldRemotePathState> paths, int id)
    {
        if (paths == null || !paths.TryGetValue(id, out var path) || path == null) return null;
        var points = new List<object>();
        for (int i = 0; i < Math.Min(path.Points.Count, 8); i++)
            points.Add(Values("x", path.Points[i].X, "y", path.Points[i].Y));
        return Values("speed", path.Speed, "points", points, "nativeRevision", path.Revision,
            "total", path.Points.Count, "truncated", path.Points.Count > points.Count);
    }

    private static string WithObservationCenter(string key, bool implicitCenter, float x, float y) =>
        implicitCenter ? key + "|resolvedCenter=" + Token(x, y) : key;

    private static string ObservationQueryKey(string domain, IReadOnlyDictionary<string, object> arguments)
    {
        var keys = new List<string>();
        foreach (var pair in arguments)
            if (pair.Key != "sinceRevision" && pair.Key != "expectedSceneRevision" && pair.Key != "sinceQueryKey") keys.Add(pair.Key);
        keys.Sort(StringComparer.Ordinal);
        var key = new System.Text.StringBuilder(domain);
        foreach (string name in keys)
        {
            string value = Convert.ToString(arguments[name], CultureInfo.InvariantCulture);
            key.Append('|').Append(name.Length).Append(':').Append(name).Append('=').Append(value.Length).Append(':').Append(value);
        }
        return key.ToString();
    }

    private static bool IsObservationDomain(string domain)
    {
        switch (domain)
        {
            case "world": case "player": case "entities": case "npcs": case "loot":
            case "inventory": case "equipment": case "skills": case "buffs": case "target":
            case "quests": case "dialogue": case "ui":
            case "endgame": case "endgame.items": case "endgame.catalog": case "endgame.rift": return true;
            default: return false;
        }
    }

    private static void ValidateObservationArguments(string domain, IReadOnlyDictionary<string, object> arguments)
    {
        string specific;
        switch (domain)
        {
            case "endgame.items": specific = "|uid|location|tab|itemId|name|offset|"; break;
            case "endgame.catalog": specific = "|catalog|offset|"; break;
            case "endgame.rift": specific = "|offset|"; break;
            case "entities": specific = "|radius|x|y|kind|entityId|name|"; break;
            case "npcs": specific = "|radius|x|y|entityId|name|"; break;
            case "loot": specific = "|radius|x|y|entityId|name|itemId|"; break;
            case "inventory": specific = "|tab|itemId|name|"; break;
            case "equipment": specific = "|itemId|name|"; break;
            case "skills": specific = "|skillId|"; break;
            case "buffs": specific = "|skillId|name|"; break;
            case "quests": specific = "|taskId|"; break;
            default: specific = "|"; break;
        }
        const string common = "|limit|sinceRevision|sinceQueryKey|expectedSceneRevision|";
        foreach (var pair in arguments)
            if (common.IndexOf("|" + pair.Key + "|", StringComparison.Ordinal) < 0 &&
                specific.IndexOf("|" + pair.Key + "|", StringComparison.Ordinal) < 0)
                throw new ArgumentException("Unsupported argument for " + domain + ": " + pair.Key);
    }

    private sealed class ObservationArguments
    {
        internal int Limit = 128, Offset;
        internal string UID, Location, Catalog;
        internal int? Tab, ItemId, SkillId, TaskId;
        internal double? X, Y, Radius;
        internal string Kind, EntityId, Name, SinceQueryKey;
        internal long? SinceRevision, ExpectedSceneRevision;
        internal ObservationArguments(IReadOnlyDictionary<string, object> arguments)
        {
            foreach (var pair in arguments)
            {
                switch (pair.Key)
                {
                    case "uid": UID = EndgameIdentity(Text(pair.Value, pair.Key), pair.Key); break;
                    case "location": Location = Text(pair.Value, pair.Key); if (Location != "bag" && Location != "equipped" && Location != "warehouse" && Location != "changeset") throw new ArgumentException("Unknown endgame item location."); break;
                    case "catalog": Catalog = EndgameCatalogName(Text(pair.Value, pair.Key)); break;
                    case "offset": Offset = Integer(pair.Value, pair.Key); if (Offset < 0 || Offset > 4096) throw new ArgumentException("offset must be between 0 and 4096."); break;
                    case "limit": Limit = Math.Max(1, Math.Min(MaximumRows, Integer(pair.Value, pair.Key))); break;
                    case "tab": Tab = Integer(pair.Value, pair.Key); break;
                    case "itemId": ItemId = Integer(pair.Value, pair.Key); break;
                    case "skillId": SkillId = Integer(pair.Value, pair.Key); break;
                    case "taskId": TaskId = Integer(pair.Value, pair.Key); break;
                    case "x": X = Number(pair.Value, pair.Key); break;
                    case "y": Y = Number(pair.Value, pair.Key); break;
                    case "radius": Radius = Number(pair.Value, pair.Key); if (Radius < 0) throw new ArgumentException("radius must be nonnegative."); break;
                    case "kind": Kind = Text(pair.Value, pair.Key).ToLowerInvariant(); break;
                    case "entityId": EntityId = Text(pair.Value, pair.Key); break;
                    case "name": Name = Text(pair.Value, pair.Key); break;
                    case "sinceQueryKey": SinceQueryKey = Text(pair.Value, pair.Key); break;
                    case "sinceRevision": SinceRevision = LongInteger(pair.Value, pair.Key); break;
                    case "expectedSceneRevision": ExpectedSceneRevision = LongInteger(pair.Value, pair.Key); break;
                    default: throw new ArgumentException("Unsupported observation argument: " + pair.Key);
                }
            }
            if (X.HasValue != Y.HasValue) throw new ArgumentException("x and y must be supplied together.");
        }
        private static string Text(object value, string name)
        {
            if (!(value is string text) || text.Length > 4096) throw new ArgumentException(name + " must be a string up to 4096 characters.");
            return text;
        }
        private static double Number(object value, string name)
        {
            if (value == null || value is bool || value is string) throw new ArgumentException(name + " must be numeric.");
            try
            {
                double result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                if (double.IsNaN(result) || double.IsInfinity(result)) throw new ArgumentException(name + " must be finite.");
                return result;
            }
            catch (InvalidCastException) { throw new ArgumentException(name + " must be numeric."); }
            catch (OverflowException) { throw new ArgumentException(name + " is out of range."); }
        }
        private static long LongInteger(object value, string name)
        {
            if (value == null || value is bool || value is string) throw new ArgumentException(name + " must be an integer.");
            // Integral doubles are normal Lua values. Reject fractions before decimal's
            // conversion can round them, and reject integers outside their exact safe range.
            if (value is double floating && (double.IsNaN(floating) || double.IsInfinity(floating) ||
                Math.Truncate(floating) != floating || Math.Abs(floating) > 9007199254740991d))
                throw new ArgumentException(name + " must be an exactly representable integer.");
            if (value is float single && (float.IsNaN(single) || float.IsInfinity(single) ||
                Math.Truncate(single) != single || Math.Abs(single) > 16777215f))
                throw new ArgumentException(name + " must be an exactly representable integer.");
            try
            {
                decimal number = Convert.ToDecimal(value, CultureInfo.InvariantCulture);
                if (number < 0 || number > long.MaxValue || number != decimal.Truncate(number)) throw new ArgumentException(name + " must be a nonnegative 64-bit integer.");
                return (long)number;
            }
            catch (InvalidCastException) { throw new ArgumentException(name + " must be an integer."); }
            catch (OverflowException) { throw new ArgumentException(name + " is out of range."); }
        }
        private static int Integer(object value, string name)
        {
            long result = LongInteger(value, name);
            if (result > int.MaxValue) throw new ArgumentException(name + " is out of range.");
            return (int)result;
        }
    }
}
}
