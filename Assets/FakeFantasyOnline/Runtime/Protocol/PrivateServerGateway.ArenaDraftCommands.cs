using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IArenaDraftCommands
{
    // Official NetClient.ArenaPick (0x06000744): capture the clicked card index in the display class
    // NetClient+<>c__DisplayClass218_0 (+0x10), then Send(0x1083) with the writer 0x06000835, which
    // writes exactly one Buf.U8. The index is the card slot, not a card identity: the window's own
    // enabled test (_cards != null && i < _cards.Count in Pick, 0x06000052) is the only client-side
    // gate, so nothing here re-validates the range or inspects the card list.
    public Task<Result> ArenaPickAsync(int index, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ArenaPick(index);
            return Task.FromResult(Result.Success());
        }

        return SendArenaDraftAsync(
            () => _netClient.ArenaPick(index),
            "arena.pick.send.failure",
            "The arena talent pick request could not be sent.",
            cancellationToken);
    }

    // Official NetClient.ArenaReroll (0x06000745): Send(0x1086) with the cached writer 0x060007a9,
    // whose emitted extent is a bare return - no payload at all. The window's own gate is
    // _rerollCost >= 0 (Reroll, 0x06000051); the gateway adds no balance or free-reroll check.
    public Task<Result> ArenaRerollAsync(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ArenaReroll();
            return Task.FromResult(Result.Success());
        }

        return SendArenaDraftAsync(
            () => _netClient.ArenaReroll(),
            "arena.reroll.send.failure",
            "The arena talent reroll request could not be sent.",
            cancellationToken);
    }

    private async Task<Result> SendArenaDraftAsync(
        Action send,
        string failureCode,
        string failureMessage,
        CancellationToken cancellationToken)
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
            return Result.Failure(failureCode, failureMessage);
        }
    }
}
}
