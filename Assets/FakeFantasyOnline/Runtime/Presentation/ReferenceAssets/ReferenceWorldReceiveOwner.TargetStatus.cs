using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ITargetStatusReceiveOwner
{
    public void ReceiveTargetStatus(ITargetStatusReader reader) =>
        ReferenceTargetStatusAdapter.ReceiveIntoNativeOwner(reader);
}
}
