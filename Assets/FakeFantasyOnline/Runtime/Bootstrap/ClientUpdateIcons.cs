using UnityEngine;
namespace FakeFantasyOnline.Bootstrap
{
/// <summary>Small source-owned glyphs. No system-font emoji and no downloaded game assets.</summary>
internal static class ClientUpdateIcons
{
    internal static Texture2D Create(bool gear)
    {
        const int size = 32;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
        {
            float px = x + .5f - 16, py = y + .5f - 16;
            bool ink;
            if (gear)
            {
                float r = Mathf.Sqrt(px * px + py * py), a = Mathf.Atan2(py, px);
                float outer = Mathf.Cos(a * 8) > .3f ? 13.5f : 10.5f;
                ink = r >= 5 && r <= outer;
            }
            else
            {
                // Diagonal open jaw + handle, with an open center at the handle end.
                float dx = px - 5, dy = py - 5;
                bool jaw = dx * dx + dy * dy <= 7.4f * 7.4f && !(dx + dy > 2 && Mathf.Abs(dx - dy) < 6);
                float along = (px + py) * .70710678f, across = (px - py) * .70710678f;
                bool handle = along >= -11 && along <= 6 && Mathf.Abs(across) < 2.8f;
                float bx = px + 7.5f, by = py + 7.5f;
                bool end = bx * bx + by * by < 4.3f * 4.3f;
                bool hole = bx * bx + by * by < 1.5f * 1.5f;
                ink = (jaw || handle || end) && !hole;
            }
            texture.SetPixel(x, y, ink ? new Color(.95f, .82f, .55f, 1) : new Color(0, 0, 0, 0));
        }
        texture.Apply(false, true);
        return texture;
    }
}
}
