using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<string> writeFeedback;
    private Action<string> writeVoteAdd;
    private Action<int, byte> writeVoteCast;
    internal void BindHistoryWritersForIndependentRuntime(Action<string> feedback,
        Action<string> voteAdd, Action<int, byte> voteCast)
    { writeFeedback = feedback; writeVoteAdd = voteAdd; writeVoteCast = voteCast; }
    public void SendFeedbackForIndependentRuntime(string text) => writeFeedback(text);
    public void SendVoteAddForIndependentRuntime(string text) => writeVoteAdd(text);
    public void SendVoteCastForIndependentRuntime(int id, byte stance) => writeVoteCast(id, stance);
}
}
