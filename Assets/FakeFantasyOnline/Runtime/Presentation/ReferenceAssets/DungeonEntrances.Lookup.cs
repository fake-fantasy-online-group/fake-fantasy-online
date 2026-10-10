using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class DungeonEntrances
{
    private static Dictionary<string, Dungeon[]> byNpc;
    private static readonly HashSet<int> ExcludedMaps = new HashSet<int> { 34200 };

    // Official 1.5.8 06000297-06000299: retain source table order and exact NPC keys.
    private static void Ensure()
    {
        if (byNpc != null) return;
        byNpc = new Dictionary<string, Dungeon[]>();
        string text = GameTables.LoadText("dungeon_entrances");
        if (text == null) return;
        Root root = JsonUtility.FromJson<Root>(text);
        if (root == null || root.entries == null) return;
        foreach (Entry entry in root.entries)
        {
            if (string.IsNullOrEmpty(entry.npc) || entry.dungeons == null) continue;
            var available = new List<Dungeon>(entry.dungeons.Length);
            foreach (Dungeon dungeon in entry.dungeons)
                if (dungeon != null && !ExcludedMaps.Contains(dungeon.map))
                    available.Add(dungeon);
            byNpc[entry.npc] = available.ToArray();
        }
    }

    public static Dungeon[] For(string npcName)
    {
        Ensure();
        Dungeon[] result;
        return npcName != null && byNpc != null && byNpc.TryGetValue(npcName, out result)
            ? result : Array.Empty<Dungeon>();
    }
}
}
