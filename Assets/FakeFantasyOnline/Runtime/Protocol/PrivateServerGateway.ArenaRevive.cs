using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IArenaReviveCommands
{
    // Official NetClient.ArenaRevive (0x06000746): capture the surrender flag, then Send(0x1084)
    // with writer 0x06000837, which writes exactly one Buf.U8. 0 is the revive click, 1 is the
    // give-up click; the window's countdown reaching zero is not a client action and the server
    // arbitrates both, so no product state (free revives left, balance, "kind") gates the send -
    // the native host call relies on the connected test inside NetClient.Send. Scheduled
    // compatibility calls retain their original stage and cancellation handling.
    public Task<Result> ArenaReviveAsync(bool surrender, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ArenaRevive(surrender);
            return Task.FromResult(Result.Success());
        }

        return SendArenaReviveAsync(
            () => _netClient.ArenaRevive(surrender),
            cancellationToken);
    }

    private async Task<Result> SendArenaReviveAsync(Action send, CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception e) when (IsWireFailure(e))
        {
            return Result.Failure(
                "arena.revive.send.failure",
                "The arena revive request could not be sent.");
        }
    }
}
}
