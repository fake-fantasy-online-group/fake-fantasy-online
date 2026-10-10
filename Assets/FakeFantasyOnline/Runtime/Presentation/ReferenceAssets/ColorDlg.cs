using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 ColorDlg owner, tokens 0x06000a62-0x06000a68.
// Dual-architecture native evidence is frozen in
// Build/ReferenceReverseGhidra-1.5.8/evidence/colordlg-v1/colordlg-evidence.json.
// NW/NH are the literal const floats 102/74. COLS is the ten-element colour table
// built by the static constructor; every component was read byte for byte from both
// slices (arm64 0x111273f0..0x1112747c, x86_64 0x11191d0 + 0x111a2e0..0x111a34c)
// and the two architectures agree component for component.
public partial class ColorDlg : MonoBehaviour
{
    public static ColorDlg I;

    private bool visible;

    private const float NW = 102f;
    private const float NH = 74f;

    private static readonly Color[] COLS = new Color[10]
    {
        new Color(1f, 1f, 1f, 1f),
        new Color(1f, 0f, 0f, 1f),
        new Color(1f, 0.6f, 0f, 1f),
        new Color(1f, 235f / 255f, 4f / 255f, 1f),
        new Color(0f, 1f, 0f, 1f),
        new Color(0f, 1f, 1f, 1f),
        new Color(0.4f, 0.6f, 1f, 1f),
        new Color(0.8f, 0.4f, 1f, 1f),
        new Color(1f, 0f, 1f, 1f),
        new Color(0.6f, 0.6f, 0.6f, 1f)
    };

    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static bool Visible => I != null && I.visible;

    // 06000a62: create-on-demand GameObject named after the type, then flip visible.
    public static void Toggle()
    {
        if (I == null)
        {
            GameObject gameObject = new GameObject("ColorDlg");
            I = gameObject.AddComponent<ColorDlg>();
            Object.DontDestroyOnLoad(gameObject);
        }
        I.visible = !I.visible;
    }

    // Official 1.6.0 06000a6e: preserve the two independent winX NaN checks.
    public static bool MouseOver()
    {
        if (I == null || !I.visible)
        {
            return false;
        }
        float s = UIScale.Factor;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX)
            ? (Screen.width - s * NW) * 0.5f
            : I.winX;
        float y = float.IsNaN(I.winX)
            ? (Screen.height - s * NH) * 0.5f
            : I.winY;
        return mx >= x && mx < x + s * NW && my >= y && my < y + s * NH;
    }

    // 06000a65.
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
        for (int i = 0; i < COLS.Length; i++)
        {
            float x = winX + s * (8f + (i % 5) * 18f);
            float y = winY + s * (8f + (i / 5) * 18f);
            float size = s * 16f;
            GUI.color = COLS[i];
            GUI.DrawTexture(new Rect(x, y, size, size), Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (PopUI.Click(new Rect(x, y, size, size), mousePos))
            {
                visible = false;
            }
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
