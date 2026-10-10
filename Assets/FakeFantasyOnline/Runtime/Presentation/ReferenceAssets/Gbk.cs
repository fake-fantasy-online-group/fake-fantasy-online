using System.Text;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static class Gbk
{
    private static ushort[] _map;
    private static bool _tried;
    private const int TRAILS = 190;
    private static void Ensure()
    {
        if (_tried) return;
        _tried = true;
        TextAsset asset = Resources.Load<TextAsset>("gbk_table");
        if (asset == null) { Debug.LogWarning("[Gbk] 缺 Resources/gbk_table.bytes → 中文回退Latin1(乱码)"); return; }
        byte[] bytes = asset.bytes;
        _map = new ushort[bytes.Length / 2];
        ushort[] map = _map;
        for (int i = 0; i < map.Length; i++) map[i] = (ushort)(bytes[i * 2] | (bytes[i * 2 + 1] << 8));
    }
    public static string GetString(byte[] data)
    {
        if (data == null || data.Length == 0) return string.Empty;
        Ensure();
        var sb = new StringBuilder(data.Length);
        for (int i = 0; i < data.Length; i++)
        {
            int b = data[i];
            char ch = (char)b;
            if (b >= 0x80)
            {
                ushort[] map = _map;
                if (b >= 0x81 && b <= 0xfe && i + 1 < data.Length && map != null)
                {
                    int trail = data[i + 1];
                    int t = trail >= 0x40 && trail <= 0x7e ? trail - 0x40 : trail >= 0x80 && trail <= 0xfe ? trail - 0x41 : -1;
                    if (t >= 0)
                    {
                        int index = (b - 0x81) * TRAILS + t;
                        if (index < map.Length)
                        {
                            ushort value = map[index];
                            if (value != 0) { ch = (char)value; i++; }
                        }
                    }
                }
            }
            sb.Append(ch);
        }
        return sb.ToString();
    }
}
}
