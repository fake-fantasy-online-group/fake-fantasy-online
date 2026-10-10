using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Decodes the QQF IAN GSA textures used by the reference client.
/// </summary>
/// <remarks>
/// Official 1.5.8 members are the nine GsaDecoder tokens 06000bc2 through 06000bca.
/// The evidence identity and comparison rules live in REFERENCE_CLIENT_SOURCE_RECOVERY_V1.md.
/// </remarks>
public static class GsaDecoder
{
    private const int HeaderLength = 36;
    private const int RecordHeaderLength = 16;
    private const int MaximumFrameDimension = 4096;
    private const int MaximumAssembledDimension = 8192;

    /// <summary>
    /// Selects which records of a package an assembled entry composites.
    /// </summary>
    /// <remarks>
    /// Official 06000bca (Decode) composites every record and never evaluates tilesDown: records
    /// that fall below the texture are dropped by the row bound
    /// (tileY * TileHeight + row &lt; OutputHeight). 06000bc8 (DecodeFrameAssembled) and 06000bc9
    /// (DecodeLayer) own the tilesDown * tilesAcross window, and they do not use the same test:
    /// 06000bc8 bounds the frame with the 32-bit product firstRecord = recordsPerFrame *
    /// frameIndex, while 06000bc9 tests recordIndex / recordsPerFrame == layer, which never
    /// multiplies the index.
    /// </remarks>
    private enum GsaWindow
    {
        All,
        Frame,
        Layer,
    }

    public static int ScaleOf(byte[] data)
    {
        if (!HasMagic(data))
        {
            return 1;
        }

        switch (data[7])
        {
            case (byte)'2':
                return 2;
            case (byte)'H':
                return 4;
            case (byte)'G':
            case (byte)'I':
            default:
                return 1;
        }
    }

    public static int Scale(Texture2D texture)
    {
        return GsaTextureScale.Of(texture);
    }

    public static float W(Texture2D texture)
    {
        return GsaTextureScale.Width(texture);
    }

    public static float H(Texture2D texture)
    {
        return GsaTextureScale.Height(texture);
    }

    public static int FrameCount(byte[] data)
    {
        return IsSupported(data)
            ? ReadInt32(data, 32)
            : 0;
    }

    public static Texture2D DecodeFrame(byte[] data, int frameIndex)
    {
        int recordOffset;
        if (!TryReadDataOffset(data, out recordOffset))
        {
            return null;
        }

        int remaining = frameIndex;
        GsaRecord record;
        while (TryReadRecord(data, recordOffset, out record))
        {
            if (remaining == 0)
            {
                Color32[] pixels = new Color32[record.Width * record.Height];
                for (int sourceY = 0; sourceY < record.Height; sourceY++)
                {
                    int destinationY = record.Height - 1 - sourceY;
                    for (int x = 0; x < record.Width; x++)
                    {
                        pixels[destinationY * record.Width + x] =
                            ReadColor(data, record, sourceY, x);
                    }
                }

                return CreateTexture(
                    pixels,
                    record.Width,
                    record.Height,
                    ScaleOf(data),
                    // Official 06000bc7 creates the frame texture without a mip chain and calls
                    // the parameterless Apply(), i.e. Apply(true, false).
                    mipChain: false,
                    updateMipmaps: true);
            }

            remaining--;
            recordOffset = record.EndOffset;
        }

        return null;
    }

    public static Texture2D DecodeFrameAssembled(byte[] data, int frameIndex)
    {
        return DecodeAssembled(data, frameIndex, false, false, GsaWindow.Frame);
    }

    public static Texture2D DecodeLayer(byte[] data, int layer)
    {
        return DecodeAssembled(data, layer, true, false, GsaWindow.Layer);
    }

    public static Texture2D Decode(byte[] data)
    {
        // Official 06000bca composites every record in the package; only DecodeFrameAssembled
        // (06000bc8) and DecodeLayer (06000bc9) restrict the copy to one frame's tile window.
        return DecodeAssembled(data, 0, false, true, GsaWindow.All);
    }

