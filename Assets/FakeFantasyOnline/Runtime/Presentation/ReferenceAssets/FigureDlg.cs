using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 FigureDlg 0x06000a83–0x06000a89.
public partial class FigureDlg : MonoBehaviour
{
    public static FigureDlg I;
    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoOk;
    private const float NW = 345f;
    private const float NH = 476f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("FigureDlg");
        I = owner.AddComponent<FigureDlg>();
        DontDestroyOnLoad(owner);
    }

    public static bool Visible
    {
        get { return I != null && I.visible; }
    }

    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
    }

    public static bool MouseOver()
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float mouseX = Input.mousePosition.x;
        float mouseY = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(mouseX, mouseY));
    }

    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        bg = loader.LoadGsa("res/uires/figuredlg/figuredlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoOk = loader.LoadGsa("res/uires/figuredlg/figurebtn.gsa");
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        var stacking = WinZ.Frame(this, L(0, 0, NW, NH), 10);
        Vector2 originalMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-9999f, -9999f);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width - NW * s) * 0.5f;
                winY = (Screen.height - NH * s) * 0.5f;
            }
            Vector2 mp = Event.current.mousePosition;
            GUI.depth = stacking.Item1;
            if (bg != null)
            {
                GUI.DrawTexture(L(0, 0, NW, NH), bg);
            }
            else
            {
                GUI.color = new Color(0.09f, 0.08f, 0.12f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            GUI.color = new Color(0f, 0f, 0f, 0.16f);
            GUI.DrawTexture(L(40f, 70f, 265f, 350f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            PopUI.Btn(L(67f, 441f, 90f, 22f), icoOk, "确定", mp, s);
            Rect close = L(276f, 34f, 20f, 19f);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (PopUI.Click(close, mp)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 56f), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            if (stacking.Item2) Event.current.mousePosition = originalMouse;
        }
    }
}
}
