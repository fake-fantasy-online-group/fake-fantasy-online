using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-layer poses produced by the normal stand/run cone of PlayerAvatar.FramesOf.
/// </summary>
internal sealed class ReferenceAvatarNormalSourcePoses
{
    public ReferenceAvatarNormalSourcePoses(
        ReferenceAvatarStandSourcePose[][] standAnimation,
        int standBodyFramesPerDirection,
        int standWingFramesPerDirection,
        ReferenceAvatarStandSourcePose[,] walk)
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

        Stand = new ReferenceAvatarStandSourcePose[standAnimation.Length];
        for (int direction = 0; direction < standAnimation.Length; direction++)
        {
            ReferenceAvatarStandSourcePose[] row = standAnimation[direction];
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
        int headPixels = 0;
        foreach (ReferenceAvatarStandSourcePose[] direction in StandAnimation)
        {
            if (direction == null)
            {
                continue;
            }

            foreach (ReferenceAvatarStandSourcePose pose in direction)
            {
                headPixels = Math.Max(headPixels, HeadPixelsOf(pose));
            }
        }

        for (int direction = 0; direction < Walk.GetLength(0); direction++)
        {
            for (int frame = 0; frame < Walk.GetLength(1); frame++)
            {
                headPixels = Math.Max(
                    headPixels,
                    HeadPixelsOf(Walk[direction, frame]));
            }
        }

        HeadPixels = headPixels;
    }

    public ReferenceAvatarStandSourcePose[] Stand { get; }
    public ReferenceAvatarStandSourcePose[][] StandAnimation { get; }
    public int StandBodyFramesPerDirection { get; }
    public int StandWingFramesPerDirection { get; }
    public ReferenceAvatarStandSourcePose[,] Walk { get; }
    public int HeadPixels { get; }

    private static int HeadPixelsOf(ReferenceAvatarStandSourcePose pose)
    {
        if (pose == null)
        {
            return 0;
        }

        int pixels = 0;
        foreach (ReferenceAvatarStandSourceLayer layer in pose.Layers)
        {
            if (layer != null && layer.Tag == 3)
            {
                pixels = Math.Max(pixels, -layer.OriginY);
            }
        }

        return pixels;
    }
}

/// <summary>
/// Source translation of PlayerAvatar.Build/FramesOf for stand and 5x8 run.
/// </summary>
/// <remarks>
/// SuitFxDB supplies the complete animated weapon/backpack membership. Every server-authored
/// equipped part is loaded through its source candidate order; a present paired glow source is
/// composed without a second product allow-list.
/// The body-only 1.0.1 work costumes 950..953 are admitted for RemotePlayer.UpdateIdle; their
/// zero-binding body art deliberately suppresses head/equipment composition. The additive material
/// remains owned by the adjacent recovered GlowLayerMat boundary.
/// </remarks>
internal static class ReferenceAvatarNormalPoseAssembler
{
    private const int DirectionCount = 5;
    private const int WalkFramesPerDirection = 8;
    private const int StandFileAction = 1;
    private const int WalkFileAction = 2;
    private const int StandPoseIndex = 0;
    private const int RunPoseIndex = 1;

    // SuitFxDB..cctor (06000C40) owns these two exact read-only model sets.
    private static readonly HashSet<int> StandAnimatedWeaponModels = new HashSet<int>
    {
        1271, 1273, 1280, 1281, 1282, 1283, 1284, 1285, 1300, 1302,
        1351, 1352, 1362, 1363, 1364, 1368, 1369, 1370, 1371, 1372,
        1376, 1377, 1378, 1381, 1388, 1389, 1390, 1392, 1393, 1394,
        1395, 1396, 1397, 1399, 1400, 1401, 1402, 1403, 1404, 1405,
        1406, 1407, 1422, 1423, 1424, 1425, 1700, 1702, 1704, 1718,
        1719, 1720, 1721, 1722, 1723, 2298, 2299
    };

    private static readonly HashSet<int> StandAnimatedBackpackModels = new HashSet<int>
    {
        884, 885, 886, 942
    };

    public static bool TryAssemble(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        out ReferenceAvatarNormalSourcePoses poses)
    {
        poses = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded)
        {
            return false;
        }