    private static Texture2D DecodeAssembled(
        byte[] data,
        int frameIndex,
        bool discardTransparentRgb,
        bool scaled,
        GsaWindow window)
    {
        GsaLayout layout;
        if (!TryReadLayout(data, out layout))
        {
            return null;
        }

        int tilesAcross = TilesFor(layout.OutputWidth, layout.TileWidth);
        int recordsPerFrame = 0;
        int firstRecord = 0;
        if (window != GsaWindow.All)
        {
            int tilesDown = TilesFor(layout.OutputHeight, layout.TileHeight);
            recordsPerFrame = tilesDown * tilesAcross;
            // Only 06000bc8 hoists the first record of the window.
            if (window == GsaWindow.Frame)
            {
                firstRecord = unchecked(recordsPerFrame * frameIndex);
            }
        }

        Color32[] topDown = new Color32[layout.OutputWidth * layout.OutputHeight];
        int recordOffset = layout.DataOffset;
        int recordIndex = 0;
        GsaRecord record;
        while (TryReadRecord(data, recordOffset, out record))
        {
            int frameRecord = recordIndex;
            bool selected = true;
            if (window == GsaWindow.Frame)
            {
                frameRecord = unchecked(recordIndex - firstRecord);
                selected = recordIndex >= firstRecord &&
                           recordIndex < unchecked(firstRecord + recordsPerFrame);
            }
            else if (window == GsaWindow.Layer)
            {
                selected = recordIndex / recordsPerFrame == frameIndex;
                frameRecord = recordIndex % recordsPerFrame;
            }

            if (selected)
            {
                CopyRecord(
                    data,
                    record,
                    topDown,
                    layout,
                    frameRecord % tilesAcross,
                    frameRecord / tilesAcross,
                    discardTransparentRgb);
            }

            recordIndex++;
            recordOffset = record.EndOffset;
        }

        Color32[] bottomUp = new Color32[topDown.Length];
        for (int sourceY = 0; sourceY < layout.OutputHeight; sourceY++)
        {
            int destinationY = layout.OutputHeight - 1 - sourceY;
            Array.Copy(
                topDown,
                sourceY * layout.OutputWidth,
                bottomUp,
                destinationY * layout.OutputWidth,
                layout.OutputWidth);
        }

        int scale = ScaleOf(data);
        bool mipChain = scaled && scale > 1;
        return CreateTexture(
            bottomUp,
            layout.OutputWidth,
            layout.OutputHeight,
            scale,
            mipChain,
            // Official 06000bca calls Apply(scale > 1); 06000bc8 and 06000bc9 build the texture
            // without a mip chain and call the parameterless Apply(), i.e. Apply(true, false).
            scaled ? mipChain : true);
    }

    private static void CopyRecord(
        byte[] data,
        GsaRecord record,
        Color32[] destination,
        GsaLayout layout,
        int tileX,
        int tileY,
        bool discardTransparentRgb)
    {
        int destinationX = tileX * layout.TileWidth;
        int destinationY = tileY * layout.TileHeight;
        for (int sourceY = 0; sourceY < record.Height; sourceY++)
        {
            int y = destinationY + sourceY;
            if (y >= layout.OutputHeight)
            {
                break;
            }

            for (int sourceX = 0; sourceX < record.Width; sourceX++)
            {
                int x = destinationX + sourceX;
                if (x >= layout.OutputWidth)
                {
                    break;
                }

                Color32 color = ReadColor(data, record, sourceY, sourceX);
                // Official 06000bca/06000bc8/06000bc9 leave the destination untouched for an
                // attribute outside 0/1/2; only DecodeFrame (06000bc7) zeroes the whole image.
                if (IsKnownAttribute(record.Attribute) &&
                    (!discardTransparentRgb || color.a != 0))
                {
                    destination[y * layout.OutputWidth + x] = color;
                }
            }
        }
    }

