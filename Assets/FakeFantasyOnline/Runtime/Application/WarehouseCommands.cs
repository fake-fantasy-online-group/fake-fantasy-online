using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
namespace FakeFantasyOnline.Application
{
public sealed class WarehouseMoveRequest
{
    public WarehouseMoveRequest(int direction, int fromTab, int fromSlot, int warehouseTab, int count, int toSlot = -1)
    { Direction = direction; FromTab = fromTab; FromSlot = fromSlot; WarehouseTab = warehouseTab; Count = count; ToSlot = toSlot; }
    public int Direction { get; }
    public int FromTab { get; }
    public int FromSlot { get; }
    public int WarehouseTab { get; }
    public int Count { get; }
    public int ToSlot { get; }
}
public interface IWarehouseSortCommands
{
    Task<Result> SortWarehouseAsync(int tab, CancellationToken cancellationToken);
}
public interface IWarehouseCommands
{
    // Native money confirmation writes on the caller thread and preserves send exceptions.
    void WarehouseMoney(int mode, long amount);
    Task<Result> BuyWarehouseAsync(CancellationToken cancellationToken);
    Task<Result> WarehouseMoveAsync(WarehouseMoveRequest request, CancellationToken cancellationToken);
    Task<Result> WarehouseMoneyAsync(int mode, long amount, CancellationToken cancellationToken);
    Task<Result> WarehouseSplitAsync(int tab, int slot, int count, CancellationToken cancellationToken);
    Task<Result> WarehouseRearrangeAsync(int fromTab, int fromSlot, int toTab, int toSlot, CancellationToken cancellationToken);
}
}
