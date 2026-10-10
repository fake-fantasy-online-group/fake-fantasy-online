using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 060000cd–060000d5, the 430x270 centred "select and copy" window. Open stores the body and
// the caption, parks the scroll position at float.MaxValue so the view opens at the bottom of the
// text, closes any UiInput right-click menu and focuses the body field under the name "chatcopy".
// OnGUI paints the panel, hands the body to UiInput.Area (which owns drag selection, Ctrl/⌘+C and the
// right-click menu) and closes itself — plus suppresses the world click — on a mouse down outside the
// box. The two buttons are drawn by the OnGUI-local Btn helper and "全部复制" pushes the body to the
// system clipboard and reports the copied line count through UITip.
internal sealed class ChatCopyOverlay : MonoBehaviour
{
    private const float LW = 430, LH = 270;
    private const string CTRL = "chatcopy";
    private const string HINT = "鼠标拖选 或 Ctrl/⌘+A 全选 → Ctrl/⌘+C 复制 (右键也有菜单; ESC 关闭)";

    // .cctor（060000d5）: the panel fill and the border, both packed as Color32 (0xf0161b1a / 0xff5ca4c4).
    private static readonly Color32 BK = new Color32(26, 27, 22, 240);
    private static readonly Color32 LINE = new Color32(196, 164, 92, 255);

    public static ChatCopyOverlay I;

    internal static void OpenForIndependentRuntime(string body, string caption)
    {
        Open(body, caption);
        I.enabled = false;
    }

    // Receipt callers may run before a world/manual chat renderer exists. Give the original
    // component its native OnGUI owner; a later manual world pass must not draw it again.
    internal static void OpenAtReceiptForIndependentRuntime(string body, string caption)
    {
        Open(body, caption);
        I.enabled = true;
    }

    internal static void DrawForIndependentRuntime()
    {
        if (I != null && !I.enabled) I.OnGUI();
    }

    internal static string TextForIndependentRuntime => I != null ? I.text : string.Empty;
    internal static string TitleForIndependentRuntime => I != null ? I.title : string.Empty;

    private bool visible;
    private string text;
    private string title;
    private Vector2 scroll;
    private float s;
    private GUIStyle st;
    private GUIStyle bt;

    // .ctor（060000d4）: both string fields start empty (two usage 10335 writes before the base call).
    public ChatCopyOverlay()
    {
        text = string.Empty;
        title = string.Empty;
    }

    // Ensure（060000ce）: one DontDestroyOnLoad GameObject carrying the component.
    public static void Ensure()
    {
        if (I != null) return;
        GameObject go = new GameObject("ChatCopyOverlay");
        I = go.AddComponent<ChatCopyOverlay>();
        DontDestroyOnLoad(go);
    }

    // Open（060000cf）: body/caption fall back to "" when null; the official caption default is "选取复制".
    public static void Open(string body, string caption = "选取复制")
    {
        Ensure();
        I.text = body ?? string.Empty;
        I.title = caption ?? string.Empty;
        I.visible = true;
        I.scroll = new Vector2(0f, float.MaxValue);
        UiInput.Close();
        GUI.FocusControl(CTRL);
    }

    // Hide（060000d0）: clears the flag and drops the keyboard focus the body field was holding.
    public static void Hide()
    {
        if (I == null) return;
        I.visible = false;
        GUI.FocusControl(null);
    }

    // get_Visible（060000cd）
    public static bool Visible => I != null && I.visible;

    // MouseOver（060000d1）: falls through to UiInput.MouseOver so the right-click menu keeps the
    // pointer while it is open.
    public static bool MouseOver()
    {
        if (I == null || !I.visible) return false;
        Vector2 pointer = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        Rect box = I.Box();
        if (box.Contains(pointer)) return true;
        return UiInput.MouseOver();
    }

    // Box（060000d2）
    private Rect Box()
    {
        float scale = UIScale.Factor;
        float w = LW * scale, h = LH * scale;
        return new Rect((Screen.width - w) * .5f, (Screen.height - h) * .5f, w, h);
    }

