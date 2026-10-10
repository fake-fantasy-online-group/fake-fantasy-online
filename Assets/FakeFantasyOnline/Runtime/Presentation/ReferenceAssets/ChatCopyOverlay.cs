using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Official 060000cd–060000d5, the 430x270 centred "select and copy" window. Open stores the body and
// the caption, parks the scroll position at float.MaxValue so the view opens at the bottom of the
// text, closes any UiInput right-click menu and focuses the body field under the name "chatcopy".
// OnGUI paints the panel, hands the body to UiInput.Area (which owns drag selection, Ctrl/⌘+C and the
// right-click menu) and closes itself — plus suppresses the world click — on a mouse down outside the
// box. The two buttons use cached reference artwork and "全部复制" pushes the body to the
// system clipboard and reports the copied line count through UITip.
internal sealed class ChatCopyOverlay : MonoBehaviour
{
    private const float LW = 430, LH = 270;
    private const string CTRL = "chatcopy";
    private const string HINT = "鼠标拖选 或 Ctrl/⌘+A 全选 → Ctrl/⌘+C 复制 (右键也有菜单; ESC 关闭)";

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
    private readonly GUIContent bodyContent = new GUIContent();

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
        I.bodyContent.text = I.text;
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
        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;
        try
        {
            GUI.depth = 0;
            ArtworkSkin.Panel(box, s);
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
            }
            int fs = Mathf.RoundToInt(s * 12f);
            st.fontSize = fs;
            if (BitmapFont.Ready)
            {
                float titleScale = Mathf.Min(s, (w - 16f * s) / Mathf.Max(1f, BitmapFont.Measure(title, 1f)));
                BitmapFont.DrawOutline(x + s * 8f, y + s * 8f, title,
                    new Color(1f, 220f / 255f, 120f / 255f, 1f), titleScale);
                float hintScale = Mathf.Min(s, (w - 16f * s) / Mathf.Max(1f, BitmapFont.Measure(HINT, 1f)));
                BitmapFont.DrawOutline(x + s * 8f, y + s * 8f + s * 14f, HINT,
                    new Color(160f / 255f, 160f / 255f, 150f / 255f, 1f), hintScale);
            }
            float pad = s * 8f;
            // Official 060000d3 keeps the differences it later reuses: the hint baseline is
            // (y + pad) + s * 14 and the view top is (y + pad) + s * 32, and the view height comes back
            // from that same top, ((y + h) - top) - s * 36 (arm64 0x22f3fc). Writing 22 and 68 instead
            // re-associates the same terms and lands up to one float apart for a non-dyadic UIScale.
            float viewTop = y + pad + s * 32f;
            Rect view = new Rect(x + pad, viewTop, w - pad * 2f, (y + h - viewTop) - s * 36f);
            float cw = view.width - s * 18f;
            float ch = Mathf.Max(view.height, st.CalcHeight(bodyContent, cw));
            Rect content = new Rect(0f, 0f, cw, ch);
            ArtworkSkin.Input(new Rect(view.x - 2f * s, view.y - 2f * s,
                view.width + 4f * s, view.height + 4f * s), s,
                GUI.GetNameOfFocusedControl() == CTRL);
            scroll = GUI.BeginScrollView(view, scroll, content, GUIStyle.none, GUIStyle.none);
            // The official discards the returned string: the body is read-only here, UiInput.Area only
            // supplies drag selection, Ctrl/⌘+C and the right-click menu (060000d3 stores nothing back).
            UiInput.Area(content, CTRL, text, 20000, st, s);
            GUI.EndScrollView();
            NativeScroll.Bar(this, view, ch, ref scroll);
            float bh = s * 20f;
            float by = y + h - pad - bh;
            float bx = x + w - pad - s * 56f;
            if (CopyButton(new Rect(bx - s * 92f, by, s * 88f, bh), "全部复制", e))
            {
                GUIUtility.systemCopyBuffer = text;
                int lines = 1;
                for (int i = 0; i < text.Length; i++) if (text[i] == '\n') lines++;
                UITip.Show("已复制 " + lines.ToString() + " 行", 1.2f);
            }
            if (CopyButton(new Rect(bx, by, s * 56f, bh), "关 闭", e)) Hide();
            if (e.type == EventType.MouseDown && !box.Contains(e.mousePosition))
            {
                Hide();
                PlayerController.SuppressWorldClick();
            }
        }
        finally
        {
            GUI.color = previousColor;
            GUI.depth = previousDepth;
        }
    }

    // Keep this overlay's existing press-to-activate semantics and world-click suppression.
    private bool CopyButton(Rect rect, string caption, Event current)
    {
        bool hover = rect.Contains(current.mousePosition);
        ArtworkSkin.ButtonFace(rect, caption, s, selected: hover,
            pressed: hover && Input.GetMouseButton(0));
        if (!hover || current.type != EventType.MouseDown || current.button != 0) return false;
        current.Use();
        PlayerController.SuppressWorldClick();
        return true;
    }
}
}
