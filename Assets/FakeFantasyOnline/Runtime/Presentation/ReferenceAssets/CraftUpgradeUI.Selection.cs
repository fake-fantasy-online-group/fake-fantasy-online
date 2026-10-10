using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class CraftUpgradeUI
{
    // Official 1.5.8 060001b9. Walks EquipDlg.equipped through the IEnumerable<EquippedItem>
    // interface; every miss leaves the out struct untouched and only the final failure clears
    // all 14 int slots before returning false.
    public static bool TryGetSelected(out EquipDlg.EquippedItem item)
    {
        if (EquipDlg.Equipped != null)
        {
            foreach (EquipDlg.EquippedItem row in EquipDlg.Equipped)
            {
                if (row.slot != SelSlot) continue;
                item = row;
                return true;
            }
        }
        item = new EquipDlg.EquippedItem();
        return false;
    }

    // Official 1.5.8 060001ba. The count is re-read on every step; an out-of-range walk falls
    // back to index -1 so the wrap selects the first equipped row.
    public static void CycleSelect()
    {
        IReadOnlyList<EquipDlg.EquippedItem> equipped = EquipDlg.Equipped;
        if (equipped == null || equipped.Count == 0) return;
        int index = 0;
        while (true)
        {
            if (index >= equipped.Count) { index = -1; break; }
            if (equipped[index].slot == SelSlot) break;
            index++;
        }
        int count = equipped.Count;
        int wrapped = count == 0 ? 0 : (index + 1) / count;
        Select(equipped[(index + 1) - wrapped * count].slot);
    }
}
}
