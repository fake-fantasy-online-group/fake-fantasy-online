using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IHistoryCommands
{
    public Task<Result> SendFeedbackAsync(string text, CancellationToken cancellationToken)
    {
        // DrawGmFeedback clears its input and focus only after the native sender returns.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendFeedback(text);
            return Task.FromResult(Result.Success());
        }

        return SendHistoryCommandAsync(() => _netClient.SendFeedback(text), cancellationToken);
    }

    public Task<Result> SendVoteAddAsync(string text, CancellationToken cancellationToken)
    {
        // DrawVote's nomination tail has the same synchronous return boundary.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendVoteAdd(text);
            return Task.FromResult(Result.Success());
        }

        return SendHistoryCommandAsync(() => _netClient.SendVoteAdd(text), cancellationToken);
    }

    public Task<Result> SendVoteCastAsync(int id, byte stance, CancellationToken cancellationToken)
    {
        // DrawVote consumes the click only after the native sender returns normally.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendVoteCast(id, stance);
            return Task.FromResult(Result.Success());
        }

        return SendHistoryCommandAsync(() => _netClient.SendVoteCast(id, stance), cancellationToken);
    }

    private async Task<Result> SendHistoryCommandAsync(Action send, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("history.send.failure", "The history request could not be sent."); }
    }
}
}
