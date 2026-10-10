using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IHornCommands
{
    public Task<Result> SendHornAsync(string text, int itemId, byte skin,
        List<(byte, byte, int)> shares, CancellationToken cancellationToken)
    {
        // The 0x8074 receipt can open the original dialog outside an admitted world.
        // SendHorn has no world-stage gate; its existing Send owns connection availability.
        // HornDialog closes only after the native sender returns normally.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendHorn(text, itemId, skin, shares);
            return Task.FromResult(Result.Success());
        }

        return SendHornScheduledAsync(text, itemId, skin, shares, cancellationToken);
    }

    private async Task<Result> SendHornScheduledAsync(string text, int itemId, byte skin,
        List<(byte, byte, int)> shares, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.SendHorn(text, itemId, skin, shares),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("horn.send.failure", "The horn request could not be sent.");
        }
    }
}
}
