using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // True consumes the native gate even when loading, busy, defeated or completed.
    // Only rift.leave may restore ordinary portal semantics for an admitted member.
    internal static bool TryTransferRiftPortalFromWorld()
    {
        EndgamePanel panel = Current;
        EndgameClientState state = panel?.State;
        if (state == null) return false;
        // A reconnect may finish building before its sidecar handshake. Wait for
        // the authoritative context instead of assuming that an unknown map is public.
        if (!state.RiftContextKnown && panel.client.EndgameInWorld) return true;
        if (!state.RiftInInstance) return false;
        EndgameRiftView rift = state.Rift;
        if (WorldEntryAvailable && rift != null && rift.Phase == "active" &&
            !rift.Transitioning && rift.RunID.Length > 0 && rift.FloorIndex > 0 &&
            !state.Pending && state.HasSnapshot && !state.SnapshotLoading &&
            state.SnapshotRevision == state.Revision)
        {
            // CanAdvance is a periodically refreshed presentation hint. Let the
            // server validate the opener and current gate position at this edge.
            panel.Send(new EndgameRequest("rift.advance", state.Revision,
                runID: rift.RunID, floorIndex: rift.FloorIndex));
        }
        return true;
    }
}
}
