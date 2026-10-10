using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 QueryDlg 0x060001a3-0x060001ac.
public partial class QueryDlg : MonoBehaviour
{
    public static QueryDlg I;
    private bool visible;
    private bool tried;
    private int mode;
    private Texture2D icoClose;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    private float NW
    {
        get { return 262f; }
    }

    private float NH
    {
        get { return mode != 0 ? 253f : 322f; }
    }

    public static void Open(int mode)
    {
        if (I == null)
        {
            GameObject owner = new GameObject("QueryDlg");
            I = owner.AddComponent<QueryDlg>();
            DontDestroyOnLoad(owner);
        }
        I.mode = mode;
        I.visible = true;
    }

    public static bool Visible
    {
        get { return I != null && I.visible; }
    }

    public static void CloseNow()
    {
        if (I == null) return;
        I.visible = false;
    }

    public static bool MouseOver()
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float height = I.NH;
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - I.NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - height * scale) * 0.5f : I.winY;
        return new Rect(x, y, I.NW * scale, height * scale).Contains(mp);
    }

    private void Load()
    {
        tried = true;
        icoClose = ResLoader.I?.LoadGsa("res/uires/common/miniclose.gsa");
    }

    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        float height = NH;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - height * s) * 0.5f;
        }
        Vector2 mp = Event.current.mousePosition;
        GUI.depth = 3;
        CraftUI.Panel(L(0, 0, NW, height));
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(L(16f, 34f, 230f, height - 50f), Texture2D.whiteTexture);
        GUI.color = Color.white;
        Rect close = L(236f, 6f, 20f, 19f);
        if (icoClose != null) GUI.DrawTexture(close, icoClose);
        if (CraftUI.Click(close, mp)) visible = false;
        WinDrag.Handle(this, L(0, 0, NW, 28f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, NW, height).Contains(mp) && !SysMsgBox.Visible)
            Event.current.Use();
    }
}
}
