using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 ArenaDraftDlg (竞技场选将窗口): the real static fields and their construction
/// values, the .ctor / .cctor / Card .ctor, Show, Close, Visible, SecLeft, MouseOver, the layout
/// helpers, the two commands and the single OnGUI that draws the window.
/// Evidence: the frozen exports in
/// Build/ReferenceReverseGhidra-1.5.8/queries/wbb029-arenadraft-arm64 and
/// .../wbb029-arenadraft-x86_64, the atlas members 0x06000041..0x06000056 (ArenaDraftDlg is
/// MonoBehaviour, atlas type token 0x0200000a), the atlas fields I / _visible / _round / _total /
/// _sec / _rerollFree / _rerollCost / _recvTime / _cards at static offsets 0 / 8 / 12 / 16 / 20 /
/// 24 / 32 / 40 / 48, the metadata string literal 11390 ("ArenaDraftDlg") used as the GameObject
/// name, and the kind5 literals 16994 / 16666 / 16665 / 18191 / 18190 / 18189 / 18188 / 17774 /
/// 17773 (all decode_state exact).
///
/// The window is opened and closed only by the official receive branch (NetClient.Dispatch case
/// 0x8060 -> arm64 0x24d2cc): a leading zero flag tail-calls Close, every other value tail-calls
/// Show. OnGUI (0x06000050) is the only drawing entry: it is an instance method on the
/// MonoBehaviour Show creates, so Unity calls it once per frame and no host calls it by hand.
/// Pick (0x06000052) and Reroll (0x06000051) send the two official commands
/// NetClient.ArenaPick (0x06000744, opcode 0x1083) and NetClient.ArenaReroll (0x06000745, opcode
/// 0x1086); neither one copies the live card list and neither one adds a check the native body
/// does not have.
/// </summary>
public partial class ArenaDraftDlg : MonoBehaviour
{
    /// <summary>
    /// Official const fields CW / CH / GAP / RH (0x04000037..0x0400003a, flags 0x8051). The atlas
    /// stores no constant_json for them, so the values below are the design-pixel immediates the
    /// frozen bodies inline: CardRect widths are 210 (0x0600004a), heights 250, the card stride is
    /// 228 = 210 + 18, and RerollRect's row height is 26. The remaining layout offsets (the panel's
    /// 666x330, the card row's 38 and the reroll row's 296) stay inline literals because no
    /// immediate proves which const they came from.
    /// </summary>
    private const float CW = 210f;
    private const float CH = 250f;
    private const float GAP = 18f;
    private const float RH = 26f;

    /// <summary>
    /// Official nested type ArenaDraftDlg+Card (atlas type token 0x0200000b, parent System.Object):
    /// skillId at +0x10, lv at +0x14, name at +0x18, desc at +0x20. Its only method is the
    /// parameterless .ctor (0x06000056), which stores the empty literal (metadata usage ordinal
    /// 10335) into name and desc before the base Object..ctor and leaves both counters at 0.
    /// </summary>
    public class Card
    {
        public int skillId;
        public int lv;
        public string name = "";
        public string desc = "";
    }

    /// <summary>Official public static field I (offset +0x0); created lazily by <see cref="Show"/>.</summary>
    public static ArenaDraftDlg I;

    private static bool _visible;
    private static int _round;
    private static int _total;
    private static int _sec;
    private static int _rerollFree;
    private static long _rerollCost;
    private static float _recvTime;
    /// <summary>
    /// Official .cctor (0x06000055): an empty List&lt;Card&gt; is the only static initialization the
    /// native body performs, so I stays null until Show runs.
    /// </summary>
    private static List<Card> _cards = new List<Card>();

    /// <summary>Official get_Visible (0x06000041): the static _visible byte, no caller in 1.5.8.</summary>
    public static bool Visible
    {
        get
        {
            return _visible;
        }
    }

