namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    private bool LosClear()
    {
        if (target == null || PlayerController.I == null) return true;
        return PlayerController.LineOfSight(PlayerController.I.worldX, PlayerController.I.worldY,
            target.worldX, target.worldY);
    }
    internal bool LosClearForIndependentRuntime() => LosClear();
}

}
