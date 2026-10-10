using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 ArenaDeathDlg (竞技场阵亡窗口), restored from the whole native 17-member surface
/// (tokens 0x06000030..0x06000040) plus the literal fields I / PH / PW / _visible / _deathNo /
/// _freeLeft / _kind / _waitSec / _amount / _label / _ladder / _afford / _recvTime.
/// Evidence: the frozen exports in
/// Build/ReferenceReverseGhidra-1.5.8/queries/wbb027-arenadeath-arm64 and
/// .../wbb027-arenadeath-x86_64, and the metadata string literals
/// 11389 / 16664 / 16963 / 17045 / 17072 / 17073 / 17190 / 17259 / 17260 / 17261 / 17262 / 17263 /
/// 17683 / 17895 / 18011 / 18012 / 18340 of Build/ReferenceReverseIndex-1.5.8/index.sqlite3.
///
/// The window is opened only by the official receive branch (NetClient.Dispatch jump-table entry
/// 0x8061 -> arm64 0x2528d0) and closed by the same branch's zero flag, by the 0x801a local revive
/// tail and by both buttons. It never sends anything on its own: 复活 and 认输 both go through
/// NetClient.ArenaRevive (0x06000746, opcode 0x1084) and the outcome stays server-authored.
/// </summary>
public partial class ArenaDeathDlg : MonoBehaviour
{
    /// <summary>Official public static field I (offset +0x0); created lazily by <see cref="Show"/>.</summary>
    public static ArenaDeathDlg I;

    // Official private const (literal, flags 0x8051) panel size in design pixels; PanelRect inlines
    // 340f as the width and 176f as the height, so they are never read from a static slot.
    private const float PW = 340f;
    private const float PH = 176f;

    private static bool _visible;
    private static int _deathNo;
    private static int _freeLeft;
    private static int _kind;
    private static int _waitSec;
    private static long _amount;
    // Official .cctor (0x06000040) stores the empty literal into both string fields.
    private static string _label = "";
    private static string _ladder = "";
    private static bool _afford;
    private static float _recvTime;

    /// <summary>Official get_Visible (0x06000030): the static _visible byte, no caller in 1.5.8.</summary>
    public static bool Visible
    {
        get
        {
            return _visible;
        }
    }

    /// <summary>
    /// 1.6.0 get_SecLeft (0x06000032): max(0, _waitSec - floorToInt(Time.realtimeSinceStartup -
    /// _recvTime)). The seconds still left before the server treats the round as surrendered.
    /// </summary>
    public static int SecLeft
    {
        get
        {
            return Mathf.Max(0, _waitSec - Mathf.FloorToInt(Time.realtimeSinceStartup - _recvTime));
        }
    }

    /// <summary>
    /// Official Show (0x06000032, eight parameters). Ensures the singleton exactly like the native
    /// body - new GameObject("ArenaDeathDlg") + AddComponent + DontDestroyOnLoad only while I is
    /// null (Unity Object == null test) - then stores every argument in the official order
    /// (_deathNo, _freeLeft, _kind, _amount, _label ?? "", _ladder ?? "", _afford, _waitSec,
    /// _recvTime = Time.realtimeSinceStartup, _visible) and ends with ReviveDlg.Hide (0x06000c7e).
    /// </summary>
    public static void Show(int deathNo, int freeLeft, int kind, long amount, string label,
        bool afford, int waitSec, string ladder)
    {
        if (I == null)
        {
            GameObject go = new GameObject("ArenaDeathDlg");
            I = go.AddComponent<ArenaDeathDlg>();
            Object.DontDestroyOnLoad(go);
        }
        _deathNo = deathNo;
        _freeLeft = freeLeft;
        _kind = kind;
        _amount = amount;
        _label = label ?? "";
        _ladder = ladder ?? "";
        _afford = afford;
        _waitSec = waitSec;
        _recvTime = Time.realtimeSinceStartup;
        _visible = true;
        ReviveDlg.Hide();
    }

    /// <summary>Official Close (0x06000033): the single unconditional static _visible = false.</summary>
    public static void Close()
    {
        _visible = false;
    }

