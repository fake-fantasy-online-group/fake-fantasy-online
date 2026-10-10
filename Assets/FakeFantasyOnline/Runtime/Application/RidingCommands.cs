using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IRidingCommands
    {
        Task<Result> RideTogetherAsync(int targetId, CancellationToken ct);
        Task<Result> InviteAsync(int targetId, CancellationToken ct);
    }
}
