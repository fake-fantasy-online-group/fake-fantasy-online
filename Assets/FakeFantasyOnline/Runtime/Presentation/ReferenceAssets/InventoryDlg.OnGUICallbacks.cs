using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 0x0600053e, InventoryDlg+<>c__DisplayClass132_1.<OnGUI>b__2(InventoryDlg.Item).
    // The bag lookup predicate: it selects the row whose own bag index equals the captured index.
    private static bool SlotOwnsIndex(Item it, int index) => it.bagIdx == index;

    internal void ActivateOrdinaryItemForIndependentRuntime(ref int bagTab, Item item)
    {
        ActivateOrdinaryItem(ref bagTab, item);
    }

    // Share 06000527's complete ordinary suffix, including full Handle's failed-opening tail.
    internal bool HandlePetResetForIndependentRuntime(ref int bagTab, Item item)
    {
        if (!ItemActions.IsPetResetForIndependentRuntime(item.id)) return false;
        ActivateOrdinaryItem(ref bagTab, item);
        return true;
    }

    internal bool HandlePetFeedOrGrowthForIndependentRuntime(ref int bagTab, Item item)
    {
        if (!ItemActions.IsPetFeedOrGrowthForIndependentRuntime(item.id)) return false;
        ActivateOrdinaryItem(ref bagTab, item);
        return true;
    }

    // Official 0x06000540, InventoryDlg+<>c__DisplayClass132_2.<OnGUI>b__3().
    // Equipping an unbound item asks first, and only when the item is not a mining tool; everything
    // else goes straight to the server. The confirmation body is the companion below.
    private void EquipHeld(Item it)
    {
        int bagIndex = it.bagIdx;
        if (!it.bound && !NetClient.IsMiningTool(it.id))
        {
            SysMsgBox.Show("装备后将绑定(绑定后不可交易), 是否装备？", () => EquipConfirmed(bagIndex), null);
            return;
        }
        NetClient.I.EquipItemForIndependentRuntime(bagIndex);
    }

    // Official 0x06000547, InventoryDlg+<>c__DisplayClass132_5.<OnGUI>b__8().
    // Only affirmation skips a CLR-null current receiver; the direct path above does not.
    private static void EquipConfirmed(int bagIdx)
    {
        NetClient client = NetClient.I;
        if (!object.ReferenceEquals(client, null)) client.EquipItemForIndependentRuntime(bagIdx);
    }

    // Official 0x06000545, InventoryDlg+<>c__DisplayClass132_4.<OnGUI>b__7().
    private static void WardrobeStoreConfirmed(byte tab, int slot)
    {
        NetClient.I?.WardrobeStoreForIndependentRuntime(tab, slot);
    }

    // Official 0x06000549, InventoryDlg+<>c__DisplayClass132_6.<OnGUI>b__9().
    // The world-drop answer: it is always the confirmed variant, so the server no longer asks.
    private static void DropItemOnGround(byte tab, int slot, int x, int y, int itemId)
    {
        if (NetClient.I != null) NetClient.I.DropItemForIndependentRuntime(tab, slot, x, y, itemId, true);
    }

    // Official 0x06000542, InventoryDlg+<>c__DisplayClass132_3.<OnGUI>b__5(System.String).
    // Unlocking with a security code: the chat notice comes first, then the watchdog record, then
    // the request itself, always with on = false.
    private static void RequestUnlock(string name, byte tab, int slot, int itemId, string secCode)
    {
        RequestUnlockCore(name, tab, slot, itemId, secCode);
    }

    // Official 0x06000543, InventoryDlg+<>c__DisplayClass132_3.<OnGUI>b__6().
    // The same unlock without a security code; the official body differs only in the empty string.
    private static void RequestUnlockWithoutSecCode(string name, byte tab, int slot, int itemId)
    {
        RequestUnlockCore(name, tab, slot, itemId, "");
    }

    private static void RequestUnlockCore(string name, byte tab, int slot, int itemId, string secCode)
    {
        ChatUI.Add("系统", "正在解锁【" + name + "】…", new Color(150f / 255f, 215f / 255f, 1f, 1f), "", null);
        LockReqSent(name);
        NetClient.I?.SetItemLockForIndependentRuntime(tab, slot, false, secCode, itemId);
    }
}
}