        byte[] standBody;
        byte[] walkBody;
        bool bodyOnly = IsSpecialWorkBody(assets.BodyModel);
        byte[] head = bodyOnly ? null : resources.ReadRes(assets.HeadPath);
        if (!TryReadFirst(resources, assets.BodyCandidatePaths(StandFileAction), out standBody) ||
            !TryReadFirst(resources, assets.BodyCandidatePaths(WalkFileAction), out walkBody) ||
            DimgDecoder.FrameCount(standBody) < DirectionCount ||
            DimgDecoder.FrameCount(walkBody) < DirectionCount * WalkFramesPerDirection ||
            (!bodyOnly && DimgDecoder.FrameCount(head) < DirectionCount))
        {
            return false;
        }

        if (bodyOnly)
        {
            DimgFrame standFrame;
            DimgFrame walkFrame;
            if (DimgDecoder.Version(standBody) != ReferenceGsoDecoder.Version101 ||
                DimgDecoder.Version(walkBody) != ReferenceGsoDecoder.Version101 ||
                DimgDecoder.DirectionCount(standBody) != DirectionCount ||
                DimgDecoder.DirectionCount(walkBody) != DirectionCount ||
                DimgDecoder.FramesPerDir(standBody) < 2 ||
                DimgDecoder.FramesPerDir(walkBody) != WalkFramesPerDirection ||
                !DimgDecoder.TryDecodeFrame(standBody, 0, out standFrame) ||
                !DimgDecoder.TryDecodeFrame(walkBody, 0, out walkFrame) ||
                standFrame.Bindings.Length != 0 || walkFrame.Bindings.Length != 0)
            {
                return false;
            }
        }

        List<PartData> parts = new List<PartData>(assets.Parts.Length);
        if (!bodyOnly)
        {
            foreach (ReferenceAvatarPart part in assets.Parts)
            {
                byte[] standPart;
                byte[] walkPart;
                if (!TryReadFirst(
                        resources,
                        part.CandidatePathsForAction(StandFileAction),
                        out standPart) ||
                    !TryReadFirst(
                        resources,
                        part.CandidatePathsForAction(WalkFileAction),
                        out walkPart))
                {
                    return false;
                }

                byte[] glowStandPart = null;
                byte[] glowWalkPart = null;
                if (part.Tag == 5 || part.Tag == 6)
                {
                    TryReadFirst(
                        resources,
                        part.GlowCandidatePathsForAction(StandFileAction),
                        out glowStandPart);
                    TryReadFirst(
                        resources,
                        part.GlowCandidatePathsForAction(WalkFileAction),
                        out glowWalkPart);
                }

                bool hasGlowStand = glowStandPart != null;
                bool hasGlowWalk = glowWalkPart != null;
                if (hasGlowStand != hasGlowWalk)
                {
                    return false;
                }

                parts.Add(
                    new PartData(
                        part.Tag,
                        part.Model,
                        standPart,
                        walkPart,
                        glowStandPart,
                        glowWalkPart));
            }
        }

        int standBodySourceFramesPerDirection =
            DimgDecoder.FramesPerDir(standBody);
        int standBodyFramesPerDirection = CalculateStandBodyFramesPerDirection(
            standBody,
            parts);
        int standWingFramesPerDirection = CalculateStandWingFramesPerDirection(parts);
        int standFrameCount = checked(
            standBodyFramesPerDirection * standWingFramesPerDirection);
        ReferenceAvatarStandSourcePose[][] standAnimation =
            new ReferenceAvatarStandSourcePose[DirectionCount][];
        ReferenceAvatarStandSourcePose[,] walk =
            new ReferenceAvatarStandSourcePose[DirectionCount, WalkFramesPerDirection];
        for (int direction = 0; direction < DirectionCount; direction++)
        {
            standAnimation[direction] =
                new ReferenceAvatarStandSourcePose[standFrameCount];
            for (int combinedFrame = 0;
                 combinedFrame < standFrameCount;
                 combinedFrame++)
            {
                int bodyFrame = combinedFrame / standWingFramesPerDirection;
                int wingFrame = combinedFrame % standWingFramesPerDirection;
                if (!TryAssemblePose(
                        standBody,
                        head,
                        parts,
                        avatarDepth,
                        assets,
                        direction,
                        bodyFrame,
                        wingFrame,
                        standWingFramesPerDirection,
                        standBodySourceFramesPerDirection,
                        false,
                        combinedFrame,
                        true,
                        true,
                        out standAnimation[direction][combinedFrame]))
                {
                    return false;
                }
            }

            for (int frame = 0; frame < WalkFramesPerDirection; frame++)
            {
                if (!TryAssemblePose(
                        walkBody,
                        head,
                        parts,
                        avatarDepth,
                        assets,
                        direction,
                        frame,
                        frame,
                        1,
                        WalkFramesPerDirection,
                        true,
                        0,
                        true,
                        true,
                        out walk[direction, frame]))
                {
                    return false;
                }
            }
        }

