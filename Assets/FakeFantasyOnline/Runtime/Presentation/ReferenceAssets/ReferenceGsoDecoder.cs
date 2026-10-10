using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Bounded decoder for the QQF DIMG object images used by reviewed legacy maps.
/// </summary>
/// <remarks>
/// This translates the 1.0.1 and 1.0.3 branches of DimgDecoder.Decode/DecodeRawInner
/// (06000A0C/06000A03). It admits the RGBA4444 1.0.1/1.0.3 object cone exercised by
/// qw0060/qw0006 plus the reviewed zero-extended 36-byte 1.0.1 header used by magic0343/0344,
/// structurally decodes every declared frame, and exposes the first direction used by the official
/// owners. Unsupported versions, attributes, tables, and offsets fail closed.
/// </remarks>
public static class ReferenceGsoDecoder
{
    public const uint Version101 = 65537;
    public const uint Version103 = 65539;

    private const int Version101HeaderSize = 24;
    private const int Version103HeaderSize = 36;
    private const int GlobalGeometrySize = 24;
    private const int FramePrefixSize = 12;
    private const int FrameGeometrySize = 16;
    private const int MaximumDimension = 4096;
    private const int MaximumClipCount = 4096;
    private const int MaximumFrameCount = 4096;

    public static bool TryDecode(byte[] data, out ReferenceGsoImage image)
    {
        image = null;
        int sourceScale = HdMagic.DimgScaleOf(data);
        if (!HasSignature(data) || sourceScale < 1 || !CanRead(data, 8, 16))
        {
            return false;
        }

        uint version = ReadUInt32(data, 8);
        int expectedHeaderSize;
        if (version == Version101)
        {
            expectedHeaderSize = ReadInt32(data, 12);
            if (expectedHeaderSize != Version101HeaderSize &&
                expectedHeaderSize != Version103HeaderSize)
            {
                return false;
            }
        }
        else if (version == Version103)
        {
            expectedHeaderSize = Version103HeaderSize;
        }
        else
        {
            return false;
        }

        int headerSize = ReadInt32(data, 12);
        int frameCount = ReadInt32(data, 16);
        int directionCount = ReadInt32(data, 20);
        if (headerSize != expectedHeaderSize ||
            (version == Version101 && headerSize == Version103HeaderSize &&
             (ReadInt32(data, 24) != 0 || ReadInt32(data, 28) != 0 ||
              ReadInt32(data, 32) != 0)) ||
            frameCount < 1 ||
            frameCount > MaximumFrameCount || directionCount != 1 ||
            !CanRead(data, headerSize, GlobalGeometrySize))
        {
            return false;
        }

        int boundWidth = ReadInt32(data, headerSize);
        int boundHeight = ReadInt32(data, headerSize + 4);
        int boundCenterX = ReadInt32(data, headerSize + 8);
        int boundCenterY = ReadInt32(data, headerSize + 12);
        int clipWidth = ReadInt32(data, headerSize + 16);
        int encodedClipCount = ReadInt32(data, headerSize + 20);
        if (!ValidDimension(boundWidth) || !ValidDimension(boundHeight) || clipWidth < 1 ||
            encodedClipCount < 0 || encodedClipCount > MaximumClipCount)
        {
            return false;
        }

        int cursor;
        if (!TryAdd(headerSize, GlobalGeometrySize, out cursor) ||
            !CanRead(data, cursor, (long)encodedClipCount * 4))
        {
            return false;
        }

        int[] encodedClips = new int[encodedClipCount];
        bool retainClips = false;
        for (int index = 0; index < encodedClipCount; index++)
        {
            int value = ReadInt32(data, cursor + index * 4);
            encodedClips[index] = value;
            retainClips |= value > 0;
        }

        if (!TryAdd(cursor, (long)encodedClipCount * 4, out cursor))
        {
            return false;
        }

        if (version >= Version103)
        {
            int tableCount;
            if (!TryReadCountedTable(data, ref cursor, 12, out tableCount))
            {
                return false;
            }
        }

        ReferenceGsoFrame[] frames = new ReferenceGsoFrame[frameCount];
        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            if (!CanRead(data, cursor, FramePrefixSize + FrameGeometrySize))
            {
                return false;
            }

            int currentLeft = ReadInt32(data, cursor);
            int currentTop = ReadInt32(data, cursor + 4);
            int currentFlags = ReadInt32(data, cursor + 8);
            cursor += FramePrefixSize;

            int currentWidth = ReadInt32(data, cursor);
            int currentHeight = ReadInt32(data, cursor + 4);
            int currentPitch = ReadInt32(data, cursor + 8);
            int currentAttribute = ReadInt32(data, cursor + 12);
            cursor += FrameGeometrySize;
            if (!ValidDimension(currentWidth) || !ValidDimension(currentHeight) ||
                currentPitch < currentWidth * 2 || currentAttribute != 1 ||
                !CanRead(data, cursor, (long)currentPitch * currentHeight))
            {
                return false;
            }

            Color32[] pixels = Decode4444(
                data,
                cursor,
                currentWidth,
                currentHeight,
                currentPitch);
            if (pixels == null)
            {
                return false;
            }

            if (!TryAdd(cursor, (long)currentPitch * currentHeight, out cursor))
            {
                return false;
            }

            if (version >= 65538)
            {
                if (!CanRead(data, cursor, 12))
                {
                    return false;
                }

                int frameCenterX = ReadInt32(data, cursor);
                int frameCenterY = ReadInt32(data, cursor + 4);
                cursor += 12;
                frames[frameIndex] = new ReferenceGsoFrame(
                    currentLeft,
                    currentTop,
                    currentFlags,
                    currentWidth,
                    currentHeight,
                    currentPitch,
                    currentAttribute,
                    frameCenterX,
                    frameCenterY,
                    pixels);
            }
            else
            {
                frames[frameIndex] = new ReferenceGsoFrame(
                    currentLeft,
                    currentTop,
                    currentFlags,
                    currentWidth,
                    currentHeight,
                    currentPitch,
                    currentAttribute,
                    0,
                    0,
                    pixels);
            }
        }

