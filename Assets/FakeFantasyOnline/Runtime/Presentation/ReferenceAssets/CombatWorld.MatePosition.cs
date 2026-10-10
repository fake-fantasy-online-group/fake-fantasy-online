namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static bool MatePos(int id, out float wx, out float wy)
    {
        wy = 0f;
        wx = 0f;
        if (I == null) return false;
        RemotePlayer player;
        if (!I.players.TryGetValue(id, out player) || player == null) return false;
        wx = player.worldX;
        wy = player.worldY;
        return true;
    }

    // Data/lifetime connections from the existing remote actor assembly, with no follow policy.
    internal void RegisterPlayerForIndependentRuntime(RemotePlayer player)
    {
        players[player.id] = player;
    }

    internal void UnregisterPlayerForIndependentRuntime(RemotePlayer player)
    {
        RemotePlayer current;
        if (players.TryGetValue(player.id, out current) && current == player)
            players.Remove(player.id);
    }
}

}
