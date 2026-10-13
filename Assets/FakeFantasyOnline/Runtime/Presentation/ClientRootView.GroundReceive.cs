using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IGroundItemReceiveContext
{
    bool IGroundItemReceiveContext.SpawnGroundItem(WorldGroundItemState state)
    {
        bool existed = GroundItems.TryGetForIndependentRuntime(state.GroundId, out _, out GroundItems.Item item);
        bool identityChanged = existed && (item.uniqueId != state.UniqueId || item.dropToken != state.DropToken);
        GroundItems.Spawn(state.GroundId, state.ItemId, state.Count, state.X, state.Y, state.DisplayName,
            state.Owned, state.LockMilliseconds, state.Kind, state.Quality, state.UniqueId, state.FreshDrop, state.DropToken);
        return !existed || identityChanged;
    }
    void IGroundItemReceiveContext.RemoveGroundItem(int id) => GroundItems.Remove(id);
}
}
