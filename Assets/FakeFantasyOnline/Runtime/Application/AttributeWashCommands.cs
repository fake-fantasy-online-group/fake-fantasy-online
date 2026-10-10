using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IAttributeWashCommands
{
    Task<Result> WashAffixAsync(byte kind, byte tab, int slot, int index, CancellationToken cancellationToken);
}
}
