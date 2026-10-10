using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official InventoryDlg static +0x34: a byte tab index. The two writers below are this tree's
    // adapter entry points; the getter is the official one, so it reads the official field.
    private static byte heldTab;
    private static Action clearHeldForIndependentRuntime;
    internal static void BindHeldIdentityForIndependentRuntime(int tab, int slot, Action clear)
    {
        if (slot >= 0) heldTab = (byte)tab;
        heldSlot = slot; clearHeldForIndependentRuntime = clear;
    }
    public static byte HeldTab => heldTab;
    public static int HeldBagSlot => heldSlot;
    public static void ClearHeld()
    {
        var clear = clearHeldForIndependentRuntime;
        var clearPet = clearBarPetForIndependentRuntime;
        heldSlot = -1; heldWhSlot = -1; heldBarPet = -1; heldBound = false;
        clearHeldForIndependentRuntime = null; clearBarPetForIndependentRuntime = null;
        clear?.Invoke();
        clearPet?.Invoke();
    }
}
}
