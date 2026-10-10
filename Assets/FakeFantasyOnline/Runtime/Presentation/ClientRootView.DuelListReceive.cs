using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IDuelListReceiveOwner
{
    void IDuelListReceiveOwner.ReceiveDuelList(WorldDuelState state) =>
        ReferenceWorldReceiveOwner.ReceiveDuelListForIndependentRuntime(state);
}
}