    /// <summary>
    /// Official MouseOver (0x06000034), the PlayerController.OverAnyUI slot 13 hook. False while
    /// hidden; otherwise the panel rectangle test against Screen.height - Input.mousePosition.y.
    /// </summary>
    public static bool MouseOver()
    {
        if (!_visible)
        {
            return false;
        }
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        Rect r = PanelRect();
        return mx >= r.x && mx < r.x + r.width && my >= r.y && my < r.y + r.height;
    }

    /// <summary>Official get_S (0x06000035): UIScale.Factor. No caller in 1.5.8.</summary>
    private static float S
    {
        get
        {
            return UIScale.Factor;
        }
    }

    /// <summary>
    /// 1.6.0 get_Font (0x06000037): floating-point floor of the scale, clamped below 1.
    /// </summary>
    private static float Font
    {
        get
        {
            float f = Mathf.Floor(S);
            return f < 1f ? 1f : f;
        }
    }

    /// <summary>
    /// Official PanelRect (0x06000037): a 340x176 design-pixel panel, horizontally centered and
    /// placed at 34% of the screen height.
    /// </summary>
    private static Rect PanelRect()
    {
        float s = UIScale.Factor;
        return UIScale.Snap(new Rect((Screen.width - s * PW) * 0.5f, Screen.height * 0.34f,
            s * PW, s * PH));
    }

    /// <summary>
    /// Official BtnRevive (0x06000038): the left half of a 308-wide button row sitting 44 pixels
    /// (scaled) below the panel.
    /// </summary>
    private static Rect BtnRevive()
    {
        Rect r = PanelRect();
        float s = S;
        return UIScale.Snap(new Rect(r.x + s * 16f, r.y + r.height - s * 44f,
            s * 308f * 0.5f - s * 6f, s * 30f));
    }

    /// <summary>
    /// Official BtnGiveUp (0x06000039): the right half of the same row, mirrored around the panel
    /// centre with the official 6-pixel gap.
    /// </summary>
    private static Rect BtnGiveUp()
    {
        Rect r = PanelRect();
        float s = S;
        return UIScale.Snap(new Rect(r.x + r.width * 0.5f + s * 6f, r.y + r.height - s * 44f,
            s * 308f * 0.5f - s * 6f, s * 30f));
    }

    /// <summary>Official Snap (0x0600003a): UIScale.Snap. No caller in 1.5.8.</summary>
    private static Rect Snap(Rect r)
    {
        return UIScale.Snap(r);
    }

    /// <summary>
    /// Official TX (0x0600003b): BitmapFont.DrawOutline with the official position rounding.
    /// No caller in 1.5.8; OnGUI calls BitmapFont.DrawOutline with the same rounding inline.
    /// </summary>
    private static void TX(float x, float y, string t, Color c, float sc)
    {
        BitmapFont.DrawOutline(Mathf.Round(x), Mathf.Round(y), t, c, sc);
    }

