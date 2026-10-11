using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    internal static string BagTooltip(int nativeTab, InventoryDlg.Item item, string original)
    {
        string result = OverlayTooltip(Current?.State?.Find("bag", nativeTab, item.bagIdx, item.id), original);
        // Stackable materials have no instance UID and therefore need no snapshot overlay.
        string use = MaterialUse(item.id);
        return Current?.State != null && Current.State.Negotiated && use.Length > 0 ? result + "\n\n" + use : result;
    }
    internal static string EquippedTooltip(EquipDlg.EquippedItem item, string original)
    {
        return OverlayTooltip(Current?.State?.Find("equipped", 0, item.slot, item.id), original);
    }
    // Use the native line protocol and layout. Never prepend a second equipment card.
    internal static string OverlayTooltip(EndgameItemView item, string original)
    {
        if (item == null || !MatchesNative(item)) return original;
        if (IsRune(item) || item.SocketCount > 0) Current?.LoadCatalog("runes");
        if (IsRune(item)) return RuneTooltip(item, original);
        if (item.SocketCount > 0 && item.Level > 0) original = ReplaceLevelRequirement(original, item.Level);
        if (item.UniqueID == 0 && item.FormationID == 0) return original;
        EndgameFormationView formation = Formation(item.FormationID);
        if (item.FormationID > 0 && formation == null) Current?.LoadFormationForTooltip();
        var lines = new List<string>((original ?? "").Replace("\\n", "\n").Split('\n'));
        var attributes = new List<string>();
        bool nativeUniqueBlock = false;
        // Old servers put unique details above durability. Move the actual rolled lines,
        // not catalog reference numbers, into the existing attribute section.
        for (int i = 0; i < lines.Count; i++)
        {
            if (PlainLine(lines[i]) != "仙遗·" + item.Name) continue;
            nativeUniqueBlock = true;
            int end = i + 1;
            while (end < lines.Count && lines[end].Length > 0)
            {
                string plain = PlainLine(lines[end]);
                if (!plain.StartsWith("基底：", StringComparison.Ordinal) &&
                    !plain.StartsWith("底材：", StringComparison.Ordinal) &&
                    plain != "同名仙遗特性取最高值；不同特性按通道上限叠加。" &&
                    plain != item.Description)
                    attributes.Add(DarkGold(plain));
                end++;
            }
            lines.RemoveRange(i, end - i);
            break;
        }
        string name = formation != null ? formation.Name : item.Name;
        string suffix = lines.Count > 0 && lines[0].StartsWith("\u0007", StringComparison.Ordinal) ? RefinementSuffix(PlainLine(lines[0])) : "";
        string title = "\u0007" + DarkGold(name + suffix);
        if (lines.Count > 0 && lines[0].StartsWith("\u0007", StringComparison.Ordinal)) lines[0] = title;
        else lines.Insert(0, title);
        // Existing quality markers belong to the native attribute renderer. Preserve all
        // requirement/red/gray markers and the ordinary yellow quality palette.
        for (int i = 1; i < lines.Count; i++)
            if (IsAttributeLine(PlainLine(lines[i])) && !lines[i].StartsWith("\u0002", StringComparison.Ordinal) && !lines[i].StartsWith("\u0004", StringComparison.Ordinal))
                lines[i] = DarkGold(PlainLine(lines[i]));
        if (!nativeUniqueBlock && item.UniqueID > 0)
        {
            foreach (var stat in item.Stats) attributes.Add(DarkGold(StatText(stat)));
            if (item.Trait.Length > 0) attributes.Add(DarkGold(item.Trait));
        }
        if (formation != null)
            // Equal-valued affixes from native gear, uniques and formations are
            // independent contributions, not duplicate presentation blocks.
            foreach (var stat in formation.Stats) attributes.Add(DarkGold(StatText(stat)));
        string status = FormationStatus(item);
        if (status.Length > 0) attributes.Add("\u0002" + status);
        // Socket count is already authoritative in the original description; constituent
        // runes occupy that same section, immediately after its single count line.
        int sockets = lines.FindIndex(line => PlainLine(line).StartsWith("孔数 ", StringComparison.Ordinal));
        int anchor = sockets >= 0 ? sockets : NativeAttributeAnchor(lines);
        lines.InsertRange(anchor, attributes);
        if (item.FormationID > 0)
        {
            string runes = SocketNames(item);
            if (runes.Length > 0) lines.Insert(anchor + attributes.Count + (sockets >= 0 ? 1 : 0), DarkGold(runes));
        }
        if (item.Description.Length > 0) AddDescription(lines, item.Description);
        if (formation != null && formation.Description.Length > 0) AddDescription(lines, formation.Description);
        return string.Join("\n", lines);
    }
    private static int NativeAttributeAnchor(List<string> lines)
    {
        int end = lines.FindIndex(line => PlainLine(line).StartsWith("套装:", StringComparison.Ordinal) || PlainLine(line).StartsWith("法宝技能:", StringComparison.Ordinal));
        if (end < 0) end = lines.Count;
        for (int i = end - 1; i > 0; i--)
        {
            string plain = PlainLine(lines[i]);
            if (IsAttributeLine(plain) || plain.StartsWith("重量 ", StringComparison.Ordinal) || plain.StartsWith("需要", StringComparison.Ordinal) || plain.StartsWith("耐久度:", StringComparison.Ordinal)) return i + 1;
        }
        return Math.Min(1, lines.Count);
    }
    private static void AddDescription(List<string> lines, string description)
    {
        foreach (string line in lines) if (PlainLine(line) == description) return;
        int footer = lines.FindIndex(line => PlainLine(line).StartsWith("（绑定物品不可交易", StringComparison.Ordinal));
        lines.Insert(footer >= 0 ? footer : lines.Count, description);
    }
    private void LoadFormationForTooltip()
    {
        // An owned recipe can be beyond the first catalog page (32 rows). Continue
        // the server cursor lazily until this tooltip resolves or the catalog ends.
        if (State == null || State.CatalogRequested("formations") || !State.CatalogHasMore("formations")) return;
        Send(new EndgameRequest("catalog", catalog: "formations", cursor: State.NextCatalogCursor("formations")));
    }
    private static string RefinementSuffix(string name)
    {
        int start = name.LastIndexOf('+');
        if (start < 0 || start == name.Length - 1) return "";
        for (int i = start + 1; i < name.Length; i++) if (name[i] < '0' || name[i] > '9') return "";
        return name.Substring(start);
    }
    private static string RuneTooltip(EndgameItemView item, string original)
    {
        var lines = new List<string>((original ?? "").Replace("\\n", "\n").Split('\n'));
        if (Current?.State == null) return original;
        foreach (var rune in Current.State.Runes)
        {
            if (rune.ItemID != item.ItemID) continue;
            if (rune.MinLevel > 0 && !lines.Exists(line => PlainLine(line).StartsWith("需要等级: ", StringComparison.Ordinal)))
                lines.Insert(lines.Count > 0 && lines[0].StartsWith("\u0007", StringComparison.Ordinal) ? 1 : 0, "需要等级: " + rune.MinLevel);
            // Native socket-card values are instance data. Never repeat/replace them
            // with reference catalog values when the native description has stats.
            if (!lines.Exists(line => IsAttributeLine(PlainLine(line))))
                foreach (var stat in rune.Stats) AddMissingLine(lines, lines, StatText(stat));
            string mansion = rune.Quadrant + (rune.Mansion.Length > 0 ? (rune.Quadrant.Length > 0 ? " · " : "") + rune.Mansion + "宿" : "");
            if (mansion.Length > 0) AddMissingLine(lines, lines, mansion);
            if (rune.Description.Length > 0) AddMissingLine(lines, lines, rune.Description);
            break;
        }
        return string.Join("\n", lines);
    }
    private static string ReplaceLevelRequirement(string original, int level)
    {
        var lines = (original ?? "").Split('\n');
        for (int i = 0; i < lines.Length; i++)
        {
            string plain = PlainLine(lines[i]);
            if (plain.StartsWith("需要等级: ", StringComparison.Ordinal))
                lines[i] = lines[i].Substring(0, lines[i].Length - plain.Length) + "需要等级: " + level;
        }
        return string.Join("\n", lines);
    }
    internal static string DarkGold(string text) => TooltipContent.QualityName(text, 5);
    private static string PlainLine(string line)
    {
        if (line.Length > 0 && line[0] == '\u0007') line = line.Substring(1);
        if (line.Length >= 2 && line[0] == '\u0003') return line.Substring(2);
        if (line.Length > 0 && line[0] < ' ') return line.Substring(1);
        return line;
    }
    private static bool IsAttributeLine(string line)
    {
        string[] names = { "攻击", "防御", "魔法攻击", "魔法防御", "命中", "负重上限", "力量", "体质", "敏捷", "智慧", "精神", "灵巧",
            "爆击率", "暴击率", "魔法爆击率", "魔法暴击率", "攻击速度", "移动速度", "最小攻击", "最大攻击", "最大生命", "最大法力", "生命恢复", "最大负重", "伤害抗性", "魔法抗性", "不良状态抗性" };
        foreach (string name in names)
            if (line.StartsWith(name + " ", StringComparison.Ordinal) && line.Length > name.Length + 1)
            {
                char value = line[name.Length + 1];
                return char.IsDigit(value) || value == '+' || value == '-';
            }
        return false;
    }
    private static void AddMissingLine(List<string> target, List<string> native, string line)
    {
        string plain = PlainLine(line);
        foreach (string existing in native) if (PlainLine(existing) == plain) return;
        foreach (string existing in target) if (PlainLine(existing) == plain) return;
        target.Add(line);
    }
    private static EndgameFormationView Formation(int id)
    {
        if (id == 0 || Current?.State == null) return null;
        foreach (var row in Current.State.Formations) if (row.ID == id) return row;
        foreach (var row in Current.State.ActiveFormations) if (row.ID == id) return row;
        return null;
    }
    private static string SocketNames(EndgameItemView item)
    {
        var names = new StringBuilder();
        for (int index = 0; index < item.SocketCount; index++)
            foreach (var row in item.Sockets)
                if (row.Index == index && row.ItemID > 0)
                {
                    if (names.Length > 0) names.Append(" · ");
                    names.Append(row.Name.Length > 0 ? row.Name : Current?.RuneName(row.ItemID) ?? "星篆 " + row.ItemID);
                    break;
                }
        return names.ToString();
    }
    internal static string ItemDescription(EndgameItemView item, bool tooltip)
    {
        var text = new StringBuilder();
        if (item.UniqueID > 0 && item.UniqueRollBP > 0) text.Append(item.UID.Length == 0 ? "图鉴基准 · 道意 " : "仙遗余韵 · 道意 ").Append((item.UniqueRollBP / 100f).ToString("0.##", CultureInfo.InvariantCulture)).Append("%\n");
        if (item.Profession != 0) text.Append("职业要求：").Append(ProfessionNames(item.Profession)).Append('\n');
        foreach (var stat in item.Stats) text.Append(tooltip && IsSpecial(item) ? DarkGold(StatText(stat)) : StatText(stat)).Append('\n');
        if (item.Trait.Length > 0) text.Append(tooltip && IsSpecial(item) ? DarkGold(item.Trait) : item.Trait).Append('\n');
        if (item.Description.Length > 0) text.Append('\n').Append(item.Description).Append('\n');
        if (IsRune(item))
        {
            text.Append(RuneDetails(item.ItemID, false));
            if (!tooltip) text.Append("\n在太清演劫录 → 星篆·宿阵，选择器物与空窍后嵌入。\n洞天可寻得星篆；巧匠或左慈的周天鼎可凝髓成篆。\n");
        }
        if (item.SocketCount > 0)
        {
            text.Append("\n宿序 ");
            for (int index = 0; index < item.SocketCount; index++)
            {
                EndgameSocketView found = null; foreach (var row in item.Sockets) if (row.Index == index) found = row;
                text.Append(index == 0 ? "" : " → ").Append(index + 1).Append('·');
                text.Append(found == null || found.ItemID == 0 ? "空窍" : found.Name.Length > 0 ? found.Name : Current != null && Current.State != null ? Current.RuneName(found.ItemID) : "星篆 " + found.ItemID);
            }
            text.Append('\n');
        }
        string status = FormationStatus(item);
        if (status.Length > 0) text.Append('\n').Append(status).Append('\n');
        if (!tooltip && !IsRune(item) && (item.UniqueID > 0 || item.SocketCount > 0))
            text.Append("\n太清演劫录 → 星篆·宿阵可嵌入/取出。\n巧匠或左慈 → 周天鼎可问鼎开窍、调序与炼化，先看耗费再投料。\n");
        if (item.Locked) text.Append("\n器物已锁定，须先解除锁定才能加工。\n");
        if (!tooltip && item.UID.Length > 0) text.Append("\n器物凭证 ").Append(item.UID).Append(" · 修订 ").Append(item.Revision);
        return text.ToString();
    }
    private static string MaterialUse(int id)
    {
        switch (id)
        {
            case 1900028101: return "洞天符钥 · 向妖魔结界守卫问询演劫洞天，开启时消耗。";
            case 1900028201: return "星髓 · 解器归元所得。巧匠或左慈 → 周天鼎，用于开窍、凝篆与淬器；先问鼎查看完整耗费。";
            case 1900028202: return "净煞尘 · 演劫洞天所得。巧匠或左慈 → 周天鼎，用于炼化；先问鼎查看完整耗费。";
            case 1900028203: return "八仙余韵 · 仙遗化韵所得。巧匠或左慈 → 周天鼎，用于返本重铸与炼真守一。";
            default: return "";
        }
    }
    internal static string RuneDetails(int itemID, bool includeName)
    {
        var text = new StringBuilder();
        if (Current?.State == null) return "";
        foreach (var rune in Current.State.Runes)
        {
            if (rune.ItemID != itemID) continue;
            if (includeName) text.Append(rune.Name).Append('\n');
            if (rune.MinLevel > 0) text.Append("需要等级: ").Append(rune.MinLevel).Append('\n');
            text.Append(rune.Quadrant);
            // Older servers carry the mansion in Description and omit Mansion.
            if (rune.Mansion.Length > 0) text.Append(text.Length > 0 ? " · " : "").Append(rune.Mansion).Append("宿");
            if (rune.Description.Length > 0) text.Append('\n').Append(rune.Description);
            foreach (var stat in rune.Stats) text.Append('\n').Append(StatText(stat));
            text.Append('\n'); break;
        }
        return text.ToString();
    }
    internal static string FormationStatus(EndgameItemView item)
    {
        // Active=false is also used for bag items, unmet requirements and broken gear.
        // The current server's aggregate reason cannot prove duplicate suppression.
        // Only an explicit authoritative conflict reason can label the effect inactive.
        if (item.FormationID == 0 || item.Active || item.Location != "equipped") return "";
        switch (item.InactiveReason)
        {
            case "同类宿阵优先级未满足": return "不生效";
            default: return "";
        }
    }
    private static string ProfessionNames(int mask)
    {
        string[] names = { "战士", "剑客", "刺客", "药师", "术士" };
        var text = new StringBuilder();
        for (int i = 0; i < names.Length; i++) if ((mask & (1 << i)) != 0)
        { if (text.Length > 0) text.Append('/'); text.Append(names[i]); }
        return text.Length == 0 ? "依原装备限制" : text.ToString();
    }
    private static string StatText(EndgameStatView stat)
    {
        bool basisPoints = stat.Attr == 23 || stat.Attr == 66;
        string value = basisPoints ? (stat.Value / 100m).ToString("0.##", CultureInfo.InvariantCulture) : stat.Value.ToString(CultureInfo.InvariantCulture);
        return StatName(stat.Attr) + " " + (stat.Value >= 0 ? "+" : "") + value + (basisPoints || stat.Mode == 1 ? "%" : "");
    }
    private static string StatName(int id)
    {
        switch (id)
        {
            case 1: return "力量"; case 3: return "智慧"; case 5: return "体质"; case 7: return "敏捷"; case 9: return "灵巧"; case 61: return "精神";
            case 13: return "防御"; case 15: return "魔法攻击"; case 17: return "魔法防御"; case 19: return "命中"; case 23: return "爆击率";
            case 25: return "攻击速度"; case 27: return "移动速度"; case 29: return "最小攻击"; case 31: return "最大攻击";
            case 34: return "最大生命"; case 36: return "最大法力"; case 38: return "生命恢复"; case 40: return "负重上限"; case 47: return "攻击";
            case 66: return "魔法爆击率"; case 69: return "伤害抗性"; case 70: return "魔法抗性"; case 118: return "不良状态抗性";
            default: return "属性 " + id;
        }
    }
}
}
