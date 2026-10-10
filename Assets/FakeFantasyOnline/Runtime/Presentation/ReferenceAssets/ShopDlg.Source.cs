using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private void DrawSource(float w, float rowH, bool block)
    {
        Vector2 mp = Event.current.mousePosition;
        if (!sellMode)
        {
            int row = 0;
            for (int i = 0; i < buyItems.Count; i++)
            {
                SItem item = buyItems[i];
                if (stallMode && item.count <= 0) continue;
                Rect r = new Rect(0, rowH * row, w, 39f * s);
                string label = item.count > 0 ? string.Format("{0} ×{1}", item.name, item.count) : item.name;
                RowCard(r, item.icon, label, item.price, !block && r.Contains(mp), null, item.blocked != 0, item.quality, item.id);
                row++;
                if (!block && r.Contains(mp))
                {
                    if (item.pet.HasValue) tipPet = item.pet;
                    else tipText = TooltipContent.Fmt(TooltipContent.QualityName(item.name, item.quality), item.desc);
                    int max = item.count > 0 ? Mathf.Min(AffordMax(item.price), item.count) : AffordMax(item.price);
                    if (Clicked())
                    {
                        BeginPick(255, item.id, 0, item.name, item.desc, item.icon, item.price, max, item.quality, stallMode ? i : -1);
                        Event.current.Use();
                    }
                }
            }
        }
        else if (!stallMode)
        {
            int row = 0;
            for (byte tab = 0; tab < 3; tab++)
                foreach (InventoryDlg.Item item in InventoryDlg.Items[tab])
                {
                    int remaining = unchecked(item.count - CartQtyOf(tab, item.bagIdx));
                    if (remaining <= 0 || item.locked) continue;
                    Rect r = new Rect(0, rowH * row, w, 39f * s);
                    string label = item.name + (remaining > 1 ? string.Format(" ×{0}", remaining) : "");
                    RowCard(r, item.icon, label, item.sell, !block && r.Contains(mp), null, false, item.quality, item.id);
                    row++;
                    if (!block && r.Contains(mp))
                    {
                        tipText = TooltipContent.Fmt(TooltipContent.QualityName(item.name, item.quality), item.desc);
                        byte capturedTab = tab;
                        InventoryDlg.Item capturedItem = item;
                        int capturedRemaining = remaining;
                        if (Clicked())
                        {
                            FusedGuard.ConfirmAddToSell(capturedItem, () => BeginPick(capturedTab, capturedItem.id,
                                capturedItem.bagIdx, capturedItem.name, capturedItem.desc, capturedItem.icon,
                                capturedItem.sell, capturedRemaining, capturedItem.quality, -1));
                            Event.current.Use();
                        }
                        else if (RightClicked())
                        {
                            FusedGuard.ConfirmAddToSell(capturedItem, () => AddCart(capturedTab, capturedItem.id,
                                capturedItem.bagIdx, capturedItem.name, capturedItem.desc, capturedItem.icon,
                                capturedItem.sell, capturedRemaining, capturedItem.quality, -1));
                            Event.current.Use();
                        }
                    }
                }
        }
        else
        {
            for (int i = 0; i < buyItems.Count; i++)
            {
                SItem item = buyItems[i];
                int remaining = unchecked(item.count - CartQtyOfItem(item.id));
                int owned = PlayerHas(item.id);
                bool unavailable = owned < 1 || remaining < 1;
                Rect r = new Rect(0, rowH * i, w, 39f * s);
                if (unavailable) GUI.color = new Color(1f, 1f, 1f, 0.4f);
                int wanted = Mathf.Max(0, remaining);
                RowCard(r, item.icon, string.Format("{0} 收购{1}", item.name, wanted), item.price,
                    !block && !unavailable && r.Contains(mp), null, false, item.quality, item.id);
                GUI.color = Color.white;
                if (!block && r.Contains(mp))
                {
                    tipText = TooltipContent.Fmt(TooltipContent.QualityName(item.name, item.quality),
                        owned < 1 ? item.desc + "\n(你的背包里没有此物, 无法出售)" : item.desc);
                    if (!unavailable && FindSellSlot(item.id, out byte tab, out int bagIdx, out int available))
                    {
                        int max = Mathf.Min(wanted, available);
                        if (Clicked())
                        {
                            BeginPick(tab, item.id, bagIdx, item.name, item.desc, item.icon, item.price, max, item.quality, i);
                            Event.current.Use();
                        }
                        else if (RightClicked())
                        {
                            AddCart(tab, item.id, bagIdx, item.name, item.desc, item.icon, item.price, max, item.quality, i);
                            Event.current.Use();
                        }
                    }
                }
            }
        }
    }
}
}
