namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    // Observation-only intent identities; repeated requests for the same target/skill
    // are still distinct. Native queue promotion retains its original identity.
    private long pluginAttackIntentRevision;
    private long pluginSkillIntentRevision;
    private bool pluginResumingQueuedSkill;
    internal long AttackIntentRevisionForPlugins => pluginAttackIntentRevision;
    internal long SkillIntentRevisionForPlugins => pluginSkillIntentRevision;
    internal static long CurrentAttackIntentRevisionForPlugins => I == null ? 0 : I.pluginAttackIntentRevision;
    internal static long CurrentSkillIntentRevisionForPlugins => I == null ? 0 : I.pluginSkillIntentRevision;

    // InWorld plugin actions need the official CombatWorld singleton. Some world
    // entries never called Bind, which made combat.attack look like the player was missing.
    internal static CombatWorld OwnerForPlugins()
    {
        if (I == null && PlayerController.I != null) Ensure();
        else if (I != null) I.ActivateForIndependentRuntime();
        return I;
    }

    private void ResumeQueuedSkillForPlugins(int skill, float range)
    {
        pluginResumingQueuedSkill = true;
        try { RequestSkill(skill, range); }
        finally { pluginResumingQueuedSkill = false; }
    }
}
}
