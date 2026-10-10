using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static void OnCombat(int attackerId, int targetId, int dmg, int flags, int targetHp)
    {
        if (!I) return;
        int critical = flags & 1;
        if (targetId == NetClient.SelfEntityId && targetId != 0)
        {
            if (I.monsters.TryGetValue(attackerId, out MonsterEntity selfAttacker) && selfAttacker)
                selfAttacker.PlayAttack();
            Transform player = CombatNumber.PlayerTransform;
            if (I.players.TryGetValue(attackerId, out RemotePlayer remoteAttacker) && remoteAttacker)
            {
                remoteAttacker.PlayAttack(player ? (Vector3?)player.position : null);
                if (player) PlayRemoteProjectile(remoteAttacker, player, (flags & 8) != 0);
            }
            float head = 80f / I.ppu;
            if ((flags & 2) != 0)
            {
                if (player) MissFx.Spawn(player.position + Vector3.up * head, I.ppu);
                return;
            }
            if (player) CombatNumber.Spawn(player.position + Vector3.up * head, dmg, 2 | critical, I.ppu);
            if (PlayerController.I) PlayerController.I.PlayHurt();
            if (player)
            {
                int magnitude = dmg < 0 ? unchecked(-dmg) : dmg;
                string effect = critical != 0 ? "BFX007" : "BFX00" + (magnitude % 3 + 4);
                EffectPlayer.Play(player.position + Vector3.up * 0.5f, effect, I.ppu, 1, -1, false, false);
            }
            SoundManager.Play(CharData.gender == 1 ? 1701 : 1702);
            return;
        }

        if (I.players.TryGetValue(attackerId, out RemotePlayer attackerRemote) && attackerRemote)
        {
            Transform targetTransform = null;
            if (I.monsters.TryGetValue(targetId, out MonsterEntity aimMonster) && aimMonster)
                targetTransform = aimMonster.transform;
            else if (I.players.TryGetValue(targetId, out RemotePlayer aimPlayer) && aimPlayer)
                targetTransform = aimPlayer.transform;
            attackerRemote.PlayAttack(targetTransform ? (Vector3?)targetTransform.position : null);
            if (targetTransform) PlayRemoteProjectile(attackerRemote, targetTransform, (flags & 8) != 0);
        }
        if (I.players.TryGetValue(targetId, out RemotePlayer targetRemote) && targetRemote)
        {
            int selfId = NetClient.SelfEntityId;
            float head = 80f / I.ppu;
            if ((flags & 2) == 0)
            {
                targetRemote.SetHp(targetHp);
                if (attackerId != selfId) return;
                CombatNumber.Spawn(targetRemote.transform.position + Vector3.up * head,
                    dmg, critical, I.ppu);
                int variation = unchecked(targetRemote.id + dmg);
                int magnitude = variation < 0 ? unchecked(-variation) : variation;
                string effect = critical != 0 ? "BFX007" : "BFX00" + (magnitude % 3 + 4);
                EffectPlayer.Play(targetRemote.transform.position + Vector3.up * 0.5f,
                    effect, I.ppu, 1, -1, false, false);
                if ((flags & 8) == 0) SoundManager.Play(HitSnd(CharData.weaponCType, critical != 0));
            }
            else if (attackerId == selfId)
                MissFx.Spawn(targetRemote.transform.position + Vector3.up * head, I.ppu);
            return;
        }
        if (!I.monsters.TryGetValue(targetId, out MonsterEntity targetMonster) || !targetMonster) return;
        I.monsters.TryGetValue(attackerId, out MonsterEntity attackerMonster);
        bool attackerNoAttack = attackerMonster ? attackerMonster.NoAttack : false;
        if (attackerMonster) attackerMonster.PlayAttack();
        bool feedback = attackerId == NetClient.SelfEntityId || attackerNoAttack || targetMonster.NoAttack;
        if ((flags & 16) != 0)
        {
            if (dmg > 0) CombatNumber.Spawn(targetMonster.transform.position + Vector3.up * (80f / I.ppu),
                dmg, 5, I.ppu);
            targetMonster.SetHp(targetHp);
            return;
        }
        if ((flags & 2) != 0)
        {
            if (feedback) MissFx.Spawn(targetMonster.transform.position + Vector3.up * (80f / I.ppu), I.ppu);
        }
        else if (feedback)
        {
            int numberType = critical | (targetMonster.NoAttack ? 2 : 0);
            CombatNumber.Spawn(targetMonster.transform.position + Vector3.up * (80f / I.ppu),
                dmg, numberType, I.ppu);
            int variation = unchecked(targetMonster.id + dmg);
            int magnitude = variation < 0 ? unchecked(-variation) : variation;
            string effect = critical != 0 ? "BFX007" : "BFX00" + (magnitude % 3 + 4);
            EffectPlayer.Play(targetMonster.CenterPos(), effect, I.ppu, 1, -1, false, false);
            if ((flags & 8) == 0) SoundManager.Play(HitSnd(CharData.weaponCType, critical != 0));
        }
        targetMonster.SetHp(targetHp);
        if (((flags & 4) != 0 || targetHp <= 0) && !targetMonster.IsDying) targetMonster.BeginDie();
    }
}

}
