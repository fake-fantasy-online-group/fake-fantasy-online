using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // RxLoop allocates a separate payload array for each inbox packet. The native
    // reader never mutates it and 805e is consumed before retained packet queues.
    // Keep it only for an explicitly requested immutable observation, not as HUD state.
    private byte[] _targetStatusObservedPayload;

    // Called by both Pump forms on the host thread before any stage-specific retention.
    // The setter is reached once after the complete row loop and before the next packet.
    private bool TryReceiveTargetStatusPacket(
        ushort opcode, byte[] payload, ITargetStatusReceiveOwner owner)
    {
        if (opcode != ProtocolOpcodes.TargetStatusSnapshot) return false;
        WorldTargetStatusState state = null;
        if (owner != null)
            owner.ReceiveTargetStatus(new NetClient.Buf(payload));
        else
            PrivateServerWorldStateDecoder.DecodeTargetStatusState(payload, out state);
        // These existing API properties are observations only. A context-free data host can
        // retain a reply without a Presentation owner, but no later window may replay it.
        lock (_worldStateSync)
        {
            _targetStatusObservedPayload = owner != null ? payload : null;
            Volatile.Write(ref _targetStatusState, state);
            Interlocked.Increment(ref _targetStatusStateVersion);
        }
        return true;
    }

    private WorldTargetStatusState ObserveTargetStatusState()
    {
        lock (_worldStateSync)
        {
            if (_targetStatusState == null && _targetStatusObservedPayload != null)
            {
                // This is the existing pure DTO parser. It does not call the native
                // receiver, sample its clock, poll, or replay the target-status setter.
                PrivateServerWorldStateDecoder.DecodeTargetStatusState(
                    _targetStatusObservedPayload, out var state);
                Volatile.Write(ref _targetStatusState, state);
                _targetStatusObservedPayload = null;
            }
            return _targetStatusState;
        }
    }
}
}