        poses = new ReferenceAvatarNormalSourcePoses(
            standAnimation,
            standBodyFramesPerDirection,
            standWingFramesPerDirection,
            walk);
        return true;
    }

    internal static int PartFrameIndex(
        int tag,
        int direction,
        int partFramesPerDirection,
        int bodyFrame,
        int wingFrame,
        int wingSplit,
        bool partsFrameAdvance,
        bool wingFrameAdvance)
    {
        if (direction < 0 || direction >= DirectionCount ||
            partFramesPerDirection < 1 || bodyFrame < 0 || wingFrame < 0 ||
            wingSplit < 1)
        {
            throw new ArgumentOutOfRangeException();
        }

        if (partFramesPerDirection < 2)
        {
            return direction;
        }

        int selectedFrame = bodyFrame;
        if (tag == 4)
        {
            if (wingSplit < 2)
            {
                wingFrame = bodyFrame;
            }

            selectedFrame = wingFrameAdvance ? wingFrame : 0;
        }
        else if (!partsFrameAdvance && (tag == 5 || tag == 6))
        {
            selectedFrame = 0;
        }

        return selectedFrame % partFramesPerDirection +
               partFramesPerDirection * direction;
    }

    internal static bool IsStandAnimatedPart(int tag, int model)
    {
        return (tag == 4 && StandAnimatedBackpackModels.Contains(model)) ||
               ((tag == 5 || tag == 6) && StandAnimatedWeaponModels.Contains(model));
    }

    internal static bool IsSpecialWorkBody(int bodyModel)
    {
        return bodyModel >= 950 && bodyModel <= 953;
    }

    private static int CalculateStandBodyFramesPerDirection(
        byte[] standBody,
        IEnumerable<PartData> parts)
    {
        int frames = CappedStandFramesPerDirection(standBody);
        foreach (PartData part in parts)
        {
            bool contributes = part.Tag == 1 || part.Tag == 2 ||
                ((part.Tag == 5 || part.Tag == 6) &&
                 StandAnimatedWeaponModels.Contains(part.Model));
            if (contributes)
            {
                frames = Math.Max(
                    frames,
                    CappedStandFramesPerDirection(part.StandBytes));
                if (part.GlowStandBytes != null)
                {
                    frames = Math.Max(
                        frames,
                        CappedStandFramesPerDirection(part.GlowStandBytes));
                }
            }
        }

        return frames;
    }

    private static int CalculateStandWingFramesPerDirection(IEnumerable<PartData> parts)
    {
        foreach (PartData part in parts)
        {
            if (part.Tag == 4 && StandAnimatedBackpackModels.Contains(part.Model))
            {
                return CappedStandFramesPerDirection(part.StandBytes);
            }
        }

        return 1;
    }

    private static int CappedStandFramesPerDirection(byte[] bytes)
    {
        return Math.Max(1, Math.Min(16, DimgDecoder.FramesPerDir(bytes)));
    }

    private static bool TryAssemblePose(
        byte[] bodyBytes,
        byte[] headBytes,
        IList<PartData> parts,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        int direction,
        int bodyFrame,
        int wingFrame,
        int wingSplit,
        int bodyStride,
        bool walk,
        int depthFrame,
        bool partsFrameAdvance,
        bool wingFrameAdvance,
        out ReferenceAvatarStandSourcePose pose)
    {
        pose = null;
        if (direction < 0 || direction >= DirectionCount ||
            bodyFrame < 0 || wingFrame < 0 || wingSplit < 1 ||
            bodyStride < 1 || depthFrame < 0)
        {
            return false;
        }

        int bodyFrameIndex =
            direction * bodyStride + bodyFrame % bodyStride;
        DimgFrame body;
        if (!DimgDecoder.TryDecodeFrame(bodyBytes, bodyFrameIndex, out body))
        {
            return false;
        }

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
                    false)
            };

        if (IsSpecialWorkBody(assets.BodyModel))
        {
            if (body.Bindings.Length != 0 || parts.Count != 0)
            {
                return false;
            }

            pose = new ReferenceAvatarStandSourcePose(layers, body.BoundWidth);
            return true;
        }

        DimgFrame head;
        if (body.Bindings.Length < 1 ||
            !DimgDecoder.TryDecodeFrame(headBytes, direction, out head))
        {
            return false;
        }

        ReferenceAvatarStandAssembler.Recolor(head.Pixels, assets.HairColor);
        layers.Add(
            new ReferenceAvatarStandSourceLayer(
                3,
                head,
                (body.Bindings[0].X - head.CenterX) - bodyCenterX,
                (body.Bindings[0].Y - head.CenterY) - bodyCenterY,
                head.CenterX,
                head.CenterY,
                false));

        foreach (PartData part in parts)
        {
            byte[] partBytes = walk ? part.WalkBytes : part.StandBytes;
            int partFramesPerDirection = DimgDecoder.FramesPerDir(partBytes);
            int partFrameIndex = PartFrameIndex(
                part.Tag,
                direction,
                partFramesPerDirection,
                bodyFrame,
                wingFrame,
                wingSplit,
                partsFrameAdvance,
                wingFrameAdvance);
            DimgFrame partFrame;
            int bindingIndex = ReferenceAvatarStandAssembler.BindingIndexForTag(part.Tag);
            if (!DimgDecoder.TryDecodeFrame(partBytes, partFrameIndex, out partFrame) ||
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

        int poseIndex = walk ? RunPoseIndex : StandPoseIndex;
        int screenDirection = direction;
        layers.Sort(
            delegate(
                ReferenceAvatarStandSourceLayer left,
                ReferenceAvatarStandSourceLayer right)
            {
                return avatarDepth.ComparePlayerLayerTags(
                    left.Tag,
                    right.Tag,
                    poseIndex,
                    direction,
                    depthFrame,
                    screenDirection);
            });

        foreach (PartData part in parts)
        {
            byte[] glowBytes = walk ? part.GlowWalkBytes : part.GlowStandBytes;
            if (glowBytes == null)
            {
                continue;
            }

            int glowFramesPerDirection = DimgDecoder.FramesPerDir(glowBytes);
            int glowFrameIndex = PartFrameIndex(
                part.Tag,
                direction,
                glowFramesPerDirection,
                bodyFrame,
                wingFrame,
                wingSplit,
                partsFrameAdvance,
                wingFrameAdvance);
            DimgFrame glowFrame;
            int bindingIndex = ReferenceAvatarStandAssembler.BindingIndexForTag(part.Tag);
            if (!DimgDecoder.TryDecodeFrame(glowBytes, glowFrameIndex, out glowFrame) ||
                bindingIndex < 0 || bindingIndex >= body.Bindings.Length)
            {
                return false;
            }

            DimgBinding binding = body.Bindings[bindingIndex];
            ReferenceAvatarStandSourceLayer glowLayer =
                new ReferenceAvatarStandSourceLayer(
                    part.Tag,
                    glowFrame,
                    (binding.X - glowFrame.CenterX) - bodyCenterX,
                    (binding.Y - glowFrame.CenterY) - bodyCenterY,
                    glowFrame.CenterX,
                    glowFrame.CenterY,
                    true);
            MergeGlowLayer(layers, glowLayer);
        }

        pose = new ReferenceAvatarStandSourcePose(layers, body.BoundWidth);
        return true;
    }

    private static void MergeGlowLayer(
        IList<ReferenceAvatarStandSourceLayer> main,
        ReferenceAvatarStandSourceLayer glow)
    {
        int insertAt = -1;
        for (int index = main.Count - 1; index >= 0; index--)
        {
            ReferenceAvatarStandSourceLayer layer = main[index];
            if (layer.Tag == glow.Tag && !layer.Additive)
            {
                insertAt = index + 1;
                break;
            }
        }

        if (insertAt < 0)
        {
            main.Add(glow);
        }
        else
        {
            main.Insert(insertAt, glow);
        }
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
        public PartData(
            int tag,
            int model,
            byte[] standBytes,
            byte[] walkBytes,
            byte[] glowStandBytes,
            byte[] glowWalkBytes)
        {
            Tag = tag;
            Model = model;
            StandBytes = standBytes ?? throw new ArgumentNullException(nameof(standBytes));
            WalkBytes = walkBytes ?? throw new ArgumentNullException(nameof(walkBytes));
            GlowStandBytes = glowStandBytes;
            GlowWalkBytes = glowWalkBytes;
        }

        public int Tag { get; }
        public int Model { get; }
        public byte[] StandBytes { get; }
        public byte[] WalkBytes { get; }
        public byte[] GlowStandBytes { get; }
        public byte[] GlowWalkBytes { get; }
    }
}

}
