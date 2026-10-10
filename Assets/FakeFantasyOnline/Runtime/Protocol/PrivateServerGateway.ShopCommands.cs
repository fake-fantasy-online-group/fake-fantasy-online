using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IShopCommands
{
    public Task<Result> OpenNativeShopAsync(int shopId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.OpenShop(shopId);
            return Task.FromResult(Result.Success());
        }

        return SendNativeShopCommandAsync(() => _netClient.OpenShop(shopId), cancellationToken);
    }
    public Task<Result> BuyItemAsync(int itemId, int count, CancellationToken cancellationToken)
    {
        // The shop cart advances and clears only after each native sender returns normally.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.BuyItem(itemId, count);
            return Task.FromResult(Result.Success());
        }

        return SendNativeShopCommandAsync(() => _netClient.BuyItem(itemId, count), cancellationToken);
    }

    public Task<Result> SellItemAsync(byte tab, int slot, int count, int itemId, bool confirmed, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SellItem(tab, slot, count, itemId, confirmed);
            return Task.FromResult(Result.Success());
        }

        return SendNativeShopCommandAsync(() => _netClient.SellItem(tab, slot, count, itemId, confirmed), cancellationToken);
    }
    public Task<Result> StallDealAsync(int ownerId, int index, int count, byte bagTab, int bagSlot,
        int expectItemId, long expectPrice, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.StallDeal(ownerId, index, count, bagTab, bagSlot, expectItemId, expectPrice);
            return Task.FromResult(Result.Success());
        }

        return SendNativeShopCommandAsync(() => _netClient.StallDeal(ownerId, index, count, bagTab, bagSlot, expectItemId, expectPrice), cancellationToken);
    }

    private async Task<Result> SendNativeShopCommandAsync(Action send, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("shop.command.send.failure", "The shop command could not be sent."); }
    }
}
}
