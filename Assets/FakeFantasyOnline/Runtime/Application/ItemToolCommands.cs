using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IItemToolCommands
{
    Task<Result> UseItemOnAsync(int toolId, byte targetKind, byte tab, int slot,
        CancellationToken cancellationToken);
}
}
