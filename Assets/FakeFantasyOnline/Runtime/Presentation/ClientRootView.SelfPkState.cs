using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : ISelfPkStateReceiveContext
{
    // Official 1.5.8 Dispatch case 0x8052 (arm64 0x251be4 / x86_64 0x260bc8) writes its six
    // CharData statics and calls SysConfigDlg.SyncPkFromServer while the case body runs, so the
    // side effects happen once per received packet and not once per rendered frame. The gateway
    // therefore hands every receipt to this host in NetClient.Update's inbox order, which is the
    // order the official dispatcher runs: a later 0x8053 self byte overwrites the earlier 0x8052
    // pkState, and the reverse order leaves the 0x8052 value in place.
    void ISelfPkStateReceiveContext.ReceiveSelfPkState(WorldSelfPkState state)
    {
        CharData.BindPkStateForIndependentRuntime(state);
    }
}
}
