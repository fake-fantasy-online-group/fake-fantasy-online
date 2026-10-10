using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One immutable remote-player mount composition selected by ComposeRemoteMount.
/// </summary>
internal sealed class ReferenceMountRemoteComposition
{
    private readonly ReferenceMountLocalComposition shared;

    public ReferenceMountRemoteComposition(ReferenceMountLocalComposition shared)
    {
        this.shared = shared ?? throw new ArgumentNullException(nameof(shared));
        RiderSortingBase = shared.BodySortingOrder +
            (shared.BelowAnchorPixels == 0 ? 1 : 2);
    }

    public ReferenceMountActionKind ActionKind => shared.ActionKind;
    public int ActionId => shared.ActionId;
    public float IntervalSeconds => shared.IntervalSeconds;
    public int Direction => shared.Direction;
    public int FrameIndex => shared.FrameIndex;
    public bool FlipX => shared.FlipX;
    public Sprite BodySprite => shared.BodySprite;
    public Sprite TopSprite => shared.TopSprite;
    public bool HasTopSprite => shared.HasTopSprite;
    public bool HasEffectSprite => shared.HasEffectSprite;
    public bool TopUsesAdditiveMaterial => shared.TopUsesAdditiveMaterial;
    public Vector2 BodyLocalPosition => shared.BodyLocalPosition;
    public Vector2 TopLocalPosition => shared.TopLocalPosition;
    public Vector2 SaddlePixels => shared.SaddlePixels;
    public Vector2 RiderLocalPosition => shared.RiderLocalPosition;
    public int BodySortingOrder => shared.BodySortingOrder;
    public int RiderSortingBase { get; }
    public int TopSortingOrder => shared.TopSortingOrder;
    public int EffectSortingOrder => shared.EffectSortingOrder;
    public int MountBoundTopPixels => shared.MountBoundTopPixels;
    public int BelowAnchorPixels => shared.BelowAnchorPixels;
}

/// <summary>
/// Remote-player translation of the reviewed normal ComposeRemoteMount branch.
/// </summary>
/// <remarks>
/// The mount source grids, saddle projection and shared clock are identical to the local source
/// composer. RemotePlayer differs at the actor boundary: it consumes the already stored art
/// direction/flip and places the rider one or two sorting slots above the mount base before its
/// own ApplyPose tail. Ring, tianmo and multi-seat/anchor branches remain fail closed.
/// </remarks>
internal sealed class ReferenceMountRemoteComposer : IDisposable
{
    private readonly ReferenceMountLocalComposer shared;
    private bool disposed;

    private ReferenceMountRemoteComposer(ReferenceMountLocalComposer shared)
    {
        this.shared = shared ?? throw new ArgumentNullException(nameof(shared));
    }

    public int FrameCounter => shared.FrameCounter;
    public float TimerSeconds => shared.TimerSeconds;
    public float PixelsPerUnit => shared.PixelsPerUnit;

    public static bool TryCreate(
        ReferenceMountAvatarModel model,
        float pixelsPerUnit,
        out ReferenceMountRemoteComposer composer)
    {
        composer = null;
        ReferenceMountLocalComposer shared;
        if (!ReferenceMountLocalComposer.TryCreate(model, pixelsPerUnit, out shared))
        {
            return false;
        }

        composer = new ReferenceMountRemoteComposer(shared);
        return true;
    }

    public bool TryAdvance(
        bool moving,
        int direction,
        bool flipX,
        float mapY,
        float deltaTime,
        out ReferenceMountRemoteComposition composition)
    {
        composition = null;
        int octant;
        if (disposed || !TryResolveOctant(direction, flipX, out octant))
        {
            return false;
        }

        ReferenceMountLocalComposition selected;
        if (!shared.TryAdvance(moving, octant, mapY, deltaTime, out selected))
        {
            return false;
        }

        composition = new ReferenceMountRemoteComposition(selected);
        return true;
    }

    public bool TryComposeAt(
        ReferenceMountActionKind kind,
        int direction,
        bool flipX,
        int frameIndex,
        float mapY,
        out ReferenceMountRemoteComposition composition)
    {
        composition = null;
        int octant;
        if (disposed || !TryResolveOctant(direction, flipX, out octant))
        {
            return false;
        }

        ReferenceMountLocalComposition selected;
        if (!shared.TryComposeAt(kind, octant, frameIndex, mapY, out selected))
        {
            return false;
        }

        composition = new ReferenceMountRemoteComposition(selected);
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        shared.Dispose();
    }

    private static bool TryResolveOctant(int direction, bool flipX, out int octant)
    {
        octant = ReferenceWorldPlayerMotionMath.FacingOctant(direction, flipX);
        ReferenceWorldPlayerFacing facing =
            ReferenceWorldPlayerMotionMath.FromOctant(octant);
        return facing.StoredDirection == direction && facing.FlipX == flipX;
    }
}

}
