using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Immutable view of the official 1.5.8 PlayerController octant mapping.
/// </summary>
public struct ReferenceWorldPlayerFacing
{
    public ReferenceWorldPlayerFacing(
        int octant,
        int storedDirection,
        bool flipX,
        string name)
    {
        Octant = octant;
        StoredDirection = storedDirection;
        FlipX = flipX;
        Name = name ?? string.Empty;
    }

    public int Octant { get; }
    public int StoredDirection { get; }
    public bool FlipX { get; }
    public string Name { get; }
}

/// <summary>
/// Source-owned translation of the bounded PlayerController path-neighbor, facing and step
/// equations.
/// </summary>
/// <remarks>
/// This class deliberately contains no input loop, path movement, transform mutation or animation
/// selection. Those surrounding behaviors remain outside this recovered boundary.
/// </remarks>
public static class ReferenceWorldPlayerMotionMath
{
    public const float FaceEpsilon = 0.0001f;
    public const int DefaultFacingOctant = 2;
    public const int SpeedAttributeIndex = 9;
    public const float MoveDeltaResetSeconds = 0.1f;
    public const float MoveDeltaAverageWeight = 0.05f;
    public const float MoveDeltaMinimumFactor = 0.8f;
    public const float MoveDeltaMaximumFactor = 1.2f;

    // AStarCells direction order. This is not the visual octant order below.
    private static readonly int[] DeltaXByPathNeighbor =
    {
        1, -1, 0, 0, 1, 1, -1, -1,
    };

    private static readonly int[] DeltaYByPathNeighbor =
    {
        0, 0, 1, -1, 1, -1, 1, -1,
    };

    private static readonly string[] NameByOctant =
    {
        "东", "东南", "南", "西南", "西", "西北", "北", "东北",
    };

    public static ReferenceWorldPlayerFacing FromOctant(int octant)
    {
        int normalized = octant & 7;
        PlayerController.OctToStored(octant, out int storedDirection, out bool flipX);
        return new ReferenceWorldPlayerFacing(
            normalized,
            storedDirection,
            flipX,
            NameByOctant[normalized]);
    }

    public static int FacingOctant(int storedDirection, bool flipX)
    {
        for (int octant = 0; octant < 8; octant++)
        {
            PlayerController.OctToStored(octant, out int nativeDirection, out bool nativeFlip);
            if (nativeDirection == storedDirection && nativeFlip == flipX)
            {
                return octant;
            }
        }

        return DefaultFacingOctant;
    }

    public static string OctantName(int octant)
    {
        return octant >= 0 && octant < NameByOctant.Length
            ? NameByOctant[octant]
            : string.Empty;
    }

    public static bool TryGetPathNeighborDelta(
        int directionIndex,
        out Vector2Int delta)
    {
        if (directionIndex < 0 ||
            directionIndex >= DeltaXByPathNeighbor.Length)
        {
            delta = default(Vector2Int);
            return false;
        }

        delta = new Vector2Int(
            DeltaXByPathNeighbor[directionIndex],
            DeltaYByPathNeighbor[directionIndex]);
        return true;
    }

    public static bool TryFaceTowards(
        Vector3 currentPosition,
        Vector3 targetPosition,
        ReferenceWorldPlayerFacing currentFacing,
        out ReferenceWorldPlayerFacing resultingFacing)
    {
        resultingFacing = currentFacing;
        float deltaX = targetPosition.x - currentPosition.x;
        float deltaY = currentPosition.y - targetPosition.y;
        if (Mathf.Abs(deltaX) < FaceEpsilon &&
            Mathf.Abs(deltaY) < FaceEpsilon)
        {
            return false;
        }

        int octant =
            (Mathf.RoundToInt(Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg / 45f) + 8) % 8;
        resultingFacing = FromOctant(octant);
        return true;
    }

    public static float MoveStepPixels(
        float fallbackMoveSpeed,
        float pixelsPerSpeed,
        IReadOnlyList<int> attributes)
    {
        if (attributes == null ||
            attributes.Count <= SpeedAttributeIndex ||
            attributes[SpeedAttributeIndex] <= 0)
        {
            return fallbackMoveSpeed;
        }

        return pixelsPerSpeed * attributes[SpeedAttributeIndex];
    }

    /// <summary>
    /// Source translation of official 1.5.8 PlayerController.MoveDt.
    /// </summary>
    public static float SmoothMoveDelta(
        float deltaTime,
        ref float movingAverage)
    {
        if (!(deltaTime > 0f))
        {
            return 0f;
        }

        if (deltaTime > MoveDeltaResetSeconds)
        {
            movingAverage = 0f;
            return deltaTime;
        }

        movingAverage = movingAverage > 0f
            ? movingAverage +
              (deltaTime - movingAverage) * MoveDeltaAverageWeight
            : deltaTime;
        return Mathf.Clamp(
            movingAverage,
            deltaTime * MoveDeltaMinimumFactor,
            deltaTime * MoveDeltaMaximumFactor);
    }
}

}
