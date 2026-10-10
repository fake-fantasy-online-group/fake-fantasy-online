using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : ISocialNoticeReceiveOwner
{
    void ISocialNoticeReceiveOwner.ReceivePrivateChat(string sender, string text,
        IReadOnlyList<WorldChatSharedEntryState> shares) =>
        GossipDlg.ReceiveForIndependentRuntime(sender, text, ChatUI.MapIncomingShares(shares));
    void ISocialNoticeReceiveOwner.ReceiveSystemRequests(List<(int, byte, int, string, string)> requests) =>
        SystemInfoBar.SetList(requests);
    void ISocialNoticeReceiveOwner.ReceiveSystemRequests(IReadOnlyList<WorldSystemRequestState> requests) =>
        ReferenceWorldReceiveOwner.ReceiveSystemRequestsForIndependentRuntime(requests);
}
}
