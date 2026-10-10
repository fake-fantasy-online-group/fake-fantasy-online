using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    // Existing source-owned BlobShadow component, preserving its recovered field mapping.
    private ReferenceWorldBlobShadow shadow;
    private float _artDeriveT0;
    private const float ArtDeriveTimeoutSec = 3f;

    private void SyncShadowSize()
    {
        if (!shadow || shadow.diameterU > 0f) return;
        int primary = standAct > 0 ? standAct : walkAct;
        int width = FrameWOf(primary);
        if (width <= 0 && standAct > 0) width = FrameWOf(walkAct);
        if (width <= 0) width = BoundWOf(primary);
        if (width <= 0 && standAct > 0) width = BoundWOf(walkAct);
        if (width > 0) shadow.SetDiameterPx(width, ppu, 69);
    }

    private void EnsureArtDerived()
    {
        SyncShadowSize();
        if (_artDerived) return;
        if (_headTopCache.TryGetValue(model, out float head) &&
            _colorlessModel.TryGetValue(model, out bool mask))
        {
            headTopU = head;
            colorlessMask = mask;
            if (mask) TintColorlessMask();
            _artDerived = true;
            return;
        }
        Sprite sprite = StandSprite(curDir) ?? StandSprite(0);
        if (!sprite)
        {
            _artDeriveT0 = 0f;
            return;
        }
        if (_artDeriveT0 <= 0f) _artDeriveT0 = Time.realtimeSinceStartup;
        bool readable = sprite.texture && sprite.texture.isReadable;
        if (!readable && Time.realtimeSinceStartup - _artDeriveT0 < ArtDeriveTimeoutSec) return;
        TintColorlessMask();
        ComputeHeadTop(sprite);
        _artDerived = true;
    }

    private void TintColorlessMask()
    {
        if (!_colorlessModel.TryGetValue(model, out colorlessMask))
        {
            Sprite sprite = StandSprite(0);
            colorlessMask = sprite && sprite.texture && sprite.texture.isReadable &&
                IsColorlessMask(sprite.texture);
            _colorlessModel[model] = colorlessMask;
        }
        if (!colorlessMask || !_recoloredModel.Add(model)) return;
        var done = new HashSet<Texture2D>();
        RecolorGrid(standFrames, done);
        RecolorGrid(walkFrames, done);
    }

    private static bool IsColorlessMask(Texture2D texture)
    {
        Color32[] pixels = texture.GetPixels32();
        int opaque = 0, colored = 0;
        for (int i = 0; i < pixels.Length; i = unchecked(i + 7))
        {
            Color32 pixel = pixels[i];
            if (pixel.a < 17) continue;
            opaque = unchecked(opaque + 1);
            if (pixel.r > 16 || pixel.g > 16 || pixel.b > 16) colored = unchecked(colored + 1);
        }
        return opaque >= 31 && unchecked(colored * 100) / opaque < 3;
    }

    private void ComputeHeadTop(Sprite sprite)
    {
        if (_headTopCache.TryGetValue(model, out float cached))
        {
            headTopU = cached;
            return;
        }
        if (!sprite)
        {
            headTopU = 1f;
            return;
        }
        ComputeHeadTopUncached(sprite);
        _headTopCache[model] = headTopU;
    }

    private void ComputeHeadTopUncached(Sprite sprite)
    {
        if (!sprite)
        {
            headTopU = 1f;
            return;
        }
        headTopU = sprite.bounds.max.y;
        Texture2D texture = sprite.texture;
        if (!texture || !texture.isReadable) return;
        int width = texture.width, height = texture.height;
        Color32[] pixels = texture.GetPixels32();
        int top = -1;
        for (int y = height - 1; y >= 0 && top < 0; y--)
            for (int x = 0; x < width; x++)
                if (pixels[unchecked(y * width + x)].a >= 17)
                {
                    top = y;
                    break;
                }
        if (top < 0) return;
        float numerator = unchecked(top + 1) - sprite.pivot.y;
        float denominator = sprite.pixelsPerUnit > 0.001f ? sprite.pixelsPerUnit : ppu;
        if (numerator > 0f) headTopU = numerator / denominator;
    }

    internal void BindArtPresentationForIndependentRuntime(SpriteRenderer renderer,
        ReferenceWorldBlobShadow blobShadow)
    {
        body = renderer;
        shadow = blobShadow;
    }

    internal void SyncShadowSizeForIndependentRuntime() => SyncShadowSize();
    internal void EnsureArtDerivedForIndependentRuntime() => EnsureArtDerived();
    internal float HeadTopForIndependentRuntime => headTopU;
}

}
