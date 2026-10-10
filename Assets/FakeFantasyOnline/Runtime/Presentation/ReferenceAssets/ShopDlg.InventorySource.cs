namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private int PlayerHas(int itemId)
    {
        int count = 0;
        for (int tab = 0; tab < 3; tab++)
            foreach (InventoryDlg.Item item in InventoryDlg.Items[tab])
                if (item.id == itemId && !item.pet.HasValue && !item.locked)
                    count = unchecked(count + item.count - CartQtyOf(tab, item.bagIdx));
        return count;
    }

    private bool FindSellSlot(int itemId, out byte tab, out int bagIdx, out int remain)
    {
        for (byte candidateTab = 0; candidateTab < 3; candidateTab++)
            foreach (InventoryDlg.Item item in InventoryDlg.Items[candidateTab])
            {
                if (item.id != itemId || item.pet.HasValue || item.locked) continue;
                int available = unchecked(item.count - CartQtyOf(candidateTab, item.bagIdx));
                if (available <= 0) continue;
                tab = candidateTab;
                bagIdx = item.bagIdx;
                remain = available;
                return true;
            }
        tab = 0;
        bagIdx = -1;
        remain = 0;
        return false;
    }

    private int SellSrcCount()
    {
        if (stallMode) return buyItems.Count;
        int count = 0;
        for (int tab = 0; tab < 3; tab++)
            foreach (InventoryDlg.Item item in InventoryDlg.Items[tab])
                if (!item.locked && unchecked(item.count - CartQtyOf(tab, item.bagIdx)) > 0)
                    count = unchecked(count + 1);
        return count;
    }
}
}
