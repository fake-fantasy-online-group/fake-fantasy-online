using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 PopUI colours and Box (tokens 06000a42, 06000a45). The three
// public static readonly Color fields are the whole body of the static
// constructor; every component was read byte for byte from the x86_64 slice
// (GOLD at 0x1119e30, GRAY at 0x111a0f0, Backdrop at 0x111a100).
// Frozen evidence:
// Build/ReferenceReverseGhidra-1.5.8/evidence/popui-box-v1/popui-box-evidence.json
internal static partial class PopUI
{
    public static readonly Color GOLD = new Color(1f, 0.92f, 0.66f, 1f);

    public static readonly Color GRAY = new Color(0.66f, 0.64f, 0.55f, 1f);

    public static readonly Color Backdrop = new Color(0.06f, 0.055f, 0.09f, 1f);

    // 06000a42: one translucent black plate, then the GUI tint is reset.
    public static void Box(Rect r)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
}
