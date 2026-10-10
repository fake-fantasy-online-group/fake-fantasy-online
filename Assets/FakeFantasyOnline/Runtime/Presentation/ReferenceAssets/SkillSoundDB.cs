using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official SkillSoundDB table and playback dispatch owner.
/// </summary>
public static partial class SkillSoundDB
{
    private struct Entry
    {
        public int cast;
        public int cd;
        public int[] fire;
        public int[] fd;
    }

    public static readonly bool Use24All;

    private static Dictionary<int, Entry> map;
    private static ReferenceWorldCombatSound runtimeSound;

    internal static void BindIndependentRuntime(ReferenceWorldCombatSound sound)
    {
        runtimeSound = sound;
        Ensure();
    }

    internal static void UnbindIndependentRuntime(ReferenceWorldCombatSound sound)
    {
        if (ReferenceEquals(runtimeSound, sound))
        {
            runtimeSound = null;
        }
    }

    internal static int EntryCountForAudit
    {
        get
        {
            Ensure();
            return map == null ? 0 : map.Count;
        }
    }

    public static void PlayCast(int skillId)
    {
        Ensure();
        Entry entry;
        if (map != null && map.TryGetValue(skillId, out entry) && entry.cast > 0)
        {
            Play(entry.cast, entry.cd);
        }
    }

    public static void PlayFire(int skillId)
    {
        Ensure();
        Entry entry;
        if (map == null || !map.TryGetValue(skillId, out entry) || entry.fire == null)
        {
            return;
        }

        for (int index = 0; index < entry.fire.Length; index++)
        {
            int soundId = entry.fire[index];
            if (soundId > 0)
            {
                int delay = entry.fd != null && index < entry.fd.Length
                    ? entry.fd[index]
                    : 0;
                Play(soundId, delay);
            }
        }
    }

    internal static void Play(int soundId, int delayMilliseconds)
    {
        SoundManager.Play(soundId, delayMilliseconds);
    }

    private static void Ensure()
    {
        if (map != null)
        {
            return;
        }

        map = new Dictionary<int, Entry>();
        string tableName = Use24All ? "skillsound24" : "skillsound";
        string text = GameTables.LoadText(tableName);
        if (text == null)
        {
            Debug.LogWarning("[SkillSoundDB] " + tableName + ".json 缺失");
            return;
        }

        JFile file = JsonUtility.FromJson<JFile>(text);
        if (file == null || file.sounds == null)
        {
            return;
        }

        for (int index = 0; index < file.sounds.Length; index++)
        {
            // Official arm64/x86_64 dereference every row before the dictionary lookup and
            // never compare the id with zero: the null paths reach LAB_003204f4 (arm64) and
            // LAB_003336f4 (x86_64), which call FUN_00177a74 / FUN_0016c1b0 and do not return,
            // so a null row throws rather than being skipped and rows whose id is not positive
            // are still inserted first-come-first-served.
            JSnd row = file.sounds[index];
            if (map.ContainsKey(row.id))
            {
                continue;
            }

            map.Add(
                row.id,
                new Entry
                {
                    cast = row.cast,
                    cd = row.cd,
                    fire = row.fire,
                    fd = row.fd,
                });
        }
    }
}

}