        if (!CanRead(data, cursor, 12) || cursor + 12 != data.Length ||
            ReadInt32(data, cursor) != frameCount ||
            ReadInt32(data, cursor + 4) != 0 ||
            ReadInt32(data, cursor + 8) != 0)
        {
            return false;
        }

        int[] clips = retainClips ? encodedClips : Array.Empty<int>();
        int clipBy = clips.Length > 0 && clips[0] > 0 ? clips[0] : boundCenterY;
        image = new ReferenceGsoImage(
            version,
            headerSize,
            frameCount,
            directionCount,
            boundWidth,
            boundHeight,
            boundCenterX,
            boundCenterY,
            clipBy,
            clipWidth,
            clips,
            sourceScale,
            frames);
        return true;
    }

    private static bool TryReadCountedTable(
        byte[] data,
        ref int cursor,
        int recordSize,
        out int count)
    {
        count = 0;
        if (!CanRead(data, cursor, 4))
        {
            return false;
        }

        count = ReadInt32(data, cursor);
        cursor += 4;
        if (count < 0 || count > MaximumClipCount ||
            !CanRead(data, cursor, (long)count * recordSize))
        {
            return false;
        }

        return TryAdd(cursor, (long)count * recordSize, out cursor);
    }

    private static Color32[] Decode4444(
        byte[] data,
        int offset,
        int width,
        int height,
        int pitch)
    {
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            int rowOffset = offset + y * pitch;
            for (int x = 0; x < width; x++)
            {
                byte low = data[rowOffset + x * 2];
                byte high = data[rowOffset + x * 2 + 1];
                pixels[y * width + x] = new Color32(
                    ExpandNibble(high & 15),
                    ExpandNibble(low >> 4),
                    ExpandNibble(low & 15),
                    ExpandNibble(high >> 4));
            }
        }

        return pixels;
    }

    private static byte ExpandNibble(int value)
    {
        return (byte)(value | (value << 4));
    }

    private static bool ValidDimension(int value)
    {
        return value > 0 && value <= MaximumDimension;
    }

    private static bool HasSignature(byte[] data)
    {
        return data != null && data.Length >= Version101HeaderSize &&
            data[0] == (byte)'Q' && data[1] == (byte)'Q' &&
            data[2] == (byte)'F' && data[3] == 26 &&
            HdMagic.DimgScaleOf(data) > 0;
    }

    private static bool CanRead(byte[] data, int offset, long count)
    {
        return data != null && offset >= 0 && count >= 0 &&
               (long)offset + count <= data.Length;
    }

    private static bool TryAdd(int value, long addition, out int result)
    {
        long sum = (long)value + addition;
        if (sum < 0 || sum > int.MaxValue)
        {
            result = 0;
            return false;
        }

        result = (int)sum;
        return true;
    }

    private static int ReadInt32(byte[] data, int offset)
    {
        return unchecked((int)ReadUInt32(data, offset));
    }

    private static uint ReadUInt32(byte[] data, int offset)
    {
        return (uint)(data[offset] |
                      (data[offset + 1] << 8) |
                      (data[offset + 2] << 16) |
                      (data[offset + 3] << 24));
    }
}

