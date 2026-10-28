using System;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class DialogUI
{
    private WorldNpcState spiritNpc;
    private int spiritNpcMap;
    private EndgameClientState spiritNpcState;
    private long spiritNpcGeneration;
    private object spiritNpcBindings;
    private void CaptureSpiritWorkbench(WorldNpcState npc)
    {
        spiritNpc = npc; spiritNpcMap = GameBootstrap.CurMapId; spiritNpcState = EndgamePanel.WorldState;
        spiritNpcGeneration = spiritNpcState?.Generation ?? -1; spiritNpcBindings = spiritNpcState?.Spirits;
    }
    private bool SpiritNpcCurrent(WorldNpcState source)
    {
        var state = EndgamePanel.WorldState;
        return source != null && ReferenceEquals(source, spiritNpc) && ReferenceEquals(state, spiritNpcState) && state != null &&
            state.Generation == spiritNpcGeneration && ReferenceEquals(state.Spirits, spiritNpcBindings) && spiritNpcMap == GameBootstrap.CurMapId &&
            GameBootstrap.I?.Window?.IsNpcStateCurrentForIndependentRuntime(source) == true &&
            EndgamePanel.SpiritNpcDefinition(spiritNpcMap, source.DisplayName, source.Script) != null;
    }
    private void AddSpiritWorkbenchOptions()
    {
        var source = spiritNpc; if (!SpiritNpcCurrent(source)) return;
        AddSpiritOption(source, "解封", "unseal"); AddSpiritOption(source, "加工 / 回退", "process"); AddSpiritOption(source, "归元", "reset");
    }
    private void AddSpiritOption(WorldNpcState source, string label, string mode)
    {
        Add(new ReferenceNpcDialogOption(label, () =>
        {
            if (EndgamePanel.TryOpenSpiritFromNpc(spiritNpcMap, source.EntityId, source.DisplayName, source.Script, mode, () => SpiritNpcCurrent(source))) Close();
            else ShowSay("此处灵息已变，请重新交谈。");
        }));
    }
}
}
