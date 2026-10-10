using PetBrief = FakeFantasyOnline.Application.PetBrief;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal static partial class CharData
{
    // Original zero-initialized scalar fields, not a second receipt mirror.
    internal static byte petGender, petHabit;
    internal static int petStr, petVit, petAgi, petInt, petDex, petSpi;
    internal static int petMinAtk, petMaxAtk, petDef, petMatk, petMdef, petHit;
    internal static int petPpAi, petPpSheng, petSanctified;
    internal static bool PetSnapshotReceiptOwned { get; private set; }

    internal static void ReceivePetSnapshotForIndependentRuntime(IWorldPetSnapshotReader reader, bool publishTips)
    {
        petModel = reader.I32();
        PetSnapshotReceiptOwned = true;
        string fallbackName = reader.Str();
        petPrename = reader.Str();
        string preferredName = reader.Str();
        petName = string.IsNullOrEmpty(preferredName) ? fallbackName : preferredName;
        petGender = (byte)reader.U8();
        petHabit = (byte)reader.U8();
        petLevel = reader.I32();
        petExp = reader.I32();
        petExpUp = reader.I32();
        petStarve = reader.I32();
        petTrust = reader.I32();
        petPoints = reader.I32();
        petCurHp = reader.I32();
        petMaxHp = reader.I32();
        petCurSp = reader.I32();
        petMaxSp = reader.I32();
        petStr = reader.I32();
        petVit = reader.I32();
        petAgi = reader.I32();
        petInt = reader.I32();
        petDex = reader.I32();
        petSpi = reader.I32();
        petMinAtk = reader.I32();
        petMaxAtk = reader.I32();
        petDef = reader.I32();
        petMatk = reader.I32();
        petMdef = reader.I32();
        petHit = reader.I32();
        petActiveSkill = reader.I32();
        reader.U8();
        int activeSlot = reader.U8();
        int skillCount = reader.U8();
        var skills = new List<PetSkill>();
        for (int i = 0; i < skillCount; i++)
        {
            var skill = new PetSkill
            {
                id = reader.I32(), name = reader.Str(), icon = reader.I32(),
                desc = reader.Str(), fight = reader.U8() != 0, active = reader.U8() != 0,
                level = reader.I32(), max = reader.I32()
            };
            skills.Add(skill);
        }
        petSkills = skills;
        foreach (PetSkill skill in skills)
            if (skill.icon > 0) petSkillIcons[skill.id] = skill.icon;

        int count = reader.U8();
        var bag = new List<int>();
        var names = new List<string>();
        var slots = new List<PetBrief>();
        for (int i = 0; i < count; i++)
        {
            PetBrief pet = reader.ReadPetBrief();
            slots.Add(pet);
            bag.Add(pet.model);
            names.Add(pet.nickname);
        }
        petBag = bag;
        petBagNames = names;
        petSlots = slots;
        petActive = activeSlot;
        petShow = reader.U8() != 0;
        petArousal = reader.I32();
        petPpAi = reader.I32();
        petPpSheng = reader.I32();
        petSanctified = reader.U8();
        petRidable = reader.U8() != 0;
        petTianmo = reader.U8() != 0;
        petShowTianmo = reader.U8() != 0;
        petBound = reader.Remaining >= 1 && reader.U8() != 0;

        if (reader.Remaining >= 1)
        {
            int lockedCount = reader.U8();
            for (int i = 0; i < lockedCount && reader.Remaining > 0; i++)
            {
                int locked = reader.U8();
                if (locked != 0 && i < slots.Count)
                {
                    PetBrief pet = slots[i];
                    pet.locked = true;
                    slots[i] = pet;
                }
            }
            petSlots = slots;
        }
        if (reader.Remaining >= 1)
        {
            int pointCount = reader.U8();
            for (int i = 0; i < pointCount && reader.Remaining >= 16; i++)
            {
                int aiUsed = reader.I32();
                int aiCapacity = reader.I32();
                int shengUsed = reader.I32();
                int shengCapacity = reader.I32();
                if (i < slots.Count)
                {
                    PetBrief pet = slots[i];
                    pet.ppAiUsed = aiUsed;
                    pet.ppAiCap = aiCapacity;
                    pet.ppShUsed = shengUsed;
                    pet.ppShCap = shengCapacity;
                    slots[i] = pet;
                }
            }
            petSlots = slots;
        }
        if (reader.Remaining >= 1) petPickQuality = reader.U8();
        if (reader.Remaining >= 2)
        {
            petFightCap = reader.U8();
            petLifeCap = reader.U8();
        }
        if (reader.Remaining >= 1) petGrowTier = reader.U8();
        if (reader.Remaining >= 1)
        {
            int growthCount = reader.U8();
            for (int i = 0; i < growthCount && reader.Remaining > 0; i++)
            {
                int tier = reader.U8();
                if (i < slots.Count)
                {
                    PetBrief pet = slots[i];
                    pet.growTier = tier;
                    slots[i] = pet;
                }
            }
            petSlots = slots;
        }
        petShowKnown = true;
        if (publishTips)
        {
            foreach (PetSkill skill in skills)
                if (skill.level > 0) { NewbieTipBox.Push(51); break; }
            foreach (PetSkill skill in skills)
                if (skill.id == 13002 && skill.level >= 1) { NewbieTipBox.Push(52); break; }
        }
    }

    // Explicit observation after all reached writes/callbacks, never their precondition.
    internal static WorldPlayerPetStatusState CapturePetSnapshotForIndependentRuntime()
    {
        var skills = new List<WorldPlayerPetSkillState>();
        if (petSkills != null)
            foreach (PetSkill skill in petSkills)
                skills.Add(WorldPlayerPetSkillState.FromReceipt(skill.id, skill.name, skill.icon,
                    skill.desc, skill.fight, skill.active, skill.level, skill.max));
        var slots = new List<WorldPlayerPetSlotState>();
        foreach (PetBrief p in petSlots)
        {
            var briefSkills = new List<WorldPlayerPetBriefSkillState>();
            if (p.skills != null)
                foreach (var skill in p.skills)
                    briefSkills.Add(new WorldPlayerPetBriefSkillState(skill.Item1, skill.Item2));
            slots.Add(new WorldPlayerPetSlotState(p.model, p.slot, p.nickname, p.prename, p.species,
                p.gender, p.habit, p.opened, p.level, p.str, p.intel, p.vit, p.agi, p.dex, p.spi,
                p.starve, p.trust, p.freePts, p.freeUsed, briefSkills, p.bound, p.locked,
                p.ppAiUsed, p.ppAiCap, p.ppShUsed, p.ppShCap, p.growTier));
        }
        return new WorldPlayerPetStatusState(petModel, petName, petLevel, petCurHp, petMaxHp,
            petCurSp, petMaxSp, petBound, petShow, petShowKnown, petRidable, petTianmo, petShowTianmo,
            skills, new WorldPlayerPetDetailState(petPrename, petGender, petHabit,
                petExp, petExpUp, petStarve, petTrust, petPoints,
                petStr, petVit, petAgi, petInt, petDex, petSpi,
                petMinAtk, petMaxAtk, petDef, petMatk, petMdef, petHit, petActiveSkill, petActive,
                petArousal, petPpAi, petPpSheng, petSanctified != 0), slots,
            petPickQuality, petFightCap, petLifeCap, petGrowTier);
    }
}
}
