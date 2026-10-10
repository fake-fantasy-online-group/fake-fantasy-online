using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public enum ReferenceWorldSetPathOutcome
{
    None = 0,
    Stopped = 1,
    DirectTarget = 2,
    PrimaryPath = 3,
    RetryPath = 4,
}

/// <summary>
/// Immutable state changes produced by the source-owned PlayerController.SetPathTo translation.
/// </summary>
public struct ReferenceWorldSetPathPlan
{
    public ReferenceWorldSetPathPlan(
        ReferenceWorldSetPathOutcome outcome,
        bool moveToTarget,
        bool targetUpdated,
        float targetX,
        float targetY,
        bool pendingTargetUpdated,
        float pendingX,
        float pendingY,
        int pendingHops,
        Vector2Int startCell,
        Vector2Int goalCell,
        bool exactEnd,
        int retryCandidateCount,
        int retryAttemptCount,
        Vector2Int selectedRetryCell)
    {
        Outcome = outcome;
        MoveToTarget = moveToTarget;
        TargetUpdated = targetUpdated;
        TargetX = targetX;
        TargetY = targetY;
        PendingTargetUpdated = pendingTargetUpdated;
        PendingX = pendingX;
        PendingY = pendingY;
        PendingHops = pendingHops;
        StartCell = startCell;
        GoalCell = goalCell;
        ExactEnd = exactEnd;
        RetryCandidateCount = retryCandidateCount;
        RetryAttemptCount = retryAttemptCount;
        SelectedRetryCell = selectedRetryCell;
    }

    public ReferenceWorldSetPathOutcome Outcome { get; }
    public int PathIndex => 0;
    public bool MoveToTarget { get; }
    public bool TargetUpdated { get; }
    public float TargetX { get; }
    public float TargetY { get; }
    public bool PendingTargetUpdated { get; }
    public float PendingX { get; }
    public float PendingY { get; }
    public int PendingHops { get; }
    public Vector2Int StartCell { get; }
    public Vector2Int GoalCell { get; }
    public bool ExactEnd { get; }
    public int RetryCandidateCount { get; }
    public int RetryAttemptCount { get; }
    public Vector2Int SelectedRetryCell { get; }
}

/// <summary>
/// Source-owned translation of both PlayerController.SetPathTo overloads and AddWaypoint.
/// </summary>
/// <remarks>
/// The full method clears the current route, validates the start cell, snaps the requested goal,
/// runs the normal 30,000-expansion A*, then falls back to a direction-ranked 32-point ring with
/// 2,000-expansion searches. A successful fallback retains the original target as pending work and
/// increments its hop count. Input dispatch and rendered movement remain outside this boundary.
/// </remarks>
public static class ReferenceWorldSetPathTo
{
    public const int PrimaryExpansionCap = 30000;
    public const int RetryExpansionCap = 2000;
    public const int MaximumRetryHops = 15;

    private struct RetryCandidate
    {
        public RetryCandidate(long score, int cellX, int cellY)
        {
            Score = score;
            CellX = cellX;
            CellY = cellY;
        }

        public long Score { get; }
        public int CellX { get; }
        public int CellY { get; }
    }

