namespace FakeFantasyOnline.Application
{
// Facet of the already-installed receive adapter; all state remains in the original
// InventoryDlg/EquipDlg, Win05 and SocialClient static owners.
public interface IWorldStaticSnapshotReceiveOwner
{
    IWorldInventorySnapshotRows BeginInventorySnapshotRows(int itemCount);
    IWorldInventoryDeltaRows BeginInventoryDeltaRows(int itemCount);
    void ReceiveInventorySnapshot(WorldInventoryState state);
    void ReceiveLifeSkillSnapshot(WorldLifeSkillBookState state);
    void ReceivePartySnapshot(WorldPartyState state);
}

// The original 8006 temporary bag/ordinal/equipment lists live in Presentation.
// Each row reaches them while its packet is being read, so resource failures keep
// their original position relative to later reads and the five header stores.
public interface IWorldInventorySnapshotRows
{
    void AddItem(WorldInventoryItemState item);
    void BeginEquipped();
    void AddEquipped(WorldInventoryEquippedItemState item);
    void LockItem(int ordinal);
    void SetPetPoints(int ordinal, int aiUsed, int aiCapacity, int shengUsed, int shengCapacity);
    void SetFusedDescription(int ordinal, string description);
    void SetGrowthTier(int ordinal, int growthTier);
    void SetEquippedQuality(int ordinal, byte quality);
    void Publish(WorldInventoryState state);
}

// Original 8069 temporary rows, followed by its ordered PatchSlot calls.
public interface IWorldInventoryDeltaRows
{
    void AddItem(WorldInventoryItemState item);
    void BeginEquipped();
    void AddEquipped(WorldInventoryEquippedItemState item);
    void SetLocked(int ordinal, bool locked);
    void SetFusedDescription(int ordinal, string description);
    void SetGrowthTier(int ordinal, int growthTier);
    void SetEquippedQuality(int ordinal, byte quality);
    void PatchItem(int ordinal, bool lockAuthoritative);
    void PatchEquipped();
    void Publish(WorldInventoryState state);
}
}
