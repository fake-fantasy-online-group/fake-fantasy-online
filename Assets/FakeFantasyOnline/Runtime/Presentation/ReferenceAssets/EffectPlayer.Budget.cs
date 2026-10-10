using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class EffectPlayer
{
    private const long FxBudgetBase = 134217728L;
    private static readonly ArtBudget<string> _fxBudget;
    private static readonly HashSet<string> _fxPinned;

    private static void ApplyFxBudgetScale()
    {
        _fxBudget.BudgetBytes = (long)(FxBudgetBase * ArtDetail.BudgetScale);
    }

    public static string FxBudgetStat() => _fxBudget.Stat();

    private static void NoteFx(string key, Sprite[] frames)
    {
        if (frames == null) return;
        var textures = new HashSet<Texture2D>();
        long bytes = 0;
        foreach (Sprite sprite in frames)
        {
            Texture2D texture = sprite != null ? sprite.texture : null;
            if (texture == null || !textures.Add(texture)) continue;
            bytes += (long)texture.width * texture.height *
                (texture.format == TextureFormat.ARGB4444 ? 2 : 4);
        }
        _fxBudget.Note(key, bytes);
    }
}

}
