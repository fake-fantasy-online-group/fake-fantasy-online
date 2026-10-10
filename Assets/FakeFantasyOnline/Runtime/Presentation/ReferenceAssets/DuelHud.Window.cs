using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class DuelHud
{
    private void OnGUI()
    {
        if (!active || GameBootstrap.CurMapId != recvMapId || !BitmapFont.Ready) return;
        GUI.depth = 1;
        float scale = UIScale.Factor;
        float width = Screen.width, height = Screen.height;
        string title = "生死擂台·" + (mode == 1 ? string.Format("金币场 {0}金", entryGold) : "装备场") +
            "  " + aName + "  VS  " + bName + (role == 1 ? "" : "  (观战)");
        Center(title, scale, 62f * scale, C(255, 215, 60), width);
        float now = Time.realtimeSinceStartup;
        if (phase == 0)
        {
            Center("等待双方进场…", scale * 1.5f, height * .3f, C(220, 220, 220), width);
        }
        else if (phase == 1)
        {
            string text = countdown > 0 ? countdown.ToString() : "0";
            float pulse = 1f + Mathf.Max(0f, 1f - (now - recvTime) * 2f) * .25f;
            Color color = countdown < 4 ? C(255, 80, 60) : C(255, 230, 120);
            Center(text, scale * 6f * pulse, height * .3f, color, width);
            string hint = role == 1
                ? mode == 1 ? "倒计时结束后开战，退出即判负（报名费归对方）" : "倒计时结束后开战，装备已锁定，退出即判负"
                : "开打前可下注：按 Alt+D 打开擂台窗";
            Center(hint, scale, height * .3f + 60f * scale, C(200, 200, 200), width);
        }
        else if (phase == 2)
        {
            if (fightStartTime >= 0f && now - fightStartTime < 1.6f)
                Center("开 始！", scale * 5f, height * .3f, C(255, 70, 50), width);
        }
        else if (phase == 3)
        {
            float y = height * .28f;
            Center(string.IsNullOrEmpty(winnerName) ? "本场结束" : winnerName + " 获胜！",
                scale * 3f, y, C(255, 215, 60), width);
            if (!string.IsNullOrEmpty(reason))
                Center(reason, scale * 1.2f, y + 44f * scale, C(230, 230, 230), width);
            if (!string.IsNullOrEmpty(lootName))
                Center("战利品: " + lootName + "（已寄给 " + winnerName + "）",
                    scale * 1.2f, y + 66f * scale, C(150, 255, 150), width);
            Center("稍后将送回龙城", scale, y + 90f * scale, C(180, 180, 180), width);
        }
    }
    private static Color C(byte r, byte g, byte b) => new Color(r / 255f, g / 255f, b / 255f, 1f);
    private static void Center(string text, float scale, float y, Color color, float width)
        => BitmapFont.DrawOutline((width - BitmapFont.Measure(text, scale)) * .5f, y, text, color, scale);
}
}
