using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // Presentation availability only: opening this view does not grant NPC permission,
    // spend a key, or submit a recipe. Those decisions remain with the server.
    internal static bool WorldEntryAvailable => Current != null && Current.client != null &&
        Current.client.EndgameInWorld && Current.State != null && Current.State.Negotiated;

    internal static bool TryOpenRiftFromWorld() => TryOpenWorldView(EndgameModule.Rift);
    internal static bool TryOpenCubeFromWorld() => TryOpenWorldView(EndgameModule.Cube);

    internal static bool TryOpenRunesFromWorld() => TryOpenWorldView(EndgameModule.Runes);

    private static bool TryOpenWorldView(EndgameModule targetModule)
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
        // No workbench may inherit another module's selected item or quote.
        panel.ClearSelection();
        panel.State.InvalidateQuote();
        panel.module = targetModule;
        panel.detailScroll = Vector2.zero;
        panel.open = true;
        panel.Refresh();
        panel.EnsureModuleData();
        return true;
    }
}
}
