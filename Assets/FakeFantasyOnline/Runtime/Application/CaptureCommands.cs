using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface ICaptureCommands
{
    Task<Result> CaptureAsync(int targetId, int itemId, CancellationToken cancellationToken);
}
}
