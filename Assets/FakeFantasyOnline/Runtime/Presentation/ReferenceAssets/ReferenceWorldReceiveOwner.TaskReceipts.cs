using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IWorldTaskReceiptOwner
{
    bool IWorldTaskReceiptOwner.ReceiveTaskLog(IWorldTaskReceiptReader reader) =>
        NetClient.ReceiveTaskLogForIndependentRuntime(reader);

    void IWorldTaskReceiptOwner.ReceiveTaskOffers(IWorldTaskReceiptReader reader) =>
        NetClient.ReceiveTaskOffersForIndependentRuntime(reader);
}
}
