using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IRepairReceiveContext
{
    void IRepairReceiveContext.ReceiveRepairQuote(byte mode, int slot, long fee, int count, int itemId,
        string info, byte targetKind, byte tab)
    {
        var icon = itemId != 0 ? NetClient.LoadItemIcon(itemId) : null;
        RepairDlg.Show(mode, slot, fee, count, itemId, info, icon, targetKind, tab);
    }
}
}
