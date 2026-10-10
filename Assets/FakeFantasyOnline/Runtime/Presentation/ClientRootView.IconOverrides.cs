using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IItemIconOverrideReceiveContext
{
    void IItemIconOverrideReceiveContext.EnsureIconMap() => NetClient.EnsureIconMapForIndependentRuntime();
    void IItemIconOverrideReceiveContext.ApplyIconOverride(int itemId, int iconId) =>
        NetClient.ApplyIconOverrideForIndependentRuntime(itemId, iconId);
    void IItemIconOverrideReceiveContext.ClearGroundIconCache() => GroundItems.ClearIconCache();
}
}
