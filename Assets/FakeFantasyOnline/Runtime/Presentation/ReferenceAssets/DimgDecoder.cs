using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official QQF DIMG decoder owner recovered from the 1.3.4 client.
/// </summary>
/// <remarks>
/// Raw, Result, the version/constants/primitives, DecodeRaw/DecodeRawInner, Decode, DecodeIani,
/// LoadTrace.Decode and the pixel/texture conversion members used by those paths are restored
/// official owners. Complete official DecodeAnim is restored on the adjacent partial owner.
/// ParseAnimGeom, FrameToRgba and TexFromRgba are restored from official 1.5.8 on the geometry
/// partial owner; the 4444 predicate, row copy and texture upload also retain original 1.5.8
/// behavior. TryDecodeFrame is a product carrier without an official token: it is reached from the
/// runtime avatar, portrait and resource-loader paths and delegates the whole container traversal
/// to the official owners instead of carrying a second copy of it - DecodeRaw (06000b85) /
/// DecodeRawInner (06000b86) for one frame with its pixels, and ParseAnimGeom (06000b92) /
/// FrameGeom for the zero-dimension slots the official geometry owner keeps but the pixel owner
/// does not publish. The frame payload keeps going through the official attribute switch (ToColors
/// 06000b8a, or To4444 06000b88 followed by Expand16 06000b89); the container bound height, the one
/// field the 80-byte official Raw drops, is read from the global-block slot that official Decode
/// (06000b8f) and ParseAnimGeom publish as boundH. No version whitelist, header-length floor,
/// frame-count ceiling or self-made dimension/binding cap is left in the carrier: every gate comes
/// from the official body that runs. The zero-dimension frame output is a caller-driven use of the
/// official geometry owner, not a property of the pixel path, and its "this slot was really read"
/// test is the geometry owner's own parse progress (AnimGeom.parsedFrames, published by the same
/// loop that writes the slots) rather than a value read back out of the slot.
/// </remarks>
public static partial class DimgDecoder
{
    /// <summary>
    /// Recovered result shape returned by the official Decode/ResLoader.LoadNpcFull boundary.
    /// </summary>
    public struct Result
    {
        public Texture2D tex;
        public int nLeft;
        public int nTop;
        public int boundW;
        public int boundH;
        public int boundCx;
        public int boundCy;
        public int clipBy;
        public int clipWidth;
        public int[] clipArr;
        public int fcx;
        public int fcy;
        public int bind0x;
        public int bind0y;
        public int frames;
        public int dirs;
        public bool hasBind;
        public bool ok;
        public int scale;
    }

    public struct Raw
    {
        public Color32[] px;
        public byte[] px16;
        public int w;
        public int h;
        public int fcx;
        public int fcy;
        public int bind0x;
        public int bind0y;
        public int[] binds;
        public int nLeft;
        public int nTop;
        public int boundW;
        public int boundCx;
        public int boundCy;
        public bool hasBind;
        public bool ok;
        public int dwFlags;
        public int scale;
    }

    public struct FrameGeom
    {
        public int nLeft;
        public int nTop;
        public int flags;
        public int w;
        public int h;
        public int pitch;
        public int attrib;
        public int srcOff;
        public int srcLen;
        public int fcx;
        public int fcy;
        public int bindx;
        public int bindy;
        public int[] binds;
        public bool hasFc;
        public bool hasBind;
    }

    public class AnimGeom
    {
        public bool ok;
        public int frames;
        /// <summary>
        /// Adapter-only publication of the official loop's own progress counter in ParseAnimGeom
        /// (06000b92): the number of slots that loop actually wrote into <see cref="f"/>. The
        /// official result is unchanged - ok stays "at least one slot was written" and frames stays
        /// the declared header count, which can exceed the filled prefix - and no official consumer
        /// reads this field; the product carrier reads it so it never mistakes array capacity for
        /// a frame the official body read.
        /// </summary>
        public int parsedFrames;
        public int dirs;
        public int boundW;
        public int boundCx;
        public int boundCy;
        public int boundH;
        public int attrib;
        public int scale = 1;
        public FrameGeom[] f;
        public byte[] src;
    }

    private const uint COPYRIGHT = 440815953U;
    private const uint V11 = 65537U;
    private const uint V12 = 65538U;
    private const uint V13 = 65539U;
    private const uint V14 = 65540U;
    // Official containers are rejected below 24 bytes: FramesPerDir (06000b83), DecodeRawInner
    // (06000b86) and ParseAnimGeom (06000b92) all test d.Length < 24 before reading the header.
    private const int MinimumHeaderLength = 24;
    private const int MaximumFrameCount = 4096;

