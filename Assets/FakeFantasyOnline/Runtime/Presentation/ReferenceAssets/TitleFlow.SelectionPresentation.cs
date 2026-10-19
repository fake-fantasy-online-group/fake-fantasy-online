using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // Approved selection-page presentation. Original textures remain owned by ResLoader;
    // no package pixels, replacement fonts, or copied character artwork are bundled here.
    private enum SelectionButton { Confirm, Auto, Return, Quit, Create, Delete, Enter }
    private readonly GUIStyle[] selectionButtons = new GUIStyle[7];
    private ResLoader selectionLoader;
    private object selectionPackage, selectionPackage24, selectionPackage21;
    private GUIStyle selectionCenter, selectionText;

    private void EnsureSelectionPresentation()
    {
        ResLoader loader = ResLoader.I;
        if (selectionCenter != null && ReferenceEquals(selectionLoader, loader) &&
            ReferenceEquals(selectionPackage, loader?.Pkg) &&
            ReferenceEquals(selectionPackage24, loader?.Pkg24) &&
            ReferenceEquals(selectionPackage21, loader?.PkgFf21)) return;

        ReleaseSelectionPresentation();
        selectionLoader = loader;
        selectionPackage = loader?.Pkg;
        selectionPackage24 = loader?.Pkg24;
        selectionPackage21 = loader?.PkgFf21;
        // The complete external gbk12 atlas is preferred for dynamic text. The existing
        // GUI font remains a whole-string fallback for unavailable art or missing glyphs;
        // no new font is guessed and the registration-only subset is not expanded.
        selectionCenter = new GUIStyle(center)
        { fontSize = 12, clipping = TextClipping.Clip };
        selectionText = new GUIStyle(selectionCenter) { richText = false, wordWrap = false };
        if (loader == null || loader.Pkg == null) return;

        string[] names = { "affirmbtn", "autobtn", "returnbtn", "quitbtn", "createbtn", "deletebtn", "enterbtn" };
        for (int i = 0; i < names.Length; i++)
        {
            string path = "res/uires/titleui/" + names[i] + ".gsa";
            Texture2D normal = loader.LoadGsaFrameA(path, 0);
            if (!IsSelectionButtonFrame(normal)) continue;
            Texture2D hover = loader.LoadGsaFrameA(path, 1);
            Texture2D pressed = loader.LoadGsaFrameA(path, 2);
            var style = new GUIStyle
            {
                border = new RectOffset(), margin = new RectOffset(), padding = new RectOffset(),
                overflow = new RectOffset(), imagePosition = ImagePosition.ImageOnly
            };
            style.normal.background = normal;
            style.hover.background = IsSelectionButtonFrame(hover) ? hover : normal;
            style.active.background = IsSelectionButtonFrame(pressed) ? pressed : normal;
            style.focused.background = style.hover.background;
            selectionButtons[i] = style;
        }
    }

    private static bool IsSelectionButtonFrame(Texture2D texture) =>
        texture && GsaTextureScale.Width(texture) == 152f && GsaTextureScale.Height(texture) == 33f;

    private bool DrawSelectionButton(float x, float y, SelectionButton button, string fallback)
    {
        Rect rect = new Rect(x, y, 152f, 33f);
        GUIStyle style = selectionButtons[(int)button];
        if (style == null) return ArtworkSkin.Button(rect, fallback);
        Color previous = GUI.color;
        try
        {
            if (!GUI.enabled) GUI.color = previous * new Color(.42f, .42f, .42f, 1f);
            // Full baked-label frames. Unity owns mouse release/cancel, focus and disabled
            // behavior; no label is painted over the original lettering.
            return GUI.Button(rect, GUIContent.none, style);
        }
        finally { GUI.color = previous; }
    }

    private void DrawSelectionLabel(Rect rect, string text, Color color, bool leftAligned = false)
    {
        if (string.IsNullOrEmpty(text)) return;
        BitmapFont.EnsureForIndependentRuntime();
        bool covered = BitmapFont.Ready;
        // Ready alone is insufficient: BitmapFont.Draw intentionally skips missing cells.
        // Fall back for the WHOLE string so mixed Chinese/Latin/emoji names never lose glyphs.
        if (covered)
            foreach (char c in text)
                if (c != ' ' && BitmapFont.CellOfForIndependentRuntime(c) < 0) { covered = false; break; }
        if (!covered)
        {
            selectionText.alignment = leftAligned ? TextAnchor.MiddleLeft : TextAnchor.MiddleCenter;
            selectionText.normal.textColor = color;
            GUI.Label(rect, text, selectionText);
            return;
        }

        float width = BitmapFont.Measure(text, 1f);
        float scale = width <= 0f ? 1f : Mathf.Min(1f, Mathf.Max(0f, rect.width - 2f) / width);
        GUI.BeginGroup(rect);
        try
        {
            BitmapFont.DrawOutline(leftAligned ? 1f : (rect.width - width * scale) * .5f,
                (rect.height - 12f * scale) * .5f, text, color, scale);
        }
        finally { GUI.EndGroup(); }
    }

    private void SelectionValue(float x, float y, int value) =>
        DrawSelectionLabel(new Rect(x, y, 60f, 14f), value.ToString(), new Color(.86f, .8f, .72f, 1f), true);

    private void ReleaseSelectionPresentation()
    {
        Array.Clear(selectionButtons, 0, selectionButtons.Length);
        selectionLoader = null;
        selectionPackage = selectionPackage24 = selectionPackage21 = null;
        selectionCenter = selectionText = null;
    }
}
}
