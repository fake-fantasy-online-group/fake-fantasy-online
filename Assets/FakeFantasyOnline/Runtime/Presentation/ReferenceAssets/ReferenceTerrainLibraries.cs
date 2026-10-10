using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned decoder for the tile/tileimg*.dat revision used by the locked
/// reference client.
/// </summary>
/// <remarks>
/// This is a bounded translation of TileImgLib.Load (06000AEE),
/// BuildDiamondCoords (06000AEF), and DecodeTile (06000AF0). The archive bytes
/// are BGRA; decoded Unity pixels are RGBA in a 128 by 64 transparent surface.
/// </remarks>
public sealed class ReferenceTileImageLibrary
{
    public const int TileWidth = 128;
    public const int TileHeight = 64;
    public const int PixelsPerTile = 4096;
    public const int BytesPerTile = 16384;
    public const int HeaderSize = 64;

    private const int SupportedVersion = 65536;
    private const int SupportedFormat = 1;

    private static readonly int[] DiamondX;
    private static readonly int[] DiamondY;

    private readonly byte[] data;
    private readonly Dictionary<int, Color32[]> cache;

    static ReferenceTileImageLibrary()
    {
        BuildDiamondCoordinates(out DiamondX, out DiamondY);
    }

    private ReferenceTileImageLibrary(byte[] data, int count)
    {
        this.data = data;
        Count = count;
        cache = new Dictionary<int, Color32[]>();
    }

    public int Count { get; }

    public static string ResourcePathForSet(int tileSet)
    {
        return tileSet == 0
            ? "tile/tileimg.dat"
            : "tile/tileimg" + tileSet.ToString("D2") + ".dat";
    }

    public static IReadOnlyList<int> CoordinateX => DiamondX;

    public static IReadOnlyList<int> CoordinateY => DiamondY;

    public static bool TryParse(
        byte[] data,
        out ReferenceTileImageLibrary library,
        out ReferenceTileImageParseFailure failure)
    {
        library = null;
        failure = ReferenceTileImageParseFailure.None;
        if (data == null || data.Length < HeaderSize)
        {
            failure = ReferenceTileImageParseFailure.TruncatedHeader;
            return false;
        }

        if (data[0] != (byte)'Q' || data[1] != (byte)'Q' ||
            data[4] != (byte)'T' || data[5] != (byte)'I' ||
            data[6] != (byte)'L' || data[7] != (byte)'E')
        {
            failure = ReferenceTileImageParseFailure.InvalidSignature;
            return false;
        }

        int version = ReadInt32(data, 8);
        int headerSize = ReadInt32(data, 12);
        int width = ReadInt32(data, 16);
        int height = ReadInt32(data, 20);
        int format = ReadInt32(data, 24);
        int count = ReadInt32(data, 28);
        if (version != SupportedVersion || headerSize != HeaderSize ||
            width != TileWidth || height != TileHeight ||
            format != SupportedFormat || count < 1)
        {
            failure = ReferenceTileImageParseFailure.UnsupportedHeader;
            return false;
        }

        long expectedLength = HeaderSize + (long)count * BytesPerTile;
        if (expectedLength != data.Length)
        {
            failure = ReferenceTileImageParseFailure.InvalidPayloadLength;
            return false;
        }

        library = new ReferenceTileImageLibrary(data, count);
        return true;
    }

    public bool TryDecodeTile(int imageId, out Color32[] pixels)
    {
        pixels = null;
        if (imageId < 0 || imageId >= Count)
        {
            return false;
        }

        if (cache.TryGetValue(imageId, out pixels))
        {
            return true;
        }

        pixels = new Color32[TileWidth * TileHeight];
        int sourceOffset = HeaderSize + imageId * BytesPerTile;
        for (int index = 0; index < PixelsPerTile; index++)
        {
            int source = sourceOffset + index * 4;
            int destination = DiamondX[index] + DiamondY[index] * TileWidth;
            pixels[destination] = new Color32(
                data[source + 2],
                data[source + 1],
                data[source],
                data[source + 3]);
        }

        cache[imageId] = pixels;
        return true;
    }

    private static void BuildDiamondCoordinates(out int[] x, out int[] y)
    {
        x = new int[PixelsPerTile];
        y = new int[PixelsPerTile];
        int cursor = 0;
        for (int row = 0; row < TileHeight; row++)
        {
            // TileImgLib.BuildDiamondCoords (06000AEF / 0x2c7078) produces two
            // equally wide center rows. Its native/runtime coordinate sequence starts
            // 2,6,...,126,126,...,6,2; the former 0,4,...,128,...,4 sequence was a
            // recovery error that kept the count at 4096 while shifting every source pixel.
            int first = row < TileHeight / 2
                ? TileWidth / 2 - row * 2
                : (row - (TileHeight / 2 - 1)) * 2;
            int width = row < TileHeight / 2
                ? row * 4 + 2
                : (TileHeight - row) * 4 - 2;
            int last = first + width;
            for (int column = first; column < last; column++)
            {
                x[cursor] = column;
                y[cursor] = row;
                cursor++;
            }
        }

        if (cursor != PixelsPerTile)
        {
            throw new InvalidOperationException(
                "Recovered tile diamond did not contain exactly 4096 pixels.");
        }
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return bytes[offset] |
               bytes[offset + 1] << 8 |
               bytes[offset + 2] << 16 |
               bytes[offset + 3] << 24;
    }
}

