using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity world-space helpers: the two coordinate conversions that put a
/// monster at its map position and the head anchor the name tag and head effects hang from.
/// </summary>
public partial class MonsterEntity
{
    private Vector3 WorldToLocal(float wx, float wy)
    {
        return new Vector3((wx - worldW * 0.5f) / ppu, (worldH * 0.5f - wy) / ppu, 0f);
    }

    private int SortFor(float wy) => Mathf.Clamp((int)wy, 0, 30000);

    private Vector3 HeadPos()
    {
        return transform.position + new Vector3(0f, headTopU + 0.15f, 0f);
    }

    private static Material GlowMat() => EffectPlayer.NpcGlowLayerMat();

    private bool HandHasDir =>
        handFrames != null && curDir < handFrames.GetLength(0) && handFrames.GetLength(1) > 0;

    private Sprite AnchorSprite(Texture2D t, int bcx, int bcy, int nLeft, int nTop, int scale)
    {
        return AnchorSpriteAt(t, bcx, bcy, nLeft, nTop, scale, ppu);
    }
}

}
