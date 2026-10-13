using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Host-thread state owner. Only complete server state publishes; commands do not alter game facts.</summary>
public sealed partial class EndgameClientState
{
    public bool Negotiated { get; private set; }
    public bool NativeEquipmentStats { get; private set; }
    public bool GrowthV5 { get; private set; }
    public bool SkillBehaviorV1 { get; private set; }
    public bool SkillVariantsV1 { get; private set; }
    public bool HelloSent { get; private set; }
    public bool HasSnapshot { get; private set; }
    public string Revision { get; private set; } = "0";
    public string CatalogVersion { get; private set; } = "";
    public string Error { get; private set; } = "";
    public string Message { get; private set; } = "";
    public long Version { get; private set; }
    public long Generation { get; private set; }
    public bool Pending => PendingRequest != null;
    public EndgameRequest PendingRequest { get; private set; }
    public DateTime PendingSinceUtc { get; private set; }
    public EndgameQuoteView Quote { get; private set; }
    public bool QuotePending { get; private set; }
    public string QuoteRequestID { get; private set; } = "";
    public EndgameRiftView Rift { get; private set; }
    public IReadOnlyList<EndgameRiftEntranceView> RiftEntrances { get; private set; } = Array.Empty<EndgameRiftEntranceView>();
    public bool CubeInputsV1 { get; private set; }
    public bool RiftPortalsV1 { get; private set; }
    public bool RiftInInstance { get; private set; }
    public bool RiftContextKnown { get; private set; }
    private bool hasRiftSceneContext;
    // A valid sidecar reply distinguishes a temporarily unavailable current server
    // from a silent legacy server. Retry exhaustion is not evidence of a public map.
    private bool helloServerKnown;
    private string helloRequestID = "";
    private const string RiftContextUnavailableMessage = "地图归属尚未确认，暂不能使用传送门；请重新连接后重试。";
    public DateTime RiftReceivedUtc { get; private set; }
    public IReadOnlyList<EndgameItemView> Items { get; private set; } = Array.Empty<EndgameItemView>();
    public IReadOnlyList<EndgameMaterialView> Materials { get; private set; } = Array.Empty<EndgameMaterialView>();
    public IReadOnlyList<EndgameFormationView> ActiveFormations { get; private set; } = Array.Empty<EndgameFormationView>();
    // Bounded, generation-scoped authoritative responses for native API receipt correlation.
    // A transient error is a response, not proof that a mutation failed.
    private readonly Dictionary<string, EndgameResponse> responses = new Dictionary<string, EndgameResponse>(StringComparer.Ordinal);
    private readonly Queue<string> responseOrder = new Queue<string>();
    public EndgameResponse ResponseFor(string requestID) => requestID != null && responses.TryGetValue(requestID, out EndgameResponse response) ? response : null;
    private void RememberResponse(EndgameResponse response)
    {
        if (response.RequestID.Length == 0) return;
        if (!responses.ContainsKey(response.RequestID)) responseOrder.Enqueue(response.RequestID);
        responses[response.RequestID] = response;
        while (responseOrder.Count > 128) responses.Remove(responseOrder.Dequeue());
    }
    private DateTime quoteRequestedAt, helloRequestedAt, helloRetryAt;
    private int helloAttempts;
    private bool helloRetryScheduled, helloAwaitingResponse;

