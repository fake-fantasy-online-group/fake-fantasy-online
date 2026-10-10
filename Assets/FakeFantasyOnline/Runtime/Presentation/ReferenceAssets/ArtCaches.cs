using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 ArtCaches, tokens 06000b65-06000b6a. It is the shared texture
// census used by the in-game memory dump: TexBytes prices one Texture2D in the
// same "raw pixels + mip" scale the art budgets use, Tally walks every art owner
// with a TexTally<Texture2D>, and Dump/DumpTextures render the two report lines.
public static partial class ArtCaches
{
    // Official 1.5.8 token 06000b66.
    public static long TexBytes(Texture2D t)
    {
        long bytes = (long)t.width * (long)t.height;
        switch (t.format)
        {
            // 1 byte per pixel: Alpha8 (1), DXT5 (12), BC7 (25), R8 (63).
            case TextureFormat.Alpha8:
            case TextureFormat.DXT5:
            case TextureFormat.BC7:
            case TextureFormat.R8:
                break;
            // 2 bytes per pixel: ARGB4444 (2), RGB565 (7), RGBA4444 (13).
            case TextureFormat.ARGB4444:
            case TextureFormat.RGB565:
            case TextureFormat.RGBA4444:
                bytes *= 2;
                break;
            case TextureFormat.RGB24:
                bytes *= 3;
                break;
            case TextureFormat.DXT1:
                bytes /= 2;
                break;
            default:
                bytes *= 4;
                break;
        }

        if (t.mipmapCount > 1)
        {
            bytes = bytes * 4L / 3L;
        }

        return bytes;
    }

    // Official 1.5.8 token 06000b68.
    public static long AccountedBytes()
    {
        long accounted;
        int accountedCount;
        long rawAccounted;
        Tally(out accounted, out accountedCount, out rawAccounted);
        return accounted;
    }

    // Official 1.5.8 token 06000b67.
    public static List<string> Tally(out long accounted, out int accountedCount, out long rawAccounted)
    {
        TexTally<Texture2D> ta = new TexTally<Texture2D>();
        HashSet<Texture2D> grmsSeen = new HashSet<Texture2D>();
        long grmsSum = 0L;
        Action<Texture2D> each = delegate (Texture2D t)
        {
            if (t == null)
            {
                return;
            }

            long bytes = TexBytes(t);
            ta.AddRaw(t, bytes);
            if (grmsSeen.Add(t))
            {
                grmsSum += Profiler.GetRuntimeMemorySizeLong(t);
            }
        };
        List<string> lines = new List<string>();
        ta.Begin();
        PlayerAvatar.CollectTextures(each);
        lines.Add(ta.End("纸娃娃部件"));
        ta.Begin();
        MonsterEntity.CollectTextures(each);
        lines.Add(ta.End("怪物网格"));
        ta.Begin();
        MountAvatar.CollectTextures(each);
        lines.Add(ta.End("坐骑"));
        if (ResLoader.I != null)
        {
            ta.Begin();
            ResLoader.I.CollectNpcTextures(each);
            lines.Add(ta.End("NPC美术"));
            ta.Begin();
            ResLoader.I.CollectObjTextures(each);
            lines.Add(ta.End("物件源纹理"));
            ta.Begin();
            ResLoader.I.CollectUiTextures(each);
            lines.Add(ta.End("★不清的tex/ui"));
        }

        if (GameBootstrap.I != null)
        {
            ta.Begin();
            GameBootstrap.I.CollectMapObjTextures(each);
            lines.Add(ta.End("地图物件/切片/树"));
        }

        ta.Begin();
        EffectPlayer.CollectTextures(each);
        lines.Add(ta.End("特效帧"));
        if (FloorRenderer.Any != null)
        {
            ta.Begin();
            FloorRenderer.Any.CollectTextures(each);
            lines.Add(ta.End("地板瓦片"));
        }

        accounted = ta.TotalBytes;
        accountedCount = ta.TotalCount;
        rawAccounted = grmsSum;
        return lines;
    }

