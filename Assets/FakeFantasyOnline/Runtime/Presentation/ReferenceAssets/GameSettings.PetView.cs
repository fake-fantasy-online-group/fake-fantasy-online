namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class GameSettings
{
    public static int PetViewMask => (HideOtherPets ? 1 : 0) | (HideMyPetGlow ? 2 : 0) | (HideOtherPetGlow ? 4 : 0);

    public static void ApplyPetViewMask(int mask)
    {
        EnsureLoaded();
        bool other = (mask & 1) != 0;
        bool myGlow = (mask & 2) != 0;
        bool otherGlow = (mask & 4) != 0;
        if (other == HideOtherPets && myGlow == HideMyPetGlow && otherGlow == HideOtherPetGlow) return;
        HideOtherPets = other;
        HideMyPetGlow = myGlow;
        HideOtherPetGlow = otherGlow;
        Save();
        MonsterEntity.OnPetGlowSettingChanged();
    }
}

}
