using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    // Original AppendDmgBonusRow 06001029; row color 1, non-heading.
    private static void AppendDmgBonusRow(List<(string, char, bool)> rows, SkInfo info)
    {
        if (CharData.skillDmgBonus == null ||
            !CharData.skillDmgBonus.TryGetValue(info.id, out int bonus) || bonus == 0) return;
        rows.Add((string.Format("已学被动使本技能伤害{0}{1}%", bonus > 0 ? "提升" : "降低",
            Mathf.Abs(bonus)), (char)1, false));
    }
}

}
