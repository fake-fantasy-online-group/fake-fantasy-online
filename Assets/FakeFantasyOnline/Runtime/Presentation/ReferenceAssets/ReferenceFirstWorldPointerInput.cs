using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned selectable kinds currently admitted by the reviewed first-world actor cone.
/// </summary>
internal enum ReferenceWorldPointerTargetKind
{
    None = 0,
    RemotePlayer = 1,
    Monster = 2,
    Npc = 3,
    LocalPlayer = 4
}

/// <summary>
/// Observable world-pointer result. Attack values are intents, never local combat acceptance.
/// </summary>
public enum ReferenceFirstWorldPointerAction
{
    None = 0,
    FriendlyRemoteSelected = 1,
    PlayerVsPlayerIntentSelected = 2,
    MonsterAttackIntentSelected = 3,
    TargetSelected = 4,
    TargetCleared = 5,
    NpcDialogOpened = 6,
    GroundItemPickupApproach = 7,
    SkillCast = 8,
}

/// <summary>
/// Exact RemotePlayer.OpenWorldHostile decision recovered from official 1.3.4 code.
/// </summary>
internal static class ReferenceWorldRemoteHostilityMath
{
    public static bool OpenWorldHostile(
        int localFaction,
        int remoteFaction,
        bool remoteDead,
        byte mapSafe,
        byte pkMode,
        int remoteLevel,
        int protectionLevel,
        int localLevel,
        byte remotePkState,
        bool inParty)
    {
        if (localFaction != 0 || remoteFaction != 0 || remoteDead ||
            mapSafe != 0 || pkMode == 0 ||
            remoteLevel < protectionLevel || localLevel < protectionLevel)
        {
            return false;
        }

        if (pkMode == 4 && remotePkState == 0)
        {
            return false;
        }

        if (pkMode == 2 && inParty)
        {
            return false;
        }

        return true;
    }
}

/// <summary>
/// The visible, source-recovered branches of GameHud.MouseOverHud.
/// </summary>
/// <remarks>
/// Minimap remains intentionally absent until that surface is rendered. Hotbar, chat and dialog
/// owners extend this base predicate from the actual world-window composition root.
/// The diagnostic rectangle is an external fail-closed overlay and therefore also suppresses
/// world clicks even though it is not part of the official GameHud.
/// </remarks>
internal static class ReferenceGameHudPointerMath
{
    public static Rect AvatarRect(float scale)
    {
        return new Rect(0f, 0f, 171f * scale, 102f * scale);
    }

    public static Rect MenuRect(int screenWidth, int screenHeight, float scale)
    {
        return new Rect(
            screenWidth - 382f * scale,
            screenHeight - 62f * scale,
            382f * scale,
            62f * scale);
    }

    public static bool IsOverReviewedHud(
        Vector2 topOriginPointer,
        int screenWidth,
        int screenHeight,
        float scale,
        bool targetVisible,
        Rect externalDiagnosticRect)
    {
        if (screenWidth < 1 || screenHeight < 1 || scale <= 0f ||
            float.IsNaN(scale) || float.IsInfinity(scale))
        {
            return true;
        }

        GameSettings.EnsureLoaded();
        return AvatarRect(scale).Contains(topOriginPointer) ||
               (!GameSettings.HotbarBottom && MenuRect(screenWidth, screenHeight, scale).Contains(topOriginPointer)) ||
               (targetVisible &&
                ReferenceFirstWorldTargetHud.TargetBarRect(scale).Contains(topOriginPointer)) ||
               externalDiagnosticRect.Contains(topOriginPointer);
    }
}

/// <summary>
/// Exact short-right-click clock used by PlayerController.Update.
/// </summary>
internal sealed class ReferenceWorldRightClickTracker
{
    public const float HoldThresholdSeconds = 0.25f;
    public const float MaximumMovementSquared = 64f;

    private float downTime = -1f;
    private Vector3 downScreen;

    public void Press()
    {
        // Preserve the native write between the two live input reads.
        downTime = Time.time;
        downScreen = Input.mousePosition;
    }

    public void Press(float unscaledTime, Vector3 screenPosition)
    {
        downTime = unscaledTime;
        downScreen = screenPosition;
    }

    public bool Release(float unscaledTime, Vector3 screenPosition)
    {
        return 0f < downTime &&
            unscaledTime - downTime < HoldThresholdSeconds &&
            (screenPosition - downScreen).sqrMagnitude < MaximumMovementSquared;
    }

    public bool Release()
    {
        // Update never clears this latch on release. Sample each live value only
        // after the preceding native guard has passed, including the strict > 0 test.
        return 0f < downTime &&
            Time.time - downTime < HoldThresholdSeconds &&
            (Input.mousePosition - downScreen).sqrMagnitude < MaximumMovementSquared;
    }

    public void Reset()
    {
        downTime = -1f;
        downScreen = Vector3.zero;
    }
}

}