    /// <summary>
    /// Official get_SecLeft (0x06000042): max(0, _sec - Mathf.FloorToInt(Time.realtimeSinceStartup -
    /// _recvTime)). The seconds still left before the server decides the draft round.
    /// </summary>
    public static int SecLeft
    {
        get
        {
            return Mathf.Max(0, _sec - Mathf.FloorToInt(Time.realtimeSinceStartup - _recvTime));
        }
    }

    /// <summary>
    /// Official get_S (0x06000046): UIScale.Factor. No caller in 1.5.8 - IL2CPP inlines the getter
    /// into PanelRect, which reads UIScale.Factor (0x06000fb6) directly on both architectures.
    /// </summary>
    private static float S
    {
        get
        {
            return UIScale.Factor;
        }
    }

    /// <summary>
    /// Official Show (0x06000043, six parameters). Ensures the singleton exactly like the native
    /// body - new GameObject("ArenaDraftDlg") + AddComponent + DontDestroyOnLoad only while I is
    /// null (Unity Object == null test) - then stores every argument in the official order
    /// (_round, _total, _sec, _cards, _rerollFree, _rerollCost, _recvTime =
    /// Time.realtimeSinceStartup). A null card list is replaced by a new empty one, and the window
    /// only becomes visible when that list carries at least one card.
    /// </summary>
    public static void Show(int round, int total, int sec, List<Card> cards, int rerollFree = 0,
        long rerollCost = -1)
    {
        if (I == null)
        {
            GameObject go = new GameObject("ArenaDraftDlg");
            I = go.AddComponent<ArenaDraftDlg>();
            Object.DontDestroyOnLoad(go);
        }
        _round = round;
        _total = total;
        _sec = sec;
        _cards = cards ?? new List<Card>();
        _rerollFree = rerollFree;
        _rerollCost = rerollCost;
        _recvTime = Time.realtimeSinceStartup;
        _visible = _cards.Count > 0;
    }

    /// <summary>
    /// Official Close (0x06000044): _visible = false and then _cards.Clear() - the list instance is
    /// kept, its size goes back to 0, its version advances and the used prefix is wiped, which is
    /// what the native List.Clear body does on both architectures.
    /// </summary>
    public static void Close()
    {
        _visible = false;
        _cards.Clear();
    }

    /// <summary>
    /// Official MouseOver (0x06000045), the PlayerController.OverAnyUI slot 12 hook. False while
    /// hidden; otherwise the panel rectangle test against Screen.height - Input.mousePosition.y.
    /// </summary>
    public static bool MouseOver()
    {
        if (!_visible)
        {
            return false;
        }
        float mx = Input.mousePosition.x;
        int screenHeight = Screen.height;
        float mouseY = Input.mousePosition.y;
        Rect r = PanelRect();
        float my = screenHeight - mouseY;
        if (mx < r.x)
        {
            return false;
        }
        if (r.x + r.width <= mx)
        {
            return false;
        }
        if (my < r.y)
        {
            return false;
        }
        return my < r.y + r.height;
    }

    /// <summary>
    /// Official PanelRect (0x06000049): a 666x330 design-pixel panel, horizontally centered and
    /// placed at 24% of the screen height. Both architectures inline the four constants
    /// (arm64 0x44268000 = 666f, 0x3e75c28f = 0.24f, 0x43a50000 = 330f, 0.5f) and call
    /// UIScale.Snap (0x06000fb7).
    /// </summary>
    private static Rect PanelRect()
    {
        float s = S;
        return UIScale.Snap(new Rect((Screen.width - s * 666f) * 0.5f, Screen.height * 0.24f,
            s * 666f, s * 330f));
    }

    /// <summary>
    /// Official CardRect (0x0600004a, arm64 0x21895c): a CW x CH card at the row offset 38, laid
    /// out with the stride CW + GAP = 228. Both architectures build the same four immediates
    /// (228 / 210 / 250 / 38) and end in UIScale.Snap (0x06000fb7).
    /// </summary>
    private static Rect CardRect(int i)
    {
        Rect panel = PanelRect();
        float s = S;
        return UIScale.Snap(new Rect(panel.x + (float)i * (CW + GAP) * s, panel.y + 38f * s,
            CW * s, CH * s));
    }