    // Official 1.5.8 token 06000b65.
    public static string Dump(string tag)
    {
        StringBuilder sb = new StringBuilder();
        List<string> lines = Tally(out long accounted, out int accountedCount, out long rawAccounted);
        sb.Append("[内存] ");
        sb.Append(tag);
        sb.Append(" | ");
        sb.Append(string.Join(" · ", lines));
        sb.Append("\n  额外: ");
        sb.Append(PlayerAvatar.SpriteCacheStat());
        sb.Append(' ');
        sb.Append(PlayerAvatar.LiveAvatarStat());
        sb.Append(" · ");
        sb.Append(PlayerAvatar.FileCacheStat());
        if (ResLoader.I != null)
        {
            sb.Append(" · ");
            sb.Append(ResLoader.I.NpcArtStat());
            sb.Append(" · ");
            sb.Append(ResLoader.I.MapImgBudgetStat());
        }

        sb.Append(" · ");
        sb.Append(MountAvatar.CacheStat());
        sb.Append(" · ");
        sb.Append(MonsterEntity.GridBudgetStat());
        sb.Append(" · ");
        sb.Append(EffectPlayer.FxBudgetStat());
        if (GameBootstrap.I != null)
        {
            sb.Append(" · ");
            sb.Append(GameBootstrap.I.MapObjBudgetStat());
        }

        long gpu = (long)Texture.currentTextureMemory;
        long managed = GC.GetTotalMemory(false);
        long reserved = Profiler.GetTotalReservedMemoryLong();
        sb.Append("\n  ‖ 已记账 ");
        sb.Append(accountedCount);
        sb.Append("张/");
        sb.Append((accounted / 1048576f).ToString("F0"));
        sb.Append("MB(裸像素+mip, 与预算同尺)");
        sb.Append(" | 纹理显存(GPU) ");
        sb.Append((gpu / 1048576f).ToString("F0"));
        sb.Append("MB");
        sb.Append("(盲区 ");
        sb.Append(((gpu - accounted) / 1048576f).ToString("F0"));
        sb.Append("MB");
        if (gpu > 0)
        {
            sb.Append('=');
            sb.Append((((gpu - accounted) * 100f) / gpu).ToString("F0"));
            sb.Append('%');
        }

        sb.Append(", 负数=记账重复 ⇒ 跑 DumpTextures()");
        sb.Append("\n  ‖ 托管堆 ");
        sb.Append((managed / 1048576f).ToString("F0"));
        sb.Append("MB");
        sb.Append(" | Unity保留(CPU侧) ");
        sb.Append((reserved / 1048576f).ToString("F0"));
        sb.Append("MB(峰值水位, 释放不回落)");
        sb.Append(" ⇒ 其中非托管 ");
        sb.Append(((reserved - managed) / 1048576f).ToString("F0"));
        sb.Append("MB");
        sb.Append(" | Profiler口径 ");
        sb.Append((rawAccounted / 1048576f).ToString("F0"));
        sb.Append("MB(恒约2.2×裸像素, 只作参考别减)");
        return sb.ToString();
    }

    // Official 1.5.8 token 06000b69.
    public static string DumpTextures(int topN = 14)
    {
        Dictionary<string, (int, long, string)> groups =
            new Dictionary<string, (int, long, string)>();
        long total = 0L;
        Texture2D[] textures = Resources.FindObjectsOfTypeAll<Texture2D>();
        for (int index = 0; index < textures.Length; index++)
        {
            Texture2D t = textures[index];
            if (t == null)
            {
                continue;
            }

            long bytes = TexBytes(t);
            string key = string.Format("{0}x{1} {2}", t.width, t.height, t.format)
                       + (t.mipmapCount < 2 ? string.Empty : " +mip");
            total += bytes;
            if (groups.TryGetValue(key, out (int, long, string) group))
            {
                string sample = group.Item3;
                if (string.IsNullOrEmpty(sample))
                {
                    sample = t.name;
                }

                groups[key] = (group.Item1 + 1, group.Item2 + bytes, sample);
            }
            else
            {
                groups[key] = (1, bytes, t.name);
            }
        }

        List<KeyValuePair<string, (int, long, string)>> ranked =
            new List<KeyValuePair<string, (int, long, string)>>(groups);
        ranked.Sort((left, right) => right.Value.Item2.CompareTo(left.Value.Item2));
        StringBuilder sb = new StringBuilder();
        long accounted;
        int accountedCount;
        long rawAccounted;
        Tally(out accounted, out accountedCount, out rawAccounted);
        sb.Append(string.Format("[内存·普查] 活纹理 {0} 张 / 合计 {1:F0}MB",
            textures.Length, total / 1048576f));
        sb.Append(string.Format(" | 已记账 {0:F1}MB / **未记账 {1:F0}MB**",
            accounted / 1048576f, (total - accounted) / 1048576f));
        sb.Append(string.Format(" | Unity报告显存 {0:F0}MB",
            Texture.currentTextureMemory / 1048576f));
        int count = ranked.Count;
        if (topN > 0 && count > 0)
        {
            for (int index = 0; index < topN && index < count; index++)
            {
                KeyValuePair<string, (int, long, string)> entry = ranked[index];
                sb.Append(string.Format("  {0,7:F1}MB  ×{1,-5} {2}",
                    entry.Value.Item2 / 1048576f, entry.Value.Item1, entry.Key));
                if (!string.IsNullOrEmpty(entry.Value.Item3))
                {
                    sb.Append("   例: ");
                    sb.Append(entry.Value.Item3);
                }

                sb.Append('\n');
            }
        }

        if (topN < count)
        {
            long rest = 0L;
            for (int index = topN; index < count; index++)
            {
                rest += ranked[index].Value.Item2;
            }

            sb.Append(string.Format("  (其余 {0} 组共 {1:F1}MB)", count - topN, rest / 1048576f));
        }

        return sb.ToString().TrimEnd();
    }

    // Official 1.5.8 token 06000b6a.
    public static void EvictAtSafePoint()
    {
        PlayerAvatar.EvictSpriteCacheAtSafePoint();
        if (ResLoader.I != null)
        {
            ResLoader.I.EvictNpcArtAtSafePoint();
            ResLoader.I.EvictMapImagesAtSafePoint();
        }
        EffectPlayer.EvictAtSafePoint();
    }
}

}
