using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class StallDetailDlg
{
    private void Confirm()
    {
        long price = PI(gold) * 1000000L + PI(silver) * 1000L + PI(copper);
        int amount = Mathf.Clamp(PI(count), 1, maxCount);
        if (price == 0)
        {
            SysMsgBox.Show("请设置有效价格。", () => { }, null);
            return;
        }
        if (itemId != PetSoulItemId)
        {
            StallConfigDlg.AddStaged(bagTab, bagSlot, itemId, amount, price, itemName, icon, quality, itemDesc);
            visible = false;
            return;
        }
        SysMsgBox.Show("确认以【" + StallDlg.MoneyWords(price) + "】上架宠物【" + itemName + "】?\n宠物售出后无法找回。", () =>
        {
            StallConfigDlg.AddStaged(bagTab, bagSlot, itemId, amount, price, itemName, icon, quality, itemDesc);
            visible = false;
        }, () => { });
    }
}
}