    public static Raw DecodeRaw(byte[] d, int wantFrame = 0, bool pixels = true)
    {
        if (LoadTrace.On)
        {
            long started = LoadTrace.Now;
            Raw traced = DecodeRawInner(d, wantFrame, pixels);
            LoadTrace.Decode(pixels, traced.w, traced.h, LoadTrace.Ms(started));
            return traced;
        }

        return DecodeRawInner(d, wantFrame, pixels);
    }

    private static Raw DecodeRawInner(byte[] d, int wantFrame, bool pixels)
    {
        Raw result = default(Raw);
        if (d == null || d.Length < 24)
        {
            return result;
        }

        int header = 0;
        if (U32(d, ref header) != COPYRIGHT)
        {
            return result;
        }

        uint family = U32(d, ref header);
        if (!IsDimg(family))
        {
            return result;
        }

        result.scale = ScaleOf(family);
        uint version = U32(d, ref header);
        int globalOffset = I32(d, ref header);
        int frames = I32(d, ref header);
        int directions = I32(d, ref header);

        int bindingCount = 0;
        bool hasAuxiliaryPlane = false;
        if (version >= V12)
        {
            header = 28;
            bindingCount = I32(d, ref header);
            hasAuxiliaryPlane = I32(d, ref header) != 0;
        }

        if (unchecked(globalOffset + 24) > d.Length)
        {
            return result;
        }

        int cursor = globalOffset;
        int boundW = I32(d, ref cursor);
        I32(d, ref cursor);
        int boundCx = I32(d, ref cursor);
        int boundCy = I32(d, ref cursor);
        I32(d, ref cursor);
        int paletteCount = I32(d, ref cursor);
        if (paletteCount > 0 && paletteCount < 10000)
        {
            cursor += paletteCount * 4;
        }

        if (version > V13)
        {
            if (unchecked(cursor + 4) <= d.Length)
            {
                int count = I32(d, ref cursor);
                if (count > 0 && count < 10000)
                {
                    cursor += count * 8;
                }
            }
        }

        if (version == V13 || version > V13)
        {
            if (unchecked(cursor + 4) <= d.Length)
            {
                int count = I32(d, ref cursor);
                if (count > 0 && count < 10000)
                {
                    cursor += count * 12;
                }
            }
        }

        int total = Math.Max(directions, 1) * frames;
        for (int frameIndex = 0; frameIndex < total; frameIndex++)
        {
            if (unchecked(cursor + 8) > d.Length)
            {
                break;
            }

            int nLeft = I32(d, ref cursor);
            int nTop = I32(d, ref cursor);
            int dwFlags = 0;
            if (version > 65536U)
            {
                if (unchecked(cursor + 4) > d.Length)
                {
                    break;
                }

                dwFlags = I32(d, ref cursor);
            }

            if (unchecked(cursor + 16) > d.Length)
            {
                break;
            }

            int w = I32(d, ref cursor);
            int h = I32(d, ref cursor);
            int pitch = I32(d, ref cursor);
            int attrib = I32(d, ref cursor);
            bool validDimensions = w >= 1 && w <= 4096 && h >= 1 && h <= 4096;
            bool wanted = frameIndex == wantFrame;
            byte[] rawData = null;

            if (validDimensions)
            {
                int rawLength = unchecked(pitch * h);
                if (unchecked(cursor + rawLength) > d.Length)
                {
                    break;
                }

                if (pixels && wanted)
                {
                    rawData = Sub(d, cursor, rawLength);
                }

                cursor += rawLength;
            }

            int fcx = 0;
            int fcy = 0;
            if (version > V11 && unchecked(cursor + 12) <= d.Length)
            {
                fcx = I32(d, ref cursor);
                fcy = I32(d, ref cursor);
                I32(d, ref cursor);
            }

            int bind0x = 0;
            int bind0y = 0;
            int[] binds = null;
            bool hasBind = false;
            int bindingBytes = unchecked(bindingCount * 8);
            if (bindingCount > 0 && unchecked(cursor + bindingBytes) <= d.Length)
            {
                if (wanted)
                {
                    binds = new int[bindingCount * 2];
                    int bindingCursor = cursor;
                    for (int bindingIndex = 0;
                         bindingIndex < bindingCount;
                         bindingIndex++)
                    {
                        binds[bindingIndex * 2] = I32(d, ref bindingCursor);
                        binds[bindingIndex * 2 + 1] = I32(d, ref bindingCursor);
                    }

                    bind0x = binds[0];
                    bind0y = binds[1];
                    hasBind = true;
                }

                cursor += bindingBytes;
            }

            if (hasAuxiliaryPlane)
            {
                int auxiliaryStart = cursor;
                cursor += 8;
                if (unchecked(auxiliaryStart + 24) <= d.Length)
                {
                    int auxiliaryCursor = auxiliaryStart + 8;
                    int auxiliaryWidth = I32(d, ref auxiliaryCursor);
                    int auxiliaryHeight = I32(d, ref auxiliaryCursor);
                    int auxiliaryPitch = I32(d, ref auxiliaryCursor);
                    cursor = auxiliaryStart + 24;
                    if (auxiliaryWidth > 0 && auxiliaryHeight > 0)
                    {
                        cursor += unchecked(auxiliaryPitch * auxiliaryHeight);
                    }
                }
            }

            if (wanted)
            {
                if (validDimensions)
                {
                    if (pixels && attrib == 1)
                    {
                        result.px16 = To4444(w, h, pitch, rawData);
                    }
                    else
                    {
                        result.px = pixels
                            ? ToColors(w, h, pitch, attrib, rawData)
                            : null;
                    }

                    result.w = w;
                    result.h = h;
                    result.fcx = fcx;
                    result.fcy = fcy;
                    result.bind0x = bind0x;
                    result.bind0y = bind0y;
                    result.binds = binds;
                    result.nLeft = nLeft;
                    result.nTop = nTop;
                    result.boundW = boundW;
                    result.boundCx = boundCx;
                    result.boundCy = boundCy;
                    result.hasBind = hasBind;
                    result.ok = pixels
                        ? result.px != null || result.px16 != null
                        : (uint)attrib < 3U;
                    result.dwFlags = dwFlags;
                }

                break;
            }
        }

        return result;
    }

