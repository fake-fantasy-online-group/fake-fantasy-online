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
        Set(p, "open", true); Set(p, "module", EndgameModule.Rift); Rift(c, "", "closed", keys: 1);
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
    static EndgameItemView TooltipItem(string fields) => EndgameResponse.Parse("{\"kind\":\"snapshot\",\"ok\":true,\"items\":[{" + fields + "}]}").Items[0];
    static int Occurrences(string text, string value) => (text.Length - text.Replace(value, "").Length) / value.Length;
    static void TooltipContracts(Client c, EndgamePanel p)
    {
        var unique = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"飞鹤遗音\",\"description\":\"仙人遗音。\",\"socketCount\":2,\"level\":42");
        string native = "\u0007\u00034旧名字+7\n\u0007(武器)\n\n\u00034仙遗·飞鹤遗音\n基底：旧名字\n\u00034力量 +12\n\u00034暴击时，伤害提高8%\n同名仙遗特性取最高值；不同特性按通道上限叠加。\n仙人遗音。\n\n耐久度: 50/50\n需要等级: 20\n需要职业: 剑客\n\n攻击 20-30\n重量 5\n\n\u00034命中 +5\n\u00034孔数 0/2\n原有描述。";
        string tip = EndgamePanel.OverlayTooltip(unique, native);
        Check(tip.StartsWith("\u0007\u00035飞鹤遗音+7\n\u0007(武器)"), "unique replaces centered title without prepended block and retains refinement");
        Check(!tip.Contains("仙遗·") && !tip.Contains("基底") && !tip.Contains("同名仙遗") && !tip.Contains("宿序") && !tip.Contains("职业要求"), "unique removes redundant header and explanatory overlay");
        Check(Occurrences(tip, "力量 +12") == 1 && Occurrences(tip, "仙人遗音。") == 1 && Occurrences(tip, "孔数") == 1 && Occurrences(tip, "需要职业") == 1, "unique native attributes lore sockets and requirements appear once");
        Check(tip.IndexOf("攻击 20-30") < tip.IndexOf("力量 +12") && tip.IndexOf("力量 +12") < tip.IndexOf("孔数") && tip.IndexOf("孔数") < tip.IndexOf("仙人遗音。"), "unique uses native intrinsic attribute socket description order");
        Check(tip.Contains("\u00035力量 +12") && tip.Contains("\u00035暴击时") && tip.Contains("\u00035命中 +5") && tip.Contains("需要等级: 42"), "unique attributes and traits use dark gold and existing level row is updated");
        string noSockets = native.Replace("\u00034孔数 0/2\n", "").Replace("原有描述。", "\u00032套装: 原生套装 (1/2)\n\u0002[2件] 力量 +4\n法宝技能: 原生技能\n原有描述。\n（绑定物品不可交易/摆摊/邮寄, 可卖NPC）");
        string noSocketTip = EndgamePanel.OverlayTooltip(unique, noSockets);
        Check(noSocketTip.IndexOf("力量 +12") < noSocketTip.IndexOf("套装:") && noSocketTip.IndexOf("力量 +12") < noSocketTip.IndexOf("原有描述。"), "socketless unique attributes remain before native suit skill and description sections");
        Check(noSocketTip.Contains("\u00032套装:") && noSocketTip.Contains("\u0002[2件] 力量 +4") && noSocketTip.IndexOf("仙人遗音。") < noSocketTip.IndexOf("（绑定物品不可交易"), "native section colors activation markers and final binding footer are preserved");
        c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"tooltip\",\"chunks\":1,\"items\":[],\"formations\":[{\"id\":1,\"name\":\"青龙归元\",\"description\":\"星宿归元。\",\"active\":true,\"channel\":\"battle\",\"stats\":[{\"attr\":1,\"value\":15}]}]}");
        var formation = TooltipItem("\"itemId\":1001,\"formationId\":1,\"location\":\"bag\",\"slot\":0,\"name\":\"普通剑\",\"socketCount\":2,\"sockets\":[{\"index\":1,\"itemId\":1900028002,\"name\":\"乙篆\"},{\"index\":0,\"itemId\":1900028001,\"name\":\"甲篆\"}]");
        tip = EndgamePanel.OverlayTooltip(formation, "\u0007普通剑+12\n\u0007(武器)\n耐久度: 50/50\n需要等级: 20\n攻击 20-30\n孔数 2/2");
        Check(tip.StartsWith("\u0007\u00035青龙归元+12\n\u0007(武器)") && Occurrences(tip, "青龙归元") == 1, "formation uses native title once");
        Check(tip.Contains("\u00035力量 +15") && tip.IndexOf("力量 +15") < tip.IndexOf("孔数 2/2") && tip.IndexOf("孔数 2/2") < tip.IndexOf("甲篆 · 乙篆") && tip.EndsWith("星宿归元。"), "formation stats socket constituents and lore use native order");
        string equalStats = EndgamePanel.OverlayTooltip(formation, "\u0007普通剑\n\u0007(武器)\n耐久度: 50/50\n力量 +15\n孔数 2/2");
        Check(Occurrences(equalStats, "力量 +15") == 2, "equal native and formation values remain independent contributions");
        var combined = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"formationId\":1,\"location\":\"bag\",\"slot\":0,\"name\":\"组合器\",\"description\":\"组合传说\"");
        string combinedTip = EndgamePanel.OverlayTooltip(combined, "\u0007组合器\n\u0007(武器)\n\n\u00034仙遗·组合器\n基底：普通剑\n\u00034力量 +15\n组合传说\n\n耐久度: 50/50\n力量 +15\n孔数 2/2");
        Check(Occurrences(combinedTip, "力量 +15") == 3, "equal native unique and formation bonuses each retain their real contribution");
        Check(!tip.Contains("生效") && !tip.Contains("未成形") && Occurrences(tip, "孔数") == 1, "bag formation has no activation commentary or duplicated sockets");
        var inactive = TooltipItem("\"itemId\":1002,\"formationId\":1,\"location\":\"equipped\",\"slot\":25,\"inactiveReason\":\"职业、等级、耐久或同类宿阵优先级未满足\"");
        Check(EndgamePanel.FormationStatus(inactive) == "", "generic server inactivity does not prove duplicate suppression");
        var suppressed = TooltipItem("\"itemId\":1002,\"formationId\":1,\"location\":\"equipped\",\"slot\":25,\"inactiveReason\":\"同类宿阵优先级未满足\"");
        Check(EndgamePanel.FormationStatus(suppressed) == "不生效", "explicit authoritative suppression is concise");
        var laterRecipe = TooltipItem("\"itemId\":1001,\"formationId\":40,\"location\":\"bag\",\"slot\":0,\"name\":\"普通剑\"");
        EndgamePanel.OverlayTooltip(laterRecipe, "\u0007普通剑+2");
        Check(c.Endgame.CatalogRequested("formations"), "missing owned formation requests first catalog page");
        Apply(c, "{\"kind\":\"catalog.formations\",\"ok\":true,\"nextCursor\":\"32\",\"formations\":[{\"id\":2,\"name\":\"前页宿阵\"}]}");
        EndgamePanel.OverlayTooltip(laterRecipe, "\u0007普通剑+2");
        Check(c.Last.Catalog == "formations" && c.Last.Cursor == "32", "unresolved owned formation automatically continues exact server cursor");
        int beforeCatalog = c.Sent.Count;
        EndgamePanel.OverlayTooltip(laterRecipe, "\u0007普通剑+2");
        Check(c.Sent.Count == beforeCatalog, "pending continuation cannot duplicate catalog request");
        Apply(c, "{\"kind\":\"catalog.formations\",\"ok\":true,\"cursor\":\"32\",\"formations\":[{\"id\":40,\"name\":\"末页宿阵\",\"description\":\"末页传说\",\"stats\":[{\"attr\":1,\"value\":9}]}]}");
        string lateTip = EndgamePanel.OverlayTooltip(laterRecipe, "\u0007普通剑+2");
        Check(lateTip.StartsWith("\u0007\u00035末页宿阵+2") && lateTip.Contains("力量 +9") && lateTip.Contains("末页传说"), "last-page owned formation resolves name stats lore and keeps refinement");
        var ordinary = TooltipItem("\"itemId\":1001,\"location\":\"bag\",\"slot\":0");
        Check(EndgamePanel.OverlayTooltip(ordinary, native) == native, "ordinary item tooltip stays byte-for-byte unchanged");
    }
    static void AuthoredDescriptionContracts(Client c, EndgamePanel p)
    {
        c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        var item = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"location\":\"bag\",\"slot\":0,\"name\":\"炉火旧剑\",\"description\":\"旧炉之火。\\n主人已去。\",\"trait\":\"普通攻击伤害提高10%\"");
        string native = "\u0007旧剑\n\u0007(武器)\n\n仙遗·炉火旧剑\n基底：旧剑\n力量 +12\n普通攻击伤害提高10%\n旧炉之火。\n主人已去。\n\n耐久度: 50/50\n攻击 20-30\n孔数 0/2\n（绑定物品不可交易）";
        string tip = EndgamePanel.OverlayTooltip(item, native);
        Check(Occurrences(tip, "旧炉之火。") == 1 && Occurrences(tip, "主人已去。") == 1 && !tip.Contains("\u00035旧炉之火。"), "multiline authored lore remains once in description rather than attributes");
        Check(Occurrences(tip, "普通攻击伤害提高10%") == 1 && tip.IndexOf("普通攻击伤害提高10%") < tip.IndexOf("孔数") && tip.IndexOf("孔数") < tip.IndexOf("旧炉之火。"), "native unique effect is not repeated by existing owned Trait field");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"effects\",\"chunks\":1,\"items\":[],\"formations\":[{\"id\":1,\"name\":\"枪火宿阵\",\"description\":\"军中旧旗。\",\"stats\":[{\"attr\":1,\"value\":15}]}]}");
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1001, slot = 0 });
        foreach (string state in new[] { "\"location\":\"bag\"", "\"location\":\"equipped\",\"active\":true", "\"location\":\"equipped\",\"inactiveReason\":\"同类宿阵优先级未满足\"", "\"location\":\"equipped\",\"inactiveReason\":\"职业、等级或耐久不符合要求\"" })
        {
            item = TooltipItem("\"itemId\":1001,\"formationId\":1,\"slot\":0,\"name\":\"旧枪\",\"trait\":\"连环枪法伤害提高12%\"," + state);
            tip = EndgamePanel.OverlayTooltip(item, "\u0007旧枪\n需要等级: 50\n攻击 20-30\n孔数 2/2");
            Check(Occurrences(tip, "连环枪法伤害提高12%") == 1 && tip.IndexOf("连环枪法伤害提高12%") < tip.IndexOf("孔数") && Occurrences(tip, "军中旧旗。") == 1, "ordinary formation potential effect remains once in native section for " + state);
            Check(tip.Contains("不生效") == state.Contains("同类宿阵优先级"), "only authoritative duplicate suppression claims the inactive formation status");
        }
        EquipDlg.Equipped.RemoveAll(row => row.id == 1001 && row.slot == 0);
        var old = EndgameResponse.Parse("{\"kind\":\"snapshot\",\"ok\":true,\"items\":[{\"itemId\":1001,\"formationId\":1}]}").Items[0];
        Check(old.Trait == "", "old payload without optional owned Trait stays accepted");
        c.Endgame.Begin(new EndgameRequest("catalog", catalog: "runes"));
        Apply(c, "{\"kind\":\"catalog.runes\",\"ok\":true,\"runes\":[{\"itemId\":1900028028,\"name\":\"星篆·轸水蚓\",\"mansion\":\"轸\",\"quadrant\":\"朱雀\",\"minLevel\":60,\"stats\":[{\"attr\":38,\"value\":1,\"mode\":0}]}]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1900028028, bagIdx = 0 };
        var rune = TooltipItem("\"itemId\":1900028028,\"location\":\"bag\",\"slot\":0,\"name\":\"星篆·轸水蚓\"");
        tip = EndgamePanel.OverlayTooltip(rune, "\u0007星篆·轸水蚓\n有效穿戴时每2秒恢复7点生命");
        Check(Occurrences(tip, "每2秒恢复7点生命") == 1 && !tip.Contains("恢复1点生命") && !tip.Contains("生命恢复 +1"), "rune28 actual stored recovery amount suppresses catalog fallback");
        tip = EndgamePanel.OverlayTooltip(rune, "\u0007星篆·轸水蚓");
        Check(Occurrences(tip, "每2秒恢复1点生命") == 1, "rune catalog fallback remains available without native properties");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1001, bagIdx = 0 };
    }
    static void SocketRecipeContracts()
    {
        // The native client has no capacity/probability model: each target-specific quote is server text.
        string[] chances = { "100%", "50%", "1/3", "25%", "20%" };
        for (int capacity = 1; capacity <= 5; capacity++)
        {
            var c = new Client(); var p = new EndgamePanel(); p.Bind(c);
            c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
            string gear = "{\"uid\":\"9007199254740901\",\"itemId\":2001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"无孔装备\",\"socketCount\":0}";
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"socket-base\",\"chunks\":1,\"revision\":\"1\",\"items\":[" + gear + "],\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":8}]}");
            InventoryDlg.rows[0] = new InventoryDlg.Item { id = 2001, bagIdx = 0 };
            Set(p, "open", true); Set(p, "module", EndgameModule.Cube); Set(p, "recipeID", 1006); Set(p, "selectedUID", "9007199254740901");
            c.Endgame.Begin(new EndgameRequest("catalog", catalog: "recipes"));
            string description = "仅限无孔装备，随机开出1至真实容量的孔数，各孔数等概率；已有孔不可重开。";
            Apply(c, "{\"kind\":\"catalog.recipes\",\"ok\":true,\"recipes\":[{\"id\":1006,\"name\":\"开窍引星\",\"description\":\"" + description + "\",\"operation\":\"socket.open\",\"targetRequired\":true,\"money\":\"5000\",\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":4}]}]}");
            Draw(p);
            Check(UnityEngine.GUI.labels.Contains(description) && UnityEngine.GUI.labels.Exists(t => t.Contains("铜币：5000") && t.Contains("持有 8 / 需要 4")) && !UnityEngine.GUI.labels.Exists(t => t.Contains("净煞尘")), "server random socket recipe and marrow-only cost displayed for capacity " + capacity);
            Draw(p, "问鼎 · 查看完整耗费"); var request = c.Last;
            Check(request.Op == "cube.quote" && request.Json.Contains("\"recipeId\":1006") && request.Json.Contains("9007199254740901"), "random opening quotes exact target for capacity " + capacity);
            int sent = c.Sent.Count; Draw(p, "问鼎 · 查看完整耗费");
            Check(c.Sent.Count == sent && !ArtworkSkin.enableds["照此投料 · 炼化"], "pending quote cannot repeat or execute for capacity " + capacity);
            string preview = "随机开出1至" + capacity + "孔，每种孔数概率" + chances[capacity - 1] + "；已有孔不可重开。";
            Apply(c, "{\"kind\":\"cube.quote\",\"ok\":true,\"requestId\":\"" + request.RequestID + "\",\"quote\":{\"quoteId\":\"socket-quote\",\"recipeId\":1006,\"revision\":\"1\",\"expiresAt\":\"2099-01-01T00:00:00Z\",\"description\":\"" + preview + "\",\"money\":\"5000\",\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":4,\"owned\":8}]}}");
            Draw(p);
            Check(UnityEngine.GUI.labels.Exists(t => t.StartsWith(preview) && t.Contains("铜币：5000") && t.Contains("星髓：8 / 4")) && !UnityEngine.GUI.labels.Exists(t => t.Contains("净煞尘")), "target-specific probability and cost stay verbatim for capacity " + capacity);
            Check(c.Endgame.Items[0].SocketCount == 0 && c.Endgame.Materials[0].Count == 8, "quote never predicts holes or spends materials for capacity " + capacity);
            Draw(p, "照此投料 · 炼化"); var execute = c.Last;
            Check(execute.Op == "cube.execute" && execute.Json.Contains("socket-quote") && !execute.Json.Contains("socketCount") && !execute.Json.Contains("targetUid"), "execution uses only frozen quote identity for capacity " + capacity);
            sent = c.Sent.Count; Draw(p, "照此投料 · 炼化");
            Check(c.Sent.Count == sent, "random opening cannot reroll by double click for capacity " + capacity);
            Draw(p, "×"); Check(ReferenceEquals(c.Endgame.PendingRequest, execute), "close retains the exact uncertain random request for capacity " + capacity);
            Receipt(c);
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"opened-base\",\"chunks\":1,\"revision\":\"2\",\"items\":[" + gear.Replace("\"socketCount\":0", "\"socketCount\":1") + "]}");
            Set(p, "open", true); Set(p, "selectedUID", "9007199254740901");
            Draw(p, "问鼎 · 查看完整耗费"); request = c.Last;
            Apply(c, "{\"kind\":\"cube.quote\",\"ok\":false,\"requestId\":\"" + request.RequestID + "\",\"error\":\"requirements\",\"message\":\"已有孔不可重开。\"}");
            Draw(p);
            Check(c.Endgame.Quote == null && !ArtworkSkin.enableds["照此投料 · 炼化"] && UnityEngine.GUI.labels.Contains("已有孔不可重开。"), "server rejection of existing holes leaves no executable quote for capacity " + capacity);
        }
    }
    static void ItemizationStatTextContracts()
    {
        var format = typeof(EndgamePanel).GetMethod("StatText", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var pair in new[] { new KeyValuePair<int, string>(-20, "攻击速度 +20%"), new KeyValuePair<int, string>(15, "攻击速度 -15%") })
        {
            var stat = new EndgameStatView(new Dictionary<string, object> { { "attr", 25L }, { "mode", 1L }, { "value", (long)pair.Key } });
            Check((string)format.Invoke(null, new object[] { stat }) == pair.Value, "attack-speed wording matches actual player percentage: " + pair.Key);
        }
    }
    static void Main()
    {
        ItemizationStatTextContracts();
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); Draw(p); Check(c.Sent.Count == 0, "unnegotiated UI sends no commands");
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"); Call(p, "Update");
        string items = "{\"uid\":\"9007199254740993\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"飞鹤遗音\",\"uniqueId\":1001,\"uniqueRollBp\":10000,\"socketCount\":2,\"sockets\":[{\"index\":0,\"itemId\":0},{\"index\":1,\"itemId\":0}]}, {\"uid\":\"9007199254740994\",\"itemId\":1900028001,\"location\":\"bag\",\"tab\":1,\"slot\":1,\"name\":\"角木蛟星篆\"}, {\"uid\":\"9007199254740995\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":25,\"name\":\"归元佩\"}";
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"ui\",\"chunks\":1,\"revision\":\"1\",\"items\":[" + items + "]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1001, bagIdx = 0 }; InventoryDlg.rows[1] = new InventoryDlg.Item { id = 1900028001, bagIdx = 1 };
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1002, slot = 25 });
        Draw(p, "演劫 · F8"); Check(!(bool)Get(p, "open"), "removed toolbar cannot open any module");
        Check(EndgamePanel.TryOpenRunesFromWorld(), "artisan opens standalone rune workbench");
        Check(c.Sent.Exists(r => r.Op == "snapshot") && !c.Sent.Exists(r => r.Catalog == "uniques" || r.Catalog == "formations" || r.Catalog == "recipes"), "rune entry loads owned state without encyclopedia catalogs");
        string tooltip = EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], "legacy details"); Check(tooltip.Contains("\u00035飞鹤遗音") && tooltip.Contains("legacy details") && !tooltip.Contains("宿序"), "dark-gold tooltip retains native description without duplicate socket block");
        Check(EndgamePanel.BagTooltip(0, InventoryDlg.rows[0], "legacy") == "legacy", "wrong tab cannot overlay");
        Set(p, "selectedUID", "9007199254740993"); Set(p, "module", EndgameModule.Runes); Set(p, "selectedRuneUID", "9007199254740994");
        Draw(p, "嵌入所选星篆"); Check(c.Last.Op == "socket.insert" && c.Last.Json.Contains("9007199254740994"), "insert submits exact rune UID");
        int count = c.Sent.Count; Draw(p, "嵌入所选星篆"); Check(c.Sent.Count == count, "double mutation blocked"); Draw(p, "查询原请求"); Check(c.Retries == 1, "retry uses pending request");
        var pending = c.Endgame.PendingRequest; Draw(p, "×"); Check(!(bool)Get(p, "open") && ReferenceEquals(c.Endgame.PendingRequest, pending) && WinZ.forgotten > 0, "close retains nonce and removes hit region"); Receipt(c);
        Set(p, "open", true); Set(p, "module", EndgameModule.Cube); Set(p, "recipeID", 11001);
        c.Endgame.Begin(new EndgameRequest("catalog", catalog: "recipes"));
        Apply(c, "{\"kind\":\"catalog.recipes\",\"ok\":true,\"recipes\":[{\"id\":11001,\"name\":\"凝髓成篆\",\"description\":\"确定星篆\",\"operation\":\"rune.inscribe\",\"money\":\"30000\",\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":30}]}]}");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"wallet\",\"chunks\":1,\"revision\":\"2\",\"items\":[" + items + "],\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":12}]}");
        Draw(p, "问鼎 · 查看完整耗费"); var quoteRequest = c.Last;
        Check(quoteRequest.Op == "cube.quote" && !quoteRequest.Json.Contains("targetUid"), "inscribe quote needs no target"); Check(UnityEngine.GUI.labels.Exists(t => t.Contains("持有 12 / 需要 30")), "material shortfall visible before quote");
        Apply(c, "{\"kind\":\"cube.quote\",\"ok\":true,\"requestId\":\"" + quoteRequest.RequestID + "\",\"quote\":{\"quoteId\":\"opaque-quote\",\"recipeId\":11001,\"revision\":\"2\",\"expiresAt\":\"2099-01-01T00:00:00Z\",\"description\":\"消耗材料炼制\",\"money\":\"30000\"}}");
        Draw(p, "照此投料 · 炼化"); Check(c.Last.Op == "cube.execute" && c.Last.Json.Contains("opaque-quote") && !c.Last.Json.Contains("itemUids") && !c.Last.Json.Contains("recipeId"), "execute contains only frozen quote identity"); Receipt(c);
        Set(p, "module", EndgameModule.Rift); Rift(c, "invited-run", "reserved"); Call(p, "Refresh"); Check(c.Last.Json.Contains("invited-run"), "refresh preserves invitation run");
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
        Check((bool)Get(p, "open") && (EndgameModule)Get(p, "module") == EndgameModule.Rift && c.Sent.FindAll(r => r.Mutates).Count == mutations, "HUD opens result review without claiming automatically");
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
        TooltipContracts(c, p);
        AuthoredDescriptionContracts(c, p);
        SocketRecipeContracts();
        Console.WriteLine("PASS " + assertions + " real-panel action contracts with Unity/artwork stubs; no rendering or engine launch.");
    }
}
