using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway : IWorldEntryAdmissionSource
{
    private readonly object _worldDrainSync = new object();
    private bool _worldDrainActive;
    private CancellationToken _worldDeliveryToken;

    // One capability per selected entry, tied to the existing connection and caller token.
    // This is an adapter suspension of the common receive consumer, not a native scene-ready
    // flag and not an NPC queue. Socket receive and Update's connection-control prefix continue.
    private sealed class WorldEntryAdmission
    {
        internal readonly CancellationTokenSource Connection;
        internal readonly CancellationToken Cancellation;
        internal volatile bool SelfDataDequeued;
        internal WorldEntryRecord ReturnedEntry;
        internal SelectedWorldEntryReceipt SelectedReceipt;
        internal WorldEntryAdmission(CancellationTokenSource connection, CancellationToken cancellation)
        { Connection = connection; Cancellation = cancellation; }
    }

    private WorldEntryAdmission _worldEntryAdmission;

    private void HoldInboxAfterEntrySelfData(ushort opcode)
    {
        WorldEntryAdmission pending = Volatile.Read(ref _worldEntryAdmission);
        if (opcode == ProtocolOpcodes.SelfData && pending != null &&
            ReferenceEquals(pending.Connection, _connectionLifetime))
            pending.SelfDataDequeued = true;
    }

    private bool MayDequeueAfterEntry()
    {
        WorldEntryAdmission pending = Volatile.Read(ref _worldEntryAdmission);
        if (pending == null)
        {
            // A failed initial drain already consumed its failing packet. Resume only its
            // untouched tail before Update can dequeue a newer native-inbox packet.
            DrainRetainedWorldPackets();
            return true;
        }
        if (pending.Cancellation.IsCancellationRequested)
        {
            if (pending.SelfDataDequeued)
            {
                // A cancelled, partially applied entry cannot resume later packets as if its
                // callback returned. Retire this connection through its existing close path.
                if (ReferenceEquals(pending.Connection, _connectionLifetime)) DisposeConnectionCore();
                return false;
            }
            Interlocked.CompareExchange(ref _worldEntryAdmission, null, pending);
            return true;
        }
        return !pending.SelfDataDequeued;
    }

    public Action CaptureWorldEntryCompletion(WorldEntryRecord selectedEntry)
    {
        WorldEntryAdmission pending = Volatile.Read(ref _worldEntryAdmission);
        if (pending == null || !ReferenceEquals(pending.ReturnedEntry, selectedEntry)) return null;
        return () => CompleteWorldEntryAttempt(pending);
    }

    private void CompleteWorldEntryAttempt(WorldEntryAdmission pending)
    {
        if (!_host.IsHostThread)
            throw new InvalidOperationException("World entry completion must run on the host thread.");
        lock (_worldDrainSync)
        {
            if (!ReferenceEquals(pending, _worldEntryAdmission) ||
                !ReferenceEquals(pending.Connection, _connectionLifetime) ||
                _stage != GatewayStage.InWorld) return;
            if (pending.Cancellation.IsCancellationRequested || pending.Connection == null ||
                pending.Connection.IsCancellationRequested)
            {
                DisposeConnectionCore();
                return;
            }
            // Called only after the selected BuildWorld attempt returned normally. A normal
            // early return is sufficient; Window/Built/_netNpcParent are not success gates.
            Interlocked.CompareExchange(ref _worldEntryAdmission, null, pending);
            BeginWorldDrain();
        }
    }

    private void ReleaseWorldEntryFenceAfterException(WorldEntryAdmission pending)
    {
        lock (_worldDrainSync)
        {
            if (!ReferenceEquals(pending, _worldEntryAdmission) ||
                !ReferenceEquals(pending.Connection, _connectionLifetime) ||
                _stage != GatewayStage.InWorld || pending.Connection == null ||
                pending.Cancellation.IsCancellationRequested ||
                pending.Connection.IsCancellationRequested) return;

            CancellationToken deliveryToken = pending.Connection.Token;
            if (!ReferenceEquals(
                    Interlocked.CompareExchange(ref _worldEntryAdmission, null, pending),
                    pending)) return;

            // Native Update propagates the callback exception for this frame, but its
            // next invocation still dequeues. Do not acknowledge a successful build,
            // retry it, or drain another packet while that exception is unwinding.
            // MayDequeueAfterEntry resumes the existing FIFO on the next Pump.
            _worldDeliveryToken = deliveryToken;
            _worldDrainActive = true;
        }
    }

    private void BeginWorldDrain()
    {
        lock (_worldDrainSync)
        {
            CancellationTokenSource connection = _connectionLifetime;
            if (connection == null || connection.IsCancellationRequested) return;
            if (_worldDrainActive)
                throw new InvalidOperationException("The previous world receive owner is still active.");
            _worldDeliveryToken = connection.Token;
            _worldDrainActive = true;
            // Preserve the existing staged FIFO before direct world delivery. The new entry
            // fence leaves subsequent native-inbox packets in that inbox until this handoff.
            DrainRetainedWorldPackets();
        }
    }

    private void DrainRetainedWorldPackets()
    {
        lock (_worldDrainSync)
        {
            while (_worldDrainActive && _stage == GatewayStage.InWorld && _dispatchedReady.Wait(0))
            {
                if (_dispatchedDownlinks.TryDequeue(out ProtocolPacket packet))
                    DeliverWorldPacket(packet);
            }
        }
    }

    private void DispatchPacket(ProtocolPacket packet)
    {
        if (TryReceiveSelectedWorldEntry(packet)) return;
        lock (_worldDrainSync)
        {
            if (_worldDrainActive && _stage == GatewayStage.InWorld)
            {
                DeliverWorldPacket(packet);
                return;
            }
            _dispatchedDownlinks.Enqueue(packet);
            _dispatchedReady.Release();
        }
    }

    // Called only under _worldDrainSync. World decode/publication completes before
    // the main-thread NetClient.Update can dispatch its next inbox message.
    // Both producers are host-thread bound: the entry completion invokes BeginWorldDrain
    // and NetClient.Update is driven by Pump from the Unity frame loop, so the
    // disposal it may trigger runs on the host as well.
    private void DeliverWorldPacket(ProtocolPacket packet)
    {
        try
        {
            _worldDeliveryToken.ThrowIfCancellationRequested();
            ProcessWorldPacket(packet, _worldDeliveryToken);
        }
        catch (OperationCanceledException) when (_worldDeliveryToken.IsCancellationRequested)
        {
            _worldDrainActive = false;
        }
        // Native Update propagates Dispatch failures. Transport drop handling belongs to
        // OnTransportError; a failed world read/owner must not become a swallowed disconnect.
    }

    // Acquiring the delivery lock waits for any in-flight synchronous publication.
    // Session return may start a roster reader only after this owner is retired.
    internal Task StopWorldDrainAsync()
    {
        lock (_worldDrainSync)
        {
            _worldDrainActive = false;
            return Task.CompletedTask;
        }
    }

    private async Task ObserveWorldDrainStopAsync(Task completion)
    {
        try { await completion.ConfigureAwait(false); }
        catch (Exception exception)
        {
            _diagnosticSink?.Invoke("protocol.world_drain.stop_failure type=" + exception.GetType().Name);
        }
    }
}

}
