using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IRepairCommands
{
    public Task<Result> RepairAsync(byte mode, int slot, byte targetKind, byte tab, CancellationToken cancellationToken)
    {
        // Native target selection cancels only after the repair query returns normally.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Repair(mode, slot, targetKind, tab);
            return Task.FromResult(Result.Success());
        }

        return SendRepairCommandAsync(() => _netClient.Repair(mode, slot, targetKind, tab), cancellationToken);
    }

    public Task<Result> RepairConfirmAsync(byte mode, int slot, byte targetKind, byte tab, CancellationToken cancellationToken)
    {
        // The confirmation dialog remains visible if the native sender throws.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.RepairConfirm(mode, slot, targetKind, tab);
            return Task.FromResult(Result.Success());
        }

        return SendRepairCommandAsync(() => _netClient.RepairConfirm(mode, slot, targetKind, tab), cancellationToken);
    }
    private async Task<Result> SendRepairCommandAsync(Action send, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("repair.send.failure", "The repair command could not be sent."); }
    }
}
}
