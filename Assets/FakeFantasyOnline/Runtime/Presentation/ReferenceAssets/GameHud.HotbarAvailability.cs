using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    private static bool HotSlotUsable(int index)
    {
        if (hotSkill[index] == 0) return false;
        if (hotKind[index] == 2) return InventoryDlg.ItemCount(hotSkill[index]) > 0;
        if (hotKind[index] == 1)
        {
            if (CharData.petSkills != null)
                foreach (CharData.PetSkill skill in CharData.petSkills)
                    if (skill.id == hotSkill[index]) return skill.level > 0;
            return false;
        }
        return CharData.skills != null &&
            CharData.skills.TryGetValue(hotSkill[index], out int level) && level > 0;
    }

    internal static bool HotSlotUsableForIndependentRuntime(int index) => HotSlotUsable(index);

    // Original SlotsRow icon/disabled-appearance body. The item inset reads this receiver's
    // current s at the reached item branch; only slot geometry keeps the captured sh.
    internal void DrawHotSlotIconForIndependentRuntime(int index, Rect image, Texture2D icon,
        bool usable, float slotScale)
    {
        if (icon != null)
        {
            if (!usable) GUI.color = new Color(1f, 1f, 1f, 0.4f);
            if (hotKind[index] == 2)
            {
                float itemInset = s;
                ItemIconUI.Draw(new Rect(image.x + itemInset, image.y + itemInset,
                    28f * slotScale, 28f * slotScale), icon, slotScale);
            }
            else GUI.DrawTexture(image, icon);
            GUI.color = Color.white;
            if (!usable)
            {
                GUI.color = new Color(0.15f, 0.15f, 0.15f, 0.5f);
                GUI.DrawTexture(image, Texture2D.whiteTexture);
                GUI.color = Color.white;
            }
        }
    }
}

}
