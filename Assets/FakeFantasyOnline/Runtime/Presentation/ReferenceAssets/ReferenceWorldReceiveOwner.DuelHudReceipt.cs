using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IDuelHudReceiveOwner
{
    public void ReceiveDuelHud(WorldDuelHudState state) =>
        ReceiveDuelHudForIndependentRuntime(state);

    internal static void ReceiveDuelHudForIndependentRuntime(WorldDuelHudState state)
    {
        // Original Dispatch calls Apply here. Its clocks, map read, Clear branch and lazy
        // scene-root creation belong to receipt, including while no world frame exists.
        DuelHud.Apply(state.Active, state.Phase, state.Role, state.Countdown, state.CenterX,
            state.CenterY, state.Radius, state.Gap, state.AName, state.BName, state.WinnerName,
            state.Reason, state.LootName, state.Mode, state.EntryGold);
    }
}
}
