using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal partial class InventoryDlg
{
    internal static PetBrief? ProjectPetForIndependentRuntime(WorldInventoryItemState item)
    {
        WorldInventoryPetState pet = item.Pet;
        if (pet == null) return null;
        PetBrief brief = ProjectPetBriefForIndependentRuntime(pet);
        brief.ppAiUsed = item.PetAiPointsUsed;
        brief.ppAiCap = item.PetAiPointsCapacity;
        brief.ppShUsed = item.PetShengPointsUsed;
        brief.ppShCap = item.PetShengPointsCapacity;
        brief.growTier = item.PetGrowthTier;
        return brief;
    }

    internal static PetBrief ProjectPetBriefForIndependentRuntime(WorldInventoryPetState pet)
    {
        var skills = new List<(string, int)>();
        foreach (WorldInventoryPetSkillState skill in pet.Skills)
            skills.Add((skill.DisplayName, skill.Level));
        return new PetBrief
        {
            model = pet.Model,
            slot = pet.Slot,
            nickname = pet.Nickname,
            prename = pet.Prename,
            species = pet.Species,
            gender = pet.Gender,
            habit = pet.Habit,
            opened = pet.OpenedValue,
            level = pet.Level,
            str = pet.Strength,
            intel = pet.Intelligence,
            vit = pet.Vitality,
            agi = pet.Agility,
            dex = pet.Dexterity,
            spi = pet.Spirit,
            starve = pet.Starvation,
            trust = pet.Trust,
            freePts = pet.FreePoints,
            freeUsed = pet.UsedFreePoints,
            skills = skills,
            bound = pet.Bound
        };
    }
}
}
