using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    private enum AtkState { Idle, Windup, Recovery }
    private AtkState atkState;
    private float atkPhaseT;
    private float castPauseUntil;
    private bool autoAttacking;
    private int pendSkillId;
    private int queuedSkillId;
    private int pendGroundSkill;

    public static void StopAttack()
    {
        if (I != null)
        {
            I.autoAttacking = false;
            I.atkState = AtkState.Idle;
            I.atkPhaseT = 0f;
            I.pendSkillId = 0;
            I.queuedSkillId = 0;
            I.pendGroundSkill = 0;
        }
    }

    internal bool AutoAttackingForIndependentRuntime => autoAttacking;
    internal int PendingSkillForIndependentRuntime
    {
        get => pendSkillId;
        set => pendSkillId = value;
    }
    internal int PendingGroundSkillForIndependentRuntime
    {
        get => pendGroundSkill;
        set => pendGroundSkill = value;
    }

    public static void NotifyCast(float seconds)
    {
        if (I != null)
        {
            I.castPauseUntil = Time.time + seconds;
            I.atkState = AtkState.Idle;
            I.atkPhaseT = 0f;
        }
    }

    public static Vector3? TargetCenterPos =>
        I != null && I.target != null ? I.target.CenterPos() : (Vector3?)null;

    // Read-only diagnostics. The complete Update owns the per-frame phase writes.
    internal bool CastPausedForIndependentRuntime => Time.time < castPauseUntil;
    internal int AttackStateForIndependentRuntime => (int)atkState;
    internal float AttackPhaseTimeForIndependentRuntime => atkPhaseT;
}

}