    private static Color32 ReadColor(byte[] data, GsaRecord record, int row, int column)
    {
        if (record.Attribute == 0)
        {
            int offset = record.PayloadOffset + row * record.Pitch + column * 2;
            byte low = data[offset];
            byte high = data[offset + 1];
            byte red = (byte)(((high >> 2) & 31) << 3);
            byte green = (byte)((((low >> 5) | ((high & 3) << 3)) & 31) << 3);
            byte blue = (byte)((low & 31) << 3);
            byte alpha = (byte)((high & 128) == 0 ? 0 : 255);
            return new Color32(red, green, blue, alpha);
        }

        if (record.Attribute == 1)
        {
            int offset = record.PayloadOffset + row * record.Pitch + column * 2;
            byte low = data[offset];
            byte high = data[offset + 1];
            return new Color32(
                ExpandNibble(high & 15),
                ExpandNibble(low >> 4),
                ExpandNibble(low & 15),
                ExpandNibble(high >> 4));
        }

        if (record.Attribute == 2)
        {
            int offset = record.PayloadOffset + row * record.Pitch + column * 4;
            return new Color32(
                data[offset + 2],
                data[offset + 1],
                data[offset],
                data[offset + 3]);
        }

        return default(Color32);
    }

    private static bool IsKnownAttribute(int attribute)
    {
        return attribute >= 0 && attribute <= 2;
    }

    private static byte ExpandNibble(int value)
    {
        return (byte)(value | (value << 4));
    }

    /// <summary>
    /// Official 06000bca/06000bc7/06000bc8/06000bc9 all use "(tile + output - 1) / tile" in 32-bit
    /// arithmetic and then force at least one tile; Decode never evaluates it for the height.
    /// </summary>
    private static int TilesFor(int output, int tile)
    {
        int tiles = unchecked((output + tile - 1) / tile);
        return tiles < 2 ? 1 : tiles;
    }

