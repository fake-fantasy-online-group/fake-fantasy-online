using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Strict reader for the official 1.5.8 inventory replacement and mutation surface.
/// </summary>
public static class PrivateServerInventoryCodec
{
    private const int MaximumVisibleBagIndex = 99;

    /// <summary>
    /// Writes the exact official ordinary UseItem payload: i32 item id, u8 tab, i32 bag slot.
    /// </summary>
    public static bool TryEncodeUse(
        int itemId,
        byte tab,
        int bagIndex,
        out byte[] payload)
    {
        payload = null;
        bool hotbarIdentity = tab == byte.MaxValue && bagIndex == -1;
        bool bagIdentity = tab <= 3 &&
            bagIndex >= 0 && bagIndex <= MaximumVisibleBagIndex;
        if (itemId <= 0 || (!hotbarIdentity && !bagIdentity))
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(itemId);
        writer.WriteByte(tab);
        writer.WriteInt32(bagIndex);
        payload = writer.ToArray();
        return true;
    }

    /// <summary>
    /// Writes the exact official Capture payload: i32 target entity id, i32 item id.
    /// A zero target is a valid source-client value for the five basic capture tools.
    /// </summary>
    public static bool TryEncodeCaptureUse(
        int targetEntityId,
        int itemId,
        out byte[] payload)
    {
        payload = null;
        if (targetEntityId < 0 || itemId <= 0)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(targetEntityId);
        writer.WriteInt32(itemId);
        payload = writer.ToArray();
        return true;
    }

    /// <summary>
    /// Writes the exact official UseItemOn payload: i32 tool id, u8 target kind, u8 tab, i32 slot.
    /// </summary>
    public static bool TryEncodeToolUse(
        int toolId,
        byte targetKind,
        byte tab,
        int slot,
        out byte[] payload)
    {
        payload = null;
        if ((toolId != 0x6311 && toolId != 0x3125) || targetKind > 1)
        {
            return false;
        }

        bool bagTarget = targetKind == 0;
        if ((bagTarget && (tab > 3 || slot < 0 || slot > MaximumVisibleBagIndex)) ||
            (!bagTarget && (tab != 0 || slot < 0 || slot > byte.MaxValue)))
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(toolId);
        writer.WriteByte(targetKind);
        writer.WriteByte(tab);
        writer.WriteInt32(slot);
        payload = writer.ToArray();
        return true;
    }

    /// <summary>
    /// Writes the exact official SplitItem payload: u8 tab, i32 bag slot, i32 split count.
    /// </summary>
    public static bool TryEncodeSplit(
        byte tab,
        int bagIndex,
        int count,
        out byte[] payload)
    {
        payload = null;
        // Native SplitItem (0600074b) forwards the full tab/slot/count input domain.
        WireWriter writer = new WireWriter();
        writer.WriteByte(tab);
        writer.WriteInt32(bagIndex);
        writer.WriteInt32(count);
        payload = writer.ToArray();
        return true;
    }

    /// <summary>
    /// Writes the exact official EquipItem payload: one zero-based bag-slot i32.
    /// </summary>
    public static bool TryEncodeEquip(int bagIndex, out byte[] payload)
    {
        payload = null;
        if (bagIndex < 0 || bagIndex > MaximumVisibleBagIndex)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(bagIndex);
        payload = writer.ToArray();
        return true;
    }

    /// <summary>
    /// Writes the exact official UnequipItem payload: one equipment-slot i32.
    /// </summary>
    public static bool TryEncodeUnequip(int slot, out byte[] payload)
    {
        payload = null;
        if (slot < 1 || slot > 11)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        writer.WriteInt32(slot);
        payload = writer.ToArray();
        return true;
    }

    /// <summary>
    /// Writes the exact official AllocPoints payload: seven positional i32 deltas.
    /// </summary>
    public static bool TryEncodeAttributePoints(
        IReadOnlyList<int> points,
        out byte[] payload)
    {
        payload = null;
        if (points == null || points.Count != 7)
        {
            return false;
        }

        WireWriter writer = new WireWriter();
        for (int index = 0; index < points.Count; index++)
        {
            if (points[index] < 0)
            {
                return false;
            }

            writer.WriteInt32(points[index]);
        }

        payload = writer.ToArray();
        return true;
    }

    /// <summary>
    /// Writes the exact official SortBag payload: one zero-based inventory tab byte.
    /// </summary>
    public static bool TryEncodeSort(byte tab, out byte[] payload)
    {
        payload = null;
        // Native SortBag (0600073f) writes the low byte of any Int32 tab.
        WireWriter writer = new WireWriter();
        writer.WriteByte(tab);
        payload = writer.ToArray();
        return true;
    }

