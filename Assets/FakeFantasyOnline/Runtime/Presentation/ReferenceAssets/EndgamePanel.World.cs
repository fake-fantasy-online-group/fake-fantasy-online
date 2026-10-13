using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // Presentation availability only: opening this view does not grant NPC permission,
    // spend a key, or submit a recipe. Those decisions remain with the server.
    internal static bool WorldEntryAvailable => Current != null && Current.client != null &&
        Current.client.EndgameInWorld && Current.State != null && Current.State.Negotiated;

    internal static EndgameClientState WorldState => WorldEntryAvailable ? Current.State : null;

    private int riftEntranceMapID, riftEntranceEntityID;
    private string riftEntranceNpc, riftEntranceScript;
    private Func<bool> riftEntranceSourceCurrent;
    private object riftEntranceBindings;

    internal static bool RiftEntranceAvailable(int mapID, int entityID, string npc, string script)
    {
        if (!WorldEntryAvailable || mapID != GameBootstrap.CurMapId || entityID == 0) return false;
        foreach (var entrance in Current.State.RiftEntrances)
            if (entrance.Matches(mapID, npc, script)) return true;
        return false;
    }
    internal static bool TryOpenRiftFromNpc(int mapID, int entityID, string npc, string script, Func<bool> sourceCurrent)
    {
        if (!RiftEntranceAvailable(mapID, entityID, npc, script) || sourceCurrent == null || !sourceCurrent()) return false;
        if (!TryOpenWorldView(EndgameModule.Rift)) return false;
        Current.riftEntranceMapID = mapID; Current.riftEntranceEntityID = entityID;
        Current.riftEntranceNpc = npc; Current.riftEntranceScript = script;
        Current.riftEntranceSourceCurrent = sourceCurrent; Current.riftEntranceBindings = Current.State.RiftEntrances;
        return true;
    }
    private void ClearRiftEntrance()
    {
        riftEntranceMapID = riftEntranceEntityID = 0;
        riftEntranceNpc = riftEntranceScript = null; riftEntranceSourceCurrent = null; riftEntranceBindings = null;
    }
    private bool RiftEntranceCurrent()
    {
        bool valid = State != null && generation == State.Generation && ReferenceEquals(riftEntranceBindings, State.RiftEntrances) &&
            RiftEntranceAvailable(riftEntranceMapID, riftEntranceEntityID, riftEntranceNpc, riftEntranceScript) &&
            riftEntranceSourceCurrent != null && riftEntranceSourceCurrent();
        if (!valid) ClearRiftEntrance();
        return valid;
    }

    // HUD review carries no NPC authority. Only fresh paid entry requires a live entrance dialog.
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
