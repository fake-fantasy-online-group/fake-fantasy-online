using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    private readonly Dictionary<int, bool> animFlag = new Dictionary<int, bool>();

    private bool IsAnimated(int code)
    {
        if (animFlag.TryGetValue(code, out bool animated)) return animated;
        int first = TerraShp.GetTile(code, 0);
        animated = TerraShp.GetTile(code, 1) != first || TerraShp.GetTile(code, 2) != first ||
            TerraShp.GetTile(code, 3) != first;
        animFlag[code] = animated;
        return animated;
    }

    private Sprite GetLegacySprite(int iid)
    {
        if (spriteCache.TryGetValue(iid, out Sprite cached)) return cached;
        Color32[] pixels = TileImgLib.DecodeTile(iid);
        var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels32(FlipV(pixels, 128, 64));
        texture.Apply(false, false);
        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 128, 64), new Vector2(0, 1), ppu);
        spriteCache[iid] = sprite;
        return sprite;
    }

    private void RefreshLegacy(float x0, float x1, float y0, float y1, bool force)
    {
        int width = gm.GridWidth, height = gm.GridHeight;
        float originX = height * 64 - 64;
        int minA = Mathf.FloorToInt((x0 - originX - 128f) / 64f) - 1;
        int maxA = Mathf.CeilToInt((x1 - originX) / 64f) + 1;
        int minB = Mathf.FloorToInt((y0 - 64f) / 32f) - 1;
        int maxB = Mathf.CeilToInt(y1 / 32f) + 1;
        if (!force && minA == lastMinA && maxA == lastMaxA && minB == lastMinB && maxB == lastMaxB) return;
        lastMinA = minA;
        lastMaxA = maxA;
        lastMinB = minB;
        lastMaxB = maxB;
        visibleNow.Clear();
        for (int b = minB; b <= maxB; b++)
            for (int a = minA; a <= maxA; a++)
            {
                if (((a ^ b) & 1) != 0) continue;
                int x = (a + b) >> 1, y = (b - a) >> 1;
                if (x < 0 || y < 0 || x >= width || y >= height) continue;
                int code = gm.Grids[x + y * width];
                if (code < 0) continue;
                bool animated = IsAnimated(code);
                int iid = TerraShp.GetTile(code, animated ? curFrame : 0);
                if (iid < 0) continue;
                long id = ((long)x << 32) | (uint)y;
                visibleNow.Add(id);
                if (active.ContainsKey(id)) continue;
                Sprite sprite = GetLegacySprite(iid);
                if (sprite == null) continue;
                Place(id, sprite, (height - y + x) * 64 - 64, (y + x) * 32);
                if (animated) animCode[id] = code;
            }
        Cull();
    }
}

}
