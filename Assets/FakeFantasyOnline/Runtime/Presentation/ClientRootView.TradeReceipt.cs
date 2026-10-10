using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : ITradeReceiveOwner
{
    void ITradeReceiveOwner.ReceiveTradeOpen(int otherEntityId, string otherName) =>
        ReferenceTradeReceipt.Open(otherEntityId, otherName);
    void ITradeReceiveOwner.ReceiveTradeState(WorldActiveTradeState state) =>
        ReferenceTradeReceipt.State(state);
    void ITradeReceiveOwner.ReceiveTradeEnd(WorldTradeEndState end) =>
        ReferenceTradeReceipt.End(end);
}
}
