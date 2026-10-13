using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private IEndgameClient EndgameClient => Session?.EndgameClientForPlugins;

    private void AddEndgameActions(List<GameActionDefinition> definitions)
    {
        IEndgameClient owner = EndgameClient;
        EndgameClientState state = owner?.Endgame;
        bool ready = owner != null && owner.EndgameInWorld && state.Negotiated;
        bool writable = ready && state.InventorySnapshotCurrent && !state.Pending;
        const string unavailable = "The native world must negotiate endgame.v1 before this operation.";
        const string mutation = "A complete server snapshot and no uncertain mutation are required. Observe endgame; use endgame.retry only for the exact pending request.";
        definitions.Add(EndgameDefinition("endgame.skill.refresh", "Request authoritative hard, granted, soft and effective skill ranks, effects and unspent points.", ready && state.GrowthV5, "The server must negotiate endgame.growth.v5."));
        definitions.Add(EndgameDefinition("endgame.skill.upgrade", "Invest one hard skill point using the observed revision. Completion requires the correlated server receipt; effective ranks include soft levels and never authorize hard points.", ready && state.GrowthV5 && state.HasSkillRanks && state.SkillRanksRevision == state.Revision && !state.Pending && state.UnspentSkillPoints > 0, "Observe current endgame.skills and resolve any pending mutation first.", "expectedRevision:string", "skillId:integer"));
        definitions.Add(EndgameDefinition("endgame.refresh", "Request an authoritative inventory/material/active-formation snapshot through the native endgame owner.", ready, unavailable));
        definitions.Add(EndgameDefinition("endgame.catalog.request", "Request one server catalog page. Omitted cursor uses the observed next cursor. Observe endgame.catalog for page/load state.", ready, unavailable, "catalog:string", "cursor?:string"));
        definitions.Add(EndgameDefinition("endgame.socket.insert", "Insert an observed bag rune into an observed empty socket; only the correlated server receipt establishes the outcome.", writable, mutation, "expectedRevision:string", "targetUid:string", "runeUid:string", "socket:integer"));
        definitions.Add(EndgameDefinition("endgame.socket.remove", "Extract an observed installed rune through the native server command.", writable, mutation, "expectedRevision:string", "targetUid:string", "socket:integer"));
        definitions.Add(EndgameDefinition("endgame.cube.quote", "Ask the server for costs, outcome and expiry; this never executes the recipe. itemUids preserves selected rune order.", writable && !state.QuotePending, mutation, "expectedRevision:string", "recipeId:integer", "targetUid?:string", "runeUid?:string", "itemUids?:array"));
        definitions.Add(EndgameDefinition("endgame.cube.execute", "Execute exactly the currently observed unexpired server quote. Costs/risks are in endgame.quote; no implicit quote or reroll.", writable && state.Quote != null && !state.QuotePending, mutation, "expectedRevision:string", "quoteId:string"));
        definitions.Add(EndgameDefinition("endgame.rift.refresh", "Request authoritative rift state, invitation, combat markers and reward eligibility.", ready, unavailable));
        definitions.Add(EndgameDefinition("endgame.rift.start", "Spend a key for a fresh run, or enter the observed invitation/run with its exact runId. Complete native NPC prerequisites first.", writable && state.Rift != null, mutation, "expectedRevision:string", "tier:integer", "runId?:string"));
        definitions.Add(EndgameDefinition("endgame.rift.advance", "Use the observed floor exit to skip remaining packs. Preserves run, difficulty tier, timer and earned progress; runId and floorIndex guard stale navigation. The server checks run opener/exit proximity.", writable && state.Rift != null && state.Rift.CanAdvance && state.Rift.RunID.Length > 0 && state.Rift.FloorIndex > 0 && !state.Rift.Transitioning && state.Rift.Phase == "active", "Observe an active, non-transitioning floor with canAdvance=true and no uncertain native mutation.", "expectedRevision:string", "runId:string", "floorIndex:integer"));
        definitions.Add(EndgameDefinition("endgame.rift.claim", "Claim only the observed server-claimable run reward or interruption compensation.", writable && state.Rift != null && state.Rift.Claimable, mutation, "expectedRevision:string", "runId:string"));
        definitions.Add(EndgameDefinition("endgame.rift.leave", "Leave the observed run. confirmLeave=true acknowledges that leaving does not automatically refund its key.", writable && state.Rift != null && state.Rift.RunID.Length > 0, mutation, "expectedRevision:string", "runId:string", "confirmLeave:boolean"));
        definitions.Add(EndgameDefinition("endgame.retry", "Explicitly query/resend the exact immutable uncertain native request. expectedRequestId is pendingRequest.requestId (the server ID), not a new operation.", ready && state.Pending, "No uncertain native endgame request is available.", "expectedRequestId:string"));
        definitions.Add(EndgameDefinition("endgame.equipment.equip", "Equip the UID-guarded native bag item. allowBind=true is required for an unbound item; sent never proves equipment changed.", writable, mutation, "expectedRevision:string", "targetUid:string", "allowBind?:boolean"));
        definitions.Add(EndgameDefinition("endgame.equipment.unequip", "Unequip the UID-guarded native equipped item, including slots 14–25. Observe a later inventory snapshot for outcome.", writable, mutation, "expectedRevision:string", "targetUid:string"));
    }

    private static GameActionDefinition EndgameDefinition(string name, string description, bool available, string reason, params string[] fields)
    {
        var properties = new Dictionary<string, object>(); var required = new List<object>();
        foreach (string field in fields)
        {
            string[] parts = field.Split(':'); bool optional = parts[0].EndsWith("?", StringComparison.Ordinal);
            string key = optional ? parts[0].Substring(0, parts[0].Length - 1) : parts[0];
            var schema = new Dictionary<string, object> { { "type", parts[1] } };
            if (parts[1] == "string") schema["maxLength"] = 128;
            if (key == "expectedRevision" || key == "targetUid" || key == "runeUid")
            { schema["pattern"] = key == "expectedRevision" ? "^(0|[1-9][0-9]{0,18})$" : "^[1-9][0-9]{0,18}$"; schema["maxLength"] = 19; }
            if (key == "socket") { schema["minimum"] = 0; schema["maximum"] = 4; }
            if (key == "tier") { schema["minimum"] = 1; schema["maximum"] = 150; }
            if (key == "floorIndex") { schema["minimum"] = 1; schema["maximum"] = int.MaxValue; }
            if (key == "skillId") { schema["minimum"] = 1; schema["maximum"] = int.MaxValue; }
            if (key == "recipeId") { schema["minimum"] = 1; schema["maximum"] = int.MaxValue; }
            if (key == "catalog") schema["enum"] = new object[] { "uniques", "runes", "formations", "recipes" };
            if (key == "confirmLeave") schema["const"] = true;
            if (key == "itemUids")
            {
                schema["maxItems"] = 5; schema["uniqueItems"] = true;
                schema["items"] = new Dictionary<string, object> { { "type", "string" }, { "pattern", "^[1-9][0-9]{0,18}$" }, { "maxLength", 19 } };
            }
            properties.Add(key, schema); if (!optional) required.Add(key);
        }
        return new GameActionDefinition(name, description, new Dictionary<string, object> { { "type", "object" }, { "properties", properties }, { "required", required }, { "additionalProperties", false } }, available, available ? null : reason);
    }

    private static string EndgameIdentity(string value, string key, bool allowZero = false)
    {
        long parsed;
        if (string.IsNullOrEmpty(value) || value.Length > 19 || (value.Length > 1 && value[0] == '0') ||
            !long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed) || parsed < (allowZero ? 0 : 1))
            throw new ArgumentException(key + " must be a canonical decimal Int64 string.");
        return value;
    }
    private static string EndgameString(IReadOnlyDictionary<string, object> args, string key, bool required = true)
    {
        string value = ActionString(args, key, required);
        if (value != null && (value.Length > 128 || (required && value.Length == 0))) throw new ArgumentException(key + " must contain 1..128 characters.");
        return value ?? "";
    }
    private static List<string> EndgameUIDs(IReadOnlyDictionary<string, object> args)
    {
        if (!args.TryGetValue("itemUids", out object value)) return null;
        var rows = value as IReadOnlyList<object>;
        if (rows == null || rows.Count > 5) throw new ArgumentException("itemUids must be an ordered array of at most five UID strings.");
        var result = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (object row in rows)
        {
            string id = EndgameIdentity(row as string, "itemUids");
            if (!seen.Add(id)) throw new ArgumentException("itemUids cannot contain duplicate identities.");
            result.Add(id);
        }
        return result;
    }
    private static bool EndgameTransient(string error) => error == "pending" || error == "busy" || error == "storage" || error == "storage_unavailable";
    private static string EndgameWireID(string requestID)
    {
        // Stable across an agent reconnect/API restart; no truncation or numeric conversion.
        using (var hash = SHA256.Create())
        {
            byte[] bytes = hash.ComputeHash(Encoding.UTF8.GetBytes("ffo-plugin-endgame/1:" + requestID));
            var text = new StringBuilder(64); foreach (byte value in bytes) text.Append(value.ToString("x2", CultureInfo.InvariantCulture));
            return text.ToString();
        }
    }
    private EndgameItemView EndgameItem(string uid, string location = null)
    {
        EndgameIdentity(uid, "uid");
        if (!EndgameClient.Endgame.InventorySnapshotCurrent)
            throw new ActionFailure("endgame.item_stale", "The native inventory changed; wait for or refresh its complete authoritative item snapshot.");
        foreach (var item in EndgameClient.Endgame.Items)
            if (item.UID == uid && (location == null ? item.Location == "bag" || item.Location == "equipped" : item.Location == location))
            {
                if (item.Locked) throw new ActionFailure("endgame.item_locked", "The observed item is locked.");
                return item;
            }
        throw new ActionFailure("endgame.item_stale", "The UID is not present at the required location in the current native snapshot.");
    }

    private void DispatchEndgame(PluginAction entry)
    {
        if (string.IsNullOrWhiteSpace(entry.Request.RequestId)) throw new ArgumentException("Endgame actions require a stable requestId.");
        IEndgameClient owner = EndgameClient; EndgameClientState state = owner.Endgame;
        var args = entry.Request.Arguments; string op = entry.Request.Name.Substring("endgame.".Length);
        string revision = "", uid = "", rune = "", quote = "", run = "", catalog = "", cursor = "";
        int socket = 0, recipe = 0, tier = 0, floor = 0, skill = 0; List<string> itemUIDs = null;
        if (args.ContainsKey("expectedRevision"))
        {
            revision = EndgameIdentity(ActionString(args, "expectedRevision"), "expectedRevision", true);
            if (revision != state.Revision) throw new ActionFailure("endgame.stale_revision", "Observe the current endgame inventory revision before deciding.");
        }
        if (op == "retry")
        {
            if (EndgameString(args, "expectedRequestId") != state.PendingRequest?.RequestID)
                throw new ActionFailure("endgame.pending_changed", "The uncertain request changed; inspect pendingRequest before retrying.");
            TrackEndgame(entry, owner, state.PendingRequest);
            bool sent = owner.RetryEndgame();
            ActionSet(entry, sent ? GameActionState.Sent : GameActionState.Unknown, sent ? "endgame.retry_sent" : "endgame.uncertain", "The exact original request was queried; its server outcome is not yet known.", EndgameReceiptData(entry));
            return;
        }
        if (op == "equipment.equip" || op == "equipment.unequip")
        {
            EndgameItemView item = EndgameItem(ActionString(args, "targetUid"), op == "equipment.equip" ? "bag" : "equipped");
            if (SocialClient.TradeActive) throw new ActionFailure("inventory.trade_active", "Native equipment activation is blocked during a trade.");
            if (op == "equipment.equip" && !item.Bound && !ActionBool(args, "allowBind", false))
                throw new ActionFailure("equipment.binding_confirmation_required", "Equipping this item can bind it permanently. Supply allowBind=true only if intended.");
            if (!(op == "equipment.equip" ? owner.EquipEndgameItem(item) : owner.UnequipEndgameItem(item)))
                throw new ActionFailure("endgame.native_rejected", "The native equipment owner rejected the current item or session.");
            ActionSet(entry, GameActionState.Sent, "endgame.equipment_sent", "The native equipment command was submitted. Observe authoritative inventory/equipment; this is not server completion."); return;
        }
        switch (op)
        {
            case "refresh": op = "snapshot"; break;
            case "skill.refresh": op = "skill.ranks"; break;
            case "skill.upgrade":
                skill = ActionInt(args, "skillId", 1);
                if (!state.CanInvestSkill(skill)) throw new ActionFailure("skill.upgrade.unavailable", "The server does not currently permit investing in this skill; inspect blockedReasons in endgame.skills.");
                break;
            case "rift.refresh": op = "rift.state"; run = state.Rift?.RunID ?? ""; break;
            case "catalog.request":
                op = "catalog"; catalog = EndgameCatalogName(EndgameString(args, "catalog"));
                if (state.CatalogRequested(catalog)) throw new ActionFailure("endgame.catalog_pending", "This catalog already has a pending native request.");
                cursor = args.ContainsKey("cursor") ? EndgameString(args, "cursor", false) : state.NextCatalogCursor(catalog);
                if (cursor.Length > 0 && cursor != state.NextCatalogCursor(catalog)) throw new ActionFailure("endgame.catalog_stale", "Use the currently observed next catalog cursor.");
                break;
            case "socket.insert": case "socket.remove":
                uid = EndgameIdentity(ActionString(args, "targetUid"), "targetUid"); EndgameItemView target = EndgameItem(uid);
                socket = ActionInt(args, "socket", 0, 4);
                if (socket >= target.SocketCount) throw new ActionFailure("endgame.socket_unavailable", "The observed socket is not open.");
                bool occupied = false; foreach (var installed in target.Sockets) if (installed.Index == socket && installed.ItemID > 0) occupied = true;
                if (occupied != (op == "socket.remove")) throw new ActionFailure("endgame.socket_changed", "Insertion requires an empty socket; removal requires an installed rune.");
                if (op == "socket.insert") { rune = EndgameIdentity(ActionString(args, "runeUid"), "runeUid"); EndgameItem(rune, "bag"); }
                break;
            case "cube.quote":
                recipe = ActionInt(args, "recipeId", 1); itemUIDs = EndgameUIDs(args);
                uid = EndgameString(args, "targetUid", false); rune = EndgameString(args, "runeUid", false);
                EndgameRecipeView chosen = null; foreach (var row in state.Recipes) if (row.ID == recipe) chosen = row;
                if (chosen == null) throw new ActionFailure("endgame.recipe_unobserved", "Load and observe the recipe catalog before requesting its quote.");
                EndgameItemView cubeTarget = uid.Length > 0 ? EndgameItem(uid, "bag") : null;
                if (chosen.TargetRequired && cubeTarget == null) throw new ArgumentException("This recipe requires targetUid.");
                if (!chosen.TargetRequired && cubeTarget != null) throw new ArgumentException("This recipe does not use targetUid.");
                if (rune.Length > 0) EndgameItem(rune, "bag");
                if (itemUIDs != null) foreach (string id in itemUIDs)
                {
                    if (chosen.Operation != "socket.reorder") EndgameItem(id, "bag");
                    else
                    {
                        bool installed = false; if (cubeTarget != null) foreach (var row in cubeTarget.Sockets) if (row.UID == id && row.ItemID > 0) installed = true;
                        if (!installed) throw new ActionFailure("endgame.rune_stale", "Reorder inputs must be the observed installed rune UIDs.");
                    }
                }
                break;
            case "cube.execute":
                quote = EndgameString(args, "quoteId");
                if (state.Quote == null || state.Quote.QuoteID != quote || state.Quote.Revision != revision ||
                    !DateTimeOffset.TryParse(state.Quote.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out DateTimeOffset expires) || expires <= DateTimeOffset.UtcNow)
                    throw new ActionFailure("endgame.quote_stale", "The exact server quote must still be current and unexpired. Request and inspect a new quote.");
                break;
            case "rift.start":
                tier = ActionInt(args, "tier", 1, 150); run = EndgameString(args, "runId", false);
                var rift = state.Rift;
                bool existing = rift != null && (rift.Phase == "reserved" || rift.Phase == "active" || rift.Phase == "boss" || rift.Phase == "completed");
                if (existing && (run != rift.RunID || tier != rift.Tier) || !existing && run.Length > 0)
                    throw new ActionFailure("endgame.run_stale", "Use the observed invitation/run ID and tier for reentry; a fresh entry leaves runId empty.");
                if (rift == null || rift.Transitioning || rift.Phase == "reward_pending" || rift.Phase == "interrupted" || !existing && rift.KeyCount < 1)
                    throw new ActionFailure("endgame.rift_unavailable", "Refresh the rift, resolve pending rewards/compensation, and obtain a key before fresh entry.");
                if (!existing && (rift.MaxTier > 0 && tier > rift.MaxTier || rift.UnlockedTier > 0 && tier > rift.UnlockedTier))
                    throw new ActionFailure("endgame.tier_locked", "Select a difficulty tier within the server's observed maximum and unlocked limit.");
                break;
            case "rift.advance":
                run = EndgameString(args, "runId"); floor = ActionInt(args, "floorIndex", 1);
                if (state.Rift == null || state.Rift.RunID != run || state.Rift.FloorIndex != floor)
                    throw new ActionFailure("endgame.floor_stale", "The run or floor changed. Observe the current exit before advancing.");
                if (state.Rift.Phase != "active" || !state.Rift.CanAdvance || state.Rift.Transitioning)
                    throw new ActionFailure("endgame.advance_unavailable", "The current floor cannot advance; the server owns transition eligibility.");
                break;
            case "rift.claim": case "rift.leave":
                run = EndgameString(args, "runId");
                if (state.Rift == null || state.Rift.RunID != run) throw new ActionFailure("endgame.run_stale", "The run ID differs from the observed current run.");
                if (op == "rift.claim" && !state.Rift.Claimable) throw new ActionFailure("endgame.not_claimable", "Only the server's claimable flag enables a claim.");
                if (op == "rift.leave" && !ActionBool(args, "confirmLeave", false)) throw new ArgumentException("confirmLeave must be true; leaving does not automatically refund the key.");
                break;
            default: throw new ArgumentException("Unknown endgame operation.");
        }
        var command = new EndgameRequest(op, revision, uid, rune, socket, recipe, quote, run, tier, itemUIDs, catalog, cursor, EndgameWireID(entry.Request.RequestId), floor, skill);
        TrackEndgame(entry, owner, command);
        bool submitted = owner.SendEndgame(command);
        if (!submitted && !ReferenceEquals(state.PendingRequest, command))
        {
            entry.EndgameWaiting = false;
            ActionSet(entry, GameActionState.Unknown, "endgame.send_unconfirmed", "The native request did not report a successful submission; observe before deciding whether to retry.", EndgameReceiptData(entry));
        }
        else ActionSet(entry, submitted ? GameActionState.Sent : GameActionState.Unknown, submitted ? "endgame.sent" : "endgame.uncertain", "Awaiting the matching authoritative server receipt. No gameplay success is inferred.", EndgameReceiptData(entry));
    }

    private static void TrackEndgame(PluginAction entry, IEndgameClient owner, EndgameRequest command)
    {
        entry.EndgameOwner = owner; entry.EndgameGeneration = owner.Endgame.Generation;
        entry.EndgameRequest = command; entry.EndgameWaiting = true; entry.Deadline = ActionNow + 30d;
    }
    private static Dictionary<string, object> EndgameReceiptData(PluginAction entry) => new Dictionary<string, object>
    {
        { "serverRequestId", entry.EndgameRequest.RequestID }, { "operation", entry.EndgameRequest.Op },
        { "serverOutcomeKnown", false }, { "completionScope", "server" }, { "retryAction", "endgame.retry" }
    };
    private void PollEndgame(PluginAction entry)
    {
        if (!entry.EndgameWaiting) return;
        IEndgameClient owner = EndgameClient;
        if (!ReferenceEquals(owner, entry.EndgameOwner) || owner == null || !owner.EndgameInWorld || owner.Endgame.Generation != entry.EndgameGeneration)
        {
            entry.EndgameWaiting = false;
            ActionSet(entry, GameActionState.Unknown, "endgame.session_changed", "The native connection changed. Re-observe; only a restored pendingRequest may be explicitly retried.", EndgameReceiptData(entry)); return;
        }
        EndgameResponse response = owner.Endgame.ResponseFor(entry.EndgameRequest.RequestID);
        if (response != null && !entry.EndgameRequest.MatchesResponse(response)) response = null;
        if (response != null && (response.OK || !EndgameTransient(response.Error)))
        {
            // A chunked snapshot is complete only after its entire immutable view publishes.
            if (response.OK && entry.EndgameRequest.Op == "snapshot" && (response.Kind != "snapshot" || response.Chunk + 1 != response.Chunks || owner.Endgame.SnapshotLoading || owner.Endgame.SnapshotID != response.SnapshotID)) return;
            entry.EndgameWaiting = false;
            var data = EndgameReceiptData(entry); data["serverOutcomeKnown"] = true; data["serverRevision"] = response.Revision;
            data["serverKind"] = response.Kind; data["serverError"] = response.Error;
            var result = new Dictionary<string, object>(); foreach (var pair in response.Result) result.Add(pair.Key, pair.Value); data["result"] = result;
            ActionSet(entry, response.OK ? GameActionState.Completed : GameActionState.Rejected,
                response.OK ? "endgame.confirmed" : "endgame.server." + response.Error,
                response.Message.Length > 0 ? response.Message : response.OK ? "The server confirmed this request; refresh relevant observations." : "The server rejected this request.", data); return;
        }
        if (response != null && EndgameTransient(response.Error) && !ReferenceEquals(entry.EndgameResponseSeen, response))
        {
            entry.EndgameResponseSeen = response;
            var data = EndgameReceiptData(entry); data["serverError"] = response.Error;
            ActionSet(entry, GameActionState.Unknown, "endgame.server." + response.Error, "The server has not established a final result. Preserve the request and explicitly retry the pending native request only.", data);
        }
        if (ActionNow >= entry.Deadline && entry.Receipt.Code != "endgame.receipt_unconfirmed")
        {
            // An uncertain mutation remains correlated until a final receipt or session reset.
            entry.EndgameWaiting = entry.EndgameRequest.Mutates;
            if (entry.EndgameRequest.Op == "snapshot")
            {
                ActionSet(entry, GameActionState.Sent, "endgame.snapshot_unconfirmed", "The current server publishes uncorrelated snapshots. Observe endgame.snapshotId and inventory; no correlated completion is asserted.", EndgameReceiptData(entry)); return;
            }
            ActionSet(entry, GameActionState.Unknown, "endgame.receipt_unconfirmed", "No final correlated receipt arrived within 30 seconds. Re-observe and reconcile; elapsed time is not rejection.", EndgameReceiptData(entry));
        }
    }
    private static string EndgameCatalogName(string name)
    {
        if (name != "uniques" && name != "runes" && name != "formations" && name != "recipes") throw new ArgumentException("catalog must be uniques, runes, formations, or recipes.");
        return name;
    }
}
}
