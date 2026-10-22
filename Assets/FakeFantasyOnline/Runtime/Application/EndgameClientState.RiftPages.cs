using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Application
{
public sealed partial class EndgameClientState
{
    public bool RiftPagesV1 { get; private set; }
    public bool RiftPagesAvailable { get; private set; }
    public bool NeedsRiftPageNegotiation { get; private set; }
    public bool RiftSnapshotLoading => riftPageBuffer != null;
    private string riftCapabilitiesRequest = "";
    private int riftCapabilitiesAttempts;
    private bool riftCapabilitiesAwaiting, riftCapabilitiesRetry;
    private DateTime riftCapabilitiesAt, riftCapabilitiesRetryAt;
    private byte[] riftPageBuffer;
    private string riftPageID;
    private int riftPageCount, riftPageNext, riftPageWritten;
    private DateTime riftPageStarted;
    private double riftPageStartedMS;
    private readonly HashSet<string> retiredRiftPages = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<string> retiredRiftPageOrder = new Queue<string>();

    private void ResetRiftPages()
    {
        DiscardRiftPages(); retiredRiftPages.Clear(); retiredRiftPageOrder.Clear();
        RiftPagesV1 = RiftPagesAvailable = NeedsRiftPageNegotiation = false;
        riftCapabilitiesRequest = ""; riftCapabilitiesAttempts = 0;
        riftCapabilitiesAwaiting = riftCapabilitiesRetry = false;
    }
    private void OfferRiftPages(EndgameResponse response)
    {
        RiftPagesAvailable = response.Result.TryGetValue("riftPages", out string offered) && offered == "1";
        // An explicit successful hello refresh can restart an exhausted read-only opt-in.
        if (!RiftPagesV1 && !riftCapabilitiesAwaiting && riftCapabilitiesAttempts >= 3) riftCapabilitiesAttempts = 0;
        if (RiftPagesAvailable && !RiftPagesV1 && !riftCapabilitiesAwaiting && riftCapabilitiesAttempts == 0)
            NeedsRiftPageNegotiation = true;
    }
    private bool BeginRiftCapabilities(EndgameRequest request)
    {
        if (!Negotiated || !RiftPagesAvailable || RiftPagesV1 || riftCapabilitiesAwaiting) return false;
        riftCapabilitiesRequest = request.RequestID; riftCapabilitiesAttempts++;
        riftCapabilitiesAt = DateTime.UtcNow; riftCapabilitiesAwaiting = true;
        NeedsRiftPageNegotiation = riftCapabilitiesRetry = false; return true;
    }
    private bool ApplyRiftCapabilities(EndgameResponse response)
    {
        if (!riftCapabilitiesAwaiting || response.RequestID != riftCapabilitiesRequest) return false;
        if (!response.OK)
        {
            RetryRiftCapabilities(DateTime.UtcNow);
            Error = response.Error; Message = response.Message.Length > 0 ? response.Message : ErrorText(response.Error);
            Version++; return false;
        }
        bool core = false, pages = false;
        foreach (string capability in response.Capabilities)
        { core |= capability == "endgame.v1"; pages |= capability == "endgame.rift-pages.v1"; }
        if (!core || !pages) throw new FormatException("Incomplete rift page capability acknowledgement.");
        RiftPagesV1 = true; riftCapabilitiesAwaiting = riftCapabilitiesRetry = NeedsRiftPageNegotiation = false;
        Error = ""; Version++; return true;
    }
    private void RetryRiftCapabilities(DateTime now)
    {
        riftCapabilitiesAwaiting = false;
        riftCapabilitiesRetry = !RiftPagesV1 && riftCapabilitiesAttempts < 3;
        riftCapabilitiesRetryAt = now.AddSeconds(2);
    }
    private void TickRiftPages(DateTime now)
    {
        if (riftCapabilitiesAwaiting && (now - riftCapabilitiesAt).TotalSeconds >= 15)
        { RetryRiftCapabilities(now); Error = "timeout"; Message = "洞天同步尚未完成，请稍后重试。"; Version++; }
        if (riftCapabilitiesRetry && now >= riftCapabilitiesRetryAt)
        { riftCapabilitiesRetry = false; NeedsRiftPageNegotiation = true; }
        if (RiftSnapshotLoading && (now - riftPageStarted).TotalSeconds >= 15)
        { DiscardRiftPages(); Error = "timeout"; Message = "洞天快照接收未完成，请刷新重试。"; Version++; }
    }
    private void RetireRiftPages(string id)
    {
        if (string.IsNullOrEmpty(id) || !retiredRiftPages.Add(id)) return;
        retiredRiftPageOrder.Enqueue(id);
        while (retiredRiftPageOrder.Count > 128) retiredRiftPages.Remove(retiredRiftPageOrder.Dequeue());
    }
    private void DiscardRiftPages()
    {
        RetireRiftPages(riftPageID); riftPageBuffer = null; riftPageID = null;
        riftPageCount = riftPageNext = riftPageWritten = 0;
    }
    /// <summary>Returns only a complete original response. Pages never reach receipt or presentation owners.</summary>
    public EndgameResponse ResolveRiftPage(EndgameResponse response)
    {
        if (response == null) throw new ArgumentNullException(nameof(response));
        if (response.Kind != "rift.page")
        {
            // A newer whole response or explicit size failure supersedes the partial document.
            if (response.Rift != null || response.Error == "snapshot_too_large") DiscardRiftPages();
            return response;
        }
        if (!Negotiated || !RiftPagesV1) throw new FormatException("Rift pages were not negotiated.");
        if (retiredRiftPages.Contains(response.SnapshotID)) return null;
        try
        {
            if (response.Chunk == 0)
            {
                if (riftPageID == response.SnapshotID) throw new FormatException("Repeated rift page.");
                DiscardRiftPages();
                riftPageID = response.SnapshotID; riftPageCount = response.Chunks;
                riftPageBuffer = new byte[response.TotalBytes]; riftPageStarted = DateTime.UtcNow; riftPageStartedMS = MonotonicMS;
            }
            if (riftPageBuffer == null || riftPageID != response.SnapshotID || riftPageCount != response.Chunks ||
                riftPageBuffer.Length != response.TotalBytes || riftPageNext != response.Chunk)
                throw new FormatException("Interrupted or out-of-order rift page.");
            byte[] page = Convert.FromBase64String(response.Data);
            int expected = Math.Min(EndgameResponse.RiftPageBytes, riftPageBuffer.Length - riftPageWritten);
            if (page.Length != expected || Convert.ToBase64String(page) != response.Data)
                throw new FormatException("Invalid rift page byte count or encoding.");
            Buffer.BlockCopy(page, 0, riftPageBuffer, riftPageWritten, page.Length);
            riftPageWritten += page.Length; riftPageNext++;
            if (riftPageNext != riftPageCount) return null;
            if (riftPageWritten != riftPageBuffer.Length) throw new FormatException("Incomplete rift snapshot.");
            EndgameResponse complete = EndgameResponse.ParseAssembledRift(riftPageBuffer);
            complete.RiftPageReceivedUtc = riftPageStarted; complete.RiftPageReceivedMS = riftPageStartedMS;
            DiscardRiftPages(); return complete;
        }
        catch (Exception error) when (error is FormatException || error is ArgumentException)
        {
            RetireRiftPages(response.SnapshotID); DiscardRiftPages(); throw;
        }
    }
}
}
