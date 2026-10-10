using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// The horn composer. The retained composer/skin machinery was restored from 1.5.8;
// 1.6.0 adds the pending share lifecycle and the InsertShare/InputFocused entry points.
// The window turns a horn item into a
// server-wide shout. Open is driven from the wire and GameHud asks
// get_Visible while hit-testing the HUD. The plate is 261x186 design units; it carries a
// 120 character text area, and - only for a tier 2 horn that was opened without a fixed skin - an
// eleven entry skin picker laid out as six columns below the plate.
internal sealed class HornDialog : MonoBehaviour
{
    private const int MAX_CHARS = 120;
    private const float BG_W = 261f;
    private const float BG_H = 186f;
    private const string FIELD = "horninput";

    public static HornDialog I;
    public static bool Typing;
    public static readonly string[] SkinNames;

    private bool shown;
    private int itemId;
    private int tier;
    private int fixedSkin;
    private int skin = 1;
    private string text = string.Empty;
    private readonly List<(byte kind, byte tab, int slot, string name)> pendShr =
        new List<(byte, byte, int, string)>();
    private Texture2D bg;
    private Texture2D okN, okH;
    private Texture2D caN, caH;
    private Texture2D clN, clH;
    private bool triedAssets;
    private bool focusPending;
    private GUIStyle editSt;

    // 1.5.8 evidence .cctor（060004f2）: the twelve skin names. Slot 0 stays empty because the picker enumerates
    // 1..11, so the array is a one-based table (arm64 0x4576a4/744B - newarr String[12] then twelve
    // direct stelem stores with write barriers; x86_64 0x4692a0 matches).
    static HornDialog()
    {
        SkinNames = new string[12];
        SkinNames[0] = string.Empty;
        SkinNames[1] = "黄金龙";
        SkinNames[2] = "黄金凤";
        SkinNames[3] = "决斗之王";
        SkinNames[4] = "恶魔王子";
        SkinNames[5] = "精灵仙子";
        SkinNames[6] = "财源广进";
        SkinNames[7] = "年年有余";
        SkinNames[8] = "张灯结彩";
        SkinNames[9] = "招财进宝";
        SkinNames[10] = "白富美";
        SkinNames[11] = "高富帅";
    }

    // Official 1.6.0 060004fa: the field initializers assign skin, text, and pendShr
    // in that order before the MonoBehaviour base constructor.
    public HornDialog()
    {
    }

    // 1.5.8 evidence get_Visible（060004e8）: the instance flag, never the GameObject liveness - Close only clears
    // shown and leaves the singleton allocated.
    public static bool Visible => I != null && I.shown;

    // Official 1.6.0 060004f2: a stale GUI control name cannot focus a hidden composer.
    public static bool InputFocused => Visible && GUI.GetNameOfFocusedControl() == FIELD;

    // Official 1.6.0 060004f1. Shares append to this visible composer's text and list;
    // the limit failures do not append a share or refocus the horn input.
    public static bool InsertShare(byte kind, byte tab, int slot, string name)
    {
        if (I == null || !I.shown) return false;
        if (I.pendShr.Count >= 4)
        {
            SysMsgBox.Show("一句话最多分享4件", null, null);
            return false;
        }
        string token = "【" + name + "】";
        if (unchecked((I.text ?? string.Empty).Length + token.Length) > MAX_CHARS)
        {
            SysMsgBox.Show("字数满了, 放不下这一件", null, null);
            return false;
        }
        I.text += token;
        I.pendShr.Add((kind, tab, slot, name));
        GUI.FocusControl(FIELD);
        return true;
    }

    // 1.5.8 evidence MouseOver（060004e9）: the official body tail-calls get_Visible (arm64 0x45589c: b 0x45574c),
    // so the horn plate shares the whole-window hit test rather than a shaped one.
    public static bool MouseOver() => Visible;

    // Official 1.6.0 Open (060004f3): clears text and shares before showing/rearming the caret.
    // A fixed skin is authoritative; otherwise the previous choice survives unless it is still 0.
    public static void Open(int itemId, int tier, int fixedSkin)
    {
        if (I == null)
        {
            GameObject go = new GameObject("HornDialog");
            I = go.AddComponent<HornDialog>();
        }
        I.itemId = itemId;
        I.tier = tier;
        I.fixedSkin = fixedSkin;
        if (fixedSkin >= 1) I.skin = fixedSkin;
        else if (I.skin < 1) I.skin = 1;
        I.text = string.Empty;
        I.pendShr.Clear();
        I.shown = true;
        I.focusPending = true;
    }

