namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    public static Item ItemAt(int tab, int bagIdx)
    {
        if (tab < 0 || tab >= Items.Length || Items[tab] == null) return default;
        foreach (Item item in Items[tab])
            if (item.bagIdx == bagIdx) return item;
        return default;
    }
}
}
