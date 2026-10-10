using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // The selected response reader's existing locals, retained by its one admission. This
    // carrier changes where that reader resumes, not its packet grammar or publication rules.
    private sealed class SelectedWorldEntryReceipt : IDisposable
    {
        internal readonly string RequestedName;
        internal readonly int RosterLevel;
        internal readonly TaskCompletionSource<Result<WorldEntryRecord>> Completion =
            new TaskCompletionSource<Result<WorldEntryRecord>>(
                TaskCreationOptions.RunContinuationsAsynchronously);
        internal readonly List<PendingRemoteWorldState> remoteWorldStates =
            new List<PendingRemoteWorldState>();
        internal readonly List<WorldGroundItemState> groundItems = new List<WorldGroundItemState>();
        internal readonly List<PendingChannelChat> channelChats = new List<PendingChannelChat>();
        internal WorldSelfPkState selfPkState;
        internal WorldLocalFactionState localFactionState;
        internal WorldPartyState partyState;
        internal WorldTaskLogState taskLogState;
        internal WorldNpcChatterState npcChatterState;
        internal WorldInventoryState inventoryState;
        internal WorldWardrobeState wardrobeState;
        internal WorldSkillBookObservation skillBookState;
        internal WorldLifeSkillBookState lifeSkillBookState;
        internal byte[] buffStatePayload;
        internal bool buffStateNonEmpty;
        internal WorldPlayerAttributeSnapshot attributeSnapshot;
        internal WorldPlayerPetStatusState petStatus;
        internal int PacketCount;
        internal bool ReceiptFailed;
        private readonly object _timeoutSync = new object();
        private CancellationTokenSource _timeout;
        private CancellationTokenRegistration _timeoutRegistration;
        private bool _disposed;

        internal SelectedWorldEntryReceipt(string requestedName, int rosterLevel)
        { RequestedName = requestedName; RosterLevel = rosterLevel; }

        internal void StartTimeout(CancellationTokenSource timeout)
        {
            lock (_timeoutSync)
            {
                if (_disposed || Completion.Task.IsCompleted)
                {
                    timeout.Dispose();
                    return;
                }
                _timeout = timeout;
                CancellationToken token = timeout.Token;
                _timeoutRegistration = token.Register(() =>
                {
                    lock (_timeoutSync)
                    {
                        if (!_disposed && ReferenceEquals(_timeout, timeout))
                            Completion.TrySetCanceled(token);
                    }
                });
            }
        }

        internal bool TryBeginReceipt()
        {
            CancellationTokenSource timeout;
            CancellationTokenRegistration registration;
            lock (_timeoutSync)
            {
                if (_disposed || Completion.Task.IsCompleted) return false;
                timeout = _timeout;
                registration = _timeoutRegistration;
                _timeout = null;
                _timeoutRegistration = default(CancellationTokenRegistration);
            }
            // Whichever won the same lock owns the boundary: either timeout completed the
            // waiter, or this actual receipt retires that wait before any body runs. Caller
            // cancellation during the body is observed at its existing token checkpoints.
            registration.Dispose();
            timeout?.Dispose();
            return true;
        }

        public void Dispose()
        {
            CancellationTokenSource timeout;
            CancellationTokenRegistration registration;
            lock (_timeoutSync)
            {
                _disposed = true;
                timeout = _timeout;
                registration = _timeoutRegistration;
                _timeout = null;
            }
            // A cancellation callback only completes the observer task. Do not wait for it
            // while holding a lock used by the host's receipt or timeout reset.
            registration.Dispose();
            timeout?.Dispose();
        }
    }

    private void ConsumePendingSelectedEntry(WorldEntryAdmission admission)
    {
        // Preserve the earlier session reader's retained FIFO, then already-dispatched FIFO.
        // Consume each existing packet once; never put it back into NetClient or Dispatch.
        while (!admission.SelectedReceipt.Completion.Task.IsCompleted)
        {
            ProtocolPacket packet;
            if (!TryTakeOldestPendingDownlink(out packet))
            {
                if (!_dispatchedReady.Wait(0)) break;
                if (!_dispatchedDownlinks.TryDequeue(out packet)) continue;
            }
            ReceiveSelectedWorldEntry(admission, packet, false);
        }
    }

    private bool TryReceiveSelectedWorldEntry(ProtocolPacket packet)
    {
        WorldEntryAdmission admission = Volatile.Read(ref _worldEntryAdmission);
        if (admission?.SelectedReceipt == null ||
            admission.SelectedReceipt.Completion.Task.IsCompleted ||
            !ReferenceEquals(admission.Connection, _connectionLifetime)) return false;
        return ReceiveSelectedWorldEntry(admission, packet, true);
    }

    private bool ReceiveSelectedWorldEntry(
        WorldEntryAdmission admission, ProtocolPacket packet, bool waitForNextPacket)
    {
        if (!_host.IsHostThread)
            throw new InvalidOperationException("Selected world-entry receipts must run on the host thread.");
        SelectedWorldEntryReceipt receipt = admission.SelectedReceipt;
        // The old per-packet read timeout ended before its decode/publication. Keep that
        // boundary: only a pending next packet has a response timer, never the world builder.
        if (!receipt.TryBeginReceipt()) return false;
        EnterWorldResponse response;
        try
        {
            admission.Cancellation.ThrowIfCancellationRequested();
            Task<EnterWorldResponse> read = ReadEnterWorldResponseAsync(
                receipt, packet, admission.Cancellation);
            if (!read.IsCompleted)
                throw new InvalidOperationException("The selected receipt reader did not complete on the host.");
            response = read.GetAwaiter().GetResult();
            receipt.PacketCount++;
            if (response == null)
            {
                if (receipt.PacketCount >= MaximumPacketsBeforeExpectedResponse)
                    throw new InvalidDataException("The enter-world response did not arrive.");
                if (waitForNextPacket)
                    receipt.StartTimeout(CreateResponseTimeout(admission.Cancellation));
                return true;
            }
        }
        catch (Exception exception)
        {
            // Retain the existing pre-SelfData rejection/reader-error result boundary.
            receipt.Completion.TrySetException(exception);
            return true;
        }

        try
        {
            Task<Result<WorldEntryRecord>> applied = ApplyEnterWorldResponseAsync(
                response, receipt.RequestedName, receipt.RosterLevel,
                admission, admission.Cancellation);
            if (!applied.IsCompleted)
                throw new InvalidOperationException("The selected world-entry callback did not complete on the host.");
            // This observes only a completed host action. The actual title callback includes
            // the existing synchronous builder and its normal-return admission completion.
            receipt.Completion.TrySetResult(applied.GetAwaiter().GetResult());
            return true;
        }
        catch (Exception exception)
        {
            receipt.ReceiptFailed = true;
            receipt.Completion.TrySetException(exception);
            // Preserve the receipt's throw edge and reached mutations. The selected observer
            // may report this same failure, but cannot remap it into a wire error/disconnect.
            throw;
        }
    }
}
}
