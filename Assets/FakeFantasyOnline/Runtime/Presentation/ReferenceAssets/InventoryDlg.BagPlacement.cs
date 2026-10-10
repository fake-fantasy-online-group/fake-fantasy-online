using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Original 0527 grid branch precedes the occupied-item branch, so empty cells also work.
    // 0527 already sampled offered alongside the row lookup, before any move callback.
    internal static bool PlaceHeldBagForIndependentRuntime(Event ev, int tab, int targetSlot, bool offered)
    {
        if (heldSlot < 0 || tab != HeldTab || ev == null || ev.type != EventType.MouseDown ||
            ev.button != 0 || ev.clickCount >= 2) return false;
        if (!offered)
        {
            if (NetClient.I != null) NetClient.I.ItemMoveForIndependentRuntime(tab, heldSlot, targetSlot);
            DropBagReferenceForIndependentRuntime();
        }
        ev.Use();
        return true;
    }
    // Original 0527 fallback after modifiers and specialized selectors.
    internal static bool PickUpBagForIndependentRuntime(Event ev, byte tab, Item item, Action clear)
    {
        if (ev == null || ev.type != EventType.MouseDown || ev.button != 0 || ev.clickCount == 2 ||
            heldSlot >= 0 || heldWhSlot >= 0) return false;
        heldSlot = item.bagIdx; heldTab = (byte)tab; heldIcon = item.icon;
        heldName = item.name; heldId = item.id; heldPet = item.pet.HasValue; heldBound = item.bound;
        clearHeldForIndependentRuntime = clear;
        ev.Use(); return true;
    }
}
}
