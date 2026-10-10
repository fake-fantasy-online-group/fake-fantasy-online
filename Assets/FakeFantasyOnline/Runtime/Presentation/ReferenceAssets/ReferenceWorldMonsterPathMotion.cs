using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal enum ReferenceWorldMonsterSetPathOutcome
{
    ConsumedWithoutPath,
    Queued,
    StoppedNearCurrent,
    WarpedToFirst,
    WarpedToLast,
}

/// <summary>
/// Historical construction-audit state for the previously migrated MonsterEntity path owner.
/// </summary>
/// <remarks>
/// The product actor no longer constructs this class: live paths enter actual MonsterEntity.SetPath
/// and are advanced only by recovered MonsterEntity.Update. This type must not regain runtime
/// ownership.
/// A zero-count path is a consumed legal no-op, not a failed application: official
/// MonsterEntity.SetPath (0600063E) returns at arm64 0x48f77c..0x48f794, before any field write,
/// when xs/ys are null, empty or of different length, and the 0x8018 branch tail-calls
/// CombatWorld.MovePath (06000149) after allocating both arrays even when the count byte is zero.
/// </remarks>
internal sealed class ReferenceWorldMonsterPathMotion
{
    public const float DefaultMoveSpeed = 120f;
    public const float LargeGapWarpDistance = 260f;
    public const float CatchUpDistanceBase = 40f;
    public const float CatchUpSpeedFactor = 0.25f;
    public const float CatchUpMultiplier = 1.6f;
    public const float OrdinaryMultiplier = 1f;
    public const float LeadingPointDistanceSquared = 4f;
    public const float FacingDistanceSquared = 0.25f;
    public const float AdvanceDistanceEpsilon = 0.01f;

    private readonly bool friendly;
    private WorldPathPoint[] path = Array.Empty<WorldPathPoint>();
    private int pathIndex;
    private float worldX;
    private float worldY;
    private float moveSpeed = DefaultMoveSpeed;
    private float catchUp = OrdinaryMultiplier;
    private bool moving;

    public ReferenceWorldMonsterPathMotion(float worldX, float worldY, bool friendly)
    {
        if (!IsFinite(worldX) || !IsFinite(worldY))
        {
            throw new ArgumentOutOfRangeException(nameof(worldX));
        }

        this.worldX = worldX;
        this.worldY = worldY;
        this.friendly = friendly;
    }

    public float WorldX => worldX;
    public float WorldY => worldY;
    public float MoveSpeed => moveSpeed;
    public float CatchUp => catchUp;
    public int PathIndex => pathIndex;
    public int PathCount => path.Length;
    public bool Moving => moving;
    public bool Friendly => friendly;

    public bool TrySetPath(
        WorldRemotePathState value,
        out ReferenceWorldMonsterSetPathOutcome outcome)
    {
        outcome = ReferenceWorldMonsterSetPathOutcome.ConsumedWithoutPath;
        if (value == null || value.Points == null)
        {
            return false;
        }

        // Official MonsterEntity.SetPath returns before touching any field for two empty arrays,
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
            outcome = ReferenceWorldMonsterSetPathOutcome.WarpedToFirst;
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
        if (!friendly && lastDistance > LargeGapWarpDistance)
        {
            worldX = last.X;
            worldY = last.Y;
            ClearPath();
            outcome = ReferenceWorldMonsterSetPathOutcome.WarpedToLast;
            return true;
        }

        catchUp = !friendly && moveSpeed > 0f &&
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
                outcome = ReferenceWorldMonsterSetPathOutcome.Queued;
                return true;
            }

            pathIndex++;
        }

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
                // Mirrors official 06000641: the leg is normalised by distance first and the
                // remaining distance scales the unit vector afterwards.
                worldX += remaining * (deltaX / distance);
                worldY += remaining * (deltaY / distance);
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

    public bool TryGetFacingDelta(
        float previousWorldX,
        float previousWorldY,
        out float deltaX,
        out float deltaY)
    {
        if (pathIndex < path.Length)
        {
            deltaX = path[pathIndex].X - worldX;
            deltaY = path[pathIndex].Y - worldY;
        }
        else
        {
            deltaX = worldX - previousWorldX;
            deltaY = worldY - previousWorldY;
        }

        return deltaX * deltaX + deltaY * deltaY > FacingDistanceSquared;
    }

    public void ClearPath()
    {
        path = Array.Empty<WorldPathPoint>();
        pathIndex = 0;
        moving = false;
        catchUp = OrdinaryMultiplier;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
