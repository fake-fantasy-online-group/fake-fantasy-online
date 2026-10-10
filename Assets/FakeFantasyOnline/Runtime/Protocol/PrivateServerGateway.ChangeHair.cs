using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : ICharacterAppearanceCommands
{
    public Task<Result> ChangeHairAsync(byte mode, byte targetId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ChangeHair(mode, targetId);
            return Task.FromResult(Result.Success());
        }

        return ChangeHairScheduledAsync(mode, targetId, cancellationToken);
    }

    private async Task<Result> ChangeHairScheduledAsync(byte mode, byte targetId, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.ChangeHair(mode, targetId), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("appearance.send.failure", "The appearance command could not be sent."); }
    }
}
}
