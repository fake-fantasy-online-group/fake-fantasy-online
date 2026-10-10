using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    private float mouseDownTime = -1f;
    private bool mouseDownOverUI;
    private bool mouseDownOverMob;
    private bool mouseDownOverSign;

    // Original Update early exits also precede the camera/pointer region.
    internal bool CanHandleWorldPointerForIndependentRuntime(int rideAnchor) =>
        !dead && rideAnchor == 0 && workSkill == 0;

    // Original primary-down state writes, after camera projection/clamp and before hit branches.
    internal void LatchPrimaryPressForIndependentRuntime(bool overAnyUI)
    {
        mouseDownTime = Time.time;
        mouseDownOverUI = overAnyUI || Time.time <= suppressWorldClickUntil;
        mouseDownOverMob = false;
        mouseDownOverSign = false;
        if (!mouseDownOverUI) followId = 0;
    }

    internal float MouseDownTimeForIndependentRuntime => mouseDownTime;
    internal bool MouseDownOverUIForIndependentRuntime => mouseDownOverUI;
    internal bool MouseDownOverMobForIndependentRuntime
    {
        get => mouseDownOverMob;
        set => mouseDownOverMob = value;
    }
    internal bool MouseDownOverSignForIndependentRuntime
    {
        get => mouseDownOverSign;
        set => mouseDownOverSign = value;
    }

    internal void StopForNpcPointerForIndependentRuntime()
    {
        moveToTarget = false;
        followMode = false;
    }

    internal void ApproachMonsterPointerForIndependentRuntime(float px, float py)
    {
        sitting = false;
        followMode = false;
        SetPathTo(px, py);
    }

    private float followPathX;
    private float followPathY;

    // The common camera/pointer tail is outside the UI/stall and left/right-click blocks.
    // Returning the ring decision lets the caller preserve SetNpcHover before EmitRing.
    internal bool AdvancePointerFollowTailForIndependentRuntime(bool npcHovered, float px, float py)
    {
        if (!followMode || npcHovered) return false;
        sitting = false;
        if (Time.time >= nextFollowPathT || (!moveToTarget && path.Count == 0))
        {
            nextFollowPathT = Time.time + 0.4f;
            followPathX = px;
            followPathY = py;
            SetPathTo(px, py);
            if (!moveToTarget)
            {
                tgtX = px;
                tgtY = py;
                moveToTarget = true;
            }
        }
        return true;
    }

    public static void SuppressWorldClick()
    {
        suppressWorldClickUntil = Time.time + 0.35f;
    }

    // The existing Update adapter reads the recovered field only when latching a new press.
    internal static float WorldClickSuppressedUntilForIndependentRuntime => suppressWorldClickUntil;
}

}
