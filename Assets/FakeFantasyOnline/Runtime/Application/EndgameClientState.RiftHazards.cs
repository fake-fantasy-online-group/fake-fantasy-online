using System;
using System.Globalization;

namespace FakeFantasyOnline.Application
{
public sealed partial class EndgameClientState
{
    private string riftHazardContextRun = "";
    private int riftHazardContextMap, riftHazardContextFloor;
    private long riftHazardContextTime, riftHazardContextEpoch, riftHazardWorldEpoch, riftHazardRejectedContextBarrier;
    private bool riftHazardContextReady, riftHazardNeedsNewWorld;
    private object riftHazardBoundWorld;
    private string riftHazardBoundRun = "";
    private int riftHazardBoundFloor, riftHazardBoundMap;
    public void NoteRiftSceneChange()
    {
        DiscardRiftPages();
        RiftHazards.Suspend();
        // Private context normally precedes the native teleport. Consume it once;
        // a reused native map cannot borrow a previous floor's context.
        if (riftHazardContextEpoch <= riftHazardWorldEpoch) riftHazardContextReady = false;
        riftHazardWorldEpoch = riftHazardContextEpoch;
        riftHazardNeedsNewWorld = riftHazardBoundWorld != null;
    }
    public void NoteRiftSceneChangeRejected(object retainedWorldIdentity, int retainedMapID)
    {
        // Native preflight retained this exact world. Recovery still needs a new
        // authoritative context and full snapshot for its bound run/floor/map.
        // Never reinterpret a failed client load as a server-side floor rollback.
        if (!ReferenceEquals(retainedWorldIdentity, riftHazardBoundWorld) || retainedWorldIdentity == null || retainedMapID != riftHazardBoundMap) return;
        riftHazardRejectedContextBarrier = Math.Max(riftHazardRejectedContextBarrier,
            Math.Max(riftHazardContextTime, RiftHazards.Snapshot == null ? 0 : RiftHazards.Snapshot.ServerNowMS));
        RiftHazards.Suspend(); riftHazardContextReady = false; riftHazardNeedsNewWorld = false;
        riftHazardWorldEpoch = riftHazardContextEpoch;
    }
    private void ResetRiftHazardContext()
    {
        riftHazardContextRun = ""; riftHazardContextMap = riftHazardContextFloor = 0;
        riftHazardContextTime = riftHazardContextEpoch = riftHazardWorldEpoch = riftHazardRejectedContextBarrier = 0;
        riftHazardContextReady = riftHazardNeedsNewWorld = false;
        riftHazardBoundWorld = null; riftHazardBoundRun = ""; riftHazardBoundFloor = riftHazardBoundMap = 0;
    }
    private void ApplyRiftHazardContext(EndgameResponse response, bool inside)
    {
        var values = response.Result;
        bool extended = values.ContainsKey("runId") || values.ContainsKey("floorIndex") || values.ContainsKey("mapId") || values.ContainsKey("serverNowMS");
        if (!extended)
        {
            RiftHazards.Suspend(); riftHazardContextReady = false;
            return; // Older v5 contexts remain valid for their original portal behavior.
        }
        if (!inside || !values.TryGetValue("runId", out string run) || run.Length == 0 || run.Length > 128 ||
            !values.TryGetValue("mapId", out string mapText) || !int.TryParse(mapText, NumberStyles.None, CultureInfo.InvariantCulture, out int map) || map <= 0 ||
            !values.TryGetValue("floorIndex", out string floorText) || !int.TryParse(floorText, NumberStyles.None, CultureInfo.InvariantCulture, out int floor) || floor < 0 ||
            !values.TryGetValue("serverNowMS", out string timeText) || !long.TryParse(timeText, NumberStyles.None, CultureInfo.InvariantCulture, out long time) || time <= 0)
            throw new FormatException("Invalid rift hazard scene context.");
        // A cached context followed by a newer snapshot is not a fresh scene
        // confirmation. Same-millisecond recovery conservatively waits for a later sample.
        if (time <= riftHazardRejectedContextBarrier || time < riftHazardContextTime ||
            run == riftHazardContextRun && floor < riftHazardContextFloor) return;
        RiftHazards.Suspend();
        riftHazardContextRun = run; riftHazardContextMap = map; riftHazardContextFloor = floor; riftHazardContextTime = time;
        riftHazardContextEpoch++; riftHazardContextReady = true;
    }
    // A world identity is the actual native root object, so a fresh PlayerController
    // cannot bypass the old root's consumed context when a map template is reused.
    public bool CanUseRiftHazards(int mapID, object worldIdentity)
    {
        if (worldIdentity == null || !CanUseRiftHazards(mapID)) return false;
        var view = RiftHazards.Snapshot;
        if (ReferenceEquals(worldIdentity, riftHazardBoundWorld))
        {
            if (riftHazardNeedsNewWorld || view.RunID != riftHazardBoundRun || view.FloorIndex != riftHazardBoundFloor || mapID != riftHazardBoundMap) return false;
        }
        else
        {
            if (riftHazardBoundWorld != null && !riftHazardNeedsNewWorld) return false;
            riftHazardBoundWorld = worldIdentity; riftHazardBoundRun = view.RunID;
            riftHazardBoundFloor = view.FloorIndex; riftHazardBoundMap = mapID; riftHazardNeedsNewWorld = false;
        }
        riftHazardWorldEpoch = riftHazardContextEpoch;
        return true;
    }
    public bool CanUseRiftHazards(int mapID)
    {
        var view = RiftHazards.Snapshot;
        return Negotiated && RiftContextKnown && RiftInInstance && riftHazardContextReady && RiftHazards.SnapshotCurrent && view != null &&
            ReferenceEquals(view, Rift) && mapID == riftHazardContextMap && view.MapID == mapID &&
            view.RunID == riftHazardContextRun && view.FloorIndex == riftHazardContextFloor && view.ServerNowMS >= riftHazardContextTime;
    }
}
}
