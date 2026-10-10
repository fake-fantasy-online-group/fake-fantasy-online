using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static partial class CharData
    {

        internal static void BindPetSlotsForIndependentRuntime(IReadOnlyList<WorldPlayerPetSlotState> rows)
        {
            // Receipt-owned roots must survive a stale frame, forced rebuild or adapter teardown.
            if (PetSnapshotReceiptOwned) return;
            petSlots = new List<PetBrief>(rows == null ? 0 : rows.Count);
            if (rows == null) return;
            foreach (WorldPlayerPetSlotState row in rows)
            {
                petSlots.Add(ProjectPetSlotBriefForIndependentRuntime(row));
            }
        }

        // WBD-071 lane D: this list is the official root for the pet-slot hover
        // (Win05.DrawPet 06001043 reads CharData.petSlots[hovered] through List.get_Item) and for
        // GossipDlg.BuildLocalShares 060005e4, which likewise addresses reference.Item3 as a list
        // index rather than matching PetBrief.slot. ReferenceFirstWorldWindowOwner rebinds it here
        // from the same immutable rows whenever an authoritative status version is applied, so the
        // row order of the projection is what those two official readers address.
        internal static PetBrief ProjectPetSlotBriefForIndependentRuntime(WorldPlayerPetSlotState row)
        {
            var skills = new List<(string, int)>();
            foreach (WorldPlayerPetBriefSkillState skill in row.Skills) skills.Add((skill.DisplayName, skill.Level));
            return new PetBrief
            {
                model = row.Model, slot = row.Slot,
                nickname = row.Nickname, prename = row.Prename, species = row.Species,
                gender = row.Gender, habit = row.Habit, opened = row.Opened, level = row.Level,
                str = row.Strength, intel = row.Intelligence, vit = row.Vitality,
                agi = row.Agility, dex = row.Dexterity, spi = row.Spirit,
                starve = row.Starve, trust = row.Trust, freePts = row.FreePoints,
                freeUsed = row.UsedPoints, skills = skills, bound = row.Bound,
                locked = row.Locked, ppAiUsed = row.PpAiUsed, ppAiCap = row.PpAiCapacity,
                ppShUsed = row.PpShengUsed, ppShCap = row.PpShengCapacity, growTier = row.GrowthTier
            };
        }
    }
}
