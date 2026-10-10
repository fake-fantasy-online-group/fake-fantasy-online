using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IDuelHudReceiveOwner
{
    void IDuelHudReceiveOwner.ReceiveDuelHud(WorldDuelHudState state) =>
        ReferenceWorldReceiveOwner.ReceiveDuelHudForIndependentRuntime(state);
}
}
