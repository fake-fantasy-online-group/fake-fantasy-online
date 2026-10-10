using System;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // The same distinct, read-only RxLoop payload retained by the target-status observer.
    // No copied buffer, row mirror, factory delegate or per-receipt holder is allocated here.
    private byte[] _buffObservedPayload;

    private bool ReadNativeBuffState(byte[] payload)
    {
        if (_worldReceiveOwner is IWorldBuffReceiveOwner native)
            return native.ReceiveBuffState(new NetClient.Buf(payload), null, null);
        // Preserve the existing custom/unbound adapter route, including its host capabilities.
        return !ReferenceEquals(PrivateServerWorldStateDecoder.ReadBuffStateInto(
            payload, HostRealtimeSeconds, HostIsCustomMap, _worldReceiveOwner), WorldBuffState.Empty);
    }

    private void PublishBuffPayload(byte[] payload, bool nonEmpty, CancellationToken cancellationToken)
    {
        if (payload == null) throw new ArgumentNullException(nameof(payload));
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _buffObservedPayload = nonEmpty ? payload : null;
            Volatile.Write(ref _buffState, nonEmpty ? null : WorldBuffState.Empty);
            Interlocked.Increment(ref _buffStateVersion);
        }
    }

    private WorldBuffState ObserveBuffState()
    {
        lock (_worldStateSync)
        {
            if (_buffObservedPayload != null)
            {
                // Pure observer reads ALL original wire rows. It never probes the current map,
                // samples a row clock or writes CharData, and therefore cannot replay a receipt.
                WorldBuffState state = PrivateServerWorldStateDecoder.DecodeBuffState(_buffObservedPayload);
                Volatile.Write(ref _buffState, state);
                _buffObservedPayload = null;
            }
            return _buffState;
        }
    }
}
}
