using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    private void OnDestroy()
    {
        ReleaseHeldGrids();
        if (friendly && CombatNumber.PetTransform == transform) CombatNumber.RegisterPet(null, 0f);
        if (eliteRing) Destroy(eliteRing.gameObject);
        if (petRing) Destroy(petRing.gameObject);
        if (nameTagSr && nameTagSr.sprite)
        {
            Texture2D texture = nameTagSr.sprite.texture;
            Destroy(nameTagSr.sprite);
            if (texture) Destroy(texture);
        }
        if (siSr && siSr.sprite)
        {
            Texture2D texture = siSr.sprite.texture;
            Destroy(siSr.sprite);
            if (texture) Destroy(texture);
        }
        hpBarView.Dispose();
    }
}

}
