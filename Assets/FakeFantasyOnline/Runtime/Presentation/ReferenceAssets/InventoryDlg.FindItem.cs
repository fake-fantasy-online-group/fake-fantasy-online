namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class InventoryDlg
{
    public partial struct Item
    {
        public int bagIdx;
        public bool bound;
        public bool cosmetic;
    }

    // Original FindItem 0600050a; no count/blocked/locked filter is added.
    public static (bool, byte, int, bool, bool, string) FindItem(int itemId)
    {
        for (int tab = 0; tab < Items.Length; tab++)
            foreach (Item item in Items[tab])
                if (item.id == itemId && !item.pet.HasValue)
                    return (true, (byte)tab, item.bagIdx, item.bound, item.cosmetic, item.name);
        return (false, byte.MaxValue, -1, false, false, string.Empty);
    }
}

}
