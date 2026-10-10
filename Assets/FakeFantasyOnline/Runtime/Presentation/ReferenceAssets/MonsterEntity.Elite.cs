using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official elite-monster effect owner.
/// </summary>
public partial class MonsterEntity
{
    public byte eliteKind;
    private bool eliteFresh;
    private EffectPlayer eliteRing;

    private void ShowEliteFx()
    {
        eliteRing = EffectPlayer.PlaySustained(
            transform.position,
            "TFX304",
            ppu,
            transform,
            0f);
        if (eliteRing)
        {
            eliteRing.transform.SetParent(transform, true);
            eliteRing.attachSort = body;
            eliteRing.attachUnder = true;
        }

        if (eliteFresh)
        {
            EffectPlayer.PlayNamed(
                transform.position,
                "TFX303",
                ppu,
                1,
                -1,
                false,
                false);
        }
    }
}

}
