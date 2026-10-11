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
        Check(gear.Contains("原装备完整要求") && gear.Contains("需要职业: 剑客") && gear.Contains("孔数 1/2") && gear.Contains("星篆 1900028001"), "native requirements and socket count retained with constituent rune");
        Check(!gear.Contains("生效") && !gear.Contains("已成形") && !gear.Contains("宿序"), "bag formation has no activation or duplicate socket commentary");
        Check(!gear.Contains("多一空窍") && !gear.Contains("巧匠或左慈"), "equipment tooltip excludes crafting instructions and explanatory clutter");
        Check(client.Sent.Exists(r => r.Catalog == "runes") && client.Sent.Exists(r => r.Catalog == "formations"), "formed item requests authoritative names stats and descriptions");
        Apply(client, "{\"kind\":\"catalog.runes\",\"ok\":true,\"runes\":[{\"itemId\":1900028001,\"name\":\"角木蛟\",\"quadrant\":\"青龙\",\"mansion\":\"角\",\"minLevel\":12,\"description\":\"实际星篆传说\",\"stats\":[{\"attr\":1,\"value\":3}]}]}");
        string rune = EndgamePanel.BagTooltip(1, InventoryDlg.rows[1], "\u0007角木蛟\n需要等级: 12\n重量 1\n力量 +7\n原星篆说明");
        Check(rune.Contains("原星篆说明") && rune.Contains("实际星篆传说") && rune.Contains("力量 +7") && !rune.Contains("力量 +3") && rune.Contains("青龙"), "owned rune tooltip reuses authoritative lore and stats");
        Check(rune.IndexOf("需要等级: 12") == rune.LastIndexOf("需要等级: 12") && rune.IndexOf("力量 +7") == rune.LastIndexOf("力量 +7") && !rune.Contains("选择器物与空窍"), "owned rune preserves instance values and one native requirement without navigation clutter");
        string oldRune = EndgamePanel.BagTooltip(1, InventoryDlg.rows[1], "\u0007角木蛟\n旧服描述");
        Check(oldRune.Contains("需要等级: 12") && oldRune.Contains("力量 +3") && oldRune.Contains("实际星篆传说"), "older native rune tooltip fills absent requirement and stats from catalog");
        client.Endgame.Begin(new EndgameRequest("catalog", catalog: "runes", cursor: "1"));
        Apply(client, "{\"kind\":\"catalog.runes\",\"ok\":true,\"cursor\":\"1\",\"runes\":[{\"itemId\":1900028002,\"name\":\"亢金龙\",\"quadrant\":\"青龙\",\"description\":\"亢\"}]}");
        string legacyRune = EndgamePanel.RuneDetails(1900028002, false);
        Check(legacyRune.Contains("青龙") && legacyRune.Contains("亢") && !legacyRune.Contains(" · 宿"), "legacy server mansion in Description does not create empty mansion label");
        Check(EndgamePanel.BagTooltip(0, InventoryDlg.rows[0], "旧说明") == "旧说明", "stale tab is never decorated");
        var material = new InventoryDlg.Item { id = 1900028201, bagIdx = 7 };
        Check(EndgamePanel.BagTooltip(1, material, "星髓").Contains("周天鼎"), "stackable material without UID still has crafting context");
        var key = new InventoryDlg.Item { id = 1900028101, bagIdx = 8 };
        Check(EndgamePanel.BagTooltip(1, key, "符钥").Contains("妖魔结界守卫"), "key has in-world trial entry");
        Call(panel, "EnsureStyles"); Set(panel, "selectedUID", "1"); Set(panel, "selectedRuneUID", "2");
        UnityEngine.GUI.labels.Clear(); Call(panel, "DrawRunes");
        Check(!ArtworkSkin.enableds["嵌入所选星篆"] && ArtworkSkin.enableds["取出此孔星篆"], "occupied socket offers extraction instead of doomed insert");
        Check(client.Sent.FindAll(r => r.Catalog == "formations").Count == 1, "normal rune view does not duplicate pending formed-item catalog request");
        ArtworkSkin.click = "宿阵图鉴"; Call(panel, "DrawRunes");
        Check(client.Sent.Exists(r => r.Catalog == "formations"), "explicit catalog button reuses already requested formations");
        Apply(client, "{\"kind\":\"catalog.formations\",\"ok\":true,\"formations\":[{\"id\":1,\"name\":\"宿阵甲\",\"channel\":\"战阵\",\"minLevel\":20,\"professions\":0,\"slots\":[4,13],\"runes\":[1900028001,1900028001]}]}");
        string formation = (string)Call(panel, "FormationDescription", client.Endgame.Formations[0]);
        Check(formation.Contains("不限职业") && formation.Contains("单手武器/双手武器"), "formation catalog explains profession mask zero and real slot restrictions");
        Check(formation.Contains("背包有") && formation.Contains("背包缺"), "repeated recipe rune consumes each owned instance only once");
        Console.WriteLine("PASS " + assertions + " contextual loot contracts; no Unity engine launch.");
    }
}
