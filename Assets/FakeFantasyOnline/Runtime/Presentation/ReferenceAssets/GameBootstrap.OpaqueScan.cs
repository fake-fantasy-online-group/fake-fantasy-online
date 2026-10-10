using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameBootstrap
{

    // Official 1.5.8 token 060003c3; returned right/bottom coordinates are exclusive.
    private static (int, int, int, int) OpaqueBounds(int imageId, Texture2D texture)
    {
        if (_opaqueBoundsCache.TryGetValue(imageId, out var cached)) return cached;
        int width = texture.width;
        int height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        int left = width, right = -1, top = height, bottom = -1;
        for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
                if (pixels[y * width + x].a > 16)
                {
                    left = Mathf.Min(left, x);
                    right = Mathf.Max(right, x);
                    int fromTop = height - 1 - y;
                    top = Mathf.Min(top, fromTop);
                    bottom = Mathf.Max(bottom, fromTop);
                }
        var bounds = right < 0 ? (0, width, 0, height) : (left, right + 1, top, bottom + 1);
        _opaqueBoundsCache[imageId] = bounds;
        return bounds;
    }

    // Official 1.5.8 token 060003e3.
    private static void ScanOpaque(Texture2D texture, out float cx, out float topf)
    {
        cx = 0.5f;
        topf = 0f;
        int width = texture.width;
        int height = texture.height;
        if (width <= 0 || height <= 0) return;
        Color32[] pixels = texture.GetPixels32();
        long sumX = 0, count = 0;
        int firstRow = -1;
        for (int y = height - 1; y >= 0; y--)
        {
            bool any = false;
            for (int x = 0; x < width; x++)
                if (pixels[y * width + x].a > 8)
                {
                    any = true;
                    sumX += x;
                    count++;
                }
            if (any && firstRow < 0) firstRow = height - 1 - y;
        }
        if (count > 0) cx = ((float)sumX / (float)count) / width;
        if (firstRow >= 0) topf = (float)firstRow / height;
    }
}

}
