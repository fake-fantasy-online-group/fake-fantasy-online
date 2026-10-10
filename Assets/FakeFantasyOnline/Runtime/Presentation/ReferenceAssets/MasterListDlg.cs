using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 MasterListDlg 0x060005c4–0x060005ca.
public partial class MasterListDlg : MonoBehaviour
{
    public static MasterListDlg I;
    private bool visible;
    private bool tried;
    private Texture2D bg;
    private Texture2D icoClose;
    private Texture2D icoShowAll;
    private Texture2D icoMake;
    private Texture2D icoCancel;
    private const float NW = 439f;
    private const float NH = 449f;
    private float s;
    private float winX = float.NaN;
    private float winY;
    private int _wdGen = -1;

    private static void Ensure()
    {
        if (I != null) return;
        GameObject owner = new GameObject("MasterListDlg");
        I = owner.AddComponent<MasterListDlg>();
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

    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        bg = loader.LoadGsa("res/uires/master/masterlistdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoShowAll = loader.LoadGsa("res/uires/master/showallbtn.gsa");
        icoMake = loader.LoadGsa("res/uires/master/makemaster.gsa");
        icoCancel = loader.LoadGsa("res/uires/common/closebtn.gsa");
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
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else PopUI.Panel(L(0, 0, NW, NH));
            PopUI.Box(L(15f, 62f, 409f, 348f));
            PopUI.Btn(L(20f, 420f, 90f, 22f), icoShowAll, "显示全部", mp, s);
            PopUI.Btn(L(280f, 420f, 70f, 22f), icoMake, "拜师", mp, s);
            if (PopUI.Btn(L(353f, 420f, 70f, 22f), icoCancel, "关闭", mp, s)) visible = false;
            Rect close = L(413f, 18f, 20f, 19f);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (PopUI.Click(close, mp)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 36f), ref winX, ref winY);
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
