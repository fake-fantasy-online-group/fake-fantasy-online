using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 06000d3c–06000d43. Native content is the package background and an empty
// dark inset; affirmative/cancel button results are unused, while the close icon closes.
internal sealed class MasterInfoDlg : MonoBehaviour
{
    public static MasterInfoDlg I;
    private const float NW = 262, NH = 282;
    private bool visible, tried;
    private Texture2D bg, icoClose, icoAffirm, icoCancel;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    public static bool Visible => I != null && I.visible;
    public static void Toggle()
    {
        if (I == null)
        {
            GameObject go = new GameObject("MasterInfoDlg");
            I = go.AddComponent<MasterInfoDlg>();
            DontDestroyOnLoad(go);
        }
        I.visible = !I.visible;
    }
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        return new Rect(float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX,
            float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY, NW * scale, NH * scale).Contains(pointer);
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
        bg = r.LoadGsa("res/uires/master/masterinfodlg.gsa");
        icoClose = r.LoadGsa("res/uires/common/miniclose.gsa");
        icoAffirm = r.LoadGsa("res/uires/common/affirmbtn.gsa");
        icoCancel = r.LoadGsa("res/uires/common/cancelbtn.gsa");
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void Lbl(float x, float y, string text, Color color)
    { if (BitmapFont.Ready) BitmapFont.DrawOutline(winX + x * s, winY + y * s, text, color, s); }
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen)) { winX = (Screen.width - NW * s) * .5f; winY = (Screen.height - NH * s) * .5f; }
        Vector2 pointer = Event.current.mousePosition; GUI.depth = 2;
        if (bg != null) GUI.DrawTexture(L(0, 0, NW, NH), bg);
        else { GUI.color = new Color(.1f, .09f, .13f, .98f); GUI.DrawTexture(L(0, 0, NW, NH), Texture2D.whiteTexture); GUI.color = Color.white; }
        GUI.color = new Color(0, 0, 0, .18f); GUI.DrawTexture(L(20, 40, 222, 200), Texture2D.whiteTexture); GUI.color = Color.white;
        SocUI.Btn(L(18, 256, 70, 22), icoAffirm, "确定", pointer, s);
        SocUI.Btn(L(180, 256, 70, 22), icoCancel, "取消", pointer, s);
        Rect close = L(237, 9, 20, 19);
        if (icoClose != null) GUI.DrawTexture(close, icoClose);
        if (SocUI.Click(close, pointer)) visible = false;
        WinDrag.Handle(this, L(0, 0, NW, 28), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(pointer) && !SysMsgBox.Visible) Event.current.Use();
    }
    internal static void ToggleForIndependentRuntime() => Toggle();
}
}
