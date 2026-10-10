using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IHistoryReceiveContext
{
    IList<(string, long, string, byte, string, int)> IHistoryReceiveContext.GmFeedbacks => NetClient.GmFeedbacks;
    IList<(int, string, int, int, byte)> IHistoryReceiveContext.VoteOptions => NetClient.VoteOptions;
    void IHistoryReceiveContext.AdvanceGmFeedbackRevision() =>
        NetClient.GmFeedbackVer = unchecked(NetClient.GmFeedbackVer + 1);
    void IHistoryReceiveContext.AdvanceVoteRevision() =>
        NetClient.VoteVer = unchecked(NetClient.VoteVer + 1);
}
}
