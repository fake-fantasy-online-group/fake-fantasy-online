using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class NewbieTipBox : MonoBehaviour
{
    private class Tip
    {
        public string text;
        public float start;
    }

    [Serializable]
    private class Row
    {
        public int id;
        public string text;
    }

    [Serializable]
    private class Wrap
    {
        public Row[] list;
    }

    public static NewbieTipBox I;
    private const float SHOW_SEC = 12f, FADE = 0.5f, BOX_W = 290f, MAXN = 5f;
    private readonly List<Tip> active = new List<Tip>();
    private static HashSet<int> shownIds;
    private static string shownFor;
    private static Dictionary<int, string> tips;

    // Original EnsureShown 060008d6.
    private static void EnsureShown()
    {
        string character = CharData.name ?? string.Empty;
        if (shownIds != null && shownFor == character) return;
        shownFor = character;
        shownIds = new HashSet<int>();
        foreach (string value in PlayerPrefs.GetString("nbtip_" + character, string.Empty).Split(','))
            if (int.TryParse(value, out int id)) shownIds.Add(id);
    }

    // Original SeedServer 060008d7.
    public static void SeedServer(List<int> ids)
    {
        EnsureShown();
        bool changed = false;
        foreach (int id in ids) changed |= shownIds.Add(id);
        if (changed)
        {
            PlayerPrefs.SetString("nbtip_" + shownFor, string.Join(",", shownIds));
            PlayerPrefs.Save();
        }
    }

    // Original EnsureTips 060008db.
    private static void EnsureTips()
    {
        if (tips != null) return;
        tips = new Dictionary<int, string>();
        string json = GameTables.LoadText("tips");
        if (json == null) return;
        Wrap table = JsonUtility.FromJson<Wrap>(json);
        if (table == null || table.list == null) return;
        foreach (Row row in table.list)
            if (!string.IsNullOrEmpty(row.text)) tips[row.id] = row.text;
    }

    // Original Push 060008d8: persist and notify before looking up display text.
    public static void Push(int id)
    {
        EnsureShown();
        if (shownIds.Contains(id)) return;
        shownIds.Add(id);
        PlayerPrefs.SetString("nbtip_" + shownFor, string.Join(",", shownIds));
        PlayerPrefs.Save();
        if (NetClient.I != null) NetClient.I.TipSeenForIndependentRuntime(id);
        EnsureTips();
        if (tips.TryGetValue(id, out string text)) PushText(text);
    }

    private static string Strip(string s) => RichCode.Strip(s);
}

}
