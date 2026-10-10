using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    internal bool PlacedForIndependentRuntime { get => placed; set => placed = value; }

    internal int TabForIndependentRuntime => tab;

    // Mechanical named-root creation shared by the original creation sites and old fixtures.
    // It adds no visibility, placement, persistence or loading policy.
    internal static InventoryDlg EnsureOwnerForIndependentRuntime()
    {
        if (I == null) I = new GameObject("InventoryDlg").AddComponent<InventoryDlg>();
        return I;
    }

    // Official 0x06000518: closing only changes visibility.
    public static void Toggle()
    {
        EnsureOwnerForIndependentRuntime();
        I.open = !I.open;
        if (I.open) I.EnsureOnScreen(423f * UIScale.Factor, 501f * UIScale.Factor);
    }

    // Official 0x06000519: retain any positive-area intersection, including negative coordinates.
    internal void EnsureOnScreen(float w, float h)
    {
        if (!placed) return;
        float x = winX, y = winY;
        int width = Screen.width;
        int height = Screen.height;
        ApplyScreenBounds(x, y, w, h, width, height);
    }

    // The original overlap expression also serves the existing explicit-viewport audit seam.
    internal bool ApplyScreenBounds(float x, float y, float w, float h, int width, int height)
    {
        if (!placed) return false;
        if (!(x < width && x + w > 0f && y < height && y + h > 0f)) placed = false;
        return placed;
    }

    // Official 0x0600051a uses Unity's object-lifetime comparison.
    public static bool IsOpen => I != null && I.open;
}
}
