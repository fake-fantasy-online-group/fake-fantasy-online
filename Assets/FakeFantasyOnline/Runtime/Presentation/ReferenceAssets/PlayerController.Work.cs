using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    private float workUntil;
    private float workStart;
    private PlayerAvatar.Pose[,] workFrames;
    private int workFpd;
    private PlayerAvatar workAvatarRef;
    public bool Working => workSkill != 0;
    public float WorkElapsed => workSkill != 0 ? Time.time - workStart : 0f;
    public string WorkNamePrefix => workSkill == 11007 ? "挖矿中的" : workSkill == 11006 ? "钓鱼中的" : null;
    public void StartWork(int skill)
    {
        if (dead || workSkill != 0) return;
        workSkill = skill;
        workUntil = Time.time + 3f;
        workStart = Time.time;
        path.Clear(); pathIdx = 0; moveToTarget = false; followMode = false; sitting = false;
        if (NetClient.I != null) NetClient.I.BeginWorkForIndependentRuntime(skill);
        PlayWorkPose();
    }
    private void PlayWorkPose()
    {
        if (avatar == null) return;
        if (workAvatarRef != avatar)
        {
            workAvatarRef = avatar;
            workFrames = avatar.PoseFrames(0, out workFpd);
        }
        if (workFrames == null || workFpd < 2) { PlayAttackPose(0f); return; }
        actAction = 1; actFrames = workFrames; actFpd = workFpd; actFrameSec = 0.12f; actElapsed = 0f;
        actDur = (float)workFpd * 0.12f; actLoop = true; actIsAtkSwing = false;
    }
    private void UpdateWork()
    {
        if (workSkill == 0) return;
        if (Time.time >= workUntil)
        {
            if (NetClient.I != null) NetClient.I.GatherForIndependentRuntime(workSkill);
            workUntil = Time.time + 3f;
        }
        if (actAction == -1 || workAvatarRef != avatar) PlayWorkPose();
    }
    internal void AdvanceWorkForIndependentRuntime(Color32 tint, System.Action<PlayerController> confirmation)
    {
        dollTint = tint;
        UpdateWork();
        confirmation?.Invoke(this);
        UpdateAnim(0f, 0f, false);
    }
}
}
