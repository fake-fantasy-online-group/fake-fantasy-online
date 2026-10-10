using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroceryStatusDlg : MonoBehaviour
{
    public static GroceryStatusDlg I;
    private static List<StallDlg.Row> rows = new List<StallDlg.Row>();
    private bool visible, tried;
    private Texture2D bg, icoClose, icoHelp, icoEnd, icoBuyLabel, slideBtn;
    private const float NW = 291f, NH = 443f;
    private float s;
    private float winX = float.NaN, winY;
    private int _wdGen = -1;
    private Vector2 statusScroll;
    private bool sbDrag;
    private float sbGrab;

    public static bool Visible => I != null && I.visible;
    public static void Ensure()
    {
        if (I != null) return;
        var go = new GameObject("GroceryStatusDlg");
        I = go.AddComponent<GroceryStatusDlg>();
        DontDestroyOnLoad(go);
    }
    public static void Toggle()
    {
        Ensure();
        I.visible = !I.visible;
    }
    public static void OpenRunning(int shopType)
    {
        Ensure();
        GroUI.ShopType = shopType > 0 ? 1 : 0;
        I.visible = true;
    }
    public static void ShowContents(byte type, string name, List<StallDlg.Row> r)
    {
        Ensure();
        GroUI.ShopType = type != 0 ? 1 : 0;
        rows = r ?? new List<StallDlg.Row>();
        I.visible = true;
        StallConfigDlg.LocalStalling = true;
        StallConfigDlg.LocalType = (byte)GroUI.ShopType;
        if (!string.IsNullOrEmpty(name)) StallConfigDlg.LocalName = name;
    }

    public static void ShowContents(WorldStallContentsState snapshot)
    {
        if (snapshot == null) return;
        List<StallDlg.Row> projected = new List<StallDlg.Row>(snapshot.Items.Count);
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
            projected.Add(new StallDlg.Row
            {
                itemId = item.ItemId, count = item.Count, price = item.Price,
                blocked = item.Blocked, quality = item.Quality, name = item.Name, desc = item.Description,
                pet = pet,
                icon = NetClient.LoadItemIcon(item.ItemId)
            });
        }
        List<StallSalesBox.Sale> projectedSales = new List<StallSalesBox.Sale>(snapshot.Sales.Count);
        for (int i = 0; i < snapshot.Sales.Count; i++)
        {
            WorldStallSaleState sale = snapshot.Sales[i];
            projectedSales.Add(new StallSalesBox.Sale { t = sale.Time, item = sale.Item, cnt = sale.Count, money = sale.Money });
        }
        ShowContents(snapshot.Type,
            string.IsNullOrEmpty(snapshot.ShopName) ? snapshot.OwnerName : snapshot.ShopName,
            projected);
        StallSalesBox.SetSales(snapshot.Type, projectedSales);
    }

    internal static void ShowContents(WorldStallContentsState snapshot, IReadOnlyList<StallDlg.Row> sourceRows)
    {
        if (snapshot == null || sourceRows == null || sourceRows.Count != snapshot.Items.Count)
        {
            ShowContents(snapshot);
            return;
        }
        List<StallDlg.Row> projected = new List<StallDlg.Row>(sourceRows.Count);
        for (int i = 0; i < sourceRows.Count; i++)
        {
            StallDlg.Row row = sourceRows[i];
            WorldStallItemState item = snapshot.Items[i];
            row.itemId = item.ItemId; row.count = item.Count; row.price = item.Price;
            row.blocked = item.Blocked; row.quality = item.Quality;
            row.name = item.Name; row.desc = item.Description;
            projected.Add(row);
        }
        ShowContents(snapshot.Type,
            string.IsNullOrEmpty(snapshot.ShopName) ? snapshot.OwnerName : snapshot.ShopName,
            projected);
        List<StallSalesBox.Sale> projectedSales = new List<StallSalesBox.Sale>(snapshot.Sales.Count);
        for (int i = 0; i < snapshot.Sales.Count; i++)
        {
            WorldStallSaleState sale = snapshot.Sales[i];
            projectedSales.Add(new StallSalesBox.Sale { t = sale.Time, item = sale.Item, cnt = sale.Count, money = sale.Money });
        }
        StallSalesBox.SetSales(snapshot.Type, projectedSales);
    }
    private void Load()
    {
        tried = true;
        ResLoader loader = ResLoader.I;
        if (loader == null) { tried = false; return; }
        bg = loader.LoadGsa("res/uires/grocerydlg/grocerystatus.gsa");
        icoClose = loader.LoadGsa("res/uires/common/miniclose.gsa");
        icoHelp = loader.LoadGsa("res/uires/gameui/helpicon.gsa");
        icoEnd = loader.LoadGsa("res/uires/grocerydlg/endshop.gsa");
        icoBuyLabel = loader.LoadGsa("res/uires/grocerydlg/buylabel.gsa");
        slideBtn = loader.LoadGsa("res/uires/common/slidebtn.gsa");
    }
}
}
