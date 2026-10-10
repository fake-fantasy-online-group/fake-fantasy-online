using System;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class PrivateServerGateway : IAscendSkillCommands
{
    public Task<Result> AscendSkillAsync(int skillId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.AscendSkill(skillId);
            return Task.FromResult(Result.Success());
        }

        return AscendSkillScheduledAsync(skillId, cancellationToken);
    }

    private async Task<Result> AscendSkillScheduledAsync(int skillId, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.AscendSkill(skillId), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("skill.ascend.send.failure", "The skill cultivation request could not be sent."); }
    }
}
}
