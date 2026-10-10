using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IGroundPickupCommands
{
    Task<Result> PickItemAsync(int groundId, CancellationToken cancellationToken);
}
}