    public static bool TryDecode(byte[] payload, out WorldInventoryState state)
    {
        state = null;
        try
        {
            state = DecodeSnapshot(payload, null);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            state = null;
            return false;
        }
    }

    internal static WorldInventoryState DecodeSnapshot(byte[] payload, IWorldReceiveOwner owner) =>
        DecodeSnapshot(payload, owner, out _);

    internal static WorldInventoryState DecodeSnapshot(
        byte[] payload, IWorldReceiveOwner owner, out IWorldInventorySnapshotRows rows)
    {
        rows = null;
        WireReader reader = new WireReader(payload);
        long money = reader.ReadInt64();
        long caiYu = reader.ReadInt64();
        long honor = reader.ReadInt64();
        int weight = reader.ReadInt32();
        int maximumWeight = reader.ReadInt32();

        int itemCount = reader.ReadUInt16();
        rows = (owner as IWorldStaticSnapshotReceiveOwner)?.BeginInventorySnapshotRows(itemCount);

        List<WorldInventoryItemState> items =
            new List<WorldInventoryItemState>(itemCount);
        for (int index = 0; index < itemCount; index++)
        {
            WorldInventoryItemState item = ReadItem(reader);
            rows?.AddItem(item);
            items.Add(item);
        }

        // Original 8006 stores the five header slots after the item rows and before
        // the equipment count. Later read/owner failures retain these reached stores.
        owner?.ReceiveInventoryHeader(money, caiYu, weight, maximumWeight, honor);

        int equippedCount = reader.ReadByte();
        rows?.BeginEquipped();
        List<WorldInventoryEquippedItemState> equipped =
            new List<WorldInventoryEquippedItemState>(equippedCount);
        for (int index = 0; index < equippedCount; index++)
        {
            WorldInventoryEquippedItemState item = ReadEquippedItem(reader);
            rows?.AddEquipped(item);
            equipped.Add(item);
        }

        ReadCompatibilityTails(reader, items, equipped, rows);
        return new WorldInventoryState(
            money,
            caiYu,
            honor,
            weight,
            maximumWeight,
            items,
            equipped);
    }

    /// <summary>
    /// Decodes the official 1.5.8 0x8069 header plus InventoryDlg/EquipDlg slot patches and
    /// applies them to the latest immutable 0x8006-rooted inventory snapshot.
    /// </summary>
    public static bool TryDecodeDelta(
        byte[] payload,
        WorldInventoryState current,
        out WorldInventoryState state)
    {
        state = null;
        if (current == null) return false;
        try
        {
            state = DecodeDelta(payload, current, null, out _);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            return false;
        }
    }

