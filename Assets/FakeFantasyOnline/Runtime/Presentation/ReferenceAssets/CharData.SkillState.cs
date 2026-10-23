using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal static partial class CharData
{

    // Official TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391b34 / x86_64 0x3a4e70) publish of
    // the same static: DateTime.Now.ToString("yyyy-MM-dd HH:mm"). arm64 0x391e7c bl get_Now
    // (0xe9144c), 0x391e90 bl ToString(System.String) (0xe91bac) with metadata usage ordinal
    // 16594, whose frozen decode state is exact with the value "yyyy-MM-dd HH:mm", then
    // 0x391eb0 str x21,[x0,#0x28]! plus the write barrier at 0x391eb4-0x391eb8; x86_64 0x3a50f3
    // call get_Now (0xe90240), 0x3a510c call ToString(System.String) (0xe90980), 0x3a5134 mov
    // [rdi+0x28],rax plus the write barrier at 0x3a5138-0x3a513f. The overload is resolved by
    // the labels table (arm64 15276972 = ToString(System.String format), not the no-argument,
    // IFormatProvider or two-argument overload), so the separators come from the current
    // culture and DateTime.Now is the local wall clock - no provider is passed and no UTC shift
    // happens. Nothing else is stored with it and no branch guards it.
    internal static void SetEntryLastLoginForIndependentRuntime()
    {
        lastLogin = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }
    internal static int mp;
    // The other three scalars of the same official seven-field block, pinned by the frozen 1.5.8
    // field atlas and not by any gameplay naming: CharData.maxMp is System.Int32 at instance
    // offset 100 (+0x64), CharData.exp at 104 (+0x68) and CharData.expToNext at 108 (+0x6c).
    // Official CharData..cctor (0x06000f2d, arm64 0x395b74 / x86_64 0x3a8ea0, 1724/1936 bytes)
    // does not write these three fields. They retain the language's zero default; the complete
    // native-written initializer sequence is declared in CharData.Initializers.cs.
    internal static int maxMp;
    internal static int exp;
    internal static int expToNext;
    internal static int skillPoints;
    internal static int[] attrs;
    // Second positional attribute array. Identity is pinned by the frozen 1.5.8 field atlas,
    // not by any product parameter name: CharData.attrs is System.Int32[] at instance offset
    // 176 (+0xb0) and CharData.attrsTotal is System.Int32[] at instance offset 184 (+0xb8).
    // The official readers (CombatWorld.get_AtkInterval 0600012c, CombatWorld.get_AtkSpeedMul
    // 06000128, EquipDlg 0600030d) all take +0xb8 first and fall back to +0xb0 only when it is
    // null, so this one is the preferred array and attrs is the fallback.
    internal static int[] attrsTotal;
    internal static int weaponCType;
    internal static int glowUnlock;
    internal static int atkVariant;
    internal static int atkDist;
    internal static int? nativeAtkDist;
    internal static int AttackIdentityDistance => nativeAtkDist ?? atkDist;

    // Official 0x8015 reached stores: points before count; dictionary replacement after count;
    // each fully read pair survives the next failed read; bonuses replace only after all
    // required rows. The reader's progress is applied outside its wire-exception catch.
    internal static WorldSkillBookState ReceiveSkillsForIndependentRuntime(WorldSkillBookReceipt receipt)
    {
        if (receipt == null) throw new ArgumentNullException(nameof(receipt));
        skillPoints = receipt.SkillPoints;
        if (receipt.Skills != null)
        {
            skills = new Dictionary<int, int>();
            foreach (WorldLearnedSkillState row in receipt.Skills) skills[row.Id] = row.Level;
        }
        if (receipt.DamageBonuses != null)
        {
            skillDmgBonus = new Dictionary<int, int>();
            foreach (var pair in receipt.DamageBonuses) skillDmgBonus[pair.Key] = pair.Value;
        }
        var rows = new List<WorldLearnedSkillState>(skills.Count);
        foreach (var pair in skills) rows.Add(new WorldLearnedSkillState(pair.Key, pair.Value));
        return new WorldSkillBookState(skillPoints, rows, skillDmgBonus);
    }

    // Native 0x8015: each read is followed by its reached store before the next read.
    // Reuse the original dictionaries as observation data after receipt; later packets
    // replace those roots rather than modifying an older observation's contents.
    internal static void ReceiveSkillsForIndependentRuntime(ISkillBookReader reader,
        out WorldSkillBookObservation observation)
    {
        observation = default;
        bool pointsRead = false;
        try
        {
            skillPoints = reader.I32();
            pointsRead = true;
            int count = reader.U16();
            skills = new Dictionary<int, int>();
            for (int index = 0; index < count; index++)
            {
                int id = reader.I32();
                int level = reader.I32();
                skills[id] = level;
            }
            skillDmgBonus = new Dictionary<int, int>();
            if (reader.Remaining >= 2)
            {
                int bonusCount = reader.U16();
                for (int index = 0; index < bonusCount && reader.Remaining >= 8; index++)
                {
                    int id = reader.I32();
                    int bonus = reader.I32();
                    skillDmgBonus[id] = bonus;
                }
            }
        }
        finally
        {
            if (pointsRead)
                observation = WorldSkillBookObservation.FromNativeRoots(
                    skillPoints, skills, skillDmgBonus);
        }
    }

    // Retained for explicit Editor/setup callers; runtime receive now uses the reached-store
    // owner above and UI/command projectors never call this helper. The null book
    // argument has no official counterpart (official never binds "no book"), so the null branch
    // below keeps whatever it did before this package - see the WBB-060 report, pending #2.
    internal static void BindSkillsForIndependentRuntime(WorldSkillBookState book)
    {
        skillPoints = book == null ? 0 : book.SkillPoints;
        skills = book == null ? null : new Dictionary<int, int>();
        skillDmgBonus = new Dictionary<int, int>();
        if (book != null)
        {
            foreach (var pair in book.DamageBonuses) skillDmgBonus[pair.Key] = pair.Value;
            foreach (WorldLearnedSkillState skill in book.Skills)
                skills[skill.Id] = skill.Level;
        }
    }
}

}