public sealed class ReferenceGsoImage
{
    private readonly int[] clipOffsets;
    private readonly ReferenceGsoFrame[] frames;

    public ReferenceGsoImage(
        uint version,
        int headerSize,
        int frameCount,
        int directionCount,
        int boundWidth,
        int boundHeight,
        int boundCenterX,
        int boundCenterY,
        int clipBy,
        int clipWidth,
        int[] clipOffsets,
        int scale,
        ReferenceGsoFrame[] frames)
    {
        Version = version;
        HeaderSize = headerSize;
        FrameCount = frameCount;
        DirectionCount = directionCount;
        BoundWidth = boundWidth;
        BoundHeight = boundHeight;
        BoundCenterX = boundCenterX;
        BoundCenterY = boundCenterY;
        ClipBy = clipBy;
        ClipWidth = clipWidth;
        this.clipOffsets = clipOffsets ?? Array.Empty<int>();
        ClipOffsets = Array.AsReadOnly(this.clipOffsets);
        Scale = scale;
        this.frames = frames ?? Array.Empty<ReferenceGsoFrame>();
        Frames = Array.AsReadOnly(this.frames);
    }

    public uint Version { get; }
    public int HeaderSize { get; }
    public int FrameCount { get; }
    public int DirectionCount { get; }
    public int BoundWidth { get; }
    public int BoundHeight { get; }
    public int BoundCenterX { get; }
    public int BoundCenterY { get; }
    public int ClipBy { get; }
    public int ClipWidth { get; }
    public IReadOnlyList<int> ClipOffsets { get; }
    public IReadOnlyList<ReferenceGsoFrame> Frames { get; }
    public ReferenceGsoFrame InitialFrame => frames[0];
    public int Left => InitialFrame.Left;
    public int Top => InitialFrame.Top;
    public int Flags => InitialFrame.Flags;
    public int Width => InitialFrame.Width;
    public int Height => InitialFrame.Height;
    public int Pitch => InitialFrame.Pitch;
    public int Attribute => InitialFrame.Attribute;
    public int FrameCenterX => InitialFrame.FrameCenterX;
    public int FrameCenterY => InitialFrame.FrameCenterY;
    public int Scale { get; }
    public Color32[] Pixels => InitialFrame.Pixels;
}

/// <summary>
/// One decoded DIMG map-object frame with its source placement geometry.
/// </summary>
public sealed class ReferenceGsoFrame
{
    public ReferenceGsoFrame(
        int left,
        int top,
        int flags,
        int width,
        int height,
        int pitch,
        int attribute,
        int frameCenterX,
        int frameCenterY,
        Color32[] pixels)
    {
        Left = left;
        Top = top;
        Flags = flags;
        Width = width;
        Height = height;
        Pitch = pitch;
        Attribute = attribute;
        FrameCenterX = frameCenterX;
        FrameCenterY = frameCenterY;
        Pixels = pixels ?? Array.Empty<Color32>();
    }

    public int Left { get; }
    public int Top { get; }
    public int Flags { get; }
    public int Width { get; }
    public int Height { get; }
    public int Pitch { get; }
    public int Attribute { get; }
    public int FrameCenterX { get; }
    public int FrameCenterY { get; }
    public Color32[] Pixels { get; }
}

}
