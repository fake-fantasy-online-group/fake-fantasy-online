using System;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Redacted, opt-in source-entry observer for the fixed official-1.5.8 local-avatar slice.
/// </summary>
internal static class ReferenceLocalAvatarSourceCheck
{
    public const string Argument = "--ffo-local-avatar-source-check-1-5-8";
    public const string Marker = "FFO_LOCAL_AVATAR_SOURCE_CHECK_1_5_8_V1";
    public const string ManualArgument =
        "--ffo-local-avatar-manual-check-1-5-8";
    public const string ManualMarker =
        "FFO_LOCAL_AVATAR_MANUAL_CHECK_1_5_8_V1";
    public const int ExpectedMapId = 6;
    public const int ExpectedX = 10701;
    public const int ExpectedY = 3096;
    public const int ExpectedBodyModel = 142;
    public const int ExpectedHead = 2;
    public const int ExpectedHair = 1;
    public const int ExpectedLayerCount = 2;

    public static bool IsRequested(string[] arguments)
    {
        if (arguments == null)
        {
            return false;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], Argument, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static bool IsManualRequested(string[] arguments)
    {
        if (arguments == null)
        {
            return false;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(
                    arguments[index],
                    ManualArgument,
                    StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    public static string ManualReadyLine(int mapId)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} READY state=InWorld map={1} canvas=800x600 input=manual-only",
            ManualMarker,
            mapId);
    }

    public static string ManualStageLine(ReferenceWorldLocalPlayerDoll player)
    {
        if (player == null)
        {
            return ManualMarker + " STAGE unavailable";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} STAGE moving={1} facing={2} stored={3} flip={4} frame={5}",
            ManualMarker,
            player.MovementActive ? 1 : 0,
            player.CurrentFacing.Octant,
            player.CurrentFacing.StoredDirection,
            player.CurrentFacing.FlipX ? 1 : 0,
            player.CurrentFrame);
    }

    public static bool TryVerify(
        ReferenceFirstWorldWindowOwner window,
        WorldEntryRecord world,
        out string result)
    {
        result = FailureLine("world.unavailable");
        if (window == null || !window.Built || window.Frame == null || world == null)
        {
            return false;
        }

        CharacterAppearanceRecord appearance = world.Appearance;
        if (world.MapId != ExpectedMapId ||
            Mathf.RoundToInt(world.X) != ExpectedX ||
            Mathf.RoundToInt(world.Y) != ExpectedY ||
            world.Gender != 0 || world.Head != ExpectedHead || world.Hair != ExpectedHair ||
            appearance == null || appearance.Body != 0 || appearance.Cap != 0 ||
            appearance.Backpack != 0 || appearance.RightWeapon != 0 ||
            appearance.LeftWeapon != 0 || appearance.Face != 0 ||
            appearance.AttackVariant != 0)
        {
            result = FailureLine("authority.unexpected");
            return false;
        }

        ReferenceWorldLocalPlayerDoll player = window.Frame.Player;
        if (player == null || !player.Built || player.Root == null ||
            player.DollRoot == null || player.DollRoot.childCount !=
                ReferenceWorldLocalPlayerDoll.LayerCapacity ||
            player.Appearance == null || player.Appearance.Body != 0 ||
            player.NormalPoses == null || player.NormalPoses.Stand.Length != 5 ||
            player.NormalPoses.Walk.GetLength(0) != 5 ||
            player.NormalPoses.Walk.GetLength(1) != 8 ||
            player.Pose == null || player.Pose.Layers.Count != ExpectedLayerCount ||
            !HasOnlyBodyAndHead(player.Pose) ||
            player.CurrentFacing.Octant != 3 ||
            player.CurrentFacing.StoredDirection != 0 ||
            player.CurrentFacing.FlipX || player.CurrentFrame != 0)
        {
            result = FailureLine("doll.initial_state");
            return false;
        }

        if (!DepthAndShadowReady(player))
        {
            result = FailureLine("doll.depth_or_shadow");
            return false;
        }

        float initialResetDelta = player.EvaluateMoveDeltaTime(0.2f);
        float zeroDelta = player.EvaluateMoveDeltaTime(0f);
        float firstDelta = player.EvaluateMoveDeltaTime(0.02f);
        float secondDelta = player.EvaluateMoveDeltaTime(0.01f);
        float finalResetDelta = player.EvaluateMoveDeltaTime(0.2f);
        if (!Approximately(initialResetDelta, 0.2f) ||
            !Approximately(zeroDelta, 0f) ||
            !Approximately(firstDelta, 0.02f) ||
            !Approximately(secondDelta, 0.012f) ||
            !Approximately(finalResetDelta, 0.2f) ||
            !Approximately(player.MoveDeltaTimeAverage, 0f))
        {
            result = FailureLine("movement.delta");
            return false;
        }

        Color32 tint = ReferenceWorldLocalPlayerDoll.VisibilityTint(false);
        if (!player.ApplyNormalAnimation(18f, 0f, true, 0f, world.Y, tint) ||
            player.CurrentFacing.Octant != 0 ||
            player.CurrentFacing.StoredDirection != 1 ||
            !player.CurrentFacing.FlipX || player.CurrentFrame != 1 ||
            !Approximately(player.AnimationDistancePixels, 18f) ||
            !DepthAndShadowReady(player))
        {
            result = FailureLine("movement.east_pose");
            return false;
        }

        if (!player.ApplyNormalAnimation(-18f, 0f, true, 0f, world.Y, tint) ||
            player.CurrentFacing.Octant != 4 ||
            player.CurrentFacing.StoredDirection != 1 ||
            player.CurrentFacing.FlipX || player.CurrentFrame != 2 ||
            !Approximately(player.AnimationDistancePixels, 36f) ||
            !DepthAndShadowReady(player))
        {
            result = FailureLine("movement.west_pose");
            return false;
        }

        result = string.Format(
            CultureInfo.InvariantCulture,
            "{0} PASS map={1} body={2} head={3} hair={4} layers={5} " +
            "shadow=ready depthY=ready moveDt=ready facing={6} frame={7}",
            Marker,
            world.MapId,
            ExpectedBodyModel,
            world.Head,
            world.Hair,
            player.Pose.Layers.Count,
            player.CurrentFacing.Octant,
            player.CurrentFrame);
        return true;
    }

