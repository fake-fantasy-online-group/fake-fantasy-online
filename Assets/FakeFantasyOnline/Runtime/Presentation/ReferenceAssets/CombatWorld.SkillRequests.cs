using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    private float pendSkillRangeU;
    private float pendAwaitUntil;
    private float queuedRangePx;
    // Preserve the existing tip adapter without allocating its fallback delegate in
    // the original instance constructor. An explicitly bound null still throws on
    // invocation; it must not silently select the default tip surface.
    private Action<string, float> boundSkillTipForIndependentRuntime;
    private bool skillTipBoundForIndependentRuntime;
    private Action<string, float> skillTipForIndependentRuntime => skillTipBoundForIndependentRuntime
        ? boundSkillTipForIndependentRuntime
        : DefaultSkillTipForIndependentRuntime.Show;

    private static class DefaultSkillTipForIndependentRuntime
    {
        internal static readonly Action<string, float> Show =
            (text, scale) => UITip.Show(text, scale);
    }

    public static void RequestSkill(int skillId, float rangePx)
    {
        Ensure();
        if (I.pvpTarget != null && !I.pvpTarget.IsDead)
        {
            if (NetClient.I != null) NetClient.I.UseSkill(skillId, I.pvpTarget.id);
            I.autoAttacking = true;
            return;
        }
        // Explicit type projection while the existing remote selectable view is still in use.
        // This returns its actual RemotePlayer component without a Unity-null/friendship filter.
        RemotePlayer remote = TargetSelect.CurrentSourceForIndependentRuntime as RemotePlayer;
        if (!ReferenceEquals(remote, null) && !remote.IsDead)
        {
            Win05.SkInfo skill = Win05.SkillInfo(skillId);
            if (skill != null && skill.canFriendly == 1)
            {
                if (NetClient.I != null) NetClient.I.UseSkill(skillId, remote.id);
                return;
            }
        }
        if (Time.time < I.castPauseUntil)
        {
            I.queuedSkillId = skillId;
            I.queuedRangePx = rangePx;
            return;
        }
        float scale = I.ppu > 0f ? I.ppu : 100f;
        float pixels = rangePx > 0f ? rangePx : 100f;
        if (TargetSelect.Current != null && I.target == null)
        {
            string error = SkillTargeting.ValidateTarget(skillId, TargetSelect.Current);
            if (error != null)
            {
                I.skillTipForIndependentRuntime(error, UIScale.Factor);
                return;
            }
        }
        I.pendSkillId = skillId;
        I.pendSkillRangeU = pixels / scale;
        I.pendAwaitUntil = Time.time + 8f;
        if (I.target == null)
            I.skillTipForIndependentRuntime("请选择攻击目标", UIScale.Factor);
    }

    private bool TargetWithin(float rangeU)
    {
        Transform player = CombatNumber.PlayerTransform;
        if (player == null || target == null) return false;
        Vector3 position = player.position;
        Vector3 targetPosition = target.transform.position;
        return Vector2.Distance(new Vector2(position.x, position.y),
            new Vector2(targetPosition.x, targetPosition.y)) <= rangeU;
    }

    internal void BindSkillTipForIndependentRuntime(Action<string, float> show)
    {
        boundSkillTipForIndependentRuntime = show;
        skillTipBoundForIndependentRuntime = true;
    }
    internal void ActivateForIndependentRuntime() => I = this;
    internal MonsterEntity TargetForIndependentRuntime => target;
    internal int QueuedSkillForIndependentRuntime
    {
        get => queuedSkillId;
        set => queuedSkillId = value;
    }
}

}
