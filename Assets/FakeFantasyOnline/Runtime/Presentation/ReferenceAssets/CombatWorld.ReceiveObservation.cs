namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    // Post-call observation only. Remove already selected and mutated the actual registry.
    internal static void ObserveRegistryPresenceForIndependentRuntime(int id,
        out bool trapPresent, out bool playerPresent, out bool monsterPresent)
    {
        trapPresent = I && I.trapPos.ContainsKey(id);
        playerPresent = I && I.players.ContainsKey(id);
        monsterPresent = I && I.monsters.ContainsKey(id);
    }
}
}
