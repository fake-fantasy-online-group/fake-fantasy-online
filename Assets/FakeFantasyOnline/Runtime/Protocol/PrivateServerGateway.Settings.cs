using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway
{
    public Task<Result> SetCharacterPkModeAsync(int mode, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SetPkMode(mode);
            return Task.FromResult(Result.Success());
        }

        return SendSettingsCommandAsync(() => _netClient.SetPkMode(mode), cancellationToken);
    }

    public Task<Result> SetCharacterGlowModeAsync(int mode, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SetGlowMode(mode);
            return Task.FromResult(Result.Success());
        }

        return SendSettingsCommandAsync(() => _netClient.SetGlowMode(mode), cancellationToken);
    }

    public Task<Result> SetCharacterSmartCastAsync(bool enabled, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SetSmartCast(enabled);
            return Task.FromResult(Result.Success());
        }

        return SendSettingsCommandAsync(() => _netClient.SetSmartCast(enabled), cancellationToken);
    }

    public Task<Result> SetPetPickQualityAsync(byte quality, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetPickQuality(quality);
            return Task.FromResult(Result.Success());
        }

        return SendSettingsCommandAsync(() => _netClient.PetPickQuality(quality), cancellationToken);
    }

    private async Task<Result> SendSettingsCommandAsync(Action writer, CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
            return Result.Failure("session.stage.invalid", "The session is not in the world.");
        try
        {
            await SendAsync(writer, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("player.settings.send.failure", "The settings request could not be sent.");
        }
    }
}
}
