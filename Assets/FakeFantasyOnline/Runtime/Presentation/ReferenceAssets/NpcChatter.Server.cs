using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NpcChatter
{
    private static Dictionary<string, List<string>> byNpc;

    // Official 1.5.8 token 0600090f.
    public static void SetServerChatter(
        List<(string npcName, string role, string text)> lines,
        int enabled, int minGap, int maxGap, int showSec)
    {
        EnsureData();
        byRole = new Dictionary<string, List<string>>();
        all = new List<string>();
        ovGreets = new List<string>();
        ovByRole = new Dictionary<string, List<string>>();
        byNpc = new Dictionary<string, List<string>>();
        if (lines != null)
        {
            foreach (var row in lines)
            {
                if (string.IsNullOrEmpty(row.text)) continue;
                if (string.IsNullOrEmpty(row.npcName))
                {
                    all.Add(row.text);
                    ovGreets.Add(row.text);
                    if (!string.IsNullOrEmpty(row.role))
                    {
                        if (!byRole.TryGetValue(row.role, out List<string> list))
                        {
                            list = new List<string>();
                            byRole[row.role] = list;
                        }
                        list.Add(row.text);
                    }
                }
                else
                {
                    foreach (string name in row.npcName.Split(','))
                    {
                        string key = name.Trim();
                        if (key.Length == 0) continue;
                        if (!byNpc.TryGetValue(key, out List<string> list))
                        {
                            list = new List<string>();
                            byNpc[key] = list;
                        }
                        list.Add(row.text);
                    }
                }
            }
        }
        sEnabled = enabled != 0;
        if (minGap > 0) sMinGap = minGap;
        if (maxGap > 0) sMaxGap = maxGap;
        if (showSec > 0) sShowSec = showSec;
    }
}

}
