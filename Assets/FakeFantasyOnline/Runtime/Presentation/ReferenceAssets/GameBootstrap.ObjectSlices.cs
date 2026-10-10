using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{

    internal List<(Sprite, int, int)> GetObjectSlicesForIndependentRuntime(int id, ResLoader.ObjSprite image, float ppu) =>
        GetSlices(id, image, ppu);

    // Extracted original PlaceObj whole-image cache branch; ownership remains on GameBootstrap.
    internal Sprite GetObjectSpriteForIndependentRuntime(int id, ResLoader.ObjSprite image, float ppu)
    {
        if (objCache.TryGetValue(id, out Sprite cached)) return cached;
        Sprite sprite = Sprite.Create(image.tex, new Rect(0, 0, image.tex.width, image.tex.height),
            new Vector2(0, 1), ppu * Mathf.Max(image.hdScale, 1));
        objCache[id] = sprite;
        return sprite;
    }

    // Official 1.5.8 token 060003d2. Sprite rectangles borrow os.tex.
    private List<(Sprite, int, int)> GetSlices(int id, ResLoader.ObjSprite os, float ppu)
    {
        if (sliceCache.TryGetValue(id, out List<(Sprite, int, int)> cached)) return cached;
        var slices = new List<(Sprite, int, int)>();
        int[] offsets = os.clipArr;
        int clipWidth = os.clipWidth;
        int width = os.tex.width;
        int height = os.tex.height;
        int count = offsets.Length;
        int scale = Mathf.Max(os.hdScale, 1);
        for (int start = 0; start < count;)
        {
            int end = start + 1;
            while (end < count && Mathf.Abs(offsets[end] - offsets[start]) <= 3) end++;
            int left = start * clipWidth * scale;
            int right = Mathf.Min(end * clipWidth * scale, width);
            if (right - left > 0)
            {
                Sprite sprite = Sprite.Create(os.tex, new Rect(left, 0, right - left, height),
                    new Vector2(0, 1), ppu * scale);
                int ground = offsets[(start + end - 1) / 2];
                if (ground <= 0) ground = os.clipBy;
                slices.Add((sprite, start * clipWidth, ground));
            }
            start = end;
        }
        sliceCache[id] = slices;
        return slices;
    }
}

}
