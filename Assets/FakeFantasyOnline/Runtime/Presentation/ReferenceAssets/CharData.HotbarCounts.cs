using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{

    internal static void BindPetSkillsForIndependentRuntime(WorldPlayerPetStatusState pet)
    {
        // Receipt-owned roots must survive a stale frame, forced rebuild or adapter teardown.
        if (PetSnapshotReceiptOwned) return;
        if (pet == null) { petSkills = null; return; }
        var list = new List<PetSkill>();
        foreach (WorldPlayerPetSkillState skill in pet.Skills)
            list.Add(new PetSkill
            {
                id = skill.Id, icon = skill.IconId, name = skill.DisplayName,
                desc = skill.Description, fight = skill.Fight, active = skill.Active,
                level = skill.Level, max = skill.MaximumLevel
            });
        petSkills = list;
    }

    public struct PetSkill
    {
        public int id;
        public int icon;
        public string name;
        public string desc;
        public bool fight;
        public bool active;
        public int level;
        public int max;
    }


}

}
