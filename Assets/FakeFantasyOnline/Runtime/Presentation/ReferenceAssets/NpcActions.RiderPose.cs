using System.Collections.Generic;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public static partial class NpcActions
{
    private static readonly Dictionary<int, int> _rideIdxCache = new Dictionary<int, int>();

    public static int RideActionIndex(int model)
    {
        if (_rideIdxCache.TryGetValue(model, out int index)) return index;
        index = RideActionIndexUncached(model);
        _rideIdxCache[model] = index;
        return index;
    }

    private static int RideActionIndexUncached(int model)
    {
        string key = RideKey(model);
        if (string.IsNullOrEmpty(key)) return -1;
        key = key.ToLowerInvariant();
        switch (key)
        {
            case "stand": return 0;
            case "run": case "walk": return 1;
            case "sit": return 2;
            case "die": return 3;
            case "attack1": return 4;
            case "attack2": return 5;
            case "attack3": return 6;
            case "prepare1": return 8;
            case "prepare2": return 9;
            case "prepare3": return 10;
            case "defence": case "hurt": return 11;
            case "ride": return 12;
            case "hand": return 13;
        }
        if (key.Contains("magic")) return 7;
        if (key.Contains("attack")) return 6;
        return key.Contains("pare") ? 10 : -1;
    }
}

}
