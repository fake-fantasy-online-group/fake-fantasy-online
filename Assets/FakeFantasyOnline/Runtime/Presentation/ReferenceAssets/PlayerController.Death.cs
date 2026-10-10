using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private bool moveToTarget;
    private int pathIdx;
    private int pendHops;
    private PlayerAvatar.Pose[,] walkFrames;
    private PlayerAvatar.Pose[,] dieFrames;
    private int dieFpd;
    private float dieElapsed;
    private const float DIE_FRAME_SEC = 0.09f;

    public void StopMove()
    {
        path.Clear();
        pathIdx = 0;
        moveToTarget = false;
        pendHops = 0;
    }

    public void Revive()
    {
        dead = false;
        dieFrames = null;
    }

    public void PlayDie()
    {
        CancelWork();
        dead = true;
        sitting = false;
        path.Clear();
        moveToTarget = false;
        CharData.riding = false;
        CombatWorld.StopAttack();
        actAction = -1;
        actFrames = null;
        dieElapsed = 0f;
        dieFrames = null;
        if (avatar != null) dieFrames = avatar.PoseFrames(3, out dieFpd);
    }

    // The caller preserves UpdateAnim's walkFrames guard and HideMount ordering.
    // This is the original dead-branch selection, not the remaining full UpdateAnim.
    internal PlayerAvatar.Pose AdvanceDeathPoseForIndependentRuntime()
    {
        int dir = Mathf.Clamp(curDir, 0, DIRS - 1);
        if (dieFrames == null) return null;
        dieElapsed += Time.deltaTime;
        int frame = Mathf.Clamp((int)(dieElapsed / DIE_FRAME_SEC), 0, dieFpd - 1);
        return dieFrames[dir, frame];
    }

    // Original Update dead prefix, before UpdateAnim's walkFrames guard.
    internal void UpdateDeadMovementForIndependentRuntime()
    {
        path.Clear();
        moveToTarget = false;
    }

    internal List<Vector2> PathForIndependentRuntime => path;
    internal int PathIndexForIndependentRuntime
    {
        get => pathIdx;
        set => pathIdx = value;
    }
    internal int PendingHopsForIndependentRuntime
    {
        get => pendHops;
        set => pendHops = value;
    }
    internal bool MoveToTargetForIndependentRuntime
    {
        get => moveToTarget;
        set => moveToTarget = value;
    }

    internal bool CanUpdateAnimationForIndependentRuntime => walkFrames != null;
    internal PlayerAvatar.Pose[,] DeathFramesForIndependentRuntime => dieFrames;
    internal int DeathFramesPerDirectionForIndependentRuntime => dieFpd;
    internal float DeathElapsedForIndependentRuntime => dieElapsed;
    internal int DeathFrameForIndependentRuntime =>
        Mathf.Clamp((int)(dieElapsed / DIE_FRAME_SEC), 0, dieFpd - 1);
}

}
