using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IEquipmentFusionCommands
{
    public Task<Result> EquipFuseAsync(byte op, byte mainKind, byte mainTab, int mainSlot,
        byte subTab, int subSlot, int sig, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.EquipFuse(op, mainKind, mainTab, mainSlot, subTab, subSlot, sig);
            return Task.FromResult(Result.Success());
        }

        return EquipFuseScheduledAsync(op, mainKind, mainTab, mainSlot, subTab, subSlot, sig, cancellationToken);
    }

    private async Task<Result> EquipFuseScheduledAsync(byte op, byte mainKind, byte mainTab, int mainSlot,
        byte subTab, int subSlot, int sig, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.EquipFuse(op, mainKind, mainTab, mainSlot, subTab, subSlot, sig),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("equipment.fuse.send.failure", "The equipment fusion request could not be sent."); }
    }
}
}
