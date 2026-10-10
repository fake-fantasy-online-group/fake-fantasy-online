using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface IInventoryPlacementCommands
{
    Task<Result> ItemMoveAsync(int tab, int from, int to, CancellationToken cancellationToken);
    Task<Result> DropItemAsync(int tab, int slot, int x, int y, int itemId, bool confirmed,
        CancellationToken cancellationToken);
}
}
