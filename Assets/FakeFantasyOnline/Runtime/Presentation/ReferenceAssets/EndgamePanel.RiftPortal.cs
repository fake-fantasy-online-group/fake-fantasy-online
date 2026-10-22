using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal enum RiftPortalContinuation { Waiting, Finished, Sent, Public }
public sealed partial class EndgamePanel
{
    internal static bool TryGetRiftPortalFromWorld(out string runID, out int floorIndex,
        out long generation, out float x, out float y)
    {
        EndgameClientState state = Current?.State;
        EndgameRiftView rift = state?.Rift;
        runID = rift?.RunID; floorIndex = rift?.FloorIndex ?? 0;
        generation = state?.Generation ?? -1; x = rift?.ExitX ?? 0; y = rift?.ExitY ?? 0;
        return WorldEntryAvailable && state.RiftContextKnown && state.RiftInInstance &&
            rift != null && rift.Phase == "active" && !rift.Transitioning &&
            rift.RunID.Length > 0 && rift.FloorIndex > 0;
    }

    // True consumes the native gate even when loading, busy, defeated or completed.
    // Only rift.leave may restore ordinary portal semantics for an admitted member.
    internal static bool TryTransferRiftPortalFromWorld()
        => TryTransferRiftPortalFromWorld(out _);

    internal static bool TryTransferRiftPortalFromWorld(out Func<RiftPortalContinuation> continueWhenReady)
    {
        continueWhenReady = null;
        EndgamePanel panel = Current;
        EndgameClientState state = panel?.State;
        if (state == null) return false;
        // A reconnect may finish building before its sidecar handshake. Wait for
        // the authoritative context instead of assuming that an unknown map is public.
        if (state.RiftContextKnown ? !state.RiftInInstance : !panel.client.EndgameInWorld) return false;
        var intent = new RiftPortalIntent(panel, state);
        if (intent.Continue() == RiftPortalContinuation.Waiting) continueWhenReady = intent.Continue;
        return true;
    }

    // A physical gate edge can arrive before its sidecar state. Keep that one
    // intent until it can be sent, instead of consuming the edge without a request.
    // PlayerController discards it on leaving the gate; sent requests never retry
    // here, including authoritative rejections and uncertain transport outcomes.
    private sealed class RiftPortalIntent
    {
        private readonly EndgamePanel panel;
        private readonly EndgameClientState state;
        private readonly long generation;
        private string runID;
        private int floorIndex;

        internal RiftPortalIntent(EndgamePanel owner, EndgameClientState source)
        {
            panel = owner; state = source; generation = source.Generation;
            if (source.RiftContextKnown && source.RiftInInstance) CaptureFloor(source.Rift);
        }

        private void CaptureFloor(EndgameRiftView rift)
        {
            if (rift == null || rift.RunID.Length == 0 || rift.FloorIndex <= 0) return;
            runID = rift.RunID; floorIndex = rift.FloorIndex;
        }

        internal RiftPortalContinuation Continue()
        {
            if (Current != panel || !ReferenceEquals(panel.State, state) || state.Generation != generation ||
                panel.client == null || !panel.client.EndgameInWorld) return RiftPortalContinuation.Finished;
            if (!state.RiftContextKnown) return RiftPortalContinuation.Waiting;
            if (!state.RiftInInstance) return RiftPortalContinuation.Public;
            EndgameRiftView rift = state.Rift;
            if (rift == null) return RiftPortalContinuation.Waiting;
            if (runID == null) CaptureFloor(rift);
            if (runID != null && (rift.RunID != runID || rift.FloorIndex != floorIndex)) return RiftPortalContinuation.Finished;
            if (rift.Transitioning) return RiftPortalContinuation.Finished;
            if (rift.Phase != "active") return rift.Phase == "reserved" ? RiftPortalContinuation.Waiting : RiftPortalContinuation.Finished;
            if (runID == null) return RiftPortalContinuation.Finished; // Legacy runs do not have traversable floors.
            if (state.Pending)
                return state.PendingRequest.Op != "rift.advance" && state.PendingRequest.Op != "rift.leave"
                    ? RiftPortalContinuation.Waiting : RiftPortalContinuation.Finished;
            if (!WorldEntryAvailable || !state.HasSnapshot || state.SnapshotLoading ||
                state.SnapshotRevision != state.Revision) return RiftPortalContinuation.Waiting;
            // CanAdvance is a periodically refreshed presentation hint. Let the
            // server validate the opener and current gate position at this edge.
            // Movement is normally sampled every 0.3 seconds. Queue the current
            // position first without local throttling, even if the previous
            // sample was sent before player movement in this same frame.
            GameBootstrap.FlushMoveReport(true);
            panel.Send(new EndgameRequest("rift.advance", state.Revision,
                runID: runID, floorIndex: floorIndex));
            return RiftPortalContinuation.Sent;
        }
    }
}
}
