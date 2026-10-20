using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    private int MobileFixedPotionActionRows(Item item) => 6 +
        (GameHud.CanBindMobilePotion(MobileFixedAction.HealthPotion, tab, item) ? 1 : 0) +
        (GameHud.CanBindMobilePotion(MobileFixedAction.ManaPotion, tab, item) ? 1 : 0) +
        (GameHud.CanBindMobileSaddle(tab, item) ? 1 : 0);

    private int DrawMobileFixedPotionBindings(Rect box, float scale, Item item, bool bindingAllowed)
    {
        int row = 5;
        foreach (MobileFixedAction action in new[] { MobileFixedAction.HealthPotion, MobileFixedAction.ManaPotion })
        {
            if (!GameHud.CanBindMobilePotion(action, tab, item)) continue;
            string label = action == MobileFixedAction.HealthPotion ? "设为红药" : "设为蓝药";
            if (MobilePanelLayout.Action(box, row++, label, scale, bindingAllowed && !item.locked) &&
                GameHud.BindMobilePotion(action, tab, mobileSlot, mobileId)) ClearMobileSelection();
        }
        if (GameHud.CanBindMobileSaddle(tab, item) &&
            MobilePanelLayout.Action(box, row++, "设为上坐骑", scale, bindingAllowed && !item.locked && item.blocked == 0) &&
            GameHud.BindMobileSaddle(tab, mobileSlot, mobileId)) ClearMobileSelection();
        return row;
    }
}
}
