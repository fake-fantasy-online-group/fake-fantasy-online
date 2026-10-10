using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IHistoryCommands
{
    Task<Result> SendFeedbackAsync(string text, CancellationToken cancellationToken);
    Task<Result> SendVoteAddAsync(string text, CancellationToken cancellationToken);
    Task<Result> SendVoteCastAsync(int id, byte stance, CancellationToken cancellationToken);
}
}
