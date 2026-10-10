using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{

public sealed partial class ClientRootView : INpcNoticeReceiveOwner
{
    // Every packet borrows the live original singleton, independent of map replacement.
    public DialogUI NpcDialogAtReceiptForIndependentRuntime() => DialogUI.I;

    void INpcNoticeReceiveOwner.ReceiveNpcNotice(string text, byte displayMode) =>
        ReferenceNpcNoticeReceipt.ReceiveNotice(text, displayMode, NpcDialogAtReceiptForIndependentRuntime);

    void INpcNoticeReceiveOwner.ReceiveInvitationCodeResult(WorldInvitationCodeResult result) =>
        ReferenceNpcNoticeReceipt.ReceiveInvitationCodeResult(result, NpcDialogAtReceiptForIndependentRuntime);
}

}
