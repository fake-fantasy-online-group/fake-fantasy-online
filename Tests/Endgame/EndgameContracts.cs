using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        internal sealed class Buf { public void Bytes(byte[] payload) { } }
        private void Send(ushort op, Action<Buf> write) { write(new Buf()); }
    }
}
class EndgameContracts
{
    static int assertions;
    static void Check(bool value, string name) { if (!value) throw new Exception(name); assertions++; }
    static void Reject(Action action, string name)
    {
        try { action(); } catch (Exception e) when (e is FormatException || e is ArgumentException) { assertions++; return; }
        throw new Exception("Accepted " + name);
    }
    static EndgameResponse Response(string json) => EndgameResponse.Parse(json);
    static EndgameClientState State()
    {
        var state = new EndgameClientState();
        Check(!state.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}")), "unsolicited hello rejected");
        state.Begin(new EndgameRequest("hello")); state.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}")); return state;
    }
    static string Item(string uid = "9007199254740993", int slot = 0) => "{\"uid\":\"" + uid + "\",\"itemId\":1001,\"location\":\"bag\",\"tab\":2,\"slot\":" + slot + ",\"name\":\"飞鹤\"}";
    static string Snap(string id, string rev, int chunk, int chunks, string items) => "{\"kind\":\"snapshot\",\"ok\":true,\"revision\":\"" + rev + "\",\"snapshotId\":\"" + id + "\",\"chunk\":" + chunk + ",\"chunks\":" + chunks + ",\"items\":[" + items + "]}";
    static byte[] Envelope(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json); var result = new byte[bytes.Length + 4]; result[0] = 1; result[2] = (byte)bytes.Length; result[3] = (byte)(bytes.Length >> 8);
        Buffer.BlockCopy(bytes, 0, result, 4, bytes.Length); return result;
    }
    static EndgameResponse Fixture(string directory, string name)
    {
        string hex = File.ReadAllText(Path.Combine(directory, name + ".wire.hex")).Trim(); var bytes = new byte[hex.Length / 2];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hex.Substring(i * 2, 2), 16);
        Check(bytes[0] == 0xd0 && bytes[1] == 0x80, "Go opcode " + name);
        byte[] payload = new byte[bytes.Length - 2]; Buffer.BlockCopy(bytes, 2, payload, 0, payload.Length);
        return PrivateServerEndgameCodec.Decode(payload);
    }
    static void PortableCubeContracts()
    {
        var s = new EndgameClientState();
        Action<string, bool> hello = (result, capable) =>
        {
            s.Begin(new EndgameRequest("hello"));
            s.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"" + (capable ? ",\"endgame.cube-inputs.v1\"" : "") + "],\"result\":{" + result + "}}"));
        };
        hello("\"cubeItemId\":\"1900028301\",\"cubeNPC\":\"游方巧匠\",\"cubeLore\":\"旧鼎随身。\"", true);
        Check(s.CubeItemID == 1900028301 && s.CubeNPC == "游方巧匠" && s.CubeLore == "旧鼎随身。", "hello supplies server-owned portable item and recovery lore");
        hello("\"cubeItemId\":\"1900028399\"", true);
        Check(s.CubeItemID == 1900028399 && s.CubeNPC == "" && s.CubeLore == "", "portable identity is server-configured and omitted lore never inherits old metadata");
        foreach (string invalid in new[] { "", "0", "-1", "+1900028301", "1900028301 ", "not-an-id", "2147483648" })
        {
            hello("\"cubeItemId\":\"" + invalid + "\",\"cubeNPC\":\"游方巧匠\",\"cubeLore\":\"旧鼎随身。\"", true);
            Check(s.CubeItemID == 0 && s.CubeNPC == "" && s.CubeLore == "", "invalid portable ID fails closed: " + invalid);
        }
        hello("", true); Check(s.CubeItemID == 0, "missing portable ID fails closed");
        hello("\"cubeItemId\":\"1900028301\"", false); Check(!s.CubeInputsV1 && s.CubeItemID == 0, "unnegotiated result cannot grant portable entry");
        hello("\"cubeItemId\":\"1900028301\"", true); s.Reset();
        Check(!s.CubeInputsV1 && s.CubeItemID == 0 && s.CubeNPC == "" && s.CubeLore == "", "reset removes portable identity and NPC metadata");
        Reject(() => new EndgameRequest("cube.recover", "1"), "recovery without native NPC");
        Reject(() => new EndgameRequest("cube.recover", npcEntityID: 7), "recovery without revision");
        Reject(() => new EndgameRequest("cube.recover", "invalid", npcEntityID: 7), "recovery with malformed revision");
        var recover = new EndgameRequest("cube.recover", "42", npcEntityID: -1);
        Check(recover.Mutates && recover.Json.Contains("\"npcEntityId\":4294967295") && recover.Json.Contains("\"expectedRevision\":\"42\""), "recovery freezes unsigned native NPC identity and revision");
        hello("\"cubeItemId\":\"1900028301\"", true); Check(s.Begin(recover), "negotiated recovery enters mutation tracking");
        string frozen = recover.Json;
        s.Apply(Response("{\"kind\":\"cube.result\",\"ok\":true,\"requestId\":\"" + recover.RequestID + "\"}"));
        Check(ReferenceEquals(s.PendingRequest,recover), "cube execution receipt cannot finish recovery");
        s.Apply(Response("{\"kind\":\"cube.recover\",\"ok\":false,\"error\":\"storage\",\"requestId\":\"" + recover.RequestID + "\"}"));
        Check(ReferenceEquals(s.PendingRequest,recover) && recover.Json == frozen, "uncertain recovery retains exact nonce and payload");
        s.Apply(Response("{\"kind\":\"cube.recover\",\"ok\":true,\"requestId\":\"" + recover.RequestID + "\",\"revision\":\"43\"}"));
        Check(!s.Pending, "correlated recovery success ends pending state");
    }
    static void Main(string[] args)
    {
        PortableCubeContracts();
        var s = State(); Check(s.Negotiated, "hello negotiated");
        foreach (string error in new[] { "snapshot_size", "storage" })
        {
            var failedRead = State();
            failedRead.Apply(Response(Snap("before-read-error", "1", 0, 1, Item())));
            var pendingMutation = new EndgameRequest("socket.insert", "1", "9007199254740993", "82", 0);
            failedRead.Begin(pendingMutation);
            failedRead.NoteNativeInventoryPublished();
            failedRead.Begin(new EndgameRequest("snapshot"));
            failedRead.Apply(Response(Snap("interrupted-read", "1", 0, 2, Item())));
            failedRead.Apply(Response("{\"kind\":\"snapshot\",\"ok\":false,\"error\":\"" + error + "\",\"message\":\"精确读取错误\"}"));
            failedRead.Tick(DateTime.UtcNow.AddSeconds(16));
            Check(failedRead.Error == error && failedRead.Message == "精确读取错误", "explicit snapshot failure is not overwritten by timeout: " + error);
            Check(!failedRead.InventorySnapshotCurrent && !failedRead.SnapshotLoading && !failedRead.NeedsInventorySnapshotRefresh && failedRead.Find("bag", 2, 0, 1001) == null,
                "failed snapshot retires assembly and automatic attempt without restoring old UID: " + error);
            Check(ReferenceEquals(failedRead.PendingRequest, pendingMutation), "failed read leaves immutable pending mutation intact: " + error);
            failedRead.Apply(Response(Snap("interrupted-read", "1", 1, 2, "")));
            Check(!failedRead.InventorySnapshotCurrent, "failed read's late tail cannot restore authority: " + error);
            Check(failedRead.Begin(new EndgameRequest("snapshot")), "explicit manual snapshot retry remains available: " + error);
            failedRead.Apply(Response(Snap("manual-read-recovery", "1", 0, 1, Item())));
            Check(failedRead.InventorySnapshotCurrent && ReferenceEquals(failedRead.PendingRequest, pendingMutation), "manual snapshot recovery does not resolve unrelated mutation: " + error);
        }
        Check(!s.NativeEquipmentStats, "new client with legacy server retains legacy stat overlay");
        Check(new EndgameRequest("hello").Json.Contains("endgame.native-stats.v1"), "new client explicitly offers native stat capability");
        var statState = new EndgameClientState(); statState.Begin(new EndgameRequest("hello"));
        statState.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}"));
        Check(statState.NativeEquipmentStats, "new client and server agree to native aggregated stat ownership");
        statState.Reset(); Check(!statState.NativeEquipmentStats, "disconnect clears native stat capability");
        statState.Begin(new EndgameRequest("hello"));
        statState.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.native-stats.v1\"]}"));
        Check(!statState.Negotiated && !statState.NativeEquipmentStats, "optional stat capability cannot authorize missing base protocol");
        statState.Apply(Response("{\"kind\":\"hello\",\"ok\":false,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}"));
        Check(!statState.NativeEquipmentStats, "rejected hello cannot enable native stat ownership");
        var lateStats = new EndgameClientState(); lateStats.Begin(new EndgameRequest("hello"));
        Check(!lateStats.Apply(Response(Snap("too-early", "1", 0, 1, Item()))) && !lateStats.HasSnapshot, "pre-hello snapshot cannot publish stale equipment overlays");
        lateStats.Apply(Response("{\"kind\":\"hello\",\"ok\":false,\"error\":\"busy\"}"));
        lateStats.Tick(DateTime.UtcNow.AddSeconds(3));
        Check(!lateStats.HelloSent && !lateStats.NativeEquipmentStats, "failed native refresh keeps overlay disabled and permits hello retry");
        lateStats.Begin(new EndgameRequest("hello"));
        lateStats.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}"));
        Check(lateStats.NativeEquipmentStats && lateStats.Find("bag", 2, 0, 1001) == null, "accepted capability alone cannot overlay before refreshed snapshot");
        lateStats.Apply(Response(Snap("fresh-native", "2", 0, 1, Item())));
        Check(lateStats.Find("bag", 2, 0, 1001) != null, "post-hello snapshot enables the negotiated overlay");
        lateStats.Reset(); lateStats.Begin(new EndgameRequest("hello"));
        lateStats.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}"));
        Check(lateStats.Negotiated && !lateStats.NativeEquipmentStats && !lateStats.HasSnapshot, "reconnecting to a legacy server never carries forward native-stat ownership");
        Reject(() => Response("{} {}"), "multiple values"); Reject(() => Response("{\"kind\":\"snapshot\",\"kind\":\"hello\"}"), "duplicates");
        Reject(() => Response("{\"kind\":\"hello\",\"extra\":0}"), "unknown fields"); Reject(() => Response("{\"kind\":\"hello\",\"ok\":1}"), "wrong bool");
        Reject(() => Response("{\"kind\":\"snapshot\",\"chunks\":-1}"), "negative chunks"); Reject(() => Response("{\"kind\":\"snapshot\",\"chunks\":129}"), "chunk limit");
        Reject(() => Response(Snap("a", "1", 0, 1, Item("-1"))), "negative UID"); Reject(() => Response(Snap("a", "1", 0, 1, Item("01"))), "noncanonical UID");
        Reject(() => Response(Snap("a", "1", 0, 1, Item("9223372036854775808"))), "64 bit overflow");
        Reject(() => Response(Snap("a", "1", 0, 1, Item().Replace("\"9007199254740993\"", "9007199254740993"))), "numeric UID");
        Reject(() => Response(Snap("a", "1", 0, 1, Item().Replace("1001", "2147483648"))), "int32 overflow");
        Reject(() => Response(Snap("a", "1", 0, 1, Item().Replace("1001", "1001.0"))), "fraction integer");
        Reject(() => Response("{\"kind\":\"hello\",\"message\":\"\\ud800\"}"), "invalid Unicode");
        Reject(() => Response("{\"kind\":\"hello\",\"message\":\"" + new string('x', 61440) + "\"}"), "payload limit");
        Reject(() => Response("{\"kind\":\"catalog.runes\",\"cursor\":28}"), "numeric cursor");
        Check(Response("{\"kind\":\"catalog.runes\",\"ok\":true,\"runes\":[{\"itemId\":1900028001,\"name\":\"角\",\"minLevel\":12},{\"itemId\":1900028028,\"name\":\"轸\",\"minLevel\":60}]}").Runes[1].MinLevel == 60, "rune minLevel survives wire DTO");
        Reject(() => Response("{\"kind\":\"catalog.runes\",\"runes\":[{\"itemId\":1900028001,\"minLevel\":\"12\"}]}"), "rune level requires integer");
        var rows = new List<string>(); for (int i = 0; i < 33; i++) rows.Add(Item((i + 1).ToString(), i));
        Reject(() => Response(Snap("a", "1", 0, 1, string.Join(",", rows))), "33 rows"); rows.RemoveAt(32);
        Check(Response(Snap("a", "1", 0, 1, string.Join(",", rows))).Items.Count == 32, "32 rows accepted");
        byte[] wire = Envelope("{\"kind\":\"hello\",\"ok\":true}"); Check(PrivateServerEndgameCodec.Decode(wire).OK, "wire decode");
        for (int size = 0; size < wire.Length; size++)
        { byte[] shortWire = new byte[size]; Buffer.BlockCopy(wire, 0, shortWire, 0, size); Reject(() => PrivateServerEndgameCodec.Decode(shortWire), "truncation " + size); }
        byte[] trailing = new byte[wire.Length + 1]; Buffer.BlockCopy(wire, 0, trailing, 0, wire.Length); Reject(() => PrivateServerEndgameCodec.Decode(trailing), "trailing bytes");
        wire[0] = 2; Reject(() => PrivateServerEndgameCodec.Decode(wire), "schema mismatch");
        Reject(() => PrivateServerEndgameCodec.Decode(new byte[] { 1, 0, 2, 0, 0xc0, 0xaf }), "invalid UTF8");
        s.Apply(Response(Snap("first", "9007199254740993", 0, 2, Item()))); Check(!s.HasSnapshot && s.Items.Count == 0 && s.Revision == "0", "atomic chunk staging");
        s.Apply(Response(Snap("first", "9007199254740993", 1, 2, Item("9007199254740994", 1))));
        Check(s.HasSnapshot && s.Items.Count == 2 && s.Items[0].UID == "9007199254740993", "exact64 snapshot");
        var old = s.Items; s.Apply(Response(Snap("old", "7", 0, 1, ""))); Check(ReferenceEquals(old, s.Items), "stale snapshot ignored");
        s.Apply(Response(Snap("mix", s.Revision, 0, 2, Item()))); Reject(() => s.Apply(Response(Snap("mix", s.Revision, 2, 3, Item("4")))), "mixed chunk envelope"); Check(ReferenceEquals(old, s.Items), "failure retains prior snapshot");
        s.Apply(Response(Snap("duplicate", s.Revision, 0, 2, Item()))); Reject(() => s.Apply(Response(Snap("duplicate", s.Revision, 1, 2, Item()))), "duplicate UID across chunks");
        Check(s.Find("bag", 2, 0, 1001) == null, "failed replacement retains historical rows without allowing their stale UID overlay");
        s.Apply(Response(Snap("recovered", s.Revision, 0, 1, Item())));
        Check(s.Find("bag", 2, 0, 1001) != null && s.Find("bag", 0, 0, 1001) == null && s.Find("bag", 2, 0, 1002) == null, "location and template matching");
        var cmd = new EndgameRequest("socket.insert", s.Revision, old[0].UID, "9223372036854775806", 1); string serialized = cmd.Json;
        Check(s.Begin(cmd) && !s.Begin(cmd), "one pending mutation");
        s.Apply(Response("{\"kind\":\"socket.insert\",\"requestId\":\"" + cmd.RequestID + "\",\"ok\":false,\"error\":\"pending\"}"));
        Check(ReferenceEquals(cmd, s.PendingRequest) && serialized == cmd.Json, "pending immutable retry");
        s.Apply(Response("{\"kind\":\"socket.insert\",\"requestId\":\"" + cmd.RequestID + "\",\"ok\":false,\"error\":\"storage\"}")); Check(ReferenceEquals(cmd, s.PendingRequest), "uncertain storage retains exact nonce");
        s.Apply(Response("{\"kind\":\"socket.insert\",\"requestId\":\"" + cmd.RequestID + "\",\"ok\":true,\"revision\":\"9007199254740995\"}")); Check(!s.Pending, "receipt clears pending");
        var quote = new EndgameRequest("cube.quote", s.Revision, recipeID: 11001); s.Begin(quote);
        s.Apply(Response("{\"kind\":\"cube.quote\",\"requestId\":\"" + quote.RequestID + "\",\"ok\":false,\"error\":\"pending\"}")); Check(!s.QuotePending, "quote error releases");
        s.Begin(new EndgameRequest("cube.quote", s.Revision, recipeID: 11001)); s.Tick(DateTime.UtcNow.AddSeconds(16)); Check(!s.QuotePending, "quote timeout releases");
        var page = new EndgameRequest("catalog", catalog: "runes"); s.Begin(page); Check(s.CatalogRequested("runes"), "catalog pending");
        s.Apply(Response("{\"kind\":\"catalog.runes\",\"requestId\":\"" + page.RequestID + "\",\"ok\":false,\"error\":\"pending\"}")); Check(!s.CatalogRequested("runes"), "catalog error releases");
        s.Begin(page); s.Tick(DateTime.UtcNow.AddSeconds(16)); Check(!s.CatalogRequested("runes"), "catalog timeout releases");
        var ids = new List<string> { "1", "2" }; var command = new EndgameRequest("cube.quote", "0", recipeID: 1009, itemUIDs: ids); ids[0] = "3"; Check(!command.Json.Contains("\"3\""), "command deep copy");
        Reject(() => new EndgameRequest("cube.quote", itemUIDs: new[] { "1", "1" }), "duplicate rune UIDs"); Reject(() => new EndgameRequest("cube.quote", itemUIDs: new[] { "wrong" }), "nondecimal rune UID");
        var journal = new EndgameRetryJournal(); string key = EndgameRetryJournal.Scope("host:1", "account", "hero"); journal.Remember(key, cmd);
        Check(ReferenceEquals(journal.Find(key), cmd) && journal.Find(EndgameRetryJournal.Scope("host:1", "account", "other")) == null, "recovery scoped character");
        s.Reset(); Check(!s.Negotiated && !s.Pending && !s.HasSnapshot && s.Items.Count == 0 && s.Quote == null && s.Rift == null, "disconnect clears cache"); Check(!s.Begin(cmd), "reset gates commands");
        s.Begin(new EndgameRequest("hello")); s.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\"]}")); s.RestoreUncertainRequest(journal.Find(key)); Check(ReferenceEquals(s.PendingRequest, cmd), "reconnect keeps nonce without executing");
        journal.Complete(key, "wrong"); Check(journal.Find(key) != null, "wrong receipt preserves journal"); journal.Complete(key, cmd.RequestID); Check(journal.Find(key) == null, "terminal receipt removes journal");
        var g = new EndgameClientState(); g.Begin(new EndgameRequest("hello")); g.Apply(Fixture(args[0], "hello"));
        g.Apply(Fixture(args[0], "snapshot-0")); Check(!g.HasSnapshot, "Go first chunk staged"); g.Apply(Fixture(args[0], "snapshot-1"));
        Check(g.Items.Count == 2 && g.Items[0].UID == "9007199254740993" && g.Items[0].Sockets[0].UID == "9223372036854775806" && g.Items[0].Tab == 0 && g.Items[1].Slot == 25, "Go64 tab and slot25");
        g.Begin(new EndgameRequest("catalog", catalog: "runes")); g.Apply(Fixture(args[0], "runes")); Check(g.Runes.Count == 1 && !g.CatalogHasMore("runes"), "Go catalog terminal cursor");
        var receipt = Fixture(args[0], "mutation"); g.Begin(new EndgameRequest("socket.insert", g.Revision, "9007199254740993", "9223372036854775806", requestID: receipt.RequestID)); g.Apply(receipt); Check(!g.Pending && g.Revision == "4", "Go mutation receipt");
        g.Apply(Fixture(args[0], "rift")); Check(g.Rift.ProgressMax == 10000 && g.Rift.Members[0].CharID == "9007199254740993", "Go rift/member schema");
        g.Apply(Fixture(args[0], "audit-snapshot_empty")); Check(g.Items.Count == 0 && g.ActiveFormations.Count == 1 && g.ActiveFormations[0].Active && g.Materials.Count == 1, "Go empty snapshot with active formations");
        g.Apply(Response(Snap("clear-wallet", "19", 0, 1, ""))); Check(g.ActiveFormations.Count == 0 && g.Materials.Count == 0, "empty snapshot clears wallet and arrays");
        var extra = State(); extra.Apply(Response(Snap("repeat", "1", 0, 2, Item()))); Reject(() => extra.Apply(Response(Snap("repeat", "1", 0, 2, Item()))), "repeat first chunk");
        extra.Apply(Response(Snap("timeout", "1", 0, 2, Item()))); extra.Tick(DateTime.UtcNow.AddSeconds(16)); Check(!extra.SnapshotLoading && extra.Error == "timeout", "snapshot timeout");
        Reject(() => extra.Apply(Response(Snap("badtab", "1", 0, 1, Item().Replace("\"tab\":2", "\"tab\":4")))), "native tab bound");
        Reject(() => Response("{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"keyCount\":-1}}"), "negative key count");
        Reject(() => Response("{\"kind\":\"snapshot\",\"materials\":[{\"itemId\":1,\"count\":-1}]}"), "negative material count");
        var q = new EndgameRequest("cube.quote", "0", recipeID: 11001); extra.Begin(q); extra.InvalidateQuote();
        extra.Apply(Response("{\"kind\":\"cube.quote\",\"ok\":true,\"requestId\":\"" + q.RequestID + "\",\"quote\":{\"quoteId\":\"old\",\"recipeId\":11001,\"revision\":\"0\",\"expiresAt\":\"2099-01-01T00:00:00Z\"}}")); Check(extra.Quote == null, "late quote ignored");
        extra.Apply(Response("{\"kind\":\"cube.quote\",\"ok\":true,\"quote\":{\"quoteId\":\"unsolicited\",\"recipeId\":11001,\"revision\":\"0\",\"expiresAt\":\"2099-01-01T00:00:00Z\"}}")); Check(extra.Quote == null, "unrequested quote rejected");
        var full = Fixture(args[0], "full-fields");
        Check(full.Items[0].Profession == 4 && full.Items[0].Level == 50 && full.Items[0].Tab == 2 && full.Items[0].Bound && full.Items[0].Locked, "Go nonzero numeric item fields");
        Check(full.Items[0].Sockets[0].Index == 1 && full.Items[0].Sockets[0].UID == "9223372036854775807" && full.Items[0].Stats[0].Mode == 1, "Go nonzero socket/stat fields");
        Check(full.Runes[0].Mansion == "角" && full.Formations[0].Professions == 31 && full.Formations[0].Slots[1] == 25 && full.Formations[0].Runes.Count == 2, "Go rune/formation types");
        Check(full.Recipes[0].TargetRequired && full.Recipes[0].Money == "9007199254740993" && full.Recipes[0].Materials[0].Owned == 99 && full.Materials[0].Count == 7, "Go recipe/material types");
        Check(full.Quote.QuoteID == "quote-full-fixture" && full.Quote.RecipeID == 11001 && full.Quote.Materials[0].Owned == 99, "Go quote types");
        Check(full.Rift.Auras[0].NativeEntityID == -1 && full.Rift.Globes[0].Value == 900 && full.Rift.Globes[0].X == 321 && full.Rift.Claimable && full.Rift.Members[0].Claimed, "Go aura/globe/claim types");
        Check(full.Rift.Tier == 150 && full.Rift.FloorIndex == 3 && full.Rift.Seed == "18446744073709551615" && full.Rift.GeneratorVersion == 1, "production Go fixture seeded difficulty/floor fields");
        Check(full.Rift.CanAdvance && full.Rift.Transitioning && full.Rift.MaxTier == 150 && full.Rift.UnlockedTier == 150 && full.Rift.ExitX == 720 && full.Rift.ExitY == 960 && full.Rift.MapName == "试炼秘林", "production Go fixture floor exit/limits fields");
        Check(full.Rift.Auras[0].Affixes[0] == "烈焰" && full.Rift.Telegraphs[0].Ability == "震环冲击" && full.Rift.Telegraphs[0].RemainingMS == 1800 && full.Rift.Telegraphs[0].InnerRadius == 85 && full.Rift.Telegraphs[0].EndX == 300, "production Go fixture readable combat fields");
        Check(full.Chunk == 1 && full.Chunks == 2 && full.Cursor == "32" && full.NextCursor == "64" && full.Result["status"] == "saved", "Go envelope/result types");
        Reject(() => Response(Snap("badprofession", "1", 0, 1, Item().Replace("\"itemId\":1001", "\"profession\":\"4\",\"itemId\":1001"))), "string profession rejected");
        var transient = new EndgameClientState();
        for (int attempt = 0; attempt < 3; attempt++) {
            transient.Begin(new EndgameRequest("hello"));
            transient.Apply(Response("{\"kind\":\"hello\",\"ok\":false,\"error\":\"storage\"}"));
            transient.Tick(DateTime.UtcNow.AddSeconds(1)); Check(transient.HelloSent, "hello retry cooldown");
            transient.Tick(DateTime.UtcNow.AddSeconds(3)); Check(transient.HelloSent == (attempt == 2), "hello retry bounded to three attempts");
        }
        Check(!transient.Negotiated && !transient.Pending, "hello never creates mutation");
        foreach (bool sendFailed in new[] { false, true })
        {
            var missingHello = new EndgameClientState();
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var request = new EndgameRequest("hello"); missingHello.Begin(request);
                DateTime sentAt = DateTime.UtcNow;
                if (sendFailed) missingHello.NoteTransportFailure("injected send failure", request);
                DateTime timeoutAt = sentAt.AddSeconds(16);
                if (!sendFailed)
                {
                    missingHello.Tick(sentAt.AddSeconds(14));
                    Check(missingHello.HelloSent && missingHello.Error != "timeout", "lost/map-transfer hello waits for response deadline");
                    missingHello.Tick(timeoutAt);
                    Check(missingHello.HelloSent && missingHello.Error == "timeout", "lost hello enters cooldown without resending each frame");
                }
                DateTime retryAt = sendFailed ? sentAt : timeoutAt;
                missingHello.Tick(retryAt.AddSeconds(1));
                Check(missingHello.HelloSent, "failed or missing hello observes two-second cooldown");
                missingHello.Tick(retryAt.AddSeconds(3));
                Check(missingHello.HelloSent == (attempt == 2), "failed or missing hello stops after three attempts per connection");
            }
            missingHello.Tick(DateTime.UtcNow.AddHours(1));
            Check(missingHello.HelloSent && !missingHello.Negotiated && !missingHello.Pending, "exhausted hello retries never create a mutation or restart later");
            missingHello.Reset(); missingHello.Tick(DateTime.UtcNow.AddHours(2));
            Check(!missingHello.HelloSent && !missingHello.NativeEquipmentStats && missingHello.Error == "", "disconnect clears old response deadlines and retry budget");
            var freshHello = new EndgameRequest("hello"); missingHello.Begin(freshHello);
            missingHello.NoteTransportFailure("new connection", freshHello); missingHello.Tick(DateTime.UtcNow.AddSeconds(3));
            Check(!missingHello.HelloSent, "new connection gets a fresh bounded hello retry budget");
        }
        var refreshState = new EndgameClientState(); refreshState.Begin(new EndgameRequest("hello"));
        refreshState.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}"));
        refreshState.Begin(new EndgameRequest("hello"));
        Check(!refreshState.Apply(Response("{\"kind\":\"hello\",\"ok\":false,\"error\":\"busy\"}")) && refreshState.Negotiated && refreshState.NativeEquipmentStats, "failed rehello preserves the server's previously accepted native renderer");
        refreshState.Tick(DateTime.UtcNow.AddMinutes(1));
        Check(refreshState.HelloSent && refreshState.NativeEquipmentStats, "failed refresh cannot switch back to duplicate legacy overlays or start background retry spam");
        var delayedHello = new EndgameClientState(); delayedHello.Begin(new EndgameRequest("hello"));
        delayedHello.Tick(DateTime.UtcNow.AddSeconds(16));
        delayedHello.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.native-stats.v1\"]}"));
        delayedHello.Tick(DateTime.UtcNow.AddMinutes(1));
        Check(delayedHello.HelloSent && delayedHello.NativeEquipmentStats, "late successful hello cancels pending cooldown retry");
        var extended = Response("{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"runId\":\"seeded\",\"tier\":150,\"floorIndex\":3,\"seed\":\"18446744073709551615\",\"generatorVersion\":1,\"mapName\":\"试炼地图\",\"canAdvance\":true,\"transitioning\":false,\"exitX\":700,\"exitY\":850,\"maxTier\":150,\"unlockedTier\":150,\"auras\":[{\"entityId\":\"4294967295\",\"class\":2,\"affixes\":[\"烈焰\",\"震环\"]}],\"telegraphs\":[{\"id\":\"tell-1\",\"entityId\":\"4294967295\",\"ability\":\"震环冲击\",\"shape\":\"ring\",\"x\":100,\"y\":200,\"endX\":300,\"endY\":400,\"radius\":220,\"innerRadius\":85,\"remainingMS\":1800}]}}").Rift;
        Check(extended.Tier == 150 && extended.FloorIndex == 3 && extended.Seed == "18446744073709551615" && extended.GeneratorVersion == 1, "difficulty and floor preserve exact unsigned seed independently");
        Check(extended.CanAdvance && !extended.Transitioning && extended.ExitX == 700 && extended.ExitY == 850 && extended.MaxTier == 150 && extended.UnlockedTier == 150, "optional exit and selection metadata types");
        Check(extended.Auras[0].Affixes[1] == "震环" && extended.Telegraphs[0].InnerRadius == 85 && extended.Telegraphs[0].RemainingMS == 1800 && extended.Telegraphs[0].Shape == "ring", "affix and telegraph DTO projection");
        Check(Response("{\"kind\":\"rift.state\",\"rift\":{\"tier\":50}}").Rift.FloorIndex == 0, "legacy floorless DTO remains readable");
        foreach (string seed in new[] { "01", "-1", "18446744073709551616", "1.0", "+1" })
            Reject(() => Response("{\"kind\":\"rift.state\",\"rift\":{\"seed\":\"" + seed + "\"}}"), "noncanonical or overflowing seed");
        Reject(() => Response("{\"kind\":\"rift.state\",\"rift\":{\"seed\":42}}"), "numeric seed loses uint64 safety");
        Reject(() => Response("{\"kind\":\"rift.state\",\"rift\":{\"tier\":151}}"), "difficulty over 150");
        Reject(() => Response("{\"kind\":\"rift.state\",\"rift\":{\"floorIndex\":-1}}"), "negative run floor");
        Reject(() => Response("{\"kind\":\"rift.state\",\"rift\":{\"unlockedTier\":151}}"), "invalid tier unlock metadata");
        string tell = "{\"id\":\"tell-1\",\"entityId\":\"1\",\"shape\":\"circle\",\"radius\":105,\"remainingMS\":1500}";
        foreach (string invalid in new[] { tell.Replace("circle", "triangle"), tell.Replace("105", "-1"), tell.Replace("1500", "-1"), tell.Replace("\"1\"", "\"4294967296\""), tell.Replace("1500", "1500,\"innerRadius\":106") })
            Check(Response("{\"kind\":\"rift.state\",\"rift\":{\"telegraphs\":[" + invalid + "]}}").Rift.Telegraphs.Count == 0, "invalid optional danger geometry isolated");
        var tooManyTells = new string[129]; for (int i = 0; i < tooManyTells.Length; i++) tooManyTells[i] = tell;
        Reject(() => Response("{\"kind\":\"rift.state\",\"rift\":{\"telegraphs\":[" + string.Join(",", tooManyTells) + "]}}"), "danger list capped before publication");
        var advance = new EndgameRequest("rift.advance", runID: "seeded", floorIndex: 3);
        Check(advance.Mutates && advance.Json.Contains("\"floorIndex\":3") && !advance.Json.Contains("tier"), "floor command guards floor without changing difficulty");
        Reject(() => new EndgameRequest("rift.advance", runID: "seeded"), "advance requires positive observed floor");
        Reject(() => new EndgameRequest("rift.advance", floorIndex: 3), "advance requires observed run");
        Reject(() => new EndgameRequest("rift.start", tier: 151), "native command tier maximum");
        Check(new EndgameRequest("rift.start", tier: 150).Json.Contains("\"tier\":150"), "native command accepts tier150");
        var advancedState = State(); advancedState.Begin(advance);
        advancedState.Apply(Response("{\"kind\":\"rift.advance\",\"requestId\":\"" + advance.RequestID + "\",\"ok\":false,\"error\":\"busy\"}"));
        Check(ReferenceEquals(advancedState.PendingRequest, advance), "busy floor transition keeps immutable retry");
        advancedState.Apply(Response("{\"kind\":\"rift.state\",\"requestId\":\"" + advance.RequestID + "\",\"ok\":true}"));
        Check(advancedState.Pending, "wrong-kind floor receipt cannot complete transition");
        advancedState.Apply(Response("{\"kind\":\"rift.advance\",\"requestId\":\"" + advance.RequestID + "\",\"ok\":true}"));
        Check(!advancedState.Pending, "correlated advance receipt completes transition");
        var skillFixture = Fixture(args[0], "skill-ranks-synergies-v5");
        Check(skillFixture.Kind == "skill.ranks" && skillFixture.SkillRanks.Count == 2, "Go production skill projection reaches actual wire decoder");
        var actualSkill = skillFixture.SkillRanks[0];
        Check(actualSkill.ID == 10401 && actualSkill.HardRank == 1 && actualSkill.NextEffectiveRank == "2" && actualSkill.NextRequiredLevel == 11,
            "Go next-investment rank and player-level requirement remain exact");
        var actualSynergy = actualSkill.TrainingSynergies[0];
        Check(actualSynergy.SourceID == 10402 && actualSynergy.SourceName == "火球术" && actualSynergy.PerPointValue == 100 &&
            actualSynergy.InvestedHardPoints == 3 && actualSynergy.ContributionValue == 300 && !actualSynergy.EquipmentApplies,
            "Go active attack source hard points and per-point damage coefficient survive encoding");
        Check(actualSkill.NativeModifierLines.Count == 1 && actualSkill.CurrentEffectLines.Count == 4 &&
            !string.Join("", actualSkill.CurrentEffectLines).Contains("毫秒"), "Go compact effects and separate native modifiers decode");
        var unlearnedSkill = skillFixture.SkillRanks[1];
        Check(!unlearnedSkill.CanCast && unlearnedSkill.CurrentEffectLines.Count == 0 && unlearnedSkill.NextEffectLines.Count > 0 &&
            unlearnedSkill.TrainingSynergies[0].InvestedHardPoints == 0, "Go unlearned source remains in first-investment planning view");
        var skillState = new EndgameClientState(); skillState.Begin(new EndgameRequest("hello"));
        skillState.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\"]}"));
        Check(skillState.Apply(skillFixture) && skillState.SkillRanksRevision == "1" && skillState.UnspentSkillPoints == 93 &&
            skillState.CanInvestSkill(10401) && !skillState.CanInvestSkill(10410), "complete Go wire fixture publishes authoritative investment guards");
        var equippedSynergyFixture = Fixture(args[0], "skill-synergy-equipped-v11");
        Check(equippedSynergyFixture.Kind == "skill.ranks" && equippedSynergyFixture.SkillRanks.Count == 4,
            "Migration74 production PostgreSQL/equipment projection reaches unchanged wire decoder");
        var equippedSynergyState = new EndgameClientState(); equippedSynergyState.Begin(new EndgameRequest("hello"));
        equippedSynergyState.Apply(Response("{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.growth.v5\"]}"));
        Check(equippedSynergyState.Apply(equippedSynergyFixture), "Complete equipment-aware wire snapshot publishes through production state");
        var equippedSource = equippedSynergyState.SkillRank(10402).TrainingSynergies[0];
        Check(equippedSource.SourceID == 10401 && equippedSource.InvestedHardPoints == 3 && equippedSource.SourceEffectiveRank == "5" &&
            equippedSource.PerPointValue == 300 && equippedSource.ContributionValue == 1500 && equippedSource.EquipmentApplies,
            "Actual equipped item2018 source3 plus2 contributes five levels at beneficiary3percent");
        var unlearnedSource = equippedSynergyState.SkillRank(10410).TrainingSynergies[0];
        Check(unlearnedSource.SourceID == 10409 && unlearnedSource.InvestedHardPoints == 0 && unlearnedSource.SourceEffectiveRank == "0" &&
            unlearnedSource.ContributionValue == 0 && unlearnedSource.EquipmentApplies, "Unlearned equipment-only source stays zero through actual wire encoding");
        Console.WriteLine("PASS " + assertions + " endgame contracts including real Go-produced wire fixtures.");
    }
}
