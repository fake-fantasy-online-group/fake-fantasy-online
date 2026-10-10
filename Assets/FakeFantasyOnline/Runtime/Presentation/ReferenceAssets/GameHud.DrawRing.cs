using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // 06000420. The original instance dictionary is constructed in Initialization.
    // Its entries follow this GameHud receiver, including ordinary map transitions.
    internal void DrawRing(Texture2D source, Rect full, float fraction, Vector2 pivotImg, int ringId)
    {
        if (!source || fraction < 0f || fraction <= 0.001f)
        {
            return;
        }

        if (fraction >= 0.999f)
        {
            GUI.DrawTexture(full, source);
            return;
        }

        int quantized = Mathf.Clamp(
            (int)Math.Round(fraction * 40f, MidpointRounding.ToEven),
            0,
            40);
        int key = unchecked(100 * ringId + quantized);
        if (!ringMaskCache.TryGetValue(key, out Texture2D mask) || !mask)
        {
            mask = ReferenceFirstWorldPlayerStatusHud.BuildRingMask(
                pivotImg.x, pivotImg.y, quantized / 40f, source);
            ringMaskCache[key] = mask;
        }

        // Publish the exact result, including null or the borrowed source, before
        // the original final Unity-null gate. A dead cached texture is rebuilt above.
        if (mask)
        {
            GUI.DrawTexture(full, mask);
        }
    }
}
}
