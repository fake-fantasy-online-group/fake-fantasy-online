using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
/// <summary>Host-thread state owner. Only complete server state publishes; commands do not alter game facts.</summary>
public sealed partial class EndgameClientState
{
    public bool Negotiated { get; private set; }
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
    public DateTime RiftReceivedUtc { get; private set; }
    public IReadOnlyList<EndgameItemView> Items { get; private set; } = Array.Empty<EndgameItemView>();
    public IReadOnlyList<EndgameMaterialView> Materials { get; private set; } = Array.Empty<EndgameMaterialView>();
    public IReadOnlyList<EndgameFormationView> ActiveFormations { get; private set; } = Array.Empty<EndgameFormationView>();
    private DateTime quoteRequestedAt;

    public void Reset()
    {
        Negotiated = HelloSent = HasSnapshot = false; Revision = "0"; CatalogVersion = Error = Message = "";
        PendingRequest = null; Quote = null; QuotePending = false; QuoteRequestID = ""; Rift = null;
        Items = Array.Empty<EndgameItemView>(); Materials = Array.Empty<EndgameMaterialView>(); ActiveFormations = Array.Empty<EndgameFormationView>();
        DiscardChunks(); publishedSnapshotID = null; ResetCatalogs(); Generation++; Version++;
    }
    public bool Begin(EndgameRequest request)
    {
        if (request.Op == "hello") { HelloSent = true; Version++; return true; }
        if (!Negotiated) return false;
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
        Error = "pending"; Message = message; Version++;
    }
    public void NoteInvalidResponse(string message) { DiscardChunks(); Error = "invalid_response"; Message = message; Version++; }
    public void Tick(DateTime now)
    {
        if (SnapshotLoading && (now - snapshotStartedAt).TotalSeconds >= 15)
        { DiscardChunks(); Error = "timeout"; Message = "星图接收未完成，请刷新重试。"; Version++; }
        if (QuotePending && (now - quoteRequestedAt).TotalSeconds >= 15)
        { ReleaseReadRequest(QuoteRequestID); Error = "timeout"; Message = "问鼎未收到答复，可重新预览。"; Version++; }
        var expired = new List<string>();
        foreach (var row in catalogRequests) if ((now - row.Value.At).TotalSeconds >= 15) expired.Add(row.Key);
        foreach (string key in expired) { catalogRequests.Remove(key); Version++; }
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
        if (!Negotiated || !HasSnapshot) return null;
        foreach (var item in Items) if (item.Location == location && item.Tab == tab && item.Slot == slot && item.ItemID == itemID) return item;
        return null;
    }
    public bool Apply(EndgameResponse response)
    {
        if (response.Kind == "hello")
        {
            if (!HelloSent) return false;
            bool capable = false; foreach (var value in response.Capabilities) if (value == "endgame.v1") capable = true;
            Negotiated = response.OK && capable; Error = Negotiated ? "" : response.Error;
            Message = Negotiated ? "星图已启，静候仙缘。" : "当前服务器未开放演劫系统。"; Version++; return Negotiated;
        }
        if (!Negotiated) return false;
        if (response.RequestID == QuoteRequestID) QuotePending = false;
        bool receipt = PendingRequest != null && response.RequestID == PendingRequest.RequestID;
        // A rejected admission can still return the authoritative prior run to claim.
        if (response.Rift != null && (Rift == null || Rift.RunID != response.Rift.RunID || CompareRevision(response.Rift.Revision, Rift.Revision) >= 0))
        { Rift = response.Rift; RiftReceivedUtc = DateTime.UtcNow; }
        if (!response.OK)
        {
            ReleaseReadRequest(response.RequestID); Error = response.Error;
            Message = response.Message.Length > 0 ? response.Message : ErrorText(response.Error);
            if (receipt && response.Error != "pending" && response.Error != "storage_unavailable" && response.Error != "busy") PendingRequest = null;
            if (response.Error == "stale_revision" || response.Error == "quote_expired") InvalidateQuote();
            Version++; return false;
        }
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
        if (response.Quote != null && response.RequestID == QuoteRequestID && CompareRevision(response.Quote.Revision, Revision) >= 0)
        { Quote = response.Quote; Error = ""; Message = "问鼎已成，请核对耗费与所得。"; }
        Version++; return true;
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
