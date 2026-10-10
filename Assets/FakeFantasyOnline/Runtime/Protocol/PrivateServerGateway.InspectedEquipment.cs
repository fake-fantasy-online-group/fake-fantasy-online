using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    private bool TryReceiveInspectedEquipmentPacket(ushort opcode, byte[] payload,
        IInspectedEquipmentReceiptOwner owner)
    {
        if (opcode != ProtocolOpcodes.InspectedPlayerEquipment) return false;
        WorldInspectedPlayerEquipmentState state;
        if (owner is IInspectedEquipmentReaderOwner readerOwner)
        {
            state = readerOwner.ReceiveInspectedEquipment(new NetClient.Buf(payload));
        }
        else
        {
            // Existing custom adapter: same recovered grammar; native read/owner failures
            // still propagate immediately through the existing inbox, without a raw retry.
            PrivateServerWorldStateDecoder.DecodeInspectedPlayerEquipment(payload, out state);
            owner.ReceiveInspectedEquipment(state);
        }
        PublishInspectedPlayerEquipmentState(state, CancellationToken.None);
        return true;
    }
}

}
