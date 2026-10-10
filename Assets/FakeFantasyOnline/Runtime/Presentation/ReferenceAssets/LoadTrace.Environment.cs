using System;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class LoadTrace
{
    // Official 06000201. The builder and final ToString are outside the settings catch.
    private static string Env()
    {
        StringBuilder sb = new StringBuilder();
        try
        {
            GameSettings.EnsureLoaded();
            sb.Append("设置: ");
            sb.Append("不显示他人宠物=").Append(GameSettings.HideOtherPets).Append(' ');
            sb.Append("关自己宠光效=").Append(GameSettings.HideMyPetGlow).Append(' ');
            sb.Append("他人宠光效关=").Append(GameSettings.HideOtherPetGlow).Append(' ');
            sb.Append("坐骑光效=").Append(GameSettings.AnyRideGlowWanted).Append(' ');
            sb.Append("F11档=").Append(CharData.nameMode).Append(' ');
            sb.Append("vSync=").Append(QualitySettings.vSyncCount);
        }
        catch (Exception ex)
        {
            sb.Append("(设置读不到: ").Append(ex.Message).Append(')');
        }

        return sb.ToString();
    }

    // Official 06000202. Each format's probe is caught; its common result append is not.
    private static string GpuFormatProbe()
    {
        StringBuilder sb = new StringBuilder("GPU纹理格式(2048² 实占): ");
        TextureFormat[] formats =
        {
            TextureFormat.RGBA32,
            TextureFormat.ARGB4444,
            TextureFormat.RGBA4444,
            TextureFormat.BC7,
            TextureFormat.DXT5,
        };

        for (int i = 0; i < formats.Length; i++)
        {
            TextureFormat format = formats[i];
            string text;
            try
            {
                if (!SystemInfo.SupportsTextureFormat(format))
                {
                    sb.Append(format).Append("=不支持  ");
                    continue;
                }

                int raw = 4 << 20;
                switch (format)
                {
                    case TextureFormat.RGBA4444:
                        raw = 8 << 20;
                        break;
                    case TextureFormat.RGBA32:
                        raw = 16 << 20;
                        break;
                    case TextureFormat.ARGB4444:
                        raw = 8 << 20;
                        break;
                }

                long before = unchecked((long)Texture.currentTextureMemory);
                Texture2D probe = new Texture2D(2048, 2048, format, false);
                probe.LoadRawTextureData(new byte[raw]);
                probe.Apply(false, true);
                long after = unchecked((long)Texture.currentTextureMemory);
                long runtime = Profiler.GetRuntimeMemorySizeLong(probe);
                UnityEngine.Object.DestroyImmediate(probe);
                text = string.Format(
                    "{0:F1}MB(GRMS {1:F1})",
                    unchecked(after - before) / 1048576f,
                    runtime / 1048576f);
            }
            catch (Exception ex)
            {
                text = "建不出来:" + ex.GetType().Name;
            }

            sb.Append(format).Append('=').Append(text).Append("  ");
        }

        sb.Append("\n  ★先看 RGBA32 那一列: 它必须≈16.0MB(对照组)。不是的话这套量法在本机失效, 其余列作废。");
        sb.Append("\n  ★然后看 ARGB4444: ≈8.0MB = GPU 原生支持 ⇒ 可以走【4444 直传】(显存÷2, 无损);");
        sb.Append(" ≈16.0MB = 被引擎内部转成了 RGBA32 ⇒ 这条路堵死, 只能走 BC7 离线烘焙(BC7/DXT5 应≈4.0MB)。");
        return sb.ToString();
    }

    // Official 06000203. Failed candidates are reported without adding a cleanup finally.
    private static string Argb4444SelfCheck()
    {
        Color32[] want = new Color32[16];
        byte[] little = new byte[32];
        byte[] big = new byte[32];

        for (int i = 0; i < 16; i++)
        {
            byte r = (byte)(15 - i);
            byte g = (byte)((i * 5) & 0xF);
            byte b = (byte)((i * 3) & 0xF);
            byte a = (byte)i;
            little[2 * i] = (byte)((g << 4) | b);
            little[2 * i + 1] = (byte)((a << 4) | r);
            big[2 * i] = (byte)((a << 4) | r);
            big[2 * i + 1] = (byte)((g << 4) | b);
            want[i] = new Color32((byte)(r * 17), (byte)(g * 17), (byte)(b * 17), (byte)(a * 17));
        }

        StringBuilder sb = new StringBuilder("4444直传字节序自检: ");
        (string, TextureFormat, byte[])[] rows =
        {
            ("ARGB4444+小端", TextureFormat.ARGB4444, little),
            ("ARGB4444+大端", TextureFormat.ARGB4444, big),
            ("RGBA4444+小端", TextureFormat.RGBA4444, little),
            ("RGBA4444+大端", TextureFormat.RGBA4444, big),
        };

        string first = null;
        foreach ((string name, TextureFormat format, byte[] raw) in rows)
        {
            string text;
            try
            {
                Texture2D probe = new Texture2D(16, 1, format, false);
                probe.LoadRawTextureData(raw);
                probe.Apply(false, false);
                Color32[] got = probe.GetPixels32();
                int bad = 0;

                for (int i = 0; i < 16; i++)
                {
                    if (Math.Abs(want[i].r - got[i].r) > 17 ||
                        Math.Abs(want[i].g - got[i].g) > 17 ||
                        Math.Abs(want[i].b - got[i].b) > 17 ||
                        Math.Abs(want[i].a - got[i].a) > 17)
                    {
                        bad++;
                    }
                }

                UnityEngine.Object.DestroyImmediate(probe);
                text = bad != 0 ? string.Format("错{0}/{1}", bad, 16) : "✅全对";
                if (first == null && bad == 0)
                {
                    first = name;
                }
            }
            catch (Exception ex)
            {
                text = "建不出:" + ex.GetType().Name;
            }

            sb.Append(name).Append('=').Append(text).Append("  ");
        }

        sb.Append("\n  ★结论: ");
        sb.Append(first != null
            ? "用【" + first + "】直传即可, 客户端照这个写。"
            : "**四种候选无一对得上** —— 别做 4444 直传, 先把这行发回来查(可能是 Unity 对 4444 做了额外的通道重排)。");
        return sb.ToString();
    }
}

}
