using System;
using System.IO;
using System.Text;

namespace FakeFantasyOnline.Editor
{
/// <summary>Generates the core text decoder table from the Editor's standard CP936 codec, not game assets.</summary>
public static class GbkBuildTable
{
    public const int ByteLength = 126 * 190 * 2;

    public static byte[] Generate()
    {
        // This runs in the full Unity Editor, not in the stripped IL2CPP player.
        // Fail if the host lacks its standard codec; never silently ship Latin-1 fallback.
        Encoding codec = Encoding.GetEncoding(936, EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
        byte[] table = new byte[ByteLength];
        byte[] pair = new byte[2];
        for (int lead = 0x81; lead <= 0xfe; lead++)
        {
            pair[0] = (byte)lead;
            for (int trailIndex = 0; trailIndex < 190; trailIndex++)
            {
                pair[1] = (byte)(trailIndex < 63 ? trailIndex + 0x40 : trailIndex + 0x41);
                string decoded;
                try { decoded = codec.GetString(pair); }
                catch (DecoderFallbackException) { continue; }
                if (decoded.Length != 1 || char.IsSurrogate(decoded[0])) continue;
                int offset = ((lead - 0x81) * 190 + trailIndex) * 2;
                table[offset] = (byte)decoded[0];
                table[offset + 1] = (byte)(decoded[0] >> 8);
            }
        }
        Verify(table, 0xd6, 0xd0, '\u4e2d'); // 中
        Verify(table, 0xce, 0xc4, '\u6587'); // 文
        Verify(table, 0xc4, 0xe3, '\u4f60'); // 你
        Verify(table, 0xba, 0xc3, '\u597d'); // 好
        Verify(table, 0x81, 0x40, '\u4e02'); // GBK extension, first table entry
        return table;
    }

    public static void WriteTo(string resourcesDirectory)
    {
        byte[] table = Generate();
        Directory.CreateDirectory(resourcesDirectory);
        File.WriteAllBytes(Path.Combine(resourcesDirectory, "gbk_table.bytes"), table);
    }

    private static void Verify(byte[] table, int lead, int trail, char expected)
    {
        int trailIndex = trail <= 0x7e ? trail - 0x40 : trail - 0x41;
        int offset = ((lead - 0x81) * 190 + trailIndex) * 2;
        if ((table[offset] | table[offset + 1] << 8) != expected)
            throw new InvalidDataException("The Editor's CP936 codec failed the GBK table probe.");
    }
}
}