    public static bool TryApply(
        ReferenceWorldCollisionMask mask,
        List<Vector2> path,
        float worldX,
        float worldY,
        float targetX,
        float targetY,
        int hops,
        out ReferenceWorldSetPathPlan plan)
    {
        plan = default(ReferenceWorldSetPathPlan);
        if (path == null || hops < 0 ||
            !IsFinite(worldX) || !IsFinite(worldY) ||
            !IsFinite(targetX) || !IsFinite(targetY))
        {
            return false;
        }

        path.Clear();
        if (mask == null)
        {
            plan = CreatePlan(
                ReferenceWorldSetPathOutcome.DirectTarget,
                true,
                true,
                targetX,
                targetY,
                false,
                0f,
                0f,
                0,
                default(Vector2Int),
                default(Vector2Int),
                false,
                0,
                0,
                default(Vector2Int));
            return true;
        }

        int startX;
        int startY;
        int rawGoalX;
        int rawGoalY;
        if (!TryTruncateCell(worldX, mask.CellWidthPixels, out startX) ||
            !TryTruncateCell(worldY, mask.CellHeightPixels, out startY) ||
            !TryTruncateCell(targetX, mask.CellWidthPixels, out rawGoalX) ||
            !TryTruncateCell(targetY, mask.CellHeightPixels, out rawGoalY))
        {
            return false;
        }

        Vector2Int startCell = new Vector2Int(startX, startY);
        Vector2Int rawGoalCell = new Vector2Int(rawGoalX, rawGoalY);
        if (!mask.IsCellWalkable(startX, startY))
        {
            plan = Stopped(startCell, rawGoalCell, false);
            return true;
        }

        int goalX = rawGoalX;
        int goalY = rawGoalY;
        if (!ReferenceWorldPathSearchPrimitives.TrySnapWalkable(
                mask, ref goalX, ref goalY))
        {
            plan = Stopped(startCell, rawGoalCell, false);
            return true;
        }

        bool exactEnd = goalX == rawGoalX && goalY == rawGoalY;
        Vector2Int goalCell = new Vector2Int(goalX, goalY);
        if (startX == goalX && startY == goalY)
        {
            if (!exactEnd)
            {
                plan = Stopped(startCell, goalCell, false);
                return true;
            }

            plan = CreatePlan(
                ReferenceWorldSetPathOutcome.DirectTarget,
                true,
                true,
                targetX,
                targetY,
                false,
                0f,
                0f,
                0,
                startCell,
                goalCell,
                true,
                0,
                0,
                default(Vector2Int));
            return true;
        }

        List<int> cells = new List<int>();
        if (ReferenceWorldAStarCells.TryFind(
                mask,
                startX,
                startY,
                goalX,
                goalY,
                cells,
                PrimaryExpansionCap))
        {
            bool moveToTarget;
            if (!ReferenceWorldBuildPath.TryAppendWaypoints(
                    mask,
                    cells,
                    targetX,
                    targetY,
                    exactEnd,
                    path,
                    out moveToTarget))
            {
                path.Clear();
                return false;
            }

            plan = CreatePlan(
                ReferenceWorldSetPathOutcome.PrimaryPath,
                moveToTarget,
                false,
                0f,
                0f,
                false,
                0f,
                0f,
                0,
                startCell,
                goalCell,
                exactEnd,
                0,
                0,
                default(Vector2Int));
            return true;
        }

        if (hops > MaximumRetryHops)
        {
            plan = Stopped(startCell, goalCell, exactEnd);
            return true;
        }

        List<RetryCandidate> candidates = BuildRetryCandidates(
            mask,
            startX,
            startY,
            targetX - worldX,
            targetY - worldY,
            worldX,
            worldY);
        candidates.Sort(CompareRetryCandidates);

        for (int index = 0; index < candidates.Count; index++)
        {
            RetryCandidate candidate = candidates[index];
            cells.Clear();
            if (!ReferenceWorldAStarCells.TryFind(
                    mask,
                    startX,
                    startY,
                    candidate.CellX,
                    candidate.CellY,
                    cells,
                    RetryExpansionCap))
            {
                continue;
            }

            bool moveToTarget;
            if (!ReferenceWorldBuildPath.TryAppendWaypoints(
                    mask,
                    cells,
                    targetX,
                    targetY,
                    false,
                    path,
                    out moveToTarget))
            {
                path.Clear();
                return false;
            }

            Vector2Int selected = new Vector2Int(candidate.CellX, candidate.CellY);
            plan = CreatePlan(
                ReferenceWorldSetPathOutcome.RetryPath,
                moveToTarget,
                false,
                0f,
                0f,
                true,
                targetX,
                targetY,
                hops + 1,
                startCell,
                goalCell,
                exactEnd,
                candidates.Count,
                index + 1,
                selected);
            return true;
        }

        plan = CreatePlan(
            ReferenceWorldSetPathOutcome.Stopped,
            false,
            false,
            0f,
            0f,
            false,
            0f,
            0f,
            0,
            startCell,
            goalCell,
            exactEnd,
            candidates.Count,
            candidates.Count,
            default(Vector2Int));
        return true;
    }

