using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IStallCommands
{
    public Task<Result> StallCreateAsync(byte type, string name, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.StallCreate(type, name);
            return Task.FromResult(Result.Success());
        }

        return SendStallCommandAsync(() => _netClient.StallCreate(type, name), cancellationToken);
    }
    public Task<Result> StallAddAsync(byte tab, int slot, int itemId, int count, long price, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.StallAdd(tab, slot, itemId, count, price);
            return Task.FromResult(Result.Success());
        }

        return SendStallCommandAsync(() => _netClient.StallAdd(tab, slot, itemId, count, price), cancellationToken);
    }
    public Task<Result> StallBrowseAsync(int ownerId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.StallBrowse(ownerId);
            return Task.FromResult(Result.Success());
        }

        return SendStallCommandAsync(() => _netClient.StallBrowse(ownerId), cancellationToken);
    }
    public Task<Result> StallEndAsync(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.StallEnd();
            return Task.FromResult(Result.Success());
        }

        return SendStallCommandAsync(() => _netClient.StallEnd(), cancellationToken);
    }
    public Task<Result> StallDeleteAsync(int index, CancellationToken cancellationToken) =>
        SendStallCommandAsync(() => _netClient.StallDelete(index), cancellationToken);
    private async Task<Result> SendStallCommandAsync(Action send, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("stall.send.failure", "The stall request could not be sent."); }
    }
}
}
