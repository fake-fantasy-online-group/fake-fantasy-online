using System;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Fixed no-input source-entry observer for the official 1.5.8 inventory/equipment slice.
/// </summary>
internal static class ReferenceInventoryEquipmentSourceCheck
{
    public const string Argument =
        "--ffo-inventory-equipment-source-check-1-5-8";
    public const string ManualArgument =
        "--ffo-inventory-equipment-manual-check-1-5-8";
    public const string Marker =
        "FFO_INVENTORY_EQUIPMENT_SOURCE_CHECK_1_5_8_V1";
    public const string ManualMarker =
        "FFO_INVENTORY_EQUIPMENT_MANUAL_CHECK_1_5_8_V1";
    public const int ExpectedMapId = 6;
    public const int ExpectedItemId = 2876;
    public const byte ExpectedTab = 2;
    public const int ExpectedBagIndex = 0;
    public const byte ExpectedEquipmentSlot = 8;
    public const float TimeoutSeconds = 30f;

    public static bool IsRequested(string[] arguments)
    {
        return HasArgument(arguments, Argument);
    }

    public static bool IsManualRequested(string[] arguments)
    {
        return HasArgument(arguments, ManualArgument);
    }

    public static string ManualReadyLine(int mapId)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} READY state=InWorld map={1} canvas=800x600 input=manual-only " +
            "item={2} tab={3} bag={4} slot={5}",
            ManualMarker,
            mapId,
            ExpectedItemId,
            ExpectedTab,
            ExpectedBagIndex,
            ExpectedEquipmentSlot);
    }

    public static string ManualStageLine(ReferenceFirstWorldWindowOwner window)
    {
        if (window == null || !window.Built || window.InventoryDialog == null ||
            window.LocalEquipmentDialog == null)
        {
            return ManualMarker + " STAGE unavailable";
        }

        WorldInventoryState state = window.InventoryDialog.State;
        string stage = StateName(state);
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} STAGE state={1} inventoryOpen={2} equipmentOpen={3} " +
            "items={4} equipped={5}",
            ManualMarker,
            stage,
            window.InventoryDialog.Visible ? 1 : 0,
            window.LocalEquipmentDialog.Visible ? 1 : 0,
            state == null ? -1 : state.Items.Count,
            state == null ? -1 : state.EquippedItems.Count);
    }

    public static string FailureLine(string code)
    {
        return Marker + " FAIL code=" +
            (string.IsNullOrEmpty(code) ? "unknown" : code);
    }

    internal static bool IsInitial(WorldInventoryState state)
    {
        WorldInventoryItemState item = state == null
            ? null
            : state.ItemAt(ExpectedTab, ExpectedBagIndex);
        return state != null && state.Items.Count == 1 &&
            state.EquippedItems.Count == 0 && item != null &&
            item.ItemId == ExpectedItemId && item.Count == 1 &&
            item.EquipmentSlot == ExpectedEquipmentSlot && !item.Bound &&
            !item.Cosmetic && !item.Blocked && !item.Locked;
    }

    internal static bool IsEquipped(WorldInventoryState state)
    {
        if (state == null || state.Items.Count != 0 ||
            state.EquippedItems.Count != 1)
        {
            return false;
        }

        WorldInventoryEquippedItemState item = state.EquippedItems[0];
        return item != null && item.ItemId == ExpectedItemId &&
            item.Slot == ExpectedEquipmentSlot;
    }

    internal static bool IsReturned(WorldInventoryState state)
    {
        WorldInventoryItemState item = state == null
            ? null
            : state.ItemAt(ExpectedTab, ExpectedBagIndex);
        return state != null && state.Items.Count == 1 &&
            state.EquippedItems.Count == 0 && item != null &&
            item.ItemId == ExpectedItemId && item.Count == 1 && item.Bound;
    }

    private static string StateName(WorldInventoryState state)
    {
        if (IsInitial(state))
        {
            return "INITIAL";
        }
        if (IsEquipped(state))
        {
            return "EQUIPPED";
        }
        if (IsReturned(state))
        {
            return "RETURNED";
        }

        return "OTHER";
    }

    private static bool HasArgument(string[] arguments, string expected)
    {
        if (arguments == null)
        {
            return false;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

internal sealed class ReferenceInventoryEquipmentSourceDriver
{
    private enum Phase
    {
        Initial,
        WaitingForEquip,
        WaitingForUnequip,
        Complete,
        Failed,
    }

    private readonly ClientSessionCoordinator session;
    private readonly ReferenceFirstWorldWindowOwner window;
    private readonly float deadline;
    private Phase phase;
    private long initialInventoryVersion;
    private long initialStatusVersion;
    private long initialAppearanceVersion;
    private long equippedInventoryVersion;
    private long equippedStatusVersion;
    private long equippedAppearanceVersion;

    public ReferenceInventoryEquipmentSourceDriver(
        ClientSessionCoordinator session,
        ReferenceFirstWorldWindowOwner window)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.window = window ?? throw new ArgumentNullException(nameof(window));
        deadline = Time.realtimeSinceStartup +
            ReferenceInventoryEquipmentSourceCheck.TimeoutSeconds;
        phase = Phase.Initial;
    }

    public bool Terminal => phase == Phase.Complete || phase == Phase.Failed;

    public bool Succeeded => phase == Phase.Complete;

    public string Result { get; private set; } =
        ReferenceInventoryEquipmentSourceCheck.FailureLine("result.missing");

    public void Tick()
    {
        if (Terminal)
        {
            return;
        }

        if (Time.realtimeSinceStartup >= deadline)
        {
            Fail("mutation.timeout");
            return;
        }

        try
        {
            switch (phase)
            {
                case Phase.Initial:
                    StartEquip();
                    break;
                case Phase.WaitingForEquip:
                    ObserveEquip();
                    break;
                case Phase.WaitingForUnequip:
                    ObserveUnequip();
                    break;
            }
        }
        catch (Exception)
        {
            Fail("driver.unexpected");
        }
    }

    private void StartEquip()
    {
        ReferenceFirstWorldInventoryDialog inventory = window.InventoryDialog;
        ReferenceFirstWorldLocalEquipmentDialog equipment =
            window.LocalEquipmentDialog;
        if (window.MapDescriptor.id !=
                ReferenceInventoryEquipmentSourceCheck.ExpectedMapId ||
            inventory == null || !inventory.Ready || equipment == null ||
            !equipment.Ready ||
            !ReferenceInventoryEquipmentSourceCheck.IsInitial(
                session.InventoryState))
        {
            Fail("authority.initial");
            return;
        }

        inventory.Open();
        equipment.EnsureOpen();
        if (!inventory.ApplyState(session.InventoryState) ||
            !equipment.ApplyInventory(session.InventoryState) ||
            !inventory.BeginEquipForAudit(
                ReferenceInventoryEquipmentSourceCheck.ExpectedTab,
                ReferenceInventoryEquipmentSourceCheck.ExpectedBagIndex))
        {
            Fail("request.equip_queue");
            return;
        }

        if (window.SystemInfoBar == null ||
            window.SystemInfoBar.ConfirmationMessage !=
                ReferenceFirstWorldInventoryDialog.EquipBindingConfirmation)
        {
            Fail("request.equip_confirmation");
            return;
        }
        window.SystemInfoBar.FireAffirmativeForAudit();

        initialInventoryVersion = session.InventoryStateVersion;
        initialStatusVersion = session.PlayerStatusStateVersion;
        initialAppearanceVersion = session.PlayerAppearanceStateVersion;
        if (!ReferenceInventoryEquipmentSourceCheck.IsInitial(
                session.InventoryState) ||
            !ReferenceInventoryEquipmentSourceCheck.IsInitial(inventory.State) ||
            !ReferenceInventoryEquipmentSourceCheck.IsInitial(equipment.Inventory))
        {
            Fail("request.equip_optimistic_mutation");
            return;
        }

        Debug.Log(
            ReferenceInventoryEquipmentSourceCheck.Marker +
            " STAGE equip=requested authority=unchanged");
        phase = Phase.WaitingForEquip;
    }

    private void ObserveEquip()
    {
        if (session.InventoryStateVersion == initialInventoryVersion)
        {
            return;
        }
        if (session.InventoryStateVersion < initialInventoryVersion ||
            session.PlayerStatusStateVersion <= initialStatusVersion ||
            session.PlayerAppearanceStateVersion <= initialAppearanceVersion ||
            !ReferenceInventoryEquipmentSourceCheck.IsEquipped(
                session.InventoryState) ||
            !ReferenceInventoryEquipmentSourceCheck.IsEquipped(
                window.InventoryDialog.State) ||
            !ReferenceInventoryEquipmentSourceCheck.IsEquipped(
                window.LocalEquipmentDialog.Inventory))
        {
            Fail("authority.equip_result");
            return;
        }

        equippedInventoryVersion = session.InventoryStateVersion;
        equippedStatusVersion = session.PlayerStatusStateVersion;
        equippedAppearanceVersion = session.PlayerAppearanceStateVersion;
        Debug.Log(
            ReferenceInventoryEquipmentSourceCheck.Marker +
            " STAGE equip=authoritative slot=8");
        if (!window.LocalEquipmentDialog.ActOnSlotForAudit(
                ReferenceInventoryEquipmentSourceCheck.ExpectedEquipmentSlot,
                1) ||
            !ReferenceInventoryEquipmentSourceCheck.IsEquipped(
                session.InventoryState))
        {
            Fail("request.unequip_queue");
            return;
        }

        Debug.Log(
            ReferenceInventoryEquipmentSourceCheck.Marker +
            " STAGE unequip=requested authority=unchanged");
        phase = Phase.WaitingForUnequip;
    }

    private void ObserveUnequip()
    {
        if (session.InventoryStateVersion == equippedInventoryVersion)
        {
            return;
        }
        if (session.InventoryStateVersion < equippedInventoryVersion ||
            session.PlayerStatusStateVersion <= equippedStatusVersion ||
            session.PlayerAppearanceStateVersion <= equippedAppearanceVersion ||
            !ReferenceInventoryEquipmentSourceCheck.IsReturned(
                session.InventoryState) ||
            !ReferenceInventoryEquipmentSourceCheck.IsReturned(
                window.InventoryDialog.State) ||
            !ReferenceInventoryEquipmentSourceCheck.IsReturned(
                window.LocalEquipmentDialog.Inventory) ||
            !window.InventoryDialog.Visible ||
            !window.LocalEquipmentDialog.Visible)
        {
            Fail("authority.unequip_result");
            return;
        }

        Result = string.Format(
            CultureInfo.InvariantCulture,
            "{0} PASS map={1} item={2} tab={3} bag={4} slot={5} " +
            "inventoryVersions={6},{7},{8} status=advanced appearance=advanced " +
            "authority=ready optimistic=none dialogs=ready",
            ReferenceInventoryEquipmentSourceCheck.Marker,
            ReferenceInventoryEquipmentSourceCheck.ExpectedMapId,
            ReferenceInventoryEquipmentSourceCheck.ExpectedItemId,
            ReferenceInventoryEquipmentSourceCheck.ExpectedTab,
            ReferenceInventoryEquipmentSourceCheck.ExpectedBagIndex,
            ReferenceInventoryEquipmentSourceCheck.ExpectedEquipmentSlot,
            initialInventoryVersion,
            equippedInventoryVersion,
            session.InventoryStateVersion);
        phase = Phase.Complete;
    }

    private void Fail(string code)
    {
        Result = ReferenceInventoryEquipmentSourceCheck.FailureLine(code);
        phase = Phase.Failed;
    }
}

}
