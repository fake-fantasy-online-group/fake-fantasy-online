using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class NetClient
    {
        internal sealed class Buf { public void Bytes(byte[] payload) { } }
        private void Send(ushort op, Action<Buf> write) { write(new Buf()); }
    }
}
class RiftPageContract
{
    static int checks;
    static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    static void Reject(Action action, string message)
    {
        try { action(); } catch (Exception e) when (e is FormatException || e is ArgumentException) { checks++; return; }
        throw new Exception("Accepted " + message);
    }
    static EndgameResponse Response(object value) => EndgameResponse.Parse(BoundedJson.Serialize(value));
    static Dictionary<string, object> Dict(params object[] pairs)
    { var d = new Dictionary<string, object>(); for (int i = 0; i < pairs.Length; i += 2) d.Add((string)pairs[i], pairs[i + 1]); return d; }
    static EndgameClientState State(bool pages = true)
    {
        var state = new EndgameClientState(); state.Begin(new EndgameRequest("hello"));
        state.Apply(Response(Dict("kind", "hello", "ok", true, "capabilities", new object[] { "endgame.v1", "endgame.rift-portals.v1" }, "result", pages ? Dict("riftPages", "1") : Dict())));
        Check(state.Negotiated && !state.RiftPagesV1, "offer is not accepted capability");
        if (pages)
        {
            Check(state.NeedsRiftPageNegotiation, "offer schedules lightweight opt-in");
            var optIn = new EndgameRequest("capabilities"); Check(!optIn.Mutates && state.Begin(optIn), "capability request is read-only");
            state.Apply(Response(Dict("kind", "capabilities", "requestId", "not-the-pending-request", "ok", true, "capabilities", new object[] { "endgame.v1", "endgame.rift-pages.v1" })));
            Check(!state.RiftPagesV1, "uncorrelated grant ignored");
            state.Apply(Response(Dict("kind", "capabilities", "requestId", optIn.RequestID, "ok", true, "capabilities", new object[] { "endgame.v1", "endgame.rift-pages.v1" })));
            Check(state.RiftPagesV1 && !state.NeedsRiftPageNegotiation, "correlated capability grant only");
        }
        return state;
    }
    static Dictionary<string, object> Snapshot(int population, string kind = "rift.state", string request = "", int revision = 7, long now = 100000)
    {
        var auras = new List<object>();
        for (int i = 1; i <= population; i++) auras.Add(Dict("entityId", i.ToString(), "class", 0, "originalName", "洞天怪物🌊"));
        return Dict("kind", kind, "ok", true, "requestId", request, "revision", revision.ToString(),
            "rift", Dict("runId", "dense-run", "revision", revision.ToString(), "phase", "active", "mapId", 1, "floorIndex", 0, "serverNowMS", now, "auras", auras));
    }
    static List<Dictionary<string, object>> Pages(object response, string id = "unique-snapshot")
    {
        var raw = Encoding.UTF8.GetBytes(BoundedJson.Serialize(response)); var pages = new List<Dictionary<string, object>>();
        int count = (raw.Length + EndgameResponse.RiftPageBytes - 1) / EndgameResponse.RiftPageBytes;
        for (int n = 0; n < count; n++)
        {
            int offset = n * EndgameResponse.RiftPageBytes, size = Math.Min(raw.Length - offset, EndgameResponse.RiftPageBytes);
            pages.Add(Dict("kind", "rift.page", "ok", true, "snapshotId", id, "chunk", n, "chunks", count, "totalBytes", raw.Length, "data", Convert.ToBase64String(raw, offset, size)));
        }
        return pages;
    }
    static void Apply(EndgameClientState state, List<Dictionary<string, object>> pages)
    { foreach (var page in pages) state.Apply(Response(page)); }
    static void Main(string[] args)
    {
        var hello = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(new EndgameRequest("hello").Json);
        Check(((IReadOnlyList<object>)hello["capabilities"]).Count == 8, "initial hello remains old-server-safe eight capabilities");
        var opt = (IReadOnlyDictionary<string, object>)BoundedJson.Parse(new EndgameRequest("capabilities").Json);
        Check(((IReadOnlyList<object>)opt["capabilities"]).Count == 2, "lightweight opt-in stays below old cap count guard");
        var legacy = State(false); Check(!legacy.NeedsRiftPageNegotiation && !legacy.Begin(new EndgameRequest("capabilities")), "old server never receives new operation");
        var dense = Snapshot(3000); var frames = Pages(dense);
        Check(frames.Count > 1, "dense snapshot actually exceeds one frame");
        Reject(() => EndgameResponse.Parse(BoundedJson.Serialize(dense)), "oversized ordinary frame");
        Reject(() => legacy.Apply(Response(frames[0])), "unnegotiated page");
        var state = State(); state.Apply(Response(Snapshot(1, revision: 1, now: 90000))); var previous = state.Rift;
        var start = new EndgameRequest("rift.start"); state.Begin(start);
        frames = Pages(Snapshot(3000, "rift.start", start.RequestID)); long version = state.Version;
        for (int i = 0; i < frames.Count - 1; i++)
        {
            string json = BoundedJson.Serialize(frames[i]);
            Check(Encoding.UTF8.GetByteCount(json) <= EndgameResponse.MaximumBytes, "every actual JSON frame fits unchanged 60KiB bound");
            state.Apply(EndgameResponse.Parse(json));
            Check(ReferenceEquals(previous, state.Rift) && state.Pending && state.Version == version, "partial page has no presentation or receipt side effects");
        }
        state.Apply(Response(frames[frames.Count - 1]));
        Check(!state.Pending && state.Rift.Auras.Count == 3000 && state.Rift.Auras[2999].OriginalName == "洞天怪物🌊" && !state.RiftSnapshotLoading, "complete 3000-monster UTF8 snapshot publishes with original receipt atomically");
        Check(state.Rift.Auras.Select(x => x.EntityID).Distinct().Count() == 3000, "all auras preserved, not truncated to 320 or 2048");
        version = state.Version; Apply(state, frames); Check(state.Version == version, "completed snapshot replay ignored");
        var next = Pages(Snapshot(3000, revision: 8, now: 100100), "next"); previous = state.Rift;
        state.Apply(Response(next[0])); Reject(() => state.Apply(Response(next[2])), "skipped page");
        Check(!state.RiftSnapshotLoading && ReferenceEquals(previous, state.Rift), "out-of-order assembly fully discarded");
        Apply(state, next); Check(ReferenceEquals(previous, state.Rift), "retired interrupted snapshot cannot restart");
        next = Pages(dense, "duplicate"); state.Apply(Response(next[0])); Reject(() => state.Apply(Response(next[0])), "duplicate page zero");
        next = Pages(dense, "wrong-total"); state.Apply(Response(next[0])); next[1]["totalBytes"] = (int)next[1]["totalBytes"] + 1;
        Reject(() => state.Apply(Response(next[1])), "inconsistent total bytes");
        next = Pages(dense, "bad-base64"); next[0]["data"] = "%%%"; Reject(() => state.Apply(Response(next[0])), "malformed base64");
        next = Pages(dense, "short-page"); next[0]["data"] = Convert.ToBase64String(new byte[1]); Reject(() => state.Apply(Response(next[0])), "short non-final page");
        next = Pages(dense, "business-envelope"); next[0]["requestId"] = start.RequestID; Reject(() => Response(next[0]), "business fields in transport-only envelope");
        next = Pages(dense, "over-budget"); next[0]["totalBytes"] = EndgameResponse.MaximumRiftSnapshotBytes + 1;
        Reject(() => Response(next[0]), "oversized aggregate before allocation");
        next = Pages(dense, "wrong-count"); next[0]["chunks"] = 30; Reject(() => Response(next[0]), "page count not derived from byte count");
        var duplicate = Snapshot(3000); var dupAuras = (List<object>)((Dictionary<string, object>)duplicate["rift"])["auras"];
        ((Dictionary<string, object>)dupAuras[2999])["entityId"] = "1";
        Apply(state, Pages(duplicate, "duplicate-aura"));
        Check(state.Rift.Auras.Count == 2998 && state.Rift.SkippedVisualEntries == 2, "complete aggregate isolates duplicate optional aura across pages");
        previous = state.Rift;
        next = Pages(dense, "timeout"); state.Apply(Response(next[0])); state.Tick(DateTime.UtcNow.AddSeconds(16));
        Check(!state.RiftSnapshotLoading && state.Error == "timeout" && ReferenceEquals(previous, state.Rift), "timeout keeps last complete state");
        Apply(state, next); Check(!state.RiftSnapshotLoading && ReferenceEquals(previous, state.Rift), "timeout retires trailing pages");
        next = Pages(dense, "scene-change"); state.Apply(Response(next[0])); state.NoteRiftSceneChange(); Apply(state, next);
        Check(!state.RiftSnapshotLoading && ReferenceEquals(previous, state.Rift), "scene change cannot complete previous assembly");
        next = Pages(dense, "reset"); state.Apply(Response(next[0])); state.Reset();
        Check(!state.RiftSnapshotLoading && !state.RiftPagesV1 && !state.RiftPagesAvailable && state.Rift == null, "connection reset clears capability and assembly");
        var nonRift = Dict("kind", "snapshot", "ok", true); Reject(() => Apply(State(), Pages(nonRift, "non-rift")), "non-rift nested document");
        var small = Pages(Snapshot(1), "single-page"); var one = State(); Apply(one, small); Check(one.Rift.Auras.Count == 1, "single-page snapshot valid");
        var tooLarge = State(); var pending = new EndgameRequest("rift.advance", runID: "dense-run", floorIndex: 1);
        tooLarge.Begin(pending); var interruptedByError = Pages(dense, "size-failure"); tooLarge.Apply(Response(interruptedByError[0]));
        tooLarge.Apply(Response(Dict("kind", "rift.advance", "requestId", pending.RequestID, "ok", false, "error", "snapshot_too_large")));
        Apply(tooLarge, interruptedByError);
        Check(!tooLarge.RiftSnapshotLoading && tooLarge.Rift == null, "size failure retires old partial document and all trailing pages");
        Check(!tooLarge.Pending && tooLarge.Error == "snapshot_too_large" && tooLarge.Begin(new EndgameRequest("rift.leave", runID: "dense-run")), "explicit size failure releases original receipt and permits leaving");
        var supersede = State(); next = Pages(dense, "superseded"); supersede.Apply(Response(next[0]));
        supersede.Apply(Response(Snapshot(2, revision: 9, now: 200000))); Apply(supersede, next);
        Check(!supersede.RiftSnapshotLoading && supersede.Rift.Auras.Count == 2, "new whole snapshot supersedes and retires partial pages");
        var sampled = Response(Snapshot(1)).Rift; var clock = new EndgameRiftHazardState();
        clock.Apply(sampled, 5000, 4000);
        Check(clock.ServerTimeMS(5000) == sampled.ServerNowMS + 4000, "slow assembly does not extend authoritative effect clock");
        Reject(() => clock.Apply(sampled, 5000, -1), "negative snapshot age");
        Reject(() => clock.Apply(sampled, 5000, double.NaN), "nonfinite snapshot age");
        var retry = new EndgameClientState(); retry.Begin(new EndgameRequest("hello"));
        retry.Apply(Response(Dict("kind", "hello", "ok", true, "capabilities", new object[] { "endgame.v1" }, "result", Dict("riftPages", "1"))));
        var retryRequest = new EndgameRequest("capabilities"); retry.Begin(retryRequest);
        retry.Apply(Response(Dict("kind", "capabilities", "requestId", retryRequest.RequestID, "ok", false, "error", "busy")));
        Check(!retry.RiftPagesV1 && !retry.NeedsRiftPageNegotiation && retry.Negotiated, "busy opt-in preserves base negotiation with bounded cooldown");
        retry.Tick(DateTime.UtcNow.AddSeconds(3)); Check(retry.NeedsRiftPageNegotiation, "transient capability failure schedules retry without another hello");
        for (int attempt = 1; attempt < 3; attempt++)
        {
            retryRequest = new EndgameRequest("capabilities"); retry.Begin(retryRequest);
            retry.Apply(Response(Dict("kind", "capabilities", "requestId", retryRequest.RequestID, "ok", false, "error", "busy")));
            retry.Tick(DateTime.UtcNow.AddSeconds(3));
        }
        Check(!retry.NeedsRiftPageNegotiation, "failed capability attempts are bounded");
        retry.Begin(new EndgameRequest("hello"));
        retry.Apply(Response(Dict("kind", "hello", "ok", true, "capabilities", new object[] { "endgame.v1" }, "result", Dict("riftPages", "1"))));
        Check(retry.NeedsRiftPageNegotiation, "explicit refresh can recover exhausted opt-in");
        if (args.Length > 0)
        {
            var interop = State(); int received = 0;
            foreach (string line in File.ReadAllLines(args[0]))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string hex = line.Trim(); var bytes = new byte[hex.Length / 2];
                for (int n = 0; n < bytes.Length; n++) bytes[n] = Convert.ToByte(hex.Substring(n * 2, 2), 16);
                Check(bytes.Length >= 6 && bytes[0] == 0xd0 && bytes[1] == 0x80, "actual Go wire opcode");
                var payload = new byte[bytes.Length - 2]; Buffer.BlockCopy(bytes, 2, payload, 0, payload.Length);
                interop.Apply(PrivateServerEndgameCodec.Decode(payload)); received++;
            }
            Check(received > 1 && interop.Rift != null && interop.Rift.Auras.Count == int.Parse(args[1]) && !interop.RiftSnapshotLoading, "actual Go-produced pages decode to every expected aura");
        }
        Console.WriteLine("Rift page contracts passed: " + checks + " assertions.");
    }
}
