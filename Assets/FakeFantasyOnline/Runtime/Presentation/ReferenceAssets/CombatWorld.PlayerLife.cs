namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // Official 1.5.8 token 06000135: resolve the current registered remote identity.
    public static void PlayerDeadState(int id, bool dead)
    {
        if (I == null) return;
        if (!I.players.TryGetValue(id, out RemotePlayer player) || player == null) return;
        if (dead) player.PlayDie();
        else player.Revive();
    }
}

}
