namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class PlayerController
{
    // Observation-only ownership token. Native path retries keep the token; a new path
    // from input, combat, follow, pickup, or an addon supersedes the previous command.
    private long pluginNavigationRevision;
    internal long NavigationRevisionForPlugins => pluginNavigationRevision;
    private long pluginApproachPathRevision = -1;
    private long pluginApproachAttackRevision;
    private long pluginApproachSkillRevision;
    private void ObserveCombatApproachForPlugins()
    {
        pluginApproachPathRevision = pluginNavigationRevision;
        pluginApproachAttackRevision = CombatWorld.CurrentAttackIntentRevisionForPlugins;
        pluginApproachSkillRevision = CombatWorld.CurrentSkillIntentRevisionForPlugins;
    }
    internal void StopOwnedCombatApproachForPlugins(long revision, bool skill)
    {
        if (pluginApproachPathRevision == pluginNavigationRevision &&
            revision == (skill ? pluginApproachSkillRevision : pluginApproachAttackRevision)) StopMove();
    }
    internal bool CanNavigateForPlugins => !dead && workSkill == 0 &&
        CharData.immobile == 0 && CharData.rideAnchorId == 0;
}
}
