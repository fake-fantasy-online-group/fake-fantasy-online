namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    internal static int apBody, apCap, apBackpack, apWeaponR, apWeaponL, apFace, apVersion;
    internal static int avBody, avCap, avBackpack, avWeaponR, avWeaponL, avFace;

    // Original 1.5.8 CharData.SetHair 0x06000f20.
    internal static void SetHair(int newHead, int newHair)
    {
        if (head == newHead && hair == newHair) return;
        head = newHead;
        hair = newHair;
        apVersion = unchecked(apVersion + 1);
    }

    internal static void SetAppearance(int body, int cap, int bag, int wr, int wl, int face)
    {
        if (apBody == body && apCap == cap && apBackpack == bag && apWeaponR == wr && apWeaponL == wl && apFace == face) return;
        apBody = body; apCap = cap; apBackpack = bag; apWeaponR = wr; apWeaponL = wl; apFace = face;
        apVersion = unchecked(apVersion + 1);
    }

    internal static void SetAvatarSkin(int body, int cap, int bag, int wr, int wl, int face)
    {
        if (avBody == body && avCap == cap && avBackpack == bag && avWeaponR == wr && avWeaponL == wl && avFace == face) return;
        avBody = body; avCap = cap; avBackpack = bag; avWeaponR = wr; avWeaponL = wl; avFace = face;
        apVersion = unchecked(apVersion + 1);
    }

    internal static PlayerAvatar.Appear CombinedAppear() => new PlayerAvatar.Appear
    {
        Body = avBody > 0 ? avBody : apBody,
        Cap = avCap > 0 ? avCap : apCap,
        Backpack = avBackpack > 0 ? avBackpack : apBackpack,
        WeaponR = avWeaponR > 0 ? avWeaponR : apWeaponR,
        WeaponL = avWeaponL > 0 ? avWeaponL : apWeaponL,
        Face = avFace > 0 ? avFace : apFace,
    };
}

public partial class PlayerController
{
    private int _apVersion = -1;
    internal bool AppearanceVersionMatchesForIndependentRuntime => _apVersion == CharData.apVersion;
}

}
