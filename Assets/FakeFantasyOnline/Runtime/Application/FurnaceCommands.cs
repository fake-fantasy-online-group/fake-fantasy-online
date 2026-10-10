using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{
public interface IFurnaceCommands
{
    Task<Result> OpenFurnaceAsync(byte modType, CancellationToken cancellationToken);
    Task<Result> CraftAsync(byte modType, int resultItem, CancellationToken cancellationToken);
    Task<Result> CraftUpgradeAsync(byte mode, byte equipSlot, byte execute, byte tab, int bagSlot,
        byte protect, CancellationToken cancellationToken);
    Task<Result> DrillAsync(byte targetKind, byte tab, int slot, byte execute, byte protect,
        CancellationToken cancellationToken);
    Task<Result> InlayAsync(byte targetKind, byte tab, int slot, byte holeIdx, byte runeTab,
        int runeBagSlot, CancellationToken cancellationToken);
}
}
