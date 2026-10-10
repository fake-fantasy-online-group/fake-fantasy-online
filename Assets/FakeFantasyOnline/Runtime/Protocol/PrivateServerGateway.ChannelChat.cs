using System.Collections.Generic;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private void ReceiveChannelChat(byte[] payload, IChannelChatReceiveContext context,
        ICombatBubbleReceiveOwner bubbleOwner = null)
    {
        PrivateServerWorldStateDecoder.DecodeChannelChat(payload,
            out int senderId, out string sender, out byte bubble, out byte channel, out string text,
            out IReadOnlyList<WorldChatSharedEntryState> shares, out byte tier, out byte skin);
        context.ReceiveChannelChat(channel, sender, text, tier, skin, shares);
        bool bubbleHandled = false;
        if (channel == 0 && bubbleOwner != null)
        {
            // Original 8039: ReadShares/optional tail, ChatUI.Add, then ShowPlayerBubble.
            // A prefix failure prevents this call; a bubble failure preserves the added line
            // and escapes before any deferred envelope can retry the bubble.
            bubbleOwner.ReceivePlayerBubble(senderId, text, bubble == 1 ? "red" : "huang");
            bubbleHandled = true;
        }
        // Allocate the observation envelope only after all native callbacks have run.
        var chat = new PendingChannelChat(senderId, sender, bubble, channel, text, shares, tier, skin)
        { NativeBubbleHandled = bubbleHandled };
        // Retained startup/world rows only observe a bubble already handled at receipt.
        DispatchPacket(new ProtocolPacket(ProtocolOpcodes.ChannelChat, payload, channelChat: chat));
    }

    private static PendingChannelChat DecodeChannelChat(byte[] payload, bool startup)
    {
        PrivateServerWorldStateDecoder.DecodeChannelChat(payload,
            out int senderId, out string sender, out byte bubble, out byte channel, out string text,
            out IReadOnlyList<WorldChatSharedEntryState> shares, out byte tier, out byte skin);
        return new PendingChannelChat(senderId, sender, bubble, channel, text, shares, tier, skin);
    }
}
}
