using System;
using System.Collections.Generic;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

internal static class ReferenceAudioSourceCheck
{
    public const string SourceArgument = "--ffo-audio-source-check-1-5-8";
    public const string ManualArgument = "--ffo-audio-manual-check-1-5-8";
    public const string Marker = "FFO_AUDIO_SOURCE_CHECK_1_5_8_V1";
    public const float StartTimeoutSeconds = 30f;
    public const float MapStartTimeoutSeconds = 30f;
    public const float UnattendedLoginHoldSeconds = 0.5f;
    public const float UnattendedBackgroundHoldSeconds = 35f;
    public const float ManualLoginHoldSeconds = 8f;
    public const int MinimumHeartbeatReplies = 6;

    public static bool IsRequested(string[] arguments)
    {
        return Contains(arguments, SourceArgument);
    }

    public static bool IsManualRequested(string[] arguments)
    {
        return Contains(arguments, ManualArgument);
    }

    public static string StageLine(string stage)
    {
        return Marker + " STAGE stage=" + stage;
    }

    public static string SuccessLine(float volume)
    {
        return Marker +
            " PASS login=M001 map=6 world=M003 order=trigger-start-interrupt-start" +
            " loop=1 volume=" + volume.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) +
            " backgroundSeconds=35 worldOwner=retained npcOpen=public-pointer" +
            " npcReply=authoritative musicRetained=1" +
            " runInBackground=GameBootstrap.Start" +
            " physicalOutput=pending";
    }

    public static string ReadyLine(float volume)
    {
        return Marker +
            " READY login=M001 map=6 world=M003 order=trigger-start-interrupt-start" +
            " loop=1 volume=" + volume.ToString("0.000", System.Globalization.CultureInfo.InvariantCulture) +
            " physicalOutput=pending-user-verdict";
    }

    public static string FailureLine(string code)
    {
        return Marker + " FAIL code=" + code;
    }

    public static bool TryOpenLiveNpcThroughPublicPointer(
        ReferenceFirstWorldWindowOwner window,
        out string npcName)
    {
        npcName = string.Empty;
        if (window == null || !window.Built || window.Frame == null ||
            window.Camera == null)
        {
            return false;
        }

        int selectedEntityId = int.MaxValue;
        ReferenceWorldNpcActor selectedActor = null;
        foreach (KeyValuePair<int, ReferenceWorldNpcActor> pair in
                 window.Frame.NpcActors)
        {
            ReferenceWorldNpcActor actor = pair.Value;
            if (pair.Key < selectedEntityId && actor != null &&
                actor.State != null && actor.RootObject != null &&
                actor.PickBox != null && actor.PickBox.enabled)
            {
                selectedEntityId = pair.Key;
                selectedActor = actor;
            }
        }

        if (selectedActor == null)
        {
            return false;
        }

        Vector3 screen = window.Camera.WorldToScreenPoint(
            selectedActor.RootObject.transform.position);
        npcName = selectedActor.State.DisplayName;
        return screen.z > 0f &&
               window.HandlePointerInput(
                   screen,
                   true,
                   false,
                   false,
                   false,
                   false,
                   Time.unscaledTime,
                   Time.time,
                   Rect.zero) &&
               window.LastPointerAction ==
                   ReferenceFirstWorldPointerAction.NpcDialogOpened &&
               window.NpcDialog != null && window.NpcDialog.Visible &&
               string.Equals(
                   window.NpcDialog.NpcName,
                   selectedActor.State.DisplayName,
                   StringComparison.Ordinal);
    }

    private static bool Contains(string[] arguments, string expected)
    {
        if (arguments == null)
        {
            return false;
        }

        for (int index = 0; index < arguments.Length; index++)
        {
            if (string.Equals(arguments[index], expected, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}

}
