using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Same replacement and server write as a desktop skill-book drop; never activate it.
    internal static void BindMobileBookSkill(int slot, int skill, Texture2D icon)
    {
        if (slot < 0 || slot >= 30 || skill <= 0) return;
        DropHotbarSlotForIndependentRuntime(slot, skill, icon, 0, -1);
        SyncHotbarForIndependentRuntime();
    }
}
}
