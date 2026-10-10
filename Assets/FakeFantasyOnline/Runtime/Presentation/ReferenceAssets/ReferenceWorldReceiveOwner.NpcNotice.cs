using System;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceWorldReceiveOwner : INpcNoticeReceiveOwner
{
    private readonly Func<DialogUI> noticeDialogAtReceipt;

    // The existing composition root binds a live projection, not a cached window. The default
    // reads the same native singleton even when there is no map host.
    public ReferenceWorldReceiveOwner(Func<DialogUI> noticeDialogAtReceipt = null)
    {
        this.noticeDialogAtReceipt = noticeDialogAtReceipt ?? (() => DialogUI.I);
    }

    public void ReceiveNpcNotice(string text, byte displayMode) =>
        ReferenceNpcNoticeReceipt.ReceiveNotice(text, displayMode, noticeDialogAtReceipt);

    public void ReceiveInvitationCodeResult(WorldInvitationCodeResult result) =>
        ReferenceNpcNoticeReceipt.ReceiveInvitationCodeResult(result, noticeDialogAtReceipt);
}

}
