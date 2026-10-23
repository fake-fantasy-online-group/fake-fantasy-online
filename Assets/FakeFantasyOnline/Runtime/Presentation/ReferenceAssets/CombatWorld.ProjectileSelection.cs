namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    private static (string, float)? AtkProjectile(int weaponCType, int nativeAttackDistancePx)
    {
        if (nativeAttackDistancePx <= 150) return null;
        if (weaponCType == 16) return ("TFX047_20", 0.6f);
        if (weaponCType == 32) return ("TFX302", 1f);
        return null;
    }
}

}
