using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 PasswdDlg 0x06000FFA–0x06001006.
public partial class PasswdDlg : MonoBehaviour
{
    public static PasswdDlg I;
    private bool visible;
    private bool tried;
    private int mode;
    private string p1 = "";
    private string p2 = "";
    private string p3 = "";
    private Texture2D bgInput;
    private Texture2D bgSet;
    private Texture2D bgMod;
    private Texture2D icoClose;
    private Texture2D icoOk;
    private Texture2D icoCancel;
    private GUIStyle _edit;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    private float NW
    {
        get { return 214f; }
    }

    private float NH
    {
        get { return mode != 0 ? 181f : 114f; }
    }

    public static bool Visible
    {
        get { return I != null && I.visible; }
    }

    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("PasswdDlg");
        I = owner.AddComponent<PasswdDlg>();
        DontDestroyOnLoad(owner);
    }

    public static void Open(int mode)
    {
        Ensure();
        I.mode = mode;
        // 官方三处写的是同一个空串字面量，原生顺序 p3 → p2 → p1，取值相同所以顺序无语义差别。
        I.p3 = "";
        I.p2 = "";
        I.p1 = "";
        I.visible = true;
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
        float x = float.IsNaN(I.winX) ? (Screen.width - I.NW * scale) * 0.5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - height * scale) * 0.5f : I.winY;
        Vector3 mp = Input.mousePosition;
        return new Rect(x, y, I.NW * scale, height * scale).Contains(new Vector2(mp.x, Screen.height - mp.y));
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
        bgInput = loader.LoadGsa("res/uires/garagedlg/passwdinput.gsa");
        bgSet = loader.LoadGsa("res/uires/garagedlg/passwdset.gsa");
        bgMod = loader.LoadGsa("res/uires/garagedlg/passwdmod.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoOk = loader.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/cancelbtn.gsa");
    }

    private GUIStyle Edit()
    {
        if (_edit == null)
        {
            _edit = new GUIStyle(GUIStyle.none);
            _edit.font = NpcChatter.CnFont();
            _edit.alignment = TextAnchor.MiddleLeft;
            _edit.normal.textColor = Color.white;
        }
        _edit.fontSize = Mathf.RoundToInt(s * 12f);
        return _edit;
    }

    private string Pwd(string n, Rect r, string v)
    {
        GUI.color = new Color(0f, 0f, 0f, 0.3f);
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = Color.white;
        GUI.SetNextControlName(n);
        return GUI.PasswordField(new Rect(r.x + 3f, r.y, r.width - 6f, r.height), v ?? "", '*', 12, Edit());
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
        GUI.depth = 2;
        Texture2D bg = mode == 0 ? bgInput : mode == 1 ? bgSet : bgMod;
        if (bg != null)
        {
            GUI.DrawTexture(L(0, 0, NW, height), bg);
        }
        else
        {
            GUI.color = new Color(0.1f, 0.09f, 0.13f, 0.98f);
            GUI.DrawTexture(L(0, 0, NW, height), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        p1 = Pwd("pw1", L(26f, 52f, 168f, 12f), p1);
        if (mode != 0)
        {
            if (mode == 1)
            {
                p2 = Pwd("pw2", L(26f, 94f, 168f, 12f), p2);
            }
            else
            {
                p2 = Pwd("pw2", L(26f, 90f, 168f, 12f), p2);
                p3 = Pwd("pw3", L(26f, 127f, 168f, 12f), p3);
            }
        }
        float buttonY = mode != 0 ? 155f : 88f;
        WhUI.Btn(L(16f, buttonY, 70f, 22f), icoOk, "确定", mp, s);
        if (WhUI.Btn(L(136f, buttonY, 70f, 22f), icoCancel, "取消", mp, s)) visible = false;
        Rect close = L(190f, 5f, 20f, 19f);
        if (icoClose != null) GUI.DrawTexture(close, icoClose);
        if (WhUI.Click(close, mp)) visible = false;
        WinDrag.Handle(this, L(0, 0, NW, 28f), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, NW, height).Contains(mp) && !SysMsgBox.Visible)
            Event.current.Use();
    }
}
}
