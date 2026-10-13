using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class Win05
{
    private static bool UsesGrowthSkill(int id) => EndgamePanel.GrowthState != null && EndgameSkillRankView.IsProfessionSkill(id);
    private static int DisplayedSkillPoints => EndgamePanel.GrowthState != null ? EndgamePanel.GrowthState.UnspentSkillPoints : CharData.skillPoints;

    // Original ShowSkillTip / SkillTipText structure (pre-V5 ababca5): the complete
    // description and native cost/cast/interval block are repeated for the next rank.
    // Only their rank-dependent values come from the server. Never replace the
    // authored prose with a changed-only summary or a generic list of effects.
    private static string BuildGrowthSkillTip(SkInfo info, bool includeNext)
    {
        var state = EndgamePanel.GrowthState;
        var rank = state.SkillRank(info.id);
        var rows = new List<(string, char, bool)>();
        if (rank == null || state.SkillBehaviorV1 && state.SkillRanksRevision != state.Revision)
        {
            rows.Add((info.name, (char)5, true));
            rows.Add(("正在等待服务器技能资料", (char)6, false));
            return ComposeTip(rows);
        }

        if (state.SkillBehaviorV1 && info.id != 10212 && rank.Contract == null)
        {
            rows.Add((info.name, (char)5, true));
            rows.Add(("正在等待服务器技能资料", (char)6, false));
            return ComposeTip(rows);
        }

        AppendGrowthLevelBlock(rows, rank.CanCast ? rank.CurrentEffectLines : rank.NextEffectLines,
            rank.CanCast ? "等级: " + rank.EffectiveRank : "等级: 未学习", info.name, true);
        if (includeNext)
        {
            if (!rank.CanCast) AppendGrowthRequirement(rows, rank);
            if (rank.CanCast && rank.HardRank < state.SkillHardCap && rank.NextEffectLines.Count > 0)
            {
                rows.Add((string.Empty, (char)0, false));
                AppendGrowthLevelBlock(rows, rank.NextEffectLines, null, "下一等级", false);
                AppendGrowthRequirement(rows, rank);
            }
        }

        // This is the original AppendDmgBonusRow position and color. The server
        // supplies the native passive modifiers using the authoritative ranks.
        foreach (string line in rank.NativeModifierLines)
            if (IsOriginalDamageBonus(line)) rows.Add((line, (char)1, false));
        if (includeNext)
        {
            rows.Add((string.Empty, (char)0, false));
            string pointsLine = "您现在的剩余技能点数为" + state.UnspentSkillPoints;
            if (state.Pending) pointsLine += "（确认中）";
            else if (state.SkillRanksRevision != state.Revision) pointsLine += "（更新中）";
            rows.Add((pointsLine, (char)6, false));
            if (!state.CanInvestSkill(info.id)) rows.Add(("您现在还无法升级这个技能", (char)6, false));
        }
        if (SkillUsableForHotbar(info)) rows.Add(("双击或拖曳至快捷栏以使用这个技能", (char)6, false));

        // A single append-only extension, on the receiving skill. Never enumerate
        // a source skill's outgoing targets or calculate contributions locally.
        AppendGrowthSynergies(rows, rank);
        return ComposeTip(rows);
    }

    private static void AppendGrowthLevelBlock(List<(string, char, bool)> rows,
        IReadOnlyList<string> lines, string levelLine, string title, bool named)
    {
        rows.Add((title, named ? (char)5 : (char)0, true));
        if (levelLine != null) rows.Add((levelLine, (char)0, true));
        foreach (string line in lines) rows.Add((line, (char)0, false));
        if (lines.Count == 0) rows.Add(("服务器尚未提供技能效果", (char)6, false));
    }

    private static void AppendGrowthRequirement(List<(string, char, bool)> rows, EndgameSkillRankView rank)
    {
        if (rank.NextRequiredLevel > 0) rows.Add(("需要等级: " + rank.NextRequiredLevel, (char)1, false));
        if (rank.Contract != null)
        {
            foreach (int id in rank.Contract.PrerequisiteSkillIDs)
            {
                var prerequisite = EndgamePanel.GrowthState.SkillRank(id);
                if (prerequisite?.HardRank > 0) continue;
                string name = prerequisite?.Contract?.Name ?? "技能 " + id;
                rows.Add(("需要技能: " + name, (char)1, false));
            }
        }
    }

    private static bool IsOriginalDamageBonus(string line) =>
        line.StartsWith("已学被动使本技能伤害", System.StringComparison.Ordinal);

    private static void AppendGrowthSynergies(List<(string, char, bool)> rows, EndgameSkillRankView rank)
    {
        bool hasNativeIncoming = false;
        foreach (string line in rank.NativeModifierLines)
            if (!IsOriginalDamageBonus(line)) { hasNativeIncoming = true; break; }
        if (rank.TrainingSynergies.Count == 0 && !hasNativeIncoming) return;
        rows.Add((string.Empty, (char)0, false));
        rows.Add(("受到以下技能加成", (char)1, true));
        int previousSource = 0;
        foreach (var synergy in rank.TrainingSynergies)
        {
            if (synergy.SourceID != previousSource)
                rows.Add((synergy.SourceName + (synergy.EquipmentApplies
                    ? "（等级" + synergy.SourceEffectiveRank + "）"
                    : "（投入" + synergy.InvestedHardPoints + "点）"), (char)1, true));
            string value = synergy.Unit == "percent_bp"
                ? (synergy.PerPointValue / 100m).ToString("0.##", CultureInfo.InvariantCulture) + "%"
                : synergy.PerPointValue.ToString(CultureInfo.InvariantCulture);
            rows.Add(((synergy.EquipmentApplies ? "每级: " : "每点: ") + synergy.EffectLabel + " +" + value, (char)1, true));
            previousSource = synergy.SourceID;
        }
        foreach (string line in rank.NativeModifierLines)
            if (!IsOriginalDamageBonus(line)) rows.Add((line, (char)1, true));
    }
}
}
