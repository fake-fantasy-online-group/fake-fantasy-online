using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Five source-layer sitting poses produced by PlayerAvatar.Build.
/// </summary>
internal sealed class ReferenceAvatarSittingSourcePoses
{
    public ReferenceAvatarSittingSourcePoses(ReferenceAvatarStandSourcePose[] poses)
    {
        Poses = poses ?? throw new ArgumentNullException(nameof(poses));
    }

    public ReferenceAvatarStandSourcePose[] Poses { get; }
}

/// <summary>
/// Bounded source translation of the PlayerAvatar.Build sitting-pose cone.
/// </summary>
/// <remarks>
/// The locked Build method reads requested-body action 03 with the gender-default body fallback,
/// asks FramesOf for one pose per direction with bodyStride equal to the body frames-per-direction,
/// and selects PartsFor(0) action index 2. LoadPart therefore tries action 03 for each equipment
/// part and falls back to its action-01 stand art when the action-specific file is absent.
/// </remarks>
internal static class ReferenceAvatarSittingPoseAssembler
{
    private const int DirectionCount = 5;
    private const int SittingFileAction = 3;
    private const int SittingPoseIndex = 2;

    public static bool TryAssemble(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        out ReferenceAvatarSittingSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded)
        {
            return false;
        }

        byte[] body;
        if (!TryReadFirst(
                resources,
                assets.BodyCandidatePaths(SittingFileAction),
                out body))
        {
            return false;
        }

        int bodyFramesPerDirection = DimgDecoder.FramesPerDir(body);
        byte[] head = resources.ReadRes(assets.HeadPath);
        if (bodyFramesPerDirection < 1 ||
            DimgDecoder.FrameCount(body) < DirectionCount * bodyFramesPerDirection ||
            DimgDecoder.FrameCount(head) < DirectionCount)
        {
            return false;
        }

        List<PartData> parts = new List<PartData>(assets.Parts.Length);
        foreach (ReferenceAvatarPart part in assets.Parts)
        {
            byte[] partBytes;
            if (!TryReadFirst(
                    resources,
                    part.CandidatePathsForAction(SittingFileAction),
                    out partBytes))
            {
                return false;
            }

            parts.Add(new PartData(part.Tag, partBytes));
        }

        ReferenceAvatarStandSourcePose[] sourcePoses =
            new ReferenceAvatarStandSourcePose[DirectionCount];
        for (int direction = 0; direction < DirectionCount; direction++)
        {
            if (!TryAssembleDirection(
                    body,
                    bodyFramesPerDirection,
                    head,
                    parts,
                    avatarDepth,
                    assets,
                    direction,
                    out sourcePoses[direction]))
            {
                return false;
            }
        }

        poses = new ReferenceAvatarSittingSourcePoses(sourcePoses);
        return true;
    }

    private static bool TryAssembleDirection(
        byte[] bodyBytes,
        int bodyFramesPerDirection,
        byte[] headBytes,
        IList<PartData> parts,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        int direction,
        out ReferenceAvatarStandSourcePose pose)
    {
        pose = null;
        DimgFrame body;
        DimgFrame head;
        int bodyFrameIndex = direction * bodyFramesPerDirection;
        if (!DimgDecoder.TryDecodeFrame(bodyBytes, bodyFrameIndex, out body) ||
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
                0,
                0,
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
                    false));
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
                    SittingPoseIndex,
                    artDirection,
                    0,
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
        public PartData(int tag, byte[] bytes)
        {
            Tag = tag;
            Bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
        }

        public int Tag { get; }
        public byte[] Bytes { get; }
    }
}

}
