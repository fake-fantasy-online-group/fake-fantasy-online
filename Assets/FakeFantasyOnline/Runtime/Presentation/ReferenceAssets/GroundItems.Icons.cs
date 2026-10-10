using System.Collections.Generic;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class GroundItems
{
    private static readonly Color32[] CatColor =
    {
        new Color32(246, 241, 186, 255), new Color32(82, 150, 252, 255),
        new Color32(10, 253, 222, 255), new Color32(234, 53, 255, 255),
        new Color32(40, 240, 73, 255), new Color32(244, 241, 18, 255),
        new Color32(196, 196, 196, 255), new Color32(247, 245, 245, 255),
        new Color32(253, 65, 0, 255), new Color32(231, 233, 237, 255),
        new Color32(255, 90, 90, 255)
    };
    private static readonly Color32 DefaultName = new Color32(247, 245, 245, 255);
    private static readonly Dictionary<int, Texture2D> iconCache = new Dictionary<int, Texture2D>();

    private static Color32 NameColorFor(int cat) => cat >= 0 && cat < CatColor.Length ? CatColor[cat] : DefaultName;
    private static Texture2D LoadIcon(int id)
    {
        if (iconCache.TryGetValue(id, out Texture2D texture)) return texture;
        texture = NetClient.LoadItemIcon(id);
        iconCache[id] = texture;
        return texture;
    }
    public static void ClearIconCache() => iconCache.Clear();
    internal static Texture2D LoadIconForIndependentRuntime(int id) => LoadIcon(id);
}
}
