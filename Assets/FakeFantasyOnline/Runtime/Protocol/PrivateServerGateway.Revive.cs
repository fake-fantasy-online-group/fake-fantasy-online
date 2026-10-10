using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway
{
    public Task<Result> ReviveCharacterAsync(byte option, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Revive(option);
            return Task.FromResult(Result.Success());
        }

        return ReviveCharacterScheduledAsync(option, cancellationToken);
    }

    private async Task<Result> ReviveCharacterScheduledAsync(byte option, CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
            return Result.Failure("session.stage.invalid", "The session is not in the world.");
        try
        {
            await SendAsync(() => _netClient.Revive(option), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("combat.revive.send.failure", "The revive request could not be sent.");
        }
    }
}
}
