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
    private static string OverlayTooltip(EndgameItemView item, string original)
    {
        if (item == null || !MatchesNative(item)) return original;
        if (IsRune(item) || item.SocketCount > 0) Current?.LoadCatalog("runes");
        return (item.UniqueID > 0 ? TooltipContent.QualityName(item.Name, 4) + "\n" : "") + original + "\n" + ItemDescription(item, true);
    }
    internal static string ItemDescription(EndgameItemView item, bool tooltip)
    {
        var text = new StringBuilder();
        if (item.UniqueID > 0 && item.UniqueRollBP > 0) text.Append(item.UID.Length == 0 ? "图鉴基准 · 道意 " : "仙遗余韵 · 道意 ").Append((item.UniqueRollBP / 100f).ToString("0.##", CultureInfo.InvariantCulture)).Append("%\n");
        if (item.Profession != 0) text.Append("职业要求：").Append(ProfessionNames(item.Profession)).Append('\n');
        if (item.Trait.Length > 0) text.Append('\n').Append(item.Trait).Append('\n');
        if (!tooltip && item.Description.Length > 0) text.Append('\n').Append(item.Description).Append('\n');
        foreach (var stat in item.Stats) text.Append(StatText(stat)).Append('\n');
        if (IsRune(item))
        {
            text.Append(RuneDetails(item.ItemID, false));
            text.Append("\n在太清演劫录 → 星篆·宿阵，选择器物与空窍后嵌入。\n洞天可寻得星篆；巧匠或左慈的周天鼎可凝髓成篆。\n");
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
            text.Append("\n孔数、从左到右的宿序与装备部位须完全符合宿阵；多一空窍也不能成阵。\n");
        }
        if (item.FormationID > 0) text.Append('\n').Append(FormationStatus(item)).Append('\n');
        else if (item.SocketCount > 0) text.Append("宿阵尚未成形；已嵌星篆仍有自身属性。\n");
        if (!IsRune(item) && (item.UniqueID > 0 || item.SocketCount > 0))
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
        string name = "宿阵";
        if (Current?.State != null)
        {
            foreach (var row in Current.State.Formations) if (row.ID == item.FormationID) { name = row.Name; break; }
            foreach (var row in Current.State.ActiveFormations) if (row.ID == item.FormationID) { name = row.Name; break; }
        }
        if (item.Active) return name + " · 已生效";
        if (item.Location == "bag") return name + " · 已成形，穿戴后还须满足职业、等级与耐久要求；同类宿阵只生效一个。";
        return name + " · 未生效：" + (item.InactiveReason.Length > 0 ? item.InactiveReason : "请核对穿戴、职业、等级、耐久及同类宿阵优先级。");
    }
    private static string ProfessionNames(int mask)
    {
        string[] names = { "战士", "剑客", "刺客", "药师", "术士" };
        var text = new StringBuilder();
        for (int i = 0; i < names.Length; i++) if ((mask & (1 << i)) != 0)
        { if (text.Length > 0) text.Append('/'); text.Append(names[i]); }
        return text.Length == 0 ? "依原装备限制" : text.ToString();
    }
    private static string StatText(EndgameStatView stat) => StatName(stat.Attr) + "：" + (stat.Value >= 0 ? "+" : "") + stat.Value + (stat.Mode == 1 ? "%" : "");
    private static string StatName(int id)
    {
        switch (id)
        {
            case 1: return "力量"; case 3: return "智慧"; case 5: return "体质"; case 7: return "敏捷"; case 9: return "灵巧"; case 61: return "精神";
            case 13: return "防御"; case 15: return "魔法攻击"; case 17: return "魔法防御"; case 19: return "命中"; case 23: return "爆击(万分比)";
            case 25: return "攻击间隔"; case 27: return "移动速度"; case 29: return "最小攻击"; case 31: return "最大攻击";
            case 34: return "最大生命"; case 36: return "最大法力"; case 38: return "生命恢复"; case 40: return "最大负重"; case 47: return "攻击";
            case 66: return "魔法爆击(万分比)"; case 69: return "伤害抗性"; case 70: return "魔法抗性"; case 118: return "不良状态抗性";
            default: return "属性 " + id;
        }
    }
}
}
