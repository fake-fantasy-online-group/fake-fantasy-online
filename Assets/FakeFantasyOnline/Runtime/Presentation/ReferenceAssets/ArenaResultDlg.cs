using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 ArenaResultDlg (竞技场单局结算窗口), recovered from the native 15-method surface
/// (tokens 0x06000069..0x06000077, including the two nested payload types ArenaResultDlg+Row and
/// ArenaResultDlg+Data). Evidence:
/// docs/contracts/VERSION_UPGRADE_1_5_8_ARENA_RESULT_DLG_TRANSLATION_ARM64_V1.json and
/// docs/contracts/VERSION_UPGRADE_1_5_8_ARENA_RESULT_DLG_VERIFICATION_X86_V1.json.
/// A centered 460x(n*18+176) panel: place line (第 X 名 / 共 Y 人, or the 吃鸡 headline at rank 1),
/// the personal 杀/伤害/承受/开箱/存活 summary, one scoreboard row per player, then the 本局天赋,
/// 奖励, 积分, 生涯, 赛季 lines, the 继  续 button and the "({0}s 后自动关闭)" countdown.
/// The window closes itself AutoCloseSec seconds after <see cref="Show"/> (Update does it, not a
/// callback), is closed by the button and by UiEsc.CloseTop, and suppresses world clicks while open.
/// </summary>
public sealed class ArenaResultDlg : MonoBehaviour
{
    /// <summary>One scoreboard line of a single arena match.</summary>
    public class Row
    {
        public string name;
        public int rank;
        public int kills;
        public int damage;
        public int taken;
        public int surviveSec;
        public bool isSelf;

        /// <summary>Official .ctor only zeroes-inits name; every score field stays 0.</summary>
        public Row()
        {
            name = "";
        }
    }

    /// <summary>The whole settlement payload carried by opcode 0x8063.</summary>
    public class Data
    {
        public List<Row> rows;
        public int myRank;
        public int players;
        public int kills;
        public int damage;
        public int taken;
        public int boxes;
        public int surviveSec;
        public string build;
        public int rewardExp;
        public long rewardCoin;
        public bool firstWin;
        public int pointsGain;
        public long pointsTotal;
        public string title;
        public long toNext;
        public int streak;
        public int bestStreak;
        public int matches;
        public int wins;
        public int seasonId;
        public int seasonRank;
        public long seasonPoints;
        public int seasonDaysLeft;

        /// <summary>Official .ctor allocates rows and zero-inits both string fields.</summary>
        public Data()
        {
            rows = new List<Row>();
            build = "";
            title = "";
        }
    }

    public static ArenaResultDlg I;

    private static Data _d;

    private static float _closeAt;

    public const float AutoCloseSec = 16f;

    private const float PW = 460f;
    private const float HEAD = 46f;
    private const float ROWH = 18f;
    private const float GAP = 10f;

    private const float RowColRank = 14f;
    private const float RowColName = 38f;
    private const float RowColKills = 160f;
    private const float RowColDamage = 210f;
    private const float RowColSurvive = 340f;

    /// <summary>Official Showing: true while a settlement payload is attached to the singleton.</summary>
    public static bool Showing
    {
        get
        {
            return _d != null;
        }
    }

    private static float S
    {
        get
        {
            return UIScale.Factor;
        }
    }

    /// <summary>Font weight actually used for text, floor(scale) but never below 1.</summary>
    private static float Font
    {
        get
        {
            float f = Mathf.Floor(UIScale.Factor);
            return f < 1f ? 1f : f;
        }
    }

    /// <summary>Lazy singleton creation; the official body creates a named GameObject and adds this
    /// component to it, then keeps it across scene loads.</summary>
    public static void Ensure()
    {
        if (I == null)
        {
            GameObject go = new GameObject("ArenaResultDlg");
            I = go.AddComponent<ArenaResultDlg>();
            Object.DontDestroyOnLoad(go);
        }
    }

    /// <summary>Attach a settlement payload and start the official auto-close countdown.</summary>
    public static void Show(Data d)
    {
        Ensure();
        _d = d;
        _closeAt = Time.realtimeSinceStartup + AutoCloseSec;
    }

    /// <summary>Detach the payload. Shared by the 继  续 button, Update and UiEsc.CloseTop.</summary>
    public static void Close()
    {
        _d = null;
    }

