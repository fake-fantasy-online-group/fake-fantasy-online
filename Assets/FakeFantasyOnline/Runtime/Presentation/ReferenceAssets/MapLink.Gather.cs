using System;
using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public static partial class MapLink
{
    private static byte[] ReadFb(PkgArchive pkg, PkgArchive pkg24, string path)
    {
        byte[] data = pkg != null ? pkg.Read(path) : null;
        if (data == null) data = ResLoader.Read24Res(path);
        if (data == null && pkg24 != null) data = pkg24.Read(path);
        return data;
    }
    public static List<GatherZone> LoadGather(PkgArchive pkg, PkgArchive pkg24, string mapName)
    {
        var zones = new List<GatherZone>();
        byte[] prc = ReadFb(pkg, pkg24, "map/" + mapName + ".prc");
        if (prc == null || prc.Length < 24) return zones;
        var transfers = new HashSet<int>();
        byte[] link = ReadFb(pkg, pkg24, "map/" + mapName + ".link");
        if (link != null)
        {
            int current = -1;
            foreach (string raw in Gbk.GetString(link).Split('\n'))
            {
                string[] tokens = raw.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length < 2 || tokens[0][0] == '#') continue;
                if (tokens[0] == "proc_id") { current = int.TryParse(tokens[1], out int id) ? id : -1; continue; }
                if (tokens[0] == "map_to" && current >= 0 && int.TryParse(tokens[1], out int to) && to > 0) transfers.Add(current);
            }
        }
        int count = BitConverter.ToInt32(prc, 16);
        if (count < 1) return zones;
        for (int i = 0; i < count; i++)
        {
            int offset = unchecked(i * 396);
            if (unchecked(offset + 416) > prc.Length) break;
            int id = BitConverter.ToInt32(prc, unchecked(offset + 20));
            if (transfers.Contains(id)) continue;
            int n = BitConverter.ToInt32(prc, unchecked(offset + 28));
            if (n < 3 || n > 16) continue;
            var poly = new Vector2[n];
            for (int j = 0; j < n; j++)
            {
                int x = BitConverter.ToInt32(prc, unchecked(offset + 64 + j * 4));
                int y = BitConverter.ToInt32(prc, unchecked(offset + 128 + j * 4));
                bool shifted = mapName == "qw0007";
                poly[j] = new Vector2(shifted ? (float)x + 432f : (float)x, shifted ? (float)y - 608f : (float)y);
            }
            zones.Add(new GatherZone { procId = id, poly = poly, kind = 0 });
        }
        return zones;
    }
}
}
