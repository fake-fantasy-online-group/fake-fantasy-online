using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One normal, alive, unmounted PlayerController locomotion-animation decision.
/// </summary>
public struct ReferenceWorldPlayerLocomotionPlan
{
    public ReferenceWorldPlayerLocomotionPlan(
        ReferenceWorldPlayerFacing facing,
        bool useWalkPose,
        float animationDistancePixels,
        int currentFrame)
    {
        Facing = facing;
        UseWalkPose = useWalkPose;
        AnimationDistancePixels = animationDistancePixels;
        CurrentFrame = currentFrame;
    }

    public ReferenceWorldPlayerFacing Facing { get; }
    public bool UseWalkPose { get; }
    public float AnimationDistancePixels { get; }
    public int CurrentFrame { get; }
}

/// <summary>
/// Source-owned normal-locomotion subset of PlayerController.UpdateAnim.
/// </summary>
/// <remarks>
/// Dead, mounted, sitting, action, pose lookup, ApplyPose and Transform movement branches remain
/// outside this bounded state projection.
/// </remarks>
public static class ReferenceWorldPlayerLocomotionMath
{
    public const int DirectionCount = 5;
    public const int FramesPerDirection = 8;
    public const float DistancePerFramePixels = 18f;

    public static ReferenceWorldPlayerLocomotionPlan AdvanceNormal(
        float deltaX,
        float deltaY,
        bool moving,
        float animationDistancePixels,
        int currentFrame,
        ReferenceWorldPlayerFacing currentFacing)
    {
        ValidateFinite(deltaX, nameof(deltaX));
        ValidateFinite(deltaY, nameof(deltaY));
        ValidateFinite(animationDistancePixels, nameof(animationDistancePixels));
        if (animationDistancePixels < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(animationDistancePixels));
        }

        if (currentFrame < 0 || currentFrame >= FramesPerDirection)
        {
            throw new ArgumentOutOfRangeException(nameof(currentFrame));
        }

        if (!moving || (deltaX == 0f && deltaY == 0f))
        {
            return new ReferenceWorldPlayerLocomotionPlan(
                currentFacing,
                false,
                0f,
                currentFrame);
        }

        ReferenceWorldPlayerFacing facing = FacingForMovement(
            deltaX,
            deltaY,
            moving,
            currentFacing);
        float nextDistance = animationDistancePixels +
                             Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
        int frame = (int)(nextDistance / DistancePerFramePixels) % FramesPerDirection;
        return new ReferenceWorldPlayerLocomotionPlan(
            facing,
            true,
            nextDistance,
            frame);
    }

    /// <summary>
    /// Applies the direction-selection prefix shared by the mounted and ordinary UpdateAnim
    /// branches without advancing the ordinary Doll walk clock.
    /// </summary>
    public static ReferenceWorldPlayerFacing FacingForMovement(
        float deltaX,
        float deltaY,
        bool moving,
        ReferenceWorldPlayerFacing currentFacing)
    {
        ValidateFinite(deltaX, nameof(deltaX));
        ValidateFinite(deltaY, nameof(deltaY));
        if (!moving || (deltaX == 0f && deltaY == 0f))
        {
            return currentFacing;
        }

        int octant =
            (Mathf.RoundToInt(Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg / 45f) + 8) % 8;
        return ReferenceWorldPlayerMotionMath.FromOctant(octant);
    }

    private static void ValidateFinite(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

}
