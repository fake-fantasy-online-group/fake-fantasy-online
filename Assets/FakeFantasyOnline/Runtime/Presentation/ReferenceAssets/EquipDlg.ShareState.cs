using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    // Native equipment data; GossipDlg.BuildLocalShares borrows this owner.
    internal partial class EquipDlg : MonoBehaviour
    {
        public struct EquippedItem
        {
            public int slot;
            public int id;
            public string name;
            public string desc;
            public Texture2D icon;
            public int curDur;
            public int maxDur;
            public int avatar;
            public int soulAvatar;
            public int quality;
        }

        // Official 0x060002ff and 0x060002fd.
        public static IReadOnlyList<EquippedItem> Equipped => equipped;
        public static void SetEquipped(List<EquippedItem> rows)
        {
            equipped.Clear();
            equipped.AddRange(rows);
        }

        private static WorldInventoryState boundPublicationForIndependentRuntime;

        internal static void ObservePublicationForIndependentRuntime(WorldInventoryState state)
        {
            if (ReferenceEquals(boundPublicationForIndependentRuntime, state)) return;
            if (!state.IsEquipmentDeltaAfter(boundPublicationForIndependentRuntime))
            {
                BindEquippedForIndependentRuntime(state);
                return;
            }
            try
            {
                foreach (WorldInventoryEquippedItemState row in state.EquipmentPatches)
                    PatchSlot(row.Slot, ProjectEquippedForIndependentRuntime(row));
                boundPublicationForIndependentRuntime = state;
            }
            catch
            {
                boundPublicationForIndependentRuntime = null;
                throw;
            }
        }

        internal static void BindEquippedForIndependentRuntime(WorldInventoryState state)
        {
            // Receipt already installed this exact publication. Empty is a mirror sentinel.
            if (ReferenceEquals(state, WorldInventoryState.Empty) ||
                ReferenceEquals(boundPublicationForIndependentRuntime, state)) return;
            try
            {
                var projected = new List<EquippedItem>();
                foreach (WorldInventoryEquippedItemState row in state.EquippedItems)
                    projected.Add(ProjectEquippedForIndependentRuntime(row));
                SetEquipped(projected);
                boundPublicationForIndependentRuntime = state;
            }
            catch
            {
                boundPublicationForIndependentRuntime = null;
                throw;
            }
        }

        internal static void PublishReceivedForIndependentRuntime(
            WorldInventoryState state, List<EquippedItem> rows)
        {
            try
            {
                SetEquipped(rows);
                boundPublicationForIndependentRuntime = state;
            }
            catch
            {
                boundPublicationForIndependentRuntime = null;
                throw;
            }
        }

        private static EquippedItem ProjectEquippedForIndependentRuntime(WorldInventoryEquippedItemState row) =>
            ProjectEquippedForIndependentRuntime(row, NetClient.LoadItemIcon(row.IconLookupId));

        internal static void PublishReceivedDeltaForIndependentRuntime(WorldInventoryState state) =>
            boundPublicationForIndependentRuntime = state;

        internal static EquippedItem ProjectEquippedForIndependentRuntime(
            WorldInventoryEquippedItemState row, Texture2D icon) =>
            new EquippedItem { slot = row.Slot, id = row.ItemId,
                name = row.DisplayName, desc = row.Description,
                icon = icon,
                curDur = row.CurrentDurability, maxDur = row.MaximumDurability,
                avatar = row.Avatar, soulAvatar = row.SoulAvatar, quality = row.Quality };
    }
}