    // Official 1.6.0 Close (060004f4): hides the plate and clears pending shares before
    // dropping the typing latch and releasing keyboard focus, within the live-instance gate.
    public static void Close()
    {
        if (I == null) return;
        I.shown = false;
        I.pendShr.Clear();
        Typing = false;
        GUI.FocusControl(null);
    }

    // 1.5.8 evidence get_Title（060004ec）: a plain horn is 大喇叭; a tier 2 horn shows the skin name, falling back
    // to 炫彩大喇叭 while no skin was chosen.
    public string Title
    {
        get
        {
            if (tier < 2) return "大喇叭";
            if (fixedSkin < 1) return "炫彩大喇叭";
            int idx = fixedSkin;
            if (idx < 2) idx = 1;
            if (idx > 10) idx = 11;
            return SkinNames[idx] + "喇叭";
        }
    }

    // 1.5.8 evidence EnsureAssets（060004ed）: one-shot behind triedAssets; every texture comes from ResLoader and
    // stays null when the loader is not up yet, which is why OnGUI keeps the 261x186/0.8 black
    // fallbacks.
    private void EnsureAssets()
    {
        if (triedAssets) return;
        triedAssets = true;
        if (ResLoader.I == null) return;
        bg = ResLoader.I.LoadGsa("res/uires/laba/laba.gsa");
        okN = ResLoader.I.LoadGsaFrameA("res/uires/common/affirmbtn.gsa", 0);
        okH = ResLoader.I.LoadGsaFrameA("res/uires/common/affirmbtn.gsa", 1);
        caN = ResLoader.I.LoadGsaFrameA("res/uires/common/cancelbtn.gsa", 0);
        caH = ResLoader.I.LoadGsaFrameA("res/uires/common/cancelbtn.gsa", 1);
        clN = ResLoader.I.LoadGsaFrame("res/uires/common/miniclose.gsa", 0);
        clH = ResLoader.I.LoadGsaFrame("res/uires/common/miniclose.gsa", 1);
    }

    // Official 1.6.0 Submit (060004f7): normalises the buffer the same way OnGUI does, refuses an empty shout,
    // truncates to MAX_CHARS and then closes whether or not the packet could go out.
    // The 1.6.0 SendHorn gate remains a plain reference test (arm64 0x473588/0x47358c),
    // matching the historical 1.5.8 gate proof. ReferenceEquals keeps the product's
    // NetClient : MonoBehaviour from compiling it into UnityEngine.Object.op_Inequality.
    private void Submit()
    {
        string body = (text ?? string.Empty).Replace("\r", string.Empty).Replace("\n", " ").Trim();
        if (body.Length == 0)
        {
            SysMsgBox.Show("您还没有输入哟!", null, null);
            return;
        }
        if (body.Length > MAX_CHARS) body = body.Substring(0, MAX_CHARS);
        byte hornSkin;
        if (tier < 2) hornSkin = 0;
        else if (fixedSkin < 1)
        {
            int idx = skin;
            if (idx < 2) idx = 1;
            if (idx > 10) idx = 11;
            hornSkin = (byte)idx;
        }
        else hornSkin = (byte)fixedSkin;
        List<(byte, byte, int)> shares = null;
        if (pendShr.Count > 0)
        {
            ChatUI.TakeOutgoing(body, pendShr);
            shares = ChatUI.OutgoingShares;
        }
        if (!ReferenceEquals(NetClient.I, null)) NetClient.I.SendHornForIndependentRuntime(body, itemId, hornSkin, shares);
        Close();
    }

