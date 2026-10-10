using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    private void LoadPrefs()
    {
        loaded = true;
        float[] volumes =
        {
            PlayerPrefs.GetFloat("vol_music", 0.6f),
            PlayerPrefs.GetFloat("vol_env", 0.5f),
            PlayerPrefs.GetFloat("vol_sound", 0.8f)
        };
        float[] defaults = { 60f, 50f, 80f };
        for (int i = 0; i < 3; i++)
        {
            sndOn[i] = volumes[i] > 0f;
            sndVal[i] = volumes[i] > 0f ? volumes[i] * 100f : defaults[i];
        }
        sndVal[1] = Mathf.Clamp01((UIScale.User - 0.6f) / 1.4f) * 100f;
        sndOn[1] = true;
        GameSettings.EnsureLoaded();
        opt[0] = GameSettings.HotbarBottom;
        opt[5] = GameSettings.ColorWeak;
        opt[1] = GameSettings.ShowPlayerNames;
        opt[2] = GameSettings.ShowMonsterNames;
        opt[3] = GameSettings.Shadow;
        opt[4] = GameSettings.Weather;
        opt[6] = GameSettings.ShowTeamNames;
        int currentPk = CharData.pkMode;
        smartCast = GameSettings.SmartCast;
        int currentPickQuality = CharData.petPickQuality;
        pk = currentPk;
        pickQ = currentPickQuality;
        hpBar = HeadHpBar.Show;
    }
}

}
