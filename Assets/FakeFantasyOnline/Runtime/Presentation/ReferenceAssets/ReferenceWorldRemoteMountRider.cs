using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal interface IReferenceWorldRemoteRiderDoll
{
    bool Built { get; }
    Transform Root { get; }
    SpriteRenderer MainRenderer { get; }
    ReferenceWorldPlayerFacing CurrentFacing { get; }

    bool ApplyRemoteMountedRider(
        ReferenceMountRemoteComposition composition,
        float standTimeSeconds,
        Color32 tint);
}

/// <summary>
/// Owns ordinary remote-player mount renderers and rider Doll boundary.
/// </summary>
/// <remarks>
/// Construction requires a server-authored remote spawn or riding-update state. Ordinary
/// single-seat, non-anchor, non-tianmo mounts are selected from NpcActions and package art by
/// model id. Ring, tianmo, anchor and multi-seat behavior remain separate official branches.
/// </remarks>
internal sealed class ReferenceWorldRemoteMountRider : IDisposable
{
    private readonly IReferenceWorldRemoteRiderDoll doll;
    private readonly WorldRemoteRidingState state;
    private readonly ReferenceMountRemoteComposer composer;

    private GameObject rootObject;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer topRenderer;
    private SpriteRenderer effectRenderer;
    private ReferenceMountRemoteComposition currentComposition;
    private bool artVisible = true;
    private bool built;
    private bool disposed;

    private ReferenceWorldRemoteMountRider(
        IReferenceWorldRemoteRiderDoll doll,
        WorldRemoteRidingState state,
        ReferenceMountRemoteComposer composer)
    {
        this.doll = doll ?? throw new ArgumentNullException(nameof(doll));
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.composer = composer ?? throw new ArgumentNullException(nameof(composer));
    }