    // Official 1.6.0 OnGUI (060004f8): paint order is plate, title, text area, optional skin strip, three buttons.
    // Escape closes before anything is drawn; Enter/KeypadEnter submits only while the text area
    // owns the keyboard (unlike Escape, shift is rejected).
    private void OnGUI()
    {
        if (!shown) return;
        EnsureAssets();
        GUI.depth = 0;
        float sc = UIScale.Factor;
        Event ev = Event.current;
        float bw = bg != null ? TexScale.W(bg) : BG_W;
        float bh = bg != null ? TexScale.H(bg) : BG_H;
        float x0 = Mathf.Round((Screen.width - sc * bw) * .5f);
        float y0 = Mathf.Round((Screen.height - sc * bh) * .5f);

        // 1.5.8 evidence L（060004f3, inlined）: every child rect is authored in design units and scaled here.
        Rect L(float x, float y, float w, float h) =>
            new Rect(x0 + x * sc, y0 + y * sc, w * sc, h * sc);

        if (ev.type == EventType.KeyDown && ev.keyCode == KeyCode.Escape)
        {
            Close();
            ev.Use();
            return;
        }

        if (bg != null)
        {
            GUI.DrawTexture(L(0f, 0f, bw, bh), bg);
        }
        else
        {
            GUI.color = new Color(0f, 0f, 0f, .8f);
            GUI.DrawTexture(L(0f, 0f, bw, bh), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        if (BitmapFont.Ready)
        {
            string title = Title;
            float titleW = BitmapFont.Measure(title, sc);
            BitmapFont.DrawOutline(sc * 79f + x0 + (sc * 111f - titleW) * .5f,
                sc * 5f + sc * 19f + y0, title, new Color(.8f, .6f, .2f, 1f), sc);
        }

        GUIStyle st = editSt;
        if (st == null)
        {
            st = new GUIStyle(GUIStyle.none)
            {
                font = NpcChatter.CnFont(),
                alignment = TextAnchor.UpperLeft,
                wordWrap = true,
            };
            editSt = st;
            st.normal.textColor = Color.white;
            st.focused.textColor = Color.white;
            st.padding = new RectOffset(2, 2, 2, 2);
        }
        st.fontSize = Mathf.RoundToInt(sc * 12f);

        if (ev.type == EventType.KeyDown &&
            (ev.keyCode == KeyCode.Return || ev.keyCode == KeyCode.KeypadEnter) && !ev.shift &&
            GUI.GetNameOfFocusedControl() == FIELD)
        {
            ev.Use();
            Submit();
            return;
        }

        GUI.SetNextControlName(FIELD);
        string oldText = text ?? string.Empty;
        string typed = GUI.TextArea(L(33f, 48f, 195f, 106f), oldText, MAX_CHARS, st);
        if (typed != null) text = typed.Replace("\r", string.Empty).Replace("\n", " ");
        text = ChatUI.GuardShareEdit(oldText, text, pendShr);
        if (focusPending)
        {
            GUI.FocusControl(FIELD);
            focusPending = false;
        }
        Typing = GUI.GetNameOfFocusedControl() == FIELD;

        if (tier > 1 && fixedSkin < 1 && BitmapFont.Ready)
        {
            GUI.color = new Color(0f, 0f, 0f, .6f);
            GUI.DrawTexture(L(0f, bh + 2f, bw, 40f), Texture2D.whiteTexture);
            GUI.color = Color.white;
            float cellW = sc * (bw / 6f);
            for (int idx = 1; idx <= 11; idx++)
            {
                int col = (idx - 1) % 6;
                int row = (idx - 1) / 6;
                Rect cell = L(col * (bw / 6f), bh + 4f + row * 18f, bw / 6f, 18f);
                bool over = cell.Contains(ev.mousePosition);
                Color ink = idx == skin
                    ? new Color(1f, .843f, .235f, 1f)
                    : (over ? Color.white : new Color(.784f, .784f, .784f, 1f));
                float nameW = BitmapFont.Measure(SkinNames[idx], sc);
                BitmapFont.DrawOutline(cell.x + (cellW - nameW) * .5f, cell.y + sc * 3f,
                    SkinNames[idx], ink, sc);
                if (over && ev.type == EventType.MouseDown && ev.button == 0)
                {
                    skin = idx;
                    ev.Use();
                }
            }
        }

        Btn(L(47f, 157f, 70f, 22f), okN, okH, ev, Submit);
        Btn(L(169f, 157f, 70f, 22f), caN, caH, ev, Close);
        Btn(L(235f, 20f, 22f, 24f), clN, clH, ev, Close);
    }

    // 1.5.8 evidence Btn（060004f0）: hover swaps in the hot frame (and raises the placeholder alpha from .2 to .35)
    // and the click is a bare invisible GUI.Button over the same rect.
    private static void Btn(Rect r, Texture2D n, Texture2D h, Event ev, Action onClick)
    {
        Texture2D face = n;
        float alpha = .2f;
        if (r.Contains(ev.mousePosition))
        {
            face = h != null ? h : n;
            alpha = .35f;
        }
        if (face == null)
        {
            GUI.color = new Color(1f, 1f, 1f, alpha);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }
        else
        {
            GUI.DrawTexture(r, face);
        }
        if (GUI.Button(r, GUIContent.none, GUIStyle.none)) onClick();
    }
}
}
