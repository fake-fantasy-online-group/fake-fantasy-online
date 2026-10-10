namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class ToolTarget
{
    public static void PickEquip(int equipSlot, string equipName, bool hasAvatar)
    {
        if (!Active) return;
        int tool = ToolId;
        Active = false;
        CursorManager.I?.SetAvatarMode(false);
        if (!hasAvatar && tool == 0x3125)
        {
            SysMsgBox.Show("【" + equipName + "】没有融合变装, 无需分离。", () => { }, null);
            return;
        }
        SysMsgBox.Show("确定对【" + equipName + "】" + Verb + "？" + Warn,
            () => NetClient.I?.UseItemOnForIndependentRuntime(tool, 1, 0, equipSlot), null);
    }

    public static void PickBag(byte tab, int bagIdx, string itemName, bool bound, bool hasAvatar)
    {
        if (!Active) return;
        int tool = ToolId;
        Active = false;
        CursorManager.I?.SetAvatarMode(false);
        if (!bound && tool == 0x6311)
        {
            SysMsgBox.Show("【" + itemName + "】未绑定, 无需解绑。", () => { }, null);
            return;
        }
        if (!hasAvatar && tool == 0x3125)
        {
            SysMsgBox.Show("【" + itemName + "】没有融合变装, 无需分离。", () => { }, null);
            return;
        }
        SysMsgBox.Show("确定对【" + itemName + "】" + Verb + "？" + Warn,
            () => NetClient.I?.UseItemOnForIndependentRuntime(tool, 0, tab, bagIdx), null);
    }
}
}
