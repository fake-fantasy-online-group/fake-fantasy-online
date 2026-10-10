using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private ITradeStateReceiptOwner _tradeStateObservationOwner;
    private bool _tradeStateObservationDirty;

    private WorldActiveTradeState ReadActiveTradeState()
    {
        lock (_worldStateSync)
        {
            if (_tradeStateObservationDirty && _tradeStateObservationOwner != null)
            {
                WorldActiveTradeState observed = _tradeStateObservationOwner.CaptureTradeState();
                Volatile.Write(ref _activeTradeState, observed);
                _tradeStateObservationDirty = false;
            }
            return Volatile.Read(ref _activeTradeState);
        }
    }

    // Both Pump entries reach this before any stage-dependent queue. Decode and owner failures
    // propagate immediately; neither a failed packet nor a successful receipt is replayed.
    private bool TryReceiveTradePacket(ushort opcode, byte[] payload, ITradeReceiveOwner owner)
    {
        WorldActiveTradeState current = Volatile.Read(ref _activeTradeState);
        WorldActiveTradeState next;
        WorldTradeEndState end = null;
        if (opcode == ProtocolOpcodes.TradeOpen)
        {
            PrivateServerTradeCodec.DecodeOpen(payload, out next);
            // ResetTrade deliberately retains the pet maps; Empty/Open DTOs are not resets.
            next = TradeResetMirror(next, current);
            owner.ReceiveTradeOpen(next.OtherEntityId, next.OtherName);
        }
        else if (opcode == ProtocolOpcodes.TradeState)
        {
            if (owner is ITradeStateReceiptOwner stateOwner)
            {
                lock (_worldStateSync)
                {
                    try
                    {
                        PrivateServerTradeCodec.ReadStateInto(payload, stateOwner);
                    }
                    finally
                    {
                        // Metadata only: no observation allocation may stop a native write
                        // or replace the original read/owner exception. Getters copy later.
                        _tradeStateObservationOwner = stateOwner;
                        _tradeStateObservationDirty = true;
                        Interlocked.Increment(ref _activeTradeStateVersion);
                    }
                }
                return true;
            }
            // Existing custom owners without the mutable receipt port keep their adapter.
            PrivateServerTradeCodec.DecodeState(payload, current, out next);
            owner.ReceiveTradeState(next);
        }
        else if (opcode == ProtocolOpcodes.TradeEnd)
        {
            PrivateServerTradeCodec.DecodeEnd(payload, out end);
            next = TradeResetMirror(WorldActiveTradeState.Empty, current);
            owner.ReceiveTradeEnd(end);
        }
        else return false;

        // Outside every decode envelope: an owner exception escapes NetClient.Update and
        // publishes no successful receipt, queues no packet and cannot schedule an owner retry.
        // These immutable mirrors are observations of this receipt, never gameplay writers.
        lock (_worldStateSync)
        {
            Volatile.Write(ref _activeTradeState, next);
            _tradeStateObservationOwner = owner as ITradeStateReceiptOwner;
            _tradeStateObservationDirty = _tradeStateObservationOwner != null;
            Interlocked.Increment(ref _activeTradeStateVersion);
            if (end != null)
            {
                Volatile.Write(ref _tradeEndState, end);
                Interlocked.Increment(ref _tradeEndStateVersion);
            }
        }
        return true;
    }

    private static WorldActiveTradeState TradeResetMirror(
        WorldActiveTradeState reset, WorldActiveTradeState previous)
    {
        return new WorldActiveTradeState(reset.Active, reset.OtherEntityId, reset.OtherName,
            reset.MyLocked, reset.OtherLocked, reset.MyConfirmed, reset.OtherConfirmed,
            reset.MyMoney, reset.OtherMoney, reset.MyItems, reset.OtherItems,
            reset.MyItemInfo, reset.OtherItemInfo, previous?.MyPets, previous?.OtherPets);
    }
}
}
