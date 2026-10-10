using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official SkillEffectDB owner.
/// </summary>
public partial class SkillEffectDB
{
    public struct Ph
    {
        public int index;
        public int delay;
        public int effect;
    }

    public class Entry
    {
        public int effect;
        public List<Ph> fly = new List<Ph>();
        public List<Ph> atk = new List<Ph>();
        public List<Ph> hit = new List<Ph>();
    }

    private static Dictionary<int, Entry> map;
    private static HashSet<int> physical;

    public static readonly bool Use05Fx;

    public static Entry Get(int skill)
    {
        Ensure();
        Entry result;
        return map != null && map.TryGetValue(skill, out result) ? result : null;
    }

    public static bool IsPhysical(int skill)
    {
        Ensure();
        return physical != null && physical.Contains(skill);
    }

    internal static int EntryCountForAudit
    {
        get
        {
            Ensure();
            return map == null ? 0 : map.Count;
        }
    }

    private static void Ensure()
    {
        if (map != null)
        {
            return;
        }

        map = new Dictionary<int, Entry>();
        physical = new HashSet<int>();
        string tableName = Use05Fx ? "skillshow05" : "skillshow";
        string text = GameTables.LoadText(tableName);
        if (text == null)
        {
            Debug.LogWarning("[SkillEffectDB] " + tableName + ".json 缺失");
            return;
        }

        JFile file = JsonUtility.FromJson<JFile>(text);
        if (file == null || file.shows == null)
        {
            return;
        }

        for (int rowIndex = 0; rowIndex < file.shows.Length; rowIndex++)
        {
            // Official IL/arm64/x86_64 dereference every row and phase without a preceding
            // filter: the IL2CPP null guard throws instead of skipping, and neither the row id
            // nor the phase effect is compared against zero.
            JShow row = file.shows[rowIndex];
            if (row.phy == 1)
            {
                physical.Add(row.id);
            }

            Entry entry = new Entry();
            if (row.ph != null)
            {
                for (int phaseIndex = 0; phaseIndex < row.ph.Length; phaseIndex++)
                {
                    JPh source = row.ph[phaseIndex];
                    if (entry.effect == 0)
                    {
                        entry.effect = source.e;
                    }

                    Ph phase = new Ph
                    {
                        index = source.s,
                        delay = source.d,
                        effect = source.e,
                    };
                    if (source.s == 50 || source.s == 30)
                    {
                        entry.hit.Add(phase);
                    }
                    else if (source.s == 20)
                    {
                        entry.fly.Add(phase);
                    }
                    else
                    {
                        entry.atk.Add(phase);
                    }
                }
            }

            if (entry.fly.Count + entry.atk.Count + entry.hit.Count > 0 &&
                !map.ContainsKey(row.id))
            {
                map.Add(row.id, entry);
            }
        }
    }
}

}
