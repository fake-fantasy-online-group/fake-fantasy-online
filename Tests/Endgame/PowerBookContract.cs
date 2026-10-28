using System;
using System.IO;
using FakeFantasyOnline.Foundation;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

partial class EndgameUiContract
{
    static string PowerRow(string id, bool unlocked, string slots = "\"衣服\",\"戒指\"") => "{\"powerId\":\"" + id + "\",\"name\":\"凝神回路\",\"category\":\"通用\",\"class\":\"术士\",\"description\":\"消耗降低45%［40–55%］。\",\"rollBp\":" + (unlocked ? "9000" : "0") + ",\"minRollBp\":7500,\"maxRollBp\":10000,\"minLevel\":20,\"unlocked\":" + (unlocked ? "true" : "false") + ",\"slots\":[" + slots + "]}";
    static void PowerReply(Client c, string request, string revision, string rows, string next = "") => Apply(c,
        "{\"kind\":\"powerbook.list\",\"ok\":true,\"requestId\":\"" + request + "\",\"revision\":\"" + revision + "\",\"nextCursor\":\"" + next + "\",\"powers\":[" + rows + "]}");
    static void PowerBookServerWireContracts()
    {
        string directory = Environment.GetEnvironmentVariable("FFO_POWER_BOOK_FIXTURES");
        if (string.IsNullOrEmpty(directory)) throw new Exception("Missing actual Go power-codex fixtures.");
        var list = EndgameResponse.Parse(File.ReadAllText(Path.Combine(directory, "list.json")));
        Check(list.Powers.Count > 0, "actual Go complete-catalog first page decodes");
        bool locked = false, owned = false;
        foreach (var power in list.Powers) { locked |= !power.Unlocked; owned |= power.Unlocked; Check(power.Slots.Count > 0, "actual Go catalog carries authoritative slot eligibility"); }
        Check(locked, "actual Go catalog includes unobtained entries");
        var scroll = EndgameResponse.Parse(File.ReadAllText(Path.Combine(directory, "scroll-snapshot.json")));
        Check(scroll.Items.Count == 1 && scroll.Items[0].PowerScroll.RollBP == 8123 && scroll.Items[0].UID == "9007199254740993" && scroll.Items[0].Bound, "actual Go bound scroll preserves exact UID and captured roll");
        foreach (string filename in new[] { "extract-request.json", "imprint-request.json" })
        {
            var fields = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(File.ReadAllText(Path.Combine(directory, filename)));
            string op = (string)fields["op"];
            var request = new EndgameRequest(op, (string)fields["expectedRevision"], targetUID: fields.ContainsKey("targetUid") ? (string)fields["targetUid"] : "", runeUID: fields.ContainsKey("runeUid") ? (string)fields["runeUid"] : "", powerID: fields.ContainsKey("powerId") ? (string)fields["powerId"] : "", requestID: (string)fields["requestId"]);
            var encoded = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(request.Json);
            Check(encoded.Count == fields.Count, "client request matches Go field count: " + op);
            foreach (var pair in fields) Check(encoded.TryGetValue(pair.Key, out var value) && Equals(value, pair.Value), "client request matches Go exact field: " + pair.Key);
            var result = EndgameResponse.Parse(File.ReadAllText(Path.Combine(directory, op == "powerbook.extract" ? "extract-success.json" : "imprint-success.json")));
            Check(request.MatchesResponse(result), "actual Go result settles corresponding extract/apply request");
        }
    }
    static void PowerBookContracts()
    {
        PowerBookServerWireContracts();
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c);
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"],\"result\":{\"powerBook\":\"1\"}}");
        Check(!c.Endgame.PowerBookAvailable && !EndgamePanel.TryOpenPowerBookFromWorld(), "legacy direct-imprint server and header shortcut cannot open new book");
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"],\"result\":{\"powerBook\":\"1\",\"powerBookItem\":\"1\",\"powerCodexItemId\":\"1900031001\",\"powerScrollItemId\":\"1900031002\",\"powerBookName\":\"万象秘典\"}}");
        Check(c.Endgame.PowerBookAvailable && c.Endgame.PowerBookItemID == 1900031001, "negotiated item-only book configuration");
        var request = new EndgameRequest("powerbook.list"); c.SendEndgame(request);
        PowerReply(c, "wrong-response-id", "1", PowerRow("wrong", true));
        Check(c.Endgame.PowerBookLoading && c.Endgame.Powers.Count == 0, "unsolicited catalog cannot replace records");
        PowerReply(c, request.RequestID, "1", PowerRow("SS-U03", true) + "," + PowerRow("SS-U04", false));
        Check(c.Endgame.Powers.Count == 2 && c.Endgame.PowerBookCurrent, "catalog contains owned and unowned records");
        var owned = c.Endgame.Powers[0]; var locked = c.Endgame.Powers[1];
        Check(EndgamePanel.PowerMatches(owned, "术士", "通用", "戒指", true), "class powers can filter a non-head/chest/weapon slot");
        Check(!EndgamePanel.PowerMatches(owned, "战士", "", "", false), "scope filter");
        Check(!EndgamePanel.PowerMatches(owned, "", "攻击", "", false), "category filter");
        Check(!EndgamePanel.PowerMatches(owned, "", "", "鞋", false), "server-authored slot eligibility filter");
        Check(EndgamePanel.PowerMatches(locked, "", "", "", false) && !EndgamePanel.PowerMatches(locked, "", "", "", true), "hide-unobtained checkbox does not delete locked records");
        Check(owned.Description.Contains("45%［40–55%］") && locked.RollBP == 0, "actual value/range stays server authored; locked is not falsely maxed");
        var extract = new EndgameRequest("powerbook.extract", "1", powerID: "SS-U03");
        Check(extract.Mutates && extract.Json.Contains("\"powerId\":\"SS-U03\""), "extract is immutable mutation with no added cost");
        c.SendEndgame(extract);
        Check(!c.SendEndgame(new EndgameRequest("powerbook.extract", "1", powerID: "SS-U03")), "duplicate extraction is blocked while pending");
        Apply(c, "{\"kind\":\"powerbook.result\",\"ok\":true,\"requestId\":\"" + extract.RequestID + "\",\"revision\":\"2\"}");
        Check(!c.Endgame.Pending && c.Endgame.Revision == "2", "extraction receipt clears only matching mutation");
        var apply = new EndgameRequest("powerbook.apply", "2", targetUID: "9007199254740993", runeUID: "9007199254740994");
        Check(apply.Mutates && apply.Json.Contains("9007199254740993") && apply.Json.Contains("9007199254740994"), "scroll and gear UID remain exact beyond JavaScript precision");
        bool rejected = false; try { new EndgameRequest("powerbook.imprint", "2", targetUID: "42", powerID: "SS-U03"); } catch (ArgumentException) { rejected = true; }
        Check(rejected, "old direct-imprint bypass no longer encodable");
        c.SendEndgame(apply); Apply(c, "{\"kind\":\"powerbook.apply\",\"ok\":false,\"requestId\":\"" + apply.RequestID + "\",\"revision\":\"2\",\"error\":\"requirements\",\"message\":\"此器不可承载\"}");
        Check(!c.Endgame.Pending && c.Endgame.Message == "此器不可承载", "server apply rejection preserves message and releases pending");
        InventoryDlg.rows[11] = new InventoryDlg.Item { id = 1900031001, bagIdx = 11, name = "万象秘典", count = 1 };
        InventoryDlg.rows[12] = new InventoryDlg.Item { id = 1900031002, bagIdx = 12, name = "威能卷轴", count = 1 };
        InventoryDlg.rows[13] = new InventoryDlg.Item { id = 1001, bagIdx = 13, name = "暗金甲", count = 1 };
        string bookItem = "{\"uid\":\"101\",\"itemId\":1900031001,\"location\":\"bag\",\"tab\":0,\"slot\":11,\"bound\":true}";
        string scrollItem = "{\"uid\":\"102\",\"itemId\":1900031002,\"location\":\"bag\",\"tab\":0,\"slot\":12,\"bound\":true,\"powerScroll\":{\"powerId\":\"SS-U03\",\"name\":\"凝神回路\",\"description\":\"消耗降低45%［40–55%］。\",\"rollBp\":9000}}";
        string gearItem = "{\"uid\":\"103\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":13,\"legendary\":{\"powerId\":\"OLD\",\"name\":\"旧威能\",\"description\":\"原有效果\"}}";
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"codex-items\",\"revision\":\"2\",\"chunks\":1,\"items\":[" + bookItem + "," + scrollItem + "," + gearItem + "]}");
        Check(c.Endgame.Items.Count == 3 && c.Endgame.Items[1].PowerScroll.RollBP == 9000, "scroll snapshot carries independent captured value");
        Check(EndgamePanel.HandlePowerItemForIndependentRuntime(1900031001, 0, 11) && c.Last.Op == "powerbook.list", "right-click owned bound book opens catalog, no consumable request");
        PowerReply(c, c.Last.RequestID, "2", PowerRow("SS-U03", true) + "," + PowerRow("SS-U04", false));
        Draw(p);
        Check(UnityEngine.GUI.labels.Contains("万象秘典") && !UnityEngine.GUI.labels.Contains("选择背包暗金"), "item window has no direct-imprint target picker");
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.Repaint, mousePosition = new UnityEngine.Vector2(20, 12) };
        Call(p, "DrawPowerBook");
        Check(UITip.last.Contains("45%［40–55%］"), "hover renders saved effect and range without reformatting");
        int beforeLockedClick = c.Sent.Count;
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(20, 56), button = 0 };
        Call(p, "DrawPowerBook");
        Check(c.Sent.Count == beforeLockedClick, "unobtained list row cannot extract");
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(20, 12), button = 0 };
        Call(p, "DrawPowerBook");
        Check(c.Last.Op == "powerbook.extract" && c.Endgame.Pending, "one click on owned list row extracts without target picker or confirmation");
        var rowExtract = c.Last;
        Apply(c, "{\"kind\":\"powerbook.extract\",\"ok\":false,\"requestId\":\"" + rowExtract.RequestID + "\",\"revision\":\"2\",\"error\":\"bag_full\"}");
        UnityEngine.Event.current = new UnityEngine.Event();
        Check(EndgamePanel.HandlePowerItemForIndependentRuntime(1900031002, 0, 12) && AvatarFuse.BegunKind == 3, "right-click scroll enters reused appearance targeting mode");
        Check(!EndgamePanel.ApplyPowerScroll(0, 12, "bag", 0, 12), "scroll cannot target itself");
        Check(EndgamePanel.ApplyPowerScroll(0, 12, "bag", 2, 13) && c.Last.Op == "powerbook.apply" && c.Last.Json.Contains("\"runeUid\":\"102\""), "appearance selection submits exact scroll identity, not avatar opcode");
        Check(!EndgamePanel.ApplyPowerScroll(0, 12, "bag", 2, 13), "repeated selection cannot submit while mutation is pending");
        Apply(c, "{\"kind\":\"powerbook.apply\",\"ok\":false,\"requestId\":\"" + c.Last.RequestID + "\",\"revision\":\"2\",\"error\":\"requirements\"}");
        // A pristine wing has no Legendary payload; eligibility is decided by server WingRules.
        string wingItem = "{\"uid\":\"104\",\"itemId\":1002,\"location\":\"bag\",\"tab\":2,\"slot\":14}";
        InventoryDlg.rows[14] = new InventoryDlg.Item { id = 1002, bagIdx = 14, name = "羽翼", count = 1 };
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"wing-bag\",\"revision\":\"2\",\"chunks\":1,\"items\":[" + scrollItem + "," + wingItem + "]}");
        Check(EndgamePanel.HandlePowerItemForIndependentRuntime(1900031002, 0, 12), "scroll can be selected again after rejected target");
        Check(EndgamePanel.ApplyPowerScroll(0, 12, "bag", 2, 14) && c.Last.Json.Contains("\"targetUid\":\"104\""), "pristine bag wing reaches authoritative scroll application");
        Apply(c, "{\"kind\":\"powerbook.apply\",\"ok\":false,\"requestId\":\"" + c.Last.RequestID + "\",\"revision\":\"2\",\"error\":\"requirements\"}");
        EquipDlg.Equipped.Add(new EquipDlg.EquippedItem { id = 1002, slot = 14, name = "羽翼" });
        wingItem = wingItem.Replace("\"location\":\"bag\"", "\"location\":\"equipped\"");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"wing-worn\",\"revision\":\"2\",\"chunks\":1,\"items\":[" + scrollItem + "," + wingItem + "]}");
        Check(EndgamePanel.HandlePowerItemForIndependentRuntime(1900031002, 0, 12), "scroll targets equipped wing from same owned bag source");
        Check(!EndgamePanel.ApplyPowerScroll(0, 12, "equipped", 0, 15), "missing equipment target cannot submit");
        Check(EndgamePanel.ApplyPowerScroll(0, 12, "equipped", 0, 14), "pristine equipped wing reaches authoritative scroll application");
        EquipDlg.Equipped.RemoveAll(item => item.id == 1002);
        InventoryDlg.rows.Remove(14);
        InventoryDlg.rows.Remove(11); InventoryDlg.rows.Remove(12); InventoryDlg.rows.Remove(13);
        p.Unbind(c);
    }
}
