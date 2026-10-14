using System.IO;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official GameTables text-root owner.
/// </summary>
public static class GameTables
{
    private static string[] _roots;

    public static string[] Roots()
    {
        if (_roots != null)
        {
            return _roots;
        }

        string[] roots =
        {
            Path.Combine(ResourceFileRoot.Path, "gameassets", "tables")
        };
        _roots = roots;
        return _roots;
    }

    public static string LoadText(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        string[] roots = Roots();
        for (int index = 0; index < roots.Length; index++)
        {
            try
            {
                string candidate = Path.Combine(roots[index], name + ".json");
                if (File.Exists(candidate)) return File.ReadAllText(candidate);
            }
            catch { }
        }

        TextAsset fallback = Resources.Load<TextAsset>(name);
        return fallback != null ? fallback.text : null;
    }
}

}
