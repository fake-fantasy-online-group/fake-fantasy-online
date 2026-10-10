using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class CoverFloor
{
    // Original 1.5.8 CoverFloor.Build (06000b82): takes the native GsmMap owner and reads the
    // cover block fields directly. Fallback scene selection is separate.
    public static Texture2D Build(GsmMap map)
    {
        if (map == null || !map.hasCover || map.coverImgs == null) return null;
        int width = map.coverPw > 0 ? map.coverPw : map.coverBw * map.coverCx;
        int height = map.coverPh > 0 ? map.coverPh : map.coverBh * map.coverCy;
        if (width <= 0 || height <= 0) return null;
        Color32[] canvas = new Color32[width * height];
        int drawn = 0;
        for (int row = 0; row < map.coverCy; row++)
            for (int column = 0; column < map.coverCx; column++)
            {
                GsmMap.NakedImg item = map.coverImgs[row * map.coverCx + column];
                if (item.data == null || item.w <= 0 || item.h <= 0) continue;
                Color32[] pixels = DimgDecoder.ToColors(item.w, item.h, item.pitch, item.attrib, item.data);
                if (pixels == null) continue;
                int left = column * map.coverBw;
                int top = row * map.coverBh;
                for (int y = 0; y < item.h; y++)
                {
                    int dy = top + y;
                    if (dy < 0 || dy >= height) continue;
                    for (int x = 0; x < item.w; x++)
                    {
                        int dx = left + x;
                        if (dx < 0 || dx >= width) continue;
                        Color32 pixel = pixels[y * item.w + x];
                        if (pixel.a != 0) canvas[dy * width + dx] = pixel;
                    }
                }
                drawn++;
            }
        if (drawn == 0) return null;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Point;
        Color32[] bottomUp = new Color32[width * height];
        for (int y = 0; y < height; y++) Array.Copy(canvas, y * width, bottomUp, (height - 1 - y) * width, width);
        texture.SetPixels32(bottomUp);
        texture.Apply(false, false);
        return texture;
    }
}

}
