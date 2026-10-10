// Runs the production action adapter against deterministic native-owner substitutes.
// This is not Unity gameplay, collision/pathfinding, rendering, or live-server verification.
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.PluginApi;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static class GameActionAdapterTests
{
    private static int checks;
    private static Dictionary<string, object> Map(params object[] pairs)
    { var d = new Dictionary<string, object>(); for (int i = 0; i < pairs.Length; i += 2) d.Add((string)pairs[i], pairs[i + 1]); return d; }
    private static void Check(bool value, string label) { if (!value) throw new Exception(label); checks++; }
    private static PluginGameApi Setup()
    {
        var frame = new ReferenceFirstWorldInitialFrame();
        GameBootstrap.I = new GameBootstrap { Window = new Window { Frame = frame } };
        NetClient.I = new NetClient();
        DialogUI.I = new DialogUI { CurrentNpcEntityId = 3, Revision = 9, Visible = true };
        DialogUI.I.Options.Add(new ReferenceNpcDialogOption { Label = "Continue" });
        frame.NpcActors.Add(3, new ReferenceWorldNpcActor { State = new WorldNpcState { EntityId = 3, DisplayName = "Guide" }, SelectableOwner = new NpcInteract() });
        frame.MonsterActors.Add(5, new ReferenceWorldMonsterActor { SelectableOwner = new MonsterEntity { SelHp = 50 } });
        var session = new ClientSessionCoordinator();
        session.InventoryState.Items.Add(new WorldInventoryItemState { ItemId = 44, Tab = 0, BagIndex = 1, Count = 10 });
        session.InventoryState.Items.Add(new WorldInventoryItemState { ItemId = 45, Tab = 2, BagIndex = 2, Count = 1 });
        session.NpcTaskOffersState.Offers.Add(new WorldNpcTaskOfferState { TaskId = 7 });
        session.GroundItems[10] = new WorldGroundItemState { X = 100, Y = 100 };
        GroundItems.TestItems.Clear(); GroundItems.TestItems[10] = new GroundItems.Item { go = new object(), owned = true };
        return new PluginGameApi(session);
    }
    private static GameActionReceipt Act(PluginGameApi api, string name, Dictionary<string, object> args = null, string id = null, long? scene = 1) =>
        api.Execute(new GameActionRequest(name, args, id, scene));

    public static int Main()
    {
        try
        {
            var api = Setup();
            var stale = Act(api, "inventory.sort", Map("tab", 0), "stale", 0);
            Check(stale.Code == "scene.stale" && api.TestSession.Sends == 0, "stale scene rejected before send");
            var first = Act(api, "inventory.sort", Map("tab", 0), "once");
            var retry = Act(api, "inventory.sort", Map("tab", 0m), "once");
            Check(first.State == GameActionState.Sent && first.ActionId == retry.ActionId && api.TestSession.Sends == 1, "equivalent numeric retry does not resend");
            var conflict = Act(api, "inventory.sort", Map("tab", 1), "once");
            Check(conflict.Code == "request_id.conflict" && api.TestSession.Sends == 1, "changed request payload rejected without resend");
            Check(!api.CancelAction(first.ActionId), "sent request cannot be revoked");
            var staleItem = Act(api, "inventory.split", Map("tab", 0, "slot", 1, "expectedItemId", 99, "count", 1));
            Check(staleItem.Code == "inventory.stale" && api.TestSession.Sends == 1, "stale inventory identity blocks mutation");
            var fractional = Act(api, "inventory.sort", Map("tab", 1.0000000000000002));
            Check(fractional.Code == "arguments.invalid", "near-integer double never rounded into valid integer");
            var invalidAttack = Act(api, "combat.attack", Map("entityId", "monster:1:5", "timeoutSeconds", -1));
            Check(invalidAttack.Code == "arguments.invalid" && !GameBootstrap.I.Window.Frame.CombatOwnerForIndependentRuntime.AutoAttackingForIndependentRuntime, "invalid timeout never arms combat");
            var invalidSkill = Act(api, "skill.cast", Map("skillId", 8, "entityId", "monster:1:5", "x", -1, "y", 10));
            Check(invalidSkill.Code == "arguments.invalid" && TargetSelect.CurrentSourceForIndependentRuntime == null, "invalid skill coordinates never select a target");
            var underflow = Act(api, "inventory.sort", Map("tab", 1e-100));
            Check(underflow.Code == "arguments.invalid", "tiny fraction cannot underflow into integer zero");
            Act(api, "movement.move", Map("x", 1.0000000000000002, "y", 1), "precise");
            var numericConflict = Act(api, "movement.move", Map("x", 1, "y", 1), "precise");
            Check(numericConflict.Code == "request_id.conflict", "idempotency retains double precision");
            var oldMenu = Act(api, "dialogue.choose", Map("index", 0, "expectedLabel", "Continue", "expectedNpcId", "npc:1:3", "expectedDialogueRevision", 8));
            Check(oldMenu.Code == "dialogue.stale" && DialogUI.I.Activations == 0, "stale dialogue revision blocks callback");
            var oldLabel = Act(api, "dialogue.choose", Map("index", 0, "expectedLabel", "Accept", "expectedNpcId", "npc:1:3", "expectedDialogueRevision", 9));
            Check(oldLabel.Code == "dialogue.stale" && DialogUI.I.Activations == 0, "stale dialogue label blocks callback");
            var choose = Act(api, "dialogue.choose", Map("index", 0, "expectedLabel", "Continue", "expectedNpcId", "npc:1:3", "expectedDialogueRevision", 9));
            Check(choose.State == GameActionState.Completed && (string)choose.Data["completionScope"] == "local" && DialogUI.I.Activations == 1, "dialogue completion explicitly local");
            var oldOffers = Act(api, "quest.accept", Map("questId", 7, "expectedOffersRevision", 0));
            Check(oldOffers.Code == "quest.offers_stale" && api.TestSession.Sends == 1, "stale offers block quest send");
            var binding = Act(api, "equipment.equip", Map("slot", 2, "expectedItemId", 45));
            Check(binding.Code == "equipment.binding_confirmation_required" && api.TestSession.Sends == 1, "unbound gear requires explicit bind intent");
            var equip = Act(api, "equipment.equip", Map("slot", 2, "expectedItemId", 45, "allowBind", true));
            Check(equip.State == GameActionState.Sent && api.TestSession.Sends == 2, "equipment resolves native tab 2 and preserves send-only state");

            api = Setup();
            var move = Act(api, "movement.move", Map("x", 100, "y", 100), "walk");
            Check(move.State == GameActionState.Running, "movement starts native navigation");
            var player = GameBootstrap.I.Window.Frame.Player.SourceControllerForIndependentRuntime;
            player.SetTestPath();
            Check(api.GetAction(move.ActionId).Code == "movement.superseded", "new native path supersedes owned navigation");
            int stops = player.Stops;
            Check(!api.CancelAction(move.ActionId) && player.Stops == stops, "cancelling old action never stops replacement path");
            move = Act(api, "movement.move", Map("x", 100, "y", 100));
            player.worldX = 100; player.worldY = 100;
            Check(api.GetAction(move.ActionId).Code == "movement.arrived", "observed local arrival completes movement");
            var running = Act(api, "movement.move", Map("x", 200, "y", 200));
            Check(api.CancelAction(running.ActionId) && api.GetAction(running.ActionId).State == GameActionState.Cancelled, "owned navigation cancellation stops local intent");

            api = Setup();
            var attack = Act(api, "combat.attack", Map("entityId", "monster:1:5"));
            var frame = GameBootstrap.I.Window.Frame;
            CombatWorld.SetTarget(frame.MonsterActors[5].SelectableOwner, true);
            Check(!api.CancelAction(attack.ActionId) && frame.CombatOwnerForIndependentRuntime.AutoAttackingForIndependentRuntime,
                "old attack cancellation cannot stop newer native same-target engagement");
            attack = Act(api, "combat.attack", Map("entityId", "monster:1:5"));
            player = frame.Player.SourceControllerForIndependentRuntime;
            player.TestCombatApproach(); stops = player.Stops;
            Check(api.CancelAction(attack.ActionId) && player.Stops == stops + 1, "owned combat approach stops with owned attack");
            var skill = Act(api, "skill.request", Map("skillId", 8, "entityId", "monster:1:5"));
            CombatWorld.RequestSkill(8, 10);
            Check(!api.CancelAction(skill.ActionId) && frame.CombatOwnerForIndependentRuntime.PendingSkillForIndependentRuntime == 8,
                "old skill cancellation cannot stop newer native same-ID intent");
            skill = Act(api, "skill.request", Map("skillId", 8, "entityId", "monster:1:5"));
            long skillRevision = frame.CombatOwnerForIndependentRuntime.SkillIntentRevisionForPlugins;
            frame.CombatOwnerForIndependentRuntime.TestResumeSkill(8);
            Check(skillRevision == frame.CombatOwnerForIndependentRuntime.SkillIntentRevisionForPlugins, "native queued-skill promotion retains intent identity");
            NetClient.I.ObserveWriteForPlugins("skill", 8, Task.FromResult(Result.Success()));
            frame.CombatOwnerForIndependentRuntime.PendingSkillForIndependentRuntime = 0;
            Check(api.GetAction(skill.ActionId).State == GameActionState.Sent, "native queued-skill write is observed as Sent");

            api = Setup();
            var pickup = Act(api, "loot.pickup", Map("entityId", "loot:1:10"));
            Check(pickup.State == GameActionState.Running && api.TestSession.Sends == 0 &&
                GameBootstrap.I.Window.Frame.Player.SourceControllerForIndependentRuntime.PendingPickupIdForIndependentRuntime == 10,
                "pickup starts native approach without raw pickup send");
            NetClient.I.ObserveWriteForPlugins("pickup", 10, Task.FromResult(Result.Success()));
            Check(api.GetAction(pickup.ActionId).State == GameActionState.Sent, "correlated native pickup write reports Sent only");
            pickup = Act(api, "loot.pickup", Map("entityId", "loot:1:10"));
            GameBootstrap.I.Window.Frame.Player.SourceControllerForIndependentRuntime.WalkToPickup(10, 100, 100);
            NetClient.I.ObserveWriteForPlugins("pickup", 10, Task.FromResult(Result.Success()));
            Check(api.GetAction(pickup.ActionId).State == GameActionState.Unknown, "replaced pickup does not claim a later same-ID native write");
            GroundItems.TestItems[10].owned = false; GroundItems.TestItems[10].lockEnd = 99;
            Check(Act(api, "loot.pickup", Map("entityId", "loot:1:10")).Code == "loot.locked", "native pickup ownership lock is preserved");

            api = Setup();
            var pending = new TaskCompletionSource<Result>(); api.TestSession.Next = pending.Task;
            var queued = Act(api, "inventory.sort", Map("tab", 0));
            Check(queued.State == GameActionState.Queued, "unfinished gateway task stays queued");
            api.TestScene = 2;
            Check(api.GetAction(queued.ActionId).Code == "scene.changed", "scene interruption never reports invented completion");
            pending.SetResult(Result.Success());
            Check(api.GetAction(queued.ActionId).State == GameActionState.Sent, "late real write wins over scene uncertainty");
            api = Setup(); pending = new TaskCompletionSource<Result>(); api.TestSession.Next = pending.Task;
            queued = Act(api, "inventory.sort", Map("tab", 0));
            Check(api.CancelAction(queued.ActionId), "cancel requests pending writer cancellation");
            pending.SetResult(Result.Success());
            Check(api.GetAction(queued.ActionId).State == GameActionState.Sent, "successful write wins cancellation race");

            var net = NetClient.I; var task = Task.FromResult(Result.Success());
            Check(ReferenceEquals(task, net.ObserveWriteForPlugins("pickup", 4, task)), "native observation returns identical task");
            for (int i = 0; i < 140; i++) net.ObserveWriteForPlugins("pickup", i, task);
            Check(net.ReadWriteForPlugins("pickup", 0) == null && net.ReadWriteForPlugins("pickup", 139) != null, "native write history is bounded");
            api = Setup();
            var retained = Act(api, "inventory.sort", Map("tab", 0), "old");
            for (int i = 0; i < 1024; i++) Act(api, "inventory.sort", Map("tab", 0), "new" + i);
            Check(api.GetAction(retained.ActionId).Code == "action.not_found", "settled action retention bounded to 1024");
            Console.WriteLine("Game action adapter passed: " + checks + " assertions (native substitutes; no gameplay executed).");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}

namespace UnityEngine { internal static class Time { internal static float time; } }
namespace FakeFantasyOnline.Application
{
    public enum ClientSessionState { InWorld, Offline }
    public enum WorldReviveOption : byte { RespawnCharacter }
    public sealed class WorldGroundItemState { public int X, Y; }
    public sealed class WorldNpcState { public int EntityId; public string DisplayName; }
    public sealed class WorldNpcTaskOfferState { public int TaskId; }
    public sealed class WorldNpcTaskOffersState { public List<WorldNpcTaskOfferState> Offers = new List<WorldNpcTaskOfferState>(); }
    public sealed class WorldTaskLogState { public object FindActive(int id) => id == 7 ? new object() : null; }
    public sealed class WorldInventoryItemState { public int BagIndex, ItemId, Count; public byte Tab; public bool Bound, Cosmetic; public object Pet; }
    public sealed class WorldInventoryEquippedItemState { public byte Slot; public int ItemId; }
    public sealed class WorldInventoryState { public List<WorldInventoryItemState> Items = new List<WorldInventoryItemState>(); public List<WorldInventoryEquippedItemState> EquippedItems = new List<WorldInventoryEquippedItemState>(); }
    public sealed class WorldSkillBookState { public int LevelOf(int id) => id == 8 ? 1 : 0; }
    public sealed class WorldLocalPlayerLifeState { public bool Alive = true; public bool Allows(WorldReviveOption option) => true; }
    public sealed class WorldChatSendRequest { public static WorldChatSendRequest ForChannel(byte c, string t, object s) => new WorldChatSendRequest(); public static WorldChatSendRequest ForWhisper(string n, string t, object s) => new WorldChatSendRequest(); }
    public sealed class ClientSessionCoordinator
    {
        public ClientSessionState State = ClientSessionState.InWorld;
        public WorldInventoryState InventoryState = new WorldInventoryState();
        public WorldSkillBookState SkillBookState = new WorldSkillBookState();
        public WorldNpcTaskOffersState NpcTaskOffersState = new WorldNpcTaskOffersState();
        public WorldTaskLogState TaskLogState = new WorldTaskLogState();
        public WorldLocalPlayerLifeState LocalPlayerLifeState = new WorldLocalPlayerLifeState();
        public Dictionary<int, WorldGroundItemState> GroundItems = new Dictionary<int, WorldGroundItemState>();
        public long NpcTaskOffersStateVersion = 1, TaskLogStateVersion = 1;
        public int Sends; public Task<Result> Next;
        private Task<Result> Send() { Sends++; return Next ?? Task.FromResult(Result.Success()); }
        public Task<Result> SortInventoryAsync(byte t, CancellationToken c) => Send();
        public Task<Result> SplitInventoryItemAsync(byte t, int s, int n, CancellationToken c) => Send();
        public Task<Result> EquipInventoryItemAsync(int s, CancellationToken c) => Send();
        public Task<Result> UnequipInventoryItemAsync(int s, CancellationToken c) => Send();
        public Task<Result> RequestNpcTaskOffersAsync(string n, CancellationToken c) => Send();
        public Task<Result> AcceptTaskAsync(int id, CancellationToken c) => Send();
        public Task<Result> CompleteTaskAsync(int id, CancellationToken c) => Send();
        public Task<Result> AbandonTaskAsync(int id, CancellationToken c) => Send();
        public Task<Result> RequestReviveAsync(WorldReviveOption option, CancellationToken c) => Send();
        public Task<Result> SendWorldChatAsync(WorldChatSendRequest r, CancellationToken c) => Send();
        public bool RequestCharacterSkill(int id, float range, CancellationToken c) { CombatWorld.RequestSkill(id, range); return true; }
        public Task<Result> UseCharacterSkillAsync(int id, int target, CancellationToken c) => Send();
        public Task<Result> CastCharacterSkillAtTargetAsync(int id, CancellationToken c) => Send();
        public Task<Result> UseCharacterSkillAtAsync(int id, int x, int y, int target, CancellationToken c) => Send();
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class PluginGameApi
    {
        public ClientSessionCoordinator TestSession; public long TestScene = 1;
        private ClientSessionCoordinator Session => TestSession;
        private long sceneRevision => TestScene;
        public PluginGameApi(ClientSessionCoordinator session) { TestSession = session; }
        private void RequireMainThread() { }
        private void RefreshScene() { }
        private void Emit(string name, IReadOnlyDictionary<string, object> data) { }
        private long CurrentDialogueRevision() => DialogUI.I.Revision;
    }
    internal sealed class GameBootstrap { internal static GameBootstrap I; internal Window Window; }
    internal sealed class Window { internal ReferenceFirstWorldInitialFrame Frame; }
    internal sealed class ReferenceFirstWorldInitialFrame
    {
        internal LocalPlayer Player = new LocalPlayer(); internal CombatWorld CombatOwnerForIndependentRuntime = new CombatWorld();
        internal float MapWidthForIndependentRuntime => 1000; internal float MapHeightForIndependentRuntime => 1000;
        internal Dictionary<int, ReferenceWorldNpcActor> NpcActors = new Dictionary<int, ReferenceWorldNpcActor>();
        internal Dictionary<int, ReferenceWorldMonsterActor> MonsterActors = new Dictionary<int, ReferenceWorldMonsterActor>();
        internal Dictionary<int, ReferenceWorldRemotePlayerActor> RemoteActors = new Dictionary<int, ReferenceWorldRemotePlayerActor>();
        internal TargetSelection TargetSelection = new TargetSelection();
        internal bool TrySelectMonsterIdentity(int id) { TargetSelect.CurrentSourceForIndependentRuntime = MonsterActors[id].SelectableOwner; return true; }
        internal bool TrySelectRemotePlayerIdentity(int id) { TargetSelect.CurrentSourceForIndependentRuntime = RemoteActors[id].SourceIdentityForIndependentRuntime; return true; }
    }
    internal sealed class LocalPlayer
    {
        internal PlayerController SourceControllerForIndependentRuntime = new PlayerController(); internal int EntityId = 1;
        internal bool TrySetPathTo(object mask, float x, float y) { SourceControllerForIndependentRuntime.SetTestPath(); return true; }
    }
    public partial class PlayerController
    {
        private bool dead; private int workSkill;
        public bool IsDead => dead; public float worldX = 10, worldY = 10;
        internal bool MoveToTargetForIndependentRuntime; internal int PendingPickupIdForIndependentRuntime = -1; internal int Stops;
        internal void SetTestPath() { pluginNavigationRevision++; MoveToTargetForIndependentRuntime = true; }
        internal void TestCombatApproach() { SetTestPath(); ObserveCombatApproachForPlugins(); }
        public void StopMove() { pluginNavigationRevision++; MoveToTargetForIndependentRuntime = false; Stops++; }
        public void CancelPickup() { PendingPickupIdForIndependentRuntime = -1; }
        public static void ClearFollow() { }
        public void BreakSit() { }
        internal object CaptureMaskViewForIndependentRuntime() => null;
        public void WalkToPickup(int id, int x, int y) { PendingPickupIdForIndependentRuntime = id; SetTestPath(); }
        internal void StopForNpcPointerForIndependentRuntime() { MoveToTargetForIndependentRuntime = false; }
    }
    internal static class CharData { internal static int immobile, rideAnchorId; }
    internal partial class NetClient { internal static NetClient I; internal static bool IsMiningTool(int id) => false; }
    public partial class CombatWorld
    {
        private static CombatWorld I => Owner;
        internal void TestResumeSkill(int skill) { ResumeQueuedSkillForPlugins(skill, 10); }
        internal static void RequestSkill(int id, float range) { if (!Owner.pluginResumingQueuedSkill) Owner.pluginSkillIntentRevision++; Owner.PendingSkillForIndependentRuntime = id; }
        private static CombatWorld Owner => GameBootstrap.I.Window.Frame.CombatOwnerForIndependentRuntime;
        internal bool AutoAttackingForIndependentRuntime; internal int PendingSkillForIndependentRuntime, QueuedSkillForIndependentRuntime;
        internal static int LocalFaction;
        internal static void StopAttack() { Owner.pluginAttackIntentRevision++; Owner.AutoAttackingForIndependentRuntime = false; CancelPending(); }
        internal static void CancelPending() { Owner.pluginSkillIntentRevision++; Owner.PendingSkillForIndependentRuntime = 0; Owner.QueuedSkillForIndependentRuntime = 0; }
        internal static void ClearTarget() { TargetSelect.Clear(); CancelPending(); }
        internal static void SetTarget(MonsterEntity owner, bool attack) { Owner.pluginAttackIntentRevision++; Owner.pluginSkillIntentRevision++; Owner.AutoAttackingForIndependentRuntime = attack; TargetSelect.Select(owner); }
        internal static void SetPvpTarget(RemotePlayer owner) { Owner.pluginAttackIntentRevision++; Owner.pluginSkillIntentRevision++; Owner.AutoAttackingForIndependentRuntime = true; TargetSelect.Select(owner); }
    }
    internal static class TargetSelect { internal static object CurrentSourceForIndependentRuntime; internal static void Select(object o) { CurrentSourceForIndependentRuntime = o; } internal static void Clear() { CurrentSourceForIndependentRuntime = null; } }
    internal sealed class TargetSelection { internal bool TrySelectNpc(ReferenceWorldNpcActor actor) { TargetSelect.Select(actor.SelectableOwner); return true; } }
    internal sealed class MonsterEntity { internal bool friendly, ally; internal int SelHp; }
    internal sealed class ReferenceWorldMonsterActor { internal MonsterEntity SelectableOwner; internal bool SourceOwnerLive => SelectableOwner != null; }
    internal sealed class RemotePlayer { internal bool IsDead, OpenWorldHostile; internal string SelName = "Other"; }
    internal sealed class RemoteState { internal int Faction; }
    internal sealed class ReferenceWorldRemotePlayerActor { internal RemotePlayer SourceIdentityForIndependentRuntime; internal RemoteState State = new RemoteState(); }
    internal static class ReferenceWorldTargetSelectionMath { internal static bool IsFriendly(int a, int b, bool hostile) => !hostile; }
    internal sealed class ReferenceWorldNpcActor { internal NpcInteract SelectableOwner; internal WorldNpcState State; }
    internal sealed class NpcInteract { internal void Show() { DialogUI.I.Visible = true; } }
    internal sealed class ReferenceNpcDialogOption { internal string Label; }
    internal sealed class DialogUI
    {
        internal static DialogUI I; internal static bool VisibleS => I != null && I.Visible;
        internal bool Visible; internal int CurrentNpcEntityId; internal long Revision; internal int Activations;
        internal List<ReferenceNpcDialogOption> Options = new List<ReferenceNpcDialogOption>();
        internal bool ActivateOption(ReferenceNpcDialogOption option) { Activations++; return true; }
        internal void Close() { Visible = false; }
    }
    internal sealed class GroundItems
    {
        internal sealed class Item { internal object go; internal bool owned; internal float lockEnd; }
        internal static Dictionary<int, Item> TestItems = new Dictionary<int, Item>();
        internal static bool TryGetForIndependentRuntime(int id, out GroundItems owner, out Item item) { owner = new GroundItems(); return TestItems.TryGetValue(id, out item); }
    }
    internal static class Win05 { internal sealed class SkInfo { internal float dist; } internal static SkInfo SkillInfo(int id) => new SkInfo(); }
    internal static class SocialClient { internal static bool TradeActive; }
    internal static class SysMsgBox { internal static bool Visible; }
    internal sealed class InventoryDlg
    {
        internal struct Item { internal int id; }
        internal static Item ItemAt(int tab, int slot) => new Item { id = 44 };
        internal static InventoryDlg EnsureOwnerForIndependentRuntime() => new InventoryDlg();
        internal void ActivateOrdinaryItemForIndependentRuntime(ref int tab, Item item) { }
    }
}
