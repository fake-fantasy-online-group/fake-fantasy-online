using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One owned Sprite/Texture layer produced by PlayerAvatar.MakePose and MakeLayerSprite rules.
/// </summary>
internal sealed class ReferenceWorldPlayerStandLayer : IDisposable
{
    private readonly bool borrowed;

    internal ReferenceWorldPlayerStandLayer(PlayerAvatar.LayerInst layer)
    {
        borrowed = true;
        Tag = layer.tag;
        Additive = layer.additive;
        Sprite = layer.spr;
        Texture = layer.spr != null ? layer.spr.texture : null;
        OffsetX = layer.offX;
        OffsetY = layer.offY;
    }

    public ReferenceWorldPlayerStandLayer(
        int tag,
        int originX,
        int originY,
        int anchorX,
        int anchorY,
        bool additive,
        Texture2D texture,
        Sprite sprite,
        float offsetX,
        float offsetY)
    {
        Tag = tag;
        OriginX = originX;
        OriginY = originY;
        AnchorX = anchorX;
        AnchorY = anchorY;
        Additive = additive;
        Texture = texture == null ? throw new ArgumentNullException(nameof(texture)) : texture;
        Sprite = sprite == null ? throw new ArgumentNullException(nameof(sprite)) : sprite;
        OffsetX = offsetX;
        OffsetY = offsetY;
    }

    public int Tag { get; }
    public int OriginX { get; }
    public int OriginY { get; }
    public int AnchorX { get; }
    public int AnchorY { get; }
    public bool Additive { get; }
    public Texture2D Texture { get; private set; }
    public Sprite Sprite { get; private set; }
    public float OffsetX { get; }
    public float OffsetY { get; }

    public void Dispose()
    {
        if (borrowed) return;
        DestroyOwned(Sprite);
        DestroyOwned(Texture);
        Sprite = null;
        Texture = null;
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

/// <summary>
/// Disposable world-ready stand pose built from the same source pose as the title portrait.
/// </summary>
internal sealed class ReferenceWorldPlayerStandPose : PlayerAvatar.Pose, IDisposable
{
    private readonly List<ReferenceWorldPlayerStandLayer> ownedLayers;
    private bool disposed;
    private readonly bool borrowed;

    internal ReferenceWorldPlayerStandPose(PlayerAvatar.Pose source, int bodyBoundWidth, int topPixels)
    {
        borrowed = true;
        SourcePose = source;
        layers = source.layers;
        ownedLayers = new List<ReferenceWorldPlayerStandLayer>(layers == null ? 0 : layers.Length);
        if (layers != null)
            foreach (PlayerAvatar.LayerInst layer in layers) ownedLayers.Add(new ReferenceWorldPlayerStandLayer(layer));
        Layers = new ReadOnlyCollection<ReferenceWorldPlayerStandLayer>(ownedLayers);
        BodyBoundWidth = bodyBoundWidth;
        TopPixels = topPixels;
    }

    public ReferenceWorldPlayerStandPose(
        IList<ReferenceWorldPlayerStandLayer> sourceLayers,
        int bodyBoundWidth)
    {
        if (sourceLayers == null)
        {
            throw new ArgumentNullException(nameof(sourceLayers));
        }

        ownedLayers = new List<ReferenceWorldPlayerStandLayer>(sourceLayers);
        layers = new PlayerAvatar.LayerInst[sourceLayers.Count];
        for (int i = 0; i < sourceLayers.Count; i++)
        {
            ReferenceWorldPlayerStandLayer layer = sourceLayers[i];
            layers[i] = new PlayerAvatar.LayerInst
            {
                spr = layer.Sprite, offX = layer.OffsetX, offY = layer.OffsetY,
                tag = layer.Tag, additive = layer.Additive
            };
        }
        Layers = new ReadOnlyCollection<ReferenceWorldPlayerStandLayer>(ownedLayers);
        BodyBoundWidth = bodyBoundWidth;
        int topPixels = 0;
        foreach (ReferenceWorldPlayerStandLayer layer in ownedLayers)
        {
            topPixels = Math.Max(topPixels, -layer.OriginY);
        }

        TopPixels = topPixels;
    }

    internal PlayerAvatar.Pose SourcePose { get; }
    internal PlayerAvatar.Pose OriginalPose => SourcePose ?? this;

    public IReadOnlyList<ReferenceWorldPlayerStandLayer> Layers { get; }
    public int BodyBoundWidth { get; }
    public int TopPixels { get; }

    public void Dispose()
    {
        if (borrowed || disposed)
        {
            return;
        }

        disposed = true;
        foreach (ReferenceWorldPlayerStandLayer layer in ownedLayers)
        {
            layer.Dispose();
        }

        ownedLayers.Clear();
        layers = Array.Empty<PlayerAvatar.LayerInst>();
    }
}

/// <summary>
/// Disposable world-ready normal stand and walk frames produced by PlayerAvatar.Build.
/// </summary>
internal sealed class ReferenceWorldPlayerNormalPoses : IDisposable
{
    public const float StandBodySecondsPerFrame = 0.07f;
    public const float StandWingSecondsPerFrame = 0.105000004f;

    private bool disposed;
    private readonly ReferencePlayerAvatarRuntime native;

    // Non-owning by construction. RemotePlayer.Init/Rebuild (0x06000af6 / 0x06000b06) is the only
    // producer of a remote-player avatar and RemotePlayer.OnDestroy (0x06000b37) is the only disposer,
    // so a stand-pose view must never own the avatar whose frames it wraps: disposing it here would
    // dispose the owner's live avatar and leave the owner with a released instance.
    internal PlayerAvatar SourceAvatarForIndependentRuntime => native?.Source;
    internal ReferenceWorldPlayerStandPose WrapSourceForIndependentRuntime(PlayerAvatar.Pose source) =>
        source as ReferenceWorldPlayerStandPose ?? native?.Wrap(source);

    internal ReferenceWorldPlayerSittingPoses SittingViewForIndependentRuntime() =>
        native.Source.sit == null ? null : native.Sitting();
    internal ReferenceWorldPlayerDeathPoses DeathViewForIndependentRuntime(PlayerAvatar.Pose[,] frames, int fpd) =>
        frames == null ? null : new ReferenceWorldPlayerDeathPoses(native.Wrap(frames), fpd);

    internal ReferenceWorldPlayerNormalPoses(ReferencePlayerAvatarRuntime native)
    {
        this.native = native;
        PlayerAvatar source = native.Source;
        Stand = native.Wrap(source.stand);
        StandAnimation = new ReferenceWorldPlayerStandPose[source.standAnim.Length][];
        for (int d = 0; d < StandAnimation.Length; d++) StandAnimation[d] = native.Wrap(source.standAnim[d]);
        StandBodyFramesPerDirection = source.standBodyFpd;
        StandWingFramesPerDirection = source.standWingFpd;
        Walk = native.Wrap(source.walk);
        HeadPixels = source.headPx;
        NameTopPixels = source.nameTopPx;
    }

    public ReferenceWorldPlayerNormalPoses(
        ReferenceWorldPlayerStandPose[][] standAnimation,
        int standBodyFramesPerDirection,
        int standWingFramesPerDirection,
        ReferenceWorldPlayerStandPose[,] walk,
        int headPixels)
    {
        StandAnimation = standAnimation ??
            throw new ArgumentNullException(nameof(standAnimation));
        if (standAnimation.Length < 1)
        {
            throw new ArgumentException(
                "Stand animation requires at least one direction.",
                nameof(standAnimation));
        }

        if (standBodyFramesPerDirection < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(standBodyFramesPerDirection));
        }

        if (standWingFramesPerDirection < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(standWingFramesPerDirection));
        }

        Stand = new ReferenceWorldPlayerStandPose[standAnimation.Length];
        for (int direction = 0; direction < standAnimation.Length; direction++)
        {
            ReferenceWorldPlayerStandPose[] row = standAnimation[direction];
            if (row == null || row.Length < 1 || row[0] == null)
            {
                throw new ArgumentException(
                    "Every stand-animation direction requires a frame-zero pose.",
                    nameof(standAnimation));
            }

            Stand[direction] = row[0];
        }

        StandBodyFramesPerDirection = standBodyFramesPerDirection;
        StandWingFramesPerDirection = standWingFramesPerDirection;
        Walk = walk ?? throw new ArgumentNullException(nameof(walk));
        if (headPixels < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(headPixels));
        }

        HeadPixels = headPixels;
        int nameTopPixels = 0;
        foreach (ReferenceWorldPlayerStandPose[] direction in StandAnimation)
        {
            if (direction == null)
            {
                continue;
            }

            foreach (ReferenceWorldPlayerStandPose pose in direction)
            {
                if (pose != null)
                {
                    nameTopPixels = Math.Max(nameTopPixels, pose.TopPixels);
                }
            }
        }

        for (int direction = 0; direction < Walk.GetLength(0); direction++)
        {
            for (int frame = 0; frame < Walk.GetLength(1); frame++)
            {
                ReferenceWorldPlayerStandPose pose = Walk[direction, frame];
                if (pose != null)
                {
                    nameTopPixels = Math.Max(nameTopPixels, pose.TopPixels);
                }
            }
        }

        NameTopPixels = nameTopPixels;
    }

    public ReferenceWorldPlayerStandPose[] Stand { get; }
    public ReferenceWorldPlayerStandPose[][] StandAnimation { get; }
    public int StandBodyFramesPerDirection { get; }
    public int StandWingFramesPerDirection { get; }
    public ReferenceWorldPlayerStandPose[,] Walk { get; }
    public int NameTopPixels { get; }
    public int HeadPixels { get; }

    public ReferenceWorldPlayerStandPose PoseFor(
        int direction,
        int frame,
        bool walking)
    {
        if (direction < 0 || direction >= Stand.Length)
        {
            return null;
        }

        if (!walking)
        {
            return Stand[direction];
        }

        return frame >= 0 && frame < Walk.GetLength(1)
            ? Walk[direction, frame]
            : null;
    }

    public ReferenceWorldPlayerStandPose StandAt(int direction, float timeSeconds)
    {
        if (native != null) return native.Wrap(native.Source.StandAt(direction, timeSeconds));
        int selectedDirection = Math.Max(0, Math.Min(Stand.Length - 1, direction));
        if (float.IsNaN(timeSeconds) || float.IsInfinity(timeSeconds))
        {
            return null;
        }

        ReferenceWorldPlayerStandPose[] animation =
            StandAnimation[selectedDirection];
        if (animation != null && animation.Length > 1)
        {
            float nonnegativeTime = Math.Max(0f, timeSeconds);
            int bodyFrame = StandBodyFramesPerDirection > 1
                ? (int)(nonnegativeTime / StandBodySecondsPerFrame) %
                  StandBodyFramesPerDirection
                : 0;
            int wingCount = Math.Max(StandWingFramesPerDirection, 1);
            int wingFrame = StandWingFramesPerDirection > 1
                ? (int)(nonnegativeTime / StandWingSecondsPerFrame) % wingCount
                : 0;
            int combinedFrame = bodyFrame * wingCount + wingFrame;
            if (combinedFrame >= animation.Length)
            {
                combinedFrame %= animation.Length;
            }

            ReferenceWorldPlayerStandPose selected = animation[combinedFrame];
            if (selected != null)
            {
                return selected;
            }
        }

        return Stand[selectedDirection];
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (ReferenceWorldPlayerStandPose[] direction in StandAnimation)
        {
            if (direction == null)
            {
                continue;
            }

            foreach (ReferenceWorldPlayerStandPose pose in direction)
            {
                pose?.Dispose();
            }
        }

        for (int direction = 0; direction < Walk.GetLength(0); direction++)
        {
            for (int frame = 0; frame < Walk.GetLength(1); frame++)
            {
                Walk[direction, frame]?.Dispose();
            }
        }
    }
}