    /// <summary>PlayerController.OverAnyUI hook: suppress world clicks over the settlement panel.
    /// Uses the same unsnapped panel rectangle OnGUI draws into.</summary>
    public static bool MouseOver()
    {
        if (_d == null)
        {
            return false;
        }
        float s = S;
        int count = _d.rows.Count;
        if (count < 2)
        {
            count = 1;
        }
        float w = s * PW;
        float h = s * (count * ROWH + HEAD + GAP + 84f + GAP + 26f);
        int screenW = Screen.width;
        int screenH = Screen.height;
        float top = (screenH - h) * 0.5f;
        float y = s * 16f > top ? s * 16f : top;
        float x = (screenW - w) * 0.5f;
        float mx = Input.mousePosition.x;
        float my = Screen.height - Input.mousePosition.y;
        return mx >= x && mx < x + w && my >= y && my < y + h;
    }

    private static Rect Snap(Rect r)
    {
        return UIScale.Snap(r);
    }

    /// <summary>BitmapFont.DrawOutline with the official rounding of the draw position.</summary>
    private static void TX(float x, float y, string t, Color c, float sc)
    {
        BitmapFont.DrawOutline(Mathf.Round(x), Mathf.Round(y), t, c, sc);
    }

    /// <summary>
    /// Coin formatting with the official 金/银/铜 units: 1 金 = 1,000,000 and 1 银 = 1,000 base units.
    /// Every non-empty unit is emitted with its suffix; a zero remainder is dropped only when a
    /// larger unit was already appended.
    /// </summary>
    private static string Money(long c)
    {
        StringBuilder sb = new StringBuilder();
        if (c > 999999L)
        {
            sb.Append(c / 1000000L);
            sb.Append('\u91d1');
        }
        if (c % 1000000L > 999L)
        {
            sb.Append(c % 1000000L / 1000L);
            sb.Append('\u94f6');
        }
        if (c % 1000L < 1L)
        {
            if (sb.Length != 0)
            {
                return sb.ToString();
            }
        }
        sb.Append(c % 1000L);
        sb.Append('\u94dc');
        return sb.ToString();
    }

    private void Update()
    {
        if (_d != null)
        {
            if (Time.realtimeSinceStartup >= _closeAt)
            {
                _d = null;
            }
        }
    }

    private void OnGUI()
    {
        Data d = _d;
        if (d == null)
        {
            return;
        }

        float s = S;
        float font = Font;
        Texture2D dot = ZoneOverlay.Dot;
        Color restore = GUI.color;

        int count = d.rows.Count;
        if (count < 2)
        {
            count = 1;
        }
        float w = s * PW;
        float h = s * (count * ROWH + HEAD + GAP + 84f + GAP + 26f);
        Rect win = Snap(new Rect((Screen.width - w) * 0.5f, Mathf.Max(s * 16f, (Screen.height - h) * 0.5f), w, h));
        float x0 = win.x;
        float y0 = win.y;
        float pw = win.width;
        float ph = win.height;

        GUI.color = new Color(0.1f, 0.08f, 0.14f, 0.98f);
        GUI.DrawTexture(win, dot);
        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.9f);
        GUI.DrawTexture(Snap(new Rect(x0, y0, pw, font)), dot);
        GUI.DrawTexture(Snap(new Rect(x0, y0 + ph - font, pw, font)), dot);
        GUI.DrawTexture(Snap(new Rect(x0, y0, font, ph)), dot);
        GUI.DrawTexture(Snap(new Rect(x0 + pw - font, y0, font, ph)), dot);
        GUI.color = Color.white;

        bool won = d.myRank == 1;
        string place = won
            ? "\u2605 \u5927\u5409\u5927\u5229 \u00b7 \u5403\u9e21\uff01"
            : string.Format("\u7b2c {0} \u540d / \u5171 {1} \u4eba", d.myRank, d.players);
        uint placePacked = won ? 0xff8ae2ffu : 0xffeee2e2u;
        Color placeColor = new Color(
            (placePacked & 0xff) / 255f,
            226f / 255f,
            ((placePacked >> 16) & 0xfe) / 255f,
            1f);
        float col = x0 + 14f * s;
        TX(Mathf.Round(col), Mathf.Round(y0 + 10f * s), place, placeColor, font);

