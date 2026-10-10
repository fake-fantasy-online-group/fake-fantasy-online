using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IItemUseCommands
{
    Task<Result> UseItemAsync(int itemId, byte tab, int slot, CancellationToken cancellationToken);
}
}
