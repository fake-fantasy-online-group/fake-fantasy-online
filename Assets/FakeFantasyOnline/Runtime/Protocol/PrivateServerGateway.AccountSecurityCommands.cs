using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IAccountSecurityCommands
{
    public Task<Result> ChangePasswordAsync(string oldPassword, string newPassword, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ChangePwd(oldPassword, newPassword);
            return Task.FromResult(Result.Success());
        }

        return SendAccountSecurityCommandAsync(() => _netClient.ChangePwd(oldPassword, newPassword), cancellationToken);
    }
    public Task<Result> ChangeSecurityCodeAsync(string password, string oldCode, string newCode, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.ChangeSecCode(password, oldCode, newCode);
            return Task.FromResult(Result.Success());
        }

        return SendAccountSecurityCommandAsync(() => _netClient.ChangeSecCode(password, oldCode, newCode), cancellationToken);
    }
    private async Task<Result> SendAccountSecurityCommandAsync(Action send, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("account.security.send.failure", "The account security request could not be sent."); }
    }
}
}
