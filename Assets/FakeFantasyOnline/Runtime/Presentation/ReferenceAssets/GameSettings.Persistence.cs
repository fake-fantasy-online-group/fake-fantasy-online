using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class GameSettings
{
    internal static bool ShowPlayerNames = true;
    internal static bool ShowMonsterNames = true;
    internal static bool ShowTeamNames = true;
    internal static bool Weather = true;
    internal static bool Shadow = true;
    internal static bool SmartCast;
    internal static bool HotbarBottom;
    internal static bool ColorWeak;
    internal static bool HideOtherPets;
    internal static bool HideMyPetGlow;
    internal static bool HideOtherPetGlow;
    private static bool loaded;

    internal static void EnsureLoaded()
    {
        if (loaded) return;
        loaded = true;
        ShowPlayerNames = PlayerPrefs.GetInt("opt_pname", 1) != 0;
        ShowMonsterNames = PlayerPrefs.GetInt("opt_mname", 1) != 0;
        ShowTeamNames = PlayerPrefs.GetInt("opt_teamname", 1) != 0;
        Weather = PlayerPrefs.GetInt("opt_weather", 1) != 0;
        Shadow = PlayerPrefs.GetInt("opt_shadow", 1) != 0;
        SmartCast = PlayerPrefs.GetInt("opt_smartcast", 0) != 0;
        HeadHpBarOn = PlayerPrefs.GetInt("opt_hpbar", 0) != 0;
        HotbarBottom = PlayerPrefs.GetInt("opt_hotbar_bottom", 0) != 0;
        ColorWeak = PlayerPrefs.GetInt("opt_colorweak", 0) != 0;
        HideOtherPets = PlayerPrefs.GetInt("opt_hideotherpet", 0) != 0;
        HideMyPetGlow = PlayerPrefs.GetInt("opt_hidemyglow", 0) != 0;
        HideOtherPetGlow = PlayerPrefs.GetInt("opt_hideotherglow", 0) != 0;
        LoadLocalDisplaySettings();
    }

    public static void Save()
    {
        PlayerPrefs.SetInt("opt_pname", ShowPlayerNames ? 1 : 0);
        PlayerPrefs.SetInt("opt_mname", ShowMonsterNames ? 1 : 0);
        PlayerPrefs.SetInt("opt_teamname", ShowTeamNames ? 1 : 0);
        PlayerPrefs.SetInt("opt_weather", Weather ? 1 : 0);
        PlayerPrefs.SetInt("opt_shadow", Shadow ? 1 : 0);
        PlayerPrefs.SetInt("opt_smartcast", SmartCast ? 1 : 0);
        PlayerPrefs.SetInt("opt_hpbar", HeadHpBarOn ? 1 : 0);
        PlayerPrefs.SetInt("opt_hotbar_bottom", HotbarBottom ? 1 : 0);
        PlayerPrefs.SetInt("opt_colorweak", ColorWeak ? 1 : 0);
        PlayerPrefs.SetInt("opt_hideotherpet", HideOtherPets ? 1 : 0);
        PlayerPrefs.SetInt("opt_hidemyglow", HideMyPetGlow ? 1 : 0);
        PlayerPrefs.SetInt("opt_hideotherglow", HideOtherPetGlow ? 1 : 0);
        PlayerPrefs.Save();
    }

#if UNITY_EDITOR
    internal static void SetForAudit(bool hideMine, bool hideOther)
    {
        EnsureLoaded();
        HideMyPetGlow = hideMine;
        HideOtherPetGlow = hideOther;
    }
#endif
}

}
