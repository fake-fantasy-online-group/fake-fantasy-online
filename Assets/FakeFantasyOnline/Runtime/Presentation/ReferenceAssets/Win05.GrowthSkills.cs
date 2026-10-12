using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private static bool UsesGrowthSkill(int id) => EndgamePanel.GrowthState != null && EndgameSkillRankView.IsProfessionSkill(id);
    private static int DisplayedSkillPoints => EndgamePanel.GrowthState != null ? EndgamePanel.GrowthState.UnspentSkillPoints : CharData.skillPoints;
    private static string BuildGrowthSkillTip(SkInfo info, bool includeNext)
    {
        var state = EndgamePanel.GrowthState;
        var rank = state.SkillRank(info.id);
        var rows = new List<(string, char, bool)> { (info.name, (char)5, true) };
        if (rank == null)
        {
            rows.Add(("正在等待服务器技能资料", (char)6, false));
            return ComposeTip(rows);
        }
        rows.Add(("有效等级: " + rank.EffectiveRank, (char)5, true));
        rows.Add(("投入 " + rank.HardRank + "/" + state.SkillHardCap + "  装备与状态 +" + rank.SoftRank + "  授予 " + rank.GrantRank, (char)0, false));
        if (!rank.CanCast) rows.Add(("未学习，需先投入1点；装备等级不满足学习前置", (char)6, false));
        foreach (string line in rank.CurrentEffectLines) rows.Add((line, (char)0, false));
        if (rank.CurrentEffectLines.Count == 0) rows.Add(("服务器尚未提供技能效果", (char)6, false));
        if (includeNext && rank.NextEffectLines.Count > 0)
        {
            rows.Add((string.Empty, (char)0, false));
            rows.Add(("有效等级提升1级后的效果", (char)5, true));
            foreach (string line in rank.NextEffectLines) rows.Add((line, (char)0, false));
        }
        if (includeNext)
        {
            rows.Add(("剩余技能点 " + state.UnspentSkillPoints, (char)6, false));
            foreach (string reason in rank.BlockedReasons) rows.Add((reason, (char)6, false));
            if (state.Pending) rows.Add(("操作结果确认中，可点下方“查询原请求”", (char)6, false));
            else if (state.SkillRanksRevision != state.Revision) rows.Add(("技能资料更新中", (char)6, false));
        }
        if (rank.CanCast && SkillUsableForHotbar(info)) rows.Add(("双击或拖曳至快捷栏以使用这个技能", (char)6, false));
        return ComposeTip(rows);
    }
}
}
