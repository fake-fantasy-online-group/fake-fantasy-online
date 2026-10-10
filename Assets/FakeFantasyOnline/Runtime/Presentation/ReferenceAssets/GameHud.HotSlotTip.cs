namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class GameHud
{
    // Official 1.5.8 token 06000440. The official owner reads the live hotbar arrays and the
    // CharData character/pet publications directly; hotSkill/hotKind and those publications are
    // the same state HotSlotNumber already consumes, so no extra aggregation is introduced.
    public static string HotSlotTip(int idx)
    {
        if (idx < 0) return null;

        int id = hotSkill[idx];
        if (id == 0) return null;

        switch (hotKind[idx])
        {
            case 2:
            {
                // Official 0x0600050b / 0x06000509. Empty name falls back to the 物品 literal,
                // the description is optional, and the stack suffix is always appended.
                (string name, string desc) = InventoryDlg.ItemInfo(id);
                string text = string.IsNullOrEmpty(name) ? "物品 " + id : name;
                if (!string.IsNullOrEmpty(desc)) text = text + "\n" + desc;
                return text + "\n数量: " + InventoryDlg.ItemCount(id);
            }

            case 1:
            {
                if (CharData.petSkills != null)
                {
                    foreach (CharData.PetSkill skill in CharData.petSkills)
                    {
                        if (skill.id != id) continue;
                        string level = skill.level > 0 ? "  Lv." + skill.level : string.Empty;
                        string body = string.IsNullOrEmpty(skill.desc) ? string.Empty : "\n" + skill.desc;
                        return skill.name + level + body;
                    }
                }
                return "宠物技能 " + id;
            }

            default:
            {
                // Official non-1/non-2 path: the learned level comes from CharData.skills, and a
                // null skill tip degrades to the 技能 literal instead of an empty tooltip.
                int level = 0;
                if (CharData.skills != null) { CharData.skills.TryGetValue(id, out level); }
                return Win05.SkillTipText(id, level) ?? ("技能 " + id);
            }
        }
    }
}

}
