using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MissFx
{
    private static bool EnsureLoaded()
    {
        if (glyphs != null) return glyphs.Length != 0;
        glyphs = new Sprite[0];
        if (!ResLoader.I || ResLoader.I.Pkg == null)
        {
            glyphs = null;
            return false;
        }
        byte[] data = ResLoader.I.Pkg.Read("res/effect/miss.gsa");
        if (data == null) return false;
        DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(data);
        if (!anim.ok || anim.f == null) return false;
        glyphScale = Mathf.Max(1, anim.scale);
        var loaded = new List<Sprite>();
        foreach (DimgDecoder.FrameTex frame in anim.f)
        {
            if (!frame.tex) continue;
            Sprite sprite = Crop(frame.tex);
            if (sprite) loaded.Add(sprite);
        }
        glyphs = loaded.ToArray();
        return glyphs.Length != 0;
    }

    private static Sprite Crop(Texture2D tex)
    {
        int width = tex.width, height = tex.height;
        Color32[] pixels;
        try { pixels = tex.GetPixels32(); }
        catch
        {
            return Sprite.Create(tex, new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0f), 100f * glyphScale);
        }
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= 16) continue;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        Rect rect = maxX >= minX
            ? new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1)
            : new Rect(0f, 0f, width, height);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0f), 100f * glyphScale);
    }
}

}
