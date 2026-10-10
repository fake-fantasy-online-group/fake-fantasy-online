using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned PlayerController primitives used immediately before the full A* branch.
/// </summary>
/// <remarks>
/// This boundary preserves SnapWalkable's deterministic square-ring order and Heur's 10/14
/// octile cost. It does not own the A* open set, route reconstruction, input or Transform movement.
/// </remarks>
public static class ReferenceWorldPathSearchPrimitives
{
    public const int MaximumSnapRadius = 50;
    public const int CardinalCost = 10;
    public const int DiagonalCost = 14;

    public static bool TrySnapWalkable(
        ReferenceWorldCollisionMask mask,
        ref int cellX,
        ref int cellY)
    {
        if (mask == null)
        {
            return false;
        }

        if (mask.IsCellWalkable(cellX, cellY))
        {
            return true;
        }

        int originX = cellX;
        int originY = cellY;
        for (int radius = 1; radius <= MaximumSnapRadius; radius++)
        {
            for (int deltaY = -radius; deltaY <= radius; deltaY++)
            {
                for (int deltaX = -radius; deltaX <= radius; deltaX++)
                {
                    if (Math.Abs(deltaX) != radius && Math.Abs(deltaY) != radius)
                    {
                        continue;
                    }

                    long candidateXLong = (long)originX + deltaX;
                    long candidateYLong = (long)originY + deltaY;
                    if (candidateXLong < int.MinValue || candidateXLong > int.MaxValue ||
                        candidateYLong < int.MinValue || candidateYLong > int.MaxValue)
                    {
                        continue;
                    }

                    int candidateX = (int)candidateXLong;
                    int candidateY = (int)candidateYLong;
                    if (!mask.IsCellWalkable(candidateX, candidateY))
                    {
                        continue;
                    }

                    cellX = candidateX;
                    cellY = candidateY;
                    return true;
                }
            }
        }

        return false;
    }

    public static int OctileHeuristic(
        int cellX,
        int cellY,
        int targetX,
        int targetY)
    {
        int deltaX = Math.Abs(targetX - cellX);
        int deltaY = Math.Abs(targetY - cellY);
        int diagonalSteps = Math.Min(deltaX, deltaY);
        return (deltaX + deltaY) * CardinalCost -
               diagonalSteps * (CardinalCost * 2 - DiagonalCost);
    }
}

}
