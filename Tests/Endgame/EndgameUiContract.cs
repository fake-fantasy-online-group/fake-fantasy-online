using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;

partial class EndgameUiContract
{
    static int assertions;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); assertions++; }
    sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public List<EndgameRequest> Sent = new List<EndgameRequest>();
        public int Retries, Equips, Unequips;
        public readonly List<EndgameRequest> Retried = new List<EndgameRequest>();
        public bool SendEndgame(EndgameRequest request) { if (!Endgame.Begin(request)) return false; Sent.Add(request); return true; }
        public bool RetryEndgame() { Retries++; Retried.Add(Endgame.PendingRequest); return Endgame.Pending; }
        public bool EquipEndgameItem(EndgameItemView item) { Equips++; return true; }
        public bool UnequipEndgameItem(EndgameItemView item) { Unequips++; return true; }
        public EndgameRequest Last => Sent[Sent.Count - 1];
    }
    static void Set(object panel, string key, object value) => panel.GetType().GetField(key, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(panel, value);
    static object Get(object panel, string key) => panel.GetType().GetField(key, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
    static void Call(object panel, string key) => panel.GetType().GetMethod(key, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, null);
    static void Apply(Client client, string json) => client.Endgame.Apply(EndgameResponse.Parse(json));
    static bool Enabled(string text) => ArtworkSkin.enableds.TryGetValue(text, out bool enabled) && enabled;
    static void Draw(EndgamePanel panel, string click = null) { ArtworkSkin.enableds.Clear(); ArtworkSkin.click = click; UnityEngine.GUI.labels.Clear(); Call(panel, "OnGUI"); }
    static void Receipt(Client c)
    {
        var request = c.Endgame.PendingRequest;
        Apply(c, "{\"kind\":\"" + (request.Op == "cube.execute" ? "cube.result" : request.Op) + "\",\"ok\":true,\"requestId\":\"" + request.RequestID + "\",\"revision\":\"2\"}");
    }
    static void Rift(Client c, string runID, string phase, bool claimable = false, int keys = 0, bool ok = true, string extra = "")
    {
        Apply(c, "{\"kind\":\"rift.state\",\"ok\":" + (ok ? "true" : "false") + ",\"error\":\"requirements\",\"rift\":{\"runId\":\"" + runID + "\",\"revision\":\"4\",\"phase\":\"" + phase + "\",\"tier\":2,\"claimable\":" + (claimable ? "true" : "false") + ",\"keyCount\":" + keys + extra + "}}");
    }
    static void DropdownEvent(EndgamePanel panel, UnityEngine.EventType type, float x = -9000, float y = -9000, UnityEngine.KeyCode key = UnityEngine.KeyCode.F8, float wheel = 0)
    {
        UnityEngine.Event.current = new UnityEngine.Event { type = type, mousePosition = new UnityEngine.Vector2(x, y), keyCode = key, delta = new UnityEngine.Vector2(0, wheel) };
        Call(panel, "DrawRiftTierDropdown");
    }
    static void OpenTierList(EndgamePanel p)
    {
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(60, 150) };
        Call(p, "DrawRift");
        Check((bool)Get(p, "riftTierOpen") && UnityEngine.Event.current.type == UnityEngine.EventType.Used, "native input click opens dropdown and consumes world click");
    }
    static void OpenEntrance(Client c)
    {
        if (c.Endgame.RiftEntrances.Count == 0)
        {
            c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-entrance.v1\"],\"riftEntrances\":[{\"mapId\":7,\"npcId\":83,\"npc\":\"八仙使者\",\"script\":\"Nbaxian\"}]}");
        }
        GameBootstrap.CurMapId = 7;
        Check(EndgamePanel.TryOpenRiftFromNpc(7, 83, "八仙使者", "Nbaxian", () => true), "UI fixture opens from negotiated live entrance");
    }
    static void RiftDropdownContracts(Client c, EndgamePanel p)
    {
        Rift(c, "", "closed", keys: 1, extra: ",\"unlockedTier\":150,\"maxTier\":150");
        Set(p, "tier", 1); UnityEngine.Event.current = new UnityEngine.Event(); Draw(p);
        Check(WinZ.lastFrame.width == 600 && WinZ.lastFrame.height == 450, "rift uses its compact independent 600 by 450 window");
        Check(UnityEngine.GUI.labels.FindAll(t => t == "演劫洞天").Count == 1, "rift title is drawn only once");
        Check(Enabled("消耗一钥 · 开启洞天") && !ArtworkSkin.enableds.ContainsKey("领取我的洞天所得") && !ArtworkSkin.enableds.ContainsKey("离开洞天"), "fresh selector offers only its necessary start action");
        OpenTierList(p);
        DropdownEvent(p, UnityEngine.EventType.Repaint);
        Check(NativeScroll.calls > 0 && UnityEngine.GUI.labels.Exists(t => t == "第 8 阶"), "long unlocked list uses native scrolling and eight visible rows");
        DropdownEvent(p, UnityEngine.EventType.ScrollWheel, 100, 240, wheel: 1);
        Check((int)Get(p, "riftTierScroll") == 3 && UnityEngine.Event.current.type == UnityEngine.EventType.Used, "wheel scroll changes only dropdown rows");
        Set(p, "riftTierScroll", 149); DropdownEvent(p, UnityEngine.EventType.Repaint);
        Check((int)Get(p, "riftTierScroll") == 142, "150-tier scroll clamps to last full page");
        DropdownEvent(p, UnityEngine.EventType.MouseDown, 90, 194 + 7 * 24 + 5);
        Check((int)Get(p, "tier") == 150 && !(bool)Get(p, "riftTierOpen"), "clicking final visible row selects exactly tier 150");
        for (int option = 1; option <= 150; option++)
        {
            Set(p, "riftTierOpen", true); int scroll = Math.Min(option - 1, 142); Set(p, "riftTierScroll", scroll);
            DropdownEvent(p, UnityEngine.EventType.MouseDown, 90, 194 + (option - scroll - 1) * 24 + 5);
            Check((int)Get(p, "tier") == option && !(bool)Get(p, "riftTierOpen"), "every unlocked option is individually selectable: " + option);
        }
        OpenTierList(p); DropdownEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Home);
        Check((int)Get(p, "riftTierHighlight") == 1 && (int)Get(p, "riftTierScroll") == 0 && (int)Get(p, "tier") == 150, "Home moves highlight without changing selection");
        DropdownEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.UpArrow);
        Check((int)Get(p, "riftTierHighlight") == 1, "keyboard lower bound remains one");
        DropdownEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.PageDown);
        Check((int)Get(p, "riftTierHighlight") == 9 && (int)Get(p, "riftTierScroll") == 1, "PageDown keeps keyboard highlight visible");
        DropdownEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.End);
        DropdownEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.DownArrow);
        Check((int)Get(p, "riftTierHighlight") == 150 && (int)Get(p, "riftTierScroll") == 142, "End and arrow respect upper unlock bound");
        DropdownEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Return);
        Check(!(bool)Get(p, "riftTierOpen") && (int)Get(p, "tier") == 150, "Enter accepts dropdown highlight");
        OpenTierList(p); DropdownEvent(p, UnityEngine.EventType.MouseDown, 550, 350);
        Check(!(bool)Get(p, "riftTierOpen") && UnityEngine.Event.current.type == UnityEngine.EventType.Used, "outside dismissal consumes click before covered leave or start buttons");
        OpenTierList(p); DropdownEvent(p, UnityEngine.EventType.MouseDown, 60, 150);
        Check(!(bool)Get(p, "riftTierOpen"), "second field click closes list without reopening");
        OpenTierList(p); DropdownEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Escape);
        Check(!(bool)Get(p, "riftTierOpen") && (bool)Get(p, "open"), "Escape closes dropdown without closing its workbench");
        OpenTierList(p); Call(p, "ClearSelection");
        Check(!(bool)Get(p, "riftTierOpen") && (int)Get(p, "riftTierScroll") == 0, "close module or reconnect selection cleanup clears popup state");
        OpenTierList(p); Call(p, "Close"); Set(p, "open", true);
        Check(!(bool)Get(p, "riftTierOpen"), "reopened panel does not inherit an open dropdown");
        ArtworkSkin.respectEventUse = true;
        OpenTierList(p); int sent = c.Sent.Count;
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(200, 350) };
        Draw(p, "消耗一钥 · 开启洞天");
        Check(c.Sent.Count == sent && !(bool)Get(p, "riftTierOpen"), "click on dropdown row over start button selects only and cannot spend a key");
        OpenTierList(p);
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(565, 30) };
        Draw(p, "×"); Check((bool)Get(p, "open") && !(bool)Get(p, "riftTierOpen"), "close button first dismisses overlaid dropdown without closing workbench");
        OpenTierList(p);
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(510, 395) };
        Draw(p, "刷新"); Check(c.Sent.Count == sent && !(bool)Get(p, "riftTierOpen"), "refresh click dismisses popup without sending an underlying refresh");
        ArtworkSkin.respectEventUse = false; ArtworkSkin.click = null;
        OpenTierList(p); Set(p, "generation", -99L); Call(p, "Update");
        Check(!(bool)Get(p, "open") && !(bool)Get(p, "riftTierOpen"), "session generation replacement closes both workbench and popup");
        Set(p, "open", true);
        Rift(c, "", "closed", keys: 1, extra: ",\"unlockedTier\":7,\"maxTier\":150");
        UnityEngine.Event.current = new UnityEngine.Event(); Draw(p);
        Check((int)Get(p, "tier") == 7 && UnityEngine.GUI.labels.Exists(t => t.Contains("已开放至第 7 阶")), "authoritative lower unlock bound clamps restored selection");
        OpenTierList(p); Set(p, "riftTierScroll", 100); UnityEngine.GUI.labels.Clear(); DropdownEvent(p, UnityEngine.EventType.Repaint);
        Check((int)Get(p, "riftTierScroll") == 0 && !UnityEngine.GUI.labels.Exists(t => t == "第 8 阶"), "locked tiers are not offered as dropdown options");
        Rift(c, "", "closed", keys: 1, extra: ",\"unlockedTier\":150,\"maxTier\":150");
        Set(p, "tier", 999); UnityEngine.Event.current = new UnityEngine.Event(); Draw(p);
        Check((int)Get(p, "tier") == 150, "invalid restored selection cannot expose tier 151");
        Call(p, "ResetRiftTierDropdown"); OpenTierList(p);
        c.Endgame.Begin(new EndgameRequest("rift.start", c.Endgame.Revision, tier: 150)); DropdownEvent(p, UnityEngine.EventType.Repaint);
        Check(!(bool)Get(p, "riftTierOpen"), "pending mutation immediately closes and disables selector"); Receipt(c);
        Rift(c, "", "closed", keys: 1); Set(p, "tier", 150); UnityEngine.Event.current = new UnityEngine.Event(); Draw(p);
        Check((int)Get(p, "tier") == 1, "missing unlock field falls back to tier one rather than unlocking 150");
        Rift(c, "finished", "completed", extra: ",\"remainingSeconds\":360,\"unlockedTier\":4,\"maxTier\":150");
        typeof(EndgameClientState).GetField("<RiftReceivedUtc>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(c.Endgame, DateTime.UtcNow.AddSeconds(-120));
        Draw(p);
        Check(UnityEngine.GUI.labels.Exists(t => t.Contains("通关余时 06:00") && t.Contains("第 4 阶")), "completed summary displays frozen server time and authoritative unlock without predicting extra levels");
        var bg = new UnityEngine.Texture2D(); var back = new UnityEngine.Texture2D(); var fill = new UnityEngine.Texture2D();
        ArtworkSkin.frameTextures["res/uires/common/msgboxbk.gsa"] = new[] { bg };
        ArtworkSkin.frameTextures["res/uires/gameui/intonatebarbk.gsa"] = new[] { back };
        ArtworkSkin.frameTextures["res/uires/gameui/intonatebar.gsa"] = new[] { fill };
        UnityEngine.GUI.uvTextures.Clear(); UnityEngine.GUI.uvAlphaBlends.Clear();
        typeof(EndgamePanel).GetMethod("DrawRiftWindowArtwork", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { new UnityEngine.Rect(0, 0, 940, 660), 1f, false });
        Check(UnityEngine.GUI.uvTextures.Count == 10 && UnityEngine.GUI.uvTextures[1].Item1.width == 44 && UnityEngine.GUI.uvTextures[1].Item1.height == 44, "original background expands with intact native 44px corners");
        Check(!UnityEngine.GUI.uvAlphaBlends[0] && UnityEngine.GUI.uvAlphaBlends[1] && ReferenceEquals(UnityEngine.GUI.uvTextures[0].Item2, bg), "opaque backing reuses original background pixels, decorative corners retain their alpha");
        UnityEngine.GUI.uvTextures.Clear();
        typeof(EndgamePanel).GetMethod("DrawRiftWindowArtwork", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { new UnityEngine.Rect(45, 345, 270, 252), 1f, true });
        Check(UnityEngine.GUI.uvTextures[0].Item1.width == 270 && UnityEngine.GUI.uvTextures[0].Item1.height == 252, "dropdown backing covers its entire pointer area so covered text cannot bleed through border margins");
        UnityEngine.GUI.uvTextures.Clear();
        var progress = typeof(EndgamePanel).GetMethod("DrawProgress", BindingFlags.NonPublic | BindingFlags.Instance);
        progress.Invoke(p, new object[] { new UnityEngine.Rect(0, 0, 800, 34), 50, 100 });
        var clip = UnityEngine.GUI.uvTextures[0];
        Check(ReferenceEquals(clip.Item2, fill) && clip.Item1.width == 58 && clip.Item1.height == 10 && clip.Item3.width == .5f, "selected original green fill is half-clipped at 50 percent in its native gold frame");
        UnityEngine.GUI.uvTextures.Clear(); progress.Invoke(p, new object[] { new UnityEngine.Rect(0, 0, 800, 34), -1, 100 });
        Check(UnityEngine.GUI.uvTextures.Count == 0, "negative progress draws no fill");
        progress.Invoke(p, new object[] { new UnityEngine.Rect(0, 0, 800, 34), 200, 100 });
        Check(UnityEngine.GUI.uvTextures[0].Item1.width == 116, "overflow progress remains inside authored bar");
        ArtworkSkin.frameTextures.Clear(); UnityEngine.Event.current = new UnityEngine.Event();
    }
    static void MultiFloorContracts(Client c, EndgamePanel p)
    {
        OpenEntrance(c); Rift(c, "", "closed", keys: 1);
        RiftDropdownContracts(c, p); OpenEntrance(c);
        Rift(c, "", "closed", keys: 1, extra: ",\"unlockedTier\":150,\"maxTier\":150");
        Set(p, "tier", 150); Draw(p);
        Check((int)Get(p, "tier") == 150 && UnityEngine.GUI.labels.Exists(t => t.Contains("阶")) && !ArtworkSkin.enableds.ContainsKey("+"), "dropdown selection reaches150 without stepper controls");
        Draw(p, "消耗一钥 · 开启洞天"); Check(c.Last.Json.Contains("\"tier\":150") && !c.Last.Json.Contains("floorIndex"), "fresh paid entry chooses difficulty independently of run floor"); Receipt(c);
        QuietNegotiate(c); RiftContext(c, true);
        string floor = ",\"floorIndex\":3,\"seed\":\"18446744073709551615\",\"generatorVersion\":1,\"mapName\":\"秘林\",\"canAdvance\":true,\"exitX\":700,\"exitY\":850";
        int notices = NewbieTipBox.notices.Count;
        Rift(c, "multifloor", "active", extra: floor); Call(p, "Update"); Draw(p);
        Check(!(bool)Get(p, "open") && !UnityEngine.GUI.labels.Exists(t => t.Contains("走入本层出口传送门")), "active floor retains no modal objective");
        var pending = new EndgameRequest("rift.advance", c.Endgame.Revision, runID: "multifloor", floorIndex: 3);
        Check(c.SendEndgame(pending), "physical gate may stage authoritative floor request");
        int before = c.Sent.Count; Draw(p, "前往下一层 · 跳过余敌");
        Check(c.Sent.Count == before && ReferenceEquals(c.Endgame.PendingRequest, pending) && !ArtworkSkin.enableds.ContainsKey("前往下一层 · 跳过余敌"), "quiet HUD neither duplicates gate request nor adds modal floor controls"); Receipt(c);
        Rift(c, "multifloor", "active", extra: floor.Replace("\"floorIndex\":3", "\"floorIndex\":4")); Call(p, "Update");
        Check(NewbieTipBox.notices.Count == notices && c.Endgame.Rift.Tier == 2, "floor change silently preserves difficulty without restoring exit controls");
        Rift(c, "multifloor", "active", extra: floor + ",\"transitioning\":true"); Call(p, "Update"); Draw(p);
        Check(!(bool)Get(p, "open") && !UnityEngine.GUI.labels.Exists(t => t.Contains("正在前往下一层")), "floor transition never revives progress modal");
        Rift(c, "multifloor", "boss", extra: floor); Call(p, "Update"); Draw(p);
        Check(NewbieTipBox.notices.Count == notices && !ArtworkSkin.enableds.ContainsKey("查看洞天"), "boss transition has no phase popup or workbench button");
        RiftContext(c, false);
    }
    static void NativeRiftNoticeContracts()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); QuietNegotiate(c); RiftContext(c, true);
        int notices = NewbieTipBox.notices.Count;
        Rift(c, "notice-run", "active", extra: ",\"floorIndex\":1"); Call(p, "Update");
        foreach (string phase in new[] { "reserved", "active", "boss", "reward_pending" })
        {
            Rift(c, "notice-run", phase, extra: ",\"floorIndex\":2"); Call(p, "Update");
            Check(NewbieTipBox.notices.Count == notices, "non-completion phase stays quiet: " + phase);
        }
        Rift(c, "notice-run", "completed", true); Call(p, "Update");
        Check(NewbieTipBox.notices.Count == notices + 1 && NewbieTipBox.notices[notices] == "拾妥所得，可与归元道使交谈离境。", "live completion emits only the exact approved native sentence");
        foreach (string phase in new[] { "completed", "closed", "failed", "aborted", "interrupted", "reward_pending", "completed" })
        {
            Rift(c, "notice-run", phase); Call(p, "Update"); Draw(p); Draw(p);
            Check(NewbieTipBox.notices.Count == notices + 1, "duplicates and terminal transitions never add or replay notices: " + phase);
        }
        c.EndgameInWorld = false; Call(p, "Update"); Rift(c, "offline-run", "completed", true); c.EndgameInWorld = true; Call(p, "Update");
        Check(NewbieTipBox.notices.Count == notices + 1, "restoring a completion missed offline remains silent");
        p.Unbind(c); p.Bind(c); Call(p, "Update");
        Check(NewbieTipBox.notices.Count == notices + 1, "rebinding never replays completion");
        c.Endgame.Reset(); QuietNegotiate(c); RiftContext(c, true); Rift(c, "notice-run", "completed", true); Call(p, "Update");
        Rift(c, "notice-run", "boss"); Call(p, "Update"); Rift(c, "notice-run", "completed", true); Call(p, "Update");
        Check(NewbieTipBox.notices.Count == notices + 1, "reconnect baseline consumes restored completion even if older phases replay");
        RiftContext(c, false); Rift(c, "outside-run", "boss"); Call(p, "Update"); Rift(c, "outside-run", "completed", true); Call(p, "Update");
        RiftContext(c, true); Call(p, "Update");
        Check(NewbieTipBox.notices.Count == notices + 1, "completion observed outside does not notify on reentry");
        Rift(c, "fresh-run", "boss"); Call(p, "Update"); Rift(c, "fresh-run", "completed", true); Call(p, "Update");
        Check(NewbieTipBox.notices.Count == notices + 2 && NewbieTipBox.notices[notices + 1] == "拾妥所得，可与归元道使交谈离境。", "a genuinely new run may emit its own one-time completion prompt");
        Rift(c, "", "closed"); Call(p, "Update"); Check((string)Get(p, "observedRun") == "", "empty run clears observation baseline");
        p.Unbind(c);
    }
    static EndgameItemView TooltipItem(string fields) => EndgameResponse.Parse("{\"kind\":\"snapshot\",\"ok\":true,\"items\":[{" + fields + "}]}").Items[0];
    static int Occurrences(string text, string value) => (text.Length - text.Replace(value, "").Length) / value.Length;
    static void TooltipContracts(Client c, EndgamePanel p)
    {
        c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
        Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"tooltip-native-ready\",\"chunks\":1,\"items\":[]}");
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
        var inactive = TooltipItem("\"itemId\":1002,\"formationId\":1,\"location\":\"equipped\",\"tab\":2,\"slot\":25,\"inactiveReason\":\"职业、等级、耐久或同类宿阵优先级未满足\"");
        Check(EndgamePanel.FormationStatus(inactive) == "", "generic server inactivity does not prove duplicate suppression");
        var suppressed = TooltipItem("\"itemId\":1002,\"formationId\":1,\"location\":\"equipped\",\"tab\":2,\"slot\":25,\"inactiveReason\":\"同类宿阵优先级未满足\"");
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
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"authored-native-ready\",\"chunks\":1,\"items\":[]}");
        var item = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"location\":\"bag\",\"slot\":0,\"name\":\"炉火旧剑\",\"description\":\"旧炉之火。\\n主人已去。\",\"trait\":\"普通攻击伤害提高10%\"");
        string native = "\u0007旧剑\n\u0007(武器)\n\n仙遗·炉火旧剑\n基底：旧剑\n力量 +12\n普通攻击伤害提高10%\n旧炉之火。\n主人已去。\n\n耐久度: 50/50\n攻击 20-30\n孔数 0/2\n（绑定物品不可交易）";
        string tip = EndgamePanel.OverlayTooltip(item, native);
        Check(Occurrences(tip, "旧炉之火。") == 1 && Occurrences(tip, "主人已去。") == 1 && !tip.Contains("\u00035旧炉之火。"), "multiline authored lore remains once in description rather than attributes");
        Check(Occurrences(tip, "普通攻击伤害提高10%") == 1 && tip.IndexOf("孔数") < tip.IndexOf("旧炉之火。") && tip.Contains("\u0006普通攻击伤害提高10%\n") && tip.IndexOf("\u0006") < tip.IndexOf("孔数"), "native unique effect is not repeated by existing owned Trait field");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"effects\",\"chunks\":1,\"items\":[],\"formations\":[{\"id\":1,\"name\":\"枪火宿阵\",\"description\":\"军中旧旗。\",\"stats\":[{\"attr\":1,\"value\":15}]}]}");
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1001, slot = 0 });
        foreach (string state in new[] { "\"location\":\"bag\"", "\"location\":\"equipped\",\"active\":true", "\"location\":\"equipped\",\"inactiveReason\":\"同类宿阵优先级未满足\"", "\"location\":\"equipped\",\"inactiveReason\":\"职业、等级或耐久不符合要求\"" })
        {
            item = TooltipItem("\"itemId\":1001,\"formationId\":1,\"slot\":0,\"name\":\"旧枪\",\"trait\":\"连环枪法伤害提高12%\"," + state);
            tip = EndgamePanel.OverlayTooltip(item, "\u0007旧枪\n需要等级: 50\n攻击 20-30\n孔数 2/2");
            Check(Occurrences(tip, "连环枪法伤害提高12%") == 1 && tip.Contains("\u00035力量 +15\n\u0006连环枪法伤害提高12%\n") && tip.IndexOf("\u0006") < tip.IndexOf("军中旧旗。") && Occurrences(tip, "军中旧旗。") == 1, "ordinary formation potential effect remains once at the attribute-section end for " + state);
            Check(tip.Contains("不生效") == state.Contains("同类宿阵优先级"), "only authoritative duplicate suppression claims the inactive formation status");
        }
        EquipDlg.Equipped.RemoveAll(row => row.id == 1001 && row.slot == 0);
        var old = EndgameResponse.Parse("{\"kind\":\"snapshot\",\"ok\":true,\"items\":[{\"itemId\":1001,\"formationId\":1}]}").Items[0];
        Check(old.Trait == "", "old payload without optional owned Trait stays accepted");
        c.Endgame.Begin(new EndgameRequest("catalog", catalog: "runes"));
        Apply(c, "{\"kind\":\"catalog.runes\",\"ok\":true,\"runes\":[{\"itemId\":1900028028,\"name\":\"星篆·轸水蚓\",\"description\":\"有效穿戴时，每2秒按生命恢复数值恢复生命。\",\"mansion\":\"轸\",\"quadrant\":\"朱雀\",\"minLevel\":60,\"stats\":[{\"attr\":38,\"value\":1,\"mode\":0}]}]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1900028028, bagIdx = 0 };
        var rune = TooltipItem("\"itemId\":1900028028,\"location\":\"bag\",\"slot\":0,\"name\":\"星篆·轸水蚓\"");
        tip = EndgamePanel.OverlayTooltip(rune, "\u0007星篆·轸水蚓\n有效穿戴时每2秒恢复7点生命");
        Check(Occurrences(tip, "每2秒恢复7点生命") == 1 && !tip.Contains("恢复1点生命") && !tip.Contains("生命恢复 +1"), "rune28 actual stored recovery amount suppresses catalog fallback");
        tip = EndgamePanel.OverlayTooltip(rune, "\u0007星篆·轸水蚓");
        Check(Occurrences(tip, "生命恢复 +1") == 1 && tip.Contains("每2秒按生命恢复数值恢复生命"), "rune catalog fallback uses generic values and authoritative cadence description");
        Check(EndgamePanel.RuneDetails(1900028028, false).Contains("每2秒按生命恢复数值恢复生命"), "installed rune details preserve server-owned eligibility and cadence");
        c.Endgame.Begin(new EndgameRequest("catalog", catalog: "runes"));
        Apply(c, "{\"kind\":\"catalog.runes\",\"ok\":true,\"runes\":[{\"itemId\":1900028001,\"name\":\"配置恢复星篆\",\"description\":\"有效穿戴时，每1.5秒按生命恢复数值恢复生命。\",\"stats\":[{\"attr\":38,\"value\":3,\"mode\":0}]}]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1900028001, bagIdx = 0 };
        rune = TooltipItem("\"itemId\":1900028001,\"location\":\"bag\",\"slot\":0,\"name\":\"配置恢复星篆\"");
        tip = EndgamePanel.OverlayTooltip(rune, "\u0007配置恢复星篆");
        Check(tip.Contains("生命恢复 +3") && tip.Contains("每1.5秒") && !tip.Contains("每2秒"), "configured alternate rune identity uses authoritative cadence without item-specific inference");
        tip = EndgamePanel.OverlayTooltip(rune, "\u0007配置恢复星篆\n有效穿戴时每1.5秒恢复7点生命");
        Check(tip.Contains("每1.5秒恢复7点生命") && !tip.Contains("生命恢复 +3"), "alternate cadence still preserves actual owned recovery amount");
        Check(EndgamePanel.RuneDetails(1900028001, false).Contains("每1.5秒"), "alternate rune details share authoritative catalog description");
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
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"lore-paths\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"tab\":2,\"slot\":25," + fields + "}],\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\",\"description\":\"宿阵旧事。\\n星河长明。\"}]}");
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
            Check(!bagTip.Contains("旧底材的来历") && bagTip.IndexOf(footer) > bagTip.LastIndexOf("\u0006"), "authored overlay keeps binding footer after the attribute effect block without reintroducing template lore");
        }
        c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"ordinary-lore\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0}]}");
        string ordinary = "\u0007普通剑\n" + nativeBody.Replace(footer, "旧底材的来历。\n另一段原装叙述。\n" + footer);
        Check(EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], ordinary) == ordinary, "ordinary multiline base prose remains byte-for-byte unchanged through the bag path");
    }
    static void SocketRecipeContracts()
    {
        // Random capacity and probability remain authoritative server text.
        for (int capacity = 1; capacity <= 5; capacity++)
        {
            var c = new Client(); var p = new EndgamePanel(); p.Bind(c);
            c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.cube-inputs.v1\"]}");
            string gear = "{\"uid\":\"9007199254740901\",\"itemId\":2001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"无孔装备\",\"socketCount\":0}";
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"socket-base\",\"chunks\":1,\"revision\":\"1\",\"items\":[" + gear + "],\"materials\":[{\"itemId\":1900028201,\"name\":\"星髓\",\"count\":8}]}");
            InventoryDlg.rows[0] = new InventoryDlg.Item { id = 2001, bagIdx = 0 }; GameBootstrap.CurMapId = 506;
            Check(EndgamePanel.TryOpenCubeFromNpc(506, 77, "Nzhoutianding", () => true), "dedicated cube NPC opens container");
            Draw(p);
            var put = typeof(EndgamePanel).GetMethod("PutCubeItem", BindingFlags.NonPublic | BindingFlags.Instance);
            put.Invoke(p, new object[] { "9007199254740901", 2001, "无孔装备" });
            put.Invoke(p, new object[] { "", 1900028201, "星髓" });
            Set(p, "cubeQuantity", "4"); Draw(p, "确认数量"); Call(p, "SubmitCube"); var request = c.Last;
            Check(request.Op == "cube.quote" && request.Json.Contains("cubeInputs") && !request.Json.Contains("recipeId") && request.Json.Contains("9007199254740901"), "container sends only selected identities and quantities");
            int sent = c.Sent.Count; Call(p, "SubmitCube"); Check(c.Sent.Count == sent && !SysMsgBox.Visible, "pending match cannot repeat or execute");
            string preview = "随机开出1至" + capacity + "孔，每种孔数概率1/" + capacity + "；已有孔不可重开。";
            Apply(c, "{\"kind\":\"cube.quote\",\"ok\":true,\"requestId\":\"" + request.RequestID + "\",\"quote\":{\"quoteId\":\"socket-quote\",\"recipeId\":0,\"revision\":\"1\",\"expiresAt\":\"2099-01-01T00:00:00Z\",\"description\":\"" + preview + "\",\"money\":\"5000\"}}");
            Draw(p); Check(SysMsgBox.Visible && SysMsgBox.text.StartsWith(preview) && SysMsgBox.text.Contains("铜币：5000"), "matched risk and fee remain server-authored");
            Check(c.Endgame.Items[0].SocketCount == 0 && c.Endgame.Materials[0].Count == 8, "container and match never spend inventory");
            SysMsgBox.Finish(true); var execute = c.Last;
            Check(execute.Op == "cube.execute" && execute.Json.Contains("socket-quote") && execute.Json.Contains("npcEntityId") && !execute.Json.Contains("cubeInputs") && !execute.Json.Contains("recipeId"), "execute binds quote and live workbench only");
            sent = c.Sent.Count; Call(p, "SubmitCube"); Check(c.Sent.Count == sent, "double click cannot duplicate or reroll");
            Call(p, "Close"); Check(ReferenceEquals(c.Endgame.PendingRequest, execute), "close retains uncertain request identity"); Receipt(c);
        }
    }
    static void NativeStatTooltipContracts(Client c, EndgamePanel p)
    {
        const string native = "\u0007(武器)\n耐久度: 50/50\n需要等级: 60\n需要职业: 剑客\n攻击 200-300\n防御 60\n魔法攻击 50\n重量 5\n\u00035力量 +18\n\u00035攻击 +36%\n\u00035移动速度 -3%\n\u00035本件基础攻击强化 +180%\n\u00035连环枪法伤害提高12%\n\u00035孔数 2/2\n法宝技能: 原生技能\n（绑定物品不可交易）";
        foreach (bool nativeStats in new[] { false, true })
        {
            string description = nativeStats ? native.Replace("\n攻击 200-300\n", "\n\u00035攻击 200-300\n") : native;
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            foreach (string activation in new[] { "\"active\":true", "\"inactiveReason\":\"同类宿阵优先级未满足\"", "\"inactiveReason\":\"职业、等级或耐久不符合要求\"" })
            {
                string fields = "\"name\":\"普通剑\",\"socketCount\":2,\"formationId\":1,\"level\":60,\"trait\":\"本件基础攻击强化 +180%；连环枪法伤害提高12%\",\"sockets\":[{\"index\":1,\"itemId\":1900028002,\"name\":\"星篆·亢金龙\"},{\"index\":0,\"itemId\":1900028001,\"name\":\"星篆·角木蛟\"}]";
                Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"native-stats-" + activation.Length + "\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"tab\":2,\"slot\":25," + fields + "," + activation + "}],\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\",\"description\":\"故旗尚温。\",\"stats\":[{\"attr\":1,\"value\":15},{\"attr\":47,\"value\":30,\"mode\":1}]}]}");
                var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "普通剑+7", desc = description };
                var equipped = new EquipDlg.EquippedItem { id = 1002, slot = 25, quality = 4, name = "普通剑+7", desc = description };
                InventoryDlg.rows[0] = bag; EquipDlg.Equipped.RemoveAll(row => row.slot == 25); EquipDlg.Equipped.Add(equipped);
                var ownedTooltip = typeof(EndgamePanel).GetMethod("OwnedItemTooltip", BindingFlags.Static | BindingFlags.NonPublic);
                string original = TooltipContent.Fmt(TooltipContent.QualityName(bag.name, bag.quality), description);
                string bagTip = EndgamePanel.BagTooltip(2, bag, original);
                string equippedTip = EndgamePanel.EquippedTooltip(equipped, original);
                foreach (string tip in new[] { bagTip, equippedTip })
                {
                    Check(tip.Contains("力量 +15") != nativeStats && tip.Contains("攻击 +30%") != nativeStats, "negotiated native extras suppress catalog duplication; old servers retain fallback");
                    Check(Occurrences(tip, "力量 +18") == 1 && Occurrences(tip, "攻击 +36%") == 1 && Occurrences(tip, "移动速度 -3%") == 1, "server-merged same-scope flat percentage and negative values display once");
                    Check(Occurrences(tip, "攻击强化 +180%") == 1 && !tip.Contains("本件") && Occurrences(tip, "连环枪法伤害提高12%") == 1, "local enhancement and real effect lines survive once without merging into global percent");
                    Check(tip.Contains("\n" + (nativeStats ? "\u00035" : "") + "攻击 200-300\n防御 60\n魔法攻击 50\n重量 5\n") && tip.Contains("\u00035孔数 2/2"), "server-confirmed local attack is dark gold; unrelated bases and unmarked legacy rows retain their colors");
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
    static void GeneratedEnhancedBaseTooltipContracts(Client c, EndgamePanel p)
    {
        string path = Environment.GetEnvironmentVariable("FFO_ENHANCED_TOOLTIP_FIXTURES");
        Check(!string.IsNullOrEmpty(path) && File.Exists(path), "server-generated local-base color fixtures exist");
        var fixtures = (IReadOnlyList<object>)BoundedJson.Parse(File.ReadAllText(path));
        Check(fixtures.Count >= 10, "fixtures cover unique and formation local scopes, no-local and ordinary equipment");
        foreach (IReadOnlyDictionary<string, object> fixture in fixtures)
        {
            string name = (string)fixture["name"], native = (string)fixture["tooltip"];
            var rows = (IReadOnlyList<object>)fixture["intrinsic"];
            string bagTip = null;
            foreach (string location in new[] { "bag", "equipped" })
            {
                c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
                Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}");
                var itemData = new Dictionary<string, object>((IDictionary<string, object>)fixture["item"]);
                itemData["location"] = location; itemData["slot"] = location == "bag" ? 0 : 25;
                var snapshot = new Dictionary<string, object> {
                    { "kind", "snapshot" }, { "ok", true }, { "snapshotId", name + location }, { "chunks", 1 },
                    { "items", new object[] { itemData } }
                };
                if (fixture.TryGetValue("formation", out object formation)) snapshot["formations"] = new object[] { formation };
                Apply(c, BoundedJson.Serialize(snapshot));
                Check(c.Endgame.Items.Count == 1, "generated item snapshot loads: " + name);
                var item = c.Endgame.Items[0];
                string original = TooltipContent.Fmt(TooltipContent.QualityName(item.Name, 4), native);
                string tip;
                if (location == "bag")
                {
                    var bag = new InventoryDlg.Item { id = item.ItemID, bagIdx = 0, quality = 4, name = item.Name, desc = native };
                    InventoryDlg.rows[0] = bag;
                    tip = EndgamePanel.BagTooltip(2, bag, original); bagTip = tip;
                }
                else
                {
                    var equipped = new EquipDlg.EquippedItem { id = item.ItemID, slot = 25, quality = 4, name = item.Name, desc = native };
                    EquipDlg.Equipped.RemoveAll(row => row.slot == 25); EquipDlg.Equipped.Add(equipped);
                    tip = EndgamePanel.EquippedTooltip(equipped, original);
                    Check(tip == bagTip, "same server-generated local colors through bag and equipment entrances: " + name);
                }
                int prior = -1;
                foreach (string row in rows)
                {
                    int index = ("\n" + tip + "\n").IndexOf("\n" + row + "\n", StringComparison.Ordinal);
                    Check(index > prior && Occurrences("\n" + tip + "\n", "\n" + row + "\n") == 1,
                        "server-generated intrinsic value, exact color, order and uniqueness: " + name + "/" + row);
                    prior = index;
                }
                Check(EndgamePanel.OverlayTooltip(item, tip) == tip, "repeated overlay preserves server-derived local colors: " + name);
                var owned = typeof(EndgamePanel).GetMethod("OwnedItemTooltip", BindingFlags.Static | BindingFlags.NonPublic);
                Check((string)owned.Invoke(null, new object[] { item }) == tip, "workbench shares server-generated local colors: " + name);
            }
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
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"equipment-presentation\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"tab\":2,\"slot\":25," + fields + "}],\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\",\"description\":\"星河长明。\",\"stats\":[{\"attr\":1,\"value\":77}]}]}");
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
                Check(tip.Contains(finalEffects + "\n") && tip.IndexOf("\u0006") < tip.IndexOf("孔数") && tip.IndexOf("\u0006") < tip.IndexOf(footer), "native and legacy effects keep their order at the attribute-block end before sockets binding and lore: " + tip.Replace("\u0003", "[quality]").Replace("\u0006", "[orange]"));
                Check(tip.Contains("技能效果: 伤害提高5%") && tip.Contains("\n旧事记载着伤害提高。\n"), "functional prose and authored lore are not classified as effect attributes");
                Check(tip.Contains("力量 +99") == (!nativeStats && (identity & 1) != 0) && tip.Contains("力量 +77") == (!nativeStats && (identity & 2) != 0), "negotiated native stats suppress unique/formation reference numbers");
                Check(tip.Contains("\u0002不生效") == (item.Location == "equipped" && (identity & 2) != 0), "inactive formation status remains gray and authoritative");
                Check(EndgamePanel.OverlayTooltip(item, tip) == tip, "repeated overlay preserves native/legacy values colors attribute-effect order and composition");
                Check((string)ownedTooltip.Invoke(null, new object[] { item }) == tip, "workbench uses exact bag/equipped/comparison presentation");
                string title = (identity & 2) != 0 ? "照夜" : "旧器";
                Check(tip.StartsWith("\u0007\u00035" + title + "+7\n\u0007(武器)\n") && Occurrences(tip, "角亢") == ((identity & 2) != 0 ? 1 : 0), "name refinement slot and runeword composition stay compact");
            }
            string description = EndgamePanel.ItemDescription(c.Endgame.Items[0], true);
            Check(description.Contains("\u00035魔法装备获取几率提升 +15%") && description.Contains("\u00035攻击强化 +180%") && description.Contains("\u0006普通攻击伤害提高10%\n\n旧事记载着伤害提高。"), "catalog-style fallback also closes mixed attributes with orange effects before lore");
        }
        var legacyUnique = TooltipItem("\"itemId\":1001,\"location\":\"bag\",\"slot\":0,\"uniqueId\":1,\"name\":\"旧器\"");
        string legacyBlock = "\u0007旧器\n\u0007(武器)\n仙遗·旧器\n基底：旧剑\n\u00035未来属性 余韵一重\n\u0006攻击 +999\n\u00034染色率 +15%\n\n攻击 20-30\n孔数 0/2\n" + footer;
        string blockTip = EndgamePanel.OverlayTooltip(legacyUnique, legacyBlock);
        Check(blockTip.Contains("\u00035未来属性 余韵一重") && blockTip.Contains("\u00035魔法装备获取几率提升 +15%") && blockTip.Contains("\u00035魔法装备获取几率提升 +15%\n\u0006攻击 +999\n") && blockTip.IndexOf("\u0006") < blockTip.IndexOf("孔数"), "legacy unique header extraction respects authoritative gold/orange markers before text heuristics");
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
                Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"formation-level-" + required + color.Length + "\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"tab\":2,\"slot\":25," + fields + "}],\"formations\":[{\"id\":6,\"name\":\"担山不语\",\"minLevel\":14}]}");
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
    static void ItemizationStatTextContracts(Client c, EndgamePanel p)
    {
        var format = typeof(EndgamePanel).GetMethod("StatText", BindingFlags.NonPublic | BindingFlags.Static);
        foreach (var row in new[] {
            new { Value = -20, Mode = 1, Text = "攻击速度 +20%" },
            new { Value = 15, Mode = 1, Text = "攻击速度 -15%" },
            new { Value = 0, Mode = 1, Text = "攻击速度 +0%" },
            new { Value = int.MinValue, Mode = 1, Text = "攻击速度 +2147483648%" },
            new { Value = int.MaxValue, Mode = 1, Text = "攻击速度 -2147483647%" },
            new { Value = -200, Mode = 0, Text = "攻击间隔 -200毫秒" },
            new { Value = 150, Mode = 0, Text = "攻击间隔 +150毫秒" },
            new { Value = 0, Mode = 0, Text = "攻击间隔 +0毫秒" }
        })
        {
            var stat = new EndgameStatView(new Dictionary<string, object> { { "attr", 25L }, { "mode", (long)row.Mode }, { "value", (long)row.Value } });
            for (int repeat = 0; repeat < 2; repeat++)
                Check((string)format.Invoke(null, new object[] { stat }) == row.Text && stat.Value == row.Value,
                    "attack-speed sign/unit does not mutate raw data: " + row.Value + "/" + row.Mode);
        }
        foreach (int attr in new[] { 27, 47 })
        {
            var stat = new EndgameStatView(new Dictionary<string, object> { { "attr", (long)attr }, { "mode", 1L }, { "value", -20L } });
            Check(((string)format.Invoke(null, new object[] { stat })).EndsWith(" -20%", StringComparison.Ordinal), "other attribute signs remain unchanged");
        }
        // Native text is already human-facing. Only raw DTO values are inverted;
        // re-opening a native or fallback tooltip must preserve signs and colors.
        foreach (bool nativeStats in new[] { false, true })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"stat-text-native-ready\",\"chunks\":1,\"items\":[]}");
            var item = TooltipItem("\"itemId\":1001,\"uniqueId\":1,\"name\":\"快剑\",\"location\":\"bag\",\"slot\":0,\"stats\":[{\"attr\":25,\"value\":-20,\"mode\":1},{\"attr\":25,\"value\":15,\"mode\":1},{\"attr\":25,\"value\":-200,\"mode\":0}]");
            string original = "\u0007旧剑\n\u0007(武器)\n\u00030攻击 20~30\n" +
                (nativeStats ? "\u00035攻击速度 +20%\n\u00035攻击速度 -15%\n\u00035攻击间隔 -200毫秒\n" : "") + "\u0006命中时击退敌人";
            string tip = EndgamePanel.OverlayTooltip(item, original);
            for (int repeat = 0; repeat < 2; repeat++)
            {
                foreach (string line in new[] { "攻击速度 +20%", "攻击速度 -15%", "攻击间隔 -200毫秒" })
                    Check(Occurrences(tip, "\u00035" + line) == 1 && !tip.Contains("\u0006" + line), "speed rows retain single gold attribute placement: " + line);
                Check(tip.Contains("\u00030攻击 20~30") && tip.Contains("\u0006命中时击退敌人"), "speed display preserves white base and orange effects");
                tip = EndgamePanel.OverlayTooltip(item, tip);
            }
        }
    }

    static void EquippedCategoryTooltipContracts(Client c, EndgamePanel p)
    {
        // Production endgameItem preserves InventoryTab even for worn items. Move
        // one UID through native publications, rather than inventing tab-zero worn data.
        foreach (int category in new[] { 0, 1, 2, 3 })
        foreach (int slot in new[] { 1, 13, 25 })
        foreach (bool nativeStats in new[] { false, true })
        foreach (bool special in new[] { false, true })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            string fields = "\"uid\":\"9007199254740993\",\"itemId\":1001,\"tab\":" + category + ",\"name\":\"照夜\"";
            if (special) fields += ",\"uniqueId\":1,\"formationId\":1,\"socketCount\":2,\"description\":\"星河长明。\",\"sockets\":[{\"index\":0,\"itemId\":1900028001,\"name\":\"星篆·角木蛟\"},{\"index\":1,\"itemId\":1900028002,\"name\":\"星篆·亢金龙\"}]";
            string desc = "\u0007(武器)\n耐久度: 50/50\n\u00030攻击 200-300\n\u00035力量 +18\n\u00035孔数 2/2\n\u0006攻击命中时有5%概率释放3级天雷";
            if (!special) desc += "\n普通装备原有描述。";
            var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "旧剑+7", desc = desc };
            var worn = new EquipDlg.EquippedItem { id = 1001, slot = slot, quality = 4, name = bag.name, desc = desc };
            InventoryDlg.rows.Clear(); InventoryDlg.rows[0] = bag; EquipDlg.Equipped.Clear();
            string original = TooltipContent.Fmt(TooltipContent.QualityName(bag.name, bag.quality), desc);
            string forms = ",\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\"}]";
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"before-equip\",\"chunks\":1,\"items\":[{" + fields + ",\"location\":\"bag\",\"slot\":0}]" + forms + "}");
            string bagTip = EndgamePanel.BagTooltip(category, bag, original);
            string genericBagTip = TooltipContent.Get(category + 1, 0);
            Check(c.Endgame.Find("bag", (category + 1) % 4, 0, 1001) == null, "bag category still prevents unrelated same-slot overlays");
            InventoryDlg.rows.Clear(); EquipDlg.Equipped.Add(worn);
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"after-equip\",\"chunks\":1,\"items\":[{" + fields + ",\"location\":\"equipped\",\"slot\":" + slot + "}]" + forms + "}");
            Check(c.Endgame.Find("equipped", 0, slot, 1001)?.UID == "9007199254740993", "worn identity resolves with retained inventory category " + category);
            Check(c.Endgame.Find("equipped", 0, slot, 1002) == null && c.Endgame.Find("equipped", 0, slot + 1, 1001) == null, "worn lookup still checks slot and item identity");
            string equippedTip = EndgamePanel.EquippedTooltip(worn, original);
            Check(equippedTip == bagTip, "same UID tooltip survives bag-to-equipped transition exactly");
            Check(TooltipContent.Get(5, slot) == genericBagTip, "generic drag tooltip shares bag/equipped overlay");
            var ownedTooltip = typeof(EndgamePanel).GetMethod("OwnedItemTooltip", BindingFlags.Static | BindingFlags.NonPublic);
            Check((string)ownedTooltip.Invoke(null, new object[] { c.Endgame.Items[0] }) == bagTip, "workbench shares resolved worn presentation");
            if (special)
            {
                Check(equippedTip.StartsWith("\u0007\u00035照夜+7\n\u0007(武器)\n\u0007\u00035角亢\n"), "worn name and composition use exact shared layout");
                Check(equippedTip.Contains("\u00030攻击 200-300") && equippedTip.Contains("\u00035力量 +18") && equippedTip.Contains("\u00035孔数 2/2") && equippedTip.Contains("\u00035力量 +18\n\u0006攻击命中时有5%概率释放3级天雷\n\u00035孔数 2/2"), "worn base attributes gold extras sockets and attribute-ending orange effect survive");
                Check(TooltipContent.Get(5, slot) == bagTip, "generic special tooltip uses exact latest bag card");
            }
            else Check(equippedTip == original && TooltipContent.Get(5, slot) == TooltipContent.Fmt(bag.name, desc), "ordinary equipment keeps native and generic tooltip behavior byte-for-byte");
            Check(TooltipContent.Get(5, slot + 1) == null && TooltipContent.Get(1, 0) == null, "missing native entries never show stale special gear");
            c.Endgame.Reset();
            Check(EndgamePanel.EquippedTooltip(worn, original) == original, "disconnect resets special worn overlay");
        }
        Check(TooltipContent.Get(0, 7) == "hotbar:7" && TooltipContent.Get(6, 0) == null, "generic hotbar and unsupported drag routes are unchanged");
    }

    static void AttributeEffectOrderingContracts(Client c, EndgamePanel p)
    {
        const string effect1 = "普通攻击伤害提高10%";
        const string effect2 = "攻击命中时有5%概率释放3级天雷";
        const string effect3 = "赋予2级白虎卫身光环";
        const string footer = "（绑定物品不可交易/摆摊/邮寄, 可卖NPC）";
        foreach (bool nativeStats in new[] { false, true })
        foreach (int identity in new[] { 1, 2, 3 })
        foreach (bool sockets in new[] { false, true })
        {
            c.Endgame.Reset(); p.Bind(c); c.Endgame.Begin(new EndgameRequest("hello"));
            Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (nativeStats ? ",\"endgame.native-stats.v1\"" : "") + "]}");
            string fields = "\"name\":\"照夜\",\"description\":\"器物故事。\",\"trait\":\"魔法攻击强化 +33%；" + effect1 + "；" + effect3 + "\"";
            if ((identity & 1) != 0) fields += ",\"uniqueId\":1";
            if ((identity & 2) != 0) fields += ",\"formationId\":1";
            if (sockets) fields += ",\"socketCount\":2";
            Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"effect-order\",\"chunks\":1,\"items\":[{\"uid\":\"81\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0," + fields + "},{\"uid\":\"82\",\"itemId\":1002,\"location\":\"equipped\",\"tab\":2,\"slot\":25," + fields + "}],\"formations\":[{\"id\":1,\"name\":\"宿阵·照夜\",\"description\":\"星河故事。\"}]}");
            // Actual line protocol with effects previously scattered through the
            // native description, including before ordinary affixes and after lore.
            string metadata = "\n器物故事。\n耐久度: 25/50\n\u0004需要等级: 60\n需要职业: 剑客\n重量 5\n" +
                (sockets ? "\u00035孔数 2/2\n" : "") + "\u00032套装: 旧套装 (1/2)\n\u0002[2件] 防御 +9\n法宝技能: 旧技能\n技能效果: 原生说明。\n" + footer;
            string desc = "\u0007(武器)\n\u00030攻击 200-300\n\u0006" + effect1 + "\n\u00035力量 +18" + metadata + "\n\u0006" + effect2;
            var bag = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "照夜", desc = desc };
            var worn = new EquipDlg.EquippedItem { id = 1002, slot = 25, quality = 4, name = "照夜", desc = desc };
            InventoryDlg.rows.Clear(); InventoryDlg.rows[0] = bag; EquipDlg.Equipped.Clear(); EquipDlg.Equipped.Add(worn);
            string original = TooltipContent.Fmt(TooltipContent.QualityName(bag.name, bag.quality), desc);
            string bagTip = EndgamePanel.BagTooltip(2, bag, original), wornTip = EndgamePanel.EquippedTooltip(worn, original);
            string continuous = "\u00030攻击 200-300\n\u00035力量 +18\n\u00035魔法攻击强化 +33%\n\u0006" + effect1 + "\n\u0006" + effect2 + "\n\u0006" + effect3;
            Check(bagTip == wornTip, "bag and worn use identical attribute-section effect ordering");
            Check(bagTip.Contains(continuous + "\n器物故事。\n耐久度: 25/50\n\u0004需要等级: 60"), "base stats ordinary extras and orange effects are contiguous before story or metadata even when the raw description interleaves them");
            foreach (string row in new[] { effect1, effect2, effect3, "力量 +18", "魔法攻击强化 +33%", "器物故事。", "需要等级: 60", footer })
                Check(Occurrences(bagTip, row) == 1, "attribute-section relocation preserves exactly one row: " + row);
            Check(bagTip.EndsWith(footer) && bagTip.Contains("\u0002[2件] 防御 +9\n法宝技能: 旧技能\n技能效果: 原生说明。"), "native functional blocks and binding stay outside the orange attribute block");
            foreach (var item in c.Endgame.Items)
                Check(EndgamePanel.OverlayTooltip(item, bagTip) == bagTip, "repeated overlay keeps attribute-ending effects and metadata stable");
        }
    }
    static void InventoryIdentityContracts(Client c, EndgamePanel p)
    {
        c.Endgame.Reset(); p.Bind(c); c.EndgameInWorld = true;
        c.Endgame.Begin(new EndgameRequest("hello"));
        Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}");
        Func<string, string, string> item = (uid, name) => "{\"uid\":\"" + uid + "\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"uniqueId\":1,\"name\":\"" + name + "\",\"description\":\"" + name + "旧事。\",\"trait\":\"普通攻击伤害提高10%\"}";
        Action<string, int, int, int, string> snapshot = (id, revision, chunk, chunks, items) => Apply(c,
            "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"" + id + "\",\"revision\":\"" + revision + "\",\"chunk\":" + chunk + ",\"chunks\":" + chunks + ",\"items\":[" + items + "]}");
        const string body = "\u0007(武器)\n\u00030攻击 20-30\n\u00035力量 +8\n\u0006普通攻击伤害提高10%\n孔数 0/2\n（绑定物品不可交易）";
        var native = new InventoryDlg.Item { id = 1001, bagIdx = 0, quality = 4, name = "新器+7", desc = body };
        InventoryDlg.rows[0] = native;
        string original = TooltipContent.Fmt(TooltipContent.QualityName(native.name, native.quality), body);
        snapshot("identity-old", 1, 0, 1, item("81", "旧器"));
        Check(EndgamePanel.BagTooltip(2, native, original).Contains("旧器旧事。"), "initial native/sidecar pair supplies the owned lore");
        var historical = c.Endgame.Items[0];
        c.Endgame.NoteNativeInventoryPublished();
        Check(c.Endgame.HasSnapshot && !c.Endgame.InventorySnapshotCurrent && c.Endgame.Find("bag", 2, 0, 1001) == null,
            "same-template native replacement immediately fences old UID identity without erasing historical observation");
        Check(EndgamePanel.BagTooltip(2, native, original) == original && !EndgamePanel.MatchesNative(historical),
            "native authoritative stats and effects stay byte-identical while old name lore and workbench UID are withheld");
        snapshot("identity-new", 2, 0, 2, item("82", "新器"));
        Check(c.Endgame.SnapshotLoading && EndgamePanel.BagTooltip(2, native, original) == original, "partial replacement cannot overlay its incomplete identity");
        snapshot("delayed-old-revision", 1, 0, 1, item("81", "旧器"));
        Check(c.Endgame.SnapshotLoading && !c.Endgame.InventorySnapshotCurrent, "older full snapshot cannot displace a newer assembling revision");
        snapshot("identity-new", 2, 1, 2, "");
        string current = EndgamePanel.BagTooltip(2, native, original);
        Check(c.Endgame.InventorySnapshotCurrent && c.Endgame.Find("bag", 2, 0, 1001).UID == "82" && current.Contains("新器旧事。") && !current.Contains("旧器旧事。"), "only complete replacement publishes the new same-template UID and lore");
        Check(current.Contains("\u00030攻击 20-30") && Occurrences(current, "力量 +8") == 1 && Occurrences(current, "普通攻击伤害提高10%") == 1,
            "restored overlay preserves authoritative base color actual stats and effects exactly once");
        snapshot("identity-old", 2, 0, 1, item("81", "旧器"));
        Check(c.Endgame.Find("bag", 2, 0, 1001).UID == "82", "retired snapshot cannot revive an old UID even at an equal revision");
        snapshot("interrupted-old", 3, 0, 2, item("83", "旧器"));
        c.Endgame.NoteNativeInventoryPublished();
        snapshot("interrupted-old", 3, 1, 2, "");
        Check(!c.Endgame.SnapshotLoading && !c.Endgame.InventorySnapshotCurrent, "native publication retires an interrupted snapshot and ignores its delayed tail");
        snapshot("newer-first", 4, 0, 2, item("84", "新器"));
        snapshot("interrupted-old", 3, 1, 2, "");
        Check(c.Endgame.SnapshotLoading, "retired tail cannot discard a newer valid assembly");
        snapshot("newer-first", 4, 1, 2, "");
        Check(c.Endgame.Find("bag", 2, 0, 1001).UID == "84", "newer assembly survives old/new revision interleaving");
        Apply(c, "{\"kind\":\"socket.insert\",\"ok\":true,\"revision\":\"5\"}");
        Check(!c.Endgame.InventorySnapshotCurrent && EndgamePanel.BagTooltip(2, native, original) == original, "new authoritative revision invalidates old sidecar even before inventory arrives");
        snapshot("revision-five", 5, 0, 1, item("85", "新器"));
        c.EndgameInWorld = false;
        Check(EndgamePanel.BagTooltip(2, native, original) == original, "world departure prevents an overlay before connection teardown completes");
        c.Endgame.Reset(); c.EndgameInWorld = true;
        Check(EndgamePanel.BagTooltip(2, native, original) == original, "disconnect removes all old UID overlay authority");
        c.Endgame.Begin(new EndgameRequest("hello"));
        Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}");
        Check(EndgamePanel.BagTooltip(2, native, original) == original, "reconnect negotiation cannot reuse the prior character snapshot");
        snapshot("identity-old", 1, 0, 1, item("91", "重连器"));
        Check(c.Endgame.InventorySnapshotCurrent && c.Endgame.Find("bag", 2, 0, 1001).UID == "91", "snapshot identity bookkeeping is generation scoped and permits a new session");
        var worn = new EquipDlg.EquippedItem { id = 1001, slot = 25, quality = 4, name = native.name, desc = body };
        EquipDlg.Equipped.Clear(); EquipDlg.Equipped.Add(worn);
        snapshot("equipped-before", 2, 0, 1, item("91", "旧器").Replace("\"location\":\"bag\"", "\"location\":\"equipped\"").Replace("\"slot\":0", "\"slot\":25"));
        c.Endgame.NoteNativeInventoryPublished();
        Check(EndgamePanel.EquippedTooltip(worn, original) == original, "same-base equipped replacement also retains the new native description without old overlay");
        snapshot("equipped-after", 3, 0, 1, item("92", "新器").Replace("\"location\":\"bag\"", "\"location\":\"equipped\"").Replace("\"slot\":0", "\"slot\":25"));
        Check(EndgamePanel.EquippedTooltip(worn, original).Contains("新器旧事。"), "equipped overlay resumes with the complete replacement UID");
        c.Endgame.NoteNativeInventoryPublished(); c.Endgame.Tick(DateTime.UtcNow.AddSeconds(16));
        Check(c.Endgame.Error == "timeout" && c.Endgame.Message.Contains("刷新") && EndgamePanel.EquippedTooltip(worn, original) == original,
            "missing sidecar exposes bounded refresh recovery and never revives old equipment");
    }
    static void Main()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); Draw(p); Check(c.Sent.Count == 0, "unnegotiated UI sends no commands");
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"); Call(p, "Update");
        string items = "{\"uid\":\"9007199254740993\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"飞鹤遗音\",\"uniqueId\":1001,\"uniqueRollBp\":10000,\"socketCount\":2,\"sockets\":[{\"index\":0,\"itemId\":0},{\"index\":1,\"itemId\":0}]}, {\"uid\":\"9007199254740994\",\"itemId\":1900028001,\"location\":\"bag\",\"tab\":1,\"slot\":1,\"name\":\"角木蛟星篆\"}, {\"uid\":\"9007199254740995\",\"itemId\":1002,\"location\":\"equipped\",\"tab\":2,\"slot\":25,\"name\":\"归元佩\"}";
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"ui\",\"chunks\":1,\"revision\":\"1\",\"items\":[" + items + "]}");
        InventoryDlg.rows[0] = new InventoryDlg.Item { id = 1001, bagIdx = 0 }; InventoryDlg.rows[1] = new InventoryDlg.Item { id = 1900028001, bagIdx = 1 };
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1002, slot = 25 });
        Draw(p, "演劫 · F8"); Check(!(bool)Get(p, "open"), "removed toolbar cannot open any module");
        Check(!Enum.IsDefined(typeof(EndgameModule), "Runes") && typeof(EndgamePanel).GetMethod("TryOpenRunesFromWorld", BindingFlags.Static | BindingFlags.NonPublic) == null, "standalone rune module and opener are removed");
        string tooltip = EndgamePanel.BagTooltip(2, InventoryDlg.rows[0], "legacy details"); Check(tooltip.Contains("\u00035飞鹤遗音") && tooltip.Contains("legacy details") && !tooltip.Contains("宿序"), "dark-gold tooltip retains native description without duplicate socket block");
        Check(EndgamePanel.BagTooltip(0, InventoryDlg.rows[0], "legacy") == "legacy", "wrong tab cannot overlay");
        QuietRiftLifecycleContracts();
        QuietRiftRecoveryContracts();
        p.Bind(c); MultiFloorContracts(c, p);
        int notices = NewbieTipBox.notices.Count;
        c.Endgame.Reset(); Call(p, "Update");
        Check(!(bool)Get(p, "open") && (string)Get(p, "selectedUID") == "", "reset clears selection");
        Check(NewbieTipBox.notices.Count == notices && (string)Get(p, "observedRun") == "", "reset clears phase baseline without replaying notices");
        var requirement = EndgameResponse.Parse("{\"kind\":\"catalog.uniques\",\"ok\":true,\"items\":[{\"itemId\":1001,\"uniqueId\":1001,\"profession\":24,\"uniqueRollBp\":10000}]}").Items[0];
        string catalogTip = EndgamePanel.ItemDescription(requirement, true); Check(catalogTip.Contains("药师/术士") && catalogTip.Contains("图鉴基准"), "profession bitmask and catalog baseline labels");
        TooltipContracts(c, p);
        AuthoredDescriptionContracts(c, p);
        SpecialLoreTooltipPathContracts(c, p);
        NativeStatTooltipContracts(c, p);
        GeneratedEnhancedBaseTooltipContracts(c, p);
        EquipmentPresentationContracts(c, p);
        FormationLevelTooltipContracts(c, p);
        SocketRecipeContracts();
        EquippedCategoryTooltipContracts(c, p);
        AttributeEffectOrderingContracts(c, p);
        ItemizationStatTextContracts(c, p);
        InventoryIdentityContracts(c, p);
        NativeRiftNoticeContracts();
        RiftHudProgressContracts();
        GuardianArrivalSchemaContracts();
        GuardianServerWireContracts();
        RiftWorldVisualContracts();
        Console.WriteLine("PASS " + assertions + " real-panel action contracts with Unity/artwork stubs; no rendering or engine launch.");
    }
}
