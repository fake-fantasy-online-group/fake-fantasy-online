using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Owns ordinary local mount renderers and the rider Doll application boundary.
/// </summary>
/// <remarks>
/// Construction requires a complete server-authored self-riding state. Ordinary single-seat,
/// non-anchor, non-tianmo mounts are selected from NpcActions and package art by model id; no
/// audit-sample model list participates. Entry pet summary or local UI state never proves riding.
/// </remarks>
internal sealed class ReferenceWorldLocalMountRider : IDisposable
{
    private readonly ReferenceWorldLocalPlayerDoll doll;
    private readonly WorldRidingState state;
    private readonly ReferenceMountLocalComposer composer;

    private GameObject rootObject;
    private SpriteRenderer bodyRenderer;
    private SpriteRenderer topRenderer;
    private SpriteRenderer effectRenderer;
    private ReferenceMountLocalComposition currentComposition;
    private bool artVisible;
    private bool built;
    private bool disposed;

    private ReferenceWorldLocalMountRider(
        ReferenceWorldLocalPlayerDoll doll,
        WorldRidingState state,
        ReferenceMountLocalComposer composer)
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
    public WorldRidingState State => state;
    public ReferenceMountLocalComposition CurrentComposition => currentComposition;
    public bool ArtVisible => built && artVisible;

    public static bool TryCreate(
        ReferenceResourceLoader resources,
        ReferenceWorldLocalPlayerDoll doll,
        WorldRidingState state,
        out ReferenceWorldLocalMountRider owner)
    {
        owner = null;
        if (resources == null || doll == null || !doll.Built || doll.Root == null ||
            state == null || !state.Riding ||
            state.MountModel < 1 ||
            state.RiderSeat != 0 || state.RideAnchorId != 0 || state.MountTianmo)
        {
            return false;
        }

        ReferenceMountAvatarModel model;
        ReferenceMountLocalComposer composer;
        if (!ReferenceMountAvatarModel.TryLoad(resources, state.MountModel, out model) ||
            model == null || model.ModelId != state.MountModel ||
            !model.UsesDefaultStandRiderPose ||
            !ReferenceMountLocalComposer.TryCreate(
                model,
                ReferenceWorldLocalPlayerDoll.PixelsPerUnit,
                out composer))
        {
            return false;
        }

        owner = new ReferenceWorldLocalMountRider(doll, state, composer);
        return true;
    }

    public bool Build(float mapY, Color32 riderTint)
    {
        ThrowIfDisposed();
        if (built || doll.Root == null || doll.MainRenderer == null)
        {
            return false;
        }

        try
        {
            rootObject = new GameObject("RecoveredLocalMount");
            rootObject.transform.SetParent(doll.Root, false);
            bodyRenderer = CreateLayer("RecoveredLocalMountBody");
            topRenderer = CreateLayer("RecoveredLocalMountTop");
            effectRenderer = CreateLayer("RecoveredLocalMountEffect");
            effectRenderer.enabled = false;

            ReferenceMountLocalComposition composition;
            if (!composer.TryComposeAt(
                    ReferenceMountActionKind.Stand,
                    doll.FaceOctForIndependentRuntime,
                    0,
                    mapY,
                    out composition) ||
                !ApplyComposition(composition, riderTint))
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
        Color32 riderTint)
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        ReferenceMountLocalComposition composition;
        return composer.TryAdvance(
                   moving,
                   doll.FaceOctForIndependentRuntime,
                   mapY,
                   deltaTime,
                   out composition) &&
               ApplyComposition(composition, riderTint);
    }

    internal bool TryApplyAt(
        ReferenceMountActionKind kind,
        int octant,
        int frameIndex,
        float mapY,
        Color32 riderTint)
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        ReferenceMountLocalComposition composition;
        return composer.TryComposeAt(
                   kind,
                   octant,
                   frameIndex,
                   mapY,
                   out composition) &&
               ApplyComposition(composition, riderTint);
    }

    /// <summary>
    /// Applies PlayerController.HideMount for the local dead-first UpdateAnim branch.
    /// </summary>
    /// <remarks>
    /// The official method disables all three mount renderers and restores the Doll root to
    /// local zero without changing the server-authored riding fact. The source-owned composer
    /// may retain its decoded cache, but no composition or sprite remains presented while dead.
    /// A later alive UpdateAnim can compose the still-authoritative mount again.
    /// </remarks>
    internal bool HideForLocalDeath()
    {
        ThrowIfDisposed();
        if (!built || bodyRenderer == null || topRenderer == null ||
            effectRenderer == null)
        {
            return false;
        }

        bodyRenderer.enabled = false;
        bodyRenderer.sprite = null;
        topRenderer.enabled = false;
        topRenderer.sprite = null;
        effectRenderer.enabled = false;
        effectRenderer.sprite = null;
        currentComposition = null;
        artVisible = false;
        return doll.ResetMountedRiderOffset();
    }

    public bool Matches(WorldRidingState value)
    {
        return value != null &&
               state.Riding == value.Riding &&
               state.MountModel == value.MountModel &&
               state.RiderSeat == value.RiderSeat &&
               state.RideAnchorId == value.RideAnchorId &&
               state.MountTianmo == value.MountTianmo;
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
        ReferenceMountLocalComposition composition,
        Color32 riderTint)
    {
        if (composition == null || bodyRenderer == null || topRenderer == null ||
            effectRenderer == null || composition.BodySprite == null)
        {
            return false;
        }

        bodyRenderer.enabled = true;
        bodyRenderer.sprite = composition.BodySprite;
        bodyRenderer.flipX = composition.FlipX;
        bodyRenderer.transform.localPosition = Vector3.zero;
        bodyRenderer.sortingOrder = composition.BodySortingOrder;
        bodyRenderer.color = new Color32(255, 255, 255, 255);
        bodyRenderer.sharedMaterial = doll.MainRenderer.sharedMaterial;

        topRenderer.enabled = composition.HasTopSprite;
        topRenderer.sprite = composition.TopSprite;
        topRenderer.flipX = composition.FlipX;
        topRenderer.transform.localPosition = new Vector3(
            composition.TopLocalPosition.x,
            composition.TopLocalPosition.y,
            0f);
        topRenderer.sortingOrder = composition.TopSortingOrder;
        topRenderer.color = new Color32(255, 255, 255, 255);
        topRenderer.sharedMaterial = doll.MainRenderer.sharedMaterial;

        effectRenderer.enabled = false;
        effectRenderer.sprite = null;
        effectRenderer.flipX = composition.FlipX;
        effectRenderer.transform.localPosition = Vector3.zero;
        effectRenderer.sortingOrder = composition.EffectSortingOrder;
        effectRenderer.color = new Color32(255, 255, 255, 255);

        if (!doll.ApplyMountedRider(composition, riderTint))
        {
            return false;
        }

        currentComposition = composition;
        artVisible = true;
        return true;
    }

    private void ReleaseHierarchy()
    {
        DestroyOwned(rootObject);
        rootObject = null;
        bodyRenderer = null;
        topRenderer = null;
        effectRenderer = null;
        currentComposition = null;
        artVisible = false;
        built = false;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldLocalMountRider));
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
