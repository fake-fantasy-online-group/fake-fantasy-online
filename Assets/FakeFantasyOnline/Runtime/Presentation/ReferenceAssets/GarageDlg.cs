using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 GarageDlg 0x06000FF1–0x06000FF9.
public partial class GarageDlg : MonoBehaviour
{
    public static GarageDlg I;
    private const float NW = 341f;
    private const float NH = 425f;
    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoBuy;
    private Texture2D icoCancel;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static bool Visible
    {
        get { return I != null && I.visible; }
    }

    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("GarageDlg");
        I = owner.AddComponent<GarageDlg>();
        DontDestroyOnLoad(owner);
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
        float px = Input.mousePosition.x;
        float py = Screen.height - Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(px, py));
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
        bg = loader.LoadGsa("res/uires/garagedlg/garagedlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoBuy = loader.LoadGsa("res/uires/garagedlg/buygaragebtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
    }

    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private void Lbl(float x, float y, string t, Color col)
    {
        if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, t, col, s);
    }

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * 0.5f;
            winY = (Screen.height - NH * s) * 0.5f;
        }
        Vector2 mp = Event.current.mousePosition;
        GUI.depth = 4;
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
        Rect content = L(32f, 52f, 256f, 340f);
        GUI.color = new Color(0f, 0f, 0f, 0.16f);
        GUI.DrawTexture(content, Texture2D.whiteTexture);
        GUI.color = Color.white;
        Lbl(110f, 215f, "暂无仓库", WhUI.GRAY);
        if (WhUI.Click(content, mp)) PasswdDlg.Open(0);
        WhUI.Btn(L(35f, 399f, 90f, 22f), icoBuy, "购买仓库", mp, s);
        if (WhUI.Btn(L(130f, 399f, 44f, 18f), null, "设密码", mp, s)) PasswdDlg.Open(1);
        if (WhUI.Btn(L(176f, 399f, 44f, 18f), null, "改密码", mp, s)) PasswdDlg.Open(2);
        if (WhUI.Btn(L(226f, 399f, 70f, 22f), icoCancel, "取消", mp, s)) visible = false;
        Rect close = L(293f, 23f, 20f, 19f);
        if (icoClose != null) GUI.DrawTexture(close, icoClose);
        if (WhUI.Click(close, mp)) visible = false;
        WinDrag.Handle(this, L(0, 0, NW, 28f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
            Event.current.Use();
    }
}
}
