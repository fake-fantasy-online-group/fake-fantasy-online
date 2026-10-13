using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private WorldNpcState riftExitNpc;
    private int riftExitMapID;
    private long riftExitDialogGeneration;
    private EndgameClientState riftExitState;
    private long riftExitStateGeneration;

    private void CaptureRiftExit(WorldNpcState npc)
    {
        riftExitNpc = npc;
        riftExitMapID = GameBootstrap.CurMapId;
        riftExitState = EndgamePanel.WorldState;
        riftExitStateGeneration = riftExitState?.Generation ?? -1;
        riftExitDialogGeneration++;
    }

    // The server publishes this actor only after durable guardian completion.
    // Keep the native dialogue and trialleave route, with no second confirmation.
    private void AddRiftExitOptions()
    {
        // Historical frozen runs may still publish the former guide's name.
        if (!string.Equals(NpcName, "洞天残魂", StringComparison.Ordinal) &&
            !string.Equals(NpcName, "归元道使", StringComparison.Ordinal)) return;
        WorldNpcState source = riftExitNpc;
        if (source == null || source.EntityId == 0 ||
            !string.Equals(source.Script, "endgame_rift_exit", StringComparison.Ordinal)) return;
        long dialogue = riftExitDialogGeneration;
        Greeting = "好久没见到别的人了……这里的风，总算静下来了。来吧，我这就送你回去。";
        Add(new ReferenceNpcDialogOption("劳烦送我回去", () =>
        {
            // Closing, rebuilding for another actor, reconnecting, or a second
            // click cannot revive this dialogue's one-shot exit intent.
            if (!Visible || dialogue != riftExitDialogGeneration) return;
            EndgameClientState state = EndgamePanel.WorldState;
            if (state == null || !ReferenceEquals(state, riftExitState) ||
                state.Generation != riftExitStateGeneration ||
                !ReferenceEquals(source, riftExitNpc) || riftExitMapID != GameBootstrap.CurMapId ||
                GameBootstrap.I?.Window?.IsNpcStateCurrentForIndependentRuntime(source) != true ||
                NetClient.I == null)
            {
                Close();
                return;
            }
            Close();
            NetClient.I.GmCmdForIndependentRuntime("trialleave");
        }));
    }
}
}
