using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private static bool UsesGrowthSkill(int id) => EndgamePanel.GrowthState != null && EndgameSkillRankView.IsProfessionSkill(id);
    private static int DisplayedSkillPoints => EndgamePanel.GrowthState != null ? EndgamePanel.GrowthState.UnspentSkillPoints : CharData.skillPoints;

    // Keep the original skill card's hierarchy and ComposeTip protocol. Only the
    // numeric/effect authority changes: legacy rank-five prose is never a V5 fallback.
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

        rows.Add((rank.CanCast ? "等级: " + rank.EffectiveRank : "等级: 未学习", (char)0, true));
        rows.Add(("投入点数: " + rank.HardRank + "/" + state.SkillHardCap, (char)0, false));
        if (rank.SoftRank != "0") rows.Add(("装备与状态: +" + rank.SoftRank + "级", (char)0, false));
        if (rank.GrantRank != "0") rows.Add(("授予等级: " + rank.GrantRank, (char)0, false));

        if (rank.CanCast)
        {
            AppendGrowthEffectLines(rows, rank.CurrentEffectLines);
            if (includeNext && rank.HardRank < state.SkillHardCap && rank.NextEffectLines.Count > 0)
            {
                rows.Add((string.Empty, (char)0, false));
                rows.Add(("下一等级" + (rank.NextEffectiveRank.Length > 0 ? ": " + rank.NextEffectiveRank : string.Empty), (char)0, true));
                // The server compares semantic effect blocks, preserving target and
                // status headings. Never infer changed values by parsing tooltip text.
                AppendGrowthEffectLines(rows, rank.NextChangeLines.Count > 0 ? rank.NextChangeLines : rank.NextEffectLines);
                AppendGrowthRequirement(rows, rank);
            }
        }
        else
        {
            // Like the native unlearned card, show the first investment's preview
            // once. It is not the current effect, and includes the server's soft ranks.
            rows.Add(("投入1点后:" + (rank.NextEffectiveRank.Length > 0 ? " 等级 " + rank.NextEffectiveRank : string.Empty), (char)0, false));
            AppendGrowthEffectLines(rows, rank.NextEffectLines);
            if (includeNext) AppendGrowthRequirement(rows, rank);
        }

        AppendGrowthSynergies(rows, rank);

        if (includeNext)
        {
            // Bonus sections already have their own heading and color boundary.
            // Keep the original spacer only when the footer follows effect prose.
            if (rank.TrainingSynergies.Count == 0 && rank.NativeModifierLines.Count == 0)
                rows.Add((string.Empty, (char)0, false));
            string pointsLine = "剩余技能点: " + state.UnspentSkillPoints;
            if (state.Pending) pointsLine += "（确认中）";
            else if (state.SkillRanksRevision != state.Revision) pointsLine += "（更新中）";
            rows.Add((pointsLine, (char)6, false));
            foreach (string reason in rank.BlockedReasons) rows.Add((reason, (char)6, false));
        }
        if (rank.CanCast && SkillUsableForHotbar(info)) rows.Add(("双击或拖曳至快捷栏以使用这个技能", (char)6, false));
        return ComposeTip(rows);
    }

    private static void AppendGrowthRequirement(List<(string, char, bool)> rows, EndgameSkillRankView rank)
    {
        if (rank.NextRequiredLevel > 0) rows.Add(("需要等级: " + rank.NextRequiredLevel, (char)1, false));
    }

    private static void AppendGrowthSynergies(List<(string, char, bool)> rows, EndgameSkillRankView rank)
    {
        if (rank.TrainingSynergies.Count > 0)
        {
            rows.Add(("受到以下技能加成", (char)1, true));
            int previousSource = 0;
            foreach (var synergy in rank.TrainingSynergies)
            {
                if (synergy.SourceID != previousSource)
                    rows.Add((synergy.SourceName + "（投入" + synergy.InvestedHardPoints + "点）", (char)1, false));
                string value = synergy.Unit == "percent_bp"
                    ? (synergy.PerPointValue / 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%"
                    : synergy.PerPointValue.ToString(CultureInfo.InvariantCulture);
                rows.Add(("每点: " + synergy.EffectLabel + " +" + value, (char)1, false));
                previousSource = synergy.SourceID;
            }
            rows.Add(("装备加级不增加协同", (char)2, false));
        }
        if (rank.NativeModifierLines.Count > 0)
        {
            rows.Add(("其他技能加成", (char)1, true));
            foreach (string line in rank.NativeModifierLines) rows.Add((line, (char)1, false));
        }
    }

    private static void AppendGrowthEffectLines(List<(string, char, bool)> rows, IReadOnlyList<string> lines)
    {
        foreach (string line in lines) rows.Add((line, (char)0, false));
        if (lines.Count == 0) rows.Add(("服务器尚未提供技能效果", (char)6, false));
    }
}
}
