using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class SuitFxDB
{
    public static (string, int) Skin(int id)
    {
        EnsureLoaded();
        return _byId.TryGetValue(id, out Item item) ? (item.cat, item.model) : (string.Empty, 0);
    }

    public static string Resolve(IEnumerable<int> ids)
    {
        EnsureLoaded();
        var keys = new HashSet<int>();
        foreach (int id in ids)
            if (_byId.TryGetValue(id, out Item item) && item.key != 0) keys.Add(item.key);
        if (keys.Count == 0) return string.Empty;
        foreach (Spec spec in _specs)
        {
            if (spec.k == null) continue;
            bool match = true;
            foreach (int key in spec.k)
            {
                if (key != 0 && !keys.Contains(key)) { match = false; break; }
            }
            if (match) return spec.fx;
        }
        return string.Empty;
    }

    public static bool IsSoul(int id) { EnsureLoaded(); return _soulIds.Contains(id); }
    public static bool IsDragon(int id) { EnsureLoaded(); return _dragon.ContainsKey(id); }
    public static string DragonSlot(int id)
    {
        EnsureLoaded();
        return _dragon != null && _dragon.TryGetValue(id, out string value) ? value : string.Empty;
    }

    public static bool IsAvatar(int id)
    {
        EnsureLoaded();
        return _fuseIds.Count > 0 ? _fuseIds.Contains(id) : _byId.ContainsKey(id);
    }
}

}
