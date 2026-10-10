using System;
using System.Threading;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class DuelDlg
{
    // 官方签名：DrawChallenge(Rect p, float s, float f, Func<float,float,float,float,Rect> L)。
    // L 由 OnGUI 的闭包提供，读面板原点与缩放。
    private void DrawChallenge(Rect panel, float scale, float font,
        Func<float, float, float, float, Rect> L)
    {
        TX(panel.x + 22f * scale, panel.y + 74f * scale, "挑战对象", C(220, 210, 180), font);
        Rect target = L(84, 68, 150, 22);
        Box(target, scale);
        string name = UiInput.Field(UIScale.Snap(new Rect(target.x + 4f * scale, target.y + 2f * scale,
            target.width - 8f * scale, target.height - 4f * scale)), "duel_target",
            _targetName ?? string.Empty, 12, editSt, scale);
        _targetName = name;
        TX(panel.x + 242f * scale, panel.y + 74f * scale, "右键玩家或手填", C(150, 150, 160), font);
        Option(L(20, 100, 327, 52), scale, font, 0, "装 备 场",
            "锁定双方装备，输者随机掉一件未绑定装备给对方。");
        Option(L(20, 158, 327, 52), scale, font, 1, "金 币 场",
            string.Format("各交 {0} 金，赢家得 {1} 金（抽 {2}%），装备不掉。",
                entryGold, entryGold * (100 - feePct) * 2 / 100, feePct));
        TX(panel.x + 22f * scale, panel.y + 220f * scale,
            "倒计时 10 秒后开打；出不了圈、不能换装备，退出判负。", C(160, 160, 172), font);
        TX(panel.x + 22f * scale, panel.y + 236f * scale,
            "观众可在「下注·观战」页押注（开打前）。", C(160, 160, 172), font);
        if (SkinBtn(L(60, 268, 110, 39), redBtn,
            _mode == 1 ? "发起金币场挑战" : "发起装备场挑战",
            !string.IsNullOrEmpty((_targetName ?? string.Empty).Trim()), font))
        {
            _ = commands?.ChallengeAsync(_targetId, _targetName.Trim(), (byte)_mode, CancellationToken.None);
            Close();
        }
        if (SkinBtn(L(237, 277, 70, 22), noBtn, string.Empty, true, font)) Close();
    }

    private void Option(Rect rect, float scale, float font, int mode, string title, string description)
    {
        bool on = _mode == mode;
        Box(rect, scale, on, .45f);
        TX(rect.x + 12f * scale, rect.y + 9f * scale, (on ? "◆ " : "◇ ") + title,
            on ? C(255, 226, 138) : C(215, 215, 225), font);
        TX(rect.x + 12f * scale, rect.y + 29f * scale, description,
            on ? C(235, 225, 200) : C(165, 165, 178), font);
        if (GUI.Button(rect, GUIContent.none, GUIStyle.none))
            _mode = mode;
    }
}
}
