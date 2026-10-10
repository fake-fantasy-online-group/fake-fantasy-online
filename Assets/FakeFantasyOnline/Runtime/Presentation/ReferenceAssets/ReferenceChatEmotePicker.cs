using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source translation of the locked 1.3.4 EmotePicker surface.
/// </summary>
internal sealed class ReferenceChatEmotePicker
{
    public const int Count = 96;
    public const int Columns = 8;
    public const int VisibleRows = 6;
    public const float CellPixels = 22f;
    public const float IconPixels = 20f;
    public const float PaddingPixels = 6f;
    public const float PanelWidthPixels = 204f;
    public const float PanelHeightPixels = 144f;

    private static readonly object ScrollOwner = typeof(ReferenceChatEmotePicker);
    private static Vector2 scroll;

    private static readonly Color PanelColor =
        new Color(0.06f, 0.05f, 0.04f, 0.94f);
    private static readonly Color BorderColor =
        new Color(1f, 0.85f, 0.4f, 0.7f);
    private static readonly Color HoverColor =
        new Color(1f, 0.9f, 0.4f, 0.3f);

    private readonly ReferenceResourceLoader resources;
    private Texture2D[] icons;

    public ReferenceChatEmotePicker(ReferenceResourceLoader resources)
    {
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
    }

    public static float PanelWidth(float scale)
    {
        return PanelWidthPixels * scale;
    }

    public static float PanelHeight(float scale)
    {
        return PanelHeightPixels * scale;
    }

    public static float ContentHeight(float scale)
    {
        return (Count / Columns) * CellPixels * scale;
    }

    public static Rect InnerRect(Rect panel, float scale)
    {
        float padding = PaddingPixels * scale;
        return new Rect(
            panel.x + padding,
            panel.y + padding,
            panel.width - padding * 2f,
            panel.height - padding * 2f);
    }

    public static Rect IconRect(int zeroBasedIndex, float scale)
    {
        if (zeroBasedIndex < 0 || zeroBasedIndex >= Count)
        {
            return new Rect();
        }

        return new Rect(
            (zeroBasedIndex & (Columns - 1)) * CellPixels * scale,
            (zeroBasedIndex >> 3) * CellPixels * scale,
            IconPixels * scale,
            IconPixels * scale);
    }

    public Texture2D IconFor(int emoteId)
    {
        if (emoteId < 1 || emoteId > Count || !EnsureIcons())
        {
            return null;
        }

        return icons[emoteId - 1];
    }

    public int Draw(Rect panel, Event current, float scale)
    {
        if (current == null || !(scale > 0f) || float.IsNaN(scale) ||
            float.IsInfinity(scale) || !EnsureIcons())
        {
            return 0;
        }

        DrawPanel(panel);

        Rect inner = InnerRect(panel, scale);
        float contentHeight = ContentHeight(scale);
        Rect content = new Rect(
            0f,
            0f,
            inner.width - 16f * scale,
            contentHeight);
        scroll = GUI.BeginScrollView(
            inner,
            scroll,
            content,
            false,
            true,
            GUIStyle.none,
            GUIStyle.none);

        int selected = 0;
        Vector2 pointer = current.mousePosition;
        float iconPixels = IconPixels * scale;
        for (int index = 0; index < Count; index++)
        {
            Rect rect = IconRect(index, scale);
            if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            {
                selected = index + 1;
            }

            if (rect.Contains(pointer))
            {
                Color previous = GUI.color;
                GUI.color = HoverColor;
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
                GUI.color = previous;
            }

            Texture2D icon = icons[index];
            if (icon)
            {
                GUI.DrawTexture(
                    new Rect(rect.x, rect.y, iconPixels, iconPixels),
                    icon,
                    ScaleMode.ScaleToFit,
                    true);
            }
        }

        GUI.EndScrollView();
        NativeScroll.Bar(
            ScrollOwner,
            inner,
            contentHeight,
            ref scroll);
        return selected;
    }

    internal static void ResetScrollForAudit()
    {
        scroll = Vector2.zero;
    }

    private bool EnsureIcons()
    {
        if (icons != null)
        {
            return true;
        }

        Texture2D[] loaded = new Texture2D[Count];
        for (int index = 0; index < Count; index++)
        {
            loaded[index] = resources.LoadGsaFrame(
                ReferenceWorldChatBubbleAssets.EmotePath(index + 1),
                0);
        }

        icons = loaded;
        return true;
    }

    private static void DrawPanel(Rect panel)
    {
        Color previous = GUI.color;
        GUI.color = PanelColor;
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = BorderColor;
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 1f), Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(panel.x, panel.yMax - 1f, panel.width, 1f),
            Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panel.x, panel.y, 1f, panel.height), Texture2D.whiteTexture);
        GUI.DrawTexture(
            new Rect(panel.xMax - 1f, panel.y, 1f, panel.height),
            Texture2D.whiteTexture);
        GUI.color = previous;
    }
}

}
