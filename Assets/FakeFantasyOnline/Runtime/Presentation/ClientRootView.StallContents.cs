using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace FakeFantasyOnline.Presentation
{
public sealed partial class ClientRootView : IStallContentsReceiveContext
{
    private WorldStallContentsState _stallContentsState = WorldStallContentsState.Empty;
    private long _stallContentsStateVersion;
    public WorldStallContentsState StallContentsState => _stallContentsState;
    public long StallContentsStateVersion => _stallContentsStateVersion;
    IStallContentsReceiveBuffer IStallContentsReceiveContext.CreateStallContentsBuffer(int capacity) =>
        new StallContentsBuffer(this, capacity);

    private sealed class StallContentsBuffer : IStallContentsReceiveBuffer
    {
        private readonly ClientRootView root;
        private readonly List<StallDlg.Row> rows;
        private List<StallSalesBox.Sale> sales;
        public StallContentsBuffer(ClientRootView root, int capacity)
        {
            this.root = root;
            rows = new List<StallDlg.Row>(capacity);
        }
        public void AddRow(int itemId, int count, long price, string name, string description)
        {
            var icon = NetClient.LoadItemIcon(itemId);
            rows.Add(new StallDlg.Row { itemId = itemId, count = count, price = price,
                name = name, desc = description, icon = icon });
        }
        public void BeginSales(int capacity) => sales = new List<StallSalesBox.Sale>(capacity);
        public void AddSale(long time, string item, int count, long money) =>
            sales.Add(new StallSalesBox.Sale { t = time, item = item, cnt = count, money = money });
        public void SetBlocked(int index, byte blocked)
        {
            StallDlg.Row row = rows[index]; row.blocked = blocked; rows[index] = row;
        }
        public void SetPet(int index, PetBrief pet)
        {
            StallDlg.Row row = rows[index];
            row.pet = pet;
            rows[index] = row;
        }
        public void SetQuality(int index, byte quality)
        {
            StallDlg.Row row = rows[index]; row.quality = quality; rows[index] = row;
        }
        public bool HasPet(int index) => rows[index].pet.HasValue;
        public void SetPetPoints(int index, int aiUsed, int aiCapacity, int shengUsed, int shengCapacity)
        {
            StallDlg.Row row = rows[index];
            PetBrief pet = row.pet.Value;
            pet.ppAiUsed = aiUsed; pet.ppAiCap = aiCapacity;
            pet.ppShUsed = shengUsed; pet.ppShCap = shengCapacity;
            row.pet = pet; rows[index] = row;
        }
        public void Commit(int ownerId, string ownerName, byte type, string shopName)
        {
            var snapshotItems = new List<WorldStallItemState>(rows.Count);
            for (int index = 0; index < rows.Count; index++)
            {
                StallDlg.Row row = rows[index];
                snapshotItems.Add(new WorldStallItemState(row.itemId, row.count, row.price, row.name, row.desc, row.blocked, row.quality, row.pet.HasValue ? WorldInventoryPetState.FromPetBrief(row.pet.Value) : null,
                    row.pet.HasValue ? row.pet.Value.ppAiUsed : 0,
                    row.pet.HasValue ? row.pet.Value.ppAiCap : 0,
                    row.pet.HasValue ? row.pet.Value.ppShUsed : 0,
                    row.pet.HasValue ? row.pet.Value.ppShCap : 0));
            }
            var snapshotSales = new List<WorldStallSaleState>(sales == null ? 0 : sales.Count);
            if (sales != null) foreach (StallSalesBox.Sale sale in sales)
                snapshotSales.Add(new WorldStallSaleState(sale.t, sale.item, sale.cnt, sale.money));
            root._stallContentsState = new WorldStallContentsState(ownerId, ownerName, type, shopName, snapshotItems, snapshotSales);
            root._stallContentsStateVersion = unchecked(root._stallContentsStateVersion + 1L);
            if (ownerId == NetClient.SelfEntityId)
            {
                GroceryStatusDlg.Ensure();
                GroceryStatusDlg.ShowContents(root._stallContentsState, rows);
            }
            else
            {
                ShopDlg.OpenStall(root._stallContentsState, rows);
                ShopDlg.I.BindRepairSelectionForIndependentRuntime(mode => root._firstWorldWindow?.BeginRepairSelectionForIndependentRuntime(mode));
            }
        }
    }
}
}
