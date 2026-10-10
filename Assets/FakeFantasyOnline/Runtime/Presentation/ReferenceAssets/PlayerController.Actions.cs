using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private void StartActPose(PlayerAvatar.Pose[,] frames, int fpd, float frameSec,
        bool loop = false, bool atkSwing = false)
    {
        if (frames == null) return;
        actAction = 1;
        actFrames = frames;
        actFpd = Mathf.Max(1, fpd);
        actFrameSec = frameSec;
        actElapsed = 0f;
        actDur = actFpd * frameSec;
        actLoop = loop;
        actIsAtkSwing = atkSwing;
    }

    private PlayerAvatar.Pose ActPoseNow(int dir)
    {
        if (actAction < 0) return null;

        actElapsed += Time.deltaTime;
        if (actElapsed >= actDur)
        {
            if (actLoop)
            {
                actElapsed -= actDur;
            }
            else if (actElapsed >= actDur + 0.15f || !actIsAtkSwing)
            {
                actAction = -1;
                actFrames = null;
                actIsAtkSwing = false;
                return null;
            }
        }

        if (actFrames == null)
        {
            actAction = -1;
            return null;
        }

        int frame = Mathf.Clamp((int)(actElapsed / actFrameSec), 0, actFpd - 1);
        return actFrames[Mathf.Clamp(dir, 0, DIRS - 1), frame];
    }

    internal PlayerAvatar.Pose ActPoseNowForIndependentRuntime(int dir) => ActPoseNow(dir);

    internal void ClearSittingActionForIndependentRuntime()
    {
        actAction = -1;
        actFrames = null;
        actLoop = false;
    }

    internal int ActionFrameForIndependentRuntime =>
        Mathf.Clamp((int)(actElapsed / actFrameSec), 0, actFpd - 1);
}

}
