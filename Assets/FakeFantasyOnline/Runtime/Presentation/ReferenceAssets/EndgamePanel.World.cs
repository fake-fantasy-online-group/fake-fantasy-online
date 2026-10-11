using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // Presentation availability only: opening this view does not grant NPC permission,
    // spend a key, or submit a recipe. Those decisions remain with the server.
    internal static bool WorldEntryAvailable => Current != null && Current.client != null &&
        Current.client.EndgameInWorld && Current.State != null && Current.State.Negotiated;

    internal static bool TryOpenRiftFromWorld() => TryOpenWorldView(3);
    internal static bool TryOpenCubeFromWorld() => TryOpenWorldView(2);

    private static bool TryOpenWorldView(int targetTab)
    {
        // Recheck on activation: a dialog row may outlive a disconnect or negotiation.
        if (!WorldEntryAvailable) return false;
        EndgamePanel panel = Current;
        if (panel.generation != panel.State.Generation)
        {
            panel.ClearSelection();
            // An NPC may open the view before the first Update after binding/reset.
            // Synchronize now so that Update cannot immediately close the new view.
            panel.generation = panel.State.Generation;
        }
        panel.confirmLeave = false;
        panel.tab = targetTab;
        panel.detailScroll = Vector2.zero;
        panel.open = true;
        panel.Refresh();
        panel.EnsureCatalogs();
        return true;
    }
}
}
