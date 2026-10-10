using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class ReferenceWorldReceiveOwner : IWorldStaticSnapshotReceiveOwner
{
    public IWorldInventorySnapshotRows BeginInventorySnapshotRows(int itemCount) =>
        new InventorySnapshotRows(this, itemCount);

    public IWorldInventoryDeltaRows BeginInventoryDeltaRows(int itemCount) =>
        new InventoryDeltaRows(itemCount);

    private sealed class InventoryDeltaRows : IWorldInventoryDeltaRows
    {
        private readonly List<(int Tab, InventoryDlg.Item Item)> items;
        private List<EquipDlg.EquippedItem> equipped;

        internal InventoryDeltaRows(int itemCount) =>
            items = new List<(int, InventoryDlg.Item)>(itemCount);

        public void AddItem(WorldInventoryItemState item)
        {
            // Unlike 8006, 8069 skips the icon load for a removal row. Positive rows
            // load before the next wire row, even when a later tail/patch will fail.
            var icon = item.ItemId > 0 ? NetClient.LoadItemIcon(item.IconLookupId) : null;
            InventoryDlg.Item nativeItem = InventoryDlg.ProjectItemForIndependentRuntime(item, icon);
            nativeItem.fusedDesc = null;
            items.Add((item.Tab, nativeItem));
        }

        public void BeginEquipped() => equipped = new List<EquipDlg.EquippedItem>();

        public void AddEquipped(WorldInventoryEquippedItemState item)
        {
            var icon = NetClient.LoadItemIcon(item.IconLookupId);
            equipped.Add(EquipDlg.ProjectEquippedForIndependentRuntime(item, icon));
        }

        public void SetLocked(int ordinal, bool locked)
        {
            var row = items[ordinal];
            row.Item.locked = locked;
            items[ordinal] = row;
        }

        public void SetFusedDescription(int ordinal, string description)
        {
            var row = items[ordinal];
            row.Item.fusedDesc = description;
            items[ordinal] = row;
        }

        public void SetGrowthTier(int ordinal, int growthTier)
        {
            var row = items[ordinal];
            if (!row.Item.pet.HasValue) return;
            PetBrief pet = row.Item.pet.Value;
            pet.growTier = growthTier;
            row.Item.pet = pet;
            items[ordinal] = row;
        }

        public void SetEquippedQuality(int ordinal, byte quality)
        {
            EquipDlg.EquippedItem item = equipped[ordinal];
            item.quality = quality;
            equipped[ordinal] = item;
        }

        public void PatchItem(int ordinal, bool lockAuthoritative)
        {
            var row = items[ordinal];
            InventoryDlg.PatchSlot(row.Tab, row.Item.bagIdx, row.Item.id,
                row.Item, lockAuthoritative);
        }

        public void PatchEquipped()
        {
            foreach (EquipDlg.EquippedItem item in equipped)
                EquipDlg.PatchSlot(item.slot, item);
        }

        public void Publish(WorldInventoryState state)
        {
            // The original owners were patched in wire order already. The observation
            // only advances their identity markers; it cannot load or apply rows again.
            InventoryDlg.PublishReceivedDeltaForIndependentRuntime(state);
            EquipDlg.PublishReceivedDeltaForIndependentRuntime(state);
        }
    }

    private sealed class InventorySnapshotRows : IWorldInventorySnapshotRows
    {
        private readonly ReferenceWorldReceiveOwner owner;
        private readonly List<InventoryDlg.Item>[] bags;
        private readonly List<(int Tab, int Index)> ordinals;
        private List<EquipDlg.EquippedItem> equipped;

        internal InventorySnapshotRows(ReferenceWorldReceiveOwner owner, int itemCount)
        {
            this.owner = owner;
            bags = new[] { new List<InventoryDlg.Item>(), new List<InventoryDlg.Item>(),
                new List<InventoryDlg.Item>(), new List<InventoryDlg.Item>() };
            ordinals = new List<(int, int)>(itemCount);
        }

        public void AddItem(WorldInventoryItemState item)
        {
            List<InventoryDlg.Item> bag = bags[item.Tab];
            ordinals.Add((item.Tab, bag.Count));
            // Native loads even for a nonpositive id, before the next row/header.
            var icon = NetClient.LoadItemIcon(item.IconLookupId);
            InventoryDlg.Item nativeItem = InventoryDlg.ProjectItemForIndependentRuntime(item, icon);
            nativeItem.fusedDesc = null;
            bag.Add(nativeItem);
        }

        public void BeginEquipped() => equipped = new List<EquipDlg.EquippedItem>();

        public void AddEquipped(WorldInventoryEquippedItemState item)
        {
            var icon = NetClient.LoadItemIcon(item.IconLookupId);
            equipped.Add(EquipDlg.ProjectEquippedForIndependentRuntime(item, icon));
        }

        public void LockItem(int ordinal)
        {
            var location = ordinals[ordinal];
            InventoryDlg.Item item = bags[location.Tab][location.Index];
            item.locked = true;
            bags[location.Tab][location.Index] = item;
        }

        public void SetPetPoints(int ordinal, int aiUsed, int aiCapacity, int shengUsed, int shengCapacity)
        {
            var location = ordinals[ordinal];
            InventoryDlg.Item item = bags[location.Tab][location.Index];
            if (!item.pet.HasValue) return;
            PetBrief pet = item.pet.Value;
            pet.ppAiUsed = aiUsed;
            pet.ppAiCap = aiCapacity;
            pet.ppShUsed = shengUsed;
            pet.ppShCap = shengCapacity;
            item.pet = pet;
            bags[location.Tab][location.Index] = item;
        }

        public void SetFusedDescription(int ordinal, string description)
        {
            var location = ordinals[ordinal];
            InventoryDlg.Item item = bags[location.Tab][location.Index];
            item.fusedDesc = description;
            bags[location.Tab][location.Index] = item;
        }

        public void SetGrowthTier(int ordinal, int growthTier)
        {
            var location = ordinals[ordinal];
            InventoryDlg.Item item = bags[location.Tab][location.Index];
            if (!item.pet.HasValue) return;
            PetBrief pet = item.pet.Value;
            pet.growTier = growthTier;
            item.pet = pet;
            bags[location.Tab][location.Index] = item;
        }

        public void SetEquippedQuality(int ordinal, byte quality)
        {
            EquipDlg.EquippedItem item = equipped[ordinal];
            item.quality = quality;
            equipped[ordinal] = item;
        }

        public void Publish(WorldInventoryState state)
        {
            InventoryDlg.PublishReceivedForIndependentRuntime(state, bags);
            EquipDlg.PublishReceivedForIndependentRuntime(state, equipped);
            owner.ReceiveInventorySnapshot(state);
        }
    }

    public void ReceiveInventorySnapshot(WorldInventoryState state)
    {
        InventoryDlg.BindFieldsForIndependentRuntime(state);
        EquipDlg.BindEquippedForIndependentRuntime(state);
        NetClient.I?.ApplyEquipmentPublicationForIndependentRuntime(state.EquipmentSnapshotItems);
    }

    public void ReceiveLifeSkillSnapshot(WorldLifeSkillBookState state)
    {
        var learned = new Dictionary<int, (int, string)>();
        foreach (WorldLifeSkillState row in state.Skills)
            learned[row.Id] = (row.Level, row.DisplayName);
        Win05.SetLifeSkills(learned, new HashSet<int>(state.LearnableIds));
    }

    public void ReceivePartySnapshot(WorldPartyState state) =>
        SocialClient.BindPartyForIndependentRuntime(state);
}
}
