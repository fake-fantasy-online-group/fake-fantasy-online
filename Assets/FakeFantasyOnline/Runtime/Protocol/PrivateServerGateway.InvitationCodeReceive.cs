using FakeFantasyOnline.Application;
using System.Threading;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // Dispatch 0600077f: arm64 252980..2529d4, x86_64 261af3..261b3d.
    // All five Buf reads precede the result/code gates; trailing bytes are not tested.
    private void ReceiveInvitationCodeResult(byte[] payload, INpcNoticeReceiveOwner owner)
    {
        var reader = new NetClient.Buf(payload);
        byte result = (byte)reader.U8();
        string code = reader.Str();
        int lifetimeSeconds = reader.I32();
        int remainingAccounts = reader.I32();
        string message = reader.Str();
        var receipt = new WorldInvitationCodeResult(result, code, lifetimeSeconds, remainingAccounts, message);
        owner.ReceiveInvitationCodeResult(receipt);
        // Retain only a read-only observation after the case effect. No frame applies this again.
        AppendNpcDialogTextEdge(null, receipt, CancellationToken.None);
    }
}

}
