using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class SkillTargeting
{
    private static bool CastTrapAtPointer(int skillId, int gx, int gy)
    {
        if (!CombatWorld.TryTrapAtPointerForSkill(gx, gy, out int trapId))
        { UITip.Show("请选择可见的陷阱", UIScale.Factor); return false; }
        NetClient.I?.UseSkill(skillId, trapId);
        return true;
    }
}
public partial class CombatWorld
{
    // Pointer hit slop is UI-only; the server verifies visibility, ownership and skill range.
    internal static bool TryTrapAtPointerForSkill(int gx, int gy, out int trapId)
    {
        trapId = 0;
        if (I == null || I.ppu <= 0) return false;
        float best = 24f * 24f;
        foreach (var entry in I.trapPos)
        {
            float dx = entry.Value.x * I.ppu + I.worldW * .5f - gx;
            float dy = I.worldH * .5f - entry.Value.y * I.ppu - gy;
            float distance = dx * dx + dy * dy;
            if (distance <= best && (distance < best || trapId == 0 || entry.Key < trapId))
            { best = distance; trapId = entry.Key; }
        }
        return trapId != 0;
    }
}
}
