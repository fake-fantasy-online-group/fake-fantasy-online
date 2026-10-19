using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Pixel-grid composition for the approved registration presentation, not an original registration screen.</summary>
internal static class RegistrationPixelArt
{
    internal const int PanelWidth = 348, PanelHeight = 242;
    internal const string SubsetResource = "FakeFantasyOnline/Registration/WqyRegistrationSubset";

    internal static Dictionary<char, bool[]> ReadSubset(string source)
    {
        var result = new Dictionary<char, bool[]>();
        if (source == null) return result;
        foreach (string raw in source.Split('\n'))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
            string[] parts = line.Split(':');
            if (parts.Length != 2 || !int.TryParse(parts[0], NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out int code) || code > char.MaxValue || code < 0) continue;
            string[] rows = parts[1].Split(',');
            if (rows.Length != 12) continue;
            bool[] mask = new bool[144];
            bool valid = true;
            for (int y = 0; y < 12; y++)
            {
                if (!int.TryParse(rows[y], NumberStyles.HexNumber, CultureInfo.InvariantCulture,
                        out int bits) || bits < 0 || bits > 4095) { valid = false; break; }
                for (int x = 0; x < 12; x++) mask[y * 12 + x] = (bits & (1 << (11 - x))) != 0;
            }
            if (valid) result[(char)code] = mask;
        }
        return result;
    }

    // GSA pixels use Unity's bottom-up convention; all composition coordinates are top-down.
    private static Color32 Pixel(Texture2D texture, Color32[] pixels, int x, int y)
    {
        int scale = GsaTextureScale.Of(texture);
        return pixels[(texture.height - 1 - y * scale) * texture.width + x * scale];
    }

    private static bool IsSize(Texture2D texture, int width, int height)
    {
        return texture && texture.isReadable && GsaTextureScale.Width(texture) == width &&
            GsaTextureScale.Height(texture) == height;
    }

    internal static Texture2D BuildPanel(Texture2D top, Texture2D middle, Texture2D bottom)
    {
        if (!IsSize(top, 348, 29) || !IsSize(middle, 348, 16) || !IsSize(bottom, 348, 40)) return null;
        Color32[] a = top.GetPixels32(), b = middle.GetPixels32(), c = bottom.GetPixels32();
        var output = new Color32[PanelWidth * PanelHeight];
        // Bottom bar starts at 176. Insert 26 copies of its blank row 16 to separate
        // the inner border and the original outer border, without stretching ornaments.
        for (int y = 0; y < PanelHeight; y++)
            for (int x = 0; x < PanelWidth; x++)
            {
                Color32 color;
                if (y < 29) color = Pixel(top, a, x, y);
                else if (y < 176) color = Pixel(middle, b, x, (y - 29) % 16);
                else
                {
                    int row = y - 176;
                    color = Pixel(bottom, c, x, row < 16 ? row : row < 42 ? 16 : row - 26);
                }
                output[(PanelHeight - 1 - y) * PanelWidth + x] = color;
            }
        return CreateTexture("Runtime registration panel", PanelWidth, PanelHeight, output);
    }

    internal static Color32[] AddNativeGlyphs(Texture2D source, Dictionary<char, bool[]> glyphs)
    {
        // Coordinates were verified on the 05diff 317x131 loginbk only. Never sample
        // a differently laid-out package, and never distribute its glyphs in this assembly.
        if (!IsSize(source, 317, 131)) return null;
        Color32[] pixels = source.GetPixels32();
        const string characters = "帐号密码";
        int[] startsX = { 132, 144, 156, 168 };
        int[] startsY = { 28, 28, 65, 65 };
        var gradient = new Color32[11];
        for (int i = 0; i < characters.Length; i++)
        {
            var mask = new bool[144];
            bool found = false;
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 12; x++)
                {
                    Color32 sample = Pixel(source, pixels, startsX[i] + x, startsY[i] + y);
                    bool ink = sample.a != 0 && sample.r >= 238 && sample.g >= 100 && sample.r > sample.b + 50;
                    mask[y * 12 + x] = ink;
                    if (!ink) continue;
                    found = true;
                    if (y > 0) gradient[y - 1] = new Color32(sample.r, sample.g, sample.b, 255);
                }
            if (found) glyphs[characters[i]] = mask;
        }
        for (int y = 0; y < gradient.Length; y++) if (gradient[y].a == 0) return null;
        return gradient;
    }

    internal static Texture2D BuildLabel(string text, int tracking,
        Dictionary<char, bool[]> glyphs, Color32[] gradient)
    {
        if (string.IsNullOrEmpty(text) || gradient == null || gradient.Length != 11) return null;
        int width = text.Length * 12 + (text.Length - 1) * tracking + 2;
        const int height = 14;
        bool[] ink = new bool[width * height];
        for (int i = 0; i < text.Length; i++)
        {
            if (!glyphs.TryGetValue(text[i], out bool[] glyph) || glyph.Length != 144) return null;
            for (int y = 0; y < 12; y++)
                for (int x = 0; x < 12; x++)
                    ink[(y + 1) * width + i * (12 + tracking) + x + 1] = glyph[y * 12 + x];
        }
        var pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (ink[y * width + x])
                    for (int dy = -1; dy <= 1; dy++)
                        for (int dx = -1; dx <= 1; dx++)
                            if (x + dx >= 0 && x + dx < width && y + dy >= 0 && y + dy < height)
                                pixels[(height - 1 - y - dy) * width + x + dx] = new Color32(0, 0, 0, 255);
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (ink[y * width + x]) pixels[(height - 1 - y) * width + x] = gradient[Math.Max(0, Math.Min(10, y - 2))];
        return CreateTexture("Runtime registration label", width, height, pixels);
    }

    private static Texture2D CreateTexture(string name, int width, int height, Color32[] pixels)
    {
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        { name = name, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }
}
}
