using System;
using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    // Both Pump entries call this on the existing host thread, before DispatchPacket can queue
    // a packet for entry/world admission. These two cases never enter that queue or replay later.
    private bool TryReceiveNpcNoticePacket(ushort opcode, byte[] payload, INpcNoticeReceiveOwner owner)
    {
        if (opcode == ProtocolOpcodes.NpcDialogText)
        {
            // Dispatch 0600077f: arm64 24f8c0/24f8d0, x86_64 25de85/25de92:
            // Str then U8, with the recovered Buf's exact UTF-8 and short-input behavior.
            // No EOF check and no try-decode envelope around either the reads or callback.
            var reader = new NetClient.Buf(payload);
            string text = reader.Str();
            byte mode = (byte)reader.U8();
            Action<string> notice = _netClient.OnNotice;
            if (notice != null)
            {
                // The delegate consumes this packet. A throw propagates through NetClient.Update;
                // neither display nor observation is attempted, and there is no queued retry.
                notice(text);
                return true;
            }

            // Mandatory case tail: absence of the injected capability is not a skip/fallback.
            // The real owner itself preserves each native no-dialog/no-controller branch.
            owner.ReceiveNpcNotice(text, mode);
            PublishNpcDialogTextState(new WorldNpcDialogTextState(text, mode), CancellationToken.None);
            return true;
        }
        if (opcode == 0x8072)
        {
            ReceiveInvitationCodeResult(payload, owner);
            return true;
        }
        return false;
    }
}

}
