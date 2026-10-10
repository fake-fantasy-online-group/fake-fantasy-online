using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    internal float DrawScaleForIndependentRuntime { set => s = value; }

    // Official 0x060004fd: a drop destination is only the placed window rectangle.
    public static bool OverWindow(Vector2 m)
    {
        if (I == null || !I.open || !I.placed) return false;
        float scale = UIScale.Factor;
        return new Rect(I.winX, I.winY, 423f * scale, 501f * scale).Contains(m);
    }

    // Official 0x06000526: held sources capture input even outside that rectangle.
    public bool MouseOver()
    {
        if (!open || !placed) return false;
        if (HasHeldAny) return true;
        Vector2 m = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return new Rect(winX, winY, 423f * s, 501f * s).Contains(m);
    }
}
}
