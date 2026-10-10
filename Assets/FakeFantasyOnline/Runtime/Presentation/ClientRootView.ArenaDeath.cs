using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IArenaDeathReceiveContext
{
    // Official 1.5.8 NetClient.Dispatch case 0x8061 (arm64 0x2528d0). The leading flag decides the
    // branch: 0 tail-calls ArenaDeathDlg.Close, every other value tail-calls ArenaDeathDlg.Show with
    // the values just read off the wire. Nothing here invents a third path or a default.
    void IArenaDeathReceiveContext.ReceiveArenaDeath(ArenaDeathStateData state)
    {
        if (state.Flag == 0)
        {
            ArenaDeathDlg.Close();
            return;
        }

        ArenaDeathDlg.Show(state.DeathNo, state.FreeLeft, state.Kind, state.Amount, state.Label,
            state.Afford, state.WaitSec, state.Ladder);
    }
}
}