/// <summary>
/// Disposable world-ready sitting poses produced by PlayerAvatar.Build.
/// </summary>
internal sealed class ReferenceWorldPlayerSittingPoses : IDisposable
{
    private bool disposed;

    internal ReferenceWorldPlayerSittingPoses(ReferenceWorldPlayerStandPose[] poses, int sitTopPixels)
    {
        Poses = poses;
        SitTopPixels = sitTopPixels;
    }

    public ReferenceWorldPlayerSittingPoses(ReferenceWorldPlayerStandPose[] poses)
    {
        Poses = poses ?? throw new ArgumentNullException(nameof(poses));
        int sitTopPixels = 0;
        foreach (ReferenceWorldPlayerStandPose pose in poses)
        {
            if (pose != null)
            {
                sitTopPixels = Math.Max(sitTopPixels, pose.TopPixels);
            }
        }

        SitTopPixels = sitTopPixels;
    }

    public ReferenceWorldPlayerStandPose[] Poses { get; }
    public int SitTopPixels { get; }

    public ReferenceWorldPlayerStandPose PoseFor(int direction)
    {
        return direction >= 0 && direction < Poses.Length
            ? Poses[direction]
            : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (ReferenceWorldPlayerStandPose pose in Poses)
        {
            pose?.Dispose();
        }
    }
}

/// <summary>
/// Disposable world-ready frames produced by PlayerAvatar.PoseFrames(3).
/// </summary>
internal sealed class ReferenceWorldPlayerDeathPoses : IDisposable
{
    private bool disposed;

    public ReferenceWorldPlayerDeathPoses(
        ReferenceWorldPlayerStandPose[,] frames,
        int framesPerDirection)
    {
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        FramesPerDirection = framesPerDirection;
    }

    public ReferenceWorldPlayerStandPose[,] Frames { get; }
    public int FramesPerDirection { get; }

    public ReferenceWorldPlayerStandPose PoseFor(int direction, int frame)
    {
        return direction >= 0 && direction < Frames.GetLength(0) &&
               frame >= 0 && frame < Frames.GetLength(1)
            ? Frames[direction, frame]
            : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        for (int direction = 0; direction < Frames.GetLength(0); direction++)
        {
            for (int frame = 0; frame < Frames.GetLength(1); frame++)
            {
                Frames[direction, frame]?.Dispose();
            }
        }
    }
}

/// <summary>
/// Disposable world-ready frames produced by PlayerAvatar.PoseFrames(11).
/// </summary>
internal sealed class ReferenceWorldPlayerHurtPoses : IDisposable
{
    private bool disposed;

    public ReferenceWorldPlayerHurtPoses(
        ReferenceWorldPlayerStandPose[,] frames,
        int framesPerDirection)
    {
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        FramesPerDirection = framesPerDirection;
    }

    public ReferenceWorldPlayerStandPose[,] Frames { get; }
    public int FramesPerDirection { get; }

    public ReferenceWorldPlayerStandPose PoseFor(int direction, int frame)
    {
        return direction >= 0 && direction < Frames.GetLength(0) &&
               frame >= 0 && frame < Frames.GetLength(1)
            ? Frames[direction, frame]
            : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        for (int direction = 0; direction < Frames.GetLength(0); direction++)
        {
            for (int frame = 0; frame < Frames.GetLength(1); frame++)
            {
                Frames[direction, frame]?.Dispose();
            }
        }
    }
}

/// <summary>
/// Disposable world-ready frames produced by PlayerAvatar.AttackPose for one variant.
/// </summary>
internal sealed class ReferenceWorldPlayerAttackPoses : IDisposable
{
    private bool disposed;

    public ReferenceWorldPlayerAttackPoses(
        ReferenceWorldPlayerStandPose[,] frames,
        int framesPerDirection,
        int variant)
    {
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        FramesPerDirection = framesPerDirection;
        Variant = variant;
    }

    public ReferenceWorldPlayerStandPose[,] Frames { get; }
    public int FramesPerDirection { get; }
    public int Variant { get; }

    public ReferenceWorldPlayerStandPose PoseFor(int direction, int frame)
    {
        return direction >= 0 && direction < Frames.GetLength(0) &&
               frame >= 0 && frame < Frames.GetLength(1)
            ? Frames[direction, frame]
            : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        for (int direction = 0; direction < Frames.GetLength(0); direction++)
        {
            for (int frame = 0; frame < Frames.GetLength(1); frame++)
            {
                Frames[direction, frame]?.Dispose();
            }
        }
    }
}

/// <summary>
/// Disposable five-direction PoseAt result for one held preparation action.
/// </summary>
internal sealed class ReferenceWorldPlayerPreparePoses : IDisposable
{
    private bool disposed;

    public ReferenceWorldPlayerPreparePoses(
        ReferenceWorldPlayerStandPose[] poses,
        int actionIndex)
    {
        Poses = poses ?? throw new ArgumentNullException(nameof(poses));
        ActionIndex = actionIndex;
    }

    public ReferenceWorldPlayerStandPose[] Poses { get; }
    public int ActionIndex { get; }

    public ReferenceWorldPlayerStandPose PoseFor(int direction)
    {
        return direction >= 0 && direction < Poses.Length
            ? Poses[direction]
            : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        foreach (ReferenceWorldPlayerStandPose pose in Poses)
        {
            pose?.Dispose();
        }
    }
}

/// <summary>
/// Disposable world-ready frames produced by PlayerAvatar.MagicPose.
/// </summary>
internal sealed class ReferenceWorldPlayerMagicPoses : IDisposable
{
    private bool disposed;

    public ReferenceWorldPlayerMagicPoses(
        ReferenceWorldPlayerStandPose[,] frames,
        int framesPerDirection)
    {
        Frames = frames ?? throw new ArgumentNullException(nameof(frames));
        FramesPerDirection = framesPerDirection;
    }

    public ReferenceWorldPlayerStandPose[,] Frames { get; }
    public int FramesPerDirection { get; }

    public ReferenceWorldPlayerStandPose PoseFor(int direction, int frame)
    {
        return direction >= 0 && direction < Frames.GetLength(0) &&
               frame >= 0 && frame < Frames.GetLength(1)
            ? Frames[direction, frame]
            : null;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        for (int direction = 0; direction < Frames.GetLength(0); direction++)
        {
            for (int frame = 0; frame < Frames.GetLength(1); frame++)
            {
                Frames[direction, frame]?.Dispose();
            }
        }
    }
}

/// <summary>
/// Identifies which recovered PlayerController action frame source is active.
/// </summary>
public enum ReferenceWorldPlayerActionPoseKind
{
    None = 0,
    Attack = 1,
    Chant = 2,
    Magic = 3,
    Hurt = 4
}

/// <summary>
/// Converts the recovered managed stand pose into the original layered Unity Sprite shape.
/// </summary>
internal static class ReferenceWorldPlayerStandPoseFactory
{
    public const int MaximumDollLayers = 14;

#if UNITY_EDITOR
    internal static bool TryBuildNormalForAudit(
        ReferenceResourceLoader resources,
        float pixelsPerUnit,
        int gender,
        int head,
        int hair,
        int body,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face,
        out ReferenceWorldPlayerNormalPoses poses)
    {
        WorldEntryRecord character = new WorldEntryRecord(
            1,
            "audit",
            "audit",
            string.Empty,
            0,
            gender,
            head,
            hair,
            ReferenceFirstWorldInitialFrame.FirstMapId,
            0f,
            0f,
            Array.Empty<int>(),
            new CharacterAppearanceRecord(
                body,
                cap,
                backpack,
                rightWeapon,
                leftWeapon,
                face,
                0,
                0,
                0),
            new WorldPetRecord(0, string.Empty, 0),
            null,
            null,
            null,
            null,
            null);
        return TryBuildNormal(resources, character, pixelsPerUnit, out poses);
    }

    internal static bool TryBuildAttackForAudit(
        ReferenceResourceLoader resources,
        float pixelsPerUnit,
        int gender,
        int head,
        int hair,
        int body,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face,
        int variant,
        out ReferenceWorldPlayerAttackPoses poses)
    {
        WorldEntryRecord character = new WorldEntryRecord(
            1,
            "audit",
            "audit",
            string.Empty,
            0,
            gender,
            head,
            hair,
            ReferenceFirstWorldInitialFrame.FirstMapId,
            0f,
            0f,
            Array.Empty<int>(),
            new CharacterAppearanceRecord(
                body,
                cap,
                backpack,
                rightWeapon,
                leftWeapon,
                face,
                0,
                0,
                0),
            new WorldPetRecord(0, string.Empty, 0),
            null,
            null,
            null,
            null,
            null);
        return TryBuildAttack(resources, character, pixelsPerUnit, variant, out poses);
    }

    internal static bool TryBuildPrepareForAudit(
        ReferenceResourceLoader resources,
        float pixelsPerUnit,
        int body,
        int actionIndex,
        out ReferenceWorldPlayerPreparePoses poses)
    {
        WorldEntryRecord character = CreatePrepareMagicAuditCharacter(body);
        return TryBuildPrepare(
            resources,
            character,
            pixelsPerUnit,
            actionIndex,
            out poses);
    }

    internal static bool TryBuildMagicForAudit(
        ReferenceResourceLoader resources,
        float pixelsPerUnit,
        int body,
        out ReferenceWorldPlayerMagicPoses poses)
    {
        WorldEntryRecord character = CreatePrepareMagicAuditCharacter(body);
        return TryBuildMagic(resources, character, pixelsPerUnit, out poses);
    }

    private static WorldEntryRecord CreatePrepareMagicAuditCharacter(int body)
    {
        return new WorldEntryRecord(
            1,
            "audit",
            "audit",
            string.Empty,
            0,
            1,
            1,
            1,
            ReferenceFirstWorldInitialFrame.FirstMapId,
            0f,
            0f,
            Array.Empty<int>(),
            new CharacterAppearanceRecord(
                body,
                49,
                332,
                114,
                0,
                2,
                0,
                0,
                0),
            new WorldPetRecord(0, string.Empty, 0),
            null,
            null,
            null,
            null,
            null);
    }
#endif

