using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Exact inverse of PlayerController.WorldToLocal used before SetPathTo receives a click.
/// </summary>
internal static class ReferenceWorldPointerProjection
{
    public static bool TrySceneToMap(
        Vector3 scene,
        float pixelsPerUnit,
        float worldHalfWidth,
        float worldHalfHeight,
        out Vector2 map)
    {
        map = default(Vector2);
        if (!(pixelsPerUnit > 0f) || !IsFinite(pixelsPerUnit) ||
            !IsFinite(scene.x) || !IsFinite(scene.y) ||
            !IsFinite(worldHalfWidth) || !IsFinite(worldHalfHeight))
        {
            return false;
        }

        map = new Vector2(
            scene.x * pixelsPerUnit + worldHalfWidth * pixelsPerUnit,
            worldHalfHeight * pixelsPerUnit - scene.y * pixelsPerUnit);
        return IsFinite(map.x) && IsFinite(map.y);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

internal struct ReferenceWorldManualMovementPlan
{
    public ReferenceWorldManualMovementPlan(
        bool stopAttack,
        bool setPath,
        bool emitRing,
        float targetX,
        float targetY,
        bool standAfterPath = false)
    {
        StandAfterPath = standAfterPath;
        StopAttack = stopAttack;
        SetPath = setPath;
        EmitRing = emitRing;
        TargetX = targetX;
        TargetY = targetY;
    }

    public bool StandAfterPath { get; }
    public bool StopAttack { get; }

    public bool SetPath { get; }

    public bool EmitRing { get; }

    public float TargetX { get; }

    public float TargetY { get; }
}

/// <summary>
/// Source-owned short-click and long-hold cursor-follow state from PlayerController.Update.
/// </summary>
internal sealed class ReferenceWorldManualMovementController
{
    public const float HoldThresholdSeconds = 0.25f;
    public const float FollowPathIntervalSeconds = 0.4f;

    private float detachedMouseDownTime = -1f;
    private bool detachedMouseDownOverUI;
    private bool detachedMouseDownOverMob;
    private bool detachedMouseDownOverSign;
    private float mouseDownTime
    {
        get => nativeOwner != null ? nativeOwner.MouseDownTimeForIndependentRuntime : detachedMouseDownTime;
        set
        {
            // Original mouseDownTime is written only by construction and a new primary press.
            if (nativeOwner == null) detachedMouseDownTime = value;
        }
    }
    private bool mouseDownOverUI
    {
        get => nativeOwner != null ? nativeOwner.MouseDownOverUIForIndependentRuntime : detachedMouseDownOverUI;
        set
        {
            if (nativeOwner == null) detachedMouseDownOverUI = value;
        }
    }
    private bool mouseDownOverMob
    {
        get => nativeOwner != null ? nativeOwner.MouseDownOverMobForIndependentRuntime : detachedMouseDownOverMob;
        set
        {
            if (nativeOwner != null) nativeOwner.MouseDownOverMobForIndependentRuntime = value;
            else detachedMouseDownOverMob = value;
        }
    }
    private bool mouseDownOverSign
    {
        get => nativeOwner != null ? nativeOwner.MouseDownOverSignForIndependentRuntime : detachedMouseDownOverSign;
        set
        {
            if (nativeOwner != null) nativeOwner.MouseDownOverSignForIndependentRuntime = value;
            else detachedMouseDownOverSign = value;
        }
    }
    private bool detachedFollowMode;
    private PlayerController nativeOwner;
    private bool followMode
    {
        get => nativeOwner != null ? nativeOwner.FollowModeForIndependentRuntime : detachedFollowMode;
        set
        {
            if (nativeOwner != null) nativeOwner.FollowModeForIndependentRuntime = value;
            else detachedFollowMode = value;
        }
    }
    internal void BindOwnerForIndependentRuntime(PlayerController owner)
    {
        bool value = followMode;
        float next = nextFollowPathTime;
        nativeOwner = owner;
        followMode = value;
        nextFollowPathTime = next;
    }
    private float detachedNextFollowPathTime;
    private float nextFollowPathTime
    {
        get => nativeOwner != null ? nativeOwner.NextFollowPathTimeForIndependentRuntime : detachedNextFollowPathTime;
        set
        {
            if (nativeOwner != null) nativeOwner.NextFollowPathTimeForIndependentRuntime = value;
            else detachedNextFollowPathTime = value;
        }
    }

    public bool FollowMode => followMode;
    internal bool MouseDownOverUI => mouseDownOverUI;

    public bool TryAdvance(
        bool leftButtonDown,
        bool leftButtonHeld,
        bool leftButtonUp,
        bool overUi,
        bool currentNpc,
        bool currentAttackableMonster,
        bool downOverMob,
        bool downOverSign,
        bool movementActive,
        int movementPathCount,
        float time,
        float targetX,
        float targetY,
        out ReferenceWorldManualMovementPlan plan,
        bool processWorldInput = true,
        bool advanceFollowTail = true)
    {
        plan = default(ReferenceWorldManualMovementPlan);
        if (!IsNonNegativeFinite(time) || !IsFinite(targetX) || !IsFinite(targetY) ||
            movementPathCount < 0)
        {
            return false;
        }

        if (processWorldInput && leftButtonDown)
        {
            // The live camera/pointer entry has already latched the original fields before
            // any skill/ground-item/target branch. Detached historical callers retain their inputs.
            if (nativeOwner == null)
            {
                mouseDownTime = time;
                mouseDownOverUI = overUi ||
                    time <= PlayerController.WorldClickSuppressedUntilForIndependentRuntime;
            }
            mouseDownOverMob = downOverMob;
            mouseDownOverSign = downOverSign;
        }

        float elapsed = mouseDownTime < 0f ? float.PositiveInfinity : time - mouseDownTime;
        bool quickRelease = leftButtonUp && elapsed < HoldThresholdSeconds;

        if (processWorldInput && followMode && quickRelease && !overUi && !mouseDownOverUI)
        {
            followMode = false;
            if (!mouseDownOverMob && !mouseDownOverSign)
            {
                plan = new ReferenceWorldManualMovementPlan(
                    true,
                    true,
                    false,
                    targetX,
                    targetY);
            }

            mouseDownTime = -1f;
            return true;
        }

        if (processWorldInput && !followMode && leftButtonHeld && !overUi &&
            HoldThresholdSeconds < elapsed &&
            !currentNpc && !currentAttackableMonster && !mouseDownOverUI)
        {
            followMode = true;
            PlayerController.I?.BreakSit();
            plan = new ReferenceWorldManualMovementPlan(
                true,
                false,
                false,
                targetX,
                targetY);
        }

        if (processWorldInput && !followMode && quickRelease && !overUi && !mouseDownOverUI &&
            !currentNpc && !currentAttackableMonster &&
            !mouseDownOverMob && !mouseDownOverSign)
        {
            plan = new ReferenceWorldManualMovementPlan(
                true,
                true,
                false,
                targetX,
                targetY,
                true);
            mouseDownTime = -1f;
            return true;
        }

        if (processWorldInput && leftButtonUp)
        {
            mouseDownTime = -1f;
        }

        // The live pointer owner runs this tail after right-click handling and before
        // its NPC-cursor/ring publication. Historical direct callers retain this entry.
        if (!advanceFollowTail || !followMode || currentNpc)
        {
            return true;
        }

        PlayerController.I?.BreakSit();
        bool refreshPath = nextFollowPathTime <= time ||
            (!movementActive && movementPathCount == 0);
        if (refreshPath)
        {
            nextFollowPathTime = time + FollowPathIntervalSeconds;
        }

        plan = new ReferenceWorldManualMovementPlan(
            plan.StopAttack,
            refreshPath,
            true,
            targetX,
            targetY);
        return true;
    }

    internal void ClearFollowModeForIndependentRuntime() => followMode = false;

    public void CancelForWorldInteraction()
    {
        followMode = false;
        mouseDownTime = -1f;
    }

    public void Reset()
    {
        if (nativeOwner == null)
        {
            mouseDownTime = -1f;
            mouseDownOverUI = false;
            mouseDownOverMob = false;
            mouseDownOverSign = false;
        }
        followMode = false;
        nextFollowPathTime = 0f;
    }

    private static bool IsNonNegativeFinite(float value)
    {
        return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

// Historical Editor diagnostics only. The Player reporting owner is GameBootstrap.
#if UNITY_EDITOR
internal struct ReferenceWorldPositionReportPlan
{
    public ReferenceWorldPositionReportPlan(
        bool savePosition,
        bool reportMovement,
        int x,
        int y)
    {
        SavePosition = savePosition;
        ReportMovement = reportMovement;
        X = x;
        Y = y;
    }

    public bool SavePosition { get; }

    public bool ReportMovement { get; }

    public int X { get; }

    public int Y { get; }
}

/// <summary>
/// Exact GameBootstrap.Update timers for periodic saves and authoritative movement reports.
/// </summary>
internal sealed class ReferenceWorldPositionReportController
{
    public const float SaveIntervalSeconds = 8f;
    public const float MovementIntervalSeconds = 0.3f;

    private float saveTimer;
    private float movementTimer;
    private int lastReportX = int.MinValue;
    private int lastReportY = int.MinValue;
    private bool wasMoving;

    public bool WasMoving => wasMoving;

    public bool TryAdvance(
        float deltaTime,
        float worldX,
        float worldY,
        out ReferenceWorldPositionReportPlan plan)
    {
        plan = default(ReferenceWorldPositionReportPlan);
        if (!IsNonNegativeFinite(deltaTime) || !IsFinite(worldX) || !IsFinite(worldY) ||
            worldX < int.MinValue || worldX > int.MaxValue ||
            worldY < int.MinValue || worldY > int.MaxValue)
        {
            return false;
        }

        saveTimer += deltaTime;
        bool save = saveTimer >= SaveIntervalSeconds;
        if (save)
        {
            saveTimer = 0f;
        }

        movementTimer += deltaTime;
        int x = (int)worldX;
        int y = (int)worldY;
        bool report = false;
        if (x == lastReportX && y == lastReportY)
        {
            if (wasMoving)
            {
                wasMoving = false;
                report = true;
            }
        }
        else if (movementTimer >= MovementIntervalSeconds)
        {
            movementTimer = 0f;
            wasMoving = true;
            report = true;
        }

        if (report)
        {
            lastReportX = x;
            lastReportY = y;
        }

        plan = new ReferenceWorldPositionReportPlan(save, report, x, y);
        return true;
    }

    public void Reset()
    {
        saveTimer = 0f;
        movementTimer = 0f;
        lastReportX = int.MinValue;
        lastReportY = int.MinValue;
        wasMoving = false;
    }

    private static bool IsNonNegativeFinite(float value)
    {
        return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

#endif

}
