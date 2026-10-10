using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned translation of PlayerController.BuildPath for a validated A* cell route.
/// </summary>
/// <remarks>
/// The locked method greedily chooses the farthest future cell reachable by LineWalkable, appends
/// that cell's mask-pixel center, and repeats. When exactEnd is true, the final appended cell
/// center is replaced by the original targetX/targetY pair. SetPathTo and Transform movement
/// remain outside this boundary.
/// </remarks>
public static class ReferenceWorldBuildPath
{
    public static bool TryAppendWaypoints(
        ReferenceWorldCollisionMask mask,
        IReadOnlyList<int> cells,
        float targetX,
        float targetY,
        bool exactEnd,
        List<Vector2> path,
        out bool moveToTarget)
    {
        moveToTarget = false;
        if (mask == null || cells == null || path == null || cells.Count < 2)
        {
            return false;
        }

        for (int index = 0; index < cells.Count; index++)
        {
            if (cells[index] < 0 || cells[index] >= mask.CellCount)
            {
                return false;
            }
        }

        List<Vector2> additions = new List<Vector2>();
        int lastIndex = cells.Count - 1;
        int currentIndex = 0;
        while (currentIndex < lastIndex)
        {
            int currentCell = cells[currentIndex];
            int currentY = currentCell / mask.Width;
            int currentX = currentCell - currentY * mask.Width;
            int nextIndex = currentIndex + 1;
            int selectedIndex = lastIndex;

            if (nextIndex < lastIndex)
            {
                while (true)
                {
                    int selectedCell = cells[selectedIndex];
                    int selectedY = selectedCell / mask.Width;
                    int selectedX = selectedCell - selectedY * mask.Width;
                    if (mask.IsLineWalkable(
                            currentX, currentY, selectedX, selectedY))
                    {
                        break;
                    }

                    // The original does not test the adjacent fallback: after a failed line it
                    // decrements first and stops once the candidate reaches nextIndex.
                    selectedIndex--;
                    if (selectedIndex <= nextIndex)
                    {
                        break;
                    }
                }
            }

            int waypointCell = cells[selectedIndex];
            int waypointY = waypointCell / mask.Width;
            int waypointX = waypointCell - waypointY * mask.Width;
            additions.Add(new Vector2(
                (waypointX + 0.5f) * mask.CellWidthPixels,
                (waypointY + 0.5f) * mask.CellHeightPixels));

            currentIndex = selectedIndex;
            lastIndex = cells.Count - 1;
        }

        if (additions.Count == 0)
        {
            return false;
        }

        path.AddRange(additions);
        if (exactEnd)
        {
            path[path.Count - 1] = new Vector2(targetX, targetY);
        }

        moveToTarget = path.Count > 0;
        return true;
    }
}

}
