namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    public static void SpawnCaptureAnim(int targetId, int itemId)
    {
        if (I == null || !I.monsters.TryGetValue(targetId, out MonsterEntity monster) || monster == null) return;
        EffectPlayer.PlayNamed(monster.CenterPos(), "CP0002", I.ppu, 1, -1, false, false);
    }
}
}
