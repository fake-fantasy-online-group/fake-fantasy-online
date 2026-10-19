using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    // Mirror the real Frame early returns and R-key guards before entering their shared action.
    internal string MobileSittingUnavailableReason
    {
        get
        {
            if (dead) return "当前状态无法打坐";
            if (CharData.riding || CharData.rideAnchorId != 0) return "请先下坐骑";
            if (workSkill != 0) return "请先结束打工";
            if (CharData.immobile != 0) return "当前无法行动";
            if (StallConfigDlg.LocalStalling) return "请先结束摆摊";
            if (Time.time < chantUntil) return "施法中无法打坐";
            if (MobileClientPresentation.GameplayInputBlocked || ChatUI.TypingNow || Win05.GmTyping ||
                GUIUtility.keyboardControl != 0) return "请先结束当前操作";
            if (!sitting && InCombat) return "战斗中无法打坐";
            return string.Empty;
        }
    }

    internal bool TryToggleMobileSitting()
    {
        string reason = MobileSittingUnavailableReason;
        if (reason.Length != 0)
        {
            ChatUI.Add("系统", reason, new Color(1f, 180f / 255f, 120f / 255f, 1f));
            return false;
        }
        ToggleSittingAction();
        return true;
    }
}
}
