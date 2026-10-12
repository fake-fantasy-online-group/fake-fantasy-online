using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered Win05 skill-table ownership used by GameHud and SkillTargeting.
/// </summary>
public partial class Win05
{
    private static Dictionary<int, SkInfo> skInfo;
    private static SkLayout skLayout;
    private static bool skLoaded;

    // Official 0x06001020, static System.Void Win05.LoadSkillData(). arm64 rva 0x3ad4f8 (720
    // bytes) and x86_64 rva 0x3c00b0 (736 bytes) decode to one body over the Win05 static fields
    // (usage 02686; +0xb8 static_fields, so +0x00 skInfo, +0x08 skLayout, +0x10 skLoaded):
    //   * `if (skLoaded) return;` and `skLoaded = true` (arm64 :50 / x86_64 :47 and :52) - the flag
    //     is set BEFORE the two reads, so there is no retry: one failed pass leaves the layout null
    //     for the rest of the process;
    //   * `GameTables.LoadText("skilldlg_layout")` (usage 16077, 0600046f) and, when that text is
    //     non-null, `JsonUtility.FromJson<SkLayout>` stored into skLayout (arm64 :56-66);
    //   * `GameTables.LoadText("skills")` (usage 16079) and, again only when the text is non-null,
    //     a fresh `Dictionary<int, SkInfo>` published to skInfo before the rows are added, so a
    //     null/empty table still leaves a non-null dictionary (arm64 :67-83);
    //   * the final `if (skInfo == null) skInfo = new Dictionary<int, SkInfo>();` (arm64 :102-119)
    //     covers the "neither text was found" case, inside the same `skLoaded` arm.
    // No path throws on a missing file, a null parse result or an empty array: every one of them
    // just leaves the static field as it is.
    private static void LoadSkillData()
    {
        if (skLoaded)
        {
            return;
        }

        skLoaded = true;
        string layoutText = GameTables.LoadText("skilldlg_layout");
        if (layoutText != null)
        {
            skLayout = JsonUtility.FromJson<SkLayout>(layoutText);
        }

        string skillText = GameTables.LoadText("skills");
        if (skillText != null)
        {
            SkDB database = JsonUtility.FromJson<SkDB>(skillText);
            skInfo = new Dictionary<int, SkInfo>();
            if (database != null && database.skills != null)
            {
                for (int index = 0; index < database.skills.Length; index++)
                {
                    // Official 06001020 null-checks the element and the dictionary before it reads
                    // `id` (arm64 :92-95 with the non-returning helper 0x177a74; x86_64 :82-85 with
                    // FUN_0016c1b0), so a null row raises NullReferenceException instead of being
                    // skipped. The unguarded `skill.id` read below is that semantics: JsonUtility
                    // never emits a null element, but the `if (skill != null)` guard that used to
                    // stand here translated an official throw into a silent skip.
                    SkInfo skill = database.skills[index];
                    NativeSwordSkillTargeting.Apply(skill);
                    skInfo[skill.id] = skill;
                }
            }
        }

        if (skInfo == null)
        {
            skInfo = new Dictionary<int, SkInfo>();
        }
    }

    // Official 0x06001030, static System.Void Win05.LoadStatic(): the second, skills-only loader.
    // arm64 rva 0x3b25f4 (460 bytes, Ghidra C export
    // Build/ReferenceReverseGhidra-1.5.8/queries/wbb056-loadstatic-arm64/06001030.c) and
    // x86_64 rva 0x3c5750 (capstone over the frozen dylib) decode to the same body:
    //   * the guard is the FIELD, not the flag: `if (skInfo != 0) return;` reads Win05 static +0x00
    //     (arm64 :40/:43, x86_64 0x3c5781 / 0x3c57fe `cmp qword [rax], 0`). skLoaded (+0x10) is
    //     never read and never written here, so this loader neither consumes nor supplies the
    //     LoadSkillData 06001020 one-shot flag;
    //   * exactly ONE table: `GameTables.LoadText("skills")`. arm64 uses metadata_usage_16079_kind5
    //     and the export's usage set has no 16077 ("skilldlg_layout") and no 07911 (SkLayout type);
    //     on x86_64 the LoadText argument slot is 0x1491390, byte-identical to the *second*
    //     LoadText slot of LoadSkillData (0x3c0204 + 0x10db18c) and different from its first, so
    //     this is the "skills" text on both architectures;
    //   * `new Dictionary<int, SkInfo>()` (usage 00528 + ctor 03790) is published to skInfo and
    //     fenced by the write barrier BEFORE the text is even tested for null (arm64 :47-59,
    //     x86_64 0x3c581c-0x3c5861), so one failed pass still leaves a non-null dictionary and
    //     there is no retry - exactly like 06001020, but reached through the other guard;
    //   * rows: `FromJson<SkDB>` (07910) -> `db.skills` (+0x10) non-null and `Length` (+0x18) > 0,
    //     then `skInfo[skills[i].id] = skills[i]` in array order (usage 03792), so a later
    //     duplicate id still wins (arm64 :60-77, x86_64 0x3c5866-0x3c58e3);
    //   * a null row throws: arm64 :70-73 `if ((row == 0) || (skInfo == 0)) FUN_00177a74();` and
    //     x86_64 0x3c58b9 / 0x3c58c8 -> 0x3c58f4 `call 0x16c1b0`, the same non-returning helper
    //     FindSwitch 06001024 uses. The unguarded `skill.id` read below carries that semantics.
    // Nothing here throws on a missing file, a null parse result or an empty array either.
    private static void LoadStatic()
    {
        if (skInfo != null)
        {
            return;
        }

        string skillText = GameTables.LoadText("skills");
        skInfo = new Dictionary<int, SkInfo>();
        if (skillText != null)
        {
            SkDB database = JsonUtility.FromJson<SkDB>(skillText);
            if (database != null && database.skills != null)
            {
                for (int index = 0; index < database.skills.Length; index++)
                {
                    SkInfo skill = database.skills[index];
                    NativeSwordSkillTargeting.Apply(skill);
                    skInfo[skill.id] = skill;
                }
            }
        }
    }

    // Official 0x0600102f, static System.Int32 Win05.SkillIconId(System.Int32). Its native body
    // (arm64 rva 0x3b284c) calls `bl 0x3b25f4` - LoadStatic 06001030, not LoadSkillData 06001020 -
    // and only then reads Win05 static +0x00 (skInfo), returning 0 when it is null
    // (arm64 0x3b2894-0x3b28ac). The index agrees: 0600102f has the single dependency 06001030.
    // Calling LoadSkillData here instead pulled "skilldlg_layout" in as a side effect of drawing a
    // hotbar icon and, through its `skLoaded` flag, decided the layout's one-shot load at
    // hotbar-icon time - a second, earlier read point DrawSkill 06001025 does not have.
    public static int SkillIconId(int id)
    {
        if (AuthoritativeProfessionSkill.TryInfo(id, out SkInfo authoritative)) return authoritative?.icon ?? 0;
        LoadStatic();
        return skInfo != null && skInfo.TryGetValue(id, out SkInfo info) ? info.icon : 0;
    }

    public static SkInfo SkillInfo(int id)
    {
        if (AuthoritativeProfessionSkill.TryInfo(id, out SkInfo authoritative)) return authoritative;
        LoadSkillData();
        SkInfo result;
        return skInfo.TryGetValue(id, out result) ? result : null;
    }

    internal static int SkillInfoCountForAudit
    {
        get
        {
            LoadSkillData();
            return skInfo.Count;
        }
    }
}

}
