using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 ArenaEscDlg (自定义地图竞技场退出小窗口), recovered from the complete native
/// 18-member surface: tokens 0x06000057..0x06000068, type 0x0200000c. Dual-architecture evidence
/// was re-exported for this recovery into
/// Build/ReferenceReverseGhidra-1.5.8/queries/arena-esc-20260929/{arm64,x86} from the frozen atlas
/// Build/ReferenceReverseIndex-1.5.8/index.sqlite3.
/// A centered 320x242 (scaled) panel: 吊村大乱斗 title with the 本局进行中/本局未开始 status on
/// the right, the 毒圈 phase line, the 场上存活 / 天赋卡 / 本局阵亡 counters, the optional
/// 圈外 bleeding warning and 看全场天赋 tip, the optional wrapped surrender-block reason, then the
/// 继续游戏 and 认输 buttons.
/// Entry: only UIManager.Update (0x06000fa8) opens it - when UiEsc.CloseTop (0x06000fca) closed
/// nothing and ShouldTakeOver is true it calls Toggle(), otherwise the ordinary SysOptionDlg.Toggle
/// keeps the key, so the ordinary menu is never swallowed. UiEsc.CloseTop closes it at its own slot
/// (after ArenaResultDlg, before DialogUI) and PlayerController.OverAnyUI (0x060009ff, arm64
/// 0x2ac7c0) hit-tests MouseOver at its slot 14.
/// </summary>
public partial class ArenaEscDlg : MonoBehaviour
{
    public static ArenaEscDlg I;

    private static bool _visible;

    private const float PW = 320f;

    private const float PH = 242f;

    /// <summary>0x06000057 get_Visible: the static bool at ArenaEscDlg + 8.</summary>
    public static bool Visible
    {
        get
        {
            return _visible;
        }
    }

    /// <summary>1.6.0 0x0600005c get_ShouldTakeOver: custom maps except the duel arena.</summary>
    public static bool ShouldTakeOver
    {
        get
        {
            return CustomMaps.IsCustom(GameBootstrap.CurMapId) &&
                   !CustomMaps.IsDuelArena(GameBootstrap.CurMapId);
        }
    }

    private static float S
    {
        get
        {
            return UIScale.Factor;
        }
    }

    /// <summary>0x0600005e get_Font: floor(scale) but never below 1.</summary>
    private static float Font
    {
        get
        {
            float f = Mathf.Floor(UIScale.Factor);
            return f < 1f ? 1f : f;
        }
    }

    /// <summary>0x06000058 Ensure: lazily creates the named host object and keeps it across loads.</summary>
    public static void Ensure()
    {
        if (!(I == null))
        {
            return;
        }
        GameObject go = new GameObject("ArenaEscDlg");
        I = go.AddComponent<ArenaEscDlg>();
        Object.DontDestroyOnLoad(go);
    }

    /// <summary>0x06000059 Toggle: Ensure then flip the static visibility byte.</summary>
    public static void Toggle()
    {
        Ensure();
        _visible = !_visible;
    }

    /// <summary>0x0600005a Close: shared by UiEsc.CloseTop and the 继续游戏 button.</summary>
    public static void Close()
    {
        _visible = false;
    }

    /// <summary>
    /// 0x0600005c MouseOver: PlayerController.OverAnyUI (0x060009ff) slot 14. The frozen atlas gives
    /// this member exactly one caller - that edge is arm64 0x2ac9d0, between ArenaDraftDlg (12,
    /// 0x2ac9a0) / ArenaDeathDlg (13, 0x2ac9c4) and the ArenaResultDlg tail call (15, 0x2ac9fc).
    /// Uses the same unsnapped panel rectangle OnGUI draws into; the two buttons sit inside it.
    /// </summary>
    public static bool MouseOver()
    {
        if (!_visible)
        {
            return false;
        }
        float mx = Input.mousePosition.x;
        int screenH = Screen.height;
        float my = screenH - Input.mousePosition.y;
        Rect p = PanelRect();
        return mx >= p.x && mx < p.x + p.width && my >= p.y && my < p.y + p.height;
    }

    /// <summary>0x0600005f Snap.</summary>
    private static Rect Snap(Rect r)
    {
        return UIScale.Snap(r);
    }

    /// <summary>0x06000060 TX: BitmapFont.DrawOutline with the official rounding of the position.</summary>
    private static void TX(float x, float y, string t, Color c, float sc)
    {
        BitmapFont.DrawOutline(Mathf.Round(x), Mathf.Round(y), t, c, sc);
    }

