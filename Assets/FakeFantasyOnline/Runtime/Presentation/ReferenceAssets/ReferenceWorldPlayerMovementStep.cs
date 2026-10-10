using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Immutable result of one normal first-world PlayerController movement step.
/// </summary>
public struct ReferenceWorldPlayerMovementPlan
{
    /// <summary>
    /// Result delivered by the frame on which the official transfer tail returns from Update.
    /// </summary>
    /// <remarks>
    /// Both frozen exports dispose the MapProc enumerator and return right after the transfer arm
    /// (arm64 :1916-1917 / x86_64 :1889-1890 of PlayerController.Update 0x06000a00), so that frame
    /// runs no projection, no sorting order, no IsMoving store and no UpdateAnim/UpdateBuffAura
    /// (:1969 / :1980 / :1991 / :1992 / :1993). Every other true return of
    /// ReferenceWorldLocalPlayerDoll.TryAdvanceMovement carries a produced plan, so this flag is
    /// the only thing that distinguishes the returned frame from a produced one.
    /// </remarks>
    public ReferenceWorldPlayerMovementPlan(bool transferReturned)
    {
        this = default(ReferenceWorldPlayerMovementPlan);
        TransferReturned = transferReturned;
    }

    public ReferenceWorldPlayerMovementPlan(
        float worldX,
        float worldY,
        float deltaX,
        float deltaY,
        int pathIndex,
        bool moveToTarget,
        bool pathCleared,
        bool resetFollowPathTime,
        bool pathReplanned,
        bool animationMoving,
        bool isMoving,
        float targetX,
        float targetY,
        float pendingX,
        float pendingY,
        int pendingHops,
        Vector3 transformPosition)
    {
        WorldX = worldX;
        WorldY = worldY;
        DeltaX = deltaX;
        DeltaY = deltaY;
        PathIndex = pathIndex;
        MoveToTarget = moveToTarget;
        PathCleared = pathCleared;
        ResetFollowPathTime = resetFollowPathTime;
        PathReplanned = pathReplanned;
        AnimationMoving = animationMoving;
        IsMoving = isMoving;
        TargetX = targetX;
        TargetY = targetY;
        PendingX = pendingX;
        PendingY = pendingY;
        PendingHops = pendingHops;
        TransformPosition = transformPosition;
        TransferReturned = false;
    }

    public float WorldX { get; }
    public float WorldY { get; }
    public float DeltaX { get; }
    public float DeltaY { get; }
    public int PathIndex { get; }
    public bool MoveToTarget { get; }
    public bool PathCleared { get; }
    public bool ResetFollowPathTime { get; }
    public bool PathReplanned { get; }
    public bool AnimationMoving { get; }
    public bool IsMoving { get; }
    public float TargetX { get; }
    public float TargetY { get; }
    public float PendingX { get; }
    public float PendingY { get; }
    public int PendingHops { get; }
    public Vector3 TransformPosition { get; }
    public bool TransferReturned { get; }
}

/// <summary>
/// Source-owned normal, alive, unmounted PlayerController.Update movement branch.
/// </summary>
/// <remarks>
/// The bounded branch owns direct-target and waypoint stepping, final-waypoint clearing, first-
/// choice axis sliding, blocked-route cancellation, world clamping, exact rounded Transform
/// projection and the two movement flags passed to state and animation. Final-waypoint pending-hop
/// retries synchronously reuse the source-owned SetPathTo translation before collision, matching
/// the locked frame order. Input, follow mode, pickup, transfer, pose lookup/application and
/// exceptional animation states remain outside this boundary.
/// </remarks>
public static class ReferenceWorldPlayerMovementStep
{
    public static bool TryAdvance(
        ReferenceWorldCollisionMask mask,
        List<Vector2> path,
        int pathIndex,
        bool moveToTarget,
        bool sitting,
        float targetX,
        float targetY,
        int pendingHops,
        float worldX,
        float worldY,
        float worldWidth,
        float worldHeight,
        float pixelsPerUnit,
        float stepPixels,
        out ReferenceWorldPlayerMovementPlan plan,
        PlayerController nativeNavigation = null)
    {
        if (pendingHops != 0)
        {
            plan = default(ReferenceWorldPlayerMovementPlan);
            return false;
        }

        return TryAdvance(
            mask,
            path,
            pathIndex,
            moveToTarget,
            sitting,
            targetX,
            targetY,
            0f,
            0f,
            0,
            worldX,
            worldY,
            worldWidth,
            worldHeight,
            pixelsPerUnit,
            stepPixels,
            out plan, nativeNavigation);
    }

