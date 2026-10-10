using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TitleFlow
{
    // Official 1.5.8 06000ee4. Width-only scaling keeps the supplied top-left position.
    // Owner-only: no original caller is established; do not substitute this for DrawSprite.
    private void DrawSpriteTop(Texture2D t, Rect area)
    {
        if (!t) return;
        float factor = area.width / t.width;
        float height = factor * t.height;
        GUI.DrawTexture(new Rect(area.x, area.y, area.width, height), t);
    }

    // Official 1.5.8 06000ee5. Fit the top headFrac of the texture, centered in the area.
    // Owner-only. The original does not clamp headFrac or guard zero texture dimensions.
    private void DrawHead(Texture2D t, Rect area, float headFrac)
    {
        if (!t) return;
        float headHeight = t.height * headFrac;
        float aspect = t.width / headHeight;
        float width = area.width;
        float height = width / aspect;
        if (height > area.height)
        {
            height = area.height;
            width = height * aspect;
        }
        Rect destination = new Rect(area.x + (area.width - width) * 0.5f,
            area.y + (area.height - height) * 0.5f, width, height);
        GUI.DrawTextureWithTexCoords(destination, t, new Rect(0f, 1f - headFrac, 1f, headFrac));
    }
}
}
