using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IShopItemsReceiveContext
{
    IShopItemsReceiveBuffer IShopItemsReceiveContext.CreateShopItemsBuffer(int capacity) => new ShopItemsBuffer(this, capacity);

    private sealed class ShopItemsBuffer : IShopItemsReceiveBuffer
    {
        private readonly ClientRootView root;
        private readonly List<ShopDlg.SItem> items;
        public ShopItemsBuffer(ClientRootView root, int capacity)
        { this.root = root; items = new List<ShopDlg.SItem>(capacity); }
        public void Add(int id, int price, string name, string description)
        {
            var icon = NetClient.LoadItemIcon(id);
            items.Add(new ShopDlg.SItem { id = id, price = price, name = name, desc = description, icon = icon });
        }
        public void SetBlocked(int index, byte blocked)
        {
            ShopDlg.SItem item = items[index];
            item.blocked = blocked;
            items[index] = item;
        }
        public void Commit(int shopId)
        {
            ShopDlg.SetItems(shopId, items);
            ShopDlg.I.BindRepairSelectionForIndependentRuntime(mode => root._firstWorldWindow?.BeginRepairSelectionForIndependentRuntime(mode));
        }
    }
}
}
