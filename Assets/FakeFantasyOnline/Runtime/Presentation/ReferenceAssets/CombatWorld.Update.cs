using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // Official 0600016c. Unity is the only caller: no host Tick, port, or manual driver invokes
    // this method. The component created by either Ensure or the world frame stays enabled;
    // frame teardown disables its own component before releasing targets. Relative ordering
    // with other MonoBehaviour Updates is Unity-owned, not inferred from the old host stages.
    private void Update()
    {
        if (queuedSkillId > 0 && pendSkillId == 0 && Time.time >= castPauseUntil)
        {
            int skill = queuedSkillId;
            float range = queuedRangePx;
            queuedSkillId = 0;
            ResumeQueuedSkillForPlugins(skill, range);
        }

        AdvanceGroundCastForIndependentRuntime();
        if (pendSkillId > 0)
        {
            if (Time.time > pendAwaitUntil) pendSkillId = 0;
            else if (target != null && TargetWithin(pendSkillRangeU))
            {
                CastAtTarget(pendSkillId);
                pendSkillId = 0;
            }
        }

        // One target-kind split owns the entire remaining frame, including the loss return.
        // Re-read receiver fields inside the taken arm; do not capture the target for the frame.
        if (pvpTarget != null)
        {
            if (pvpTarget.IsDead || pvpTarget.HiddenByInvis)
            {
                pvpTarget = null;
                autoAttacking = false;
                atkState = AtkState.Idle;
                atkPhaseT = 0f;
                TargetSelect.Clear();
                return;
            }
            AdvancePvpAttackForIndependentRuntime();
            return;
        }

        if (target != null && target.DyingForIndependentRuntime)
        {
            target = null;
            autoAttacking = false;
            pendSkillId = 0;
            queuedSkillId = 0;
            pendGroundSkill = 0;
            atkState = AtkState.Idle;
            atkPhaseT = 0f;
            TargetSelect.Clear();
            return;
        }

        // Null target, disabled autoattack, pause and range failure clear only these two fields.
        // In particular, a lost target preserves autoAttacking and TargetSelect (WB-174).
        if (target == null || !autoAttacking || Time.time < castPauseUntil || !InRange())
        {
            atkState = AtkState.Idle;
            atkPhaseT = 0f;
            return;
        }
        if (!LosClear())
        {
            atkState = AtkState.Idle;
            atkPhaseT = 0f;
            // Both raw bodies reject unordered too: ARM B.LT / x86 JB, unlike the ordinary
            // pause gate above (ARM B.MI / x86 JA with the operands reversed).
            if (!(Time.time >= losTipUntil)) return;
            skillTipForIndependentRuntime("无法攻击到目标", UIScale.Factor);
            losTipUntil = Time.time + 1.5f;
            return;
        }

        atkPhaseT += Time.deltaTime;
        switch (atkState)
        {
            case AtkState.Idle:
                atkState = AtkState.Windup;
                atkPhaseT = 0f;
                return;
            case AtkState.Windup:
                if (PlayerController.I != null && PlayerController.I.IsMoving)
                {
                    atkPhaseT = 0f;
                    return;
                }
                if (!(atkPhaseT >= 0f)) return;
                // TryAttack's normal early return still reaches Recovery. An exception does
                // not: the state/phase stores occur only after the void call returned.
                TryAttack();
                atkState = AtkState.Recovery;
                atkPhaseT = 0f;
                return;
            case AtkState.Recovery:
                if (!(atkPhaseT >= AtkInterval)) return;
                atkState = AtkState.Idle;
                atkPhaseT = 0f;
                return;
        }
        // An unknown state keeps the delta-time accumulation and otherwise does nothing.
    }
}

}
