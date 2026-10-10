using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 06000d55–06000d5d. The native Apply button consumes its click without
// persisting these three local options; option captions are part of the package image.
internal sealed class FriendConfigDlg : MonoBehaviour
{
    public static FriendConfigDlg I;
    private const float NW = 145, NH = 212;
    private static readonly float[] CY = { 46, 65, 84 };
    private static readonly string[] OPTS = { "允许加为好友", "拒绝陌生人", "在线提示", "显示QQ状态" };
    private bool visible, tried;
    private Texture2D bg, icoClose, icoApply, chkOff, chkOn;
    private readonly bool[] opt = new bool[3];
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    public static bool Visible => I != null && I.visible;
    public static void Toggle()
    {
        if (I == null)
        {
            var go = new GameObject("FriendConfigDlg");
            I = go.AddComponent<FriendConfigDlg>();
            DontDestroyOnLoad(go);
        }
        I.visible = !I.visible;
    }
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(pointer);
    }
    internal bool ContainsPointer(Vector2 pointer)
    {
        if (!visible) return false;
        float scale = UIScale.Factor;
        return new Rect(float.IsNaN(winX) ? (Screen.width - NW * scale) * .5f : winX,
            float.IsNaN(winX) ? (Screen.height - NH * scale) * .5f : winY, NW * scale, NH * scale).Contains(pointer);
    }
    private void Load()
    {
        tried = true; ResLoader r = ResLoader.I;
        if (r == null) { tried = false; return; }
        bg = r.LoadGsa("res/uires/teamdlg/friendconfigdlg.gsa");
        icoClose = r.LoadGsa("res/uires/common/miniclose.gsa");
        icoApply = r.LoadGsa("res/uires/common/applybtn.gsa");
        chkOff = r.LoadGsaFrame("res/uires/common/checkbox.gsa", 0);
        chkOn = r.LoadGsaFrame("res/uires/common/checkbox.gsa", 1);
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Lbl(float x, float y, string text, Color color)
    { if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, color, s); }
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        var frame = WinZ.Frame(this, new Rect(winX, winY, NW * s, NH * s), 10);
        Vector2 saved = Event.current.mousePosition;
        if (frame.Item2) Event.current.mousePosition = new Vector2(-9999, -9999);
        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen)) { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
            Vector2 pointer = Event.current.mousePosition; GUI.depth = frame.Item1;
            if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
            else { GUI.color = new Color(.1f, .09f, .13f, .98f); GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture); GUI.color = Color.white; }
            for (int i = 0; i < CY.Length; i++)
            {
                Rect checkbox = L(30, CY[i], 13, 13);
                Texture2D image = opt[i] ? chkOn : chkOff;
                if (image != null) GUI.DrawTexture(checkbox, image);
                else { GUI.color = new Color(0, 0, 0, .6f); GUI.DrawTexture(checkbox, Texture2D.whiteTexture); GUI.color = Color.white; }
                if (SocUI.Click(L(30, CY[i], 100, 14), pointer)) opt[i] = !opt[i];
            }
            SocUI.Btn(L(32, 186, 90, 22), icoApply, "应用", pointer, s);
            Rect close = L(120, 5, 14, 14);
            if (icoClose != null) GUI.DrawTexture(close, icoClose);
            if (SocUI.Click(close, pointer)) visible = false;
            WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(pointer) && !SysMsgBox.Visible) Event.current.Use();
        }
        finally { if (frame.Item2) Event.current.mousePosition = saved; }
    }
    internal static void ToggleForIndependentRuntime() => Toggle();
}
}
