using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IChannelChatReceiveContext
{
    void IChannelChatReceiveContext.ReceiveChannelChat(byte channel, string sender, string text,
        byte tier, byte skin, IReadOnlyList<WorldChatSharedEntryState> shares) =>
        ChatUI.ReceiveChannelForIndependentRuntime(channel, sender, text, tier, skin, ChatUI.MapIncomingShares(shares));
}
}
