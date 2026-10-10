using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IGroundPickupCommands
{
    public Task<Result> PickItemAsync(int groundId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PickItem(groundId);
            return Task.FromResult(Result.Success());
        }

        return PickItemScheduledAsync(groundId, cancellationToken);
    }

    private async Task<Result> PickItemScheduledAsync(int groundId, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.PickItem(groundId), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("ground-item.pickup.send.failure", "The pickup request could not be sent."); }
    }
}
}
