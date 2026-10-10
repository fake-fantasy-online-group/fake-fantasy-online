using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class RemotePlayer
{
    private int curFrame;
    private float walkTimer;
    private const float WALK_FRAME_SEC = 0.1f;
    private PlayerAvatar.Pose[,] workFrames;
    private int workFpd;
    private int workFrame;
    private float workTimer;
    private const float WORK_FRAME_SEC = 0.12f;

    private void StepWalk()
    {
        if (avatar.walk == null)
        {
            ApplyPose(StandPose(curDir), flip, SortFor(worldY));
            return;
        }
        walkTimer += Time.deltaTime;
        if (walkTimer >= WALK_FRAME_SEC)
        {
            walkTimer -= WALK_FRAME_SEC;
            curFrame++;
        }
        int dir = Mathf.Clamp(curDir, 0, avatar.walk.GetLength(0) - 1);
        curFrame %= avatar.walk.GetLength(1);
        PlayerAvatar.Pose pf = avatar.walk[dir, curFrame];
        if (pf != null) ApplyPose(pf, flip, SortFor(worldY));
    }

    private void UpdateIdle()
    {
        if (appear.Body >= 950 && appear.Body <= 953)
        {
            if (workFrames == null) workFrames = avatar.PoseFrames(0, out workFpd);
            if (workFrames != null && workFpd >= 2)
            {
                workTimer += Time.deltaTime;
                if (workTimer >= WORK_FRAME_SEC)
                {
                    workTimer -= WORK_FRAME_SEC;
                    workFrame = (workFrame + 1) % workFpd;
                }
                int dir = Mathf.Clamp(curDir, 0, workFrames.GetLength(0) - 1);
                PlayerAvatar.Pose pf = workFrames[dir, workFrame];
                if (pf != null)
                {
                    ApplyPose(pf, flip, SortFor(worldY));
                    return;
                }
            }
        }
        ApplyPose(StandPose(curDir), flip, SortFor(worldY));
    }

    private PlayerAvatar.Pose SitPose(int dir)
    {
        if (avatar == null || !avatar.ok) return null;
        if (avatar.sit != null)
        {
            PlayerAvatar.Pose pf = avatar.sit[Mathf.Clamp(dir, 0, avatar.sit.Length - 1)];
            if (pf != null) return pf;
        }
        return avatar.StandAt(Mathf.Clamp(dir, 0, 4), Time.time);
    }

    internal void StepWalkForIndependentRuntime() => StepWalk();
    internal void UpdateIdleForIndependentRuntime() => UpdateIdle();
    internal void ApplyStallPoseForIndependentRuntime() => ApplyPose(SitPose(curDir), flip, SortFor(worldY));
    internal int WalkFrameForIndependentRuntime => curFrame;
    internal int WorkFrameForIndependentRuntime => workFrame;
    internal float WalkTimerForIndependentRuntime => walkTimer;
    internal float WorkTimerForIndependentRuntime => workTimer;
    internal int SittingTopForIndependentRuntime => avatar == null ? 0 : avatar.sitTopPx;
}

}
