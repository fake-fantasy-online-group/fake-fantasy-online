using System;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action writeOpenWarehouse;
    private Action<int> writeSortWarehouse;
    private Action<int, long> writeWarehouseMoney;
    private Action<int, int, int> writeWarehouseSplit;
    private Action<int, int, int, int> writeWarehouseRearrange;
    private Action<int, int, int> writeSplitItem;
    internal void BindWarehouseActionsForIndependentRuntime(Action open, Action<int> sort,
        Action<int, long> money, Action<int, int, int> split,
        Action<int, int, int, int> rearrange, Action<int, int, int> splitItem)
    {
        writeOpenWarehouse = open; writeSortWarehouse = sort; writeWarehouseMoney = money;
        writeWarehouseSplit = split; writeWarehouseRearrange = rearrange; writeSplitItem = splitItem;
    }
    public void OpenWarehouse() => writeOpenWarehouse();
    public void SortWarehouseForIndependentRuntime(int tab) => writeSortWarehouse(tab);
    public void WarehouseMoneyForIndependentRuntime(int mode, long amount) => writeWarehouseMoney(mode, amount);
    public void WarehouseSplitForIndependentRuntime(int tab, int slot, int count) => writeWarehouseSplit(tab, slot, count);
    public void WarehouseRearrangeForIndependentRuntime(int fromTab, int fromSlot, int toTab, int toSlot) =>
        writeWarehouseRearrange(fromTab, fromSlot, toTab, toSlot);
    public void SplitItemForIndependentRuntime(int tab, int slot, int count) => writeSplitItem(tab, slot, count);
    private Action writeBuyWarehouse;
    internal void BindBuyWarehouseWriterForIndependentRuntime(Action writer) => writeBuyWarehouse = writer;
    public void BuyWarehouseForIndependentRuntime() => writeBuyWarehouse();
    private Action<WarehouseMoveRequest> writeWarehouseMove;
    internal void BindWarehouseMoveWriterForIndependentRuntime(Action<WarehouseMoveRequest> writer) => writeWarehouseMove = writer;
    public void WarehouseMoveForIndependentRuntime(int dir, int fromTab, int fromSlot, int whTab, int count, int toSlot = -1) =>
        writeWarehouseMove(new WarehouseMoveRequest(dir, fromTab, fromSlot, whTab, count, toSlot));
}
}
