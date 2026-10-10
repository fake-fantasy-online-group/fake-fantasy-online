using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IHornDialogReceiveContext
{
    void IHornDialogReceiveContext.OpenHorn(int itemId, int tier, int skin) =>
        HornDialog.Open(itemId, tier, skin);
}
}
