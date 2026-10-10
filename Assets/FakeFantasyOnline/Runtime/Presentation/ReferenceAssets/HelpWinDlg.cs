using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 06000a8a–06000a93, a 597x453 help browser: a keyword-filtered topic list on the left,
// a wrapped article pane on the right, a search field at the bottom and a close glyph at the top
// right. Toggle allocates the singleton and, when the window was hidden, asks NetClient for the
// topic table; the list keeps its own hit indices so the article pane is driven by sel, not by the
// raw topic index.
internal sealed class HelpWinDlg : MonoBehaviour
{
    public static readonly List<(string, string)> Topics;
    public static HelpWinDlg I;
    private const float NW = 597f, NH = 453f;
    private bool visible, tried;
    private Texture2D bg, icoClose, icoFind;
    private string kw = string.Empty;
    private GUIStyle _e, _t;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1, sel, listTop;
    private float contentScroll;
    private Vector2 _helpScroll, _leftScroll;
    private readonly List<string> _helpLines = new List<string>();
    private int _helpSel = -1;
    private float _helpW = -1f;

    // .cctor（06000a93）: the only static state the official initializer builds is the topic table
    // (arm64 0x2d85c4/148B, x86_64 0x2e9830/128B - new List<ValueTuple<string,string>> stored through
    // usage 754 into HelpWinDlg+0xb8 with a write barrier). I stays null, NW/NH are literals.
    static HelpWinDlg()
    {
        Topics = new List<(string, string)>();
    }

    // .ctor（06000a92）: empty keyword, NaN winX so the first layout centres the window, -1 drag
    // generation and -1/-1f article cache so the first paint always rebuilds the wrapped lines.
    public HelpWinDlg()
    {
    }

    // Toggle（06000a8a）: flips the flag and only fetches the topic table when opening.
    public static void Toggle()
    {
        if (I == null)
        {
            GameObject go = new GameObject("HelpWinDlg");
            I = go.AddComponent<HelpWinDlg>();
            DontDestroyOnLoad(go);
        }
        bool wasVisible = I.visible;
        I.visible = !I.visible;
        if (!wasVisible) NetClient.I?.HelpReqForIndependentRuntime();
    }

    // get_Visible（06000a8b）
    public static bool Visible => I != null && I.visible;

