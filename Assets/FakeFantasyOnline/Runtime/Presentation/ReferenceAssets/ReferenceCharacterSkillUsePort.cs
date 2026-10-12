using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>Main-thread, state-projection and task adapter around the recovered NetClient members.</summary>
public sealed class ReferenceCharacterSkillUsePort : ICharacterSkillUsePort, IDisposable
{
    private readonly ClientSessionCoordinator session;
    private readonly IGameServerGateway writer;
    private readonly Func<GameBootstrap> currentWorld;
    private readonly SynchronizationContext context;
    private readonly int mainThread;
    private readonly NetClient core;
    private CancellationToken activeCancellation;
    private Task<Result> activeWrite;
    private bool collectingWrite;
    private CancellationToken? sideEffectCancellation;
    private CancellationToken SkillWriteCancellation => collectingWrite
        ? activeCancellation
        : sideEffectCancellation ?? currentWorld()?.Window?.SkillSideEffectCancellationForIndependentRuntime
            ?? CancellationToken.None;
    private volatile bool disposed;
    private readonly object queueSync = new object();
    private readonly Dictionary<TaskCompletionSource<Result>, CancellationTokenRegistration> queued =
        new Dictionary<TaskCompletionSource<Result>, CancellationTokenRegistration>();

    internal ReferenceCharacterSkillUsePort(
        NetClient owner,
        ClientSessionCoordinator session,
        IGameServerGateway writer,
        Func<GameBootstrap> currentWorld)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.writer = writer ?? throw new ArgumentNullException(nameof(writer));
        this.currentWorld = currentWorld ?? throw new ArgumentNullException(nameof(currentWorld));
        context = SynchronizationContext.Current;
        mainThread = Thread.CurrentThread.ManagedThreadId;
        core = owner ?? throw new ArgumentNullException(nameof(owner));
        core.BindTargetStatusWriterForIndependentRuntime(id =>
            ObserveTargetStatusWrite(this.session.QueryTargetStatusAsync(id, CancellationToken.None)));
        core.BindPetViewWriterForIndependentRuntime(mask =>
            ObserveSideEffectWrite(this.writer.SetPetViewAsync(mask, CancellationToken.None)));
        core.BindEquipmentWindowCommandsForIndependentRuntime(
            slot => ObserveEquipmentWrite(token => this.session.UnequipInventoryItemAsync(slot, token)),
            points => ObserveEquipmentWrite(token => this.session.AllocateAttributePointsAsync(points, token)),
            title => ObserveEquipmentWrite(token => this.session.SetCharacterTitleAsync(title, token)));
        core.BindEquipmentEffectsWriterForIndependentRuntime(readHideMask =>
            ObserveSideEffectWrite(this.writer.SetEquipmentEffectsMaskAsync(readHideMask, CancellationToken.None)));
        core.BindWorkWritersForIndependentRuntime(
            skill => ObserveSideEffectWrite(this.writer is ICharacterWorkCommands work ? work.BeginCharacterWorkAsync(skill, CancellationToken.None) : Task.FromResult(Result.Failure("work.unsupported", "Work commands are unavailable."))),
            skill => ObserveSideEffectWrite(this.writer is ICharacterWorkCommands work ? work.GatherCharacterWorkAsync(skill, CancellationToken.None) : Task.FromResult(Result.Failure("work.unsupported", "Work commands are unavailable."))));
        core.BindReviveWriterForIndependentRuntime(option => ObserveReviveWrite(
            this.writer is ICharacterReviveCommands revive
                ? revive.ReviveCharacterAsync(option, CancellationToken.None)
                : Task.FromResult(Result.Failure("revive.unsupported", "Revive commands are unavailable."))));
        core.BindRepairWritersForIndependentRuntime(
            (mode, slot, kind, tab) => ObserveSideEffectWrite(this.writer is IRepairCommands repair
                ? repair.RepairAsync(mode, slot, kind, tab, CancellationToken.None)
                : Task.FromResult(Result.Failure("repair.unsupported", "Repair commands are unavailable."))),
            (mode, slot, kind, tab) => ObserveSideEffectWrite(this.writer is IRepairCommands repair
                ? repair.RepairConfirmAsync(mode, slot, kind, tab, CancellationToken.None)
                : Task.FromResult(Result.Failure("repair.unsupported", "Repair commands are unavailable."))));
        core.BindFurnaceWritersForIndependentRuntime(
            mode => ObserveSideEffectWrite(this.writer is IFurnaceCommands furnace
                ? furnace.OpenFurnaceAsync(mode, CancellationToken.None)
                : Task.FromResult(Result.Failure("furnace.unsupported", "Furnace commands are unavailable."))),
            (mode, item) => ObserveSideEffectWrite(this.writer is IFurnaceCommands furnace
                ? furnace.CraftAsync(mode, item, CancellationToken.None)
                : Task.FromResult(Result.Failure("furnace.unsupported", "Furnace commands are unavailable."))),
            (mode, equip, execute, tab, slot, protect) => ObserveSideEffectWrite(this.writer is IFurnaceCommands furnace
                ? furnace.CraftUpgradeAsync(mode, equip, execute, tab, slot, protect, CancellationToken.None)
                : Task.FromResult(Result.Failure("furnace.unsupported", "Furnace commands are unavailable."))),
            (kind, tab, slot, execute, protect) => ObserveSideEffectWrite(this.writer is IFurnaceCommands furnace
                ? furnace.DrillAsync(kind, tab, slot, execute, protect, CancellationToken.None)
                : Task.FromResult(Result.Failure("furnace.unsupported", "Furnace commands are unavailable."))),
            (kind, tab, slot, hole, runeTab, runeSlot) => ObserveSideEffectWrite(this.writer is IFurnaceCommands furnace
                ? furnace.InlayAsync(kind, tab, slot, hole, runeTab, runeSlot, CancellationToken.None)
                : Task.FromResult(Result.Failure("furnace.unsupported", "Furnace commands are unavailable."))));
        core.BindChangeHairWriterForIndependentRuntime((mode, targetId) =>
            ObserveSideEffectWrite(this.writer is ICharacterAppearanceCommands appearance
                ? appearance.ChangeHairAsync(mode, targetId, CancellationToken.None)
                : Task.FromResult(Result.Failure("appearance.unsupported", "Appearance commands are unavailable."))));
        core.BindShopWritersForIndependentRuntime(
            shopId => ObserveSideEffectWrite(this.writer is IShopCommands shop
                ? shop.OpenNativeShopAsync(shopId, CancellationToken.None)
                : Task.FromResult(Result.Failure("shop.unsupported", "Shop commands are unavailable."))),
            (itemId, count) => ObserveSideEffectWrite(this.writer is IShopCommands shop
                ? shop.BuyItemAsync(itemId, count, CancellationToken.None)
                : Task.FromResult(Result.Failure("shop.unsupported", "Shop commands are unavailable."))),
            (tab, slot, count, itemId, confirmed) => ObserveSideEffectWrite(this.writer is IShopCommands shop
                ? shop.SellItemAsync(tab, slot, count, itemId, confirmed, CancellationToken.None)
                : Task.FromResult(Result.Failure("shop.unsupported", "Shop commands are unavailable."))),
            (ownerId, index, count, tab, slot, expectedId, expectedPrice) => ObserveSideEffectWrite(this.writer is IShopCommands shop
                ? shop.StallDealAsync(ownerId, index, count, tab, slot, expectedId, expectedPrice, CancellationToken.None)
                : Task.FromResult(Result.Failure("shop.unsupported", "Shop commands are unavailable."))));
        core.BindItemUseWritersForIndependentRuntime(
            (itemId, tab, slot) => ObserveSideEffectWrite(core.ObserveWriteForPlugins("item", itemId,
                this.writer is IItemUseCommands use
                    ? use.UseItemAsync(itemId, tab, slot, CancellationToken.None)
                    : Task.FromResult(Result.Failure("item.use.unsupported", "Item use is unavailable.")))),
            (targetId, itemId) => ObserveSideEffectWrite(core.ObserveWriteForPlugins("item", itemId,
                this.writer is ICaptureCommands targeted
                    ? targeted.CaptureAsync(targetId, itemId, CancellationToken.None)
                    : Task.FromResult(Result.Failure("item.targeted.unsupported", "Targeted item use is unavailable.")))),
            (text, color) => ChatUI.Add("系统", text, color));
        core.BindCaptureWriterForIndependentRuntime((targetId, itemId) =>
            ObserveSideEffectWrite(core.ObserveWriteForPlugins("item", itemId,
                this.writer is ICaptureCommands capture
                    ? capture.CaptureAsync(targetId, itemId, CancellationToken.None)
                    : Task.FromResult(Result.Failure("capture.unsupported", "Capture commands are unavailable.")))));
        core.BindWashAffixWriterForIndependentRuntime((kind, tab, slot, index) =>
            ObserveSideEffectWrite(this.writer is IAttributeWashCommands wash
                ? wash.WashAffixAsync(kind, tab, slot, index, CancellationToken.None)
                : Task.FromResult(Result.Failure("attribute.wash.unsupported", "Attribute washing is unavailable."))));
        core.BindEquipFuseWriterForIndependentRuntime((op, mainKind, mainTab, mainSlot, subTab, subSlot, sig) =>
            ObserveSideEffectWrite(this.writer is IEquipmentFusionCommands fusion
                ? fusion.EquipFuseAsync(op, mainKind, mainTab, mainSlot, subTab, subSlot, sig, CancellationToken.None)
                : Task.FromResult(Result.Failure("equipment.fuse.unsupported", "Equipment fusion is unavailable."))));
        core.BindUseItemOnWriterForIndependentRuntime((toolId, targetKind, tab, slot) =>
            ObserveSideEffectWrite(this.writer is IItemToolCommands tools
                ? tools.UseItemOnAsync(toolId, targetKind, tab, slot, CancellationToken.None)
                : Task.FromResult(Result.Failure("item.tool.unsupported", "Item tools are unavailable."))));
        core.BindApplyAvatarWriterForIndependentRuntime((tab, slot, target, kind, targetKind, targetTab) =>
            ObserveSideEffectWrite(this.writer is IAvatarFusionCommands fusion
                ? fusion.ApplyAvatarAsync(tab, slot, target, kind, targetKind, targetTab, CancellationToken.None)
                : Task.FromResult(Result.Failure("avatar.apply.unsupported", "Avatar fusion is unavailable."))));
        core.BindPickItemWriterForIndependentRuntime(groundId =>
            ObserveSideEffectWrite(core.ObserveWriteForPlugins("pickup", groundId,
                this.writer is IGroundPickupCommands pickup
                    ? pickup.PickItemAsync(groundId, CancellationToken.None)
                    : Task.FromResult(Result.Failure("ground-item.pickup.unsupported", "Pickup is unavailable.")))));
        core.BindHistoryWritersForIndependentRuntime(
            text => ObserveSideEffectWrite(this.writer is IHistoryCommands history
                ? history.SendFeedbackAsync(text, CancellationToken.None)
                : Task.FromResult(Result.Failure("history.unsupported", "History commands are unavailable."))),
            text => ObserveSideEffectWrite(this.writer is IHistoryCommands history
                ? history.SendVoteAddAsync(text, CancellationToken.None)
                : Task.FromResult(Result.Failure("history.unsupported", "History commands are unavailable."))),
            (id, stance) => ObserveSideEffectWrite(this.writer is IHistoryCommands history
                ? history.SendVoteCastAsync(id, stance, CancellationToken.None)
                : Task.FromResult(Result.Failure("history.unsupported", "History commands are unavailable."))));
        core.BindStallBrowseWriterForIndependentRuntime(ownerId =>
            ObserveSideEffectWrite(this.writer is IStallCommands stall
                ? stall.StallBrowseAsync(ownerId, CancellationToken.None)
                : Task.FromResult(Result.Failure("stall.unsupported", "Stall commands are unavailable."))));
        core.BindStallWritersForIndependentRuntime(
            (type, name) => ObserveSideEffectWrite(this.writer is IStallCommands stall
                ? stall.StallCreateAsync(type, name, CancellationToken.None)
                : Task.FromResult(Result.Failure("stall.unsupported", "Stall commands are unavailable."))),
            (tab, slot, id, count, price) => ObserveSideEffectWrite(this.writer is IStallCommands stall
                ? stall.StallAddAsync(tab, slot, id, count, price, CancellationToken.None)
                : Task.FromResult(Result.Failure("stall.unsupported", "Stall commands are unavailable."))),
            () => ObserveSideEffectWrite(this.writer is IStallCommands stall
                ? stall.StallEndAsync(CancellationToken.None)
                : Task.FromResult(Result.Failure("stall.unsupported", "Stall commands are unavailable."))));
        core.BindSortBagWriterForIndependentRuntime(tab =>
            ObserveSideEffectWrite(this.writer.SortInventoryAsync(tab, CancellationToken.None)));
        core.BindEquipItemWriterForIndependentRuntime(bagIdx =>
            ObserveSideEffectWrite(this.writer.EquipInventoryItemAsync(bagIdx, CancellationToken.None)));
        core.BindWardrobeStoreWriterForIndependentRuntime((tab, slot) =>
            ObserveSideEffectWrite(this.writer.StoreWardrobeItemAsync(tab, slot, CancellationToken.None)));
        core.BindWardrobeRemoveWriterForIndependentRuntime(itemId =>
            ObserveSideEffectWrite(this.writer.ExtractWardrobeItemAsync(itemId, CancellationToken.None)));
        core.BindWardrobeWearWriterForIndependentRuntime(itemId =>
            ObserveSideEffectWrite(this.writer.ToggleWardrobeItemAsync(itemId, CancellationToken.None)));
        core.BindWardrobeMoveWriterForIndependentRuntime((itemId, toIndex) =>
            ObserveSideEffectWrite(this.writer.MoveWardrobeItemAsync(itemId, toIndex, CancellationToken.None)));
        core.BindItemLockWriterForIndependentRuntime(request => ObserveSideEffectWrite(
            this.writer is IItemLockCommands itemLock
                ? itemLock.SetItemLockAsync(request, CancellationToken.None)
                : Task.FromResult(Result.Failure("item.lock.unsupported", "Item locking is unavailable."))));
        core.BindAccountSecurityWritersForIndependentRuntime(
            (oldPassword, newPassword) => ObserveSideEffectWrite(this.writer is IAccountSecurityCommands security
                ? security.ChangePasswordAsync(oldPassword, newPassword, CancellationToken.None)
                : Task.FromResult(Result.Failure("account.security.unsupported", "Account security is unavailable."))),
            (password, oldCode, newCode) => ObserveSideEffectWrite(this.writer is IAccountSecurityCommands security
                ? security.ChangeSecurityCodeAsync(password, oldCode, newCode, CancellationToken.None)
                : Task.FromResult(Result.Failure("account.security.unsupported", "Account security is unavailable."))));
        core.BindChangeSetForIndependentRuntime(
            (tab, slot, cell) => ObserveSideEffectWrite(this.session.ChangeSetCommands.ChangeSetPutAsync(tab, slot, cell, CancellationToken.None)),
            cell => ObserveSideEffectWrite(this.session.ChangeSetCommands.ChangeSetTakeAsync(cell, CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.ChangeSetCommands.ChangeSetSwapAsync(CancellationToken.None)),
            slot => ObserveSideEffectWrite(this.session.DeployPetAsync(slot, CancellationToken.None)));
        core.BindPetHatchWriterForIndependentRuntime(slot =>
            ObserveSideEffectWrite(this.session.HatchPetAsync(slot, CancellationToken.None)));
        CharData.BindPetSwitchWriterForIndependentRuntime(slot =>
            ObserveSideEffectWrite(this.session.SwitchPetAsync(slot, CancellationToken.None)));
        // NetClient.PetRecall 0x06000738, PetSwap 0x0600073d and PetAddPoint 0x06000734
        // are official pet bodies Win05.DrawPet 0x06001043 reaches inside the drawing pass, so they
        // take the same session/gateway/Protocol route with no second pump and no re-encode. The route is
        // synchronous on the existing host/None route: each gateway invokes the recovered sender
        // before returning its completed observation Task. Off-host/cancellable adapter behavior
        // remains qualified; the per-click count and argument values are unchanged.
        core.BindPetAddPointWriterForIndependentRuntime(attributeCode =>
            ObserveSideEffectWrite(this.session.AllocatePetPointAsync(attributeCode, CancellationToken.None)));
        // PetRename's existing host/None gateway entry invokes the native sender before
        // returning this observation Task, preserving SubmitPetRename's normal-return tail.
        core.BindPetRenameWriterForIndependentRuntime(name =>
            ObserveSideEffectWrite(this.writer.RenamePetAsync(name, CancellationToken.None)));
        core.BindPetRecallWriterForIndependentRuntime(slot =>
            ObserveSideEffectWrite(this.session.RecallPetAsync(slot, CancellationToken.None)));
        core.BindPetSwapWriterForIndependentRuntime((fromSlot, toSlot) =>
            ObserveSideEffectWrite(this.session.SwapPetAsync(fromSlot, toSlot, CancellationToken.None)));
        // NetClient.PetShow 0x0600073b is the fifth official pet body Win05.DrawPet 0x06001043
        // reaches inside the drawing arm - the display checkbox at (17,520,97,14) - so it takes
        // the same route: the drawing arm echoes into CharData.petShow first and then calls the
        // member at the hit, with no second pump in between.
        core.BindPetShowWriterForIndependentRuntime(show =>
            ObserveSideEffectWrite(this.session.SetPetShownAsync(show, CancellationToken.None)));
        core.BindInventoryPlacementForIndependentRuntime(
            (tab, from, to) => ObserveSideEffectWrite(this.writer is IInventoryPlacementCommands placement
                ? placement.ItemMoveAsync(tab, from, to, CancellationToken.None)
                : Task.FromResult(Result.Failure("inventory.move.unsupported", "Inventory movement is unavailable."))),
            (tab, slot, x, y, id, confirmed) => ObserveSideEffectWrite(this.writer is IInventoryPlacementCommands placement
                ? placement.DropItemAsync(tab, slot, x, y, id, confirmed, CancellationToken.None)
                : Task.FromResult(Result.Failure("inventory.drop.unsupported", "Item dropping is unavailable."))));
        core.BindWarehouseActionsForIndependentRuntime(
            () => ObserveSideEffectWrite(this.session.OpenWarehouseAsync(CancellationToken.None)),
            tab => ObserveSideEffectWrite(this.writer is IWarehouseSortCommands warehouse
                ? warehouse.SortWarehouseAsync(tab, CancellationToken.None)
                : Task.FromResult(Result.Failure("warehouse.sort.unsupported", "Warehouse sorting is unavailable."))),
            this.writer.WarehouseMoney,
            (tab, slot, count) => ObserveSideEffectWrite(this.session.RequestWarehouseSplitAsync(tab, slot, count, CancellationToken.None)),
            (fromTab, fromSlot, toTab, toSlot) => ObserveSideEffectWrite(this.session.RequestWarehouseRearrangeAsync(fromTab, fromSlot, toTab, toSlot, CancellationToken.None)),
            (tab, slot, count) => ObserveSideEffectWrite(this.writer.SplitInventoryItemAsync(tab, slot, count, CancellationToken.None)));
        core.BindBuyWarehouseWriterForIndependentRuntime(() => ObserveSideEffectWrite(
            this.writer is IWarehouseCommands warehouse
                ? warehouse.BuyWarehouseAsync(CancellationToken.None)
                : Task.FromResult(Result.Failure("warehouse.buy.unsupported", "Warehouse expansion is unavailable."))));
        core.BindWarehouseMoveWriterForIndependentRuntime(request => ObserveSideEffectWrite(
            this.writer is IWarehouseCommands warehouse
                ? warehouse.WarehouseMoveAsync(request, CancellationToken.None)
                : Task.FromResult(Result.Failure("warehouse.move.unsupported", "Warehouse movement is unavailable."))));
        core.BindPetAwakenWriterForIndependentRuntime(() => ObserveSideEffectWrite(
            this.writer is IPetForgeCommands forge
                ? forge.PetAwakenAsync(CancellationToken.None)
                : Task.FromResult(Result.Failure("pet.awaken.unsupported", "Pet awakening is unavailable."))));
        core.BindPetForgeWriterForIndependentRuntime(request => ObserveSideEffectWrite(
            this.writer is IPetForgeCommands forge
                ? forge.PetForgeAsync(request, CancellationToken.None)
                : Task.FromResult(Result.Failure("pet.forge.unsupported", "Pet forging is unavailable."))));
        core.BindHotbarWriterForIndependentRuntime((ids, kinds, expanded) => ObserveSideEffectWrite(
            this.writer is IHotbarWriteCommands hotbar
                ? hotbar.WriteHotbarAsync(ids, kinds, expanded, CancellationToken.None)
                : Task.FromResult(Result.Failure("hotbar.writer.unsupported", "Hotbar writing is unavailable."))));
        core.BindAscendSkillWriterForIndependentRuntime(id => ObserveSideEffectWrite(
            this.writer is IAscendSkillCommands ascend
                ? ascend.AscendSkillAsync(id, CancellationToken.None)
                : Task.FromResult(Result.Failure("skill.ascend.unsupported", "Skill cultivation is unavailable."))));
        core.BindUpgradeSkillWriterForIndependentRuntime(id =>
            ObserveSideEffectWrite(this.writer.UpgradeSkillAsync(id, CancellationToken.None)));
        core.BindLearnLifeWriterForIndependentRuntime(id =>
            ObserveSideEffectWrite(this.writer.LearnLifeSkillAsync(id, CancellationToken.None)));
        core.BindTipSeenWriterForIndependentRuntime(id => ObserveSideEffectWrite(
            this.writer is INewbieTipCommands tips
                ? tips.MarkNewbieTipSeenAsync(id, CancellationToken.None)
                : Task.FromResult(Result.Failure("newbie.tip.unsupported", "Newbie tip commands are unavailable."))));
        core.BindChatWriterForIndependentRuntime(request =>
            ObserveSideEffectWrite(request.Kind == WorldChatSendKind.Whisper
                ? this.writer.SendWorldChatAsync(request, CancellationToken.None)
                : this.session.SendWorldChatAsync(request, CancellationToken.None)));
        // TransportUI.DoTeleport (0x06000f96) drives the single official transport command
        // NetClient.Teleport (0x060006e9, opcode 0x1072): it passes the transport list of the NPC
        // and the index of the row the player selected. Nothing here pre-checks the destination,
        // the money or the map - the row price and the resulting map switch stay server-authored,
        // and the client only paints the new map when the server's own teleport downlink arrives.
        core.BindOpenTeleportWriterForIndependentRuntime(transportList =>
            ObserveSideEffectWrite(
                this.session.OpenTransportAsync(transportList, CancellationToken.None)));
        core.BindTeleportWriterForIndependentRuntime((transportList, index) =>
            ObserveSideEffectWrite(
                this.session.ChooseTransportAsync(transportList, index, CancellationToken.None)));
        core.BindRenameCharForIndependentRuntime(name =>
            ObserveSideEffectWrite(this.writer is IRenameCharacterCommands rename
                ? rename.RenameCharacterAsync(name, CancellationToken.None)
                : Task.FromResult(Result.Failure("rename.unsupported", "Rename commands are unavailable."))));
        // The share list is forwarded as the caller built it: the official writer captures the
        // list itself (0x060006dc stores it in the display class, 0x060008ae hands the same
        // reference to WriteShares), so no conversion to another shape happens here and a null
        // list reaches the writer unchanged.
        core.BindHornWriterForIndependentRuntime((text, itemId, skin, shares) =>
            ObserveSideEffectWrite(this.writer is IHornCommands horn
                ? horn.SendHornAsync(text, itemId, skin, shares, CancellationToken.None)
                : Task.FromResult(Result.Failure("horn.unsupported", "Horn commands are unavailable."))));
        // Preserve the native Byte argument; SystemInfoBar currently supplies 0/1,
        // while SysMsgRespond itself forwards every byte value without normalization.
        // The boot-lifetime writer also serves receipt-created notices before world entry.
        core.BindSystemRequestWriterForIndependentRuntime((requestId, accept) =>
            ObserveSideEffectWrite(this.writer.RespondSystemRequestAsync(requestId, accept, CancellationToken.None)));
        core.BindSettingsWritersForIndependentRuntime(
            mode => ObserveSideEffectWrite(this.writer is ICharacterSettingsCommands settings
                ? settings.SetCharacterPkModeAsync(mode, CancellationToken.None) : SettingsUnavailable()),
            mode => ObserveSideEffectWrite(this.writer is ICharacterSettingsCommands settings
                ? settings.SetCharacterGlowModeAsync(mode, CancellationToken.None) : SettingsUnavailable()),
            enabled => ObserveSideEffectWrite(this.writer is ICharacterSettingsCommands settings
                ? settings.SetCharacterSmartCastAsync(enabled, CancellationToken.None) : SettingsUnavailable()),
            quality => ObserveSideEffectWrite(this.writer is ICharacterSettingsCommands settings
                ? settings.SetPetPickQualityAsync(quality, CancellationToken.None) : SettingsUnavailable()));
        core.BindFittingCommandsForIndependentRuntime(
            () => ObserveSideEffectWrite(this.session.RequestRackFittingCatalogAsync(CancellationToken.None)),
            (kind, part, search, page, perPage) => ObserveSideEffectWrite(this.session.RequestRackFittingPageAsync(kind, part, search, page, perPage, CancellationToken.None)),
            ids => ObserveSideEffectWrite(this.session.RequestRackFittingDescriptionsAsync(ids, CancellationToken.None)));
        core.BindRackCommandsForIndependentRuntime(
            () => ObserveSideEffectWrite(this.session.RequestRackOpenAsync(CancellationToken.None)),
            (itemId, source, count) => ObserveSideEffectWrite(this.session.RequestRackBuyAsync(itemId, source, count, CancellationToken.None)),
            (chain, amount) => ObserveSideEffectWrite(this.session.RequestRackDepositAsync(chain, amount, CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.RequestRackRefundListAsync(CancellationToken.None)),
            (itemId, unitPrice, bundle, day, shares) => ObserveSideEffectWrite(this.session.RequestRackRefundAsync(itemId, unitPrice, bundle, day, shares, CancellationToken.None)));
        core.BindFamilyCommandsForIndependentRuntime(
            () => ObserveSideEffectWrite(this.session.RequestFamilyListAsync(CancellationToken.None)),
            name => ObserveSideEffectWrite(this.session.RequestFamilyApplyAsync(name, CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.RequestFamilyRefreshAsync(CancellationToken.None)),
            (op, name, id, text) => ObserveSideEffectWrite(
                this.session.RequestFamilyManageAsync(op, name, id, text, CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.RequestFamilyLeaveAsync(CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.RequestFamilyPositionsAsync(CancellationToken.None)),
            (id, name) => ObserveSideEffectWrite(this.session.RequestFamilyPositionNameAsync(id, name, CancellationToken.None)),
            (title, body) => ObserveSideEffectWrite(this.session.RequestFamilyMailAsync(title, body, CancellationToken.None)));
        core.BindFamilyCreateForIndependentRuntime(
            (name, proclaim) => ObserveSideEffectWrite(
                this.session.RequestFamilyCreateAsync(name, proclaim, CancellationToken.None)));
        core.BindFamilyStashCommandsForIndependentRuntime(
            (op, bagTab, bagSlot, index) => ObserveSideEffectWrite(
                this.session.RequestFamilyStashAsync(op, bagTab, bagSlot, index, CancellationToken.None)),
            pos => ObserveSideEffectWrite(
                this.session.RequestFamilyStashPermissionAsync(pos, CancellationToken.None)));
        core.BindTaskCommandsForIndependentRuntime(
            name => ObserveSideEffectWrite(this.session.RequestNpcTaskOffersAsync(name, CancellationToken.None)),
            id => ObserveSideEffectWrite(this.session.AcceptTaskAsync(id, CancellationToken.None)),
            id => ObserveSideEffectWrite(this.session.AbandonTaskAsync(id, CancellationToken.None)),
            id => ObserveSideEffectWrite(this.session.CompleteTaskAsync(id, CancellationToken.None)));
        core.BindHelpRequestForIndependentRuntime(() => ObserveSideEffectWrite(
            this.session.RequestRackHelpAsync(CancellationToken.None)));
        core.BindPlayerMenuWriterForIndependentRuntime(this.writer);
        core.BindPlayerActionCommandsForIndependentRuntime(
            id => ObserveSideEffectWrite(this.writer.RequestTradeAsync(id, CancellationToken.None)),
            id => ObserveSideEffectWrite(this.writer.InspectPlayerAsync(id, CancellationToken.None)));
        // TradeDlg.SendOffer (06000f67) and TradeDlg.OnGUI (06000f68) drive the four official
        // NetClient trade writers. The offer list travels unchanged: official TradeOffer captures
        // the caller's live list by reference and NetClient.Send reads it when the writer runs, so
        // a projection here would move both the null case and the read time.
        core.BindTradeCommandsForIndependentRuntime(
            (money, items) => ObserveSideEffectWrite(this.session.SubmitTradeOfferAsync(
                money, items, CancellationToken.None)),
            locked => ObserveSideEffectWrite(this.session.SetTradeLockAsync(locked, CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.ConfirmTradeAsync(CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.CancelTradeAsync(CancellationToken.None)));
        // ArenaDeathDlg.OnGUI (0x0600003c) drives the single official arena command
        // NetClient.ArenaRevive (0x06000746): the revive button passes false, the give-up button
        // passes true. Both are decided by the server, so nothing here pre-checks revives, balance
        // or the countdown - the window's own enabled test is the only client-side gate.
        core.BindArenaReviveWriterForIndependentRuntime(surrender => ObserveSideEffectWrite(
            this.session.ArenaReviveAsync(surrender, CancellationToken.None)));
        // ArenaDraftDlg.Pick (0x06000052) and Reroll (0x06000051) drive the two official arena draft
        // commands NetClient.ArenaPick (0x06000744, opcode 0x1083) and NetClient.ArenaReroll
        // (0x06000745, opcode 0x1086). The pick passes the clicked card slot; the reroll passes
        // nothing. Which talent a slot holds and what the reroll costs are decided by the server, so
        // nothing here pre-checks cards, balance or the countdown - the window's own enabled tests
        // are the only client-side gates.
        core.BindArenaDraftWritersForIndependentRuntime(
            index => ObserveSideEffectWrite(this.session.ArenaPickAsync(index, CancellationToken.None)),
            () => ObserveSideEffectWrite(this.session.ArenaRerollAsync(CancellationToken.None)));
        // The existing host/None PetSetSkill binding now returns only after the native sender.
        // Its caller's active-skill echo and Event.Use therefore follow normal completion.
        core.BindPetSetSkillWriterForIndependentRuntime(id => ObserveSideEffectWrite(
            this.session.SetPetSkillAsync(id, CancellationToken.None)));
        core.BindPetSkillWriterForIndependentRuntime(id => ObserveSideEffectWrite(
            this.session.UsePetSkillAsync(id, CancellationToken.None)));
        core.BindSkillCommandsForIndependentRuntime(
            (text, scale) => this.currentWorld()?.Window?.ShowSkillTipForIndependentRuntime(text, scale),
            (id, target) => RecordSkillWrite(core.ObserveWriteForPlugins("skill", id,
                this.writer.UseCharacterSkillAsync(id, target, SkillWriteCancellation))),
            (id, x, y, target) => RecordSkillWrite(core.ObserveWriteForPlugins("skill", id,
                this.writer.UseCharacterSkillAtAsync(id, x, y, target, SkillWriteCancellation))),
            sitting => ObserveSideEffectWrite(this.writer.SetPlayerSittingAsync(sitting, CancellationToken.None)),
            () => ObserveSideEffectWrite(this.writer.EndCharacterWorkAsync(CancellationToken.None)));
    }

    private static Task<Result> SettingsUnavailable() => Task.FromResult(
        Result.Failure("settings.unsupported", "Settings commands are unavailable."));

    private void RecordSkillWrite(Task<Result> pending)
    {
        if (collectingWrite) activeWrite = pending;
        else ObserveSideEffectWrite(pending);
    }

    public bool RequestSkill(int skillId, float rangePixels, CancellationToken cancellationToken) =>
        InvokeNativeDecision(() => CombatWorld.RequestSkill(skillId, rangePixels), cancellationToken);

    private bool InvokeNativeDecision(Action invoke, CancellationToken cancellationToken)
    {
        if (disposed || core == null || cancellationToken.IsCancellationRequested ||
            session.State != ClientSessionState.InWorld ||
            Thread.CurrentThread.ManagedThreadId != mainThread) return false;
        ProjectCurrentState();
        if (currentWorld()?.Window?.Frame == null) return false;
        CancellationToken? previous = sideEffectCancellation;
        sideEffectCancellation = cancellationToken;
        try { invoke(); }
        finally { sideEffectCancellation = previous; }
        return true;
    }

    private async void ObserveReviveWrite(Task<Result> pending)
    {
        try
        {
            currentWorld()?.Window?.TrackReviveRequestForIndependentRuntime(pending);
            await pending;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    // Target polling has no outstanding-task state. Observe every completion through the
    // session port, but never put it in a window command list whose failure can stop DrawHud
    // before the next due poll. Map disposal neither cancels the native deadline nor this send.
    private async void ObserveTargetStatusWrite(Task<Result> pending)
    {
        try
        {
            await pending;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    private void ObserveEquipmentWrite(Func<CancellationToken, Task<Result>> begin)
    {
        ReferenceFirstWorldWindowOwner owner = currentWorld()?.Window;
        CancellationToken cancellationToken = owner == null ? CancellationToken.None :
            owner.EquipmentCommandCancellationForIndependentRuntime;
        // A live native callback finishes its send before GUI continuation; retain cancelled admission.
        Task<Result> pending = begin(cancellationToken.IsCancellationRequested
            ? cancellationToken : CancellationToken.None);
        if (owner != null) owner.TrackEquipmentCommandForIndependentRuntime(pending);
        else ObserveSideEffectWrite(pending);
    }

    private async void ObserveSideEffectWrite(Task<Result> pending)
    {
        try
        {
            currentWorld()?.Window?.TrackSkillSideEffectForIndependentRuntime(pending);
            await pending;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    public Task<Result> UseSkillAsync(int skillId, int targetId, CancellationToken cancellationToken)
    {
        return Dispatch(() => core.UseSkill(skillId, targetId), cancellationToken);
    }

    public Task<Result> CastAtTargetAsync(int skillId, CancellationToken cancellationToken)
    {
        return Dispatch(() =>
        {
            CombatWorld owner = currentWorld()?.Window?.Frame?.CombatOwnerForIndependentRuntime;
            if (owner != null) owner.CastAtTargetForIndependentRuntime(skillId);
        }, cancellationToken);
    }

    public Task<Result> UseSkillAtAsync(
        int skillId, int x, int y, int targetId, CancellationToken cancellationToken)
    {
        return Dispatch(() => core.UseSkillAt(skillId, x, y, targetId), cancellationToken);
    }

    private Task<Result> Dispatch(Action invoke, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested) return Task.FromCanceled<Result>(cancellationToken);
        if (disposed) return Unavailable();
        if (Thread.CurrentThread.ManagedThreadId == mainThread)
        {
            return InvokeOnMainThread(invoke, cancellationToken);
        }
        if (context == null)
        {
            return Task.FromResult(Result.Failure(
                "skill.owner.thread.unavailable", "The client main-thread dispatcher is unavailable."));
        }
        TaskCompletionSource<Result> completion = new TaskCompletionSource<Result>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        lock (queueSync)
        {
            if (disposed) return Unavailable();
            queued.Add(completion, default(CancellationTokenRegistration));
        }
        CancellationTokenRegistration registration = cancellationToken.Register(() =>
        {
            CancellationTokenRegistration registered;
            if (TakeQueued(completion, out registered))
            {
                completion.TrySetCanceled();
                registered.Dispose();
            }
        });
        bool retained;
        lock (queueSync)
        {
            retained = queued.ContainsKey(completion);
            if (retained) queued[completion] = registration;
        }
        if (!retained)
        {
            registration.Dispose();
            return completion.Task;
        }
        try
        {
            context.Post(async ignored =>
            {
                CancellationTokenRegistration registered;
                if (!TakeQueued(completion, out registered)) return;
                registered.Dispose();
                try
                {
                    // All Unity work finishes in InvokeOnMainThread. Completion needs no later UI pump.
                    completion.TrySetResult(await InvokeOnMainThread(invoke, cancellationToken).ConfigureAwait(false));
                }
                catch (OperationCanceledException) { completion.TrySetCanceled(); }
                catch (Exception exception) { completion.TrySetException(exception); }
            }, null);
        }
        catch (Exception exception)
        {
            CancellationTokenRegistration registered;
            if (TakeQueued(completion, out registered))
            {
                registered.Dispose();
                completion.TrySetException(exception);
            }
        }
        return completion.Task;
    }

    private bool TakeQueued(TaskCompletionSource<Result> completion, out CancellationTokenRegistration registration)
    {
        lock (queueSync)
        {
            if (!queued.TryGetValue(completion, out registration)) return false;
            queued.Remove(completion);
            return true;
        }
    }

    private static Task<Result> Unavailable() => Task.FromResult(
        Result.Failure("skill.owner.unavailable", "The skill owner is unavailable."));

    private Task<Result> InvokeOnMainThread(Action invoke, CancellationToken cancellationToken)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled<Result>(cancellationToken);
        }
        if (disposed || core == null)
        {
            return Task.FromResult(Result.Failure("skill.owner.unavailable", "The skill owner is unavailable."));
        }
        if (session.State != ClientSessionState.InWorld)
        {
            return Task.FromResult(Result.Failure("session.stage.invalid", "Skills require an active world session."));
        }

        ProjectCurrentState();
        activeCancellation = cancellationToken;
        activeWrite = null;
        collectingWrite = true;
        try { invoke(); }
        finally { collectingWrite = false; }
        return activeWrite ?? Task.FromResult(Result.Failure(
            "skill.request.not.sent", "The native skill entry returned without sending a request."));
    }

    // Composition-root binding: native UI/input entries bypass Dispatch, but need the same state.
    public void ProjectCurrentStateForIndependentRuntime()
    {
        if (disposed || core == null || session.State != ClientSessionState.InWorld ||
            Thread.CurrentThread.ManagedThreadId != mainThread) return;
        ProjectCurrentState();
    }

    private void ProjectCurrentState()
    {
        // Skill roots are written by the receive owner on both Pump paths, including failed
        // required reads. A UI/command projection must never restore an older mirrored book.
        // WBB-074: CharData.mp (+0x60) is written officially by TitleFlow.DoEnterWorld
        // (0x06000efd) from element 5 of the entry array and by the 0x8007 case of
        // NetClient.Dispatch (0x0600077f) from element 5 of its first array, both of which the
        // bound receive owner performs with the official gate - the entry stores nothing for a
        // null or empty array and nothing below element 5 for a length of one to six, and 0x8007
        // stores nothing below seven. This projection used to re-publish the Application
        // snapshot's MagicPoints over it; that has no official store behind it and, because
        // WorldPlayerStatusState.FromEntry turned an absent index into 0 (WBB-075 keeps the value
        // the official skip leaves there instead), so it pushed a 0 over the mp an earlier 0x8007
        // had stored. It is still a second writer with no official store behind it, so the receive
        // owner stays the single writer.

        GameBootstrap world = currentWorld();
        ReferenceFirstWorldInitialFrame frame = world?.Window?.Frame;
        frame?.CombatOwnerForIndependentRuntime?.ActivateForIndependentRuntime();
        // Awake publishes the real local receiver even if optional Window/Frame admission
        // fails. Never erase that native identity because its projection is absent.
        PlayerController player = PlayerController.I;
        CombatNumber.BindPlayerTransformForIndependentRuntime(player == null ? null : player.transform);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }
        disposed = true;
        KeyValuePair<TaskCompletionSource<Result>, CancellationTokenRegistration>[] abandoned;
        lock (queueSync)
        {
            abandoned = new KeyValuePair<TaskCompletionSource<Result>, CancellationTokenRegistration>[queued.Count];
            ((ICollection<KeyValuePair<TaskCompletionSource<Result>, CancellationTokenRegistration>>)queued).CopyTo(abandoned, 0);
            queued.Clear();
        }
        foreach (KeyValuePair<TaskCompletionSource<Result>, CancellationTokenRegistration> entry in abandoned)
        {
            entry.Value.Dispose();
            entry.Key.TrySetResult(Result.Failure("skill.owner.unavailable", "The skill owner was released."));
        }
        session.UnbindCharacterSkillUsePort(this);
        // The host's Unity destruction owns NetClient.OnDestroy and its Close.
        // Retiring this command adapter neither destroys the component early nor
        // clears the original I field before Unity establishes destroyed liveness.
    }
}

}