    // Actual receipt uses this throwing reader. Native header/row/patch failures must not
    // be converted into a decode failure after their already-reached owner writes.
    internal static WorldInventoryState DecodeDelta(
        byte[] payload,
        WorldInventoryState current,
        IWorldReceiveOwner receiver,
        out IWorldInventoryDeltaRows rows)
    {
        rows = null;
        WireReader reader = new WireReader(payload);
        long money = reader.ReadInt64();
        long caiYu = reader.ReadInt64();
        long honor = reader.ReadInt64();
        int weight = reader.ReadInt32();
        int maximumWeight = reader.ReadInt32();

        // 8069 stores the five header fields before reading its U16 item count.
        receiver?.ReceiveInventoryHeader(money, caiYu, weight, maximumWeight, honor);
        int itemPatchCount = reader.ReadUInt16();
        rows = (receiver as IWorldStaticSnapshotReceiveOwner)?.BeginInventoryDeltaRows(itemPatchCount);

        List<WorldInventoryItemState> itemPatches =
            new List<WorldInventoryItemState>(itemPatchCount);
        for (int index = 0; index < itemPatchCount; index++)
        {
            WorldInventoryItemState item = ReadItem(reader);
            rows?.AddItem(item);
            itemPatches.Add(item);
        }

        int equippedPatchCount = reader.ReadByte();
        rows?.BeginEquipped();
        List<WorldInventoryEquippedItemState> equippedPatches =
            new List<WorldInventoryEquippedItemState>(equippedPatchCount);
        for (int index = 0; index < equippedPatchCount; index++)
        {
            WorldInventoryEquippedItemState item = ReadEquippedItem(reader);
            rows?.AddEquipped(item);
            equippedPatches.Add(item);
        }

        var lockedFlags = new List<byte>();
        if (reader.Remaining >= 2)
        {
            int lockCount = reader.ReadUInt16();
            for (int index = 0; index < lockCount && reader.Remaining >= 1; index++)
                lockedFlags.Add(reader.ReadByte());
        }

        if (reader.Remaining >= 1)
        {
            int qualityCount = reader.ReadByte();
            for (int index = 0; index < qualityCount && reader.Remaining >= 1; index++)
            {
                byte quality = reader.ReadByte();
                if (index < equippedPatches.Count)
                {
                    equippedPatches[index] = equippedPatches[index].WithQuality(quality);
                    rows?.SetEquippedQuality(index, quality);
                }
            }
        }

        var fusedDescriptions = new Dictionary<int, string>();
        if (reader.Remaining >= 2)
        {
            int fusedCount = reader.ReadUInt16();
            for (int index = 0; index < fusedCount && reader.Remaining >= 4; index++)
                fusedDescriptions[reader.ReadUInt16()] = reader.ReadString();
        }

        var growthTiers = new Dictionary<int, int>();
        if (reader.Remaining >= 2)
        {
            int growthCount = reader.ReadUInt16();
            for (int index = 0; index < growthCount && reader.Remaining >= 3; index++)
                growthTiers[reader.ReadUInt16()] = reader.ReadByte();
        }
        for (int index = 0; index < itemPatches.Count; index++)
        {
            WorldInventoryItemState item = itemPatches[index];
            bool locked = index < lockedFlags.Count && lockedFlags[index] != 0;
            string description = fusedDescriptions.TryGetValue(index, out string fused)
                ? fused : item.FusedDescription;
            int tier = item.Pet != null && growthTiers.TryGetValue(index, out int growthTier)
                ? growthTier : item.PetGrowthTier;
            itemPatches[index] = item.WithCompatibilityPatch(
                locked, item.PetAiPointsUsed, item.PetAiPointsCapacity,
                item.PetShengPointsUsed, item.PetShengPointsCapacity, description, tier);
            if (index < lockedFlags.Count) rows?.SetLocked(index, locked);
            if (fusedDescriptions.ContainsKey(index)) rows?.SetFusedDescription(index, description);
            if (item.Pet != null && growthTiers.ContainsKey(index)) rows?.SetGrowthTier(index, tier);
            rows?.PatchItem(index, index < lockedFlags.Count);
        }

        // Original item patches precede every equipment patch; reached writes are retained
        // when a later native owner throws. Mirror conversion runs after those native calls.
        rows?.PatchEquipped();
        WorldInventoryState previous = current ?? WorldInventoryState.Empty;
        List<WorldInventoryItemState> items =
            new List<WorldInventoryItemState>(previous.Items);
        var originalPatches = new List<WorldInventoryItemPatch>(itemPatches.Count);
        for (int index = 0; index < itemPatches.Count; index++)
        {
            originalPatches.Add(new WorldInventoryItemPatch(itemPatches[index], index < lockedFlags.Count));
            ApplyItemPatch(items, itemPatches[index], index < lockedFlags.Count);
        }

        List<WorldInventoryEquippedItemState> equipped =
            new List<WorldInventoryEquippedItemState>(previous.EquippedItems);
        for (int index = 0; index < equippedPatches.Count; index++)
        {
            ApplyEquippedPatch(equipped, equippedPatches[index]);
        }

        return previous.WithDelta(
            money,
            caiYu,
            honor,
            weight,
            maximumWeight,
            items,
            equipped,
            originalPatches,
            equippedPatches);
    }

    private static void ApplyItemPatch(
        IList<WorldInventoryItemState> items,
        WorldInventoryItemState patch,
        bool lockAuthoritative)
    {
        int found = -1;
        for (int index = 0; index < items.Count; index++)
        {
            if (items[index].Tab == patch.Tab &&
                items[index].BagIndex == patch.BagIndex)
            {
                found = index;
                break;
            }
        }

        if (!lockAuthoritative && found >= 0 && items[found].ItemId == patch.ItemId)
            patch = patch.WithCompatibilityPatch(
                items[found].Locked, patch.PetAiPointsUsed, patch.PetAiPointsCapacity,
                patch.PetShengPointsUsed, patch.PetShengPointsCapacity,
                patch.FusedDescription);

        if (patch.ItemId < 1)
        {
            if (found >= 0)
            {
                items.RemoveAt(found);
            }
            return;
        }

        if (found < 0)
        {
            items.Add(patch);
        }
        else
        {
            items[found] = patch;
        }
    }

