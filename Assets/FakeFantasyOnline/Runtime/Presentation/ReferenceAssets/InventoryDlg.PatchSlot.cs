namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 0x0600051c; the capturing predicate is 0x0600053c.
    public static void PatchSlot(int tab, int bagIdx, int itemId, Item newItem, bool lockAuthoritative = false)
    {
        if (tab < 0 || tab > 3) return;
        var bag = Items[tab];
        int index = bag.FindIndex(item => item.bagIdx == bagIdx);
        if (itemId <= 0)
        {
            if (index >= 0) bag.RemoveAt(index);
            return;
        }
        if (!lockAuthoritative && index >= 0 && bag[index].id == itemId)
            newItem.locked = bag[index].locked;
        if (index >= 0) bag[index] = newItem;
        else bag.Add(newItem);
    }
}
}
