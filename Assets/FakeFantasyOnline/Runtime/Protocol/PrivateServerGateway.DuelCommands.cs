using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IDuelCommands
    {
        // Native DuelDlg callbacks send inline before their close/GUI continuation.
        public Task<Result> ChallengeAsync(int id, string name, byte mode, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.DuelChallenge(id, name, mode);
                return Task.FromResult(Result.Success());
            }

            return SendDuel(() => _netClient.DuelChallenge(id, name, mode), ct);
        }
        public Task<Result> RequestListAsync(CancellationToken ct) => SendDuel(() => _netClient.RequestDuelList(), ct);
        public Task<Result> BetAsync(int id, byte side, int amount, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.DuelBet(id, side, amount);
                return Task.FromResult(Result.Success());
            }

            return SendDuel(() => _netClient.DuelBet(id, side, amount), ct);
        }
        public Task<Result> SpectateAsync(int id, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.DuelSpectate(id);
                return Task.FromResult(Result.Success());
            }

            return SendDuel(() => _netClient.DuelSpectate(id), ct);
        }
        private async Task<Result> SendDuel(Action send, CancellationToken ct)
        {
            try { await SendAsync(send, ct).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e) when (IsWireFailure(e)) { return Result.Failure("duel.send.failure", "The duel request could not be sent."); }
        }
    }
}
