using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class Win05
{
    // Original CastReducePct 0600102b.
    private static int CastReducePct()
    {
        int reduction = 0;
        if (CharData.skills != null)
        {
            if (CharData.skills.TryGetValue(0x284a, out int first) && first > 0)
                reduction = Mathf.Max(0, 15 + first * 5);
            if (CharData.skills.TryGetValue(0x28af, out int second) && second > 0)
                reduction = Mathf.Max(reduction, 15 + second * 5);
        }
        return Mathf.Min(reduction, 80);
    }

    // Original AppendLevelBlock 0600102a.
    private static void AppendLevelBlock(List<(string, char, bool)> rows, SkInfo info, int level,
        string levelLine, string title)
    {
        rows.Add((title, title == info.name ? (char)5 : (char)0, true));
        if (levelLine != null) rows.Add((levelLine, (char)0, true));
        SkLevel selected = null;
        if (info.levels != null)
            foreach (SkLevel candidate in info.levels)
                if (candidate.lv == level) { selected = candidate; break; }
        string description = selected != null && !string.IsNullOrEmpty(selected.desc)
            ? selected.desc : info.desc ?? string.Empty;
        description = BakeDesc(description, level);
        if (!string.IsNullOrEmpty(description)) rows.Add((description, (char)0, false));
        if (info.tgt == 18 || info.type == 19 || info.type == 20) return;
        rows.Add(("消耗: " + (selected != null && selected.mp > 0 ? selected.mp + "点法力" : "无"), (char)0, false));
        int cast = selected == null ? 0 : selected.cast;
        int reduction = CastReducePct();
        if (cast > 0 && reduction != 0) cast = Mathf.Max(200, (100 - reduction) * cast / 100);
        rows.Add(("技能吟唱: " + (cast > 0 ? string.Format("{0:0.##}秒", cast * 0.001f) : "瞬发"), (char)0, false));
        rows.Add((string.Format("技能间隔: {0:0.#}秒", selected == null ? 0f : selected.cd * 0.1f), (char)0, false));
    }
}

}
