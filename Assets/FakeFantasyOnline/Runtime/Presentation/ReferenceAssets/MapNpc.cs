using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

// Official 1.5.8 map NPC table parser. The table is GBK text made of "[Npc]" sections whose
// "key=value" lines fill an NpcEntry; a section is flushed when the next bracket opens and
// once more at the end of the file.
public static class MapNpc
{
    // 060005c1
    public static List<NpcEntry> Parse(byte[] bytes, PkgArchive pkg)
    {
        var list = new List<NpcEntry>();
        if (bytes == null || bytes.Length == 0) return list;

        string text = Gbk.GetString(bytes);
        text = text.Replace("\r\n", "\n");
        text = text.Replace('\r', '\n');

        string[] lines = text.Split('\n');

        NpcEntry cur = null;
        int index = 0;
        int refreshType = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (line.Length == 0 || line[0] == '#' || line[0] == ';') continue;

            if (line[0] == '[')
            {
                FlushAndReset(list, ref cur, ref index, ref refreshType, pkg);
                if (string.Equals(line, "[Npc]", StringComparison.OrdinalIgnoreCase))
                {
                    refreshType = 0;
                    index = 0;
                    cur = new NpcEntry();
                }
                continue;
            }

            if (cur == null) continue;
            int split = line.IndexOf('=');
            if (split <= 0) continue;

            string key = line.Substring(0, split).Trim();
            string value = line.Substring(split + 1).Trim();
            switch (key.ToLowerInvariant())
            {
                case "name":
                    cur.name = value;
                    break;
                case "init_pos":
                    string[] pair = value.Split(',');
                    float x;
                    float y;
                    if (pair.Length > 1 && float.TryParse(pair[0].Trim(), out x)
                        && float.TryParse(pair[1].Trim(), out y))
                    {
                        cur.x = x;
                        cur.y = y;
                    }
                    break;
                case "script":
                    cur.script = value;
                    break;
                case "selllist":
                    int.TryParse(value, out cur.sell);
                    break;
                case "translist":
                    int.TryParse(value, out cur.trans);
                    break;
                case "index":
                    int.TryParse(value, out index);
                    break;
                case "refreshtype":
                    int.TryParse(value, out refreshType);
                    break;
            }
        }

        FlushAndReset(list, ref cur, ref index, ref refreshType, pkg);
        return list;
    }

    // 060005c2: only a numbered entry with no refresh rule is kept; the three locals are
    // always cleared afterwards.
    private static void FlushAndReset(List<NpcEntry> list, ref NpcEntry cur, ref int index,
        ref int refreshType, PkgArchive pkg)
    {
        if (cur != null && index > 0 && refreshType == 0)
        {
            cur.sprite = "npc" + index;
            string portrait = string.Empty;
            if (pkg != null && pkg.Has("res/dlgpic/N" + index + ".gsa"))
                portrait = "N" + index;
            cur.portrait = portrait;
            cur.label = "nm" + list.Count;
            list.Add(cur);
        }
        cur = null;
        index = 0;
        refreshType = 0;
    }

    // 060005c3
    public static string DecodeGbk(byte[] bytes)
    {
        return Gbk.GetString(bytes);
    }
}

}
