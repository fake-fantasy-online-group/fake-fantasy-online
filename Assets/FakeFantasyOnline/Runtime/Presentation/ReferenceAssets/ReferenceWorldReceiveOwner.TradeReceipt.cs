using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ITradeReceiveOwner
{
    public void ReceiveTradeOpen(int otherEntityId, string otherName) =>
        ReferenceTradeReceipt.Open(otherEntityId, otherName);
    public void ReceiveTradeState(WorldActiveTradeState state) =>
        ReferenceTradeReceipt.State(state);
    public void ReceiveTradeEnd(WorldTradeEndState end) =>
        ReferenceTradeReceipt.End(end);
}
}
