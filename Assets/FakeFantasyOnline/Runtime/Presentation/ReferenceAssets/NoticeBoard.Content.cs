using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official 1.5.8 NoticeBoard content state and formatting. Resource loading,
/// fetching and the history-window notice tab remain separate recovery work.
/// </summary>
public static partial class NoticeBoard
{
    [Serializable]
    public class Entry
    {
        public string version;
        public string[] notes;
    }

    [Serializable]
    public class Root
    {
        public string version;
        public string[] notes;
        public Entry[] history;
    }

    public static string Version = "";
    public static readonly List<Entry> Versions = new List<Entry>();
    public static bool Loaded;
    public static int Stamp;

    // 0x060008e4. The source intentionally publishes only the current entry;
    // Root.history is not consumed by this method.
    public static void Publish(Root r)
    {
        if (r == null || r.notes == null || r.notes.Length == 0)
        {
            return;
        }

        Versions.Clear();
        Version = r.version ?? "";
        Versions.Add(new Entry { version = r.version, notes = r.notes });
        int stamp = unchecked(527 + Version.GetHashCode());
        foreach (string note in r.notes)
        {
            stamp = unchecked(stamp * 31 + (note == null ? 0 : note.GetHashCode()));
        }
        Stamp = stamp;
        Loaded = true;
    }

    // 0x060008e5. The native exception landing pad consumes parse/publication
    // exceptions; it does not clear previously published content.
    public static void PublishJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            return;
        }
        try
        {
            Publish(JsonUtility.FromJson<Root>(json));
        }
        catch
        {
        }
    }

    // 0x060008e8.
    public static string Strip(string s)
    {
        return string.IsNullOrEmpty(s) ? s : s.Replace("**", "");
    }

    // 0x060008e9.
    public static string Rich(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }

        var builder = new StringBuilder(s.Length + 16);
        bool bold = false;
        for (int i = 0; i < s.Length; i++)
        {
            if (i + 1 < s.Length && s[i] == '*' && s[i + 1] == '*')
            {
                builder.Append(bold ? "</b>" : "<b>");
                bold = !bold;
                i++;
            }
            else if (s[i] == '<')
            {
                builder.Append("&lt;");
            }
            else if (s[i] == '>')
            {
                builder.Append("&gt;");
            }
            else
            {
                builder.Append(s[i]);
            }
        }
        if (bold)
        {
            builder.Append("</b>");
        }
        return builder.ToString();
    }

    // 0x060008ea. Preserve the source's indexed traversal and raw note text.
    public static void ForEachLine(Action<string, bool> cb)
    {
        if (cb == null)
        {
            return;
        }
        if (!Loaded || Versions.Count == 0)
        {
            cb("(公告还没加载出来，稍等一下或重进一次)", false);
            return;
        }

        for (int i = 0; i < Versions.Count; i++)
        {
            Entry entry = Versions[i];
            cb("—— 更新公告 v" +
               (string.IsNullOrEmpty(entry.version) ? "?" : entry.version) + " ——", true);
            string[] notes = entry.notes;
            if (notes == null)
            {
                continue;
            }
            foreach (string note in notes)
            {
                if (!string.IsNullOrEmpty(note))
                {
                    cb("· " + note, false);
                }
            }
        }
    }
}
}
