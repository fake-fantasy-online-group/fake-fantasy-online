using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 06000a58–06000a61, a 240x92 centred panel: title bar, hint line, one name field and a
// confirm/cancel pair. Open allocates the singleton and clears the field, Close only clears the
// visible flag, and confirm (the confirm button, Return or KeypadEnter) submits the trimmed name
// through NetClient while cancel (the cancel button or Escape) just hides the window.
internal sealed class RenameDlg : MonoBehaviour
{
    public static RenameDlg I;
    private const float NW = 240, NH = 92;
    private bool visible;
    private string nm;
    private GUIStyle _e;
    private float s, winX, winY;
    private int _wdGen;

    // .ctor（06000a61）: clears the name, parks the window at NaN so the first layout centres it,
    // and seeds the window-generation cache at -1 so WinDrag.Stale reports stale on first paint.
    public RenameDlg()
    {
        nm = string.Empty;
        winX = float.NaN;
        _wdGen = -1;
    }

    // Open（06000a58）
    public static void Open()
    {
        if (I == null)
        {
            GameObject go = new GameObject("RenameDlg");
            I = go.AddComponent<RenameDlg>();
            DontDestroyOnLoad(go);
        }
        I.nm = string.Empty;
        I.visible = true;
    }

    // Close（06000a59）
    public static void Close()
    {
        if (I != null) I.visible = false;
    }

    // get_Visible（06000a5a）
    public static bool Visible => I != null && I.visible;

    // MouseOver（06000a5b）: the official body re-tests winX (not winY) for the vertical centre,
    // the same reading the other recovered windows carry, so the fallback is driven by winX alone.
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        float x = float.IsNaN(I.winX) ? (Screen.width - NW * scale) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height - NH * scale) * .5f : I.winY;
        return new Rect(x, y, NW * scale, NH * scale).Contains(pointer);
    }

    // L（06000a5c）
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    // E（06000a5d）: the field style is built once and re-sized every paint so the font follows UIScale.
    private GUIStyle E()
    {
        if (_e == null)
        {
            _e = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _e.normal.textColor = Color.white;
            _e.hover.textColor = Color.white;
            _e.active.textColor = Color.white;
            _e.focused.textColor = Color.white;
        }
        _e.fontSize = Mathf.RoundToInt(13f * s);
        return _e;
    }

    // Confirm（06000a5f）: hides first, then sends, so the window closes even when NetClient has no instance.
    private void Confirm()
    {
        string name = (nm ?? string.Empty).Trim();
        if (name.Length == 0) return;
        visible = false;
        if (NetClient.I != null) NetClient.I.SendRenameCharForIndependentRuntime(name);
    }

    // OnGUI（06000a60）
    private void OnGUI()
    {
        if (!visible) return;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;
        try { DrawWindow(); }
        finally
        {
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }

    private void DrawWindow()
    {
        if (!visible) return;
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * .5f;
            winY = (Screen.height - NH * s) * .5f;
        }
        Vector2 mp = Event.current.mousePosition;
        GUI.depth = 2;
        ArtworkSkin.Panel(L(0, 0, NW, NH), s);
        ArtworkSkin.Panel(L(2, 2, 236, 18), s, true);
        if (BitmapFont.Ready) BitmapFont.CenterOutline(L(0, 5, NW, 14), "角色改名", new Color(1f, .9f, .55f, 1f), s);
        if (BitmapFont.Ready) BitmapFont.CenterOutline(L(0, 26, NW, 12), "请输入新的角色名字", new Color(.85f, .82f, .72f, 1f), s);
        Rect field = L(20, 42, 200, 20);
        ArtworkSkin.Input(new Rect(field.x - 2f * s, field.y - 2f * s, field.width + 4f * s, field.height + 4f * s),
            s, GUI.GetNameOfFocusedControl() == "rename_in");
        GUI.SetNextControlName("rename_in");
        nm = GUI.TextField(new Rect(field.x + 5f * s, field.y + 2f * s, field.width - 10f * s, field.height - 4f * s), nm ?? string.Empty, 16, E());
        Rect ok = L(52, 68, 60, 18), cancel = L(128, 68, 60, 18);
        bool overOk = ok.Contains(mp), overCancel = cancel.Contains(mp);
        ArtworkSkin.ButtonFace(ok, "确定", s, selected: overOk,
            pressed: overOk && Input.GetMouseButton(0));
        ArtworkSkin.ButtonFace(cancel, "取消", s, selected: overCancel,
            pressed: overCancel && Input.GetMouseButton(0));
        bool click = Event.current.type == EventType.MouseDown && Event.current.button == 0;
        if (click && overOk)
        { Confirm(); Event.current.Use(); }
        else if (click && overCancel)
        { visible = false; Event.current.Use(); }
        else if (Event.current.type == EventType.KeyDown &&
            (Event.current.keyCode == KeyCode.Return || Event.current.keyCode == KeyCode.KeypadEnter))
        { Confirm(); Event.current.Use(); }
        else if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        { visible = false; Event.current.Use(); }
        WinDrag.Handle(this, L(0, 0, NW, 20), ref winX, ref winY);
        if (Event.current.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible) Event.current.Use();
    }
}
}
