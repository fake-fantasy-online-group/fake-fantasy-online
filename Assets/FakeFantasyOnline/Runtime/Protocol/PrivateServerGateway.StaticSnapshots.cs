using System.IO;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Both actual Pump paths reach Dispatch before the session/selected/world readers.
    // A successful receipt consumes the packet here. The existing collectors are retained
    // only for packets already retained by an earlier reader or an unbound custom adapter.
    private bool TryReceiveStaticSnapshotPacket(ushort opcode, byte[] payload)
    {
        var owner = _worldReceiveOwner as IWorldStaticSnapshotReceiveOwner;
        if (owner == null) return false;
        if (opcode == ProtocolOpcodes.InventorySnapshot)
        {
            WorldInventoryState state = PrivateServerInventoryCodec.DecodeSnapshot(
                payload, _worldReceiveOwner, out IWorldInventorySnapshotRows rows);
            rows.Publish(state);
            PublishInventoryState(state, CancellationToken.None);
            return true;
        }
        if (opcode == ProtocolOpcodes.InventoryDelta)
        {
            WorldInventoryState state = PrivateServerInventoryCodec.DecodeDelta(
                payload, Volatile.Read(ref _inventoryState), _worldReceiveOwner,
                out IWorldInventoryDeltaRows rows);
            rows.Publish(state);
            PublishInventoryState(state, CancellationToken.None);
            return true;
        }
        if (opcode == ProtocolOpcodes.LifeSkillSnapshot)
        {
            if (owner is IWorldLifeSkillReceiptOwner nativeOwner)
            {
                nativeOwner.ReceiveLifeSkills(new NetClient.Buf(payload));
                lock (_worldStateSync)
                {
                    _lifeSkillBookObservationPayload = payload;
                    Interlocked.Increment(ref _lifeSkillBookStateVersion);
                }
                return true;
            }
            if (!PrivateServerSkillBookCodec.TryDecodeLifeSkills(payload, out var state))
                throw new InvalidDataException("The server sent an invalid life-skill snapshot.");
            owner.ReceiveLifeSkillSnapshot(state);
            PublishLifeSkillBookState(state, CancellationToken.None);
            return true;
        }
        if (opcode == ProtocolOpcodes.PartySnapshot)
        {
            if (!PrivateServerWorldStateDecoder.TryDecodePartyState(payload, out var state))
                throw new InvalidDataException("The server sent an invalid party packet.");
            owner.ReceivePartySnapshot(state);
            PublishPartyState(state, CancellationToken.None);
            return true;
        }
        return false;
    }
}
}
