using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.PluginApi;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class PluginGameApi
{
    private const int ActionRetention = 1024;
    private readonly Dictionary<string, PluginAction> actions = new Dictionary<string, PluginAction>(StringComparer.Ordinal);
    private readonly Dictionary<string, string> actionRequests = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Queue<string> actionOrder = new Queue<string>();
    private readonly HashSet<PluginAction> activeActions = new HashSet<PluginAction>();
    private readonly List<PluginAction> actionPollBuffer = new List<PluginAction>();

    private sealed class PluginAction
    {
        internal GameActionRequest Request;
        internal GameActionReceipt Receipt;
        internal long Scene;
        internal double Deadline;
        internal string Kind;
        internal PlayerController Player;
        internal long NavigationRevision;
        internal long CombatRevision;
        internal NetClient Native;
        internal long WriteSequence;
        internal int NativeId;
        internal object Target;
        internal float X, Y, Tolerance;
        internal IEndgameClient EndgameOwner;
        internal EndgameRequest EndgameRequest;
        internal long EndgameGeneration;
        internal bool EndgameWaiting;
        internal EndgameResponse EndgameResponseSeen;
        internal Task<Result> Pending;
        internal bool PendingSettled;
        internal CancellationTokenSource Cancellation;
    }

    private static double ActionNow => System.Diagnostics.Stopwatch.GetTimestamp() /
        (double)System.Diagnostics.Stopwatch.Frequency;
    private static ReferenceFirstWorldInitialFrame ActionFrame => GameBootstrap.I?.Window?.Frame;
    private static CombatWorld ActionCombat => ActionFrame?.CombatOwnerForIndependentRuntime;

    public IReadOnlyList<GameActionDefinition> GetActions()
    {
        RequireMainThread();
        RefreshScene();
        var session = Session;
        var frame = ActionFrame;
        var player = frame?.Player?.SourceControllerForIndependentRuntime;
        bool world = session != null && session.State == ClientSessionState.InWorld && frame != null;
        bool alive = world && player != null && !player.IsDead;
        bool mobile = alive && player.CanNavigateForPlugins;
        bool native = alive && NetClient.I != null && ActionCombat != null;
        bool dialog = world && DialogUI.VisibleS;
        string worldReason = "An active, built world session is required.";
        string actorReason = "A live native player and combat owner are required.";
        string mobileReason = "The native player is unavailable, dead, immobile, working, or riding another player.";
        var definitions = new List<GameActionDefinition>
        {
            ActionDefinition("movement.move", "Navigate through native collision/pathfinding in map pixels. Completion observes local arrival only. Cancelling stops only the owned path. Receipts and request IDs retain the last 1024 settled actions per API instance.", mobile, mobileReason,
                "x:number", "y:number", "tolerance?:number", "timeoutSeconds?:number"),
            ActionDefinition("movement.stop", "Stop native navigation, following, pending pickup and combat approach locally.", world && player != null, worldReason),
            ActionDefinition("target.select", "Select an observed scene-qualified player, monster, NPC or self without arming attack.", world, worldReason, "entityId:string"),
            ActionDefinition("target.clear", "Clear native selection and pending combat target locally.", world, worldReason),
            ActionDefinition("combat.attack", "Arm the native attack/approach loop for an observed hostile target. Running is local engagement; damage and death remain server-authored.", mobile && native, actorReason,
                "entityId:string", "timeoutSeconds?:number"),
            ActionDefinition("combat.stop", "Stop the native attack/skill queue and combat approach locally.", world && player != null, worldReason),
            ActionDefinition("skill.request", "Request a learned skill through the native skill-use port and native target/approach queue. Sent reports the observed gateway write, never combat success.", native, actorReason,
                "skillId:integer", "entityId?:string", "timeoutSeconds?:number"),
            ActionDefinition("skill.cast", "Invoke the native skill-use port directly at current/explicit target or map pixels. This does not navigate. Sent means only the real gateway task succeeded.", native, actorReason,
                "skillId:integer", "entityId?:string", "x?:integer", "y?:integer"),
            ActionDefinition("skill.cancel", "Cancel the native pending and queued skill intents. Already sent requests cannot be revoked.", native, actorReason),
            ActionDefinition("loot.pickup", "Use native WalkToPickup approach, ownership lock, distance and expiry. Sent is the correlated native pickup write; removal alone never proves loot acquisition.", mobile && NetClient.I != null, mobileReason,
                "entityId:string", "timeoutSeconds?:number"),
            ActionDefinition("inventory.use", "Activate an observed item through native inventory rules, cooldowns and special-item UI. Sent reports an actual write; otherwise completion is local activation only and may require native UI interaction.", world && NetClient.I != null, worldReason,
                "tab:integer", "slot:integer", "expectedItemId:integer"),
            ActionDefinition("inventory.sort", "Request a native inventory tab sort; later snapshots are authoritative.", world, worldReason, "tab:integer"),
            ActionDefinition("inventory.split", "Request a stack split after validating the observed item and count.", world, worldReason,
                "tab:integer", "slot:integer", "expectedItemId:integer", "count:integer"),
            ActionDefinition("equipment.equip", "Equip an observed tab-2 item through the native session command. Unbound non-mining equipment requires allowBind=true because equipping permanently binds it.", world, worldReason, "slot:integer", "expectedItemId:integer", "allowBind?:boolean"),
            ActionDefinition("equipment.unequip", "Unequip an observed equipment slot; later snapshots determine the result.", world, worldReason, "slot:integer", "expectedItemId:integer"),
            ActionDefinition("npc.interact", "Open the observed NPC through its native Show entry, following the same local stop behavior as a click. NPC offers remain asynchronous.", alive && DialogUI.I != null, actorReason, "entityId:string"),
            ActionDefinition("dialogue.choose", "Activate a current native dialogue option. Exact expectedLabel and expectedNpcId guard against changed menus. Completion is local callback dispatch only.", dialog, "There is no visible NPC dialogue.",
                "index:integer", "expectedLabel:string", "expectedNpcId:string", "expectedDialogueRevision:integer"),
            ActionDefinition("dialogue.close", "Close the native NPC dialogue locally.", dialog, "There is no visible NPC dialogue."),
            ActionDefinition("quest.request_offers", "Request current offers for an observed NPC via the native session owner.", world, worldReason, "entityId:string"),
            ActionDefinition("quest.accept", "Send acceptance of a currently observed NPC offer. Sent does not imply acceptance.", world && session.NpcTaskOffersState != null, worldReason, "questId:integer", "expectedOffersRevision:integer"),
            ActionDefinition("quest.complete", "Send completion of a currently active quest. The server decides advancement and reward.", world, worldReason, "questId:integer", "expectedQuestRevision:integer"),
            ActionDefinition("quest.abandon", "Send abandonment of a currently active quest; no local optimistic removal.", world, worldReason, "questId:integer", "expectedQuestRevision:integer"),
            ActionDefinition("player.revive", "Send an available server-authored revive option; only a later life snapshot revives the player.", world && session.LocalPlayerLifeState != null && !session.LocalPlayerLifeState.Alive,
                "The player is not in a server-authored death state.", "option:integer"),
            ActionDefinition("inventory.move", "Move inventory items between slots.", false, "The plugin adapter does not expose the native inventory placement command yet."),
            ActionDefinition("ui.open", "Open a native game window by identifier.", false, "Only addon-owned UI is exposed through IPluginUi; the native window catalog is not adapted."),
            ActionDefinition("chat.send", "Send text through the native session chat owner: channel 0 nearby, 1 party, 2 family, 3 world. Sent confirms only a write, never delivery. Commands and item shares are not accepted.", world, worldReason,
                "channel:integer", "text:string"),
            ActionDefinition("chat.whisper", "Send text to the current name of an observed player through the native session chat owner. Sent does not prove delivery.", world, worldReason,
                "entityId:string", "text:string")
        };
        AddEndgameActions(definitions);
        return definitions.AsReadOnly();
    }

    private static GameActionDefinition ActionDefinition(string name, string description, bool available, string reason, params string[] fields)
    {
        var properties = new Dictionary<string, object>(StringComparer.Ordinal);
        var required = new List<object>();
        foreach (string field in fields)
        {
            string[] parts = field.Split(':');
            bool optional = parts[0].EndsWith("?", StringComparison.Ordinal);
            string key = optional ? parts[0].Substring(0, parts[0].Length - 1) : parts[0];
            properties.Add(key, new Dictionary<string, object> { { "type", parts[1] } });
            if (!optional) required.Add(key);
        }
        return new GameActionDefinition(name, description,
            new Dictionary<string, object> { { "type", "object" }, { "properties", properties },
                { "required", required }, { "additionalProperties", false } }, available, available ? null : reason);
    }

    public GameActionReceipt Execute(GameActionRequest request)
    {
        RequireMainThread();
        if (request == null) throw new ArgumentNullException(nameof(request));
        RefreshScene();
        TickActions();
        if (request.RequestId != null && (request.RequestId.Length == 0 || request.RequestId.Length > 128))
            return ActionRejected(request, "request_id.invalid", "requestId must contain between 1 and 128 characters.");
        string previousId;
        PluginAction previous;
        if (request.RequestId != null && actionRequests.TryGetValue(request.RequestId, out previousId) && actions.TryGetValue(previousId, out previous))
        {
            if (previous.Request.Name == request.Name && previous.Request.ExpectedSceneRevision == request.ExpectedSceneRevision &&
                ActionValuesEqual(previous.Request.Arguments, request.Arguments)) return previous.Receipt;
            return new GameActionReceipt(previousId, request.RequestId, request.Name, GameActionState.Rejected,
                "request_id.conflict", "This requestId is already associated with different action content. Nothing was executed.");
        }
        // Bound retained bytes as well as records. The only compound action input is
        // the ordered, bounded list of rune identities for a native cube quote.
        int argumentTextSize = 0;
        if (request.Arguments.Count > 16) return ActionRejected(request, "arguments.invalid", "At most 16 action arguments are supported.");
        foreach (var argument in request.Arguments)
        {
            argumentTextSize += argument.Key.Length;
            if (argument.Value is string) argumentTextSize += ((string)argument.Value).Length;
            else if (request.Name == "endgame.cube.quote" && argument.Key == "itemUids")
            {
                var ids = argument.Value as IReadOnlyList<object>;
                if (ids == null || ids.Count > 5) return ActionRejected(request, "arguments.invalid", "itemUids must be an array of at most five UID strings.");
                foreach (object uid in ids)
                {
                    if (!(uid is string)) return ActionRejected(request, "arguments.invalid", "itemUids must contain only strings.");
                    argumentTextSize += ((string)uid).Length;
                }
            }
            else if (!(argument.Value is bool) && !ActionIsNumber(argument.Value))
                return ActionRejected(request, "arguments.invalid", "Action arguments must be strings, booleans, or finite numbers.");
        }
        if (argumentTextSize > 8192) return ActionRejected(request, "arguments.invalid", "Action argument text exceeds 8192 characters.");
        if (!ActionMakeRoom()) return ActionRejected(request, "actions.capacity", "Too many active actions; cancel or finish one before submitting more.");
        var entry = new PluginAction { Request = request, Scene = sceneRevision, Deadline = ActionNow + 30d };
        string id = "act_" + Guid.NewGuid().ToString("N");
        entry.Receipt = new GameActionReceipt(id, request.RequestId, request.Name, GameActionState.Queued, "queued", "Validating the local action.");
        actions.Add(id, entry);
        actionOrder.Enqueue(id);
        if (request.RequestId != null) actionRequests.Add(request.RequestId, id);
        if (request.ExpectedSceneRevision.HasValue && request.ExpectedSceneRevision.Value != sceneRevision)
            return ActionSet(entry, GameActionState.Rejected, "scene.stale", "Observe the current scene before issuing this action.");
        GameActionDefinition definition = null;
        foreach (var candidate in GetActions()) if (candidate.Name == request.Name) { definition = candidate; break; }
        if (definition == null) return ActionSet(entry, GameActionState.Rejected, "action.unknown", "This action name is not defined.");
        if (!definition.Available) return ActionSet(entry, GameActionState.Rejected, "action.unavailable", definition.UnavailableReason);
        try
        {
            var properties = (IReadOnlyDictionary<string, object>)definition.Arguments["properties"];
            foreach (string key in request.Arguments.Keys)
            {
                if (!properties.ContainsKey(key)) throw new ArgumentException("Unknown argument: " + key);
                var property = (IReadOnlyDictionary<string, object>)properties[key];
                switch ((string)property["type"])
                {
                    case "integer": ActionLong(request.Arguments, key); break;
                    case "number": ActionFloat(request.Arguments, key); break;
                    case "string": ActionString(request.Arguments, key); break;
                    case "boolean": ActionBool(request.Arguments, key, false); break;
                    case "array": EndgameUIDs(request.Arguments); break;
                }
            }
            foreach (object key in (IEnumerable)definition.Arguments["required"])
                if (!request.Arguments.ContainsKey((string)key)) throw new ArgumentException("Missing argument: " + key);
            ActionDispatch(entry);
            ActionPoll(entry);
        }
        catch (ArgumentException error) { ActionSet(entry, GameActionState.Rejected, "arguments.invalid", error.Message); }
        catch (ActionFailure error) { ActionSet(entry, GameActionState.Rejected, error.Code, error.Message); }
        catch (Exception error)
        {
            // Native callbacks may already have begun side effects. An exception cannot prove rejection.
            ActionSet(entry, GameActionState.Unknown, "action.exception", error.GetType().Name + ": " + error.Message);
        }
        return entry.Receipt;
    }

    public GameActionReceipt GetAction(string actionId)
    {
        RequireMainThread();
        RefreshScene();
        TickActions();
        PluginAction entry;
        return actionId != null && actions.TryGetValue(actionId, out entry) ? entry.Receipt :
            new GameActionReceipt(actionId, null, "unknown", GameActionState.Unknown, "action.not_found",
                "The action is unknown or outside the 1024-record retention window.");
    }

    public bool CancelAction(string actionId)
    {
        RequireMainThread();
        RefreshScene();
        TickActions();
        PluginAction entry;
        if (actionId == null || !actions.TryGetValue(actionId, out entry) || !ActionActive(entry)) return false;
        if (entry.Pending != null)
        {
            // A token can request cancellation before a gateway write, but a successful send wins.
            if (entry.Cancellation == null) return false;
            entry.Cancellation.Cancel();
            ActionPoll(entry);
            return true;
        }
        if (!ActionStopOwned(entry)) return false;
        ActionSet(entry, GameActionState.Cancelled, "action.cancelled", "The owned local action was cancelled. Already sent requests are unaffected.");
        return true;
    }

    private void TickActions()
    {
        actionPollBuffer.Clear();
        foreach (PluginAction entry in activeActions) actionPollBuffer.Add(entry);
        foreach (PluginAction entry in actionPollBuffer) ActionPoll(entry);
        actionPollBuffer.Clear();
    }

    private void ActionDispatch(PluginAction entry)
    {
        if (entry.Request.Name.StartsWith("endgame.", StringComparison.Ordinal)) { DispatchEndgame(entry); return; }
        var args = entry.Request.Arguments;
        var session = Session;
        var frame = ActionFrame;
        var player = frame?.Player?.SourceControllerForIndependentRuntime;
        entry.Player = player;
        entry.Native = NetClient.I;
        entry.WriteSequence = entry.Native == null ? 0 : entry.Native.PluginWriteSequence;
        switch (entry.Request.Name)
        {
            case "movement.move":
                entry.X = ActionFloat(args, "x"); entry.Y = ActionFloat(args, "y");
                ActionCheckPosition(frame, entry.X, entry.Y);
                entry.Tolerance = ActionFloat(args, "tolerance", 4f, 0.1f, 48f);
                entry.Deadline = ActionNow + ActionFloat(args, "timeoutSeconds", 60f, 1f, 600f);
                ActionSupersede("movement"); ActionSupersede("pickup");
                player.CancelPickup(); PlayerController.ClearFollow(); CombatWorld.StopAttack(); player.BreakSit();
                frame.Player.TrySetPathTo(player.CaptureMaskViewForIndependentRuntime(), entry.X, entry.Y);
                entry.NavigationRevision = player.NavigationRevisionForPlugins;
                entry.Kind = "movement";
                ActionSet(entry, GameActionState.Running, "movement.started", "Native pathfinding has started; arrival is a local observation.");
                break;
            case "movement.stop":
                player.CancelPickup(); PlayerController.ClearFollow(); CombatWorld.StopAttack(); player.StopMove();
                ActionSupersede("movement"); ActionSupersede("pickup"); ActionSupersede("attack"); ActionSupersede("skill");
                ActionLocal(entry, "movement.stopped", "Native movement, follow, pickup and combat approach were stopped."); break;
            case "target.select":
                ActionSelect(ActionString(args, "entityId"), false);
                ActionLocal(entry, "target.selected", "The native local selection was updated."); break;
            case "target.clear":
                CombatWorld.ClearTarget(); TargetSelect.Clear();
                ActionLocal(entry, "target.cleared", "Native selection and pending target were cleared."); break;
            case "combat.attack":
                entry.Deadline = ActionNow + ActionFloat(args, "timeoutSeconds", 60f, 1f, 600f);
                entry.Target = ActionSelect(ActionString(args, "entityId"), true);
                entry.CombatRevision = ActionCombat.AttackIntentRevisionForPlugins;
                ActionSupersede("attack"); entry.Kind = "attack";
                player.CancelPickup(); PlayerController.ClearFollow(); player.BreakSit();
                ActionSet(entry, GameActionState.Running, "combat.engaged", "Native attack and approach are armed. No hit or server acceptance is asserted."); break;
            case "combat.stop":
                CombatWorld.StopAttack(); player.StopMove();
                ActionSupersede("attack"); ActionSupersede("skill");
                ActionLocal(entry, "combat.stopped", "Native attack, queued skills and approach were stopped."); break;
            case "skill.request":
            case "skill.cast":
                ActionSkill(entry); break;
            case "skill.cancel":
                CombatWorld.CancelPending(); ActionSupersede("skill");
                ActionLocal(entry, "skill.cancelled", "Native pending skills were cleared; already sent casts are unaffected."); break;
            case "loot.pickup":
                string lootKind;
                int groundId = ActionEntity(ActionString(args, "entityId"), out lootKind);
                if (lootKind != "loot") throw new ActionFailure("target.kind", "loot.pickup requires a loot entity ID.");
                WorldGroundItemState item;
                GroundItems groundOwner;
                GroundItems.Item groundItem;
                if (!session.GroundItems.TryGetValue(groundId, out item) ||
                    !GroundItems.TryGetForIndependentRuntime(groundId, out groundOwner, out groundItem) || groundItem.go == null)
                    throw new ActionFailure("loot.gone", "The observed loot no longer exists in this scene.");
                if (!groundItem.owned && Time.time < groundItem.lockEnd) throw new ActionFailure("loot.locked", "The native ownership lock does not permit this pickup yet.");
                entry.Deadline = ActionNow + ActionFloat(args, "timeoutSeconds", 16f, 1f, 16f);
                ActionSupersede("pickup"); ActionSupersede("movement"); PlayerController.ClearFollow();
                entry.NativeId = groundId; entry.Kind = "pickup";
                player.WalkToPickup(groundId, item.X, item.Y);
                entry.NavigationRevision = player.NavigationRevisionForPlugins;
                ActionSet(entry, GameActionState.Running, "pickup.approaching", "Native pickup approach is active; no pickup request has yet been observed."); break;
            case "inventory.use":
            case "inventory.split":
                WorldInventoryItemState bagItem = ActionInventoryItem(args, ActionInt(args, "tab", 0, 255));
                if (entry.Request.Name == "inventory.use")
                {
                    if (SocialClient.TradeActive) throw new ActionFailure("inventory.trade_active", "Native inventory activation is blocked during a trade.");
                    if (bagItem.Pet != null) throw new ActionFailure("inventory.pet", "Pet deployment and hatching require a dedicated pet action; ordinary item use does not activate pets.");
                    var nativeItem = InventoryDlg.ItemAt(bagItem.Tab, bagItem.BagIndex);
                    if (nativeItem.id != bagItem.ItemId) throw new ActionFailure("inventory.native_stale", "The native inventory projection does not match the observed item.");
                    int nativeTab = bagItem.Tab;
                    InventoryDlg.EnsureOwnerForIndependentRuntime().ActivateOrdinaryItemForIndependentRuntime(ref nativeTab, nativeItem);
                    var write = entry.Native.ReadWriteForPlugins("item", bagItem.ItemId);
                    if (write != null && write.Sequence > entry.WriteSequence)
                    {
                        entry.Pending = write.Pending;
                        ActionSet(entry, GameActionState.Queued, "item.send.pending", "Native activation produced a gateway write; its result is pending.");
                    }
                    else ActionLocal(entry, "item.activated", "The native item activation returned without an observed write. A cooldown, item-specific window, target picker, or confirmation may require attention.",
                        new Dictionary<string, object> { { "nativeWriteObserved", false }, { "confirmationVisible", SysMsgBox.Visible } });
                }
                else
                {
                    int count = ActionInt(args, "count", 1, Math.Min(999, Math.Max(1, bagItem.Count - 1)));
                    if (count >= bagItem.Count) throw new ActionFailure("inventory.count", "The split count must be smaller than the current stack.");
                    ActionSend(entry, token => session.SplitInventoryItemAsync(bagItem.Tab, bagItem.BagIndex, count, token));
                }
                break;
            case "inventory.sort":
                byte tab = (byte)ActionInt(args, "tab", 0, 255);
                ActionSend(entry, token => session.SortInventoryAsync(tab, token)); break;
            case "equipment.equip":
                WorldInventoryItemState equipment = ActionInventoryItem(args, 2);
                if (SocialClient.TradeActive) throw new ActionFailure("inventory.trade_active", "Native equipment activation is blocked during a trade.");
                if (equipment.Pet != null || equipment.Cosmetic) throw new ActionFailure("equipment.kind", "This entry is not ordinary equipment.");
                if (!equipment.Bound && !NetClient.IsMiningTool(equipment.ItemId) && !ActionBool(args, "allowBind", false))
                    throw new ActionFailure("equipment.binding_confirmation_required", "Equipping this item permanently binds it and prevents trading. Resubmit with allowBind=true only if intended.");
                ActionSend(entry, token => session.EquipInventoryItemAsync(equipment.BagIndex, token)); break;
            case "equipment.unequip":
                int slot = ActionInt(args, "slot", 0, 255), expected = ActionInt(args, "expectedItemId");
                bool found = false;
                foreach (var worn in session.InventoryState.EquippedItems)
                    if (worn.Slot == slot && worn.ItemId == expected) { found = true; break; }
                if (!found) throw new ActionFailure("equipment.stale", "This equipped slot does not contain expectedItemId.");
                ActionSend(entry, token => session.UnequipInventoryItemAsync(slot, token)); break;
            case "npc.interact":
                ReferenceWorldNpcActor npc = ActionNpc(ActionString(args, "entityId"));
                npc.SelectableOwner.Show(); player.StopForNpcPointerForIndependentRuntime();
                ActionLocal(entry, "npc.interacted", "The native NPC interaction ran; dialogue or a function-specific window may open, and server effects are asynchronous.",
                    new Dictionary<string, object> { { "dialogueVisible", DialogUI.VisibleS } }); break;
            case "dialogue.choose":
                if (ActionLong(args, "expectedDialogueRevision") != CurrentDialogueRevision())
                    throw new ActionFailure("dialogue.stale", "The dialogue changed after observation; refresh it before choosing.");
                ReferenceWorldNpcActor expectedNpc = ActionNpc(ActionString(args, "expectedNpcId"));
                if (DialogUI.I.CurrentNpcEntityId != expectedNpc.State.EntityId) throw new ActionFailure("dialogue.stale", "The open dialogue belongs to another NPC.");
                int index = ActionInt(args, "index", 0, int.MaxValue);
                var options = DialogUI.I.Options;
                if (index >= options.Count || options[index].Label != ActionString(args, "expectedLabel"))
                    throw new ActionFailure("dialogue.stale", "The requested option is no longer at this index with this label.");
                if (!DialogUI.I.ActivateOption(options[index])) throw new ActionFailure("dialogue.unavailable", "The native option was unavailable.");
                ActionLocal(entry, "dialogue.dispatched", "The current native dialogue option callback ran. Any server outcome remains unconfirmed."); break;
            case "dialogue.close":
                DialogUI.I.Close(); ActionLocal(entry, "dialogue.closed", "The local NPC dialogue was closed."); break;
            case "quest.request_offers":
                ReferenceWorldNpcActor offerNpc = ActionNpc(ActionString(args, "entityId"));
                ActionSend(entry, token => session.RequestNpcTaskOffersAsync(offerNpc.State.DisplayName, token)); break;
            case "quest.accept":
                if (ActionLong(args, "expectedOffersRevision") != session.NpcTaskOffersStateVersion)
                    throw new ActionFailure("quest.offers_stale", "NPC offers changed after observation.");
                int acceptId = ActionInt(args, "questId"); bool offered = false;
                foreach (var offer in session.NpcTaskOffersState.Offers) if (offer.TaskId == acceptId) { offered = true; break; }
                if (!offered) throw new ActionFailure("quest.not_offered", "This quest is not in the current NPC offer snapshot.");
                ActionSend(entry, token => session.AcceptTaskAsync(acceptId, token)); break;
            case "quest.complete":
            case "quest.abandon":
                if (ActionLong(args, "expectedQuestRevision") != session.TaskLogStateVersion)
                    throw new ActionFailure("quest.log_stale", "The quest log changed after observation.");
                int taskId = ActionInt(args, "questId");
                if (session.TaskLogState == null || session.TaskLogState.FindActive(taskId) == null)
                    throw new ActionFailure("quest.not_active", "This quest is not in the active quest snapshot.");
                ActionSend(entry, token => entry.Request.Name == "quest.complete" ? session.CompleteTaskAsync(taskId, token) : session.AbandonTaskAsync(taskId, token)); break;
            case "player.revive":
                var option = (WorldReviveOption)ActionInt(args, "option", 0, 255);
                if (!session.LocalPlayerLifeState.Allows(option)) throw new ActionFailure("revive.unavailable", "The current death snapshot does not offer this revive option.");
                ActionSend(entry, token => session.RequestReviveAsync(option, token)); break;
            case "chat.send":
            case "chat.whisper":
                string chatText = ActionString(args, "text");
                if (string.IsNullOrWhiteSpace(chatText) || chatText.Length > 512 || chatText.TrimStart().StartsWith("/", StringComparison.Ordinal))
                    throw new ArgumentException("Chat text must contain 1..512 characters and cannot be a slash command.");
                WorldChatSendRequest chat;
                if (entry.Request.Name == "chat.send") chat = WorldChatSendRequest.ForChannel((byte)ActionInt(args, "channel", 0, 3), chatText, null);
                else
                {
                    string playerKind; int playerId = ActionEntity(ActionString(args, "entityId"), out playerKind);
                    ReferenceWorldRemotePlayerActor remote;
                    if (playerKind != "player" || !frame.RemoteActors.TryGetValue(playerId, out remote) || remote.SourceIdentityForIndependentRuntime == null)
                        throw new ActionFailure("chat.target_gone", "Whispers require an observed live remote player.");
                    chat = WorldChatSendRequest.ForWhisper(remote.SourceIdentityForIndependentRuntime.SelName, chatText, null);
                }
                ActionSend(entry, token => session.SendWorldChatAsync(chat, token)); break;
            default: throw new ActionFailure("action.unavailable", "The action has no native adapter.");
        }
    }

    private void ActionSkill(PluginAction entry)
    {
        var args = entry.Request.Arguments;
        int skill = ActionInt(args, "skillId", 1, int.MaxValue);
        if (Session.SkillBookState == null || Session.SkillBookState.LevelOf(skill) <= 0)
            throw new ActionFailure("skill.not_learned", "The skill is not present in the learned-skill snapshot.");
        bool requestSkill = entry.Request.Name == "skill.request";
        bool coordinates = args.ContainsKey("x") || args.ContainsKey("y");
        int x = 0, y = 0;
        if (requestSkill) entry.Deadline = ActionNow + ActionFloat(args, "timeoutSeconds", 15f, 1f, 60f);
        else if (coordinates)
        {
            x = ActionInt(args, "x", 0, int.MaxValue); y = ActionInt(args, "y", 0, int.MaxValue);
            ActionCheckPosition(ActionFrame, x, y);
        }
        string targetId = ActionString(args, "entityId", false);
        int nativeTarget = 0;
        if (targetId != null)
        {
            string kind;
            nativeTarget = ActionEntity(targetId, out kind);
            ActionSelect(targetId, false);
        }
        entry.NativeId = skill;
        if (requestSkill)
        {
            Win05.SkInfo info = Win05.SkillInfo(skill);
            ActionSupersede("skill");
            CombatWorld.CancelPending();
            entry.Kind = "skill";
            if (!Session.RequestCharacterSkill(skill, info == null ? 0f : info.dist, CancellationToken.None))
                throw new ActionFailure("skill.owner.unavailable", "The native skill-use port could not admit the request.");
            entry.CombatRevision = ActionCombat.SkillIntentRevisionForPlugins;
            ActionSet(entry, GameActionState.Running, "skill.requested", "The native skill queue was invoked; no gateway write is asserted yet.");
        }
        else if (coordinates)
        {
            ActionSend(entry, token => Session.UseCharacterSkillAtAsync(skill, x, y, nativeTarget, token));
        }
        else if (targetId != null) ActionSend(entry, token => Session.UseCharacterSkillAsync(skill, nativeTarget, token));
        else ActionSend(entry, token => Session.CastCharacterSkillAtTargetAsync(skill, token));
    }

    private void ActionSend(PluginAction entry, Func<CancellationToken, Task<Result>> send)
    {
        entry.Cancellation = new CancellationTokenSource();
        entry.Pending = send(entry.Cancellation.Token);
        if (entry.Pending == null) throw new InvalidOperationException("The native command returned no task.");
        ActionSet(entry, GameActionState.Queued, "send.pending", "The native command is pending; no successful write is asserted yet.");
    }

    private void ActionPoll(PluginAction entry)
    {
        if (entry.EndgameRequest != null) { PollEndgame(entry); return; }
        if (!ActionActive(entry) && (entry.Pending == null || entry.PendingSettled)) return;
        // Observe a native queued pickup/skill's actual gateway task before scene/cancel checks.
        // Sending may already have happened, in which case it cannot be revoked.
        if (entry.Pending == null && (entry.Kind == "pickup" || entry.Kind == "skill") && entry.Native != null &&
            (entry.Kind != "pickup" || ActionOwnsPath(entry)) &&
            (entry.Kind != "skill" || ActionOwnsSkill(entry)))
        {
            var write = entry.Native.ReadWriteForPlugins(entry.Kind == "pickup" ? "pickup" : "skill", entry.NativeId);
            if (write != null && write.Sequence > entry.WriteSequence) entry.Pending = write.Pending;
        }
        if (entry.Pending != null && entry.Pending.IsCompleted)
        {
            entry.PendingSettled = true;
            if (entry.Pending.IsCanceled) ActionSet(entry, GameActionState.Cancelled, "send.cancelled", "The native gateway task was cancelled; no successful write was reported.");
            else if (entry.Pending.IsFaulted)
            {
                var ignored = entry.Pending.Exception;
                ActionSet(entry, GameActionState.Unknown, "send.exception", "The native gateway task faulted. Whether a request reached the server is unknown.");
            }
            else if (!entry.Pending.Result.IsSuccess) ActionSet(entry, GameActionState.Rejected, entry.Pending.Result.Error.Code, entry.Pending.Result.Error.Message);
            else ActionSet(entry, GameActionState.Sent, "request.sent", "The native gateway reported a successful write. Server acceptance and gameplay completion are unconfirmed.");
            return;
        }
        if (!ActionActive(entry)) return;
        if (entry.Scene != sceneRevision || Session == null || Session.State != ClientSessionState.InWorld)
        {
            entry.Cancellation?.Cancel();
            // Never stop an actor in a replacement scene. Its native lifecycle owns teardown.
            ActionSet(entry, GameActionState.Unknown, "scene.changed", "The scene/session changed before a definitive result. Observe again; any in-flight write may still settle.");
            return;
        }
        if (ActionNow >= entry.Deadline)
        {
            entry.Cancellation?.Cancel();
            if (entry.Pending == null) ActionStopOwned(entry);
            ActionSet(entry, GameActionState.TimedOut, "action.timeout", "The local observation deadline expired. Already dispatched effects may still occur; this is not server rejection.");
            return;
        }
        if (entry.Pending != null) return;
        switch (entry.Kind)
        {
            case "movement":
                if (!ActionOwnsPath(entry)) { ActionSet(entry, GameActionState.Unknown, "movement.superseded", "Another native path or actor replaced this action."); break; }
                float dx = entry.Player.worldX - entry.X, dy = entry.Player.worldY - entry.Y;
                if (dx * dx + dy * dy <= entry.Tolerance * entry.Tolerance)
                {
                    entry.Player.StopMove();
                    ActionLocal(entry, "movement.arrived", "The native player's locally predicted position is within the requested tolerance.",
                        new Dictionary<string, object> { { "x", entry.Player.worldX }, { "y", entry.Player.worldY } });
                }
                else if (!entry.Player.MoveToTargetForIndependentRuntime)
                    ActionSet(entry, GameActionState.Rejected, "movement.stopped", "The native path stopped before reaching the requested position (blocked, snapped endpoint, death, or interrupted).");
                break;
            case "pickup":
                if (!ActionOwnsPath(entry) || entry.Player.PendingPickupIdForIndependentRuntime != entry.NativeId)
                    ActionSet(entry, GameActionState.Unknown, "pickup.ended", "Native pickup approach ended or was replaced without a correlated successful write.");
                break;
            case "skill":
                if (!ActionOwnsSkill(entry))
                { ActionSet(entry, GameActionState.Unknown, "skill.superseded", "A newer native skill or target intent replaced this request."); break; }
                if (ActionCombat == null || (ActionCombat.PendingSkillForIndependentRuntime != entry.NativeId && ActionCombat.QueuedSkillForIndependentRuntime != entry.NativeId))
                    ActionSet(entry, GameActionState.Rejected, "skill.not_queued", "The native skill entry left no matching queued intent or gateway write.");
                break;
            case "attack":
                if (!ActionOwnsAttack(entry) || !ActionCombat.AutoAttackingForIndependentRuntime)
                    ActionSet(entry, GameActionState.Unknown, "combat.ended", "The local native attack loop ended or target changed; no combat outcome is inferred.");
                break;
        }
    }

    private bool ActionStopOwned(PluginAction entry)
    {
        if (entry.Kind == "movement" || entry.Kind == "pickup")
        {
            if (!ActionOwnsPath(entry)) return false;
            if (entry.Kind == "pickup" && entry.Player.PendingPickupIdForIndependentRuntime == entry.NativeId) entry.Player.CancelPickup();
            entry.Player.StopMove(); return true;
        }
        if (entry.Kind == "attack")
        {
            if (!ActionOwnsAttack(entry)) return false;
            CombatWorld.StopAttack(); entry.Player?.StopOwnedCombatApproachForPlugins(entry.CombatRevision, false); return true;
        }
        if (entry.Kind == "skill" && ActionOwnsSkill(entry) &&
            (ActionCombat.PendingSkillForIndependentRuntime == entry.NativeId || ActionCombat.QueuedSkillForIndependentRuntime == entry.NativeId))
        { CombatWorld.CancelPending(); entry.Player?.StopOwnedCombatApproachForPlugins(entry.CombatRevision, true); return true; }
        return false;
    }

    private static bool ActionOwnsPath(PluginAction entry) => entry.Player != null &&
        ReferenceEquals(ActionFrame?.Player?.SourceControllerForIndependentRuntime, entry.Player) &&
        entry.Player.NavigationRevisionForPlugins == entry.NavigationRevision;

    private static bool ActionOwnsAttack(PluginAction entry) => ActionCombat != null && entry.Player != null &&
        ReferenceEquals(ActionFrame?.Player?.SourceControllerForIndependentRuntime, entry.Player) &&
        ActionCombat.AttackIntentRevisionForPlugins == entry.CombatRevision &&
        ReferenceEquals(TargetSelect.CurrentSourceForIndependentRuntime, entry.Target);

    private static bool ActionOwnsSkill(PluginAction entry) => ActionCombat != null && entry.Player != null &&
        ReferenceEquals(ActionFrame?.Player?.SourceControllerForIndependentRuntime, entry.Player) &&
        ActionCombat.SkillIntentRevisionForPlugins == entry.CombatRevision;

    private void ActionSupersede(string kind)
    {
        foreach (var entry in actions.Values)
            if (entry.Kind == kind && ActionActive(entry) && entry.Pending == null)
                ActionSet(entry, GameActionState.Cancelled, "action.superseded", "A newer local action replaced this intent. Already sent requests are unaffected.");
    }

    private object ActionSelect(string entityId, bool attack)
    {
        string kind; int id = ActionEntity(entityId, out kind);
        var frame = ActionFrame;
        if (kind == "monster")
        {
            ReferenceWorldMonsterActor actor;
            if (!frame.MonsterActors.TryGetValue(id, out actor) || !actor.SourceOwnerLive)
                throw new ActionFailure("target.gone", "The monster no longer has a live native actor.");
            if (attack)
            {
                if (actor.SelectableOwner.friendly || actor.SelectableOwner.ally || actor.SelectableOwner.SelHp <= 0)
                    throw new ActionFailure("target.not_attackable", "The native monster is friendly, allied, or dead.");
                CombatWorld.SetTarget(actor.SelectableOwner, true);
            }
            else frame.TrySelectMonsterIdentity(id);
            return actor.SelectableOwner;
        }
        if (kind == "player")
        {
            if (frame.Player != null && frame.Player.EntityId == id)
            {
                if (attack) throw new ActionFailure("target.self", "The player cannot attack itself.");
                TargetSelect.Select(frame.Player.SourceControllerForIndependentRuntime);
                return frame.Player.SourceControllerForIndependentRuntime;
            }
            ReferenceWorldRemotePlayerActor actor;
            if (!frame.RemoteActors.TryGetValue(id, out actor) || actor.SourceIdentityForIndependentRuntime == null)
                throw new ActionFailure("target.gone", "The player no longer has a native actor.");
            if (attack)
            {
                var owner = actor.SourceIdentityForIndependentRuntime;
                if (owner.IsDead || ReferenceWorldTargetSelectionMath.IsFriendly(CombatWorld.LocalFaction, actor.State.Faction, owner.OpenWorldHostile))
                    throw new ActionFailure("target.not_attackable", "The native player is friendly or dead.");
                CombatWorld.SetPvpTarget(owner);
            }
            else frame.TrySelectRemotePlayerIdentity(id);
            return actor.SourceIdentityForIndependentRuntime;
        }
        if (kind == "npc" && !attack)
        {
            var actor = ActionNpc(entityId);
            frame.TargetSelection.TrySelectNpc(actor);
            return actor.SelectableOwner;
        }
        throw new ActionFailure("target.kind", "The target kind is not supported by this action.");
    }

    private ReferenceWorldNpcActor ActionNpc(string entityId)
    {
        string kind; int id = ActionEntity(entityId, out kind);
        ReferenceWorldNpcActor npc;
        if (kind != "npc" || !ActionFrame.NpcActors.TryGetValue(id, out npc) || npc.SelectableOwner == null)
            throw new ActionFailure("npc.gone", "The observed NPC is not a live native actor in this scene.");
        return npc;
    }

    private int ActionEntity(string value, out string kind)
    {
        string[] parts = value.Split(':'); long scene; int id;
        if (parts.Length != 3 || !long.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out scene) ||
            !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out id))
            throw new ArgumentException("entityId must be an observed kind:sceneRevision:nativeId string.");
        if (scene != sceneRevision) throw new ActionFailure("entity.stale", "The entity belongs to a different scene revision.");
        kind = parts[0]; return id;
    }

    private WorldInventoryItemState ActionInventoryItem(IReadOnlyDictionary<string, object> args, int tab)
    {
        int slot = ActionInt(args, "slot", 0, int.MaxValue), expected = ActionInt(args, "expectedItemId");
        var inventory = Session.InventoryState;
        if (inventory != null) foreach (var item in inventory.Items)
            if (item.Tab == tab && item.BagIndex == slot && item.ItemId == expected && item.Count > 0) return item;
        throw new ActionFailure("inventory.stale", "This bag tab/slot does not contain expectedItemId in the current snapshot.");
    }

    private static void ActionCheckPosition(ReferenceFirstWorldInitialFrame frame, float x, float y)
    {
        if (x < 0 || y < 0 || x >= frame.MapWidthForIndependentRuntime || y >= frame.MapHeightForIndependentRuntime)
            throw new ArgumentException("Coordinates must be inside the current map's pixel bounds.");
    }

    private GameActionReceipt ActionLocal(PluginAction entry, string code, string message, Dictionary<string, object> data = null)
    {
        data = data ?? new Dictionary<string, object>(); data["completionScope"] = "local";
        return ActionSet(entry, GameActionState.Completed, code, message, data);
    }

    private GameActionReceipt ActionSet(PluginAction entry, GameActionState state, string code, string message, Dictionary<string, object> data = null)
    {
        data = data ?? new Dictionary<string, object>();
        data["sceneRevision"] = entry.Scene;
        if (!data.ContainsKey("serverOutcomeKnown")) data["serverOutcomeKnown"] = false;
        entry.Receipt = new GameActionReceipt(entry.Receipt.ActionId, entry.Request.RequestId, entry.Request.Name, state, code, message, data);
        if (!ActionActive(entry)) { entry.Cancellation?.Dispose(); entry.Cancellation = null; }
        if (ActionActive(entry) || entry.EndgameWaiting || (entry.Pending != null && !entry.PendingSettled)) activeActions.Add(entry);
        else activeActions.Remove(entry);
        Emit("action.changed", new Dictionary<string, object> { { "actionId", entry.Receipt.ActionId }, { "requestId", entry.Request.RequestId },
            { "name", entry.Request.Name }, { "state", state.ToString() }, { "code", code }, { "message", message }, { "data", entry.Receipt.Data } });
        return entry.Receipt;
    }

    private static bool ActionActive(PluginAction entry) => entry.Receipt.State == GameActionState.Queued || entry.Receipt.State == GameActionState.Running;

    private bool ActionMakeRoom()
    {
        int count = actionOrder.Count;
        while (actions.Count >= ActionRetention && count-- > 0)
        {
            string id = actionOrder.Dequeue(); PluginAction entry;
            if (!actions.TryGetValue(id, out entry)) continue;
            if (ActionActive(entry) || entry.EndgameWaiting || (entry.Pending != null && !entry.PendingSettled)) { actionOrder.Enqueue(id); continue; }
            actions.Remove(id);
            if (entry.Request.RequestId != null) actionRequests.Remove(entry.Request.RequestId);
        }
        return actions.Count < ActionRetention;
    }

    private static GameActionReceipt ActionRejected(GameActionRequest request, string code, string message) =>
        new GameActionReceipt(null, request.RequestId, request.Name, GameActionState.Rejected, code, message);

    private sealed class ActionFailure : Exception
    {
        internal readonly string Code;
        internal ActionFailure(string code, string message) : base(message) { Code = code; }
    }

    private static string ActionString(IReadOnlyDictionary<string, object> args, string key, bool required = true)
    {
        object value;
        if (!args.TryGetValue(key, out value))
        { if (!required) return null; throw new ArgumentException("Missing argument: " + key); }
        var result = value as string;
        if (result == null || result.Length > 4096) throw new ArgumentException(key + " must be a string of at most 4096 characters.");
        return result;
    }

    private static int ActionInt(IReadOnlyDictionary<string, object> args, string key, int minimum = int.MinValue, int maximum = int.MaxValue)
    {
        object value;
        if (!args.TryGetValue(key, out value) || !ActionIsNumber(value)) throw new ArgumentException(key + " must be an integer.");
        decimal number;
        try { number = ActionDecimal(value); }
        catch (Exception) { throw new ArgumentException(key + " is outside the supported numeric range."); }
        if (ActionCanonicalNumber(value) != ActionCanonicalNumber(number) || number != decimal.Truncate(number) || number < minimum || number > maximum)
            throw new ArgumentException(key + " must be an integer between " + minimum + " and " + maximum + ".");
        return (int)number;
    }

    private static float ActionFloat(IReadOnlyDictionary<string, object> args, string key, float? fallback = null, float minimum = -float.MaxValue, float maximum = float.MaxValue)
    {
        object value;
        if (!args.TryGetValue(key, out value))
        { if (fallback.HasValue) return fallback.Value; throw new ArgumentException("Missing argument: " + key); }
        if (!ActionIsNumber(value)) throw new ArgumentException(key + " must be a finite number.");
        float number;
        try { number = Convert.ToSingle(value, CultureInfo.InvariantCulture); }
        catch (Exception) { throw new ArgumentException(key + " is outside the supported numeric range."); }
        if (float.IsNaN(number) || float.IsInfinity(number) || number < minimum || number > maximum)
            throw new ArgumentException(key + " is outside the supported numeric range.");
        return number;
    }

    private static bool ActionBool(IReadOnlyDictionary<string, object> args, string key, bool fallback)
    {
        object value;
        if (!args.TryGetValue(key, out value)) return fallback;
        if (!(value is bool)) throw new ArgumentException(key + " must be a boolean.");
        return (bool)value;
    }

    private static long ActionLong(IReadOnlyDictionary<string, object> args, string key)
    {
        object value;
        if (!args.TryGetValue(key, out value) || !ActionIsNumber(value)) throw new ArgumentException(key + " must be an integer.");
        decimal number;
        try { number = ActionDecimal(value); }
        catch (Exception) { throw new ArgumentException(key + " is outside the supported numeric range."); }
        if (ActionCanonicalNumber(value) != ActionCanonicalNumber(number) || number != decimal.Truncate(number) || number < long.MinValue || number > long.MaxValue)
            throw new ArgumentException(key + " must be a signed 64-bit integer.");
        return (long)number;
    }

    private static string ActionNumberText(object value) => value is double
        ? ((double)value).ToString("R", CultureInfo.InvariantCulture)
        : value is float ? ((float)value).ToString("R", CultureInfo.InvariantCulture)
        : Convert.ToString(value, CultureInfo.InvariantCulture);

    private static decimal ActionDecimal(object value) => decimal.Parse(ActionNumberText(value), NumberStyles.Float, CultureInfo.InvariantCulture);

    // Normalize decimal/exponent syntax without rounding through double or decimal.
    // In particular 1.0000000000000002 must never collide with 1 during idempotency.
    private static string ActionCanonicalNumber(object value)
    {
        string text = ActionNumberText(value);
        int exponent = 0, e = text.IndexOfAny(new[] { 'E', 'e' });
        if (e >= 0) { exponent = int.Parse(text.Substring(e + 1), CultureInfo.InvariantCulture); text = text.Substring(0, e); }
        string sign = text.StartsWith("-", StringComparison.Ordinal) ? "-" : "";
        text = text.TrimStart('-', '+');
        int point = text.IndexOf('.');
        if (point >= 0) { exponent -= text.Length - point - 1; text = text.Remove(point, 1); }
        text = text.TrimStart('0');
        if (text.Length == 0) return "0";
        int length = text.Length;
        text = text.TrimEnd('0'); exponent += length - text.Length;
        return sign + text + "e" + exponent.ToString(CultureInfo.InvariantCulture);
    }

    private void DisposeActions()
    {
        foreach (var entry in actions.Values)
        {
            if (ActionActive(entry) && entry.Pending == null) ActionStopOwned(entry);
            entry.Cancellation?.Cancel(); entry.Cancellation?.Dispose(); entry.Cancellation = null;
            // Observe a later fault without touching Unity or changing the action outcome.
            if (entry.Pending != null && !entry.Pending.IsCompleted)
                entry.Pending.ContinueWith(pending => { var ignored = pending.Exception; },
                    CancellationToken.None, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        }
        actions.Clear(); actionRequests.Clear(); actionOrder.Clear(); activeActions.Clear(); actionPollBuffer.Clear();
    }

    private static bool ActionIsNumber(object value) => value is byte || value is sbyte || value is short || value is ushort ||
        value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal;

    private static bool ActionValuesEqual(object left, object right)
    {
        if (ReferenceEquals(left, right)) return true;
        if (left == null || right == null) return false;
        var a = left as IReadOnlyDictionary<string, object>; var b = right as IReadOnlyDictionary<string, object>;
        if (a != null || b != null)
        {
            if (a == null || b == null || a.Count != b.Count) return false;
            foreach (var pair in a) { object value; if (!b.TryGetValue(pair.Key, out value) || !ActionValuesEqual(pair.Value, value)) return false; }
            return true;
        }
        if (left is string || right is string) return Equals(left, right);
        if (ActionIsNumber(left) && ActionIsNumber(right))
        {
            return ActionCanonicalNumber(left) == ActionCanonicalNumber(right);
        }
        var listA = left as IList; var listB = right as IList;
        if (listA != null || listB != null)
        {
            if (listA == null || listB == null || listA.Count != listB.Count) return false;
            for (int i = 0; i < listA.Count; i++) if (!ActionValuesEqual(listA[i], listB[i])) return false;
            return true;
        }
        return left.Equals(right);
    }
}
}