    public void Reset()
    {
        Negotiated = NativeEquipmentStats = GrowthV5 = SkillBehaviorV1 = SkillVariantsV1 = HelloSent = HasSnapshot = false; ResetSkillRanks(); Revision = SnapshotRevision = "0"; CatalogVersion = Error = Message = "";
        responses.Clear(); responseOrder.Clear();
        CombatStats = null; CombatStatsReceivedUtc = default(DateTime); CombatStatsRevision = "0"; CombatStatsVersion++;
        PendingRequest = null; Quote = null; QuotePending = false; QuoteRequestID = ""; Rift = null; CubeInputsV1 = RiftPortalsV1 = RiftInInstance = RiftContextKnown = hasRiftSceneContext = helloServerKnown = false;
        RiftEntrances = Array.Empty<EndgameRiftEntranceView>();
        Items = Array.Empty<EndgameItemView>(); Materials = Array.Empty<EndgameMaterialView>(); ActiveFormations = Array.Empty<EndgameFormationView>();
        helloAttempts = 0; helloRequestID = ""; helloRetryScheduled = helloAwaitingResponse = false;
        helloRequestedAt = helloRetryAt = default(DateTime);
        DiscardChunks(); publishedSnapshotID = null; retiredSnapshots.Clear(); retiredSnapshotOrder.Clear();
        inventorySnapshotRequired = nativeSnapshotRefreshPending = false; inventorySnapshotRequiredAt = default(DateTime);
        ResetCatalogs(); Generation++; Version++;
    }
    public bool Begin(EndgameRequest request)
    {
        if (request.Op == "hello")
        { RiftEntrances = Array.Empty<EndgameRiftEntranceView>(); helloRequestID = request.RequestID; HelloSent = true; helloAttempts++; helloRetryScheduled = false; helloAwaitingResponse = true; helloRequestedAt = DateTime.UtcNow; Error = ""; Version++; return true; }
        if (!Negotiated) return false;
        // A native-only publication gets one coalesced read, including a failed
        // send. Manual refresh remains available; host ticks never retry forever.
        if (request.Op == "snapshot") nativeSnapshotRefreshPending = false;
        if (request.Op.StartsWith("skill.", StringComparison.Ordinal) && !GrowthV5) return false;
        if (request.Mutates)
        {
            if (Pending) return false;
            PendingRequest = request; PendingSinceUtc = DateTime.UtcNow; Error = ""; Message = "正在等待服务器确认…";
        }
        if (request.Op == "cube.quote")
        {
            if (Pending || QuotePending) return false;
            Quote = null; QuotePending = true; QuoteRequestID = request.RequestID; quoteRequestedAt = DateTime.UtcNow;
        }
        if (request.Op == "catalog") catalogRequests[request.Catalog] = new CatalogRequest(request);
        Version++; return true;
    }
    public void RestoreUncertainRequest(EndgameRequest request)
    {
        if (!Negotiated || Pending || request == null || !request.Mutates) return;
        PendingRequest = request; PendingSinceUtc = DateTime.UtcNow; Error = "pending"; Message = "上次操作尚未确认，请查询原请求。"; Version++;
    }
    public void InvalidateQuote() { Quote = null; QuotePending = false; QuoteRequestID = ""; Version++; }
    public void NoteTransportFailure(string message, EndgameRequest request = null)
    {
        if (request != null && !request.Mutates) ReleaseReadRequest(request.RequestID);
        if (request != null && request.Op == "hello") ScheduleHelloRetry(DateTime.UtcNow);
        Error = "pending"; Message = message; Version++;
    }
    public void NoteInvalidResponse(string message) { DiscardChunks(); Error = "invalid_response"; Message = message; Version++; }
    public void Tick(DateTime now)
    {
        // A read-only hello can disappear during a map transfer or a dropped
        // send. Release it after a bounded wait, then use the same cooldown as
        // transient server errors. Never retry each host frame indefinitely.
        if (!Negotiated && helloAwaitingResponse && (now - helloRequestedAt).TotalSeconds >= 15)
        {
            ScheduleHelloRetry(now); Error = "timeout";
            if (!helloRetryScheduled && !hasRiftSceneContext && !helloServerKnown) RiftContextKnown = true;
            Message = helloRetryScheduled ? "星图暂未响应，稍后自动重试。" :
                helloServerKnown && !RiftContextKnown ? RiftContextUnavailableMessage : "星图暂未响应，请重新连接。"; Version++;
        }
        if (helloRetryScheduled && !Negotiated && now >= helloRetryAt)
        { helloRetryScheduled = false; HelloSent = false; Version++; }
        if (SnapshotLoading && (now - snapshotStartedAt).TotalSeconds >= 15)
        { DiscardChunks(); inventorySnapshotRequiredAt = default(DateTime); Error = "timeout"; Message = "星图接收未完成，请刷新重试。"; Version++; }
        else if (Negotiated && inventorySnapshotRequired && !SnapshotLoading &&
            inventorySnapshotRequiredAt != default(DateTime) && (now - inventorySnapshotRequiredAt).TotalSeconds >= 15)
        {
            // A lost or rejected sidecar must leave the native item readable and
            // expose recovery, rather than silently reviving its previous UID.
            inventorySnapshotRequiredAt = default(DateTime);
            Error = "timeout"; Message = "器物资料尚未同步，请刷新重试。"; Version++;
        }
        if (QuotePending && (now - quoteRequestedAt).TotalSeconds >= 15)
        { ReleaseReadRequest(QuoteRequestID); Error = "timeout"; Message = "问鼎未收到答复，可重新预览。"; Version++; }
        var expired = new List<string>();
        foreach (var row in catalogRequests) if ((now - row.Value.At).TotalSeconds >= 15) expired.Add(row.Key);
        foreach (string key in expired) { catalogRequests.Remove(key); Version++; }
    }
    private void ScheduleHelloRetry(DateTime now)
    {
        helloAwaitingResponse = false;
        helloRetryScheduled = !Negotiated && helloAttempts < 3;
        if (helloRetryScheduled) helloRetryAt = now.AddSeconds(2);
    }
    private void ReleaseReadRequest(string requestID)
    {
        if (requestID.Length == 0) return;
        if (requestID == QuoteRequestID) { QuotePending = false; QuoteRequestID = ""; }
        string name = CatalogForRequest(requestID);
        if (name != null) catalogRequests.Remove(name);
    }
    public EndgameItemView Find(string location, int tab, int slot, int itemID)
    {
        if (!InventorySnapshotCurrent) return null;
        // Tab is a bag address, not part of an equipped-slot address. The server
        // retains the item's inventory category (often 2) after equipping it.
        foreach (var item in Items)
            if (item.Location == location && (location == "equipped" || item.Tab == tab) &&
                item.Slot == slot && item.ItemID == itemID) return item;
        return null;
    }
    public bool Apply(EndgameResponse response)
    {
        // The scene context precedes native map entry and is safe before hello.
        // It grants no mutation capability; it only prevents speculative gate loads.
        if (response.Kind == "rift.context")
        {
            if (!RiftPortalsV1) return false;
            if (!response.OK || !response.Result.TryGetValue("inInstance", out string inside) ||
                (inside != "true" && inside != "false"))
                throw new FormatException("Invalid rift scene context.");
            RiftInInstance = inside == "true";
            RiftContextKnown = hasRiftSceneContext = true;
            Version++; return true;
        }
        if (response.Kind == "hello")
        {
            if (!HelloSent) return false;
            bool capable = false, nativeStats = false, growth = false, skillBehavior = false, skillVariants = false, riftPortals = false, riftEntrances = false, cubeInputs = false;
            foreach (var value in response.Capabilities)
            {
                if (value == "endgame.v1") capable = true;
                if (value == "endgame.native-stats.v1") nativeStats = true;
                if (value == "endgame.growth.v5") growth = true;
                if (value == "endgame.skill-behavior.v1") skillBehavior = true;
                if (value == "endgame.skill-variants.v1") skillVariants = true;
                if (value == "endgame.rift-portals.v1") riftPortals = true;
                if (value == "endgame.rift-entrance.v1") riftEntrances = true;
                if (value == "endgame.cube-inputs.v1") cubeInputs = true;
            }
            bool accepted = response.OK && capable;
            bool transientHello = response.Error == "pending" || response.Error == "storage" ||
                response.Error == "storage_unavailable" || response.Error == "busy";
            // A delayed load may successfully answer an earlier attempt. Accept that
            // success, but never let an old pending/busy undo a completed negotiation
            // or pollute an explicitly newer refresh.
            if (Negotiated && transientHello && (!helloAwaitingResponse ||
                response.RequestID.Length > 0 && response.RequestID != helloRequestID)) return false;
            if (capable || transientHello) helloServerKnown = true;
            if (transientHello && !Negotiated && !hasRiftSceneContext) RiftContextKnown = false;
            RiftEntrances = accepted && riftEntrances ? response.RiftEntrances : Array.Empty<EndgameRiftEntranceView>();
            // A failed refresh leaves the server's previously negotiated
            // renderer intact. Keep its matching client ownership as well.
            if (accepted || !Negotiated)
            {
                bool previousVariants = SkillVariantsV1;
                CubeInputsV1 = accepted && cubeInputs;
                RiftPortalsV1 = accepted && riftPortals;
                if (RiftPortalsV1 && !hasRiftSceneContext) RiftContextKnown = false;
                Negotiated = accepted; NativeEquipmentStats = accepted && nativeStats; GrowthV5 = accepted && growth;
                SkillBehaviorV1 = GrowthV5 && skillBehavior; SkillVariantsV1 = SkillBehaviorV1 && skillVariants;
                if (!GrowthV5 || previousVariants != SkillVariantsV1) ResetSkillRanks();
            }
            Error = accepted ? "" : response.Error;
            helloAwaitingResponse = helloRetryScheduled = false;
            if (!accepted && transientHello)
                ScheduleHelloRetry(DateTime.UtcNow);
            Message = accepted ? "星图已启，静候仙缘。" : helloRetryScheduled ? "星图暂未就绪，稍后自动重试。" : response.Message.Length > 0 ? response.Message : "当前服务器未开放演劫系统。";
            // An old server can negotiate the sidecar without portal ownership.
            // Never release a previously confirmed private context on a failed refresh.
            if (!hasRiftSceneContext && (accepted && !riftPortals || !accepted && !helloRetryScheduled && !helloServerKnown))
                RiftContextKnown = true;
            if (!accepted && !helloRetryScheduled && helloServerKnown && !RiftContextKnown)
                Message = RiftContextUnavailableMessage;
            Version++; return accepted;
        }
        if (!Negotiated) return false;
        if (PendingRequest != null && response.RequestID == PendingRequest.RequestID && !PendingRequest.MatchesResponse(response))
        { NoteInvalidResponse("服务器回执类型与原请求不符，请查询原请求。"); return false; }
        if (QuoteRequestID.Length > 0 && response.RequestID == QuoteRequestID && response.Kind != "cube.quote")
        { NoteInvalidResponse("服务器问鼎回执类型无效，请刷新重试。"); return false; }
        if (response.RequestID == QuoteRequestID) QuotePending = false;
        bool receipt = PendingRequest != null && response.RequestID == PendingRequest.RequestID;
        // A rejected admission can still return the authoritative prior run to claim.
        if (response.Rift != null && (Rift == null || Rift.RunID != response.Rift.RunID || CompareRevision(response.Rift.Revision, Rift.Revision) >= 0))
        { Rift = response.Rift; RiftReceivedUtc = DateTime.UtcNow; }
        // Leave receipts precede the native teleport; do not keep intercepting public gates.
        if (!hasRiftSceneContext && receipt && response.OK && response.Kind == "rift.leave") RiftInInstance = false;
        if (!response.OK)
        {
            if (response.Kind == "snapshot") RejectSnapshot();
            ReleaseReadRequest(response.RequestID); Error = response.Error;
            Message = response.Message.Length > 0 ? response.Message : ErrorText(response.Error);
            if (receipt && response.Error != "pending" && response.Error != "storage_unavailable" && response.Error != "storage" && response.Error != "busy") PendingRequest = null;
            if (response.Error == "stale_revision" || response.Error == "quote_expired") InvalidateQuote();
            RememberResponse(response); Version++; return false;
        }
        if (response.Kind == "skill.ranks" && !ApplySkillRanks(response)) return false;
        if (response.Kind == "combat.stats" && !ApplyCombatStats(response)) return false;
        if (receipt) { PendingRequest = null; InvalidateQuote(); Error = ""; Message = response.Message.Length > 0 ? response.Message : "服务器已确认。"; }
        if (response.Kind != "snapshot" && response.Revision.Length > 0 && CompareRevision(response.Revision, Revision) >= 0) Revision = response.Revision;
        if (response.CatalogVersion.Length > 0 && response.CatalogVersion != CatalogVersion)
        {
            if (CatalogVersion.Length > 0) ResetCatalogValues();
            CatalogVersion = response.CatalogVersion;
        }
        if (response.Kind == "snapshot") ApplySnapshot(response);
        string catalog = response.Kind.StartsWith("catalog.", StringComparison.Ordinal) ? response.Kind.Substring(8) : response.Kind == "catalog" ? CatalogForRequest(response.RequestID) : null;
        if (catalog != null) ApplyCatalog(catalog, response);
        if (response.Quote != null && response.RequestID.Length > 0 && response.RequestID == QuoteRequestID && CompareRevision(response.Quote.Revision, Revision) >= 0)
        { Quote = response.Quote; Error = ""; Message = "问鼎已成，请核对耗费与所得。"; }
        RememberResponse(response); Version++; return true;
    }
    public static int CompareRevision(string a, string b)
    {
        if (string.IsNullOrEmpty(a)) a = "0"; if (string.IsNullOrEmpty(b)) b = "0";
        return a.Length == b.Length ? string.CompareOrdinal(a, b) : a.Length.CompareTo(b.Length);
    }
    public static string ErrorText(string code)
    {
        switch (code)
        {
            case "pending": return "结果确认中，请以同一请求重试。";
            case "busy": return "角色正在处理另一笔操作，请稍候重试。";
            case "storage":
            case "storage_unavailable": return "暂时无法保存，请稍后查询原请求。";
            case "stale_revision": return "背包已变化，请刷新后重新选择。";
            case "quote_expired": return "鼎中预览已过期，请重新问鼎。";
            case "bag_full": return "背包空间不足，请先整理背包。";
            case "materials": return "材料不足或所选材料已变化。";
            case "funds": return "铜币不足。";
            case "locked": return "所选物品已锁定。";
            case "run_expired": return "洞天已结束。";
            case "party_changed": return "队伍已变化，请重新集合。";
            default: return code;
        }
    }
}
}
