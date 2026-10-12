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
        var classifyEffect = typeof(EndgamePanel).GetMethod("IsLegacyEquipmentEffect", BindingFlags.NonPublic | BindingFlags.Static);
        Check((bool)classifyEffect.Invoke(null, new object[] { "赋予2级白虎卫身光环", "" }), "concise aura line remains a special effect without detailed equipment explanation");
        Check(!(bool)classifyEffect.Invoke(null, new object[] { "白虎卫身的古老传说", "" }), "aura name in ordinary lore is not misclassified");
        var unique = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"飞鹤遗音\",\"description\":\"仙人遗音。\",\"socketCount\":2,\"level\":42");
        string native = "\u0007\u00034旧名字+7\n\u0007(武器)\n\n\u00034仙遗·飞鹤遗音\n基底：旧名字\n\u00034力量 +12\n\u00034暴击时，伤害提高8%\n同名仙遗特性取最高值；不同特性按通道上限叠加。\n仙人遗音。\n\n耐久度: 50/50\n需要等级: 20\n需要职业: 剑客\n\n攻击 20-30\n重量 5\n\n\u00034命中 +5\n\u00034孔数 0/2\n原有描述。";
        string tip = EndgamePanel.OverlayTooltip(unique, native);
        Check(tip.StartsWith("\u0007\u00035飞鹤遗音+7\n\u0007(武器)"), "unique replaces centered title without prepended block and retains refinement");
        Check(!tip.Contains("仙遗·") && !tip.Contains("基底") && !tip.Contains("同名仙遗") && !tip.Contains("宿序") && !tip.Contains("职业要求"), "unique removes redundant header and explanatory overlay");
        Check(Occurrences(tip, "力量 +12") == 1 && Occurrences(tip, "仙人遗音。") == 1 && Occurrences(tip, "孔数") == 1 && Occurrences(tip, "需要职业") == 1, "unique native attributes lore sockets and requirements appear once");
        Check(tip.IndexOf("攻击 20-30") < tip.IndexOf("力量 +12") && tip.IndexOf("力量 +12") < tip.IndexOf("孔数") && tip.IndexOf("孔数") < tip.IndexOf("仙人遗音。"), "unique uses native intrinsic attribute socket description order");
        Check(tip.Contains("\u00035力量 +12") && tip.Contains("\u0006暴击时") && tip.Contains("\u00035命中 +5") && tip.Contains("需要等级: 42"), "unique attributes use dark gold, effects orange and existing level row is updated");
        string noSockets = native.Replace("\u00034孔数 0/2\n", "").Replace("原有描述。", "\u00032套装: 原生套装 (1/2)\n\u0002[2件] 力量 +4\n法宝技能: 原生技能\n原有描述。\n（绑定物品不可交易/摆摊/邮寄, 可卖NPC）");
        string noSocketTip = EndgamePanel.OverlayTooltip(unique, noSockets);
        Check(noSocketTip.IndexOf("力量 +12") < noSocketTip.IndexOf("套装:") && noSocketTip.IndexOf("力量 +12") < noSocketTip.IndexOf("原有描述。"), "socketless unique attributes remain before native suit skill and description sections");
        Check(noSocketTip.Contains("\u00032套装:") && noSocketTip.Contains("\u0002[2件] 力量 +4") && noSocketTip.IndexOf("仙人遗音。") < noSocketTip.IndexOf("（绑定物品不可交易"), "native section colors activation markers and final binding footer are preserved");
        c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"tooltip\",\"chunks\":1,\"items\":[],\"formations\":[{\"id\":1,\"name\":\"宿阵·青龙归元\",\"description\":\"星宿归元。\",\"active\":true,\"channel\":\"battle\",\"stats\":[{\"attr\":1,\"value\":15}]}]}");
        var formation = TooltipItem("\"itemId\":1001,\"formationId\":1,\"location\":\"bag\",\"slot\":0,\"name\":\"普通剑\",\"socketCount\":2,\"sockets\":[{\"index\":1,\"itemId\":1900028002,\"name\":\"星篆·亢金龙\"},{\"index\":0,\"itemId\":1900028001,\"name\":\"星篆·角木蛟\"}]");
        tip = EndgamePanel.OverlayTooltip(formation, "\u0007普通剑+12\n\u0007(武器)\n耐久度: 50/50\n需要等级: 20\n攻击 20-30\n孔数 2/2");
        Check(tip.StartsWith("\u0007\u00035青龙归元+12\n\u0007(武器)") && Occurrences(tip, "青龙归元") == 1, "formation uses native title once");
        Check(tip.Contains("\u0007(武器)\n\u0007\u00035角亢\n耐久度:") && tip.IndexOf("力量 +15") < tip.IndexOf("孔数 2/2") && tip.EndsWith("星宿归元。"), "formation initials form one centered separator-free line below the slot heading in socket order, with stats and lore in native sections");
        Check(!tip.Contains("宿阵·") && !tip.Contains("星篆·") && !tip.Contains("角木蛟") && !tip.Contains("亢金龙") && Occurrences(tip, "角亢") == 1, "equipment composition contains only each rune's first character and no prefixed formation title");
        Check(tip.Contains("\n攻击 20-30\n") && !tip.Contains("\u00035攻击 20-30") && tip.Contains("\u00035孔数 2/2"), "formation intrinsic base remains white and socket count is dark gold");
        string legacyColors = EndgamePanel.OverlayTooltip(formation, "\u0007普通剑\n\u0007(武器)\n攻击 20-30\n\u0005力量 +3\n\u0001体质 +4\n\u0004需要等级: 60\n\u0002[2件] 防御 +9\n\u0005孔数 2/2");
        Check(legacyColors.Contains("\u00035力量 +3") && legacyColors.Contains("\u00035体质 +4") && legacyColors.Contains("\u0004需要等级: 60") && legacyColors.Contains("\u0002[2件] 防御 +9"), "legacy special affix palette becomes dark gold without recoloring red requirements or disabled set bonuses");
        string absentSlot = EndgamePanel.OverlayTooltip(formation, "\u0007普通剑+12\n耐久度: 50/50\n攻击 20-30\n孔数 2/2");
        Check(absentSlot.StartsWith("\u0007\u00035青龙归元+12\n\u0007\u00035角亢\n耐久度:"), "legacy tooltip without slot heading keeps composition below title");
        string absentTitle = EndgamePanel.OverlayTooltip(formation, "\u0007(武器)\n耐久度: 50/50\n孔数 2/2");
        Check(absentTitle.StartsWith("\u0007\u00035青龙归元\n\u0007(武器)\n\u0007\u00035角亢\n耐久度:"), "slot heading survives an older native description without its title wrapper");
        var fiveRunes = TooltipItem("\"itemId\":1001,\"formationId\":1,\"location\":\"bag\",\"slot\":0,\"name\":\"普通剑\",\"socketCount\":5,\"sockets\":[{\"index\":4,\"itemId\":1900028006,\"name\":\"星篆·尾火虎\"},{\"index\":1,\"itemId\":1900028002,\"name\":\"亢金龙\"},{\"index\":3,\"itemId\":1900028001,\"name\":\"角木蛟\"},{\"index\":0,\"itemId\":1900028001,\"name\":\"星篆·角木蛟\"},{\"index\":2,\"itemId\":1900028005,\"name\":\"星篆·心月狐\"}]");
        string fiveTip = EndgamePanel.OverlayTooltip(fiveRunes, "\u0007普通剑\n\u0007(武器)\n\n耐久度: 50/50\n孔数 5/5");
        Check(fiveTip.Contains("\u0007(武器)\n\u0007\u00035角亢心角尾\n\n耐久度:"), "five-rune recipe is centered without separators and preserves exact socket indexes including repeated runes and native blank spacing");
        string equalStats = EndgamePanel.OverlayTooltip(formation, "\u0007普通剑\n\u0007(武器)\n耐久度: 50/50\n力量 +15\n孔数 2/2");
        Check(Occurrences(equalStats, "力量 +15") == 2, "equal native and formation values remain independent contributions");
        Check(EndgamePanel.OverlayTooltip(formation, equalStats) == equalStats, "repeated legacy formation overlay retains both equal independent contributions once");
        var combined = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"formationId\":1,\"location\":\"bag\",\"slot\":0,\"name\":\"组合器\",\"description\":\"组合传说\"");
        string combinedTip = EndgamePanel.OverlayTooltip(combined, "\u0007组合器\n\u0007(武器)\n\n\u00034仙遗·组合器\n基底：普通剑\n\u00034力量 +15\n组合传说\n\n耐久度: 50/50\n力量 +15\n孔数 2/2");
        Check(Occurrences(combinedTip, "力量 +15") == 3, "equal native unique and formation bonuses each retain their real contribution");
        Check(EndgamePanel.OverlayTooltip(combined, combinedTip) == combinedTip, "repeated combined legacy overlay retains all three equal independent contributions once");
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
        Check(Occurrences(tip, "普通攻击伤害提高10%") == 1 && tip.IndexOf("孔数") < tip.IndexOf("旧炉之火。") && tip.EndsWith("\u0006普通攻击伤害提高10%"), "native unique effect is not repeated by existing owned Trait field");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"effects\",\"chunks\":1,\"items\":[],\"formations\":[{\"id\":1,\"name\":\"枪火宿阵\",\"description\":\"军中旧旗。\",\"stats\":[{\"attr\":1,\"value\":15}]}]}");
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1001, slot = 0 });
        foreach (string state in new[] { "\"location\":\"bag\"", "\"location\":\"equipped\",\"active\":true", "\"location\":\"equipped\",\"inactiveReason\":\"同类宿阵优先级未满足\"", "\"location\":\"equipped\",\"inactiveReason\":\"职业、等级或耐久不符合要求\"" })
        {
            item = TooltipItem("\"itemId\":1001,\"formationId\":1,\"slot\":0,\"name\":\"旧枪\",\"trait\":\"连环枪法伤害提高12%\"," + state);
            tip = EndgamePanel.OverlayTooltip(item, "\u0007旧枪\n需要等级: 50\n攻击 20-30\n孔数 2/2");
            Check(Occurrences(tip, "连环枪法伤害提高12%") == 1 && tip.EndsWith("\u0006连环枪法伤害提高12%") && Occurrences(tip, "军中旧旗。") == 1, "ordinary formation potential effect remains once as the final orange block for " + state);
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
    static void SpecialLoreTooltipPathContracts(Client c, EndgamePanel p)
    {
        // The native server omits only template prose for these identities, before
        // composing item.desc. The client must preserve that result and supplement
        // authored lore without filtering arbitrary native requirement/effect text.
        const string footer = "（绑定物品不可交易/摆摊/邮寄, 可卖NPC）";
        string nativeBody = "\u0007(武器)\n（绑定）（可用天工剪解除绑定）\n\u00034变装: 原生外观\n耐久度: 0/50\n\u0004需要等级: 60\n需要职业: 剑客\n攻击 20-30\n重量 5\n\u00035孔数 2/2\n法宝技能: 原生技能\n技能效果: 原生技能效果。\n" + footer;
        foreach (bool unique in new[] { true, false })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
            string special = unique ? "\"uniqueId\":1,\"description\":\"仙遗旧事。\\n故人已去。\",\"trait\":\"普通攻击伤害提高10%\"" : "\"formationId\":1,\"trait\":\"连环枪法伤害提高12%\"";
            string fields = "\"name\":\"器物\",\"socketCount\":2,\"level\":60,\"sockets\":[{\"index\":1,\"itemId\":1900028002,\"name\":\"星篆·亢金龙\"},{\"index\":0,\"itemId\":1900028001,\"name\":\"星篆·角木蛟\"}]," + special;
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"lore-paths\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":25," + fields + "}],\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\",\"description\":\"宿阵旧事。\\n星河长明。\"}]}");
            string native = nativeBody;
            if (unique) native = native.Replace("（绑定）（可用天工剪解除绑定）", "仙遗·器物\n基底：原生剑\n力量 +12\n普通攻击伤害提高10%\n仙遗旧事。\n故人已去。\n\n（绑定）（可用天工剪解除绑定）");
            var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "器物+7", desc = native };
            var equipped = new EquipDlg.EquippedItem { id = 1002, slot = 25, quality = 4, name = "器物+7", desc = native };
            InventoryDlg.rows[0] = bag;
            EquipDlg.Equipped.RemoveAll(row => row.slot == 25); EquipDlg.Equipped.Add(equipped);
            string original = TooltipContent.Fmt(TooltipContent.QualityName(bag.name, bag.quality), native);
            string bagTip = EndgamePanel.BagTooltip(2, bag, original);
            string equippedTip = EndgamePanel.EquippedTooltip(equipped, original);
            var ownedTooltip = typeof(EndgamePanel).GetMethod("OwnedItemTooltip", BindingFlags.Static | BindingFlags.NonPublic);
            Check(bagTip == equippedTip, "bag and equipped/comparison preserve the same authored-only lore for " + special);
            if (!unique)
            {
                Check(bagTip.StartsWith("\u0007\u00035照夜+7\n\u0007(武器)\n\u0007\u00035角亢\n"), "bag/equipped/comparison and workbench use prefix-free title and abbreviated composition directly below slot");
                Check(!bagTip.Contains("宿阵·照夜") && !bagTip.Contains("角木蛟") && !bagTip.Contains("亢金龙") && Occurrences(bagTip, "角亢") == 1, "all equipment tooltip paths keep one compact ordered composition");
            }
            Check(bagTip.Contains("\n攻击 20-30\n") && bagTip.Contains("\u00035孔数 2/2"), "all special equipment paths retain white intrinsic base and dark-gold socket count");
            foreach (var item in c.Endgame.Items)
                Check((string)ownedTooltip.Invoke(null, new object[] { item }) == bagTip, "workbench shares the native authored-only tooltip for " + item.Location);
            foreach (string lore in unique ? new[] { "仙遗旧事。", "故人已去。" } : new[] { "宿阵旧事。", "星河长明。" })
                Check(Occurrences(bagTip, lore) == 1 && bagTip.IndexOf("技能效果:") < bagTip.IndexOf(lore) && bagTip.IndexOf(lore) < bagTip.IndexOf(footer), "authored lore remains once after functional sections and before binding footer: " + lore);
            foreach (string preserved in new[] { "（绑定）（可用天工剪解除绑定）", "\u00034变装: 原生外观", "耐久度: 0/50", "\u0004需要等级: 60", "需要职业: 剑客", "攻击 20-30", "重量 5", "孔数 2/2", "法宝技能: 原生技能", "技能效果: 原生技能效果。", footer })
                Check(bagTip.Contains(preserved), "native functional section and status survives authored-only lore: " + preserved);
            Check(!bagTip.Contains("旧底材的来历") && bagTip.IndexOf(footer) < bagTip.LastIndexOf("\u0006"), "authored overlay keeps binding footer before the final effect block without reintroducing template lore");
        }
        c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"ordinary-lore\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0}]}");
        string ordinary = "\u0007普通剑\n" + nativeBody.Replace(footer, "旧底材的来历。\n另一段原装叙述。\n" + footer);
        Check(EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], ordinary) == ordinary, "ordinary multiline base prose remains byte-for-byte unchanged through the bag path");
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
    static void NativeStatTooltipContracts(Client c, EndgamePanel p)
    {
        const string native = "\u0007(武器)\n耐久度: 50/50\n需要等级: 60\n需要职业: 剑客\n攻击 200-300\n防御 60\n魔法攻击 50\n重量 5\n\u00035力量 +18\n\u00035攻击 +36%\n\u00035移动速度 -3%\n\u00035本件基础攻击强化 +180%\n\u00035连环枪法伤害提高12%\n\u00035孔数 2/2\n法宝技能: 原生技能\n（绑定物品不可交易）";
        foreach (bool nativeStats in new[] { false, true })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            foreach (string activation in new[] { "\"active\":true", "\"inactiveReason\":\"同类宿阵优先级未满足\"", "\"inactiveReason\":\"职业、等级或耐久不符合要求\"" })
            {
                string fields = "\"name\":\"普通剑\",\"socketCount\":2,\"formationId\":1,\"level\":60,\"trait\":\"本件基础攻击强化 +180%；连环枪法伤害提高12%\",\"sockets\":[{\"index\":1,\"itemId\":1900028002,\"name\":\"星篆·亢金龙\"},{\"index\":0,\"itemId\":1900028001,\"name\":\"星篆·角木蛟\"}]";
                Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"native-stats-" + activation.Length + "\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":25," + fields + "," + activation + "}],\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\",\"description\":\"故旗尚温。\",\"stats\":[{\"attr\":1,\"value\":15},{\"attr\":47,\"value\":30,\"mode\":1}]}]}");
                var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "普通剑+7", desc = native };
                var equipped = new EquipDlg.EquippedItem { id = 1002, slot = 25, quality = 4, name = "普通剑+7", desc = native };
                InventoryDlg.rows[0] = bag; EquipDlg.Equipped.RemoveAll(row => row.slot == 25); EquipDlg.Equipped.Add(equipped);
                var ownedTooltip = typeof(EndgamePanel).GetMethod("OwnedItemTooltip", BindingFlags.Static | BindingFlags.NonPublic);
                string original = TooltipContent.Fmt(TooltipContent.QualityName(bag.name, bag.quality), native);
                string bagTip = EndgamePanel.BagTooltip(2, bag, original);
                string equippedTip = EndgamePanel.EquippedTooltip(equipped, original);
                foreach (string tip in new[] { bagTip, equippedTip })
                {
                    Check(tip.Contains("力量 +15") != nativeStats && tip.Contains("攻击 +30%") != nativeStats, "negotiated native extras suppress catalog duplication; old servers retain fallback");
                    Check(Occurrences(tip, "力量 +18") == 1 && Occurrences(tip, "攻击 +36%") == 1 && Occurrences(tip, "移动速度 -3%") == 1, "server-merged same-scope flat percentage and negative values display once");
                    Check(Occurrences(tip, "攻击强化 +180%") == 1 && !tip.Contains("本件") && Occurrences(tip, "连环枪法伤害提高12%") == 1, "local enhancement and real effect lines survive once without merging into global percent");
                    Check(tip.Contains("\n攻击 200-300\n防御 60\n魔法攻击 50\n重量 5\n") && tip.Contains("\u00035孔数 2/2"), "all intrinsic base rows stay white while special socket count is dark gold");
                    Check(tip.StartsWith("\u0007\u00035照夜+7\n\u0007(武器)\n\u0007\u00035角亢\n") && Occurrences(tip, "故旗尚温。") == 1, "merged stats preserve compact name composition and authored lore");
                }
                Check(!bagTip.Contains("不生效") && equippedTip.Contains("不生效") == activation.Contains("同类宿阵"), "potential values remain visible across active broken and suppressed items without false activation claims");
                foreach (var item in c.Endgame.Items)
                    Check((string)ownedTooltip.Invoke(null, new object[] { item }) == (item.Location == "bag" ? bagTip : equippedTip), "workbench exactly shares bag/equipment/comparison negotiated tooltip");
                var displayName = typeof(EndgamePanel).GetMethod("DisplayName", BindingFlags.Static | BindingFlags.NonPublic);
                Check((string)displayName.Invoke(null, new object[] { c.Endgame.Items[0] }) == "照夜", "workbench item rows strip legacy formation prefix too");
            }
            var unique = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"uniqueRollBp\":12000,\"location\":\"bag\",\"slot\":0,\"name\":\"照夜剑\",\"stats\":[{\"attr\":1,\"value\":99}]");
            string uniqueTip = EndgamePanel.OverlayTooltip(unique, "\u0007照夜剑\n\u0007(武器)\n攻击 200-300\n\u00035力量 +18\n\u00035孔数 0/2");
            Check(uniqueTip.Contains("力量 +99") != nativeStats && Occurrences(uniqueTip, "力量 +18") == 1, "native unique instance values are not duplicated with DTO reference stats");
            string explicitWhite = EndgamePanel.OverlayTooltip(unique, "\u0007照夜剑\n\u0007(武器)\n\u00030攻击 200-300\n\u00030魔法防御 -20\n\u00035力量 +18\n\u00035孔数 0/2");
            Check(explicitWhite.Contains("\n\u00030攻击 200-300\n\u00030魔法防御 -20\n") && !explicitWhite.Contains("\u00035魔法防御 -20"), "explicit native white marker preserves even negative intrinsic rows without center alignment");
            Check(EndgamePanel.ItemDescription(unique, true).Contains("道意") != nativeStats, "selective variable rolls never imply a universal equipment multiplier");
            var unformed = TooltipItem("\"itemId\":1001,\"location\":\"bag\",\"slot\":0,\"socketCount\":2");
            string ordinary = "\u0007普通剑\n\u0007(武器)\n攻击 20-30\n\u00032力量 +3\n\u00032孔数 1/2";
            Check(EndgamePanel.OverlayTooltip(unformed, ordinary) == ordinary, "unformed rune gear preserves original colors and values");
        }
    }
    static void EquipmentPresentationContracts(Client c, EndgamePanel p)
    {
        const string footer = "（绑定物品不可交易/摆摊/邮寄, 可卖NPC）";
        string[] ordinary = { "力量 +18", "攻击 +36%", "移动速度 -3%", "攻击强化 +180%", "防御强化 +25%", "魔法装备获取几率提升 +15%",
            "所有战斗技能 +2级", "战士左系主修技能 +1级", "连环枪法 +3级", "火系伤害 +10%", "敌人冰系抗性 -12.5%", "圣系抗性 +7%", "暗系伤害减免 8%", "火系伤害吸收 9%" };
        string[] effects = { "普通攻击伤害提高10%", "攻击命中时有5%概率释放3级天雷（冷却6秒）", "赋予2级护身光环：物理伤害减免 4%（范围300，含自身最多4人）", "攻击 +999", "生命恢复 +2" };
        foreach (int identity in new[] { 1, 2, 3 }) // unique, formation, combined
        foreach (bool nativeStats in new[] { false, true })
        foreach (bool legacyText in new[] { false, true })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            string fields = "\"name\":\"旧器\",\"socketCount\":2,\"level\":60,\"description\":\"旧事记载着伤害提高。\",\"trait\":\"本件基础攻击强化 +180%；防御强化 +25%（本件）；染色率 +15%；所有战斗技能 +2级；火系伤害 +10%；魔法攻击强化 +33%（本件）；普通攻击伤害提高10%\",\"stats\":[{\"attr\":1,\"value\":99}],\"sockets\":[{\"index\":0,\"itemId\":1900028001,\"name\":\"星篆·角木蛟\"},{\"index\":1,\"itemId\":1900028002,\"name\":\"星篆·亢金龙\"}]";
            if ((identity & 1) != 0) fields += ",\"uniqueId\":1";
            if ((identity & 2) != 0) fields += ",\"formationId\":1,\"inactiveReason\":\"同类宿阵优先级未满足\"";
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"equipment-presentation\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":25," + fields + "}],\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\",\"description\":\"星河长明。\",\"stats\":[{\"attr\":1,\"value\":77}]}]}");
            string native = "\u0007(武器)\n\u00034变装: 原生外观\n耐久度: 0/50\n\u0004需要等级: 60\n需要职业: 剑客\n\u00030攻击 200-300\n\u00030魔法防御 -20\n防御 60\n重量 5\n";
            foreach (string row in ordinary)
            {
                string value = row;
                if (legacyText) value = value.Replace("攻击强化 +180%", "本件基础攻击强化 +180%").Replace("防御强化 +25%", "防御强化 +25%（本件）").Replace("魔法装备获取几率提升", "染色率");
                native += (legacyText ? "\u00034" : "\u00035") + value + "\n";
            }
            native += "\u00035未来属性 余韵一重\n\u00035未来伤害提高系数 +3%\n";
            string finalEffects = "";
            for (int i = 0; i < effects.Length; i++)
                finalEffects += (i == 0 ? "" : "\n") + "\u0006" + effects[i];
            if (legacyText)
                for (int i = 0; i < effects.Length; i++) native += (i < 3 ? "\u00034" : "\u0006") + effects[i] + "\n";
            native += "\u00034孔数 2/2\n\u00032套装: 原生套装 (1/2)\n\u0002[2件] 防御 +9\n法宝技能: 原生技能\n技能效果: 伤害提高5%\n" + footer;
            if (!legacyText) native += "\n" + finalEffects;
            var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "旧器+7", desc = native };
            var equipped = new EquipDlg.EquippedItem { id = 1002, slot = 25, quality = 4, name = "旧器+7", desc = native };
            InventoryDlg.rows[0] = bag; EquipDlg.Equipped.RemoveAll(row => row.slot == 25); EquipDlg.Equipped.Add(equipped);
            string original = TooltipContent.Fmt(TooltipContent.QualityName(bag.name, bag.quality), native);
            string bagTip = EndgamePanel.BagTooltip(2, bag, original), equippedTip = EndgamePanel.EquippedTooltip(equipped, original);
            var ownedTooltip = typeof(EndgamePanel).GetMethod("OwnedItemTooltip", BindingFlags.Static | BindingFlags.NonPublic);
            foreach (var item in c.Endgame.Items)
            {
                string tip = item.Location == "bag" ? bagTip : equippedTip;
                foreach (string row in ordinary)
                    Check(tip.Contains("\u00035" + row) && Occurrences(tip, row) == 1 && tip.IndexOf(row) < tip.IndexOf("孔数"), "ordinary/new attribute retains one darkgold native-section row: " + row);
                Check(tip.Contains("\u00035魔法攻击强化 +33%") && Occurrences(tip, "魔法攻击强化 +33%") == 1 && tip.IndexOf("魔法攻击强化 +33%") < tip.IndexOf("孔数"), "mixed Trait's absent local ordinary attribute fills in once");
                Check(!tip.Contains("本件") && !tip.Contains("染色率") && !tip.Contains("MF"), "legacy local and MF labels normalize everywhere");
                Check(tip.Contains("\u00035未来属性 余韵一重") && tip.Contains("\u00035未来伤害提高系数 +3%") && tip.IndexOf("未来伤害提高系数 +3%") < tip.IndexOf("孔数") && tip.Contains("\u00034变装: 原生外观"), "future semantic darkgold attribute and unrelated native quality marker survive");
                Check(tip.Contains("\u00030攻击 200-300\n\u00030魔法防御 -20\n防御 60\n重量 5") && tip.Contains("\u0004需要等级: 60") && tip.Contains("\u0002[2件] 防御 +9"), "white bases red requirements and gray set bonuses remain unchanged");
                foreach (string row in effects) Check(Occurrences(tip, row) == 1 && tip.Contains("\u0006" + row), "effect preserved once and orange including attribute-like future effects: " + row);
                Check(tip.EndsWith(finalEffects) && tip.IndexOf(footer) < tip.IndexOf("\u0006"), "native and legacy effects keep their order at the literal end after binding and lore: " + tip.Replace("\u0003", "[quality]").Replace("\u0006", "[orange]"));
                Check(tip.Contains("技能效果: 伤害提高5%") && tip.Contains("\n旧事记载着伤害提高。\n"), "functional prose and authored lore are not classified as effect attributes");
                Check(tip.Contains("力量 +99") == (!nativeStats && (identity & 1) != 0) && tip.Contains("力量 +77") == (!nativeStats && (identity & 2) != 0), "negotiated native stats suppress unique/formation reference numbers");
                Check(tip.Contains("\u0002不生效") == (item.Location == "equipped" && (identity & 2) != 0), "inactive formation status remains gray and authoritative");
                Check(EndgamePanel.OverlayTooltip(item, tip) == tip, "repeated overlay preserves native/legacy values colors final-effect order and composition");
                Check((string)ownedTooltip.Invoke(null, new object[] { item }) == tip, "workbench uses exact bag/equipped/comparison presentation");
                string title = (identity & 2) != 0 ? "照夜" : "旧器";
                Check(tip.StartsWith("\u0007\u00035" + title + "+7\n\u0007(武器)\n") && Occurrences(tip, "角亢") == ((identity & 2) != 0 ? 1 : 0), "name refinement slot and runeword composition stay compact");
            }
            string description = EndgamePanel.ItemDescription(c.Endgame.Items[0], true);
            Check(description.Contains("\u00035魔法装备获取几率提升 +15%") && description.Contains("\u00035攻击强化 +180%") && description.EndsWith("\u0006普通攻击伤害提高10%\n"), "catalog-style fallback also separates mixed traits and ends in orange effects");
        }
        var legacyUnique = TooltipItem("\"itemId\":1001,\"location\":\"bag\",\"slot\":0,\"uniqueId\":1,\"name\":\"旧器\"");
        string legacyBlock = "\u0007旧器\n\u0007(武器)\n仙遗·旧器\n基底：旧剑\n\u00035未来属性 余韵一重\n\u0006攻击 +999\n\u00034染色率 +15%\n\n攻击 20-30\n孔数 0/2\n" + footer;
        string blockTip = EndgamePanel.OverlayTooltip(legacyUnique, legacyBlock);
        Check(blockTip.Contains("\u00035未来属性 余韵一重") && blockTip.Contains("\u00035魔法装备获取几率提升 +15%") && blockTip.EndsWith("\u0006攻击 +999"), "legacy unique header extraction respects authoritative gold/orange markers before text heuristics");
    }
    static void FormationLevelTooltipContracts(Client c, EndgamePanel p)
    {
        foreach (bool nativeStats in new[] { false, true })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            foreach (int required in new[] { 14, 63 })
            foreach (string color in new[] { "", "\u0004" })
            {
                string fields = "\"name\":\"普通剑\",\"socketCount\":2,\"formationId\":6,\"level\":" + required;
                Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"formation-level-" + required + color.Length + "\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"slot\":25," + fields + "}],\"formations\":[{\"id\":6,\"name\":\"担山不语\",\"minLevel\":14}]}");
                string native = "\u0007(武器)\n耐久度: 50/50\n" + color + "需要等级: 20\n需要职业: 战士\n攻击 20-30\n孔数 2/2";
                var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, name = "普通剑", desc = native };
                var equipped = new EquipDlg.EquippedItem { id = 1002, slot = 25, name = "普通剑", desc = native };
                InventoryDlg.rows[0] = bag; EquipDlg.Equipped.RemoveAll(row => row.slot == 25); EquipDlg.Equipped.Add(equipped);
                string original = TooltipContent.Fmt(bag.name, native);
                foreach (string tip in new[] { EndgamePanel.BagTooltip(2, bag, original), EndgamePanel.EquippedTooltip(equipped, original) })
                {
                    Check(tip.Contains(color + "需要等级: " + required) && Occurrences(tip, "需要等级: ") == 1 && !tip.Contains("需要等级: 20"), "formed item uses exact server max-rune/base level and preserves native requirement color");
                    Check(tip.Contains("需要职业: 战士") && tip.Contains("耐久度: 50/50") && tip.Contains("攻击 20-30"), "level replacement retains independent native requirements and equipment properties");
                }
            }
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
        SpecialLoreTooltipPathContracts(c, p);
        NativeStatTooltipContracts(c, p);
        EquipmentPresentationContracts(c, p);
        FormationLevelTooltipContracts(c, p);
        SocketRecipeContracts();
        Console.WriteLine("PASS " + assertions + " real-panel action contracts with Unity/artwork stubs; no rendering or engine launch.");
    }
}
