using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Official 1.5.8 EnduranceHud equipment-update cone: 060002f8 UpdateFromEquipped and 060002f9
/// Recalc, both carrying the official List&lt;EquipDlg.EquippedItem&gt; parameter. 060002fc is the
/// field-initializer constructor and is recorded as an evidence-backed exclusion instead.
/// </summary>
/// <remarks>
/// The official type flags mark both EnduranceHud and EquipDlg public, but the product tree
/// declares EquipDlg internal and twelve recorded EquipDlg members depend on those files, so the
/// restored UpdateFromEquipped takes internal visibility to keep the parameter type legal. That is
/// a visibility-only accommodation; no behaviour decision follows from it.
/// </remarks>
public partial class EnduranceHud
{
    // Official 0x060002f8, static System.Void EnduranceHud.UpdateFromEquipped(
    // System.Collections.Generic.List<EquipDlg.EquippedItem>): Ensure, then Recalc on the singleton.
    internal static void UpdateFromEquipped(List<EquipDlg.EquippedItem> equipped)
    {
        Ensure();
        I.Recalc(equipped);
    }

    // Official 0x060002f9, instance System.Void EnduranceHud.Recalc(
    // System.Collections.Generic.List<EquipDlg.EquippedItem>): clear the ten part slots, then walk
    // the equipped rows keeping only rows with a positive maxDur and a mapped part slot, and store
    // 2 below the red threshold, 1 below the gold threshold and 0 otherwise.
    private void Recalc(List<EquipDlg.EquippedItem> equipped)
    {
        for (int i = 0; i < 10; i++)
        {
            partState[i] = 0;
        }

        bool any = false;
        if (equipped != null)
        {
            foreach (EquipDlg.EquippedItem item in equipped)
            {
                if (item.maxDur <= 0) continue;
                int part = PartIndex(item.slot);
                if (part < 0) continue;
                int state = (float)item.curDur <= (float)item.maxDur * THRESH_RED ? 2
                    : (float)item.curDur <= (float)item.maxDur * THRESH_GOLD ? 1 : 0;
                if (state != 0) any = true;
                partState[part] = state;
            }
        }

        show = any;
    }
}
}
