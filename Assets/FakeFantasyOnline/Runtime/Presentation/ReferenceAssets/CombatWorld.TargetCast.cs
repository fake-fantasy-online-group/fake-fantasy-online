using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // The original field is retained; its SetPvpTarget lifecycle is separate recovery.
    private RemotePlayer pvpTarget;
    private int pendGroundGx;
    private int pendGroundGy;

    private Vector3? CurTargetPos => pvpTarget != null
        ? pvpTarget.transform.position
        : target != null ? target.transform.position : (Vector3?)null;

    public static bool TargetWorldPx(out int gx, out int gy)
    {
        gy = 0;
        gx = 0;
        if (I == null) return false;
        if (I.pendGroundSkill > 0)
        {
            gx = I.pendGroundGx;
            gy = I.pendGroundGy;
            return true;
        }
        Vector3 position;
        if (I.pvpTarget != null)
        {
            position = I.pvpTarget.transform.position;
        }
        else
        {
            if (I.target == null) return false;
            position = I.target.transform.position;
        }
        gx = unchecked((int)Math.Round((double)(position.x * I.ppu + I.worldW * 0.5f)));
        gy = unchecked((int)Math.Round((double)(I.worldH * 0.5f - position.y * I.ppu)));
        return true;
    }

    private void CastAtTarget(int skillId)
    {
        if (AuthoritativeProfessionSkill.Uses(skillId))
        {
            var behavior = AuthoritativeProfessionSkill.Current(skillId);
            // A pending entity cast has already approached its target. Re-entering
            // RequestSkill here only queues it again; Update then clears that queue.
            float range = behavior != null && behavior.CastDistance > 0
                ? behavior.CastDistance / (ppu > 0f ? ppu : 100f)
                : float.PositiveInfinity;
            if (behavior != null && behavior.InputMode == "entity" && target != null &&
                pvpTarget == null && !(TargetSelect.CurrentSourceForIndependentRuntime is RemotePlayer) &&
                Time.time >= castPauseUntil && TargetWithin(range))
            {
                string error = AuthoritativeProfessionSkill.ValidateTarget(skillId, target);
                if (error != null) skillTipForIndependentRuntime(error, UIScale.Factor);
                else if (NetClient.I != null)
                {
                    NetClient.I.UseSkill(skillId, target.id);
                    autoAttacking = CharData.race < 3;
                }
                return;
            }
            RequestSkill(skillId, behavior?.CastDistance ?? 0);
            return;
        }
        int gx, gy;
        if (!TargetWorldPx(out gx, out gy)) return;
        if (NetClient.I != null)
        {
            NetClient.I.UseSkillAt(skillId, gx, gy, target != null ? target.id : 0);
            autoAttacking = CharData.race < 3;
        }
    }

    internal void BindMapSizeForIndependentRuntime(float width, float height)
    {
        worldW = width;
        worldH = height;
    }
    internal int PendingGroundXForIndependentRuntime
    {
        get => pendGroundGx;
        set => pendGroundGx = value;
    }
    internal int PendingGroundYForIndependentRuntime
    {
        get => pendGroundGy;
        set => pendGroundGy = value;
    }
    internal void CastAtTargetForIndependentRuntime(int skillId) => CastAtTarget(skillId);
}

}
