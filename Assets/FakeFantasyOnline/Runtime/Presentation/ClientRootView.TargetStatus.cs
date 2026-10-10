using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : ITargetStatusReceiveOwner
{
    void ITargetStatusReceiveOwner.ReceiveTargetStatus(ITargetStatusReader reader) =>
        ReferenceTargetStatusAdapter.ReceiveIntoNativeOwner(reader);
}
}