    private void OnGUI()
    {
        if (!_visible)
        {
            return;
        }

        float s = S;
        float font = Font;
        float hair = Mathf.Max(1f, Mathf.Floor(s));
        Texture2D dot = ZoneOverlay.Dot;
        Color restore = GUI.color;
        Rect panel = PanelRect();
        int secLeft = SecLeft;
        float now = Time.realtimeSinceStartup;

        float px = panel.x;
        float py = panel.y;
        float pw = panel.width;
        float ph = panel.height;
        float textX = px + 12f * s;
        float textW = pw - 24f * s;

        GUI.color = new Color(0.1f, 0.08f, 0.14f, 0.96f);
        GUI.DrawTexture(panel, dot);
        GUI.color = new Color(0.7176f, 0.6078f, 0.3843f, 0.8f);
        GUI.DrawTexture(Snap(new Rect(px, py, pw, hair)), dot);
        GUI.DrawTexture(Snap(new Rect(px, py + ph - hair, pw, hair)), dot);
        GUI.DrawTexture(Snap(new Rect(px, py, hair, ph)), dot);
        GUI.DrawTexture(Snap(new Rect(px + pw - hair, py, hair, ph)), dot);
        GUI.color = Color.white;

        float titleY = py + 10f * s;
        TX(textX, titleY, "你阵亡了", new Color(1f, 120f / 255f, 120f / 255f, 1f), font);
        string deathLine = string.Format("本局第 {0} 次", _deathNo);
        TX(px + pw - BitmapFont.Measure(deathLine, font) - 12f * s, titleY, deathLine,
            new Color(170f / 255f, 170f / 255f, 180f / 255f, 1f), font);

        string reviveLine;
        if (_kind == 3)
        {
            reviveLine = "复活机会已用完，本局失败";
        }
        else if (_kind == 0)
        {
            reviveLine = string.Format("免费重生（还剩 {0} 次）", _freeLeft);
        }
        else
        {
            reviveLine = string.Concat("复活需要 ", _label, _afford ? "" : "（余额不够）");
        }
        Color reviveColor = _kind == 3
            ? new Color(200f / 255f, 120f / 255f, 120f / 255f, 1f)
            : new Color(1f, 226f / 255f, 138f / 255f, 1f);
        float y = Wrap(textX, py + 32f * s, textW, reviveLine, reviveColor, font);

        Color ladderColor = new Color(160f / 255f, 160f / 255f, 172f / 255f, 1f);
        string ladderLine;
        if (_kind == 3)
        {
            ladderLine = "只能认输，等下一局";
        }
        else
        {
            ladderLine = string.IsNullOrEmpty(_ladder)
                ? "复活阶梯：免费 → 500银 → 5金 → 10彩玉 → 不能复活"
                : _ladder;
        }
        y = Wrap(textX, y + 2f * s, textW, ladderLine, ladderColor, font);
        if (_kind != 0 && _kind != 3)
        {
            y = Wrap(textX, y + 2f * s, textW, "不想付费就等倒计时走完（算本局结束），或直接认输",
                ladderColor, font);
        }

        float barY = py + 76f * s;
        float textBottom = y + 6f * s;
        barY = barY > textBottom ? barY : textBottom;
        float barH = 2f * s;
        if (barH < 2f)
        {
            barH = 2f;
        }
        float progress = 0f;
        if (_waitSec > 0)
        {
            progress = Mathf.Clamp01((float)secLeft / (float)_waitSec);
        }
        GUI.color = new Color(1f, 1f, 1f, 0.12f);
        GUI.DrawTexture(Snap(new Rect(textX, barY, textW, barH)), dot);
        GUI.color = 10 < secLeft
            ? new Color(0.85f, 0.75f, 0.45f, 0.9f)
            : new Color(1f, 0.35f, 0.3f, 0.9f);
        GUI.DrawTexture(Snap(new Rect(textX, barY, textW * progress, barH)), dot);
        GUI.color = Color.white;

        string countText = string.Format("{0} 秒后自动认输", secLeft);
        Color countColor;
        if (secLeft < 11)
        {
            float frac = now - Mathf.Floor(now);
            float pulse = Mathf.Abs(500f - frac * 1000f) / 500f;
            Color32 mixed = Color32.Lerp(new Color32(255, 80, 80, 255),
                new Color32(255, 220, 120, 255), pulse);
            countColor = new Color(mixed.r / 255f, mixed.g / 255f, mixed.b / 255f, mixed.a / 255f);
        }
        else
        {
            countColor = new Color(150f / 255f, 150f / 255f, 160f / 255f, 1f);
        }
        TX(textX, barY + 8f * s, countText, countColor, font);

        bool canRevive = _kind != 3 && _afford;
        bool canSurrender = ZoneOverlay.canSurrender;

        Rect reviveButton = BtnRevive();
        string reviveText;
        if (_kind == 0)
        {
            reviveText = "免费重生";
        }
        else if (string.IsNullOrEmpty(_label))
        {
            reviveText = "复活";
        }
        else
        {
            reviveText = string.Concat("复活 ", _label);
        }
        DrawBtn(reviveButton, reviveText, canRevive, font, hair, dot);
        DrawBtn(BtnGiveUp(), canSurrender ? "认输" : "认输(条件未到)", canSurrender, font, hair, dot);

        GUI.color = restore;

        // Both official buttons are evaluated in the same OnGUI pass: the revive button hides the
        // window and the give-up button is still hit-tested afterwards.
        if (canRevive)
        {
            if (GUI.Button(BtnRevive(), GUIContent.none, GUIStyle.none))
            {
                NetClient.I?.ArenaReviveForIndependentRuntime(false);
                _visible = false;
            }
        }
        if (!GUI.Button(BtnGiveUp(), GUIContent.none, GUIStyle.none))
        {
            return;
        }
        if (canSurrender)
        {
            NetClient.I?.ArenaReviveForIndependentRuntime(true);
            _visible = false;
            return;
        }
        if (!string.IsNullOrEmpty(ZoneOverlay.blockWhy))
        {
            ChatUI.Add("系统", ZoneOverlay.blockWhy, new Color(1f, 160f / 255f, 90f / 255f, 1f), "");
        }
    }

