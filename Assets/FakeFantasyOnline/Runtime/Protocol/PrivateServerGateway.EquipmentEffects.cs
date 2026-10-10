using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    public Task<Result> SetEquipmentEffectsMaskAsync(
        Func<int> readHideMask,
        CancellationToken cancellationToken)
    {
        // Retain the existing host stage policy. It is not evidence about the original
        // outer SendEquipFx admission; the recovered callback begins inside NetClient.Send.
        if (_stage != GatewayStage.InWorld)
        {
            return Task.FromResult(Result.Failure(
                "session.stage.invalid",
                "The session is not in the world."));
        }

        // The ordinary native caller supplies None on the host. Run its send on that stack,
        // so getter/Buf failures reach the caller before its observer or later UI work.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendEquipFx(readHideMask);
            return Task.FromResult(Result.Success());
        }

        return SetEquipmentEffectsMaskScheduledAsync(readHideMask, cancellationToken);
    }

    // Product-only cancellable/off-host adaptation. Its scheduling and failure conversion
    // are qualified separately from the no-catch native payload callback.
    private async Task<Result> SetEquipmentEffectsMaskScheduledAsync(
        Func<int> readHideMask,
        CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.SendEquipFx(readHideMask), cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "equipment.effects.timeout",
                "The equipment-effect preference timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "equipment.effects.failure",
                "The equipment-effect preference could not be sent.");
        }
    }
}
}
