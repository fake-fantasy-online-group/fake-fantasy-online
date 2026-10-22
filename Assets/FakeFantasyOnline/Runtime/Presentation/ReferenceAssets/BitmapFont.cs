using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class BitmapFont
{
    private const int CELL = 12;
    private const char SepBar = '\uff5c';
    private static Texture2D atlas;
    private static ushort[] idx;
    private static byte[] wid;
    private static int cols;
    private static bool tried;
    private static Texture2D nameTagAtlas;
    private static ushort[] nameTagIndex;
    private static byte[] nameTagWidths;
    private static object nameTagIdentity;
    private static byte[] nameTagAlpha;

    public static bool Ready => atlas && idx != null;
    // Font metrics are replaced as a set by Ensure. Layout caches must not reuse
    // rows measured against a previous index table after a reload.
    internal static object LayoutIdentity => idx;

    // A map transition does not replace the font. A new atlas or metrics binding
    // does: discard the one cached alpha plane and invalidate existing name tags.
    internal static object NameTagIdentity
    {
        get
        {
            Ensure();
            RefreshNameTagBinding();
            return nameTagIdentity;
        }
    }

    private static void RefreshNameTagBinding()
    {
        if (nameTagIdentity != null && ReferenceEquals(nameTagAtlas, atlas) &&
            ReferenceEquals(nameTagIndex, idx) && ReferenceEquals(nameTagWidths, wid)) return;
        nameTagAtlas = atlas;
        nameTagIndex = idx;
        nameTagWidths = wid;
        nameTagAlpha = null;
        nameTagIdentity = new object();
    }

    private static byte[] NameTagAlpha()
    {
        RefreshNameTagBinding();
        if (nameTagAlpha != null) return nameTagAlpha;
        Color32[] pixels = atlas.GetPixels32();
        byte[] alpha = new byte[pixels.Length];
        for (int i = 0; i < pixels.Length; i++) alpha[i] = pixels[i].a;
        // Publish only a complete read. A failed read must be retried, not cached.
        nameTagAlpha = alpha;
        return alpha;
    }

    private static void Ensure()
    {
        if (Ready || tried) return;
        ResLoader loader = ResLoader.I;
        if (!loader) return;
        tried = true;
        atlas = loader.LoadUI("font/gbk12.png");
        byte[] bytes = loader.LoadUIBytes("font/gbk12.idx");
        if (bytes != null)
        {
            idx = new ushort[bytes.Length / 2];
            Buffer.BlockCopy(bytes, 0, idx, 0, bytes.Length);
        }
        wid = loader.LoadUIBytes("font/gbk12.wid");
        if (atlas)
        {
            cols = atlas.width / CELL;
            atlas.filterMode = FilterMode.Point;
        }
        RefreshNameTagBinding();
        if (!Ready) tried = false;
    }

    private static int CellOf(char c)
    {
        if (idx == null || c >= idx.Length) return -1;
        int cell = idx[c];
        return cell == ushort.MaxValue ? -1 : cell;
    }

    private static float Adv(int cell)
    {
        if (cell < 0 || wid == null || cell >= wid.Length) return CELL;
        return wid[cell];
    }

    public static float Measure(string s, float scale)
        => Measure(s, 0, s == null ? 0 : s.Length, scale);

    internal static float Measure(string s, int start, int length, float scale)
    {
        Ensure();
        if (!Ready || string.IsNullOrEmpty(s)) return 0f;
        float width = 0f;
        for (int index = start; index < start + length; index++)
        {
            char c = s[index];
            width += (c == ' ' ? 6f : Adv(CellOf(c))) * scale;
        }
        return width;
    }

    public static void Draw(float px, float py, string s, Color col, float scale)
        => Draw(px, py, s, 0, s == null ? 0 : s.Length, col, scale);

    internal static void Draw(float px, float py, string s, int start, int length, Color col, float scale)
    {
        Ensure();
        if (!Ready || string.IsNullOrEmpty(s)) return;
        px = Mathf.Round(px);
        py = Mathf.Round(py);
        int aw = atlas.width;
        int ah = atlas.height;
        Color previous = GUI.color;
        GUI.color = col;
        for (int index = start; index < start + length; index++)
        {
            char c = s[index];
            if (c == ' ')
            {
                px += 6f * scale;
                continue;
            }
            int cell = CellOf(c);
            if (cell >= 0)
            {
                int column = cell % cols;
                int row = cell / cols;
                GUI.DrawTextureWithTexCoords(
                    new Rect(px, py, CELL * scale, CELL * scale), atlas,
                    new Rect(column * CELL / (float)aw,
                        1f - (row + 1) * CELL / (float)ah,
                        CELL / (float)aw, CELL / (float)ah));
            }
            px += Adv(cell) * scale;
        }
        GUI.color = previous;
    }

    public static void DrawOutline(float px, float py, string s, Color fill, float scale)
        => DrawOutline(px, py, s, 0, s == null ? 0 : s.Length, fill, scale);

    internal static void DrawOutline(float px, float py, string s, int start, int length, Color fill, float scale)
    {
        if (!Ready)
        {
            Ensure();
            if (!Ready) return;
        }
        px = Mathf.Round(px);
        py = Mathf.Round(py);
        float offset = Mathf.Max(1f, scale);
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                if (x != 0 || y != 0)
                    Draw(px + x * offset, py + y * offset, s, start, length, Color.black, scale);
        Draw(px, py, s, start, length, fill, scale);
    }

    public static Texture2D BakeNameTag(string name, string title, Color nameCol, Color titleCol,
        int scale, string[] topLines = null, Color? topCol = null, string bottomLine = null,
        Color? bottomCol = null, Texture2D nameIcon = null)
    {
        Ensure();
        if (!Ready || string.IsNullOrEmpty(name)) return null;
        if (scale < 1) scale = 1;
        bool hasTitle = !string.IsNullOrEmpty(title);
        bool hasBottom = !string.IsNullOrEmpty(bottomLine);
        int iconW = nameIcon ? (int)Math.Round(TexScale.W(nameIcon)) * scale : 0;
        int iconH = nameIcon ? (int)Math.Round(TexScale.H(nameIcon)) * scale : 0;
        int lineH = CELL * scale;
        int iconGap = nameIcon ? 2 * scale : 0;
        int extra = Mathf.Max(0, iconH - lineH);
        float nameMeasure = Measure(name, 1f);
        int titleW = hasTitle ? (int)Math.Ceiling(Measure(title, 1f)) * scale : 0;
        int bottomW = hasBottom ? (int)Math.Ceiling(Measure(bottomLine, 1f)) * scale : 0;
        int topCount = topLines == null ? 0 : topLines.Length;
        int topW = 0;
        for (int i = 0; i < topCount; i++)
            topW = Mathf.Max(topW, (int)Math.Ceiling(Measure(topLines[i], 1f)) * scale);
        int contentW = (int)Math.Ceiling(nameMeasure) * scale + iconW + iconGap;
        contentW = Mathf.Max(contentW, titleW);
        contentW = Mathf.Max(contentW, topW);
        contentW = Mathf.Max(contentW, bottomW);
        int W = contentW + 2 * scale;
        if ((W & 1) != 0) W++;
        int H = (topCount + 1 + (hasTitle ? 1 : 0) + (hasBottom ? 1 : 0)) * lineH + 2 * scale + extra;
        Color32[] buf = new Color32[W * H];
        byte[] ap = NameTagAlpha();
        int aw = atlas.width;
        int ah = atlas.height;

        int displayTop = scale + extra / 2;
        Color topFill = topCol ?? new Color(1f, 0.45f, 0.25f, 1f);
        for (int i = 0; i < topCount; i++)
        {
            Line(topLines[i], displayTop, topFill, -1);
            displayTop += lineH;
        }
        if (hasTitle)
        {
            Line(title, displayTop, titleCol, -1);
            displayTop += lineH;
        }
        int forceNameX = -1;
        if (nameIcon)
        {
            int nameW = (int)Math.Ceiling(Measure(name, 1f)) * scale;
            int iconX = (W - (nameW + iconW + iconGap)) / 2;
            Icon(nameIcon, iconX, displayTop - (iconH - lineH) / 2);
            forceNameX = iconX + iconW + iconGap;
        }
        Line(name, displayTop, nameCol, forceNameX);
        if (hasBottom)
            Line(bottomLine, displayTop + lineH, bottomCol ?? new Color(0.7f, 0.9f, 1f, 1f), -1);

        Texture2D texture = new Texture2D(W, H, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(buf);
        texture.Apply(false, false);
        return texture;

        void Line(string s, int dispTop, Color fill, int xForce)
        {
            float measured = Measure(s, 1f);
            int x0 = xForce >= 0 ? xForce : (W - (int)Math.Ceiling(measured) * scale) / 2;
            Pass(scale, scale, new Color32(0, 0, 0, 255));
            Pass(0, 0, fill);

            void Pass(int offx, int offy, Color32 col)
            {
                int cursor = x0;
                foreach (char c in s)
                {
                    if (c == ' ')
                    {
                        cursor += 6 * scale;
                        continue;
                    }
                    int cell = CellOf(c);
                    if (cell < 0)
                    {
                        cursor += CELL * scale;
                        continue;
                    }
                    if (c == SepBar)
                    {
                        int advance = (int)Adv(cell) * scale;
                        int barX = cursor + (advance - scale) / 2 + offx;
                        for (int y = 0; y < CELL; y++)
                        {
                            for (int sy = 0; sy < scale; sy++)
                            {
                                for (int sx = 0; sx < scale; sx++)
                                {
                                    int x = barX + sx;
                                    int topY = dispTop + y * scale + sy + offy;
                                    if (x < 0 || x >= W || topY < 0 || topY >= H) continue;
                                    int at = (H - 1 - topY) * W + x;
                                    if (col.a >= buf[at].a) buf[at] = col;
                                }
                            }
                        }
                        cursor += advance;
                        continue;
                    }
                    int sourceX = cell % cols * CELL;
                    int sourceTop = cell / cols * CELL;
                    for (int y = 0; y < CELL; y++)
                    {
                        for (int x = 0; x < CELL; x++)
                        {
                            byte sourceAlpha = ap[(ah - 1 - sourceTop - y) * aw + sourceX + x];
                            if (sourceAlpha < 8) continue;
                            for (int sy = 0; sy < scale; sy++)
                            {
                                for (int sx = 0; sx < scale; sx++)
                                {
                                    int dx = cursor + x * scale + sx + offx;
                                    int dy = dispTop + y * scale + sy + offy;
                                    if (dx < 0 || dx >= W || dy < 0 || dy >= H) continue;
                                    int at = (H - 1 - dy) * W + dx;
                                    byte alpha = (byte)(sourceAlpha * col.a / 255);
                                    if (alpha >= buf[at].a)
                                        buf[at] = new Color32(col.r, col.g, col.b, alpha);
                                }
                            }
                        }
                    }
                    cursor += (int)Adv(cell) * scale;
                }
            }
        }

        void Icon(Texture2D t, int dx0, int dispTop)
        {
            Color32[] pixels = t.GetPixels32();
            int width = t.width;
            int height = t.height;
            int hd = TexScale.Of(t);
            int logicalHeight = height / hd;
            int logicalWidth = width / hd;
            if (logicalHeight < 1 || logicalWidth < 1) return;
            for (int y = 0; y < logicalHeight; y++)
            {
                for (int x = 0; x < logicalWidth; x++)
                {
                    Color32 source = pixels[(height - 1 - y * hd) * width + x * hd];
                    if (source.a < 8) continue;
                    for (int sy = 0; sy < scale; sy++)
                    {
                        for (int sx = 0; sx < scale; sx++)
                        {
                            int dx = dx0 + x * scale + sx;
                            int dy = dispTop + y * scale + sy;
                            if (dx < 0 || dy < 0 || dx >= W || dy >= H) continue;
                            buf[(H - 1 - dy) * W + dx] = source;
                        }
                    }
                }
            }
        }
    }

    public static List<string> Wrap(string s, float maxW, float scale)
    {
        List<string> lines = new List<string>();
        if (string.IsNullOrEmpty(s))
        {
            lines.Add(string.Empty);
            return lines;
        }
        if (maxW <= 0f)
        {
            lines.Add(s);
            return lines;
        }
        foreach (string paragraph in s.Split('\n'))
        {
            StringBuilder line = new StringBuilder();
            foreach (char c in paragraph)
            {
                if (line.Length > 0 && Measure(line.ToString() + c, scale) > maxW)
                {
                    lines.Add(line.ToString());
                    line.Clear();
                }
                line.Append(c);
            }
            lines.Add(line.ToString());
        }
        return lines;
    }

    public static string Elide(string s, float maxW, float scale)
    {
        if (string.IsNullOrEmpty(s) || maxW <= 0f) return s ?? string.Empty;
        if (Measure(s, scale) <= maxW) return s;
        while (s.Length >= 2 && Measure(s + "…", scale) > maxW)
            s = s.Substring(0, s.Length - 1);
        return s + "…";
    }

    public static void CenterOutline(Rect r, string s, Color fill, float scale)
    {
        if (string.IsNullOrEmpty(s)) return;
        float width = Measure(s, scale);
        DrawOutline(r.x + (r.width - width) * 0.5f,
            r.y + (r.height - CELL * scale) * 0.5f, s, fill, scale);
    }

    public static void CenterOutlineBold(Rect r, string s, Color fill, float scale)
    {
        if (string.IsNullOrEmpty(s)) return;
        float width = Measure(s, scale);
        float x = Mathf.Round(r.x + (r.width - width) * 0.5f);
        float y = Mathf.Round(r.y + (r.height - CELL * scale) * 0.5f);
        DrawOutline(x, y, s, fill, scale);
        Draw(x + Mathf.Max(1f, scale), y, s, fill, scale);
    }

    internal static void EnsureForIndependentRuntime() => Ensure();
    internal static int CellOfForIndependentRuntime(char c) => CellOf(c);
    internal static float AdvForIndependentRuntime(int cell) => Adv(cell);
}

}
