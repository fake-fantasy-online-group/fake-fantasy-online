using System.Collections.Generic;
namespace FakeFantasyOnline.Application
{
public interface IHistoryReceiveContext
{
    IList<(string, long, string, byte, string, int)> GmFeedbacks { get; }
    IList<(int, string, int, int, byte)> VoteOptions { get; }
    void AdvanceGmFeedbackRevision();
    void AdvanceVoteRevision();
}
}
