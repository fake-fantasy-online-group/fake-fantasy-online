namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class InventoryDlg
{
    private static int heldSlot = -1;
    private static int heldWhSlot = -1;
    internal static bool CanRightActivateForIndependentRuntime => heldSlot < 0 && heldWhSlot < 0;
    public static bool HasHeld => heldSlot >= 0;
    // Official 0x06000505.
    public static int HeldSlot => heldSlot;

    // Actual migrated inventory selection supplies the held identity until full OnGUI recovery.
    internal static void BindHeldSlotForIndependentRuntime(int slot) => heldSlot = slot;
}

}
