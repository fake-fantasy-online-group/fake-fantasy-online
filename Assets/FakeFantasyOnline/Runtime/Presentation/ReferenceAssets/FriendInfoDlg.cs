using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 FriendInfoDlg 0x06000d34–0x06000d3b.
public partial class FriendInfoDlg : MonoBehaviour
{
    public static FriendInfoDlg I;
    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private const float NW = 118f;
    private const float NH = 116f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("FriendInfoDlg");
        I = owner.AddComponent<FriendInfoDlg>();
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
        Vector2 mp = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * 0.5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(mp);
    }

    private Rect L(float x, float y, float w, float h)
    {
        return new Rect(winX + x * s, winY + y * s, w * s, h * s);
    }

    private void Lbl(float x, float y, string t, Color c)
    {
        if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, t, c, s);
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
        bg = loader.LoadGsa("res/uires/teamdlg/friendinfodlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
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
        Lbl(50f, 18f, "—", SocUI.WHITE);
        Lbl(50f, 38f, "—", SocUI.WHITE);
        Lbl(50f, 58f, "—", SocUI.WHITE);
        Lbl(50f, 78f, "—", SocUI.WHITE);
        Rect close = L(96f, 4f, 18f, 18f);
        if (icoClose != null) GUI.DrawTexture(close, icoClose);
        if (SocUI.Click(close, mp)) visible = false;
        WinDrag.Handle(this, L(0, 0, NW, 28f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible)
            Event.current.Use();
    }
}
}
