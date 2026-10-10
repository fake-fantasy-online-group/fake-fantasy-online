using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class ShopDlg
{
    public static void OpenStall(WorldStallContentsState snapshot)
    {
        if (snapshot == null) return;
        List<StallDlg.Row> rows = new List<StallDlg.Row>(snapshot.Items.Count);
        for (int i = 0; i < snapshot.Items.Count; i++)
        {
            WorldStallItemState item = snapshot.Items[i];
            PetBrief? pet = item.Pet == null ? (PetBrief?)null : InventoryDlg.ProjectPetBriefForIndependentRuntime(item.Pet);
            if (pet.HasValue)
            {
                PetBrief value = pet.Value;
                value.ppAiUsed = item.PetAiUsed; value.ppAiCap = item.PetAiCapacity;
                value.ppShUsed = item.PetShengUsed; value.ppShCap = item.PetShengCapacity;
                pet = value;
            }
            rows.Add(new StallDlg.Row
            {
                itemId = item.ItemId, count = item.Count, price = item.Price,
                blocked = item.Blocked, quality = item.Quality, name = item.Name, desc = item.Description,
                pet = pet,
                icon = NetClient.LoadItemIcon(item.ItemId)
            });
        }
        OpenStall(snapshot.OwnerId,
            string.IsNullOrEmpty(snapshot.ShopName) ? snapshot.OwnerName : snapshot.ShopName,
            snapshot.Type, rows);
    }

    internal static void OpenStall(WorldStallContentsState snapshot, IReadOnlyList<StallDlg.Row> sourceRows)
    {
        if (snapshot == null || sourceRows == null || sourceRows.Count != snapshot.Items.Count)
        {
            OpenStall(snapshot);
            return;
        }
        List<StallDlg.Row> rows = new List<StallDlg.Row>(sourceRows.Count);
        for (int i = 0; i < sourceRows.Count; i++)
        {
            StallDlg.Row row = sourceRows[i];
            WorldStallItemState item = snapshot.Items[i];
            row.itemId = item.ItemId; row.count = item.Count; row.price = item.Price;
            row.blocked = item.Blocked; row.quality = item.Quality;
            row.name = item.Name; row.desc = item.Description;
            rows.Add(row);
        }
        OpenStall(snapshot.OwnerId,
            string.IsNullOrEmpty(snapshot.ShopName) ? snapshot.OwnerName : snapshot.ShopName,
            snapshot.Type, rows);
    }

    public static void OpenStall(int ownerId, string ownerName, byte type, List<StallDlg.Row> rows)
    {
        Ensure();
        ShopDlg owner = I;
        owner.stallMode = true;
        owner.stallOwnerId = ownerId;
        owner.stallName = ownerName ?? "";
        owner.stallType = type;
        owner.visible = true;
        owner.cart.Clear();
        owner.picking = false;
        owner.scrollR = Vector2.zero;
        owner.scrollL = Vector2.zero;
        owner.stallIdxByItem.Clear();
        owner.stallWant.Clear();
        if (type == 0)
        {
            owner.sellMode = false;
            buyItems.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                StallDlg.Row row = rows[i];
                buyItems.Add(new SItem
                {
                    id = row.itemId, count = row.count, price = unchecked((int)row.price),
                    blocked = row.blocked, quality = row.quality, name = row.name, desc = row.desc,
                    icon = row.icon, pet = row.pet
                });
                owner.stallIdxByItem[row.itemId] = i;
            }
        }
        else
        {
            owner.sellMode = true;
            buyItems.Clear();
            for (int i = 0; i < rows.Count; i++)
            {
                StallDlg.Row row = rows[i];
                buyItems.Add(new SItem
                {
                    id = row.itemId, count = row.count, price = unchecked((int)row.price),
                    quality = row.quality, name = row.name, desc = row.desc,
                    icon = row.icon, pet = row.pet
                });
                owner.stallWant[row.itemId] = (i, row.price);
            }
        }
    }
}
}