    // OnGUI（060000d3）
    private void OnGUI()
    {
        if (!visible) return;
        s = UIScale.Factor;
        Event e = Event.current;
        float w = LW * UIScale.Factor, h = LH * UIScale.Factor;
        float x = (Screen.width - w) * .5f, y = (Screen.height - h) * .5f;
        Rect box = new Rect(x, y, w, h);
        GUI.depth = 0;
        GUI.color = BK;
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = LINE;
        GUI.DrawTexture(new Rect(x, y, w, s), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y + h - s, w, s), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x, y, s, h), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(x + w - s, y, s, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
        if (st == null)
        {
            st = new GUIStyle(GUIStyle.none);
            st.font = NpcChatter.CnFont();
            st.wordWrap = true;
            st.alignment = TextAnchor.UpperLeft;
            st.richText = false;
            Color body = new Color(.92f, .92f, .86f, 1f);
            st.normal.textColor = body;
            st.focused.textColor = body;
            st.hover.textColor = body;
            st.active.textColor = body;
            bt = new GUIStyle(GUIStyle.none);
            bt.font = NpcChatter.CnFont();
            bt.alignment = TextAnchor.MiddleCenter;
            Color label = new Color(1f, .88f, .55f, 1f);
            bt.normal.textColor = label;
            bt.hover.textColor = label;
        }
        int fs = Mathf.RoundToInt(s * 12f);
        bt.fontSize = fs;
        st.fontSize = fs;
        if (BitmapFont.Ready)
        {
            BitmapFont.DrawOutline(x + s * 8f, y + s * 8f, title,
                new Color(1f, 220f / 255f, 120f / 255f, 1f), s);
            BitmapFont.DrawOutline(x + s * 8f, y + s * 8f + s * 14f, HINT,
                new Color(160f / 255f, 160f / 255f, 150f / 255f, 1f), s);
        }
        float pad = s * 8f;
        // Official 060000d3 keeps the differences it later reuses: the hint baseline is
        // (y + pad) + s * 14 and the view top is (y + pad) + s * 32, and the view height comes back
        // from that same top, ((y + h) - top) - s * 36 (arm64 0x22f3fc). Writing 22 and 68 instead
        // re-associates the same terms and lands up to one float apart for a non-dyadic UIScale.
        float viewTop = y + pad + s * 32f;
        Rect view = new Rect(x + pad, viewTop, w - pad * 2f, (y + h - viewTop) - s * 36f);
        float cw = view.width - s * 18f;
        float ch = Mathf.Max(view.height, st.CalcHeight(new GUIContent(text), cw));
        Rect content = new Rect(0f, 0f, cw, ch);
        scroll = GUI.BeginScrollView(view, scroll, content);
        // The official discards the returned string: the body is read-only here, UiInput.Area only
        // supplies drag selection, Ctrl/⌘+C and the right-click menu (060000d3 stores nothing back).
        UiInput.Area(content, CTRL, text, 20000, st, s);
        GUI.EndScrollView();
        float bh = s * 20f;
        float by = y + h - pad - bh;
        float bx = x + w - pad - s * 56f;
        Btn(new Rect(bx - s * 92f, by, s * 88f, bh), "全部复制", () =>
        {
            GUIUtility.systemCopyBuffer = text;
            int lines = 1;
            for (int i = 0; i < text.Length; i++) if (text[i] == '\n') lines++;
            UITip.Show("已复制 " + lines.ToString() + " 行", 1.2f);
        });
        Btn(new Rect(bx, by, s * 56f, bh), "关 闭", Hide);
        if (e.type == EventType.MouseDown && !box.Contains(e.mousePosition))
        {
            Hide();
            PlayerController.SuppressWorldClick();
        }

        // <OnGUI>g__Btn|0（060000d7）: fills the button plate, draws the caption with the bitmap font
        // and, on a left click while hovered, consumes the event, suppresses the world click and runs
        // the action. The official caption baseline keeps a degenerate centring term that cancels to
        // zero, (s * 12f - s * 12f) * .5f, so the text lands at r.y + 4f * s.
        void Btn(Rect r, string cap, Action act)
        {
            bool hover = r.Contains(e.mousePosition);
            GUI.color = new Color(1f, 1f, 1f, hover ? .18f : .08f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = Color.white;
            if (BitmapFont.Ready && !string.IsNullOrEmpty(cap))
            {
                float m = BitmapFont.Measure(cap, s);
                BitmapFont.DrawOutline(r.x + (r.width - m) * .5f,
                    r.y + s * 4f + (s * 12f - s * 12f) * .5f,
                    cap, hover ? Color.white : new Color(1f, .88f, .55f, 1f), s);
            }
            if (!hover) return;
            if (e.type != EventType.MouseDown || e.button != 0) return;
            e.Use();
            PlayerController.SuppressWorldClick();
            act();
        }
    }
}
}