    public static Result Decode(byte[] d, int wantFrame = 0)
    {
        Result result = default(Result);
        if (d == null || d.Length < 24)
        {
            return result;
        }

        int header = 0;
        if (U32(d, ref header) != COPYRIGHT)
        {
            return result;
        }

        uint family = U32(d, ref header);
        if (!IsDimg(family))
        {
            return result;
        }

        result.scale = ScaleOf(family);
        uint version = U32(d, ref header);
        int globalOffset = I32(d, ref header);
        result.frames = I32(d, ref header);
        result.dirs = I32(d, ref header);

        int bindingCount = 0;
        bool hasAuxiliaryPlane = false;
        if (version > V11)
        {
            header = 28;
            bindingCount = I32(d, ref header);
            hasAuxiliaryPlane = I32(d, ref header) != 0;
        }

        if (unchecked(globalOffset + 24) > d.Length)
        {
            return result;
        }

        int cursor = globalOffset;
        result.boundW = I32(d, ref cursor);
        result.boundH = I32(d, ref cursor);
        result.boundCx = I32(d, ref cursor);
        result.boundCy = I32(d, ref cursor);
        result.clipBy = result.boundCy;
        result.clipWidth = I32(d, ref cursor);

        int clipCount = I32(d, ref cursor);
        if (clipCount > 0 && clipCount < 10000)
        {
            int clipEnd = unchecked(cursor + clipCount * 4);
            if (clipEnd <= d.Length)
            {
                int[] clips = new int[clipCount];
                bool anyPositive = false;
                for (int index = 0; index < clipCount; index++)
                {
                    int clip = I32(d, ref cursor);
                    clips[index] = clip;
                    anyPositive |= clip > 0;
                }

                if (anyPositive)
                {
                    result.clipArr = clips;
                    if (clips[0] > 0)
                    {
                        result.clipBy = clips[0];
                    }
                }
            }

            cursor = clipEnd;
        }

        if (version > V13 && unchecked(cursor + 4) <= d.Length)
        {
            int count = I32(d, ref cursor);
            if (count > 0 && count < 10000)
            {
                cursor = unchecked(cursor + count * 8);
            }
        }

        if (version >= V13 && unchecked(cursor + 4) <= d.Length)
        {
            int count = I32(d, ref cursor);
            if (count > 0 && count < 10000)
            {
                cursor = unchecked(cursor + count * 12);
            }
        }

        int remaining = unchecked(Math.Max(result.dirs, 1) * result.frames);
        while (remaining > 0)
        {
            if (unchecked(cursor + 8) > d.Length)
            {
                break;
            }

            int nLeft = I32(d, ref cursor);
            int nTop = I32(d, ref cursor);
            if (version > 65536U)
            {
                if (unchecked(cursor + 4) > d.Length)
                {
                    break;
                }

                I32(d, ref cursor);
            }

            if (unchecked(cursor + 16) > d.Length)
            {
                break;
            }

            int w = I32(d, ref cursor);
            int h = I32(d, ref cursor);
            int pitch = I32(d, ref cursor);
            int attrib = I32(d, ref cursor);
            bool validDimensions = w >= 1 && w <= 4096 && h >= 1 && h <= 4096;
            byte[] raw = null;

            if (validDimensions)
            {
                int rawLength = unchecked(pitch * h);
                int rawEnd = unchecked(cursor + rawLength);
                if (rawEnd > d.Length)
                {
                    break;
                }

                if (wantFrame == 0)
                {
                    raw = new byte[rawLength];
                    Array.Copy(d, cursor, raw, 0, rawLength);
                }

                cursor = rawEnd;
            }

            int fcx = 0;
            int fcy = 0;
            if (version > V11 && unchecked(cursor + 12) <= d.Length)
            {
                fcx = I32(d, ref cursor);
                fcy = I32(d, ref cursor);
                I32(d, ref cursor);
            }

            int bind0x = 0;
            int bind0y = 0;
            bool hasBind = false;
            int bindingBytes = unchecked(bindingCount * 8);
            if (bindingCount > 0 && unchecked(cursor + bindingBytes) <= d.Length)
            {
                bind0x = I32(d, ref cursor);
                bind0y = I32(d, ref cursor);
                hasBind = true;
                cursor = unchecked(cursor + bindingBytes - 8);
            }

            if (hasAuxiliaryPlane)
            {
                int auxiliaryStart = cursor;
                int auxiliaryHeader = unchecked(auxiliaryStart + 8);
                int auxiliaryEnd = unchecked(auxiliaryStart + 24);
                if (auxiliaryEnd <= d.Length)
                {
                    int auxiliaryCursor = auxiliaryHeader;
                    int auxiliaryWidth = I32(d, ref auxiliaryCursor);
                    int auxiliaryHeight = I32(d, ref auxiliaryCursor);
                    int auxiliaryPitch = I32(d, ref auxiliaryCursor);
                    cursor = auxiliaryEnd;
                    if (auxiliaryWidth > 0 && auxiliaryHeight > 0)
                    {
                        cursor = unchecked(
                            cursor + auxiliaryPitch * auxiliaryHeight);
                    }
                }
                else
                {
                    cursor = auxiliaryHeader;
                }
            }

            if (wantFrame != 0)
            {
                wantFrame--;
                remaining--;
                continue;
            }

            if (raw == null)
            {
                break;
            }

            Texture2D texture = ToTexture(w, h, pitch, attrib, raw, result.scale);
            result.tex = texture;
            result.nLeft = nLeft;
            result.nTop = nTop;
            result.fcx = fcx;
            result.fcy = fcy;
            result.bind0x = bind0x;
            result.bind0y = bind0y;
            result.hasBind = hasBind;
            result.ok = texture;
            break;
        }

        return result;
    }

