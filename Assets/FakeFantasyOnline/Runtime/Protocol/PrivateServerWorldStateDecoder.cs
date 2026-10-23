using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Decodes authoritative in-world state packets that are independent from the entry snapshot.
/// </summary>
public static class PrivateServerWorldStateDecoder
{
    public static bool TryDecodeGroundEffect(byte[] payload, out WorldSkillHitEffect effect)
    {
        effect = null;
        if (payload == null) return false;
        try {
            var reader = new WireReader(payload);
            int owner = reader.ReadInt32(), map = reader.ReadInt32(), x = reader.ReadInt32(), y = reader.ReadInt32();
            string key = reader.ReadString(), name = reader.ReadString();
            int duration = reader.ReadInt32(); byte mode = reader.ReadByte();
            reader.EnsureFinished();
            effect = new WorldSkillHitEffect(owner, map, x, y, key, name, duration, mode);
            return true;
        } catch (Exception ex) when (ex is ArgumentException || ex is EndOfStreamException || ex is InvalidDataException || ex is OverflowException) { return false; }
    }

    /// <summary>
    /// Decodes the exact 0x8045 CombatWorld.PlayEntityEffect parameter packet.
    /// </summary>
    /// <remarks>
    /// The official arm reads the id and the name and hands both straight to
    /// CombatWorld.PlayEntityEffect with no filter of its own; that owner is where the official
    /// checks happen, in the order CombatWorld.I, IsNullOrEmpty, monster, remote player and the
    /// id 0 / self fallback. A zero id, a negative id and an empty name therefore decode here and
    /// are dropped by that owner instead. Surplus bytes still fail closed.
    /// </remarks>
    public static bool TryDecodeSkillHitEffect(
        byte[] payload,
        out WorldSkillHitEffect effect)
    {
        effect = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int entityId = reader.ReadInt32();
            string name = reader.ReadString();
            reader.EnsureFinished();

            effect = new WorldSkillHitEffect(entityId, name);
            return true;
        }
        catch (Exception exception) when (
            exception is ArgumentException ||
            exception is EndOfStreamException ||
            exception is InvalidDataException ||
            exception is OverflowException)
        {
            effect = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact 0x801b CombatWorld.ShowEntityStatus parameter packet.
    /// </summary>
    /// <remarks>
    /// The official handler reads one fixed root and leaves packet residue untouched. Empty names
    /// and negative durations still decode; MonsterEntity.ShowStatusFx decides their no-op or
    /// minimum-lifetime semantics after receive-time entity resolution.
    /// </remarks>
    public static bool TryDecodeEntityStatusEffect(
        byte[] payload,
        out int entityId,
        out WorldEntityStatusEffect effect)
    {
        entityId = 0;
        effect = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            entityId = reader.ReadInt32();
            effect = new WorldEntityStatusEffect(
                reader.ReadString(),
                reader.ReadByte(),
                reader.ReadInt32());
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            entityId = 0;
            effect = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact 0x8064 CombatWorld.SetEntityStatusIcons replacement.
    /// </summary>
    /// <remarks>
    /// The locked 1.3.4 consumer stops when fewer than thirteen bytes remain even when the
    /// declared count is larger, and ignores bytes left after the declared rows. Preserving that
    /// bounded partial-row behavior is part of compatibility; only a truncated five-byte root
    /// fails closed.
    /// </remarks>
    public static bool TryDecodeEntityStatusIcons(
        byte[] payload,
        out int entityId,
        out IReadOnlyList<WorldEntityStatusIcon> icons)
    {
        entityId = 0;
        icons = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            entityId = reader.ReadInt32();
            int count = reader.ReadByte();
            List<WorldEntityStatusIcon> rows =
                new List<WorldEntityStatusIcon>(Math.Min(count, reader.Remaining / 13));
            for (int index = 0; index < count && reader.Remaining > 12; index++)
            {
                rows.Add(
                    new WorldEntityStatusIcon(
                        reader.ReadInt32(),
                        reader.ReadInt32(),
                        reader.ReadByte(),
                        reader.ReadInt32()));
            }

            icons = rows.AsReadOnly();
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            entityId = 0;
            icons = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact 0x804b NpcChatter.SetServerChatter complete replacement.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x804b (jump-table entry 0x804b -&gt; arm64
    /// 0x25da70 / x86_64 0x26c433, frozen in WBD-011 B and re-used here): one NetClient.Buf is
    /// read with Buf.U8 for the enable flag, Buf.I32 three times for minimumGap, maximumGap and
    /// showSec, Buf.U16 for the line count and then count triples of Buf.Str - npcName, role,
    /// text, in that order, as one ValueTuple&lt;string,string,string&gt; per iteration with a
    /// 24-byte stride. The five roots are read before the loop and the loop is bounded only by
    /// the declared count (arm64 cmp w25,#1 / b.lt), so there is no Remaining gate anywhere in
    /// the case and no trailing-byte check: whatever the server appends after the last triple is
    /// never read and is therefore legal. A surplus tail is ignored here for the same reason and
    /// no EnsureFinished gate is applied - the earlier product gate rejected a packet the
    /// official client accepts.
    /// The whole list is complete before anything else happens: the case ends with a tail call
    /// to NpcChatter.SetServerChatter (0x0600090f, arm64 bl 0x281454 / x86_64 call 0x28ff40)
    /// carrying x0 = the list and w1/w2/w3/w4 = enabled/minGap/maxGap/showSec. A primitive that
    /// throws part-way through therefore leaves no server state behind: the official side effect
    /// happens exactly once per packet, after the last read, which is why this decoder returns
    /// false instead of publishing a partially filled list.
    /// Reading past the end raises IndexOutOfRangeException inside the Buf indexer, kept here as
    /// a decode failure in the same way as the other recovered Buf branches; the shared
    /// <see cref="IsDecodeFailure"/> list is not widened for this branch, so a failure that is
    /// not a primitive read error still propagates instead of being swallowed as a bad packet.
    /// A null payload is reported as a decode failure here instead of raising the native
    /// NullReferenceException: that is the pre-existing external contract of this helper, not an
    /// evidenced official rule.
    /// </remarks>
    public static bool TryDecodeNpcChatter(
        byte[] payload,
        out WorldNpcChatterState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            NetClient.Buf reader = new NetClient.Buf(payload);
            byte enabled = (byte)reader.U8();
            int minimumGapSeconds = reader.I32();
            int maximumGapSeconds = reader.I32();
            int showSeconds = reader.I32();
            int count = reader.U16();
            List<WorldNpcChatterLineState> lines =
                new List<WorldNpcChatterLineState>(count);
            for (int index = 0; index < count; index++)
            {
                string npcName = reader.Str();
                string role = reader.Str();
                string text = reader.Str();
                lines.Add(new WorldNpcChatterLineState(npcName, role, text));
            }

            state = new WorldNpcChatterState(
                enabled != 0,
                minimumGapSeconds,
                maximumGapSeconds,
                showSeconds,
                lines.AsReadOnly());
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact 0x8028 TaskClient.SetOffers replacement.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8028 (frozen jump table: arm64 0x1126fc4 with
    /// target base 0x24be18, x86_64 0x270104, index = opcode - 0x8001): one NetClient.Buf is read
    /// with Buf.Str for the NPC name, Buf.U16 for the row count and then count rows of Buf.I32 id,
    /// Buf.U8 state, Buf.Str title, Buf.Str desc and Buf.Str award, in that order and with no
    /// validation of any kind on the id - no positive test, no duplicate test, no sort. The case
    /// ends with a tail call to TaskClient.SetOffers (0x06000e5e, arm64 0x36ea70 / x86_64
    /// 0x3810e0, both confirmed as the same method token in the frozen atlas), which stores the
    /// NPC name, clears and refills the offer list and only then fires OnOffersChanged, so a
    /// primitive that throws part-way through leaves no server state behind: the official side
    /// effect happens exactly once per packet, after the last read. This decoder therefore still
    /// returns false instead of publishing a partially filled list.
    /// There is no EnsureFinished gate in the case and no trailing-byte check, so whatever the
    /// server appends after the last row is never read and is legal; the earlier product gate
    /// rejected a packet the official client accepts, and the duplicate-id gate dropped rows the
    /// official client shows.
    /// Reading past the end raises IndexOutOfRangeException inside the Buf indexer, kept here as a
    /// decode failure in the same way as the other recovered Buf branches. A null payload is
    /// reported as a decode failure instead of raising the native NullReferenceException: that is
    /// the pre-existing external contract of this helper, not an evidenced official rule.
    /// </remarks>
    public static bool TryDecodeNpcTaskOffers(
        byte[] payload,
        out WorldNpcTaskOffersState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            NetClient.Buf reader = new NetClient.Buf(payload);
            string npcName = reader.Str();
            int count = reader.U16();
            List<WorldNpcTaskOfferState> offers =
                new List<WorldNpcTaskOfferState>(count);
            for (int index = 0; index < count; index++)
            {
                offers.Add(
                    new WorldNpcTaskOfferState(
                        reader.I32(),
                        (byte)reader.U8(),
                        reader.Str(),
                        reader.Str(),
                        reader.Str()));
            }

            state = new WorldNpcTaskOffersState(npcName, offers.AsReadOnly());
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    public static bool TryDecodeShopState(byte[] payload, out WorldShopState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int shopId = reader.ReadInt32();
            int count = reader.ReadUInt16();
            int[] ids = new int[count];
            int[] prices = new int[count];
            string[] names = new string[count];
            string[] descriptions = new string[count];
            for (int index = 0; index < count; index++)
            {
                ids[index] = reader.ReadInt32();
                prices[index] = reader.ReadInt32();
                names[index] = reader.ReadString();
                descriptions[index] = reader.ReadString();
                if (ids[index] <= 0 || prices[index] <= 0)
                {
                    throw new InvalidDataException("The shop item row is invalid.");
                }
            }

            if (reader.ReadUInt16() != count)
            {
                throw new InvalidDataException("The shop blocked patch count is invalid.");
            }

            List<WorldShopItemState> items = new List<WorldShopItemState>(count);
            for (int index = 0; index < count; index++)
            {
                byte blocked = reader.ReadByte();
                if (blocked > 1)
                {
                    throw new InvalidDataException("The shop blocked flag is invalid.");
                }

                items.Add(new WorldShopItemState(
                    ids[index],
                    prices[index],
                    names[index],
                    descriptions[index],
                    blocked != 0));
            }

            reader.EnsureFinished();
            state = new WorldShopState(shopId, items.AsReadOnly());
            return shopId > 0;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            state = null;
            return false;
        }
    }

    public static bool TryDecodeTransportState(
        byte[] payload,
        out WorldTransportState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int transportList = reader.ReadInt32();
            int count = reader.ReadUInt16();
            List<WorldTransportDestinationState> destinations =
                new List<WorldTransportDestinationState>(count);
            for (int index = 0; index < count; index++)
            {
                destinations.Add(new WorldTransportDestinationState(
                    reader.ReadString(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadInt32()));
            }

            reader.EnsureFinished();
            state = new WorldTransportState(transportList, destinations.AsReadOnly());
            return transportList > 0;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            state = null;
            return false;
        }
    }

    public static bool TryDecodeWarehouseState(
        byte[] payload,
        out WorldWarehouseState state)
    {
        // Both official warehouse messages carry the same rows and optional quality tail.
        return TryDecodeWarehouseRows(payload, out state);
    }

    public static bool TryDecodeWarehouseDelta(byte[] payload, out WorldWarehouseState delta)
    {
        return TryDecodeWarehouseRows(payload, out delta);
    }

    private static bool TryDecodeWarehouseRows(byte[] payload, out WorldWarehouseState state)
    {
        state = null;
        if (payload == null) return false;
        try
        {
            NetClient.Buf reader = new NetClient.Buf(payload);
            byte pageCount = (byte)reader.U8();
            long money = reader.I64();
            int count = reader.U16();
            List<WorldWarehouseItemState> items = new List<WorldWarehouseItemState>(count);
            for (int index = 0; index < count; index++)
            {
                byte tab = (byte)reader.U8();
                int slot = reader.I32();
                int id = reader.I32();
                int quantity = reader.I32();
                string name = reader.Str();
                string description = reader.Str();
                int currentDurability = reader.I32();
                int maximumDurability = reader.I32();
                byte blocked = (byte)reader.U8();
                items.Add(new WorldWarehouseItemState(tab, slot, id, quantity, name,
                    description, currentDurability, maximumDurability, blocked, 0));
            }

            // Dispatch 0x8026/0x806a: the tail may be absent, shorter or longer than
            // the item list. Consume each available byte; only existing rows are patched.
            if (reader.Remaining >= 2)
            {
                int qualityCount = reader.U16();
                for (int index = 0; index < qualityCount && reader.Remaining > 0; index++)
                {
                    byte quality = (byte)reader.U8();
                    if (index >= items.Count) continue;
                    WorldWarehouseItemState item = items[index];
                    items[index] = new WorldWarehouseItemState(item.Tab, item.Slot, item.ItemId,
                        item.Count, item.Name, item.Description, item.CurrentDurability,
                        item.MaximumDurability, item.BlockedByte, quality);
                }
            }
            state = new WorldWarehouseState(pageCount, money, items);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact two-part 0x8027 TaskClient.SetState replacement.
    /// </summary>
    public static bool TryDecodeTaskLog(
        byte[] payload,
        out WorldTaskLogState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            NetClient.Buf reader = new NetClient.Buf(payload);
            int activeCount = reader.U16();
            List<WorldActiveTaskState> active =
                new List<WorldActiveTaskState>(activeCount);
            for (int index = 0; index < activeCount; index++)
            {
                int taskId = reader.I32();
                int remainingSeconds = reader.I32();
                int step = reader.I32();
                int total = reader.I32();
                string text = reader.Str();
                string progress = reader.Str();
                byte ready = (byte)reader.U8();
                string turnInNpc = reader.Str();
                active.Add(
                    new WorldActiveTaskState(
                        taskId,
                        remainingSeconds,
                        step,
                        total,
                        text,
                        progress,
                        ready != 0,
                        turnInNpc, ready));
            }

            int doneCount = reader.U16();
            List<int> done = new List<int>(doneCount);
            for (int index = 0; index < doneCount; index++)
            {
                int taskId = reader.I32();
                done.Add(taskId);
            }

            state = new WorldTaskLogState(active.AsReadOnly(), done.AsReadOnly());
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    /// <summary>
    /// Try-decode observation adapter for the exact 0x8008 Buf.Str plus Buf.U8 reads.
    /// Trailing bytes are ignored. Live receipt calls the recovered readers directly so their
    /// exceptions and the subsequent OnNotice exception propagate without this bool envelope.
    /// </summary>
    public static bool TryDecodeNpcDialogText(
        byte[] payload,
        out WorldNpcDialogTextState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            NetClient.Buf reader = new NetClient.Buf(payload);
            string text = reader.Str();
            byte displayMode = (byte)reader.U8();
            state = new WorldNpcDialogTextState(text, displayMode);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the server-owned system-request list. The U8 row count and every row field
    /// are mandatory; duplicate ids and unread trailing bytes retain native acceptance.
    /// </summary>
    public static bool TryDecodeSystemRequests(
        byte[] payload,
        out IReadOnlyList<WorldSystemRequestState> requests)
    {
        requests = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeSystemRequests(payload, out requests);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            requests = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeSystemRequests(
        byte[] payload,
        out IReadOnlyList<WorldSystemRequestState> requests)
    {
        var reader = new NetClient.Buf(payload);
        int count = (byte)reader.U8();
        List<WorldSystemRequestState> rows =
            new List<WorldSystemRequestState>(count);
        for (int index = 0; index < count; index++)
        {
            int requestId = reader.I32();
            rows.Add(
                new WorldSystemRequestState(
                    requestId,
                    (byte)reader.U8(),
                    reader.I32(),
                    reader.Str(),
                    reader.Str()));
        }

        requests = rows.AsReadOnly();

    }

    /// <summary>
    /// Decodes the exact 0x8050 inspected-player equipment snapshot consumed by ExEquipDlg.Show.
    /// The compatibility fields are consumed at their official widths, positional attributes are
    /// preserved, and the optional quality tail patches rows only while both item and bytes remain.
    /// </summary>
    public static bool TryDecodeInspectedPlayerEquipment(
        byte[] payload,
        out WorldInspectedPlayerEquipmentState state)
    {
        state = null;
        if (payload == null) return false;
        try
        {
            DecodeInspectedPlayerEquipment(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    internal static void DecodeInspectedPlayerEquipment(
        byte[] payload, out WorldInspectedPlayerEquipmentState state)
    {
        var reader = new NetClient.Buf(payload);
        int ignoredCompatibilityValue = reader.I32();
        string name = reader.Str();
        int level = reader.I32();
        int race = reader.U8();
        int gender = reader.U8();
        int head = reader.U8();
        int hair = reader.U8();
        string title = reader.Str();
        long honor = reader.I64();
        int body = reader.U16();
        int cap = reader.U16();
        int backpack = reader.U16();
        int rightWeapon = reader.U16();
        int leftWeapon = reader.U16();
        int face = reader.U16();
        reader.U8(); // atkVariant: consumed and discarded by the official handler.
        reader.U8(); // weaponCType: consumed and discarded by the official handler.
        reader.I16(); // legacyAtkDist16: consumed and discarded by the official handler.

        int attributeCount = reader.U16();
        List<int> attributes = new List<int>(attributeCount);
        for (int index = 0; index < attributeCount; index++)
        {
            attributes.Add(reader.I32());
        }

        int maximumAttack = reader.I32();
        int minimumAttack = reader.I32();
        int defense = reader.I32();
        int magicAttack = reader.I32();
        int magicDefense = reader.I32();
        int hit = reader.I32();
        int equipmentCount = reader.U8();
        List<InspectedEquipmentWireRow> rows =
            new List<InspectedEquipmentWireRow>(equipmentCount);
        for (int index = 0; index < equipmentCount; index++)
        {
            rows.Add(
                new InspectedEquipmentWireRow(
                    reader.U8(),
                    reader.I32(),
                    reader.Str(),
                    reader.Str(),
                    reader.I32(),
                    reader.I32(),
                    reader.I32(),
                    reader.I32()));
        }

        if (reader.Remaining >= 1)
        {
            int qualityCount = reader.U8();
            for (int index = 0;
                 index < qualityCount && index < rows.Count && reader.Remaining > 0;
                 index++)
            {
                rows[index].Quality = reader.U8();
            }
        }

        List<WorldInspectedEquipmentItem> equipment =
            new List<WorldInspectedEquipmentItem>(rows.Count);
        for (int index = 0; index < rows.Count; index++)
        {
            InspectedEquipmentWireRow row = rows[index];
            equipment.Add(
                new WorldInspectedEquipmentItem(
                    row.Slot,
                    row.ItemId,
                    row.Name,
                    row.Description,
                    row.CurrentDurability,
                    row.MaximumDurability,
                    row.Avatar,
                    row.SoulAvatar,
                    row.Quality));
        }

        state = new WorldInspectedPlayerEquipmentState(
            ignoredCompatibilityValue,
            name,
            level,
            race,
            gender,
            head,
            hair,
            title,
            honor,
            new CharacterAppearanceRecord(
                body,
                cap,
                backpack,
                rightWeapon,
                leftWeapon,
                face,
                0,
                0,
                0),
            attributes,
            maximumAttack,
            minimumAttack,
            defense,
            magicAttack,
            magicDefense,
            hit,
            equipment);
    }

    /// <summary>
    /// Decodes the 0x8056 identity and name without adding a trailing-byte restriction.
    /// </summary>
    public static bool TryDecodePlayerRename(byte[] payload, out WorldPlayerRenameState state)
    {
        state = null;
        if (payload == null) return false;
        try
        {
            WireReader reader = new WireReader(payload);
            int entityId = reader.ReadInt32();
            string name = reader.ReadString();
            // Original dispatch consumes these two fields without an end-of-packet assertion.
            state = new WorldPlayerRenameState(entityId, name);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            return false;
        }
    }

    /// <summary>Decodes the 0x800a appearance root and independently guarded tails.</summary>
    public static bool TryDecodePlayerAppearance(
        byte[] payload,
        out WorldPlayerAppearanceState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int entityId = reader.ReadInt32();
            CharacterAppearanceRecord appearance = new CharacterAppearanceRecord(
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadUInt16(),
                reader.ReadByte(),
                reader.ReadByte(),
                reader.ReadUInt16());
            string avatarAura = reader.ReadString();
            string soulEffects = reader.ReadString();
            string titleEffect = string.Empty;
            int? glowMode = null;
            int? glowUnlock = null;
            if (reader.Remaining >= 1)
            {
                titleEffect = reader.ReadString();
            }

            if (reader.Remaining >= 1)
            {
                glowMode = reader.ReadByte();
            }

            if (reader.Remaining >= 1)
            {
                glowUnlock = reader.ReadByte();
            }

            if (reader.Remaining >= 1)
                appearance = appearance.WithNativeAttackDistance(reader.ReadUInt16());
            reader.EnsureFinished();
            state = new WorldPlayerAppearanceState(
                entityId,
                appearance,
                avatarAura,
                soulEffects,
                titleEffect,
                glowMode,
                glowUnlock);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact 0x805e GameHud.TgSt list. The count is authoritative, the beneficial
    /// byte is an unvalidated wire boolean and there is no trailing-byte gate.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x805e (frozen jump tables: arm64 0x1126fc4 with
    /// target base 0x24be18, x86_64 0x270104 with the table base itself, index = opcode -
    /// 0x8001; handler arm64 0x25469c / x86_64 0x2639d5): Buf.I32 target id, Buf.U8 count, a
    /// list allocated with that count, then count rows of Buf.I32 icon, Buf.U8 beneficial,
    /// Buf.I32 remainMs, Buf.Str name and Buf.Str desc, ending in the class-init check and the
    /// GameHud.SetTargetStatus tail call (0x0600041c, arm64 0x43d128 / x86_64 0x454e10).
    /// beneficial is a wire boolean and not a validated 0/1 value: arm64 0x254768-0x25476c is
    /// <c>cmp w0,#0x0 / cset w8,ne</c> and x86_64 0x263ab5-0x263ab7 is <c>testl %eax,%eax /
    /// setne</c>, so every non-zero byte stores true and no byte is rejected - the earlier
    /// <c>&gt; 1</c> gate had no native counterpart. No Buf.get_Remaining exists in the case, so
    /// a surplus tail is never read and is legal, and the earlier <c>EnsureFinished</c> gate
    /// rejected a packet the official client accepts. A short packet still raises inside the Buf
    /// indexer before the single official write, so this decoder still returns false instead of
    /// publishing partially filled rows.
    /// </remarks>
    public static bool TryDecodeTargetStatusState(
        byte[] payload,
        out WorldTargetStatusState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeTargetStatusState(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeTargetStatusState(
        byte[] payload,
        out WorldTargetStatusState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int entityId = reader.I32();
        int count = reader.U8();
        List<WorldTargetStatusRow> rows = new List<WorldTargetStatusRow>(count);
        for (int index = 0; index < count; index++)
        {
            int iconId = reader.I32();
            bool beneficial = reader.U8() != 0;
            rows.Add(
                new WorldTargetStatusRow(
                    iconId,
                    beneficial,
                    reader.I32(),
                    reader.Str(),
                    reader.Str()));
        }

        state = new WorldTargetStatusState(entityId, rows);

    }

    /// <summary>
    /// Explicit compatibility read returning the complete, unfiltered Buff snapshot.
    /// A native receiver performs the original object/list writes first; only this requested
    /// return value triggers the subsequent pure DTO projection. Automatic receipt uses the
    /// native reader port directly and never calls this observer-returning method.
    /// </summary>
    /// <remarks>
    /// Supplied diagnostic clock/map capabilities retain their validation and call counts.
    /// Custom or absent receivers without the native reader port retain the previous adapter
    /// implementation below, including its separate all-row mirror. That legacy allocation
    /// boundary is not asserted to be the native owner. No exception is swallowed here.
    /// </remarks>
    public static WorldBuffState ReadBuffStateInto(
        byte[] payload,
        Func<float> clock,
        Func<bool> customMap,
        IWorldReceiveOwner receiver)
    {
        if (payload == null)
        {
            throw new ArgumentNullException(nameof(payload));
        }

        if (clock == null)
        {
            throw new ArgumentNullException(nameof(clock));
        }

        if (customMap == null)
        {
            throw new ArgumentNullException(nameof(customMap));
        }

        if (receiver is IWorldBuffReceiveOwner native)
        {
            native.ReceiveBuffState(new NetClient.Buf(payload), clock, customMap);
            // Returning a DTO was explicitly requested by this compatibility caller.
            return DecodeBuffState(payload);
        }

        NetClient.Buf reader = new NetClient.Buf(payload);
        int count = reader.U16();
        // Official arm64 0x251334-0x251340 / x86_64 0x25ff7d-0x25ff8e: the case evaluates
        // GameBootstrap.CurMapId (field token 67110216, System.Int32, GameBootstrap statics
        // +0xb4, TypeInfo:GameBootstrap from metadata usage ordinal 1706) once per packet -
        // after the count read and before the row loop, so also for an empty packet - and
        // passes it to CustomMaps.IsCustom (0x060001d0, arm64 0x3590f8 / x86_64 0x36b1b0).
        // Both types belong to Presentation, so this layer only invokes the injected probe;
        // the call happens here, inside the single host action that owns the read, and the
        // answer is a local of this call and never a field that outlives it.
        bool isCustomMap = customMap();
        bool invisible = false;
        byte immobile = 0;
        List<WorldBuffRow> rows = new List<WorldBuffRow>(count);
        receiver?.BeginBuffRows(count);
        for (int index = 0; index < count; index++)
        {
            int skillId = reader.I32();
            int iconId = reader.I32();
            int remainingSeconds = reader.I32();
            string displayName = reader.Str();
            string description = reader.Str();
            bool beneficial = reader.U8() != 0;
            int positionMode = unchecked((sbyte)(byte)reader.U8());
            // Official arm64 0x251450 / x86_64 0x260074: the row stamp is taken here, after the
            // posMode read and before the invisible read, once per row.
            float receiveTime = clock();
            // arm64 0x25145c-0x251478 / x86_64 0x26008f-0x2600b0: the invisible byte is OR-ed
            // and the immobile byte is taken as the unsigned maximum here, before the drop test.
            bool rowInvisible = reader.U8() != 0;
            byte rowImmobile = (byte)reader.U8();
            invisible |= rowInvisible;
            if (rowImmobile > immobile)
            {
                immobile = rowImmobile;
            }

            // The mirror keeps every row the read walked, before and independently of the drop
            // test below - see the mirror/CharData asymmetry in the method remarks.
            rows.Add(
                new WorldBuffRow(
                    skillId,
                    iconId,
                    remainingSeconds,
                    displayName,
                    description,
                    beneficial,
                    positionMode,
                    rowInvisible,
                    rowImmobile));

            // arm64 0x25147c-0x251498 / x86_64 0x2600b3-0x2600cc: the unsigned test drops the
            // row from the official list but skips the List.Add only - the row object was already
            // built and its aggregates were already folded in above. The packet-level boolean is
            // the answer the injected host probe returned once above, reused for every row.
            if (isCustomMap || unchecked((uint)(skillId - 90000)) >= 10000u)
            {
                receiver?.AddBuffRow(
                    skillId,
                    iconId,
                    remainingSeconds,
                    displayName,
                    description,
                    beneficial,
                    positionMode,
                    receiveTime);
            }
        }

        receiver?.CompleteBuffRows(invisible, immobile);
        // Official arm64 0x2588a8 / x86_64 0x2653de: one byte is read when at least one remains
        // and every later byte is left unread.
        bool doubleExperience = false;
        if (reader.Remaining >= 1)
        {
            doubleExperience = reader.U8() != 0;
        }

        receiver?.CompleteBuffTail(doubleExperience);
        // Same identity rule the standalone parser used, kept so an empty no-tail reply stays the
        // shared WorldBuffState.Empty instance rather than a freshly allocated equal one.
        return rows.Count == 0 && !doubleExperience
            ? WorldBuffState.Empty
            : new WorldBuffState(rows, doubleExperience);
    }

    // Pure all-row observer reused by explicit snapshot access and the Editor parser.
    // It deliberately retains rows the native custom-map filter may have dropped and every
    // original invisible/immobile byte. It never samples time/map or publishes native state.
    internal static WorldBuffState DecodeBuffState(byte[] payload)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int count = reader.U16();
        List<WorldBuffRow> rows = new List<WorldBuffRow>(count);
        for (int index = 0; index < count; index++)
        {
            int skillId = reader.I32();
            int iconId = reader.I32();
            int remainingSeconds = reader.I32();
            string displayName = reader.Str();
            string description = reader.Str();
            bool beneficial = reader.U8() != 0;
            int positionMode = unchecked((sbyte)(byte)reader.U8());
            bool invisible = reader.U8() != 0;
            byte immobile = (byte)reader.U8();
            rows.Add(
                new WorldBuffRow(
                    skillId,
                    iconId,
                    remainingSeconds,
                    displayName,
                    description,
                    beneficial,
                    positionMode,
                    invisible,
                    immobile));
        }

        // Official arm64: Buf.get_Remaining() at 0x2588a8, cmp w0, #0x1 / b.lt, then one
        // Buf.U8 read; x86_64: Buf.get_Remaining at 0x2653de, testl / jle, then one Buf.U8
        // read. One byte is read when at least one remains and every later byte is left
        // unread, so a longer tail is accepted instead of failing the packet.
        bool doubleExperience = false;
        if (reader.Remaining >= 1)
        {
            doubleExperience = reader.U8() != 0;
        }

        return rows.Count == 0 && !doubleExperience
            ? WorldBuffState.Empty
            : new WorldBuffState(rows, doubleExperience);
    }

    public static bool TryDecodeBuffState(
        byte[] payload,
        out WorldBuffState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            state = DecodeBuffState(payload);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                         exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    /// <summary>
    /// Decodes the two independently sized I32 arrays consumed by the locked 1.3.4 0x8007
    /// handler. No positional values are padded, renamed or reordered in Protocol.
    /// </summary>
    public static bool TryDecodePlayerAttributeSnapshot(
        byte[] payload,
        out WorldPlayerAttributeSnapshot snapshot)
    {
        snapshot = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            IReadOnlyList<int> current = ReadPlayerAttributeArray(reader);
            IReadOnlyList<int> total = ReadPlayerAttributeArray(reader);
            reader.EnsureFinished();
            snapshot = new WorldPlayerAttributeSnapshot(current, total);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            snapshot = null;
            return false;
        }
    }

    /// <summary>
    /// Consumes the complete 0x800b pet snapshot and publishes the current-pet fields owned by the
    /// recovered first-world HUD plus the optional bound bit consumed by InventoryDlg. Nested
    /// skills, pet slots and known 1.5.8 tails follow the original independent Remaining guards.
    /// Bytes beyond the final known extension are left unread, as in the native handler.
    /// </summary>
    public static bool TryDecodePlayerPetStatus(
        byte[] payload,
        out WorldPlayerPetStatusState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int model = reader.ReadInt32();
            string fallbackName = reader.ReadString();
            string prename = reader.ReadString();
            string preferredName = reader.ReadString();
            byte gender = reader.ReadByte();
            byte habit = reader.ReadByte();
            int level = reader.ReadInt32();
            int experience = reader.ReadInt32();
            int experienceToNext = reader.ReadInt32();
            int starve = reader.ReadInt32();
            int trust = reader.ReadInt32();
            int freePoints = reader.ReadInt32();
            int hitPoints = reader.ReadInt32();
            int maximumHitPoints = reader.ReadInt32();
            int magicPoints = reader.ReadInt32();
            int maximumMagicPoints = reader.ReadInt32();
            int strength = reader.ReadInt32();
            int vitality = reader.ReadInt32();
            int agility = reader.ReadInt32();
            int intelligence = reader.ReadInt32();
            int dexterity = reader.ReadInt32();
            int spirit = reader.ReadInt32();
            int minimumAttack = reader.ReadInt32();
            int maximumAttack = reader.ReadInt32();
            int defense = reader.ReadInt32();
            int magicAttack = reader.ReadInt32();
            int magicDefense = reader.ReadInt32();
            int hit = reader.ReadInt32();
            int activeSkillId = reader.ReadInt32();
            reader.ReadByte(); // ignoredCompatByte
            int activeSlot = reader.ReadByte();

            int currentSkillCount = reader.ReadByte();
            List<WorldPlayerPetSkillState> currentSkills =
                new List<WorldPlayerPetSkillState>(currentSkillCount);
            for (int index = 0; index < currentSkillCount; index++)
            {
                currentSkills.Add(new WorldPlayerPetSkillState(
                    reader.ReadInt32(),
                    reader.ReadString(),
                    reader.ReadInt32(),
                    reader.ReadString(),
                    reader.ReadByte() != 0,
                    reader.ReadByte() != 0,
                    reader.ReadInt32(),
                    reader.ReadInt32()));
            }

            int petSlotCount = reader.ReadByte();
            List<WorldPlayerPetSlotState> petSlots =
                new List<WorldPlayerPetSlotState>(petSlotCount);
            for (int index = 0; index < petSlotCount; index++)
            {
                petSlots.Add(ReadPetBrief(reader));
            }

            bool petShow = ReadWireBoolean(reader, "pet show") != 0;
            int arousal = reader.ReadInt32();
            int ppAi = reader.ReadInt32();
            int ppSheng = reader.ReadInt32();
            bool sanctified = ReadWireBoolean(reader, "pet sanctified") != 0;
            bool petRidable = ReadWireBoolean(reader, "pet ridable") != 0;
            bool petTianmo = ReadWireBoolean(reader, "pet tianmo") != 0;
            bool petShowTianmo = ReadWireBoolean(reader, "pet show tianmo") != 0;

            // These three compatibility tails are independently guarded by Remaining in the
            // official handler. Counts are likewise consumed only while a complete item remains.
            bool petBound = false;
            if (reader.Remaining >= 1)
            {
                petBound = ReadWireBoolean(reader, "current-pet bound") != 0;
            }

            if (reader.Remaining >= 1)
            {
                int lockedFlagCount = reader.ReadByte();
                for (int index = 0; index < lockedFlagCount && reader.Remaining >= 1; index++)
                {
                    bool locked = ReadWireBoolean(reader, "pet-slot locked") != 0;
                    if (index < petSlots.Count)
                    {
                        WorldPlayerPetSlotState row = petSlots[index];
                        petSlots[index] = row.WithCompatibilityPatch(
                            locked,
                            row.PpAiUsed,
                            row.PpAiCapacity,
                            row.PpShengUsed,
                            row.PpShengCapacity);
                    }
                }
            }

            if (reader.Remaining >= 1)
            {
                int ppCount = reader.ReadByte();
                for (int index = 0; index < ppCount && reader.Remaining >= 16; index++)
                {
                    int ppAiUsed = reader.ReadInt32();
                    int ppAiCapacity = reader.ReadInt32();
                    int ppShengUsed = reader.ReadInt32();
                    int ppShengCapacity = reader.ReadInt32();
                    if (index < petSlots.Count)
                    {
                        WorldPlayerPetSlotState row = petSlots[index];
                        petSlots[index] = row.WithCompatibilityPatch(
                            row.Locked,
                            ppAiUsed,
                            ppAiCapacity,
                            ppShengUsed,
                            ppShengCapacity);
                    }
                }
            }

            int? pickQuality = reader.Remaining >= 1 ? (int?)reader.ReadByte() : null;
            int? fightCapacity = null;
            int? lifeCapacity = null;
            if (reader.Remaining >= 2)
            {
                fightCapacity = reader.ReadByte();
                lifeCapacity = reader.ReadByte();
            }
            int? growthTier = reader.Remaining >= 1 ? (int?)reader.ReadByte() : null;
            if (reader.Remaining >= 1)
            {
                int growthCount = reader.ReadByte();
                for (int index = 0; index < growthCount && reader.Remaining > 0; index++)
                {
                    int tier = reader.ReadByte();
                    if (index < petSlots.Count) petSlots[index] = petSlots[index].WithGrowthTier(tier);
                }
            }

            // The native 1.5.8 handler ends its reads here; any later extension is left unread.
            state = new WorldPlayerPetStatusState(
                model,
                string.IsNullOrEmpty(preferredName) ? fallbackName : preferredName,
                level,
                hitPoints,
                maximumHitPoints,
                magicPoints,
                maximumMagicPoints,
                petBound,
                petShow,
                true,
                petRidable,
                petTianmo,
                petShowTianmo,
                currentSkills,
                new WorldPlayerPetDetailState(
                    prename,
                    gender,
                    habit,
                    experience,
                    experienceToNext,
                    starve,
                    trust,
                    freePoints,
                    strength,
                    vitality,
                    agility,
                    intelligence,
                    dexterity,
                    spirit,
                    minimumAttack,
                    maximumAttack,
                    defense,
                    magicAttack,
                    magicDefense,
                    hit,
                    activeSkillId,
                    activeSlot,
                    arousal,
                    ppAi,
                    ppSheng,
                    sanctified),
                petSlots, pickQuality, fightCapacity, lifeCapacity, growthTier);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            state = null;
            return false;
        }
    }

    // Dispatch 0600077f, 0x806f: each I32 is stored before the following I32 is read.
    // Frozen cases: arm64 0x24d4e0-0x24d5f4 / x86_64 0x25aff2-0x25b119.
    // The callback owns the actual roots; a failed primitive never rolls back earlier stores.
    public static bool TryDecodePetVitals(byte[] payload, Action<WorldPetVital, int> store)
    {
        if (store == null) throw new ArgumentNullException(nameof(store));
        if (payload == null) return false;
        try
        {
            DecodePetVitals(payload, store);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodePetVitals(byte[] payload, Action<WorldPetVital, int> store)
    {
        var reader = new NetClient.Buf(payload);
        store(WorldPetVital.Level, reader.I32());
        store(WorldPetVital.Experience, reader.I32());
        store(WorldPetVital.ExperienceToNext, reader.I32());
        store(WorldPetVital.HitPoints, reader.I32());
        store(WorldPetVital.MaximumHitPoints, reader.I32());
        store(WorldPetVital.MagicPoints, reader.I32());
        store(WorldPetVital.MaximumMagicPoints, reader.I32());
        store(WorldPetVital.Starve, reader.I32());
        store(WorldPetVital.Trust, reader.I32());
        store(WorldPetVital.FreePoints, reader.I32());

    }

    // Data-only callers retain the reached prefix as well, including without a previous pet.
    // This overload is not a second receive owner and is not used by the dispatch path.
    public static bool TryDecodePetVitals(
        byte[] payload,
        WorldPlayerPetStatusState previous,
        out WorldPlayerPetStatusState state)
    {
        WorldPlayerPetStatusState next = previous;
        var reached = new WorldPetVitalsState();
        bool complete = TryDecodePetVitals(payload, (field, value) =>
        {
            reached = reached.WithValue(field, value);
            if (next == null)
                next = new WorldPlayerPetStatusState(0, string.Empty, 0, 0, 0, 0, 0, false);
            next = next.WithPetVitals(reached);
        });
        state = next;
        return complete;
    }

    /// <summary>
    /// Decodes the fixed 0x8046 NPC spawn root recovered from the frozen 1.5.8 Dispatch case.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8046 (jump-table entry 0x8046 -&gt; arm64
    /// 0x252ca0 / 428 bytes, x86_64 0x261e3e with its tail jmp at 0x261ffb): one NetClient.Buf
    /// (arm64 ldr x19,[sp,#0x110] / x86_64 r13) is read with Buf.I32 (arm64 0x26b00c / x86_64
    /// 0x27a660) three times, then Buf.Str (arm64 0x26b088 / x86_64 0x27a6e0) five times, then
    /// Buf.I32 three times again, in exactly this order:
    ///   entity id  arm64 0x252cb0 -&gt; [sp,#0x100]   / x86_64 0x261e4c -&gt; [rbp-0x6f0]
    ///   x          arm64 0x252cc0 -&gt; [sp,#0x108]   / x86_64 0x261e5c -&gt; [rbp-0x680]
    ///   y          arm64 0x252cd0 -&gt; x22           / x86_64 0x261e6c -&gt; [rbp-0x690]
    ///   sprite     arm64 0x252ce0 -&gt; x25           / x86_64 0x261e7c -&gt; rbx
    ///   name       arm64 0x252cf0 -&gt; x21           / x86_64 0x261e89 -&gt; r13
    ///   label      arm64 0x252d00 -&gt; x26           / x86_64 0x261e99 -&gt; r15
    ///   portrait   arm64 0x252d10 -&gt; x24           / x86_64 0x261ea6 -&gt; r14
    ///   script     arm64 0x252d20 -&gt; x23           / x86_64 0x261eb3 -&gt; [rbp-0x6a0]
    ///   sell       arm64 0x252d30 -&gt; x27           / x86_64 0x261ec4 -&gt; [rbp-0x6c0]
    ///   trans      arm64 0x252d40 -&gt; x28           / x86_64 0x261ed4 -&gt; [rbp-0x6a8]
    ///   dir        arm64 0x252d50 -&gt; x20           / x86_64 0x261ee4 -&gt; [rbp-0x6e0]
    /// The NpcEntry is allocated after the reads (arm64 0x252d64 bl 0x177a60 / x86_64 0x261ef9
    /// call 0x16c180) and its .ctor (arm64 0x284c0c / x86_64 0x293600) stores only
    /// dir = 3 (arm64 mov w8,#3; str w8,[x0,#0x50] / x86_64 mov dword [rdi+0x50],3) before the
    /// eleven values overwrite it, so no field keeps a constructor default. The writes land on
    /// NpcEntry +0x18 sprite, +0x10 name, +0x30 label, +0x38 portrait, +0x40 script, +0x48 sell,
    /// +0x4c trans, +0x50 dir (arm64 0x252d8c / 0x252d7c / 0x252dac / 0x252dbc / 0x252dcc /
    /// 0x252ddc / 0x252de0; x86_64 0x261f2b / 0x261f19 / 0x261f5b / 0x261f6d / 0x261f88 /
    /// 0x261f98 / 0x261fa3 / 0x261fae), which fixes the string order as
    /// sprite, name, label, portrait, script.
    /// x and y are read as signed 32-bit integers and only then widened into the two float slots
    /// NpcEntry +0x20 / +0x24 (arm64 scvtf s0,[sp,#0x108] / scvtf s1,x22 then stp s0,s1,[x19,#0x20];
    /// x86_64 cvtsi2ss xmm0,[rbp-0x680] / cvtsi2ss xmm1,[rbp-0x690] then movss [r12+0x20]/[r12+0x24]).
    /// There is no Buf.get_Remaining gate, no entity-id or sprite whitelist, no clamp and no
    /// trailing-byte rejection, so a surplus tail stays unread. The branch tail-calls
    /// GameBootstrap.OnNetNpcSpawn(int, NpcEntry) (arm64 b 0x423a68 / x86_64 jmp 0x438d50) with
    /// the first I32 as the id.
    /// A null payload is the dispatcher's NullReferenceException path (arm64 cbz x19 -&gt; 0x25c960
    /// / x86_64 test r13,r13 -&gt; je 0x26b60d); it is reported as a decode failure here instead of
    /// raising, which is the pre-existing external contract of this helper and not an evidenced
    /// official rule. Reading past the end raises IndexOutOfRangeException inside the Buf indexer,
    /// kept here as a decode failure in the same way as the other recovered Buf branches.
    /// </remarks>
    public static bool TryDecodeNpcState(
        byte[] payload,
        out WorldNpcState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeNpcState(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeNpcState(
        byte[] payload,
        out WorldNpcState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int entityId = reader.I32();
        int x = reader.I32();
        int y = reader.I32();
        string sprite = reader.Str();
        string displayName = reader.Str();
        string label = reader.Str();
        string portrait = reader.Str();
        string script = reader.Str();
        int sellList = reader.I32();
        int transportList = reader.I32();
        int direction = reader.I32();
        state = new WorldNpcState(
            entityId,
            x,
            y,
            sprite,
            displayName,
            label,
            portrait,
            script,
            sellList,
            transportList,
            direction);

    }

    /// <summary>
    /// Decodes the fixed 0x8047 NPC removal root: one signed 32-bit entity id.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8047 (jump-table entry 0x8047 -&gt; arm64
    /// 0x252e4c / 120 bytes, x86_64 0x262000 with its tail jmp at 0x262056): the only read is
    /// Buf.I32 (arm64 0x26b00c at 0x252e58 / x86_64 0x27a660 at 0x26200e) and its result is
    /// passed straight to GameBootstrap.OnNetNpcRemove(int) (arm64 b 0x423c24 / x86_64 jmp
    /// 0x438ee0); x1 = 0 on both architectures is the hidden MethodInfo slot and not a payload
    /// field. The only test in the branch is the Buf null test (arm64 cbz x0 -&gt; 0x25c960 /
    /// x86_64 test r13,r13 -&gt; je 0x26b60d); there is no nonzero-id gate, no id whitelist and no
    /// trailing-byte check, so an id of zero is still forwarded and a surplus tail stays unread.
    /// A null payload is reported as a decode failure here instead of raising the native
    /// NullReferenceException: that is the pre-existing external contract of this helper, not an
    /// evidenced official rule. Reading past the end raises IndexOutOfRangeException inside the
    /// Buf indexer, kept here as a decode failure in the same way as the other recovered Buf
    /// branches.
    /// </remarks>
    public static bool TryDecodeNpcRemoval(byte[] payload, out int entityId)
    {
        entityId = 0;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeNpcRemoval(payload, out entityId);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            entityId = 0;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeNpcRemoval(byte[] payload, out int entityId)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        entityId = reader.I32();

    }

    /// <summary>
    /// Decodes the exact 0x800f CombatWorld spawn root and its independently optional tails.
    /// </summary>
    public static bool TryDecodeWorldEntityState(byte[] payload, out WorldEntityState state)
    {
        state = null;
        if (payload == null) return false;
        try
        {
            state = DecodeWorldEntityState(payload, null);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            return false;
        }
    }

    // Null means the original main-thread switching/map gate rejected this receive.
    // Rejection happens before optional status decoding, not after a complete snapshot exists.
    internal static WorldEntityState DecodeWorldEntityState(byte[] payload, IWorldReceiveContext context)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int entityId = reader.I32();
        WorldEntityKind kind = (WorldEntityKind)reader.U8();
        int modelOrSkillId = reader.I32();
        string displayName = reader.Str();
        int level = reader.I32();
        int x = reader.I32();
        int y = reader.I32();
        int hitPointsOrRadiusPixels = reader.I32();
        int maximumHitPoints = reader.I32();
        int ownerEntityId = reader.I32();
        byte eliteFlags = (byte)reader.U8();

        if (context != null && context.Switching) return null;

        int mapIdGuard = 0;
        byte friendlyFlag = 0;

        // These guards intentionally mirror the locked consumer. In particular, a one- to
        // three-byte legacy tail omits mapId and begins directly with friendly/status data.
        if (reader.Remaining >= 4)
        {
            mapIdGuard = reader.I32();
            if (context != null && mapIdGuard > 0 && context.CurrentMapId > 0 &&
                mapIdGuard != context.CurrentMapId) return null;
        }

        if (reader.Remaining >= 1)
        {
            friendlyFlag = (byte)reader.U8();
        }

        List<WorldEntityStatusEffect> statusEffects = new List<WorldEntityStatusEffect>();
        List<WorldEntityStatusIcon> statusIcons = null;
        if (reader.Remaining >= 1)
        {
            int effectCount = reader.U8();
            for (int index = 0; index < effectCount && reader.Remaining > 0; index++)
            {
                statusEffects.Add(
                    new WorldEntityStatusEffect(
                        reader.Str(),
                        (byte)reader.U8(),
                        reader.I32()));
            }
            // The native tuple array keeps declared slots that were never entered.
            while (statusEffects.Count < effectCount)
                statusEffects.Add(new WorldEntityStatusEffect(null, 0, 0));
        }

        if (reader.Remaining >= 1)
        {
            int iconCount = reader.U8();
            if (iconCount > 0) statusIcons = new List<WorldEntityStatusIcon>(iconCount);
            for (int index = 0; index < iconCount && reader.Remaining >= 13; index++)
            {
                statusIcons.Add(
                    new WorldEntityStatusIcon(
                        reader.I32(),
                        reader.I32(),
                        (byte)(reader.U8() != 0 ? 1 : 0),
                        reader.I32()));
            }
        }

        return new WorldEntityState(
            entityId,
            kind,
            modelOrSkillId,
            displayName,
            level,
            x,
            y,
            hitPointsOrRadiusPixels,
            maximumHitPoints,
            ownerEntityId,
            eliteFlags,
            mapIdGuard,
            friendlyFlag,
            statusEffects,
            statusIcons);
    }

    /// <summary>
    /// Decodes the fixed 0x8010 CombatWorld removal root: entity id followed by reason byte.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8010 (arm64 block 0x252684-0x252710, x86_64
    /// block 0x26182a-0x261891; jump-table entry 0x8010 -> arm64 0x252684 / x86_64 0x26182a):
    /// Buf.I32 (arm64 0x26b00c / x86_64 0x27a660) yields the id and Buf.U8 (arm64 0x26afcc /
    /// x86_64 0x27a630) yields the reason, which the body narrows with `and w1, w20, #0xff`
    /// (arm64 0x2526dc) / `movzx esi, r14b` (x86_64 0x261878) before the tail call to
    /// CombatWorld.Remove (0x06000144, arm64 0x241888 / x86_64 0x24f0e0), whose managed signature
    /// is `static void Remove(int id, byte reason = 0)`. The only test in the branch is the Buf
    /// null test (`cbz x20, #0x25c960` / `test r13,r13` -> `je 0x26b60d`); there is no nonzero-id
    /// gate, no reason range gate and no trailing-byte check, so a surplus tail stays unread.
    /// A null payload is reported as a decode failure here instead of raising the native
    /// NullReferenceException: that is the pre-existing external contract of this helper, not an
    /// evidenced official rule.
    /// </remarks>
    public static bool TryDecodeWorldEntityRemoval(
        byte[] payload,
        out int entityId,
        out byte reason)
    {
        entityId = 0;
        reason = 0;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeWorldEntityRemoval(payload, out entityId, out reason);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            entityId = 0;
            reason = 0;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeWorldEntityRemoval(
        byte[] payload,
        out int entityId,
        out byte reason)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        entityId = reader.I32();
        reason = (byte)reader.U8();

    }

    /// <summary>
    /// Decodes the fixed 0x8043 CombatWorld owner root: entity id followed by owner id.
    /// </summary>
    /// <remarks>
    /// Official 0x8043 -> CombatWorld.SetOwner (0x06000132). Two Buf.I32 in this order: entity id
    /// then owner id (arm64 bl 0x26b00c at 0x24ca5c / 0x24ca6c, tail b 0x23efd0 at 0x24cad8;
    /// x86_64 callq 0x27a660 at 0x25a48a / 0x25a496, tail jmp 0x24cdb0 at 0x25a4e2). Evidence:
    /// <c>Build/ReferenceReverseGhidra-1.5.8/queries/WBD-122-06000132-{arm64,x86_64}/</c>,
    /// <c>docs/contracts/VERSION_UPGRADE_1_5_8_DISPATCH_SWITCH_ARM64_V1.json</c> (case 0x24ca4c,
    /// bufReads I32/I32) and <c>PROTOCOL_STATIC_DIFF.json</c> (reader sites 0x24ca5c, 0x24ca6c).
    /// Only the Buf null test guards the branch; no Remaining gate, no trailing-byte check, so a
    /// surplus tail stays unread and a zero or negative owner is passed on unchanged. Decode only:
    /// the store is carried by the ordered entity event, not performed here.
    /// </remarks>
    public static bool TryDecodeWorldEntityOwner(
        byte[] payload,
        out int entityId,
        out int ownerId)
    {
        entityId = 0;
        ownerId = 0;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeWorldEntityOwner(payload, out entityId, out ownerId);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            entityId = 0;
            ownerId = 0;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeWorldEntityOwner(
        byte[] payload,
        out int entityId,
        out int ownerId)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        entityId = reader.I32();
        ownerId = reader.I32();

    }

    /// <summary>
    /// Decodes the 0x8052 self PK prefix and optional pkMapFree byte. The original Dispatch
    /// writes zero when that byte is absent and leaves any later bytes unread.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8052 (jump-table entry 0x8052 -&gt; arm64
    /// 0x251be4 / x86_64 0x260bc8; forward CFG reach arm64 68 instructions): one
    /// NetClient.Buf (arm64 ldr x21,[sp,#0x110] / x86_64 r13) is read with Buf.U8
    /// (arm64 0x26afcc / x86_64 0x27a630), Buf.I64 (arm64 0x274eac / x86_64 0x284070),
    /// Buf.U8, Buf.U8, Buf.I32 (arm64 0x26b00c / x86_64 0x27a660) and then Buf.get_Remaining
    /// (arm64 0x26afc0 / x86_64 0x27a620) gating a sixth Buf.U8, in exactly that order:
    /// arm64 0x251bf4 / 0x251c28 / 0x251c40 / 0x251c80 / 0x251c98 / 0x251cb0 / 0x251cc4 and
    /// x86_64 0x260bd6 / 0x260c0a / 0x260c25 / 0x260c6a / 0x260c84 / 0x260c9e / 0x260cac.
    /// Every read passes an empty second argument (arm64 mov x1,#0 / x86_64 xor esi,esi),
    /// which is the hidden MethodInfo slot and not a payload field.
    /// Each value is stored straight into its official static in the same order it is read:
    /// arm64 str w19,[x8,#0xdc] / str x0,[x8,#0xe0] / strb w0,[x8,#0xe8] / strb w0,[x8,#0xe9]
    /// / str w0,[x8,#0xec] / strb w19,[x8,#0xf0] and x86_64 movl %ebx,0xdc(%rax) /
    /// movq %rax,0xe0(%rcx) / movb %al,0xe8(%rcx) / movb %al,0xe9(%rcx) /
    /// movl %eax,0xec(%rcx) / movb %bl,0xf0(%rax). The frozen atlas resolves the static base
    /// to TypeInfo:CharData (metadata usage 1441, arm64 slot 0x148a5a8 / x86_64 slot
    /// 0x147ea20) and the offsets to pkMode int32 +0xdc (0x040014fc), karma int64 +0xe0,
    /// pkState byte +0xe8, pkMapSafe byte +0xe9, pkProtectLv int32 +0xec and pkMapFree byte
    /// +0xf0, so the leading U8 is widened into an int32 slot while karma keeps its signed
    /// 64-bit width and the three flag bytes keep their byte width.
    /// The third read is followed by static void SysConfigDlg.SyncPkFromServer(int)
    /// (0x06000df3, arm64 0x33df14 / x86_64 0x350970), called at arm64 0x251c74 / x86_64
    /// 0x260c60 with the mode reloaded from CharData.pkMode (arm64 0x251c50 ldr w19,[x8,#0xdc]
    /// / x86_64 0x260c3a movl 0xdc(%rcx),%r14d), that is after pkState and before pkMapSafe.
    /// The call therefore belongs to the owner that writes these fields, not to this decoder,
    /// and the PK-specific bridge that consumes the snapshot keeps exactly that position.
    /// The tail is optional and gated only by Remaining &gt;= 1 (arm64 cmp w0,#1 /
    /// b.lt 0x2588cc; x86_64 testl %eax,%eax / jle 0x260cb3); the absent case stores zero
    /// (arm64 0x2588cc mov w19,#0 / x86_64 0x260c59 xorl %ebx,%ebx) and bytes after that one
    /// are left unread. There is no mode whitelist, no clamp and no trailing-byte rejection.
    /// Reading past the end raises IndexOutOfRangeException inside the Buf indexer, which this
    /// decoder keeps as a decode failure in the same way as the other recovered Buf branches.
    /// A null payload is the dispatcher's NullReferenceException path (arm64 cbz x21 -&gt;
    /// 0x25c960 / x86_64 je 0x26b60d) and still fails closed without inventing a default mode.
    /// </remarks>
    public static bool TryDecodeSelfPkState(
        byte[] payload,
        out WorldSelfPkState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeSelfPkState(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeSelfPkState(
        byte[] payload,
        out WorldSelfPkState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        byte mode = (byte)reader.U8();
        long karma = reader.I64();
        byte pkState = (byte)reader.U8();
        byte mapSafe = (byte)reader.U8();
        int protectionLevel = reader.I32();
        // Official gate is Remaining >= 1, so a missing tail reads nothing and stores zero.
        byte mapFree = reader.Remaining >= 1 ? (byte)reader.U8() : (byte)0;
        state = new WorldSelfPkState(
            mode,
            karma,
            pkState,
            mapSafe,
            protectionLevel,
            mapFree);

    }

    /// <summary>
    /// Decodes the exact fixed 0x8044 root assigned to CombatWorld.LocalFaction.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8044 (jump-table entry 0x8044 -&gt; arm64
    /// 0x25416c / x86_64 0x2633eb): one NetClient.Buf (arm64 ldr x0,[sp,#0x110] with
    /// cbz -&gt; 0x25c960; x86_64 testq %r13,%r13 / je 0x26b60d) is read with Buf.I32
    /// (arm64 0x254178 bl 0x26b00c / x86_64 0x2633f9 callq 0x27a660) and the 32-bit value is
    /// stored straight into the CombatWorld static block (usage 1463 TypeInfo:CombatWorld,
    /// arm64 0x148a658 / x86_64 0x147ead0; static_fields at class +0xb8) at offset 8, which is
    /// the System.Int32 field CombatWorld.LocalFaction (atlas offset 8). The case ends with the
    /// shared dispatch exit (arm64 b 0x260d68 / x86_64 jmp 0x27009b): there is no Remaining
    /// gate, no sign or range filter, no default for a short payload and no trailing-byte
    /// check, so a negative faction is stored unchanged and bytes after the four are ignored.
    /// </remarks>
    public static bool TryDecodeLocalFactionState(
        byte[] payload,
        out WorldLocalFactionState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeLocalFactionState(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeLocalFactionState(
        byte[] payload,
        out WorldLocalFactionState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int faction = reader.I32();
        // Native 0x8044 consumes this fixed root and leaves later bytes unread.
        state = new WorldLocalFactionState(faction);

    }

    /// <summary>
    /// Decodes the exact fixed 0x8053 remote PK payload forwarded by the official dispatcher to
    /// CombatWorld.SetPlayerPkState(entityId, pkState) for every id that is not the local one.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8053 (jump-table entry 0x8053 -&gt; arm64
    /// 0x24d5f8 / x86_64 0x25b11e; forward CFG reach arm64 67 instructions with no exit into a
    /// shared tail, so the trailing call below belongs to this opcode alone): one
    /// NetClient.Buf is read with Buf.I32 (arm64 0x26b00c, called at 0x24d608 / x86_64
    /// 0x27a660 at 0x25b12c) for the entity id and Buf.U8 (arm64 0x26afcc at 0x24d618 /
    /// x86_64 0x27a630 at 0x25b139) for the PK-state byte, in that order. Every read passes an
    /// empty second argument (arm64 mov x1,#0 / x86_64 xor esi,esi), which is the hidden
    /// MethodInfo slot and not a payload field. There is no Buf.get_Remaining call and no
    /// trailing-byte check anywhere in the reachable body, so extra bytes are simply left
    /// unread; there is no clamp and no rejection of any particular state byte either.
    /// The entity id is then compared with NetClient.SelfEntityId, the int32 static at +0x7c
    /// (0x040009ee) reached through TypeInfo:NetClient (metadata usage 2073, arm64 slot
    /// 0x148b968 / x86_64 slot 0x147fde0): arm64 0x24d640 ldr w8,[x8,#0x7c] / cmp w20,w8 /
    /// b.ne 0x258034 and x86_64 0x25b162 cmpl 0x7c(%rax),%r14d / jne 0x266f73. Equal ids
    /// store the byte straight into CharData.pkState (metadata usage 1441 slot arm64 0x148a5a8
    /// / x86_64 0x147ea20, offset +0xe8; arm64 strb w19,[x8,#0xe8] at 0x24d66c / x86_64
    /// movb %bl,0xe8(%rax) at 0x25b18e) and return from the case without invoking
    /// SetPlayerPkState. Any other id tail-calls static void
    /// CombatWorld.SetPlayerPkState(int, byte) (0x06000134, arm64 0x23f5b4 reached by
    /// b 0x258098 / x86_64 0x24d310 reached by jmp 0x266fb8) with the entity id and the full
    /// byte widened back with arm64 and w1,w19,#0xff / x86_64 movzbl %bl,%esi.
    /// Reading past the end raises IndexOutOfRangeException inside the Buf indexer, kept here
    /// as a decode failure in the same way as the other recovered Buf branches. A null payload
    /// is the dispatcher's NullReferenceException path (arm64 cbz x19 -&gt; 0x25c960 / x86_64
    /// je 0x26b60d) and still fails closed. No default target is invented here: dropping an
    /// unknown entity id is SetPlayerPkState's own decision and is not duplicated in this
    /// decoder, and the id equality above is applied by the gateway's receive context.
    /// </remarks>
    public static bool TryDecodeRemotePkState(
        byte[] payload,
        out int entityId,
        out byte pkState)
    {
        entityId = 0;
        pkState = 0;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeRemotePkState(payload, out entityId, out pkState);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            entityId = 0;
            pkState = 0;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeRemotePkState(
        byte[] payload,
        out int entityId,
        out byte pkState)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        entityId = reader.I32();
        pkState = (byte)reader.U8();
        // Native 0x8053 consumes this fixed root and leaves later bytes unread.

    }

    /// <summary>
    /// Decodes the 0x805d entity id and the raw visibility byte, leaving the <c>!= 0</c> decision
    /// to the receive arm exactly as the case body makes it.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch (0x0600077f) case 0x805d (jump-table index
    /// 0x805d - 0x8001 = 92 of the frozen 118-entry table: arm64 case block 0x24c370 through
    /// targetBase 0x24be18, x86_64 case block 0x259d7c through table base 0x270104). The body is
    /// two reads in this order: Buf.I32 (0x06000795, arm64 0x26b00c / x86_64 0x27a660) into the
    /// entity id, then Buf.U8 (0x06000792, arm64 0x26afcc / x86_64 0x27a630) into the visibility
    /// byte. Reading past the end raises IndexOutOfRangeException inside the Buf indexer and is
    /// kept here as a decode failure like every other recovered Buf branch; a null payload is the
    /// dispatcher's own null path (arm64 cbz x19 -&gt; 0x25c960 / x86_64 test r13,r13 -&gt;
    /// je 0x26b60d) and still fails closed. Both reads precede the lookup, so a short payload
    /// never performs one.
    /// The byte is <b>not</b> normalised here: the official arm compares it after the lookup
    /// (arm64 0x24c3c0 cmp w19,#0 / 0x24c3c4 cset w1,ne; x86_64 0x259dcb test ebx,ebx /
    /// 0x259dcd setne cl), so the conversion stays on the route that also performs the lookup.
    /// No trailing-byte assertion is made: the case leaves later bytes unread.
    /// </remarks>
    public static bool TryDecodeRemoteInvisibility(
        byte[] payload,
        out int entityId,
        out byte invisibleFlag)
    {
        entityId = 0;
        invisibleFlag = 0;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeRemoteInvisibility(payload, out entityId, out invisibleFlag);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            entityId = 0;
            invisibleFlag = 0;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeRemoteInvisibility(
        byte[] payload,
        out int entityId,
        out byte invisibleFlag)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        entityId = reader.I32();
        invisibleFlag = (byte)reader.U8();
        // Native 0x805d consumes this fixed root and leaves later bytes unread.

    }

    /// <summary>
    /// Decodes the fixed 0x8011 result consumed by CombatWorld.OnCombat.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8011 (jump-table entry 0x8011 -> arm64 0x252fb8
    /// / x86_64 0x262111; forward CFG reach arm64 72 / x86_64 75 instructions): one
    /// NetClient.Buf (arm64 ldr x19,[sp,#0x110] / x86_64 r13) is read with Buf.I32
    /// (arm64 0x26b00c / x86_64 0x27a660) for attacker id, target id and damage, then Buf.U8
    /// (arm64 0x26afcc / x86_64 0x27a630) for the flags byte, then Buf.I32 again for target
    /// hit points, in exactly that order (arm64 0x252fc8 / 0x252fd8 / 0x252fe8 / 0x252ff8 /
    /// 0x253008; x86_64 0x26211f / 0x26212b / 0x262138 / 0x262145 / 0x262152). Every read
    /// passes an empty second argument (arm64 mov x1,#0 / x86_64 xor esi,esi), which is the
    /// hidden MethodInfo slot and not a payload field. There is no Remaining gate, no value
    /// clamp and no trailing-byte check; the five values are forwarded with a tail call to
    /// CombatWorld.OnCombat (0x0600015c, arm64 0x246684 / x86_64 0x253b50) as
    /// (int attackerId, int targetId, int dmg, int flags, int targetHp), the flags byte being
    /// widened to the int parameter without any restriction to 0/1. Reading past the end
    /// raises IndexOutOfRangeException inside the Buf indexer, which this decoder keeps as a
    /// decode failure in the same way as the other recovered Buf branches.
    /// The record stays immutable and holds the same five values; the ordering, the signed
    /// damage and the full byte width of the flags are preserved, and no default target is
    /// invented when a lookup later misses.
    /// </remarks>
    public static bool TryDecodeCombatResult(
        byte[] payload,
        out WorldCombatResultState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeCombatResult(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeCombatResult(
        byte[] payload,
        out WorldCombatResultState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int attackerEntityId = reader.I32();
        int targetEntityId = reader.I32();
        int damage = reader.I32();
        byte flags = (byte)reader.U8();
        int targetHitPoints = reader.I32();
        // Dispatch forwards this fixed root to OnCombat without value or residue gates.
        state = new WorldCombatResultState(
            attackerEntityId,
            targetEntityId,
            damage,
            flags,
            targetHitPoints);

    }

    /// <summary>
    /// Decodes the exact 0x800c target/value/type world-number request.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x800c (jump-table entry 0x800c -&gt; arm64
    /// 0x2517e0 / x86_64 0x26041c): one NetClient.Buf (arm64 ldr x21,[sp,#0x110]) is read with
    /// Buf.U8 (arm64 0x26afcc) for the target, Buf.I32 (arm64 0x26b00c) for the value and
    /// Buf.U8 again for the type, in exactly that order (arm64 0x2517f0 / 0x251800 / 0x251810).
    /// Every read passes an empty second argument (arm64 mov x1,#0), which is the hidden
    /// MethodInfo slot and not a payload field. There is no Remaining gate, no value clamp, no
    /// type filter and no trailing-byte check; the three values are forwarded with a tail call
    /// to CombatNumber.SpawnOn (0x06000117, arm64 0x23a734 / x86_64 0x248990) as
    /// (int target, int value, int type), both bytes being widened to int parameters without
    /// restriction. CombatNumber.SpawnOn itself gates only on type == 5 (heal per-frame stack
    /// cap of two, arm64 cmp w19,#0x5 at 0x23a798 with the cap branch at 0x23a914) and on
    /// target == 1 (arm64 cmp w21,#0x1 at 0x23a870), so a negative value and every other type
    /// are real official inputs and are not rejected here. Reading past the end raises
    /// IndexOutOfRangeException inside the Buf indexer, which this decoder keeps as a decode
    /// failure in the same way as the other recovered Buf branches. A null payload is the
    /// dispatcher's NullReferenceException path (arm64 cbz x21 -&gt; 0x25c960) and still fails
    /// closed without inventing a default number. The record stays immutable and keeps the
    /// exact byte width of target and type.
    /// </remarks>
    public static bool TryDecodeCombatNumber(
        byte[] payload,
        out WorldCombatNumberState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeCombatNumber(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeCombatNumber(
        byte[] payload,
        out WorldCombatNumberState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        byte target = (byte)reader.U8();
        int value = reader.I32();
        byte type = (byte)reader.U8();
        // Dispatch forwards this fixed root to CombatNumber.SpawnOn without value,
        // type or residue gates.
        state = new WorldCombatNumberState(target, value, type);

    }

    /// <summary>
    /// Decodes the exact ordered 0x800e root and its three independently guarded tails. Optional
    /// presence is retained so the official local-player branch can consume hold, cooldown, aim
    /// and scale semantics without Protocol layout escaping this assembly.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x800e (jump-table entry 0x800e -&gt; arm64
    /// 0x24f4a4 / x86_64 0x25da50): one NetClient.Buf (arm64 ldr x27,[sp,#0x110]) is read with
    /// Buf.I32 (arm64 0x26b00c) for skill id and chant hold, Buf.U8 (arm64 0x26afcc) for phase,
    /// Buf.I32 again for cooldown and caster entity id (arm64 0x24f4b4 / 0x24f4c4 / 0x24f4d4 /
    /// 0x24f4e4 / 0x24f4f4). Each read passes an empty second argument (arm64 mov x1,#0), the
    /// hidden MethodInfo slot. The three tails are guarded by Buf.get_Remaining
    /// (arm64 0x26afc0) and nothing else:
    ///   Remaining &gt;= 4  -&gt; Buf.I32 target, otherwise the register stays 0
    ///     (arm64 0x24f504 / 0x24f518 / 0x2581ec);
    ///   Remaining &gt;= 9  -&gt; Buf.U8 aim byte, Buf.I32 aim x, Buf.I32 aim y
    ///     (arm64 0x2581f8 / 0x25821c / 0x25822c / 0x25823c), otherwise no aim wire byte is read
    ///     at all, both coordinates stay 0 and the no-aim boolean register keeps its entry value
    ///     1 (arm64 0x258204 / 0x258208 / 0x25820c);
    ///   Remaining &gt;= 1  -&gt; Buf.U8 scale byte (arm64 0x258258 / 0x258270), otherwise no scale
    ///     byte is consumed.
    /// Three distinct things must not be confused here: the wire raw byte, the no-aim boolean
    /// register the native body derives from it, and the absent tail. The wire raw keeps its full
    /// byte width and is not a 0/1 flag. The native body compares that raw against zero and
    /// inverts the comparison into the no-aim boolean register, so the register - not the wire -
    /// is what carries the 1 seen at arm64 0x258204: it is the "no aim" value that the register
    /// already holds before any raw is read, and it is also the value raw == 0 writes back. The
    /// resulting truth table is:
    ///   absent tail         -&gt; no aim (no wire byte exists; the register keeps 1);
    ///   raw == 0            -&gt; no aim;
    ///   raw != 0            -&gt; aim position present, including raw values other than 1;
    ///   aim x == 0 and y==0 -&gt; still "aim present", because only raw is compared.
    /// This decoder therefore keeps the raw byte nullable and adds no invented default wire byte;
    /// <see cref="WorldSkillCastState.HasAim"/> is the single product expression of the register.
    /// Phase keeps its full byte
    /// width too; the official body only tests phase == 0 (arm64 cbz w25 -&gt; 0x259b88 /
    /// 0x25c564) for the preparation branch. There is no trailing-byte check, so a legally
    /// longer payload is accepted and the residue is ignored, and there is no value clamp and no
    /// 0/1 rejection. Reading past the end raises IndexOutOfRangeException inside the Buf
    /// indexer, which this decoder keeps as a decode failure like the other recovered Buf
    /// branches. A null payload is the dispatcher's NullReferenceException path
    /// (arm64 cbz x27 -&gt; 0x25c960) and still fails closed.
    /// </remarks>
    public static bool TryDecodeSkillCastAction(
        byte[] payload,
        out WorldSkillCastState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeSkillCastAction(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            state = null;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeSkillCastAction(
        byte[] payload,
        out WorldSkillCastState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int skillId = reader.I32();
        int chantHoldMilliseconds = reader.I32();
        byte phase = (byte)reader.U8();
        int cooldownMilliseconds = reader.I32();
        int casterEntityId = reader.I32();

        int? targetEntityId = null;
        if (reader.Remaining >= 4)
        {
            targetEntityId = reader.I32();
        }

        byte? aimFlag = null;
        int aimX = 0;
        int aimY = 0;
        if (reader.Remaining >= 9)
        {
            aimFlag = (byte)reader.U8();
            aimX = reader.I32();
            aimY = reader.I32();
        }

        byte? scalePercent = null;
        if (reader.Remaining >= 1)
        {
            scalePercent = (byte)reader.U8();
        }

        // Dispatch forwards the fixed root and the optional tails without a residue gate.
        state = new WorldSkillCastState(
            skillId,
            chantHoldMilliseconds,
            phase,
            cooldownMilliseconds,
            casterEntityId,
            targetEntityId,
            aimFlag,
            aimX,
            aimY,
            scalePercent);

    }

    /// <summary>
    /// Decodes the exact shared player life-state payload. The official dispatcher resolves the
    /// entity id after consuming both leading bytes; zero means death and nonzero means revive.
    /// Revive flags are retained for the future local-player dialog owner but do not affect a
    /// remote actor.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x801a (jump-table entry 0x801a -> arm64 0x24f5d4
    /// / x86_64 0x25db7f; forward CFG reach arm64 233 / x86_64 222 instructions): one
    /// NetClient.Buf is read with Buf.U8 (arm64 0x26afcc / x86_64 0x27a630) for the life byte,
    /// Buf.U8 again for the revive-flags byte and Buf.I32 (arm64 0x26b00c / x86_64 0x27a660)
    /// for the target id, in that order (arm64 0x24f5e4 / 0x24f5f4 / 0x24f604; x86_64
    /// 0x25db8d / 0x25db99 / 0x25dba6). There is no Remaining gate, no trailing-byte check and
    /// no rejection of any particular byte value: the whole 0..255 range of the life byte is
    /// decided by a single zero test (arm64 cbz w21 at 0x24f64c and cset w1,eq at 0x259acc;
    /// x86_64 test bl,bl at 0x25dbf0 and sete al at 0x268571), so 0 means dead and every
    /// nonzero value means alive.
    /// A target id of zero takes the same local branch without reading NetClient.SelfEntityId
    /// (arm64 0x24f608 cbz w0 -> 0x24f63c; x86_64 0x25dbab test eax,eax / je 0x25dbe0). A
    /// nonzero id is compared with NetClient static + 0x7c SelfEntityId (TypeInfo:NetClient
    /// slot arm64 0x148b968; arm64 0x24f630 ldr w8,[x8,#0x7c] / cmp w19,w8 / b.ne 0x259ab0;
    /// x86_64 0x25dbd6 cmp r15d,[rax+0x7c] / jne 0x268555). Equal ids go down the local
    /// branch; any other id tail-calls CombatWorld.PlayerDeadState (0x06000135,
    /// arm64 0x23f74c / x86_64 0x24d440) as (int id, bool dead) with dead = life byte == 0.
    /// Reading past the end raises IndexOutOfRangeException inside the Buf indexer, kept here
    /// as a decode failure.
    /// </remarks>
    public static bool TryDecodePlayerLifeState(
        byte[] payload,
        out int entityId,
        out bool alive,
        out int reviveFlags)
    {
        entityId = 0;
        alive = false;
        reviveFlags = 0;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodePlayerLifeState(payload, out entityId, out alive, out reviveFlags);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            entityId = 0;
            alive = false;
            reviveFlags = 0;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodePlayerLifeState(
        byte[] payload,
        out int entityId,
        out bool alive,
        out int reviveFlags)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int aliveByte = reader.U8();
        reviveFlags = reader.U8();
        entityId = reader.I32();
        // Native 0x801a consumes this prefix and leaves later bytes unread.
        alive = aliveByte != 0;

    }

    /// <summary>
    /// Decodes the exact shared entity movement-path payload. The gateway resolves the entity id
    /// before publishing a type-specific remote-player or monster path; this decoder does not
    /// guess the entity kind.
    /// </summary>
    /// <remarks>
    /// Official 1.5.8 NetClient.Dispatch case 0x8018 (arm64 block 0x24f77c-0x24f8ac, x86_64
    /// block 0x25dd64-0x25de72; jump-table entry 0x8018 -> arm64 0x24f77c / x86_64 0x25dd64):
    /// Buf.I32 (arm64 0x26b00c / x86_64 0x27a660) reads the id, a second Buf.I32 reads the speed
    /// and Buf.U8 (arm64 0x26afcc / x86_64 0x27a630) reads the point count. Two `int[]` of exactly
    /// that count are allocated before the count is even tested (arm64 0x24f7c4 and 0x24f7d4,
    /// x86_64 0x25dda6 and 0x25ddb4) and are filled interleaved, xs[i] before ys[i]
    /// (arm64 0x24f804 / 0x24f820, x86_64 0x25dde5 / 0x25de02). The count test
    /// (arm64 0x24f7dc `cmp w19,#1` / `b.lt 0x24f840`, x86_64 0x25ddbc `test r13d,r13d` /
    /// `jle 0x25de20`) only skips the fill loop, so a zero count still produces two empty arrays
    /// and still reaches the tail call to CombatWorld.MovePath (0x06000149, arm64 0x242ef4 /
    /// x86_64 0x250760) as `MovePath(int id, int[] xs, int[] ys, int speed)`. There is no
    /// trailing-byte check, and a short payload raises IndexOutOfRangeException inside the Buf
    /// read.
    /// The product keeps one immutable record holding the same interleaved (x, y) pairs instead
    /// of the two parallel arrays; the Presentation owner rebuilds `int[] xs` and `int[] ys` in
    /// the same order before calling the official RemotePlayer.SetPath / MonsterEntity.SetPath,
    /// so no value and no order is lost. Because the native branch accepts an empty path, the
    /// record no longer rejects one.
    /// </remarks>
    public static bool TryDecodeRemotePathState(
        byte[] payload,
        out WorldRemotePathState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeRemotePathState(payload, out state);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeRemotePathState(
        byte[] payload,
        out WorldRemotePathState state)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        int entityId = reader.I32();
        int speed = reader.I32();
        int pointCount = reader.U8();

        List<WorldPathPoint> points = new List<WorldPathPoint>(pointCount);
        for (int index = 0; index < pointCount; index++)
        {
            points.Add(new WorldPathPoint(
                reader.I32(),
                reader.I32()));
        }

        state = new WorldRemotePathState(entityId, speed, points);

    }

    /// <summary>
    /// Decodes the official 1.5.8 self-riding payload, including its optional seat count.
    /// Omitted tails default to zero; dismount clears all four tail fields.
    /// </summary>
    public static bool TryDecodeLocalRidingState(
        byte[] payload,
        WorldRidingState previous,
        out WorldRidingState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int ridingByte = reader.ReadByte();
            if (ridingByte > 1)
            {
                return false;
            }

            int mountModel = reader.ReadInt32();
            // Official 1.5.8 defaults omitted tails on each packet. The previous
            // parameter remains only for compatibility with existing callers.
            int riderSeat = 0;
            int rideAnchorId = 0;
            bool mountTianmo = false;
            int rideSeats = 0;

            if (reader.Remaining >= 5)
            {
                riderSeat = reader.ReadByte();
                rideAnchorId = reader.ReadInt32();
            }

            if (reader.Remaining >= 1)
            {
                int tianmoByte = reader.ReadByte();
                if (tianmoByte > 1)
                {
                    return false;
                }

                mountTianmo = tianmoByte != 0;
            }

            if (reader.Remaining >= 1)
            {
                rideSeats = reader.ReadByte();
            }

            reader.EnsureFinished();
            bool riding = ridingByte != 0;
            state = new WorldRidingState(
                riding,
                mountModel,
                riding ? riderSeat : 0,
                riding ? rideAnchorId : 0,
                riding && mountTianmo,
                riding ? rideSeats : 0);
            return true;
        }
        catch (Exception exception) when (
            exception is IOException ||
            exception is InvalidDataException ||
            exception is ArgumentException ||
            exception is OverflowException)
        {
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact remote-player riding update. Omitted compatibility tails use the
    /// original handler's zero defaults on every update.
    /// </summary>
    public static bool TryDecodeRemoteRidingState(
        byte[] payload,
        out WorldRemoteRidingState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            WireReader reader = new WireReader(payload);
            int entityId = reader.ReadInt32();
            int ridingByte = reader.ReadByte();
            if (ridingByte > 1)
            {
                return false;
            }

            int mountModel = reader.ReadInt32();
            bool mountTianmo = false;
            int riderSeat = 0;
            int rideAnchorId = 0;
            int mountSeats = 0;

            if (reader.Remaining >= 1)
            {
                int tianmoByte = reader.ReadByte();
                if (tianmoByte > 1)
                {
                    return false;
                }

                mountTianmo = tianmoByte != 0;
            }

            if (reader.Remaining > 0)
            {
                if (reader.Remaining < 5)
                {
                    return false;
                }

                riderSeat = reader.ReadByte();
                rideAnchorId = reader.ReadInt32();
            }

            if (reader.Remaining > 0)
            {
                mountSeats = reader.ReadByte();
            }

            reader.EnsureFinished();
            bool riding = ridingByte != 0;
            if (!riding)
            {
                riderSeat = 0;
                rideAnchorId = 0;
                mountSeats = 0;
            }

            state = new WorldRemoteRidingState(
                entityId,
                riding,
                mountModel,
                mountTianmo,
                riderSeat,
                rideAnchorId,
                mountSeats);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception))
        {
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact remote-player spawn root and ordered compatibility tails.
    /// </summary>
    /// <remarks>
    /// Official NetClient.Dispatch opcode 0x8023: the switch jump table maps 0x8023 to the arm64
    /// block 0x252714 (F32 at 0x252744 and 0x252754) and to the x86_64 block 0x261896 (F32 at
    /// 0x2618c1 and 0x2618d3). The two wire F32 coordinates are truncated to signed integers
    /// immediately before CombatWorld.SpawnPlayer (arm64 0x23f168, called with w4/w5): arm64
    /// 0x258b64 `fcvtzs w9, s8` and 0x258b7c `fcvtzs w9, s9` with the IL2CPP positive-infinity
    /// fix-up (`fcmp s8, s0` against 0x7f800000, then `csel w4, w10, w9, eq` with
    /// w10 = int.MinValue); x86_64 0x26776b and 0x267774 `cvttss2si`. No bound is tested before
    /// the conversion, so the product performs the same plain C# `unchecked((int))` cast and
    /// substitutes nothing for NaN or infinity.
    /// Conversion timing: the two float results stay in v8 and v9 from the read at 0x252744 /
    /// 0x252754 through the whole branch and are only converted at 0x258b64 / 0x258b7c, i.e.
    /// after every root and optional field has been read, after the local-player guard
    /// (arm64 0x258ac0 `UnityEngine.Object.op_Inequality`, then `ldrb w8,[x8,#0x108]` /
    /// `tbz w8,#0` -> `b 0x260d68`) and after the map guard (arm64 0x258af4 `cmp w21,#1 / b.lt`,
    /// 0x258b18 `cmp w8,#1 / b.lt`, 0x258b38 `cmp w21,w8 / b.ne 0x260d68`), one instruction
    /// block before the SpawnPlayer call at 0x258bfc. The product therefore keeps the raw float
    /// across the whole read batch and converts with the same x-then-y order right before the
    /// spawn record is built; it does not convert at the read site. The live decoder now evaluates
    /// the switching and map gates here, after the optional reads and before conversion. The
    /// legacy Try facade supplies no live context and retains its caller's compatibility gate.
    /// Every optional tail is gated on Remaining alone and falls back to a default when the tail
    /// is short - the native bodies compare Remaining against 5, 1 or 4 and otherwise store the
    /// default (arm64 0x258918 `stp wzr, wzr`, 0x258978 `str wzr`, 0x2589a4 `mov w21, #0`,
    /// 0x2589d0 `mov w26, #1`, 0x2589e8 `mov w22, #0`, 0x258a44 `mov w23, #0`, 0x258a74
    /// `mov w19, #0`). A short tail is never a rejection. No flag byte is validated against 1:
    /// the bodies only test inequality with zero (arm64 0x25893c, 0x258968, 0x258a6c, 0x258b58
    /// `cset w?, ne`). The branch ends without any trailing-byte check.
    /// The optional riding values are forwarded exactly as the packet carries them. Whether a
    /// rider seat, anchor id or seat count is zeroed when riding is false is decided by
    /// RemotePlayer.SetRiding (0x06000b26, arm64 0x2e0d28 / 980 bytes, x86_64 0x2f1fd0), which
    /// stores `csel w8, w23, wzr, ne` for the seat, the anchor id and the seat count while it
    /// stores the model and the tianmo flag unconditionally; that owner already reproduces it,
    /// so this decode adapter keeps the raw values and does not repeat the decision here.
    /// </remarks>
    public static bool TryDecodeRemotePlayerState(
        byte[] payload, out WorldRemotePlayerState state, out int mapIdGuard)
    {
        state = null;
        mapIdGuard = 0;
        if (payload == null) return false;
        try
        {
            DecodeRemotePlayerState(payload, null, out state, out mapIdGuard);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            return false;
        }
    }

    internal static void DecodeRemotePlayerState(byte[] payload, IWorldReceiveContext context,
        out WorldRemotePlayerState state, out int mapIdGuard)
    {
        state = null;
        mapIdGuard = 0;
        NetClient.Buf reader = new NetClient.Buf(payload);
        int entityId = reader.I32();
        string displayName = reader.Str();
        // Native order: the two F32 results stay in v8/v9 from 0x252744 / 0x252754 to
        // 0x258b64 / 0x258b7c, so the raw floats survive the whole read batch and are
        // converted only where the spawn is produced.
        float rawX = reader.F32();
        float rawY = reader.F32();
        int level = reader.I32();
        reader.U8();
        int gender = reader.U8();
        int head = reader.U8();
        int hair = reader.U8();
        string title = reader.Str();
        int body = reader.U16();
        int cap = reader.U16();
        int backpack = reader.U16();
        int rightWeapon = reader.U16();
        int leftWeapon = reader.U16();
        int face = reader.U16();
        int attackVariant = reader.U8();
        int weaponCategory = reader.U8();
        int attackDistance = reader.U16();
        int deadByte = reader.U8();

        string avatarAura = reader.Str();
        string soulEffects = reader.Str();
        int faction = reader.U8();
        int hitPoints = reader.I32();
        int maximumHitPoints = reader.I32();

        string titleEffect = string.Empty;
        if (reader.Remaining > 0)
        {
            titleEffect = reader.Str();
        }

        bool riding = false;
        int mountModel = 0;
        bool mountTianmo = false;
        int riderSeat = 0;
        int rideAnchorId = 0;
        int mountSeats = 0;
        int glowMode = 1;
        bool invisible = false;
        // arm64 0x258908: Remaining is compared with 5 and the pair is only read when the
        // tail is complete; a short tail stores (false, 0) and continues.
        if (reader.Remaining >= 5)
        {
            int ridingByte = reader.U8();
            riding = ridingByte != 0;
            mountModel = reader.I32();
        }

        if (reader.Remaining >= 1)
        {
            int tianmoByte = reader.U8();
            mountTianmo = tianmoByte != 0;
        }

        // arm64 0x258984: Remaining >= 4, otherwise 0 - not a rejection.
        if (reader.Remaining >= 4)
        {
            mapIdGuard = reader.I32();
        }

        // arm64 0x2589d0: the default is 1 when the tail is absent.
        if (reader.Remaining >= 1)
        {
            glowMode = reader.U8();
        }

        if (reader.Remaining >= 5)
        {
            riderSeat = reader.U8();
            rideAnchorId = reader.I32();
        }

        if (reader.Remaining >= 1)
        {
            mountSeats = reader.U8();
        }

        if (reader.Remaining >= 1)
        {
            int invisibleByte = reader.U8();
            invisible = invisibleByte != 0;
        }

        int? nativeAttackDistance = reader.Remaining >= 1 ? (int?)reader.U16() : null;

        // Original remote spawn admission follows ALL optional reads, before the F32 casts.
        if (context != null && context.Switching) return;
        if (context != null && mapIdGuard > 0 && context.CurrentMapId > 0 &&
            mapIdGuard != context.CurrentMapId) return;

        // The native branch hands these three optional values straight to
        // RemotePlayer.SetRiding (arm64 0x258c10-0x258c28: w1/w2 from [sp,#0x74]/[sp,#0x78],
        // w3 from [sp,#0x70], w4 = x28 riderSeat, w5 = x22 rideAnchorId, w6 = x23 mountSeats,
        // x7 = 0 methodInfo), so no riding-dependent normalization happens at decode time:
        // the raw packet values are forwarded and the owner decides.
        // Truncation happens here, at the point the spawn record is produced, in the native
        // x-then-y order and with no bound test, NaN substitution or clamp.
        int x = unchecked((int)rawX);
        int y = unchecked((int)rawY);

        WorldRemoteRidingState ridingState = new WorldRemoteRidingState(
            entityId,
            riding,
            mountModel,
            mountTianmo,
            riderSeat,
            rideAnchorId,
            mountSeats);
        state = new WorldRemotePlayerState(
            entityId,
            displayName,
            x,
            y,
            level,
            gender,
            head,
            hair,
            title,
            new CharacterAppearanceRecord(
                body,
                cap,
                backpack,
                rightWeapon,
                leftWeapon,
                face,
                attackVariant,
                weaponCategory,
                attackDistance,
                nativeAttackDistance),
            deadByte != 0,
            avatarAura,
            soulEffects,
            faction,
            hitPoints,
            maximumHitPoints,
            titleEffect,
            glowMode,
            invisible,
            ridingState);
    }

    /// <summary>
    /// Compatibility projection for the already published remote-riding contract.
    /// </summary>
    public static bool TryDecodeRemoteSpawnRidingState(
        byte[] payload,
        out WorldRemoteRidingState state,
        out int mapIdGuard)
    {
        state = null;
        WorldRemotePlayerState player;
        if (!TryDecodeRemotePlayerState(payload, out player, out mapIdGuard))
        {
            return false;
        }

        state = player.RidingState;
        return true;
    }

    /// <summary>
    /// Decodes the complete 0x8039 channel-chat payload consumed by NetClient.Dispatch.
    /// </summary>
    /// <remarks>
    /// The five root fields and the optional tail are read from one official NetClient.Buf, the
    /// same cursor the shared ReadShares reader continues on, so there is no second reader and no
    /// snapshot buffer. The share list is produced by ReadShares, so it is null - not an empty
    /// list - when the count byte is absent or zero. Trailing bytes after the optional level/flag
    /// pair are ignored because the native handler has no final Remaining check. See ReadShares
    /// for the row-truncation boundary.
    /// </remarks>
    public static bool TryDecodeChannelChat(
        byte[] payload,
        out int senderEntityId,
        out string senderName,
        out byte bubbleThemeSelector,
        out byte channelCode,
        out string text)
    {
        IReadOnlyList<WorldChatSharedEntryState> ignoredShares;
        return TryDecodeChannelChat(
            payload,
            out senderEntityId,
            out senderName,
            out bubbleThemeSelector,
            out channelCode,
            out text,
            out ignoredShares);
    }

    public static bool TryDecodeChannelChat(
        byte[] payload,
        out int senderEntityId,
        out string senderName,
        out byte bubbleThemeSelector,
        out byte channelCode,
        out string text,
        out IReadOnlyList<WorldChatSharedEntryState> shares)
    {
        return TryDecodeChannelChat(payload, out senderEntityId, out senderName, out bubbleThemeSelector,
            out channelCode, out text, out shares, out _, out _);
    }

    public static bool TryDecodeChannelChat(
        byte[] payload,
        out int senderEntityId,
        out string senderName,
        out byte bubbleThemeSelector,
        out byte channelCode,
        out string text,
        out IReadOnlyList<WorldChatSharedEntryState> shares, out byte senderLevel, out byte senderFlag)
    {
        senderLevel = 0; senderFlag = 0;
        senderEntityId = 0;
        senderName = string.Empty;
        bubbleThemeSelector = 0;
        channelCode = 0;
        text = string.Empty;
        shares = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeChannelChat(payload, out senderEntityId, out senderName, out bubbleThemeSelector,
                out channelCode, out text, out shares, out senderLevel, out senderFlag);
            return true;
        }
        // Compatibility/inspection API only. Live receipt calls DecodeChannelChat directly,
        // preserving the original Buf exception rather than replacing it with Try-false.
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            senderLevel = 0; senderFlag = 0;
            senderEntityId = 0;
            senderName = string.Empty;
            bubbleThemeSelector = 0;
            channelCode = 0;
            text = string.Empty;
            shares = null;
            return false;
        }
    }

    internal static void DecodeChannelChat(
        byte[] payload,
        out int senderEntityId,
        out string senderName,
        out byte bubbleThemeSelector,
        out byte channelCode,
        out string text,
        out IReadOnlyList<WorldChatSharedEntryState> shares, out byte senderLevel, out byte senderFlag)
    {
        senderLevel = 0; senderFlag = 0;
        NetClient.Buf reader = new NetClient.Buf(payload);
        senderEntityId = reader.I32();
        senderName = reader.Str();
        bubbleThemeSelector = (byte)reader.U8();
        channelCode = (byte)reader.U8();
        text = reader.Str();

        IReadOnlyList<WorldChatSharedEntryState> decodedShares = ReadShares(reader);

        if (reader.Remaining >= 2)
        { senderLevel = (byte)reader.U8(); senderFlag = (byte)reader.U8(); }
        shares = decodedShares;
    }

    /// <summary>
    /// Decodes the exact 0x8036 private-chat payload consumed by NetClient.Dispatch.
    /// </summary>
    /// <remarks>
    /// The official branch reads two strings from one NetClient.Buf, calls the shared ReadShares
    /// reader on that same cursor and tail-calls GossipDlg.Receive. The share list is therefore
    /// null - not an empty list - for an absent or zero count, and the native reader keeps the
    /// rows it already read when the next row has no byte left. There is no optional tail and no
    /// final Remaining check.
    /// </remarks>
    public static bool TryDecodePrivateChat(
        byte[] payload,
        out string senderName,
        out string text,
        out IReadOnlyList<WorldChatSharedEntryState> shares)
    {
        senderName = string.Empty;
        text = string.Empty;
        shares = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodePrivateChat(payload, out senderName, out text, out shares);
            return true;
        }
        // Compatibility/inspection API only; live receipt uses the propagating reader below.
        catch (Exception exception) when (IsDecodeFailure(exception) ||
                                          exception is IndexOutOfRangeException)
        {
            senderName = string.Empty;
            text = string.Empty;
            shares = null;
            return false;
        }
    }

    internal static void DecodePrivateChat(
        byte[] payload,
        out string senderName,
        out string text,
        out IReadOnlyList<WorldChatSharedEntryState> shares)
    {
        NetClient.Buf reader = new NetClient.Buf(payload);
        senderName = reader.Str();
        text = reader.Str();
        shares = ReadShares(reader);
    }

    /// <summary>
    /// The shared 0x8039/0x8036 share reader, restored against official 0x06000711
    /// NetClient.ReadShares (arm64 0x26aad8 / 1256 bytes,
    /// Build/WBD-008/arm64-06000711/06000711.c; x86_64 0x279f70 / 1712 bytes,
    /// Build/WBD-008-R2/x86_64-06000711/06000711.c). The carrier keeps the official parameter:
    /// one NetClient.Buf, the same cursor the caller's root fields were read from.
    /// </summary>
    /// <remarks>
    /// Both frozen bodies read one cursor and, in order:
    /// arm64 06000711.c:132-134 / x86_64 :156-159 - <c>_len - _pos &lt; 1</c> returns the null
    /// list, so an absent count byte is not a failure;
    /// arm64 :145-152 / x86_64 :162-178 - the count byte, and <c>count == 0</c> returns the null
    /// list;
    /// arm64 :153-154 / x86_64 :175-176 - a list allocated with exactly that count as its
    /// capacity;
    /// arm64 :155-158 / x86_64 :182 - per row, <c>_len - _pos &lt; 1</c> returns the rows read so
    /// far instead of failing, so a row that never begins is dropped;
    /// every other field is read by the official Buf primitives, so a row that begins but is
    /// short still fails there.
    /// The null result is observable: native ChatUI.Add and GossipDlg.Receive receive the null
    /// reference, and MapIncomingShares keeps null instead of substituting an empty list.
    /// </remarks>
    private static List<WorldChatSharedEntryState> ReadShares(NetClient.Buf reader)
    {
        if (reader.Remaining < 1)
        {
            return null;
        }

        int shareCount = reader.U8();
        if (shareCount == 0)
        {
            return null;
        }

        List<WorldChatSharedEntryState> shares =
            new List<WorldChatSharedEntryState>(shareCount);
        for (int index = 0; index < shareCount; index++)
        {
            if (reader.Remaining < 1)
            {
                return shares;
            }

            shares.Add(ReadSharedChatEntry(reader));
        }

        return shares;
    }

    /// <summary>
    /// One ChatUI.Share row. The official element type is the 152-byte ChatUI.Share struct
    /// (arm64 stride 0x98 at 06000711.c:339, x86_64 the same stride), and the field order is
    /// pinned by both bodies: U8 kind, I32 itemId, U8 quality, Str name, Str description, one
    /// U8 pet flag and, only when it is non-zero, the 0x06000712 brief, then four fixed I32.
    /// The product keeps the same widths (byte kind/quality/flag, int itemId and PP values) and
    /// the same null pet reference when the flag is zero.
    /// </summary>
    private static WorldChatSharedEntryState ReadSharedChatEntry(NetClient.Buf reader)
    {
        byte kind = (byte)reader.U8();
        int itemId = reader.I32();
        byte quality = (byte)reader.U8();
        string name = reader.Str();
        string description = reader.Str();
        PetBrief? pet = null;
        if (reader.U8() != 0)
        {
            pet = NetClient.ReadPetBrief(reader);
        }

        int ppAiUsed = reader.I32();
        int ppAiCap = reader.I32();
        int ppShUsed = reader.I32();
        int ppShCap = reader.I32();

        if (pet.HasValue)
        {
            PetBrief brief = pet.Value;
            brief.ppAiUsed = ppAiUsed;
            brief.ppAiCap = ppAiCap;
            brief.ppShUsed = ppShUsed;
            brief.ppShCap = ppShCap;
            pet = brief;
        }
        return new WorldChatSharedEntryState
        {
            kind = kind, itemId = itemId, quality = quality,
            name = name, desc = description, pet = pet
        };
    }

    private sealed class InspectedEquipmentWireRow
    {
        public InspectedEquipmentWireRow(
            int slot,
            int itemId,
            string name,
            string description,
            int currentDurability,
            int maximumDurability,
            int avatar,
            int soulAvatar)
        {
            Slot = slot;
            ItemId = itemId;
            Name = name;
            Description = description;
            CurrentDurability = currentDurability;
            MaximumDurability = maximumDurability;
            Avatar = avatar;
            SoulAvatar = soulAvatar;
        }

        public int Slot { get; }
        public int ItemId { get; }
        public string Name { get; }
        public string Description { get; }
        public int CurrentDurability { get; }
        public int MaximumDurability { get; }
        public int Avatar { get; }
        public int SoulAvatar { get; }
        public int Quality { get; set; }
    }



    /// <summary>
    /// Decodes the official SocialClient party replacement packet, including both optional
    /// index-patch tails. Members begin online; each available U8 replaces one row's flag, then
    /// each available I64 appends one persistent character id. The original loops are bounded by
    /// member count and tolerate an omitted compatibility tail.
    /// </summary>
    public static bool TryDecodePartyState(byte[] payload, out WorldPartyState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            var reader = new NetClient.Buf(payload);
            int leaderEntityId = reader.I32();
            string partyName = reader.Str();
            int memberCount = reader.U8();
            List<WorldPartyMemberState> members =
                new List<WorldPartyMemberState>(memberCount);
            for (int index = 0; index < memberCount; index++)
            {
                members.Add(
                    new WorldPartyMemberState(
                        reader.I32(),
                        reader.Str(),
                        reader.I32(),
                        reader.I32(),
                        reader.I32(),
                        reader.U8(),
                        reader.I32(),
                        reader.I32(),
                        true));
            }

            for (int index = 0; index < memberCount && reader.Remaining >= 1; index++)
            {
                members[index] = members[index].WithOnline(reader.U8() != 0);
            }

            List<long> characterIds = new List<long>(memberCount);
            for (int index = 0; index < memberCount && reader.Remaining >= 8; index++)
            {
                characterIds.Add(reader.I64());
            }

            List<byte> professions = new List<byte>(memberCount);
            for (int index = 0; index < memberCount && reader.Remaining >= 1; index++)
            {
                professions.Add((byte)reader.U8());
            }

            state = new WorldPartyState(
                leaderEntityId,
                partyName,
                members,
                characterIds,
                professions);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Decodes the exact six-field stall-owner packet while projecting only the three values
    /// retained by the official StallDlg.Owners dictionary.
    /// </summary>
    public static bool TryDecodeStallOwnerState(
        byte[] payload,
        out WorldStallOwnerState state)
    {
        state = null;
        if (payload == null)
        {
            return false;
        }

        try
        {
            var reader = new NetClient.Buf(payload);
            int ownerEntityId = reader.I32();
            reader.Str();
            int stallType = reader.U8();
            string displayName = reader.Str();
            reader.I32();
            reader.I32();
            state = new WorldStallOwnerState(ownerEntityId, stallType, displayName);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Decodes the single owner entity id consumed by the official stall-close branch.
    /// </summary>
    public static bool TryDecodeStallClosedOwnerId(byte[] payload, out int ownerEntityId)
    {
        ownerEntityId = 0;
        if (payload == null)
        {
            return false;
        }

        try
        {
            DecodeStallClosedOwnerId(payload, out ownerEntityId);
            return true;
        }
        catch (Exception exception) when (IsDecodeFailure(exception) || exception is IndexOutOfRangeException)
        {
            ownerEntityId = 0;
            return false;
        }
    }

    // Live dispatch entry: original read/store failures propagate before any later publication.
    internal static void DecodeStallClosedOwnerId(byte[] payload, out int ownerEntityId)
    {
        var reader = new NetClient.Buf(payload);
        ownerEntityId = reader.I32();

    }

    private static IReadOnlyList<int> ReadPlayerAttributeArray(WireReader reader)
    {
        int count = reader.ReadUInt16();
        int[] values = new int[count];
        for (int index = 0; index < count; index++)
        {
            values[index] = reader.ReadInt32();
        }

        return values;
    }

    private static byte ReadWireBoolean(WireReader reader, string field)
    {
        byte value = reader.ReadByte();
        if (value > 1)
        {
            throw new InvalidDataException(
                "The " + field + " flag is not a wire boolean.");
        }

        return value;
    }

    private static WorldPlayerPetSlotState ReadPetBrief(WireReader reader)
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
        int starve = reader.ReadInt32();
        int trust = reader.ReadInt32();
        int freePoints = reader.ReadInt32();
        int usedPoints = reader.ReadInt32();
        int skillCount = reader.ReadByte();
        List<WorldPlayerPetBriefSkillState> skills =
            new List<WorldPlayerPetBriefSkillState>(skillCount);
        for (int index = 0; index < skillCount; index++)
        {
            skills.Add(new WorldPlayerPetBriefSkillState(
                reader.ReadString(),
                reader.ReadInt32()));
        }

        // ReadPetBrief guards this byte with global Remaining. Inside a valid 0x800b packet it
        // is contextually mandatory because another row or the fixed 17-byte post-slot block
        // always follows.
        bool bound = ReadWireBoolean(reader, "pet-slot bound") != 0;
        return new WorldPlayerPetSlotState(
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
            starve,
            trust,
            freePoints,
            usedPoints,
            skills,
            bound);
    }

    private static bool IsDecodeFailure(Exception exception)
    {
        return exception is IOException ||
               exception is InvalidDataException ||
               exception is ArgumentException ||
               exception is OverflowException;
    }
}

}
