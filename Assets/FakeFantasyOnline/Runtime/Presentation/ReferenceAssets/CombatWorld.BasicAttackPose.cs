namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static bool BasicAtkUsesMagicPose(int cType, int atkDistPx) =>
        cType == 16 && atkDistPx > 150;
}

}
