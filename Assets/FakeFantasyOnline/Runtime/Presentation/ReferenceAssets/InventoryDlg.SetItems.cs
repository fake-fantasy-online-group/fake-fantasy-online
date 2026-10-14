using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 0x0600051d. Even an out-of-range tab clears the request timer.
    public static void SetItems(int t, List<Item> list)
    {
        if ((uint)t < 4)
        {
            Items[t].Clear();
            Items[t].AddRange(list);
        }
        if (I != null) I.ClearMobileSelection();
        _lockReqAt = 0f;
    }
}
}
