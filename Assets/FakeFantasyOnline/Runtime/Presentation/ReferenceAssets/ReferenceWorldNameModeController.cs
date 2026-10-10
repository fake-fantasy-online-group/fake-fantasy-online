using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned CharData.nameMode state, preference migration and F11 cycle behavior.
/// </summary>
/// <remarks>
/// The official 1.3.4 client has six modes, persists them under namemode_ plus PrefSuffix and
/// migrates the legacy name-suffixed key when a positive persistent character id is available.
/// This owner deliberately contains no remote-player policy; it only supplies the exact input.
/// </remarks>
public sealed class ReferenceWorldNameModeController
{
    public const int ModeCount = 6;
    public const string PreferencePrefix = "namemode_";
    public const string FeedbackPrefix = "其他玩家显示: ";

    private static readonly string[] Tips =
    {
        "显示其他玩家",
        "隐藏其他玩家名字",
        "隐藏其他玩家名字+血条",
        "隐藏其他玩家",
        "隐藏其他玩家(保留队友)",
        "隐藏其他玩家(保留摊位)"
    };

    private string loadedSuffix;

    public int Mode { get => CharData.nameMode; private set => CharData.nameMode = value; }

    public string LoadedSuffix => loadedSuffix ?? string.Empty;

    public bool Load(long persistentCharacterId, string displayName)
    {
        string suffix = PreferenceSuffix(persistentCharacterId, displayName);
        if (suffix.Length == 0 || suffix == loadedSuffix)
        {
            return false;
        }

        loadedSuffix = suffix;
        string currentKey = PreferencePrefix + suffix;
        int value;
        if (PlayerPrefs.HasKey(currentKey))
        {
            value = PlayerPrefs.GetInt(currentKey, 0);
        }
        else if (persistentCharacterId > 0 && !string.IsNullOrEmpty(displayName))
        {
            string legacyKey = PreferencePrefix + displayName;
            value = PlayerPrefs.GetInt(legacyKey, 0);
            if (PlayerPrefs.HasKey(legacyKey))
            {
                PlayerPrefs.SetInt(currentKey, value);
                PlayerPrefs.Save();
            }
        }
        else
        {
            value = 0;
        }

        Mode = Normalize(value);
        return true;
    }

    public string Cycle()
    {
        int oldMode = Mode;
        int count = CharData.NAMEMODE_COUNT;
        int nextMode = unchecked(oldMode + 1) % count;
        Mode = nextMode;
        string feedback = FeedbackPrefix + CharData.NAMEMODE_TIPS[nextMode];
        ChatUI.Add("系统", feedback,
            new Color(180f / 255f, 220f / 255f, 1f, 1f), "", null);
        CharData.SetPref(PreferencePrefix, Mode);
        return feedback;
    }

    public static int Normalize(int mode)
    {
        return mode < 0 || mode >= ModeCount ? 0 : mode;
    }

    public static string TipFor(int mode)
    {
        return Tips[Normalize(mode)];
    }

    public static string PreferenceSuffix(long persistentCharacterId, string displayName)
    {
        return persistentCharacterId > 0
            ? persistentCharacterId.ToString()
            : displayName ?? string.Empty;
    }

    private void SetPreference(int value)
    {
        if (string.IsNullOrEmpty(loadedSuffix))
        {
            return;
        }

        PlayerPrefs.SetInt(PreferencePrefix + loadedSuffix, value);
        PlayerPrefs.Save();
    }
}

}
