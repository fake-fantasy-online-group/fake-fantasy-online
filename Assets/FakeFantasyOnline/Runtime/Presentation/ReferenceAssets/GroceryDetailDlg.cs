using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 GroceryDetailDlg 0x06000494–0x0600049F.
public partial class GroceryDetailDlg : MonoBehaviour
{
    public static GroceryDetailDlg I;
    private const float NW = 255f;
    private const float NH = 212f;
    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoHelp;
    private Texture2D icoOk;
    private Texture2D icoCancel;
    private GUIStyle _e;
    private string g = "0";
    private string sv = "0";
    private string c = "0";
    private string count = "1";
    private string itemName = "未选商品";
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
        GameObject owner = new GameObject("GroceryDetailDlg");
        I = owner.AddComponent<GroceryDetailDlg>();
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
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(mp);
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
        bg = loader.LoadGsa("res/uires/grocerydlg/grocerydetail.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoHelp = loader.LoadGsa("res/uires/gameui/helpicon.gsa");
        icoOk = loader.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
    }

    private GUIStyle E()
    {
        if (_e == null)
        {
            _e = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.MiddleLeft
            };
            _e.normal.textColor = Color.white;
        }
        _e.fontSize = Mathf.RoundToInt(s * 12f);
        return _e;
    }

    private void Cap(float x, float y, float w, float h, string t)
    {
        if (BitmapFont.Ready) BitmapFont.CenterOutline(L(x, y, w, h), t, GroUI.GOLD, s);
    }

    private void Lbl(float x, float y, string t, Color col)
    {
        if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, t, col, s);
    }

    private string Field(string n, Rect r, string v, int max)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.3f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.SetNextControlName(n);
        return GUI.TextField(new Rect(r.x + 2f, r.y, r.width - 4f, r.height), v ?? "0", max, E());
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
        GUI.depth = 2;
        if (bg != null)
        {
            GUI.DrawTexture(L(0, 0, NW, NH), bg);
        }
        else
        {
            GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
            GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        Cap(55f, 9f, 150f, 12f, "开店商品设置");
        Lbl(24f, 53f, itemName, GroUI.GOLD);
        Lbl(95f, 84f, "0", GroUI.GRAY);
        Lbl(141f, 84f, "0", GroUI.GRAY);
        Lbl(183f, 84f, "0", GroUI.GRAY);
        g = Field("gd_g", L(96f, 127f, 24f, 12f), g, 3);
        sv = Field("gd_s", L(139f, 127f, 24f, 12f), sv, 3);
        c = Field("gd_c", L(181f, 127f, 24f, 12f), c, 3);
        count = Field("gd_n", L(99f, 150f, 66f, 12f), count, 5);
        GroUI.Btn(L(34f, 185f, 70f, 22f), icoOk, "确定", mp, s);
        if (GroUI.Btn(L(159f, 185f, 70f, 22f), icoCancel, "取消", mp, s)) visible = false;
        Rect help = L(210f, 6f, 20f, 19f);
        if (icoHelp != null) GUI.DrawTexture(help, icoHelp);
        Rect close = L(230f, 6f, 20f, 19f);
        if (icoClose != null) GUI.DrawTexture(close, icoClose);
        if (GroUI.Click(close, mp)) visible = false;
        WinDrag.Handle(this, L(0, 0, NW, 28f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
            Event.current.Use();
    }
}
}
