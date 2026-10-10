using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 GroceryConfigDlg 0x0600047B–0x06000486.
public partial class GroceryConfigDlg : MonoBehaviour
{
    public static GroceryConfigDlg I;
    private const float NW = 287f;
    private const float NH = 447f;
    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoHelp;
    private Texture2D icoBegin;
    private Texture2D icoGiveup;
    private GUIStyle _e;
    private string sname = "";
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    public static bool Visible
    {
        get { return I != null && I.visible; }
    }

    public static void Open(int mode)
    {
        Ensure();
        GroUI.ShopType = mode > 0 ? 1 : 0;
        I.visible = true;
    }

    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("GroceryConfigDlg");
        I = owner.AddComponent<GroceryConfigDlg>();
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
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        float x = I.winX;
        if (float.IsNaN(x)) x = (Screen.width - NW * scale) * 0.5f;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(mx, my));
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
        bg = loader.LoadGsa("res/uires/grocerydlg/groceryconfig.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoHelp = loader.LoadGsa("res/uires/gameui/helpicon.gsa");
        icoBegin = loader.LoadGsa("res/uires/grocerydlg/beginshop.gsa");
        icoGiveup = loader.LoadGsa("res/uires/common/giveupbtn.gsa");
    }

    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private GUIStyle E()
    {
        if (_e == null)
        {
            GUIStyle style = new GUIStyle(GUIStyle.none);
            style.font = NpcChatter.CnFont();
            style.alignment = TextAnchor.MiddleLeft;
            _e = style;
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

    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        var stacking = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 savedMouse = Event.current.mousePosition;
        if (stacking.Item2) Event.current.mousePosition = new Vector2(-10000f, -10000f);
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
                GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
                GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
            Cap(56f, 11f, 174f, 12f, GroUI.ShopName[GroUI.ShopType]);
            Rect itemPlate = L(18f, 35f, 240f, 319f);
            GUI.color = new Color(0f, 0f, 0f, 0.16f);
            GUI.DrawTexture(itemPlate, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready)
                BitmapFont.CenterOutline(L(18f, 180f, 240f, 12f),
                    "点格子设置要" + (GroUI.ShopType != 0 ? "收购" : "出售") + "的商品", GroUI.GRAY, s);
            if (GroUI.Click(L(18f, 35f, 240f, 319f), mp)) GroceryDetailDlg.Toggle();
            Rect nameField = L(18f, 379f, 256f, 12f);
            GUI.SetNextControlName("gro_n");
            sname = GUI.TextField(new Rect(nameField.x + 3f, nameField.y, nameField.width - 6f, nameField.height),
                sname ?? "", 16, E());
            if (GroUI.Btn(L(22f, 418f, 90f, 22f), icoBegin, "开张", mp, s))
            {
                visible = false;
                GroceryStatusDlg.OpenRunning(GroUI.ShopType);
            }
            if (GroUI.Btn(L(204f, 418f, 70f, 22f), icoGiveup, "取消", mp, s)) visible = false;
            Rect help = L(241f, 6f, 20f, 19f);
            if (icoHelp != null) GUI.DrawTexture(help, icoHelp);
            Rect close = L(262f, 6f, 20f, 19f);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (GroUI.Click(close, mp)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 28f), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            if (stacking.Item2) Event.current.mousePosition = savedMouse;
        }
    }
}
}