    public static bool TryBuild(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerStandPose pose)
    {
        pose = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarStandSourcePose sourcePose;
        if (!ReferenceAvatarStandAssembler.TryAssemble(
                resources, depth, assets, out sourcePose) ||
            sourcePose.Layers.Count < 1 ||
            sourcePose.Layers.Count > MaximumDollLayers)
        {
            return false;
        }

        return TryBuild(sourcePose, pixelsPerUnit, out pose);
    }

    public static bool TryBuildNormal(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerNormalPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return TryBuildNormal(resources, assets, pixelsPerUnit, out poses);
    }

    /// <summary>
    /// Builds the non-owning stand/sitting frame view over an avatar that its owner already produced.
    /// </summary>
    /// <remarks>
    /// This factory deliberately does not build an avatar. A remote player's avatar is produced only by
    /// RemotePlayer.Init (0x06000af6) through the official <c>!ShouldHideNow() &amp;&amp; TakeBuildBudget()</c>
    /// gate into RemotePlayer.Rebuild (0x06000b06), or later by the owner's own missing-avatar update
    /// branch; building one here would create a second avatar for the same spawn, and handing it to the
    /// owner would replace (and dispose) the one the official gate had just produced. The returned view
    /// wraps the owner's frames and never disposes the avatar.
    /// </remarks>
    public static bool TryBuildNormalView(
        PlayerAvatar source,
        CharacterAppearanceRecord appearance,
        out ReferenceWorldPlayerNormalPoses poses)
    {
        poses = null;
        if (source == null || !source.ok || appearance == null)
        {
            return false;
        }

        poses = new ReferencePlayerAvatarRuntime(source, appearance).Normal();
        return true;
    }

    private static bool TryBuildNormal(
        ReferenceResourceLoader resources,
        ReferenceAvatarAssets assets,
        float pixelsPerUnit,
        out ReferenceWorldPlayerNormalPoses poses)
    {
        poses = null;

        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarNormalSourcePoses source;
        if (!ReferenceAvatarNormalPoseAssembler.TryAssemble(
                resources, depth, assets, out source))
        {
            return false;
        }

        ReferenceWorldPlayerStandPose[][] standAnimation =
            new ReferenceWorldPlayerStandPose[source.StandAnimation.Length][];
        ReferenceWorldPlayerStandPose[,] walk =
            new ReferenceWorldPlayerStandPose[
                source.Walk.GetLength(0),
                source.Walk.GetLength(1)];
        try
        {
            for (int direction = 0; direction < standAnimation.Length; direction++)
            {
                ReferenceAvatarStandSourcePose[] sourceDirection =
                    source.StandAnimation[direction];
                standAnimation[direction] =
                    new ReferenceWorldPlayerStandPose[sourceDirection.Length];
                for (int frame = 0; frame < sourceDirection.Length; frame++)
                {
                    if (!TryBuild(
                            sourceDirection[frame],
                            pixelsPerUnit,
                            out standAnimation[direction][frame]))
                    {
                        DisposePoses(standAnimation, walk);
                        return false;
                    }
                }

                for (int frame = 0; frame < walk.GetLength(1); frame++)
                {
                    if (!TryBuild(
                            source.Walk[direction, frame],
                            pixelsPerUnit,
                            out walk[direction, frame]))
                    {
                        DisposePoses(standAnimation, walk);
                        return false;
                    }
                }
            }

            poses = new ReferenceWorldPlayerNormalPoses(
                standAnimation,
                source.StandBodyFramesPerDirection,
                source.StandWingFramesPerDirection,
                walk,
                source.HeadPixels);
            return true;
        }
        catch
        {
            DisposePoses(standAnimation, walk);
            throw;
        }
    }

    public static bool TryBuildDeath(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerDeathPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarDeathSourcePoses source;
        if (!ReferenceAvatarDeathPoseAssembler.TryAssemble(
                resources, depth, assets, out source))
        {
            return false;
        }

        return TryBuildDeath(source, pixelsPerUnit, out poses);
    }

    public static bool TryBuildHurt(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerHurtPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarHurtSourcePoses source;
        if (!ReferenceAvatarDeathPoseAssembler.TryAssembleHurt(
                resources, depth, assets, out source))
        {
            return false;
        }

        ReferenceWorldPlayerStandPose[,] frames =
            new ReferenceWorldPlayerStandPose[
                source.Frames.GetLength(0),
                source.Frames.GetLength(1)];
        try
        {
            for (int direction = 0; direction < frames.GetLength(0); direction++)
            {
                for (int frame = 0; frame < frames.GetLength(1); frame++)
                {
                    if (!TryBuild(
                            source.Frames[direction, frame],
                            pixelsPerUnit,
                            out frames[direction, frame]))
                    {
                        DisposePoses(frames);
                        return false;
                    }
                }
            }

            poses = new ReferenceWorldPlayerHurtPoses(
                frames,
                source.FramesPerDirection);
            return true;
        }
        catch
        {
            DisposePoses(frames);
            throw;
        }
    }

    public static bool TryBuildRemoteDeath(
        ReferenceResourceLoader resources,
        WorldRemotePlayerState character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerDeathPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarDeathSourcePoses source;
        if (!ReferenceAvatarDeathPoseAssembler.TryAssembleRemote(
                resources, depth, assets, out source))
        {
            return false;
        }

        return TryBuildDeath(source, pixelsPerUnit, out poses);
    }

    private static bool TryBuildDeath(
        ReferenceAvatarDeathSourcePoses source,
        float pixelsPerUnit,
        out ReferenceWorldPlayerDeathPoses poses)
    {
        poses = null;

        ReferenceWorldPlayerStandPose[,] frames =
            new ReferenceWorldPlayerStandPose[
                source.Frames.GetLength(0),
                source.Frames.GetLength(1)];
        try
        {
            for (int direction = 0; direction < frames.GetLength(0); direction++)
            {
                for (int frame = 0; frame < frames.GetLength(1); frame++)
                {
                    if (!TryBuild(
                            source.Frames[direction, frame],
                            pixelsPerUnit,
                            out frames[direction, frame]))
                    {
                        DisposePoses(frames);
                        return false;
                    }
                }
            }

            poses = new ReferenceWorldPlayerDeathPoses(
                frames,
                source.FramesPerDirection);
            return true;
        }
        catch
        {
            DisposePoses(frames);
            throw;
        }
    }

    public static bool TryBuildSitting(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerSittingPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return TryBuildSitting(resources, assets, pixelsPerUnit, out poses);
    }

    public static bool TryBuildSitting(
        ReferenceResourceLoader resources,
        WorldRemotePlayerState character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerSittingPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return TryBuildSitting(resources, assets, pixelsPerUnit, out poses);
    }

    private static bool TryBuildSitting(
        ReferenceResourceLoader resources,
        ReferenceAvatarAssets assets,
        float pixelsPerUnit,
        out ReferenceWorldPlayerSittingPoses poses)
    {
        poses = null;

        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarSittingSourcePoses source;
        if (!ReferenceAvatarSittingPoseAssembler.TryAssemble(
                resources, depth, assets, out source))
        {
            return false;
        }

        ReferenceWorldPlayerStandPose[] builtPoses =
            new ReferenceWorldPlayerStandPose[source.Poses.Length];
        try
        {
            for (int direction = 0; direction < builtPoses.Length; direction++)
            {
                if (!TryBuild(
                        source.Poses[direction],
                        pixelsPerUnit,
                        out builtPoses[direction]))
                {
                    DisposePoses(builtPoses);
                    return false;
                }
            }

            poses = new ReferenceWorldPlayerSittingPoses(builtPoses);
            return true;
        }
        catch
        {
            DisposePoses(builtPoses);
            throw;
        }
    }

    public static bool TryBuildAttack(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        int variant,
        out ReferenceWorldPlayerAttackPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return TryBuildAttack(resources, assets, pixelsPerUnit, variant, out poses);
    }

    public static bool TryBuildAttack(
        ReferenceResourceLoader resources,
        WorldRemotePlayerState character,
        float pixelsPerUnit,
        int variant,
        out ReferenceWorldPlayerAttackPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return TryBuildAttack(resources, assets, pixelsPerUnit, variant, out poses);
    }

    private static bool TryBuildAttack(
        ReferenceResourceLoader resources,
        ReferenceAvatarAssets assets,
        float pixelsPerUnit,
        int variant,
        out ReferenceWorldPlayerAttackPoses poses)
    {
        poses = null;
        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarAttackSourcePoses source;
        if (!ReferenceAvatarAttackPoseAssembler.TryAssemble(
                resources, depth, assets, variant, out source))
        {
            return false;
        }

        ReferenceWorldPlayerStandPose[,] frames =
            new ReferenceWorldPlayerStandPose[
                source.Frames.GetLength(0),
                source.Frames.GetLength(1)];
        try
        {
            for (int direction = 0; direction < frames.GetLength(0); direction++)
            {
                for (int frame = 0; frame < frames.GetLength(1); frame++)
                {
                    if (!TryBuild(
                            source.Frames[direction, frame],
                            pixelsPerUnit,
                            out frames[direction, frame]))
                    {
                        DisposePoses(frames);
                        return false;
                    }
                }
            }

            poses = new ReferenceWorldPlayerAttackPoses(
                frames,
                source.FramesPerDirection,
                source.Variant);
            return true;
        }
        catch
        {
            DisposePoses(frames);
            throw;
        }
    }

    public static bool TryBuildPrepare(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        int actionIndex,
        out ReferenceWorldPlayerPreparePoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarPrepareSourcePoses source;
        if (!ReferenceAvatarPrepareMagicPoseAssembler.TryAssemblePrepare(
                resources, depth, assets, actionIndex, out source))
        {
            return false;
        }

        ReferenceWorldPlayerStandPose[] builtPoses =
            new ReferenceWorldPlayerStandPose[source.Poses.Length];
        try
        {
            for (int direction = 0; direction < builtPoses.Length; direction++)
            {
                if (!TryBuild(
                        source.Poses[direction],
                        pixelsPerUnit,
                        out builtPoses[direction]))
                {
                    DisposePoses(builtPoses);
                    return false;
                }
            }

            poses = new ReferenceWorldPlayerPreparePoses(
                builtPoses,
                source.ActionIndex);
            return true;
        }
        catch
        {
            DisposePoses(builtPoses);
            throw;
        }
    }

    public static bool TryBuildMagic(
        ReferenceResourceLoader resources,
        WorldEntryRecord character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerMagicPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return TryBuildMagic(resources, assets, pixelsPerUnit, out poses);
    }

    public static bool TryBuildMagic(
        ReferenceResourceLoader resources,
        WorldRemotePlayerState character,
        float pixelsPerUnit,
        out ReferenceWorldPlayerMagicPoses poses)
    {
        poses = null;
        if (resources == null || character == null || pixelsPerUnit <= 0f)
        {
            return false;
        }

        ReferenceAvatarAssets assets;
        if (!ReferenceAvatarAssetPlan.TryCreate(character, out assets))
        {
            return false;
        }

        return TryBuildMagic(resources, assets, pixelsPerUnit, out poses);
    }

