using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
public sealed partial class EndgameClientState
{
    public bool SnapshotLoading => assembling != null;
    // Native inventory rows and extension rows arrive in separate packets. A
    // matching template/slot does not identify a rolled equipment instance.
    public bool InventorySnapshotCurrent => Negotiated && HasSnapshot && !inventorySnapshotRequired &&
        !SnapshotLoading && SnapshotRevision == Revision;
    public bool NeedsInventorySnapshotRefresh => Negotiated && nativeSnapshotRefreshPending && !SnapshotLoading;
    public string SnapshotID => publishedSnapshotID ?? "";
    public string SnapshotRevision { get; private set; } = "0";
    public EndgameCombatStatsView CombatStats { get; private set; }
    public DateTime CombatStatsReceivedUtc { get; private set; }
    public long CombatStatsVersion { get; private set; }
    public string CombatStatsRevision { get; private set; } = "0";
    private long snapshotCombatStatsVersion;
    private bool ApplyCombatStats(EndgameResponse response)
    {
        if (response.CombatStats == null || response.Revision.Length == 0) throw new FormatException("Invalid combat stats update.");
        if (CompareRevision(response.Revision, Revision) < 0 || CompareRevision(response.Revision, CombatStatsRevision) < 0) return false;
        PublishCombatStats(response.CombatStats, response.Revision);
        return true;
    }
    private void PublishCombatStats(EndgameCombatStatsView stats, string revision)
    {
        CombatStats = stats; CombatStatsRevision = revision;
        CombatStatsReceivedUtc = stats == null ? default(DateTime) : DateTime.UtcNow; CombatStatsVersion++;
    }
    private EndgameCombatStatsView snapshotCombatStats;
    private List<EndgameItemView> assembling;
    private IReadOnlyList<EndgameMaterialView> snapshotMaterials;
    private IReadOnlyList<EndgameFormationView> snapshotFormations;
    private string snapshotID, snapshotRevision, publishedSnapshotID;
    private int nextChunk, chunkCount;
    private DateTime snapshotStartedAt;
    private readonly HashSet<string> seenUIDs = new HashSet<string>(StringComparer.Ordinal);
    private bool inventorySnapshotRequired;
    private bool nativeSnapshotRefreshPending;
    private DateTime inventorySnapshotRequiredAt;
    private readonly HashSet<string> retiredSnapshots = new HashSet<string>(StringComparer.Ordinal);
    private readonly Queue<string> retiredSnapshotOrder = new Queue<string>();

    public void NoteNativeInventoryPublished()
    {
        // Pushed sidecars follow native inventory on the same ordered connection;
        // native-only paths are recovered by one host-batch snapshot read.
        // Never keep an old UID overlay alive
        // just because a replacement happens to have the same item-table ID.
        inventorySnapshotRequired = true;
        nativeSnapshotRefreshPending = true;
        inventorySnapshotRequiredAt = DateTime.UtcNow;
        DiscardChunks();
        InvalidateQuote();
    }

    private void RetireSnapshot(string id)
    {
        if (string.IsNullOrEmpty(id) || !retiredSnapshots.Add(id)) return;
        retiredSnapshotOrder.Enqueue(id);
        while (retiredSnapshotOrder.Count > 128) retiredSnapshots.Remove(retiredSnapshotOrder.Dequeue());
    }

    private void RejectSnapshot()
    {
        // An explicit failed read is terminal for this automatic attempt. Keep
        // its precise server error, not a later misleading timeout or old UID.
        inventorySnapshotRequired = true;
        nativeSnapshotRefreshPending = false;
        inventorySnapshotRequiredAt = default(DateTime);
        DiscardChunks();
    }

    private void ApplySnapshot(EndgameResponse r)
    {
        if (r.SnapshotID.Length == 0 || r.Chunks < 1 || r.Chunk >= r.Chunks) throw new FormatException("Invalid snapshot envelope.");
        if (CompareRevision(r.Revision, Revision) < 0 || retiredSnapshots.Contains(r.SnapshotID) ||
            r.SnapshotID == publishedSnapshotID || assembling != null && CompareRevision(r.Revision, snapshotRevision) < 0) return;
        if (r.Chunk == 0)
        {
            if (assembling != null && snapshotID == r.SnapshotID) { DiscardChunks(); throw new FormatException("Repeated snapshot chunk."); }
            DiscardChunks(); snapshotStartedAt = DateTime.UtcNow; assembling = new List<EndgameItemView>();
            inventorySnapshotRequired = true;
            nativeSnapshotRefreshPending = false;
            inventorySnapshotRequiredAt = snapshotStartedAt;
            snapshotID = r.SnapshotID; snapshotRevision = r.Revision; chunkCount = r.Chunks;
            snapshotMaterials = r.Materials; snapshotFormations = r.Formations; snapshotCombatStats = r.CombatStats; snapshotCombatStatsVersion = CombatStatsVersion;
        }
        if (assembling == null || snapshotID != r.SnapshotID || snapshotRevision != r.Revision || chunkCount != r.Chunks || r.Chunk != nextChunk)
        { DiscardChunks(); throw new FormatException("Interrupted or out-of-order snapshot."); }
        foreach (var item in r.Items)
        {
            if (item.UID.Length == 0 || item.UID == "0" || !seenUIDs.Add(item.UID)) { DiscardChunks(); throw new FormatException("Duplicate or missing item UID."); }
            if (item.Location != "bag" && item.Location != "equipped" && item.Location != "warehouse" && item.Location != "changeset") throw new FormatException("Invalid item location.");
            if (item.Tab < 0 || item.Slot < 0 || (item.Location == "bag" && (item.Tab > 3 || item.Slot > 99)) || ((item.Location == "equipped" || item.Location == "changeset") && (item.Slot < 1 || item.Slot > 25))) throw new FormatException("Invalid item coordinate.");
            assembling.Add(item);
        }
        nextChunk++;
        if (nextChunk != chunkCount) return;
        Items = new ReadOnlyCollection<EndgameItemView>(assembling); Materials = snapshotMaterials; ActiveFormations = snapshotFormations;
        // A same-revision aura/proc push may arrive while the inventory chunks assemble.
        // Its newer runtime state must survive publication of the older chunk-zero view.
        if (snapshotCombatStatsVersion == CombatStatsVersion && CompareRevision(snapshotRevision, CombatStatsRevision) >= 0)
            PublishCombatStats(snapshotCombatStats, snapshotRevision);
        RetireSnapshot(publishedSnapshotID);
        Revision = SnapshotRevision = snapshotRevision; HasSnapshot = true; inventorySnapshotRequired = false;
        nativeSnapshotRefreshPending = false;
        publishedSnapshotID = snapshotID; InvalidateQuote(); DiscardChunks();
    }
    private void DiscardChunks()
    {
        RetireSnapshot(snapshotID);
        assembling = null; snapshotCombatStats = null; snapshotMaterials = null; snapshotFormations = null; snapshotID = snapshotRevision = null; nextChunk = chunkCount = 0; seenUIDs.Clear();
    }
}
}
