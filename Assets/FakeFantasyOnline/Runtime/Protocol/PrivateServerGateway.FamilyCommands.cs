using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IFamilyCommands
    {
        Task<Result> IFamilyCommands.InviteAsync(int id,string n,CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyInvite(id,n);
                return Task.FromResult(Result.Success());
            }
            return SendFamily(()=>_netClient.FamilyInvite(id,n),c);
        }
        Task<Result> IFamilyCommands.LeaveAsync(CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyLeave();
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyLeave(),c);
        }
        Task<Result> IFamilyCommands.CreateAsync(string name,string proclaim,CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyCreate(name,proclaim);
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyCreate(name,proclaim),c);
        }
        Task<Result> IFamilyCommands.ListAsync(CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyList();
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyList(),c);
        }
        Task<Result> IFamilyCommands.ApplyAsync(string n,CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyApply(n);
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyApply(n),c);
        }
        Task<Result> IFamilyCommands.ManageAsync(byte o,string n,int id,string t,CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyManage(o,n,id,t);
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyManage(o,n,id,t),c);
        }
        Task<Result> IFamilyCommands.MailAsync(string t,string b,CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyMail(t,b);
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyMail(t,b),c);
        }
        Task<Result> IFamilyCommands.PositionsAsync(CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyPositions();
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyPositions(),c);
        }
        Task<Result> IFamilyCommands.RefreshAsync(CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyRefresh();
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyRefresh(),c);
        }
        Task<Result> IFamilyCommands.PositionNameAsync(int id,string n,CancellationToken c)
        {
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyPosName(id,n);
                return Task.FromResult(Result.Success());
            }

            return SendFamily(()=>_netClient.FamilyPosName(id,n),c);
        }
        Task<Result> IFamilyCommands.StashAsync(int op, int bagTab, int bagSlot, int index, CancellationToken c)
        {
            // The live family UI continues only after the native sender returns normally.
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyStash(op, bagTab, bagSlot, index);
                return Task.FromResult(Result.Success());
            }

            return SendFamily(() => _netClient.FamilyStash(op, bagTab, bagSlot, index), c);
        }

        Task<Result> IFamilyCommands.StashPermissionAsync(int pos, CancellationToken c)
        {
            // DrawPosBtn tail-calls this sender before its caller resumes the GUI body.
            if (!c.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.FamilyStashPerm(pos);
                return Task.FromResult(Result.Success());
            }

            return SendFamily(() => _netClient.FamilyStashPerm(pos), c);
        }
        private async Task<Result> SendFamily(Action send,CancellationToken c){try{await SendAsync(send,c).ConfigureAwait(false);return Result.Success();}catch(OperationCanceledException) when(c.IsCancellationRequested){throw;}catch(Exception e) when(IsWireFailure(e)){return Result.Failure("family.send.failure","The family request could not be sent.");}}
    }
}