    public static bool TryAdvance(
        ReferenceWorldCollisionMask mask,
        List<Vector2> path,
        int pathIndex,
        bool moveToTarget,
        bool sitting,
        float targetX,
        float targetY,
        float pendingX,
        float pendingY,
        int pendingHops,
        float worldX,
        float worldY,
        float worldWidth,
        float worldHeight,
        float pixelsPerUnit,
        float stepPixels,
        out ReferenceWorldPlayerMovementPlan plan,
        PlayerController nativeNavigation = null)
    {
        plan = default(ReferenceWorldPlayerMovementPlan);
        if (mask == null || path == null || pathIndex < 0 || pendingHops < 0 ||
            !IsFinite(worldX) || !IsFinite(worldY) ||
            !IsFinite(worldWidth) || !(worldWidth > 0f) ||
            !IsFinite(worldHeight) || !(worldHeight > 0f) ||
            !IsFinite(pixelsPerUnit) || !(pixelsPerUnit > 0f) ||
            !IsFinite(stepPixels) || stepPixels < 0f ||
            !IsFinite(targetX) || !IsFinite(targetY) ||
            (pendingHops > 0 && (!IsFinite(pendingX) || !IsFinite(pendingY))))
        {
            return false;
        }

        for (int index = 0; index < path.Count; index++)
        {
            if (!IsFinite(path[index].x) || !IsFinite(path[index].y))
            {
                return false;
            }
        }

        if (path.Count > 0 && pathIndex >= path.Count)
        {
            return false;
        }

        float deltaX = 0f;
        float deltaY = 0f;
        int nextPathIndex = pathIndex;
        bool nextMoveToTarget = moveToTarget;
        bool pathCleared = false;
        bool resetFollowPathTime = false;
        bool pathReplanned = false;
        bool animationMoving = false;
        float nextTargetX = targetX;
        float nextTargetY = targetY;
        float nextPendingX = pendingX;
        float nextPendingY = pendingY;
        int nextPendingHops = pendingHops;

        if (!sitting && nextMoveToTarget)
        {
            if (path.Count < 1)
            {
                deltaX = targetX - worldX;
                deltaY = targetY - worldY;
                float distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (nativeNavigation != null) stepPixels = nativeNavigation.MovementStepForIndependentRuntime();
                if (distance > stepPixels)
                {
                    deltaX = stepPixels * deltaX / distance;
                    deltaY = stepPixels * deltaY / distance;
                }
                else
                {
                    nextMoveToTarget = false;
                }
            }
            else
            {
                Vector2 waypoint = path[nextPathIndex];
                deltaX = waypoint.x - worldX;
                deltaY = waypoint.y - worldY;
                float distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
                if (nativeNavigation != null) stepPixels = nativeNavigation.MovementStepForIndependentRuntime();
                if (distance > stepPixels)
                {
                    deltaX = stepPixels * deltaX / distance;
                    deltaY = stepPixels * deltaY / distance;
                }
                else
                {
                    nextPathIndex++;
                    if (nextPathIndex >= path.Count)
                    {
                        nextMoveToTarget = false;
                        pathCleared = true;
                        path.Clear();

                        if (nextPendingHops > 0)
                        {
                            int retryHops = nextPendingHops;
                            nextPendingHops = 0;
                            if (nativeNavigation != null)
                            {
                                nativeNavigation.SetPathToForIndependentRuntime(nextPendingX, nextPendingY, retryHops);
                                nextPathIndex = nativeNavigation.PathIndexForIndependentRuntime;
                                nextMoveToTarget = nativeNavigation.MoveToTargetForIndependentRuntime;
                                nextPendingHops = nativeNavigation.PendingHopsForIndependentRuntime;
                                nextTargetX = nativeNavigation.MovementTargetXForIndependentRuntime;
                                nextTargetY = nativeNavigation.MovementTargetYForIndependentRuntime;
                                nextPendingX = nativeNavigation.PendingTargetXForIndependentRuntime;
                                nextPendingY = nativeNavigation.PendingTargetYForIndependentRuntime;
                            }
                            else
                            {
                                ReferenceWorldSetPathPlan retryPlan;
                                if (!ReferenceWorldSetPathTo.TryApply(
                                        mask,
                                        path,
                                        worldX,
                                        worldY,
                                        nextPendingX,
                                        nextPendingY,
                                        retryHops,
                                        out retryPlan))
                                {
                                    return false;
                                }

                                nextPathIndex = retryPlan.PathIndex;
                                nextMoveToTarget = retryPlan.MoveToTarget;
                                nextPendingHops = retryPlan.PendingHops;
                                if (retryPlan.TargetUpdated)
                                {
                                    nextTargetX = retryPlan.TargetX;
                                    nextTargetY = retryPlan.TargetY;
                                }

                                if (retryPlan.PendingTargetUpdated)
                                {
                                    nextPendingX = retryPlan.PendingX;
                                    nextPendingY = retryPlan.PendingY;
                                }
                            }

                            pathReplanned = true;
                        }
                    }
                }
            }

            animationMoving = deltaX != 0f || deltaY != 0f;
        }

        float nextWorldX = worldX;
        float nextWorldY = worldY;
        if (deltaX != 0f || deltaY != 0f)
        {
            if (mask.IsMaskPixelWalkable(worldX + deltaX, worldY + deltaY))
            {
                nextWorldX += deltaX;
                nextWorldY += deltaY;
            }
            else if (mask.IsMaskPixelWalkable(worldX + deltaX, worldY))
            {
                nextWorldX += deltaX;
                deltaY = 0f;
            }
            else if (mask.IsMaskPixelWalkable(worldX, worldY + deltaY))
            {
                nextWorldY += deltaY;
                deltaX = 0f;
            }
            else
            {
                nextMoveToTarget = false;
                pathCleared = true;
                nextPathIndex = 0;
                resetFollowPathTime = true;
                animationMoving = false;
                deltaX = 0f;
                deltaY = 0f;
                path.Clear();
            }
        }

        nextWorldX = ClampWorldPixel(nextWorldX, worldWidth);
        nextWorldY = ClampWorldPixel(nextWorldY, worldHeight);
        bool isMoving = animationMoving && (deltaX != 0f || deltaY != 0f);
        plan = new ReferenceWorldPlayerMovementPlan(
            nextWorldX,
            nextWorldY,
            deltaX,
            deltaY,
            nextPathIndex,
            nextMoveToTarget,
            pathCleared,
            resetFollowPathTime,
            pathReplanned,
            animationMoving,
            isMoving,
            nextTargetX,
            nextTargetY,
            nextPendingX,
            nextPendingY,
            nextPendingHops,
            ProjectTransformPosition(
                nextWorldX,
                nextWorldY,
                worldWidth,
                worldHeight,
                pixelsPerUnit));
        return true;
    }

    public static Vector3 ProjectTransformPosition(
        float worldX,
        float worldY,
        float worldWidth,
        float worldHeight,
        float pixelsPerUnit)
    {
        return new Vector3(
            Mathf.Round(worldX - worldWidth * 0.5f) / pixelsPerUnit,
            Mathf.Round(worldHeight * 0.5f - worldY) / pixelsPerUnit,
            0f);
    }

    private static float ClampWorldPixel(float value, float maximum)
    {
        if (value < 0f)
        {
            return 0f;
        }

        return value > maximum ? maximum : value;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
