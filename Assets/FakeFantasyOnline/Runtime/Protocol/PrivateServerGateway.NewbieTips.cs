using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    public Task<Result> MarkNewbieTipSeenAsync(int tipId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TipSeen(tipId);
            return Task.FromResult(Result.Success());
        }

        return MarkNewbieTipSeenScheduledAsync(tipId, cancellationToken);
    }

    private async Task<Result> MarkNewbieTipSeenScheduledAsync(int tipId, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.TipSeen(tipId), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("newbie.tip.seen.send.failure", "The seen-tip notification could not be sent.");
        }
    }
}
}
