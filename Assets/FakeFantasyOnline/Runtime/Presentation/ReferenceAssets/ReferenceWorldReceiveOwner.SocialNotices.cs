using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : ISocialNoticeReceiveOwner
{
    public void ReceivePrivateChat(string sender, string text,
        IReadOnlyList<WorldChatSharedEntryState> shares) =>
        GossipDlg.ReceiveForIndependentRuntime(sender, text, ChatUI.MapIncomingShares(shares));

    public void ReceiveSystemRequests(List<(int, byte, int, string, string)> requests) =>
        SystemInfoBar.SetList(requests);

    public void ReceiveSystemRequests(IReadOnlyList<WorldSystemRequestState> requests) =>
        ReceiveSystemRequestsForIndependentRuntime(requests);

    internal static void ReceiveSystemRequestsForIndependentRuntime(
        IReadOnlyList<WorldSystemRequestState> requests)
    {
        var rows = new List<(int, byte, int, string, string)>(requests.Count);
        foreach (WorldSystemRequestState row in requests)
            rows.Add((row.RequestId, row.Kind, row.FromEntityId, row.FromName, row.Text));
        SystemInfoBar.SetList(rows);
    }
}
}
