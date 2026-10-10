using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    // Official 0x06000140: selecting a player does not mutate the combat monster target.
    public static bool SelectPlayer(int id)
    {
        if (I == null) return false;
        if (!I.players.TryGetValue(id, out RemotePlayer player) || player == null) return false;
        TargetSelect.Select(player);
        return true;
    }
}
}
