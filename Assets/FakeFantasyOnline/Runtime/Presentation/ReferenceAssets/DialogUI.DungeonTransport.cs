using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    // Exact current SQL dungeon service lists (init-0: 101..133), not a map-ID
    // heuristic. List 114 is deliberately held for audit: its map 34200 is
    // explicitly excluded by DungeonEntrances, despite a legacy transport row.
    // 109 changes a dungeon floor and 133 leaves it; neither opens TransportUI.
    internal static bool IsNativeDungeonTransportList(int list) =>
        list >= 101 && list <= 133 && list != 114;

    private WorldTransportState dungeonTransport;
    private int dungeonTransportGeneration;
    private int dungeonTransportMap;

    internal void ObserveDungeonQuestState()
    {
        if (!Visible) return;
        bool hasTaskEntry = false;
        foreach (DungeonEntrances.Dungeon entry in DungeonEntrances.For(NpcName))
            if (DungeonQuestAccess.RequiredQuest(entry.map) != 0) hasTaskEntry = true;
        if (dungeonTransport != null)
            foreach (WorldTransportDestinationState entry in dungeonTransport.Destinations)
                if (DungeonQuestAccess.RequiredQuest(entry.MapId) != 0) hasTaskEntry = true;
        if (!hasTaskEntry) return; // Do not rebuild unrelated NPC service menus.
        // A task receipt can remove admission while an old option is retained.
        dungeonTransportGeneration++;
        if (!InSubView) BuildMenu();
    }

    private void ResetDungeonTransport()
    {
        dungeonTransport = null;
        dungeonTransportGeneration++;
        dungeonTransportMap = GameBootstrap.CurMapId;
    }

    private void BeginDungeonTransport()
    {
        if (IsNativeDungeonTransportList(curTrans)) RequestDungeonTransport();
    }

    private void RequestDungeonTransport()
    {
        if (!Visible || !IsNativeDungeonTransportList(curTrans) ||
            GameBootstrap.CurMapId != dungeonTransportMap) return;
        // Invalidate the displayed choice before opening again: the server owns
        // the new list/index context and consumes that context on selection.
        dungeonTransport = null;
        dungeonTransportGeneration++;
        if (!InSubView) BuildMenu();
        NetClient.I?.OpenTeleportForIndependentRuntime(curTrans);
    }

    // Recognized dungeon responses are always consumed, including stale/closed
    // replies. Falling through would resurrect the generic transport window.
    internal static bool ConsumeDungeonTransport(WorldTransportState next)
    {
        if (next == null || !IsNativeDungeonTransportList(next.TransportList)) return false;
        if (I != null) I.ApplyDungeonTransport(next);
        return true;
    }

    private void ApplyDungeonTransport(WorldTransportState next)
    {
        if (!Visible || curTrans != next.TransportList ||
            GameBootstrap.CurMapId != dungeonTransportMap) return;
        dungeonTransport = next;
        // Preserve task details or other NPC subviews; returning to the menu
        // will use the received rows without an unsolicited navigation change.
        if (!InSubView) BuildMenu();
    }

    private bool TryAddDungeonTransportOptions()
    {
        if (!IsNativeDungeonTransportList(curTrans)) return false;
        if (dungeonTransport == null)
        {
            Add(new ReferenceNpcDialogOption("读取副本入口", RequestDungeonTransport));
            return true;
        }
        int list = curTrans;
        int generation = dungeonTransportGeneration;
        for (int i = 0; i < dungeonTransport.Destinations.Count; i++)
        {
            WorldTransportDestinationState destination = dungeonTransport.Destinations[i];
            int index = i; // Keep the original wire index when another row is hidden.
            if (!DungeonQuestAccess.CanEnter(destination.MapId)) continue;
            string label = list == 133 ? destination.Name : "进入" + destination.Name;
            if (destination.Price > 0) label += "（" + destination.Price + "铜）";
            Add(new ReferenceNpcDialogOption(label, () =>
            {
                if (!Visible || curTrans != list || generation != dungeonTransportGeneration ||
                    GameBootstrap.CurMapId != dungeonTransportMap ||
                    !DungeonQuestAccess.CanEnter(destination.MapId)) return;
                Visible = false;
                dungeonTransport = null;
                dungeonTransportGeneration++;
                // Identical native 0x1072 writer as TransportUI; no local map
                // switch, invented task rule, or client-side success occurs.
                NetClient.I?.TeleportForIndependentRuntime(list, index);
            }));
        }
        return true;
    }
}
}
