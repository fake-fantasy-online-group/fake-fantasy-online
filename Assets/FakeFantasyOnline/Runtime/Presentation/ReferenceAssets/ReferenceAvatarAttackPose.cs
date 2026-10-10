using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-layer matrix produced by PlayerAvatar.AttackPose for one normalized attack variant.
/// </summary>
internal sealed class ReferenceAvatarAttackSourcePoses
{
    public ReferenceAvatarAttackSourcePoses(
        ReferenceAvatarStandSourcePose[,] frames,
        int framesPerDirection,
        int variant)
    {
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        FramesPerDirection = framesPerDirection;
        Variant = variant;
    }

    public ReferenceAvatarStandSourcePose[,] Frames { get; }
    public int FramesPerDirection { get; }
    public int Variant { get; }
}

/// <summary>
/// Source translation of PlayerAvatar.AttackPose for data-driven player appearances.
/// </summary>
/// <remarks>
/// AttackPose clamps its internal variant to 0..2, reads requested-body actions 05..07, asks
/// PartsFor for the matching action with action-01 fallback, and passes attack1..attack3 into
/// FramesOf. Every attachment advances with the body frame; depth also advances with that frame.
/// Missing requested-body actions use the same gender-default body candidate fallback as the
/// official action owner. Ordinary and additive weapon layers share the complete part lookup.
/// </remarks>
internal static class ReferenceAvatarAttackPoseAssembler
{
    private const int DirectionCount = 5;
    private const int FirstAttackFileAction = 5;
    private const int FirstAttackPoseIndex = 4;

    public static int NormalizeVariant(int variant)
    {
        int nonnegative = Math.Max(0, variant);
        return nonnegative > 1 ? 2 : nonnegative;
    }

    public static bool TryAssemble(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        int variant,
        out ReferenceAvatarAttackSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded ||
            assets.BodyModel < 1 || string.IsNullOrEmpty(assets.HeadPath) ||
            assets.Parts == null)
        {
            return false;
        }

