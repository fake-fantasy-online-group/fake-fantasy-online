using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One source image in the recovered stand-direction-zero/frame-zero PlayerAvatar pose.
/// </summary>
internal sealed class ReferenceAvatarStandSourceLayer
{
    public ReferenceAvatarStandSourceLayer(
        int tag,
        DimgFrame frame,
        int originX,
        int originY,
        int anchorX,
        int anchorY,
        bool additive)
    {
        Tag = tag;
        Frame = frame ?? throw new ArgumentNullException(nameof(frame));
        OriginX = originX;
        OriginY = originY;
        AnchorX = anchorX;
        AnchorY = anchorY;
        Additive = additive;
    }

    public int Tag { get; }
    public DimgFrame Frame { get; }
    public int OriginX { get; }
    public int OriginY { get; }
    public int AnchorX { get; }
    public int AnchorY { get; }
    public bool Additive { get; }
}

/// <summary>
/// Managed source pose shared by the title portrait and world Doll paths.
/// </summary>
internal sealed class ReferenceAvatarStandSourcePose
{
    public ReferenceAvatarStandSourcePose(
        IList<ReferenceAvatarStandSourceLayer> layers,
        int bodyBoundWidth)
    {
        if (layers == null)
        {
            throw new ArgumentNullException(nameof(layers));
        }

        Layers = new ReadOnlyCollection<ReferenceAvatarStandSourceLayer>(
            new List<ReferenceAvatarStandSourceLayer>(layers));
        BodyBoundWidth = bodyBoundWidth;
    }

    public IReadOnlyList<ReferenceAvatarStandSourceLayer> Layers { get; }
    public int BodyBoundWidth { get; }
}

/// <summary>
/// Shared translation of the reviewed PlayerAvatar.FramesOf stand cone.
/// </summary>
internal static class ReferenceAvatarStandAssembler
{
    private const int StandPoseIndex = 0;
    private const int StandScreenDirection = 0;
    private const int StandFrame = 0;

    public static bool TryAssemble(
        ReferenceResourceLoader resources,
        AvatarDepthTable avatarDepth,
        ReferenceAvatarAssets assets,
        out ReferenceAvatarStandSourcePose pose)
    {
        pose = null;
        if (resources == null || avatarDepth == null || !avatarDepth.Loaded)
        {
            return false;
        }

        DimgFrame body;
        DimgFrame head;
        if (!DimgDecoder.TryDecodeFrame(resources.ReadRes(assets.BodyPath), 0, out body) ||
            !DimgDecoder.TryDecodeFrame(resources.ReadRes(assets.HeadPath), 0, out head))
        {
            return false;
        }

        Recolor(head.Pixels, assets.HairColor);

        int bodyCenterX = body.HasBind ? body.CenterX : body.BoundCenterX - body.Left;
        int bodyCenterY = body.HasBind ? body.CenterY : body.BoundCenterY - body.Top;
        if (body.Bindings.Length < 1)
        {
            return false;
        }

        List<ReferenceAvatarStandSourceLayer> layers =
            new List<ReferenceAvatarStandSourceLayer>(2 + assets.Parts.Length)
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

        foreach (ReferenceAvatarPart part in assets.Parts)
        {
            DimgFrame partFrame;
            if (!TryDecodePart(resources, part, out partFrame))
            {
                return false;
            }

            int bindingIndex = BindingIndexForTag(part.Tag);
            if (bindingIndex < 0 || bindingIndex >= body.Bindings.Length)
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

        int artDirection = avatarDepth.ArtDirection(StandScreenDirection);
        layers.Sort(
            delegate(
                ReferenceAvatarStandSourceLayer left,
                ReferenceAvatarStandSourceLayer right)
            {
                return avatarDepth.ComparePlayerLayerTags(
                    left.Tag,
                    right.Tag,
                    StandPoseIndex,
                    artDirection,
                    StandFrame,
                    StandScreenDirection);
            });

        pose = new ReferenceAvatarStandSourcePose(layers, body.BoundWidth);
        return true;
    }

    private static bool TryDecodePart(
        ReferenceResourceLoader resources,
        ReferenceAvatarPart part,
        out DimgFrame frame)
    {
        foreach (string candidate in part.CandidatePaths)
        {
            if (DimgDecoder.TryDecodeFrame(resources.ReadRes(candidate), 0, out frame))
            {
                return true;
            }
        }

        frame = null;
        return false;
    }

    internal static int BindingIndexForTag(int tag)
    {
        switch (tag)
        {
            case 1:
            case 2:
                return 0;
            case 4:
                return 1;
            case 5:
                return 2;
            case 6:
                return 3;
            default:
                return -1;
        }
    }

    internal static void Recolor(Color32[] pixels, Color32 target)
    {
        Color targetColor = target;
        float targetHue;
        float targetSaturation;
        float targetValue;
        Color.RGBToHSV(targetColor, out targetHue, out targetSaturation, out targetValue);
        float saturation = Mathf.Min(1f, targetSaturation + 0.1f);
        float valueBlend = Mathf.Clamp01(1f - targetSaturation * 3f);

        for (int index = 0; index < pixels.Length; index++)
        {
            Color32 pixel = pixels[index];
            if (pixel.a <= 29 || IsSkin(pixel.r, pixel.g, pixel.b))
            {
                continue;
            }

            float sourceHue;
            float sourceSaturation;
            float sourceValue;
            Color.RGBToHSV(pixel, out sourceHue, out sourceSaturation, out sourceValue);
            float shiftedTargetValue = Mathf.Clamp01(
                targetValue + (sourceValue - 0.5f) * 0.45f);
            float value = sourceValue + valueBlend * (shiftedTargetValue - sourceValue);
            Color recolored = Color.HSVToRGB(targetHue, saturation, value);
            pixels[index] = new Color32(
                (byte)(int)(recolored.r * 255f),
                (byte)(int)(recolored.g * 255f),
                (byte)(int)(recolored.b * 255f),
                pixel.a);
        }
    }

    private static bool IsSkin(int red, int green, int blue)
    {
        return red > 150 &&
               green <= red &&
               blue <= green &&
               red - blue < 110 &&
               blue > 70 &&
               green - blue < 70;
    }
}

}
