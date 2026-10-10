using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IApprenticeCommands
    {
        public Task<Result> RequestAsync(int id, byte role, string name, CancellationToken ct)
        {
            // All three original menu roles use the same immediate native writer on the host.
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.ApprenticeReq(id, role, name);
                return Task.FromResult(Result.Success());
            }
            return SendApprentice(() => _netClient.ApprenticeReq(id, role, name), ct);
        }
        public Task<Result> ListAsync(CancellationToken ct)
        {
            // TeamDlg.Toggle sends synchronously after changing visibility and tab.
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.ApprenticeList();
                return Task.FromResult(Result.Success());
            }
            return SendApprentice(() => _netClient.ApprenticeList(), ct);
        }
        private async Task<Result> SendApprentice(Action send, CancellationToken ct)
        {
            try { await SendAsync(send, ct).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e) when (IsWireFailure(e)) { return Result.Failure("apprentice.send.failure", "The apprentice request could not be sent."); }
        }
    }
}
