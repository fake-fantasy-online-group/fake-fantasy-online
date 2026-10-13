using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private WorldNpcState riftEntranceNpc;
    private int riftEntranceMapID;
    private EndgameClientState riftEntranceState;
    private long riftEntranceGeneration;
    private object riftEntranceBindings;

    private void CaptureRiftEntrance(WorldNpcState npc)
    {
        riftEntranceNpc = npc; riftEntranceMapID = GameBootstrap.CurMapId;
        riftEntranceState = EndgamePanel.WorldState;
        riftEntranceGeneration = riftEntranceState?.Generation ?? -1;
        riftEntranceBindings = riftEntranceState?.RiftEntrances;
    }

    // The visible label is not identity. Both native UI and agent actions use this
    // exact current world actor, matching the database-owned map/name/script binding.
    internal int RiftEntranceEntityForIndependentRuntime(EndgameClientState state)
    {
        WorldNpcState source = riftEntranceNpc;
        if (state == null || !state.Negotiated || !ReferenceEquals(state, riftEntranceState) ||
            state.Generation != riftEntranceGeneration || !ReferenceEquals(state.RiftEntrances, riftEntranceBindings) ||
            source == null || source.EntityId == 0 || riftEntranceMapID != GameBootstrap.CurMapId ||
            GameBootstrap.I?.Window?.IsNpcStateCurrentForIndependentRuntime(source) != true) return ClearRiftEntrance();
        foreach (var entrance in state.RiftEntrances)
            if (entrance.Matches(riftEntranceMapID, source.DisplayName, source.Script)) return source.EntityId;
        return ClearRiftEntrance();
    }

    private int ClearRiftEntrance()
    {
        riftEntranceNpc = null; riftEntranceState = null; riftEntranceBindings = null;
        riftEntranceMapID = 0; riftEntranceGeneration = -1;
        return 0;
    }

    private void AddRiftEntranceOption()
    {
        WorldNpcState source = riftEntranceNpc;
        int mapID = riftEntranceMapID;
        if (source == null || RiftEntranceEntityForIndependentRuntime(EndgamePanel.WorldState) == 0) return;
        Add(new ReferenceNpcDialogOption("演劫洞天 · 查看符钥与开境", () =>
        {
            bool opened = EndgamePanel.TryOpenRiftFromNpc(mapID, source.EntityId, source.DisplayName, source.Script,
                () => ReferenceEquals(riftEntranceNpc, source) &&
                    RiftEntranceEntityForIndependentRuntime(EndgamePanel.WorldState) == source.EntityId);
            if (opened) Close();
            else ShowSay("当前入口已变更，请重新与使者交谈。");
        }));
    }
}
}