    private static Anim DecodeIani(byte[] d, int headSize, int scale = 1)
    {
        List<FrameTex> frames = new List<FrameTex>();
        int cursor = headSize;
        int attrib = 0;
        while (unchecked(cursor + 16) <= d.Length && frames.Count < 64)
        {
            int width = I32(d, ref cursor);
            int height = I32(d, ref cursor);
            int pitch = I32(d, ref cursor);
            int frameAttrib = I32(d, ref cursor);
            if ((uint)(width - 1) > 4095U ||
                (uint)(height - 1) > 4095U ||
                pitch < width ||
                (uint)frameAttrib > 2U)
            {
                break;
            }

            int rawLength = unchecked(pitch * height);
            int rawEnd = unchecked(cursor + rawLength);
            if (rawEnd > d.Length)
            {
                break;
            }

            byte[] raw = new byte[rawLength];
            Array.Copy(d, cursor, raw, 0, rawLength);
            if (frames.Count == 0)
            {
                attrib = frameAttrib;
            }

            FrameTex frame = default(FrameTex);
            frame.tex = ToTexture(width, height, pitch, frameAttrib, raw, scale);
            frames.Add(frame);
            cursor = rawEnd;
        }

        Anim result = default(Anim);
        result.f = frames.ToArray();
        result.frames = frames.Count;
        result.dirs = 1;
        result.ok = frames.Count > 0;
        result.attrib = attrib;
        return result;
    }

    public static int FramesPerDir(byte[] d)
    {
        if (d != null && d.Length >= 24)
        {
            int p = 0;
            if (U32(d, ref p) == COPYRIGHT && IsDimg(U32(d, ref p)))
            {
                p = 16;
                int frames = I32(d, ref p);
                int dirs = I32(d, ref p);
                if (dirs >= 1 && frames >= 1)
                {
                    return Math.Max(frames / dirs, 1);
                }
            }
        }

        return 1;
    }

    public static int BoundWidthOf(byte[] d)
    {
        if (d == null || d.Length < 24)
        {
            return 0;
        }

        int p = 0;
        if (U32(d, ref p) != COPYRIGHT || !IsDimg(U32(d, ref p)))
        {
            return 0;
        }

        p = 12;
        p = I32(d, ref p);
        if (p <= 23 || p + 4 > d.Length)
        {
            return 0;
        }

        int width = I32(d, ref p);
        if (width < 1 || (width >> 5) > 624)
        {
            return 0;
        }

        return width;
    }

