using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // Official DialogUI 0x06000218: race + next learned level, using original skill lreq.
    private static bool AscendEligible(int sid, int tierLevel, out int nextLv)
    {
        nextLv = 0;
        if (EndgamePanel.GrowthState != null) return false;
        if (sid < 10000 || sid >= 10500 || (sid - 10000) / 100 != CharData.race) return false;
        Win05.SkInfo info = Win05.SkillInfo(sid);
        if (info == null || info.levels == null) return false;
        int current = 0;
        if (CharData.skills != null) CharData.skills.TryGetValue(sid, out current);
        if (current >= info.maxlv) return false;
        nextLv = current + 1;
        foreach (Win05.SkLevel level in info.levels)
            if (level.lv == nextLv)
                return tierLevel == 70 ? level.lreq >= 70 && level.lreq < 80 : level.lreq >= 80;
        return false;
    }
    // Unlike AscendEligible's 70–79 admission, this counts exactly lreq == tierLevel.
    private static int AscendTierLeft(int tierLevel)
    {
        int left = 0;
        for (int sid = 10000; sid < 10500; sid++)
        {
            if ((sid - 10000) / 100 != CharData.race) continue;
            Win05.SkInfo info = Win05.SkillInfo(sid);
            if (info == null || info.levels == null) continue;
            int current = 0;
            if (CharData.skills != null) CharData.skills.TryGetValue(sid, out current);
            foreach (Win05.SkLevel level in info.levels)
                if (level.lreq == tierLevel && level.lv > current) left++;
        }
        return left;
    }
    private void ShowAscendMenu()
    {
        InSubView = true;
        optLabels.Clear();
        optActs.Clear();
        if (EndgamePanel.GrowthState != null)
        {
            Greeting = "请在技能面板中使用技能点修炼职业技能。";
            Add(new ReferenceNpcDialogOption("返回", BuildMenu));
            return;
        }
        int left70 = AscendTierLeft(70);
        Greeting = "你已至凡境之极。可在此把高阶功法逐级修炼至满级 —— 每习一层需经验(70级档 1 亿 / 80级档 2 亿)与【与目标级数相同数量】的强行学习丹(学到 N 级需 N 颗)。";
        int count = 0;
        for (int sid = 10000; sid < 10500 && count < 12; sid++)
        {
            int nextLv;
            bool tier70 = AscendEligible(sid, 70, out nextLv);
            if (!tier70 && (left70 != 0 || !AscendEligible(sid, 80, out nextLv))) continue;
            Win05.SkInfo info = Win05.SkillInfo(sid);
            int selected = sid;
            Add(new ReferenceNpcDialogOption(
                string.Format("{0} → {1}级 ({2}亿经验 + {3}丹)", info.name, nextLv, tier70 ? 1L : 2L, nextLv),
                () =>
                {
                    // Official Close only hides; server owns costs and updated skill state.
                    Visible = false;
                    NetClient.I?.AscendSkillForIndependentRuntime(selected);
                }));
            count++;
        }
        if (count == 0) Greeting = "高阶功法你已尽数参透，去别处历练罢。";
        Add(new ReferenceNpcDialogOption("返回", BuildMenu));
    }
}
}