    /// <summary>0x06000061 PanelRect: the centered, snapped 320x242 panel.</summary>
    private static Rect PanelRect()
    {
        float s = UIScale.Factor;
        float w = s * PW;
        float h = s * PH;
        return Snap(new Rect((Screen.width - w) * 0.5f, (Screen.height - h) * 0.5f, w, h));
    }

    /// <summary>0x06000062 BtnBack: 继续游戏, inside the lower part of the panel.</summary>
    private static Rect BtnBack()
    {
        Rect p = PanelRect();
        float s = UIScale.Factor;
        return Snap(new Rect(p.x + s * 16f, p.y + p.height - s * 76f, p.width - s * 32f, s * 26f));
    }

    /// <summary>0x06000063 BtnGiveUp: 认输, directly below 继续游戏.</summary>
    private static Rect BtnGiveUp()
    {
        Rect p = PanelRect();
        float s = UIScale.Factor;
        return Snap(new Rect(p.x + s * 16f, p.y + p.height - s * 42f, p.width - s * 32f, s * 26f));
    }

    /// <summary>
    /// 0x06000066 DrawBtn: fill, border and centered label of one button. Disabled buttons skip the
    /// hover test and keep their own fill, border and label colors.
    /// </summary>
    private static void DrawBtn(Rect r, string text, bool enabled, float f, float hair, Texture2D dot)
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

