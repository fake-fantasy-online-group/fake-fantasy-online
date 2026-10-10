using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class ResLoader
{
    private readonly ArtBudget<string> mapImgBudget;

    private void ApplyResBudgetScale()
    {
        float scale = ArtDetail.BudgetScale;
        npcFullBudget.BudgetBytes = (long)(50331648L * scale);
        npcAnimBudget.BudgetBytes = (long)(67108864L * scale);
        mapImgBudget.BudgetBytes = (long)(67108864L * scale);
    }

    private static bool IsPerMapImage(string path) => path != null &&
        (path.StartsWith("res/sketchmap/", StringComparison.OrdinalIgnoreCase) ||
         path.StartsWith("res/map/", StringComparison.OrdinalIgnoreCase));

    private void MapImgTouch(string path)
    {
        if (IsPerMapImage(path)) mapImgBudget.Touch(path);
    }

    private void MapImgNote(string path, Texture2D texture)
    {
        if (texture == null || !IsPerMapImage(path)) return;
        mapImgBudget.Note(path, (long)texture.width * texture.height * 4);
    }

    public string MapImgBudgetStat() => mapImgBudget.Stat();

    public int EvictMapImagesAtSafePoint() => mapImgBudget.EvictAtSafePoint(key =>
    {
        if (texCache.TryGetValue(key, out Texture2D texture) && texture != null)
            UnityEngine.Object.Destroy(texture);
        texCache.Remove(key);
    });
}

}
