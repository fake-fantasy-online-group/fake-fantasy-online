using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public interface IRepairCommands
{
    Task<Result> RepairAsync(byte mode, int slot, byte targetKind, byte tab, CancellationToken cancellationToken);
    Task<Result> RepairConfirmAsync(byte mode, int slot, byte targetKind, byte tab, CancellationToken cancellationToken);
}
}
