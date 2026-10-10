using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IRackCommands
    {
        public Task<Result> OpenRackAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.OpenRack();
                return Task.FromResult(Result.Success());
            }

            return SendRack(() => _netClient.OpenRack(), ct);
        }
        public Task<Result> BuyAsync(int itemId, byte source, int count, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.BuyRack(itemId, source, count);
                return Task.FromResult(Result.Success());
            }

            return SendRack(() => _netClient.BuyRack(itemId, source, count), ct);
        }
        public Task<Result> RequestDepositAsync(string chain, int amount, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.RequestDeposit(chain, amount);
                return Task.FromResult(Result.Success());
            }

            return SendRack(() => _netClient.RequestDeposit(chain, amount), ct);
        }
        public Task<Result> RequestRefundListAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.RackRefundList();
                return Task.FromResult(Result.Success());
            }

            return SendRack(() => _netClient.RackRefundList(), ct);
        }
        public Task<Result> RefundAsync(int itemId, int unitPrice, int bundle, int day, int shares, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.RackRefund(itemId, unitPrice, bundle, day, shares);
                return Task.FromResult(Result.Success());
            }

            return SendRack(() => _netClient.RackRefund(itemId, unitPrice, bundle, day, shares), ct);
        }
        public Task<Result> HelpAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.HelpReq();
                return Task.FromResult(Result.Success());
            }

            return SendRack(() => _netClient.HelpReq(), ct);
        }
        private async Task<Result> SendRack(Action send, CancellationToken ct)
        {
            try { await SendAsync(send, ct).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e) when (IsWireFailure(e)) { return Result.Failure("rack.send.failure", "The rack request could not be sent."); }
        }
    }
}
