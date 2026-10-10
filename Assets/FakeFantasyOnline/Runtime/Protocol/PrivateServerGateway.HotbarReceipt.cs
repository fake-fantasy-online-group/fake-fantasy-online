using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // Both Pump entries reach this on the existing host thread before any startup/world gate.
    // Consume the packet here: no await response, queued replay or later mirror publication.
    private bool TryReceiveHotbarPacket(ushort opcode, byte[] payload, IHotbarReceiveOwner owner)
    {
        if (opcode != ProtocolOpcodes.HotbarSnapshot) return false;
        WorldHotbarState state = PrivateServerHotbarCodec.ReceiveSnapshot(payload, owner);
        // No cancellation checkpoint can separate successful native adoption from its mirror.
        PublishHotbarState(state);
        return true;
    }
}

}