    private static bool TryBuildMagic(
        ReferenceResourceLoader resources,
        ReferenceAvatarAssets assets,
        float pixelsPerUnit,
        out ReferenceWorldPlayerMagicPoses poses)
    {
        poses = null;
        AvatarDepthTable depth = AvatarDepthTable.Load(resources);
        ReferenceAvatarMagicSourcePoses source;
        if (!ReferenceAvatarPrepareMagicPoseAssembler.TryAssembleMagic(
                resources, depth, assets, out source))
        {
            return false;
        }

        ReferenceWorldPlayerStandPose[,] frames =
            new ReferenceWorldPlayerStandPose[
                source.Frames.GetLength(0),
                source.Frames.GetLength(1)];
        try
        {
            for (int direction = 0; direction < frames.GetLength(0); direction++)
            {
                for (int frame = 0; frame < frames.GetLength(1); frame++)
                {
                    if (!TryBuild(
                            source.Frames[direction, frame],
                            pixelsPerUnit,
                            out frames[direction, frame]))
                    {
                        DisposePoses(frames);
                        return false;
                    }
                }
            }

            poses = new ReferenceWorldPlayerMagicPoses(
                frames,
                source.FramesPerDirection);
            return true;
        }
        catch
        {
            DisposePoses(frames);
            throw;
        }
    }

    private static bool TryBuild(
        ReferenceAvatarStandSourcePose sourcePose,
        float pixelsPerUnit,
        out ReferenceWorldPlayerStandPose pose)
    {
        pose = null;
        if (sourcePose == null ||
            sourcePose.Layers.Count < 1 ||
            sourcePose.Layers.Count > MaximumDollLayers)
        {
            return false;
        }

        List<ReferenceWorldPlayerStandLayer> layers =
            new List<ReferenceWorldPlayerStandLayer>(sourcePose.Layers.Count);
        try
        {
            foreach (ReferenceAvatarStandSourceLayer source in sourcePose.Layers)
            {
                ReferenceWorldPlayerStandLayer layer;
                if (!TryBuildLayer(source, pixelsPerUnit, out layer))
                {
                    DisposeLayers(layers);
                    return false;
                }

                layers.Add(layer);
            }

            pose = new ReferenceWorldPlayerStandPose(layers, sourcePose.BodyBoundWidth);
            return true;
        }
        catch
        {
            DisposeLayers(layers);
            throw;
        }
    }

    private static void DisposePoses(
        IEnumerable<ReferenceWorldPlayerStandPose[]> standAnimation,
        ReferenceWorldPlayerStandPose[,] walk)
    {
        foreach (ReferenceWorldPlayerStandPose[] direction in standAnimation)
        {
            if (direction == null)
            {
                continue;
            }

            foreach (ReferenceWorldPlayerStandPose pose in direction)
            {
                pose?.Dispose();
            }
        }

        for (int direction = 0; direction < walk.GetLength(0); direction++)
        {
            for (int frame = 0; frame < walk.GetLength(1); frame++)
            {
                walk[direction, frame]?.Dispose();
            }
        }
    }

    private static void DisposePoses(ReferenceWorldPlayerStandPose[,] frames)
    {
        for (int direction = 0; direction < frames.GetLength(0); direction++)
        {
            for (int frame = 0; frame < frames.GetLength(1); frame++)
            {
                frames[direction, frame]?.Dispose();
            }
        }
    }

    private static void DisposePoses(ReferenceWorldPlayerStandPose[] poses)
    {
        foreach (ReferenceWorldPlayerStandPose pose in poses)
        {
            pose?.Dispose();
        }
    }

    private static bool TryBuildLayer(
        ReferenceAvatarStandSourceLayer source,
        float pixelsPerUnit,
        out ReferenceWorldPlayerStandLayer layer)
    {
        layer = null;
        DimgFrame frame = source.Frame;
        if (frame.Width < 1 || frame.Height < 1 ||
            frame.Pixels == null || frame.Pixels.Length != frame.Width * frame.Height)
        {
            return false;
        }

        Color32[] bottomUp = new Color32[frame.Pixels.Length];
        for (int sourceY = 0; sourceY < frame.Height; sourceY++)
        {
            Array.Copy(
                frame.Pixels,
                sourceY * frame.Width,
                bottomUp,
                (frame.Height - 1 - sourceY) * frame.Width,
                frame.Width);
        }

        Texture2D texture = new Texture2D(
            frame.Width,
            frame.Height,
            TextureFormat.RGBA32,
            false,
            false);
        texture.name = "RecoveredAvatarLayerTexture";
        texture.SetPixels32(bottomUp);
        texture.Apply(false, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        Sprite sprite = null;
        try
        {
            Vector2 pivot = new Vector2(
                (float)source.AnchorX / frame.Width,
                (float)(frame.Height - source.AnchorY) / frame.Height);
            sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, frame.Width, frame.Height),
                pivot,
                pixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            if (sprite == null)
            {
                DestroyOwned(texture);
                return false;
            }

            sprite.name = "RecoveredAvatarLayer";
            layer = new ReferenceWorldPlayerStandLayer(
                source.Tag,
                source.OriginX,
                source.OriginY,
                source.AnchorX,
                source.AnchorY,
                source.Additive,
                texture,
                sprite,
                (source.OriginX + source.AnchorX) / pixelsPerUnit,
                (source.OriginY + source.AnchorY) / pixelsPerUnit);
            return true;
        }
        catch
        {
            DestroyOwned(sprite);
            DestroyOwned(texture);
            throw;
        }
    }