    // MouseOver: re-tests winX for both centred fallbacks.
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        float scale = UIScale.Factor;
        float mx = Input.mousePosition.x;
        int height = Screen.height;
        float mouseY = Input.mousePosition.y;
        float x = float.IsNaN(I.winX) ? (Screen.width + scale * -NW) * .5f : I.winX;
        float y = float.IsNaN(I.winX) ? (Screen.height + scale * -NH) * .5f : I.winY;
        float my = height - mouseY;
        return mx >= x && mx < x + NW * scale && my >= y && my < y + NH * scale;
    }

    // Load（06000a8d）: clears tried again when the loader is not up yet so the next paint retries.
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null)
        {
            tried = false;
            return;
        }
        bg = loader.LoadGsa("res/uires/helpwin/helpwindlg.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoFind = loader.LoadGsa("res/uires/common/findbtn.gsa");
    }

    // L（06000a8e）
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);

    // E（06000a8f）: the field style, built once and re-sized every paint.
    private GUIStyle E()
    {
        if (_e == null)
        {
            _e = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _e.normal.textColor = Color.white;
        }
        _e.fontSize = Mathf.RoundToInt(s * 12f);
        return _e;
    }

    // T（06000a90）: the row style, re-coloured per row by the list loop.
    private GUIStyle T()
    {
        if (_t == null)
        {
            _t = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
        }
        _t.fontSize = Mathf.RoundToInt(s * 12f);
        return _t;
    }

    // OnGUI（06000a91）
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        (int depth, bool dragged) = WinZ.Frame(this, new Rect(winX, winY, s * NW, s * NH), 10);
        Vector2 mp0 = Event.current.mousePosition;
        if (dragged) Event.current.mousePosition = new Vector2(-9999f, -9999f);

        try
        {
            s = UIScale.Factor;
            if (WinDrag.Stale(winX, ref _wdGen))
            {
                winX = (Screen.width + s * -NW) * .5f;
                winY = (Screen.height + s * -NH) * .5f;
            }
            Vector2 mp = Event.current.mousePosition;
            float mx = mp.x, my = mp.y;
            GUI.depth = depth;

            Rect plate = new Rect(winX, winY, s * NW, s * NH);
            if (bg != null)
            {
                GUI.color = PopUI.Backdrop;
                GUI.DrawTexture(plate, Texture2D.whiteTexture);
                GUI.color = Color.white;
                GUI.DrawTexture(plate, bg);
            }
            else
            {
                GUI.color = new Color(.09f, .08f, .12f, .98f);
                GUI.DrawTexture(plate, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }

            List<int> hits = new List<int>();
            string key = (kw ?? string.Empty).Trim();
            for (int i = 0; i < Topics.Count; i++)
            {
                if (key.Length == 0 || Topics[i].Item1.Contains(key) || Topics[i].Item2.Contains(key))
                    hits.Add(i);
            }
            if (hits.Count <= sel) sel = 0;

            float listX = winX + 10f * s, listY = winY + 40f * s;
            float rowH = 22f * s;
            if (Topics.Count == 0)
                GUI.Label(new Rect(listX + 8f * s, listY + 8f * s, 240f * s - 16f * s, rowH), "加载中…", E());

            float listW = 240f * s - 8f * s;
            float listH = 383f * s - 8f * s;
            _leftScroll = GUI.BeginScrollView(new Rect(listX + 4f * s, listY + 4f * s, listW, listH), _leftScroll,
                new Rect(0f, 0f, listW, Mathf.Max(listH, 2f * s + rowH * hits.Count)), GUIStyle.none, GUIStyle.none);
            for (int i = 0; i < hits.Count; i++)
            {
                float rowY = rowH * i;
                float rowW = listW - 2f * s;
                float sx = (mx - (listX + 4f * s)) + _leftScroll.x;
                float sy = (my - (listY + 4f * s)) + _leftScroll.y;
                bool hover = sx >= s && sx < s + rowW && sy >= rowY && sy < rowH + rowY;
                bool isSel = i == sel;
                if (isSel)
                {
                    GUI.color = new Color(.85f, .66f, .16f, .35f);
                    GUI.DrawTexture(new Rect(s, rowY, rowW, rowH), Texture2D.whiteTexture);
                    GUI.color = Color.white;
                }
                GUIStyle row = T();
                float dim = hover || isSel ? 1f : .8f;
                row.normal.textColor = new Color(dim, isSel ? .85f : dim, isSel ? .3f : dim, 1f);
                GUI.Label(new Rect(s + 6f * s, rowY, rowW - 8f * s, rowH), Topics[hits[i]].Item1, row);
                if (hover && Event.current.type == EventType.MouseDown && Event.current.button == 0)
                {
                    sel = i;
                    contentScroll = 0f;
                    _helpScroll = Vector2.zero;
                    Event.current.Use();
                }
            }
            GUI.EndScrollView();
            NativeScroll.BarAt(this, new Rect(winX + 251f * s, winY + 42f * s, 15f * s, 379f * s),
                listH, 2f * s + rowH * hits.Count, ref _leftScroll);

            if (hits.Count > 0)
            {
                int idx = Mathf.Clamp(sel, 0, hits.Count - 1);
                (string title, string body) = Topics[hits[idx]];
                float bodyX = winX + 276f * s, bodyY = winY + 41f * s;
                if (BitmapFont.Ready)
                    BitmapFont.DrawOutline(bodyX + 10f * s, bodyY + 10f * s, title, new Color(.88f, .78f, .54f, 1f), s);
                float wrapW = 291f * s - 30f * s;
                if (BitmapFont.Ready && (hits[idx] != _helpSel || !Mathf.Approximately(_helpW, wrapW)))
                {
                    _helpSel = hits[idx];
                    _helpW = wrapW;
                    _helpLines.Clear();
                    string[] parts = NoticeBoard.Strip(body ?? string.Empty).Split('\n');
                    for (int j = 0; j < parts.Length; j++)
                        _helpLines.AddRange(EmoteText.Wrap(parts[j], wrapW - 4f * s, s));
                }
                float bodyH = 401f * s - 40f * s;
                float lineH = 24f * s;
                _helpScroll = GUI.BeginScrollView(new Rect(bodyX + 8f * s, bodyY + 30f * s, wrapW, bodyH), _helpScroll,
                    new Rect(0f, 0f, wrapW, Mathf.Max(bodyH, lineH * _helpLines.Count)), GUIStyle.none, GUIStyle.none);
                if (BitmapFont.Ready)
                {
                    float top = _helpScroll.y;
                    for (int i = 0; i < _helpLines.Count; i++)
                    {
                        float y = i * lineH;
                        if (top - lineH > y + lineH) continue;
                        if (bodyH + top + lineH < y) break;
                        string line = _helpLines[i];
                        if (line.Length > 0)
                            EmoteText.DrawColored(s + s, y, line, new Color(.81f, .75f, .62f, 1f), s, true);
                    }
                }
                GUI.EndScrollView();
                NativeScroll.BarAt(this, new Rect(winX + 573f * s, winY + 42f * s, 15f * s, 399f * s),
                    bodyH, Mathf.Max(bodyH, lineH * _helpLines.Count), ref _helpScroll);
                contentScroll = _helpScroll.y;
            }

            GUI.color = new Color(0f, 0f, 0f, .3f);
            GUI.DrawTexture(new Rect(winX + 15f * s, winY + 431f * s, 188f * s, 12f * s), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.SetNextControlName("help_k");
            kw = GUI.TextField(new Rect(winX + 15f * s + 3f, winY + 431f * s, 188f * s - 6f, 12f * s),
                kw ?? string.Empty, 16, E());

            if (PopUI.Btn(new Rect(winX + 210f * s, winY + 426f * s, 58f * s, 22f * s), icoFind, "查找", new Vector2(mx, my), s))
            {
                sel = 0;
                listTop = 0;
                GUI.FocusControl(null);
            }

            Rect closeRect = new Rect(winX + 570f * s, winY + 17f * s, 20f * s, 19f * s);
            if (icoClose != null) GUI.DrawTexture(closeRect, icoClose);
            if (PopUI.Click(closeRect, new Vector2(mx, my))) visible = false;

            WinDrag.Handle(this, new Rect(winX, winY, NW * s, 36f * s), ref winX, ref winY);
            if (Event.current.type == EventType.MouseDown && winX <= mx && mx < winX + NW * s &&
                winY <= my && my < winY + NH * s && !SysMsgBox.Visible)
                Event.current.Use();
        }
        finally
        {
            if (dragged) Event.current.mousePosition = mp0;
        }
    }
}
}
