using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 TilFloor (tokens 06000c6c-06000c76). Terrain shapes are read
// from the official package tile/<name>.til, downsampled 128 -> 32 per shape,
// composited into one RGBA32 texture and vertically flipped into SetPixels32.
// Order, channel mapping and every baked constant follow the frozen evidence:
// Build/ReferenceReverseGhidra-1.5.8/evidence/tilfloor-v1/tilfloor-evidence.json
public static class TilFloor
{
    private const int TILE = 128;

    private const int SHAPE_NUM = 15;

    private const int MAX_RANDOM = 4;

    private const int TS = 32;

    // Baked literal {1,2,4,8}: sha256 of the initializer blob is
    // 42183E5ADFB18A57973908BE5A4B787B3F64F97CEB3ACAF48CF1158D21B71C9A.
    private static readonly int[] DIRS = new int[] { 1, 2, 4, 8 };

    // 06000c6c: one texture per map; returns null when nothing was composited.
    public static Texture2D Build(PkgArchive pkg, GsmMap map)
    {
        if (pkg == null || map == null || !map.ok)
        {
            return null;
        }
        Dictionary<int, string> tileIni = LoadTileIni(pkg);
        if (!tileIni.ContainsKey(0))
        {
            tileIni[0] = "新手浅草";
        }
        Dictionary<string, Color32[][]> cache = new Dictionary<string, Color32[][]>();
        int cols = map.vw - 1;
        int rows = map.vh - 1;
        int width = cols * TS;
        int height = rows * TS;
        Color32[] buf = new Color32[height * width];
        Color32[] cell = new Color32[TS * TS];
        int drawn = 0;
        if (rows > 0 && cols > 0)
        {
            int rowAt = 0;
            for (int y = 0; y < rows; y++)
            {
                int colAt = rowAt;
                for (int x = 0; x < cols; x++)
                {
                    int[] corners = new int[]
                    {
                        map.VCode(x, y + 1),
                        map.VCode(x, y),
                        map.VCode(x + 1, y),
                        map.VCode(x + 1, y + 1)
                    };
                    if (corners[0] > -1 || corners[1] > -1 || corners[2] > -1 || corners[3] > -1)
                    {
                        int[] order = new int[] { 0, 1, 2, 3 };
                        Array.Sort(order, (a, b) =>
                            (corners[a] < 0 ? 999 : corners[a]) - (corners[b] < 0 ? 999 : corners[b]));
                        Array.Clear(cell, 0, cell.Length);
                        HashSet<int> seen = new HashSet<int>();
                        bool painted = false;
                        for (int i = 0; i < order.Length; i++)
                        {
                            int v = corners[order[i]];
                            if (v < 0 || seen.Contains(v))
                            {
                                continue;
                            }
                            seen.Add(v);
                            int mask = 0;
                            if (corners[0] == v)
                            {
                                mask |= DIRS[0];
                            }
                            if (corners[1] == v)
                            {
                                mask |= DIRS[1];
                            }
                            if (corners[2] == v)
                            {
                                mask |= DIRS[2];
                            }
                            if (corners[3] == v)
                            {
                                mask |= DIRS[3];
                            }
                            string name;
                            if (!tileIni.TryGetValue(v, out name))
                            {
                                continue;
                            }
                            Color32[][] shapes;
                            if (!cache.TryGetValue(name, out shapes))
                            {
                                shapes = LoadTil(pkg, name);
                                cache[name] = shapes;
                            }
                            if (shapes == null)
                            {
                                continue;
                            }
                            Color32[] shape = (mask == 0xf || seen.Count == 1)
                                ? shapes[SHAPE_NUM - 1]
                                : shapes[mask - 1];
                            if (shape == null)
                            {
                                shape = shapes[SHAPE_NUM - 1];
                            }
                            if (shape == null)
                            {
                                continue;
                            }
                            Over(cell, shape);
                            painted = true;
                        }
                        if (painted)
                        {
                            for (int line = 0; line < TS; line++)
                            {
                                Array.Copy(cell, line * TS, buf, colAt + line * width, TS);
                            }
                            drawn++;
                        }
                    }
                    colAt += TS;
                }
                rowAt += width * TS;
            }
        }
        if (drawn == 0)
        {
            return null;
        }
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        Color32[] pixels = new Color32[height * width];
        for (int line = 0; line < height; line++)
        {
            Array.Copy(buf, line * width, pixels, (height - 1 - line) * width, width);
        }
        tex.SetPixels32(pixels, 0);
        tex.Apply(false, false);
        return tex;
    }

