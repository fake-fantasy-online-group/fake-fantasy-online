using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Protocol
{

/// <summary>
/// Original hotbar row/writer projection and existing direct skill payloads.
/// </summary>
public static class PrivateServerHotbarCodec
{
    // Mandatory prefix; complete extension records may follow.
    public const int SnapshotPayloadSize =
        WorldHotbarState.PrimarySlotCount * (sizeof(int) + sizeof(byte) + sizeof(int)) + sizeof(byte);

    public const int SavePayloadSize =
        WorldHotbarState.SlotCount * (sizeof(int) + sizeof(byte)) + sizeof(byte);

    public const int CharacterSkillPayloadSize =
        sizeof(int) + sizeof(byte) + sizeof(int) + sizeof(int) + sizeof(int);

    // Data-only compatibility entry for Editor callers. Product receipt calls ReceiveSnapshot
    // directly: this length guard must never suppress its reached per-row owner stores.
    public static bool TryDecodeSnapshot(byte[] payload, out WorldHotbarState state)
    {
        state = null;
        if (payload == null || payload.Length < SnapshotPayloadSize) return false;
        state = ReceiveSnapshot(payload, null);
        return true;
    }

    // Dispatch 0600077f, 0x8016 only; WB-094/WB-095 frozen CFG. Keep each native store
    // between its row's final read and the next wire read. Buf and owner failures propagate:
    // reached dictionary entries survive, but no array adoption or mirror can follow a failure.
    internal static WorldHotbarState ReceiveSnapshot(byte[] payload, IHotbarReceiveOwner owner)
    {
        var reader = new NetClient.Buf(payload);
        var ids = new int[WorldHotbarState.SlotCount];
        var kinds = new int[WorldHotbarState.SlotCount];
        var icons = new int[WorldHotbarState.SlotCount];
        for (int index = 0; index < WorldHotbarState.PrimarySlotCount; index++)
        {
            ids[index] = reader.I32();
            kinds[index] = reader.U8();
            icons[index] = reader.I32();
            if (kinds[index] == 1 && ids[index] != 0 && icons[index] >= 1)
                owner?.ReceiveHotbarPetSkillIcon(ids[index], icons[index]);
        }
        bool expanded = reader.U8() != 0;
        for (int index = WorldHotbarState.PrimarySlotCount;
             index < WorldHotbarState.SlotCount && reader.Remaining > 8; index++)
        {
            ids[index] = reader.I32();
            kinds[index] = reader.U8();
            icons[index] = reader.I32();
            if (kinds[index] == 1 && ids[index] != 0 && icons[index] >= 1)
                owner?.ReceiveHotbarPetSkillIcon(ids[index], icons[index]);
        }

        // Only the complete case reaches the native tail. Optional/headless callers get the
        // same data projection, without claiming native writes or retaining a late replay.
        owner?.ReceiveHotbarSlots(ids, kinds, expanded, icons);
        var slots = new WorldHotbarSlotState[WorldHotbarState.SlotCount];
        for (int index = 0; index < slots.Length; index++)
            slots[index] = new WorldHotbarSlotState(index, ids[index],
                (WorldHotbarSlotKind)kinds[index], icons[index]);
        return new WorldHotbarState(slots, expanded);
    }

    public static bool TryEncodeSave(WorldHotbarState state, out byte[] payload)
    {
        payload = null;
        if (state == null || state.Slots.Count != WorldHotbarState.SlotCount) return false;
        var ids = new int[WorldHotbarState.SlotCount];
        var kinds = new int[WorldHotbarState.SlotCount];
        for (int index = 0; index < WorldHotbarState.SlotCount; index++)
        {
            WorldHotbarSlotState slot = state.Slots[index];
            if (slot == null || slot.Index != index) return false;
            ids[index] = slot.Id;
            kinds[index] = (int)slot.Kind;
        }
        var writer = new NetClient.Buf();
        NetClient.WriteHotbarForIndependentRuntime(writer, ids, kinds, state.Expanded);
        payload = writer.ToArray();
        return true;
    }

    public static bool TryEncodeCharacterSkill(
        int skillId,
        int targetEntityId,
        out byte[] payload)
    {
        payload = null;
        WireWriter writer = new WireWriter();
        writer.WriteInt32(skillId);
        writer.WriteByte(0);
        writer.WriteInt32(0);
        writer.WriteInt32(0);
        writer.WriteInt32(targetEntityId);
        payload = writer.ToArray();
        return payload.Length == CharacterSkillPayloadSize;
    }

    /// <summary>
    /// Exact NetClient.UseSkillAt payload: the shared 0x1013 opcode uses mode one and carries
    /// the selected map-pixel point before the optional resolved entity target.
    /// </summary>
    public static bool TryEncodeCharacterSkillAt(
        int skillId,
        int x,
        int y,
        int targetEntityId,
        out byte[] payload)
    {
        payload = null;
        WireWriter writer = new WireWriter();
        writer.WriteInt32(skillId);
        writer.WriteByte(1);
        writer.WriteInt32(x);
        writer.WriteInt32(y);
        writer.WriteInt32(targetEntityId);
        payload = writer.ToArray();
        return payload.Length == CharacterSkillPayloadSize;
    }

    public static bool TryEncodePetSkill(int skillId, out byte[] payload)
    {
        return PrivateServerPetCommandCodec.TryEncodeUseSkill(skillId, out payload);
    }
}

}
