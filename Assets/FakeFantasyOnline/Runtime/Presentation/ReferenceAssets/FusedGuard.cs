using System;
using System.Collections.Generic;
using System.Text;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static class FusedGuard
{
    public static bool Has(InventoryDlg.Item item) => !string.IsNullOrEmpty(item.fusedDesc);
    public static string What(InventoryDlg.Item item) => item.fusedDesc ?? "";

    public static void Confirm(InventoryDlg.Item item, string verb, Action go)
    {
        if (!Has(item)) { go(); return; }
        SysMsgBox.Show("【" + item.name + "】上融合着 " + What(item) + "。\n" + verb +
            "会把它一起带走, 且不会自动退回背包。\n\n(想留下融合的部分: 先用分离卷轴取下来, 神奇货架 5 彩玉)\n\n确定要" + verb + "吗？", go, null);
    }

    public static void ConfirmAddToSell(InventoryDlg.Item item, Action go)
    {
        if (!Has(item)) { go(); return; }
        SysMsgBox.Show("【" + item.name + "】\n—— " + What(item) +
            "\n\n★卖给商店后无法找回, 也不会退回背包。\n(带融合外观的: 想留下外观就先用分离卷轴取下来, 神奇货架 5 彩玉)\n\n确定要把它加入出售清单吗？", go, () => { });
    }

    public static void ConfirmBatch(List<InventoryDlg.Item> items, string verb, Action go)
    {
        var fused = new List<InventoryDlg.Item>();
        foreach (InventoryDlg.Item item in items) if (Has(item)) fused.Add(item);
        if (fused.Count == 0) { go(); return; }
        var text = new StringBuilder();
        text.Append("这批里有 ").Append(fused.Count).Append(" 件装备融合着外观, ").Append(verb).Append("会一起带走:\n");
        int shown = Math.Min(6, fused.Count);
        for (int i = 0; i < shown; i++)
            text.Append("\n· ").Append(fused[i].name).Append(" —— ").Append(What(fused[i]));
        if (fused.Count > shown) text.Append("\n· …另有 ").Append(fused.Count - shown).Append(" 件");
        text.Append("\n\n(想留下融合的部分: 先用分离卷轴取下来, 神奇货架 5 彩玉)\n\n确定要").Append(verb).Append("吗？");
        SysMsgBox.Show(text.ToString(), go, null);
    }
}
}
