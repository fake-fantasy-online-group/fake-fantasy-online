using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

class EndgameUiContract
{
    static int assertions;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); assertions++; }
    sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public List<EndgameRequest> Sent = new List<EndgameRequest>();
        public int Retries, Equips, Unequips;
        public bool SendEndgame(EndgameRequest request) { if (!Endgame.Begin(request)) return false; Sent.Add(request); return true; }
        public bool RetryEndgame() { Retries++; return Endgame.Pending; }
        public bool EquipEndgameItem(EndgameItemView item) { Equips++; return true; }
        public bool UnequipEndgameItem(EndgameItemView item) { Unequips++; return true; }
        public EndgameRequest Last => Sent[Sent.Count - 1];
    }
    static void Set(object panel, string key, object value) => panel.GetType().GetField(key, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(panel, value);
    static object Get(object panel, string key) => panel.GetType().GetField(key, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
    static void Call(object panel, string key) => panel.GetType().GetMethod(key, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, null);
    static void Apply(Client client, string json) => client.Endgame.Apply(EndgameResponse.Parse(json));
    static void Draw(EndgamePanel panel, string click = null) { ArtworkSkin.click = click; UnityEngine.GUI.labels.Clear(); Call(panel, "OnGUI"); }
    static void Receipt(Client c)
    {
        var request = c.Endgame.PendingRequest;
        Apply(c, "{\"kind\":\"" + (request.Op == "cube.execute" ? "cube.result" : request.Op) + "\",\"ok\":true,\"requestId\":\"" + request.RequestID + "\",\"revision\":\"2\"}");
    }
    static void Rift(Client c, string runID, string phase, bool claimable = false, int keys = 0, bool ok = true)
    {
        Apply(c, "{\"kind\":\"rift.state\",\"ok\":" + (ok ? "true" : "false") + ",\"error\":\"requirements\",\"rift\":{\"runId\":\"" + runID + "\",\"revision\":\"4\",\"phase\":\"" + phase + "\",\"tier\":2,\"claimable\":" + (claimable ? "true" : "false") + ",\"keyCount\":" + keys + "}}");
    }
    static void Main()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); Draw(p); Check(c.Sent.Count == 0, "unnegotiated UI sends no commands");
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"); Call(p, "Update");
        string items = "{\"uid\":\"9007199254740993\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"飞鹤遗音\",\"uniqueId\":1001,\"uniqueRollBp\":10000,\"socketCount\":2,\"sockets\":[{\"index\":0,\"itemId\":0},{\"index\":1,\"itemId\":0}]}, {\"uid\":\"9007199254740994\",\"itemId\":1900028001,\"location\":\"bag\",\"tab\":1,\"slot\":1,\"name\":\"角木蛟星篆\"}, {\"uid\":\"9007199254740995\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":25,\"name\":\"归元佩\"}";
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"ui\",\"chunks\":1,\"revision\":\"1\",\"items\":[" + items + "]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1001, bagIdx = 0 }; InventoryDlg.rows[1] = new InventoryDlg.Item { id = 1900028001, bagIdx = 1 };
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1002, slot = 25 });
        Draw(p, "演劫 · F8"); Check((bool)Get(p, "open"), "toolbar opens panel"); Check(c.Sent.Exists(r => r.Op == "snapshot") && c.Sent.Exists(r => r.Catalog == "recipes"), "real state and catalogs requested");
        Set(p, "selectedUID", "9007199254740993"); Draw(p, "穿戴器物"); Check(c.Equips == 1, "native equip port");
        Set(p, "selectedUID", "9007199254740995"); Draw(p, "卸下器物"); Check(c.Unequips == 1, "slot25 native unequip");
        string tooltip = EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], "legacy details"); Check(tooltip.Contains("gold:飞鹤遗音") && tooltip.Contains("legacy details") && tooltip.Contains("宿序"), "gold tooltip retains native description and socket order");
        Check(EndgamePanel.BagTooltip(0, InventoryDlg.rows[0], "legacy") == "legacy", "wrong tab cannot overlay");
        Set(p, "selectedUID", "9007199254740993"); Set(p, "tab", 1); Set(p, "selectedRuneUID", "9007199254740994");
        Draw(p, "嵌入所选星篆"); Check(c.Last.Op == "socket.insert" && c.Last.Json.Contains("9007199254740994"), "insert submits exact rune UID");
        int count = c.Sent.Count; Draw(p, "嵌入所选星篆"); Check(c.Sent.Count == count, "double mutation blocked"); Draw(p, "查询原请求"); Check(c.Retries == 1, "retry uses pending request");
        var pending = c.Endgame.PendingRequest; Draw(p, "×"); Check(!(bool)Get(p, "open") && ReferenceEquals(c.Endgame.PendingRequest, pending) && WinZ.forgotten > 0, "close retains nonce and removes hit region"); Receipt(c);
        Set(p, "open", true); Set(p, "tab", 2); Set(p, "recipeID", 11001);
        c.Endgame.Begin(new EndgameRequest("catalog", catalog: "recipes"));
        Apply(c, "{\"kind\":\"catalog.recipes\",\"ok\":true,\"recipes\":[{\"id\":11001,\"name\":\"凝髓成篆\",\"description\":\"确定星篆\",\"operation\":\"rune.inscribe\",\"money\":\"30000\",\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":30}]}]}");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"wallet\",\"chunks\":1,\"revision\":\"2\",\"items\":[" + items + "],\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":12}]}");
        Draw(p, "问鼎 · 查看完整耗费"); var quoteRequest = c.Last;
        Check(quoteRequest.Op == "cube.quote" && !quoteRequest.Json.Contains("targetUid"), "inscribe quote needs no target"); Check(UnityEngine.GUI.labels.Exists(t => t.Contains("持有 12 / 需要 30")), "material shortfall visible before quote");
        Apply(c, "{\"kind\":\"cube.quote\",\"ok\":true,\"requestId\":\"" + quoteRequest.RequestID + "\",\"quote\":{\"quoteId\":\"opaque-quote\",\"recipeId\":11001,\"revision\":\"2\",\"expiresAt\":\"2099-01-01T00:00:00Z\",\"description\":\"消耗材料炼制\",\"money\":\"30000\"}}");
        Draw(p, "照此投料 · 炼化"); Check(c.Last.Op == "cube.execute" && c.Last.Json.Contains("opaque-quote") && !c.Last.Json.Contains("itemUids") && !c.Last.Json.Contains("recipeId"), "execute contains only frozen quote identity"); Receipt(c);
        Set(p, "tab", 3); Rift(c, "invited-run", "reserved"); Call(p, "Refresh"); Check(c.Last.Json.Contains("invited-run"), "refresh preserves invitation run");
        UnityEngine.Time.unscaledTime = 10; Call(p, "Update"); Check(c.Last.Json.Contains("invited-run"), "poll preserves invitation run");
        Rift(c, "invited-run", "reward_pending", keys: 1); Draw(p); Check(!ArtworkSkin.enableds["领取我的洞天所得"] && !ArtworkSkin.enableds["消耗一钥 · 开启洞天"], "reward saving disables claim and start");
        Rift(c, "old-paid", "interrupted", true, ok: false); Check(c.Endgame.Rift.RunID == "old-paid", "rejected join surfaces prior run");
        Draw(p, "领取补偿符钥"); Check(c.Last.Op == "rift.claim" && c.Last.Json.Contains("old-paid"), "compensation claims exact run"); Receipt(c);
        Rift(c, "completed-run", "completed", true); Draw(p, "进入已有洞天"); Check(c.Last.Op == "rift.start" && c.Last.Json.Contains("completed-run"), "live completed run reentry"); Receipt(c);
        int beforeLeave = c.Sent.Count; Draw(p, "离开洞天"); Check(c.Sent.Count == beforeLeave, "leave requires confirmation"); Draw(p, "确认离境 · 无自动返钥"); Check(c.Last.Op == "rift.leave", "confirmed leave reaches server"); Receipt(c);
        Rift(c, "completed-run", "closed", keys: 1); Draw(p, "消耗一钥 · 开启洞天"); Check(c.Last.Op == "rift.start" && !c.Last.Json.Contains("runId"), "closed run permits new paid run");
        c.Endgame.Reset(); Call(p, "Update"); Check(!(bool)Get(p, "open") && (string)Get(p, "selectedUID") == "", "reset clears selection"); Draw(p, "演劫 · F8"); Check(!(bool)Get(p, "open"), "new negotiation required");
        var requirement = EndgameResponse.Parse("{\"kind\":\"catalog.uniques\",\"ok\":true,\"items\":[{\"itemId\":1001,\"uniqueId\":1001,\"profession\":24,\"uniqueRollBp\":10000}]}").Items[0];
        string catalogTip = EndgamePanel.ItemDescription(requirement, true); Check(catalogTip.Contains("药师/术士") && catalogTip.Contains("图鉴基准"), "profession bitmask and catalog baseline labels");
        Console.WriteLine("PASS " + assertions + " real-panel action contracts with Unity/artwork stubs; no rendering or engine launch.");
    }
}