    private static void DisposeLayers(
        IEnumerable<ReferenceWorldPlayerStandLayer> layers)
    {
        foreach (ReferenceWorldPlayerStandLayer layer in layers)
        {
            layer.Dispose();
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

/// <summary>
/// Decision-free diagnostic classification for why the recovered movement owner became idle.
/// </summary>
public enum ReferenceWorldPlayerMovementStopReason
{
    None = 0,
    SetPathStopped = 1,
    Arrived = 2,
    PixelCollisionBlocked = 3,
    ExplicitOwnerStop = 4,
    AuthoritativeHurt = 5,
    AuthoritativeDeath = 6,
    DeadlineExpired = 7,
}

/// <summary>
/// Owns the first recovered local-player Doll hierarchy and reviewed pose states.
/// </summary>
/// <remarks>
/// This module restores PlayerController.Warp, EnsureDoll, GroupBase, the ordinary LoadFrames
/// stand/walk/sit cone including the reviewed StandAt body/wing clocks, PoseFrames(3),
/// AttackPose/MagicPose/PoseAt, PlayChant/PlayCast/StartActPose, and the common UpdateAnim ApplyPose
/// tail for the reviewed appearance.
/// Warp shares the exact rounded player Transform projection with the recovered normal movement
/// step, now product-wired for the reviewed local-follow route. The official transfer edge is
/// retained below; names, combat numbers and other streamed entities remain separate boundaries.
/// </remarks>
public sealed class ReferenceWorldLocalPlayerDoll :
    IDisposable,
    IReferenceWorldRemoteRiderDoll,
    IReferenceWorldFollowNavigation
{
    public const int LayerCapacity = ReferenceWorldPlayerStandPoseFactory.MaximumDollLayers;
    public const float PixelsPerUnit = 100f;
    public const float DefaultMoveSpeedPixels = 400f;
    public const float PixelsPerSpeedAttribute = 2f;

    private static readonly Color32 VisibleTint =
        new Color32(255, 255, 255, 255);
    private static readonly Color32 InvisibleTint =
        new Color32(64, 64, 128, 80);

    private readonly ReferenceGsmMap map;
    private readonly ReferenceResourceLoader resources;
    private readonly Transform parent;
    private WorldEntryRecord character;
    private readonly List<SpriteRenderer> layerRenderers = new List<SpriteRenderer>(LayerCapacity);

    private GameObject rootObject;
    private GameObject dollRootObject;
    private SpriteRenderer mainRenderer;
    private SpriteRenderer bodyRenderer;
    private ReferenceWorldBlobShadow shadow;
    private ReferenceWorldPlayerStandPose pose;
    private ReferenceWorldPlayerNormalPoses normalPoses;
    private ReferencePlayerAvatarRuntime nativeAvatar;
    private ReferenceWorldPlayerSittingPoses sittingPoses;
    private ReferenceWorldPlayerDeathPoses deathPoses;
    private ReferenceWorldPlayerHurtPoses hurtPoses;
    private bool hurtPosesBuilt;
    private readonly ReferenceWorldPlayerAttackPoses[] attackPoses =
        new ReferenceWorldPlayerAttackPoses[3];
    private readonly bool[] attackPosesBuilt = new bool[3];
    private readonly ReferenceWorldPlayerPreparePoses[] preparePoses =
        new ReferenceWorldPlayerPreparePoses[3];
    private readonly bool[] preparePosesBuilt = new bool[3];
    private ReferenceWorldPlayerMagicPoses magicPoses;
    private bool magicPosesBuilt;
    private PlayerController actionOwner;
    private int actionVariant;
    private ReferenceWorldPlayerActionAnimationState actionState =>
        actionOwner == null ? default(ReferenceWorldPlayerActionAnimationState) :
            actionOwner.ReadActionStateForIndependentRuntime(actionVariant);
    private ReferenceWorldPlayerActionPoseKind actionPoseKind;
    private int currentActionPoseIndex;
    private ReferenceWorldPlayerFacing currentFacing =
        ReferenceWorldPlayerMotionMath.FromOctant(3);
    private float animationDistancePixels;
    private int currentFrame;
    private int currentDeathFrame;
    private int currentActionFrame;
    private List<Vector2> movementPath = new List<Vector2>();
    private float detached_worldX;
    private float worldX
    {
        get => actionOwner != null ? actionOwner.worldX : detached_worldX;
        set
        {
            if (actionOwner != null) actionOwner.worldX = value;
            else detached_worldX = value;
        }
    }
    private float detached_worldY;
    private float worldY
    {
        get => actionOwner != null ? actionOwner.worldY : detached_worldY;
        set
        {
            if (actionOwner != null) actionOwner.worldY = value;
            else detached_worldY = value;
        }
    }
    private float detached_movementTargetX;
    private float movementTargetX
    {
        get => actionOwner != null ? actionOwner.MovementTargetXForIndependentRuntime : detached_movementTargetX;
        set
        {
            if (actionOwner != null) actionOwner.MovementTargetXForIndependentRuntime = value;
            else detached_movementTargetX = value;
        }
    }
    private float detached_movementTargetY;
    private float movementTargetY
    {
        get => actionOwner != null ? actionOwner.MovementTargetYForIndependentRuntime : detached_movementTargetY;
        set
        {
            if (actionOwner != null) actionOwner.MovementTargetYForIndependentRuntime = value;
            else detached_movementTargetY = value;
        }
    }
    private float detached_pendingTargetX;
    private float pendingTargetX
    {
        get => actionOwner != null ? actionOwner.PendingTargetXForIndependentRuntime : detached_pendingTargetX;
        set
        {
            if (actionOwner != null) actionOwner.PendingTargetXForIndependentRuntime = value;
            else detached_pendingTargetX = value;
        }
    }
    private float detached_pendingTargetY;
    private float pendingTargetY
    {
        get => actionOwner != null ? actionOwner.PendingTargetYForIndependentRuntime : detached_pendingTargetY;
        set
        {
            if (actionOwner != null) actionOwner.PendingTargetYForIndependentRuntime = value;
            else detached_pendingTargetY = value;
        }
    }
    private int movementPathIndex
    {
        get => actionOwner == null ? 0 : actionOwner.PathIndexForIndependentRuntime;
        set { if (actionOwner != null) actionOwner.PathIndexForIndependentRuntime = value; }
    }
    private int pendingTargetHops
    {
        get => actionOwner == null ? 0 : actionOwner.PendingHopsForIndependentRuntime;
        set { if (actionOwner != null) actionOwner.PendingHopsForIndependentRuntime = value; }
    }
    private bool movementActive
    {
        get => actionOwner != null && actionOwner.MoveToTargetForIndependentRuntime;
        set
        {
            if (actionOwner != null) actionOwner.MoveToTargetForIndependentRuntime = value;
        }
    }
    private float diagnosticMoveDeltaAverage;
    private ReferenceWorldSetPathOutcome lastSetPathOutcome;
    private bool lastSetPathMoveToTarget;
    private ReferenceWorldPlayerMovementStopReason lastMovementStopReason;
    private List<MapLink.MapProc> detachedTransferProcs;
    private Action<int, float, float> detachedOnTransfer;
    public List<MapLink.MapProc> transferProcs
    {
        get => actionOwner ? actionOwner.transferProcs : detachedTransferProcs;
        set { if (actionOwner) actionOwner.transferProcs = value; else detachedTransferProcs = value; }
    }
    public Action<int, float, float> onTransfer
    {
        get => actionOwner ? actionOwner.onTransfer : detachedOnTransfer;
        set { if (actionOwner) actionOwner.onTransfer = value; else detachedOnTransfer = value; }
    }
    private bool wasInsideTransfer
    {
        get => actionOwner.WasInsideTransferForIndependentRuntime;
        set => actionOwner.WasInsideTransferForIndependentRuntime = value;
    }
    private bool transferred
    {
        get => actionOwner.TransferredForIndependentRuntime;
        set => actionOwner.TransferredForIndependentRuntime = value;
    }
    private bool built;
    private bool disposed;

    public ReferenceWorldLocalPlayerDoll(
        ReferenceGsmMap map,
        ReferenceResourceLoader resources,
        Transform parent,
        WorldEntryRecord character,
        Material additiveMaterial = null)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.parent = parent == null ? throw new ArgumentNullException(nameof(parent)) : parent;
        this.character = character ?? throw new ArgumentNullException(nameof(character));
        worldX = character.X;
        worldY = character.Y;

        if (!map.Ok && !map.HasCover && !map.HasTextureBackgroundForIndependentRuntime)
        {
            throw new ArgumentException(
                "The local-player Doll requires a parsed legacy map.",
                nameof(map));
        }
    }

    public bool Built => built;
    public Transform Root => rootObject == null ? null : rootObject.transform;
    public Transform DollRoot => dollRootObject == null ? null : dollRootObject.transform;
    public SpriteRenderer MainRenderer => mainRenderer;
    public SpriteRenderer BodyRenderer => bodyRenderer;
    public ReferenceWorldBlobShadow Shadow => shadow;
    // Reading the stored octant must not evaluate CurrentFacing's display-name array.
    internal int FaceOctForIndependentRuntime => actionOwner != null
        ? actionOwner.FaceOct : currentFacing.Octant;
    public ReferenceWorldPlayerFacing CurrentFacing => actionOwner != null
        ? actionOwner.FacingForIndependentRuntime : currentFacing;
    internal PlayerController SourceControllerForIndependentRuntime => actionOwner;
    public float WorldX => worldX;
    public float WorldY => worldY;
    public int EntityId => character.EntityId;
    public CharacterAppearanceRecord Appearance => character.Appearance;
    internal bool AppearanceMatchesForIndependentRuntime(WorldEntryRecord expected) =>
        nativeAvatar != null && AppearanceModelsEqual(character.Appearance, expected.Appearance) &&
        actionOwner.AppearanceVersionMatchesForIndependentRuntime &&
        character.GlowMode == expected.GlowMode &&
        character.Head == expected.Head && character.Hair == expected.Hair;
    public bool MovementActive => movementActive;
    public int MovementPathIndex => movementPathIndex;
    public int MovementPathCount => movementPath.Count;
    public int PendingTargetHops => pendingTargetHops;
    public ReferenceWorldSetPathOutcome LastSetPathOutcome => lastSetPathOutcome;
    public bool LastSetPathMoveToTarget => lastSetPathMoveToTarget;
    public ReferenceWorldPlayerMovementStopReason LastMovementStopReason =>
        lastMovementStopReason;
    public float AnimationDistancePixels => animationDistancePixels;
    internal float MoveDeltaTimeAverage => actionOwner == null ? 0f : actionOwner.MoveDeltaAverageForIndependentRuntime;
    public float DeathElapsedSeconds => actionOwner == null ? 0f : actionOwner.DeathElapsedForIndependentRuntime;
    public int CurrentFrame => currentFrame;
    public int CurrentDeathFrame => currentDeathFrame;

    internal void UpdateDeadMovementForIndependentRuntime() =>
        actionOwner.UpdateDeadMovementForIndependentRuntime();

    internal bool CanUpdateAnimationForIndependentRuntime =>
        built && actionOwner.CanUpdateAnimationForIndependentRuntime;
    public bool ActionActive => actionState.Active;
    public ReferenceWorldPlayerActionPoseKind CurrentActionPoseKind => actionPoseKind;
    public int CurrentActionPoseIndex => currentActionPoseIndex;
    public int CurrentActionFrame => currentActionFrame;
    public int CurrentActionVariant => actionState.Variant;
    public float ActionElapsedSeconds => actionState.ElapsedSeconds;
    public float ActionFrameSeconds => actionState.FrameSeconds;
    public float ActionDurationSeconds => actionState.DurationSeconds;
    public bool ActionAttackSwing => actionState.AttackSwing;
    public IReadOnlyList<SpriteRenderer> LayerRenderers =>
        new ReadOnlyCollection<SpriteRenderer>(layerRenderers);

    public static Color32 VisibilityTint(bool invisible)
    {
        return invisible ? InvisibleTint : VisibleTint;
    }

    // Legacy opt-in injected-time probe only; never changes the live native movement clock.
    internal float EvaluateMoveDeltaTime(float deltaTime)
    {
        return ReferenceWorldPlayerMotionMath.SmoothMoveDelta(
            deltaTime,
            ref diagnosticMoveDeltaAverage);
    }

    internal ReferenceWorldPlayerStandPose Pose => pose;
    internal ReferenceWorldPlayerNormalPoses NormalPoses => normalPoses;
    internal ReferenceWorldPlayerSittingPoses SittingPoses => sittingPoses;
    internal ReferenceWorldPlayerDeathPoses DeathPoses => deathPoses;
    internal ReferenceWorldPlayerHurtPoses HurtPoses => hurtPoses;
    internal ReferenceWorldPlayerAttackPoses AttackPosesForVariant(int variant)
    {
        int normalized = ReferenceAvatarAttackPoseAssembler.NormalizeVariant(variant);
        return attackPoses[normalized];
    }
    internal ReferenceWorldPlayerPreparePoses PreparePosesForAction(int actionIndex)
    {
        int index = actionIndex - 8;
        return index >= 0 && index < preparePoses.Length
            ? preparePoses[index]
            : null;
    }
    internal ReferenceWorldPlayerMagicPoses MagicPoses => magicPoses;

#if UNITY_EDITOR
    internal static ReferenceWorldLocalPlayerDoll CreateAuditDoll(
        ReferenceGsmMap map,
        ReferenceResourceLoader resources,
        Transform parent,
        float x,
        float y,
        int gender,
        int head,
        int hair,
        int body,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face,
        int? glowMode = null,
        Material additiveMaterial = null,
        int attackVariant = 0)
    {
        WorldEntryRecord auditCharacter = new WorldEntryRecord(
            1,
            "audit",
            "audit",
            string.Empty,
            0,
            gender,
            head,
            hair,
            60,
            x,
            y,
            Array.Empty<int>(),
            new CharacterAppearanceRecord(
                body,
                cap,
                backpack,
                rightWeapon,
                leftWeapon,
                face,
                attackVariant,
                0,
                0),
            new WorldPetRecord(0, string.Empty, 0),
            null,
            glowMode,
            null,
            null,
            null);
        return new ReferenceWorldLocalPlayerDoll(
            map,
            resources,
            parent,
            auditCharacter,
            additiveMaterial);
    }
#endif

    internal Action<PlayerController> NativeControllerCreatedForIndependentRuntime { get; set; }
    internal bool CallerOwnsConstructionFailureForIndependentRuntime { get; set; }

    public bool Build()
    {
        ThrowIfDisposed();
        if (built)
        {
            return false;
        }

        BindBaseAppearanceForIndependentRuntime(character.Appearance);
        CharData.glowMode = character.GlowMode ?? 1;
        CharData.glowUnlock = character.GlowUnlock ?? 0;
        CharData.atkVariant = character.Appearance.AttackVariant;
        CharData.atkDist = character.Appearance.AttackDistance;
        CharData.race = character.Race;
        CharData.gender = character.Gender;
        // WBB-074: the doll build used to publish CharData.level (+0x20), CharData.hp (+0x58) and
        // CharData.maxHp (+0x5c) from the entry record. Those three statics have three official
        // writers - TitleFlow.MirrorSlotToCharData (0x06000efb, the roster preview),
        // TitleFlow.DoEnterWorld (0x06000efd) and the 0x8007 case of NetClient.Dispatch
        // (0x0600077f) - and the bound receive owner already performs the latter two with the
        // official short-array gates (nothing at all for a null or empty attrs array, element 0
        // into level only for a length of one to six, nothing below seven for 0x8007). A build
        // time re-projection has no official store behind it and, since
        // WorldPlayerStatusState.FromEntry turned an absent index into 0 (WBB-075 keeps the value
        // the official skip leaves there instead), it overwrote the value
        // the official skip keeps - the roster level on a null or empty array, and the hp/maxHp
        // an earlier 0x8007 had stored on a short one - with an invented 0. The owner, which runs
        // its awaited host step before the session reaches InWorld and therefore before this
        // build, stays the single writer.
        CharData.head = character.Head;
        CharData.hair = character.Hair;
        CharData.weaponCType = character.Appearance.WeaponCategory;
        try
        {
            rootObject = new GameObject("RecoveredLocalPlayer");
            rootObject.transform.SetParent(parent, false);
            rootObject.AddComponent<SpriteRenderer>();
            actionOwner = rootObject.AddComponent<PlayerController>();
            actionOwner.transferProcs = detachedTransferProcs;
            actionOwner.onTransfer = detachedOnTransfer;
            actionOwner.MovementTargetXForIndependentRuntime = detached_movementTargetX;
            actionOwner.MovementTargetYForIndependentRuntime = detached_movementTargetY;
            actionOwner.PendingTargetXForIndependentRuntime = detached_pendingTargetX;
            actionOwner.PendingTargetYForIndependentRuntime = detached_pendingTargetY;
            movementPath = actionOwner.PathForIndependentRuntime;
        }
        catch
        {
            if (!CallerOwnsConstructionFailureForIndependentRuntime) ReleaseOwnedState();
            throw;
        }

        // The existing world callback ran outside this doll's cleanup scope. Keep its
        // exception with the original Window/Bootstrap owner, including deferred transition
        // cleanup, while invoking it as soon as the native controller has been established.
        NativeControllerCreatedForIndependentRuntime?.Invoke(actionOwner);
        try
        {
            rootObject.transform.position =
                ReferenceWorldPlayerMovementStep.ProjectTransformPosition(
                worldX,
                worldY,
                map.MapWidth,
                map.MapHeight,
                PixelsPerUnit);
            mainRenderer = rootObject.GetComponent<SpriteRenderer>();

            // LoadFrames returns normally when its new avatar is !ok. The original
            // player remains in the world with its renderer disabled; art availability
            // cannot reject construction. Borrow observations only when an avatar exists.
            if (AdoptNativeAvatarForIndependentRuntime()) ObserveNativeAnimation();
            built = true;
            return true;
        }
        catch
        {
            if (!CallerOwnsConstructionFailureForIndependentRuntime) ReleaseOwnedState();
            throw;
        }
    }

    /// <summary>
    /// Observes the local appearance snapshot without repeating receipt writes or loading resources.
    /// </summary>
    public bool ApplyAppearance(
        WorldEntryRecord nextCharacter,
        float mapY,
        Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || nextCharacter == null ||
            nextCharacter.EntityId != character.EntityId ||
            nextCharacter.Gender != character.Gender)
        {
            return false;
        }

        // Observation only. The real 0x800a receiver owns CharData and the current
        // PlayerController; retaining this snapshot cannot replay a native mutation.
        character = nextCharacter;
        return true;
    }

    private bool ReloadNativeAppearanceForIndependentRuntime()
    {
        if (!actionOwner.AppearanceVersionMatchesForIndependentRuntime) actionOwner.LoadFramesForIndependentRuntime();
        if (!AdoptNativeAvatarForIndependentRuntime()) return false;
        ObserveNativeAnimation();
        return true;
    }

    private bool AdoptNativeAvatarForIndependentRuntime()
    {
        PlayerAvatar source = actionOwner.Doll;
        if (source == null) return false;
        if (nativeAvatar != null && ReferenceEquals(nativeAvatar.Source, source)) return true;
        nativeAvatar?.ForgetObservationsForIndependentRuntime();
        PlayerAvatar.Appear combined = CharData.CombinedAppear();
        CharacterAppearanceRecord raw = character.Appearance;
        var appearance = new CharacterAppearanceRecord(combined.Body, combined.Cap, combined.Backpack,
            combined.WeaponR, combined.WeaponL, combined.Face, raw.AttackVariant, raw.WeaponCategory, raw.AttackDistance);
        nativeAvatar = new ReferencePlayerAvatarRuntime(source, appearance);
        normalPoses = nativeAvatar.Normal();
        sittingPoses = nativeAvatar.Sitting();
        DisposeLazyPoseCaches();
        return true;
    }

    private static void BindBaseAppearanceForIndependentRuntime(CharacterAppearanceRecord value) =>
        CharData.SetAppearance(value.Body, value.Cap, value.Backpack, value.RightWeapon, value.LeftWeapon, value.Face);

    internal bool RefreshAppearanceVersionForIndependentRuntime(Color32 tint)
    {
        if (!built || !actionOwner) return false;
        if (!ReloadNativeAppearanceForIndependentRuntime()) return false;
        actionOwner.UpdateFrameTintForIndependentRuntime();
        return true;
    }

    /// <summary>
    /// Applies the complete recovered SetPathTo state to this production local-player owner.
    /// </summary>
    public bool TrySetPathTo(
        ReferenceWorldCollisionMask mask,
        float targetX,
        float targetY)
    {
        ThrowIfDisposed();
        if (!built) return false;
        actionOwner.SetPathToForIndependentRuntime(targetX, targetY, 0);
        ObserveNativePath();
        return true;
    }

    private void ObserveNativePath()
    {
        // Read-only legacy observations, after the original owner has made all decisions.
        lastSetPathMoveToTarget = movementActive;
        lastSetPathOutcome = pendingTargetHops > 0 ? ReferenceWorldSetPathOutcome.RetryPath :
            !movementActive ? ReferenceWorldSetPathOutcome.Stopped :
            movementPath.Count == 0 ? ReferenceWorldSetPathOutcome.DirectTarget : ReferenceWorldSetPathOutcome.PrimaryPath;
        lastMovementStopReason = movementActive ? ReferenceWorldPlayerMovementStopReason.None :
            ReferenceWorldPlayerMovementStopReason.SetPathStopped;
    }

    /// <summary>
    /// Applies the engaged-and-in-range movement stop from PlayerController.Update.
    /// </summary>
    internal void ApplyImmobileForIndependentRuntime()
    {
        movementPath.Clear();
        movementActive = false;
        movementPathIndex = 0;
        pendingTargetHops = 0;
    }

    internal bool TryApplyCombatApproachForIndependentRuntime(ReferenceWorldCollisionMask mask)
    {
        if (actionOwner.ApplyApproachForIndependentRuntime()) ObserveNativePath();
        return true;
    }

    internal bool StopCombatApproachForIndependentRuntime()
    {
        ThrowIfDisposed();
        if (!built) return false;
        movementActive = false;
        movementPath.Clear();
        return true;
    }

    public bool StopMovementForCombat()
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        actionOwner.StopMove();
        lastMovementStopReason =
            ReferenceWorldPlayerMovementStopReason.ExplicitOwnerStop;
        return true;
    }

