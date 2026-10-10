using System.Collections.Generic;
namespace FakeFantasyOnline.Application
{
public interface IChannelChatReceiveContext
{
    void ReceiveChannelChat(byte channel, string sender, string text, byte tier, byte skin,
        IReadOnlyList<WorldChatSharedEntryState> shares);
}
}