public enum ReferenceTileImageParseFailure
{
    None = 0,
    TruncatedHeader = 1,
    InvalidSignature = 2,
    UnsupportedHeader = 3,
    InvalidPayloadLength = 4
}

/// <summary>
/// Source-owned reader for the tile/terrashp*.dat lookup table used by the
/// locked reference client.
/// </summary>
/// <remarks>
/// This is a bounded translation of TerraShp.Load (06000AD4), Count
/// (06000AD6), ImgId (06000AD7), and GetTile (06000AD8).
/// </remarks>
public sealed class ReferenceTerrainShapeLibrary
{
    public const int HeaderSize = 48;
    public const int RecordStride = 1044;
    public const int TerrainCount = 544;
    public const int CountOffset = 12;
    public const int VariantsOffset = 20;
    public const int FramesPerVariant = 4;
    public const int MaximumVariants = 64;

    private const int SupportedVersion = 65537;

    private readonly byte[] data;

    public static string ResourcePathForSet(int tileSet)
    {
        return tileSet == 0
            ? "tile/terrashp.dat"
            : "tile/terrashp" + tileSet.ToString("D2") + ".dat";
    }

    private ReferenceTerrainShapeLibrary(byte[] data)
    {
        this.data = data;
    }

    public static bool TryParse(
        byte[] data,
        out ReferenceTerrainShapeLibrary library,
        out ReferenceTerrainShapeParseFailure failure)
    {
        library = null;
        failure = ReferenceTerrainShapeParseFailure.None;
        if (data == null || data.Length < HeaderSize)
        {
            failure = ReferenceTerrainShapeParseFailure.TruncatedHeader;
            return false;
        }

        if (data[0] != (byte)'R' || data[1] != (byte)'A' ||
            data[2] != (byte)'Y' || data[3] != (byte)'S' ||
            data[4] != (byte)'T' || data[5] != (byte)'R' ||
            data[6] != (byte)'S' || data[7] != (byte)'P')
        {
            failure = ReferenceTerrainShapeParseFailure.InvalidSignature;
            return false;
        }

        if (ReadInt32(data, 8) != SupportedVersion ||
            ReadInt32(data, 12) != HeaderSize ||
            ReadInt32(data, 16) != TerrainCount)
        {
            failure = ReferenceTerrainShapeParseFailure.UnsupportedHeader;
            return false;
        }

        if (data.Length != HeaderSize + TerrainCount * RecordStride)
        {
            failure = ReferenceTerrainShapeParseFailure.InvalidPayloadLength;
            return false;
        }

        for (int terrain = 0; terrain < TerrainCount; terrain++)
        {
            int count = ReadInt32(data, RecordOffset(terrain) + CountOffset);
            if (count < 0 || count > MaximumVariants)
            {
                failure = ReferenceTerrainShapeParseFailure.InvalidVariantCount;
                return false;
            }
        }

        library = new ReferenceTerrainShapeLibrary(data);
        return true;
    }

    public int Count(int terrain)
    {
        if (terrain < 0 || terrain >= TerrainCount)
        {
            return 0;
        }

        return ReadInt32(data, RecordOffset(terrain) + CountOffset);
    }

    public int ImageId(int terrain, int variant, int frame)
    {
        if (terrain < 0 || terrain >= TerrainCount ||
            variant < 0 || variant >= MaximumVariants ||
            frame < 0 || frame >= FramesPerVariant)
        {
            return -1;
        }

        int offset = RecordOffset(terrain) + VariantsOffset +
                     variant * FramesPerVariant * 4 + frame * 4;
        return ReadInt32(data, offset);
    }

    public int GetTile(int code, int frame)
    {
        if (code < 0)
        {
            return -1;
        }

        int terrain;
        if ((code & 267386880) == 0)
        {
            terrain = (code >> 8) & 0xff;
        }
        else
        {
            int encodedTerrain = (code >> 16) & 0xfff;
            if ((encodedTerrain & 0xff0) > 0x20f)
            {
                return -1;
            }

            terrain = encodedTerrain + 16;
        }

        int count = Count(terrain);
        if (count < 1)
        {
            return -1;
        }

        int variant = code & 0xff;
        if (variant >= count)
        {
            variant = 0;
        }

        int selectedFrame = frame >= 0 && frame < FramesPerVariant ? frame : 0;
        return ImageId(terrain, variant, selectedFrame);
    }

    private static int RecordOffset(int terrain)
    {
        return HeaderSize + terrain * RecordStride;
    }

    private static int ReadInt32(byte[] bytes, int offset)
    {
        return bytes[offset] |
               bytes[offset + 1] << 8 |
               bytes[offset + 2] << 16 |
               bytes[offset + 3] << 24;
    }
}

public enum ReferenceTerrainShapeParseFailure
{
    None = 0,
    TruncatedHeader = 1,
    InvalidSignature = 2,
    UnsupportedHeader = 3,
    InvalidPayloadLength = 4,
    InvalidVariantCount = 5
}

}