        Color border;
        if (!enabled)
        {
            border = new Color(0.4f, 0.4f, 0.42f, 0.8f);
        }
        else
        {
            border = hover
                ? new Color(1f, 0.88f, 0.5f, 1f)
                : new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.8f);
        }
        GUI.color = border;
        GUI.DrawTexture(new Rect(r.x, r.y, r.width, hair), dot);
        GUI.DrawTexture(new Rect(r.x, r.y + r.height - hair, r.width, hair), dot);
        GUI.DrawTexture(new Rect(r.x, r.y, hair, r.height), dot);
        GUI.DrawTexture(new Rect(r.x + r.width - hair, r.y, hair, r.height), dot);
        GUI.color = Color.white;

        Color label = enabled
            ? new Color(1f, 238f / 255f, 180f / 255f, 1f)
            : new Color(120f / 255f, 120f / 255f, 126f / 255f, 1f);
        float lx = Mathf.Round(r.x + (r.width - BitmapFont.Measure(text, f)) * 0.5f);
        float ly = Mathf.Round(r.y + (r.height - f * 12f) * 0.5f);
        BitmapFont.DrawOutline(lx, ly, text, label, f);
    }

    /// <summary>
    /// 0x06000065 Wrap: draws a word-wrapped block and returns the y below the last line. A single
    /// character is emitted even when it alone is wider than <paramref name="w"/>.
    /// </summary>
    private static float Wrap(float x, float y, float w, string text, Color col, float f)
    {
        if (string.IsNullOrEmpty(text))
        {
            return y;
        }
        float lineH = Mathf.Round(f * 12f + 3f);
        float px = Mathf.Round(x);
        int i = 0;
        int len = 1;
        while (i < text.Length)
        {
            string sub = text.Substring(i, len);
            bool fits = BitmapFont.Measure(sub, f) <= w;
            if (fits)
            {
                len++;
                if (i + len <= text.Length)
                {
                    continue;
                }
            }
            len--;
            if (len < 2)
            {
                len = 1;
            }
            sub = text.Substring(i, len);
            BitmapFont.DrawOutline(px, Mathf.Round(y), sub, col, f);
            y += lineH;
            i += len;
            len = 1;
        }
        return y;
    }

    /// <summary>0x06000068: the OnGUI local function that formats a mm:ss countdown.</summary>
    private static string MS(int t)
    {
        return string.Format("{0:00}:{1:00}", t / 60, t % 60);
    }

    /// <summary>
    /// 0x06000064 OnGUI, the single draw entry. Ensure puts this component on its own
    /// DontDestroyOnLoad GameObject, so Unity dispatches OnGUI itself; the first-world owner never
    /// calls it - ArenaEscDlg appears there only as Visible / ShouldTakeOver / Toggle / Close /
    /// MouseOver, and it is not one of the windows ClientRootView.OnGUI drives through
    /// ReferenceFirstWorldWindowOwner.DrawHud (those must not declare OnGUI). No second Input read
    /// or second GUI pass was added: the pointer suppression is the separate MouseOver slot 14.
    /// </summary>
    private void OnGUI()
    {
        if (!_visible)
        {
            return;
        }
        if (!ShouldTakeOver)
        {
            _visible = false;
            return;
        }

        float s = S;
        float font = Font;
        float hair = Mathf.Max(1f, Mathf.Floor(s));
        Texture2D dot = ZoneOverlay.Dot;
        Color restore = GUI.color;

        Rect panel = PanelRect();
        float x = panel.x;
        float y = panel.y;
        float w = panel.width;
        float h = panel.height;

        GUI.color = new Color(0.1f, 0.08f, 0.14f, 0.97f);
        GUI.DrawTexture(panel, dot);
        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.85f);
        GUI.DrawTexture(Snap(new Rect(x, y, w, hair)), dot);
        GUI.DrawTexture(Snap(new Rect(x, y + h - hair, w, hair)), dot);
        GUI.DrawTexture(Snap(new Rect(x, y, hair, h)), dot);
        GUI.DrawTexture(Snap(new Rect(x + w - hair, y, hair, h)), dot);
        GUI.color = Color.white;

        float col = x + s * 12f;
        TX(col, y + s * 10f, "吊村大乱斗", new Color(1f, 226f / 255f, 138f / 255f, 1f), font);

        string state = ZoneOverlay.Showing ? "本局进行中" : "本局未开始";
        TX(x + w - BitmapFont.Measure(state, font) - s * 12f, y + s * 10f, state,
            new Color(160f / 255f, 160f / 255f, 172f / 255f, 1f), font);

        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.3f);
        GUI.DrawTexture(Snap(new Rect(col, y + s * 28f, w - s * 24f, hair)), dot);
        GUI.color = Color.white;

        int sec = ZoneOverlay.PhaseSecNow;
        string prefix = string.Format("毒圈  {0}/{1}   ", ZoneOverlay.ring, ZoneOverlay.rings);
        string tail;
        if (sec < 0)
        {
            tail = "最后一圈";
        }
        else if (ZoneOverlay.phase == 1)
        {
            tail = string.Concat("收缩中 ", MS(sec));
        }
        else
        {
            tail = string.Concat(MS(sec), " 后收缩");
        }
        TX(col, y + s * 38f,
            string.Concat(prefix, tail),
            new Color(215f / 255f, 200f / 255f, 235f / 255f, 1f), font);

        Color infoColor = new Color(206f / 255f, 206f / 255f, 218f / 255f, 1f);
        float ly = y + s * 38f + s * 16f;
        TX(col, ly, string.Format("场上存活  {0} 人", ZoneOverlay.alive), infoColor, font);
        ly += s * 16f;
        TX(col, ly, string.Format("天赋卡    还剩 {0} 张", ZoneOverlay.cardsLeft), infoColor, font);
        ly += s * 16f;
        TX(col, ly, string.Format("本局阵亡  {0} 次", ZoneOverlay.deaths), infoColor, font);
        ly += s * 16f;
        if (ZoneOverlay.outside)
        {
            TX(col, ly, "★你在圈外, 正在失血", new Color(1f, 110f / 255f, 110f / 255f, 1f), font);
            ly += s * 16f;
        }
        TX(col, ly, "按住 U    看全场天赋", new Color(150f / 255f, 150f / 255f, 162f / 255f, 1f), font);

        bool giveUp = ZoneOverlay.canSurrender;
        if (!giveUp)
        {
            if (ZoneOverlay.Showing)
            {
                if (!string.IsNullOrEmpty(ZoneOverlay.blockWhy))
                {
                    Wrap(col, ly + s * 20f, w - s * 24f, ZoneOverlay.blockWhy,
                        new Color(210f / 255f, 165f / 255f, 110f / 255f, 1f), font);
                }
            }
            else
            {
                giveUp = true;
            }
        }

        DrawBtn(BtnBack(), "继续游戏", true, font, hair, dot);
        DrawBtn(BtnGiveUp(), "认输", giveUp, font, hair, dot);

        GUI.color = restore;

        if (GUI.Button(BtnBack(), GUIContent.none, GUIStyle.none))
        {
            _visible = false;
        }
        if (GUI.Button(BtnGiveUp(), GUIContent.none, GUIStyle.none))
        {
            if (!giveUp)
            {
                ChatUI.Add("系统", ZoneOverlay.blockWhy,
                    new Color(1f, 160f / 255f, 90f / 255f, 1f), "");
                return;
            }
            NetClient.I?.ArenaReviveForIndependentRuntime(true);
            _visible = false;
        }
    }
}
}
