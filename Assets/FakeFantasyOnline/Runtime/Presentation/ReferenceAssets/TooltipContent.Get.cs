namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class TooltipContent
{
    // Official 1.5.8 token 06000f55. dragType 0 is the accelerator bar; 1..4 are the inventory
    // tabs, whose rows are matched on the bag cell index; 5 is the equipped list, matched on the
    // equipment slot. The official body reads the same kind of state the neighbouring
    // HotSlotNumber already reads, so no projection is invented here.
    public static string Get(int dragType, int index)
    {
        switch (dragType)
        {
            case 0:
                return GameHud.HotSlotTip(index);

            case 1:
            case 2:
            case 3:
            case 4:
                // Official walks InventoryDlg.Items[dragType - 1] and keeps the first row whose
                // bagIdx equals the hovered cell; every miss returns null, not the held item.
                foreach (InventoryDlg.Item item in InventoryDlg.Items[dragType - 1])
                {
                    if (item.bagIdx == index) return EndgamePanel.BagTooltip(dragType - 1, item, Fmt(item.name, item.desc));
                }
                return null;

            case 5:
                // Official reaches EquipDlg.equipped through IEnumerable<EquippedItem>, which is
                // why the enumerator is interface-dispatched in both native architectures.
                foreach (EquipDlg.EquippedItem item in EquipDlg.Equipped)
                {
                    if (item.slot == index) return EndgamePanel.EquippedTooltip(item, Fmt(item.name, item.desc));
                }
                return null;

            default:
                return null;
        }
    }
}

}
