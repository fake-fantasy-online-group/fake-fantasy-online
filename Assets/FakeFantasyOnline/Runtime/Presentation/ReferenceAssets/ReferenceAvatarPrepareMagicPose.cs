using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Five source poses produced by PlayerAvatar.PoseAt for one held preparation action.
/// </summary>
internal sealed class ReferenceAvatarPrepareSourcePoses
{
    public ReferenceAvatarPrepareSourcePoses(
        ReferenceAvatarStandSourcePose[] poses,
        int actionIndex)
    {
        Poses = poses ?? throw new ArgumentNullException(nameof(poses));
        ActionIndex = actionIndex;
    }

    public ReferenceAvatarStandSourcePose[] Poses { get; }
    public int ActionIndex { get; }
}

/// <summary>
/// Source matrix produced by PlayerAvatar.MagicPose for the prepare1 cast animation.
/// </summary>
internal sealed class ReferenceAvatarMagicSourcePoses
{
    public ReferenceAvatarMagicSourcePoses(
        ReferenceAvatarStandSourcePose[,] frames,
        int framesPerDirection)
    {
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        FramesPerDirection = framesPerDirection;
    }

    public ReferenceAvatarStandSourcePose[,] Frames { get; }
    public int FramesPerDirection { get; }
}

/// <summary>
/// Source translation of PlayerAvatar.PoseAt and MagicPose for data-driven player appearances.
/// </summary>
/// <remarks>
/// PlayChant selects action indices 8, 9, 10, 9 for classes 1..4 and PoseAt materializes only
/// the first body frame in each direction with stand equipment. MagicPose is a separate cached
/// matrix: body action 08, matching action/stand attachment fallbacks, prepare1 depth and
/// advancing attachment frames. Requested body actions use the official gender-body fallback,
/// and additive weapon layers follow the same part candidate order as the ordinary layer.
/// </remarks>
internal static class ReferenceAvatarPrepareMagicPoseAssembler
{
    private const int DirectionCount = 5;
    private const int StandFileAction = 1;
    private const int MagicFileAction = 8;
    private const int MagicPoseIndex = 7;
    private const int FirstPrepareActionIndex = 8;
    private const int LastPrepareActionIndex = 10;

    public static bool TryAssemblePrepare(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        int actionIndex,
        out ReferenceAvatarPrepareSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded ||
            assets.BodyModel < 1 || string.IsNullOrEmpty(assets.HeadPath) ||
            assets.Parts == null ||
            actionIndex < FirstPrepareActionIndex ||
            actionIndex > LastPrepareActionIndex)
        {
            return false;
        }

        int bodyFileAction = actionIndex + 1;
        byte[] body;
        if (!TryReadFirst(
                resources,
                assets.BodyCandidatePaths(bodyFileAction),
                out body))
        {
            return false;
        }
        byte[] head = resources.ReadRes(assets.HeadPath);
        int bodyFramesPerDirection = DimgDecoder.FramesPerDir(body);
        if (bodyFramesPerDirection < 1 ||
            DimgDecoder.FrameCount(body) != DirectionCount * bodyFramesPerDirection ||
            DimgDecoder.FrameCount(head) < DirectionCount)
        {
            return false;
        }

        List<PartData> parts;
        if (!TryReadParts(
                resources,
                assets,
                StandFileAction,
                bodyFileAction,
                out parts))
        {
            return false;
        }

        ReferenceAvatarStandSourcePose[] sourcePoses =
            new ReferenceAvatarStandSourcePose[DirectionCount];
        for (int direction = 0; direction < DirectionCount; direction++)
        {
            if (!TryAssembleFrame(
                    body,
                    head,
                    parts,
                    avatarDepth,
                    assets,
                    direction,
                    0,
                    bodyFramesPerDirection,
                    actionIndex,
                    out sourcePoses[direction]))
            {
                return false;
            }
        }

