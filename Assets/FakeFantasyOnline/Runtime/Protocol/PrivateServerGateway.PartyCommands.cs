using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IPartyCommands
    {
        public async Task<Result> CreateAndInviteAsync(string name, System.Collections.Generic.IReadOnlyList<PartyInviteTarget> targets, CancellationToken ct)
        {
            Result created = await SendParty(() => _netClient.PartyCreate(name), ct).ConfigureAwait(false);
            if (!created.IsSuccess || targets == null) return created;
            for (int i = 0; i < targets.Count; i++)
            {
                PartyInviteTarget target = targets[i];
                if (target == null) continue;
                Result invited = await SendParty(() => _netClient.PartyInvite(target.EntityId, target.Name), ct).ConfigureAwait(false);
                if (!invited.IsSuccess) return invited;
            }
            return Result.Success();
        }
        public Task<Result> InviteAsync(int id, string name, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.PartyInvite(id, name);
                return Task.FromResult(Result.Success());
            }
            return SendParty(() => _netClient.PartyInvite(id, name), ct);
        }
        public Task<Result> JoinAsync(int id, string name, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.PartyJoinReq(id, name);
                return Task.FromResult(Result.Success());
            }
            return SendParty(() => _netClient.PartyJoinReq(id, name), ct);
        }
        public Task<Result> KickAsync(int id, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.PartyKick(id);
                return Task.FromResult(Result.Success());
            }
            return SendParty(() => _netClient.PartyKick(id), ct);
        }
        public Task<Result> SetLeaderAsync(int id, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.PartyLeader(id);
                return Task.FromResult(Result.Success());
            }
            return SendParty(() => _netClient.PartyLeader(id), ct);
        }
        public Task<Result> CreateAsync(string name, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.PartyCreate(name);
                return Task.FromResult(Result.Success());
            }
            return SendParty(() => _netClient.PartyCreate(name), ct);
        }
        public Task<Result> LeaveAsync(CancellationToken ct)
        {
            // TeamDlg's original confirmation invokes the writer on this call stack.
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.PartyLeave();
                return Task.FromResult(Result.Success());
            }
            return SendParty(() => _netClient.PartyLeave(), ct);
        }
        public Task<Result> DismissAsync(CancellationToken ct)
        {
            // TeamDlg's original confirmation invokes the writer on this call stack.
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.PartyDismiss();
                return Task.FromResult(Result.Success());
            }
            return SendParty(() => _netClient.PartyDismiss(), ct);
        }

        private async Task<Result> SendParty(Action send, CancellationToken ct)
        {
            try { await SendAsync(send, ct).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (IsWireFailure(ex)) { return Result.Failure("party.send.failure", "The party request could not be sent."); }
        }
    }
}