        int normalizedVariant = NormalizeVariant(variant);
        int fileAction = FirstAttackFileAction + normalizedVariant;
        int poseIndex = FirstAttackPoseIndex + normalizedVariant;
        byte[] body;
        if (!TryReadFirst(
                resources,
                assets.BodyCandidatePaths(fileAction),
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

        List<PartData> parts = new List<PartData>(assets.Parts.Length * 2);
        foreach (ReferenceAvatarPart part in assets.Parts)
        {
            byte[] partBytes;
            if (!TryReadFirst(
                    resources,
                    part.CandidatePathsForAction(fileAction),
                    out partBytes))
            {
                return false;
            }

            parts.Add(new PartData(part.Tag, partBytes, false));
            byte[] glowBytes;
            if ((part.Tag == 5 || part.Tag == 6) &&
                TryReadFirst(
                    resources,
                    part.GlowCandidatePathsForAction(fileAction),
                    out glowBytes))
            {
                parts.Add(new PartData(part.Tag, glowBytes, true));
            }
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

        poses = new ReferenceAvatarAttackSourcePoses(
            frames,
            framesPerDirection,
            normalizedVariant);
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

/// <summary>
/// Immutable StartActPose/UpdateAnim action state for the reviewed valid-time cone.
/// </summary>
public struct ReferenceWorldPlayerActionAnimationState
{
    public ReferenceWorldPlayerActionAnimationState(
        bool active,
        int variant,
        int framesPerDirection,
        float frameSeconds,
        float elapsedSeconds,
        float durationSeconds,
        bool loop,
        bool attackSwing)
    {
        Active = active;
        Variant = variant;
        FramesPerDirection = framesPerDirection;
        FrameSeconds = frameSeconds;
        ElapsedSeconds = elapsedSeconds;
        DurationSeconds = durationSeconds;
        Loop = loop;
        AttackSwing = attackSwing;
    }

    public bool Active { get; }
    public int Variant { get; }
    public int FramesPerDirection { get; }
    public float FrameSeconds { get; }
    public float ElapsedSeconds { get; }
    public float DurationSeconds { get; }
    public bool Loop { get; }
    public bool AttackSwing { get; }

    public ReferenceWorldPlayerActionAnimationState Cancel()
    {
        return new ReferenceWorldPlayerActionAnimationState(
            false,
            Variant,
            FramesPerDirection,
            FrameSeconds,
            ElapsedSeconds,
            DurationSeconds,
            Loop,
            AttackSwing);
    }
}

/// <summary>
/// One action-branch result from PlayerController.UpdateAnim.
/// </summary>
public struct ReferenceWorldPlayerActionAnimationPlan
{
    public ReferenceWorldPlayerActionAnimationPlan(
        ReferenceWorldPlayerActionAnimationState state,
        bool applyPose,
        bool cancelled,
        int direction,
        int frame)
    {
        State = state;
        ApplyPose = applyPose;
        Cancelled = cancelled;
        Direction = direction;
        Frame = frame;
    }

    public ReferenceWorldPlayerActionAnimationState State { get; }
    public bool ApplyPose { get; }
    public bool Cancelled { get; }
    public int Direction { get; }
    public int Frame { get; }
}

/// <summary>
/// Source-owned PlayAttackPose, PlayChant, PlayCast, StartActPose and action UpdateAnim rules.
/// </summary>
public static class ReferenceWorldPlayerActionAnimationMath
{
    public const int DirectionCount = 5;
    public const float DefaultAttackFrameSeconds = 0.06f;
    public const float MagicFrameSeconds = 0.07f;
    public const float HurtFrameSeconds = 0.1f;
    public const float MinimumChantSeconds = 0.1f;
    public const float AttackSwingTailSeconds = 0.15f;

    private static readonly int[] AttackVariants = { 0, 1, 2, 1 };
    private static readonly int[] ChantPoseIndices = { 8, 9, 10, 9 };

    public static int AttackPoseVariant(int serverAttackVariant)
    {
        int index = serverAttackVariant - 1;
        return index >= 0 && index < AttackVariants.Length
            ? AttackVariants[index]
            : 2;
    }

#if UNITY_EDITOR
    public static ReferenceWorldPlayerActionAnimationState StartAttack(
        int serverAttackVariant,
        int framesPerDirection,
        float swingSeconds)
    {
        ValidateFinite(swingSeconds, nameof(swingSeconds));
        int normalizedFrames = framesPerDirection < 2 ? 1 : framesPerDirection;
        float frameSeconds = swingSeconds <= 0f
            ? DefaultAttackFrameSeconds
            : swingSeconds / normalizedFrames;
        return Start(
            AttackPoseVariant(serverAttackVariant),
            normalizedFrames,
            frameSeconds,
            false,
            swingSeconds > 0f);
    }

#endif
    public static int ChantPoseIndex(int characterClass)
    {
        int index = characterClass - 1;
        return index >= 0 && index < ChantPoseIndices.Length
            ? ChantPoseIndices[index]
            : 10;
    }

#if UNITY_EDITOR
    public static ReferenceWorldPlayerActionAnimationState StartChant(
        ReferenceWorldPlayerActionAnimationState previous,
        float holdSeconds)
    {
        ValidateFinite(holdSeconds, nameof(holdSeconds));
        float frameSeconds = previous.FrameSeconds > 0f &&
            !float.IsNaN(previous.FrameSeconds) &&
            !float.IsInfinity(previous.FrameSeconds)
                ? previous.FrameSeconds
                : DefaultAttackFrameSeconds;
        return new ReferenceWorldPlayerActionAnimationState(
            true,
            0,
            1,
            frameSeconds,
            0f,
            Math.Max(MinimumChantSeconds, holdSeconds),
            previous.Loop,
            false);
    }

    public static ReferenceWorldPlayerActionAnimationState StartMagic(
        int framesPerDirection)
    {
        return Start(0, framesPerDirection, MagicFrameSeconds, false, false);
    }

    public static ReferenceWorldPlayerActionAnimationState StartHurt(
        int framesPerDirection)
    {
        return Start(0, framesPerDirection, HurtFrameSeconds, false, false);
    }

    public static ReferenceWorldPlayerActionAnimationState Start(
        int variant,
        int framesPerDirection,
        float frameSeconds,
        bool loop,
        bool attackSwing)
    {
        ValidateFinitePositive(frameSeconds, nameof(frameSeconds));
        int normalizedFrames = framesPerDirection < 2 ? 1 : framesPerDirection;
        float duration = frameSeconds * normalizedFrames;
        if (float.IsInfinity(duration))
        {
            throw new ArgumentOutOfRangeException(nameof(frameSeconds));
        }

        return new ReferenceWorldPlayerActionAnimationState(
            true,
            ReferenceAvatarAttackPoseAssembler.NormalizeVariant(variant),
            normalizedFrames,
            frameSeconds,
            0f,
            duration,
            loop,
            attackSwing);
    }

    public static ReferenceWorldPlayerActionAnimationPlan Advance(
        ReferenceWorldPlayerActionAnimationState state,
        float deltaTime,
        bool sitting,
        bool moving,
        float deltaX,
        float deltaY,
        int direction)
    {
        ValidateFiniteNonnegative(deltaTime, nameof(deltaTime));
        ValidateFinite(deltaX, nameof(deltaX));
        ValidateFinite(deltaY, nameof(deltaY));
        if (!state.Active)
        {
            return new ReferenceWorldPlayerActionAnimationPlan(
                state,
                false,
                false,
                ClampDirection(direction),
                0);
        }

        if (sitting || (moving && (deltaX != 0f || deltaY != 0f)))
        {
            return new ReferenceWorldPlayerActionAnimationPlan(
                state.Cancel(),
                false,
                true,
                ClampDirection(direction),
                0);
        }

        float elapsed = state.ElapsedSeconds + deltaTime;
        if (float.IsInfinity(elapsed))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        if (elapsed >= state.DurationSeconds)
        {
            if (state.Loop)
            {
                // The reference subtracts exactly once; it does not use modulo.
                elapsed -= state.DurationSeconds;
            }
            else if (elapsed >= state.DurationSeconds + AttackSwingTailSeconds ||
                     !state.AttackSwing)
            {
                ReferenceWorldPlayerActionAnimationState cancelled =
                    WithElapsed(state, elapsed).Cancel();
                return new ReferenceWorldPlayerActionAnimationPlan(
                    cancelled,
                    false,
                    true,
                    ClampDirection(direction),
                    0);
            }
        }

        ReferenceWorldPlayerActionAnimationState advanced = WithElapsed(state, elapsed);
        int frame = Math.Max(
            0,
            Math.Min(
                state.FramesPerDirection - 1,
                (int)(elapsed / state.FrameSeconds)));
        return new ReferenceWorldPlayerActionAnimationPlan(
            advanced,
            true,
            false,
            ClampDirection(direction),
            frame);
    }

    private static ReferenceWorldPlayerActionAnimationState WithElapsed(
        ReferenceWorldPlayerActionAnimationState state,
        float elapsed)
    {
        return new ReferenceWorldPlayerActionAnimationState(
            state.Active,
            state.Variant,
            state.FramesPerDirection,
            state.FrameSeconds,
            elapsed,
            state.DurationSeconds,
            state.Loop,
            state.AttackSwing);
    }

    private static int ClampDirection(int direction)
    {
        return Math.Max(0, Math.Min(DirectionCount - 1, direction));
    }

    private static void ValidateFinite(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateFiniteNonnegative(float value, string parameterName)
    {
        ValidateFinite(value, parameterName);
        if (value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }

    private static void ValidateFinitePositive(float value, string parameterName)
    {
        ValidateFinite(value, parameterName);
        if (value <= 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
#endif
}

}
