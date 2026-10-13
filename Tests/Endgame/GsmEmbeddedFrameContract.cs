using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace UnityEngine
{
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
}

internal static class GsmEmbeddedFrameContract
{
    private static int checks;

    private static void Check(bool condition, string description)
    {
        checks++;
        if (!condition) throw new Exception(description);
    }

    // Serialized fixed frames contain exactly six Int32 fields, immediately
    // followed by pitch * height bytes. No eight-byte padding is serialized.
    private static byte[] Fixture(int version, int frameCount = 1,
        bool empty = false, bool library = true, bool fixedObject = true)
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(new byte[108]);
            stream.Position = 0x38;
            writer.Write(version);
            writer.Write(108);
            writer.Write(library ? 0x80 : 0);
            writer.Write(1000);
            writer.Write(1000);
            writer.Write(fixedObject ? 1 : 0);
            writer.Write(library ? 1 : 0);
            stream.Position = 108;
            if (fixedObject)
            {
                foreach (int value in new[] { 10, 20, 2, 1, frameCount, 32, 1, 0, 0, 0, 0, 0 })
                    writer.Write(value);
                writer.Write(7); // One clipping ordinate.
                for (int index = 0; index < frameCount; index++)
                {
                    int width = empty ? 0 : index + 1;
                    int height = empty ? 0 : 1;
                    foreach (int value in new[] { index * 3, index * 4, width, height, width * 2, 1 })
                        writer.Write(value);
                    for (int pixel = 0; pixel < width * height; pixel++)
                        writer.Write((ushort)(0xf123 + index * 0x111 + pixel));
                }
            }
            writer.Write(0); // No tile objects.
            if (library)
                foreach (int value in new[] { 0, 4, 0x013005dd, 54, 179, 0, 0, 0, 0, 0, 0, 109, 205, 20 })
                    writer.Write(value);
            return stream.ToArray();
        }
    }

    private static void ValidFrames(int version, int count)
    {
        byte[] bytes = Fixture(version, count);
        GsmMap map = GsmMap.Parse(bytes);
        Check(map.objsOk, "complete fixed frames and following library object");
        Check(map.fixObjs.Count == 1 && map.fixObjs[0].frames == count, "fixed frame count");
        Check(map.fixObjs[0].clipBy.Length == 1 && map.fixObjs[0].clipBy[0] == 7, "clip ordinates");
        for (int index = 0; index < count; index++)
        {
            GsmMap.FixFrame frame = map.fixObjs[0].frameImgs[index];
            Check(frame.xOff == index * 3 && frame.yOff == index * 4, "consecutive frame offsets");
            Check(frame.img.w == index + 1 && frame.img.h == 1, "consecutive frame geometry");
            Check(frame.img.data.Length == (index + 1) * 2, "frame payload length");
            for (int pixel = 0; pixel < index + 1; pixel++)
                Check(BitConverter.ToUInt16(frame.img.data, pixel * 2) == 0xf123 + index * 0x111 + pixel,
                    "first and last pixels are neither skipped nor taken from following records");
        }
        Check(map.tileObjs.Count == 0 && map.libObjs.Count == 1, "tile/library boundary");
        Check(map.libObjs[0].id == 0x013005dd && map.libObjs[0].py == 179 &&
            map.libObjs[0].xl == 109 && map.libObjs[0].yl == 205, "following portal is intact");
        Check(map.objectSectionEnd == bytes.Length, "exact object-section end");
    }

    private static byte[] Prefix(byte[] bytes, int length)
    {
        byte[] result = new byte[length];
        Array.Copy(bytes, result, length);
        return result;
    }

    private static void Boundaries()
    {
        byte[] empty = Fixture(0x20002, empty: true, library: false);
        GsmMap map = GsmMap.Parse(empty);
        Check(empty.Length == 188 && map.objsOk && map.fixObjs.Count == 1, "24-byte empty frame followed by four-byte tile count");
        Check(map.fixObjs[0].frameImgs[0].img.data == null, "empty frame has no pixel allocation");
        Check(map.objectSectionEnd == 188, "empty-frame exact boundary");

        byte[] bytes = Fixture(0x20002);
        foreach (int length in new[] { 160 + 7, 160 + 23, 184 + 1 })
        {
            map = GsmMap.Parse(Prefix(bytes, length));
            Check(!map.objsOk && map.fixObjs.Count == 0, "truncated header/pixels cannot append a fixed object");
        }
        map = GsmMap.Parse(Prefix(bytes, 186 + 3));
        Check(!map.objsOk && map.fixObjs.Count == 1 && map.libObjs.Count == 0,
            "complete fixed object survives truncated tile-count header");
        map = GsmMap.Parse(Prefix(bytes, bytes.Length - 1));
        Check(!map.objsOk && map.fixObjs.Count == 1 && map.libObjs.Count == 0,
            "partial library record cannot be published");
        bytes = Fixture(0x20002, fixedObject: false);
        map = GsmMap.Parse(bytes);
        Check(map.objsOk && map.fixObjs.Count == 0 && map.libObjs.Count == 1 &&
            map.objectSectionEnd == bytes.Length, "non-embedded path is unchanged");
    }

    private static void Corpus(string directory)
    {
        // Counts/offsets established independently from all ten embedded maps in
        // the q21_001 corpus. Other versions are not inferred from synthetic data.
        var expected = new Dictionary<string, int[]>
        {
            { "qw0501.gsm", new[] { 2, 1, 3208416 } },
            { "qw0502.gsm", new[] { 3, 1, 2568454 } },
            { "qw0503.gsm", new[] { 4, 2, 2493980 } },
            { "qw0504.gsm", new[] { 2, 1, 3215438 } },
            { "qw0505.gsm", new[] { 2, 1, 2804824 } },
            { "qw0507.gsm", new[] { 1, 2, 2168360 } },
            { "qw0508.gsm", new[] { 2, 1, 1193000 } },
            { "qw0509.gsm", new[] { 2, 0, 1198160 } },
            { "qw0510.gsm", new[] { 2, 0, 1198160 } },
            { "qw0511.gsm", new[] { 2, 0, 1198160 } }
        };
        int maps = 0, embedded = 0;
        foreach (string path in Directory.GetFiles(directory, "*.gsm"))
        {
            byte[] bytes = File.ReadAllBytes(path);
            GsmMap map = GsmMap.Parse(bytes);
            maps++;
            Check(map.objsOk, "corpus objects complete: " + Path.GetFileName(path));
            if (!expected.TryGetValue(Path.GetFileName(path), out int[] row)) continue;
            embedded++;
            Check(map.fixObjs.Count == row[0] && map.libObjs.Count == row[1] && map.tileObjs.Count == 0,
                "corpus object counts: " + Path.GetFileName(path));
            Check(map.objectSectionEnd == row[2], "corpus section boundary: " + Path.GetFileName(path));
            foreach (GsmMap.FixObj item in map.fixObjs)
            {
                Check(item.frames == 1 && item.frameImgs[0].img.w == item.xl &&
                    item.frameImgs[0].img.h == item.yl && item.frameImgs[0].img.pitch == item.xl * 2,
                    "corpus embedded frame dimensions");
            }
            for (int index = 0; index < map.libObjs.Count; index++)
                Check(map.libObjs[index].id == (Path.GetFileName(path) == "qw0507.gsm" && index == 1
                    ? 0x0130076d : 0x013005dd), "corpus recovered portal identifier");
            int tail = bytes.Length - map.objectSectionEnd;
            if (map.version == 0x20002)
                Check(tail == 16, "v2.0.2 metadata footer boundary");
            else
                Check(tail == 16 + 30 * 30 * 12 && BitConverter.ToInt32(bytes, map.objectSectionEnd) == 30 &&
                    BitConverter.ToInt32(bytes, map.objectSectionEnd + 4) == 30, "v1.0.9 legacy field boundary");
        }
        Check(maps == 377 && embedded == 10, "full q21_001 corpus coverage");
    }

    public static void Main(string[] args)
    {
        ValidFrames(0x10009, 1);
        ValidFrames(0x20002, 1);
        ValidFrames(0x20002, 2);
        Boundaries();
        if (args.Length > 0) Corpus(args[0]);
        Console.WriteLine("GSM embedded frame contract passed: " + checks + " checks.");
    }
}
