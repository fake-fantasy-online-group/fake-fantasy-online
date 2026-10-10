using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IAvatarFusionCommands
{
    public Task<Result> ApplyAvatarAsync(byte bagTab, int bagSlot, int targetSlot, byte kind, byte targetKind,
        byte targetTab, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ApplyAvatar(bagTab, bagSlot, targetSlot, kind, targetKind, targetTab);
            return Task.FromResult(Result.Success());
        }

        return ApplyAvatarScheduledAsync(bagTab, bagSlot, targetSlot, kind, targetKind, targetTab, cancellationToken);
    }

    private async Task<Result> ApplyAvatarScheduledAsync(byte bagTab, int bagSlot, int targetSlot, byte kind, byte targetKind,
        byte targetTab, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.ApplyAvatar(bagTab, bagSlot, targetSlot, kind, targetKind, targetTab),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("avatar.apply.send.failure", "The avatar fusion request could not be sent."); }
    }
}
}
