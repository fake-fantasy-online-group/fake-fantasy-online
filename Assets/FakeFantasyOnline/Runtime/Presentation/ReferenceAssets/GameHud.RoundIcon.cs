using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Original instance slots; only RoundIcon replaces or destroys a generated copy.
    // No view retirement or synthetic GameHud cleanup resets this cache.
    private Texture2D _roundSrc;
    internal Texture2D _roundDst;

    // 06000412. Keep reference identity distinct
    // from Unity's destroyed-object null checks, including the cached source fallback.
    private Texture2D RoundIcon(Texture2D source)
    {
        if (source == null) return null;
        if (ReferenceEquals(source, _roundSrc) && _roundDst != null)
            return _roundDst;

        if (_roundDst != null && !ReferenceEquals(_roundDst, _roundSrc))
        {
            UnityEngine.Object.Destroy(_roundDst);
        }

        _roundSrc = source;
        _roundDst = null;
        try
        {
            int width = source.width;
            int height = source.height;
            Color32[] pixels = source.GetPixels32();
            float centerX = (width - 1) * 0.5f;
            float centerY = (height - 1) * 0.5f;
            float radius = Mathf.Min(width, height) * 0.5f + 0.5f;
            for (int y = 0; y < height; y++)
            {
                float dy = y - centerY;
                for (int x = 0; x < width; x++)
                {
                    float dx = x - centerX;
                    float coverage = Mathf.Clamp01(
                        radius - Mathf.Sqrt(dx * dx + dy * dy));
                    if (coverage >= 1f) continue;
                    int index = unchecked(y * width + x);
                    pixels[index].a = unchecked((byte)(int)(coverage * pixels[index].a));
                }
            }

            Texture2D rounded = new Texture2D(width, height, TextureFormat.RGBA32, false);
            rounded.SetPixels32(pixels);
            rounded.Apply();
            rounded.filterMode = source.filterMode;
            rounded.wrapMode = TextureWrapMode.Clamp;
            GsaTextureScale.Register(rounded, GsaTextureScale.Of(source));
            _roundDst = rounded;
        }
        catch (Exception)
        {
            _roundDst = source;
        }

        return _roundDst;
    }

    // 06000413. The existing avatar stage supplies the completed pixel slot.
    internal void DrawPetHead(Rect slot, Texture2D head, float scale)
    {
        if (!head) return;
        // 06000413 calls RoundIcon before reading the result's logical dimensions on every draw.
        Texture2D rounded = RoundIcon(head);
        float textureWidth = GsaTextureScale.Width(rounded);
        float textureHeight = GsaTextureScale.Height(rounded);
        if (Mathf.Min(textureWidth, textureHeight) <= 0f)
        {
            textureWidth = 25f;
            textureHeight = 25f;
        }

        float drawWidth = scale * textureWidth;
        float drawHeight = scale * textureHeight;
        float left = slot.x + Mathf.Round((slot.width - drawWidth) * 0.5f);
        float top = slot.y + Mathf.Round((slot.height - drawHeight) * 0.5f);
        GUI.DrawTexture(new Rect(left, top, drawWidth, drawHeight), rounded);
    }

}
}
