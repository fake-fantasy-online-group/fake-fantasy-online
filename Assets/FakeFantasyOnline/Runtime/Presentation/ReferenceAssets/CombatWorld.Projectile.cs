using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    private static void PlayRemoteProjectile(RemotePlayer atkRp, Transform tT, bool skillHit)
    {
        if (skillHit) return;
        var projectile = AtkProjectile(atkRp.WeaponCType, atkRp.AttackIdentityDistance);
        if (!projectile.HasValue) return;
        var specification = projectile.Value;
        float pixelsPerUnit = CombatNumber.Ppu > 0f ? CombatNumber.Ppu : 100f;
        Vector2 from = atkRp.transform.position;
        Vector2 to = tT.position;
        EffectPlayer.NextScale = specification.Item2;
        EffectPlayer effect = EffectPlayer.PlayNamed(atkRp.transform.position,
            specification.Item1, pixelsPerUnit, 1, -1, false, false);
        EffectPlayer.NextScale = 1f;
        if (effect)
            effect.MoveTo(tT, Mathf.Max(80f, Vector2.Distance(from, to) * pixelsPerUnit));
    }

    // Steps 8-10 of the official CombatWorld.TryAttack (06000170) tail. The projectile is NOT
    // inside the PlayerController.I null check that gates the swing: on both architectures the
    // swing is conditional while CombatWorld.AtkProjectile (0600012b) is called unconditionally
    // right after it, with the two CharData fields +0x11c and +0x120 - weaponCType and atkDist,
    // the latter the same field get_AtkRange (06000160) reads. Only a returned value plays the
    // named effect and moves it to the target; otherwise the body returns.
    // The callees are not re-implemented here: this reuses the same already-present bodies that
    // PlayRemoteProjectile (0600015b) uses for the same three dependencies (0600012b, 060002aa,
    // 060002a7), in the same order and with the same arguments - the scale slot is written before
    // PlayNamed and restored to 1 afterwards, and MoveTo takes the max against 80 ms.
    private void PlayAttackProjectile()
    {
        var projectile = AtkProjectile(CharData.weaponCType, CharData.AttackIdentityDistance);
        if (!projectile.HasValue) return;
        var specification = projectile.Value;
        Transform player = CombatNumber.PlayerTransform;
        if (player == null) return;
        MonsterEntity current = target;
        if (current == null) return;
        Vector2 from = player.position;
        Vector2 to = current.transform.position;
        float pixelsPerUnit = CombatNumber.Ppu > 0f ? CombatNumber.Ppu : 100f;
        EffectPlayer.NextScale = specification.Item2;
        EffectPlayer effect = EffectPlayer.PlayNamed(player.position,
            specification.Item1, pixelsPerUnit, 1, -1, false, false);
        EffectPlayer.NextScale = 1f;
        if (effect)
            effect.MoveTo(current.transform,
                Mathf.Max(80f, Vector2.Distance(from, to) * pixelsPerUnit));
    }
}

}
