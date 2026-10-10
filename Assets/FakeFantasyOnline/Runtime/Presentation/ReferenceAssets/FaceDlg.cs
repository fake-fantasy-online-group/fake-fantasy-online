using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 FaceDlg owner, tokens 0x06000a69-0x06000a6e.
// Dual-architecture native evidence (arm64 rva 0x2d1cfc / x86_64 rva 0x2e3650 for
// Toggle, and the matching pair for every other member) is frozen in
// Build/ReferenceReverseGhidra-1.5.8/evidence/facedlg-v1/facedlg-evidence.json.
// NW/NH are the literal const floats 129/148; every other panel number (8, 66, 113,
// 132, 12) was read byte for byte out of both slices.
public partial class FaceDlg : MonoBehaviour
{
    public static FaceDlg I;

    private bool visible;

    private const float NW = 129f;
    private const float NH = 148f;

    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static bool Visible => I != null && I.visible;

    // 06000a69: create-on-demand GameObject named after the type, then flip visible.
    public static void Toggle()
    {
        if (I == null)
        {
            GameObject gameObject = new GameObject("FaceDlg");
            I = gameObject.AddComponent<FaceDlg>();
            Object.DontDestroyOnLoad(gameObject);
        }
        I.visible = !I.visible;
    }

    // Official 1.6.0 06000a75: hit test against Input.mousePosition (bottom-left
    // origin, so y is flipped with Screen.height). Preserve the separate NaN
    // checks of I.winX and defer reading I.winY until the second check is false.
    public static bool MouseOver()
    {
        if (I == null || !I.visible)
        {
            return false;
        }
        float s = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = I.winX;
        if (float.IsNaN(x))
        {
            x = (Screen.width - s * NW) * 0.5f;
        }
        float y = float.IsNaN(I.winX)
            ? (Screen.height - s * NH) * 0.5f
            : I.winY;
        return mx >= x && mx < x + s * NW && my >= y && my < y + s * NH;
    }

    // 06000a6c: the only surviving component in the decompilation is
    // winX + x * s; the remaining three are supplied by the inlined OnGUI uses.
    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - s * NW) * 0.5f;
            winY = (Screen.height - s * NH) * 0.5f;
        }
        Vector3 mousePos = Event.current.mousePosition;
        GUI.depth = 2;
        PopUI.Panel(L(0f, 0f, NW, NH));
        Rect innerPlate = L(8f, 8f, 113f, 132f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(innerPlate, Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (BitmapFont.Ready)
        {
            BitmapFont.CenterOutline(L(8f, 66f, 113f, 12f), "表情", PopUI.GRAY, s);
        }
        WinDrag.Handle(this, L(0f, 0f, NW, 12f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown
            && L(0f, 0f, NW, NH).Contains(mousePos)
            && !SysMsgBox.Visible)
        {
            Event.current.Use();
        }
    }
}

}
