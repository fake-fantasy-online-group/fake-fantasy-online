namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static bool BasicAtkUsesMagicPose(int cType, int nativeAttackDistancePx) =>
        cType == 16 && nativeAttackDistancePx > 150;
}

}
