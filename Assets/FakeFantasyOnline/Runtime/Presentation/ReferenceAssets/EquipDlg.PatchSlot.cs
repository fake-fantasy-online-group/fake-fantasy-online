namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class EquipDlg
{
    // Official 0x060002fe; the captured-slot predicate is 0x0600031d.
    public static void PatchSlot(int slot, EquippedItem it)
    {
        int index = equipped.FindIndex(item => item.slot == slot);
        if (it.id <= 0)
        {
            if (index >= 0) equipped.RemoveAt(index);
        }
        else if (index >= 0) equipped[index] = it;
        else equipped.Add(it);
    }
}
}
