namespace FakeFantasyOnline.Application
{
/// <summary>
/// Main-thread trade receipt capability. Values have passed the existing complete-message
/// decoder; the owner applies each receipt once, before startup/world observation queues.
/// </summary>
public interface ITradeReceiveOwner
{
    void ReceiveTradeOpen(int otherEntityId, string otherName);
    void ReceiveTradeState(WorldActiveTradeState state);
    void ReceiveTradeEnd(WorldTradeEndState end);
}
}
