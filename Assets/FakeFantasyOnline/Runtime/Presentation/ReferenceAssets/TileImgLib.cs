using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Original 1.5.8 TileImgLib, tokens 06000c77–06000c7b.
public static class TileImgLib
{
    public const int TW = 128, TH = 64, PXPERTILE = 4096, BYTESPERTILE = 16384, HEAD = 64;
    private static byte[] dat;
    private static int count;
    private static int curSet = -1;
    private static int[] coordX, coordY;
    private static readonly Dictionary<int, Color32[]> cache = new Dictionary<int, Color32[]>();

    public static bool Ready => dat != null;

    public static void Load(PkgArchive pkg, int setNum = 0)
    {
        if (pkg == null) return;
        if (dat != null && curSet == setNum) return;
        string path = setNum == 0 ? "tile/tileimg.dat" : string.Format("tile/tileimg{0:D2}.dat", setNum);
        byte[] bytes = pkg.Read(path);
        if (bytes == null)
        {
            if (ResLoader.I == null || ResLoader.I.Pkg24 == null) return;
            bytes = ResLoader.I.Pkg24.Read(path);
            if (bytes == null) return;
        }
        if (bytes.Length >= HEAD && bytes[0] == 'Q' && bytes[4] == 'T')
        {
            count = BitConverter.ToInt32(bytes, 28);
            dat = bytes;
            curSet = setNum;
            cache.Clear();
            if (coordX == null) BuildDiamondCoords();
        }
    }

    private static void BuildDiamondCoords()
    {
        byte[] mask = new byte[16384];
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 64; x++)
                mask[y * TW + x] = (byte)(x * 32 > 2047 - y * 64 ? 1 : 0);
        for (int y = 32; y < TH; y++)
            for (int x = 0; x < 64; x++)
                mask[y * TW + x] = mask[(63 - y) * TW + x];
        for (int y = 32; y < TH; y++)
            for (int x = 64; x < TW; x++)
                mask[y * TW + x] = (byte)(1 - mask[(y - 32) * TW + x - 64]);
        for (int y = 0; y < 32; y++)
            for (int x = 64; x < TW; x++)
                mask[y * TW + x] = (byte)(1 - mask[(y + 32) * TW + x - 64]);
        var xs = new List<int>(PXPERTILE);
        var ys = new List<int>(PXPERTILE);
        for (int y = 0; y < TH; y++)
            for (int x = 0; x < TW; x++)
                if (mask[y * TW + x] != 0)
                {
                    xs.Add(x);
                    ys.Add(y);
                }
        coordX = xs.ToArray();
        coordY = ys.ToArray();
    }

    public static Color32[] DecodeTile(int imgId)
    {
        if (cache.TryGetValue(imgId, out Color32[] cached)) return cached;
        var pixels = new Color32[TW * TH];
        if (imgId >= 0 && dat != null && imgId < count && coordX != null)
            for (int i = 0; i < coordX.Length; i++)
            {
                int offset = HEAD + imgId * BYTESPERTILE + i * 4;
                byte b = dat[offset], g = dat[offset + 1], r = dat[offset + 2], a = dat[offset + 3];
                pixels[coordY[i] * TW + coordX[i]] = new Color32(r, g, b, a);
            }
        cache[imgId] = pixels;
        return pixels;
    }
}

}
