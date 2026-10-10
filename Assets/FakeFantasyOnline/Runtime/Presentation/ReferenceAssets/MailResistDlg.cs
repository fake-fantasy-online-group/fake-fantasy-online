using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed class MailResistDlg : MonoBehaviour
{
    public static MailResistDlg I;
    private bool visible, tried;
    private int selected = -1;
    private Texture2D radioOff, radioOn;
    private static readonly float[] CY = { 8f, 24f, 40f, 56f };
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    public static void Toggle()
    {
        if (I == null)
        {
            var go = new GameObject("MailResistDlg");
            I = go.AddComponent<MailResistDlg>();
            DontDestroyOnLoad(go);
        }
        I.visible = !I.visible;
    }
    public static bool Visible => I != null && I.visible;
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - 90 * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - 80 * scale) * .5f : I.winY;
        return new Rect(x, y, 90 * scale, 80 * scale).Contains(pointer);
    }
    internal static bool ContainsPointer(Vector2 pointer)
    {
        if (!Visible) return false;
        float scale = UIScale.Factor;
        float x = float.IsNaN(I.winX) ? (Screen.width - 90 * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - 80 * scale) * .5f : I.winY;
        return new Rect(x, y, 90 * scale, 80 * scale).Contains(pointer);
    }
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        radioOff = loader.LoadGsaFrame("res/uires/common/radiobtn.gsa", 0);
        radioOn = loader.LoadGsaFrame("res/uires/common/radiobtn.gsa", 1);
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        { winX = (Screen.width - 90 * s) * .5f; winY = (Screen.height - 80 * s) * .5f; }
        Vector2 pointer = Event.current.mousePosition;
        GUI.depth = 2;
        TmUI.Panel(L(0, 0, 90, 80));
        for (int i = 0; i < CY.Length; i++)
        {
            TmUI.Box(L(9, CY[i], 14, 13), selected == i ? radioOn : radioOff);
            if (TmUI.Click(L(9, CY[i], 72, 16), pointer)) selected = i;
        }
        WinDrag.Handle(this, L(0, 0, 90, 28), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, 90, 80).Contains(pointer) && !SysMsgBox.Visible) Event.current.Use();
    }
}
}
