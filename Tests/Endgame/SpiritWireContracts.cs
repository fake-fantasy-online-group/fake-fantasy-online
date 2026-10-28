using System;
using System.IO;
using FakeFantasyOnline.Application;

internal static class SpiritWireContracts
{
    private static int assertions;
    private static void Check(bool value, string name) { assertions++; if (!value) throw new Exception(name); }
    private static void Reject(Action test, string name)
    {
        try { test(); } catch (Exception e) when (e is ArgumentException || e is FormatException) { assertions++; return; }
        throw new Exception("Accepted invalid " + name);
    }
    private const string Definition = "{\"kind\":1,\"name\":\"帽灵\",\"itemId\":36222,\"essenceId\":36223,\"separatorId\":25095,\"npcId\":279,\"mapId\":7,\"npcName\":\"帽灵仙子\",\"npcScript\":\"Nmaoling\",\"slot\":3,\"uiKey\":\"capsoul\",\"processMoney\":\"10000\"}";
    private const string View = "{\"kind\":1,\"itemId\":36222,\"sealed\":false,\"level\":15,\"attrs\":[{\"attr\":1,\"value\":300,\"permanent\":300,\"last\":20,\"level\":15,\"perfectLevel\":15},{\"attr\":2,\"value\":20,\"permanent\":20,\"last\":0,\"level\":1,\"perfectLevel\":1}],\"canUnseal\":false,\"canProcess\":true,\"canRollback\":false,\"canReset\":false,\"unsealEssence\":2,\"processEssence\":5,\"rollbackEssence\":1,\"resetEssence\":200,\"processMoney\":\"10000\"}";
    private static EndgameResponse Reply(string kind, string requestID = "", string revision = "1", bool ok = true, string extra = "") =>
        EndgameResponse.Parse("{\"kind\":\"" + kind + "\",\"requestId\":\"" + requestID + "\",\"revision\":\"" + revision + "\",\"ok\":" + (ok ? "true" : "false") + extra + "}");
    private static EndgameClientState State(bool capable = true)
    {
        var state = new EndgameClientState(); var request = new EndgameRequest("hello"); state.Begin(request);
        state.Apply(Reply("hello", request.RequestID, extra: ",\"capabilities\":[\"endgame.v1\"" + (capable ? ",\"endgame.spirit.v1\"" : "") + "],\"spirits\":[" + Definition + "],\"spiritAppearanceIds\":[25096,25097]"));
        return state;
    }
    private static void Commands()
    {
        var item = new EndgameSpiritSelection(2, 9, 36222, "9007199254740993");
        var material = new EndgameSpiritSelection(1, 7, 36223);
        Check(new EndgameRequest("hello").Json.Contains("endgame.spirit.v1"), "capability offered");
        foreach (string op in new[] { "unseal", "process", "rollback", "reset" })
        {
            var command = new EndgameRequest("spirit." + op, "1", npcEntityID: -1, spiritItem: item, spiritMaterial: material);
            Check(command.Mutates && command.Json.Contains("\"npcEntityId\":4294967295"), "unsigned NPC entity " + op);
            Check(command.Json.Contains("\"uid\":\"9007199254740993\""), "large UID string exact " + op);
            Check(command.Json.Contains("\"spiritMaterial\":{\"tab\":1,\"slot\":7,\"itemId\":36223}"), "old stacked material no UID " + op);
            Check(command.MatchesResponse(Reply("spirit.result", command.RequestID)), "success correlation " + op);
            Check(command.MatchesResponse(Reply(command.Op, command.RequestID, ok: false)), "error correlation " + op);
            Check(!command.MatchesResponse(Reply("cube.result", command.RequestID)), "other result refused " + op);
        }
        foreach (string op in new[] { "fuse", "separate", "appearance" })
        {
            var request = new EndgameRequest("spirit." + op, "1", targetUID: "9876543210", spiritItem: item);
            Check(request.Mutates && !request.Json.Contains("npcEntityId"), "equipment selection " + op);
        }
        Check(!new EndgameRequest("spirit.open", npcEntityID: 50).Mutates, "open readonly");
        Check(!new EndgameRequest("spirit.preview", npcEntityID: 50, spiritItem: item).Mutates, "preview readonly no RNG mutation");
        Reject(() => new EndgameSpiritSelection(-1, 0, 1), "negative tab");
        Reject(() => new EndgameSpiritSelection(0, 100, 1), "outside bag page");
        Reject(() => new EndgameSpiritSelection(0, 0, 0), "invalid item");
        foreach (string uid in new[] { "0", "01", "-1", "1.2", "9223372036854775808" })
            Reject(() => new EndgameSpiritSelection(0, 0, 1, uid), "UID " + uid);
        Reject(() => new EndgameRequest("spirit.preview", npcEntityID: 1, spiritItem: item, runeUID: "17"), "foreign payload fields");
        Reject(() => new EndgameRequest("snapshot", spiritItem: item), "foreign selection");
        Reject(() => new EndgameRequest("spirit.open"), "missing NPC");
        Reject(() => new EndgameRequest("spirit.preview", npcEntityID: 1), "preview missing item");
        Reject(() => new EndgameRequest("spirit.preview", npcEntityID: 1, spiritItem: item, spiritMaterial: material), "preview extra material");
        Reject(() => new EndgameRequest("spirit.open", npcEntityID: 1, spiritItem: item), "open extra item");
        Reject(() => new EndgameRequest("spirit.process", npcEntityID: 1, spiritItem: item, spiritMaterial: material), "mutation missing revision");
        Reject(() => new EndgameRequest("spirit.process", "1", npcEntityID: 1, spiritItem: item), "missing essence");
        Reject(() => new EndgameRequest("spirit.process", "1", npcEntityID: 1, spiritItem: item, spiritMaterial: item), "same bag address");
        Reject(() => new EndgameRequest("spirit.fuse", "1", spiritItem: item), "missing gear UID");
        Reject(() => new EndgameRequest("spirit.fuse", "1", targetUID: item.UID, spiritItem: item), "self fusion");
        Reject(() => new EndgameRequest("spirit.appearance", "1", targetUID: "2", npcEntityID: 1, spiritItem: item), "unexpected NPC");
    }
    private static void Views()
    {
        var response = Reply("spirit.preview", extra: ",\"spirit\":" + View);
        Check(response.Spirit.Level == 15 && response.Spirit.CanProcess, "authoritative partial capacity beats aggregate level");
        Check(response.Spirit.Attrs[1].Level == 1 && response.Spirit.Attrs[1].PerfectLevel == 1, "per-attribute retained slots");
        Check(response.Spirit.Attrs[0].Permanent == 300 && response.Spirit.Attrs[0].Last == 20, "permanent and latest independent");
        Check(response.Spirit.ProcessMoney == "10000" && response.Spirit.ResetEssence == 200, "authoritative costs");
        foreach (string mutation in new[] { View.Replace("\"kind\":1", "\"kind\":0"), View.Replace("\"level\":15", "\"level\":16"), View.Replace("\"last\":20", "\"last\":21"), View.Replace("\"permanent\":300", "\"permanent\":301"), View.Replace("\"attr\":2", "\"attr\":1"), View.Replace("\"sealed\":false", "\"sealed\":true") })
            Reject(() => Reply("spirit.preview", extra: ",\"spirit\":" + mutation), "malformed spirit view");
        Reject(() => Reply("hello", extra: ",\"spirits\":[" + Definition + "," + Definition + "]"), "duplicate kinds");
        Reject(() => Reply("hello", extra: ",\"spiritAppearanceIds\":[1,1]"), "duplicate appearance");
        Reject(() => Reply("hello", extra: ",\"spiritAppearanceIds\":[-1]"), "negative appearance");
        Reject(() => Reply("hello", extra: ",\"spirits\":[" + Definition.Replace("\"10000\"", "10000") + "]"), "money numeric instead of exact decimal string");
        string item = "{\"uid\":\"99\",\"itemId\":1,\"location\":\"bag\",\"tab\":2,\"slot\":0,\"spirit\":" + View + "}";
        Check(Reply("snapshot", extra: ",\"items\":[" + item + "]").Items[0].Spirit != null, "dynamic inventory sidecar");
        Check(Reply("snapshot", extra: ",\"items\":[" + item.Replace("\"spirit\":", "\"fusedSpirit\":") + "]").Items[0].FusedSpirit != null, "fused gear sidecar");
        Check(Reply("snapshot", extra: ",\"items\":[{\"itemId\":1}]").Items[0].Spirit == null, "legacy item parser unchanged");
    }
    private static void Reads()
    {
        var item = new EndgameSpiritSelection(2, 0, 36222);
        var state = State();
        Check(state.SpiritAvailable && state.Spirits.Count == 1 && state.SpiritAppearanceIDs.Count == 2, "hello catalog available before NPC");
        Check(state.Spirits[0].MapID == 7 && state.Spirits[0].NpcName == "帽灵仙子", "exact NPC context");
        Check(!State(false).Begin(new EndgameRequest("spirit.open", npcEntityID: 1)), "legacy server blocks new operations");
        var preview = new EndgameRequest("spirit.preview", npcEntityID: 1, spiritItem: item);
        Check(state.Begin(preview) && state.SpiritLoading, "preview starts");
        Check(!state.Begin(new EndgameRequest("spirit.open", npcEntityID: 1)), "read bounded one at time");
        Check(!state.Apply(Reply("spirit.preview", new string('a', 32), extra: ",\"spirit\":" + View)), "foreign read ignored");
        Check(state.SpiritLoading, "foreign response does not clear intent");
        Check(state.Apply(Reply("spirit.preview", preview.RequestID, extra: ",\"spirit\":" + View)) && state.SpiritCurrent, "matching preview publishes");
        state.InvalidateSpiritPreview();
        Check(!state.SpiritCurrent && !state.Apply(Reply("spirit.preview", preview.RequestID, extra: ",\"spirit\":" + View)), "closed selection stale reply ignored");
        Check(state.Begin(preview), "new intent allowed");
        state.Tick(DateTime.UtcNow.AddSeconds(16));
        Check(!state.SpiritLoading && state.Error == "timeout", "read timeout unlocks selection");
        Check(!state.Apply(Reply("spirit.preview", preview.RequestID, extra: ",\"spirit\":" + View)), "late timeout reply ignored");
        var current = new EndgameRequest("spirit.preview", npcEntityID: 1, spiritItem: item); state.Begin(current);
        state.NoteTransportFailure("failed", current);
        Check(!state.SpiritLoading, "failed transport releases read");
        current = new EndgameRequest("spirit.preview", npcEntityID: 1, spiritItem: item); state.Begin(current);
        state.Apply(Reply("spirit.preview", current.RequestID, "0", extra: ",\"spirit\":" + View));
        Check(!state.SpiritCurrent && state.Error == "stale_revision", "older read cannot revive prior snapshot");
        state.Reset();
        Check(!state.SpiritAvailable && state.Spirits.Count == 0 && state.SpiritAppearanceIDs.Count == 0, "disconnect clears capabilities and catalog");
    }
    private static void Mutations()
    {
        var state = State();
        var request = new EndgameRequest("spirit.process", "1", npcEntityID: 1, spiritItem: new EndgameSpiritSelection(2, 0, 36222), spiritMaterial: new EndgameSpiritSelection(1, 1, 36223));
        Check(state.Begin(request) && state.Pending, "mutation owns journal");
        Check(!state.Begin(request), "duplicate click refused");
        Check(!state.Begin(new EndgameRequest("spirit.open", npcEntityID: 1)), "mutating prevents competing reads");
        Check(!state.Apply(Reply("spirit.result", new string('a', 32), "99", extra: ",\"spirit\":" + View)), "unsolicited result ignored");
        Check(!state.Apply(Reply("spirit.process", new string('a', 32), "99", false, ",\"error\":\"materials\"")), "foreign failure ignored");
        Check(state.Pending && state.Revision != "99", "foreign result cannot free mutation or change revision");
        state.Apply(Reply("spirit.process", request.RequestID, ok: false, extra: ",\"error\":\"busy\""));
        Check(state.Pending && ReferenceEquals(state.PendingRequest, request), "busy retains immutable original request");
        state.Apply(Reply("spirit.result", request.RequestID, "2", extra: ",\"spirit\":" + View));
        Check(!state.Pending && state.SpiritCurrent && state.Revision == "2", "confirmed mutation view and revision");
        state.InvalidateSpiritPreview();
        Check(!state.Apply(Reply("spirit.result", request.RequestID, "2", extra: ",\"spirit\":" + View)), "replayed result not attached to new selection");
        Check(state.Begin(request), "terminal previous request permits another action");
        state.Apply(Reply("spirit.process", request.RequestID, "2", false, ",\"error\":\"materials\""));
        Check(!state.Pending && state.Error == "materials", "definitive failure terminal");
    }
    private static void Journal()
    {
        var journal = new EndgameRetryJournal(); string scope = EndgameRetryJournal.Scope("host", "account", "character");
        var request = new EndgameRequest("spirit.process", "1", npcEntityID: 1, spiritItem: new EndgameSpiritSelection(2, 0, 24000, "9007199254740993"), spiritMaterial: new EndgameSpiritSelection(1, 1, 35000));
        journal.Remember(scope, request);
        Check(ReferenceEquals(journal.Find(scope), request), "spirit retry retains immutable original request and selections");
        journal.Complete(scope, new string('f',32));
        Check(ReferenceEquals(journal.Find(scope), request), "unrelated receipt cannot erase recovery request");
        Check(journal.Find(EndgameRetryJournal.Scope("host", "account", "other")) == null, "spirit retry scoped to authenticated character");
        journal.Complete(scope, request.RequestID);
        Check(journal.Find(scope) == null, "confirmed receipt completes spirit journal");
        journal.Remember(scope, new EndgameRequest("spirit.preview", npcEntityID:1, spiritItem:new EndgameSpiritSelection(2,0,24000)));
        Check(journal.Find(scope) == null, "read-only preview never becomes retryable mutation");
    }
    private static void ServerFixtures()
    {
        string root = Environment.GetEnvironmentVariable("FFO_SPIRIT_WIRE_FIXTURES");
        if (string.IsNullOrEmpty(root)) throw new Exception("Actual server spirit fixtures are required.");
        var hello = EndgameResponse.Parse(File.ReadAllText(Path.Combine(root, "hello.json")));
        var preview = EndgameResponse.Parse(File.ReadAllText(Path.Combine(root, "preview.json")));
        var result = EndgameResponse.Parse(File.ReadAllText(Path.Combine(root, "success.json")));
        Check(hello.Spirits.Count == 9 && hello.SpiritAppearanceIDs.Count == 92, "actual Go catalog nine spirits and 92 supported appearances");
        var state = new EndgameClientState(); state.Begin(new EndgameRequest("hello", requestID: hello.RequestID)); state.Apply(hello);
        var item = new EndgameSpiritSelection(2,0,8038,"123");
        Check(state.Begin(new EndgameRequest("spirit.preview", "0", npcEntityID:287, spiritItem:item, requestID:preview.RequestID)), "actual fixture preview request begins");
        Check(state.Apply(preview) && state.SpiritCurrent && state.SpiritView.Attrs[0].Permanent == 20, "actual Go preview retains perfect value");
        Check(state.Begin(new EndgameRequest("spirit.process", "0", npcEntityID:287, spiritItem:item, spiritMaterial:new EndgameSpiritSelection(1,1,4230), requestID:result.RequestID)), "actual fixture process begins");
        Check(state.Apply(result) && !state.Pending && state.Revision == "1" && state.SpiritView.Attrs[0].PerfectLevel == 1, "actual Go result correlates and publishes state");
    }
    public static void Main()
    {
        Commands(); Views(); Reads(); Mutations(); Journal(); ServerFixtures();
        Console.WriteLine("Spirit wire contracts: " + assertions + " assertions passed.");
    }
}