    // 06000c6d: src is drawn over dst with per pixel alpha blending.
    private static void Over(Color32[] dst, Color32[] src)
    {
        for (int i = 0; i < dst.Length; i++)
        {
            int a = src[i].a;
            if (a == 0)
            {
                continue;
            }
            if (a == 255)
            {
                dst[i] = src[i];
                continue;
            }
            int inv = 255 - a;
            dst[i] = new Color32(
                (byte)((dst[i].r * inv + src[i].r * a) / 255),
                (byte)((dst[i].g * inv + src[i].g * a) / 255),
                (byte)((dst[i].b * inv + src[i].b * a) / 255),
                (byte)((dst[i].a * inv) / 255 + a));
        }
    }

    // 06000c6e: cache lookup that loads and stores the shape set on a miss.
    private static Color32[][] GetShapes(PkgArchive pkg, string name, Dictionary<string, Color32[][]> cache)
    {
        Color32[][] shapes;
        if (!cache.TryGetValue(name, out shapes))
        {
            shapes = LoadTil(pkg, name);
            cache[name] = shapes;
        }
        return shapes;
    }

    // 06000c6f: "ELIT" payload with 15 shape offsets; 316 or 324 byte header.
    private static Color32[][] LoadTil(PkgArchive pkg, string name)
    {
        byte[] d = pkg.Read("tile/" + name + ".til");
        if (d == null || d.Length < 0x10
            || d[0] != (byte)'E' || d[1] != (byte)'L' || d[2] != (byte)'I' || d[3] != (byte)'T')
        {
            return null;
        }
        int format = I32(d, 8);
        int total = I32(d, 0xc);
        int[] offsets = new int[SHAPE_NUM];
        for (int i = 0; i < SHAPE_NUM; i++)
        {
            offsets[i] = I32(d, 0x10 + i * 4);
        }
        int head = format > 0x1002 ? 0x144 : 0x13c;
        int span = TILE * TILE * 4;
        Color32[][] shapes = new Color32[SHAPE_NUM][];
        for (int i = 0; i < offsets.Length; i++)
        {
            Color32[] shape = null;
            if (offsets[i] >= 0 && offsets[i] < total)
            {
                int at = offsets[i] * span + head;
                if (at + span <= d.Length)
                {
                    shape = DownTile(d, at);
                }
            }
            shapes[i] = shape;
        }
        return shapes;
    }

    // 06000c70: 128x128 raw tile downsampled to 32x32 by a 4x4 alpha weighted box.
    // The stored sample is B,G,R,A: byte 0 feeds b, byte 1 feeds g, byte 2 feeds r.
    private static Color32[] DownTile(byte[] d, int p)
    {
        Color32[] cell = new Color32[TS * TS];
        int box = TILE / TS;
        for (int y = 0; y < TS; y++)
        {
            for (int x = 0; x < TS; x++)
            {
                long sb = 0;
                long sg = 0;
                long sr = 0;
                long sa = 0;
                for (int k = 0; k < box; k++)
                {
                    int at = p + (y * box + k) * TILE * 4 + x * box * 4;
                    for (int m = 0; m < box; m++)
                    {
                        long a = d[at + m * 4 + 3];
                        sb += d[at + m * 4] * a;
                        sg += d[at + m * 4 + 1] * a;
                        sr += d[at + m * 4 + 2] * a;
                        sa += a;
                    }
                }
                if (sa < 1)
                {
                    cell[y * TS + x] = default(Color32);
                }
                else
                {
                    cell[y * TS + x] = new Color32(
                        (byte)(sr / sa), (byte)(sg / sa), (byte)(sb / sa), (byte)(sa / 16));
                }
            }
        }
        return cell;
    }

    // 06000c71: UTF-16LE "tile/tile.ini"; "NNNN<name>" lines keyed by NNNN.
    private static Dictionary<int, string> LoadTileIni(PkgArchive pkg)
    {
        Dictionary<int, string> map = new Dictionary<int, string>();
        byte[] bytes = pkg.Read("tile/tile.ini");
        if (bytes != null)
        {
            string[] lines = Encoding.Unicode.GetString(bytes).TrimStart('\ufeff').Split('\n');
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length > 3
                    && line[0] >= '0' && line[0] <= '9'
                    && line[1] >= '0' && line[1] <= '9'
                    && line[2] >= '0' && line[2] <= '9'
                    && line[3] >= '0' && line[3] <= '9')
                {
                    map[int.Parse(line.Substring(0, 4))] = line.Substring(4).Trim();
                }
            }
        }
        return map;
    }

    // 06000c72: first n characters must all be ASCII digits.
    private static bool IsDigits(string s, int n)
    {
        for (int i = 0; i < n; i++)
        {
            if (s[i] < '0' || s[i] > '9')
            {
                return false;
            }
        }
        return true;
    }

    // 06000c73: little endian int32 at byte offset p.
    private static int I32(byte[] d, int p)
    {
        return d[p] | (d[p + 1] << 8) | (d[p + 2] << 16) | (d[p + 3] << 24);
    }
}

}
