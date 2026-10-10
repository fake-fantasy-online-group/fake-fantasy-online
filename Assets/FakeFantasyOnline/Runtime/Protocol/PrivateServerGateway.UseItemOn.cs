using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IItemToolCommands
{
    public Task<Result> UseItemOnAsync(int toolId, byte targetKind, byte tab, int slot,
        CancellationToken cancellationToken)
    {
        // SysMsgBox consumes the confirmation event only after its native callback returns.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.UseItemOn(toolId, targetKind, tab, slot);
            return Task.FromResult(Result.Success());
        }

        return UseItemOnScheduledAsync(toolId, targetKind, tab, slot, cancellationToken);
    }

    private async Task<Result> UseItemOnScheduledAsync(int toolId, byte targetKind, byte tab, int slot,
        CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.UseItemOn(toolId, targetKind, tab, slot),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("item.tool.send.failure", "The item tool request could not be sent."); }
    }
}
}
