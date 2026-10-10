using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned translation of PlayerController.AStarCells for the bounded collision-mask path.
/// </summary>
/// <remarks>
/// The open set is the original default-comparer SortedSet of (priority, cellIndex), so equal
/// priorities are resolved by the row-major cell index rather than insertion order or remaining
/// heuristic. Successful reconstruction appends goal-to-start to the supplied list and reverses
/// the complete list, matching the locked method. Callers should therefore supply an empty list.
/// Input, BuildPath, Transform movement and waypoint smoothing remain outside this boundary.
/// </remarks>
public static class ReferenceWorldAStarCells
{
    public const int NeighborCount = 8;

    public static bool TryFind(
        ReferenceWorldCollisionMask mask,
        int startX,
        int startY,
        int goalX,
        int goalY,
        List<int> cells,
        int expansionCap)
    {
        if (mask == null)
        {
            return false;
        }

        Dictionary<int, int> parents = new Dictionary<int, int>();
        Dictionary<int, int> costs = new Dictionary<int, int>();
        HashSet<int> closed = new HashSet<int>();
        SortedSet<(int Priority, int CellIndex)> open =
            new SortedSet<(int Priority, int CellIndex)>();

        int width = mask.Width;
        int startIndex = startX + width * startY;
        int goalIndex = goalX + width * goalY;
        costs[startIndex] = 0;
        open.Add((
            ReferenceWorldPathSearchPrimitives.OctileHeuristic(
                startX, startY, goalX, goalY),
            startIndex));

        int expanded = 0;
        while (open.Count > 0)
        {
            (int Priority, int CellIndex) entry = open.Min;
            open.Remove(entry);
            int currentIndex = entry.CellIndex;

            // The official method checks the goal before the closed set and expansion cap.
            if (currentIndex == goalIndex)
            {
                return Reconstruct(cells, parents, startIndex, goalIndex);
            }

            if (!closed.Add(currentIndex))
            {
                continue;
            }

            expanded++;
            if (expanded > expansionCap)
            {
                return false;
            }

            int currentY = currentIndex / width;
            int currentX = currentIndex - currentY * width;
            int currentCost = costs[currentIndex];

            for (int directionIndex = 0;
                 directionIndex < NeighborCount;
                 directionIndex++)
            {
                Vector2Int delta;
                if (!ReferenceWorldPlayerMotionMath.TryGetPathNeighborDelta(
                        directionIndex, out delta))
                {
                    return false;
                }

                int candidateX = currentX + delta.x;
                int candidateY = currentY + delta.y;
                if (!mask.IsCellWalkable(candidateX, candidateY))
                {
                    continue;
                }

                int candidateCost;
                if (directionIndex < 4)
                {
                    candidateCost =
                        currentCost + ReferenceWorldPathSearchPrimitives.CardinalCost;
                }
                else
                {
                    // Both orthogonal cells must be open; one open side is not enough.
                    if (!mask.IsCellWalkable(currentX + delta.x, currentY) ||
                        !mask.IsCellWalkable(currentX, currentY + delta.y))
                    {
                        continue;
                    }

                    candidateCost =
                        currentCost + ReferenceWorldPathSearchPrimitives.DiagonalCost;
                }

                int candidateIndex = candidateX + width * candidateY;
                int previousCost;
                if (costs.TryGetValue(candidateIndex, out previousCost) &&
                    candidateCost >= previousCost)
                {
                    continue;
                }

                costs[candidateIndex] = candidateCost;
                parents[candidateIndex] = currentIndex;
                int priority = candidateCost +
                    ReferenceWorldPathSearchPrimitives.OctileHeuristic(
                        candidateX, candidateY, goalX, goalY);
                open.Add((priority, candidateIndex));
            }
        }

        return false;
    }

    private static bool Reconstruct(
        List<int> cells,
        IReadOnlyDictionary<int, int> parents,
        int startIndex,
        int goalIndex)
    {
        if (cells == null)
        {
            return false;
        }

        int currentIndex = goalIndex;
        while (true)
        {
            cells.Add(currentIndex);
            if (currentIndex == startIndex)
            {
                cells.Reverse();
                return true;
            }

            currentIndex = parents[currentIndex];
        }
    }
}

}
