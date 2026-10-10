using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class DimgDecoder
{
    public static AnimGeom ParseAnimGeom(byte[] d)
    {
        AnimGeom result = new AnimGeom();
        if (d == null || d.Length < 24) return result;
        int cursor = 0;
        if (U32(d, ref cursor) != COPYRIGHT) return result;
        uint family = U32(d, ref cursor);
        if (!IsDimg(family)) return result;
        uint version = U32(d, ref cursor);
        int globalOffset = I32(d, ref cursor);
        result.scale = Math.Max(HdMagic.DimgScale(family), 1);
        int frameCount = I32(d, ref cursor);
        int directionCount = I32(d, ref cursor);
        int bindingCount = 0;
        bool hasAuxiliaryPlane = false;
        if (version >= V12)
        {
            U32(d, ref cursor);
            bindingCount = I32(d, ref cursor);
            hasAuxiliaryPlane = U32(d, ref cursor) != 0;
        }
        if (unchecked(globalOffset + 24) > d.Length) return result;
        cursor = globalOffset;
        result.boundW = I32(d, ref cursor);
        result.boundH = I32(d, ref cursor);
        result.boundCx = I32(d, ref cursor);
        result.boundCy = I32(d, ref cursor);
        I32(d, ref cursor);
        int clipCount = I32(d, ref cursor);
        if (clipCount > 0 && clipCount < 10000)
            cursor = unchecked(cursor + clipCount * 4);
        if (version >= V14 && unchecked(cursor + 4) <= d.Length)
        {
            int count = I32(d, ref cursor);
            if (count > 0 && count < 10000)
                cursor = unchecked(cursor + count * 8);
        }
        if (version >= V13 && unchecked(cursor + 4) <= d.Length)
        {
            int count = I32(d, ref cursor);
            if (count > 0 && count < 10000)
                cursor = unchecked(cursor + count * 12);
        }

        FrameGeom[] frames = new FrameGeom[Math.Max(0, frameCount)];
        int parsed = 0;
        for (; parsed < frameCount; parsed++)
        {
            if (unchecked(cursor + 8) > d.Length) break;
            int nLeft = I32(d, ref cursor);
            int nTop = I32(d, ref cursor);
            int flags = 0;
            if (version >= V11)
            {
                if (unchecked(cursor + 4) > d.Length) break;
                flags = I32(d, ref cursor);
            }
            if (unchecked(cursor + 16) > d.Length) break;
            int w = I32(d, ref cursor);
            int h = I32(d, ref cursor);
            int pitch = I32(d, ref cursor);
            int attrib = I32(d, ref cursor);
            if (parsed == 0) result.attrib = attrib;
            int srcOff = 0;
            int srcLen = 0;
            if (w > 0 && w <= 4096 && h > 0 && h <= 4096)
            {
                int bytes = unchecked(pitch * h);
                if (unchecked(cursor + bytes) > d.Length) break;
                srcOff = cursor;
                srcLen = bytes;
                cursor = unchecked(cursor + bytes);
            }
            int fcx = 0;
            int fcy = 0;
            bool hasFc = false;
            int bindx = 0;
            int bindy = 0;
            int[] binds = null;
            bool hasBind = false;
            if (version >= V12)
            {
                if (unchecked(cursor + 12) <= d.Length)
                {
                    fcx = I32(d, ref cursor);
                    fcy = I32(d, ref cursor);
                    I32(d, ref cursor);
                    hasFc = true;
                }
                if (bindingCount > 0 && unchecked(cursor + bindingCount * 8) <= d.Length)
                {
                    binds = new int[unchecked(bindingCount * 2)];
                    for (int seat = 0; seat < bindingCount; seat++)
                    {
                        binds[seat * 2] = I32(d, ref cursor);
                        binds[seat * 2 + 1] = I32(d, ref cursor);
                    }
                    bindx = binds[0];
                    bindy = binds[1];
                    hasBind = true;
                }
            }
            if (hasAuxiliaryPlane)
            {
                cursor = unchecked(cursor + 8);
                if (unchecked(cursor + 16) <= d.Length)
                {
                    int auxiliaryWidth = I32(d, ref cursor);
                    int auxiliaryHeight = I32(d, ref cursor);
                    int auxiliaryPitch = I32(d, ref cursor);
                    I32(d, ref cursor);
                    if (auxiliaryWidth > 0 && auxiliaryHeight > 0)
                        cursor = unchecked(cursor + auxiliaryPitch * auxiliaryHeight);
                }
            }
            frames[parsed] = new FrameGeom
            {
                nLeft = nLeft,
                nTop = nTop,
                flags = flags,
                w = w,
                h = h,
                pitch = pitch,
                attrib = attrib,
                srcOff = srcOff,
                srcLen = srcLen,
                fcx = fcx,
                fcy = fcy,
                bindx = bindx,
                bindy = bindy,
                binds = binds,
                hasFc = hasFc,
                hasBind = hasBind
            };
        }
        result.frames = frameCount;
        result.parsedFrames = parsed;
        result.dirs = Math.Max(1, directionCount);
        result.f = frames;
        result.src = d;
        result.ok = parsed > 0;
        return result;
    }

    public static byte[] FrameToRgba(AnimGeom animation, int frameIndex)
    {
        if (animation == null || animation.src == null || animation.f == null ||
            frameIndex < 0 || frameIndex >= animation.f.Length) return null;
        byte[] source = animation.src;
        FrameGeom frame = animation.f[frameIndex];
        if (frame.srcLen <= 0 || frame.w <= 0 || frame.h <= 0) return null;
        byte[] rgba = new byte[unchecked(frame.w * frame.h * 4)];
        switch (frame.attrib)
        {
            case 0:
                for (int y = 0; y < frame.h; y++)
                {
                    int src = unchecked(frame.srcOff + y * frame.pitch);
                    int dst = unchecked((frame.h - 1 - y) * frame.w * 4);
                    for (int x = 0; x < frame.w; x++)
                    {
                        int value = source[src] | source[src + 1] << 8;
                        rgba[dst] = (byte)(((value >> 10) & 31) * 255 / 31);
                        rgba[dst + 1] = (byte)(((value >> 5) & 31) * 255 / 31);
                        rgba[dst + 2] = (byte)((value & 31) * 255 / 31);
                        rgba[dst + 3] = (byte)((value >> 15) * 255);
                        src += 2;
                        dst += 4;
                    }
                }
                return rgba;
            case 1:
                for (int y = 0; y < frame.h; y++)
                {
                    int src = unchecked(frame.srcOff + y * frame.pitch);
                    int dst = unchecked((frame.h - 1 - y) * frame.w * 4);
                    for (int x = 0; x < frame.w; x++)
                    {
                        int value = source[src] | source[src + 1] << 8;
                        rgba[dst] = (byte)(((value >> 8) & 15) * 17);
                        rgba[dst + 1] = (byte)(((value >> 4) & 15) * 17);
                        rgba[dst + 2] = (byte)((value & 15) * 17);
                        rgba[dst + 3] = (byte)(((value >> 12) & 15) * 17);
                        src += 2;
                        dst += 4;
                    }
                }
                return rgba;
            case 2:
                for (int y = 0; y < frame.h; y++)
                {
                    int src = unchecked(frame.srcOff + y * frame.pitch);
                    int dst = unchecked((frame.h - 1 - y) * frame.w * 4);
                    for (int x = 0; x < frame.w; x++)
                    {
                        rgba[dst] = source[src + 2];
                        rgba[dst + 1] = source[src + 1];
                        rgba[dst + 2] = source[src];
                        rgba[dst + 3] = source[src + 3];
                        src += 4;
                        dst += 4;
                    }
                }
                return rgba;
            default:
                return null;
        }
    }

    public static Texture2D TexFromRgba(int width, int height, byte[] pixels,
        int scale = 1, bool keepReadable = true)
    {
        if (width <= 0 || height <= 0 || pixels == null ||
            pixels.Length < unchecked(width * height * 4)) return null;
        bool mipmaps = ArtDetail.NeedMip(scale);
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, mipmaps);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixelData(pixels, 0, 0);
        texture.Apply(mipmaps, !keepReadable);
        TexScale.Register(texture, scale);
        return texture;
    }
}

}
