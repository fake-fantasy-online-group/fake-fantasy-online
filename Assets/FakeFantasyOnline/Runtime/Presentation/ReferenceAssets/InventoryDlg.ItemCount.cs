using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using UnityEngine;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Inventory field projection; full publications enter the original SetItems owner.
internal partial class InventoryDlg : MonoBehaviour
{
    public static long Money;
    public static long CaiYu;
    public static int Weight;
    public static int MaxWeight;
    public static readonly List<Item>[] Items =
    {
        new List<Item>(), new List<Item>(), new List<Item>(), new List<Item>()
    };

    public partial struct Item
    {
        public int id;
        public int count;
        public int sell;
        public int quality;
        public byte blocked;
        public Texture2D icon;
        public string name;
        public string desc;
        public string fusedDesc;
        public PetBrief? pet;
    }

    private static IReadOnlyList<WorldInventoryEquippedItemState> fullPublicationForIndependentRuntime;
    private static WorldInventoryState boundPublicationForIndependentRuntime;

    internal static void ObservePublicationForIndependentRuntime(WorldInventoryState state)
    {
        if (ReferenceEquals(boundPublicationForIndependentRuntime, state)) return;
        if (!state.IsInventoryDeltaAfter(boundPublicationForIndependentRuntime))
        {
            BindFieldsForIndependentRuntime(state);
            return;
        }
        try
        {
            // Money (+0x10) and CaiYu (+0x18) are not written here. Both are receive-step stores
            // of the packet that carried them (IWorldReceiveOwner.ReceiveInventoryHeader), so
            // re-writing them from this retained delta would let an earlier packet's values be
            // applied again after a later packet had already stored its own.
            foreach (WorldInventoryItemPatch patch in state.InventoryPatches)
            {
                WorldInventoryItemState item = patch.Item;
                PatchSlot(item.Tab, item.BagIndex, item.ItemId,
                    ProjectItemForIndependentRuntime(item), patch.LockAuthoritative);
            }
            boundPublicationForIndependentRuntime = state;
        }
        catch
        {
            // Partial writes no longer represent the recorded base. Do not replay the
            // raw batch against that damaged base on the next observation.
            boundPublicationForIndependentRuntime = null;
            throw;
        }
    }

    internal static void BindFieldsForIndependentRuntime(WorldInventoryState state)
    {
        // Receipt already installed this exact publication. Empty is a mirror sentinel.
        if (ReferenceEquals(state, WorldInventoryState.Empty) ||
            ReferenceEquals(boundPublicationForIndependentRuntime, state)) return;
        try { ProjectFieldsForIndependentRuntime(state); }
        catch
        {
            boundPublicationForIndependentRuntime = null;
            throw;
        }
    }

    private static void ProjectFieldsForIndependentRuntime(WorldInventoryState state)
    {
        // Money (+0x10), CaiYu (+0x18), Weight (+0x20) and MaxWeight (+0x24) are not projected
        // from a state at all: official 1.5.8 writes them only inside the 0x8006 and 0x8069 case
        // bodies (0x8007 writes +0x24 alone), once per packet that really arrives, so this
        // projection would otherwise re-apply an earlier packet's values - or the
        // WorldInventoryState.Empty sentinel, which stands for "no inventory packet yet" and is
        // not a publication - after a later packet had already stored its own.
        if (!ReferenceEquals(state.EquipmentSnapshotItems, WorldInventoryState.Empty.EquipmentSnapshotItems) &&
            !ReferenceEquals(fullPublicationForIndependentRuntime, state.EquipmentSnapshotItems))
        {
            var bags = new[] { new List<Item>(), new List<Item>(), new List<Item>(), new List<Item>() };
            foreach (WorldInventoryItemState item in state.InventorySnapshotItems)
                bags[item.Tab].Add(ProjectItemForIndependentRuntime(item));
            for (int tab = 0; tab < bags.Length; tab++) SetItems(tab, bags[tab]);
            fullPublicationForIndependentRuntime = state.EquipmentSnapshotItems;
            if (ReferenceEquals(state.Items, state.InventorySnapshotItems))
            {
                boundPublicationForIndependentRuntime = state;
                return;
            }
        }
        // Rebuilding/forceSync or a missing predecessor projects authoritative final
        // contents without guessing/replaying patches and without clearing the timer.
        foreach (List<Item> bag in Items) bag.Clear();
        foreach (WorldInventoryItemState item in state.Items)
            Items[item.Tab].Add(ProjectItemForIndependentRuntime(item));
        boundPublicationForIndependentRuntime = state;
    }

    internal static void PublishReceivedForIndependentRuntime(WorldInventoryState state, List<Item>[] bags)
    {
        try
        {
            for (int tab = 0; tab < bags.Length; tab++) SetItems(tab, bags[tab]);
            fullPublicationForIndependentRuntime = state.EquipmentSnapshotItems;
            boundPublicationForIndependentRuntime = state;
        }
        catch
        {
            boundPublicationForIndependentRuntime = null;
            throw;
        }
    }

    internal static void PublishReceivedDeltaForIndependentRuntime(WorldInventoryState state) =>
        boundPublicationForIndependentRuntime = state;

    private static Item ProjectItemForIndependentRuntime(WorldInventoryItemState item) =>
        ProjectItemForIndependentRuntime(item,
            item.ItemId < 1 ? null : NetClient.LoadItemIcon(item.IconLookupId));

    internal static Item ProjectItemForIndependentRuntime(WorldInventoryItemState item, Texture2D icon) => new Item
    {
        bagIdx = item.BagIndex, bound = item.Bound, cosmetic = item.Cosmetic, locked = item.Locked,
        id = item.ItemId, count = item.Count, sell = item.SellPrice, quality = item.Quality,
        blocked = item.BlockedValue,
        curDur = item.CurrentDurability, maxDur = item.MaximumDurability, avatar = item.Avatar,
        slot = item.EquipmentSlot,
        icon = icon,
        name = item.DisplayName, desc = item.Description, fusedDesc = item.FusedDescription,
        pet = ProjectPetForIndependentRuntime(item)
    };

    public static (string, string) ItemInfo(int id)
    {
        foreach (List<Item> bag in Items)
            foreach (Item item in bag)
                if (item.id == id && !item.pet.HasValue) return (item.name, item.desc);
        return ("", "");
    }

    public static int ItemCount(int id)
    {
        int count = 0;
        foreach (List<Item> bag in Items)
            foreach (Item item in bag)
                if (item.id == id && !item.pet.HasValue)
                    count = unchecked(count + item.count);
        return count;
    }
}

}
