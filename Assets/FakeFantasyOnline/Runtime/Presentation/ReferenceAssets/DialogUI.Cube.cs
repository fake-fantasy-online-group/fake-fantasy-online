using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private WorldNpcState cubeWorkbenchNpc;
    private int cubeLoreIndex;
    private string cubeLoreText;
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
            state != null && state.CubeInputsV1 && state.CubeItemID > 0 && state.CubeNPC.Length > 0 &&
            source.DisplayName == state.CubeNPC &&
            ReferenceEquals(state, cubeWorkbenchState) && state.Generation == cubeWorkbenchGeneration &&
            cubeWorkbenchMapID == GameBootstrap.CurMapId && GameBootstrap.I?.Window?.IsNpcStateCurrentForIndependentRuntime(source) == true;
    }
    // Retained compatibility surface: portable cube recipes never use NPC authority.
    internal int CubeWorkbenchEntityForIndependentRuntime(EndgameClientState state) => 0;
    private void AddCubeWorkbenchOption()
    {
        var source = cubeWorkbenchNpc;
        if (!CubeWorkbenchCurrent(source)) return;
        var state = cubeWorkbenchState;
        if (state.CubeLore.Length > 0)
            Add(new ReferenceNpcDialogOption("关于周天鼎", () =>
            {
                if (!CubeWorkbenchCurrent(source)) return;
                if (cubeLoreText != state.CubeLore) { cubeLoreText = state.CubeLore; cubeLoreIndex = 0; }
                var passages = state.CubeLore.Split(new[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
                if (passages.Length > 0) ShowSay(passages[cubeLoreIndex++ % passages.Length].Trim());
            }));
        Add(new ReferenceNpcDialogOption(state.PendingRequest?.Op == "cube.recover" ? "查询寻回结果" : "寻回周天鼎", () =>
        {
            if (CubeWorkbenchCurrent(source) && EndgamePanel.RecoverCubeFromNpc(state, source.EntityId)) Close();
        }));
    }
}
}
