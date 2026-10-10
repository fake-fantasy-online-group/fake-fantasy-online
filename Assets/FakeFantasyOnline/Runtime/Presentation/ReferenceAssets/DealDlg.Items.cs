using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
// Complete official 1.5.8 DrawItems and its captured offer callback (06000f81/f88/f89).
// The native window invokes this member once for each trade side.
internal sealed partial class DealDlg : MonoBehaviour
{
    public static DealDlg I;
    private float s;
    internal string LastFusedMessageForIndependentRuntime { get; private set; } = string.Empty;

    private void DrawItems(Rect area, List<(int, int, string)> items, bool mine, Vector2 mouse)
    {
        float step = 37 * s, size = 35 * s;
        for (int i = 0; i < items.Count && i < 20; i++)
        {
            Texture2D icon = NetClient.LoadItemIcon(items[i].Item1);
            Rect cell = new Rect(area.x + (i % 5) * step, area.y + (i / 5) * step, size, size);
            if (icon != null) ItemIconUI.Draw(new Rect(cell.x + 4 * s, cell.y + 4 * s, size - 8 * s, size - 8 * s), icon, s);
            else if (BitmapFont.Ready)
                BitmapFont.DrawOutline(cell.x + 3 * s, cell.y + 12 * s,
                    items[i].Item3.Length > 2 ? items[i].Item3.Substring(0, 2) : items[i].Item3, TradeUI.GOLD, s);
            if (items[i].Item2 > 1 && BitmapFont.Ready)
                BitmapFont.DrawOutline(cell.xMax - 14 * s, cell.yMax - 13 * s, items[i].Item2.ToString(), Color.white, s);
            GameHud.DrawWashScrollTag(cell, items[i].Item1, s);
            if (cell.Contains(mouse))
            {
                var pets = mine ? SocialClient.MyItemPets : SocialClient.OtherItemPets;
                if (pets.TryGetValue(i, out PetBrief pet))
                {
                    PetInfoTooltip.Show(pet, Event.current.mousePosition, s);
                    return; // Native return skips subsequent cells and the held-item tail.
                }
                var info = mine ? SocialClient.MyItemInfo : SocialClient.OtherItemInfo;
                string description = i < info.Count ? info[i].Item1 : string.Empty;
                string title = i < info.Count && info[i].Item2 > 0
                    ? ((char)('0' + info[i].Item2)).ToString() + items[i].Item3 : items[i].Item3;
                string tip = description;
                if (!string.IsNullOrEmpty(title))
                {
                    tip = "\u0007" + title;
                    if (!string.IsNullOrEmpty(description)) tip += "\n" + description;
                }
                UITip.Show(tip, s);
            }
            if (mine && !SocialClient.MyLocked && cell.Contains(mouse) &&
                Event.current.type == EventType.MouseDown && Event.current.button == 0 && i < myOffer.Count)
            {
                myOffer.RemoveAt(i); SendOffer(); Event.current.Use();
            }
        }
        if (!mine || SocialClient.MyLocked || !InventoryDlg.HasHeld || !area.Contains(mouse) ||
            Event.current.type != EventType.MouseDown || Event.current.button != 0) return;
        int tab = InventoryDlg.HeldTab, slot = InventoryDlg.HeldBagSlot;
        int count = 1;
        foreach (InventoryDlg.Item item in InventoryDlg.Items[tab])
            if (item.bagIdx == slot) { count = item.count; break; }
        if (!InventoryDlg.WarnIfLocked(tab, slot, "交易"))
        {
            bool bound = false;
            foreach (InventoryDlg.Item item in InventoryDlg.Items[tab])
                if (item.bagIdx == slot) { bound = item.bound; break; }
            if (!bound)
            {
                InventoryDlg.Item item = InventoryDlg.ItemAt(tab, slot);
                byte capturedTab = (byte)tab;
                int capturedSlot = slot, capturedCount = count;
                InventoryDlg.ClearHeld(); Event.current.Use();
                FusedGuard.Confirm(item, "放到交易栏", () =>
                {
                    if (!myOffer.Exists(offer => offer.Item1 == capturedTab && offer.Item2 == capturedSlot) && myOffer.Count < 20)
                        myOffer.Add((capturedTab, capturedSlot, capturedCount));
                    SendOffer();
                });
                LastFusedMessageForIndependentRuntime = FusedGuard.Has(item) && SysMsgBox.I != null
                    ? SysMsgBox.I.MessageForIndependentRuntime : string.Empty;
                return;
            }
            ChatUI.Add("系统", "该物品已绑定, 不能交易(可用天工剪解绑)", new Color(1, 180f / 255, 120f / 255, 1));
        }
        InventoryDlg.ClearHeld(); Event.current.Use();
    }
    // The real List type is part of the official call shape: NetClient.TradeOffer (0x06000751)
    // takes a List<(byte,int,int)> and its writer enumerates it with the List struct enumerator.
    internal List<(byte, int, int)> OfferForIndependentRuntime => myOffer;
}
}
