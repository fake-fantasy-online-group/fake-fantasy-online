using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IDuelCommands
    {
        Task<Result> ChallengeAsync(int targetId, string targetName, byte mode, CancellationToken ct);
        Task<Result> RequestListAsync(CancellationToken ct);
        Task<Result> BetAsync(int duelId, byte side, int amount, CancellationToken ct);
        Task<Result> SpectateAsync(int duelId, CancellationToken ct);
    }
}
