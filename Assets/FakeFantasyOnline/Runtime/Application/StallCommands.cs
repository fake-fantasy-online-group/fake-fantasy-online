using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IStallCommands
{
    Task<Result> StallCreateAsync(byte type, string name, CancellationToken cancellationToken);
    Task<Result> StallAddAsync(byte tab, int slot, int itemId, int count, long price, CancellationToken cancellationToken);
    Task<Result> StallBrowseAsync(int ownerId, CancellationToken cancellationToken);
    Task<Result> StallEndAsync(CancellationToken cancellationToken);
    Task<Result> StallDeleteAsync(int index, CancellationToken cancellationToken);
}
}
