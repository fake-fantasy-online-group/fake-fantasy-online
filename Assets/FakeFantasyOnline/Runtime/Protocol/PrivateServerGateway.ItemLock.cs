using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IItemLockCommands
{
    public Task<Result> SetItemLockAsync(ItemLockRequest request, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            if (request == null)
                return Task.FromResult(Result.Failure("item.lock.invalid", "The item lock request is missing."));
            _netClient.SetItemLock(request.Tab, request.Slot, request.On,
                request.SecurityCode, request.ItemId);
            return Task.FromResult(Result.Success());
        }

        return SetItemLockScheduledAsync(request, cancellationToken);
    }

    private async Task<Result> SetItemLockScheduledAsync(ItemLockRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return Result.Failure("item.lock.invalid", "The item lock request is missing.");
        try
        {
            await SendAsync(() => _netClient.SetItemLock(request.Tab, request.Slot, request.On,
                request.SecurityCode, request.ItemId), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("item.lock.send.failure", "The item lock request could not be sent."); }
    }
}
}
