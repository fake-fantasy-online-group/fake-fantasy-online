namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// WBC-041 lane C: official <c>StallDlg.Deal</c> (06000dbd) together with the two local functions
/// its body carries, <c>&lt;Deal&gt;g__Stale|0</c> (06000dc7) and <c>&lt;Deal&gt;g__Send|1</c>
/// (06000dc8). Read on both frozen architectures: the main body from
/// <c>Build/ReferenceReverseGhidra-1.5.8/queries/wbc038-arm64-StallDlg/06000dbd.c</c> and
/// <c>wbc038-x86_64-StallDlg/06000dbd.c</c>; the two short bodies were disassembled in the same
/// frozen dylibs at arm64 0x339798 (220 bytes) / 0x339874 (456 bytes) and x86_64 0x34c3e0
/// (208 bytes) / 0x34c4b0 (416 bytes).
/// </summary>
/// <remarks>
/// Both local functions are instance methods of <c>StallDlg+&lt;&gt;c__DisplayClass38_0</c>
/// (atlas field table: <c>idx</c> +0x10, <c>&lt;&gt;4__this</c> +0x18, <c>lockId</c> +0x20,
/// <c>lockPrice</c> +0x28), so they are written as local functions inside <c>Deal</c> and close
/// over exactly those four slots. No second display class, no extra row copy and no duplicate
/// inventory list is added; the scan reads <c>InventoryDlg.Items</c> itself (atlas: InventoryDlg
/// static block +0x08, type <c>List&lt;InventoryDlg.Item&gt;[]</c>).
/// This file adds no opener: <c>Deal</c> is reached only by the row button in <c>OnGUI</c>, the
/// window stays owner-only and no 0x803C case or second stall window exists.
/// </remarks>
public partial class StallDlg
{
    /// <summary>
    /// 06000dbd: the row button body for one stall row index.
    /// </summary>
    private void Deal(int idx)
    {
        // NetClient.I is the first static field of NetClient (kind1 usage 02073, static block at
        // TypeInfo +0xb8, first slot). Its null test and the idx >= 0 test are ANDed by the native
        // body, so either one leaves everything else untouched.
        if (NetClient.I == null || idx < 0) return;

        // rows (+0x40) is dereferenced next: a null list reaches the NullReferenceException
        // helper. The body then leaves through the function tail when idx is past the end instead
        // of going into the List indexer, so the bound is guarded here rather than thrown.
        if (idx >= rows.Count) return;

        Row row = rows[idx];
        int lockId = row.itemId;   // Row native layout: itemId +0, count +4, price +8
        long lockPrice = row.price;

        // 06000dc7 <Deal>g__Stale|0(): true when the row no longer exists or no longer carries the
        // pair this dialog was opened with. Both reads of rows[idx] are separate native loads,
        // which is exactly what the short-circuiting || emits.
        bool Stale() => idx >= rows.Count
            || rows[idx].itemId != lockId
            || rows[idx].price != lockPrice;

        // 06000dc8 <Deal>g__Send|1(byte tb, int bag): Stale first, then the send. The identity of
        // the callee is NetClient.StallDeal (0x060006fb, arm64 0x2692fc / x86_64 0x2786e0), whose
        // parameter list the existing product carrier already matches exactly:
        // (ownerId, index, count, bagTab, bagSlot, expectItemId, expectPrice).
        void Send(byte tb, int bag)
        {
            if (Stale())
            {
                // usage 17567; the ok delegate is <>c.<Deal>b__38_5 (empty) and there is no cancel.
                SysMsgBox.Show(
                    "摊位内容已变动, 本次交易已取消。\n请重新确认价格后再买卖。",
                    () => { },
                    null);
                return;
            }
            // ownerId is StallDlg +0x24; count is the literal 1 on both architectures.
            if (NetClient.I != null)
                NetClient.I.StallDealForIndependentRuntime(ownerId, idx, 1, tb, bag, lockId, lockPrice);
        }

        if (type == 1)
        {
            // Acquire mode: the player sells to the stall owner, so a bag row carrying the item and
            // no pet must exist. The foreach keeps List`1+Enumerator on the stack with its embedded
            // current Item at the enumerator's +0x10, which is how the native reads map onto the
            // atlas Item offsets (bagIdx +0x0, id +0x4, pet +0x48 with HasValue first): the body
            // compares the enumerator +0x14 against lockId and tests enumerator +0x58 bit 0.
            for (byte tab = 0; tab < InventoryDlg.Items.Length; tab++)
            {
                foreach (InventoryDlg.Item item in InventoryDlg.Items[tab])
                {
                    if (item.id != lockId || item.pet.HasValue) continue;

                    // Only tab and bagIdx are captured (DisplayClass38_1 fields tb0/bag0); nothing
                    // else from the Item travels into the confirm callback.
                    byte tb0 = tab;
                    int bag0 = item.bagIdx;
                    SysMsgBox.Show(
                        "把【" + row.name + "】卖给摊主 " + ownerName + "?\n你将得到 " + MoneyWords(lockPrice),
                        () => Send(tb0, bag0),
                        () => { });
                    return;
                }
            }

            // No owned row of that item id: the ok button is the empty callback and there is no
            // cancel, so only one acknowledgement button answers.
            SysMsgBox.Show("你背包里没有【" + row.name + "】可卖。", () => { }, null);
            return;
        }

        // Purchase mode: nothing has to leave the bag, so tab 0 / slot -1 go on the wire.
        SysMsgBox.Show(
            "买入【" + row.name + "】?\n你将付出 " + MoneyWords(lockPrice),
            () => Send(0, -1),
            () => { });
    }
}

}
