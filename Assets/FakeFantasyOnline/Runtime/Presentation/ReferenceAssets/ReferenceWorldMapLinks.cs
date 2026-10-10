using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official-code-first recovery of the reference client's MapLink owner.
/// </summary>
/// <remarks>
/// Type/nested-type names, fields and the JSON-first EnsureJson/Load and PointInPoly control flow
/// come from tokens 060004FB, 060004FC and 060004FF. The injected resource argument is an
/// independent-runtime boundary adapter for the official GameTables/ResLoader owner; it does not
/// replace a MapLink behavior decision.
/// </remarks>
public static partial class MapLink
{
    public struct MapProc
    {
        public int procId;
        public Vector2[] poly;
        public int mapTo;
        public float toX;
        public float toY;
        public int toDir;
        public string cmd;
    }

    public struct GatherZone
    {
        public int procId;
        public Vector2[] poly;
        public int kind;
    }

    [Serializable]
    private sealed class TLGate
    {
        public int mapTo;
        public int toX;
        public int toY;
        public int toDir;
        public int[] poly;
        public string cmd;
    }

    [Serializable]
    private sealed class TLMap
    {
        public string file;
        public TLGate[] gates;
    }

    [Serializable]
    private sealed class TLData
    {
        public TLMap[] maps;
    }

    private const int PROC_HEAD = 20;
    private const int PROC_SIZE = 396;
    private const int OFF_PN = 8;
    private const int OFF_PX = 44;
    private const int OFF_PY = 108;
    private const int MAX_POINTS = 16;

    public const string TablePath = "tables/teleport_links.json";

    private static Dictionary<string, List<MapProc>> _json;

    /// <summary>
    /// Recovered JSON-first portion of MapLink.Load.
    /// </summary>
    /// <remarks>
    /// Every currently admitted map either has a JSON row or has neither legacy .prc nor .link
    /// data. Legacy parsing remains outside this bounded source cone and is never substituted.
    /// </remarks>
    public static List<MapProc> Load(
        ReferenceResourceLoader resources,
        string mapName)
    {
        if (resources == null)
        {
            throw new ArgumentNullException(nameof(resources));
        }

        EnsureJson(resources);
        List<MapProc> result;
        if (_json != null && _json.TryGetValue(mapName, out result))
        {
            return result;
        }

        return new List<MapProc>();
    }

    private static void EnsureJson(ReferenceResourceLoader resources)
    {
        if (_json != null)
        {
            return;
        }

        _json = new Dictionary<string, List<MapProc>>(StringComparer.Ordinal);
        byte[] encoded = resources.ReadSourceOwned(TablePath);
        if (encoded == null)
        {
            return;
        }

        string text = Encoding.UTF8.GetString(encoded);
        TLData data;
        try
        {
            data = JsonUtility.FromJson<TLData>(text);
        }
        catch (ArgumentException)
        {
            return;
        }

        if (data == null || data.maps == null)
        {
            return;
        }

        for (int mapIndex = 0; mapIndex < data.maps.Length; mapIndex++)
        {
            TLMap map = data.maps[mapIndex];
            if (map == null)
            {
                continue;
            }

            List<MapProc> result = new List<MapProc>();
            TLGate[] gates = map.gates;
            if (gates != null)
            {
                for (int gateIndex = 0; gateIndex < gates.Length; gateIndex++)
                {
                    TLGate gate = gates[gateIndex];
                    if (gate == null || gate.poly == null || gate.poly.Length <= 5)
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

                    MapProc item = new MapProc
                    {
                        procId = 0,
                        poly = poly,
                        mapTo = gate.mapTo,
                        toX = gate.toX,
                        toY = gate.toY,
                        toDir = gate.toDir,
                        cmd = gate.cmd
                    };
                    result.Add(item);
                }
            }

            _json[map.file] = result;
        }
    }

    public static bool PointInPoly(Vector2 p, Vector2[] poly)
    {
        if (poly == null || poly.Length < 3)
        {
            return false;
        }

        bool inside = false;
        int previous = poly.Length - 1;
        for (int current = 0; current < poly.Length; current++)
        {
            Vector2 currentPoint = poly[current];
            Vector2 previousPoint = poly[previous];
            if ((p.y < currentPoint.y) == (previousPoint.y <= p.y) &&
                p.x < currentPoint.x +
                ((p.y - currentPoint.y) * (previousPoint.x - currentPoint.x) /
                 (previousPoint.y - currentPoint.y)))
            {
                inside = !inside;
            }

            previous = current;
        }

        return inside;
    }
}

}
