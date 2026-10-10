using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 chat emote expansion: a "/word" token whose letters are all lowercase is
// replaced by the two-digit escape \eNN taken from the emote_codes table.
public static class EmoteCodes
{
    private static Dictionary<string, int> map;
    private static bool tried;

    [Serializable]
    private class Json
    {
        public string[] code;
        public int[] num;
    }

    // 060002e5: load the table once, whether or not the load succeeds.
    private static void Ensure()
    {
        if (tried) return;
        tried = true;
        string text = GameTables.LoadText("emote_codes");
        if (text == null) return;
        Json json = JsonUtility.FromJson<Json>(text);
        if (json == null || json.code == null || json.num == null) return;
        map = new Dictionary<string, int>(json.code.Length);
        for (int i = 0; i < json.code.Length && i < json.num.Length; i++)
            map[json.code[i]] = json.num[i];
    }

    // 060002e6
    public static string Expand(string text)
    {
        Ensure();
        if (map == null || string.IsNullOrEmpty(text)) return text;
        if (text.IndexOf('/') < 0) return text;

        var sb = new StringBuilder(text.Length + 8);
        int i = 0;
        while (i < text.Length)
        {
            if (text[i] != '/' || i + 1 >= text.Length)
            {
                sb.Append(text[i]);
                i++;
                continue;
            }
            int end = i + 1;
            while (end < text.Length && text[end] >= 'a' && text[end] <= 'z')
                end++;
            int code;
            if (end > i + 1 && map.TryGetValue(text.Substring(i + 1, end - i - 1), out code))
            {
                sb.Append("\\e");
                sb.Append(code.ToString("D2"));
                i = end;
                continue;
            }
            sb.Append(text[i]);
            i++;
        }
        return sb.ToString();
    }
}

}
