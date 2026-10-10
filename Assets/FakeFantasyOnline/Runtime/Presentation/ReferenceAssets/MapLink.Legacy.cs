using System;
using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Official legacy half of the MapLink owner: 060005b7 EnsureJson, 060005b8 Load, 060005b9
/// ParseInt, 060005ba DecodeGbk and the compiler-generated 060005c0 commit closure, all recovered
/// from the pinned 1.5.8 arm64 and x86_64 exports. The JSON-first EnsureJson/Load pair in
/// ReferenceWorldMapLinks.cs stays as the independent-runtime boundary adapter; nothing there is
/// replaced, renamed or folded into these bodies.
/// </summary>
public static partial class MapLink
{
    private sealed class LoadProcState
    {
        public bool hasCur;
        public bool hasMapTo;
        public Dictionary<int, List<Vector2[]>> polys;
        public int curProc;
        public MapProc cur;
        public List<MapProc> result;

        // Official 0x060005c0, MapLink+<>c__DisplayClass15_0.<Load>b__0(). One MapProc is appended
        // per polygon bucketed under the current proc id, and only while a proc is open and a
        // positive map_to has been read for it.
        public void Commit()
        {
            if (!hasCur || !hasMapTo)
            {
                return;
            }

            List<Vector2[]> bucket;
            if (!polys.TryGetValue(curProc, out bucket))
            {
                return;
            }

            for (int i = 0; i < bucket.Count; i++)
            {
                result.Add(new MapProc
                {
                    procId = cur.procId,
                    poly = bucket[i],
                    mapTo = cur.mapTo,
                    toX = cur.toX,
                    toY = cur.toY,
                    toDir = cur.toDir,
                    cmd = cur.cmd
                });
            }
        }
    }

    // Official 0x060005b7, static System.Void MapLink.EnsureJson(). The official table name is
    // "teleport_links" and the dictionary is only built while it is still null.
    private static void EnsureJson()
    {
        if (_json != null)
        {
            return;
        }

        _json = new Dictionary<string, List<MapProc>>();
        string text = GameTables.LoadText("teleport_links");
        if (text == null)
        {
            return;
        }

        TLData data = JsonUtility.FromJson<TLData>(text);
        if (data == null || data.maps == null || data.maps.Length <= 0)
        {
            return;
        }

        for (int mapIndex = 0; mapIndex < data.maps.Length; mapIndex++)
        {
            TLMap map = data.maps[mapIndex];
            List<MapProc> result = new List<MapProc>();
            TLGate[] gates = map.gates;
            if (gates != null && gates.Length > 0)
            {
                for (int gateIndex = 0; gateIndex < gates.Length; gateIndex++)
                {
                    TLGate gate = gates[gateIndex];
                    if (gate.poly == null || gate.poly.Length <= 5)
                    {
                        continue;
                    }

                    Vector2[] poly = new Vector2[gate.poly.Length / 2];
                    for (int pointIndex = 0; pointIndex < poly.Length; pointIndex++)
                    {
                        poly[pointIndex] = new Vector2(
                            gate.poly[pointIndex * 2],
                            gate.poly[(pointIndex * 2) + 1]);
                    }

                    result.Add(new MapProc
                    {
                        procId = 0,
                        poly = poly,
                        mapTo = gate.mapTo,
                        toX = gate.toX,
                        toY = gate.toY,
                        toDir = gate.toDir,
                        cmd = gate.cmd
                    });
                }
            }

            _json[map.file] = result;
        }
    }

