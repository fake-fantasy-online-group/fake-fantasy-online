using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class PetInfoTooltip
{
    private static void Draw(PetBrief b, Vector2 mouse, float s, string actionHint = null)
    {
        if (!BitmapFont.Ready) return;
        DrawLines(BuildLines(b, actionHint), mouse, s);
    }

    internal static List<(string, Color)> BuildLines(PetBrief b, string actionHint = null)
    {
        Color heading = new Color(1f, 0.85f, 0.3f, 1f);
        Color body = new Color32(225, 220, 195, 255);
        Color accent = new Color(1f, 0.9f, 0.35f, 1f);
        Color fullColor = new Color32(255, 140, 140, 255);
        var lines = new List<(string, Color)>();
        if (b.opened == 0)
        {
            lines.Add(("魂之精", heading));
            lines.Add(("（未孵化）", body));
            lines.Add(("类别: " + b.species, body));
            lines.Add(("成长: " + (CharData.PetTierName(b.growTier).Length > 0
                ? CharData.PetTierName(b.growTier) + " 档" : "—"), accent));
            lines.Add(("说明:", heading));
            lines.Add(("这是你捕捉到的怪物的魂魄", body));
            lines.Add(("双击可孵化, 随机生成成长型与性别", accent));
            lines.Add(("(成长档由种类决定, 已经定死)", body));
            return lines;
        }

        lines.Add(("魂之精", heading));
        lines.Add(("（" + b.prename + b.species + "）", body));
        lines.Add(("未有变形功能", new Color(1f, 0.4f, 0.35f, 1f)));
        lines.Add(("性别: " + (b.gender != 0 ? "雄性" : "雌性"), body));
        lines.Add(("食性: " + (b.habit < HabitName.Length ? HabitName[b.habit] : "未知"), body));
        lines.Add(("成长: " + (CharData.PetTierName(b.growTier).Length > 0
            ? CharData.PetTierName(b.growTier) + " 档" : "—"), accent));
        lines.Add(("等级: " + b.level.ToString(), body));
        lines.Add(("力量: " + b.str.ToString(), body));
        lines.Add(("智慧: " + b.intel.ToString(), body));
        lines.Add(("体力: " + b.vit.ToString(), body));
        lines.Add(("敏捷: " + b.agi.ToString(), body));
        lines.Add(("饥渴: " + b.starve.ToString(), body));
        lines.Add(("信任: " + b.trust.ToString(), body));
        lines.Add(("拥有技能:", heading));
        if (CharData.PetTierName(b.growTier).Length == 0)
            lines.RemoveAll(t => t.Item1.StartsWith("成长: "));
        if (b.bound) lines.Insert(2, ("（绑定）（不可解除绑定）", heading));
        if (b.locked)
            lines.Insert(2, ("【已加锁】不能交易/摆摊/邮寄/丢弃 (Shift+右键 解锁)", new Color32(255, 210, 120, 255)));
        if (b.ppAiCap > 0)
        {
            string text = string.Format("宠爱PP果: 已用 {0}/{1}", b.ppAiUsed, b.ppAiCap) +
                (b.ppAiUsed < b.ppAiCap ? string.Format("  可再用 {0}", unchecked(b.ppAiCap - b.ppAiUsed)) : " (已满)");
            lines.Insert(lines.Count - 1, (text, b.ppAiUsed >= b.ppAiCap ? fullColor : body));
        }
        if (b.ppShCap > 0)
        {
            string text = string.Format("圣化PP果: 已用 {0}/{1}", b.ppShUsed, b.ppShCap) +
                (b.ppShUsed < b.ppShCap ? string.Format("  可再用 {0}", unchecked(b.ppShCap - b.ppShUsed)) : " (已满)");
            lines.Insert(lines.Count - 1, (text, b.ppShUsed >= b.ppShCap ? fullColor : body));
        }
        else if (b.ppAiCap > 0)
            lines.Insert(lines.Count - 1, ("圣化PP果: 需先用宠物圣化卡圣化", new Color32(170, 170, 160, 255)));
        if (b.skills != null)
            foreach (var skill in b.skills)
                if (skill.Item2 > 0)
                    lines.Add(("  " + skill.Item1 + "  " + skill.Item2.ToString(), new Color32(150, 220, 255, 255)));
        lines.Add(("说明:", heading));
        lines.Add(("这是你捕捉到的怪物的魂魄。", body));
        lines.Add(("最多加成" + b.freePts.ToString() + "个自由点数，已使用" + b.freeUsed.ToString() + "点。", body));
        lines.Add((b.ppShCap > 0 ? "该宠物 已圣化" : "该宠物 未圣化", accent));
        if (!string.IsNullOrEmpty(actionHint))
            foreach (string line in actionHint.Split('\n')) lines.Add((line, accent));
        return lines;
    }
}
}
