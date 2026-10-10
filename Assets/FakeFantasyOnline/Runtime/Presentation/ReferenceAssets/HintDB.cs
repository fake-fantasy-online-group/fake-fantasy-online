using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static class HintDB
{
    [Serializable]
    private class Row
    {
        public int task;
        public int step;
        public string text;
    }

    [Serializable]
    private class Wrap
    {
        public Row[] list;
    }

    private static Dictionary<int, List<Row>> byTask;
    private static readonly StringBuilder sb = new StringBuilder(128);

    public static string CurrentLine(int taskId, int cur, int total)
    {
        Ensure();
        List<Row> list;
        if (!byTask.TryGetValue(taskId, out list))
        {
            return null;
        }
        int count = list.Count;
        if (count == 0)
        {
            return null;
        }
        int index;
        if (count < 2)
        {
            index = 0;
        }
        else if (total < 1)
        {
            index = cur < 1 ? 0 : count - 1;
        }
        else if (cur < total)
        {
            index = Mathf.FloorToInt((float)cur * (count - 1) / (float)total);
            index = Mathf.Clamp(index, 0, count - 1);
        }
        else
        {
            index = count - 1;
        }
        return Strip(list[index].text);
    }

    public static List<string> AllLines(int taskId)
    {
        Ensure();
        List<Row> list;
        if (!byTask.TryGetValue(taskId, out list))
        {
            return null;
        }
        int count = list.Count;
        if (count == 0)
        {
            return null;
        }
        List<string> result = new List<string>(count);
        foreach (Row row in list)
        {
            result.Add(Strip(row.text));
        }
        return result;
    }

    private static void Ensure()
    {
        if (byTask != null)
        {
            return;
        }
        byTask = new Dictionary<int, List<Row>>();
        string json = GameTables.LoadText("hints");
        if (json == null)
        {
            return;
        }
        Wrap wrap = JsonUtility.FromJson<Wrap>(json);
        if (wrap == null || wrap.list == null)
        {
            return;
        }
        Row[] rows = wrap.list;
        for (int i = 0; i < rows.Length; i++)
        {
            Row row = rows[i];
            List<Row> list;
            if (!byTask.TryGetValue(row.task, out list))
            {
                list = new List<Row>();
                byTask.Add(row.task, list);
            }
            list.Add(row);
        }
        foreach (List<Row> list in byTask.Values)
        {
            list.Sort((a, b) => a.step - b.step);
        }
    }

    private static string Strip(string s)
    {
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }
        sb.Clear();
        int i = 0;
        while (i < s.Length)
        {
            if (s[i] == '/' && i + 1 < s.Length)
            {
                char next = s[i + 1];
                if (next == '[')
                {
                    int close = s.IndexOf(']', i);
                    if (close >= 0)
                    {
                        i = close + 1;
                        continue;
                    }
                }
                else if (next == 'C' && i + 3 < s.Length)
                {
                    i += 4;
                    continue;
                }
            }
            sb.Append(s[i]);
            i++;
        }
        return sb.ToString();
    }
}

}
