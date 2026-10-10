using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 FindModeDlg owner, tokens 0x06000a6f-0x06000a74.
// Dual-architecture native evidence is frozen in
// Build/ReferenceReverseGhidra-1.5.8/evidence/findmodedlg-v1/findmodedlg-evidence.json.
// NW/NH are the literal const floats 98/90; the inner plate is L(6, 6, 86, 78) and
// the drag strip is L(0, 0, NW, 14). Unlike FaceDlg it draws no title text.
public partial class FindModeDlg : MonoBehaviour
{
    public static FindModeDlg I;

    private bool visible;

    private const float NW = 98f;
    private const float NH = 90f;

    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static bool Visible => I != null && I.visible;

    // 06000a6f: create-on-demand GameObject named after the type, then flip visible.
    public static void Toggle()
    {
        if (I == null)
        {
            GameObject gameObject = new GameObject("FindModeDlg");
            I = gameObject.AddComponent<FindModeDlg>();
            Object.DontDestroyOnLoad(gameObject);
        }
        I.visible = !I.visible;
    }

    // 06000a71: same shape as FaceDlg.MouseOver, with NW/NH 98/90.
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

    // 06000a72.
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
        Rect innerPlate = L(6f, 6f, 86f, 78f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(innerPlate, Texture2D.whiteTexture);
        GUI.color = Color.white;
        WinDrag.Handle(this, L(0f, 0f, NW, 14f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown
            && L(0f, 0f, NW, NH).Contains(mousePos)
            && !SysMsgBox.Visible)
        {
            Event.current.Use();
        }
    }
}

}