        poses = new ReferenceAvatarPrepareSourcePoses(sourcePoses, actionIndex);
        return true;
    }

    public static bool TryAssembleMagic(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        out ReferenceAvatarMagicSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded ||
            assets.BodyModel < 1 || string.IsNullOrEmpty(assets.HeadPath) ||
            assets.Parts == null)
        {
            return false;
        }

        byte[] body;
        if (!TryReadFirst(
                resources,
                assets.BodyCandidatePaths(MagicFileAction),
                out body))
        {
            return false;
        }
        byte[] head = resources.ReadRes(assets.HeadPath);
        int framesPerDirection = DimgDecoder.FramesPerDir(body);
        if (framesPerDirection < 1 ||
            DimgDecoder.FrameCount(body) != DirectionCount * framesPerDirection ||
            DimgDecoder.FrameCount(head) < DirectionCount)
        {
            return false;
        }

        List<PartData> parts;
        if (!TryReadParts(
                resources,
                assets,
                MagicFileAction,
                MagicFileAction,
                out parts))
        {
            return false;
        }

        ReferenceAvatarStandSourcePose[,] frames =
            new ReferenceAvatarStandSourcePose[DirectionCount, framesPerDirection];
        for (int direction = 0; direction < DirectionCount; direction++)
        {
            for (int frame = 0; frame < framesPerDirection; frame++)
            {
                if (!TryAssembleFrame(
                        body,
                        head,
                        parts,
                        avatarDepth,
                        assets,
                        direction,
                        frame,
                        framesPerDirection,
                        MagicPoseIndex,
                        out frames[direction, frame]))
                {
                    return false;
                }
            }
        }

        poses = new ReferenceAvatarMagicSourcePoses(frames, framesPerDirection);
        return true;
    }

    private static bool TryReadParts(
        ReferenceResourceLoader resources,
        ReferenceAvatarAssets assets,
        int ordinaryFileAction,
        int glowFileAction,
        out List<PartData> parts)
    {
        parts = new List<PartData>(assets.Parts.Length * 2);
        foreach (ReferenceAvatarPart part in assets.Parts)
        {
            byte[] partBytes;
            if (!TryReadFirst(
                    resources,
                    part.CandidatePathsForAction(ordinaryFileAction),
                    out partBytes))
            {
                return false;
            }

            parts.Add(new PartData(part.Tag, partBytes, false));
            byte[] glowBytes;
            if ((part.Tag == 5 || part.Tag == 6) &&
                TryReadFirst(
                    resources,
                    part.GlowCandidatePathsForAction(glowFileAction),
                    out glowBytes))
            {
                parts.Add(new PartData(part.Tag, glowBytes, true));
            }
        }

        return true;
    }

    private static bool TryAssembleFrame(
        byte[] bodyBytes,
        byte[] headBytes,
        IList<PartData> parts,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        int direction,
        int frame,
        int bodyFramesPerDirection,
        int poseIndex,
        out ReferenceAvatarStandSourcePose pose)
    {
        pose = null;
        DimgFrame body;
        DimgFrame head;
        if (!DimgDecoder.TryDecodeFrame(
                bodyBytes,
                direction * bodyFramesPerDirection + frame,
                out body) ||
            !DimgDecoder.TryDecodeFrame(headBytes, direction, out head) ||
            body.Bindings.Length < 1)
        {
            return false;
        }

        ReferenceAvatarStandAssembler.Recolor(head.Pixels, assets.HairColor);
        int bodyCenterX = body.HasBind ? body.CenterX : body.BoundCenterX - body.Left;
        int bodyCenterY = body.HasBind ? body.CenterY : body.BoundCenterY - body.Top;
        List<ReferenceAvatarStandSourceLayer> layers =
            new List<ReferenceAvatarStandSourceLayer>(2 + parts.Count)
            {
                new ReferenceAvatarStandSourceLayer(
                    0,
                    body,
                    -bodyCenterX,
                    -bodyCenterY,
                    body.CenterX,
                    body.CenterY,
                    false),
                new ReferenceAvatarStandSourceLayer(
                    3,
                    head,
                    (body.Bindings[0].X - head.CenterX) - bodyCenterX,
                    (body.Bindings[0].Y - head.CenterY) - bodyCenterY,
                    head.CenterX,
                    head.CenterY,
                    false)
            };

        foreach (PartData part in parts)
        {
            int partFramesPerDirection = DimgDecoder.FramesPerDir(part.Bytes);
            int partFrameIndex = ReferenceAvatarNormalPoseAssembler.PartFrameIndex(
                part.Tag,
                direction,
                partFramesPerDirection,
                frame,
                frame,
                1,
                true,
                true);
            DimgFrame partFrame;
            int bindingIndex = ReferenceAvatarStandAssembler.BindingIndexForTag(part.Tag);
            if (!DimgDecoder.TryDecodeFrame(part.Bytes, partFrameIndex, out partFrame) ||
                bindingIndex < 0 || bindingIndex >= body.Bindings.Length)
            {
                return false;
            }

            DimgBinding binding = body.Bindings[bindingIndex];
            layers.Add(
                new ReferenceAvatarStandSourceLayer(
                    part.Tag,
                    partFrame,
                    (binding.X - partFrame.CenterX) - bodyCenterX,
                    (binding.Y - partFrame.CenterY) - bodyCenterY,
                    partFrame.CenterX,
                    partFrame.CenterY,
                    part.Additive));
        }

        int screenDirection = direction;
        int artDirection = avatarDepth.ArtDirection(screenDirection);
        layers.Sort(
            delegate(
                ReferenceAvatarStandSourceLayer left,
                ReferenceAvatarStandSourceLayer right)
            {
                return avatarDepth.ComparePlayerLayerTags(
                    left.Tag,
                    right.Tag,
                    poseIndex,
                    artDirection,
                    frame,
                    screenDirection);
            });

        pose = new ReferenceAvatarStandSourcePose(layers, body.BoundWidth);
        return true;
    }

    private static bool TryReadFirst(
        ReferenceResourceLoader resources,
        IEnumerable<string> candidates,
        out byte[] bytes)
    {
        foreach (string candidate in candidates)
        {
            byte[] value = resources.ReadRes(candidate);
            if (DimgDecoder.FrameCount(value) > 0)
            {
                bytes = value;
                return true;
            }
        }

        bytes = null;
        return false;
    }

    private sealed class PartData
    {
        public PartData(int tag, byte[] bytes, bool additive)
        {
            Tag = tag;
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
            Additive = additive;
        }

        public int Tag { get; }
        public byte[] Bytes { get; }
        public bool Additive { get; }
    }
}

}