    public bool Built => built;
    public Transform Root => rootObject == null ? null : rootObject.transform;
    public SpriteRenderer BodyRenderer => bodyRenderer;
    public SpriteRenderer TopRenderer => topRenderer;
    public SpriteRenderer EffectRenderer => effectRenderer;
    public WorldRemoteRidingState State => state;
    public ReferenceMountRemoteComposition CurrentComposition => currentComposition;
    public bool ArtVisible => artVisible;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        IReferenceWorldRemoteRiderDoll doll,
        WorldRemoteRidingState state,
        out ReferenceWorldRemoteMountRider owner)
    {
        owner = null;
        if (resources == null || doll == null || !doll.Built || doll.Root == null ||
            state == null || !state.Riding ||
            state.MountModel < 1 ||
            state.MountTianmo || state.RiderSeat != 0 ||
            state.RideAnchorId != 0 || state.MountSeats != 0)
        {
            return false;
        }

        ReferenceMountAvatarModel model;
        ReferenceMountRemoteComposer composer;
        if (!ReferenceMountAvatarModel.TryLoad(resources, state.MountModel, out model) ||
            model == null || model.ModelId != state.MountModel ||
            !model.UsesDefaultStandRiderPose ||
            !ReferenceMountRemoteComposer.TryCreate(
                model,
                ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
                out composer))
        {
            return false;
        }

        owner = new ReferenceWorldRemoteMountRider(doll, state, composer);
        return true;
    }

    public bool Build(float mapY, float standTimeSeconds, Color32 riderTint)
    {
        ThrowIfDisposed();
        if (built || doll.Root == null || doll.MainRenderer == null)
        {
            return false;
        }

        try
        {
            rootObject = new GameObject("RecoveredRemoteMount");
            rootObject.transform.SetParent(doll.Root, false);
            bodyRenderer = CreateLayer("RecoveredRemoteMountBody");
            topRenderer = CreateLayer("RecoveredRemoteMountTop");
            effectRenderer = CreateLayer("RecoveredRemoteMountEffect");
            effectRenderer.enabled = false;

            ReferenceMountRemoteComposition composition;
            if (!composer.TryComposeAt(
                    ReferenceMountActionKind.Stand,
                    doll.CurrentFacing.StoredDirection,
                    doll.CurrentFacing.FlipX,
                    0,
                    mapY,
                    out composition) ||
                !ApplyComposition(composition, standTimeSeconds, riderTint))
            {
                ReleaseHierarchy();
                return false;
            }

            built = true;
            return true;
        }
        catch
        {
            ReleaseHierarchy();
            throw;
        }
    }

    public bool TryAdvance(
        bool moving,
        float deltaTime,
        float mapY,
        float standTimeSeconds,
        Color32 riderTint)
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        ReferenceMountRemoteComposition composition;
        return composer.TryAdvance(
                   moving,
                   doll.CurrentFacing.StoredDirection,
                   doll.CurrentFacing.FlipX,
                   mapY,
                   deltaTime,
                   out composition) &&
               ApplyComposition(composition, standTimeSeconds, riderTint);
    }

    internal bool TryApplyAt(
        ReferenceMountActionKind kind,
        int direction,
        bool flipX,
        int frameIndex,
        float mapY,
        float standTimeSeconds,
        Color32 riderTint)
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        ReferenceMountRemoteComposition composition;
        return composer.TryComposeAt(
                   kind,
                   direction,
                   flipX,
                   frameIndex,
                   mapY,
                   out composition) &&
               ApplyComposition(composition, standTimeSeconds, riderTint);
    }

    public bool Matches(WorldRemoteRidingState value)
    {
        return value != null && state.EntityId == value.EntityId &&
               state.Riding == value.Riding &&
               state.MountModel == value.MountModel &&
               state.MountTianmo == value.MountTianmo &&
               state.RiderSeat == value.RiderSeat &&
               state.RideAnchorId == value.RideAnchorId &&
               state.MountSeats == value.MountSeats;
    }

    public bool SetArtVisible(bool value)
    {
        ThrowIfDisposed();
        if (!built || currentComposition == null)
        {
            return false;
        }

        artVisible = value;
        ApplyLayerVisibility();
        return true;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseHierarchy();
        composer.Dispose();
    }

    private SpriteRenderer CreateLayer(string objectName)
    {
        GameObject layerObject = new GameObject(objectName);
        layerObject.transform.SetParent(rootObject.transform, false);
        SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
        renderer.sharedMaterial = doll.MainRenderer.sharedMaterial;
        renderer.color = new Color32(255, 255, 255, 255);
        return renderer;
    }

    private bool ApplyComposition(
        ReferenceMountRemoteComposition composition,
        float standTimeSeconds,
        Color32 riderTint)
    {
        if (composition == null || bodyRenderer == null || topRenderer == null ||
            effectRenderer == null || composition.BodySprite == null)
        {
            return false;
        }

        bodyRenderer.sprite = composition.BodySprite;
        bodyRenderer.flipX = composition.FlipX;
        bodyRenderer.transform.localPosition = Vector3.zero;
        bodyRenderer.sortingOrder = composition.BodySortingOrder;
        bodyRenderer.color = new Color32(255, 255, 255, 255);
        bodyRenderer.sharedMaterial = doll.MainRenderer.sharedMaterial;

        topRenderer.sprite = composition.TopSprite;
        topRenderer.flipX = composition.FlipX;
        topRenderer.transform.localPosition = new Vector3(
            composition.TopLocalPosition.x,
            composition.TopLocalPosition.y,
            0f);
        topRenderer.sortingOrder = composition.TopSortingOrder;
        topRenderer.color = new Color32(255, 255, 255, 255);
        topRenderer.sharedMaterial = doll.MainRenderer.sharedMaterial;

        effectRenderer.sprite = null;
        effectRenderer.flipX = composition.FlipX;
        effectRenderer.transform.localPosition = Vector3.zero;
        effectRenderer.sortingOrder = composition.EffectSortingOrder;
        effectRenderer.color = new Color32(255, 255, 255, 255);

        if (!doll.ApplyRemoteMountedRider(composition, standTimeSeconds, riderTint))
        {
            return false;
        }

        currentComposition = composition;
        ApplyLayerVisibility();
        return true;
    }

    private void ApplyLayerVisibility()
    {
        bodyRenderer.enabled = artVisible;
        topRenderer.enabled = artVisible && currentComposition.HasTopSprite;
        effectRenderer.enabled = false;
    }

    private void ReleaseHierarchy()
    {
        DestroyOwned(rootObject);
        rootObject = null;
        bodyRenderer = null;
        topRenderer = null;
        effectRenderer = null;
        currentComposition = null;
        artVisible = true;
        built = false;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldRemoteMountRider));
        }
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (value == null)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }
}

}
