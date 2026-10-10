using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ICombatBubbleReceiveOwner
{
    public void ReceiveEntityBubble(int id, string text) => CombatWorld.ShowBubble(id, text);
    public void ReceivePlayerBubble(int id, string text, string theme) =>
        CombatWorld.ShowPlayerBubble(id, text, theme);
    public void ReceiveEmote(int id, int face) => CombatWorld.ShowEmote(id, face);

    // 8039's existing static chat prefix must return before its channel-0 bubble tail.
    // This is the same owner call as ClientRootView's contextual receipt, also when no frame exists.
    void IChannelChatReceiveContext.ReceiveChannelChat(byte channel, string sender, string text,
        byte tier, byte skin, IReadOnlyList<WorldChatSharedEntryState> shares) =>
        ChatUI.ReceiveChannelForIndependentRuntime(channel, sender, text, tier, skin, ChatUI.MapIncomingShares(shares));
}
}
