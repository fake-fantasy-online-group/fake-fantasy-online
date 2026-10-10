using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    private int CartQtyOf(int tab, int bagIdx)
    {
        int result = 0;
        foreach (Cart entry in cart)
            if (entry.tab == tab && entry.bagIdx == bagIdx) result = unchecked(result + entry.qty);
        return result;
    }

    private int CartQtyOfItem(int itemId)
    {
        int result = 0;
        foreach (Cart entry in cart)
            if (entry.id == itemId) result = unchecked(result + entry.qty);
        return result;
    }

    private void AddCart(byte tab, int id, int bagIdx, string name, string desc, Texture2D icon,
        long unit, int qty, int quality = 0, int stallIdx = -1)
    {
        int index = cart.FindIndex(c => c.tab == tab && c.bagIdx == bagIdx && c.id == id && c.stallIdx == stallIdx);
        if (index >= 0)
        {
            Cart entry = cart[index];
            entry.qty = Mathf.Clamp(unchecked(entry.qty + qty), 1, PickMaxCap);
            cart[index] = entry;
        }
        else cart.Add(new Cart
        {
            tab = tab, id = id, bagIdx = bagIdx, name = name, desc = desc, icon = icon,
            unit = unit, qty = Mathf.Clamp(qty, 1, PickMaxCap), quality = unchecked((byte)quality), stallIdx = stallIdx
        });
    }

    private void AddToCart(byte tab, int id, int bagIdx, string name, string desc, Texture2D icon,
        long unit, int qty, int quality = 0, int stallIdx = -1)
    {
        int index = cart.FindIndex(c => c.tab == tab && c.bagIdx == bagIdx && c.id == id && c.stallIdx == stallIdx);
        if (index >= 0)
        {
            Cart entry = cart[index];
            entry.qty = Mathf.Clamp(unchecked(entry.qty + qty), 1, PickMaxCap);
            cart[index] = entry;
        }
        else cart.Add(new Cart
        {
            tab = tab, id = id, bagIdx = bagIdx, name = name, desc = desc, icon = icon,
            unit = unit, qty = qty, quality = unchecked((byte)quality), stallIdx = stallIdx
        });
    }

    private void BeginPick(byte tab, int id, int bagIdx, string name, string desc, Texture2D icon,
        long unit, int max, int quality = 0, int stallIdx = -1)
    {
        if (max <= 0) return;
        if (max == 1 && sellMode)
        {
            AddToCart(tab, id, bagIdx, name, desc, icon, unit, 1, quality, stallIdx);
            return;
        }
        pickQuality = quality;
        pickStallIdx = stallIdx;
        picking = true;
        pickTab = tab; pickId = id; pickBagIdx = bagIdx;
        pickName = name; pickDesc = desc; pickIcon = icon; pickUnit = unit;
        pickMax = Mathf.Min(max, PickMaxCap);
        pickQty = 1;
        pickText = "1";
    }
}
}
