using System.Collections.Generic;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private void Confirm()
    {
        if (NetClient.I == null) { cart.Clear(); return; }
        var items = new List<InventoryDlg.Item>();
        foreach (Cart entry in cart)
        {
            if (stallMode ? stallType == 0 : entry.tab == 255) continue;
            InventoryDlg.Item item = InventoryDlg.ItemAt(entry.tab, entry.bagIdx);
            if (item.id != 0) items.Add(item);
        }
        FusedGuard.ConfirmBatch(items, stallMode ? "卖给这个摊位" : "卖给商店", ConfirmAfterFusedGuard);
    }

    private void ConfirmAfterFusedGuard()
    {
        if (NetClient.I == null) { cart.Clear(); return; }
        if (stallMode)
        {
            long total = CartTotal();
            int count = 0;
            foreach (Cart entry in cart) count = unchecked(count + entry.qty);
            string message = stallType == 0
                ? string.Format("向摊主 {0} 买入 {1} 件?\n你将付出 {2}", stallName, count, StallDlg.MoneyWords(total))
                : string.Format("把 {0} 件东西卖给摊主 {1}?\n你将得到 {2}", count, stallName, StallDlg.MoneyWords(total));
            var snapshot = new List<Cart>(cart);
            SysMsgBox.Show(message, () =>
            {
                if (NetClient.I == null) { cart.Clear(); return; }
                SendStallCart(snapshot);
                cart.Clear();
            }, () => { });
            return;
        }
        foreach (Cart entry in cart)
        {
            if (entry.tab == 255) NetClient.I.BuyItemForIndependentRuntime(entry.id, entry.qty);
            else NetClient.I.SellItemForIndependentRuntime(entry.tab, entry.bagIdx, entry.qty, entry.id, true);
        }
        cart.Clear();
    }

    private void SendStallCart(List<Cart> entries)
    {
        foreach (Cart entry in entries)
        {
            int index = entry.stallIdx;
            if (stallType == 0)
            {
                if (index < 0 && !stallIdxByItem.TryGetValue(entry.id, out index)) continue;
                NetClient.I?.StallDealForIndependentRuntime(stallOwnerId, index, entry.qty, 0, -1, entry.id, entry.unit);
            }
            else
            {
                if (index < 0)
                {
                    if (!stallWant.TryGetValue(entry.id, out var want)) continue;
                    index = want.Item1;
                }
                NetClient.I?.StallDealForIndependentRuntime(stallOwnerId, index, entry.qty, entry.tab, entry.bagIdx, entry.id, entry.unit);
            }
        }
    }
}
}
