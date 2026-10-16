using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Same replacement and server write as a desktop skill-book drop; never activate it.
    internal static void BindMobileBookSkill(int slot, int skill, Texture2D icon)
    {
        BindMobileShortcut(slot, skill, icon, 0);
    }

    internal static void BindMobileShortcut(int slot, int id, Texture2D icon, int kind)
    {
        if (slot < 0 || slot >= 24 || id <= 0 || kind < 0 || kind > 2) return;
        DropHotbarSlotForIndependentRuntime(slot, id, icon, kind, -1);
        SyncHotbarForIndependentRuntime();
    }
}
}
