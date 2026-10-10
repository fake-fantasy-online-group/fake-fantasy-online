using System;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    private Action<int, byte, int> writeUseItem;
    private Action<int, int> writeTargetedItem;
    private Action<string, Color> itemUseSystemMessage;
    internal void BindItemUseWritersForIndependentRuntime(Action<int, byte, int> use,
        Action<int, int> targeted, Action<string, Color> message)
    {
        writeUseItem = use;
        writeTargetedItem = targeted;
        itemUseSystemMessage = message;
    }

    public void UseItem(int itemId, byte tab = 255, int slot = -1)
    {
        if (itemId == 0xd36 || itemId == 0xd37 || itemId == 0xd38 || itemId == 0xd3c || itemId == 0xd3d)
        {
            Capture(CombatWorld.CaptureTargetId, itemId);
            return;
        }
        if (itemId != 0x182c3)
        {
            writeUseItem(itemId, tab, slot);
            return;
        }
        int target = CombatWorld.CaptureTargetId;
        if (target != 0)
        {
            writeTargetedItem(target, itemId);
            return;
        }
        itemUseSystemMessage("请先选中一个怪物, 再使用【惊喜闪电】", new Color(1f, 180f / 255f, 120f / 255f, 1f));
    }
}
}
