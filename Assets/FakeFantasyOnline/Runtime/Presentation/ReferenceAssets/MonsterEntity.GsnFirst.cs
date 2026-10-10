using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official MonsterEntity first-frame Gsn loading: the cache wrapper and the decode that
/// fills it, both driven by the same npc gsn path the rest of the art pipeline reads.
/// </summary>
public partial class MonsterEntity
{
    private Sprite LoadGsnFirst(int m)
    {
        Sprite sprite;
        if (_firstCache.TryGetValue(m, out sprite))
        {
            return sprite;
        }

        sprite = DecodeGsnFirst(m);
        _firstCache[m] = sprite;
        return sprite;
    }

    private Sprite DecodeGsnFirst(int m)
    {
        byte[] bytes = ReadNpcGsn(m * 100 + 1, string.Empty, ArtDetail.PreferNativeArt);
        if (bytes == null)
        {
            return null;
        }

        DimgDecoder.Anim anim = DimgDecoder.DecodeAnim(bytes);
        if (!anim.ok || anim.f == null || anim.f.Length < 1)
        {
            return null;
        }

        DimgDecoder.FrameTex frame = anim.f[0];
        if (frame.tex == null)
        {
            return null;
        }

        return AnchorSprite(frame.tex, anim.boundCx, anim.boundCy, frame.nLeft, frame.nTop, anim.scale);
    }
}

}
