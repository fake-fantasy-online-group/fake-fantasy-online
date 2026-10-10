using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{
    internal static int petLevel;
    // Official CharData static +0x178 (System.Int32, atlas ordinal 78 of CharData). The 0x800b
    // handler is its only writer inside NetClient.Dispatch: arm64 0x25e000 stores the byte that
    // the decoder calls activeSlot, read at 0x24fd7c between the ignored compatibility byte and
    // the current-skill count. Win05.DrawPet 06001043 reads it once on entry and compares it
    // with the ten-slot loop index, so it is the official active-cell identity.
    internal static int petActive;
    internal static bool petBound;
    internal static int petArousal;
    internal static int petPickQuality;
    internal static int petGrowTier;
    private static WorldPlayerPetTailState boundPetTail;

    internal static void BindPetSnapshotTailForIndependentRuntime(WorldPlayerPetStatusState pet)
    {
        // Receipt-owned roots must survive a stale frame, forced rebuild or adapter teardown.
        if (PetSnapshotReceiptOwned) return;
        petModel = pet.Model;
        petActive = pet.Detail.ActiveSlot;
        // CharData.petActiveSkill is the official CharData static +0x288 (System.Int32, atlas
        // ordinal 139), the root Win05.DrawPet 0x06001043 compares a skill row against and writes
        // its echo into. The 0x800b case of NetClient.Dispatch 0x0600077f is its official writer:
        // arm64 0x24fd64 is `str w0,[x8,#0x288]` (x8 = CharData static_fields, usage 1441 at
        // 0x148a5a8) immediately after the Buf.I32 read at 0x24fd58 and immediately before the two
        // Buf.U8 reads at 0x24fd70 / 0x24fd7c whose second byte becomes petActive +0x178 at
        // 0x25e000 - i.e. exactly the wire order `i32 activeSkill || u8 ignoredCompat ||
        // u8 activeSlot` of docs/contracts/PRIVATE_SERVER_WIRE_V1.md:816. x86_64 is the same at
        // 0x262329 (`mov dword ptr [rcx + 0x288], eax` between the +0x1ec store and the two U8
        // reads). So the server owns this root and the panel's echo is a UI echo of one value,
        // overwritten by the next published snapshot, just like CharData.petName.
        petActiveSkill = pet.Detail.ActiveSkillId;
        petName = pet.DisplayName;
        // Vitals, including petLevel, belong to receipt-time CharData stores. A delayed
        // snapshot projector must not overwrite a more recent complete or partial receipt.
        petPrename = pet.Detail.Prename;
        petBound = pet.Bound;
        petArousal = pet.Detail.Arousal;
        if (pet.OptionalTail == null || object.ReferenceEquals(boundPetTail, pet.OptionalTail)) return;
        boundPetTail = pet.OptionalTail;
        if (pet.PickQuality.HasValue) petPickQuality = pet.PickQuality.Value;
        if (pet.FightCapacity.HasValue) petFightCap = pet.FightCapacity.Value;
        if (pet.LifeCapacity.HasValue) petLifeCap = pet.LifeCapacity.Value;
        if (pet.GrowthTier.HasValue) petGrowTier = pet.GrowthTier.Value;
    }
}

}
