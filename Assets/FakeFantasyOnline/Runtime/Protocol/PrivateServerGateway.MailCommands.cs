using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
    public sealed partial class PrivateServerGateway : IMailCommands
    {
        public Task<Result> OpenMailAsync(CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.OpenMail();
                return Task.FromResult(Result.Success());
            }
            return SendMail(() => _netClient.OpenMail(), ct);
        }
        public Task<Result> ActionAsync(byte action, long id, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.MailAction(action, id);
                return Task.FromResult(Result.Success());
            }
            return SendMail(() => _netClient.MailAction(action, id), ct);
        }
        public Task<Result> SendAsync(string to, string title, string body, long money, long caiyu, byte type,
            List<(byte, int, int)> attachments, CancellationToken ct)
        {
            if (!ct.CanBeCanceled && _host.IsHostThread)
            {
                _netClient.SendMail(to, title, body, money, caiyu, type, attachments);
                return Task.FromResult(Result.Success());
            }
            return SendMail(() => _netClient.SendMail(to, title, body, money, caiyu, type, attachments), ct);
        }
        private async Task<Result> SendMail(Action send, CancellationToken ct)
        {
            try { await SendAsync(send, ct).ConfigureAwait(false); return Result.Success(); }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception e) when (IsWireFailure(e)) { return Result.Failure("mail.send.failure", "The mail request could not be sent."); }
        }
    }
}
