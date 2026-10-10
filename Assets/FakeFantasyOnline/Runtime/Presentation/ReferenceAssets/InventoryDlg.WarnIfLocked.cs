using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class InventoryDlg
{
    // Official 06000514, reused by the trade drop path.
    public static bool WarnIfLocked(int tab, int bagIdx, string what)
    {
        bool locked = IsLocked(tab, bagIdx);
        if (locked) ChatUI.Add("系统", "该物品已加锁, 无法" + what + "。Shift+右键 可解锁",
            new Color(1, 180f / 255, 120f / 255, 1));
        return locked;
    }
}
}
