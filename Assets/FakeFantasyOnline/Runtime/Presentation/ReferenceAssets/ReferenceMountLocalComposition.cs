using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One immutable local-player mount composition selected by the recovered ComposeMount rules.
/// </summary>
internal sealed class ReferenceMountLocalComposition
{
    public ReferenceMountLocalComposition(
        ReferenceMountActionKind actionKind,
        int actionId,
        float intervalSeconds,
        int direction,
        int frameIndex,
        bool flipX,
        Sprite bodySprite,
        Sprite topSprite,
        Vector2 topLocalPosition,
        Vector2 saddlePixels,
        Vector2 riderLocalPosition,
        int bodySortingOrder,
        int riderSortingBase,
        int topSortingOrder,
        int effectSortingOrder,
        int mountBoundTopPixels,
        int belowAnchorPixels)
    {
        ActionKind = actionKind;
        ActionId = actionId;
        IntervalSeconds = intervalSeconds;
        Direction = direction;
        FrameIndex = frameIndex;
        FlipX = flipX;
        BodySprite = bodySprite ?? throw new ArgumentNullException(nameof(bodySprite));
        TopSprite = topSprite;
        TopLocalPosition = topLocalPosition;
        SaddlePixels = saddlePixels;
        RiderLocalPosition = riderLocalPosition;
        BodySortingOrder = bodySortingOrder;
        RiderSortingBase = riderSortingBase;
        TopSortingOrder = topSortingOrder;
        EffectSortingOrder = effectSortingOrder;
        MountBoundTopPixels = mountBoundTopPixels;
        BelowAnchorPixels = belowAnchorPixels;
    }

    public ReferenceMountActionKind ActionKind { get; }
    public int ActionId { get; }
    public float IntervalSeconds { get; }
    public int Direction { get; }
    public int FrameIndex { get; }
    public bool FlipX { get; }
    public Sprite BodySprite { get; }
    public Sprite TopSprite { get; }
    public bool HasTopSprite => TopSprite != null;
    public bool HasEffectSprite => false;
    public bool TopUsesAdditiveMaterial => false;
    public Vector2 BodyLocalPosition => Vector2.zero;
    public Vector2 TopLocalPosition { get; }
    public Vector2 SaddlePixels { get; }
    public Vector2 RiderLocalPosition { get; }
    public int BodySortingOrder { get; }
    public int RiderSortingBase { get; }
    public int TopSortingOrder { get; }
    public int EffectSortingOrder { get; }
    public int MountBoundTopPixels { get; }
    public int BelowAnchorPixels { get; }
}

/// <summary>
/// Source-owned local-player translation of the normal, non-anchor ComposeMount branch.
/// </summary>
/// <remarks>
/// The official method owns cached body/top Sprite grids, a shared stand/walk frame counter,
/// one-step interval advancement, five-direction folding, SpriteRenderer flip/order, overlay
/// offsets and the saddle used by the later rider-pose tail. This bounded owner prepares the same
/// information for reviewed model 4340 without touching global character state. Mount state,
/// rider Doll application, ring effects, remote players and product wiring remain adjacent
/// boundaries.
/// </remarks>
internal sealed class ReferenceMountLocalComposer : IDisposable
{
    public const int SortBoostCapPixels = 64;
    public const int TopSortingOffset = 30;
    public const int EffectSortingOffset = 31;

    private readonly float pixelsPerUnit;
    private readonly PreparedAction stand;
    private readonly PreparedAction walk;
    private readonly int belowAnchorPixels;
    private readonly int mountBoundTopPixels;
    private int frameCounter;
    private float timerSeconds;
    private bool disposed;

    private ReferenceMountLocalComposer(
        float pixelsPerUnit,
        PreparedAction stand,
        PreparedAction walk,
        int belowAnchorPixels,
        int mountBoundTopPixels)
    {
        this.pixelsPerUnit = pixelsPerUnit;
        this.stand = stand ?? throw new ArgumentNullException(nameof(stand));
        this.walk = walk ?? throw new ArgumentNullException(nameof(walk));
        this.belowAnchorPixels = belowAnchorPixels;
        this.mountBoundTopPixels = mountBoundTopPixels;
    }

    public int FrameCounter => frameCounter;
    public float TimerSeconds => timerSeconds;
    public float PixelsPerUnit => pixelsPerUnit;