    // Official 0x060005b8, static List&lt;MapLink.MapProc&gt; MapLink.Load(PkgArchive, PkgArchive,
    // String). The JSON table wins when it carries the map; otherwise map/&lt;name&gt;.prc supplies the
    // polygons and map/&lt;name&gt;.link supplies the proc attributes.
    public static List<MapProc> Load(PkgArchive pkg, PkgArchive pkg24, string mapName)
    {
        EnsureJson();
        List<MapProc> cached;
        if (_json != null && _json.TryGetValue(mapName, out cached))
        {
            return cached;
        }

        List<MapProc> result = new List<MapProc>();
        if (pkg == null && pkg24 == null)
        {
            return result;
        }

        LoadProcState state = new LoadProcState();
        state.result = result;
        state.polys = new Dictionary<int, List<Vector2[]>>();
        state.curProc = -1;
        state.cur = new MapProc
        {
            procId = -1,
            poly = null,
            mapTo = -1,
            toX = 0f,
            toY = 0f,
            toDir = 0,
            cmd = null
        };

        byte[] prc = ReadFb(pkg, pkg24, "map/" + mapName + ".prc");
        if (prc != null && prc.Length > 23)
        {
            int count = BitConverter.ToInt32(prc, 16);
            for (int i = 0; i < count; i++)
            {
                int entry = PROC_HEAD + (i * PROC_SIZE);
                if (prc.Length < entry + PROC_SIZE)
                {
                    break;
                }

                int id = BitConverter.ToInt32(prc, entry);
                int n = BitConverter.ToInt32(prc, entry + OFF_PN);
                if (n < 3 || n > MAX_POINTS)
                {
                    continue;
                }

                Vector2[] poly = new Vector2[n];
                for (int point = 0; point < n; point++)
                {
                    poly[point] = new Vector2(
                        BitConverter.ToInt32(prc, entry + OFF_PX + (point * 4)),
                        BitConverter.ToInt32(prc, entry + OFF_PY + (point * 4)));
                }

                List<Vector2[]> bucket;
                if (!state.polys.TryGetValue(id, out bucket))
                {
                    bucket = new List<Vector2[]>();
                    state.polys.Add(id, bucket);
                }

                bucket.Add(poly);
            }
        }

        byte[] link = ReadFb(pkg, pkg24, "map/" + mapName + ".link");
        if (link != null)
        {
            string[] lines = Gbk.GetString(link).Split('\n');
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex].Trim();
                if (line.Length == 0 || line[0] == '#')
                {
                    continue;
                }

                string[] tokens = line.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length <= 1)
                {
                    continue;
                }

                string key = tokens[0].ToLowerInvariant();
                if (key == "proc_id")
                {
                    state.Commit();
                    state.curProc = ParseInt(tokens[1], -1);
                    state.cur.procId = state.curProc;
                    state.cur.poly = null;
                    state.cur.mapTo = -1;
                    state.cur.toX = 0f;
                    state.cur.toY = 0f;
                    state.cur.toDir = 0;
                    state.cur.cmd = null;
                    state.hasCur = true;
                    state.hasMapTo = false;
                }
                else if (state.hasCur)
                {
                    if (key == "map_to")
                    {
                        state.cur.mapTo = ParseInt(tokens[1], -1);
                        state.hasMapTo = state.cur.mapTo > 0;
                    }
                    else if (key == "map_to_pos" && tokens.Length >= 3)
                    {
                        state.cur.toX = ParseInt(tokens[1], 0);
                        state.cur.toY = ParseInt(tokens[2], 0);
                    }
                    else if (key == "map_to_dir")
                    {
                        state.cur.toDir = ParseInt(tokens[1], 0);
                    }
                }
            }

            state.Commit();

            for (int i = 0; i < result.Count; i++)
            {
                MapProc proc = result[i];
                if (mapName == "qw0007" && proc.poly != null)
                {
                    for (int point = 0; point < proc.poly.Length; point++)
                    {
                        proc.poly[point] = new Vector2(
                            proc.poly[point].x + 432f,
                            proc.poly[point].y - 608f);
                    }
                }

                if (proc.mapTo == 7)
                {
                    proc.toX += 432f;
                    proc.toY -= 608f;
                }

                result[i] = proc;
            }
        }

        return result;
    }

    // Official 0x060005b9, static System.Int32 MapLink.ParseInt(System.String, System.Int32).
    private static int ParseInt(string s, int def)
    {
        int value;
        return int.TryParse(s, out value) ? value : def;
    }

    // Official 0x060005ba, static System.String MapLink.DecodeGbk(System.Byte[]).
    private static string DecodeGbk(byte[] bytes)
    {
        return Gbk.GetString(bytes);
    }
}
}
