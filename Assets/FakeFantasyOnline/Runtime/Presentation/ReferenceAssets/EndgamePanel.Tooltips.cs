using System.Globalization;
using System.Text;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    internal static string BagTooltip(int nativeTab, InventoryDlg.Item item, string original)
    {
        return OverlayTooltip(Current?.State?.Find("bag", nativeTab, item.bagIdx, item.id), original);
    }
    internal static string EquippedTooltip(EquipDlg.EquippedItem item, string original)
    {
        return OverlayTooltip(Current?.State?.Find("equipped", 0, item.slot, item.id), original);
    }
    private static string OverlayTooltip(EndgameItemView item, string original)
    {
        if (item == null || !MatchesNative(item)) return original;
        return (item.UniqueID > 0 ? TooltipContent.QualityName(item.Name, 4) + "\n" : "") + original + "\n" + ItemDescription(item, true);
    }
    internal static string ItemDescription(EndgameItemView item, bool tooltip)
    {
        var text = new StringBuilder();
        if (item.UniqueID > 0) text.Append("仙遗余韵 · 道意 ").Append((item.UniqueRollBP / 100f).ToString("0.##", CultureInfo.InvariantCulture)).Append("%\n");
        if (item.Profession.Length > 0) text.Append("职业：").Append(item.Profession).Append('\n');
        if (item.Trait.Length > 0) text.Append('\n').Append(item.Trait).Append('\n');
        if (!tooltip && item.Description.Length > 0) text.Append('\n').Append(item.Description).Append('\n');
        foreach (var stat in item.Stats) text.Append(StatText(stat)).Append('\n');
        if (item.SocketCount > 0)
        {
            text.Append("\n宿序 ");
            for (int index = 0; index < item.SocketCount; index++)
            {
                EndgameSocketView found = null; foreach (var row in item.Sockets) if (row.Index == index) found = row;
                text.Append(index == 0 ? "" : " → ").Append(index + 1).Append('·');
                text.Append(found == null || found.ItemID == 0 ? "空窍" : found.Name.Length > 0 ? found.Name : "星篆");
            }
            text.Append('\n');
        }
        if (item.FormationID > 0) text.Append(item.Active ? "\n宿阵已生效" : "\n宿阵：" + item.InactiveReason).Append('\n');
        if (!tooltip) text.Append("\n器物凭证 ").Append(item.UID).Append(" · 修订 ").Append(item.Revision);
        return text.ToString();
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
