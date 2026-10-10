using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class WeatherFX
{
    public static void Apply(string mapName, Transform worldRoot, float worldW, float worldH, float ppu)
    {
        lastMap = mapName;
        lastRoot = worldRoot;
        lastW = worldW;
        lastH = worldH;
        lastPpu = ppu;
        if (root != null)
        {
            Object.Destroy(root);
            root = null;
        }
        Ppu = ppu > 0f ? ppu : 100f;
        GameSettings.EnsureLoaded();
        if (!GameSettings.Weather)
            return;
        if (!byMap.TryGetValue(ParseMap(mapName), out WType type) || type == WType.None)
            return;
        root = new GameObject("WeatherFX_" + type);
        if (type == WType.Rain)
            root.AddComponent<RainOverlay>();
        else
            root.AddComponent<PetalOverlay>();
    }

    // The independent scene adapter owns this root. Native TeardownWorldUi's root purge
    // destroys it without clearing the cached Apply arguments or texture references.
    internal static void ReleaseSceneRootForIndependentRuntime()
    {
        if (root != null)
            Object.Destroy(root);
    }

    public static void Refresh()
    {
        if (lastMap != null)
            Apply(lastMap, lastRoot, lastW, lastH, lastPpu);
    }
}
}
