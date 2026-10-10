using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IAttributeWashCommands
{
    public Task<Result> WashAffixAsync(byte kind, byte tab, int slot, int index, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.WashAffix(kind, tab, slot, index);
            return Task.FromResult(Result.Success());
        }

        return WashAffixScheduledAsync(kind, tab, slot, index, cancellationToken);
    }

    private async Task<Result> WashAffixScheduledAsync(byte kind, byte tab, int slot, int index, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.WashAffix(kind, tab, slot, index), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("attribute.wash.send.failure", "The attribute wash request could not be sent."); }
    }
}
}
