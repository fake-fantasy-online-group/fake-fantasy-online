using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    public float ppu = 100f;
    private static readonly int[] DIRS = { 1, 2, 4, 8 };
    private readonly Dictionary<long, Sprite> spriteCache = new Dictionary<long, Sprite>();

    // Original 1.5.8 GetV27Cell (06000bb4).
    private Sprite GetV27Cell(int lb, int lt, int rt, int rb, string animName, int fr)
    {
        long key = (long)(lb & 0x1ff) | ((long)(lt & 0x1ff) << 12) |
            ((long)(rt & 0x1ff) << 24) | ((long)(rb & 0x1ff) << 36);
        if (animName != null) key |= (long)(fr & 0x3f) << 48;
        if (spriteCache.TryGetValue(key, out Sprite cached)) return cached;
        var canvas = new Color32[16384];
        int[] codes = { lb, lt, rt, rb };
        int[] order = { 0, 1, 2, 3 };
        Array.Sort(order, (a, b) => (codes[a] < 0 ? 999 : codes[a]) - (codes[b] < 0 ? 999 : codes[b]));
        var seen = new HashSet<int>();
        bool any = false;
        foreach (int corner in order)
        {
            int code = codes[corner];
            if (code < 0 || seen.Contains(code)) continue;
            seen.Add(code);
            int mask = 0;
            for (int i = 0; i < 4; i++) if (codes[i] == code) mask |= DIRS[i];
            if (!code2name.TryGetValue(code, out string name)) continue;
            Color32[] pixels = null;
            if (name == animName)
            {
                AnimTil animation = GetAnimTil(name);
                if (animation != null) pixels = animation.frames[fr % animation.frameNum];
            }
            else
            {
                Color32[][] shapes = GetShapesFull(name);
                if (shapes != null)
                    pixels = (mask == 15 || seen.Count == 1 ? shapes[14] : shapes[mask - 1]) ?? shapes[14];
            }
            if (pixels == null) continue;
            Over(canvas, pixels);
            any = true;
        }
        if (!any)
        {
            spriteCache[key] = null;
            return null;
        }
        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(FlipV(canvas, 128, 128));
        texture.Apply(false, false);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 128), new Vector2(0, 1), ppu);
        spriteCache[key] = sprite;
        return sprite;
    }
}

}
