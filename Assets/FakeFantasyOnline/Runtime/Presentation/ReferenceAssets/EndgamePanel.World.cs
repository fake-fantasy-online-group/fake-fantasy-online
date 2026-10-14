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

    private bool RiftEntryContextAvailable => State != null && State.RiftContextKnown && !State.RiftInInstance;

    private int riftEntranceMapID, riftEntranceEntityID;
    private string riftEntranceNpc, riftEntranceScript;
    private Func<bool> riftEntranceSourceCurrent;
    private object riftEntranceBindings;

    internal static bool RiftEntranceAvailable(int mapID, int entityID, string npc, string script)
    {
        if (!WorldEntryAvailable || !Current.RiftEntryContextAvailable || mapID != GameBootstrap.CurMapId || entityID == 0) return false;
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

    // Outside-run entry/recovery carries no NPC authority. Fresh paid entry requires a live entrance dialog.
    internal static bool TryOpenRiftFromWorld() => TryOpenWorldView(EndgameModule.Rift);
    internal static bool TryOpenCubeFromWorld() => false;
    // Legacy scene workbenches no longer authorize the portable tool.
    internal static bool TryOpenCubeFromNpc(int mapID, int entityID, string script, Func<bool> sourceCurrent) => false;

    internal static bool CubeItemAvailableForIndependentRuntime(EndgameClientState state) => CubeItemIDForIndependentRuntime(state) > 0;
    internal static int CubeItemIDForIndependentRuntime(EndgameClientState state)
    {
        if (state == null || !state.Negotiated || !state.CubeInputsV1 || state.CubeItemID <= 0) return 0;
        for (int tab = 0; tab < InventoryDlg.Items.Length; tab++)
            foreach (var item in InventoryDlg.Items[tab])
                if (CubeBagItemCurrent(state, tab, item.bagIdx)) return state.CubeItemID;
        return 0;
    }
    private static bool CubeBagItemCurrent(EndgameClientState state, int tab, int slot)
    {
        if (state == null || !state.Negotiated || !state.CubeInputsV1 || state.CubeItemID <= 0 ||
            tab < 0 || tab >= InventoryDlg.Items.Length || slot < 0) return false;
        var item = InventoryDlg.ItemAt(tab, slot);
        return item.id == state.CubeItemID && item.count > 0 && !InventoryDlg.IsLocked((byte)tab, slot);
    }
    internal static bool TryOpenCubeFromItem(int tab, int bagSlot)
    {
        if (!WorldEntryAvailable || SysMsgBox.Visible || !CubeBagItemCurrent(Current.State, tab, bagSlot)) return false;
        if (!TryOpenWorldView(EndgameModule.Cube)) return false;
        Current.cubeMapID = GameBootstrap.CurMapId;
        Current.cubeItemID = Current.State.CubeItemID; Current.cubeItemTab = tab; Current.cubeItemSlot = bagSlot;
        return true;
    }
    // Handled means this is the configured permanent tool, even if opening is blocked.
    // Never fall through to an ordinary consumable or start its item cooldown.
    internal static bool HandleCubeItemForIndependentRuntime(int id, int tab, int slot)
    {
        if (Current?.State == null || id <= 0 || id != Current.State.CubeItemID) return false;
        TryOpenCubeFromItem(tab, slot);
        return true;
    }
    internal static bool RecoverCubeFromNpc(EndgameClientState state, int entityID)
    {
        if (!WorldEntryAvailable || !ReferenceEquals(Current.State, state) || state.CubeItemID <= 0 || entityID == 0) return false;
        // The item is absent during recovery, so the artisan must also provide the
        // same immutable-request retry path that an open container would offer.
        if (state.Pending) return state.PendingRequest.Op == "cube.recover" && Current.client.RetryEndgame();
        return state.InventorySnapshotCurrent && Current.Send(new EndgameRequest("cube.recover", state.Revision, npcEntityID: entityID));
    }

    private static bool TryOpenWorldView(EndgameModule targetModule)
    {
        // Recheck on activation: a dialog row may outlive a disconnect or negotiation.
        if (!WorldEntryAvailable || targetModule == EndgameModule.Rift && !Current.RiftEntryContextAvailable) return false;
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
