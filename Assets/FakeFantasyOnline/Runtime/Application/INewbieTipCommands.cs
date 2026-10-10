using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface INewbieTipCommands
{
    Task<Result> MarkNewbieTipSeenAsync(int tipId, CancellationToken cancellationToken);
}
}
