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
        var effects = new List<string>();
        bool nativeStats = Current?.State?.NativeEquipmentStats == true;
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
                string plain = EquipmentAttributeText(PlainLine(lines[end]));
                if (!plain.StartsWith("基底：", StringComparison.Ordinal) &&
                    !plain.StartsWith("底材：", StringComparison.Ordinal) &&
                    plain != "同名仙遗特性取最高值；不同特性按通道上限叠加。" &&
                    !DescriptionContainsLine(item.Description, plain))
                {
                    bool ordinary = IsAttributeLine(plain) || lines[end].StartsWith("\u00035", StringComparison.Ordinal) && !TraitContainsLine(item.Trait, plain);
                    if (!lines[end].StartsWith("\u0006", StringComparison.Ordinal) && ordinary) attributes.Add(DarkGold(plain));
                    else effects.Add(Orange(plain));
                }
                end++;
            }
            lines.RemoveRange(i, end - i);
            break;
        }
        string name = formation != null ? FormationName(formation.Name) : item.FormationID > 0 ? FormationName(item.Name) : item.Name;
        string suffix = lines.Count > 0 && lines[0].StartsWith("\u0007", StringComparison.Ordinal) ? RefinementSuffix(PlainLine(lines[0])) : "";
        string title = "\u0007" + DarkGold(name + suffix);
        // Native item titles use protocol qualities 0-4. An exact centered
        // dark-gold title with the legacy block already removed is our own
        // overlay; do not append independent legacy contributions a second time.
        bool alreadyOverlaid = !nativeUniqueBlock && lines.Count > 0 && lines[0] == title;
        if (lines.Count > 0 && lines[0].StartsWith("\u0007", StringComparison.Ordinal) && !IsNativeSlotLine(lines[0])) lines[0] = title;
        else lines.Insert(0, title);
        // Orange is an authoritative effect marker, including future effect text.
        // Keep white base stats and requirement/set markers intact. New ordinary
        // attributes already carry dark gold; legacy text needs a small fallback.
        for (int i = 1; i < lines.Count; i++)
        {
            string plain = EquipmentAttributeText(PlainLine(lines[i]));
            bool coloredAttribute = lines[i].StartsWith("\u0003", StringComparison.Ordinal) && !lines[i].StartsWith("\u00030", StringComparison.Ordinal) ||
                lines[i].StartsWith("\u0001", StringComparison.Ordinal) || lines[i].StartsWith("\u0005", StringComparison.Ordinal);
            bool authored = DescriptionContainsLine(item.Description, plain) || formation != null && DescriptionContainsLine(formation.Description, plain);
            bool traitEffect = TraitContainsLine(item.Trait, plain);
            bool legacyEffect = !lines[i].StartsWith("\u00035", StringComparison.Ordinal) || traitEffect;
            if (lines[i].StartsWith("\u0006", StringComparison.Ordinal) ||
                !authored && legacyEffect && IsLegacyEquipmentEffect(plain, item.Trait) && (coloredAttribute || traitEffect && lines[i] == PlainLine(lines[i])))
            {
                effects.Add(Orange(plain));
                lines.RemoveAt(i--);
                continue;
            }
            if (plain.StartsWith("孔数 ", StringComparison.Ordinal) ||
                (IsAttributeLine(plain) && coloredAttribute) || (lines[i] == PlainLine(lines[i]) && IsAdditionalAttributeLine(plain)))
                lines[i] = DarkGold(plain);
        }
        if (!nativeStats && !alreadyOverlaid && !nativeUniqueBlock && item.UniqueID > 0)
        {
            foreach (var stat in item.Stats) attributes.Add(DarkGold(StatText(stat)));
        }
        // Older payloads mix additional ordinary attributes and effects in Trait.
        // Normalize before comparing so the renamed local/MF lines never duplicate
        // an authoritative native row, and never sum separate attribute scopes here.
        foreach (string trait in item.Trait.Split('；'))
        {
            string plain = EquipmentAttributeText(trait);
            if (plain.Length == 0) continue;
            if (IsAttributeLine(plain)) AddMissingLine(attributes, lines, DarkGold(plain));
            else AddMissingLine(effects, lines, Orange(plain));
        }
        if (!nativeStats && !alreadyOverlaid && formation != null)
            // Legacy servers do not include formation attributes in the native
            // description. Negotiated servers already merge all actual extras
            // by attribute/mode/scope, so catalog reference values stay out.
            foreach (var stat in formation.Stats) attributes.Add(DarkGold(StatText(stat)));
        string status = FormationStatus(item);
        if (status.Length > 0) AddMissingLine(attributes, lines, "\u0002" + status);
        // Keep the authoritative socket count and native attribute section intact.
        int sockets = lines.FindIndex(line => PlainLine(line).StartsWith("孔数 ", StringComparison.Ordinal));
        int anchor = sockets >= 0 ? sockets : NativeAttributeAnchor(lines);
        lines.InsertRange(anchor, attributes);
        if (item.FormationID > 0)
        {
            string runes = SocketInitials(item);
            string composition = "\u0007" + DarkGold(runes);
            if (runes.Length > 0 && !lines.Contains(composition)) lines.Insert(NativeCompositionAnchor(lines), composition);
        }
        if (item.Description.Length > 0) AddDescription(lines, item.Description);
        if (formation != null && formation.Description.Length > 0) AddDescription(lines, formation.Description);
        // Effects are the final block, after native functional sections, lore and
        // binding notices. Moving native rows preserves their values and ordering.
        lines.AddRange(effects);
        return string.Join("\n", lines);
    }
    private static int NativeCompositionAnchor(List<string> lines)
    {
        // Native equipment descriptions begin with a centered "(武器)"/slot row.
        // Locate that row, rather than assuming all callers include the title.
        for (int i = 1; i < lines.Count; i++)
        {
            if (IsNativeSlotLine(lines[i])) return i + 1;
        }
        // Older/incomplete descriptions have no slot heading: keep the composition
        // under the title without moving requirements, attributes or authored lore.
        return Math.Min(1, lines.Count);
    }
    private static bool IsNativeSlotLine(string line)
    {
        string plain = PlainLine(line);
        return line.StartsWith("\u0007", StringComparison.Ordinal) && plain.StartsWith("(", StringComparison.Ordinal) && plain.EndsWith(")", StringComparison.Ordinal);
    }
    private static string FormationName(string name) => name.StartsWith("宿阵·", StringComparison.Ordinal) ? name.Substring(3) : name;
    private static int NativeAttributeAnchor(List<string> lines)
    {
        int end = lines.FindIndex(line => PlainLine(line).StartsWith("套装:", StringComparison.Ordinal) || PlainLine(line).StartsWith("法宝技能:", StringComparison.Ordinal));
        if (end < 0) end = lines.Count;
        for (int i = end - 1; i > 0; i--)
        {
            string plain = PlainLine(lines[i]);
            if (IsAttributeLine(plain) || lines[i].StartsWith("\u00035", StringComparison.Ordinal) || plain.StartsWith("重量 ", StringComparison.Ordinal) || plain.StartsWith("需要", StringComparison.Ordinal) || plain.StartsWith("耐久度:", StringComparison.Ordinal)) return i + 1;
        }
        return Math.Min(1, lines.Count);
    }
    private static void AddDescription(List<string> lines, string description)
    {
        foreach (string part in description.Replace("\\n", "\n").Split('\n'))
        {
            if (part.Length == 0 || lines.Exists(line => PlainLine(line) == part)) continue;
            int footer = lines.FindIndex(line => PlainLine(line).StartsWith("（绑定物品不可交易", StringComparison.Ordinal));
            lines.Insert(footer >= 0 ? footer : lines.Count, part);
        }
    }
    private static bool DescriptionContainsLine(string description, string plain)
    {
        foreach (string part in description.Replace("\\n", "\n").Split('\n')) if (part == plain) return true;
        return false;
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
                foreach (var stat in rune.Stats) AddMissingLine(lines, lines, RuneStatText(rune.ItemID, stat));
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
    private static string Orange(string text) => "\u0006" + text;
    private static string PlainLine(string line)
    {
        if (line.Length > 0 && line[0] == '\u0007') line = line.Substring(1);
        if (line.Length >= 2 && line[0] == '\u0003') return line.Substring(2);
        if (line.Length > 0 && line[0] < ' ') return line.Substring(1);
        return line;
    }
    private static bool IsAttributeLine(string line)
    {
        if (IsAdditionalAttributeLine(line)) return true;
        if (line.StartsWith("有效穿戴时每", StringComparison.Ordinal) && line.Contains("秒恢复") && line.EndsWith("点生命", StringComparison.Ordinal)) return true;
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
    private static string EquipmentAttributeText(string text)
    {
        if (text.StartsWith("染色率 ", StringComparison.Ordinal))
            return "魔法装备获取几率提升" + text.Substring(3);
        foreach (string name in new[] { "攻击", "魔法攻击", "防御", "魔法防御" })
        {
            string oldPrefix = "本件基础" + name + "强化 ";
            if (text.StartsWith(oldPrefix, StringComparison.Ordinal)) text = text.Substring(4);
            if (text.StartsWith(name + "强化 ", StringComparison.Ordinal) && text.EndsWith("（本件）", StringComparison.Ordinal))
                return text.Substring(0, text.Length - 4);
        }
        return text;
    }
    private static bool IsAdditionalAttributeLine(string line)
    {
        string[] names = { "攻击强化", "魔法攻击强化", "防御强化", "魔法防御强化", "魔法装备获取几率提升",
            "火系伤害", "冰系伤害", "圣系伤害", "暗系伤害", "敌人火系抗性", "敌人冰系抗性", "敌人圣系抗性", "敌人暗系抗性",
            "火系抗性", "冰系抗性", "圣系抗性", "暗系抗性", "火系伤害减免", "冰系伤害减免", "圣系伤害减免", "暗系伤害减免",
            "火系伤害吸收", "冰系伤害吸收", "圣系伤害吸收", "暗系伤害吸收" };
        foreach (string name in names)
            if (line.StartsWith(name + " ", StringComparison.Ordinal) && IsNumericAttributeValue(line.Substring(name.Length + 1), false)) return true;
        // A named skill's +N级 is an ordinary rank bonus, regardless of its name.
        int value = line.LastIndexOf(" +", StringComparison.Ordinal);
        return value > 0 && line.EndsWith("级", StringComparison.Ordinal) && IsNumericAttributeValue(line.Substring(value + 1, line.Length - value - 2), true);
    }
    private static bool IsNumericAttributeValue(string value, bool integer)
    {
        if (value.Length == 0) return false;
        int start = value[0] == '+' || value[0] == '-' ? 1 : 0;
        int end = !integer && value.EndsWith("%", StringComparison.Ordinal) ? value.Length - 1 : value.Length;
        bool digit = false;
        for (int i = start; i < end; i++)
        {
            if (value[i] >= '0' && value[i] <= '9') digit = true;
            else if (integer || value[i] != '.') return false;
        }
        return digit;
    }
    private static bool IsLegacyEquipmentEffect(string plain, string traits)
    {
        if (IsAttributeLine(plain)) return false;
        if (TraitContainsLine(traits, plain)) return true;
        // Compatibility for native legacy/V5 text predating the orange marker.
        // Ordinary element percentages and skill ranks have distinct numeric forms.
        return plain.Contains("光环：") || plain.Contains("概率") && plain.Contains("释放") ||
            plain.Contains("伤害提高") || plain.Contains("受到伤害降低") || plain.Contains("直接治疗提高") ||
            plain.Contains("技能法力消耗降低") || plain.Contains("技能吟唱时间缩短") ||
            plain.Contains("技能尺寸扩大") || plain.Contains("陷阱触发半径扩大") ||
            plain.Contains("击杀怪物恢复最大");
    }
    private static bool TraitContainsLine(string traits, string plain)
    {
        foreach (string trait in traits.Split('；'))
            if (plain.Length > 0 && EquipmentAttributeText(trait) == plain) return true;
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
    private static string SocketInitials(EndgameItemView item)
    {
        var names = new StringBuilder();
        for (int index = 0; index < item.SocketCount; index++)
            foreach (var row in item.Sockets)
                if (row.Index == index && row.ItemID > 0)
                {
                    names.Append(RuneInitial(row));
                    break;
                }
        return names.ToString();
    }
    private static string RuneInitial(EndgameSocketView socket)
    {
        string name = socket.Name;
        if (Current?.State != null)
            foreach (var rune in Current.State.Runes)
                if (rune.ItemID == socket.ItemID)
                {
                    if (rune.Mansion.Length > 0) return StringInfo.GetNextTextElement(rune.Mansion);
                    if (name.Length == 0) name = rune.Name;
                    break;
                }
        if (name.StartsWith("星篆·", StringComparison.Ordinal)) name = name.Substring(3);
        // Missing metadata is temporary while the requested rune catalog loads.
        return name.Length > 0 ? StringInfo.GetNextTextElement(name) : "？";
    }
    internal static string ItemDescription(EndgameItemView item, bool tooltip)
    {
        var text = new StringBuilder();
        var effects = new List<string>();
        if (item.UniqueID > 0 && item.UniqueRollBP > 0 && Current?.State?.NativeEquipmentStats != true) text.Append(item.UID.Length == 0 ? "图鉴基准 · 道意 " : "仙遗余韵 · 道意 ").Append((item.UniqueRollBP / 100f).ToString("0.##", CultureInfo.InvariantCulture)).Append("%\n");
        if (item.Profession != 0) text.Append("职业要求：").Append(ProfessionNames(item.Profession)).Append('\n');
        foreach (var stat in item.Stats) text.Append(tooltip && IsSpecial(item) ? DarkGold(StatText(stat)) : StatText(stat)).Append('\n');
        foreach (string trait in item.Trait.Split('；'))
        {
            string plain = EquipmentAttributeText(trait);
            if (plain.Length == 0) continue;
            if (tooltip && IsSpecial(item) && !IsAttributeLine(plain)) effects.Add(Orange(plain));
            else text.Append(tooltip && IsSpecial(item) ? DarkGold(plain) : plain).Append('\n');
        }
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
            text.Append("\n太清演劫录 → 星篆·宿阵可嵌入/取出。\n打孔炉逐孔开启；巧匠或左慈 → 周天鼎可为无孔器物随机开窍，或调序、炼化。先问鼎查看耗费。\n");
        if (item.Locked) text.Append("\n器物已锁定，须先解除锁定才能加工。\n");
        if (!tooltip && item.UID.Length > 0) text.Append("\n器物凭证 ").Append(item.UID).Append(" · 修订 ").Append(item.Revision);
        foreach (string effect in effects) text.Append(effect).Append('\n');
        return text.ToString();
    }
    private static string MaterialUse(int id)
    {
        switch (id)
        {
            case 1900028101: return "洞天符钥 · 向妖魔结界守卫问询演劫洞天，开启时消耗。";
            case 1900028201: return "星髓 · 解器归元所得。巧匠或左慈 → 周天鼎，用于随机开窍、凝篆与淬器；先问鼎查看完整耗费。";
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
            foreach (var stat in rune.Stats) text.Append('\n').Append(RuneStatText(rune.ItemID, stat));
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
    private static string RuneStatText(int itemID, EndgameStatView stat)
    {
        if (itemID == 1900028028 && stat.Attr == 38 && stat.Mode == 0 && stat.Value > 0)
            return "有效穿戴时每2秒恢复" + stat.Value.ToString(CultureInfo.InvariantCulture) + "点生命";
        return StatText(stat);
    }
    private static string StatText(EndgameStatView stat)
    {
        bool basisPoints = stat.Attr == 23 || stat.Attr == 66;
        // Native attr25 percentage is stored as a negative interval entry but
        // consumed as positive player attack-speed points (base / speed).
        long displayValue = stat.Attr == 25 && stat.Mode == 1 ? -(long)stat.Value : stat.Value;
        string value = basisPoints ? (displayValue / 100m).ToString("0.##", CultureInfo.InvariantCulture) : displayValue.ToString(CultureInfo.InvariantCulture);
        return StatName(stat.Attr) + " " + (displayValue >= 0 ? "+" : "") + value + (basisPoints || stat.Mode == 1 ? "%" : "");
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
