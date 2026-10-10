using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IInventoryPlacementCommands
{
    public Task<Result> ItemMoveAsync(int tab, int from, int to, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ItemMove(tab, from, to);
            return Task.FromResult(Result.Success());
        }

        return ItemMoveScheduledAsync(tab, from, to, cancellationToken);
    }

    private async Task<Result> ItemMoveScheduledAsync(int tab, int from, int to, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.ItemMove(tab, from, to), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("inventory.move.send.failure", "The inventory move request could not be sent."); }
    }

    public Task<Result> DropItemAsync(int tab, int slot, int x, int y, int itemId, bool confirmed,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.DropItem(tab, slot, x, y, itemId, confirmed);
            return Task.FromResult(Result.Success());
        }

        return DropItemScheduledAsync(tab, slot, x, y, itemId, confirmed, cancellationToken);
    }

    private async Task<Result> DropItemScheduledAsync(int tab, int slot, int x, int y, int itemId, bool confirmed,
        CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.DropItem(tab, slot, x, y, itemId, confirmed), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("inventory.drop.send.failure", "The inventory drop request could not be sent."); }
    }
}
}
