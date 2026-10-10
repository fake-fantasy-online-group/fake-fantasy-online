using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatNumber
{
    private static Sprite[] expPrefix;

    private static List<Sprite> BuildGlyphs(int value, int type)
    {
        var glyphs = new List<Sprite>();
        string text = value.ToString();
        Sprite[] digits;
        if (type == 4)
        {
            Sprite[] prefix = LoadExpPrefix();
            EnsureLoaded();
            digits = sets != null ? sets[2] : null;
            if (prefix == null || digits == null) return null;
            glyphs.AddRange(prefix);
        }
        else
        {
            if (!EnsureLoaded()) return null;
            digits = sets[SetIndexForType(type)];
            if (digits == null) return null;
        }
        foreach (char character in text)
        {
            if (character < '0' || character > '9') continue;
            int digit = character - '0';
            if (digits[digit]) glyphs.Add(digits[digit]);
        }
        return glyphs;
    }

    private static Sprite[] LoadExpPrefix()
    {
        if (expPrefix != null) return expPrefix.Length > 0 ? expPrefix : null;
        expPrefix = new Sprite[0];
        byte[] data = ReadRes("res/exp/EXP.gsa");
        if (data == null) return null;
        DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(data);
        if (!anim.ok || anim.f == null || anim.f.Length < 4) return null;
        int width = anim.f[0].tex ? anim.f[0].tex.width : 32;
        int height = anim.f[0].tex ? anim.f[0].tex.height : 32;
        int columns = 5, rows = 2;
        if (data.Length >= 28)
        {
            int totalWidth = BitConverter.ToInt32(data, 24);
            int totalHeight = data.Length >= 32 ? BitConverter.ToInt32(data, 28) : 0;
            if (totalWidth > 0 && width > 0) columns = unchecked(totalWidth + width - 1) / width;
            if (totalHeight > 0 && height > 0) rows = unchecked(totalHeight + height - 1) / height;
        }
        columns = Mathf.Clamp(columns, 1, 16);
        rows = Mathf.Clamp(rows, 1, 16);
        Texture2D texture = new Texture2D(columns * width, rows * height, TextureFormat.RGBA32, false);
        texture.SetPixels32(new Color32[columns * width * rows * height]);
        for (int i = 0; i < anim.f.Length && i < columns * rows; i++)
        {
            Texture2D frame = anim.f[i].tex;
            if (!frame) continue;
            int x = (i % columns) * width;
            int y = (rows - 1 - i / columns) * height;
            texture.SetPixels32(x, y, width, height, frame.GetPixels32());
        }
        texture.Apply(false, false);
        int scale = Mathf.Max(anim.scale, 1);
        glyphScale = Mathf.Max(glyphScale, scale);
        Sprite sprite = CropSprite(texture, scale * 100f);
        expPrefix = sprite ? new[] { sprite } : new Sprite[0];
        return expPrefix.Length > 0 ? expPrefix : null;
    }
}

}
