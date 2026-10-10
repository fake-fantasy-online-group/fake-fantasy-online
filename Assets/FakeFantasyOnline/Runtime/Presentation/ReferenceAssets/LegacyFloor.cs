using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 LegacyFloor, tokens 06000be1-06000be3. It composites the whole
// legacy floor into one texture that is a quarter of the map pixel size: each
// tile is decoded at 128x64, boxed down to 32x16 and blitted into a diamond
// layout, and the finished buffer is flipped vertically before upload because
// texture rows run bottom-up.
public static class LegacyFloor
{
    // Official private constants. Their baked values are read straight off the
    // call sites: OTW 64 and OTH 32 are the screen steps between neighbouring
    // tiles, SC_DEN 4 is the scale between map pixels and the composite.
    private const int OTH = 32;
    private const int OTW = 64;
    private const int SC_DEN = 4;

    private const int TILE_W = 128;
    private const int TILE_H = 64;
    private const int SMALL_W = TILE_W / SC_DEN;
    private const int SMALL_H = TILE_H / SC_DEN;

    // 06000be1
    public static Texture2D Build(PkgArchive pkg, GsmMap gm)
    {
        if (gm == null || !gm.isLegacy || gm.grids == null) return null;

        TileImgLib.Load(pkg, gm.tileSet);
        TerraShp.Load(pkg, gm.tileSet);
        if (!TileImgLib.Ready) return null;
        if (!TerraShp.Ready) return null;

        int cols = gm.vw;
        int rows = gm.vh;
        int w = Mathf.RoundToInt(gm.mapWidth * (1f / SC_DEN));
        int h = Mathf.RoundToInt(gm.mapHeight * (1f / SC_DEN));
        if (w < 1) return null;
        if (h < 1) return null;

        Color32[] buffer = new Color32[w * h];
        Dictionary<int, Color32[]> cache = new Dictionary<int, Color32[]>();
        if (rows < 1) return null;
        if (cols < 1) return null;

        int drawn = 0;
        int yBase = 0;
        int xBase = rows * OTW;
        for (int row = 0; row < rows; row++)
        {
            xBase -= OTW;
            int px = xBase;
            int py = yBase;
            int index = row * cols;
            for (int col = 0; col < cols; col++)
            {
                int code = gm.grids[index + col];
                if (code > -1)
                {
                    int iid = TerraShp.GetTile(code, 0);
                    if (iid > -1)
                    {
                        Color32[] small = GetSmall(iid, cache);
                        Blit(buffer, w, h, small,
                            Mathf.RoundToInt(px * (1f / SC_DEN)),
                            Mathf.RoundToInt(py * (1f / SC_DEN)));
                        drawn++;
                    }
                }
                px += OTW;
                py += OTH;
            }
            yBase += OTH;
        }

        if (drawn == 0) return null;

        var texture = new Texture2D(w, h, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color32[] flipped = new Color32[w * h];
        int src = 0;
        int dst = w * (h - 1);
        for (int y = 0; y < h; y++)
        {
            Array.Copy(buffer, src, flipped, dst, w);
            src += w;
            dst -= w;
        }
        texture.SetPixels32(flipped);
        texture.Apply(false);
        return texture;
    }

    // 06000be2: one 4x4 box of the decoded tile collapses to one small pixel;
    // transparent source pixels are left out of the average, and a box with no
    // opaque pixel stays fully transparent.
    private static Color32[] GetSmall(int iid, Dictionary<int, Color32[]> cache)
    {
        Color32[] small;
        if (cache.TryGetValue(iid, out small)) return small;

        Color32[] big = TileImgLib.DecodeTile(iid);
        small = new Color32[SMALL_W * SMALL_H];
        for (int y = 0; y < SMALL_H; y++)
        {
            for (int x = 0; x < SMALL_W; x++)
            {
                int r = 0;
                int g = 0;
                int b = 0;
                int n = 0;
                for (int dy = 0; dy < SC_DEN; dy++)
                {
                    int rowBase = (y * SC_DEN + dy) * TILE_W + x * SC_DEN;
                    for (int dx = 0; dx < SC_DEN; dx++)
                    {
                        Color32 c = big[rowBase + dx];
                        if (c.a != 0)
                        {
                            r += c.r;
                            g += c.g;
                            b += c.b;
                            n++;
                        }
                    }
                }
                if (n == 0) small[y * SMALL_W + x] = new Color32(0, 0, 0, 0);
                else small[y * SMALL_W + x] = new Color32((byte)(r / n), (byte)(g / n), (byte)(b / n), 255);
            }
        }
        cache[iid] = small;
        return small;
    }

    // 06000be3: the small tile is stamped only where it lands inside the
    // composite and only for source pixels whose alpha reaches 8, which is the
    // baked comparison on both architectures.
    private static void Blit(Color32[] dst, int W, int H, Color32[] src, int bx, int by)
    {
        for (int dy = 0; dy < SMALL_H; dy++)
        {
            int ty = by + dy;
            if (ty < 0 || ty >= H) continue;
            for (int dx = 0; dx < SMALL_W; dx++)
            {
                int tx = bx + dx;
                if (tx < 0 || tx >= W) continue;
                Color32 c = src[dy * SMALL_W + dx];
                if (c.a >= 8) dst[ty * W + tx] = c;
            }
        }
    }
}

}
