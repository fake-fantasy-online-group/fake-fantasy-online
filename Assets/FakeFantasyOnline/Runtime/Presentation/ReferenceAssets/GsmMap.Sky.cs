using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GsmMap
{
    // Official 1.5.8 get_SkyColorUnity (06000bcb).
    public Color SkyColorUnity
    {
        get
        {
            return new Color(
                ((skyColor >> 16) & 0xff) / 255f,
                ((skyColor >> 8) & 0xff) / 255f,
                (skyColor & 0xff) / 255f,
                1f);
        }
    }

    // Official 1.5.8 ParseSky (06000bd0). No direct caller is present in the
    // pinned 1.5.8 graph, so this method does not join the ordinary Parse path.
    private void ParseSky(byte[] data)
    {
        if (version < 0x10004 || (flags & 0x100) == 0) return;

        int length = data.Length;
        int lowerBound = Math.Max(108, length - 2000);
        for (int cursor = length - 44; cursor > lowerBound; cursor--)
        {
            byte alpha = data[cursor + 3];
            if (alpha != 0 && alpha != 255) continue;
            if (data[cursor] + data[cursor + 1] + data[cursor + 2] <= 29) continue;

            int nearZero = 0;
            bool valid = true;
            for (int index = 0; index < 10; index++)
            {
                int offset = cursor + 4 + index * 4;
                int value = data[offset] | data[offset + 1] << 8 |
                    data[offset + 2] << 16 | data[offset + 3] << 24;
                if (value < -10000 || value > 10000)
                {
                    valid = false;
                    break;
                }
                if (value >= -99 && value <= 99) nearZero++;
            }
            if (!valid || nearZero < 6) continue;

            if ((flags & 0x200) != 0)
            {
                if (cursor + 56 > length) continue;
                float width = BitConverter.ToSingle(data, cursor + 44);
                float height = BitConverter.ToSingle(data, cursor + 48);
                int extraOffset = cursor + 52;
                int extra = data[extraOffset] | data[extraOffset + 1] << 8 |
                    data[extraOffset + 2] << 16 | data[extraOffset + 3] << 24;
                if (!(width > 0.1f && width < 1000f &&
                      height > 0.1f && height < 1000f) ||
                    extra < 0 || extra > 9999) continue;
                float widthRatio = mapWidth / width;
                float heightRatio = mapHeight / height;
                if (widthRatio < 16f || widthRatio > 4096f ||
                    heightRatio < 16f || heightRatio > 4096f) continue;
            }

            skyColor = (uint)(data[cursor] | data[cursor + 1] << 8 |
                data[cursor + 2] << 16 | data[cursor + 3] << 24);
            hasSky = true;
            return;
        }
    }
}

}