    /// <remarks>
    /// Official 06000bca: new Texture2D(width, height, RGBA32, ScaleOf(data) &gt; 1) -&gt;
    /// SetPixels32 -&gt; Apply(ScaleOf(data) &gt; 1) -&gt; filterMode Trilinear(2)/Point(0) -&gt;
    /// wrapMode Clamp(1) -&gt; ScaleOf again -&gt; TexScale.Register.
    /// Official 06000bc7/06000bc8/06000bc9: mipChain = false, the parameterless Apply() (arm64
    /// Texture2D__Apply__102d944, whose body is mov w1,1 / mov w2,0 / b Apply(bool, bool)), Point.
    /// </remarks>
    private static Texture2D CreateTexture(
        Color32[] pixels,
        int width,
        int height,
        int scale,
        bool mipChain,
        bool updateMipmaps)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, mipChain);
        texture.SetPixels32(pixels);
        texture.Apply(updateMipmaps, false);
        texture.filterMode = mipChain ? FilterMode.Trilinear : FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        GsaTextureScale.Register(texture, scale);
        return texture;
    }

    private static bool TryReadLayout(byte[] data, out GsaLayout layout)
    {
        layout = default(GsaLayout);
        int dataOffset;
        if (!TryReadDataOffset(data, out dataOffset))
        {
            return false;
        }

        int tileWidth = ReadInt32(data, 16);
        int tileHeight = ReadInt32(data, 20);
        int outputWidth = ReadInt32(data, 24);
        int outputHeight = ReadInt32(data, 28);
        if (outputWidth < 1 || outputWidth > MaximumAssembledDimension ||
            outputHeight < 1 || outputHeight > MaximumAssembledDimension ||
            tileWidth < 1 ||
            tileHeight < 1)
        {
            return false;
        }

        layout = new GsaLayout(
            dataOffset,
            tileWidth,
            tileHeight,
            outputWidth,
            outputHeight);
        return true;
    }

    private static bool TryReadDataOffset(byte[] data, out int dataOffset)
    {
        dataOffset = 0;
        if (!IsSupported(data))
        {
            return false;
        }

        // Official 06000bca/06000bc8/06000bc9 keep the raw value and only test
        // "data.Length < unchecked(dataOffset + 16)" to decide whether the record loop runs, so an
        // offset past the end still yields the assembled (all-transparent) texture. DecodeFrame
        // (06000bc7) returns null there, which TryReadRecord's length bound already reproduces.
        dataOffset = ReadInt32(data, 12);
        return true;
    }

    private static bool TryReadRecord(byte[] data, int offset, out GsaRecord record)
    {
        record = default(GsaRecord);
        // Official 06000bca/06000bc7/06000bc8/06000bc9 run the record loop while
        // "data.Length >= unchecked(offset + 16)". The addition is 32-bit, so an offset near
        // int.MaxValue wraps negative, the loop is entered and the following BitConverter read
        // raises, instead of the loop being skipped.
        if (data.Length < unchecked(offset + RecordHeaderLength))
        {
            return false;
        }

        int width = ReadInt32(data, offset);
        int height = ReadInt32(data, offset + 4);
        int pitch = ReadInt32(data, offset + 8);
        int attribute = ReadInt32(data, offset + 12);
        if (width < 1 || width > MaximumFrameDimension ||
            height < 1 || height > MaximumFrameDimension ||
            pitch < width)
        {
            return false;
        }

        // Official computes the end of the payload in 32-bit arithmetic,
        // "endOffset = unchecked(payloadOffset + pitch * height)", and then tests
        // "data.Length < endOffset".
        int payloadOffset = unchecked(offset + RecordHeaderLength);
        int endOffset = unchecked(payloadOffset + pitch * height);
        if (data.Length < endOffset)
        {
            return false;
        }

        record = new GsaRecord(width, height, pitch, attribute, payloadOffset, endOffset);
        return true;
    }

    private static bool IsSupported(byte[] data)
    {
        if (data == null || data.Length < HeaderLength || !HasMagic(data))
        {
            return false;
        }

        byte version = data[7];
        return version == (byte)'2' ||
               version == (byte)'G' ||
               version == (byte)'H' ||
               version == (byte)'I';
    }

    private static bool HasMagic(byte[] data)
    {
        return data != null &&
               data.Length >= 8 &&
               data[0] == (byte)'Q' &&
               data[1] == (byte)'Q' &&
               data[2] == (byte)'F' &&
               data[3] == 26 &&
               data[4] == (byte)'I' &&
               data[5] == (byte)'A' &&
               data[6] == (byte)'N';
    }

    /// <remarks>
    /// Official 06000bca/06000bc7/06000bc8/06000bc9 read every 32-bit header field through
    /// System.BitConverter.ToInt32 (mscorlib_dll__System_BitConverter__ToInt32), so an out-of-range
    /// startIndex raises BitConverter's ArgumentOutOfRangeException rather than the array index
    /// exception a hand-written byte assembly would raise. Pixel bytes are a different primitive:
    /// official reads them one byte at a time behind an explicit length test (the no-return raise
    /// stub FUN_00177a80 on arm64), which is why ReadColor keeps plain array indexing.
    /// </remarks>
    private static int ReadInt32(byte[] data, int offset)
    {
        return BitConverter.ToInt32(data, offset);
    }

    private struct GsaLayout
    {
        public GsaLayout(
            int dataOffset,
            int tileWidth,
            int tileHeight,
            int outputWidth,
            int outputHeight)
        {
            DataOffset = dataOffset;
            TileWidth = tileWidth;
            TileHeight = tileHeight;
            OutputWidth = outputWidth;
            OutputHeight = outputHeight;
        }

        public int DataOffset { get; }
        public int TileWidth { get; }
        public int TileHeight { get; }
        public int OutputWidth { get; }
        public int OutputHeight { get; }
    }

    private struct GsaRecord
    {
        public GsaRecord(
            int width,
            int height,
            int pitch,
            int attribute,
            int payloadOffset,
            int endOffset)
        {
            Width = width;
            Height = height;
            Pitch = pitch;
            Attribute = attribute;
            PayloadOffset = payloadOffset;
            EndOffset = endOffset;
        }

        public int Width { get; }
        public int Height { get; }
        public int Pitch { get; }
        public int Attribute { get; }
        public int PayloadOffset { get; }
        public int EndOffset { get; }
    }
}
}
