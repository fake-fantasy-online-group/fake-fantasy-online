using System;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int> writeOpenShop;
    private Action<int, int> writeBuyItem;
    private Action<byte, int, int, int, bool> writeSellItem;
    private Action<int, int, int, byte, int, int, long> writeStallDeal;
    internal void BindShopWritersForIndependentRuntime(Action<int> open, Action<int, int> buy,
        Action<byte, int, int, int, bool> sell, Action<int, int, int, byte, int, int, long> stall)
    { writeOpenShop = open; writeBuyItem = buy; writeSellItem = sell; writeStallDeal = stall; }
    public void OpenShopForIndependentRuntime(int shopId) => writeOpenShop(shopId);
    public void BuyItemForIndependentRuntime(int itemId, int count) => writeBuyItem(itemId, count);
    public void SellItemForIndependentRuntime(byte tab, int slot, int count, int itemId, bool confirmed = false) => writeSellItem(tab, slot, count, itemId, confirmed);
    public void StallDealForIndependentRuntime(int ownerId, int index, int count, byte bagTab, int bagSlot, int expectItemId = 0, long expectPrice = 0) =>
        writeStallDeal(ownerId, index, count, bagTab, bagSlot, expectItemId, expectPrice);
}
}
