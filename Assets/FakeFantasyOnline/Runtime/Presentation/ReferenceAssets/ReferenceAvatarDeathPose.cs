using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-layer poses produced by PlayerAvatar.PoseFrames(12) for the die action.
/// </summary>
internal sealed class ReferenceAvatarDeathSourcePoses
{
    public ReferenceAvatarDeathSourcePoses(
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
/// Source-layer poses produced by PlayerAvatar.PoseFrames(11) for the hurt action.
/// </summary>
internal sealed class ReferenceAvatarHurtSourcePoses
{
    public ReferenceAvatarHurtSourcePoses(
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
/// Bounded source translation of PlayerAvatar.PoseFrames(12) for the reviewed death pose cone.
/// </summary>
/// <remarks>
/// The locked method reads body action 13 from the requested body and does not take its
/// action-index-3-only fallback branch. It passes PartsFor(0) and action 1 to FramesOf, so head and
/// equipment keep their stand inputs; reading action 13 for every part would also diverge.
/// </remarks>
internal static class ReferenceAvatarDeathPoseAssembler
{
    private const int DirectionCount = 5;
    private const int DeathFileAction = 13;
    private const int StandFileAction = 1;
    private const int HurtFileAction = 12;
    private const int HurtPoseIndex = 11;
    private const int DeathPoseIndex = 12;
    private const int RemoteDeathFileAction = 4;
    private const int RemoteDeathPoseIndex = 3;

    public static bool TryAssemble(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        out ReferenceAvatarDeathSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded)
        {
            return false;
        }

        byte[] body = resources.ReadRes(assets.BodyPathForAction(DeathFileAction));
        return TryAssemble(
            resources,
            avatarDepth,
            assets,
            body,
            DeathPoseIndex,
            out poses);
    }

    /// <summary>
    /// Translates PlayerController.PlayHurt's PoseFrames(11) source cone.
    /// </summary>
    public static bool TryAssembleHurt(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        out ReferenceAvatarHurtSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded)
        {
            return false;
        }

        byte[] body = resources.ReadRes(assets.BodyPathForAction(HurtFileAction));
        ReferenceAvatarDeathSourcePoses common;
        if (!TryAssemble(
                resources,
                avatarDepth,
                assets,
                body,
                HurtPoseIndex,
                out common))
        {
            return false;
        }

        poses = new ReferenceAvatarHurtSourcePoses(
            common.Frames,
            common.FramesPerDirection);
        return true;
    }

    /// <summary>
    /// Translates the distinct RemotePlayer.PlayDie PoseFrames(3) source cone.
    /// </summary>
    /// <remarks>
    /// Unlike local-player death, action index three reads body action 04 and is the sole
    /// PoseFrames branch allowed to fall back to the gender-default body. It still composes the
    /// head and equipment from PartsFor(0), and ActionPose(4) selects the ride depth column.
    /// </remarks>
    public static bool TryAssembleRemote(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        out ReferenceAvatarDeathSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded)
        {
            return false;
        }

        byte[] body;
        if (!TryReadFirst(
                resources,
                assets.BodyCandidatePaths(RemoteDeathFileAction),
                out body))
        {
            return false;
        }

        return TryAssemble(
            resources,
            avatarDepth,
            assets,
            body,
            RemoteDeathPoseIndex,
            out poses);
    }

    private static bool TryAssemble(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        byte[] body,
        int poseIndex,
        out ReferenceAvatarDeathSourcePoses poses)
    {
        poses = null;
        if (DimgDecoder.FrameCount(body) < 1)
        {
            return false;
        }

        int framesPerDirection = DimgDecoder.FramesPerDir(body);
        byte[] head = resources.ReadRes(assets.HeadPath);
        if (framesPerDirection < 1 ||
            DimgDecoder.FrameCount(body) < DirectionCount * framesPerDirection ||
            DimgDecoder.FrameCount(head) < DirectionCount)
        {
            return false;
        }

        List<PartData> parts = new List<PartData>(assets.Parts.Length);
        foreach (ReferenceAvatarPart part in assets.Parts)
        {
            byte[] standPart;
            if (!TryReadFirst(
                    resources,
                    part.CandidatePathsForAction(StandFileAction),
                    out standPart))
            {
                return false;
            }

            parts.Add(new PartData(part.Tag, standPart));
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
                        poseIndex,
                        out frames[direction, frame]))
                {
                    return false;
                }
            }
        }

        poses = new ReferenceAvatarDeathSourcePoses(frames, framesPerDirection);
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
        int framesPerDirection,
        int poseIndex,
        out ReferenceAvatarStandSourcePose pose)
    {
        pose = null;
        DimgFrame body;
        DimgFrame head;
        if (!DimgDecoder.TryDecodeFrame(
                bodyBytes,
                direction * framesPerDirection + frame,
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
                    poseIndex,
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

#if UNITY_EDITOR
// Historical synthetic-time diagnostics only. Product death playback uses PlayerController state.
/// <summary>
/// One dead-branch frame decision from PlayerController.UpdateAnim.
/// </summary>
public struct ReferenceWorldPlayerDeathAnimationPlan
{
    public ReferenceWorldPlayerDeathAnimationPlan(
        float elapsedSeconds,
        int direction,
        int frame)
    {
        ElapsedSeconds = elapsedSeconds;
        Direction = direction;
        Frame = frame;
    }

    public float ElapsedSeconds { get; }
    public int Direction { get; }
    public int Frame { get; }
}

/// <summary>
/// Source-owned dead-branch timing subset of PlayerController.UpdateAnim.
/// </summary>
public static class ReferenceWorldPlayerDeathAnimationMath
{
    public const int DirectionCount = 5;
    public const float SecondsPerFrame = 0.09f;

    public static ReferenceWorldPlayerDeathAnimationPlan Advance(
        float elapsedSeconds,
        float deltaTime,
        int direction,
        int framesPerDirection)
    {
        ValidateFiniteNonnegative(elapsedSeconds, nameof(elapsedSeconds));
        ValidateFiniteNonnegative(deltaTime, nameof(deltaTime));
        if (framesPerDirection < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerDirection));
        }

        float nextElapsed = elapsedSeconds + deltaTime;
        if (float.IsInfinity(nextElapsed))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        int selectedDirection = Math.Max(0, Math.Min(DirectionCount - 1, direction));
        int selectedFrame = Math.Max(
            0,
            Math.Min(framesPerDirection - 1, (int)(nextElapsed / SecondsPerFrame)));
        return new ReferenceWorldPlayerDeathAnimationPlan(
            nextElapsed,
            selectedDirection,
            selectedFrame);
    }

    private static void ValidateFiniteNonnegative(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

#endif

/// <summary>
/// One frame decision from the distinct RemotePlayer.StepDie clock.
/// </summary>
public struct ReferenceWorldRemoteDeathAnimationPlan
{
    public ReferenceWorldRemoteDeathAnimationPlan(
        float elapsedSeconds,
        int direction,
        int frame)
    {
        ElapsedSeconds = elapsedSeconds;
        Direction = direction;
        Frame = frame;
    }

    public float ElapsedSeconds { get; }
    public int Direction { get; }
    public int Frame { get; }
}

/// <summary>
/// Source-owned RemotePlayer.StepDie timing, including its terminal clock hold.
/// </summary>
public static class ReferenceWorldRemoteDeathAnimationMath
{
    public const int DirectionCount = 5;
    public const float SecondsPerFrame = 0.09f;

    public static ReferenceWorldRemoteDeathAnimationPlan Advance(
        float elapsedSeconds,
        float deltaTime,
        int direction,
        int framesPerDirection)
    {
        ValidateFiniteNonnegative(elapsedSeconds, nameof(elapsedSeconds));
        ValidateFiniteNonnegative(deltaTime, nameof(deltaTime));
        if (framesPerDirection < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(framesPerDirection));
        }

        int lastFrame = Math.Max(1, framesPerDirection) - 1;
        float nextElapsed = elapsedSeconds;
        if (nextElapsed < lastFrame * SecondsPerFrame)
        {
            nextElapsed += deltaTime;
            if (float.IsInfinity(nextElapsed))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            }
        }

        int selectedDirection = Math.Max(0, Math.Min(DirectionCount - 1, direction));
        int selectedFrame = Math.Max(
            0,
            Math.Min(lastFrame, (int)(nextElapsed / SecondsPerFrame)));
        return new ReferenceWorldRemoteDeathAnimationPlan(
            nextElapsed,
            selectedDirection,
            selectedFrame);
    }

    private static void ValidateFiniteNonnegative(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

}
