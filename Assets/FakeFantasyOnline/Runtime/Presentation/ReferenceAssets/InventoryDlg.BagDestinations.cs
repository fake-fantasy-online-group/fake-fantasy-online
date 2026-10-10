using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    [Serializable] private class FbWrap { public int[] item; public int[] skill; }
    private static Dictionary<int, int> fabaoSkill;
    // Original 0508. The initialized dictionary remains cached even when the table is absent.
    public static int FabaoSkillOf(int itemId)
    {
        if (fabaoSkill == null)
        {
            fabaoSkill = new Dictionary<int, int>();
            string text = GameTables.LoadText("fabao_skills");
            if (text != null)
            {
                FbWrap table = JsonUtility.FromJson<FbWrap>(text);
                if (table != null && table.item != null && table.skill != null)
                    for (int i = 0; i < table.item.Length && i < table.skill.Length; i++)
                        fabaoSkill[table.item[i]] = table.skill[i];
            }
        }
        return fabaoSkill.TryGetValue(itemId, out int skill) ? skill : 0;
    }
    // Original 052f: derive map pixels from the actual main camera and original world dimensions.
    private static (int, int) MouseWorld()
    {
        Camera camera = Camera.main;
        if (camera == null) return (0, 0);
        Vector3 mouse = Input.mousePosition;
        mouse.z = -camera.transform.position.z;
        Vector3 world = camera.ScreenToWorldPoint(mouse);
        return (Mathf.RoundToInt(world.x * GameBootstrap.CurPpu + GameBootstrap.CurWorldW * .5f),
            Mathf.RoundToInt(GameBootstrap.CurWorldH * .5f - world.y * GameBootstrap.CurPpu));
    }
    // Original 0527 outside-inventory left-click tail. Priority is shared with the original windows.
    private static void PlaceBagOutsideForIndependentRuntime(Event ev)
    {
        Vector2 point = ev.mousePosition;
        if (heldPet && Win05.HitPetSlot(point) >= 0)
        {
            if (NetClient.I != null) NetClient.I.PetDeployForIndependentRuntime(heldSlot);
            DropBagReferenceForIndependentRuntime(); ev.Use(); return;
        }
        if (!heldPet)
        {
            int skill = FabaoSkillOf(heldId);
            if (skill > 0 && GameHud.TryDropSkillOnHotbar(skill, null, point))
            { DropBagReferenceForIndependentRuntime(); ev.Use(); return; }
            if (HeldTab != 2 && GameHud.TryDropItemOnHotbar(heldId, heldIcon, point))
            { DropBagReferenceForIndependentRuntime(); ev.Use(); return; }
        }
        if (ComposeDlg.TryDropRefine((byte)HeldTab, heldSlot, point) ||
            MailDlg.TryDropAttach((byte)HeldTab, heldSlot, point) ||
            StoreDlg.TryDropDeposit((byte)HeldTab, heldSlot, point) ||
            Win05.TryDropWardrobe((byte)HeldTab, heldSlot, heldId, point) ||
            Win05.TryDropChangeSet((byte)HeldTab, heldSlot, heldId, point))
        { DropBagReferenceForIndependentRuntime(); ev.Use(); return; }
        if (GameHud.PosOverHotbar(point))
        { DropBagReferenceForIndependentRuntime(); ev.Use(); return; }
        if (WinZ.AnyWindowAt(point)) return;
        if (IsLocked((byte)HeldTab, heldSlot))
        {
            ChatUI.Add("系统", "该物品已加锁, 无法丢弃。Shift+右键 可解锁", new Color(1, 180f / 255f, 120f / 255f, 1));
            DropBagReferenceForIndependentRuntime(); ev.Use(); return;
        }
        (int x, int y) = MouseWorld();
        byte bagTab = (byte)HeldTab;
        int bagSlot = heldSlot, itemId = heldId;
        string name = heldName;
        Item item = ItemAt(bagTab, bagSlot);
        string fused = FusedGuard.What(item);
        if (heldBound || fused.Length > 0 || item.quality > 2 || item.count > 1)
        {
            var text = new StringBuilder();
            text.Append('【').Append(name).Append('】');
            if (item.count > 1) text.Append('×').Append(item.count);
            if (fused.Length > 0) text.Append(" 上融合着 ").Append(fused).Append(heldBound ? "; 且" : ", ");
            else text.Append(item.count > 1 ? "\n" : "");
            text.Append(heldBound ? "已绑定, 直接丢弃将销毁(无法捡回)" : "丢弃后掉在地上, 别人也能捡走");
            if (item.count > 1) text.Append("。\n★这一格会【一次全部丢出去】, 不能只丢一部分。");
            if (item.quality > 2 && !heldBound)
                text.Append("。\n★这是").Append(item.quality == 3 ? "【紫色】" : "【金色】")
                    .Append("装备, 落地后 30 秒内就可能被路过的人捡走, 捡走了要不回来。");
            if (fused.Length > 0)
                text.Append("\n融合的部分会一起失去, 不会退回背包。\n(想留下它: 先用分离卷轴取下来, 神奇货架 5 彩玉)");
            text.Append("\n\n确定要丢弃吗？");
            // Original 0549 captures the location and item identity before the held state is cleared.
            SysMsgBox.Show(text.ToString(), () =>
            {
                if (NetClient.I != null) NetClient.I.DropItemForIndependentRuntime(bagTab, bagSlot, x, y, itemId, true);
            }, null);
        }
        else if (NetClient.I != null) NetClient.I.DropItemForIndependentRuntime(bagTab, bagSlot, x, y, itemId, false);
        DropBagReferenceForIndependentRuntime(); ev.Use();
    }
}
}
