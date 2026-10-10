using System.Threading;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    private bool TryReceiveCombatBubblePacket(ushort opcode, byte[] payload,
        IWorldReceiveContext context = null)
    {
        ICombatBubbleReceiveOwner owner = _worldReceiveOwner as ICombatBubbleReceiveOwner;
        if (owner == null) return false; // Detached diagnostic inputs retain their explicit route.
        if (opcode == ProtocolOpcodes.EntityBubble)
        {
            var reader = new NetClient.Buf(payload);
            int id = reader.I32();
            string text = reader.Str();
            owner.ReceiveEntityBubble(id, text);
            PublishEntityBubble(id, text, CancellationToken.None, true);
            return true;
        }
        if (opcode == ProtocolOpcodes.Emote)
        {
            var reader = new NetClient.Buf(payload);
            int id = reader.I32();
            int face = reader.I32();
            owner.ReceiveEmote(id, face);
            PublishEmote(id, face, CancellationToken.None, true);
            return true;
        }
        if (opcode == ProtocolOpcodes.ChannelChat)
        {
            ReceiveChannelChat(payload, context as IChannelChatReceiveContext ?? owner, owner);
            return true;
        }
        return false;
    }
}
}