        TX(Mathf.Round(col), Mathf.Round(y0 + 27f * s),
            string.Format(
                "{0} \u6740 \u00b7 \u4f24\u5bb3 {1} \u00b7 \u627f\u53d7 {2} \u00b7 \u5f00\u7bb1 {3} \u00b7 \u5b58\u6d3b {4} \u79d2",
                d.kills, d.damage, d.taken, d.boxes, d.surviveSec),
            new Color(170f / 255f, 170f / 255f, 182f / 255f, 1f), font);

        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.3f);
        GUI.DrawTexture(Snap(new Rect(x0 + GAP * s, y0 + 42f * s, pw - 20f * s, font)), dot);
        GUI.color = Color.white;

        float y = y0 + 46f * s;
        float rowH = 18f * s;
        float rowX2 = Mathf.Round(x0 + RowColName * s);
        float rowX3 = Mathf.Round(x0 + RowColKills * s);
        float rowX4 = Mathf.Round(x0 + RowColDamage * s);
        float rowX5 = Mathf.Round(x0 + RowColSurvive * s);
        Color killsColor = new Color(190f / 255f, 150f / 255f, 150f / 255f, 1f);
        Color damageColor = new Color(170f / 255f, 170f / 255f, 182f / 255f, 1f);
        Color surviveColor = new Color(150f / 255f, 150f / 255f, 162f / 255f, 1f);
        for (int i = 0; i < d.rows.Count; i++)
        {
            Row r = d.rows[i];
            if (r.isSelf)
            {
                GUI.color = new Color(0.24f, 0.2f, 0.1f, 0.85f);
                GUI.DrawTexture(Snap(new Rect(x0 + font, y - 2f * s, pw - font - font, rowH)), dot);
                GUI.color = Color.white;
            }

            uint packed = 0xffd6c8c8u;
            if (r.rank == 1)
            {
                packed = 0xff8ae2ffu;
            }
            else if (r.isSelf)
            {
                packed = 0xffb4eeffu;
            }
            Color rowColor = new Color(
                (packed & 0xff) / 255f,
                ((packed >> 8) & 0xff) / 255f,
                ((packed >> 16) & 0xff) / 255f,
                1f);

            TX(Mathf.Round(col), y, string.Format("{0}.", r.rank), rowColor, font);
            TX(rowX2, y, r.name, rowColor, font);
            TX(rowX3, y, string.Format("{0} \u6740", r.kills), killsColor, font);
            TX(rowX4, y, string.Format("\u4f24\u5bb3 {0}", r.damage), damageColor, font);
            TX(rowX5, y, string.Format("\u5b58\u6d3b {0}s", r.surviveSec), surviveColor, font);
            y += rowH;
        }

        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.3f);
        GUI.DrawTexture(Snap(new Rect(x0 + GAP * s, y + 6f * s, pw - 20f * s, font)), dot);
        GUI.color = Color.white;

        float infoY = y + GAP * s;
        if (d.build != null && d.build.Length > 0)
        {
            TX(Mathf.Round(col), infoY, string.Concat("\u672c\u5c40\u5929\u8d4b  ", d.build),
                new Color(186f / 255f, 186f / 255f, 202f / 255f, 1f), font);
            infoY += 14f * s;
        }

        if (d.rewardExp >= 1 || d.rewardCoin > 0L)
        {
            string rewardTail = d.rewardExp >= 1 ? string.Format("\u7ecf\u9a8c {0}", d.rewardExp) : "";
            string rewardSep = d.rewardCoin > 0L ? "  \u00b7  " : "";
            string rewardWhole = d.rewardExp >= 1 ? rewardSep : "";
            string rewardCoin = d.rewardCoin > 0L ? Money(d.rewardCoin) : "";
            string rewardLine = string.Concat("\u5956\u52b1  ", rewardTail, rewardWhole, rewardCoin);
            TX(Mathf.Round(col), infoY, rewardLine, new Color(150f / 255f, 220f / 255f, 150f / 255f, 1f), font);
            if (d.firstWin)
            {
                float offset = GAP * s + Mathf.Round(col) + BitmapFont.Measure(rewardLine, font);
                TX(offset, infoY, "\u2605\u4eca\u65e5\u9996\u80dc",
                    new Color(255f / 255f, 200f / 255f, 90f / 255f, 1f), font);
            }
            infoY += 14f * s;
        }

        string promotion = "   \u5df2\u6ee1\u6bb5";
        if (d.toNext > 0L)
        {
            promotion = string.Format("   \u518d {0} \u5206\u5347\u6bb5", d.toNext);
        }
        TX(Mathf.Round(col), infoY,
            string.Concat(
                string.Format("\u79ef\u5206  +{0}  \u2192  {1}  [{2}]", d.pointsGain, d.pointsTotal, d.title),
                promotion),
            new Color(210f / 255f, 190f / 255f, 255f / 255f, 1f), font);

        string streakTail = "";
        if (d.streak > 1)
        {
            streakTail = string.Format("   \u2605{0} \u8fde\u80dc(\u6700\u9ad8 {1})", d.streak, d.bestStreak);
        }
        TX(Mathf.Round(col), infoY + 14f * s,
            string.Concat(string.Format("\u751f\u6daf  {0} \u573a {1} \u80dc", d.matches, d.wins), streakTail),
            new Color(186f / 255f, 186f / 255f, 202f / 255f, 1f), font);

        string seasonRankTail = "";
        if (d.seasonRank >= 1)
        {
            seasonRankTail = string.Format("  \u00b7  \u5f53\u524d\u7b2c {0} \u540d", d.seasonRank);
        }
        TX(Mathf.Round(col), infoY + 28f * s,
            string.Concat(
                string.Format("\u7b2c {0} \u8d5b\u5b63  {1} \u5206", d.seasonId, d.seasonPoints),
                seasonRankTail,
                string.Format("  \u00b7  \u8fd8\u5269 {0} \u5929", d.seasonDaysLeft)),
            new Color(150f / 255f, 190f / 255f, 220f / 255f, 1f), font);

        string buttonLabel = "\u7ee7  \u7eed";
        float buttonH = 20f * s;
        float buttonW = 40f * s + BitmapFont.Measure(buttonLabel, font);
        float buttonX = x0 + (pw - buttonW) * 0.5f;
        float buttonY = infoY + 28f * s + 14f * s + 6f * s;
        Rect buttonRect = new Rect(buttonX, buttonY, buttonW, buttonH);
        bool hover = Input.mousePosition.x >= buttonX
            && Input.mousePosition.x < buttonX + buttonW
            && Screen.height - Input.mousePosition.y >= buttonY
            && Screen.height - Input.mousePosition.y < buttonY + buttonH;

        GUI.color = hover
            ? new Color(0.34f, 0.29f, 0.16f, 0.98f)
            : new Color(0.2f, 0.17f, 0.26f, 0.95f);
        GUI.DrawTexture(Snap(buttonRect), dot);
        GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, hover ? 0.9f : 0.5f);
        GUI.DrawTexture(Snap(new Rect(buttonX, buttonY, buttonW, font)), dot);
        GUI.DrawTexture(Snap(new Rect(buttonX, buttonY + buttonH - font, buttonW, font)), dot);
        GUI.DrawTexture(Snap(new Rect(buttonX, buttonY, font, buttonH)), dot);
        GUI.DrawTexture(Snap(new Rect(buttonX + buttonW - font, buttonY, font, buttonH)), dot);
        GUI.color = Color.white;

        uint buttonPacked = hover ? 0xffb4eeffu : 0xffdaceceu;
        Color buttonColor = new Color(
            (buttonPacked & 0xff) / 255f,
            ((buttonPacked >> 8) & 0xff) / 255f,
            ((buttonPacked >> 16) & 0xff) / 255f,
            1f);
        float labelY = buttonY + 3f * s;
        TX(buttonX + (buttonW - BitmapFont.Measure(buttonLabel, font)) * 0.5f, labelY, buttonLabel, buttonColor, font);

        float remain = _closeAt - Time.realtimeSinceStartup;
        int seconds = Mathf.CeilToInt(remain < 0f ? 0f : remain);
        TX(buttonX + buttonW + GAP * s, labelY,
            string.Format("({0}s \u540e\u81ea\u52a8\u5173\u95ed)", seconds),
            new Color(120f / 255f, 120f / 255f, 132f / 255f, 1f), font);

        if (Event.current != null && Event.current.type == EventType.MouseDown)
        {
            if (Event.current.button != 0)
            {
                GUI.color = restore;
                return;
            }
            if (buttonRect.Contains(Event.current.mousePosition))
            {
                Close();
                Event.current.Use();
            }
            else if (win.Contains(Event.current.mousePosition))
            {
                Event.current.Use();
            }
        }

        GUI.color = restore;
    }
}
}
