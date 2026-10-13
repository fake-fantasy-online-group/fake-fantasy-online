using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private WorldNpcState cubeWorkbenchNpc;
    private int cubeWorkbenchMapID;
    private EndgameClientState cubeWorkbenchState;
    private long cubeWorkbenchGeneration;
    private void CaptureCubeWorkbench(WorldNpcState npc)
    {
        cubeWorkbenchNpc = npc; cubeWorkbenchMapID = GameBootstrap.CurMapId;
        cubeWorkbenchState = EndgamePanel.WorldState;
        cubeWorkbenchGeneration = cubeWorkbenchState?.Generation ?? -1;
    }
    private bool CubeWorkbenchCurrent(WorldNpcState source)
    {
        var state = EndgamePanel.WorldState;
        return source != null && ReferenceEquals(cubeWorkbenchNpc, source) && source.EntityId != 0 &&
            source.Script == "Nzhoutianding" && state != null && state.CubeInputsV1 &&
            ReferenceEquals(state, cubeWorkbenchState) && state.Generation == cubeWorkbenchGeneration &&
            cubeWorkbenchMapID == GameBootstrap.CurMapId && GameBootstrap.I?.Window?.IsNpcStateCurrentForIndependentRuntime(source) == true;
    }
    internal int CubeWorkbenchEntityForIndependentRuntime(EndgameClientState state)
    {
        return state != null && state.Negotiated && ReferenceEquals(state, cubeWorkbenchState) &&
            CurrentNpcEntityId == cubeWorkbenchNpc?.EntityId && CubeWorkbenchCurrent(cubeWorkbenchNpc)
            ? cubeWorkbenchNpc.EntityId : 0;
    }
    private void AddCubeWorkbenchOption()
    {
        var source = cubeWorkbenchNpc;
        if (!CubeWorkbenchCurrent(source)) return;
        Add(new ReferenceNpcDialogOption("打开周天鼎", () =>
        {
            bool opened = EndgamePanel.TryOpenCubeFromNpc(cubeWorkbenchMapID, source.EntityId, source.Script, () => CubeWorkbenchCurrent(source));
            if (opened) Close();
            else ShowSay("请重新与周天鼎交谈。");
        }));
    }
}
}