    /// <summary>
    /// Marks the exact active-motion terminal cause before the journey owner snapshots the path.
    /// </summary>
    /// <remarks>
    /// This independent-runtime diagnostic adapter does not stop or rewrite the recovered path.
    /// The existing terminal window owner remains responsible for cancelling movement and settling
    /// every already-issued action after the immutable failure snapshot has been captured.
    /// </remarks>
    internal bool MarkActiveMovementDeadlineExpiredForIndependentRuntime()
    {
        ThrowIfDisposed();
        if (!built || !movementActive)
        {
            return false;
        }

        lastMovementStopReason =
            ReferenceWorldPlayerMovementStopReason.DeadlineExpired;
        return true;
    }

    /// <summary>
    /// Applies PlayerController.AimFace to one map-pixel target before a local attack swing.
    /// </summary>
    public bool TryAimAtWorldPixel(float targetX, float targetY)
    {
        ThrowIfDisposed();
        if (!built || rootObject == null || !IsFinite(targetX) || !IsFinite(targetY))
        {
            return false;
        }

        Vector3 targetPosition =
            ReferenceWorldPlayerMovementStep.ProjectTransformPosition(
                targetX,
                targetY,
                map.MapWidth,
                map.MapHeight,
                PixelsPerUnit);
        return TryAimAtScenePointForIndependentRuntime(targetPosition);
    }

    // Projection after native targeting has already changed the controller's facing.
    // This does not write back to the original controller or change its constructor state.
    internal void ObserveNativeFacingForIndependentRuntime()
    {
        if (actionOwner != null) currentFacing = actionOwner.FacingForIndependentRuntime;
    }

    internal bool TryAimAtScenePointForIndependentRuntime(Vector3 targetPosition)
    {
        if (!built || rootObject == null) return false;
        actionOwner.AimFace(targetPosition);
        currentFacing = actionOwner.FacingForIndependentRuntime;

        return true;
    }

    /// <summary>
    /// Advances the already source-owned SetPathTo, movement-step and UpdateAnim branches through
    /// the real local-player Transform.
    /// </summary>
    internal bool AdvanceStationaryAnimationForIndependentRuntime(float deltaTime,
        float now, Color32 tint, bool unmounted)
    {
        if (!unmounted) return true;
        bool sitting = rootObject.GetComponent<PlayerController>()?.SittingForIndependentRuntime ?? false;
        return UpdateActionAnimation(sitting, false, 0f, 0f, now, worldY, tint);
    }

    // The two array parameters are accepted but no longer written here. The official attribute
    // arrays are published once per received 0x8007 - and once from the 0x8003 enter callback for
    // the first array - by IWorldReceiveOwner.ReceivePlayerAttributes, because the official case
    // stores them on receipt: this branch is skipped by every death, mount-anchor and work frame,
    // so a per-frame re-bind missed exactly those frames and re-applied the enter root's null
    // totals over a totals array an earlier packet had already published.
    public bool TryAdvanceMovement(
        ReferenceWorldCollisionMask mask,
        IReadOnlyList<int> attributes,
        float deltaTime,
        float standTimeSeconds,
        Color32 tint,
        bool applyUnmountedAnimation,
        out ReferenceWorldPlayerMovementPlan plan,
        Func<bool> beforeTransfer = null,
        IReadOnlyList<int> attributeTotals = null)
    {
        ThrowIfDisposed();
        plan = default(ReferenceWorldPlayerMovementPlan);
        if (!built || rootObject == null ||
            !IsNonNegativeFinite(deltaTime) ||
            !IsNonNegativeFinite(standTimeSeconds))
        {
            return false;
        }

        bool wasMovementActive = movementActive;
        PlayerController owner = actionOwner;
        PlayerController.DisplacementObservation step = owner.AdvanceDisplacementForIndependentRuntime();
        if (step.Blocked) lastMovementStopReason = ReferenceWorldPlayerMovementStopReason.PixelCollisionBlocked;
        else if (wasMovementActive && !movementActive && step.PathCleared)
            lastMovementStopReason = ReferenceWorldPlayerMovementStopReason.Arrived;
        else if (movementActive) lastMovementStopReason = ReferenceWorldPlayerMovementStopReason.None;
        if (beforeTransfer != null && !beforeTransfer()) return false;

        if (owner.AdvanceTransferForIndependentRuntime())
        {
            plan = new ReferenceWorldPlayerMovementPlan(true);
            return true;
        }

        Vector3 projected = owner.ProjectMovementForIndependentRuntime();
        if (mainRenderer != null) mainRenderer.sortingOrder = owner.GroupBaseForIndependentRuntime;
        owner.PublishMovingForIndependentRuntime(step);
        plan = new ReferenceWorldPlayerMovementPlan(owner.worldX, owner.worldY, step.X, step.Y,
            owner.PathIndexForIndependentRuntime, owner.MoveToTargetForIndependentRuntime,
            step.PathCleared, step.Blocked, step.Replanned, step.Moving, owner.IsMoving,
            owner.MovementTargetXForIndependentRuntime, owner.MovementTargetYForIndependentRuntime,
            owner.PendingTargetXForIndependentRuntime, owner.PendingTargetYForIndependentRuntime,
            owner.PendingHopsForIndependentRuntime, projected);

        // Native UpdateAnim owns facing and pose selection for the mounted route.
        if (!applyUnmountedAnimation) return true;

        return UpdateActionAnimation(
            owner.SittingForIndependentRuntime,
            plan.AnimationMoving,
            plan.DeltaX,
            plan.DeltaY,
            standTimeSeconds,
            worldY,
            tint);
    }

