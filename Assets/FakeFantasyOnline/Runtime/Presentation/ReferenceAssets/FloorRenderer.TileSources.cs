using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class FloorRenderer
{
    public PkgArchive pkg;
    private Dictionary<int, string> code2name;
    private readonly Dictionary<string, Color32[][]> shapeCache = new Dictionary<string, Color32[][]>();

    // Original 06000bba.
    private static Dictionary<int, string> LoadTileIni(PkgArchive package)
    {
        var result = new Dictionary<int, string>();
        byte[] data = package.Read("tile/tile.ini");
        if (data == null) return result;
        string text = Encoding.Unicode.GetString(data).TrimStart('\ufeff');
        foreach (string row in text.Split('\n'))
        {
            string line = row.Trim();
            if (line.Length >= 4 && IsDigits(line, 4))
                result[int.Parse(line.Substring(0, 4))] = line.Substring(4).Trim();
        }
        return result;
    }

    private static bool IsDigits(string text, int count)
    {
        for (int i = 0; i < count; i++)
            if (text[i] < '0' || text[i] > '9') return false;
        return true;
    }

    // Original 06000bb7 caches null results as well as complete shape arrays.
    private Color32[][] GetShapesFull(string name)
    {
        if (!shapeCache.TryGetValue(name, out Color32[][] result))
        {
            result = LoadTilFull(pkg, name);
            shapeCache[name] = result;
        }
        return result;
    }
}

}
