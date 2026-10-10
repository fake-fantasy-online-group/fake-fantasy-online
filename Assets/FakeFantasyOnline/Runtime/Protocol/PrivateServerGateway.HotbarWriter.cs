using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    public Task<Result> WriteHotbarAsync(int[] ids, int[] kinds, bool expanded, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            if (ids == null || kinds == null || ids.Length < 20 || kinds.Length < 20)
                return Task.FromResult(Result.Failure("hotbar.invalid", "The hotbar requires at least 20 slots."));
            _netClient.SetHotbar(ids, kinds, expanded);
            return Task.FromResult(Result.Success());
        }

        return WriteHotbarScheduledAsync(ids, kinds, expanded, cancellationToken);
    }

    private async Task<Result> WriteHotbarScheduledAsync(int[] ids, int[] kinds, bool expanded, CancellationToken cancellationToken)
    {
        if (ids == null || kinds == null || ids.Length < 20 || kinds.Length < 20)
            return Result.Failure("hotbar.invalid", "The hotbar requires at least 20 slots.");
        // Encode before the first await: later UI mutations must not change this request.
        // The shared hotbar writer takes the official NetClient.Buf, so this encoder builds the
        // same bytes with the official writer surface.
        var writer = new NetClient.Buf();
        NetClient.WriteHotbarForIndependentRuntime(writer, ids, kinds, expanded);
        byte[] payload = writer.ToArray();
        try
        {
            await SendAsync(ProtocolOpcodes.SaveHotbarRequest, payload, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException)
        {
            return Result.Failure("hotbar.save.timeout", "The hotbar save request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("hotbar.save.failure", "The hotbar save request could not be sent.");
        }
    }
}
}
