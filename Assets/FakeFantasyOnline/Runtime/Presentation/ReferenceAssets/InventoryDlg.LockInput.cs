using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    internal static void HandleLockForIndependentRuntime(int tab, Item item, Action<string, Color> systemMessage)
    {
        byte capturedTab = unchecked((byte)tab);
        int slot = item.bagIdx;
        int itemId = item.id;
        string name = item.pet.HasValue
            ? !string.IsNullOrEmpty(item.pet.Value.nickname) ? item.pet.Value.nickname : item.pet.Value.species
            : item.name;
        if (NetClient.I == null)
        {
            systemMessage("未连接服务器, 无法改动道具锁", new Color(1f, 150f / 255f, 150f / 255f, 1f));
            return;
        }
        if (item.locked)
        {
            if (AccountSec.NeedSecCodeForUnlock)
            {
                AccountSec.NoteUnlockTarget(capturedTab, slot, name);
                SecCodeAskDlg.Open(name, "解锁后它就能卖店/交易/摆摊/邮寄/丢弃", sec =>
                {
                    systemMessage("正在解锁【" + name + "】…", new Color(150f / 255f, 215f / 255f, 1f, 1f));
                    LockReqSent(name);
                    NetClient.I?.SetItemLockForIndependentRuntime(capturedTab, slot, false, sec, itemId);
                });
            }
            else
            {
                AccountSec.NoteUnlockTarget(capturedTab, slot, name);
                SysMsgBox.Show("解锁【" + name + "】?\n解锁后就能卖店/交易/摆摊/邮寄/丢弃了。", () =>
                {
                    systemMessage("正在解锁【" + name + "】…", new Color(150f / 255f, 215f / 255f, 1f, 1f));
                    LockReqSent(name);
                    NetClient.I?.SetItemLockForIndependentRuntime(capturedTab, slot, false, "", itemId);
                }, null);
            }
        }
        else
        {
            systemMessage("正在给【" + name + "】加锁…", new Color(150f / 255f, 215f / 255f, 1f, 1f));
            LockReqSent(name);
            NetClient.I.SetItemLockForIndependentRuntime(capturedTab, slot, true, "", itemId);
        }
    }
}
}