    private static void ApplyEquippedPatch(
        IList<WorldInventoryEquippedItemState> equipped,
        WorldInventoryEquippedItemState patch)
    {
        int found = -1;
        for (int index = 0; index < equipped.Count; index++)
        {
            if (equipped[index].Slot == patch.Slot)
            {
                found = index;
                break;
            }
        }

        if (patch.ItemId < 1)
        {
            if (found >= 0)
            {
                equipped.RemoveAt(found);
            }
            return;
        }

        if (found < 0)
        {
            equipped.Add(patch);
        }
        else
        {
            equipped[found] = patch;
        }
    }

    private static WorldInventoryItemState ReadItem(WireReader reader)
    {
        int bagIndex = reader.ReadInt32();
        int itemId = reader.ReadInt32();
        int count = reader.ReadInt32();
        byte tab = reader.ReadByte();
        string displayName = reader.ReadString();
        string description = reader.ReadString();
        int sellPrice = reader.ReadInt32();
        int currentDurability = reader.ReadInt32();
        int maximumDurability = reader.ReadInt32();
        int avatar = reader.ReadInt32();
        byte blockedValue = reader.ReadByte();
        bool blocked = blockedValue != 0;
        bool bound = reader.ReadByte() != 0;
        byte quality = reader.ReadByte();
        bool cosmetic = reader.ReadByte() != 0;
        int slotPlusOne = reader.ReadByte();
        // Official embedded-pet gate: a single U8 tested for nonzero, never a strict boolean.
        // arm64 0x8006 0x25ecd8 "bl Buf.U8" / 0x25ecdc "cbz w0, 0x25eda8", arm64 0x8069
        // 0x2559b4 / 0x2559b8 "cbz w0, 0x255a88"; x86_64 0x8069 0x264ed3 "callq Buf.U8" /
        // 0x264ed8 "testl %eax,%eax" / 0x264eda "je 0x26502a". Both arms branch to the
        // null-brief path only when the byte is zero and call NetClient.ReadPetBrief
        // (0x06000712) for every other value, so no byte > 1 is rejected here.
        bool hasPet = reader.ReadByte() != 0;
        WorldInventoryPetState pet = hasPet ? ReadPet(reader) : null;

        // Both inventory cases clamp the wire U8 tab before constructing the row.
        // 8069: x86_64 264deb/264df0 reads it; 26506f..265084 clamps to 0..3.
        if (tab > 3) tab = 3;

        return new WorldInventoryItemState(
            bagIndex,
            itemId,
            count,
            tab,
            displayName,
            description,
            sellPrice,
            currentDurability,
            maximumDurability,
            avatar,
            blocked,
            bound,
            quality,
            cosmetic,
            slotPlusOne == 0 ? -1 : slotPlusOne - 1,
            pet,
            false,
            0,
            0,
            0,
            0,
            string.Empty,
            blockedValue: blockedValue);
    }

    private static WorldInventoryPetState ReadPet(WireReader reader)
    {
        int model = reader.ReadInt32();
        int slot = reader.ReadInt32();
        string nickname = reader.ReadString();
        string prename = reader.ReadString();
        string species = reader.ReadString();
        byte gender = reader.ReadByte();
        byte habit = reader.ReadByte();
        byte opened = reader.ReadByte();
        int level = reader.ReadInt32();
        int strength = reader.ReadInt32();
        int intelligence = reader.ReadInt32();
        int vitality = reader.ReadInt32();
        int agility = reader.ReadInt32();
        int dexterity = reader.ReadInt32();
        int spirit = reader.ReadInt32();
        int starvation = reader.ReadInt32();
        int trust = reader.ReadInt32();
        int freePoints = reader.ReadInt32();
        int usedFreePoints = reader.ReadInt32();
        int skillCount = reader.ReadByte();
        List<WorldInventoryPetSkillState> skills =
            new List<WorldInventoryPetSkillState>(skillCount);
        for (int index = 0; index < skillCount; index++)
        {
            skills.Add(
                new WorldInventoryPetSkillState(
                    reader.ReadString(),
                    reader.ReadInt32()));
        }

        // Native ReadPetBrief only guards the trailing binding flag by Remaining.
        bool bound = reader.Remaining >= 1 && reader.ReadByte() != 0;
        return new WorldInventoryPetState(
            model,
            slot,
            nickname,
            prename,
            species,
            gender,
            habit,
            opened,
            level,
            strength,
            intelligence,
            vitality,
            agility,
            dexterity,
            spirit,
            starvation,
            trust,
            freePoints,
            usedFreePoints,
            skills,
            bound);
    }

