using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    private SpriteRenderer nameTagSr;

    private void UpdateHpBar()
    {
        bool show = !dying && !friendly && maxHp > 0;
        float localY = headTopU + 17f / ppu;
        if (show && nameTagGo && nameTagGo.activeSelf && nameTagSr && nameTagSr.sprite)
            localY = Mathf.Max(localY, headTopU + 0.15f +
                unchecked(nameTagSr.sprite.texture.height + 2) / ppu);
        hpBarView.Tick(transform, show, hp, maxHp, localY, 0f);
    }

    internal HeadHpBarView HpBarForIndependentRuntime => hpBarView;
    internal void UpdateHpBarForIndependentRuntime() => UpdateHpBar();
}

}
