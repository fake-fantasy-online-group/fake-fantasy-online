using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IRidingCommands
    {
        public Task<Result> RideTogetherAsync(int id, CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.RideTogether(id);
                return Task.FromResult(Result.Success());
            }
            return SendRide(() => _netClient.RideTogether(id), c);
        }
        public Task<Result> InviteAsync(int id, CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.RideInvite(id);
                return Task.FromResult(Result.Success());
            }
            return SendRide(() => _netClient.RideInvite(id), c);
        }
        private async Task<Result> SendRide(Action send, CancellationToken c)
        {
            try { await SendAsync(send, c).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (c.IsCancellationRequested) { throw; }
            catch (Exception e) when (IsWireFailure(e)) { return Result.Failure("riding.send.failure", "The riding request could not be sent."); }
        }
    }
}
