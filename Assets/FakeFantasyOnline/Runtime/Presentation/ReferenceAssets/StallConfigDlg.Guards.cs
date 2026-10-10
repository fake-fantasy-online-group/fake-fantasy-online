using System.Collections.Generic;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallConfigDlg
{
    private void DoCreate()
    {
        if (NetClient.I == null) return;
        if (staged.Count == 0)
        {
            SysMsgBox.Show("请先上架至少一件商品。", () => { }, null);
            return;
        }
        var items = new List<InventoryDlg.Item>();
        if (shoptype != 1)
            foreach (Staged entry in staged)
            {
                InventoryDlg.Item item = InventoryDlg.ItemAt(entry.bagTab, entry.bagSlot);
                if (item.id != 0) items.Add(item);
            }
        FusedGuard.ConfirmBatch(items, "上架摆摊", () =>
        {
            if (SocialClient.Party.Count > 1)
                SysMsgBox.Show("你正在组队中。\n摆摊期间将【无法获得队友击杀的经验】。\n确定要摆摊吗？", DoCreateConfirmed, null);
            else DoCreateConfirmed();
        });
    }
}
}
