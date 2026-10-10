using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 map owner. The compatibility adapter projects this instance
// for existing renderers without interpreting the input bytes again.
public partial class GsmMap
{
    public struct Obj { public int id, sx, sy, xl, yl, py, pz; }
    public struct NakedImg { public int w, h, pitch, attrib; public byte[] data; }
    public struct FixFrame { public int xOff, yOff; public NakedImg img; }
    public sealed class FixObj
    {
        public int sx, sy, xl, yl, frames, clipWidth, clipCount, flags, effect;
        public int[] clipBy;
        public FixFrame[] frameImgs;
    }

    internal int mapWidth, mapHeight, vw, vh, tileSet;
    internal uint version, flags;
    internal int[] grids;
    internal bool ok, isLegacy, objsOk, hasCover, hasFieldColor;
    internal readonly List<Obj> tileObjs;
    internal readonly List<Obj> libObjs;
    internal readonly List<FixObj> fixObjs;
    internal int coverPw, coverPh, coverBw, coverBh, coverCx, coverCy;
    internal NakedImg[] coverImgs;
    internal int fcSx, fcSy, fcW, fcH, fcTileW, fcTileH;
    internal uint[] fcColors;
    internal bool hasSky;
    internal uint skyColor;
    internal int cloudType, cloudDensity, cloudDir, cloudFdir, cloudSpeed;
    // Adapter diagnostics; native decisions do not depend on these positions.
    internal int legacyFieldOffset = -1, parsedLength, objectSectionEnd, sourceLength;

    public GsmMap()
    {
        tileObjs = new List<Obj>();
        libObjs = new List<Obj>();
        fixObjs = new List<FixObj>();
    }

    // 06000bcc: ordinary truncation returns the constructed, possibly partial owner.
    public static GsmMap Parse(byte[] data)
    {
        var map = new GsmMap { sourceLength = data == null ? 0 : data.Length };
        if (data == null || data.Length < 0x6c) return map;
        map.version = U32(data, 0x38);
        map.flags = U32(data, 0x40);
        map.mapWidth = I32(data, 0x44);
        map.mapHeight = I32(data, 0x48);
        int objectStart = I32(data, 0x3c);
        int fixCount = I32(data, 0x4c);
        int objCount = I32(data, 0x50);
        int sourceTileSet = I32(data, 0x68);
        map.tileSet = sourceTileSet < 0 || sourceTileSet > 13 ? 0 : sourceTileSet;
        map.ParseObjects(data, objectStart, fixCount, objCount);

        int reportedWidth = I32(data, 0x60), reportedHeight = I32(data, 0x64);
        // Official metadata usage 13120 is StringLiteral:PMIT.
        int marker = Find(data, "PMIT", 0);
        if (marker < 0 || reportedWidth < 1 || reportedHeight < 1)
        {
            if (reportedWidth < 1 || reportedHeight < 1 ||
                reportedWidth > 4095 || reportedHeight > 4095) return map;
            int count = unchecked(reportedWidth * reportedHeight);
            int fieldSize = unchecked(16 + count * 12);
            int found = FindLegacyField(data, reportedWidth, reportedHeight, objectStart, fieldSize);
            if (found < 0) return map;
            map.legacyFieldOffset = found;
            map.vw = reportedWidth;
            map.vh = reportedHeight;
            map.grids = new int[count];
            int cursor = unchecked(found + 16);
            for (int i = 0; i < count; i++, cursor = unchecked(cursor + 12))
            {
                int kind = data[cursor] & 0xf0;
                map.grids[i] = kind == 0x10 || kind == 0x20 ? -1 : I32(data, cursor + 4);
            }
            map.ok = true;
            map.isLegacy = true;
            map.parsedLength = cursor;
            if (map.version >= 0x10004 && (map.flags & 0x100) != 0 &&
                data.Length >= unchecked(cursor + 24))
            {
                map.skyColor = U32(data, cursor);
                map.cloudType = I32(data, cursor + 4);
                map.cloudDensity = I32(data, cursor + 8);
                map.cloudDir = I32(data, cursor + 12);
                map.cloudFdir = I32(data, cursor + 16);
                map.cloudSpeed = I32(data, cursor + 20);
                map.hasSky = true;
                map.parsedLength = unchecked(cursor + 24);
            }
            return map;
        }

        int width = unchecked(reportedWidth + 1), height = unchecked(reportedHeight + 1);
        int byteCount = I32(data, marker + 4);
        if (byteCount > 0 && (byteCount & 3) == 0)
        {
            int declaredCount = byteCount / 4;
            if (declaredCount != unchecked(width * height))
            {
                if (height != 0 && declaredCount % height == 0) width = declaredCount / height;
                else if (width != 0 && declaredCount % width == 0) height = declaredCount / width;
            }
        }
        int gridCount = unchecked(width * height);
        int modernEnd = unchecked(marker + 8 + gridCount * 4);
        if (data.Length < modernEnd) return map;
        map.vw = width;
        map.vh = height;
        map.grids = new int[gridCount];
        for (int i = 0; i < gridCount; i++)
        {
            uint value = U32(data, unchecked(marker + 8 + i * 4));
            map.grids[i] = value >= 1 && value <= 255 ? unchecked((int)(value << 8)) : -1;
        }
        map.ok = true;
        map.parsedLength = modernEnd;
        return map;
    }

