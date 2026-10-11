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
    static void Rift(Client c, string runID, string phase, bool claimable = false, int keys = 0, bool ok = true, string extra = "")
    {
        Apply(c, "{\"kind\":\"rift.state\",\"ok\":" + (ok ? "true" : "false") + ",\"error\":\"requirements\",\"rift\":{\"runId\":\"" + runID + "\",\"revision\":\"4\",\"phase\":\"" + phase + "\",\"tier\":2,\"claimable\":" + (claimable ? "true" : "false") + ",\"keyCount\":" + keys + extra + "}}");
    }
    static void MultiFloorContracts(Client c, EndgamePanel p)
    {
        Set(p, "open", true); Set(p, "tab", 3); Rift(c, "", "closed", keys: 1);
        UnityEngine.GUI.nextText = "151"; int before = c.Sent.Count; Draw(p, "消耗一钥 · 开启洞天");
        Check(c.Sent.Count == before && !ArtworkSkin.enableds["消耗一钥 · 开启洞天"], "out-of-range direct tier input cannot spend a key");
        UnityEngine.GUI.nextText = "150"; Draw(p);
        Check((int)Get(p, "tier") == 150 && UnityEngine.GUI.labels.Exists(t => t.Contains("阶")) && !ArtworkSkin.enableds["+"], "direct difficulty selection reaches150 and retains upper bound");
        Draw(p, "消耗一钥 · 开启洞天"); Check(c.Last.Json.Contains("\"tier\":150") && !c.Last.Json.Contains("floorIndex"), "fresh paid entry chooses difficulty independently of run floor"); Receipt(c);
        string floor = ",\"floorIndex\":3,\"seed\":\"18446744073709551615\",\"generatorVersion\":1,\"mapName\":\"秘林\",\"canAdvance\":true,\"exitX\":700,\"exitY\":850";
        Rift(c, "multifloor", "active", extra: floor); Call(p, "Update");
        Check((string)Get(p, "announcement") == "", "restored floor does not invent an arrival notice");
        Draw(p); Check(UnityEngine.GUI.labels.Exists(t => t.Contains("第 2 阶") && t.Contains("第 3 层")) && ArtworkSkin.enableds["前往下一层 · 跳过余敌"], "UI separates difficulty and floor and allows skipping without full progress");
        Draw(p, "前往下一层 · 跳过余敌"); var pending = c.Last;
        Check(pending.Op == "rift.advance" && pending.Json.Contains("\"floorIndex\":3") && pending.Json.Contains("multifloor") && pending.Json.Contains("\"expectedRevision\":\"" + c.Endgame.Revision + "\"") && !pending.Json.Contains("tier"), "advance captures exact run and floor only");
        before = c.Sent.Count; Draw(p, "前往下一层 · 跳过余敌"); Check(c.Sent.Count == before, "double advance is blocked while pending");
        Draw(p, "×"); Check(!(bool)Get(p, "open") && ReferenceEquals(c.Endgame.PendingRequest, pending), "closing retains uncertain floor transition");
        Set(p, "open", true); Draw(p, "查询原请求"); Check(ReferenceEquals(c.Endgame.PendingRequest, pending), "manual floor retry retains nonce"); Receipt(c);
        Set(p, "confirmLeave", true); Rift(c, "multifloor", "active", extra: floor.Replace("\"floorIndex\":3", "\"floorIndex\":4")); Call(p, "Update");
        Check(((string)Get(p, "announcement")).Contains("第 4 层") && !(bool)Get(p, "confirmLeave") && c.Endgame.Rift.Tier == 2, "arrival announces floor while preserving tier and clearing old leave confirmation");
        float until = (float)Get(p, "announcementUntil"); Call(p, "Update"); Check((float)Get(p, "announcementUntil") == until, "same floor does not replay arrival");
        Rift(c, "multifloor", "active", extra: floor + ",\"transitioning\":true"); Draw(p);
        Check(!ArtworkSkin.enableds["正在前往下一层…"] && !ArtworkSkin.enableds["进入已有洞天"], "native transition blocks repeated advance and reentry");
        Rift(c, "multifloor", "boss", extra: floor); ArtworkSkin.enableds.Clear(); Draw(p);
        Check(!ArtworkSkin.enableds.ContainsKey("前往下一层 · 跳过余敌"), "guardian phase cannot offer skipping");
        string tells = ",\"telegraphs\":[{\"id\":\"circle\",\"entityId\":\"1\",\"ability\":\"烈焰爆裂\",\"shape\":\"circle\",\"x\":500,\"y\":500,\"radius\":105,\"remainingMS\":1500},{\"id\":\"ring\",\"entityId\":\"2\",\"ability\":\"震环冲击\",\"shape\":\"ring\",\"x\":500,\"y\":500,\"radius\":220,\"innerRadius\":85,\"remainingMS\":1800},{\"id\":\"line\",\"entityId\":\"3\",\"ability\":\"裂地斩\",\"shape\":\"line\",\"x\":500,\"y\":500,\"endX\":700,\"endY\":500,\"radius\":38,\"remainingMS\":1400}]";
        UnityEngine.Camera.main = new UnityEngine.Camera(); Rift(c, "multifloor", "active", extra: floor + tells);
        Set(p, "open", false); BitmapFont.labels.Clear(); UnityEngine.GUI.textures.Clear(); Draw(p);
        Check(BitmapFont.labels.Exists(t => t.Contains("烈焰爆裂") && t.Contains("秒") && t.Contains("离开圆圈")) && BitmapFont.labels.Exists(t => t.Contains("震环冲击") && t.Contains("避开圆环")) && BitmapFont.labels.Exists(t => t.Contains("裂地斩") && t.Contains("离开直线")), "all danger shapes show readable ability, countdown and dodge hint");
        Check(UnityEngine.GUI.textures.Count > 100 && BitmapFont.labels.Exists(t => t.Contains("下一层出口")), "native overlay draws real geometry and the optional exit marker");
        Rift(c, "multifloor", "boss", extra: floor); BitmapFont.labels.Clear(); ArtworkSkin.enableds.Clear(); before = c.Sent.Count; Draw(p, "下一层 · 出口 (700, 850)");
        Check(BitmapFont.labels.Exists(t => t == "镇境劫影 · 出口 (700, 850)") && BitmapFont.labels.Exists(t => t == "◆ 镇境劫影 · 本层出口") && BitmapFont.labels.Exists(t => t.Contains("前往本层出口，击败")), "guardian phase retains spawn marker, HUD coordinates and exit objective");
        Check(c.Sent.Count == before && !ArtworkSkin.enableds.ContainsKey("下一层 · 出口 (700, 850)") && !BitmapFont.labels.Exists(t => t.Contains("下一层出口")), "guardian location is informational and cannot advance the floor");
        Rift(c, "multifloor", "active", extra: floor.Replace("\"canAdvance\":true", "\"canAdvance\":false")); BitmapFont.labels.Clear(); ArtworkSkin.enableds.Clear(); Draw(p);
        Check(BitmapFont.labels.Exists(t => t.Contains("下一层出口")) && ArtworkSkin.enableds.ContainsKey("下一层 · 出口 (700, 850)") && !ArtworkSkin.enableds["下一层 · 出口 (700, 850)"], "faraway players can discover exit marker and HUD coordinates without advance authority");
        Rift(c, "multifloor", "active", extra: floor + tells);
        typeof(EndgameClientState).GetField("<RiftReceivedUtc>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(c.Endgame, DateTime.UtcNow.AddSeconds(-4));
        BitmapFont.labels.Clear(); Draw(p); Check(!BitmapFont.labels.Exists(t => t.Contains("烈焰爆裂") || t.Contains("震环冲击") || t.Contains("裂地斩")), "elapsed tells disappear without fabricating impact or damage");
        Rift(c, "multifloor", "active", extra: floor + tells + ",\"transitioning\":true"); BitmapFont.labels.Clear(); Draw(p);
        Check(!BitmapFont.labels.Exists(t => t.Contains("烈焰爆裂") || t.Contains("下一层出口")), "transition suppresses old map danger and exit geometry");
        UnityEngine.Camera.main = null;
    }
    static void Main()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); Draw(p); Check(c.Sent.Count == 0, "unnegotiated UI sends no commands");
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"); Call(p, "Update");
        string items = "{\"uid\":\"9007199254740993\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"飞鹤遗音\",\"uniqueId\":1001,\"uniqueRollBp\":10000,\"socketCount\":2,\"sockets\":[{\"index\":0,\"itemId\":0},{\"index\":1,\"itemId\":0}]}, {\"uid\":\"9007199254740994\",\"itemId\":1900028001,\"location\":\"bag\",\"tab\":1,\"slot\":1,\"name\":\"角木蛟星篆\"}, {\"uid\":\"9007199254740995\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":25,\"name\":\"归元佩\"}";
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"ui\",\"chunks\":1,\"revision\":\"1\",\"items\":[" + items + "]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1001, bagIdx = 0 }; InventoryDlg.rows[1] = new InventoryDlg.Item { id = 1900028001, bagIdx = 1 };
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1002, slot = 25 });
        Draw(p, "演劫 · F8"); Check((bool)Get(p, "open"), "toolbar opens panel"); Check(c.Sent.Exists(r => r.Op == "snapshot") && !c.Sent.Exists(r => r.Op == "catalog"), "equipment opens with real state but no unsolicited catalogs");
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
        Receipt(c);
        Rift(c, "stages", "active"); Call(p, "Update");
        Check((string)Get(p, "announcement") == "", "restored active run establishes silent baseline");
        Rift(c, "stages", "boss"); Call(p, "Update");
        Check(((string)Get(p, "announcement")).Contains("劫影现身"), "authoritative boss transition announces objective");
        float until = (float)Get(p, "announcementUntil"); UnityEngine.Time.unscaledTime += 1; Call(p, "Update");
        Check((float)Get(p, "announcementUntil") == until, "repeated snapshot does not replay phase announcement");
        Rift(c, "stages", "reward_pending"); Call(p, "Update");
        Check(((string)Get(p, "announcement")).Contains("正在保存"), "saving phase never promises claimable rewards");
        Rift(c, "stages", "completed", true); Call(p, "Update"); Set(p, "open", false);
        int mutations = c.Sent.FindAll(r => r.Mutates).Count;
        Draw(p, "查看战果 · 领取");
        Check((bool)Get(p, "open") && (int)Get(p, "tab") == 3 && c.Sent.FindAll(r => r.Mutates).Count == mutations, "HUD opens result review without claiming automatically");
        Check(UnityEngine.GUI.labels.Exists(t => t.Contains("战果已保存")), "completed panel describes claim instead of new paid entry instructions");
        ArtworkSkin.enableds.Clear(); Draw(p);
        Check(!ArtworkSkin.enableds.ContainsKey("查看战果 · 领取"), "open panel suppresses overlapping HUD controls");
        Rift(c, "stages", "closed", true, keys: 1); Call(p, "Update"); Draw(p);
        Check(!ArtworkSkin.enableds["消耗一钥 · 开启洞天"] && ArtworkSkin.enableds["领取我的洞天所得"], "durable closed unclaimed run cannot start another paid run");
        Check(UnityEngine.GUI.labels.Exists(t => t.Contains("仍可领取")), "closed unclaimed result does not claim loot is already in bag");
        Rift(c, "stages", "failed", keys: 1); Draw(p);
        Check(UnityEngine.GUI.labels.Exists(t => t.Contains("选择劫数")), "failed nonclaimable run keeps next-run tier selector");
        Rift(c, "other-run", "boss"); Call(p, "Update");
        Check((string)Get(p, "announcement") == "", "different run does not replay old phase transition");
        Rift(c, "other-run", "completed", true); Call(p, "Update");
        Rift(c, "", "closed"); Call(p, "Update");
        Check((string)Get(p, "announcement") == "", "empty run clears old phase feedback");
        UnityEngine.Event.current.mousePosition = new UnityEngine.Vector2(811, 477); Draw(p);
        typeof(EndgamePanel).GetMethod("ShowPanelTooltip", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, new object[] { "screen hover" });
        Check(UITip.mouse.x == 811 && UITip.mouse.y == 477 && UITip.scale == (float)Get(p, "panelScale"), "panel tooltip forwards original screen pointer and window scale");
        UnityEngine.Event.current.mousePosition = new UnityEngine.Vector2(-9000, -9000);
        MultiFloorContracts(c, p);
        c.Endgame.Reset(); Call(p, "Update"); Check(!(bool)Get(p, "open") && (string)Get(p, "selectedUID") == "", "reset clears selection"); Check((string)Get(p, "announcement") == "", "reset clears phase feedback"); Draw(p, "演劫 · F8"); Check(!(bool)Get(p, "open"), "new negotiation required");
        var requirement = EndgameResponse.Parse("{\"kind\":\"catalog.uniques\",\"ok\":true,\"items\":[{\"itemId\":1001,\"uniqueId\":1001,\"profession\":24,\"uniqueRollBp\":10000}]}").Items[0];
        string catalogTip = EndgamePanel.ItemDescription(requirement, true); Check(catalogTip.Contains("药师/术士") && catalogTip.Contains("图鉴基准"), "profession bitmask and catalog baseline labels");
        Console.WriteLine("PASS " + assertions + " real-panel action contracts with Unity/artwork stubs; no rendering or engine launch.");
    }
}
