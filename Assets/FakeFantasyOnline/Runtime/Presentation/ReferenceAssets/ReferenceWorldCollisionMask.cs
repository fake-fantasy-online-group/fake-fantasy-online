using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public enum ReferenceWorldCollisionMaskFailure
{
    None = 0,
    TruncatedHeader = 1,
    InvalidMagic = 2,
    InvalidDimensions = 3,
    InvalidCellPayload = 4,
}

/// <summary>
/// Source-owned translation of the bounded QQF MASK branch used by the first world.
/// </summary>
/// <remarks>
/// The parser preserves PlayerController.LoadMask's trailing-cell offset and ten-pixel cell-size
/// fallback. Cell and line queries preserve the hash-locked CellWalkable and LineWalkable
/// semantics. Dynamic bridge overrides, missing-mask fail-open behavior, path search and Transform
/// movement remain outside this boundary.
/// </remarks>
public sealed class ReferenceWorldCollisionMask
{
    public const int HeaderSize = 32;
    public const int DefaultCellSizePixels = 10;
    public const byte BlockedBit = 0x80;

    private static readonly byte[] Magic =
    {
        (byte)'Q', (byte)'Q', (byte)'F', 0x1a,
        (byte)'M', (byte)'A', (byte)'S', (byte)'K',
    };

    private readonly byte[] sourceBytes;

    private ReferenceWorldCollisionMask(
        byte[] sourceBytes,
        int cellDataOffset,
        int width,
        int height,
        int cellWidthPixels,
        int cellHeightPixels)
    {
        this.sourceBytes = sourceBytes;
        CellDataOffset = cellDataOffset;
        Width = width;
        Height = height;
        CellWidthPixels = cellWidthPixels;
        CellHeightPixels = cellHeightPixels;
    }

    // Optional diagnostic view of already decoded native state; no validation or byte copy.
    internal static ReferenceWorldCollisionMask FromDecodedForIndependentRuntime(byte[] data,
        int offset, int width, int height, int cellWidth, int cellHeight) =>
        new ReferenceWorldCollisionMask(data, offset, width, height, cellWidth, cellHeight);

    public int CellDataOffset { get; }
    public int Width { get; }
    public int Height { get; }
    public int CellWidthPixels { get; }
    public int CellHeightPixels { get; }
    public int CellCount => Width * Height;

    public static bool TryParse(
        byte[] data,
        out ReferenceWorldCollisionMask mask)
    {
        ReferenceWorldCollisionMaskFailure failure;
        return TryParse(data, out mask, out failure);
    }

    public static bool TryParse(
        byte[] data,
        out ReferenceWorldCollisionMask mask,
        out ReferenceWorldCollisionMaskFailure failure)
    {
        mask = null;
        failure = ReferenceWorldCollisionMaskFailure.None;
        if (data == null || data.Length < HeaderSize)
        {
            failure = ReferenceWorldCollisionMaskFailure.TruncatedHeader;
            return false;
        }

        for (int index = 0; index < Magic.Length; index++)
        {
            if (data[index] != Magic[index])
            {
                failure = ReferenceWorldCollisionMaskFailure.InvalidMagic;
                return false;
            }
        }

        int width = ReadInt32(data, 16);
        int height = ReadInt32(data, 20);
        if (width < 1 || height < 1)
        {
            failure = ReferenceWorldCollisionMaskFailure.InvalidDimensions;
            return false;
        }

        long cellCount = (long)width * height;
        if (cellCount > int.MaxValue)
        {
            failure = ReferenceWorldCollisionMaskFailure.InvalidCellPayload;
            return false;
        }

        // The official branch derives the payload start from the trailing W*H bytes rather than
        // assuming that every MASK revision has exactly the same header length.
        int cellDataOffset = data.Length - (int)cellCount;
        if (cellDataOffset < HeaderSize)
        {
            failure = ReferenceWorldCollisionMaskFailure.InvalidCellPayload;
            return false;
        }

        int cellWidthPixels = ReadInt32(data, 24);
        int cellHeightPixels = ReadInt32(data, 28);
        if (cellWidthPixels < 1)
        {
            cellWidthPixels = DefaultCellSizePixels;
        }

        if (cellHeightPixels < 1)
        {
            cellHeightPixels = DefaultCellSizePixels;
        }

        byte[] ownedBytes = new byte[data.Length];
        Buffer.BlockCopy(data, 0, ownedBytes, 0, data.Length);
        mask = new ReferenceWorldCollisionMask(
            ownedBytes,
            cellDataOffset,
            width,
            height,
            cellWidthPixels,
            cellHeightPixels);
        return true;
    }

    public bool IsCellWalkable(int cellX, int cellY)
    {
        if (cellX < 0 || cellY < 0 || cellX >= Width || cellY >= Height)
        {
            return false;
        }

        int index = CellDataOffset + cellX + Width * cellY;
        return index >= 0 && index < sourceBytes.Length &&
               (sourceBytes[index] & BlockedBit) == 0;
    }

    public bool TryWorldPixelToCell(
        float worldX,
        float worldY,
        out int cellX,
        out int cellY)
    {
        cellX = 0;
        cellY = 0;
        if (!IsFinite(worldX) || !IsFinite(worldY))
        {
            return false;
        }

        float dividedX = worldX / CellWidthPixels;
        float dividedY = worldY / CellHeightPixels;
        if (dividedX < int.MinValue || dividedX > int.MaxValue ||
            dividedY < int.MinValue || dividedY > int.MaxValue)
        {
            return false;
        }

        // The official float-to-int conversion truncates toward zero before the cell bounds check.
        cellX = (int)dividedX;
        cellY = (int)dividedY;
        return cellX >= 0 && cellY >= 0 && cellX < Width && cellY < Height;
    }

    public bool IsMaskPixelWalkable(float worldX, float worldY)
    {
        int cellX;
        int cellY;
        return TryWorldPixelToCell(worldX, worldY, out cellX, out cellY) &&
               IsCellWalkable(cellX, cellY);
    }

    public bool IsLineWalkable(int startX, int startY, int endX, int endY)
    {
        if (startX < 0 || startY < 0 || startX >= Width || startY >= Height ||
            endX < 0 || endY < 0 || endX >= Width || endY >= Height)
        {
            return false;
        }

        int deltaX = Math.Abs(endX - startX);
        int deltaY = Math.Abs(endY - startY);
        int stepX = startX < endX ? 1 : -1;
        int stepY = startY < endY ? 1 : -1;
        int error = deltaX - deltaY;

        while (true)
        {
            if (!IsCellWalkable(startX, startY))
            {
                return false;
            }

            if (startX == endX && startY == endY)
            {
                return true;
            }

            int doubledError = error * 2;
            if (doubledError > -deltaY)
            {
                error -= deltaY;
                startX += stepX;
            }

            if (doubledError < deltaX)
            {
                error += deltaX;
                startY += stepY;
            }
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return data[offset] |
               data[offset + 1] << 8 |
               data[offset + 2] << 16 |
               data[offset + 3] << 24;
    }
}

}
