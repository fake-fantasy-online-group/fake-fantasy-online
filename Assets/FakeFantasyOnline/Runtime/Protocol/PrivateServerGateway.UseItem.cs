using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IItemUseCommands
{
    public Task<Result> UseItemAsync(int itemId, byte tab, int slot, CancellationToken cancellationToken)
    {
        // Native item callers publish their cooldown only after the sender returns normally.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendOrdinaryItemRequestForIndependentRuntime(itemId, tab, slot);
            return Task.FromResult(Result.Success());
        }

        return UseItemScheduledAsync(itemId, tab, slot, cancellationToken);
    }

    private async Task<Result> UseItemScheduledAsync(int itemId, byte tab, int slot,
        CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.SendOrdinaryItemRequestForIndependentRuntime(itemId, tab, slot), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("item.use.send.failure", "The item use request could not be sent."); }
    }
}
}
