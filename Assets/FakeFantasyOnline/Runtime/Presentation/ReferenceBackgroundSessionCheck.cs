using System;
using System.Globalization;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{

internal enum ReferenceBackgroundSessionCheckExit
{
    Success = 0,
    CredentialUnavailable = 20,
    LoginFailed = 21,
    ServerSelectionFailed = 22,
    RosterEmpty = 23,
    EnterWorldFailed = 24,
    WorldOwnerFailed = 25,
    WorldStateIncomplete = 26,
    Timeout = 27,
    CharacterProvisionFailed = 28,
    Unexpected = 70
}

/// <summary>
/// Versioned, redacted contract helpers for the opt-in one-shot background Player entry.
/// </summary>
internal static class ReferenceBackgroundSessionCheck
{
    public const string Argument = "--ffo-background-session-check";
    public const string Marker = "FFO_BACKGROUND_SESSION_CHECK_V1";
    public const int ViewportWidth = 800;
    public const int ViewportHeight = 600;
    public const float WorldReadinessTimeoutSeconds = 10f;
    public const float WorldEntityQuietSeconds = 1f;
    public const string ManagedAccountName = "ffo_local_acceptance";
    public const string ManagedCharacterName = "源码验收员";
    public const string ScreenshotFileName = "background-session-check.png";
    public const float ScreenshotTimeoutSeconds = 2f;

    public static bool IsManagedAccount(string account)
    {
        return string.Equals(account, ManagedAccountName, StringComparison.Ordinal);
    }

    public static CharacterCreationRequest CreateManagedCharacterRequest()
    {
        return new CharacterCreationRequest(
            ManagedCharacterName,
            0,
            0,
            2,
            1);
    }

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

    public static bool IsWorldReady(
        ClientSessionState state,
        WorldEntryRecord world,
        WorldPlayerStatusState playerStatus,
        int npcCount,
        int entityCount)
    {
        return state == ClientSessionState.InWorld &&
               world != null &&
               world.EntityId > 0 &&
               playerStatus != null &&
               npcCount > 0 &&
               entityCount > 0;
    }

    public static bool IsWorldEntitySnapshotStable(
        int observedCount,
        int currentCount,
        float stableSince,
        float now)
    {
        return observedCount > 0 && currentCount == observedCount &&
               stableSince >= 0f && now >= stableSince &&
               now - stableSince >= WorldEntityQuietSeconds;
    }

    public static string SuccessLine(
        int mapId,
        int rosterCount,
        int npcCount,
        int entityCount)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} PASS state=InWorld map={1} roster={2} npcs={3} entities={4}",
            Marker,
            mapId,
            Math.Max(0, rosterCount),
            Math.Max(0, npcCount),
            Math.Max(0, entityCount));
    }

    public static string FailureLine(ReferenceBackgroundSessionCheckExit exit)
    {
        return Marker + " FAIL code=" + StableCode(exit);
    }

    public static string OwnerFailureLine(ReferenceFirstWorldWindowFailure failure)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} DIAGNOSTIC world_owner={1}",
            Marker,
            (int)failure);
    }

    public static string WorldRevisionFailureLine(ReferenceWorldRevisionFailure failure)
    {
        return string.Format(
            CultureInfo.InvariantCulture,
            "{0} DIAGNOSTIC world_revision={1}",
            Marker,
            (int)failure);
    }

    public static string ScreenshotLine(bool captured)
    {
        return Marker + " ARTIFACT screenshot=" +
               (captured ? ScreenshotFileName : "unavailable");
    }

    public static string StableCode(ReferenceBackgroundSessionCheckExit exit)
    {
        switch (exit)
        {
            case ReferenceBackgroundSessionCheckExit.Success:
                return "ok";
            case ReferenceBackgroundSessionCheckExit.CredentialUnavailable:
                return "credential.unavailable";
            case ReferenceBackgroundSessionCheckExit.LoginFailed:
                return "session.login_failed";
            case ReferenceBackgroundSessionCheckExit.ServerSelectionFailed:
                return "session.server_selection_failed";
            case ReferenceBackgroundSessionCheckExit.RosterEmpty:
                return "session.roster_empty";
            case ReferenceBackgroundSessionCheckExit.EnterWorldFailed:
                return "session.enter_world_failed";
            case ReferenceBackgroundSessionCheckExit.WorldOwnerFailed:
                return "world.owner_failed";
            case ReferenceBackgroundSessionCheckExit.WorldStateIncomplete:
                return "world.state_incomplete";
            case ReferenceBackgroundSessionCheckExit.Timeout:
                return "session.timeout";
            case ReferenceBackgroundSessionCheckExit.CharacterProvisionFailed:
                return "character.provision_failed";
            default:
                return "client.unexpected";
        }
    }
}
}
