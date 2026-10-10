using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerAvatar
{
    // Official 1.5.8: 060009af, 060009b0 and 060009b1.
    public Sprite Still(int dir)
    {
        dir = Mathf.Clamp(dir, 0, 4);
        if (_stillCache == null) _stillCache = new Dictionary<int, Sprite>();
        if (!_stillCache.TryGetValue(dir, out Sprite sprite))
        {
            sprite = BakeStill(stand == null || stand.Length <= dir ? null : stand[dir]);
            _stillCache[dir] = sprite;
        }
        return sprite;
    }

    private Sprite BakeStill(Pose pose)
    {
        if (pose == null || pose.layers == null || pose.layers.Length == 0) return null;
        var layers = new List<(Color32[] pixels, int x, int y, int width, int height)>();
        float minX = float.MaxValue, minY = float.MaxValue;
        float maxX = float.MinValue, maxY = float.MinValue;
        foreach (LayerInst layer in pose.layers)
        {
            if (layer.additive || layer.spr == null || layer.spr.texture == null) continue;
            Sprite sprite = layer.spr;
            Texture2D texture = sprite.texture;
            int scale = Mathf.Max(1, Mathf.RoundToInt(sprite.pixelsPerUnit / _ppu));
            int width = texture.width / scale, height = texture.height / scale;
            Color32[] pixels = texture.GetPixels32();
            if (scale != 1) pixels = Downsample(pixels, texture.width, texture.height, scale);
            float x = layer.offX * _ppu - sprite.pivot.x / scale;
            float y = -layer.offY * _ppu - sprite.pivot.y / scale;
            layers.Add((pixels, Mathf.RoundToInt(x), Mathf.RoundToInt(y), width, height));
            minX = Mathf.Min(minX, x);
            minY = Mathf.Min(minY, y);
            maxX = Mathf.Max(maxX, x + width);
            maxY = Mathf.Max(maxY, y + height);
        }
        if (layers.Count == 0) return null;
        int outputWidth = Mathf.Max(1, Mathf.CeilToInt(maxX - minX));
        int outputHeight = Mathf.Max(1, Mathf.CeilToInt(maxY - minY));
        int originX = Mathf.RoundToInt(-minX), originY = Mathf.RoundToInt(-minY);
        anchorX = originX;
        anchorY = outputHeight - originY;
        var output = new Color32[outputWidth * outputHeight];
        foreach (var layer in layers)
        {
            for (int y = 0; y < layer.height; y++)
            {
                int targetY = y + layer.y + originY;
                if (targetY < 0 || targetY >= outputHeight) continue;
                for (int x = 0; x < layer.width; x++)
                {
                    int targetX = x + layer.x + originX;
                    if (targetX < 0 || targetX >= outputWidth) continue;
                    Color32 source = layer.pixels[y * layer.width + x];
                    if (source.a == 0) continue;
                    int index = targetY * outputWidth + targetX;
                    Color32 destination = output[index];
                    if (source.a != 255 && destination.a != 0)
                    {
                        int remaining = (destination.a * (255 - source.a) + 127) / 255;
                        int alpha = source.a + remaining;
                        int half = alpha >> 1;
                        source = alpha == 0 ? new Color32() : new Color32(
                            (byte)((source.r * source.a + destination.r * remaining + half) / alpha),
                            (byte)((source.g * source.a + destination.g * remaining + half) / alpha),
                            (byte)((source.b * source.a + destination.b * remaining + half) / alpha),
                            (byte)alpha);
                    }
                    output[index] = source;
                }
            }
        }
        var result = new Texture2D(outputWidth, outputHeight, TextureFormat.RGBA32, false);
        result.filterMode = FilterMode.Bilinear;
        result.wrapMode = TextureWrapMode.Clamp;
        result.SetPixels32(output);
        result.Apply(false, false);
        return Sprite.Create(result, new Rect(0, 0, outputWidth, outputHeight),
            new Vector2((float)originX / outputWidth, (float)originY / outputHeight), _ppu);
    }

    private static Color32[] Downsample(Color32[] source, int width, int height, int scale)
    {
        int outputWidth = width / scale, outputHeight = height / scale;
        var result = new Color32[outputWidth * outputHeight];
        for (int y = 0; y < outputHeight; y++)
            for (int x = 0; x < outputWidth; x++)
            {
                long red = 0, green = 0, blue = 0, alpha = 0;
                for (int dy = 0; dy < scale; dy++)
                    for (int dx = 0; dx < scale; dx++)
                    {
                        Color32 pixel = source[(y * scale + dy) * width + x * scale + dx];
                        red += pixel.r * pixel.a;
                        green += pixel.g * pixel.a;
                        blue += pixel.b * pixel.a;
                        alpha += pixel.a;
                    }
                if (alpha > 0)
                    result[y * outputWidth + x] = new Color32((byte)(red / alpha),
                        (byte)(green / alpha), (byte)(blue / alpha), (byte)(alpha / (scale * scale)));
            }
        return result;
    }
}
}
