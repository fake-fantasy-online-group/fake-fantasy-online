using System;
using System.Threading;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official DuelDlg behavior and Unity lifetime; the adapter supplies session commands.
internal sealed partial class DuelDlg : MonoBehaviour
{
    public struct Row
    {
        public int id;
        public string a, b;
        public int mode, phase, entryGold, spectators;
        public bool betsOpen;
    }
    private const float BW = 367f, BH = 327f;
    public static DuelDlg I;
    private static bool _visible;
    private static int _tab, _targetId, _mode;
    private static string _targetName, _betText;
    private static int _sel;
    public static int entryGold, feePct, betMin, betMax;
    public static readonly System.Collections.Generic.List<Row> rows;
    public static bool Typing;
    private static IDuelCommands commands;

    // .cctor（06000288）：官方静态构造按字段偏移顺序写入 _targetName(+0x18)、_betText(+0x28)、
    // _sel(+0x30)、entryGold(+0x34)、feePct(+0x38)、betMin(+0x3c)、betMax(+0x40) 与 rows(+0x48)。
    // arm64 0x3e18c0（264 字节）/ x86_64 0x3f6020（256 字节）双架构一致。
    static DuelDlg()
    {
        _targetName = string.Empty;
        _betText = "10";
        _sel = -1;
        entryGold = 100;
        feePct = 10;
        betMin = 1;
        betMax = 1000;
        rows = new System.Collections.Generic.List<Row>();
    }

    // .ctor（06000287）：官方仅分配三个按钮帧数组（实例偏移 +0x48 / +0x50 / +0x58），
    // 随后调用 MonoBehaviour..ctor。arm64 0x3e1818（168 字节）/ x86_64 0x3f5f80（160 字节）。
    public DuelDlg()
    {
        redBtn = new Texture2D[4];
        okBtn = new Texture2D[4];
        noBtn = new Texture2D[4];
    }

    internal static Action BindCommands(IDuelCommands bound)
    {
        commands = bound;
        RebindAssetsForIndependentRuntime();
        return () =>
        {
            if (!ReferenceEquals(commands, bound)) return;
            commands = null;
        };
    }
    internal static void RebindAssetsForIndependentRuntime()
    {
        if (I != null) I.triedAssets = false;
    }
    public static bool Visible => _visible;
    internal static int CurrentTab => _tab;
    internal static WorldDuelState Snapshot()
    {
        var result = new System.Collections.Generic.List<WorldDuelRowState>(rows.Count);
        foreach (Row row in rows)
            result.Add(new WorldDuelRowState(row.id, row.a, row.b, row.mode, row.phase,
                row.entryGold, row.spectators, row.betsOpen));
        return new WorldDuelState(_visible, _targetId, _targetName, _mode, entryGold, feePct, betMin, betMax, result);
    }
    public static void Open(int targetId, string targetName, int tab = -1)
    {
        if (I == null)
        {
            var go = new GameObject("DuelDlg");
            I = go.AddComponent<DuelDlg>();
            DontDestroyOnLoad(go);
        }
        _targetId = targetId;
        if (!string.IsNullOrEmpty(targetName)) _targetName = targetName;
        _tab = tab < 0 ? (string.IsNullOrEmpty(targetName) ? 1 : 0) : tab;
        _visible = true;
        _ = commands?.RequestListAsync(CancellationToken.None);
    }
    public static void Close()
    {
        _visible = false;
        Typing = false;
        GUI.FocusControl(null);
    }
    public static void Toggle()
    {
        if (_visible) Close();
        else Open(0, string.Empty, 1);
    }
    public static void SetList(int entry, int fee, int minimum, int maximum,
        System.Collections.Generic.List<Row> items)
    {
        entryGold = entry; feePct = fee; betMin = minimum; betMax = maximum;
        rows.Clear(); rows.AddRange(items);
        if (_sel >= rows.Count) _sel = rows.Count - 1;
        if (_sel < 0 && rows.Count > 0) _sel = 0;
    }
    public static bool MouseOver()
    {
        if (!_visible) return false;
        return PanelRect().Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
    }
    internal static bool ContainsPointer(Vector2 pointer) => _visible && PanelRect().Contains(pointer);
    private void OnGUI()
    {
        if (!_visible || !BitmapFont.Ready) return;
        EnsureAssets();
        GUI.depth = 2;
        Event e = Event.current;
        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Escape)
        { Close(); e.Use(); return; }
        float scale = S;
        float font = Font;
        Rect panel = PanelRect();
        // 官方 OnGUI 把面板原点与缩放收进闭包，再以委托形式交给两个子绘制方法：
        // DuelDlg+<>c__DisplayClass56_0.<OnGUI>g__L|0（0600028b）读取 scale(+0x20)、x(+0x10)、y(+0x14)。
        float x = panel.x, y = panel.y;
        Rect L(float px, float py, float w, float h)
            => UIScale.Snap(new Rect(x + px * scale, y + py * scale, w * scale, h * scale));
        Color previous = GUI.color;
        if (bg != null) GUI.DrawTexture(panel, bg);
        else
        {
            GUI.color = new Color(.1f, .08f, .14f, .96f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        string title = "生 死 擂 台";
        TX(panel.x + (panel.width - BitmapFont.Measure(title, font)) * .5f,
            panel.y + 12f * scale, title, C(255, 226, 138), font);
        Rect close = L(337, 9, 20, 19);
        Texture2D closeIcon = close.Contains(e.mousePosition) && close1 != null ? close1 : close0;
        if (closeIcon != null) GUI.DrawTexture(close, closeIcon);
        if (GUI.Button(close, GUIContent.none, GUIStyle.none)) Close();
        else
        {
            if (Tab(L(16, 36, 100, 24), "约  斗", _tab == 0, font, scale)) _tab = 0;
            if (Tab(L(122, 36, 120, 24), "下注 · 观战", _tab == 1, font, scale))
            { _tab = 1; _ = commands?.RequestListAsync(CancellationToken.None); }
            EnsureEditStyle(scale);
            if (_tab == 0) DrawChallenge(panel, scale, font, L);
            else DrawBets(panel, scale, font, L);
            string focused = GUI.GetNameOfFocusedControl();
            Typing = focused == "duel_target" || focused == "duel_bet";
        }
        GUI.color = previous;
    }

    private static float S => UIScale.Factor;
    private static float Font => Mathf.Max(1f, Mathf.Floor(S));
    private static Rect Snap(Rect rect) => UIScale.Snap(rect);
    private static Vector2 Mouse => new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
    private static Rect PanelRect()
    {
        float scale = S;
        return Snap(new Rect((Screen.width - BW * scale) * .5f,
            (Screen.height - BH * scale) * .42f, BW * scale, BH * scale));
    }

}
}
