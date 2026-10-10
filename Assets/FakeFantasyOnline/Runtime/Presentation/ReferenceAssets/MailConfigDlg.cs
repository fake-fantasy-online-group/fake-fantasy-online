using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed class MailConfigDlg : MonoBehaviour
{
    public static MailConfigDlg I;
    private bool visible, tried;
    private Texture2D bg, icoClose, icoApply, chkOff, chkOn;
    private readonly bool[] opt = new bool[2];
    private static readonly float[] CY = { 46f, 72f };
    private const float NW = 145f, NH = 212f;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    public static void Toggle()
    {
        if (I == null)
        {
            var go = new GameObject("MailConfigDlg");
            I = go.AddComponent<MailConfigDlg>();
            DontDestroyOnLoad(go);
        }
        I.visible = !I.visible;
    }
    public static bool Visible => I != null && I.visible;
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float pointerX = Input.mousePosition.x;
        int screenHeight = Screen.height;
        float pointerY = Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(new Vector2(pointerX, screenHeight - pointerY));
    }
    internal static bool ContainsPointer(Vector2 pointer)
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(pointer);
    }
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa("res/uires/maildlg/mailconfigdlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoApply = loader.LoadGsa("res/uires/common/applybtn.gsa");
        chkOff = loader.LoadGsaFrame("res/uires/common/checkbox.gsa", 0);
        chkOn = loader.LoadGsaFrame("res/uires/common/checkbox.gsa", 1);
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        // Native Frame precedes refreshing s and recentering stale coordinates.
        var frame = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999, -9999);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
            Vector2 pointer = Event.current.mousePosition;
            GUI.depth = frame.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg); else TmUI.Panel(L(0, 0, NW, NH));
            for (int i = 0; i < CY.Length; i++)
            {
                TmUI.Box(L(16, CY[i], 14, 13), opt[i] ? chkOn : chkOff);
                if (TmUI.Click(L(16, CY[i], 110, 14), pointer)) opt[i] = !opt[i];
            }
            // Official ignores the return value: no persistence or send exists here.
            TmUI.Btn(L(32, 186, 90, 22), icoApply, "应用", pointer, s);
            Rect close = L(120, 4, 20, 19);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (TmUI.Click(close, pointer)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(pointer) && !SysMsgBox.Visible) Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
}
}
