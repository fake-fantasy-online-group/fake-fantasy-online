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
    static void Main(string[] args)
    {
        var s = State(); Check(s.Negotiated, "hello negotiated");
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
        Check(s.Find("bag", 2, 0, 1001) != null && s.Find("bag", 0, 0, 1001) == null && s.Find("bag", 2, 0, 1002) == null, "location and template matching");
        var cmd = new EndgameRequest("socket.insert", s.Revision, old[0].UID, "9223372036854775806", 1); string serialized = cmd.Json;
        Check(s.Begin(cmd) && !s.Begin(cmd), "one pending mutation");
        s.Apply(Response("{\"kind\":\"socket.insert\",\"requestId\":\"" + cmd.RequestID + "\",\"ok\":false,\"error\":\"pending\"}"));
        Check(ReferenceEquals(cmd, s.PendingRequest) && serialized == cmd.Json, "pending immutable retry");
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
        Console.WriteLine("PASS " + assertions + " endgame contracts including real Go-produced wire fixtures.");
    }
}
