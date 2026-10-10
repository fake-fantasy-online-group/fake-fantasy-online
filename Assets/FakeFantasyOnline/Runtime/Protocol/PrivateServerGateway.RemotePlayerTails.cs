using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private bool TryReceiveRemotePlayerTailPacket(ushort opcode, byte[] payload,
        IWorldReceiveContext context = null)
    {
        var owner = _worldReceiveOwner as IRemotePlayerTailReceiveOwner;
        if (owner == null) return false;
        if (opcode == 0x803f)
        {
            ReceivePlayerHair(payload, context as IPlayerHairReceiveContext ?? owner, owner);
            return true;
        }
        if (opcode == ProtocolOpcodes.PlayerAppearance)
        {
            WorldPlayerAppearanceState state = DecodePlayerAppearanceAtReceipt(payload);
            if (state.EntityId == 0 || state.EntityId == owner.SelfEntityId)
                owner.ReceiveLocalAppearance(state);
            else
                owner.ReceiveRemoteAppearance(state);
            lock (_worldStateSync)
                PublishPlayerAppearanceStateLocked(state, owner.SelfEntityId, true);
            return true;
        }
        if (opcode == ProtocolOpcodes.RemoteRidingState)
        {
            var reader = new NetClient.Buf(payload);
            int id = reader.I32();
            bool riding = reader.U8() != 0;
            int model = reader.I32();
            bool tianmo = reader.Remaining >= 1 && reader.U8() != 0;
            int seat = 0, anchor = 0;
            if (reader.Remaining >= 5) { seat = reader.U8(); anchor = reader.I32(); }
            int seats = reader.Remaining >= 1 ? reader.U8() : 0;
            var state = new WorldRemoteRidingState(id, riding, model, tianmo, seat, anchor, seats);
            owner.ReceiveRemoteRiding(state);
            PublishRemoteRidingState(state, CancellationToken.None, true);
            return true;
        }
        if (opcode == 0x805d)
        {
            PrivateServerWorldStateDecoder.DecodeRemoteInvisibility(payload, out int id, out byte flag);
            owner.ReceiveRemoteInvisibility(id, flag != 0);
            PublishRemotePlayerInvisibility(id, flag != 0, CancellationToken.None, true);
            return true;
        }
        return false;
    }

    private static WorldPlayerAppearanceState DecodePlayerAppearanceAtReceipt(byte[] payload)
    {
        var reader = new NetClient.Buf(payload);
        int id = reader.I32();
        var appearance = new CharacterAppearanceRecord(reader.U16(), reader.U16(), reader.U16(),
            reader.U16(), reader.U16(), reader.U16(), reader.U8(), reader.U8(), reader.U16());
        string aura = reader.Str();
        string soul = reader.Str();
        string title = reader.Remaining > 0 ? reader.Str() : string.Empty;
        int glow = reader.Remaining >= 1 ? reader.U8() : 1;
        int? unlock = reader.Remaining >= 1 ? reader.U8() : (int?)null;
        return new WorldPlayerAppearanceState(id, appearance, aura, soul, title, glow, unlock);
    }
}
}