    /// <summary>
    /// Official RerollRect (0x06000047, arm64 0x2186ec): a CW x RH row centred in the panel at the
    /// row offset 296 - (panel.width - CW * s) * 0.5, which is the same thing the card stride
    /// produces. Ends in UIScale.Snap like CardRect.
    /// </summary>
    private static Rect RerollRect()
    {
        Rect panel = PanelRect();
        float s = S;
        return UIScale.Snap(new Rect(panel.x + (panel.width - CW * s) * 0.5f, panel.y + 296f * s,
            CW * s, RH * s));
    }

    /// <summary>
    /// Official Snap (0x0600004f, arm64 0x218e18): UIScale.Snap. The frozen OnGUI inlines it, so it
    /// has no managed caller in 1.5.8; the product body keeps calling it so the layout stays the
    /// official one.
    /// </summary>
    private static Rect Snap(Rect r)
    {
        return UIScale.Snap(r);
    }

    /// <summary>
    /// Official SnapR (0x0600004b, arm64 0x218874): the second UIScale.Snap wrapper, byte-identical
    /// to Snap (0x0600004f). Both exist in 1.5.8 and both are inlined away by IL2CPP.
    /// </summary>
    private static Rect SnapR(Rect r)
    {
        return UIScale.Snap(r);
    }

    /// <summary>
    /// Official get_FontBase (0x0600004c, arm64 0x218af8 / 208 bytes, x86_64 identical):
    /// max(1, floor(UIScale.Factor)) - retains the floating-point floor result.
    /// </summary>
    private static float FontBase
    {
        get
        {
            float f = Mathf.Floor(UIScale.Factor);
            return f < 1f ? 1f : f;
        }
    }

    /// <summary>
    /// Official get_FontName (0x0600004d, arm64 0x218bc8 / 280 bytes): the same
    /// max(1, floor(UIScale.Factor)) as get_FontBase. It is a separate member in 1.5.8 and OnGUI uses
    /// it only for the card name line.
    /// </summary>
    private static float FontName
    {
        get
        {
            float f = Mathf.Floor(UIScale.Factor);
            return f < 1f ? 1f : f;
        }
    }

    /// <summary>
    /// Official get_CanReroll (0x06000048): (_rerollCost &gt;&gt; 63) ^ 1, i.e. _rerollCost &gt;= 0.
    /// OnGUI inlines the same test, so it has no managed caller in 1.5.8.
    /// </summary>
    private static bool CanReroll
    {
        get
        {
            return ((ulong)_rerollCost >> 63) == 0;
        }
    }

    /// <summary>
    /// Official TX (0x0600004e, arm64 0x218ce0): BitmapFont.DrawOutline after rounding both
    /// coordinates, which is why every text in OnGUI lands on whole pixels.
    /// </summary>
    private static void TX(float x, float y, string text, Color col, float scale)
    {
        BitmapFont.DrawOutline(Mathf.Round(x), Mathf.Round(y), text, col, scale);
    }