    /// <summary>
    /// Applies the ordinary alive, unmounted PlayerController.UpdateAnim stand/walk decision.
    /// </summary>
    public bool ApplyNormalAnimation(
        float deltaX,
        float deltaY,
        bool moving,
        float mapY,
        Color32 tint)
    {
        return ApplyNormalAnimation(
            deltaX,
            deltaY,
            moving,
            0f,
            mapY,
            tint);
    }

    /// <summary>
    /// Applies the ordinary branch with the Time.time value consumed by PlayerAvatar.StandAt.
    /// </summary>
    public bool ApplyNormalAnimation(
        float deltaX,
        float deltaY,
        bool moving,
        float standTimeSeconds,
        float mapY,
        Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || normalPoses == null)
        {
            return false;
        }

        ReferenceWorldPlayerLocomotionPlan plan =
            ReferenceWorldPlayerLocomotionMath.AdvanceNormal(
                deltaX,
                deltaY,
                moving,
                animationDistancePixels,
                currentFrame,
                currentFacing);
        ReferenceWorldPlayerStandPose selected = plan.UseWalkPose
            ? normalPoses.PoseFor(
                plan.Facing.StoredDirection,
                plan.CurrentFrame,
                true)
            : normalPoses.StandAt(
                plan.Facing.StoredDirection,
                standTimeSeconds);
        if (selected == null)
        {
            currentFacing = plan.Facing;
            animationDistancePixels = plan.AnimationDistancePixels;
            currentFrame = plan.CurrentFrame;
            return true;
        }
        if (!ApplyPose(
                selected,
                plan.Facing.FlipX,
                GroupBaseForMapY(mapY, false),
                tint))
        {
            return false;
        }

        currentFacing = plan.Facing;
        animationDistancePixels = plan.AnimationDistancePixels;
        currentFrame = plan.CurrentFrame;
        pose = selected;

