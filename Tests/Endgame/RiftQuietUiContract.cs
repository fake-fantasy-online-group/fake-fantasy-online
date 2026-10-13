using System;
using System.Reflection;
using System.IO;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

partial class EndgameUiContract
{
    const string ArrivalJson = "{\"id\":\"arrival-1\",\"seed\":\"18446744073709551615\",\"anchorCharId\":\"42\",\"x\":720,\"y\":960,\"startedAtMS\":1791892800000,\"deadlineMS\":1791892805000,\"durationMS\":5000,\"remainingMS\":5000}";
    static void QuietNegotiate(Client c)
    {
        c.Endgame.Begin(new EndgameRequest("hello"));
        Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-portals.v1\",\"endgame.rift-entrance.v1\",\"endgame.cube-inputs.v1\"],\"riftEntrances\":[{\"mapId\":7,\"npcId\":83,\"npc\":\"八仙使者\",\"script\":\"Nbaxian\"}]}");
    }
    static void RiftContext(Client c, bool inside) => Apply(c, "{\"kind\":\"rift.context\",\"ok\":true,\"result\":{\"inInstance\":\"" + (inside ? "true" : "false") + "\"}}");
    static void QuietRiftLifecycleContracts()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); QuietNegotiate(c);
        Check(!EndgamePanel.TryOpenRiftFromWorld(), "unknown reconnect context cannot open entry modal");
        RiftContext(c, false); Rift(c, "", "closed", keys: 2); OpenEntrance(c); Draw(p);
        Check((bool)Get(p, "open") && Enabled("消耗一钥 · 开启洞天"), "outside live NPC permits entry");
        Event.current.mousePosition = new Vector2(811, 477); Draw(p);
        HudCall(p, "ShowPanelTooltip", "screen hover");
        Check(UITip.mouse.x == 811 && UITip.mouse.y == 477 && UITip.scale == (float)Get(p, "panelScale"), "panel tooltip retains original screen pointer and window scale");
        Event.current.mousePosition = new Vector2(-9000, -9000);
        Draw(p, "消耗一钥 · 开启洞天"); var rejected = c.Endgame.PendingRequest;
        Check(rejected != null && (bool)Get(p, "open"), "sending admission never optimistically hides the entry panel");
        Draw(p, "消耗一钥 · 开启洞天"); Check(ReferenceEquals(c.Endgame.PendingRequest, rejected), "repeat entry click cannot duplicate an uncertain request");
        Apply(c, "{\"kind\":\"rift.start\",\"ok\":false,\"requestId\":\"" + rejected.RequestID + "\",\"error\":\"requirements\",\"message\":\"入口条件不足\"}");
        Call(p, "Update"); Draw(p);
        Check((bool)Get(p, "open") && Enabled("消耗一钥 · 开启洞天") && GUI.labels.Contains("入口条件不足"), "authoritative admission failure keeps a visible retry path");
        Draw(p, "消耗一钥 · 开启洞天"); var pending = c.Endgame.PendingRequest;
        Apply(c, "{\"kind\":\"rift.start\",\"ok\":false,\"requestId\":\"" + pending.RequestID + "\",\"error\":\"storage\"}");
        int retries = c.Retries; Draw(p, "查询原请求");
        Check(c.Retries == retries + 1 && ReferenceEquals(c.Endgame.PendingRequest, pending) && (bool)Get(p, "open"), "uncertain storage failure keeps same-nonce manual retry and entry modal");
        Receipt(c); Rift(c, "quiet-run", "active", extra: ",\"floorIndex\":1"); Call(p, "Update");
        Check((bool)Get(p, "open"), "run snapshot alone is not evidence the player entered");
        RiftContext(c, true); Draw(p);
        Check(!(bool)Get(p, "open") && !ArtworkSkin.enableds.ContainsKey("刷新") && !GUI.labels.Exists(t => t.Contains("选择劫数")), "authoritative entry closes before drawing any modal frame");
        int sends = c.Sent.Count;
        foreach (string phase in new[] { "active", "boss", "reward_pending", "completed", "closed", "failed" })
        {
            Rift(c, "quiet-run", phase, true, keys: 2); Call(p, "Update");
            Check(!EndgamePanel.TryOpenRiftFromWorld() && !EndgamePanel.TryOpenRiftFromNpc(7, 83, "八仙使者", "Nbaxian", () => true), "in-run entry attempts remain closed for " + phase);
            Set(p, "open", true); Draw(p);
            Check(!(bool)Get(p, "open") && !ArtworkSkin.enableds.ContainsKey("离开洞天") && !ArtworkSkin.enableds.ContainsKey("领取我的洞天所得") && !GUI.labels.Exists(t => t.Contains("正在前往下一层")), "stale open flag cannot revive objective/leave/reward modal for " + phase);
        }
        Check(c.Sent.Count == sends, "blocked in-run open attempts send no requests");
        // Cube is an independent NPC workbench; the rift-only guard must not close it.
        GameBootstrap.CurMapId = 7;
        Check(EndgamePanel.TryOpenCubeFromNpc(7, 77, "Nzhoutianding", () => true), "rift occupancy does not suppress independent cube dialogue");
        Call(p, "Update"); Check((bool)Get(p, "open") && (EndgameModule)Get(p, "module") == EndgameModule.Cube, "rift lifecycle leaves an independently opened cube intact");
        Call(p, "Close"); RiftContext(c, false); Rift(c, "quiet-run", "completed", false);
        Check(EndgamePanel.TryOpenRiftFromWorld(), "authoritative exit allows explicit reentry choice"); Draw(p);
        Check(Enabled("进入已有洞天") && !ArtworkSkin.enableds.ContainsKey("离开洞天") && !GUI.labels.Exists(t => t.Contains("镇煞：") || t.Contains("前往本层出口")), "outside reentry contains only entry controls without current-run objectives");
        Draw(p, "进入已有洞天"); Check(c.Last.Op == "rift.start" && c.Last.Json.Contains("\"runId\":\"quiet-run\"") && !c.Last.Json.Contains("npcEntityId"), "reentry preserves native NPC-independent run identity"); Receipt(c);
        RiftContext(c, true); Call(p, "Update"); Check(!(bool)Get(p, "open"), "accepted reentry closes again");
        RiftContext(c, false); Rift(c, "quiet-run", "closed", true, keys: 1); OpenEntrance(c); Draw(p);
        Check(Enabled("领取我的洞天所得") && !Enabled("消耗一钥 · 开启洞天"), "outside durable reward recovery remains available and prevents losing prior rewards");
        Draw(p, "领取我的洞天所得"); Check(c.Last.Op == "rift.claim" && c.Last.Json.Contains("\"runId\":\"quiet-run\""), "outside recovery claims only the exact run"); Receipt(c);
        Rift(c, "quiet-run", "interrupted", true, ok: false); Draw(p, "领取补偿符钥"); Check(c.Last.Op == "rift.claim", "outside interrupted-run compensation remains reachable"); Receipt(c);
        Rift(c, "quiet-run", "closed", false, keys: 1); OpenEntrance(c); Draw(p);
        Check(Enabled("消耗一钥 · 开启洞天"), "returned player can explicitly start a fresh run after recovery");
        Call(p, "Close"); Call(p, "Update"); Check(!(bool)Get(p, "open"), "polling after dismissal never automatically opens entrance");
        p.Unbind(c);
    }
    static void QuietRiftRecoveryContracts()
    {
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); QuietNegotiate(c); RiftContext(c, true);
        foreach (string op in new[] { "rift.start", "rift.advance", "rift.leave", "rift.claim" })
        {
            var pending = new EndgameRequest(op, "1", runID: "recovery-run", tier: op == "rift.start" ? 2 : 0, floorIndex: op == "rift.advance" ? 1 : 0);
            c.Endgame.RestoreUncertainRequest(pending);
            Rift(c, "recovery-run", "active", extra: ",\"floorIndex\":1");
            DateTime since = c.Endgame.PendingSinceUtc;
            int retries = c.Retries; Call(p, "Update");
            Check(c.Retries == retries && !EndgamePanel.TryOpenRiftFromWorld(), "inside recovery starts quietly with a short receipt grace period: " + op);
            HudCall(p, "RecoverPendingRiftRequest", since.AddSeconds(3));
            Check(c.Retries == retries + 1 && ReferenceEquals(c.Endgame.PendingRequest, pending) && ReferenceEquals(c.Retried[c.Retried.Count - 1], pending) && !(bool)Get(p, "open"), "lost receipt retries only its original nonce without an in-run modal: " + op);
            for (int i = 0; i < 50; i++) HudCall(p, "RecoverPendingRiftRequest", since.AddSeconds(4));
            Check(c.Retries == retries + 1, "frame updates cannot flood repeated mutation packets: " + op);
            HudCall(p, "RecoverPendingRiftRequest", since.AddSeconds(9));
            Check(c.Retries == retries + 2 && ReferenceEquals(c.Endgame.PendingRequest, pending), "persistent uncertain result receives backed-off same-request reconciliation: " + op);
            Receipt(c); HudCall(p, "RecoverPendingRiftRequest", since.AddMinutes(5));
            Check(c.Retries == retries + 2 && !c.Endgame.Pending, "receipt ends quiet retry immediately: " + op);
        }
        var original = new EndgameRequest("rift.advance", "1", runID: "recovery-run", floorIndex: 1);
        c.Endgame.RestoreUncertainRequest(original); DateTime began = c.Endgame.PendingSinceUtc; int before = c.Retries;
        RiftContext(c, false); HudCall(p, "RecoverPendingRiftRequest", began.AddMinutes(1));
        Check(c.Retries == before, "outside entry retains explicit recovery instead of unexpected automatic sends");
        c.Endgame.Reset(); QuietNegotiate(c); c.Endgame.RestoreUncertainRequest(original); RiftContext(c, true);
        HudCall(p, "RecoverPendingRiftRequest", c.Endgame.PendingSinceUtc.AddSeconds(3));
        Check(c.Retries == before + 1 && ReferenceEquals(c.Endgame.PendingRequest, original), "journal-restored reconnect request is reconciled under the new generation");
        // Native floor two can arrive before the original floor-one ACK. Retry its
        // frozen body only; neither the old gate intent nor a new click may advance again.
        string frozen = original.Json; int sends = c.Sent.Count;
        Rift(c, "recovery-run", "active", extra: ",\"floorIndex\":2");
        Func<RiftPortalContinuation> continuation;
        Check(EndgamePanel.TryTransferRiftPortalFromWorld(out continuation) && continuation == null, "pending advance consumes new-floor gate without issuing another mutation");
        HudCall(p, "RecoverPendingRiftRequest", c.Endgame.PendingSinceUtc.AddMinutes(1));
        Check(c.Sent.Count == sends && ReferenceEquals(c.Retried[c.Retried.Count - 1], original) && original.Json == frozen && frozen.Contains("\"floorIndex\":1"), "lost advance ACK retries exact original floor and nonce, never current-floor payload");
        Receipt(c);
        int afterReceipt = c.Retries;
        HudCall(p, "RecoverPendingRiftRequest", began.AddMinutes(5));
        Check(c.Retries == afterReceipt, "completed old-floor request cannot be retried after its ACK");
        Apply(c, "{\"kind\":\"snapshot\",\"ok\":true,\"snapshotId\":\"recovered-floor\",\"chunks\":1,\"revision\":\"2\"}");
        Check(EndgamePanel.TryTransferRiftPortalFromWorld(out continuation) && c.Last.Op == "rift.advance" && c.Last.RequestID != original.RequestID && c.Last.Json.Contains("\"floorIndex\":2"), "only a fresh physical gate action after reconciliation may request floor two");
        var rejected = c.Endgame.PendingRequest;
        Apply(c, "{\"kind\":\"rift.advance\",\"ok\":false,\"requestId\":\"" + rejected.RequestID + "\",\"error\":\"run_expired\"}");
        HudCall(p, "RecoverPendingRiftRequest", began.AddMinutes(6));
        Check(!c.Endgame.Pending && c.Retries == afterReceipt, "terminal rejection ends automatic reconciliation");
        c.Endgame.RestoreUncertainRequest(original);
        c.EndgameInWorld = false; HudCall(p, "RecoverPendingRiftRequest", began.AddMinutes(5));
        Check(c.Retries == afterReceipt, "disconnected client sends no quiet retry");
        p.Unbind(c);
    }
    static void GuardianArrivalSchemaContracts()
    {
        Func<string, EndgameRiftView> parse = arrival => EndgameResponse.Parse("{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"runId\":\"schema-run\",\"revision\":\"4\",\"phase\":\"boss_arriving\",\"guardianArrival\":" + arrival + "}}").Rift;
        var rift = parse(ArrivalJson);
        Check(rift.GuardianArrival.DurationMS == 5000 && rift.GuardianArrival.DeadlineMS == 1791892805000 && rift.GuardianArrival.AnchorCharID == "42" && rift.GuardianArrival.Seed == "18446744073709551615", "arrival DTO preserves integer timestamps and uint64 seed without precision loss");
        foreach (string invalid in new[] { ArrivalJson.Replace("\"remainingMS\":5000", "\"remainingMS\":5001"), ArrivalJson.Replace("\"durationMS\":5000", "\"durationMS\":0"), ArrivalJson.Replace("1791892805000", "1791892805001"), ArrivalJson.Replace("\"42\"", "\"0\""), ArrivalJson.Replace("\"x\":720", "\"x\":720.5"), ArrivalJson.Replace("\"seed\":\"18446744073709551615\"", "\"seed\":\"01\""), ArrivalJson.Replace("{", "{\"unknown\":1,") })
        {
            bool rejected = false; try { parse(invalid); } catch (FormatException) { rejected = true; }
            Check(rejected, "invalid arrival schema rejects whole DTO");
        }
        Check(parse("null").GuardianArrival == null, "waiting for an eligible native anchor does not invent a countdown or reject the run");
        DateTime received = new DateTime(2026, 10, 13, 12, 0, 0, DateTimeKind.Utc);
        Func<DateTime, int> remaining = now => (int)HudCall(null, "RiftArrivalRemainingMS", rift, received, now);
        Check(remaining(received.AddSeconds(-1)) == 5000 && remaining(received.AddMilliseconds(3001)) == 1999 && remaining(received.AddSeconds(7)) == 0, "arrival clock clamps rollback and expiration without starting another five seconds");
        var late = parse(ArrivalJson.Replace("\"remainingMS\":5000", "\"remainingMS\":1250"));
        Check((int)HudCall(null, "RiftArrivalRemainingMS", late, received, received.AddMilliseconds(250)) == 1000, "late observer resumes only authoritative time left");
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); QuietNegotiate(c); RiftContext(c, true);
        string arrivalExtra = ",\"guardianArrival\":" + ArrivalJson + ",\"remainingSeconds\":120,\"pendingLoot\":3,\"auras\":[{\"entityId\":\"7\",\"class\":1,\"originalName\":\"寒林狼\"}]";
        Rift(c, "arrival-run", "boss_arriving", extra: arrivalExtra);
        typeof(EndgameClientState).GetField("<RiftReceivedUtc>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(c.Endgame, DateTime.UtcNow.AddMilliseconds(-3100));
        Rift(c, "arrival-run", "boss_arriving", extra: arrivalExtra);
        Check(c.Endgame.Rift.GuardianArrival.RemainingMS <= 1900 && c.Endgame.Rift.PendingLoot == 3 && c.Endgame.Rift.Auras[0].OriginalName == "寒林狼", "same event replay cannot extend countdown and preserves quiet loot/name metadata");
        Event.current.mousePosition = new Vector2(90, 110); UITip.last = null; Draw(p);
        Check(UITip.last != null && UITip.last.Contains("绿色：镇煞") && UITip.last.Contains("白色：已用时间") && UITip.last.Contains("待拾取：3") && UITip.last.Contains("劫影将至"), "quiet HUD preserves detailed hover and pending loot without an adjacent status panel");
        Event.current.mousePosition = new Vector2(-9000, -9000);
        Check((int)HudCall(p, "RiftRemainingSeconds", c.Endgame.Rift, received, received.AddSeconds(20)) == 100, "main run timer continues during arrival");
        Rift(c, "arrival-run", "boss", extra: ",\"remainingSeconds\":110"); Check(c.Endgame.Rift.GuardianArrival == null, "guardian spawn removes countdown projection");
        var legacy = EndgameResponse.Parse("{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"phase\":\"boss\",\"auras\":[{\"entityId\":\"7\",\"class\":3}]}}").Rift;
        Check(legacy.GuardianArrival == null && legacy.PendingLoot == 0 && legacy.Auras[0].OriginalName == "", "legacy rift snapshots remain compatible");
        p.Unbind(c);
    }
    static void GuardianServerWireContracts()
    {
        string path = Environment.GetEnvironmentVariable("FFO_RIFT_WIRE_FIXTURES");
        Check(!string.IsNullOrEmpty(path) && File.Exists(path), "actual Go-produced rift fixtures are present");
        var fixtures = EndgameData.Object(BoundedJson.Parse(File.ReadAllText(path)));
        var arrival = EndgameResponse.Parse(BoundedJson.Serialize(fixtures["arrival"]));
        var bagFull = EndgameResponse.Parse(BoundedJson.Serialize(fixtures["bagFull"]));
        var interrupted = EndgameResponse.Parse(BoundedJson.Serialize(fixtures["interrupted"]));
        Check(arrival.OK && arrival.Rift.Phase == "boss_arriving" && arrival.Rift.GuardianArrival.DurationMS == 5000 && arrival.Rift.GuardianArrival.AnchorCharID == "100", "actual scene-emitted arrival response parses in client DTO");
        Check(interrupted.Rift.Phase == "interrupted" && interrupted.Rift.Claimable && interrupted.Rift.GuardianArrival == null, "orphaned arrival projects parseable interruption and preserves compensation access");
        Check(!bagFull.OK && bagFull.Error == "bag_full" && bagFull.Message == "" && bagFull.Rift.PendingLoot > 0, "actual server bag-full event preserves structured recovery metadata without notification text");
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); QuietNegotiate(c); RiftContext(c, true);
        c.Endgame.Apply(arrival); Call(p, "Update");
        // A completion snapshot restored independently is a silent baseline.
        c.Endgame.Apply(bagFull); Call(p, "Update");
        int notices = NewbieTipBox.notices.Count;
        c.Endgame.Apply(bagFull); Call(p, "Update"); Draw(p);
        Check(c.Endgame.Error == "bag_full" && c.Endgame.Rift.PendingLoot > 0 && NewbieTipBox.notices.Count == notices && !(bool)Get(p, "open"), "repeated real bag-full event remains quiet while preserving uncollected loot and structured error");
        p.Unbind(c);
    }

}
