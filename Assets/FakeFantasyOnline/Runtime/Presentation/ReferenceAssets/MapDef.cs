using System;
using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official-code-first recovery of the reference client's MapDef owner.
/// </summary>
/// <remarks>
/// Entry/byId/loaded and EnsureLoaded/Parse/TryGet/DecodeGbk are recovered from tokens
/// 1.5.8 tokens 060005B1..060005B4. ReferenceResourceLoader is the independent-runtime adapter for the
/// official PkgArchive/ResLoader source; parsing and lookup decisions remain in MapDef.
/// </remarks>
public static class MapDef
{
    public struct Entry
    {
        public int id;
        public string file;
        public string name;
        public bool isCity;
    }

    private static Dictionary<int, Entry> byId;
    private static bool loaded;

    public static void EnsureLoaded(ReferenceResourceLoader resources)
    {
        if (loaded)
        {
            return;
        }

        EnsureLoaded(resources == null ? null : resources.PrimaryPackageForIndependentRuntime);
    }

    public static void EnsureLoaded(PkgArchive pkg)
    {
        if (loaded)
            return;
        loaded = true;
        byId = new Dictionary<int, Entry>();
        Parse(pkg == null ? null : pkg.Read("map/map.def"));
        if (byId.Count == 0)
            Parse(ResLoader.Read24Res("map/map.def"));
    }

    private static void Parse(byte[] d)
    {
        if (d == null)
        {
            return;
        }

        string text = DecodeGbk(d);
        string[] lines = text
            .Replace("\r\n", "\n")
            .Replace('\r', '\n')
            .Split('\n');

        bool inMapDef = false;
        Entry current = default(Entry);
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            string line = lines[lineIndex].Trim();
            if (line.Length == 0 || line[0] == ';' || line[0] == '#')
            {
                continue;
            }

            if (line[0] == '[')
            {
                if (inMapDef && current.id > 0)
                {
                    byId[current.id] = current;
                }

                current = default(Entry);
                inMapDef = line.Equals("[MapDef]", StringComparison.OrdinalIgnoreCase);
                continue;
            }

            if (!inMapDef)
            {
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator < 1)
            {
                continue;
            }

            string key = line.Substring(0, separator).Trim().ToLowerInvariant();
            string value = line.Substring(separator + 1).Trim();
            int comment = value.IndexOf(';');
            if (comment >= 0)
            {
                value = value.Substring(0, comment).Trim();
            }

            if (key == "mapid")
            {
                int.TryParse(value, out current.id);
            }
            else if (key == "file")
            {
                current.file = value;
            }
            else if (key == "name")
            {
                current.name = value;
            }
            else if (key == "iscity")
            {
                current.isCity = value == "1";
            }
        }

        if (inMapDef && current.id > 0)
        {
            byId[current.id] = current;
        }
    }

    public static bool TryGet(int id, out Entry e)
    {
        if (byId != null)
        {
            return byId.TryGetValue(id, out e);
        }

        e = default(Entry);
        return false;
    }

    private static string DecodeGbk(byte[] b)
    {
        return Gbk.GetString(b);
    }
}

}