    public static int DirectionCount(byte[] data)
    {
        if (!HasSupportedHeader(data))
        {
            return 0;
        }

        int directions = ReadInt32(data, 20);
        return directions >= 1 && directions <= MaximumFrameCount ? directions : 0;
    }

    public static uint Version(byte[] data)
    {
        return HasSupportedHeader(data) ? ReadUInt32(data, 8) : 0;
    }

    public static int FrameCount(byte[] data)
    {
        if (!HasSupportedHeader(data))
        {
            return 0;
        }

        int frameCount = ReadInt32(data, 16);
        return frameCount >= 1 && frameCount <= MaximumFrameCount ? frameCount : 0;
    }

    public static bool FrameIs4444(AnimGeom animation, int frameIndex)
    {
        return animation != null && frameIndex >= 0 && animation.f != null &&
               frameIndex < animation.f.Length &&
               animation.f[frameIndex].attrib == 1 &&
               animation.f[frameIndex].srcLen > 0;
    }

    public static byte[] FrameTo4444(AnimGeom animation, int frameIndex)
    {
        if (!FrameIs4444(animation, frameIndex))
        {
            return null;
        }

        FrameGeom frame = animation.f[frameIndex];
        byte[] sourceBytes = animation.src;
        if (sourceBytes == null || frame.w < 1 || frame.h < 1)
        {
            return null;
        }

        byte[] result = new byte[unchecked(frame.w * frame.h * 2)];
        int source = frame.srcOff;
        int destination = unchecked(frame.w * (frame.h - 1) * 2);
        for (int row = 0; row < frame.h; row++)
        {
            Array.Copy(sourceBytes, source, result, destination, unchecked(frame.w * 2));
            source = unchecked(source + frame.pitch);
            destination = unchecked(destination - frame.w * 2);
        }

        return result;
    }

    public static Texture2D TexFrom4444(
        int width,
        int height,
        byte[] pixels,
        int scale = 1,
        bool keepReadable = true)
    {
        if (height < 1 || width < 1 || pixels == null ||
            pixels.Length < unchecked(width * height * 2))
        {
            return null;
        }

        Texture2D texture = new Texture2D(
            width,
            height,
            TextureFormat.ARGB4444,
            ArtDetail.NeedMip(scale));
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixelData(pixels, 0, 0);
        texture.Apply(ArtDetail.NeedMip(scale), !keepReadable);
        TexScale.Register(texture, scale);
        return texture;
    }

    public static bool TryDecodeFrame(byte[] data, int frameIndex, out DimgFrame frame)
    {
        frame = null;
        if (frameIndex < 0)
        {
            return false;
        }

        // Official owner of the single-frame read: DecodeRaw (06000b85) / DecodeRawInner (06000b86).
        // It owns every version, counted-table, frame-centre, binding and auxiliary-plane predicate,
        // so this carrier no longer carries a second copy of the container traversal, a version
        // whitelist, a header-length floor, a frame-count ceiling or a self-made dimension/binding
        // cap.
        Raw raw = DecodeRaw(data, frameIndex, true);
        if (raw.w > 0 && raw.h > 0)
        {
            Color32[] pixels = raw.px;
            if (pixels == null)
            {
                pixels = Expand16(raw.px16, raw.w, raw.h);
            }

            if (pixels == null)
            {
                return false;
            }

            frame = new DimgFrame(
                pixels,
                raw.w,
                raw.h,
                raw.fcx,
                raw.fcy,
                raw.bind0x,
                raw.bind0y,
                raw.nLeft,
                raw.nTop,
                raw.boundW,
                ReadBoundHeight(data),
                raw.boundCx,
                raw.boundCy,
                ToBindings(raw.binds),
                raw.dwFlags);
            return true;
        }

        // DecodeRawInner publishes nothing for a frame outside its 1..4096 dimension gate, so a
        // frame that exists without pixels can only come from the official owner that keeps such a
        // slot together with its flags: ParseAnimGeom (06000b92) -> FrameGeom. The real callers that
        // require this output are the explicit empty mount top frames (ReferenceMountAvatar asks for
        // a zero-size frame carrying flags 256 and no bindings),
        // ReferenceResourceLoader.LoadDimgFirstVisible and ReferenceWorldLocalSkillRelease, which
        // all keep scanning or keep building when HasPixels == false instead of failing the whole
        // container.
        AnimGeom geometry = ParseAnimGeom(data);
        if (!geometry.ok || geometry.f == null || frameIndex >= geometry.parsedFrames)
        {
            return false;
        }

        FrameGeom slot = geometry.f[frameIndex];
        // ParseAnimGeom declares the whole slot array up front (new FrameGeom[frameCount]) and
        // writes slot i only when its official loop reaches the assignment at the end of iteration
        // i, so parsedFrames is the exact boundary between a slot the official body read and a slot
        // that is only array capacity left at its default value. Existence is never inferred here
        // from a frame centre or from a non-zero flags word: a 1.0.1 slot has no frame centre at
        // all, a 1.0.2+ slot whose frame centre does not fit is still written by the official loop,
        // and an older empty frame legitimately carries flags 0. Whether the slot is empty is a
        // property of the slot the official body published, not a guess about whether it was read.
        if (slot.w != 0 || slot.h != 0 || slot.srcLen != 0)
        {
            return false;
        }

        frame = new DimgFrame(
            Array.Empty<Color32>(),
            0,
            0,
            slot.fcx,
            slot.fcy,
            slot.bindx,
            slot.bindy,
            slot.nLeft,
            slot.nTop,
            geometry.boundW,
            geometry.boundH,
            geometry.boundCx,
            geometry.boundCy,
            ToBindings(slot.binds),
            slot.flags);
        return true;
    }

