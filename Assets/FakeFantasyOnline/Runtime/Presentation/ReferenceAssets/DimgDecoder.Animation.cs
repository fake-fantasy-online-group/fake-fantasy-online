using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official DimgDecoder animation shapes and complete DecodeAnim owner.
/// </summary>
public static partial class DimgDecoder
{
    public struct FrameTex
    {
        public Texture2D tex;
        public int nLeft;
        public int nTop;
        public int fcx;
        public int fcy;
        public int bindx;
        public int bindy;
        public int[] binds;
        public bool hasFc;
        public bool hasBind;
        public int flags;
    }

    public struct Anim
    {
        public bool ok;
        public int frames;
        public int dirs;
        public FrameTex[] f;
        public int clipWidth;
        public int clipBy;
        public int[] clipArr;
        public int boundW;
        public int boundCx;
        public int boundCy;
        public int attrib;
        public int scale;
    }

    public static Anim DecodeAnim(byte[] d)
    {
        Anim result = default(Anim);
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
        uint version = U32(d, ref header);
        int globalOffset = I32(d, ref header);
        if (IsIani(family))
        {
            int scale = Math.Max(HdMagic.IaniScale(family), 1);
            result = DecodeIani(d, globalOffset, scale);
            result.scale = Math.Max(HdMagic.IaniScale(family), 1);
            return result;
        }

        if (!IsDimg(family))
        {
            return result;
        }

        result.scale = Math.Max(HdMagic.DimgScale(family), 1);
        int frameCount = I32(d, ref header);
        int directionCount = I32(d, ref header);

        int bindingCount = 0;
        bool hasAuxiliaryPlane = false;
        if (version >= V12)
        {
            U32(d, ref header);
            bindingCount = I32(d, ref header);
            hasAuxiliaryPlane = U32(d, ref header) != 0;
        }

        if (unchecked(globalOffset + 24) > d.Length)
        {
            return result;
        }

        int cursor = globalOffset;
        result.boundW = I32(d, ref cursor);
        I32(d, ref cursor);
        result.boundCx = I32(d, ref cursor);
        result.boundCy = I32(d, ref cursor);
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
                }

                if (clips[0] > 0)
                {
                    result.clipBy = clips[0];
                }
            }

            cursor = clipEnd;
        }

        if (version >= V14 && unchecked(cursor + 4) <= d.Length)
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

        FrameTex[] frames = new FrameTex[frameCount];
        for (int frameIndex = 0; frameIndex < frameCount; frameIndex++)
        {
            if (unchecked(cursor + 8) > d.Length)
            {
                break;
            }

            int nLeft = I32(d, ref cursor);
            int nTop = I32(d, ref cursor);
            int flags = 0;
            if (version >= V11)
            {
                if (unchecked(cursor + 4) > d.Length)
                {
                    break;
                }

                flags = I32(d, ref cursor);
            }

            if (unchecked(cursor + 16) > d.Length)
            {
                break;
            }

            int width = I32(d, ref cursor);
            int height = I32(d, ref cursor);
            int pitch = I32(d, ref cursor);
            int attrib = I32(d, ref cursor);
            if (frameIndex == 0)
            {
                result.attrib = attrib;
            }

            byte[] raw = null;
            if ((uint)(width - 1) <= 4095U && (uint)(height - 1) <= 4095U)
            {
                int rawLength = unchecked(pitch * height);
                int rawEnd = unchecked(cursor + rawLength);
                if (rawEnd > d.Length)
                {
                    break;
                }

                raw = new byte[rawLength];
                Array.Copy(d, cursor, raw, 0, rawLength);
                cursor = rawEnd;
            }

            int fcx = 0;
            int fcy = 0;
            bool hasFc = false;
            if (version > V11 && unchecked(cursor + 12) <= d.Length)
            {
                fcx = I32(d, ref cursor);
                fcy = I32(d, ref cursor);
                I32(d, ref cursor);
                hasFc = true;
            }

            int bindx = 0;
            int bindy = 0;
            int[] binds = null;
            bool hasBind = false;
            int bindingBytes = unchecked(bindingCount * 8);
            if (bindingCount > 0 &&
                unchecked(cursor + bindingBytes) <= d.Length)
            {
                binds = new int[unchecked(bindingCount * 2)];
                for (int bindingIndex = 0;
                     bindingIndex < bindingCount;
                     bindingIndex++)
                {
                    binds[bindingIndex * 2] = I32(d, ref cursor);
                    binds[bindingIndex * 2 + 1] = I32(d, ref cursor);
                }

                bindx = binds[0];
                bindy = binds[1];
                hasBind = true;
            }

            if (hasAuxiliaryPlane)
            {
                int auxiliaryStart = cursor;
                int auxiliaryHeader = unchecked(auxiliaryStart + 8);
                int auxiliaryEnd = unchecked(auxiliaryStart + 24);
                cursor = auxiliaryHeader;
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
            }

            if (raw != null)
            {
                FrameTex frame = default(FrameTex);
                frame.tex = ToTexture(
                    width,
                    height,
                    pitch,
                    attrib,
                    raw,
                    result.scale);
                frame.nLeft = nLeft;
                frame.nTop = nTop;
                frame.fcx = fcx;
                frame.fcy = fcy;
                frame.bindx = bindx;
                frame.bindy = bindy;
                frame.binds = binds;
                frame.hasFc = hasFc;
                frame.hasBind = hasBind;
                frame.flags = flags;
                frames[frameIndex] = frame;
            }
        }

        result.ok = frameCount > 0;
        result.frames = frameCount;
        result.dirs = Math.Max(directionCount, 1);
        result.f = frames;
        return result;
    }
}

}
