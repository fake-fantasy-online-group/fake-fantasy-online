using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IBlockCommands
    {
        public Task<Result> AddAsync(string name, byte scope, CancellationToken ct)
        {
            // PlayerActionMenu passes None from its host-thread confirmation callback.
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.SendBlockAdd(name, scope);
                return Task.FromResult(Result.Success());
            }
            return SendBlock(() => _netClient.SendBlockAdd(name, scope), ct);
        }
        public Task<Result> DeleteAsync(long characterId, CancellationToken ct)
        {
            // PlayerActionMenu and TeamDlg share this native immediate confirmation send.
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.SendBlockDel(characterId);
                return Task.FromResult(Result.Success());
            }
            return SendBlock(() => _netClient.SendBlockDel(characterId), ct);
        }
        private async Task<Result> SendBlock(Action send, CancellationToken ct)
        {
            try { await SendAsync(send, ct).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e) when (IsWireFailure(e)) { return Result.Failure("block.send.failure", "The block request could not be sent."); }
        }
    }
}