    private static List<RetryCandidate> BuildRetryCandidates(
        ReferenceWorldCollisionMask mask,
        int startX,
        int startY,
        float directionX,
        float directionY,
        float worldX,
        float worldY)
    {
        List<RetryCandidate> candidates = new List<RetryCandidate>(32);
        for (int deltaY = -4; deltaY <= 4; deltaY++)
        {
            AddRetryCandidate(
                mask, candidates, startX - 4, startY + deltaY,
                directionX, directionY, worldX, worldY);
            AddRetryCandidate(
                mask, candidates, startX + 4, startY + deltaY,
                directionX, directionY, worldX, worldY);
        }

        for (int deltaX = -3; deltaX <= 3; deltaX++)
        {
            AddRetryCandidate(
                mask, candidates, startX + deltaX, startY - 3,
                directionX, directionY, worldX, worldY);
            AddRetryCandidate(
                mask, candidates, startX + deltaX, startY + 3,
                directionX, directionY, worldX, worldY);
        }

        return candidates;
    }

    private static void AddRetryCandidate(
        ReferenceWorldCollisionMask mask,
        ICollection<RetryCandidate> candidates,
        int cellX,
        int cellY,
        float directionX,
        float directionY,
        float worldX,
        float worldY)
    {
        if (!mask.IsCellWalkable(cellX, cellY))
        {
            return;
        }

        float offsetX = (cellX + 0.5f) * mask.CellWidthPixels - worldX;
        float offsetY = (cellY + 0.5f) * mask.CellHeightPixels - worldY;
        float dot = directionX * offsetX + directionY * offsetY;
        long score = (long)dot;
        candidates.Add(new RetryCandidate(score, cellX, cellY));
    }

    private static int CompareRetryCandidates(
        RetryCandidate first,
        RetryCandidate second)
    {
        // The locked <>c comparator is exactly b.Item1.CompareTo(a.Item1).
        return second.Score.CompareTo(first.Score);
    }

    private static ReferenceWorldSetPathPlan Stopped(
        Vector2Int startCell,
        Vector2Int goalCell,
        bool exactEnd)
    {
        return CreatePlan(
            ReferenceWorldSetPathOutcome.Stopped,
            false,
            false,
            0f,
            0f,
            false,
            0f,
            0f,
            0,
            startCell,
            goalCell,
            exactEnd,
            0,
            0,
            default(Vector2Int));
    }

    private static ReferenceWorldSetPathPlan CreatePlan(
        ReferenceWorldSetPathOutcome outcome,
        bool moveToTarget,
        bool targetUpdated,
        float targetX,
        float targetY,
        bool pendingTargetUpdated,
        float pendingX,
        float pendingY,
        int pendingHops,
        Vector2Int startCell,
        Vector2Int goalCell,
        bool exactEnd,
        int retryCandidateCount,
        int retryAttemptCount,
        Vector2Int selectedRetryCell)
    {
        return new ReferenceWorldSetPathPlan(
            outcome,
            moveToTarget,
            targetUpdated,
            targetX,
            targetY,
            pendingTargetUpdated,
            pendingX,
            pendingY,
            pendingHops,
            startCell,
            goalCell,
            exactEnd,
            retryCandidateCount,
            retryAttemptCount,
            selectedRetryCell);
    }

