using System;
using System.IO;
using FakeFantasyOnline.Foundation;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

partial class EndgameUiContract
{
    static string PowerRow(string id, bool unlocked, string slots = "\"衣服\",\"戒指\"") => "{\"powerId\":\"" + id + "\",\"name\":\"凝神回路\",\"category\":\"通用\",\"class\":\"术士\",\"description\":\"消耗降低45%。\",\"rangeText\":\"消耗降低40～55%\",\"rollBp\":" + (unlocked ? "9000" : "0") + ",\"minRollBp\":7500,\"maxRollBp\":10000,\"minLevel\":20,\"unlocked\":" + (unlocked ? "true" : "false") + ",\"slots\":[" + slots + "]}";
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
        bool hasRange = false;
        foreach (var power in list.Powers) {
            hasRange |= power.RangeText.Length > 0;
            Check(!power.Description.Contains("～"), "actual Go power effect description no longer embeds roll bounds");
        }
        Check(hasRange, "actual Go catalog authors effect bounds in independent rangeText field");
        var currentOracles = new HashSet<string>();
        foreach (string filename in new[] { "current-value-response.json", "current-value-universal-response.json" })
        {
            var actual = EndgameResponse.Parse(File.ReadAllText(Path.Combine(directory, filename)));
            foreach (var power in actual.Powers)
            {
                if (power.PowerID == "SS-A01" || power.PowerID == "SS-A13" || power.PowerID == "TY-A01" || power.PowerID == "SS-U06") currentOracles.Add(power.PowerID);
                if (power.PowerID == "SS-A01") Check(EndgamePanel.PowerCurrentValueText(power) == "56.86%" && EndgamePanel.PowerRangeText(power) == "52.5%～70%", "actual Go decimal current/primary bounds retain exact precision");
                if (power.PowerID == "SS-A13") Check(EndgamePanel.PowerCurrentValueText(power) == "69%" && EndgamePanel.PowerRangeText(power) == "63%～85%", "actual Go SS-A13 current and range match same parameter despite prose ordering");
                if (power.PowerID == "TY-A01") Check(EndgamePanel.PowerCurrentValueText(power) == "56%" && EndgamePanel.PowerRangeText(power) == "52%～70%" && power.Description.Contains("120度") && power.Description.Contains("100%"), "actual Go TY-A01 does not confuse fixed geometry/base damage with current value");
                if (power.PowerID == "SS-U06") Check(power.Unlocked && EndgamePanel.PowerCurrentValueText(power) == "—" && EndgamePanel.PowerRangeText(power) == "—", "actual Go owned functional power has no invented numbers");
                if (!power.Unlocked) Check(EndgamePanel.PowerCurrentValueText(power) == "—", "actual Go unowned page row displays no current value");
            }
        }
        Check(currentOracles.Count == 4, "all four real Go current-value regression cases were exercised");
        var scenarios = (IReadOnlyList<object>)BoundedJson.Parse(File.ReadAllText(Path.Combine(directory, "current-values.json")));
        Check(scenarios.Count == 5, "actual Go matrix covers unowned, legacy zero, min, 8123 and max");
        foreach (var entry in scenarios)
        {
            var scenario = (IReadOnlyDictionary<string, object>)entry;
            var matrixRows = (IReadOnlyList<object>)scenario["rows"];
            Check(matrixRows.Count == 330, "all330 producer primary values decode in each scenario");
            foreach (var raw in matrixRows)
            {
                var fields = new Dictionary<string, object>(); foreach (var pair in (IReadOnlyDictionary<string, object>)raw) fields.Add(pair.Key, pair.Value);
                // Matrix contains the exact producer value fields. Add only the DTO's
                // unrelated required text fields; never compute or rewrite a value.
                fields["name"] = fields["powerId"]; fields["description"] = "";
                var power = new EndgamePowerRecordView(fields);
                string expectedCurrent = power.Unlocked && fields.ContainsKey("currentValueText") ? (string)fields["currentValueText"] : "—";
                string expectedRange = fields.ContainsKey("primaryRangeText") ? (string)fields["primaryRangeText"] : "—";
                Check(EndgamePanel.PowerCurrentValueText(power) == expectedCurrent && EndgamePanel.PowerRangeText(power) == expectedRange, "all330 current/range pairs preserve authoritative producer formatting");
            }
        }

