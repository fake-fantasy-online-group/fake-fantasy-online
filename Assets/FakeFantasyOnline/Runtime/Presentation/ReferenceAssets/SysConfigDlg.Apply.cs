using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class SysConfigDlg
{
    private void Apply()
    {
        if (MusicManager.I != null) MusicManager.I.SetVolume(sndOn[0] ? sndVal[0] / 100f : 0f);
        UIScale.User = sndVal[1] / 100f * 1.4f + 0.6f;
        SoundManager.Volume(sndOn[2] ? sndVal[2] / 100f : 0f);
        GameSettings.HotbarBottom = opt[0];
        GameSettings.ColorWeak = opt[5];
        GameSettings.ShowPlayerNames = opt[1];
        GameSettings.ShowMonsterNames = opt[2];
        GameSettings.Shadow = opt[3];
        GameSettings.Weather = opt[4];
        GameSettings.ShowTeamNames = opt[6];
        bool previousSmartCast = GameSettings.SmartCast;
        GameSettings.SmartCast = smartCast;
        GameSettings.HeadHpBarOn = hpBar;
        HeadHpBar.Show = hpBar;
        GameSettings.Save();
        if (previousSmartCast != smartCast && NetClient.I != null) NetClient.I.SetSmartCastForIndependentRuntime(smartCast);
        WeatherFX.Refresh();
        PlayerPrefs.Save();
        if (pk != CharData.pkMode && NetClient.I != null) NetClient.I.SetPkModeForIndependentRuntime(pk);
        if (pickQ != CharData.petPickQuality && NetClient.I != null)
        {
            NetClient.I.PetPickQualityForIndependentRuntime(unchecked((byte)pickQ));
            CharData.petPickQuality = pickQ;
        }
    }
}

}
