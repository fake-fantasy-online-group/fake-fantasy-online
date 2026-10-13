// Real endgame DTO/state/adapter sources with a deterministic native gateway substitute.
// These tests never launch Unity, create a Player, or contact an external server.
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static partial class GameActionAdapterTests
{
    private const string EquipmentUID = "9007199254740993";
    private const string FirstRuneUID = "9007199254740994";
    private const string SecondRuneUID = "9223372036854775807";
    private const string EquippedUID = "9007199254740995";
    private const string TestRevision = "9007199254740996";

    private static GameActionReceipt EndAct(PluginGameApi api, string suffix, Dictionary<string, object> args = null, string id = null)
    { return Act(api, "endgame." + suffix, args, id ?? "test-" + Guid.NewGuid().ToString("N")); }
    private static Dictionary<string, object> InsertArgs(string target = EquipmentUID, string rune = FirstRuneUID, object socket = null)
    { return Map("expectedRevision", TestRevision, "targetUid", target, "runeUid", rune, "socket", socket ?? 0); }
    private static void RunEndgameTests()
    {
        int initial = checks;
        EndgameDiscoveryAndValidation();
        EndgameReceiptsAndRetry();
        EndgameCubeAndEquipment();
        EndgameRiftRouting();
        EndgameRiftAdvance();
        EndgameProjectionContracts();
        EndgameGrowthContracts();
        EndgameCombatProjectionContracts();
        Console.WriteLine("Endgame adapter passed: " + (checks - initial) + " assertions (real DTO/state/actions/projections; native substitutes).");
    }
    private static void EndgameCombatProjectionContracts()
    {
        EndgameNative native; var api = SetupEndgame(out native);
        Check(api.TestObserveEndgame("endgame")["combatStats"] == null, "legacy snapshot has no invented combat stats");
        Apply(native, Map("kind", "combat.stats", "ok", true, "revision", TestRevision, "combatStats", Map(
            "magicFind", 9223372036854775807L, "physicalMitigationBP", 500,
            "elements", new object[] { Map("element", "fire", "damageBP", 350, "resistBP", 15000, "pierceBP", -30, "mitigationBP", 450, "absorbBP", 250) },
            "equipmentAuras", new object[] { Map("auraId", "jade", "family", "guard", "sourceEntityId", 4294967295L, "sourceUID", EquipmentUID, "rank", 12, "assetKey", "jade_aura", "modifier", "physical_mitigation_bp", "value", 500, "leaseMs", 1000) },
            "procReadiness", new object[] { Map("effectId", "flame", "sourceUID", SecondRuneUID, "trigger", "on_attack", "skillId", 10401, "fixedRank", 15, "chanceBP", 500, "icdMs", 1500, "remainingMs", 2147483648L, "globalRemainingMs", 9223372036854775807L, "ready", false) })));
        var observation = api.TestObserveEndgame("endgame"); var stats = Object(observation["combatStats"]);
        Check(Convert.ToInt64(stats["magicFind"]) == long.MaxValue && (int)stats["physicalMitigationBP"] == 500, "combat projection preserves Int64 magic find and physical channel");
        var element = Object(Rows(stats["elements"])[0]);
        Check((int)element["resistBP"] == 15000 && (int)element["pierceBP"] == -30, "agent sees raw overcap and signed equipment channels");
        var aura = Object(Rows(stats["equipmentAuras"])[0]);
        Check(Convert.ToUInt32(aura["sourceEntityId"]) == uint.MaxValue && (string)aura["sourceUID"] == EquipmentUID && (string)aura["assetKey"] == "jade_aura", "aura observation preserves uint entity and decimal UID");
        var proc = Object(Rows(stats["procReadiness"])[0]);
        Check(Convert.ToInt64(proc["remainingMs"]) == 2147483648L && Convert.ToInt64(proc["globalRemainingMs"]) == long.MaxValue && !(bool)proc["ready"], "agent proc readiness keeps Int64 delays and server readiness");
        Check((string)observation["combatStatsRevision"] == TestRevision && Convert.ToInt64(observation["combatStatsVersion"]) > 0 && (string)observation["combatStatsReceivedUtc"] != "", "combat observation carries refresh identity and timestamp");
        native.Endgame.Reset(); Check(!(bool)api.TestObserveEndgame("endgame")["available"], "reset removes live combat observations");
        Check(Convert.ToInt64(stats["magicFind"]) == long.MaxValue, "published combat observation remains immutable after reset");
    }
    private static void EndgameGrowthContracts()
    {
        EndgameNative native; var api = SetupEndgame(out native);
        Check(!Definition(api, "endgame.skill.upgrade").Available, "legacy negotiation cannot invest V5 points");
        native.Endgame.Begin(new EndgameRequest("hello"));
        Apply(native, Map("kind", "hello", "ok", true, "capabilities", new object[] { "endgame.v1", "endgame.growth.v5" }));
        string enormous = "184467440737095516170000001";
        Apply(native, Map("kind", "skill.ranks", "ok", true, "revision", TestRevision,
            "skillRanks", new object[] { Map("id", 10401, "hardRank", 19, "grantRank", "0", "softRank", "184467440737095516169999982", "effectiveRank", enormous, "canCast", true, "canInvest", true,
                "currentEffectLines", new object[] { "真实服务器效果" }, "nextEffectLines", new object[] { "下一真实效果" },
                "nextEffectiveRank", "184467440737095516170000002", "nextRequiredLevel", 20, "nextChangeLines", new object[] { "变化后的真实效果", "其余效果不变" }, "nativeModifierLines", new object[] { "原生加成" },
                "trainingSynergies", new object[] { Map("sourceId", 10402, "sourceName", "爆炎术", "effect", "damage", "effectLabel", "伤害倍率",
                    "perPointValue", 100, "unit", "percent_bp", "investedHardPoints", 3, "contributionValue", 300, "equipmentApplies", false) }) },
            "result", Map("hardCap", "20", "playerCap", "100", "petCap", "80", "unspentPoints", "2", "formulaVersion", "5")));
        var observed = api.TestObserveEndgame("endgame.skills");
        Check((bool)observed["available"] && (int)observed["unspentPoints"] == 2, "growth observation publishes authoritative points");
        var row = Object(Rows(observed["skillRanks"])[0]);
        Check((string)row["effectiveRank"] == enormous && (int)row["hardRank"] == 19, "agent exact arbitrary integral rank and hard split");
        Check(Rows(row["currentEffectLines"]).Count == 1 && Rows(row["nextEffectLines"]).Count == 1, "agent exposes server effect comparisons");
        var synergy = Object(Rows(row["trainingSynergies"])[0]);
        Check((string)row["nextEffectiveRank"] == "184467440737095516170000002" && (int)row["nextRequiredLevel"] == 20 && Rows(row["nextChangeLines"]).Count == 2 && Rows(row["nativeModifierLines"]).Count == 1, "agent receives exact next rank and separate native modifiers");
        Check((int)synergy["sourceId"] == 10402 && (string)synergy["sourceName"] == "爆炎术" && (int)synergy["perPointValue"] == 100 &&
            (int)synergy["investedHardPoints"] == 3 && (int)synergy["contributionValue"] == 300 && !(bool)synergy["equipmentApplies"], "agent receives typed hard-only synergy source and contribution");
        Check(EndAct(api, "skill.upgrade", Map("expectedRevision", "1", "skillId", 10401)).Code == "endgame.stale_revision", "skill investment requires observed revision");
        Check(EndAct(api, "skill.upgrade", Map("expectedRevision", TestRevision, "skillId", 10402)).Code == "skill.upgrade.unavailable", "unobserved rank cannot invest");
        var receipt = EndAct(api, "skill.upgrade", Map("expectedRevision", TestRevision, "skillId", 10401), "stable-skill-point");
        Check(receipt.State == GameActionState.Sent && native.Last.Op == "skill.upgrade" && native.Last.Mutates, "agent investment uses mutation receipt route");
        var args = SentArguments(native);
        Check(Convert.ToInt32(args["skillId"]) == 10401 && (string)args["expectedRevision"] == TestRevision, "skill ID and revision encoded exactly");
        Check(EndAct(api, "skill.upgrade", Map("expectedRevision", TestRevision, "skillId", 10401)).Code == "action.unavailable", "uncertain skill investment blocks double click");
        var request = native.Last;
        ApplyReceipt(native, request, false, "storage");
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Unknown && native.Endgame.Pending, "skill storage result retains same request");
        EndAct(api, "retry", Map("expectedRequestId", request.RequestID));
        Check(native.Last.Json == request.Json && native.Retries == 1, "skill retry preserves nonce and exact fields");
        ApplyReceipt(native, request);
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Completed && native.Endgame.UnspentSkillPoints == 2, "receipt settles investment without optimistic point mutation");
        native.Endgame.Reset();
        Check(!(bool)api.TestObserveEndgame("endgame.skills")["available"] && !native.Endgame.HasSkillRanks, "disconnect clears all skill rank authority");
    }
    private static void EndgameDiscoveryAndValidation()
    {
        var api = Setup(); int endgameCount = 0;
        foreach (var definition in api.GetActions()) if (definition.Name.StartsWith("endgame.", StringComparison.Ordinal))
        { endgameCount++; Check(!definition.Available && definition.UnavailableReason.Length > 0, "missing owner explicitly disables " + definition.Name); }
        Check(endgameCount == 16, "all endgame native operations are discoverable without owner");
        Check(!(bool)api.TestObserveEndgame("endgame")["available"], "missing endgame owner observes unavailable");
        EndgameNative native; api = SetupEndgame(out native, false);
        Check(EndAct(api, "refresh").Code == "action.unavailable" && native.Sent.Count == 0, "unnegotiated refresh never sends");
        Negotiate(native);
        Check(Definition(api, "endgame.refresh").Available && !Definition(api, "endgame.socket.insert").Available, "read requests precede complete inventory snapshot");
        PublishItems(native);
        var definitionInsert = Definition(api, "endgame.socket.insert");
        Check(Convert.ToInt32(Property(definitionInsert, "socket")["minimum"]) == 0 && Convert.ToInt32(Property(definitionInsert, "socket")["maximum"]) == 4, "schema bounds native zero-based sockets");
        Check((string)Property(definitionInsert, "targetUid")["type"] == "string" && Convert.ToInt32(Property(definitionInsert, "targetUid")["maxLength"]) == 19 && (string)Property(definitionInsert, "targetUid")["pattern"] == "^[1-9][0-9]{0,18}$", "schema advertises exact positive string UIDs");
        var tierSchema = Property(Definition(api, "endgame.rift.start"), "tier");
        Check(Convert.ToInt32(tierSchema["minimum"]) == 1 && Convert.ToInt32(tierSchema["maximum"]) == 150, "schema bounds rift tiers");
        var arraySchema = Property(Definition(api, "endgame.cube.quote"), "itemUids");
        Check(Convert.ToInt32(arraySchema["maxItems"]) == 5 && (bool)arraySchema["uniqueItems"] && (string)Object(arraySchema["items"])["type"] == "string", "schema advertises bounded unique ordered UID array");
        Check((bool)Property(Definition(api, "endgame.rift.leave"), "confirmLeave")["const"], "leave schema requires explicit true confirmation");
        Check(Rows(Property(Definition(api, "endgame.catalog.request"), "catalog")["enum"]).Count == 4, "catalog schema enumerates four native catalogs");
        Check(!(bool)definitionInsert.Arguments["additionalProperties"], "endgame action schemas reject unknown fields");
        Check(Act(api, "endgame.refresh").Code == "arguments.invalid", "endgame requires stable caller request ID");
        Check(EndAct(api, "refresh", Map("invented", true)).Code == "arguments.invalid", "unknown action argument never sends");
        foreach (object value in new object[] { 9007199254740993L, "0", "01", "-1", "+1", "9223372036854775808", "9007199254740993.0" })
        { var args = InsertArgs(); args["targetUid"] = value; Check(EndAct(api, "socket.insert", args).Code == "arguments.invalid", "invalid or numeric UID rejected: " + value); }
        foreach (object value in new object[] { -1, 5, 1.0000000000000002 })
            Check(EndAct(api, "socket.insert", InsertArgs(socket: value)).Code == "arguments.invalid", "socket range/integrality rejected: " + value);
        var stale = InsertArgs(); stale["expectedRevision"] = "1";
        Check(EndAct(api, "socket.insert", stale).Code == "endgame.stale_revision", "stale inventory revision cannot mutate");
        Check(EndAct(api, "socket.insert", InsertArgs("9007199254740888")).Code == "endgame.item_stale", "missing UID cannot mutate");
        Check(EndAct(api, "socket.insert", InsertArgs("9007199254740998")).Code == "endgame.item_stale", "warehouse item cannot become socket target");
        Check(EndAct(api, "socket.insert", InsertArgs(socket: 1)).Code == "endgame.socket_changed", "occupied socket cannot insert");
        Check(EndAct(api, "socket.insert", InsertArgs(socket: 2)).Code == "endgame.socket_unavailable", "unopened socket cannot insert");
        Check(EndAct(api, "socket.remove", Map("expectedRevision", TestRevision, "targetUid", EquipmentUID, "socket", 0)).Code == "endgame.socket_changed", "empty socket cannot extract");
        Check(native.Sent.Count == 0, "validation never reaches native send");
        native.Endgame.NoteNativeInventoryPublished();
        Check(!Definition(api, "endgame.socket.insert").Available && !(bool)api.TestObserveEndgame("endgame")["inventorySnapshotCurrent"],
            "same-template native replacement withdraws stale sidecar action authority and exposes freshness");
        Check(EndAct(api, "socket.insert", InsertArgs()).Code == "action.unavailable" && native.Sent.Count == 0,
            "old UID cannot be submitted between native inventory and complete sidecar publication");
        PublishItems(native);
        Check(Definition(api, "endgame.socket.insert").Available && (bool)api.TestObserveEndgame("endgame")["inventorySnapshotCurrent"],
            "complete sidecar restores common UI and plugin item authority");
        PublishItems(native, locked: true);
        Check(EndAct(api, "socket.insert", InsertArgs()).Code == "endgame.item_locked", "locked item cannot mutate");
        Check(Act(api, "inventory.sort", Map("tab", new object[] { "1" })).Code == "arguments.invalid", "ordered-array exception cannot reach unrelated native actions");
    }
    private static void EndgameReceiptsAndRetry()
    {
        EndgameNative native; var api = SetupEndgame(out native);
        var receipt = EndAct(api, "socket.insert", InsertArgs(rune: SecondRuneUID), "stable-operation");
        var command = native.Last; var sent = SentArguments(native);
        Check(receipt.State == GameActionState.Sent && !(bool)receipt.Data["serverOutcomeKnown"] && native.Endgame.Pending, "native submission never invents mutation completion");
        Check((string)sent["targetUid"] == EquipmentUID && (string)sent["runeUid"] == SecondRuneUID && (string)sent["expectedRevision"] == TestRevision, "wire preserves large UID and revision decimal strings exactly");
        Check(command.RequestID.Length == 64 && (string)receipt.Data["serverRequestId"] == command.RequestID, "receipt exposes full deterministic server request ID");
        var same = EndAct(api, "socket.insert", InsertArgs(rune: SecondRuneUID, socket: 0m), "stable-operation");
        Check(same.ActionId == receipt.ActionId && native.Sent.Count == 1, "equivalent numeric retry returns same action without resend");
        Check(EndAct(api, "socket.insert", InsertArgs(), "stable-operation").Code == "request_id.conflict" && native.Sent.Count == 1, "changed UID with same caller ID conflicts");
        Check(!api.CancelAction(receipt.ActionId), "submitted endgame mutation cannot be cancelled locally");
        Check(EndAct(api, "socket.remove", Map("expectedRevision", TestRevision, "targetUid", EquipmentUID, "socket", 1)).Code == "action.unavailable", "uncertain mutation blocks a second mutation");
        ApplyReceipt(native, command, kind: "rift.claim");
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Sent && native.Endgame.Pending && native.Endgame.ResponseFor(command.RequestID) == null, "wrong-kind success cannot clear mutation or complete adapter action");
        ApplyReceipt(native, command, false, "materials", "rift.claim");
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Sent && native.Endgame.Pending, "wrong-kind rejection cannot settle pending mutation");
        ApplyReceipt(native, new EndgameRequest("socket.insert"));
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Sent, "unrelated authoritative response cannot settle action");
        foreach (string transient in new[] { "pending", "storage", "storage_unavailable", "busy" })
        {
            ApplyReceipt(native, command, false, transient);
            var uncertain = api.GetAction(receipt.ActionId);
            Check(uncertain.State == GameActionState.Unknown && uncertain.Code == "endgame.server." + transient && !(bool)uncertain.Data["serverOutcomeKnown"] && native.Endgame.Pending,
                "transient " + transient + " retains immutable pending request");
        }
        Check(EndAct(api, "retry", Map("expectedRequestId", "wrong-request")).Code == "endgame.pending_changed" && native.Retries == 0, "retry requires exact observed native pending ID");
        var retry = EndAct(api, "retry", Map("expectedRequestId", command.RequestID));
        Check(native.Retries == 1 && ReferenceEquals(native.Last, command) && native.Last.Json == command.Json && retry.State == GameActionState.Unknown, "explicit retry resends identical immutable command; transient response remains uncertain");
        ApplyReceipt(native, command);
        var completed = api.GetAction(receipt.ActionId);
        Check(completed.State == GameActionState.Completed && (bool)completed.Data["serverOutcomeKnown"] && (string)completed.Data["completionScope"] == "server" && (string)Object(completed.Data["result"])["status"] == "saved", "matching authoritative receipt establishes server completion and result");
        Check(api.GetAction(retry.ActionId).State == GameActionState.Completed && !native.Endgame.Pending, "original and explicit retry share authoritative result");
        Check(EndAct(api, "socket.insert", InsertArgs(rune: SecondRuneUID), "stable-operation").ActionId == receipt.ActionId && native.Sent.Count == 2, "settled idempotent retry does not reexecute");

        api = SetupEndgame(out native); receipt = EndAct(api, "socket.insert", InsertArgs()); command = native.Last;
        ApplyReceipt(native, command, false, "materials");
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Rejected && (bool)api.GetAction(receipt.ActionId).Data["serverOutcomeKnown"] && !native.Endgame.Pending, "permanent server rejection settles without optimistic state change");
        Check(native.Endgame.Items.Count == 5 && native.Endgame.Items[0].Sockets[0].ItemID == 0, "receipt handling never fabricates equipment inventory mutation");

        api = SetupEndgame(out native); receipt = EndAct(api, "socket.insert", InsertArgs(), "reconnect-operation"); command = native.Last;
        native.Endgame.Reset();
        Check(api.GetAction(receipt.ActionId).Code == "endgame.session_changed" && native.Endgame.ResponseFor(command.RequestID) == null, "generation reset clears response history and makes outstanding receipt uncertain");
        Negotiate(native); PublishItems(native);
        Check(EndAct(api, "socket.insert", InsertArgs(), "reconnect-operation").ActionId == receipt.ActionId && native.Sent.Count == 1, "reconnect never silently replays retained request");
        native.Endgame.RestoreUncertainRequest(command);
        Check(EndAct(api, "retry", Map("expectedRequestId", command.RequestID)).State == GameActionState.Sent && native.Last.Json == command.Json, "restored native pending request requires explicit same-command retry");
        api = SetupEndgame(out native); receipt = EndAct(api, "refresh", id: "deterministic-wire-id"); string firstWireID = native.Last.RequestID;
        api = SetupEndgame(out native); EndAct(api, "refresh", id: "deterministic-wire-id");
        Check(native.Last.RequestID == firstWireID, "server idempotency identity is stable across adapter instances");

        api = SetupEndgame(out native); receipt = EndAct(api, "refresh"); command = native.Last;
        Apply(native, Map("kind", "snapshot", "ok", true, "requestId", command.RequestID, "revision", TestRevision, "snapshotId", "two-chunk", "chunk", 0, "chunks", 2,
            "items", new object[] { EndgameItem(EquipmentUID, 1001, "bag", 2, 0) }));
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Sent && native.Endgame.Items.Count == 5 && !Definition(api, "endgame.socket.insert").Available, "first snapshot chunk cannot complete action, replace view, or permit stale mutation");
        Apply(native, Map("kind", "snapshot", "ok", true, "requestId", command.RequestID, "revision", TestRevision, "snapshotId", "two-chunk", "chunk", 1, "chunks", 2,
            "items", new object[] { EndgameItem(FirstRuneUID, 1900028001, "bag", 1, 1) }));
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Completed && native.Endgame.Items.Count == 2, "complete ordered snapshot publishes before refresh completes");
        api = SetupEndgame(out native); receipt = EndAct(api, "socket.insert", InsertArgs()); command = native.Last;
        const string advancedRevision = "9007199254740997";
        Apply(native, Map("kind", "socket.insert", "ok", true, "requestId", command.RequestID, "revision", advancedRevision));
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Completed && !Definition(api, "endgame.socket.insert").Available, "advanced receipt revision cannot reuse older complete inventory for next mutation");
        Check((string)api.TestObserveEndgame("endgame")["snapshotRevision"] == TestRevision && (string)api.TestObserveEndgame("endgame")["expectedRevision"] == advancedRevision, "snapshot and current revision divergence is explicit");
        PublishItems(native, advancedRevision);
        Check(Definition(api, "endgame.socket.insert").Available && (string)api.TestObserveEndgame("endgame")["snapshotRevision"] == advancedRevision, "new complete snapshot restores mutation availability");

        api = SetupEndgame(out native); receipt = EndAct(api, "refresh");
        string oldSnapshotID = (string)api.TestObserveEndgame("endgame")["snapshotId"];
        PublishItems(native);
        string newSnapshotID = (string)api.TestObserveEndgame("endgame")["snapshotId"];
        Check(newSnapshotID.Length > 0 && newSnapshotID != oldSnapshotID && api.GetAction(receipt.ActionId).State == GameActionState.Sent, "actual-server uncorrelated snapshot publishes visible identity without invented action completion");
        api.TestExpireEndgame(receipt.ActionId);
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Sent && api.GetAction(receipt.ActionId).Code == "endgame.snapshot_unconfirmed", "uncorrelated snapshot read remains sent after observation deadline");
        api = SetupEndgame(out native); receipt = EndAct(api, "socket.insert", InsertArgs()); command = native.Last;
        api.TestExpireEndgame(receipt.ActionId);
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Unknown && native.Endgame.Pending, "mutation deadline preserves uncertainty and immutable pending request");
        ApplyReceipt(native, command);
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Completed, "late authoritative mutation receipt settles after local deadline");
        api = SetupEndgame(out native); native.AcceptSend = false;
        Check(EndAct(api, "refresh").Code == "endgame.send_unconfirmed" && !native.Endgame.Pending, "native pre-admission send failure does not invent server rejection");
        native.AcceptSend = true; native.FailAfterBegin = true;
        receipt = EndAct(api, "socket.insert", InsertArgs()); command = native.Last;
        Check(receipt.State == GameActionState.Unknown && native.Endgame.Pending, "write uncertainty after native admission retains pending operation");
        native.FailAfterBegin = false; ApplyReceipt(native, command);
        Check(api.GetAction(receipt.ActionId).State == GameActionState.Completed, "real later receipt wins uncertain transport outcome");
        for (int i = 0; i < 129; i++) Apply(native, Map("kind", "rift.state", "ok", true, "requestId", "bounded-history-" + i));
        Check(native.Endgame.ResponseFor("bounded-history-0") == null && native.Endgame.ResponseFor("bounded-history-128") != null, "authoritative response retention is bounded to 128 IDs");
    }
    private static void EndgameCubeAndEquipment()
    {
        EndgameNative native; var api = SetupEndgame(out native);
        Check(EndAct(api, "cube.quote", Map("expectedRevision", TestRevision, "recipeId", 11001)).Code == "endgame.recipe_unobserved", "unobserved recipe cannot be quoted");
        PublishRecipes(native);
        foreach (object array in new object[] { new object[] { FirstRuneUID, FirstRuneUID }, new object[] { 9007199254740994L }, new object[] { "0" }, new object[] { "01" }, new object[] { new object[] { FirstRuneUID } }, new object[] { "1", "2", "3", "4", "5", "6" } })
            Check(EndAct(api, "cube.quote", Map("expectedRevision", TestRevision, "recipeId", 11003, "itemUids", array)).Code == "arguments.invalid", "invalid ordered rune array rejected");
        Check(EndAct(api, "cube.quote", Map("expectedRevision", TestRevision, "recipeId", 11002)).Code == "arguments.invalid", "target-required recipe requires observed target");
        Check(EndAct(api, "cube.quote", Map("expectedRevision", TestRevision, "recipeId", 11001, "targetUid", EquipmentUID)).Code == "arguments.invalid", "target-free recipe refuses unrelated target");
        Check(EndAct(api, "cube.quote", Map("expectedRevision", TestRevision, "recipeId", 11002, "targetUid", EquipmentUID, "itemUids", new object[] { FirstRuneUID })).Code == "endgame.rune_stale", "socket reorder only accepts installed rune UIDs");
        var ordered = new object[] { SecondRuneUID, FirstRuneUID };
        var requestArgs = Map("expectedRevision", TestRevision, "recipeId", 11003, "itemUids", ordered);
        var quoted = EndAct(api, "cube.quote", requestArgs, "ordered-quote"); var command = native.Last;
        ordered[0] = "1";
        var sent = SentArguments(native);
        Check((string)Rows(sent["itemUids"])[0] == SecondRuneUID && (string)Rows(sent["itemUids"])[1] == FirstRuneUID && !command.Mutates, "quote preserves frozen rune order and never mutates");
        var retryArgs = Map("expectedRevision", TestRevision, "recipeId", 11003m, "itemUids", new object[] { SecondRuneUID, FirstRuneUID });
        Check(EndAct(api, "cube.quote", retryArgs, "ordered-quote").ActionId == quoted.ActionId && native.Sent.Count == 1, "equivalent ordered-array retry is idempotent");
        retryArgs["itemUids"] = new object[] { FirstRuneUID, SecondRuneUID };
        Check(EndAct(api, "cube.quote", retryArgs, "ordered-quote").Code == "request_id.conflict", "different rune ordering conflicts under same request ID");
        ApplyReceipt(native, command, kind: "rift.state");
        Check(native.Endgame.QuotePending && api.GetAction(quoted.ActionId).State == GameActionState.Sent, "wrong-kind quote response cannot clear preview wait");
        PublishQuote(native, command);
        Check(api.GetAction(quoted.ActionId).State == GameActionState.Completed && !native.Endgame.Pending, "server quote completes only read operation");
        Check(EndAct(api, "cube.execute", Map("expectedRevision", TestRevision, "quoteId", "wrong")).Code == "endgame.quote_stale", "execution requires exact current quote ID");
        var execute = EndAct(api, "cube.execute", Map("expectedRevision", TestRevision, "quoteId", "quote-current")); sent = SentArguments(native);
        Check(execute.State == GameActionState.Sent && native.Last.Mutates && (string)sent["quoteId"] == "quote-current" && !sent.ContainsKey("itemUids") && !sent.ContainsKey("recipeId"), "execution sends only frozen server quote identity and revision");
        ApplyReceipt(native, native.Last);
        Check(native.Endgame.Quote == null && api.GetAction(execute.ActionId).State == GameActionState.Completed && (string)api.GetAction(execute.ActionId).Data["serverKind"] == "cube.result", "actual server cube.result completes execution and invalidates quote");
        api = SetupEndgame(out native); PublishRecipes(native);
        EndAct(api, "cube.quote", Map("expectedRevision", TestRevision, "recipeId", 11001)); PublishQuote(native, native.Last, expiry: "2000-01-01T00:00:00Z");
        Check(EndAct(api, "cube.execute", Map("expectedRevision", TestRevision, "quoteId", "quote-current")).Code == "endgame.quote_stale" && native.Sent.Count == 1, "expired quote never executes");
        PublishItems(native);
        Check(!Definition(api, "endgame.cube.execute").Available, "new complete snapshot invalidates quote availability");

        api = SetupEndgame(out native);
        Check(EndAct(api, "equipment.equip", Map("expectedRevision", TestRevision, "targetUid", EquipmentUID)).Code == "equipment.binding_confirmation_required" && native.Equips == 0, "unbound endgame equipment requires explicit binding intent");
        Check(EndAct(api, "equipment.equip", Map("expectedRevision", TestRevision, "targetUid", EquipmentUID, "allowBind", true)).State == GameActionState.Sent && native.Equips == 1 && native.Equipped.UID == EquipmentUID, "equip routes real UID-bearing item to native owner");
        Check(EndAct(api, "equipment.unequip", Map("expectedRevision", TestRevision, "targetUid", EquippedUID)).State == GameActionState.Sent && native.Unequips == 1 && native.Unequipped.Slot == 25, "unequip preserves extended native slot25");
        native.AcceptEquipment = false;
        Check(EndAct(api, "equipment.unequip", Map("expectedRevision", TestRevision, "targetUid", EquippedUID)).Code == "endgame.native_rejected", "native equipment rejection is explicit");
        SocialClient.TradeActive = true;
        Check(EndAct(api, "equipment.equip", Map("expectedRevision", TestRevision, "targetUid", EquipmentUID, "allowBind", true)).Code == "inventory.trade_active", "native trade guard prevents equipment activation");
        SocialClient.TradeActive = false;
    }
    private static void EndgameRiftRouting()
    {
        EndgameNative native; var api = SetupEndgame(out native);
        Check(!Definition(api, "endgame.rift.start").Available, "rift must be observed before paid admission");
        PublishRift(native, phase: "closed");
        Check(!Definition(api, "endgame.rift.start").Available, "live native entrance interaction is required for fresh paid admission");
        int sentBeforeEntrance = native.Sent.Count;
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2)).Code == "action.unavailable" && native.Sent.Count == sentBeforeEntrance, "agent cannot bypass native entrance dialog");
        Act(api, "npc.interact", Map("entityId", "npc:1:3"));
        Check(Definition(api, "endgame.rift.start").Available, "native interaction captures configured entrance");
        PublishRift(native, phase: "reserved");
        foreach (object tier in new object[] { 0, 151, 1.0000000000000002 })
            Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", tier, "runId", "run-current")).Code == "arguments.invalid", "rift invalid tier rejected");
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2)).Code == "endgame.run_stale", "invitation cannot accidentally start a fresh paid run");
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 3, "runId", "run-current")).Code == "endgame.run_stale", "invitation tier is guarded");
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2, "runId", "run-old")).Code == "endgame.run_stale", "stale invitation ID cannot enter");
        EndAct(api, "rift.refresh");
        Check(native.Last.Op == "rift.state" && (string)SentArguments(native)["runId"] == "run-current", "rift refresh preserves invitation run through native authoritative read");
        var entered = EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2, "runId", "run-current"));
        Check(entered.State == GameActionState.Sent && (string)SentArguments(native)["runId"] == "run-current", "reentry preserves observed run identity");
        Check(!SentArguments(native).ContainsKey("npcEntityId"), "admitted-run reentry preserves its NPC-independent wire contract");
        ApplyReceipt(native, native.Last);
        Check((string)api.TestObserveEndgame("endgame.rift")["riftEntranceEntityId"] == "npc:1:3", "agent observation identifies actual current entrance");
        PublishRift(native, phase: "closed"); GameBootstrap.CurMapId = 8;
        Check(!Definition(api, "endgame.rift.start").Available, "map departure invalidates captured agent entry");
        GameBootstrap.CurMapId = 7;
        Check(!Definition(api, "endgame.rift.start").Available, "returning to the same map cannot revive a stale native dialog");
        Act(api, "npc.interact", Map("entityId", "npc:1:3")); PublishRift(native, phase: "reserved");
        Check(EndAct(api, "rift.leave", Map("expectedRevision", TestRevision, "runId", "run-current", "confirmLeave", false)).Code == "arguments.invalid", "leave requires explicit key-loss acknowledgement");
        Check(EndAct(api, "rift.leave", Map("expectedRevision", TestRevision, "runId", "run-old", "confirmLeave", true)).Code == "endgame.run_stale", "leave guards observed run");
        Check(EndAct(api, "rift.leave", Map("expectedRevision", TestRevision, "runId", "run-current", "confirmLeave", true)).State == GameActionState.Sent && native.Last.Op == "rift.leave", "confirmed leave uses native command");
        ApplyReceipt(native, native.Last);
        Check(!Definition(api, "endgame.rift.claim").Available, "claim requires authoritative claimable flag");
        PublishRift(native, phase: "interrupted", claimable: true);
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2)).Code == "endgame.rift_unavailable", "compensation must resolve before new paid run");
        Check(EndAct(api, "rift.claim", Map("expectedRevision", TestRevision, "runId", "run-old")).Code == "endgame.run_stale", "claim guards exact compensated run");
        Check(EndAct(api, "rift.claim", Map("expectedRevision", TestRevision, "runId", "run-current")).State == GameActionState.Sent && native.Last.Op == "rift.claim", "compensation claims authoritative interrupted run");
        ApplyReceipt(native, native.Last);
        PublishRift(native, phase: "reward_pending");
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2)).Code == "endgame.rift_unavailable", "reward persistence pending blocks new run");
        PublishRift(native, phase: "closed");
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2, "runId", "run-current")).Code == "endgame.run_stale", "closed run cannot masquerade as invitation");
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 150)).State == GameActionState.Sent && !SentArguments(native).ContainsKey("runId"), "fresh paid admission omits obsolete run ID");
        Check((long)SentArguments(native)["npcEntityId"] == 3, "fresh paid admission sends selected native entity rather than static config ID");
        ApplyReceipt(native, native.Last);
        native.Endgame.Begin(new EndgameRequest("hello"));
        Apply(native, Map("kind", "hello", "ok", true, "capabilities", new object[] { "endgame.v1" }));
        PublishRift(native, phase: "reserved");
        Check(Definition(api, "endgame.rift.start").Available && EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 2, "runId", "run-current")).State == GameActionState.Sent && !SentArguments(native).ContainsKey("npcEntityId"), "invitation recovery stays available with legacy hello and no NPC context");
    }
    private static void EndgameRiftAdvance()
    {
        EndgameNative native; var api = SetupEndgame(out native);
        Check(!Definition(api, "endgame.rift.advance").Available, "advance requires an observed server floor");
        PublishRift(native);
        var definition = Definition(api, "endgame.rift.advance");
        Check(definition.Available && Convert.ToInt32(Property(definition, "floorIndex")["minimum"]) == 1, "advance advertises positive floor guard");
        foreach (object floor in new object[] { 0, -1, 1.5, "3" })
            Check(EndAct(api, "rift.advance", Map("expectedRevision", TestRevision, "runId", "run-current", "floorIndex", floor)).Code == "arguments.invalid", "invalid floor guard rejected");
        Check(EndAct(api, "rift.advance", Map("expectedRevision", TestRevision, "runId", "old-run", "floorIndex", 3)).Code == "endgame.floor_stale", "advance rejects unrelated run");
        Check(EndAct(api, "rift.advance", Map("expectedRevision", TestRevision, "runId", "run-current", "floorIndex", 2)).Code == "endgame.floor_stale", "advance rejects already passed floor");
        Check(EndAct(api, "rift.advance", Map("expectedRevision", TestRevision, "runId", "run-current", "floorIndex", 3, "tier", 150)).Code == "arguments.invalid", "advance cannot redefine difficulty");
        Check(EndAct(api, "rift.advance", Map("expectedRevision", "1", "runId", "run-current", "floorIndex", 3)).Code == "endgame.stale_revision", "advance preserves inventory revision guard");
        var advanced = EndAct(api, "rift.advance", Map("expectedRevision", TestRevision, "runId", "run-current", "floorIndex", 3));
        var command = native.Last; var arguments = SentArguments(native);
        Check(advanced.State == GameActionState.Sent && command.Op == "rift.advance" && command.Mutates && (long)arguments["floorIndex"] == 3 && !arguments.ContainsKey("tier"), "advance uses immutable native guarded mutation without changing tier");
        Check(!Definition(api, "endgame.rift.advance").Available, "pending advance prevents a second floor skip");
        string frozen = command.Json;
        EndAct(api, "retry", Map("expectedRequestId", command.RequestID));
        Check(ReferenceEquals(command, native.Last) && native.Last.Json == frozen, "advance retry preserves original floor and request ID");
        ApplyReceipt(native, command);
        Check(api.GetAction(advanced.ActionId).State == GameActionState.Completed, "matching advance receipt establishes server completion");
        PublishRift(native, transitioning: true);
        Check(!Definition(api, "endgame.rift.advance").Available, "transitioning floor disables another advance");
        PublishRift(native, phase: "boss");
        Check(!Definition(api, "endgame.rift.advance").Available, "guardian phase cannot be skipped");
        PublishRift(native, canAdvance: false);
        Check(!Definition(api, "endgame.rift.advance").Available, "server exit eligibility controls advance availability");
        Act(api, "npc.interact", Map("entityId", "npc:1:3"));
        PublishRift(native, phase: "closed", unlockedTier: 80);
        Check(EndAct(api, "rift.start", Map("expectedRevision", TestRevision, "tier", 81)).Code == "endgame.tier_locked", "fresh entry respects observed tier unlock ceiling");
    }
    private static void EndgameProjectionContracts()
    {
        EndgameNative native; var api = SetupEndgame(out native); PublishRecipes(native); PublishRift(native);
        var status = api.TestObserveEndgame("endgame");
        Check((bool)status["available"] && (string)status["expectedRevision"] == TestRevision && (int)status["itemCount"] == 5, "status projects negotiated authoritative revision and item count");
        Check(Rows(status["materials"]).Count == 1 && (int)Object(Rows(status["materials"])[0])["count"] == 50, "material wallet count remains authoritative");
        var formation = Object(Rows(status["activeFormations"])[0]);
        Check((int)Rows(formation["runes"])[0] == 1900028002 && (int)Rows(formation["runes"])[1] == 1900028001, "active formation rune order preserved");
        Check(Rows(status["riftEntrances"]).Count == 1 && (int)Object(Rows(status["riftEntrances"])[0])["mapId"] == 7 && (int)Object(Rows(status["riftEntrances"])[0])["npcId"] == 83, "agent discovers server-configured entrance identity");
        Check(status["riftEntranceEntityId"] == null, "observation never invents an active native entrance");
        Check(!Object(status["rift"]).ContainsKey("auras"), "summary omits marker bulk while dedicated rift view exposes it");
        var items = api.TestObserveEndgame("endgame.items", offset: 1, limit: 2);
        Check((int)items["total"] == 5 && (int)items["count"] == 2 && (int)items["nextOffset"] == 3 && (bool)items["truncated"], "item paging returns bounded rows and continuation offset");
        Check((string)Object(Rows(items["items"])[0])["uid"] == FirstRuneUID && (string)Object(Rows(items["items"])[1])["uid"] == SecondRuneUID, "item projection preserves large decimal UID strings and snapshot ordering");
        var filter = api.TestObserveEndgame("endgame.items", offset: 1, limit: 1, location: "bag", tab: 1);
        Check((int)filter["total"] == 2 && (string)Object(Rows(filter["items"])[0])["uid"] == SecondRuneUID && filter["nextOffset"] == null, "offset counts only matching items");
        var byUID = api.TestObserveEndgame("endgame.items", uid: EquipmentUID);
        var equipment = Object(Rows(byUID["items"])[0]);
        Check((int)byUID["total"] == 1 && (int)Object(Rows(equipment["sockets"])[0])["index"] == 0 && (int)Object(Rows(equipment["sockets"])[1])["index"] == 1, "UID filter retains ordered socket projections");
        Check((int)api.TestObserveEndgame("endgame.items", name: "absent")["total"] == 0, "name filter reports empty matched page");
        var beyond = api.TestObserveEndgame("endgame.items", offset: 4096, limit: 512);
        Check((int)beyond["count"] == 0 && beyond["nextOffset"] == null && !(bool)beyond["truncated"], "offset beyond snapshot returns terminal empty page");
        var catalog = api.TestObserveEndgame("endgame.catalog", offset: 1, limit: 1, catalog: "recipes");
        Check((bool)catalog["loaded"] && !(bool)catalog["hasMore"] && (int)catalog["total"] == 3 && (int)Object(Rows(catalog["rows"])[0])["id"] == 11002 && (int)catalog["nextOffset"] == 2, "catalog observation pages loaded native rows separately from server cursor");
        var firstRecipe = api.TestObserveEndgame("endgame.catalog", limit: 1, catalog: "recipes");
        Check((string)Object(Rows(firstRecipe["rows"])[0])["money"] == EquipmentUID, "catalog money preserves exact large string amount");
        var rift = Object(api.TestObserveEndgame("endgame.rift", limit: 1)["rift"]);
        Check((string)Object(Rows(rift["members"])[0])["charId"] == EquipmentUID && (string)Object(Rows(rift["auras"])[0])["entityId"] == "4294967295" && (string)Object(Rows(rift["auras"])[0])["targetEntityId"] == "monster:1:-1", "rift member and unsigned native aura identities remain exact and scene-qualified");
        Check((int)rift["globePickupRadius"] == 75 && (int)Object(Rows(rift["globes"])[0])["x"] == 321, "rift observes actual globe routing coordinates and pickup radius");
        Check((int)rift["tier"] == 2 && (int)rift["floorIndex"] == 3 && (string)rift["seed"] == "18446744073709551615" && (int)rift["generatorVersion"] == 1, "difficulty, floor and exact uint64 seed remain independent");
        Check((bool)rift["canAdvance"] && !(bool)rift["transitioning"] && (int)rift["exitX"] == 720 && (int)rift["maxTier"] == 150 && (int)rift["unlockedTier"] == 150, "exit and difficulty limits project authoritative server fields");
        Check((string)Rows(Object(Rows(rift["auras"])[0])["affixes"])[0] == "烈焰" && (int)Object(Rows(rift["telegraphs"])[0])["remainingMS"] == 1500 && (string)Object(Rows(rift["telegraphs"])[0])["shape"] == "circle", "readable affixes and bounded danger geometry project alongside combat targets");
        Check(!Object(status["rift"]).ContainsKey("telegraphs"), "summary omits telegraph bulk");
        Check((int)rift["telegraphsTotal"] == 2 && (bool)rift["telegraphsTruncated"] && (int)rift["nextOffset"] == 1, "telegraph paging exposes total/truncation independently of other marker lists");
        var laterTell = Object(api.TestObserveEndgame("endgame.rift", offset: 1, limit: 1)["rift"]);
        Check(Rows(laterTell["auras"]).Count == 0 && Rows(laterTell["globes"]).Count == 0 && Rows(laterTell["telegraphs"]).Count == 1 && (string)Object(Rows(laterTell["telegraphs"])[0])["shape"] == "line", "paging continues for danger markers after other lists end");
        var emptyMarkers = Object(api.TestObserveEndgame("endgame.rift", offset: 2, limit: 1)["rift"]);
        Check(Rows(emptyMarkers["auras"]).Count == 0 && Rows(emptyMarkers["globes"]).Count == 0 && Rows(emptyMarkers["telegraphs"]).Count == 0 && emptyMarkers["nextOffset"] == null, "marker offset safely reaches terminal page");
        EndAct(api, "socket.insert", InsertArgs()); status = api.TestObserveEndgame("endgame");
        var pending = Object(status["pendingRequest"]);
        Check((bool)status["pending"] && (string)pending["requestId"] == native.Last.RequestID && (string)Object(pending["arguments"])["targetUid"] == EquipmentUID, "pending projection exposes exact immutable command required for safe retry");
        var frozenItems = Rows(items["items"]); native.Endgame.Reset();
        Check(frozenItems.Count == 2 && (string)Object(frozenItems[1])["uid"] == SecondRuneUID && !(bool)api.TestObserveEndgame("endgame")["available"], "returned snapshot survives reset independently while new observations become unavailable");
        api = SetupEndgame(out native);
        EndAct(api, "catalog.request", Map("catalog", "runes")); var command = native.Last;
        Check(command.Op == "catalog" && command.Catalog == "runes" && command.Cursor == "", "first catalog request uses native empty cursor");
        Check(EndAct(api, "catalog.request", Map("catalog", "runes")).Code == "endgame.catalog_pending", "duplicate pending catalog request blocked");
        Apply(native, Map("kind", "catalog.runes", "ok", true, "requestId", command.RequestID, "nextCursor", "next-page", "runes", new object[] { Map("itemId", 1900028001, "name", "Synthetic rune") }));
        Check(EndAct(api, "catalog.request", Map("catalog", "runes", "cursor", "stale-page")).Code == "endgame.catalog_stale", "catalog cursor stale guard prevents unrelated page");
        EndAct(api, "catalog.request", Map("catalog", "runes"));
        Check(native.Last.Cursor == "next-page", "omitted cursor uses authoritative native continuation");
    }

    private sealed class EndgameNative : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public readonly List<EndgameRequest> Sent = new List<EndgameRequest>();
        public int Retries, Equips, Unequips;
        public bool AcceptSend = true, AcceptEquipment = true, FailAfterBegin;
        public EndgameItemView Equipped, Unequipped;
        public EndgameRequest Last => Sent[Sent.Count - 1];
        public bool SendEndgame(EndgameRequest request)
        {
            if (!AcceptSend || !Endgame.Begin(request)) return false;
            Sent.Add(request);
            if (FailAfterBegin) { Endgame.NoteTransportFailure("Synthetic uncertain write", request); return false; }
            return true;
        }
        public bool RetryEndgame()
        {
            if (!Endgame.Pending) return false;
            Retries++; Sent.Add(Endgame.PendingRequest); return AcceptSend;
        }
        public bool EquipEndgameItem(EndgameItemView item) { Equips++; Equipped = item; return AcceptEquipment; }
        public bool UnequipEndgameItem(EndgameItemView item) { Unequips++; Unequipped = item; return AcceptEquipment; }
    }

    private static PluginGameApi SetupEndgame(out EndgameNative native, bool negotiated = true, bool snapshot = true)
    {
        var api = Setup(); native = new EndgameNative(); api.TestSession.EndgameClientForPlugins = native;
        EndgamePanel.WorldState = native.Endgame; GameBootstrap.CurMapId = 7;
        var source = GameBootstrap.I.Window.Frame.NpcActors[3].State;
        source.DisplayName = "八仙使者"; source.Script = "Nbaxian";
        if (negotiated) Negotiate(native);
        if (negotiated && snapshot) PublishItems(native);
        return api;
    }
    private static void Apply(EndgameNative native, Dictionary<string, object> response)
    { native.Endgame.Apply(EndgameResponse.Parse(BoundedJson.Serialize(response))); }
    private static void Negotiate(EndgameNative native)
    {
        native.Endgame.Begin(new EndgameRequest("hello"));
        Apply(native, Map("kind", "hello", "ok", true, "capabilities", new object[] { "endgame.v1", "endgame.rift-entrance.v1" },
            "riftEntrances", new object[] { Map("mapId", 7, "npcId", 83, "npc", "八仙使者", "script", "Nbaxian") }));
    }
    private static Dictionary<string, object> EndgameItem(string uid, int item, string location, int tab, int slot, bool locked = false)
    { return Map("uid", uid, "itemId", item, "location", location, "tab", tab, "slot", slot, "revision", TestRevision, "name", "Synthetic item", "locked", locked); }
    private static void PublishItems(EndgameNative native, string revision = TestRevision, bool locked = false)
    {
        var equipment = EndgameItem(EquipmentUID, 1001, "bag", 2, 0, locked);
        equipment["socketCount"] = 2;
        equipment["sockets"] = new object[] { Map("index", 0, "itemId", 0), Map("index", 1, "itemId", 1900028002, "uid", "9007199254740997") };
        Apply(native, Map("kind", "snapshot", "ok", true, "snapshotId", Guid.NewGuid().ToString("N"), "revision", revision, "chunk", 0, "chunks", 1,
            "items", new object[] { equipment,
                EndgameItem(FirstRuneUID, 1900028001, "bag", 1, 1), EndgameItem(SecondRuneUID, 1900028002, "bag", 1, 2),
                EndgameItem(EquippedUID, 1002, "equipped", 0, 25), EndgameItem("9007199254740998", 1003, "warehouse", 0, 5) },
            "materials", new object[] { Map("itemId", 1900028201, "name", "Synthetic material", "count", 50) },
            "formations", new object[] { Map("id", 1, "name", "Synthetic formation", "active", true, "runes", new object[] { 1900028002, 1900028001 }) }));
    }
    private static void PublishRecipes(EndgameNative native)
    {
        var request = new EndgameRequest("catalog", catalog: "recipes"); native.Endgame.Begin(request);
        Apply(native, Map("kind", "catalog.recipes", "ok", true, "requestId", request.RequestID, "recipes", new object[] {
            Map("id", 11001, "name", "Synthetic inscription", "operation", "rune.inscribe", "money", EquipmentUID),
            Map("id", 11002, "name", "Synthetic reorder", "operation", "socket.reorder", "targetRequired", true, "money", "100"),
            Map("id", 11003, "name", "Synthetic conversion", "operation", "rune.convert", "money", "100") }));
    }
    private static void PublishRift(EndgameNative native, string id = "run-current", string phase = "active", bool claimable = false, bool canAdvance = true, bool transitioning = false, int unlockedTier = 150)
    {
        Apply(native, Map("kind", "rift.state", "ok", true, "rift", Map("runId", id, "revision", TestRevision, "phase", phase, "tier", 2,
            "seed", "18446744073709551615", "generatorVersion", 1, "floorIndex", 3, "mapName", "Synthetic floor", "canAdvance", canAdvance, "transitioning", transitioning, "exitX", 720, "exitY", 960, "maxTier", 150, "unlockedTier", unlockedTier,
            "telegraphs", new object[] { Map("id", "tell-1", "entityId", "4294967295", "ability", "烈焰爆裂", "shape", "circle", "x", 400, "y", 500, "radius", 105, "remainingMS", 1500), Map("id", "tell-2", "entityId", "2", "ability", "裂地斩", "shape", "line", "x", 400, "y", 500, "endX", 800, "endY", 500, "radius", 38, "remainingMS", 1400) },
            "progress", 50, "progressMax", 100, "remainingSeconds", 90, "keyCount", 1, "claimable", claimable,
            "members", new object[] { Map("charId", EquipmentUID, "name", "Synthetic member", "level", 50) },
            "auras", new object[] { Map("entityId", "4294967295", "class", 3, "affixes", new object[] { "烈焰", "震环" }) },
            "globes", new object[] { Map("entityId", "4294967294", "x", 321, "y", 654, "value", 900) })));
    }
    private static IReadOnlyDictionary<string, object> Object(object value) => (IReadOnlyDictionary<string, object>)value;
    private static IReadOnlyList<object> Rows(object value) => (IReadOnlyList<object>)value;
    private static GameActionDefinition Definition(PluginGameApi api, string name)
    {
        foreach (var definition in api.GetActions()) if (definition.Name == name) return definition;
        throw new Exception("Missing action definition: " + name);
    }
    private static IReadOnlyDictionary<string, object> Property(GameActionDefinition definition, string key)
    { return Object(Object(definition.Arguments["properties"])[key]); }
    private static IReadOnlyDictionary<string, object> SentArguments(EndgameNative native)
    { return Object(BoundedJson.Parse(native.Last.Json)); }
    private static void PublishQuote(EndgameNative native, EndgameRequest request, string id = "quote-current", string revision = TestRevision, string expiry = "2099-01-01T00:00:00Z")
    {
        Apply(native, Map("kind", "cube.quote", "ok", true, "requestId", request.RequestID,
            "quote", Map("quoteId", id, "recipeId", 11001, "revision", revision, "expiresAt", expiry,
                "description", "Synthetic quote", "money", EquipmentUID,
                "materials", new object[] { Map("itemId", 1900028201, "name", "Synthetic material", "count", 10, "owned", 50) })));
    }
    private static void ApplyReceipt(EndgameNative native, EndgameRequest request, bool ok = true, string error = "", string kind = null)
    {
        Apply(native, Map("kind", kind ?? (ok && request.Op == "cube.execute" ? "cube.result" : request.Op), "ok", ok, "error", error, "requestId", request.RequestID, "revision", TestRevision,
            "result", Map("status", ok ? "saved" : error)));
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class PluginGameApi
    {
        // Only generic native-observation plumbing is substituted. The endgame projection
        // and all its pagination/filtering/DTO mapping execute production source above.
        private sealed class ObservationArguments
        {
            internal int Offset, Limit = 128;
            internal string UID, Location, Catalog, Name;
            internal int? Tab, ItemId, SkillId;
        }
        public void TestExpireEndgame(string actionId) { actions[actionId].Deadline = 0; }
        public IReadOnlyDictionary<string, object> TestObserveEndgame(string domain, int offset = 0, int limit = 128,
            string uid = null, string location = null, string catalog = null, string name = null, int? tab = null, int? itemId = null)
        {
            return new GameSnapshot(domain, TestScene, 1, ObserveEndgame(domain, new ObservationArguments {
                Offset = offset, Limit = limit, UID = uid, Location = location, Catalog = catalog, Name = name, Tab = tab, ItemId = itemId
            })).Data;
        }
        private static Dictionary<string, object> Values(params object[] pairs)
        { var result = new Dictionary<string, object>(); for (int i = 0; i < pairs.Length; i += 2) result.Add((string)pairs[i], pairs[i + 1]); return result; }
        private string BoundText(string text, int maximum = 512) => text == null ? "" : text.Length <= maximum ? text : text.Substring(0, maximum);
        private static bool MatchesName(string filter, string name) => filter == null || (name ?? "").IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0;
        private static List<object> CopyNumbers(IReadOnlyList<int> source, int maximum)
        { var rows = new List<object>(); for (int i = 0; i < source.Count && i < maximum; i++) rows.Add(source[i]); return rows; }
        private string EntityReference(string kind, int id) => kind + ":" + TestScene + ":" + id;
        private static Dictionary<string, object> CoordinateSystem() => Values("unit", "mapPixels", "origin", "topLeft");
    }
}
