using System;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// The ordinary stand/walk actions selected for a mount model.
/// </summary>
internal enum ReferenceMountActionKind
{
    Stand = 0,
    Walk = 1
}

/// <summary>
/// One native five-direction mount frame before PlayerController direction folding and layering.
/// </summary>
internal sealed class ReferenceMountAvatarFrame
{
    public ReferenceMountAvatarFrame(DimgFrame body, DimgFrame top)
    {
        Body = body ?? throw new ArgumentNullException(nameof(body));
        Top = top;
        if (!body.HasPixels || body.Bindings.Length != 1)
        {
            throw new ArgumentException(
                "A reviewed mount body frame requires pixels and one saddle binding.",
                nameof(body));
        }

        Saddle = body.Bindings[0];
    }

    public DimgFrame Body { get; }
    public DimgFrame Top { get; }
    public DimgBinding Saddle { get; }
    public bool HasTopPixels => Top != null && Top.HasPixels;
}

/// <summary>
/// Source-owned body/top DIMG pair for one MountAvatar action.
/// </summary>
internal sealed class ReferenceMountAvatarAction
{
    private readonly byte[] body;
    private readonly byte[] top;

    public ReferenceMountAvatarAction(
        ReferenceMountActionKind kind,
        int actionId,
        int intervalMilliseconds,
        int directionCount,
        int framesPerDirection,
        byte[] body,
        byte[] top)
    {
        Kind = kind;
        ActionId = actionId;
        IntervalMilliseconds = intervalMilliseconds;
        DirectionCount = directionCount;
        FramesPerDirection = framesPerDirection;
        this.body = body ?? throw new ArgumentNullException(nameof(body));
        this.top = top;
    }

    public ReferenceMountActionKind Kind { get; }
    public int ActionId { get; }
    public int IntervalMilliseconds { get; }
    public int DirectionCount { get; }
    public int FramesPerDirection { get; }
    public int FrameCount => checked(DirectionCount * FramesPerDirection);
    public bool HasTopSource => top != null;

    public bool TryFrame(
        int nativeDirection,
        int frameIndex,
        out ReferenceMountAvatarFrame frame)
    {
        frame = null;
        if (nativeDirection < 0 || nativeDirection >= DirectionCount ||
            frameIndex < 0 || frameIndex >= FramesPerDirection)
        {
            return false;
        }

        int sourceIndex = checked(nativeDirection * FramesPerDirection + frameIndex);
        DimgFrame bodyFrame;
        DimgFrame topFrame = null;
        if (!DimgDecoder.TryDecodeFrame(body, sourceIndex, out bodyFrame) ||
            (top != null && !DimgDecoder.TryDecodeFrame(top, sourceIndex, out topFrame)) ||
            bodyFrame == null ||
            !bodyFrame.HasPixels || bodyFrame.Bindings.Length != 1 ||
            (topFrame != null && topFrame.Bindings.Length != 0))
        {
            return false;
        }

        frame = new ReferenceMountAvatarFrame(bodyFrame, topFrame);
        return true;
    }
}

/// <summary>
/// Ordinary single-seat MountAvatar source model recovered from the locked 1.3.4 client.
/// </summary>
/// <remarks>
/// Model 4340 remains an exact audit sample; product loading is driven by the authoritative model
/// id and NpcActions rows.
/// The action ids/names/intervals come from the official Q21 npc action text, while the body/top
/// files are source-owned package extractions. The official M4340 block has no ride override, so
/// the later rider-pose boundary must retain PlayerController's ordinary-stand fallback.
/// Direction folding, SpriteRenderer composition, timers, server-state wiring and mounted rider
/// application remain adjacent modules.
/// </remarks>
internal sealed class ReferenceMountAvatarModel
{
    // Retained for the accepted model-4340 audits. Product loading is model-driven.
    public const int Model4340Id = 4340;
    public const int Model203Id = 203;
    public const int NativeDirectionCount = 5;

    private ReferenceMountAvatarModel(
        int modelId,
        ReferenceMountAvatarAction stand,
        ReferenceMountAvatarAction walk)
    {
        if (modelId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(modelId));
        }

