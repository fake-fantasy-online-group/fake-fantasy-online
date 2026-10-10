using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the locked 1.3.4 ChatCopyOverlay owner.
/// </summary>
internal sealed class ReferenceChatCopyOverlay
{
    public const float LogicalWidth = 430f;
    public const float LogicalHeight = 270f;
    public const string ControlName = "chatcopy";
    public const string HistoryTitle = "选取复制 · 聊天记录";
    public const string WithPlayerTitlePrefix = "选取复制 · 与 ";
    public const string WithPlayerTitleSuffix = " 相关的记录";
    public const string Hint =
        "鼠标拖选 或 Ctrl/⌘+A 全选 → Ctrl/⌘+C 复制 (右键也有菜单; ESC 关闭)";

    private static readonly Color Background =
        new Color32(26, 27, 22, 240);
    private static readonly Color Border =
        new Color32(196, 164, 92, 255);
    private static readonly Color BodyColor =
        new Color(0.92f, 0.92f, 0.86f, 1f);
    private static readonly Color ButtonColor =
        new Color(1f, 0.88f, 0.55f, 1f);
    private static readonly Color HintColor =
        new Color(0.627f, 0.627f, 0.588f, 1f);

    private readonly ReferenceWorldBitmapFont font;
    private Vector2 scroll;
    private GUIStyle textStyle;
    private GUIStyle buttonStyle;
    private float styleScale;

    public ReferenceChatCopyOverlay(ReferenceWorldBitmapFont font)
    {
        this.font = font ?? throw new ArgumentNullException(nameof(font));
        Text = string.Empty;
        Title = string.Empty;
    }

    public bool Visible { get; private set; }

    public string Text { get; private set; }

    public string Title { get; private set; }

    public void Open(string body, string caption)
    {
        Text = body ?? string.Empty;
        Title = caption ?? string.Empty;
        scroll = Vector2.zero;
        Visible = true;
        GUI.FocusControl(ControlName);
    }

    public void Hide()
    {
        Visible = false;
        GUI.FocusControl(null);
    }

    public bool MouseOver(Vector2 topOriginPointer, int viewportWidth, int viewportHeight, float scale)
    {
        return Visible && IsPositiveFinite(scale) && viewportWidth > 0 && viewportHeight > 0 &&
               ContainsInclusive(BoxRect(viewportWidth, viewportHeight, scale), topOriginPointer);
    }

    public bool Draw(float scale)
    {
        if (!Visible || !IsPositiveFinite(scale))
        {
            return false;
        }

        EnsureStyles(scale);
        GUI.depth = 0;
        Rect box = BoxRect(Screen.width, Screen.height, scale);
        Color previous = GUI.color;
        GUI.color = Background;
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = Border;
        DrawBorder(box, scale);
        GUI.color = previous;

        font.DrawOutline(
            box.x + 8f * scale,
            box.y + 8f * scale,
            Title,
            new Color(1f, 0.863f, 0.471f, 1f),
            scale);
        font.DrawOutline(
            box.x + 8f * scale,
            box.y + 22f * scale,
            Hint,
            HintColor,
            scale);

        Rect viewport = new Rect(
            box.x + 8f * scale,
            box.y + 40f * scale,
            box.width - 16f * scale,
            box.height - 76f * scale);
        float contentWidth = Mathf.Max(1f, viewport.width - 18f * scale);
        float contentHeight = Mathf.Max(
            viewport.height,
            textStyle.CalcHeight(new GUIContent(Text), contentWidth));
        Rect content = new Rect(0f, 0f, contentWidth, contentHeight);
        scroll = GUI.BeginScrollView(viewport, scroll, content);
        Text = UiInput.Area(content, ControlName, Text ?? string.Empty, 20000, textStyle, scale);
        GUI.EndScrollView();

        float bottomY = box.y + box.height - 28f * scale;
        Rect close = new Rect(
            box.x + box.width - 64f * scale,
            bottomY,
            56f * scale,
            20f * scale);
        Rect copy = new Rect(
            close.x - 92f * scale,
            bottomY,
            88f * scale,
            20f * scale);
        if (GUI.Button(copy, "全部复制", buttonStyle))
        {
            GUIUtility.systemCopyBuffer = Text ?? string.Empty;
        }

        if (GUI.Button(close, "关 闭", buttonStyle))
        {
            Hide();
        }

        Event current = Event.current;
        if (Visible && current != null && current.type == EventType.KeyDown &&
            current.keyCode == KeyCode.Escape)
        {
            Hide();
            current.Use();
        }
        else if (Visible && current != null && current.type == EventType.MouseDown &&
                 !ContainsInclusive(box, current.mousePosition))
        {
            Hide();
            current.Use();
        }

        GUI.color = previous;
        return true;
    }

    public static Rect BoxRect(int viewportWidth, int viewportHeight, float scale)
    {
        float width = LogicalWidth * scale;
        float height = LogicalHeight * scale;
        return new Rect(
            (viewportWidth - width) * 0.5f,
            (viewportHeight - height) * 0.5f,
            width,
            height);
    }

    private void EnsureStyles(float scale)
    {
        if (textStyle != null && buttonStyle != null &&
            Mathf.Approximately(styleScale, scale))
        {
            return;
        }

        styleScale = scale;
        textStyle = new GUIStyle(GUI.skin.textArea)
        {
            wordWrap = true,
            richText = false,
            stretchHeight = false,
            fontSize = Mathf.RoundToInt(12f * scale)
        };
        textStyle.normal.textColor = BodyColor;
        textStyle.hover.textColor = BodyColor;
        textStyle.active.textColor = BodyColor;
        textStyle.focused.textColor = BodyColor;

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(12f * scale)
        };
        buttonStyle.normal.textColor = ButtonColor;
        buttonStyle.hover.textColor = ButtonColor;
        buttonStyle.active.textColor = ButtonColor;
        buttonStyle.focused.textColor = ButtonColor;
    }

    private static void DrawBorder(Rect rect, float scale)
    {
        float edge = Mathf.Max(1f, scale);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, edge), Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(rect.x, rect.yMax - edge, rect.width, edge),
            Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.y, edge, rect.height), Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(rect.xMax - edge, rect.y, edge, rect.height),
            Texture2D.whiteTexture);
    }

    private static bool ContainsInclusive(Rect rect, Vector2 point)
    {
        return point.x >= rect.xMin && point.x <= rect.xMax &&
               point.y >= rect.yMin && point.y <= rect.yMax;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