    public static bool TryCreate(
        ReferenceMountAvatarModel model,
        float pixelsPerUnit,
        out ReferenceMountLocalComposer composer)
    {
        composer = null;
        if (model == null || model.ModelId < 1 ||
            pixelsPerUnit <= 0f || float.IsNaN(pixelsPerUnit) ||
            float.IsInfinity(pixelsPerUnit))
        {
            return false;
        }

        PreparedAction stand = null;
        PreparedAction walk = null;
        try
        {
            if (!TryPrepareAction(model.Stand, pixelsPerUnit, out stand) ||
                !TryPrepareAction(model.Walk, pixelsPerUnit, out walk))
            {
                stand?.Dispose();
                walk?.Dispose();
                return false;
            }

            ReferenceMountAvatarFrame firstStand;
            if (!model.Stand.TryFrame(0, 0, out firstStand) || firstStand == null)
            {
                stand.Dispose();
                walk.Dispose();
                return false;
            }

            composer = new ReferenceMountLocalComposer(
                pixelsPerUnit,
                stand,
                walk,
                stand.BelowAnchorPixels,
                firstStand.Body.BoundCenterY);
            return true;
        }
        catch
        {
            stand?.Dispose();
            walk?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Advances the shared official mount clock, then composes the selected stand/walk frame.
    /// </summary>
    public bool TryAdvance(
        bool moving,
        int octant,
        float mapY,
        float deltaTime,
        out ReferenceMountLocalComposition composition)
    {
        composition = null;
        if (disposed || deltaTime < 0f || float.IsNaN(deltaTime) ||
            float.IsInfinity(deltaTime))
        {
            return false;
        }

        PreparedAction action = moving ? walk : stand;
        float nextTimer = timerSeconds + deltaTime;
        if (float.IsNaN(nextTimer) || float.IsInfinity(nextTimer))
        {
            return false;
        }

        if (action.IntervalSeconds <= nextTimer)
        {
            timerSeconds = 0f;
            frameCounter = frameCounter == int.MaxValue ? 0 : frameCounter + 1;
        }
        else
        {
            timerSeconds = nextTimer;
        }

        int frameIndex = frameCounter % action.FramesPerDirection;
        return TryCompose(action, octant, frameIndex, mapY, out composition);
    }

    /// <summary>
    /// Selects a reviewed frame without mutating the shared animation clock.
    /// </summary>
    public bool TryComposeAt(
        ReferenceMountActionKind kind,
        int octant,
        int frameIndex,
        float mapY,
        out ReferenceMountLocalComposition composition)
    {
        composition = null;
        if (disposed ||
            (kind != ReferenceMountActionKind.Stand &&
             kind != ReferenceMountActionKind.Walk))
        {
            return false;
        }

        PreparedAction action = kind == ReferenceMountActionKind.Walk ? walk : stand;
        return TryCompose(action, octant, frameIndex, mapY, out composition);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        stand.Dispose();
        walk.Dispose();
    }

    private bool TryCompose(
        PreparedAction action,
        int octant,
        int frameIndex,
        float mapY,
        out ReferenceMountLocalComposition composition)
    {
        composition = null;
        if (action == null || frameIndex < 0 ||
            frameIndex >= action.FramesPerDirection)
        {
            return false;
        }

        ReferenceWorldPlayerFacing facing =
            ReferenceWorldPlayerMotionMath.FromOctant(octant);
        int direction = Math.Max(
            0,
            Math.Min(action.DirectionCount - 1, facing.StoredDirection));
        PreparedFrame frame = action.Frames[direction, frameIndex];
        if (frame == null || frame.BodySprite == null)
        {
            return false;
        }

        float signedTopX = facing.FlipX
            ? -frame.TopOffsetPixels.x
            : frame.TopOffsetPixels.x;
        Vector2 topLocalPosition = new Vector2(
            signedTopX / pixelsPerUnit,
            -frame.TopOffsetPixels.y / pixelsPerUnit);

        float saddleX = facing.FlipX
            ? -action.AverageSaddleX[direction]
            : action.AverageSaddleX[direction];
        Vector2 saddlePixels = new Vector2(saddleX, frame.SaddlePixels.y);
        Vector2 riderLocalPosition = new Vector2(
            saddlePixels.x / pixelsPerUnit,
            -saddlePixels.y / pixelsPerUnit);

        int groupBase = ReferenceWorldLocalPlayerDoll.GroupBaseForMapY(mapY, false);
        int bodySortingOrder = groupBase +
            Math.Min(SortBoostCapPixels, belowAnchorPixels);

        composition = new ReferenceMountLocalComposition(
            action.Source.Kind,
            action.Source.ActionId,
            action.IntervalSeconds,
            direction,
            frameIndex,
            facing.FlipX,
            frame.BodySprite,
            frame.TopSprite,
            topLocalPosition,
            saddlePixels,
            riderLocalPosition,
            bodySortingOrder,
            bodySortingOrder,
            bodySortingOrder + TopSortingOffset,
            bodySortingOrder + EffectSortingOffset,
            mountBoundTopPixels,
            belowAnchorPixels);
        return true;
    }

    private static bool TryPrepareAction(
        ReferenceMountAvatarAction source,
        float pixelsPerUnit,
        out PreparedAction action)
    {
        action = null;
        if (source == null || source.DirectionCount != 5 ||
            source.FramesPerDirection < 1)
        {
            return false;
        }

        PreparedFrame[,] frames = new PreparedFrame[
            source.DirectionCount,
            source.FramesPerDirection];
        float[] averageSaddleX = new float[source.DirectionCount];
        int belowAnchorPixels = -1;
        try
        {
            for (int direction = 0; direction < source.DirectionCount; direction++)
            {
                float saddleXSum = 0f;
                for (int frameIndex = 0;
                     frameIndex < source.FramesPerDirection;
                     frameIndex++)
                {
                    ReferenceMountAvatarFrame sourceFrame;
                    if (!source.TryFrame(direction, frameIndex, out sourceFrame) ||
                        sourceFrame == null)
                    {
                        DisposeFrames(frames);
                        return false;
                    }

                    int frameBelowAnchor = Math.Max(
                        0,
                        sourceFrame.Body.BoundHeight -
                        sourceFrame.Body.BoundCenterY);
                    if (belowAnchorPixels < 0)
                    {
                        belowAnchorPixels = frameBelowAnchor;
                    }
                    else if (belowAnchorPixels != frameBelowAnchor)
                    {
                        DisposeFrames(frames);
                        return false;
                    }

                    PreparedFrame prepared;
                    if (!PreparedFrame.TryCreate(
                            sourceFrame,
                            pixelsPerUnit,
                            out prepared))
                    {
                        DisposeFrames(frames);
                        return false;
                    }

                    frames[direction, frameIndex] = prepared;
                    saddleXSum += prepared.SaddlePixels.x;
                }

                averageSaddleX[direction] =
                    saddleXSum / source.FramesPerDirection;
            }

            action = new PreparedAction(
                source,
                source.IntervalMilliseconds / 2000f,
                belowAnchorPixels,
                frames,
                averageSaddleX);
            return true;
        }
        catch
        {
            DisposeFrames(frames);
            throw;
        }
    }

    private static void DisposeFrames(PreparedFrame[,] frames)
    {
        if (frames == null)
        {
            return;
        }

        for (int direction = 0; direction < frames.GetLength(0); direction++)
        {
            for (int frameIndex = 0;
                 frameIndex < frames.GetLength(1);
                 frameIndex++)
            {
                frames[direction, frameIndex]?.Dispose();
                frames[direction, frameIndex] = null;
            }
        }
    }

    private sealed class PreparedAction : IDisposable
    {
        public PreparedAction(
            ReferenceMountAvatarAction source,
            float intervalSeconds,
            int belowAnchorPixels,
            PreparedFrame[,] frames,
            float[] averageSaddleX)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            IntervalSeconds = intervalSeconds;
            BelowAnchorPixels = belowAnchorPixels;
            Frames = frames ?? throw new ArgumentNullException(nameof(frames));
            AverageSaddleX = averageSaddleX ??
                throw new ArgumentNullException(nameof(averageSaddleX));
        }

        public ReferenceMountAvatarAction Source { get; }
        public float IntervalSeconds { get; }
        public int BelowAnchorPixels { get; }
        public PreparedFrame[,] Frames { get; }
        public float[] AverageSaddleX { get; }
        public int DirectionCount => Frames.GetLength(0);
        public int FramesPerDirection => Frames.GetLength(1);

        public void Dispose()
        {
            DisposeFrames(Frames);
        }
    }

    private sealed class PreparedFrame : IDisposable
    {
        private PreparedFrame(
            ReferenceMountAvatarFrame source,
            Texture2D bodyTexture,
            Sprite bodySprite,
            Texture2D topTexture,
            Sprite topSprite,
            Vector2 topOffsetPixels,
            Vector2 saddlePixels)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            BodyTexture = bodyTexture ?? throw new ArgumentNullException(nameof(bodyTexture));
            BodySprite = bodySprite ?? throw new ArgumentNullException(nameof(bodySprite));
            TopTexture = topTexture;
            TopSprite = topSprite;
            TopOffsetPixels = topOffsetPixels;
            SaddlePixels = saddlePixels;
        }

        public ReferenceMountAvatarFrame Source { get; }
        public Texture2D BodyTexture { get; }
        public Sprite BodySprite { get; }
        public Texture2D TopTexture { get; }
        public Sprite TopSprite { get; }
        public Vector2 TopOffsetPixels { get; }
        public Vector2 SaddlePixels { get; }

        public static bool TryCreate(
            ReferenceMountAvatarFrame source,
            float pixelsPerUnit,
            out PreparedFrame frame)
        {
            frame = null;
            if (source == null || !source.Body.HasPixels)
            {
                return false;
            }

            Texture2D bodyTexture = null;
            Sprite bodySprite = null;
            Texture2D topTexture = null;
            Sprite topSprite = null;
            try
            {
                int bodyAnchorX =
                    source.Body.BoundCenterX - source.Body.Left;
                int bodyAnchorY =
                    source.Body.BoundCenterY - source.Body.Top;
                if (!TryBuildSprite(
                        source.Body,
                        bodyAnchorX,
                        bodyAnchorY,
                        pixelsPerUnit,
                        "RecoveredMountBody",
                        out bodyTexture,
                        out bodySprite))
                {
                    return false;
                }

                Vector2 topOffsetPixels = Vector2.zero;
                if (source.Top != null && source.Top.HasPixels)
                {
                    if (!TryBuildSprite(
                            source.Top,
                            0,
                            0,
                            pixelsPerUnit,
                            "RecoveredMountTop",
                            out topTexture,
                            out topSprite))
                    {
                        DestroyOwned(bodySprite);
                        DestroyOwned(bodyTexture);
                        return false;
                    }

                    topOffsetPixels = new Vector2(
                        source.Top.Left + source.Body.Left -
                            source.Body.BoundCenterX,
                        source.Top.Top + source.Body.Top -
                            source.Body.BoundCenterY);
                }

                Vector2 saddlePixels = new Vector2(
                    source.Body.Left + source.Saddle.X -
                        source.Body.BoundCenterX,
                    source.Body.Top + source.Saddle.Y -
                        source.Body.BoundCenterY);
                frame = new PreparedFrame(
                    source,
                    bodyTexture,
                    bodySprite,
                    topTexture,
                    topSprite,
                    topOffsetPixels,
                    saddlePixels);
                return true;
            }
            catch
            {
                DestroyOwned(topSprite);
                DestroyOwned(topTexture);
                DestroyOwned(bodySprite);
                DestroyOwned(bodyTexture);
                throw;
            }
        }

        public void Dispose()
        {
            DestroyOwned(TopSprite);
            DestroyOwned(TopTexture);
            DestroyOwned(BodySprite);
            DestroyOwned(BodyTexture);
        }

        private static bool TryBuildSprite(
            DimgFrame source,
            int anchorX,
            int anchorY,
            float pixelsPerUnit,
            string objectName,
            out Texture2D texture,
            out Sprite sprite)
        {
            texture = null;
            sprite = null;
            if (source == null || !source.HasPixels)
            {
                return false;
            }

            Color32[] bottomUp = new Color32[source.Pixels.Length];
            for (int sourceY = 0; sourceY < source.Height; sourceY++)
            {
                Array.Copy(
                    source.Pixels,
                    sourceY * source.Width,
                    bottomUp,
                    (source.Height - 1 - sourceY) * source.Width,
                    source.Width);
            }

            texture = new Texture2D(
                source.Width,
                source.Height,
                TextureFormat.RGBA32,
                false,
                false);
            texture.name = objectName + "Texture";
            texture.SetPixels32(bottomUp);
            texture.Apply(false, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            Vector2 pivot = new Vector2(
                (float)anchorX / source.Width,
                (float)(source.Height - anchorY) / source.Height);
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, source.Width, source.Height),
                pivot,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            if (sprite == null)
            {
                DestroyOwned(texture);
                texture = null;
                return false;
            }

            sprite.name = objectName;
            return true;
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