    private static bool TryTruncateCell(float worldPixel, int cellSize, out int cell)
    {
        cell = 0;
        float divided = worldPixel / cellSize;
        if (divided < int.MinValue || divided > int.MaxValue)
        {
            return false;
        }

        cell = (int)divided;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>
/// Read-only projection of one exact SetPathTo plan into real-combat admission.
/// </summary>
internal struct ReferenceWorldApproachEvaluation
{
    public bool Evaluated;
    public bool AlreadyInRange;
    public ReferenceWorldSetPathOutcome Outcome;
    public bool MoveToTarget;
    public int WaypointCount;
    public float FinalX;
    public float FinalY;
    public float FinalDistance;
    public int AttackRange;
    public bool CurrentLineOfSight;
    public bool EndpointLineOfSight;
    public bool Admissible;
}

/// <summary>
/// Decision-free adapter: it mutates only a new temporary path and never applies the plan.
/// </summary>
internal static class ReferenceWorldSetPathToRealCombatTailAdapter
{
    public static bool TryEvaluateApproach(
        ReferenceWorldCollisionMask mask,
        float playerX,
        float playerY,
        float targetX,
        float targetY,
        int attackDistancePixels,
        out ReferenceWorldApproachEvaluation evaluation)
    {
        evaluation = default(ReferenceWorldApproachEvaluation);
        int range = ReferenceWorldBasicAttackController.AttackRangePixels(
            attackDistancePixels);
        float deltaX = targetX - playerX;
        float deltaY = targetY - playerY;
        float currentDistance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        bool currentLineOfSight = HasLineOfSight(
            mask,
            playerX,
            playerY,
            targetX,
            targetY);
        if (ReferenceWorldBasicAttackController.IsInRange(
                playerX,
                playerY,
                targetX,
                targetY,
                attackDistancePixels))
        {
            evaluation = new ReferenceWorldApproachEvaluation
            {
                Evaluated = true,
                AlreadyInRange = true,
                Outcome = ReferenceWorldSetPathOutcome.None,
                FinalX = playerX,
                FinalY = playerY,
                FinalDistance = currentDistance,
                AttackRange = range,
                CurrentLineOfSight = currentLineOfSight,
                EndpointLineOfSight = currentLineOfSight,
                Admissible = currentLineOfSight,
            };
            return true;
        }

        List<Vector2> temporaryPath = new List<Vector2>();
        ReferenceWorldSetPathPlan plan;
        if (!ReferenceWorldSetPathTo.TryApply(
                mask,
                temporaryPath,
                playerX,
                playerY,
                targetX,
                targetY,
                0,
                out plan))
        {
            return false;
        }

        float finalX = playerX;
        float finalY = playerY;
        if (temporaryPath.Count > 0)
        {
            Vector2 final = temporaryPath[temporaryPath.Count - 1];
            finalX = final.x;
            finalY = final.y;
        }
        else if (plan.TargetUpdated)
        {
            finalX = plan.TargetX;
            finalY = plan.TargetY;
        }

        float finalDeltaX = targetX - finalX;
        float finalDeltaY = targetY - finalY;
        float finalDistance = Mathf.Sqrt(
            finalDeltaX * finalDeltaX + finalDeltaY * finalDeltaY);
        bool endpointLineOfSight = HasLineOfSight(
            mask,
            finalX,
            finalY,
            targetX,
            targetY);
        evaluation = new ReferenceWorldApproachEvaluation
        {
            Evaluated = true,
            AlreadyInRange = false,
            Outcome = plan.Outcome,
            MoveToTarget = plan.MoveToTarget,
            WaypointCount = temporaryPath.Count,
            FinalX = finalX,
            FinalY = finalY,
            FinalDistance = finalDistance,
            AttackRange = range,
            CurrentLineOfSight = currentLineOfSight,
            EndpointLineOfSight = endpointLineOfSight,
            Admissible = plan.MoveToTarget && finalDistance <= range &&
                         endpointLineOfSight,
        };
        return true;
    }

    private static bool HasLineOfSight(
        ReferenceWorldCollisionMask mask,
        float fromX,
        float fromY,
        float toX,
        float toY)
    {
        int fromCellX;
        int fromCellY;
        int toCellX;
        int toCellY;
        return mask != null &&
               mask.TryWorldPixelToCell(fromX, fromY, out fromCellX, out fromCellY) &&
               mask.TryWorldPixelToCell(toX, toY, out toCellX, out toCellY) &&
               mask.IsLineWalkable(fromCellX, fromCellY, toCellX, toCellY);
    }
}

}