        return true;
    }

    /// <summary>
    /// Applies the local mounted UpdateAnim rider tail after ComposeMount selected its frame.
    /// </summary>
    /// <remarks>
    /// The first reviewed mount has no model-specific ride action, so RideActionIndex/PoseAt
    /// falls back to the ordinary stand frame zero for the folded direction. The mount owner
    /// supplies the authoritative saddle, flip and rider sorting base.
    /// </remarks>
    internal bool ApplyMountedRider(
        ReferenceMountLocalComposition composition,
        Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || normalPoses == null || dollRootObject == null ||
            composition == null)
        {
            return false;
        }

        ReferenceWorldPlayerStandPose selected = normalPoses.PoseFor(
            composition.Direction,
            0,
            false);
        if (selected == null || !ApplyPose(
                selected,
                composition.FlipX,
                composition.RiderSortingBase,
                tint))
        {
            return false;
        }

        dollRootObject.transform.localPosition = new Vector3(
            composition.RiderLocalPosition.x,
            composition.RiderLocalPosition.y,
            0f);
        int octant = ReferenceWorldPlayerMotionMath.FacingOctant(
            composition.Direction,
            composition.FlipX);
        currentFacing = ReferenceWorldPlayerMotionMath.FromOctant(octant);
        pose = selected;

        return true;
    }

    /// <summary>
    /// Restores the local Doll root offset used by PlayerController.HideMount.
    /// </summary>
    internal bool ResetMountedRiderOffset()
    {
        ThrowIfDisposed();
        if (!built || dollRootObject == null)
        {
            return false;
        }

        dollRootObject.transform.localPosition = Vector3.zero;
        return true;
    }

    /// <summary>
    /// Applies the remote mounted ApplyRiderPose tail after ComposeRemoteMount selected its frame.
    /// </summary>
    /// <remarks>
    /// The reviewed model has no model-specific ride action. RemotePlayer therefore falls back to
    /// PlayerAvatar.StandAt for the stored art direction and the current stand clock, rather than
    /// the local mounted branch's fixed stand frame zero.
    /// </remarks>
    internal bool ApplyRemoteMountedRider(
        ReferenceMountRemoteComposition composition,
        float standTimeSeconds,
        Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || normalPoses == null || dollRootObject == null ||
            composition == null)
        {
            return false;
        }

        ReferenceWorldPlayerStandPose selected = normalPoses.StandAt(
            composition.Direction,
            standTimeSeconds);
        if (selected == null || !ApplyPose(
                selected,
                composition.FlipX,
                composition.RiderSortingBase,
                tint))
        {
            return false;
        }

        dollRootObject.transform.localPosition = new Vector3(
            composition.RiderLocalPosition.x,
            composition.RiderLocalPosition.y,
            0f);
        int octant = ReferenceWorldPlayerMotionMath.FacingOctant(
            composition.Direction,
            composition.FlipX);
        currentFacing = ReferenceWorldPlayerMotionMath.FromOctant(octant);
        pose = selected;

        return true;
    }

    bool IReferenceWorldRemoteRiderDoll.ApplyRemoteMountedRider(
        ReferenceMountRemoteComposition composition,
        float standTimeSeconds,
        Color32 tint)
    {
        return ApplyRemoteMountedRider(composition, standTimeSeconds, tint);
    }

    /// <summary>
    /// Restores the ordinary stand branch after an authoritative unmount update.
    /// </summary>
    internal bool ApplyUnmountedStand(
        float standTimeSeconds,
        float mapY,
        Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || normalPoses == null || dollRootObject == null)
        {
            return false;
        }

        ReferenceWorldPlayerStandPose selected = normalPoses.StandAt(
            currentFacing.StoredDirection,
            standTimeSeconds);
        if (selected == null || !ApplyPose(
                selected,
                currentFacing.FlipX,
                GroupBaseForMapY(mapY, false),
                tint))
        {
            return false;
        }

        dollRootObject.transform.localPosition = Vector3.zero;
        animationDistancePixels = 0f;
        pose = selected;

        return true;
    }

    /// <summary>
    /// Applies the alive, unmounted, non-action sitting branch of PlayerController.UpdateAnim.
    /// </summary>
    public bool ApplySittingAnimation(float mapY, Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || sittingPoses == null)
        {
            return false;
        }

        ReferenceWorldPlayerStandPose selected = sittingPoses.PoseFor(
            currentFacing.StoredDirection);
        if (selected == null) selected = normalPoses.StandAt(currentFacing.StoredDirection, Time.time);
        animationDistancePixels = 0f;
        if (selected == null) return true;
        if (!ApplyPose(
                selected,
                currentFacing.FlipX,
                GroupBaseForMapY(mapY, false),
                tint))
        {
            return false;
        }

        animationDistancePixels = 0f;
        pose = selected;

        return true;
    }

    /// <summary>
    /// Applies the dead PlayerController.UpdateAnim timing and common ApplyPose tail.
    /// </summary>
    public bool ApplyDeathAnimation(float deltaTime, float mapY, Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || !actionOwner) return false;
        actionOwner.UpdateAnimationForIndependentRuntime(0f, 0f, false, tint);
        ObserveNativeAnimation();
        return true;
    }

    /// <summary>
    /// Captures native state solely for the retained diagnostic labels; it never drives rendering.
    /// </summary>
    private (PlayerAvatar.Pose[,] frames, PlayerAvatar.Pose first, float elapsed, float duration) CaptureActionForObservation()
    {
        PlayerAvatar.Pose[,] frames = actionOwner.ActionFramesForIndependentRuntime;
        PlayerAvatar.Pose first = frames != null && frames.Length != 0 ? frames[0, 0] : null;
        return (frames, first, actionState.ElapsedSeconds, actionState.DurationSeconds);
    }

    private bool ActionChangedForObservation(
        (PlayerAvatar.Pose[,] frames, PlayerAvatar.Pose first, float elapsed, float duration) before)
    {
        var after = CaptureActionForObservation();
        return !ReferenceEquals(before.frames, after.frames) || !ReferenceEquals(before.first, after.first) ||
            before.elapsed != after.elapsed || before.duration != after.duration;
    }

    public bool StartHurtAnimation()
    {
        ThrowIfDisposed();
        if (!built) return false;
        var before = CaptureActionForObservation();
        actionOwner.PlayHurt();
        // Remaining action labels are observation data; actual rendering reads actFrames below.
        if (ActionChangedForObservation(before) && actionState.Active && !actionState.AttackSwing)
        {
            actionVariant = 0;
            actionPoseKind = ReferenceWorldPlayerActionPoseKind.Hurt;
            currentActionPoseIndex = 11;
            currentActionFrame = 0;
        }
        return true;
    }

    /// <summary>
    /// Applies the local PlayerController.PlayDie state edge before UpdateAnim advances it.
    /// </summary>
    public bool StartDeathState()
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        actionOwner.PlayDie();
        ObserveNativeDeathPose();
        return true;
    }

    /// <summary>
    /// The frame owns the revived dead-state edge; this adapter preserves native action/death clocks.
    /// </summary>
    public bool ReviveFromDeath()
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        actionOwner.Revive();
        // Borrowed diagnostic view only; Revive clears the native array without disposing sprites.
        deathPoses = null;
        return true;
    }

    internal bool ObserveNativeLifeForIndependentRuntime(PlayerController appliedOwner)
    {
        if (disposed || !built || appliedOwner == null || !ReferenceEquals(actionOwner, appliedOwner))
            return false;
        if (appliedOwner.IsDead)
            ObserveNativeDeathPose();
        else
            deathPoses = null;
        return true;
    }

    private void ObserveNativeDeathPose()
    {
        lastMovementStopReason =
            ReferenceWorldPlayerMovementStopReason.AuthoritativeDeath;
        actionPoseKind = ReferenceWorldPlayerActionPoseKind.None;
        currentActionPoseIndex = 0;
        currentActionFrame = 0;
        PlayerAvatar.Pose[,] frames = actionOwner.DeathFramesForIndependentRuntime;
        deathPoses = frames == null ? null : new ReferenceWorldPlayerDeathPoses(
            nativeAvatar.Wrap(frames), actionOwner.DeathFramesPerDirectionForIndependentRuntime);
        currentDeathFrame = 0;
    }

    /// <summary>
    /// Lazily builds the authoritative attack variant and applies PlayAttackPose/StartActPose.
    /// </summary>
    public bool StartAttackAnimation(float swingSeconds)
    {
        ThrowIfDisposed();
        if (!built) return false;
        var before = CaptureActionForObservation();
        actionOwner.PlayAttackPoseForIndependentRuntime(swingSeconds);
        if (ActionChangedForObservation(before) && actionState.Active)
        {
            actionVariant = CharData.atkVariant == 1 ? 0 : CharData.atkVariant == 2 || CharData.atkVariant == 4 ? 1 : 2;
            actionPoseKind = ReferenceWorldPlayerActionPoseKind.Attack;
            currentActionPoseIndex = 4 + actionVariant;
            currentActionFrame = 0;
        }
        return true;
    }

    public bool StartChantAnimation(int attackVariant, float holdSeconds)
    {
        ThrowIfDisposed();
        if (!built) return false;
        var before = CaptureActionForObservation();
        actionOwner.PlayChant(holdSeconds);
        if (ActionChangedForObservation(before) && actionState.Active)
        {
            actionPoseKind = ReferenceWorldPlayerActionPoseKind.Chant;
            currentActionPoseIndex = CharData.atkVariant == 1 ? 8 : CharData.atkVariant == 2 || CharData.atkVariant == 4 ? 9 : 10;
            currentActionFrame = 0;
        }
        return true;
    }

    public bool StartCastAnimation(bool physical, Vector3? targetWorldPos = null)
    {
        ThrowIfDisposed();
        if (!built) return false;
        var before = CaptureActionForObservation();
        actionOwner.PlayCast(physical, targetWorldPos);
        currentFacing = actionOwner.FacingForIndependentRuntime;
        if (ActionChangedForObservation(before) && actionState.Active)
        {
            actionVariant = physical ? (CharData.atkVariant == 1 ? 0 : CharData.atkVariant == 2 || CharData.atkVariant == 4 ? 1 : 2) : 0;
            actionPoseKind = physical ? ReferenceWorldPlayerActionPoseKind.Attack : ReferenceWorldPlayerActionPoseKind.Magic;
            currentActionPoseIndex = physical ? 4 + actionVariant : 7;
            currentActionFrame = 0;
        }
        return true;
    }

    /// <summary>
    /// Applies the alive, unmounted action branch of PlayerController.UpdateAnim.
    /// </summary>
    /// <remarks>
    /// The retained deltaTime argument is a compatibility seam; ActPoseNow reads Time.deltaTime.
    /// An ended or missing action pose continues into the sitting/normal branch in this call.
    /// </remarks>
    public bool UpdateActionAnimation(
        float deltaTime,
        bool sitting,
        bool moving,
        float deltaX,
        float deltaY,
        float mapY,
        Color32 tint)
    {
        return UpdateActionAnimation(sitting, moving, deltaX, deltaY, Time.time, mapY, tint);
    }

    private bool UpdateActionAnimation(
        bool sitting, bool moving, float deltaX, float deltaY,
        float standTimeSeconds, float mapY, Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || !actionOwner) return false;
        actionOwner.UpdateAnimationForIndependentRuntime(deltaX, deltaY, moving, tint);
        ObserveNativeAnimation();
        return true;
    }

    internal void AdvanceWorkForIndependentRuntime(Color32 tint, System.Action<PlayerController> confirmation)
    {
        actionOwner.AdvanceWorkForIndependentRuntime(tint, confirmation);
        ObserveNativeAnimation();
    }

    internal bool AdvanceNativeMountForIndependentRuntime(bool moving, float dx, float dy, Color32 tint)
    {
        if (!built || !actionOwner) return false;
        actionOwner.UpdateRidingForIndependentRuntime(dx, dy, moving, tint);
        ObserveNativeAnimation();
        return true;
    }

    internal void BindNativeRidingForIndependentRuntime(WorldRidingState value)
    {
        CharData.riding = value != null && value.Riding;
        CharData.mountModel = value == null ? 0 : value.MountModel;
        CharData.riderSeat = value == null ? 0 : value.RiderSeat;
        CharData.rideAnchorId = value == null ? 0 : value.RideAnchorId;
        CharData.mountTianmo = value != null && value.MountTianmo;
        CharData.rideSeats = value == null ? 0 : value.RideSeats;
    }

    internal bool ObserveNativeFrameForIndependentRuntime()
    {
        ThrowIfDisposed();
        if (!built || !actionOwner) return false;
        // LoadFrames may return normally without an avatar. Keep observing the real
        // controller instead of turning unavailable pose observations into world failure.
        if (AdoptNativeAvatarForIndependentRuntime()) ObserveNativeAnimation();
        ObserveNativePath();
        return true;
    }

    private void ObserveNativeAnimation()
    {
        currentFacing = actionOwner.FacingForIndependentRuntime;
        currentFrame = actionOwner.AnimationFrameForIndependentRuntime;
        animationDistancePixels = actionOwner.AnimationDistanceForIndependentRuntime;
        if (actionState.Active) currentActionFrame = actionOwner.ActionFrameForIndependentRuntime;
        else actionPoseKind = ReferenceWorldPlayerActionPoseKind.None;
        if (actionOwner.DeathFramesForIndependentRuntime != null)
            currentDeathFrame = actionOwner.DeathFrameForIndependentRuntime;
        PlayerAvatar.Pose nativePose = actionOwner.AppliedPoseForIndependentRuntime;
        pose = nativePose == null || nativePose.layers == null ? null : nativeAvatar.Wrap(nativePose);
        ObserveNativeRenderers();
    }

    public static int GroupBaseForMapY(float mapY, bool isoYMaterial)
    {
        if (isoYMaterial)
        {
            return 0;
        }

        if (float.IsNaN(mapY) || float.IsInfinity(mapY))
        {
            return 1;
        }

        int truncated = (int)mapY;
        int clamped = Math.Max(0, Math.Min(30000, truncated));
        return clamped + 1;
    }

    public void Dispose() => Dispose(preserveNativePlayerState: false);

    internal void Dispose(bool preserveNativePlayerState)
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseOwnedState(preserveNativePlayerState);
    }

    private bool ApplyPose(
        ReferenceWorldPlayerStandPose value,
        bool flip,
        int baseSortingOrder,
        Color32 tint)
    {
        if (!actionOwner) return false;
        actionOwner.ApplyPoseForIndependentRuntime(value?.OriginalPose, flip, baseSortingOrder, tint);
        ObserveNativeRenderers();
        return true;
    }

    private void ObserveNativeRenderers()
    {
        Transform nativeRoot = actionOwner.DollRootForIndependentRuntime;
        dollRootObject = nativeRoot ? nativeRoot.gameObject : null;
        bodyRenderer = actionOwner.DollBodyForIndependentRuntime;
        shadow = actionOwner.ShadowForIndependentRuntime;
        layerRenderers.Clear();
        SpriteRenderer[] nativeLayers = actionOwner.DollRenderersForIndependentRuntime;
        if (nativeLayers != null) layerRenderers.AddRange(nativeLayers);
    }

    private static bool AppearanceModelsEqual(
        CharacterAppearanceRecord left,
        CharacterAppearanceRecord right)
    {
        return left != null && right != null &&
               left.Body == right.Body &&
               left.Cap == right.Cap &&
               left.Backpack == right.Backpack &&
               left.RightWeapon == right.RightWeapon &&
               left.LeftWeapon == right.LeftWeapon &&
               left.Face == right.Face;
    }

    private void DisposeLazyPoseCaches()
    {
        if (hurtPoses != null)
        {
            hurtPoses.Dispose();
            hurtPoses = null;
        }

        hurtPosesBuilt = false;
        for (int variant = 0; variant < attackPoses.Length; variant++)
        {
            DisposeCandidate(attackPoses[variant]);
            attackPoses[variant] = null;
            attackPosesBuilt[variant] = false;
        }

        for (int index = 0; index < preparePoses.Length; index++)
        {
            DisposeCandidate(preparePoses[index]);
            preparePoses[index] = null;
            preparePosesBuilt[index] = false;
        }

        DisposeCandidate(magicPoses);
        magicPoses = null;
        magicPosesBuilt = false;
    }

    private static void DisposeCandidate(ReferenceWorldPlayerAttackPoses value)
    {
        value?.Dispose();
    }

    private static void DisposeCandidate(ReferenceWorldPlayerPreparePoses value)
    {
        value?.Dispose();
    }

    private static void DisposeCandidate(ReferenceWorldPlayerMagicPoses value)
    {
        value?.Dispose();
    }

    private static void DisposeCandidate(ReferenceWorldPlayerHurtPoses value)
    {
        value?.Dispose();
    }

    private void ReleaseOwnedState(bool preserveNativePlayerState = false)
    {
        // TransitionTo owns the deferred world-root Destroy. Retiring this borrower
        // cannot release the live controller's art or mutate its movement state first.
        if (!preserveNativePlayerState && !ReferenceEquals(actionOwner, null))
        {
            actionOwner.ReleaseMountLeaseForIndependentRuntime();
            actionOwner.ReleaseAvatarForIndependentRuntime();
        }
        if (!preserveNativePlayerState)
        {
            DestroyOwned(rootObject);
            rootObject = null;
        }
        nativeAvatar?.ForgetObservationsForIndependentRuntime();
        nativeAvatar = null;
        dollRootObject = null;
        mainRenderer = null;
        bodyRenderer = null;
        shadow = null;
        layerRenderers.Clear();

        if (normalPoses != null)
        {
            normalPoses.Dispose();
            normalPoses = null;
        }

        if (deathPoses != null)
        {
            deathPoses.Dispose();
            deathPoses = null;
        }

        if (hurtPoses != null)
        {
            hurtPoses.Dispose();
            hurtPoses = null;
        }

        hurtPosesBuilt = false;

        if (sittingPoses != null)
        {
            sittingPoses.Dispose();
            sittingPoses = null;
        }

        for (int variant = 0; variant < attackPoses.Length; variant++)
        {
            if (attackPoses[variant] != null)
            {
                attackPoses[variant].Dispose();
                attackPoses[variant] = null;
            }

            attackPosesBuilt[variant] = false;
        }

        for (int index = 0; index < preparePoses.Length; index++)
        {
            if (preparePoses[index] != null)
            {
                preparePoses[index].Dispose();
                preparePoses[index] = null;
            }

            preparePosesBuilt[index] = false;
        }

        if (magicPoses != null)
        {
            magicPoses.Dispose();
            magicPoses = null;
        }

        magicPosesBuilt = false;

        pose = null;
        if (!preserveNativePlayerState)
        {
            movementPath.Clear();
            worldX = character.X;
            worldY = character.Y;
            movementTargetX = 0f;
            movementTargetY = 0f;
            pendingTargetX = 0f;
            pendingTargetY = 0f;
            movementPathIndex = 0;
            pendingTargetHops = 0;
            movementActive = false;
        }
        diagnosticMoveDeltaAverage = 0f;
        lastSetPathOutcome = ReferenceWorldSetPathOutcome.None;
        lastSetPathMoveToTarget = false;
        lastMovementStopReason = ReferenceWorldPlayerMovementStopReason.None;
        currentFacing = ReferenceWorldPlayerMotionMath.FromOctant(3);
        animationDistancePixels = 0f;
        currentFrame = 0;
        currentDeathFrame = 0;
        if (!preserveNativePlayerState) actionOwner = null;
        actionVariant = 0;
        actionPoseKind = ReferenceWorldPlayerActionPoseKind.None;
        currentActionPoseIndex = 0;
        currentActionFrame = 0;
        built = false;
    }

    private static bool IsNonNegativeFinite(float value)
    {
        return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldLocalPlayerDoll));
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
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