    private static WorldInventoryEquippedItemState ReadEquippedItem(WireReader reader)
    {
        return new WorldInventoryEquippedItemState(
            reader.ReadByte(),
            reader.ReadInt32(),
            reader.ReadString(),
            reader.ReadString(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            reader.ReadInt32(),
            0);
    }

    private static void ReadCompatibilityTails(
        WireReader reader,
        IList<WorldInventoryItemState> items,
        IList<WorldInventoryEquippedItemState> equipped,
        IWorldInventorySnapshotRows rows)
    {
        if (reader.Remaining >= 2)
        {
            int lockedCount = reader.ReadUInt16();
            for (int index = 0; index < lockedCount && reader.Remaining >= 1; index++)
            {
                bool locked = reader.ReadByte() != 0;
                if (locked && index < items.Count)
                {
                    WorldInventoryItemState item = items[index];
                    items[index] = item.WithCompatibilityPatch(
                        locked,
                        item.PetAiPointsUsed,
                        item.PetAiPointsCapacity,
                        item.PetShengPointsUsed,
                        item.PetShengPointsCapacity,
                        item.FusedDescription);
                    rows?.LockItem(index);
                }
            }
        }

        if (reader.Remaining >= 2)
        {
            int petPpCount = reader.ReadUInt16();
            for (int index = 0; index < petPpCount && reader.Remaining >= 18; index++)
            {
                int itemOrdinal = reader.ReadUInt16();
                int aiUsed = reader.ReadInt32();
                int aiCapacity = reader.ReadInt32();
                int shengUsed = reader.ReadInt32();
                int shengCapacity = reader.ReadInt32();
                if (itemOrdinal < items.Count && items[itemOrdinal].Pet != null)
                {
                    WorldInventoryItemState item = items[itemOrdinal];
                    items[itemOrdinal] = item.WithCompatibilityPatch(
                        item.Locked,
                        aiUsed,
                        aiCapacity,
                        shengUsed,
                        shengCapacity,
                        item.FusedDescription);
                    rows?.SetPetPoints(itemOrdinal, aiUsed, aiCapacity, shengUsed, shengCapacity);
                }
            }
        }

        if (reader.Remaining >= 1)
        {
            int qualityCount = reader.ReadByte();
            for (int index = 0; index < qualityCount && reader.Remaining >= 1; index++)
            {
                byte quality = reader.ReadByte();
                if (index < equipped.Count)
                {
                    equipped[index] = equipped[index].WithQuality(quality);
                    rows?.SetEquippedQuality(index, quality);
                }
            }
        }

        if (reader.Remaining >= 2)
        {
            int fusedCount = reader.ReadUInt16();
            for (int index = 0; index < fusedCount && reader.Remaining >= 4; index++)
            {
                int itemOrdinal = reader.ReadUInt16();
                string fusedDescription = reader.ReadString();
                if (itemOrdinal < items.Count)
                {
                    WorldInventoryItemState item = items[itemOrdinal];
                    items[itemOrdinal] = item.WithCompatibilityPatch(
                        item.Locked,
                        item.PetAiPointsUsed,
                        item.PetAiPointsCapacity,
                        item.PetShengPointsUsed,
                        item.PetShengPointsCapacity,
                        fusedDescription);
                    rows?.SetFusedDescription(itemOrdinal, fusedDescription);
                }
            }
        }
        if (reader.Remaining >= 2)
        {
            int growthCount = reader.ReadUInt16();
            for (int index = 0; index < growthCount && reader.Remaining >= 3; index++)
            {
                int itemOrdinal = reader.ReadUInt16();
                int growthTier = reader.ReadByte();
                if (itemOrdinal < items.Count && items[itemOrdinal].Pet != null)
                {
                    WorldInventoryItemState item = items[itemOrdinal];
                    items[itemOrdinal] = item.WithCompatibilityPatch(
                        item.Locked, item.PetAiPointsUsed, item.PetAiPointsCapacity,
                        item.PetShengPointsUsed, item.PetShengPointsCapacity,
                        item.FusedDescription, growthTier);
                    rows?.SetGrowthTier(itemOrdinal, growthTier);
                }
            }
        }
    }

    private static bool IsDecodeFailure(Exception exception)
    {
        return exception is EndOfStreamException ||
               exception is InvalidDataException ||
               exception is ArgumentException ||
               exception is OverflowException;
    }
}

}
