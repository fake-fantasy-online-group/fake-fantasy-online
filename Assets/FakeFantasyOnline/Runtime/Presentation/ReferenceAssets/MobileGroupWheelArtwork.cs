using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Mobile wheel only: crisp flat disc matching the original design preview.</summary>
internal static class MobileGroupWheelArtwork
{
    private const int Size = 384;
    private static readonly Texture2D[] states = new Texture2D[5];

    internal static Texture2D Face(int pending)
    {
        int index = pending >= 0 && pending < 4 ? pending + 1 : 0;
        if (states[index] != null) return states[index];
        var pixels = new Color32[Size * Size];
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            // Texture rows run upwards; MobileGroupWheel uses GUI downwards.
            float dx = (x + .5f - Size * .5f) * .5f;
            float dy = (Size * .5f - y - .5f) * .5f;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            Color32 color = new Color32(19, 32, 29, 235);
            if (index > 0 && radius > 24f && MobileGroupWheel.Hit(dx, dy, 24f) == index - 1)
                color = new Color32(99, 80, 42, 238);
            if (radius < 24f) color = new Color32(27, 44, 38, 247);
            // Only the perimeter and cancel boundary have outlines: no sector dividers.
            float outline = Mathf.Clamp01(1.5f - Mathf.Abs(radius - 24f));
            float outer = Mathf.Clamp01(radius - 94.5f);
            float mix = Mathf.Max(outline, outer);
            color.r = (byte)Mathf.RoundToInt(color.r + (139 - color.r) * mix);
            color.g = (byte)Mathf.RoundToInt(color.g + (119 - color.g) * mix);
            color.b = (byte)Mathf.RoundToInt(color.b + (70 - color.b) * mix);
            color.a = (byte)Mathf.RoundToInt(color.a * Mathf.Clamp01((96f - radius) * 2f));
            pixels[y * Size + x] = color;
        }
        Texture2D face = new Texture2D(Size, Size, TextureFormat.RGBA32, false)
        {
            name = "Mobile group wheel " + index, filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave
        };
        face.SetPixels32(pixels);
        face.Apply(false, true);
        states[index] = face;
        return face;
    }

    internal static void Clear()
    {
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i] != null) Object.Destroy(states[i]);
            states[i] = null;
        }
    }
}
}
