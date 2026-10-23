using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class SkillCaster
{
    private static readonly System.Collections.Generic.HashSet<int> missingChildVisuals =
        new System.Collections.Generic.HashSet<int>();

    // Separate entry point: no cast script (scripts can synthesize attackee hit effects),
    // actor pose, aim, cooldown, or client gameplay work. Reuse the native data phase runner
    // and its cosmetic timing. Release excludes hits; impact-only is admitted exclusively
    // by a separate server actual-landed edge and never replays release art or sound.
    public static void CastChildVisual(int skillId, Transform caster, Transform target,
        float ppu, Vector3 origin, Vector3 center, bool impactOnly)
    {
        SkillEffectDB.Entry entry = SkillEffectDB.Get(skillId);
        if (entry == null)
        {
            if (missingChildVisuals.Add(skillId))
                Debug.LogWarning("[ChildSkillFx] Native phase table missing skill " + skillId +
                    "; not replaying an ambiguous cast script or inventing effects.");
            return;
        }
        GameObject owner = new GameObject("ChildSkillFx:" + skillId);
        SkillCaster runner = owner.AddComponent<SkillCaster>();
        runner.caster = caster;
        runner.target = target;
        runner.ppu = ppu;
        runner.groundPos = center;
        runner.releaseOrigin = origin;
        runner.releaseOnly = !impactOnly;
        runner.impactOnly = impactOnly;
        runner.StartCoroutine(runner.RunData(entry));
    }
}
}