        var scroll = EndgameResponse.Parse(File.ReadAllText(Path.Combine(directory, "scroll-snapshot.json")));
        Check(scroll.Items.Count == 1 && scroll.Items[0].PowerScroll.RollBP == 8123 && scroll.Items[0].UID == "9007199254740993" && scroll.Items[0].Bound, "actual Go bound scroll preserves exact UID and captured roll");
        var nativeFixture = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(File.ReadAllText(Path.Combine(directory, "native-scroll-display.json")));
        var actualScroll = EndgameResponse.Parse(BoundedJson.Serialize(nativeFixture["snapshot"])).Items[0].PowerScroll;
        string nativeScrollTip = (string)nativeFixture["tooltip"];
        string snapshotOverlaid = (string)typeof(EndgamePanel).GetMethod("PowerScrollTooltip", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { actualScroll, nativeScrollTip });
        Check(snapshotOverlaid == nativeScrollTip && nativeScrollTip.Contains("\u0006"), "actual Go native orange scroll effect plus typed snapshot stays byte-identical");
        foreach (string effectLine in actualScroll.Description.Split('\n'))
            if (effectLine.Length > 0) Check(Occurrences(snapshotOverlaid, effectLine) == 1, "actual native/sidecar scroll effect has one orange copy, not duplicated fallback text");

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
    static void PowerFilterEvent(EndgamePanel p, UnityEngine.EventType type, float x = -9000, float y = -9000, UnityEngine.KeyCode key = UnityEngine.KeyCode.F8, float wheel = 0)
    {
        UnityEngine.Event.current = new UnityEngine.Event { type = type, mousePosition = new UnityEngine.Vector2(x, y), keyCode = key, delta = new UnityEngine.Vector2(0, wheel) };
        Call(p, "DrawPowerBook");
    }
    static void PowerFilterContracts(Client c, EndgamePanel p)
    {
        int sent = c.Sent.Count, count = c.Endgame.Powers.Count;
        foreach (bool mobile in new[] { false, true })
        foreach (float scale in new[] { .4f, 1f, 2f })
        foreach (int optionCount in new[] { 1, 7, 26 })
        foreach (int filterIndex in new[] { 0, 1, 2 })
        {
            Set(p, "powerFilterOpen", filterIndex); Set(p, "panelScale", scale); MobileClientPresentation.Enabled = mobile;
            object[] args = { optionCount, 0, 0f };
            var popup = (UnityEngine.Rect)typeof(EndgamePanel).GetMethod("PowerFilterPopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(p, args);
            float rowHeight = (float)args[2]; int visible = (int)args[1];
            Check(popup.y + 4 + visible * rowHeight <= popup.yMax - 4, "last option/scrollbar stays entirely above matching input border");
            Check(popup.yMax <= 640 && visible >= 1 && visible <= Math.Min(8, optionCount), "native dropdown fits window including full bottom frame");
            Check(mobile ? rowHeight * scale >= 44 : rowHeight == 24, "native popup preserves desktop24px and mobile44physical rows");
            foreach (bool overflow in new[] { false, true })
            foreach (int index in new[] { 0, visible - 1 })
            {
                var row = (UnityEngine.Rect)typeof(EndgamePanel).GetMethod("PowerFilterOptionRect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).Invoke(null, new object[] { popup, index, rowHeight, overflow });
                Check(row.x >= popup.x + 4 && row.xMax <= popup.xMax - 4 && row.y >= popup.y + 4 && row.yMax <= popup.yMax - 4, "every row corner clears native editbk gold pixels at inset2");
                Check(row.x + 6 == popup.x + 10 && row.y + (rowHeight - 18) / 2 >= row.y && row.y + (rowHeight + 18) / 2 <= row.yMax, "native text matches field left padding and fits first/last option vertically");
                Check(row.width == popup.width - 8 - (overflow ? 19 : 0), "only overflowing options reserve separate15px scrollbar plus4px gap");
            }
        }
        Set(p, "powerFilterOpen", -1); Set(p, "panelScale", 1f); MobileClientPresentation.Enabled = false;
        for (int filter = 0; filter < 3; filter++)
        {
            PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 30 + filter * 216, 78);
            Check((int)Get(p, "powerFilterOpen") == -1, "outside label is not part of dropdown click target");
            var field = (UnityEngine.Rect)typeof(EndgamePanel).GetMethod("PowerFilterField", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(p, new object[] { filter });
            Check(field.x == 68 + filter * 216 && field.xMax == 226 + filter * 216, "label and native field fit separate nonoverlapping group rectangles");
            PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84 + filter * 216, 78);
            Check((int)Get(p, "powerFilterOpen") == filter, "each native input opens its option list without cycling: " + filter);
            Check((string)Get(p, filter == 0 ? "powerClass" : filter == 1 ? "powerCategory" : "powerSlot") == "", "opening does not change filter value");
            PowerFilterEvent(p, UnityEngine.EventType.Repaint);
            Check(UnityEngine.GUI.labels.Contains("全部"), "open popup paints actual options");
            Check(UnityEngine.GUI.labels.Contains(filter == 0 ? "范围：" : filter == 1 ? "类型：" : "部位：") && !UnityEngine.GUI.labels.Contains("范围：全部"), "label paints separately and input displays value only");
            object[] popupArgs = { 2, 0, 0f };
            var anchoredPopup = (UnityEngine.Rect)typeof(EndgamePanel).GetMethod("PowerFilterPopup", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(p, popupArgs);
            Check(anchoredPopup.x == field.x && anchoredPopup.width == field.width, "popup anchors to actual input, never label");

            PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 90 + filter * 216, 135);
            Check((int)Get(p, "powerFilterOpen") == -1 && UnityEngine.Event.current.type == UnityEngine.EventType.Used, "select closes popup and consumes click");
            Check((string)Get(p, filter == 0 ? "powerClass" : filter == 1 ? "powerCategory" : "powerSlot") == (filter == 0 ? "术士" : filter == 1 ? "通用" : "衣服"), "selected option updates only intended filter");
            PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84 + filter * 216, 78);
            PowerFilterEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Home);
            PowerFilterEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Return);
        }
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84, 78);
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 300, 78);
        Check((int)Get(p, "powerFilterOpen") == 1, "one click switches directly to another dropdown");
        PowerFilterEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Escape);
        Check((int)Get(p, "powerFilterOpen") == -1 && (bool)Get(p, "open"), "Escape dismisses only list, retaining book");
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84, 78);
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 20, 12);
        Check((int)Get(p, "powerFilterOpen") == -1 && c.Sent.Count == sent, "outside click consumed before covered power row can extract");
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84, 78);
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84, 78);
        Check((int)Get(p, "powerFilterOpen") == -1, "repeated field click toggles popup closed");
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84, 78);
        Call(p, "ClearSelection");
        Check((int)Get(p, "powerFilterOpen") == -1 && (int)Get(p, "powerFilterScroll") == 0, "close/generation selection reset clears transient dropdown state");
        Check(c.Sent.Count == sent && c.Endgame.Powers.Count == count && c.Endgame.Powers[0].RollBP == 9000, "filters neither request/re-roll nor mutate authoritative catalog");
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 730, 78);
        Check((bool)Get(p, "hideUnobtained"), "native checkbox label toggles hide unobtained");
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 700, 78);
        Check(!(bool)Get(p, "hideUnobtained"), "native checkbox icon toggles it back");
        string original = PowerRow("SS-U03", true) + "," + PowerRow("SS-U04", false);
        var slots = new List<string>(); for (int i = 0; i < 25; i++) slots.Add("\"部位" + i + "\"");
        var request = new EndgameRequest("powerbook.list"); c.SendEndgame(request);
        PowerReply(c, request.RequestID, "2", PowerRow("many", true, string.Join(",", slots)));
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 516, 78);
        PowerFilterEvent(p, UnityEngine.EventType.ScrollWheel, 520, 180, wheel: 1);
        Check((int)Get(p, "powerFilterScroll") == 1 && UnityEngine.Event.current.type == UnityEngine.EventType.Used, "long list wheel scroll stays within popup");
        PowerFilterEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.End);
        Check((int)Get(p, "powerFilterHighlight") == 25 && (int)Get(p, "powerFilterScroll") == 18, "End key clamps last full page of native options");
        PowerFilterEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Return);
        Check((string)Get(p, "powerSlot") == "部位24", "keyboard commits last option and dismisses");
        Set(p, "powerSlot", ""); Set(p, "panelScale", .4f); MobileClientPresentation.Enabled = true;
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 516, 78);
        PowerFilterEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.End);
        Check((int)Get(p, "powerFilterScroll") == 22, "scaled popup keeps 44-screen-pixel row targets and four visible rows inside panel");
        Set(p, "panelScale", 1f); MobileClientPresentation.Enabled = false;
        request = new EndgameRequest("powerbook.list"); c.SendEndgame(request);
        PowerFilterEvent(p, UnityEngine.EventType.Repaint);
        Check((int)Get(p, "powerFilterOpen") == -1, "pending catalog read clears popup state");
        PowerReply(c, request.RequestID, "2", original);
        var manyRows = new List<string>(); for (int i = 0; i < 40; i++) manyRows.Add(PowerRow("scroll-row-" + i, true));
        request = new EndgameRequest("powerbook.list"); c.SendEndgame(request);
        PowerReply(c, request.RequestID, "2", string.Join(",", manyRows));
        Set(p, "powerScroll", UnityEngine.Vector2.zero);
        PowerFilterEvent(p, UnityEngine.EventType.ScrollWheel, 100, 200, wheel: 1);
        Check(((UnityEngine.Vector2)Get(p, "powerScroll")).y == 52 && UnityEngine.Event.current.type == UnityEngine.EventType.Used, "book list wheel routes through native scrollbar without extraction");
        Check(UnityEngine.GUI.nativeScrollViews > 0 && NativeScroll.bars > 0, "book viewport disables Unity scroll skins and draws native BarAt");
        PowerFilterEvent(p, UnityEngine.EventType.MouseDown, 84, 78);
        PowerFilterEvent(p, UnityEngine.EventType.ScrollWheel, 80, 150, wheel: 1);
        Check(((UnityEngine.Vector2)Get(p, "powerScroll")).y == 52, "open popup wheel does not move underlying book list");
        PowerFilterEvent(p, UnityEngine.EventType.KeyDown, key: UnityEngine.KeyCode.Escape);
        Set(p, "powerScroll", new UnityEngine.Vector2(0, 99999));
        PowerFilterEvent(p, UnityEngine.EventType.Repaint);
        Check(((UnityEngine.Vector2)Get(p, "powerScroll")).y == 40 * 32 - 442, "list overflow clamps to final viewport without blank overscroll");
        Set(p, "powerClass", "no matching class"); PowerFilterEvent(p, UnityEngine.EventType.Repaint);
        Check(((UnityEngine.Vector2)Get(p, "powerScroll")).y == 0, "filtering to empty list resets native scroll bounds");
        Set(p, "powerClass", "");
        request = new EndgameRequest("powerbook.list"); c.SendEndgame(request); PowerReply(c, request.RequestID, "2", original);
        UnityEngine.Event.current = new UnityEngine.Event();
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
        Check(owned.Description == "消耗降低45%。" && owned.RangeText == "消耗降低40～55%" && locked.RollBP == 0, "current effect and legal bounds are separate server-authored fields; locked is not falsely maxed");
        Check(EndgamePanel.PowerRangeText(owned) == "40～55%", "range column uses actual parameter bounds, not generic roll percent");
        Check(EndgamePanel.PowerCurrentValueText(owned) == "—", "older server missing currentValueText never triggers local numerical inference");
        string currentFixture = PowerRow("TY-A01", true).Replace("消耗降低40～55%", "技能伤害提高52%～70%").Replace("消耗降低45%。", "前方120度内造成100%伤害，技能伤害提高56%。").Replace("\"rangeText\":", "\"currentValueText\":\"56%\",\"rangeText\":");
        var currentPower = EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + currentFixture + "]}").Powers[0];
        Check(currentPower.CurrentValueText == "56%" && EndgamePanel.PowerCurrentValueText(currentPower) == "56%", "current value is exact server primary parameter, never first120 or100 from description");
        var zeroPower = EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + currentFixture.Replace("\"currentValueText\":\"56%\"", "\"currentValueText\":\"0%\"") + "]}").Powers[0];
        Check(EndgamePanel.PowerCurrentValueText(zeroPower) == "0%", "authoritative zero is a legitimate current value");
        var lockedCurrent = EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + currentFixture.Replace("\"unlocked\":true", "\"unlocked\":false") + "]}").Powers[0];
        Check(EndgamePanel.PowerCurrentValueText(lockedCurrent) == "—", "unobtained record never displays a current value even if field supplied");
        bool badCurrent = false; try { EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + currentFixture.Replace("\"currentValueText\":\"56%\"", "\"currentValueText\":17") + "]}"); } catch (FormatException) { badCurrent = true; }
        Check(badCurrent, "malformed current value is rejected rather than formatted locally");
        var pairedRange = EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + currentFixture.Replace("TY-A01", "SS-A13").Replace("56%", "69%").Replace("\"rangeText\":", "\"primaryRangeText\":\"63%～85%\",\"rangeText\":").Replace("技能伤害提高52%～70%", "冰爆伤害48%～65%，内圈伤害63%～85%") + "]}").Powers[0];
        Check(pairedRange.PrimaryRangeText == "63%～85%" && EndgamePanel.PowerRangeText(pairedRange) == "63%～85%", "explicit server primary bounds override earlier unrelated prose range (SS-A13)");
        bool badPrimary = false; try { EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + currentFixture.Replace("\"rangeText\":", "\"primaryRangeText\":17,\"rangeText\":") + "]}"); } catch (FormatException) { badPrimary = true; }
        Check(badPrimary, "malformed primary bounds field is rejected");


        var fixedPower = EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[{\"powerId\":\"fixed\",\"name\":\"固定威能\",\"description\":\"解除控制。\"}]}").Powers[0];
        Check(EndgamePanel.PowerRangeText(fixedPower) == "—", "fixed or older catalog without range text never invents 0-0 bounds");
        bool badRange = false; try { EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + PowerRow("bad", true).Replace("\"rangeText\":\"消耗降低40～55%\"", "\"rangeText\":17") + "]}"); } catch (FormatException) { badRange = true; }
        Check(badRange, "malformed range field is rejected instead of silently formatting a number");
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
        string scrollItem = "{\"uid\":\"102\",\"itemId\":1900031002,\"location\":\"bag\",\"tab\":0,\"slot\":12,\"bound\":true,\"powerScroll\":{\"powerId\":\"SS-U03\",\"name\":\"凝神回路\",\"description\":\"消耗降低45%。\",\"rollBp\":9000}}";
        string gearItem = "{\"uid\":\"103\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":13,\"legendary\":{\"powerId\":\"OLD\",\"name\":\"旧威能\",\"description\":\"原有效果\"}}";
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"codex-items\",\"revision\":\"2\",\"chunks\":1,\"items\":[" + bookItem + "," + scrollItem + "," + gearItem + "]}");
        Check(c.Endgame.Items.Count == 3 && c.Endgame.Items[1].PowerScroll.RollBP == 9000, "scroll snapshot carries independent captured value");
        string scrollNative = "\u0007\u00035威能卷轴\n记录一项威能的卷轴。右击后选择暗金装备或羽翼，将此卷所载威能附着其上。";
        string orangeScroll = EndgamePanel.OverlayTooltip(c.Endgame.Items[1], scrollNative);
        Check(orangeScroll.StartsWith(scrollNative) && orangeScroll.EndsWith("\u0006消耗降低45%。"), "scroll preserves native title/usage and marks only actual effect with equipment orange protocol");
        Check(EndgamePanel.OverlayTooltip(c.Endgame.Items[1], orangeScroll) == orangeScroll, "orange scroll overlay is idempotent without white fallback duplication");
        string oldScroll = scrollNative + "\n消耗降低45%。\n消耗降低45%。";
        string normalizedScroll = EndgamePanel.OverlayTooltip(c.Endgame.Items[1], oldScroll);
        Check(Occurrences(normalizedScroll, "消耗降低45%。") == 1 && normalizedScroll.EndsWith("\u0006消耗降低45%。"), "existing white scroll effect is upgraded to gear orange once, not appended twice");

        Check(EndgamePanel.HandlePowerItemForIndependentRuntime(1900031001, 0, 11) && c.Last.Op == "powerbook.list", "right-click owned bound book opens catalog, no consumable request");
        PowerReply(c, c.Last.RequestID, "2", PowerRow("SS-U03", true) + "," + PowerRow("SS-U04", false));
        Draw(p);
        PowerFilterContracts(c, p);
        Check(UnityEngine.GUI.labels.Contains("万象秘典") && !UnityEngine.GUI.labels.Contains("选择背包暗金"), "item window has no direct-imprint target picker");
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.Repaint, mousePosition = new UnityEngine.Vector2(20, 12) };
        Call(p, "DrawPowerBook");
        Check(UITip.last.StartsWith("\u0006凝神回路：消耗降低45%。") && !UITip.last.Contains("40～55"), "hover renders current effect without repeating range column");
        Check(UnityEngine.GUI.labels.Contains("数值范围") && UnityEngine.GUI.labels.Contains("40～55%"), "codex has a separately labeled parameter range column");
        var multi = EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + PowerRow("multi", false).Replace("消耗降低40～55%", "技能伤害提高30～40%\\n效果持续8～12秒\\n每秒恢复法力20～30点") + "]}").Powers[0];
        float multiHeight = (float)typeof(EndgamePanel).GetMethod("PowerBookRowHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(p, new object[] { multi });
        var rangeStyle = (UnityEngine.GUIStyle)typeof(EndgamePanel).GetField("small", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).GetValue(p);
        Check(EndgamePanel.PowerRangeText(multi) == "30～40%" && multiHeight >= 32, "compact range keeps only first authored endpoints, never mixes percent/seconds/mana");
        Check(!UnityEngine.GUI.labels.Contains("分类") && UnityEngine.GUI.labels.Contains("操作") && UnityEngine.GUI.labels.Contains("当前数值"), "table removes classification column but keeps actions");
        int beforeLockedClick = c.Sent.Count;
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(20, (float)typeof(EndgamePanel).GetMethod("PowerBookRowHeight", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(p, new object[] { owned }) + 12), button = 0 };
        Call(p, "DrawPowerBook");
        Check(c.Sent.Count == beforeLockedClick, "unobtained list row cannot extract");
        UnityEngine.Event.current = new UnityEngine.Event { type = UnityEngine.EventType.MouseDown, mousePosition = new UnityEngine.Vector2(20, 12), button = 0 };
        Call(p, "DrawPowerBook");
        Check(c.Sent.Count == beforeLockedClick, "owned row hover/click never extracts accidentally");
        UnityEngine.Event.current = new UnityEngine.Event();
        ArtworkSkin.click = "提取卷轴"; Call(p, "DrawPowerBook");
        Check(c.Last.Op == "powerbook.extract" && c.Endgame.Pending, "explicit owned-row extraction button submits without target picker or confirmation");
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
