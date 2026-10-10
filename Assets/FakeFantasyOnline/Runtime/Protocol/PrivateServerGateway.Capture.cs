using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : ICaptureCommands
{
    public Task<Result> CaptureAsync(int targetId, int itemId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendTargetedItemRequestForIndependentRuntime(targetId, itemId);
            return Task.FromResult(Result.Success());
        }

        return CaptureScheduledAsync(targetId, itemId, cancellationToken);
    }

    private async Task<Result> CaptureScheduledAsync(int targetId, int itemId, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.SendTargetedItemRequestForIndependentRuntime(targetId, itemId), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("capture.send.failure", "The capture request could not be sent."); }
    }
}
}
