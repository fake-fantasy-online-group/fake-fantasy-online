using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IWarehouseCommands, IWarehouseSortCommands
{
    public void WarehouseMoney(int mode, long amount) => _netClient.WarehouseMoney(mode, amount);

    public Task<Result> BuyWarehouseAsync(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.BuyWarehouse();
            return Task.FromResult(Result.Success());
        }

        return BuyWarehouseScheduledAsync(cancellationToken);
    }

    private async Task<Result> BuyWarehouseScheduledAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.BuyWarehouse(), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("warehouse.buy.send.failure", "The warehouse expansion request could not be sent."); }
    }

    public Task<Result> WarehouseMoveAsync(WarehouseMoveRequest request, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            if (request == null)
                return Task.FromResult(Result.Failure("warehouse.move.invalid", "The warehouse move request is missing."));
            _netClient.WarehouseMove(request.Direction, request.FromTab,
                request.FromSlot, request.WarehouseTab, request.Count, request.ToSlot);
            return Task.FromResult(Result.Success());
        }

        return WarehouseMoveScheduledAsync(request, cancellationToken);
    }

    private async Task<Result> WarehouseMoveScheduledAsync(WarehouseMoveRequest request, CancellationToken cancellationToken)
    {
        if (request == null)
            return Result.Failure("warehouse.move.invalid", "The warehouse move request is missing.");
        try
        {
            await SendAsync(() => _netClient.WarehouseMove(request.Direction, request.FromTab,
                request.FromSlot, request.WarehouseTab, request.Count, request.ToSlot), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("warehouse.move.send.failure", "The warehouse move request could not be sent."); }
    }

    public Task<Result> SortWarehouseAsync(int tab, CancellationToken ct)
    {
        if (!ct.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SortWarehouse(tab);
            return Task.FromResult(Result.Success());
        }

        return SendWarehouse(() => _netClient.SortWarehouse(tab), ct);
    }
    public Task<Result> WarehouseMoneyAsync(int mode, long amount, CancellationToken ct) => SendWarehouse(() => _netClient.WarehouseMoney(mode, amount), ct);
    public Task<Result> WarehouseSplitAsync(int tab, int slot, int count, CancellationToken ct)
    {
        if (!ct.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.WarehouseSplit(tab, slot, count);
            return Task.FromResult(Result.Success());
        }

        return SendWarehouse(() => _netClient.WarehouseSplit(tab, slot, count), ct);
    }
    public Task<Result> WarehouseRearrangeAsync(int fromTab, int fromSlot, int toTab, int toSlot, CancellationToken ct)
    {
        if (!ct.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.WarehouseRearrange(fromTab, fromSlot, toTab, toSlot);
            return Task.FromResult(Result.Success());
        }

        return SendWarehouse(() => _netClient.WarehouseRearrange(fromTab, fromSlot, toTab, toSlot), ct);
    }

    private async Task<Result> SendWarehouse(Action send, CancellationToken ct)
    {
        try { await SendAsync(send, ct).ConfigureAwait(false); return Result.Success(); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception)) { return Result.Failure("warehouse.send.failure", "The warehouse request could not be sent."); }
    }
}
}
