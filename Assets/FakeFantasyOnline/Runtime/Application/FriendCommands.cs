using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IFriendCommands
    {
        Task<Result> AddAsync(string name, CancellationToken ct);
        Task<Result> DeleteAsync(string name, CancellationToken ct);
    }
}
