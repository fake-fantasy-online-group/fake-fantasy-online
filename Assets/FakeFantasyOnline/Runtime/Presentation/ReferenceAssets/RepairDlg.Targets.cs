namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class RepairDlg
{
    public static void PickSlot(int equipSlot)
    {
        if (!Picking) return;
        if (NetClient.I != null) NetClient.I.RepairForIndependentRuntime(PickMode, equipSlot, 1, 0);
        CancelPick();
    }

    public static void PickBag(byte bagTab, int bagSlot)
    {
        if (!Picking) return;
        if (NetClient.I != null) NetClient.I.RepairForIndependentRuntime(PickMode, bagSlot, 0, bagTab);
        CancelPick();
    }
}
}
