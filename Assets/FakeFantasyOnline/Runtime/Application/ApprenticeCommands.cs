using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IApprenticeCommands
    {
        Task<Result> RequestAsync(int targetId, byte role, string name, CancellationToken ct);
        Task<Result> ListAsync(CancellationToken ct);
    }
}
