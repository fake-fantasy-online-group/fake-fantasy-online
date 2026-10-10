namespace FakeFantasyOnline.Application
{
/// <summary>
/// Synchronous host-thread application of a completely decoded 0x8075 receipt.
/// Window construction and subsequent event observation do not own this transition.
/// </summary>
public interface IDuelHudReceiveOwner
{
    void ReceiveDuelHud(WorldDuelHudState state);
}
}
