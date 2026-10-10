using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : ISkillCastReceiveOwnerProvider
{
    // The context-free pump borrows the same live view as the contextual pump. A headless
    // owner has no projection and leaves the established no-world retention path intact.
    public Func<ISkillCastReceiveOwner> SkillCastOwnerAtReceiptForIndependentRuntime { private get; set; }

    public ISkillCastReceiveOwner SkillCastOwnerAtReceipt =>
        SkillCastOwnerAtReceiptForIndependentRuntime?.Invoke();

    public void ReceiveSkillCast(WorldSkillCastState state) =>
        ReceiveSkillCastForIndependentRuntime(state);

    internal static void ReceiveSkillCastForIndependentRuntime(WorldSkillCastState state)
    {
        state.Receipt.ApplyNativeOwner(() =>
        {
            // Dispatch 0x800e, ARM2582ac/2582b8 and x862671ac/2671b6: both
            // side effects precede caster identity, including preparation/nonlocal packets.
            byte scale = state.ScalePercent.GetValueOrDefault();
            EffectPlayer.NextScale = scale >= 100 ? scale / 100f : 1f;
            bool physical = SkillEffectDB.IsPhysical(state.SkillId);
            if (state.CasterEntityId != 0 && state.CasterEntityId != NetClient.SelfEntityId)
            {
                ReceiveNonlocalCast(state);
                return;
            }

            // This capture precedes either phase. Later singleton reads deliberately do not
            // substitute a different player Transform for this ring/charge/effect anchor.
            Transform caster = CombatNumber.PlayerTransform;
            if (state.Phase == 0)
            {
                if (state.CooldownMilliseconds >= 1)
                    GameHud.StartSkillCooldown(state.SkillId, state.CooldownMilliseconds / 1000f);
                SkillSoundDB.PlayCast(state.SkillId);
                Win05.SkInfo info = Win05.SkillInfo(state.SkillId);
                Vector3? ground = NetClient.PendingCastSkill == state.SkillId
                    ? NetClient.PendingCastGround : null;
                if (info != null)
                {
                    bool centeredShape = info.shape == 2 && info.dist < 1;
                    if (!ground.HasValue && !centeredShape && info.canEnemy != 0 &&
                        (info.tgt == 1 || info.castkind == 1) && CombatWorld.HasTarget)
                        ground = CombatWorld.TargetCenterPos;
                }
                if (ground.HasValue && PlayerController.I != null)
                    PlayerController.I.AimFace(ground.Value);
                if (state.ChantHoldMilliseconds < 1) return;
                if (PlayerController.I != null)
                    PlayerController.I.PlayChant(state.ChantHoldMilliseconds / 1000f);
                if (caster != null)
                {
                    string name = SkillElementDB.ChargeEff(state.SkillId);
                    if (name != null)
                    {
                        EffectPlayer fx = EffectPlayer.PlaySustained(caster.position, name,
                            CombatNumber.Ppu, caster, state.ChantHoldMilliseconds / 1000f);
                        if (PlayerController.I != null) PlayerController.I.SetChargeFx(fx);
                    }
                }
                return;
            }

            Win05.SkInfo releaseInfo = Win05.SkillInfo(state.SkillId);
            Vector3? releaseGround = NetClient.PendingCastSkill == state.SkillId
                ? NetClient.PendingCastGround : null;
            if (releaseInfo != null)
            {
                bool centeredShape = releaseInfo.shape == 2 && releaseInfo.dist < 1;
                if (SkillTargeting.SelfCenteredCircle(releaseInfo) && caster != null)
                    SkillRangeRing.Show(caster.position, releaseInfo.radius, CombatNumber.Ppu);
                if (!releaseGround.HasValue && !centeredShape && releaseInfo.canEnemy != 0 &&
                    (releaseInfo.tgt == 1 || releaseInfo.castkind == 1) && CombatWorld.HasTarget)
                    releaseGround = CombatWorld.TargetCenterPos;
            }

            Transform target = null;
            int targetId = state.TargetEntityId.GetValueOrDefault();
            if (targetId != 0 && targetId != NetClient.SelfEntityId)
            {
                RemotePlayer player = CombatWorld.FindPlayer(targetId);
                target = player != null ? player.transform : CombatWorld.MonsterTransform(targetId);
            }
            // Receiver is re-read after the Unity null test and captured before evaluating
            // target.position. No frame.Player surrogate, warning-return, or preflight gate.
            if (PlayerController.I != null)
                PlayerController.I.PlayCast(physical, target != null ? target.position : releaseGround);

            // This second lookup is native and occurs after PlayCast. Preserve the separate
            // Unity target tests and the target-or-caster capture before the PPU getter.
            Win05.SkInfo supportInfo = Win05.SkillInfo(state.SkillId);
            bool hasTarget = target != null;
            bool support = supportInfo != null && hasTarget &&
                supportInfo.canFriendly == 1 && supportInfo.canEnemy == 0;
            Transform castTarget = target != null ? target : caster;
            float ppu = CombatNumber.Ppu;
            Vector3? castGround = target != null ? null : releaseGround;
            SkillCaster.Cast(state.SkillId, caster, castTarget, ppu, castGround, support);
            SkillSoundDB.PlayFire(state.SkillId);
        });
    }

}

}
