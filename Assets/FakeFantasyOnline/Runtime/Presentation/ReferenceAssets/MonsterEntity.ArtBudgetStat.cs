using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{

    // Official 1.5.8 token 06000626. Feeds every cached monster grid sprite to
    // the caller's sink so a caller can price them without knowing the layout:
    // the main grids, the glow grids, then the single "first frame" sprites.
    public static void CollectTextures(Action<Texture2D> sink)
    {
        foreach (Sprite[,] grid in _gridCache.Values)
        {
            if (grid == null)
            {
                continue;
            }

            foreach (Sprite sprite in grid)
            {
                if (sprite != null)
                {
                    sink(sprite.texture);
                }
            }
        }

        foreach (Sprite[,] grid in _glowGridCache.Values)
        {
            if (grid == null)
            {
                continue;
            }

            foreach (Sprite sprite in grid)
            {
                if (sprite != null)
                {
                    sink(sprite.texture);
                }
            }
        }

        foreach (Sprite sprite in _firstCache.Values)
        {
            if (sprite != null)
            {
                sink(sprite.texture);
            }
        }
    }

    // Official 1.5.8 token 0600060a.
    public static string GridBudgetStat()
    {
        long bytes = GridCacheBytes;
        int live = 0;
        foreach (KeyValuePair<int, int> entry in _gridRef)
        {
            if (entry.Value > 0)
            {
                live++;
            }
        }

        return string.Format("怪宠网格预算 {0:F0}/{1:F0}MB",
                   bytes / 1048576f, GridBudgetBytes / 1048576f)
             + string.Format("(在用{0}/{1}套", live, _gridCache.Count)
             + (_evictedActs < 1
                 ? string.Empty
                 : string.Format(" 已淘汰{0}套/{1:F0}MB",
                       _evictedActs, _evictedBytes / 1048576f))
             + (bytes <= GridBudgetBytes ? string.Empty : " ⚠超预算")
             + ")";
    }
}

}
