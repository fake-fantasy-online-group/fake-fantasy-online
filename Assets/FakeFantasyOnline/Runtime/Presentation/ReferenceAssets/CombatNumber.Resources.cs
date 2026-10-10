using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    private static Sprite[][] sets;
    private static int glyphScale = 1;

    private static bool EnsureLoaded()
    {
        if (sets != null) return sets[0] != null;
        sets = new Sprite[3][];
        for (int set = 0; set < 3; set++)
        {
            byte[] data = ReadRes(string.Format("res/effect/num{0}.gsa", set + 1));
            if (data == null) continue;
            DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(data);
            if (!anim.ok || anim.f == null) continue;
            int scale = Mathf.Max(anim.scale, 1);
            glyphScale = Mathf.Max(glyphScale, scale);
            Sprite[] digits = new Sprite[10];
            float spritePpu = scale * 100f;
            for (int digit = 0; digit < 10 && digit < anim.f.Length; digit++)
            {
                Texture2D texture = anim.f[digit].tex;
                if (texture) digits[digit] = CropSprite(texture, spritePpu);
            }
            sets[set] = digits;
        }
        return sets[0] != null;
    }

    private static byte[] ReadRes(string path)
    {
        if (!ResLoader.I) return null;
        if (ResLoader.I.Pkg != null)
        {
            byte[] data = ResLoader.I.Pkg.Read(path);
            if (data != null) return data;
        }
        return ResLoader.I.Pkg24 != null ? ResLoader.I.Pkg24.Read(path) : null;
    }
    private static Sprite CropSprite(Texture2D tex, float ppu)
    {
        if (!tex) return null;
        int width = tex.width;
        int height = tex.height;
        Color32[] pixels;
        try { pixels = tex.GetPixels32(); }
        catch
        {
            return Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0f), ppu);
        }
        int minX = width, minY = height, maxX = -1, maxY = -1;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (pixels[y * width + x].a <= 16) continue;
                minX = Mathf.Min(minX, x);
                maxX = Mathf.Max(maxX, x);
                minY = Mathf.Min(minY, y);
                maxY = Mathf.Max(maxY, y);
            }
        }
        Rect rect = maxX >= minX
            ? new Rect(minX, minY, maxX - minX + 1, maxY - minY + 1)
            : new Rect(0f, 0f, width, height);
        return Sprite.Create(tex, rect, new Vector2(0.5f, 0f), ppu);
    }

}

}
