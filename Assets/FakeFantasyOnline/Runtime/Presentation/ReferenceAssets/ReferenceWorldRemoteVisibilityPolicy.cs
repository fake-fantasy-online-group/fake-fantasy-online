using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned translation of the official RemotePlayer.ShouldHideNow decision.
/// </summary>
/// <remarks>
/// The locked 1.3.4 method reads the CharData.nameMode static and combines it with authoritative
/// invisibility plus current party and stall-owner queries. Binary identity remains in the
/// recovery contract, outside production source. Applying the result to a Unity hierarchy remains
/// an adjacent RemotePlayer.Update boundary.
/// </remarks>
internal static class ReferenceWorldRemoteVisibilityPolicy
{
    public static bool IsF11Exempt(
        int nameMode,
        bool inParty,
        bool isStallOwner)
    {
        if (nameMode == 4)
        {
            return inParty;
        }

        return nameMode == 5 && isStallOwner;
    }

    public static bool ShouldShowMountedArt(
        int nameMode,
        bool inParty,
        bool isStallOwner)
    {
        bool f11Exempt = IsF11Exempt(nameMode, inParty, isStallOwner);
        return nameMode < 3 || f11Exempt;
    }

    public static bool ShouldHideNow(
        int nameMode,
        bool invisible,
        bool inParty,
        bool isStallOwner)
    {
        bool f11Exempt = IsF11Exempt(nameMode, inParty, isStallOwner);

        if (nameMode < 3 || f11Exempt)
        {
            return invisible && !inParty;
        }

        return true;
    }
}

/// <summary>
/// Source-owned subset of GameSettings used by the reviewed remote-mount branch.
/// </summary>
/// <remarks>
/// The official defaults are false. A present entry mask overwrites all three persisted values;
/// an omitted compatibility tail leaves the locally loaded settings unchanged.
/// </remarks>
internal static class ReferenceWorldPetViewSettings
{
    public const int HideOtherPetsBit = 1;
    public const int HideMyPetGlowBit = 2;
    public const int HideOtherPetGlowBit = 4;

    internal const string HideOtherPetsKey = "opt_hideotherpet";
    internal const string HideMyPetGlowKey = "opt_hidemyglow";
    internal const string HideOtherPetGlowKey = "opt_hideotherglow";

    public static bool ApplyEntryMask(int? mask)
    {
        if (mask.HasValue) GameSettings.ApplyPetViewMask(mask.Value);
        else GameSettings.EnsureLoaded();
        return GameSettings.HideOtherPets;
    }

    public static int CurrentMask()
    {
        GameSettings.EnsureLoaded();
        return GameSettings.PetViewMask;
    }

    public static bool ApplyMask(int mask)
    {
        GameSettings.ApplyPetViewMask(mask);
        return GameSettings.HideOtherPets;
    }

    public static bool HasBit(int mask, int bit)
    {
        return (mask & bit) != 0;
    }
}

/// <summary>
/// Stateful translation of RemotePlayer.MountArtWanted.
/// </summary>
internal sealed class ReferenceWorldRemoteMountArtRetention
{
    public const float ViewRadiusMultiplier = 1.8f;
    public const float FarGraceSeconds = 6f;

    public float FarSeconds { get; private set; }

    public bool TryResolve(
        bool cameraOrthographic,
        float cameraOrthographicSize,
        float cameraAspect,
        Vector3 actorPosition,
        Vector3 cameraPosition,
        float deltaTime,
        out bool artWanted)
    {
        artWanted = false;
        if (!IsFinite(deltaTime) || deltaTime < 0f ||
            !IsFinite(actorPosition.x) || !IsFinite(actorPosition.y) ||
            !IsFinite(cameraPosition.x) || !IsFinite(cameraPosition.y))
        {
            return false;
        }

        if (!cameraOrthographic)
        {
            FarSeconds = 0f;
            artWanted = true;
            return true;
        }

        if (!IsFinite(cameraOrthographicSize) || cameraOrthographicSize <= 0f ||
            !IsFinite(cameraAspect) || cameraAspect <= 0f)
        {
            return false;
        }

        float halfExtent = Mathf.Max(
            cameraOrthographicSize * cameraAspect,
            cameraOrthographicSize);
        float radius = halfExtent * ViewRadiusMultiplier;
        float deltaX = actorPosition.x - cameraPosition.x;
        float deltaY = actorPosition.y - cameraPosition.y;
        if (deltaX * deltaX + deltaY * deltaY <= radius * radius)
        {
            FarSeconds = 0f;
        }
        else
        {
            FarSeconds += deltaTime;
        }

        artWanted = FarSeconds < FarGraceSeconds;
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
