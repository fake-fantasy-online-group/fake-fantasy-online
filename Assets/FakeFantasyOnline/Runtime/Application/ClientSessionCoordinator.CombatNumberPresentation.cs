namespace FakeFantasyOnline.Application
{

public sealed partial class ClientSessionCoordinator
{
    private readonly WorldCombatNumberPresentation combatNumberPresentation =
        new WorldCombatNumberPresentation();
    private long combatNumberStreamGeneration = -1;

    // The journal and consumer belong to the connection, not to its replaceable map frame.
    public WorldCombatNumberPresentation CombatNumberPresentation
    {
        get
        {
            if (_gateway is IWorldCombatNumberStreamSource source)
            {
                long generation = source.CombatNumberStreamGeneration;
                if (generation != combatNumberStreamGeneration)
                {
                    combatNumberPresentation.Clear();
                    combatNumberStreamGeneration = generation;
                }
            }
            return combatNumberPresentation;
        }
    }

    // Called only when the actual old world is retired, never by a duplicate
    // entered=1 acknowledgement. No authoritative experience data is changed.
    public void RetireCombatNumberSceneForIndependentRuntime() =>
        CombatNumberPresentation.RetireScene();

    private void ResetCombatNumberPresentation() => combatNumberPresentation.Clear();
}

}
