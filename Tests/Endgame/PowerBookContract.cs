using System;
using System.IO;
using System.Collections.Generic;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

partial class EndgameUiContract
{
    static string PowerRow(string id, int roll = 9000) => "{\"powerId\":\"" + id + "\",\"name\":\"凝神回路\",\"category\":\"通用\",\"class\":\"术士\",\"description\":\"消耗降低45%［40–55%］。\",\"rollBp\":" + roll + ",\"minRollBp\":7500,\"maxRollBp\":10000,\"minLevel\":20}";
    static void PowerReply(Client c, string request, string revision, string rows, string next = "") => Apply(c,
        "{\"kind\":\"powerbook.list\",\"ok\":true,\"requestId\":\"" + request + "\",\"revision\":\"" + revision + "\",\"nextCursor\":\"" + next + "\",\"powers\":[" + rows + "]}");
    static void PowerBookWireContracts()
    {
        string dir = Environment.GetEnvironmentVariable("FFO_POWER_BOOK_FIXTURES");
        if (string.IsNullOrEmpty(dir)) throw new Exception("Missing generated Go power-book fixtures.");
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c);
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"],\"result\":{\"powerBook\":\"1\"}}");
        EndgamePanel.TryOpenPowerBookFromWorld(); c.Endgame.NoteTransportFailure("replaced fixture read", c.Last);
        var list = EndgameResponse.Parse(File.ReadAllText(Path.Combine(dir, "list.json")));
        c.SendEndgame(new EndgameRequest("powerbook.list", requestID: list.RequestID)); c.Endgame.Apply(list);
        Check(c.Endgame.Powers.Count == 3 && c.Endgame.PowerBookCurrent, "actual Go list fixture accepted by client state");
        foreach (var power in c.Endgame.Powers)
        {
            Set(p, "selectedPowerID", power.PowerID); Draw(p);
            Check(UnityEngine.GUI.labels.Contains(power.Description) && !UnityEngine.GUI.labels.Contains(power.Name + "：" + power.Description), "server-prefixed description draws exactly once: " + power.PowerID);
        }
        Check(c.Endgame.Powers[2].RollBP == 8123 && c.Endgame.Powers[2].Description.Contains("56%（52%～70%）"), "server's non-round roll displays exact authored integer and range without client rescaling");
        Check(c.Endgame.Powers[1].RollBP == 0 && c.Endgame.Powers[1].MaxRollBP == 0, "real mechanic fixture has no numeric scale");
        var requestData = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(File.ReadAllText(Path.Combine(dir, "imprint-request.json")));
        var request = new EndgameRequest("powerbook.imprint", (string)requestData["expectedRevision"], targetUID: (string)requestData["targetUid"], powerID: (string)requestData["powerId"], requestID: (string)requestData["requestId"]);
        var encoded = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(request.Json);
        Check(encoded.Count == requestData.Count, "client imprint schema matches actual Go-decoded request");
        foreach (var pair in requestData) Check(encoded.TryGetValue(pair.Key, out var value) && Equals(value, pair.Value), "client and server exact request field: " + pair.Key);
        c.SendEndgame(request); c.Endgame.Apply(EndgameResponse.Parse(File.ReadAllText(Path.Combine(dir, "imprint-success.json"))));
        Check(!c.Endgame.Pending && c.Endgame.Revision == "1" && c.Endgame.Message == "威能已刻入器物。", "actual authoritative commit receipt completes matching request");
        var failure = EndgameResponse.Parse(File.ReadAllText(Path.Combine(dir, "imprint-error.json")));
        c.SendEndgame(new EndgameRequest("powerbook.imprint", "1", targetUID: "42", powerID: "UNKNOWN", requestID: failure.RequestID)); c.Endgame.Apply(failure);
        Check(!c.Endgame.Pending && c.Endgame.Error == "power_locked" && c.Endgame.Message == "此威能尚未收入威能录", "actual Go mutation error completes failure with player-facing reason");
        p.Unbind(c);
    }
    static void PowerBookContracts()
    {
        PowerBookWireContracts();
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c);
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}");
        Check(!EndgamePanel.TryOpenPowerBookFromWorld() && !c.SendEndgame(new EndgameRequest("powerbook.list")), "legacy server cannot expose or receive power-book operations");
        c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"],\"result\":{\"powerBook\":\"1\"}}");
        Check(EndgamePanel.TryOpenPowerBookFromWorld() && c.Last.Op == "powerbook.list", "inventory opener asks only read data");
        var first = c.Last;
        Check(!c.SendEndgame(new EndgameRequest("powerbook.list")), "duplicate concurrent list reads suppressed");
        PowerReply(c, "wrong-response-id", "1", PowerRow("wrong"));
        Check(c.Endgame.PowerBookLoading && c.Endgame.Powers.Count == 0, "unsolicited response cannot replace records");
        PowerReply(c, first.RequestID, "1", PowerRow("SS-U03"), "1");
        Check(c.Endgame.PowerBookCurrent && c.Endgame.Powers.Count == 1 && c.Endgame.PowerBookNextCursor == "1", "first power page publishes exact server record");
        Check(c.Endgame.Powers[0].RollBP == 9000 && c.Endgame.Powers[0].Description.Contains("45%［40–55%］"), "actual number and range stay server-authored");
        Draw(p, "更多威能"); var second = c.Last;
        Check(second.Cursor == "1", "next page uses returned cursor");
        PowerReply(c, second.RequestID, "1", PowerRow("SS-U04"));
        Check(c.Endgame.Powers.Count == 2 && c.Endgame.PowerBookNextCursor == "", "last page appends without losing previous collection");
        Draw(p); Check(!Enabled("更多威能"), "last page cannot request page zero again");
        var native = new InventoryDlg.Item { id = 1001, bagIdx = 0, name = "暗金甲" }; InventoryDlg.rows[0] = native; InventoryDlg.rowTabs.Remove(0);
        string item = "{\"uid\":\"9007199254740993\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"name\":\"暗金甲\",\"legendary\":{\"powerId\":\"OLD\",\"name\":\"旧威能\",\"description\":\"原有效果\"}}";
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"power-inventory\",\"revision\":\"1\",\"chunks\":1,\"items\":[" + item + "]}");
        Set(p, "selectedUID", "9007199254740993"); Set(p, "selectedPowerID", "SS-U03"); Draw(p);
        Check(Enabled("刻印威能") && UnityEngine.GUI.labels.Contains("凝神回路：消耗降低45%［40–55%］。"), "valid record and bag target enable imprint with ordinary description");
        int before = c.Sent.Count; Draw(p, "刻印威能"); var imprint = c.Last;
        Check(c.Sent.Count == before + 1 && imprint.Mutates && imprint.Json.Contains("\"powerId\":\"SS-U03\"") && imprint.Json.Contains("9007199254740993"), "one click creates one immutable imprint request without lossy UID");
        Draw(p, "刻印威能"); Check(c.Sent.Count == before + 1, "repeated old-button click cannot duplicate mutation");
        Draw(p, "查询原请求"); Check(c.Retries == 1 && ReferenceEquals(c.Retried[0], imprint), "retry preserves exact request id and payload");
        Call(p, "Close"); Check(c.Endgame.Pending && (string)Get(p, "selectedPowerID") == "", "closing clears choice but retains uncertain mutation");
        EndgamePanel.TryOpenPowerBookFromWorld(); Draw(p, "查询原请求"); Check(c.Retries == 2 && ReferenceEquals(c.Retried[1], imprint), "reopening cannot change uncertain mutation target");
        Apply(c, "{\"kind\":\"powerbook.imprint\",\"ok\":false,\"requestId\":\"" + imprint.RequestID + "\",\"error\":\"stale_revision\",\"message\":\"背包已变化\"}");
        Check(!c.Endgame.Pending && c.Endgame.Error == "stale_revision", "terminal stale error releases mutation and shows server message");
        Set(p, "selectedUID", "9007199254740993"); Set(p, "selectedPowerID", "SS-U03");
        c.Endgame.NoteNativeInventoryPublished(); Draw(p); Check(!Enabled("刻印威能"), "native inventory changes disable stale target UID");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"power-locked\",\"revision\":\"2\",\"chunks\":1,\"items\":[" + item.Replace("\"name\":\"暗金甲\"", "\"locked\":true,\"name\":\"暗金甲\"") + "]}");
        Draw(p); Check(!Enabled("刻印威能"), "locked and newer-revision target cannot imprint");
        Call(p, "RefreshPowerBook"); var refresh = c.Last; PowerReply(c, refresh.RequestID, "1", PowerRow("stale"));
        Check(c.Endgame.Powers[0].PowerID == "SS-U03" && !c.Endgame.PowerBookCurrent, "stale refresh cannot replace newer revision records");
        Call(p, "RefreshPowerBook"); refresh = c.Last; PowerReply(c, refresh.RequestID, "2", PowerRow("SS-U03",10000));
        Check(c.Endgame.Powers.Count == 1 && c.Endgame.Powers[0].RollBP == 10000, "fresh first page replaces collection and shows improved best roll");
        Draw(p); Check(!Enabled("刻印威能"), "locked target still unavailable with fresh collection");
        Call(p, "RefreshPowerBook"); var timedOut = c.Last; c.Endgame.Tick(DateTime.UtcNow.AddSeconds(16));
        Check(!c.Endgame.PowerBookLoading && c.Endgame.Error == "timeout", "lost list request times out for manual retry");
        Call(p, "RefreshPowerBook"); var afterTimeout = c.Last; PowerReply(c, timedOut.RequestID, "2", PowerRow("late"));
        Check(c.Endgame.PowerBookLoading, "late old page cannot complete newer read");
        c.Endgame.NoteTransportFailure("网络中断", afterTimeout); Check(!c.Endgame.PowerBookLoading, "transport failure releases read without automatic endless retry");
        var result = EndgameResponse.Parse("{\"kind\":\"powerbook.result\",\"ok\":true,\"requestId\":\"" + imprint.RequestID + "\"}");
        Check(imprint.MatchesResponse(result), "imprint success uses distinct result envelope");
        var journal = new EndgameRetryJournal(); journal.Remember("character7", imprint); Check(ReferenceEquals(journal.Find("character7"), imprint), "imprint participates in character-scoped retry journal");
        Call(p, "RefreshPowerBook"); refresh = c.Last;
        bool malformed = false; try { PowerReply(c, refresh.RequestID, "2", PowerRow("duplicate") + "," + PowerRow("duplicate")); } catch (FormatException) { malformed = true; }
        Check(malformed && c.Endgame.Powers[0].PowerID == "SS-U03", "duplicate page rejected atomically without corrupting old records");
        malformed = false; try { EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[" + PowerRow("invalid",10001) + "]}"); } catch (FormatException) { malformed = true; }
        Check(malformed, "roll outside declared range rejected");
        malformed = false; try { new EndgameRequest("powerbook.imprint", "1", powerID: "SS-U03"); } catch (ArgumentException) { malformed = true; }
        Check(malformed, "missing target cannot serialize mutation");
        // A successful write updates the item only through the server snapshot, then refreshes the book once.
        Call(p, "RefreshPowerBook"); refresh = c.Last; PowerReply(c, refresh.RequestID, "2", PowerRow("SS-U03"), "1");
        Draw(p, "更多威能"); second = c.Last; PowerReply(c, second.RequestID, "3", PowerRow("changed-page"));
        Check(!c.Endgame.PowerBookCurrent && c.Endgame.Powers.Count == 1, "page revision change rejects mixed generation collection");
        Call(p, "RefreshPowerBook"); refresh = c.Last; PowerReply(c, refresh.RequestID, "3", PowerRow("SS-U03"));
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"power-success-before\",\"revision\":\"3\",\"chunks\":1,\"items\":[" + item + "]}");
        Set(p, "selectedUID", "9007199254740993"); Set(p, "selectedPowerID", "SS-U03"); Draw(p, "刻印威能"); imprint = c.Last;
        Check(c.Endgame.Items[0].Legendary.PowerID == "OLD", "submission never speculatively changes gear");
        Apply(c, "{\"kind\":\"powerbook.result\",\"ok\":true,\"requestId\":\"" + imprint.RequestID + "\",\"revision\":\"4\"}");
        Check(!c.Endgame.Pending && !c.Endgame.InventorySnapshotCurrent, "success receipt waits for matching authoritative inventory");
        int readsBefore = c.Sent.Count; Call(p, "ObservePowerBookRequest"); var successRefresh = c.Last;
        Check(c.Sent.Count == readsBefore + 2 && successRefresh.Op == "powerbook.list", "successful imprint refreshes snapshot and book once");
        Call(p, "ObservePowerBookRequest"); Check(c.Sent.Count == readsBefore + 2, "observing same success cannot loop refreshes");
        PowerReply(c, successRefresh.RequestID, "4", PowerRow("SS-U03"));
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"power-success-after\",\"revision\":\"4\",\"chunks\":1,\"items\":[" + item.Replace("\"powerId\":\"OLD\"", "\"powerId\":\"SS-U03\"") + "]}");
        Check(c.Endgame.InventorySnapshotCurrent && c.Endgame.Items[0].Legendary.PowerID == "SS-U03", "only final snapshot publishes new equipment power");
        var mechanic = EndgameResponse.Parse("{\"kind\":\"powerbook.list\",\"ok\":true,\"powers\":[{\"powerId\":\"MECHANIC\",\"name\":\"解缚\",\"description\":\"解除控制\",\"rollBp\":0,\"minRollBp\":0,\"maxRollBp\":0}]}");
        Check(mechanic.Powers[0].RollBP == 0 && mechanic.Powers[0].Description == "解除控制", "pure mechanics carry no fabricated numeric roll");
        c.Endgame.Reset(); Call(p, "Update"); Check(c.Endgame.Powers.Count == 0 && !c.Endgame.PowerBookAvailable && !(bool)Get(p, "open"), "character/session reset discards private collection and closes UI");
        p.Unbind(c);
    }
}
