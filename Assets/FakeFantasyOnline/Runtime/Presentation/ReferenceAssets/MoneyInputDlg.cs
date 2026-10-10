using System;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 1.5.8 tokens 0x06000a46..0x06000a57; native fixed-depth money popup.
public sealed class MoneyInputDlg : MonoBehaviour
{
    public static MoneyInputDlg I;
    private const float NW = 180f, NH = 66f;
    private bool visible, tried;
    private Texture2D bg;
    private string g = "0", sv = "0", c = "0";
    private GUIStyle _e;
    private float s, winX = float.NaN, winY;
    private int _wdGen = -1;
    private Action<long> onOk;
    private Func<long> capFn;
    private string capWhat = "", lastFocus = "";
    private float warnUntil;
    private long cap => capFn == null ? 0L : Math.Max(0L, capFn());

    private static string CoinStr(long v) => string.Format("{0}金{1}银{2}铜", v / 1000000, v % 1000000 / 1000, v % 1000);
    private static string Digits(string t)
    {
        if (string.IsNullOrEmpty(t)) return "";
        var b = new StringBuilder(t.Length);
        foreach (char ch in t) if (ch >= '0' && ch <= '9') b.Append(ch);
        return b.ToString();
    }
    private long Total()
    {
        long.TryParse(g, out long gold);
        long.TryParse(sv, out long silver);
        long.TryParse(c, out long copper);
        return unchecked(Math.Max(0L, gold) * 1000000L + Math.Max(0L, silver) * 1000L + Math.Max(0L, copper));
    }
    private void SetFields(long v)
    {
        v = Math.Max(0L, v);
        g = (v / 1000000).ToString();
        sv = (v % 1000000 / 1000).ToString();
        c = (v % 1000).ToString();
    }
    private bool ClampToCap()
    {
        long limit = cap;
        if (limit <= 0 || Total() <= limit) return false;
        SetFields(limit);
        GUIUtility.keyboardControl = 0;
        lastFocus = "";
        warnUntil = Time.time + 4f;
        return true;
    }
    private void Confirm()
    {
        long limit = cap;
        long amount = Total();
        if (limit > 0 && amount > limit)
        {
            ChatUI.Add("系统", capWhat + "只有 " + CoinStr(limit) + ", 已按上限处理。",
                new Color(1f, 220f / 255f, 130f / 255f, 1f));
            amount = limit;
        }
        Action<long> callback = onOk;
        visible = false;
        onOk = null;
        callback?.Invoke(amount);
    }
    public static void Toggle()
    {
        if (I == null)
        {
            var go = new GameObject("MoneyInputDlg");
            I = go.AddComponent<MoneyInputDlg>();
            DontDestroyOnLoad(go);
        }
        I.visible = !I.visible;
    }
    public static void Open(Action<long> cb, Rect? anchor = null, Func<long> capFn = null, string capWhat = "")
    {
        if (I == null)
        {
            var go = new GameObject("MoneyInputDlg");
            I = go.AddComponent<MoneyInputDlg>();
            DontDestroyOnLoad(go);
        }
        I.onOk = cb;
        I.g = I.sv = I.c = "0";
        I.visible = true;
        I.capFn = capFn;
        I.capWhat = string.IsNullOrEmpty(capWhat) ? "可用" : capWhat;
        I.lastFocus = "";
        I.warnUntil = 0;
        GUIUtility.keyboardControl = 0;
        if (!anchor.HasValue) { I.winX = float.NaN; return; }
        float factor = UIScale.Factor;
        Rect a = anchor.Value;
        I.winX = Mathf.Clamp(a.x, 0f, Mathf.Max(0f, Screen.width - NW * factor));
        float y = a.y - NH * factor - 4f * factor;
        if (y < 0f) y = Mathf.Min(a.yMax + 4f * factor, Mathf.Max(0f, Screen.height - NH * factor));
        I.winY = y;
        I._wdGen = WinDrag.Gen;
    }
    public static bool Visible => I != null && I.visible;
    public static bool MouseOver()
    {
        if (!Visible) return false;
        float factor = UIScale.Factor;
        float x = I.winX, y = I.winY;
        if (float.IsNaN(x))
        {
            x = (Screen.width - NW * factor) * 0.5f;
            y = (Screen.height - NH * factor) * 0.5f;
        }
        Vector3 mouse = Input.mousePosition;
        return new Rect(x, y, NW * factor, NH * factor).Contains(new Vector2(mouse.x, Screen.height - mouse.y));
    }
    private void Load()
    {
        tried = true;
        bg = ResLoader.I?.LoadGsa("res/uires/gameui/moneyinput.gsa");
    }
    private Rect L(float x, float y, float w, float h) => new Rect(winX + x * s, winY + y * s, w * s, h * s);
    private GUIStyle E()
    {
        if (_e == null)
        {
            _e = new GUIStyle(GUIStyle.none) { font = NpcChatter.CnFont(), alignment = TextAnchor.MiddleLeft };
            _e.normal.textColor = Color.white;
        }
        _e.fontSize = (int)Math.Round(s * 12f);
        return _e;
    }
    private string F(string n, Rect r, string v)
    {
        GUI.SetNextControlName(n);
        return Digits(GUI.TextField(r, v ?? "", 7, E()));
    }
    private Rect Btn(float x, string label, Vector2 mp, out bool hov)
    {
        Rect r = L(x, 44f, 48f, 18f);
        hov = r.Contains(mp);
        // The small controls retain their native geometry and MouseDown handling below.
        ArtworkSkin.ButtonFace(r, label, s, GUI.enabled, hov,
            pressed: hov && Input.GetMouseButton(0));
        return r;
    }
    private void OnGUI()
    {
        if (!visible) return;
        if (!tried) Load();
        s = UIScale.Factor;
        if (WinDrag.Stale(winX, ref _wdGen))
        {
            winX = (Screen.width - NW * s) * .5f;
            winY = (Screen.height - NH * s) * .5f;
        }
        Event e = Event.current;
        Vector2 mp = e.mousePosition;
        GUI.depth = 2;
        ArtworkSkin.Panel(L(0, 0, NW, NH), s);
        if (bg != null) GUI.DrawTexture(L(0, 0, NW, 26), bg);
        string oldG = g, oldS = sv, oldC = c;
        g = F("mi_g", L(21, 7, 24, 12), g);
        sv = F("mi_s", L(66, 7, 21, 12), sv);
        c = F("mi_c", L(107, 7, 21, 12), c);
        if (g != oldG || sv != oldS || c != oldC) ClampToCap();
        string focus = GUI.GetNameOfFocusedControl();
        if (focus != lastFocus)
        {
            if (lastFocus == "mi_g" && g.Length == 0) g = "0";
            else if (lastFocus == "mi_s" && sv.Length == 0) sv = "0";
            else if (lastFocus == "mi_c" && c.Length == 0) c = "0";
            if (focus == "mi_g" && g == "0") g = "";
            else if (focus == "mi_s" && sv == "0") sv = "";
            else if (focus == "mi_c" && c == "0") c = "";
            lastFocus = focus;
        }
        long limit = cap;
        bool fontReady = BitmapFont.Ready;
        if (fontReady && limit > 0)
        {
            bool warning = Time.time < warnUntil;
            string label = warning ? "最多 " + CoinStr(limit) + ", 已按上限填入" : capWhat + " " + CoinStr(limit);
            BitmapFont.CenterOutline(L(0, 28, NW, 12), label,
                warning ? new Color(1f, .55f, .45f, 1f) : new Color(.85f, .82f, .72f, 1f), s);
        }
        bool all = false;
        if (limit > 0) Btn(10, "全部", mp, out all);
        Btn(66, "确定", mp, out bool ok);
        Btn(122, "取消", mp, out bool cancel);
        bool click = e.type == EventType.MouseDown && e.button == 0;
        if (click && all)
        {
            SetFields(limit);
            GUIUtility.keyboardControl = 0;
            lastFocus = "";
            e.Use();
        }
        else if (click && ok) { Confirm(); e.Use(); }
        else if (click && cancel) { visible = false; onOk = null; e.Use(); }
        else if (e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)) { Confirm(); e.Use(); }
        else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape) { visible = false; onOk = null; e.Use(); }
        WinDrag.Handle(this, L(0, 0, NW, 6), ref winX, ref winY);
        WinDrag.Handle(this, L(133, 6, 45, 18), ref winX, ref winY);
        if (e.type == EventType.MouseDown && L(0, 0, NW, NH).Contains(mp) && !SysMsgBox.Visible) e.Use();
    }
}
}
