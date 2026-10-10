using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity grid cache accounting: how many sprites the monster grids and
/// their glow copies hold and how many megabytes their textures occupy.
/// </summary>
public partial class MonsterEntity
{
    private static string GridCacheStat()
    {
        long bytes = 0;
        int frames = 0;

        foreach (Sprite[,] grid in _gridCache.Values)
        {
            if (grid == null)
            {
                continue;
            }

            for (int dir = grid.GetLowerBound(0); dir <= grid.GetUpperBound(0); dir++)
            {
                for (int frame = grid.GetLowerBound(1); frame <= grid.GetUpperBound(1); frame++)
                {
                    Sprite sprite = grid[dir, frame];
                    if (sprite == null)
                    {
                        continue;
                    }

                    Texture2D texture = sprite.texture;
                    if (texture == null)
                    {
                        continue;
                    }

                    TextureFormat format = texture.format;
                    int perPixel = format == TextureFormat.ARGB4444 || format == TextureFormat.RGBA4444
                        ? 2
                        : 4;
                    long size = (long)texture.width * (long)texture.height * perPixel;
                    if (texture.mipmapCount > 1)
                    {
                        size = size * 4 / 3;
                    }

                    bytes += size;
                    frames++;
                }
            }
        }

        foreach (Sprite[,] grid in _glowGridCache.Values)
        {
            if (grid == null)
            {
                continue;
            }

            for (int dir = grid.GetLowerBound(0); dir <= grid.GetUpperBound(0); dir++)
            {
                for (int frame = grid.GetLowerBound(1); frame <= grid.GetUpperBound(1); frame++)
                {
                    Sprite sprite = grid[dir, frame];
                    if (sprite == null)
                    {
                        continue;
                    }

                    Texture2D texture = sprite.texture;
                    if (texture == null)
                    {
                        continue;
                    }

                    bytes += (long)texture.width * (long)texture.height * 4;
                    frames++;
                }
            }
        }

        return string.Format(
            "怪物网格 {0}种/{1}帧/{2:F1}MB",
            _gridCache.Count,
            frames,
            (float)bytes / 1048576f);
    }
}

}
