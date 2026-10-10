using System.IO;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // RxLoop gives each packet its own byte array. Retaining that existing array makes
    // immutable compatibility snapshots opt-in; no DTO/list copy can interrupt a native
    // receipt or delay the next native packet. These fields never drive TaskClient again.
    private byte[] _taskLogObservationPayload;
    private bool _taskLogObservationHasEntries;
    private byte[] _npcTaskOffersObservationPayload;

    private bool TryReceiveTaskPacket(ushort opcode, byte[] payload)
    {
        if (opcode != ProtocolOpcodes.TaskSnapshot && opcode != ProtocolOpcodes.NpcTaskOffers)
            return false;
        var owner = _worldReceiveOwner as IWorldTaskReceiptOwner;
        if (owner == null) return false;

        NetClient.Buf reader = new NetClient.Buf(payload);
        if (opcode == ProtocolOpcodes.TaskSnapshot)
        {
            bool hasEntries = owner.ReceiveTaskLog(reader);
            lock (_worldStateSync)
            {
                _taskLogObservationPayload = payload;
                _taskLogObservationHasEntries = hasEntries;
                Interlocked.Increment(ref _taskLogStateVersion);
            }
        }
        else
        {
            owner.ReceiveTaskOffers(reader);
            lock (_worldStateSync)
            {
                _npcTaskOffersObservationPayload = payload;
                Interlocked.Increment(ref _npcTaskOffersStateVersion);
            }
        }
        return true;
    }

    private WorldTaskLogState ReadTaskLogState()
    {
        lock (_worldStateSync)
        {
            if (_taskLogObservationPayload != null)
            {
                if (!PrivateServerWorldStateDecoder.TryDecodeTaskLog(
                        _taskLogObservationPayload, out WorldTaskLogState state))
                    throw new InvalidDataException("The received task-log observation could not be decoded.");
                Volatile.Write(ref _taskLogState, state);
                _taskLogObservationPayload = null;
            }
            return Volatile.Read(ref _taskLogState);
        }
    }

    private WorldNpcTaskOffersState ReadNpcTaskOffersState()
    {
        lock (_worldStateSync)
        {
            if (_npcTaskOffersObservationPayload != null)
            {
                if (!PrivateServerWorldStateDecoder.TryDecodeNpcTaskOffers(
                        _npcTaskOffersObservationPayload, out WorldNpcTaskOffersState state))
                    throw new InvalidDataException("The received task-offers observation could not be decoded.");
                Volatile.Write(ref _npcTaskOffersState, state);
                _npcTaskOffersObservationPayload = null;
            }
            return Volatile.Read(ref _npcTaskOffersState);
        }
    }
}
}
