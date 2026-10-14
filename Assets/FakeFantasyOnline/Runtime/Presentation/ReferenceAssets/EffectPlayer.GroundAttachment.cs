using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class EffectPlayer
{
    internal const string UniqueGroundBeamResource = "object/DH/DH_FY_1501.gso";
    internal const float UniqueGroundBeamScale = 1f;
    internal const float UniqueGroundBeamFrameMilliseconds = 200f;
    private const string UniqueGroundBeamCacheKey = "ground-unique:approved-C";

    // Approved C keeps the native twenty-frame light filaments inside a tall soft taper.
    // It borrows the normal effect cache/materials, independently of map portal state/layers.
    internal static EffectPlayer PlayAttachedGround(Transform owner, SpriteRenderer sort, float ppu)
    {
        if (owner == null || ResLoader.I == null) return null;
        if (!EnsureUniqueBeamC()) return null;
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
        beam.uniqueBeamC = true;
        // Play populates at the general over-world order. Set the item-relative order immediately,
        // rather than exposing a single initial frame above every actor before its first Update.
        for (int index = 0; index < beam.rends.Length; index++)
        {
            SpriteRenderer renderer = beam.rends[index];
            if (renderer == null) continue;
            renderer.transform.localScale = new Vector3(UniqueGroundBeamScale, UniqueGroundBeamScale, 1f);
            KF initial = beam.Sample(beam.tpl.layers[index], 0f);
            renderer.transform.localPosition = new Vector3(initial.pos.x / ppu, initial.pos.z / ppu, 0f);
            renderer.color = initial.rgb;
            if (sort != null) renderer.sortingOrder = sort.sortingOrder - 10;
        }
        return beam;
    }
}
}
