using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer : MonoBehaviour
{
    private static int I32(byte[] data, int offset) => data[offset] | data[offset + 1] << 8 |
        data[offset + 2] << 16 | data[offset + 3] << 24;

    // Original 06000bb8: select the first full-image index from each four-index shape group.
    private static Color32[][] LoadTilFull(PkgArchive pkg, string name)
    {
        byte[] data = pkg.Read("tile/" + name + ".til");
        if (data == null || data.Length < 16 || data[0] != 'E' || data[1] != 'L' ||
            data[2] != 'I' || data[3] != 'T') return null;
        int version = I32(data, 8);
        int imageCount = I32(data, 12);
        int[] indices = new int[60];
        for (int i = 0; i < 60; i++) indices[i] = I32(data, 16 + i * 4);
        int start = version > 0x1002 ? 0x144 : 0x13c;
        var shapes = new Color32[15][];
        for (int i = 0; i < 15; i++)
        {
            int index = indices[i * 4];
            if (index >= 0 && index < imageCount && start + (index + 1) * 65536 <= data.Length)
                shapes[i] = DecodeFull(data, start + index * 65536);
        }
        return shapes;
    }

    private static Color32[] DecodeFull(byte[] data, int offset)
    {
        var pixels = new Color32[16384];
        for (int i = 0; i < pixels.Length; i++)
        {
            int p = offset + i * 4;
            pixels[i] = new Color32(data[p + 2], data[p + 1], data[p], data[p + 3]);
        }
        return pixels;
    }

    private static Color32[] FlipV(Color32[] source, int width, int height)
    {
        var result = new Color32[width * height];
        for (int y = 0; y < height; y++)
            Array.Copy(source, y * width, result, (height - 1 - y) * width, width);
        return result;
    }

    private static void Over(Color32[] destination, Color32[] source)
    {
        for (int i = 0; i < destination.Length; i++)
        {
            Color32 s = source[i];
            if (s.a == 0) continue;
            if (s.a == 255) { destination[i] = s; continue; }
            Color32 d = destination[i];
            int inverse = 255 - s.a;
            destination[i] = new Color32(
                (byte)((s.r * s.a + d.r * inverse) / 255),
                (byte)((s.g * s.a + d.g * inverse) / 255),
                (byte)((s.b * s.a + d.b * inverse) / 255),
                (byte)(s.a + d.a * inverse / 255));
        }
    }
}

}
