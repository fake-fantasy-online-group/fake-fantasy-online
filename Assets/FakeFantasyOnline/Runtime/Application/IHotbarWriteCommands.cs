using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface IHotbarWriteCommands
{
    Task<Result> WriteHotbarAsync(int[] ids, int[] kinds, bool expanded, CancellationToken cancellationToken);
}
}
