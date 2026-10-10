using System;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

internal enum ReferenceBackgroundMapTransitionCheckExit
{
    Success = 0,
    SourceMapUnexpected = 31,
    TargetTimeout = 32,
    TargetStateIncomplete = 33
}

/// <summary>
/// Versioned QA input/result contract for a no-focus transition through the recovered owner.
/// The fixed destination is test data only; this type does not implement product transition logic.
/// </summary>
internal static class ReferenceBackgroundMapTransitionCheck
{
    public const string Argument = "--ffo-background-map-transition-check";
    public const string Marker = "FFO_BACKGROUND_MAP_TRANSITION_CHECK_V1";
    public const int SourceMapId = 6;
    public const int TargetMapId = 7;
    public const int TargetX = 5781;
    public const int TargetY = 2947;
    public const float TargetReadinessTimeoutSeconds = 240f;
    public const float TargetSnapshotStabilitySeconds = 31f;
    public const string ScreenshotFileName = "background-map-transition-check.png";

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

    public static bool IsTargetReady(
        ClientSessionState sessionState,
        GameBootstrap bootstrap,
        ReferenceFirstWorldWindowOwner window,
        WorldPlayerStatusState playerStatus,
        int npcCount,
        long npcVersion,
        long initialNpcVersion,
        long entityVersion,
        long initialEntityVersion)
    {
        WorldEntryRecord current = bootstrap == null ? null : bootstrap.CurrentEntry;
        return sessionState == ClientSessionState.InWorld &&
               bootstrap != null && !bootstrap.Switching &&
               current != null && current.MapId == TargetMapId &&
               current.X == TargetX && current.Y == TargetY &&
               window != null && window.Built && window.Frame != null &&
               ReferenceEquals(window, bootstrap.Window) &&
               playerStatus != null && npcCount > 0 &&
               window.Frame.LastNpcsVersion == npcVersion &&
               window.Frame.LastWorldEntitiesVersion == entityVersion &&
               npcVersion > initialNpcVersion && entityVersion > initialEntityVersion;
    }

    public static string SuccessLine(int npcCount, int entityCount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} PASS source={1} target={2} x={3} y={4} npcs={5} entities={6}",
            Marker,
            SourceMapId,
            TargetMapId,
            TargetX,
            TargetY,
            Math.Max(0, npcCount),
            Math.Max(0, entityCount));
    }

    public static string FailureLine(ReferenceBackgroundMapTransitionCheckExit exit)
    {
        return Marker + " FAIL code=" + StableCode(exit);
    }

    public static string CommonFailureLine(ReferenceBackgroundSessionCheckExit exit)
    {
        return Marker + " FAIL code=" + ReferenceBackgroundSessionCheck.StableCode(exit);
    }

    public static string ScreenshotLine(bool captured)
    {
        return Marker + " ARTIFACT screenshot=" +
               (captured ? ScreenshotFileName : "unavailable");
    }

    public static string DiagnosticLine(
        ClientSessionState sessionState,
        GameBootstrap bootstrap,
        ReferenceFirstWorldWindowOwner window,
        int renderedNpcCount,
        int entityCount,
        long npcVersion,
        long initialNpcVersion,
        long entityVersion,
        long initialEntityVersion)
    {
        WorldEntryRecord current = bootstrap == null ? null : bootstrap.CurrentEntry;
        ReferenceWorldRevisionFailure revision = window == null || window.Frame == null
            ? ReferenceWorldRevisionFailure.None
            : window.Frame.LastRevisionFailure;
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} DIAGNOSTIC state={1} switching={2} map={3} x={4} y={5} " +
            "window={6} npcs={7} npcRevision={8}/{9} entities={10} " +
            "entityRevision={11}/{12} worldRevision={13}",
            Marker,
            sessionState,
            bootstrap != null && bootstrap.Switching ? 1 : 0,
            current == null ? 0 : current.MapId,
            current == null ? 0 : Mathf.RoundToInt(current.X),
            current == null ? 0 : Mathf.RoundToInt(current.Y),
            window != null && window.Built ? 1 : 0,
            Math.Max(0, renderedNpcCount),
            npcVersion,
            initialNpcVersion,
            Math.Max(0, entityCount),
            entityVersion,
            initialEntityVersion,
            revision);
    }

    public static string StableCode(ReferenceBackgroundMapTransitionCheckExit exit)
    {
        switch (exit)
        {
            case ReferenceBackgroundMapTransitionCheckExit.Success:
                return "ok";
            case ReferenceBackgroundMapTransitionCheckExit.SourceMapUnexpected:
                return "transition.source_map_unexpected";
            case ReferenceBackgroundMapTransitionCheckExit.TargetTimeout:
                return "transition.target_timeout";
            default:
                return "transition.target_state_incomplete";
        }
    }
}

}
