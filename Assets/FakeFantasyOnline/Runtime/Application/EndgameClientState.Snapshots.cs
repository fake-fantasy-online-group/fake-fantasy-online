using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{
public sealed partial class EndgameClientState
{
    public bool SnapshotLoading => assembling != null;
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
    private void ApplySnapshot(EndgameResponse r)
    {
        if (r.SnapshotID.Length == 0 || r.Chunks < 1 || r.Chunk >= r.Chunks) throw new FormatException("Invalid snapshot envelope.");
        if (CompareRevision(r.Revision, Revision) < 0 || r.SnapshotID == publishedSnapshotID) return;
        if (r.Chunk == 0)
        {
            if (assembling != null && snapshotID == r.SnapshotID) { DiscardChunks(); throw new FormatException("Repeated snapshot chunk."); }
            DiscardChunks(); snapshotStartedAt = DateTime.UtcNow; assembling = new List<EndgameItemView>();
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
        Revision = SnapshotRevision = snapshotRevision; HasSnapshot = true; publishedSnapshotID = snapshotID; InvalidateQuote(); DiscardChunks();
    }
    private void DiscardChunks()
    {
        assembling = null; snapshotCombatStats = null; snapshotMaterials = null; snapshotFormations = null; snapshotID = snapshotRevision = null; nextChunk = chunkCount = 0; seenUIDs.Clear();
    }
}
}
