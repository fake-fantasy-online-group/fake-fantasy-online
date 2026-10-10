using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal enum ReferenceWorldRemoteSetPathOutcome
{
    ConsumedWithoutPath,
    Queued,
    StoppedNearCurrent,
    WarpedToFirst,
    WarpedToLast,
}

/// <summary>
/// Historical diagnostic path projection. Live SetPath/AdvancePath state is owned by RemotePlayer.Navigation.
/// </summary>
/// <remarks>
/// This class deliberately owns no wire bytes and no Unity hierarchy. Protocol supplies one
/// immutable authoritative path; the actor applies the resulting position, sorting and pose on
/// Unity's main thread.
/// A zero-count path is a consumed legal no-op, not a failed application: official
/// RemotePlayer.SetPath (06000B16) returns at arm64 0x2dbf84..0x2dbf9c, before any field write,
/// when xs/ys are null, empty or of different length, and the 0x8018 branch tail-calls
/// CombatWorld.MovePath (06000149) after allocating both arrays even when the count byte is zero.
/// </remarks>
internal sealed class ReferenceWorldRemotePathMotion
{
    public const float DefaultMoveSpeed = 120f;
    public const float LargeGapWarpDistance = 260f;
    public const float CatchUpDistanceBase = 40f;
    public const float CatchUpSpeedFactor = 0.3f;
    public const float CatchUpMultiplier = 1.6f;
    public const float OrdinaryMultiplier = 1f;
    public const float LeadingPointDistanceSquared = 4f;
    public const float AdvanceDistanceEpsilon = 0.01f;

    private WorldPathPoint[] path = Array.Empty<WorldPathPoint>();
    private int pathIndex;
    private float worldX;
    private float worldY;
    private float moveSpeed = DefaultMoveSpeed;
    private float catchUp = OrdinaryMultiplier;
    private bool moving;

    public ReferenceWorldRemotePathMotion(float worldX, float worldY)
    {
        if (!IsFinite(worldX) || !IsFinite(worldY))
        {
            throw new ArgumentOutOfRangeException(nameof(worldX));
        }

        this.worldX = worldX;
        this.worldY = worldY;
    }

    public float WorldX => worldX;
    public float WorldY => worldY;
    public float MoveSpeed => moveSpeed;
    public float CatchUp => catchUp;
    public int PathIndex => pathIndex;
    public int PathCount => path.Length;
    public bool Moving => moving;

    public bool TrySetPath(
        WorldRemotePathState value,
        out ReferenceWorldRemoteSetPathOutcome outcome)
    {
        outcome = ReferenceWorldRemoteSetPathOutcome.ConsumedWithoutPath;
        if (value == null || value.Points == null)
        {
            return false;
        }

        // Official RemotePlayer.SetPath returns before touching any field for two empty arrays,
        // so a zero-count authoritative path is consumed with no movement and no clearing.
        if (value.Points.Count < 1)
        {
            return true;
        }

        if (value.Speed < 0)
        {
            WorldPathPoint first = value.Points[0];
            worldX = first.X;
            worldY = first.Y;
            ClearPath();
            outcome = ReferenceWorldRemoteSetPathOutcome.WarpedToFirst;
            return true;
        }

        if (value.Speed != 0)
        {
            moveSpeed = value.Speed;
        }

        WorldPathPoint last = value.Points[value.Points.Count - 1];
        float lastDeltaX = last.X - worldX;
        float lastDeltaY = last.Y - worldY;
        float lastDistance = Mathf.Sqrt(
            lastDeltaX * lastDeltaX + lastDeltaY * lastDeltaY);
        if (lastDistance > LargeGapWarpDistance)
        {
            worldX = last.X;
            worldY = last.Y;
            ClearPath();
            outcome = ReferenceWorldRemoteSetPathOutcome.WarpedToLast;
            return true;
        }

        catchUp = moveSpeed > 0f &&
                  moveSpeed * CatchUpSpeedFactor + CatchUpDistanceBase < lastDistance
            ? CatchUpMultiplier
            : OrdinaryMultiplier;
        path = new WorldPathPoint[value.Points.Count];
        for (int index = 0; index < value.Points.Count; index++)
        {
            path[index] = value.Points[index];
        }

        pathIndex = 0;
        moving = false;
        while (pathIndex < path.Length)
        {
            float deltaX = path[pathIndex].X - worldX;
            float deltaY = path[pathIndex].Y - worldY;
            if (deltaX * deltaX + deltaY * deltaY > LeadingPointDistanceSquared)
            {
                moving = true;
                outcome = ReferenceWorldRemoteSetPathOutcome.Queued;
                return true;
            }

            pathIndex++;
        }

        outcome = ReferenceWorldRemoteSetPathOutcome.StoppedNearCurrent;
        return true;
    }

    public bool TryAdvance(float deltaTime)
    {
        if (!IsFinite(deltaTime) || deltaTime < 0f)
        {
            return false;
        }

        if (!moving)
        {
            return true;
        }

        float remaining = moveSpeed * catchUp * deltaTime;
        while (remaining > 0f && pathIndex < path.Length)
        {
            WorldPathPoint point = path[pathIndex];
            float deltaX = point.X - worldX;
            float deltaY = point.Y - worldY;
            float distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
            if (remaining < distance && distance > AdvanceDistanceEpsilon)
            {
                worldX += (deltaX / distance) * remaining;
                worldY += (deltaY / distance) * remaining;
                return true;
            }

            remaining -= distance;
            pathIndex++;
            worldX = point.X;
            worldY = point.Y;
        }

        if (pathIndex >= path.Length)
        {
            moving = false;
        }

        return true;
    }

    public void ClearPath()
    {
        path = Array.Empty<WorldPathPoint>();
        pathIndex = 0;
        moving = false;
        catchUp = OrdinaryMultiplier;
    }

    /// <summary>
    /// Mirrors RemotePlayer.PlayAttack/PlayCast assigning moving=false without discarding the
    /// retained path array or catch-up state.
    /// </summary>
    public void StopMoving()
    {
        moving = false;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
