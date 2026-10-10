using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IFriendCommands
    {
        public Task<Result> AddAsync(string n, CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FriendReq(n);
                return Task.FromResult(Result.Success());
            }
            return SendFriend(() => _netClient.FriendReq(n), c);
        }
        public Task<Result> DeleteAsync(string n, CancellationToken c)
        {
            // The existing PlayerActionMenu/TeamDlg confirmations run on the host with None.
            // Their native writer and any exception precede the rest of the confirmation.
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FriendDel(n);
                return Task.FromResult(Result.Success());
            }
            return SendFriend(() => _netClient.FriendDel(n), c);
        }
        private async Task<Result> SendFriend(Action send, CancellationToken c)
        {
            try { await SendAsync(send, c).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (c.IsCancellationRequested) { throw; }
            catch (Exception e) when (IsWireFailure(e)) { return Result.Failure("friend.send.failure", "The friend request could not be sent."); }
        }
    }
}
