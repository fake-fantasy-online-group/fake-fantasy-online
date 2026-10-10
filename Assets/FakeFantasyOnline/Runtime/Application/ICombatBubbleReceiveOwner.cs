namespace FakeFantasyOnline.Application
{
/// <summary>Receipt-time delivery to the original 8019/8025/8039 bubble and emote owners.</summary>
public interface ICombatBubbleReceiveOwner : IChannelChatReceiveContext
{
    void ReceiveEntityBubble(int id, string text);
    void ReceivePlayerBubble(int id, string text, string theme);
    void ReceiveEmote(int id, int face);
}
}
