namespace FakeFantasyOnline.Application
{
/// <summary>
/// Synchronous host-thread delivery of one completely decoded duel list. The native owner
/// applies every receipt, including empty or repeated lists, without requiring a window.
/// A gateway without this capability retains only the observation snapshot.
/// </summary>
public interface IDuelListReceiveOwner
{
    void ReceiveDuelList(WorldDuelState state);
}
}
