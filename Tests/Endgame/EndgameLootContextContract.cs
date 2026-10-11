// Real tooltip and rune-panel code; deterministic UI substitutes, no Unity Player.
using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

internal static class EndgameLootContextContract
{
    private static int assertions;
    private sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld => true;
        public readonly List<EndgameRequest> Sent = new List<EndgameRequest>();
        public bool SendEndgame(EndgameRequest request) { if (!Endgame.Begin(request)) return false; Sent.Add(request); return true; }
        public bool RetryEndgame() => false;
        public bool EquipEndgameItem(EndgameItemView item) => true;
        public bool UnequipEndgameItem(EndgameItemView item) => true;
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); assertions++; }
    private static void Set(EndgamePanel panel, string key, object value) => typeof(EndgamePanel).GetField(key, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(panel, value);
    private static object Call(EndgamePanel panel, string key, params object[] args) => typeof(EndgamePanel).GetMethod(key, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(panel, args);
    private static void Apply(Client client, string json) => client.Endgame.Apply(EndgameResponse.Parse(json));
    private static void Main()
    {
        var client = new Client(); var panel = new EndgamePanel(); panel.Bind(client);
        client.Endgame.Begin(new EndgameRequest("hello")); Apply(client, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Apply(client, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"loot\",\"chunks\":1,\"revision\":\"1\",\"items\":[{\"uid\":\"1\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"仙遗\",\"uniqueId\":1,\"socketCount\":2,\"formationId\":1,\"sockets\":[{\"index\":0,\"itemId\":1900028001}]},{\"uid\":\"2\",\"itemId\":1900028001,\"location\":\"bag\",\"tab\":1,\"slot\":1,\"name\":\"角木蛟\"}]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1001, bagIdx = 0 };
        InventoryDlg.rows[1] = new InventoryDlg.Item { id = 1900028001, bagIdx = 1 };
        string native = "\u0007原名\n\u0007(武器)\n耐久度: 50/50\n需要等级: 20\n需要职业: 剑客\n攻击 20~30\n孔数 1/2\n原装备完整要求";
        string gear = EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], native);
        Check(gear.Contains("原装备完整要求") && gear.Contains("需要职业: 剑客") && gear.Contains("孔数 1/2") && gear.Contains("\u0007(武器)\n\u00035？\n"), "native requirements and socket count retained while unresolved rune metadata uses a compact placeholder");
        Check(!gear.Contains("生效") && !gear.Contains("已成形") && !gear.Contains("宿序"), "bag formation has no activation or duplicate socket commentary");
        Check(!gear.Contains("多一空窍") && !gear.Contains("巧匠或左慈"), "equipment tooltip excludes crafting instructions and explanatory clutter");
        var chooserItem = InventoryDlg.rows[0]; chooserItem.name = "原名"; chooserItem.desc = "原装备完整要求"; InventoryDlg.rows[0] = chooserItem;
        string pickerTooltip = (string)typeof(EndgamePanel).GetMethod("OwnedItemTooltip", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { client.Endgame.Items[0] });
        Check(pickerTooltip == EndgamePanel.BagTooltip(2, chooserItem, TooltipContent.Fmt(TooltipContent.QualityName(chooserItem.name, chooserItem.quality), chooserItem.desc)), "workbench input chooser reuses the native tooltip byte-for-byte");
        Check(client.Sent.Exists(r => r.Catalog == "runes") && client.Sent.Exists(r => r.Catalog == "formations"), "formed item requests authoritative names stats and descriptions");
        Apply(client, "{\"kind\":\"catalog.runes\",\"ok\":true,\"runes\":[{\"itemId\":1900028001,\"name\":\"角木蛟\",\"quadrant\":\"青龙\",\"mansion\":\"角\",\"minLevel\":12,\"description\":\"实际星篆传说\",\"stats\":[{\"attr\":1,\"value\":3}]}]}");
        string rune = EndgamePanel.BagTooltip(1, InventoryDlg.rows[1], "\u0007角木蛟\n需要等级: 12\n重量 1\n力量 +7\n原星篆说明");
        Check(EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], native).Contains("\u0007(武器)\n\u00035角\n"), "catalog fills composition initial when older server omits socket name");
        Check(rune.StartsWith("\u0007角木蛟\n"), "standalone rune retains its full item name");
        Check(rune.Contains("原星篆说明") && rune.Contains("实际星篆传说") && rune.Contains("力量 +7") && !rune.Contains("力量 +3") && rune.Contains("青龙"), "owned rune tooltip reuses authoritative lore and stats");
        Check(rune.IndexOf("需要等级: 12") == rune.LastIndexOf("需要等级: 12") && rune.IndexOf("力量 +7") == rune.LastIndexOf("力量 +7") && !rune.Contains("选择器物与空窍"), "owned rune preserves instance values and one native requirement without navigation clutter");
        var nativeRune = InventoryDlg.rows[1]; nativeRune.name = "角木蛟"; nativeRune.desc = "需要等级: 12\n重量 1\n力量 +7\n原星篆说明"; InventoryDlg.rows[1] = nativeRune;
        string expectedRuneHover = EndgamePanel.BagTooltip(1, nativeRune, TooltipContent.Fmt(TooltipContent.QualityName(nativeRune.name, nativeRune.quality), nativeRune.desc));
        Call(panel, "EnsureStyles"); UnityEngine.Event.current.mousePosition = new UnityEngine.Vector2(1, 1);
        foreach (bool multiple in new[] { false, true })
        {
            UITip.last = ""; Call(panel, "DrawRuneBank", new UnityEngine.Rect(0, 0, 594, 112), multiple);
            Check(UITip.last == expectedRuneHover, "owned rune hover exactly matches native bag tooltip for " + (multiple ? "ordered cube inputs" : "socket and salvage inputs"));
            Check(UITip.last.Contains("力量 +7") && !UITip.last.Contains("力量 +3") && UITip.last.Contains("原星篆说明") && UITip.last.Contains("实际星篆传说"), "owned rune hover preserves instance values and native lore over reference catalog values");
            Check(UITip.last.IndexOf("需要等级: 12") == UITip.last.LastIndexOf("需要等级: 12") && UITip.last.IndexOf("力量 +7") == UITip.last.LastIndexOf("力量 +7"), "owned rune hover preserves overlay requirement and stat deduplication");
        }
        UnityEngine.Event.current.mousePosition = new UnityEngine.Vector2(-9000, -9000);
        string oldRune = EndgamePanel.BagTooltip(1, InventoryDlg.rows[1], "\u0007角木蛟\n旧服描述");
        Check(oldRune.Contains("需要等级: 12") && oldRune.Contains("力量 +3") && oldRune.Contains("实际星篆传说"), "older native rune tooltip fills absent requirement and stats from catalog");
        client.Endgame.Begin(new EndgameRequest("catalog", catalog: "runes", cursor: "1"));
        Apply(client, "{\"kind\":\"catalog.runes\",\"ok\":true,\"cursor\":\"1\",\"runes\":[{\"itemId\":1900028002,\"name\":\"亢金龙\",\"quadrant\":\"青龙\",\"description\":\"亢\"}]}");
        string legacyRune = EndgamePanel.RuneDetails(1900028002, false);
        Check(legacyRune.Contains("青龙") && legacyRune.Contains("亢") && !legacyRune.Contains(" · 宿"), "legacy server mansion in Description does not create empty mansion label");
        Check(EndgamePanel.BagTooltip(0, InventoryDlg.rows[0], "旧说明") == "旧说明", "stale tab is never decorated");
        var material = new InventoryDlg.Item { id = 1900028201, bagIdx = 7 };
        Check(EndgamePanel.BagTooltip(1, material, "星髓").Contains("随机开窍"), "stackable material without UID identifies random cube opening");
        string detail = EndgamePanel.ItemDescription(client.Endgame.Items[0], false);
        Check(detail.Contains("打孔炉逐孔开启") && detail.Contains("无孔器物随机开窍"), "existing item detail distinguishes native incremental opening from zero-socket cube randomness");
        var key = new InventoryDlg.Item { id = 1900028101, bagIdx = 8 };
        Check(EndgamePanel.BagTooltip(1, key, "符钥").Contains("妖魔结界守卫"), "key has in-world trial entry");
        Call(panel, "EnsureStyles"); Set(panel, "selectedUID", "1"); Set(panel, "selectedRuneUID", "2");
        UnityEngine.GUI.labels.Clear(); Call(panel, "DrawRunes");
        Check(!ArtworkSkin.enableds["嵌入所选星篆"] && ArtworkSkin.enableds["取出此孔星篆"], "occupied socket offers extraction instead of doomed insert");
        Check(!ArtworkSkin.enableds.ContainsKey("周天鼎 · 随机开窍 / 调序"), "socket workbench has no cross-module cube shortcut");
        Check(UnityEngine.GUI.labels.Exists(t => t.Contains("无原生额外词条") && t.Contains("不看颜色") && t.Contains("基础攻防与后加星篆、精炼不计")), "existing formation detail explains base eligibility without inferring it from color or later upgrades");
        Check(client.Sent.FindAll(r => r.Catalog == "formations").Count == 1, "normal rune view does not duplicate pending formed-item catalog request");
        Check(!ArtworkSkin.enableds.ContainsKey("宿阵图鉴") && !ArtworkSkin.enableds.ContainsKey("二十八宿图鉴"), "player socket view exposes no encyclopedia navigation");
        Apply(client, "{\"kind\":\"catalog.formations\",\"ok\":true,\"formations\":[{\"id\":1,\"name\":\"宿阵甲\",\"channel\":\"战阵\",\"minLevel\":20,\"professions\":0,\"slots\":[4,13],\"runes\":[1900028001,1900028001]}]}");
        Check(EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], native).Contains("宿阵甲"), "owned formation tooltip resolves metadata without an encyclopedia");
        Console.WriteLine("PASS " + assertions + " contextual loot contracts; no Unity engine launch.");
    }
}
