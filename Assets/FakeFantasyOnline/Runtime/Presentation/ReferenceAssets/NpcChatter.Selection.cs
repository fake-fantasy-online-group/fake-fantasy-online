using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter
{
    // Official 1.5.8 token 06000910.
    public static string GetLine(string role)
    {
        EnsureData();
        if (byRole != null && byRole.TryGetValue(role, out List<string> lines) && lines.Count > 0)
            return lines[Random.Range(0, lines.Count)];
        if (all != null && all.Count > 0)
            return all[Random.Range(0, all.Count)];
        return string.Empty;
    }

    // Official 1.5.8 token 06000912.
    public static string OwnGreeting(string npcName)
    {
        EnsureData();
        if (string.IsNullOrEmpty(npcName) || npcName.Length <= 1) return null;
        if (ovGreets != null)
        {
            foreach (string line in ovGreets)
                if (line.Contains(npcName)) return line;
        }
        return null;
    }

    // Official 1.5.8 token 06000913.
    public static string GetGreeting(string npcName, string role)
    {
        EnsureData();
        string own = OwnGreeting(npcName);
        if (own != null) return own;
        if (ovByRole != null && ovByRole.TryGetValue(role, out List<string> lines) && lines.Count > 0)
            return lines[Random.Range(0, lines.Count)];
        return GetLine(role);
    }
}

}
