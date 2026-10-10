using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed class ReferenceGsmEmbeddedFrame
{
    public int Left, Top, Width, Height, Pitch, Attribute;
    public byte[] Data;
}

public sealed class ReferenceGsmEmbeddedObject
{
    public int SourceX, SourceY, ExtentX, ExtentY, FrameCount, ClipWidth, ClipCount, Flags, Effect;
    public int[] ClipOffsets;
    public ReferenceGsmEmbeddedFrame[] Frames;
}

public partial class GsmMap
{
    // Fixed-object region of 06000bce. A FixObj is appended only after every frame.
    private bool ParseFixedObjects(byte[] data, ref int cursor, int count)
    {
        for (int n = 0; n < count; n++)
        {
            if (data.Length < unchecked(cursor + 28)) return false;
            var obj = new FixObj
            {
                sx = I32(data, cursor), sy = I32(data, cursor + 4),
                xl = I32(data, cursor + 8), yl = I32(data, cursor + 12),
                frames = I32(data, cursor + 16), clipWidth = I32(data, cursor + 20),
                clipCount = I32(data, cursor + 24)
            };
            int next = unchecked(cursor + 28);
            if (version > 0x10000)
            {
                if (data.Length < unchecked(cursor + 48)) return false;
                obj.flags = I32(data, cursor + 28);
                obj.effect = I32(data, cursor + 32);
                next = unchecked(cursor + 48);
            }
            cursor = next;
            if (obj.clipCount > 0 && obj.clipCount < 100000)
            {
                if (data.Length < unchecked(cursor + obj.clipCount * 4)) return false;
                obj.clipBy = new int[obj.clipCount];
                for (int i = 0; i < obj.clipCount; i++) obj.clipBy[i] = I32(data, cursor + i * 4);
                cursor = unchecked(cursor + obj.clipCount * 4);
            }
            if ((uint)obj.frames > 4096) return false;
            obj.frameImgs = new FixFrame[obj.frames];
            for (int i = 0; i < obj.frames; i++)
            {
                if (data.Length < unchecked(cursor + 8)) return false;
                FixFrame frame = new FixFrame
                {
                    xOff = I32(data, cursor), yOff = I32(data, cursor + 4)
                };
                if (data.Length < unchecked(cursor + 32)) return false;
                frame.img = new NakedImg
                {
                    w = I32(data, cursor + 8), h = I32(data, cursor + 12),
                    pitch = I32(data, cursor + 16), attrib = I32(data, cursor + 20)
                };
                cursor = unchecked(cursor + 32);
                if (frame.img.w > 0 && frame.img.h > 0)
                {
                    if (frame.img.w > 2048 || frame.img.h > 2048) return false;
                    int bytes = unchecked(frame.img.pitch * frame.img.h);
                    if (data.Length < unchecked(cursor + bytes)) return false;
                    frame.img.data = new byte[bytes];
                    System.Array.Copy(data, cursor, frame.img.data, 0, bytes);
                    cursor = unchecked(cursor + bytes);
                }
                obj.frameImgs[i] = frame;
            }
            fixObjs.Add(obj);
            objectSectionEnd = cursor;
        }
        return true;
    }
}

}
