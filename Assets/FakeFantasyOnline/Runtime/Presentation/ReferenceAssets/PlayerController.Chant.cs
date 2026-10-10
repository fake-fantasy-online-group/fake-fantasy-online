using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private EffectPlayer chargeFx;
    private int actAction = -1;
    private PlayerAvatar.Pose[,] actFrames;
    private int actFpd;
    private float actElapsed;
    private float actDur;
    private bool actLoop;
    private bool actIsAtkSwing;
    private float actFrameSec = 0.06f;
    private int DIRS = 5;
    private PlayerAvatar.Pose[,] chantFrames;

    public bool Chanting => Time.time < chantUntil;

    public void SetChargeFx(EffectPlayer fx)
    {
        if (chargeFx != null) chargeFx.Stop();
        chargeFx = fx;
    }

    public void StopChant()
    {
        chantUntil = 0f;
        if (actAction == 1)
        {
            actAction = -1;
            actFrames = null;
            actIsAtkSwing = false;
        }
        if (chargeFx != null)
        {
            chargeFx.Stop();
            chargeFx = null;
        }
    }

    // The old renderer still exchanges this immutable snapshot. It owns no second action clock:
    // Source methods and all animation consumers read the same original act* fields.
    internal ReferenceWorldPlayerActionAnimationState ReadActionStateForIndependentRuntime(int variant)
    {
        return new ReferenceWorldPlayerActionAnimationState(actAction >= 0, variant,
            actFpd, actFrameSec, actElapsed, actDur, actLoop, actIsAtkSwing);
    }

    internal void BindDirectionsForIndependentRuntime(int directions) => DIRS = directions;


}

}
