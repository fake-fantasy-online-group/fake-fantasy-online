using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class GameHud
{
    // Official 1.5.8 06000442: ARM64 0x4412d8 / x86_64 0x4590f0.
    public static bool TryDropItemOnHotbar(int itemId, Texture2D icon, Vector2 pos)
    {
        for (int i = 0; i < 30; i++)
        {
            if (hotSlotRect[i].width > 0f && hotSlotRect[i].Contains(pos))
            {
                hotSkill[i] = itemId;
                hotIcon[i] = icon;
                hotKind[i] = 2;
                SyncHotbar();
                return true;
            }
        }
        return false;
    }

    // Official 1.5.8 06000443: ARM64 0x441540 / x86_64 0x4593a0.
    public static bool TryDropSkillOnHotbar(int skillId, Texture2D icon, Vector2 pos)
    {
        for (int i = 0; i < 30; i++)
        {
            if (hotSlotRect[i].width > 0f && hotSlotRect[i].Contains(pos))
            {
                hotSkill[i] = skillId;
                hotIcon[i] = icon;
                hotKind[i] = 0;
                SyncHotbar();
                return true;
            }
        }
        return false;
    }
}
}
