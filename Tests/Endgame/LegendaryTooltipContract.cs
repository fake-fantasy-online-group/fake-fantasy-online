using System;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

partial class EndgameUiContract
{
    static void LegendaryTooltipContracts(Client c, EndgamePanel p)
    {
        const string effect1 = "再次施放火球术时额外释放一枚火球。";
        const string effect2 = "额外火球继承本次施法的伤害。";
        const string lore = "旧剑仍记得远行人的归途。";
        const string footer = "（绑定物品不可交易/摆摊/邮寄, 可卖NPC）";
        string[] affixes = { "力量 +12", "体质 +13", "敏捷 +14", "智慧 +15", "精神 +16", "灵巧 +17" };
        foreach (bool nativeStats in new[] { false, true })
        foreach (string shape in new[] { "legacy", "native", "sidecar", "duplicate", "named-native", "named-sidecar", "named-duplicate" })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            string fields = "\"name\":\"双焰\",\"description\":\"" + (shape == "legacy" ? effect1 + "\\n" + effect2 : lore) +
                "\",\"legendary\":{\"powerId\":\"FUTURE-999\",\"name\":\"双焰\",\"description\":\"" + (shape == "named-sidecar" ? "双焰：" : "") + effect1 + "\\n" + effect2 + "\"}";
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"legendary-tooltip\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"tab\":2,\"slot\":25," + fields + "}]}");
            string body = "\u0007(长剑)\n耐久度: 50/50\n\u0004需要等级: 60\n需要职业: 剑客\n\n\u00030攻击 200-300\n重量 5\n\n";
            if (shape == "legacy") body += "\u00034传奇·双焰\n基底：旧剑\n" + effect1 + "\n" + effect2 + "\n";
            foreach (string affix in affixes) body += (shape == "legacy" ? "\u00034" : "\u00035") + affix + "\n";
            body += "\u00032套装: 旧套装 (1/2)\n\u0002[2件] 防御 +9\n\u0005[1件] 命中 +3\n法宝技能: 旧技能\n" + lore + "\n" + footer;
            if (shape == "native" || shape == "duplicate") body += "\n\u0006" + effect1 + "\n\u0006" + effect2;
            if (shape.StartsWith("named-", StringComparison.Ordinal)) body += "\n\u0006双焰：" + effect1 + "\n\u0006" + effect2;
            if (shape == "duplicate" || shape == "named-duplicate") body += "\n" + effect1 + "\n" + effect2;
            var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "双焰+7", desc = body };
            var worn = new EquipDlg.EquippedItem { id = 1002, slot = 25, quality = 4, name = "双焰+7", desc = body };
            InventoryDlg.rows.Clear(); InventoryDlg.rows[0] = bag; EquipDlg.Equipped.Clear(); EquipDlg.Equipped.Add(worn);
            string original = TooltipContent.Fmt(TooltipContent.QualityName(bag.name, bag.quality), body);
            string tip = EndgamePanel.BagTooltip(2, bag, original);
            Check(tip == EndgamePanel.EquippedTooltip(worn, original), "legendary bag and worn tooltip share the existing renderer: " + shape);
            Check(tip.StartsWith("\u0007\u00035双焰+7\n\u0007(长剑)\n耐久度: 50/50"), "legendary keeps one dark-gold centered native title and original equipment structure");
            Check(!tip.Contains("传奇·") && !tip.Contains("基底："), "legendary removes only its generated secondary title/base-material block");
            foreach (string affix in affixes)
                Check(Occurrences(tip, affix) == 1 && tip.Contains("\u00035" + affix), "all six rolled attributes stay once in dark gold: " + affix);
            Check(tip.Contains("\u00035灵巧 +17\n\u0006双焰：" + effect1 + "\n\u0006" + effect2 + "\n\u00032套装:"), "legendary effects keep their existing orange position with the actual power name before the first effect row");
            Check(Occurrences(tip, "双焰：") == 1 && !tip.Contains("威能名称:"), "actual power name prefix appears once for native legacy and sidecar fallback");
            Check(Occurrences(tip, effect1) == 1 && Occurrences(tip, effect2) == 1, "native and sidecar legendary descriptions never duplicate the effect");
            Check(tip.Contains("\u00030攻击 200-300") && tip.Contains("\u0004需要等级: 60") && tip.Contains("\u0002[2件] 防御 +9\n\u0005[1件] 命中 +3"), "legendary preserves white base red requirements and existing inactive/active set colors");
            Check(Occurrences(tip, lore) == 1 && tip.EndsWith(footer), "legendary keeps native lore and binding in their original sections");
            foreach (var item in c.Endgame.Items)
                Check(EndgamePanel.OverlayTooltip(item, tip) == tip, "repeated legendary overlay remains byte-identical: " + shape);
        }

        // Compatibility cleanup is anchored to the exact generated legendary
        // heading. Similar authored lore elsewhere must remain untouched.
        var current = c.Endgame.Items[0];
        string authored = "\u0007双焰\n\u0007(长剑)\n攻击 200-300\n传奇·另一段故事\n基底：故事中的旧剑\n" + footer;
        string preserved = EndgamePanel.OverlayTooltip(current, authored);
        Check(preserved.Contains("传奇·另一段故事\n基底：故事中的旧剑"), "legendary compatibility does not strip unrelated authored title/base-material phrases");
        var bodyName = TooltipItem("\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"双焰\",\"legendary\":{\"powerId\":\"FUTURE-999\",\"name\":\"双焰\",\"description\":\"后续保留双焰：原有说明。\"}");
        string bodyNameTip = EndgamePanel.OverlayTooltip(bodyName, "\u0007双焰\n\u0007(长剑)\n\u00035力量 +12\n" + footer);
        Check(bodyNameTip.Contains("\u0006双焰：后续保留双焰：原有说明。"), "power name normalization never rewrites a name/colon occurring inside the effect body");
        Check(EndgamePanel.OverlayTooltip(bodyName, bodyNameTip) == bodyNameTip, "body-internal power name remains stable on repeated overlay");
    }
}
