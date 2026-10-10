using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{

    private static int HotSlotNumber(int index)
    {
        if (hotSkill[index] == 0) return 0;
        if (hotKind[index] == 2) return InventoryDlg.ItemCount(hotSkill[index]);
        if (hotKind[index] == 1)
        {
            if (CharData.petSkills != null)
                foreach (CharData.PetSkill skill in CharData.petSkills)
                    if (skill.id == hotSkill[index]) return skill.level;
            return 0;
        }
        return CharData.skills.TryGetValue(hotSkill[index], out int level) ? level : 0;
    }

    internal static void DrawHotSlotCountForIndependentRuntime(int index, Rect icon, float scale)
    {
        if (hotSkill[index] == 0) return;
        int count = HotSlotNumber(index);
        if (hotKind[index] == 2 || count >= 1)
        {
            int kind = hotKind[index];
            string text = count.ToString();
            Color color = kind == 2 && count < 1
                ? new Color32(255, 90, 90, 255)
                : new Color32(255, 230, 120, 255);
            DrawCountBadge(icon, text, color, scale);
        }
    }

}

}