    // 06000bce: flags become true only at their native completion points.
    private void ParseObjects(byte[] data, int cursor, int fixCount, int objCount)
    {
        objectSectionEnd = cursor;
        if (version <= 0x1ffff && (flags & 1) != 0)
        {
            if (data.Length < unchecked(cursor + 16)) return;
            cursor = unchecked(cursor + 16 + I32(data, cursor) * I32(data, cursor + 4));
            objectSectionEnd = cursor;
        }
        if ((flags & 2) != 0)
        {
            if (!ParseCover(data, ref cursor)) return;
            hasCover = true;
            objectSectionEnd = cursor;
        }
        if (!ParseFixedObjects(data, ref cursor, fixCount)) return;
        cursor = unchecked(cursor + (int)(flags & 4));
        objectSectionEnd = cursor;
        if (version <= 0x20000 && (flags & 0x10) != 0)
        {
            if (data.Length < unchecked(cursor + 4)) return;
            cursor = unchecked(cursor + 4 + I32(data, cursor) * 396);
            objectSectionEnd = cursor;
        }
        if (version > 0x10008)
        {
            if (data.Length < unchecked(cursor + 4)) return;
            int count = I32(data, cursor);
            cursor = unchecked(cursor + (version >= 0x20004 ? 8 : 4));
            objectSectionEnd = cursor;
            for (int i = 0; i < count; i++)
            {
                if (!ReadObj(data, ref cursor, version, out Obj obj)) return;
                tileObjs.Add(obj);
                objectSectionEnd = cursor;
            }
        }
        if (objCount > 0 && version > 0x10001 && (flags & 0x80) != 0)
        {
            for (int i = 0; i < objCount; i++)
            {
                if (!ReadObj(data, ref cursor, version, out Obj obj)) return;
                libObjs.Add(obj);
                objectSectionEnd = cursor;
            }
        }
        if (version > 0x20003 && (flags & 0x1000) != 0)
        {
            ParseFieldColor(data, cursor);
        }
        objsOk = true;
        objectSectionEnd = cursor;
    }

    public int VCode(int x, int y)
    {
        if ((x | y) < 0 || x >= vw || y >= vh)
            return -1;

        int value = grids[x + vw * y];
        if (value < 0)
            return -1;

        uint encoded = unchecked((uint)value);
        int code = (int)((encoded >> 8) & 0xff);
        if (code == 0)
        {
            code = (int)((encoded >> 16) & 0x0f);
            if (code == 0)
                code = (int)((encoded >> 20) & 0xff);
        }

        return code;
    }

    // 06000bd4/06000bd5: direct indexing retains native exception paths.
    private static uint U32(byte[] data, int offset) =>
        (uint)(data[offset] | data[offset + 1] << 8 |
               data[offset + 2] << 16 | data[offset + 3] << 24);
    private static int I32(byte[] data, int offset) =>
        data[offset] | data[offset + 1] << 8 |
        data[offset + 2] << 16 | data[offset + 3] << 24;

    // 06000bd3.
    private static int Find(byte[] data, string marker, int from)
    {
        char first = marker[0], second = marker[1], third = marker[2], fourth = marker[3];
        for (int i = from; i <= data.Length - 4; i++)
            if (data[i] == first && data[i + 1] == second &&
                data[i + 2] == third && data[i + 3] == fourth) return i;
        return -1;
    }

    // 06000bcd.
    private static int FindLegacyField(byte[] data, int width, int height, int from, int size)
    {
        int probeCount = (int)Math.Min(16L, (long)width * height);
        for (int candidate = from; candidate <= data.Length - size; candidate++)
        {
            if (I32(data, candidate) != width || I32(data, candidate + 4) != height) continue;
            bool accepted = true;
            for (int p = candidate + 24, n = 0; n < probeCount; n++, p += 12)
                if (U32(data, p) != 0) { accepted = false; break; }
            if (accepted) return candidate;
        }
        return -1;
    }
}

}
