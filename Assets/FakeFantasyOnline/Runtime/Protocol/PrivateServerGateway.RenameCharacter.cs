using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IRenameCharacterCommands
{
    public Task<Result> RenameCharacterAsync(string name, CancellationToken cancellationToken)
    {
        // The original sender has no world-stage guard. A real result-code-ten receipt can
        // open the persistent dialog before a map host exists or between map borrowers.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendRenameChar(name);
            return Task.FromResult(Result.Success());
        }

        return RenameCharacterScheduledAsync(name, cancellationToken);
    }

    private async Task<Result> RenameCharacterScheduledAsync(string name, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.SendRenameChar(name), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("rename.send.failure", "The rename request could not be sent.");
        }
    }
}
}
