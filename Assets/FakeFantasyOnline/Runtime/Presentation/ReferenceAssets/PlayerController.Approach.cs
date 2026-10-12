namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private bool followMode;
    internal bool FollowModeForIndependentRuntime
    {
        get => followMode;
        set => followMode = value;
    }

    internal bool ApplyChantMovementForIndependentRuntime()
    {
        if (UnityEngine.Time.time < chantUntil)
        {
            path.Clear();
            moveToTarget = false;
            followMode = false;
            return true;
        }
        return false;
    }

    // Original Update approach gate and original path owner. Return value is observation only.
    internal bool ApplyApproachForIndependentRuntime()
    {
        if (!sitting && !followMode && CombatWorld.WantsApproach())
        {
            int gx, gy;
            if (CombatWorld.TargetWorldPx(out gx, out gy))
            {
                SetPathTo(gx, gy, 0);
                ObserveCombatApproachForPlugins();
                return true;
            }
        }
        return false;
    }

    // Original later stop gate, after the separate friend-follow owner has run.
    internal void StopApproachForIndependentRuntime()
    {
        if (!followMode && moveToTarget && CombatWorld.Engaged && !CombatWorld.WantsApproach())
        {
            moveToTarget = false;
            path.Clear();
        }
    }
}

}