    /// <summary>
    /// Official DrawWrapped (0x06000053, arm64 0x21b288 / 988 bytes): greedy word wrap measured
    /// with BitmapFont.Measure against the area width, line height round(s * 12 + 3), and one
    /// official colour (206, 206, 218) at full alpha for every line. It stops as soon as the next
    /// line would fall outside the area, and it always eats at least one character per line.
    /// </summary>
    private static void DrawWrapped(string text, Rect area, float s)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        Color col = new Color(206f / 255f, 206f / 255f, 218f / 255f, 1f);
        float lineH = Mathf.Round(s * 12f + 3f);
        float x = Mathf.Round(area.x);
        float y = Mathf.Round(area.y);
        int i = 0;
        while (i < text.Length)
        {
            if (!(y + lineH <= area.y + area.height))
            {
                return;
            }

            int n = 1;
            while (true)
            {
                float w = BitmapFont.Measure(text.Substring(i, n), s);
                if (!(w <= area.width))
                {
                    break;
                }
                n = n + 1;
                if (i + n > text.Length)
                {
                    break;
                }
            }
            n = n - 1;
            if (n < 2)
            {
                n = 1;
            }

            BitmapFont.DrawOutline(x, Mathf.Round(y), text.Substring(i, n), col, s);
            i = i + n;
            y = y + lineH;
        }
    }

    /// <summary>
    /// Official Reroll (0x06000051, arm64 0x21b790): visible and _rerollCost &gt;= 0 are the only
    /// gates, then NetClient.ArenaReroll (opcode 0x1086, no payload) and _rerollCost = -1 so the
    /// row disables itself in the same frame. The price and the free count stay server-authored.
    /// </summary>
    private static void Reroll()
    {
        if (!_visible)
        {
            return;
        }
        if (_rerollCost < 0)
        {
            return;
        }
        NetClient client = NetClient.I;
        if (client != null)
        {
            client.ArenaRerollForIndependentRuntime();
        }
        _rerollCost = -1;
    }

    /// <summary>
    /// Official Pick (historical 0x06000052): actual 1.6 checks i &gt;= 0, visible and i &lt; _cards.Count as the
    /// only gates (a null list is the official IL2CPP null raise, not a skipped send), then
    /// NetClient.ArenaPick (opcode 0x1083, one U8 holding the slot) and Close. The card list is
    /// checked through Count here and never copied.
    /// </summary>
    private static void Pick(int i)
    {
        if (i < 0)
        {
            return;
        }
        if (!_visible)
        {
            return;
        }
        if (i >= _cards.Count)
        {
            return;
        }
        NetClient client = NetClient.I;
        if (client != null)
        {
            client.ArenaPickForIndependentRuntime(i);
        }
        Close();
    }

    /// <summary>
    /// Official OnGUI (0x06000050, arm64 0x218e9c / 9196 bytes). One pass, in the official order:
    /// the shadow and the two rules, the title and the right-aligned countdown, the progress bar,
    /// then the card row - slot i while i &lt; 3 and i &lt; _cards.Count, otherwise the reroll row, the
    /// keyboard branch and the return. The reroll row is what ends the pass, so the keyboard is
    /// only read once, after every card button has already been hit-tested in this frame.
    /// </summary>
    private void OnGUI()
    {
        if (!_visible)
        {
            return;
        }
        // The official early-out reads _cards (static offset 0x30) and raises on a null receiver
        // instead of returning: arm64 0x219014 ldr x8,[x8,#0x30] / 0x219018 cbz x8 -> 0x21b284
        // bl 0x177a74 (the IL2CPP raise, no return), x86_64 0x2254eb mov rax,[rax+0x30] /
        // 0x2254ef test rax,rax / 0x2254f2 je 0x227f1d call 0x16c1b0 (the same helper). Only the
        // Count == 0 arm returns: arm64 0x21901c ldr w8,[x8,#0x18] / 0x219020 cbz -> 0x21b170,
        // x86_64 0x2254f8 cmp dword [rax+0x18],0 / 0x2254fc je -> 0x227e13 epilogue. So the null
        // test is dropped here and _cards.Count raises, which is the official behaviour.
        if (_cards.Count == 0)
        {
            return;
        }

        float s = S;
        Texture2D dot = ZoneOverlay.Dot;
        Color restore = GUI.color;
        Rect panel = PanelRect();
        int secLeft = SecLeft;
        float now = Time.realtimeSinceStartup;

        float px = panel.x;
        float py = panel.y;
        float pw = panel.width;
        float ph = panel.height;

        GUI.color = Color.white;
        ArtworkSkin.Panel(Snap(new Rect(px - 16f * s, py - 10f * s, pw + 32f * s, ph + 20f * s)), s);
        float ruleWidth = Mathf.Max(1f, Mathf.Floor(s));

        float font = FontBase;
        float titleY = py + 10f * s;
        TX(px + 2f * s, titleY, string.Format("乱斗天赋  第 {0}/{1} 次", _round, _total),
            new Color(183f / 255f, 155f / 255f, 98f / 255f, 1f), font);

        string countLine;
        Color countColor;
        if (secLeft < 11)
        {
            countLine = string.Format("{0} 秒！", secLeft);
            // Actual 1.6.0 floors the captured realtime clock without converting it to Int32.
            float frac = now - Mathf.Floor(now);
            float pulse = Mathf.Abs(500f - frac * 1000f) / 500f;
            Color32 mixed = Color32.Lerp(new Color32(255, 80, 80, 255),
                new Color32(255, 220, 120, 255), pulse);
            countColor = new Color(mixed.r / 255f, mixed.g / 255f, mixed.b / 255f, mixed.a / 255f);
        }
        else
        {
            countLine = string.Format("{0} 秒后自动选择", secLeft);
            countColor = new Color(170f / 255f, 170f / 255f, 180f / 255f, 1f);
        }
        TX(px + pw - BitmapFont.Measure(countLine, font) - 2f * s, titleY, countLine, countColor,
            font);

        float progress = 0f;
        if (_sec > 0)
        {
            progress = Mathf.Clamp01((float)secLeft / (float)_sec);
        }
        float barY = py + 28f * s;
        float barH = Mathf.Max(2f, 2f * s);
        GUI.color = new Color(1f, 1f, 1f, 0.12f);
        GUI.DrawTexture(Snap(new Rect(px, barY, pw, barH)), dot);
        GUI.color = 10 < secLeft
            ? new Color(0.85f, 0.75f, 0.45f, 0.9f)
            : new Color(1f, 0.35f, 0.3f, 0.9f);
        GUI.DrawTexture(Snap(new Rect(px, barY, pw * progress, barH)), dot);
        GUI.color = Color.white;

        // Slot counter: the row is drawn for slot n - 1 and the badge and the footer print n, so
        // the number the player reads is the key the keyboard branch below reacts to (1 / 2 / 3).
        int n = 1;
        while (true)
        {
            int i = n - 1;
            if (2 < i || _cards.Count <= i)
            {
                Rect rr = RerollRect();
                float mx = Input.mousePosition.x;
                float my = Screen.height - Input.mousePosition.y;
                bool hover = rr.x <= mx && mx < rr.x + rr.width && rr.y <= my
                    && my < rr.y + rr.height && _rerollCost >= 0;

                ArtworkSkin.ButtonFace(Snap(rr), null, s, _rerollCost >= 0, hover,
                    font / s, hover && Input.GetMouseButton(0));

                string rowLabel;
                Color32 rowColor32;
                if (_rerollCost < 0)
                {
                    rowLabel = "重抽已用完";
                    rowColor32 = new Color32(120, 120, 130, 255);
                }
                else if (_rerollCost != 0)
                {
                    rowLabel = string.Format("重抽  {0} 银  R", _rerollCost / 1000);
                    rowColor32 = new Color32(255, 210, 130, 255);
                }
                else if (1 < _rerollFree)
                {
                    rowLabel = string.Format("重抽(免费 {0} 次)  R", _rerollFree);
                    rowColor32 = new Color32(150, 235, 160, 255);
                }
                else
                {
                    rowLabel = "重抽(免费一次)  R";
                    rowColor32 = new Color32(150, 235, 160, 255);
                }
                Color rowColor = hover
                    ? Color.white
                    : new Color(rowColor32.r / 255f, rowColor32.g / 255f, rowColor32.b / 255f,
                        rowColor32.a / 255f);
                TX(rr.x + (rr.width - BitmapFont.Measure(rowLabel, font)) * 0.5f,
                    rr.y + (RH * s - 12f * font) * 0.5f, rowLabel, rowColor, font);

                // The official body only hit-tests the row while a reroll is still on offer; a
                // disabled row cannot be clicked at all.
                if (_rerollCost >= 0)
                {
                    if (GUI.Button(rr, GUIContent.none, GUIStyle.none))
                    {
                        Reroll();
                    }
                }

                GUI.color = restore;

                // Same-frame keyboard branch, after the row's own button: 1 / 2 / 3 on
                // either the top row or the keypad pick a slot, R rerolls. Anything else ends the
                // pass. Pick and Reroll keep their own gates, so no card state is checked here.
                Event e = Event.current;
                if (e == null || e.type != EventType.KeyDown)
                {
                    return;
                }
                if (e.keyCode == KeyCode.Alpha1 || e.keyCode == KeyCode.Keypad1)
                {
                    Pick(0);
                    return;
                }
                if (e.keyCode == KeyCode.Alpha2 || e.keyCode == KeyCode.Keypad2)
                {
                    Pick(1);
                    return;
                }
                if (e.keyCode == KeyCode.Alpha3 || e.keyCode == KeyCode.Keypad3)
                {
                    Pick(2);
                    return;
                }
                if (e.keyCode == KeyCode.R)
                {
                    Reroll();
                }
                return;
            }

            Rect cr = CardRect(i);
            float cx = cr.x;
            float cy = cr.y;
            float cw = cr.width;
            float ch = cr.height;
            float mxc = Input.mousePosition.x;
            float myc = Screen.height - Input.mousePosition.y;
            bool cardHover = cx <= mxc && mxc < cx + cw && cy <= myc && myc < cy + ch;
            Card card = _cards[i];

            ArtworkSkin.Panel(cr, s, cardHover);
            // Retain the two semantic card colour zones inside the artwork border.
            float cardScale = cardHover ? 1.35f : 1f;
            GUI.color = new Color(cardScale * 0.13f, cardScale * 0.11f, cardScale * 0.2f, .65f);
            GUI.DrawTexture(Snap(new Rect(cx + 7f * s, cy + 7f * s, cw - 14f * s, ch * .42f - 7f * s)), dot);
            GUI.color = new Color(cardScale * 0.09f, cardScale * 0.08f, cardScale * 0.14f, .65f);
            GUI.DrawTexture(Snap(new Rect(cx + 7f * s, cy + ch * .42f, cw - 14f * s, ch * .58f - 7f * s)), dot);
            GUI.color = Color.white;
            ArtworkSkin.Input(Snap(new Rect(cx, cy, 20f * s, 20f * s)), s, cardHover);

            TX(cx + 6f * s, cy + 3f * s, n.ToString(),
                new Color(1f, .94f, .78f, 1f), font);
            float nameFont = FontName;
            TX(cx + (cw - BitmapFont.Measure(card.name, nameFont)) * 0.5f, cy + 22f * s, card.name,
                new Color(1f, 226f / 255f, 138f / 255f, 1f), nameFont);

            GUI.color = new Color(183f / 255f, 155f / 255f, 98f / 255f, 0.35f);
            GUI.DrawTexture(Snap(new Rect(cx + 14f * s, cy + 48f * s, cw - 28f * s, ruleWidth)), dot);
            GUI.color = Color.white;
            DrawWrapped(card.desc, new Rect(cx + 12f * s, cy + 58f * s, cw - 24f * s,
                ch - 92f * s), font);

            string foot = cardHover ? "点击选择" : string.Concat("点击 / 按 ", n.ToString());
            Color footColor = cardHover
                ? new Color(255f / 255f, 240f / 255f, 190f / 255f, 1f)
                : new Color(150f / 255f, 150f / 255f, 160f / 255f, 1f);
            TX(cx + (cw - BitmapFont.Measure(foot, font)) * 0.5f, cy + ch - 22f * s, foot,
                footColor, font);

            // Hit-tested against CardRect(i), after the text, exactly like the reroll row: the
            // official Pick carries the real gates.
            if (GUI.Button(cr, GUIContent.none, GUIStyle.none))
            {
                Pick(i);
            }
            n = n + 1;
        }
    }
}
}