    private static DimgBinding[] ToBindings(int[] binds)
    {
        if (binds == null || binds.Length < 2)
        {
            return Array.Empty<DimgBinding>();
        }

        DimgBinding[] result = new DimgBinding[binds.Length / 2];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = new DimgBinding(binds[index * 2], binds[index * 2 + 1]);
        }

        return result;
    }

    private static int ReadBoundHeight(byte[] data)
    {
        // The one field the official Raw cannot supply: Raw is 80 bytes / 10 slots (dual-architecture
        // evidence of the frozen 06000b86 review) and publishes no bound height. DecodeRawInner reads
        // the global-block slot at globalOffset + 4 and drops it; Decode (06000b8f) publishes the same
        // slot as Result.boundH and ParseAnimGeom (06000b92) as AnimGeom.boundH, which is exactly the
        // value official MountAvatar.Geometry reads for the mount below-anchor offset. The carrier
        // reads that one slot instead of running a second container parse for it.
        if (data == null || data.Length < 24)
        {
            return 0;
        }

        int globalOffset = ReadInt32(data, 12);
        return CanRead(data, globalOffset, 24) ? ReadInt32(data, globalOffset + 4) : 0;
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

    private static byte ExpandFive(int value)
    {
        return (byte)((value * 2156535) >> 18);
    }

    public static bool AttribSupported(int attrib)
    {
        return (uint)attrib < 3U;
    }

    public static byte[] To4444(int w, int h, int pitch, byte[] rawData)
    {
        if (h < 1 || w < 1 || rawData == null)
        {
            return null;
        }

        if ((long)pitch * h > rawData.Length)
        {
            return null;
        }

        int rowLength = w << 1;
        byte[] result = new byte[h * rowLength];
        int source = 0;
        int destination = 0;
        for (int row = 0; row < h; row++)
        {
            Array.Copy(rawData, source, result, destination, rowLength);
            source += pitch;
            destination += rowLength;
        }

        return result;
    }

    public static Color32[] Expand16(byte[] px16, int w, int h)
    {
        if (px16 == null || w < 1 || h < 1)
        {
            return null;
        }

        int pixelCount = h * w;
        if (px16.Length < pixelCount * 2)
        {
            return null;
        }

        Color32[] result = new Color32[pixelCount];
        int source = 0;
        for (int index = 0; index < result.Length; index++)
        {
            byte low = px16[source];
            byte high = px16[source + 1];
            int red = high & 15;
            int green = low >> 4;
            int blue = low & 15;
            int alpha = high >> 4;
            result[index] = new Color32(
                (byte)(red | red << 4),
                (byte)(green | green << 4),
                (byte)(blue | blue << 4),
                (byte)(alpha | alpha << 4));
            source += 2;
        }

        return result;
    }

    public static Color32[] ToColors(
        int w,
        int h,
        int pitch,
        int attrib,
        byte[] rawData)
    {
        if ((uint)attrib > 2U)
        {
            return null;
        }

        Color32[] result = new Color32[h * w];
        if (h < 1 || w < 1)
        {
            return result;
        }

        int bytesPerPixel = attrib == 2 ? 4 : 2;
        int destination = 0;
        int rowSource = 0;
        for (int row = 0; row < h; row++)
        {
            int source = rowSource;
            for (int column = 0; column < w; column++)
            {
                if (attrib == 2)
                {
                    result[destination] = new Color32(
                        rawData[source + 2],
                        rawData[source + 1],
                        rawData[source],
                        rawData[source + 3]);
                }
                else if (attrib == 1)
                {
                    byte low = rawData[source];
                    byte high = rawData[source + 1];
                    int red = high & 15;
                    int green = low >> 4;
                    int blue = low & 15;
                    int alpha = high >> 4;
                    result[destination] = new Color32(
                        (byte)(red | red << 4),
                        (byte)(green | green << 4),
                        (byte)(blue | blue << 4),
                        (byte)(alpha | alpha << 4));
                }
                else
                {
                    byte low = rawData[source];
                    byte high = rawData[source + 1];
                    int packed = low | high << 8;
                    result[destination] = new Color32(
                        (byte)((((packed >> 10) & 31) * 2156535) >> 18),
                        (byte)((((packed >> 5) & 31) * 2156535) >> 18),
                        (byte)(((packed & 31) * 2156535) >> 18),
                        (byte)((high >> 7) * 255));
                }

                destination++;
                source += bytesPerPixel;
            }

            rowSource += pitch;
        }

        return result;
    }

    private static int[] GenClipFromAlpha(
        byte[] raw,
        int w,
        int h,
        int pitch,
        int attrib,
        int nTop,
        int cw)
    {
        if (raw == null || w < 1 || h < 1 || cw < 1)
        {
            return null;
        }

        int count = (w + cw - 1) / cw;
        int[] result = new int[count];
        bool foundAny = false;
        for (int clipIndex = 0; clipIndex < count; clipIndex++)
        {
            int startX = clipIndex * cw;
            int endX = Math.Min(startX + cw, w);
            int bottom = -1;
            for (int x = startX; x < endX; x++)
            {
                for (int y = h - 1; y >= 0; y--)
                {
                    if (AlphaAt(raw, pitch, attrib, x, y) > 16)
                    {
                        bottom = Math.Max(bottom, y);
                        break;
                    }
                }
            }

            result[clipIndex] = bottom >= 0 ? bottom + nTop : -1;
            foundAny |= bottom >= 0;
        }

        if (!foundAny)
        {
            return result;
        }

        int nearest = -1;
        for (int index = 0; index < result.Length; index++)
        {
            if (result[index] < 0 && nearest >= 0)
            {
                result[index] = nearest;
            }
            else
            {
                nearest = result[index];
            }
        }

        nearest = -1;
        for (int index = result.Length - 1; index >= 0; index--)
        {
            if (result[index] < 0 && nearest >= 0)
            {
                result[index] = nearest;
            }
            else
            {
                nearest = result[index];
            }
        }

        return result;
    }

    private static int AlphaAt(byte[] raw, int pitch, int attrib, int x, int y)
    {
        int offset = y * pitch + x * (attrib == 2 ? 4 : 2);
        if (offset < 0 || raw == null)
        {
            return 0;
        }

        int last = offset + (attrib == 2 ? 3 : 1);
        if (last >= raw.Length)
        {
            return 0;
        }

        if (attrib == 2)
        {
            return raw[offset + 3];
        }

        byte high = raw[offset + 1];
        if (attrib == 1)
        {
            int alpha = high >> 4;
            return alpha | alpha << 4;
        }

        return attrib == 0 ? (high >> 7) * 255 : 0;
    }

    private static Texture2D ToTexture(
        int w,
        int h,
        int pitch,
        int attrib,
        byte[] raw,
        int scale = 1)
    {
        // The official 1.5.8 body guards nothing beyond the fast path: a null raw
        // throws through raw.Length, and a non-positive size falls into the slow
        // path and allocates, so the guards are written the way the native body
        // behaves instead of the defensive way.
        if (w >= 1 && h >= 1 && attrib == 1 && (long)pitch * h <= raw.Length)
        {
            int rowBytes = w * 2;
            byte[] pixels = new byte[w * h * 2];
            int source = 0;
            int destination = w * (h - 1) * 2;
            for (int row = 0; row < h; row++)
            {
                Array.Copy(raw, source, pixels, destination, rowBytes);
                source += pitch;
                destination -= rowBytes;
            }

            return TexFrom4444(w, h, pixels, scale);
        }

        int bytesPerPixel = attrib == 2 ? 4 : 2;
        Color32[] colors = new Color32[w * h];
        int rowSource = 0;
        int rowDestination = w * (h - 1);
        for (int row = 0; row < h; row++)
        {
            int source = rowSource;
            int destination = rowDestination;
            for (int column = 0; column < w; column++)
            {
                if (attrib == 2)
                {
                    colors[destination] = new Color32(
                        raw[source + 2],
                        raw[source + 1],
                        raw[source],
                        raw[source + 3]);
                }
                else if (attrib == 1)
                {
                    byte low = raw[source];
                    byte high = raw[source + 1];
                    int red = high & 15;
                    int green = low >> 4;
                    int blue = low & 15;
                    int alpha = high >> 4;
                    colors[destination] = new Color32(
                        (byte)(red | red << 4),
                        (byte)(green | green << 4),
                        (byte)(blue | blue << 4),
                        (byte)(alpha | alpha << 4));
                }
                else if (attrib == 0)
                {
                    byte low = raw[source];
                    byte high = raw[source + 1];
                    int packed = low | high << 8;
                    colors[destination] = new Color32(
                        (byte)((((packed >> 10) & 31) * 2156535) >> 18),
                        (byte)((((packed >> 5) & 31) * 2156535) >> 18),
                        (byte)(((packed & 31) * 2156535) >> 18),
                        (byte)((high >> 7) * 255));
                }
                else
                {
                    return null;
                }

                destination++;
                source += bytesPerPixel;
            }

            rowSource += pitch;
            rowDestination -= w;
        }

        bool mipmaps = ArtDetail.NeedMip(scale);
        Texture2D texture = new Texture2D(w, h, TextureFormat.RGBA32, mipmaps);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(colors);
        texture.Apply(mipmaps, false);
        GsaTextureScale.Register(texture, scale);
        return texture;
    }

    private static int ScaleOf(uint id)
    {
        return Math.Max(HdMagic.DimgScale(id), 1);
    }

    private static bool IsDimg(uint id)
    {
        return HdMagic.IsDimg(id);
    }

    private static bool IsIani(uint id)
    {
        return HdMagic.IsIani(id);
    }

    private static int ScaleOfIani(uint id)
    {
        return Math.Max(HdMagic.IaniScale(id), 1);
    }

    private static bool HasSupportedHeader(byte[] data)
    {
        // The official readers have no version whitelist: DecodeRawInner (06000b86) and ParseAnimGeom
        // (06000b92) branch on version comparisons (>= 1.0.2 binding/auxiliary flag, > 1.0.1 frame
        // centre, >= 1.0.4 the counted 8-byte table, >= 1.0.3 the counted 12-byte table), so every
        // version has one official behavior and none of them is refused here.
        return data != null &&
               data.Length >= MinimumHeaderLength &&
               data[0] == (byte)'Q' &&
               data[1] == (byte)'Q' &&
               data[2] == (byte)'F' &&
               data[3] == 26 &&
               data[4] == (byte)'D' &&
               data[5] == (byte)'I' &&
               data[6] == (byte)'M' &&
               data[7] == (byte)'G';
    }

    private static bool CanRead(byte[] data, int offset, long count)
    {
        return data != null && offset >= 0 && count >= 0 &&
               (long)offset + count <= data.Length;
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

    private static uint U32(byte[] d, ref int p)
    {
        uint value = (uint)(d[p] |
                            (d[p + 1] << 8) |
                            (d[p + 2] << 16) |
                            (d[p + 3] << 24));
        p += 4;
        return value;
    }

    private static int I32(byte[] d, ref int p)
    {
        return unchecked((int)U32(d, ref p));
    }

    private static byte[] Sub(byte[] d, int off, int len)
    {
        byte[] result = new byte[len];
        Array.Copy(d, off, result, 0, len);
        return result;
    }
}

public sealed class DimgFrame
{
    public DimgFrame(
        Color32[] pixels,
        int width,
        int height,
        int centerX,
        int centerY,
        int bindX,
        int bindY,
        int left,
        int top,
        int boundWidth,
        int boundHeight,
        int boundCenterX,
        int boundCenterY,
        DimgBinding[] bindings,
        int flags)
    {
        Pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
        Width = width;
        Height = height;
        CenterX = centerX;
        CenterY = centerY;
        BindX = bindX;
        BindY = bindY;
        Left = left;
        Top = top;
        BoundWidth = boundWidth;
        BoundHeight = boundHeight;
        BoundCenterX = boundCenterX;
        BoundCenterY = boundCenterY;
        Bindings = bindings ?? throw new ArgumentNullException(nameof(bindings));
        Flags = flags;
    }

    public Color32[] Pixels { get; }
    public int Width { get; }
    public int Height { get; }
    public int CenterX { get; }
    public int CenterY { get; }
    public int BindX { get; }
    public int BindY { get; }
    public int Left { get; }
    public int Top { get; }
    public int BoundWidth { get; }
    public int BoundHeight { get; }
    public int BoundCenterX { get; }
    public int BoundCenterY { get; }
    public DimgBinding[] Bindings { get; }
    public bool HasPixels => Width > 0 && Height > 0 && Pixels.Length == Width * Height;
    public bool HasBind => Bindings.Length > 0;
    public int Flags { get; }
}

public struct DimgBinding
{
    public DimgBinding(int x, int y)
    {
        X = x;
        Y = y;
    }

    public int X { get; }
    public int Y { get; }
}
}
