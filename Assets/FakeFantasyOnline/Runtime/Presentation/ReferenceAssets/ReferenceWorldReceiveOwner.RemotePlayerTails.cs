using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IRemotePlayerTailReceiveOwner
{
    int IPlayerHairReceiveContext.SelfEntityId => NetClient.SelfEntityId;
    void IPlayerHairReceiveContext.ReceiveLocalPlayerHair(int head, int hair) => CharData.SetHair(head, hair);

    public void ReceiveRemoteHair(int id, int head, int hair) => CombatWorld.UpdatePlayerHair(id, head, hair);

    public void ReceiveLocalAppearance(WorldPlayerAppearanceState state)
    {
        CharacterAppearanceRecord a = state.Appearance;
        CharData.atkVariant = a.AttackVariant;
        CharData.weaponCType = a.WeaponCategory;
        CharData.atkDist = a.AttackDistance;
        CharData.SetAppearance(a.Body, a.Cap, a.Backpack, a.RightWeapon, a.LeftWeapon, a.Face);
        // Dispatch tests Unity liveness, then reads the singleton again at the call.
        if (PlayerController.I) PlayerController.I.SetSoulFx(state.SoulEffects);
        CharData.titleEffect = state.TitleEffect;
        int glow = state.GlowMode.GetValueOrDefault(1);
        CharData.glowMode = glow;
        if (CharData.GetPref("glowmode_", -1) != glow) CharData.SetPref("glowmode_", glow);
        int unlock = state.GlowUnlock.GetValueOrDefault(-1);
        if (unlock >= 0)
        {
            CharData.glowUnlock = unlock;
            if (CharData.GetPref("glowunlock_", -1) != unlock) CharData.SetPref("glowunlock_", unlock);
        }
        // The native local arm does not assign the decoded aura or load resources here.
        // PlayerController.Update owns the existing apVersion/LoadFrames boundary.
    }

    public void ReceiveRemoteAppearance(WorldPlayerAppearanceState state)
    {
        CharacterAppearanceRecord a = state.Appearance;
        CombatWorld.UpdatePlayerAppearance(state.EntityId, a.Body, a.Cap, a.Backpack,
            a.RightWeapon, a.LeftWeapon, a.Face, a.AttackVariant, state.AvatarAura,
            state.SoulEffects, state.TitleEffect, state.GlowMode.GetValueOrDefault(1));
        // This is a new lookup after all wrapper effects; its null check is physical.
        RemotePlayer player = CombatWorld.FindPlayer(state.EntityId);
        if (!ReferenceEquals(player, null)) player.SetWeapon(a.WeaponCategory, a.AttackDistance);
    }

    public void ReceiveRemoteRiding(WorldRemoteRidingState state)
    {
        RemotePlayer player = CombatWorld.FindPlayer(state.EntityId);
        if (!ReferenceEquals(player, null))
            player.SetRiding(state.Riding, state.MountModel, state.MountTianmo,
                state.RiderSeat, state.RideAnchorId, state.MountSeats);
    }

    public void ReceiveRemoteInvisibility(int id, bool invisible)
    {
        RemotePlayer player = CombatWorld.FindPlayer(id);
        if (!ReferenceEquals(player, null)) player.SetInvis(invisible);
    }

    // The shared cast owner has already claimed this receipt, written NextScale and queried
    // IsPhysical before selecting this nonlocal arm. Its native decisions stay unchanged.
    private static void ReceiveNonlocalCast(WorldSkillCastState state)
    {
        Vector2? aim = state.HasAim ? new Vector2(state.AimX, state.AimY) : (Vector2?)null;
        if (CombatWorld.PlayerCast(state.CasterEntityId, state.SkillId, state.Phase,
            state.TargetEntityId.GetValueOrDefault(), aim)) return;

        // Dispatch's fall-through remains part of this same receipt. Retaining it would
        // let a later same-id remote player acquire a cast the native lookup just missed.
        Transform caster = CombatWorld.MonsterTransform(state.CasterEntityId);
        if (!caster) return;
        if (state.Phase == 0)
        {
            CombatWorld.MonsterPlayPrepare(state.CasterEntityId);
            return;
        }
        Transform target = null;
        int targetId = state.TargetEntityId.GetValueOrDefault();
        if (targetId != 0)
        {
            if (targetId == NetClient.SelfEntityId) target = CombatNumber.PlayerTransform;
            else
            {
                RemotePlayer player = CombatWorld.FindPlayer(targetId);
                target = player ? player.transform : CombatWorld.MonsterTransform(targetId);
            }
        }
        Transform castTarget = target ? target : caster;
        float ppu = CombatNumber.Ppu;
        Vector3? ground = target ? target.position : (Vector3?)null;
        SkillCaster.Cast(state.SkillId, caster, castTarget, ppu, ground, false);
        SkillSoundDB.PlayFire(state.SkillId);
        CombatWorld.MonsterPlayAttack(state.CasterEntityId);
    }
}
}
