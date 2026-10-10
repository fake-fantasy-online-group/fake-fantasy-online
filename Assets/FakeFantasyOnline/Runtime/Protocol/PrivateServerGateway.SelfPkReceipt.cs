using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    // Both Pump forms call this before stage retention. Every successful read reaches its
    // original store; SyncPk lies between the third store and the fourth read.
    private void ReceiveSelfPkPacket(byte[] payload, ISelfPkStateReceiveContext compatibility = null)
    {
        if (!(_worldReceiveOwner is ISelfPkReceiptOwner owner))
        {
            // Existing custom/headless hosts retain their complete-snapshot adapter.
            PrivateServerWorldStateDecoder.DecodeSelfPkState(payload, out WorldSelfPkState state);
            PublishSelfPkState(state, CancellationToken.None);
            compatibility?.ReceiveSelfPkState(state);
            return;
        }

        var reader = new NetClient.Buf(payload);
        PublishSelfPkState(owner.StoreSelfPkMode((byte)reader.U8()), CancellationToken.None);
        PublishSelfPkState(owner.StoreSelfPkKarma(reader.I64()), CancellationToken.None);
        PublishSelfPkState(owner.StoreSelfPkState((byte)reader.U8()), CancellationToken.None);
        owner.SynchronizeSelfPkMode();
        PublishSelfPkState(owner.StoreSelfPkMapSafe((byte)reader.U8()), CancellationToken.None);
        PublishSelfPkState(owner.StoreSelfPkProtectionLevel(reader.I32()), CancellationToken.None);
        byte mapFree = reader.Remaining >= 1 ? (byte)reader.U8() : (byte)0;
        PublishSelfPkState(owner.StoreSelfPkMapFree(mapFree), CancellationToken.None);
    }
}
}