    /// <summary>
    /// Official Wrap (0x0600003d): draws <paramref name="text"/> wrapped to <paramref name="w"/>
    /// with BitmapFont.DrawOutline and returns the y of the line after the last one drawn.
    /// Line height is round(f * 12 + 3); x is rounded once, y is rounded per drawn line.
    /// </summary>
    private static float Wrap(float x, float y, float w, string text, Color col, float f)
    {
        if (string.IsNullOrEmpty(text))
        {
            return y;
        }

        float lineH = Mathf.Round(f * 12f + 3f);
        float rx = Mathf.Round(x);
        int start = 0;
        int take = 1;
        while (true)
        {
            if (BitmapFont.Measure(text.Substring(start, take), f) <= w)
            {
                if (start + take + 1 > text.Length)
                {
                    // The rest of the string fits on this line; draw it and stop.
                    string lastLine = text.Substring(start, take);
                    BitmapFont.DrawOutline(rx, Mathf.Round(y), lastLine, col, f);
                    return y + lineH;
                }
                take++;
                continue;
            }
            take--;
            if (take < 2)
            {
                take = 1;
            }
            string line = text.Substring(start, take);
            BitmapFont.DrawOutline(rx, Mathf.Round(y), line, col, f);
            y += lineH;
            start += take;
            if (start >= text.Length)
            {
                return y;
            }
            take = 1;
        }
    }

    /// <summary>
    /// Official DrawBtn (0x0600003e): a dot-filled button with a border of <paramref name="hair"/>
    /// and centred text. Disabled buttons get the darker fill and the grey border, a hovered one
    /// the bright gold border; the hover test only runs while the button is enabled.
    /// </summary>
    private static void DrawBtn(Rect r, string text, bool enabled, float f, float hair,
        Texture2D dot)
    {
        bool hover = false;
        Color fill;
        if (!enabled)
        {
            fill = new Color(0.14f, 0.13f, 0.16f, 0.9f);
        }
        else
        {
            float mx = Input.mousePosition.x;
            float my = Screen.height - Input.mousePosition.y;
            hover = r.x <= mx && mx < r.x + r.width && r.y <= my && my < r.y + r.height;
            fill = hover
                ? new Color(0.3f, 0.24f, 0.14f, 0.98f)
                : new Color(0.18f, 0.15f, 0.1f, 0.95f);
        }
        GUI.color = fill;
        GUI.DrawTexture(r, dot);

        if (!enabled)
        {
            GUI.color = new Color(0.4f, 0.4f, 0.42f, 0.8f);
        }
        else if (hover)
        {
            GUI.color = new Color(1f, 0.88f, 0.5f, 1f);
        }
        else
        {
            GUI.color = new Color(0.7176f, 0.6078f, 0.3843f, 0.8f);
        }
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, hair), dot);
        GUI.DrawTexture(new Rect(r.x, r.y + r.height - hair, r.width, hair), dot);
        GUI.DrawTexture(new Rect(r.x, r.y, hair, r.height), dot);
        GUI.DrawTexture(new Rect(r.x + r.width - hair, r.y, hair, r.height), dot);
        GUI.color = Color.white;

        float tx = Mathf.Round(r.x + (r.width - BitmapFont.Measure(text, f)) * 0.5f);
        float ty = Mathf.Round(r.y + (r.height - f * 12f) * 0.5f);
        Color textColor = enabled
            ? new Color(1f, 238f / 255f, 180f / 255f, 1f)
            : new Color(120f / 255f, 120f / 255f, 126f / 255f, 1f);
        BitmapFont.DrawOutline(tx, ty, text, textColor, f);
    }
}
}
