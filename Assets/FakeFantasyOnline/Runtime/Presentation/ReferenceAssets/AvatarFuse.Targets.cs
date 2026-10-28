namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class AvatarFuse
{
    public static void PickTarget(int equipSlot, string equipName, bool hasAvatar)
    {
        if (!Active) return;
        byte tab = _tab;
        int slot = _slot;
        Active = false;
        CursorManager.I?.SetAvatarMode(false);
        byte kind = _kind;
        if (kind == 3) { EndgamePanel.ApplyPowerScroll(tab, slot, "equipped", 0, equipSlot); return; }
        if (hasAvatar && kind == 0)
        {
            SysMsgBox.Show("【" + equipName + "】已有变装, 请先用分离卷轴分离后再融合。", () => { }, null);
            return;
        }
        string label = kind == 1 ? "灵外观" : kind == 2 ? "元素" : "变装";
        SysMsgBox.Show("确定将" + label + "融合到【" + equipName + "】？",
            () => NetClient.I?.ApplyAvatarForIndependentRuntime(tab, slot, equipSlot, kind, 1, 0), null);
    }

    public static void PickTargetBag(byte targetTab, int targetBagIdx, string equipName, bool hasAvatar)
    {
        if (!Active) return;
        byte tab = _tab;
        int slot = _slot;
        byte kind = _kind;
        Active = false;
        CursorManager.I?.SetAvatarMode(false);
        if (kind == 3) { EndgamePanel.ApplyPowerScroll(tab, slot, "bag", targetTab, targetBagIdx); return; }
        if (targetTab == tab && targetBagIdx == slot)
        {
            SysMsgBox.Show("不能把道具融合到它自己身上，请选择要融合的目标装备。", () => { }, null);
            return;
        }
        if (hasAvatar && kind == 0)
        {
            SysMsgBox.Show("【" + equipName + "】已有变装, 请先用分离卷轴分离后再融合。", () => { }, null);
            return;
        }
        string label = kind == 1 ? "灵外观" : kind == 2 ? "元素" : "变装";
        SysMsgBox.Show("确定将" + label + "融合到【" + equipName + "】？",
            () => NetClient.I?.ApplyAvatarForIndependentRuntime(tab, slot, targetBagIdx, kind, 0, targetTab), null);
    }
}
}
