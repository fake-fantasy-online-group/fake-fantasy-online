using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
    public interface IBlockCommands
    {
        Task<Result> AddAsync(string name, byte scope, CancellationToken ct);
        Task<Result> DeleteAsync(long characterId, CancellationToken ct);
    }
}
