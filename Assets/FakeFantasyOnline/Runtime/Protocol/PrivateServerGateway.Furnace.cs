using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IFurnaceCommands
{
    public Task<Result> OpenFurnaceAsync(byte modType, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.OpenFurnace(modType);
            return Task.FromResult(Result.Success());
        }

        return SendFurnaceCommandAsync(() => _netClient.OpenFurnace(modType), cancellationToken);
    }

    public Task<Result> CraftAsync(byte modType, int resultItem, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Craft(modType, resultItem);
            return Task.FromResult(Result.Success());
        }

        return SendFurnaceCommandAsync(() => _netClient.Craft(modType, resultItem), cancellationToken);
    }

    public Task<Result> CraftUpgradeAsync(byte mode, byte equipSlot, byte execute, byte tab,
        int bagSlot, byte protect, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.CraftUpgrade(mode, equipSlot, execute, tab, bagSlot, protect);
            return Task.FromResult(Result.Success());
        }

        return SendFurnaceCommandAsync(() => _netClient.CraftUpgrade(mode, equipSlot, execute, tab, bagSlot, protect),
            cancellationToken);
    }

    public Task<Result> DrillAsync(byte targetKind, byte tab, int slot, byte execute, byte protect,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Drill(targetKind, tab, slot, execute, protect);
            return Task.FromResult(Result.Success());
        }

        return SendFurnaceCommandAsync(() => _netClient.Drill(targetKind, tab, slot, execute, protect), cancellationToken);
    }

    public Task<Result> InlayAsync(byte targetKind, byte tab, int slot, byte holeIdx, byte runeTab,
        int runeBagSlot, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Inlay(targetKind, tab, slot, holeIdx, runeTab, runeBagSlot);
            return Task.FromResult(Result.Success());
        }

        return SendFurnaceCommandAsync(() => _netClient.Inlay(targetKind, tab, slot, holeIdx, runeTab, runeBagSlot),
            cancellationToken);
    }

    private async Task<Result> SendFurnaceCommandAsync(Action send, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("furnace.send.failure", "The furnace command could not be sent.");
        }
    }
}
}