        ModelId = modelId;
        Stand = stand ?? throw new ArgumentNullException(nameof(stand));
        Walk = walk ?? throw new ArgumentNullException(nameof(walk));
    }

    public int ModelId { get; }
    public int SeatCount => 1;
    public bool HasTopLayer => Stand.HasTopSource || Walk.HasTopSource;
    public bool HasEffectLayer => false;
    public bool UsesDefaultStandRiderPose => true;
    public ReferenceMountAvatarAction Stand { get; }
    public ReferenceMountAvatarAction Walk { get; }

    public ReferenceMountAvatarAction Action(ReferenceMountActionKind kind)
    {
        return kind == ReferenceMountActionKind.Walk ? Walk : Stand;
    }

    public static bool TryLoad4340(
        ReferenceResourceLoader resources,
        out ReferenceMountAvatarModel model)
    {
        return TryLoad(resources, Model4340Id, out model);
    }

    /// <summary>
    /// Loads the official ordinary single-seat mount selected by its action-table rows.
    /// </summary>
    public static bool TryLoad(
        ReferenceResourceLoader resources,
        int modelId,
        out ReferenceMountAvatarModel model)
    {
        model = null;
        if (resources == null || modelId < 1 ||
            !string.IsNullOrEmpty(NpcActions.RideKey(modelId)))
        {
            return false;
        }

        int standActionId = NpcActions.Find(modelId, "Stand");
        int walkActionId = NpcActions.Find(modelId, "Walk");
        int standInterval = NpcActions.Interval(modelId, "Stand");
        int walkInterval = NpcActions.Interval(modelId, "Walk");
        if (standActionId < 1 || walkActionId < 1 ||
            standInterval < 1 || walkInterval < 1)
        {
            return false;
        }

        ReferenceMountAvatarAction stand;
        ReferenceMountAvatarAction walk;
        if (!TryCreateAction(
                resources,
                ReferenceMountActionKind.Stand,
                standActionId,
                standInterval,
                out stand) ||
            !TryCreateAction(
                resources,
                ReferenceMountActionKind.Walk,
                walkActionId,
                walkInterval,
                out walk))
        {
            return false;
        }

        model = new ReferenceMountAvatarModel(modelId, stand, walk);
        return true;
    }

    private static bool TryCreateAction(
        ReferenceResourceLoader resources,
        ReferenceMountActionKind kind,
        int actionId,
        int intervalMilliseconds,
        out ReferenceMountAvatarAction action)
    {
        action = null;
        string basePath = string.Format("avatar/npc/npc{0}", actionId);
        byte[] body = resources.ReadRes(basePath + ".gsn");
        byte[] top = resources.ReadRes(basePath + "t.gsn");
        byte[] effect = resources.ReadRes(basePath + "e.gsn");
        int framesPerDirection = DimgDecoder.FramesPerDir(body);
        int expectedFrameCount = checked(NativeDirectionCount * framesPerDirection);
        if (DimgDecoder.FrameCount(top) < 1)
        {
            top = null;
        }

        // Effect-bearing and other special mount branches remain fail closed.
        if (DimgDecoder.FrameCount(effect) > 0 ||
            DimgDecoder.Version(body) != 65540 ||
            DimgDecoder.DirectionCount(body) != NativeDirectionCount ||
            framesPerDirection < 1 ||
            DimgDecoder.FrameCount(body) != expectedFrameCount ||
            (top != null &&
             (DimgDecoder.Version(top) != 65540 ||
              DimgDecoder.DirectionCount(top) != NativeDirectionCount ||
              DimgDecoder.FramesPerDir(top) != framesPerDirection ||
              DimgDecoder.FrameCount(top) != expectedFrameCount)))
        {
            return false;
        }

        for (int index = 0; index < expectedFrameCount; index++)
        {
            DimgFrame bodyFrame;
            DimgFrame topFrame = null;
            if (!DimgDecoder.TryDecodeFrame(body, index, out bodyFrame) ||
                (top != null && !DimgDecoder.TryDecodeFrame(top, index, out topFrame)) ||
                bodyFrame == null ||
                !bodyFrame.HasPixels || bodyFrame.Bindings.Length != 1 ||
                bodyFrame.Flags != 0 ||
                (topFrame != null &&
                 (topFrame.Bindings.Length != 0 || topFrame.Flags != 256)))
            {
                return false;
            }

            if (topFrame != null && !topFrame.HasPixels &&
                (topFrame.Width != 0 || topFrame.Height != 0 ||
                 topFrame.Pixels.Length != 0))
            {
                return false;
            }
        }

        action = new ReferenceMountAvatarAction(
            kind,
            actionId,
            intervalMilliseconds,
            NativeDirectionCount,
            framesPerDirection,
            body,
            top);
        return true;
    }
}
}
