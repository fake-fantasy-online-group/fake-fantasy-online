using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class AccountSec
{
    // Official 0x06000004 (kind, code, msg, tab, slot, itemId).
    // kind 3 and kind 4 are the two lock-request replies: both clear the pending lock
    // timestamp first, kind 4 only echoes a non-empty server message in orange, kind 3
    // asks SecCodeAskDlg for the account security code. Every other kind is a plain
    // account-security result: green on success (code == 0), red otherwise, and it is
    // forwarded to AccountSecDlg.OnResult.
    public static void OnResult(int kind, int code, string msg, int tab, int slot, int itemId)
    {
        if (kind != 3 && kind != 4)
        {
            ChatUI.Add("系统", msg, code == 0
                ? new Color(150f / 255f, 235f / 255f, 150f / 255f, 1f)
                : new Color(1f, 150f / 255f, 150f / 255f, 1f), "", null);
            AccountSecDlg.OnResult(kind, code == 0, msg);
            return;
        }
        InventoryDlg.LockReqClear();
        if (kind == 4)
        {
            if (string.IsNullOrEmpty(msg)) return;
            ChatUI.Add("系统", msg, new Color(1f, 180f / 255f, 120f / 255f, 1f), "", null);
            return;
        }
        string name = pendSlot == slot && pendTab == unchecked((byte)tab) && !string.IsNullOrEmpty(pendName)
            ? pendName : "该物品";
        SecCodeAskDlg.Open(name, string.IsNullOrEmpty(msg) ? "解锁需要输入账号安全码" : msg, sec =>
        {
            InventoryDlg.LockReqSentPublic(name);
            NetClient.I?.SetItemLockForIndependentRuntime(tab, slot, false, sec, itemId);
        });
    }
}
}
