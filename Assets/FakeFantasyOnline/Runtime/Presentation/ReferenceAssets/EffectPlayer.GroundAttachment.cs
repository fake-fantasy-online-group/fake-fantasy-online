using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class EffectPlayer
{
    internal const string UniqueGroundBeamResource = "object/DH/DH_FY_1501.gso";
    internal const float UniqueGroundBeamScale = 0.65f;
    internal const float UniqueGroundBeamFrameMilliseconds = 200f;
    private const string UniqueGroundBeamCacheKey = "ground-unique:DH_FY_1501";

    // Reuse the original gold column's complete twenty-frame animation and foot anchor.
    // It borrows the normal effect cache/materials, independently of map portal state/layers.
    internal static EffectPlayer PlayAttachedGround(Transform owner, SpriteRenderer sort, float ppu)
    {
        if (owner == null || ResLoader.I == null) return null;
        if (!spriteCache.TryGetValue(UniqueGroundBeamCacheKey, out Sprite[] sprites) ||
            sprites == null || sprites.Length == 0 || sprites[0] == null)
        {
            // Missing resources are not cached: a late resource mount can recover on the next retry.
            byte[] bytes = ReadRes(UniqueGroundBeamResource);
            if (bytes == null) return null;
            DimgDecoder.Anim animation = DimgDecoder.DecodeAnim(bytes);
            if (!animation.ok || animation.f == null || animation.f.Length == 0) return null;
            var loaded = new List<Sprite>();
            foreach (DimgDecoder.FrameTex frame in animation.f)
            {
                if (frame.tex == null) continue;
                loaded.Add(MakeSprite(frame.tex, animation.boundCx, animation.boundCy,
                    frame.nLeft, frame.nTop, animation.scale));
            }
            if (loaded.Count == 0) return null;
            sprites = loaded.ToArray();
            spriteCache[UniqueGroundBeamCacheKey] = sprites;
            NoteFx(UniqueGroundBeamCacheKey, sprites);
            tplCache.Remove(UniqueGroundBeamCacheKey);
        }
        if (!tplCache.ContainsKey(UniqueGroundBeamCacheKey))
        {
            var layer = new Layer { sprite = UniqueGroundBeamCacheKey, speed = UniqueGroundBeamFrameMilliseconds,
                blendMode = 0, cField = -1f };
            float duration = sprites.Length * UniqueGroundBeamFrameMilliseconds;
            layer.kfs.Add(new KF { t = 0f, pos = Vector3.zero,
                scale = new Vector2(UniqueGroundBeamScale, UniqueGroundBeamScale), rgb = Color.white });
            layer.kfs.Add(new KF { t = duration, pos = Vector3.zero,
                scale = new Vector2(UniqueGroundBeamScale, UniqueGroundBeamScale), rgb = Color.white });
            var template = new Template { lifeMs = duration };
            template.layers.Add(layer);
            tplCache[UniqueGroundBeamCacheKey] = template;
        }
        EffectPlayer beam;
        float pendingScale = NextScale;
        try
        {
            NextScale = 1f;
            beam = PlaySustained(owner.position, UniqueGroundBeamCacheKey, ppu, owner, 0f);
        }
        finally { NextScale = pendingScale; }
        if (beam == null) return null;
        beam.transform.SetParent(owner, true);
        beam.attachSort = sort;
        beam.attachUnder = true;
        // Play populates at the general over-world order. Set the item-relative order immediately,
        // rather than exposing a single initial frame above every actor before its first Update.
        foreach (SpriteRenderer renderer in beam.rends)
        {
            if (renderer == null) continue;
            renderer.transform.localScale = new Vector3(UniqueGroundBeamScale, UniqueGroundBeamScale, 1f);
            if (sort != null) renderer.sortingOrder = sort.sortingOrder - 10;
        }
        return beam;
    }
}
}