    public static string FailureLine(string code)
    {
        return Marker + " FAIL code=" + (string.IsNullOrEmpty(code) ? "unknown" : code);
    }

    private static bool HasOnlyBodyAndHead(ReferenceWorldPlayerStandPose pose)
    {
        bool body = false;
        bool head = false;
        for (int index = 0; index < pose.Layers.Count; index++)
        {
            ReferenceWorldPlayerStandLayer layer = pose.Layers[index];
            if (layer == null || layer.Additive)
            {
                return false;
            }

            if (layer.Tag == 0)
            {
                body = true;
            }
            else if (layer.Tag == 3)
            {
                head = true;
            }
            else
            {
                return false;
            }
        }

        return body && head;
    }

    private static bool DepthAndShadowReady(ReferenceWorldLocalPlayerDoll player)
    {
        if (player == null || player.Root == null || player.Pose == null ||
            player.BodyRenderer == null || player.Shadow == null)
        {
            return false;
        }

        float depthY = player.Root.position.y;
        int active = 0;
        for (int index = 0; index < player.LayerRenderers.Count; index++)
        {
            SpriteRenderer renderer = player.LayerRenderers[index];
            if (index >= player.Pose.Layers.Count)
            {
                if (renderer.enabled || renderer.sprite != null)
                {
                    return false;
                }

                continue;
            }

            ReferenceWorldPlayerStandLayer layer = player.Pose.Layers[index];
            if (!renderer.enabled || renderer.sprite != layer.Sprite)
            {
                return false;
            }

            if (!layer.Additive)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(block);
                if (!Approximately(block.GetFloat("_DepthYOverride"), depthY))
                {
                    return false;
                }
            }

            active++;
        }

        ReferenceWorldBlobShadow shadow = player.Shadow;
        shadow.RefreshForAudit();
        float expectedDiameter = ReferenceWorldBlobShadow.DiameterPixels(
            player.Pose.BodyBoundWidth,
            ReferenceWorldBlobShadow.PlayerFloorPixels) /
            ReferenceWorldLocalPlayerDoll.PixelsPerUnit;
        return active == ExpectedLayerCount &&
               shadow.Source == player.BodyRenderer &&
               shadow.ShadowRenderer != null && shadow.ShadowRenderer.enabled &&
               shadow.ShadowRenderer.sprite != null &&
               Approximately(shadow.DiameterUnits, expectedDiameter) &&
               shadow.ShadowRenderer.sortingOrder == Math.Max(
                   0,
                   player.BodyRenderer.sortingOrder +
                   ReferenceWorldBlobShadow.SortingUnder) &&
               Approximately(shadow.ShadowRenderer.color.a, 0.2f);
    }

    private static bool Approximately(float left, float right)
    {
        return Mathf.Abs(left - right) <= 0.0001f;
    }
}

}
