using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class MonsterEntity
{
    private static bool AnyPetGlowWanted
    {
        get
        {
            GameSettings.EnsureLoaded();
            return !GameSettings.HideMyPetGlow || (!GameSettings.HideOtherPets && !GameSettings.HideOtherPetGlow);
        }
    }

    public static void OnPetGlowSettingChanged()
    {
        if (AnyPetGlowWanted) return;
        CancelGlowJobs();
        foreach (Sprite[,] grid in _glowGridCache.Values)
        {
            if (grid == null) continue;
            for (int a = grid.GetLowerBound(0); a <= grid.GetUpperBound(0); a++)
                for (int b = grid.GetLowerBound(1); b <= grid.GetUpperBound(1); b++)
                {
                    Sprite sprite = grid[a, b];
                    if (!sprite) continue;
                    if (sprite.texture) Destroy(sprite.texture);
                    Destroy(sprite);
                }
        }
        _glowGridCache.Clear();
        _glowOf.Clear();
        BumpGlowVer();
    }
}

}
