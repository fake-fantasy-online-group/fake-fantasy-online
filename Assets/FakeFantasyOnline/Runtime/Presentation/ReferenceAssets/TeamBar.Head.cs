using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal sealed partial class TeamBar : MonoBehaviour
{
    private static readonly Dictionary<string, Texture2D> headCache = new Dictionary<string, Texture2D>();
    // Official 06000e9d: the female hair 7/8 exchange uses the original hair ID.
    public static Texture2D Head(int gender, int hairId, int hairColor)
    {
        int hair = Mathf.Clamp(hairId, 1, 8);
        int color = Mathf.Clamp(hairColor, 1, 8);
        if (gender == 1 && hairId == 7) hair = 8;
        else if (gender == 1 && hairId >= 8) hair = 7;
        string key = hair + "_" + color + "_" + (gender == 1 ? 1 : 0);
        if (headCache.TryGetValue(key, out Texture2D texture) && texture != null) return texture;
        ResLoader loader = ResLoader.I;
        if (loader == null) return null;
        texture = loader.LoadGsa(string.Format("res/head/DThed{0:D2}{1}_{2}.gsa", hair, color, gender == 1 ? "01" : "51"));
        if (texture != null) headCache[key] = texture;
        return texture;
    }
    internal static void ClearHeadCacheForIndependentRuntime() => headCache.Clear();
}
}
