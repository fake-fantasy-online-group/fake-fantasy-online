using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Official Dispatch 0x8076 calls SetList after the complete row loop. Both Pump forms
    // consume the packet here, before any startup/world queue or next inbox packet.
    private void ReceiveDuelList(byte[] payload, IDuelListReceiveOwner context = null)
    {
        WorldDuelState state = ReadDuelState(payload);
        IDuelListReceiveOwner owner = _worldReceiveOwner as IDuelListReceiveOwner ?? context;
        owner?.ReceiveDuelList(state);
        // Optional/headless gateways still expose a receipt observation. No retained packet
        // or later binding can replay it into the native rows/selection.
        PublishDuelState(state);
    }
}
}
