namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    private static int HitSnd(int cType, bool crit)
    {
        if (crit) return 1267;
        switch (cType)
        {
            case 0: return 1260;
            case 1: return 1265;
            case 4: return 1263;
            case 8: return 1264;
            case 16: return 1266;
            case 32: return 1261;
            default: return 1262;
        }
    }

    internal static int HitSoundForIndependentRuntime(int cType, bool crit) => HitSnd(cType, crit);
}

}
