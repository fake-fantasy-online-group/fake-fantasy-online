namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 1.5.8 constant fields, read from the 1.5.8 generated managed source
    // Build/ReferenceManagedRecovery-1.5.8/source/InventoryDlg.cs (GameAssembly SHA-256
    // c56340ce753d18295998d55074f5d4ac9cb407edb57edc968a05db932be4ed9b, DisplayClass 132 series).
    // The frozen native index carries no constant value (const fields have no assigned offset),
    // so each value is cross-checked against native immediates of the members that use them:
    //   WinH 501 - Toggle/EnsureOnScreen and the OnGUI placement prologue and header layout;
    //   CoinY 479 - g__CoinFlow|132_1 draws the coin row at Y + 479 * s;
    //   GridTop 66 / GridH 400 - the bag grid origin and height;
    //   Cols 10 / Rows 10 / Slots 100 - one 10x10 bag page (423/10 and 400/10 give the slot box);
    //   SplitMaxCap 999 - the clamp bound used by OpenSplit and OpenSplitWarehouse.
    // Do NOT read these values from Build/.ReferenceManagedRecovery.schema1-* or
    // Build/ReferenceManagedRecovery: those caches carry GameAssembly 325521ca... and their
    // SplitMaxCap is 99, which is not this version.
    private const int Cols = 10;
    private const int Rows = 10;
    private const int Slots = 100;
    private const float GridTop = 66f;
    private const float GridH = 400f;
    private const float WinH = 501f;
    private const float CoinY = 479f;
    private const int SplitMaxCap = 999;
}
}
