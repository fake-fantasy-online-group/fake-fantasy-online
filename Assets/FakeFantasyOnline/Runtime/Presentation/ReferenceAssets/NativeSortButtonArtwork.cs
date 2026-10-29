using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// The source packages have no dedicated “整理” face. This is an inspected,
// genuinely blank native four-state button, with the existing bitmap-font label
// drawn separately at runtime. No copied/relabelled artwork is shipped.
internal static class NativeSortButtonArtwork
{
    internal const string BackgroundPath = "res/uires/festival/GetAwardBtn.gsa";
    internal const float Width = 48f;
    internal const float Height = 24f;
    private const float SourceWidth = 86f, EndWidth = 9f;

    internal static void Draw(Rect rect, float scale, bool hover, bool pressed)
    {
        bool enabled = GUI.enabled;
        int state = !enabled ? 3 : pressed ? 2 : hover ? 1 : 0;
        Texture2D[] frames = ArtworkSkin.Frames(BackgroundPath);
        Texture2D texture = frames != null && state < frames.Length ? frames[state] : null;
        // Measure initializes the existing font; no replacement glyph asset is made.
        BitmapFont.Measure("整理", scale);
        if (texture == null || GsaDecoder.W(texture) != SourceWidth ||
            GsaDecoder.H(texture) != Height || !BitmapFont.Ready)
        {
            ArtworkSkin.ButtonFace(rect, "整理", scale, enabled, hover, pressed: pressed);
            return;
        }

        // Keep the source's full 24px height and its thin side caps. Only the blank
        // middle is resized horizontally; unlike the old 79x39 nine-slice, no
        // tall-frame border is squeezed into the inventory strip.
        float end = EndWidth * scale;
        GUI.DrawTextureWithTexCoords(new Rect(rect.x, rect.y, end, rect.height), texture,
            new Rect(0f, 0f, EndWidth / SourceWidth, 1f));
        GUI.DrawTextureWithTexCoords(new Rect(rect.x + end, rect.y, rect.width - 2f * end, rect.height), texture,
            new Rect(EndWidth / SourceWidth, 0f, (SourceWidth - 2f * EndWidth) / SourceWidth, 1f));
        GUI.DrawTextureWithTexCoords(new Rect(rect.xMax - end, rect.y, end, rect.height), texture,
            new Rect(1f - EndWidth / SourceWidth, 0f, EndWidth / SourceWidth, 1f));
        BitmapFont.CenterOutline(rect, "整理", enabled
            ? new Color(1f, .94f, .78f, 1f) : new Color(.58f, .56f, .53f, 1f), scale);
    }
}
}
