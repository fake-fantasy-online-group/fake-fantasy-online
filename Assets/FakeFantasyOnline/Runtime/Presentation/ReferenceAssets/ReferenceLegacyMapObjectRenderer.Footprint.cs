using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceLegacyMapObjectRenderer
{
    // OCCLUSION-JZ-001 candidate: object .mask contains collision-cell centres,
    // not native occlusion instructions. Use its front contour as a 2D depth profile.
    // Borrow the original texture; only Sprite rectangles are created, never new pixels.
    private sealed class FootprintStrip
    {
        public Sprite Sprite;
        public int Left, Width, FrontY;
    }

    private readonly Dictionary<Texture2D, List<FootprintStrip>> footprintStrips =
        new Dictionary<Texture2D, List<FootprintStrip>>();

    private bool TryPlaceFootprintSlices(GameBootstrap receiver, ReferenceGsmObject value, Transform parent,
        float pixelsPerUnit, float worldWidth, float worldHeight,
        ReferenceMapObjectKind kind, int objectIndex, ResLoader.ObjSprite image)
    {
        if (!footprintStrips.TryGetValue(image.tex, out List<FootprintStrip> strips))
        {
            strips = CreateFootprintStrips(image, pixelsPerUnit);
            footprintStrips.Add(image.tex, strips); // Cache unsupported/missing masks too.
        }
        if (strips == null) return false;
        int scale = Math.Max(image.hdScale, 1);
        foreach (FootprintStrip strip in strips)
            PlaceSpriteCore(receiver, value, parent, pixelsPerUnit, worldWidth, worldHeight, kind, objectIndex, image.path, image,
                strip.Left, strip.Width, value.SourceY + strip.FrontY, false,
                strip.Sprite, strip.Left / scale);
        return true;
    }

    private List<FootprintStrip> CreateFootprintStrips(ResLoader.ObjSprite image, float pixelsPerUnit)
    {
        if (string.IsNullOrEmpty(image.path) ||
            !image.path.EndsWith(".gso", StringComparison.OrdinalIgnoreCase)) return null;
        string maskPath = image.path.Substring(0, image.path.Length - 4) + ".mask";
        byte[] data = resources.Pkg?.Read(maskPath);
        // Observed object-mask revision: 58-byte header, I32 count, count I32(x,y)
        // pairs, then count flag bytes. Deliberately reject other layouts/lattices.
        const int headerSize = 62;
        if (data == null || data.Length < headerSize || MaskInt(data, 0) != -1 ||
            MaskInt(data, 4) != 0x10002) return null;
        for (int i = 8; i < 58; i++) if (data[i] != 0) return null;
        int count = MaskInt(data, 58);
        if (count <= 0 || count > 1000000 || headerSize + 9L * count != data.Length)
            return null;

        const int cellSize = 10;
        const int halfCell = cellSize / 2;
        var frontByColumn = new SortedDictionary<int, int>();
        int flagsStart = headerSize + count * 8;
        for (int i = 0; i < count; i++)
        {
            int x = MaskInt(data, headerSize + i * 8);
            int y = MaskInt(data, headerSize + i * 8 + 4);
            if (x < halfCell || y < halfCell || x > 100000 || y > 100000 ||
                x % cellSize != halfCell || y % cellSize != halfCell) return null;
            // 0x81 is the blocked-cell value confirmed against map/qw0923.msk.
            // Unsupported flags are not silently interpreted as a depth boundary.
            if (data[flagsStart + i] != 0x81) return null;
            int column = x / cellSize;
            int front = y + halfCell;
            if (!frontByColumn.TryGetValue(column, out int old) || front > old)
                frontByColumn[column] = front;
        }
        int first = -1, last = -1;
        foreach (int column in frontByColumn.Keys)
        {
            if (first < 0) first = column;
            else if (column != last + 1) return null;
            last = column;
        }

        int scale = Math.Max(image.hdScale, 1);
        int logicalWidth = (image.tex.width + scale - 1) / scale;
        var strips = new List<FootprintStrip>();
        try
        {
            for (int start = 0; start < logicalWidth;)
            {
                int front = FrontAt(start);
                int end = start + 1;
                while (end < logicalWidth && FrontAt(end) == front) end++;
                int left = start * scale;
                int width = Math.Min(end * scale, image.tex.width) - left;
                Sprite sprite = Sprite.Create(image.tex,
                    new Rect(left, 0, width, image.tex.height),
                    new Vector2(0, 1), pixelsPerUnit * scale);
                strips.Add(new FootprintStrip { Sprite = sprite, Left = left,
                    Width = width, FrontY = front });
                start = end;
            }
            return strips;
        }
        catch
        {
            foreach (FootprintStrip strip in strips) DestroyOwned(strip.Sprite);
            throw;
        }

        int FrontAt(int localX)
        {
            // Mask coordinates belong to the untrimmed logical image, not HD texels.
            // Lateral overhangs use the nearest occupied column; no height is guessed.
            int column = (image.nLeft + localX) / cellSize;
            return frontByColumn[Math.Max(first, Math.Min(last, column))];
        }
    }

    private static int MaskInt(byte[] data, int offset) =>
        data[offset] | data[offset + 1] << 8 | data[offset + 2] << 16 | data[offset + 3] << 24;

    private void DisposeFootprintStrips()
    {
        foreach (List<FootprintStrip> strips in footprintStrips.Values)
            if (strips != null)
                foreach (FootprintStrip strip in strips) DestroyOwned(strip.Sprite);
        footprintStrips.Clear();
    }
}
}
