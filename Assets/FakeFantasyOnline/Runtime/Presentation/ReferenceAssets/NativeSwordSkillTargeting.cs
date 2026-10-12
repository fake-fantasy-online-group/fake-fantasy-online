namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Original sword targeting, from the five-rank ov_skilldesc rows in the earliest
/// server source release (a9c747c, init-31). Resource variants may contain later
/// area conversions; those cannot redefine the original profession's targeting.
/// This is a narrow gameplay correction, not a replacement art/resource table.
/// </summary>
internal static class NativeSwordSkillTargeting
{
    internal readonly struct Rule
    {
        internal readonly int TargetKind, Shape, Distance, Radius, CanSelf, CanEnemy;
        internal int CastKind => TargetKind == 18 ? 0 : TargetKind == 2 || CanSelf != 0 ? 2 : 1;
        internal bool SelfCentered => TargetKind == 2;
        internal Rule(int targetKind, int distance, int radius = 0, bool self = false)
        {
            TargetKind = targetKind;
            Shape = targetKind == 2 ? 2 : 0;
            Distance = distance;
            Radius = radius;
            CanSelf = self ? 1 : 0;
            CanEnemy = !self && targetKind != 18 ? 1 : 0;
        }
    }

    internal static bool TryGet(int id, out Rule rule)
    {
        switch (id)
        {
            case 10101: case 10106: case 10110: case 10112:
            case 10118: case 10120: rule = new Rule(18, 0); return true;
            case 10108: rule = new Rule(18, 10); return true;
            case 10102: case 10103: case 10104: case 10111:
            case 10113: case 10116: case 10119: case 10122:
            case 10123: case 10126: case 10129:
                rule = new Rule(1, 75); return true;
            case 10105: rule = new Rule(17, 400); return true;
            case 10117: rule = new Rule(1, 300); return true;
            case 10107: case 10109: case 10114: case 10130:
                rule = new Rule(1, 1, self: true); return true;
            case 10115: rule = new Rule(1, 0, self: true); return true;
            case 10124: rule = new Rule(1, 100, self: true); return true;
            case 10121: rule = new Rule(2, 0, 100); return true;
            case 10125: rule = new Rule(2, 100, 150); return true;
            // These radii document the server source only. Apply deliberately
            // retains the existing geometry of genuinely area-targeted resources.
            case 10127: rule = new Rule(2, 0, 200); return true;
            case 10128: rule = new Rule(2, 0, 100); return true;
            default: rule = default; return false;
        }
    }

    internal static void Apply(Win05.SkInfo skill)
    {
        if (skill == null || !TryGet(skill.id, out Rule rule)) return;
        skill.tgt = rule.TargetKind;
        skill.canSelf = rule.CanSelf;
        skill.canFriendly = 0;
        skill.canEnemy = rule.CanEnemy;
        if (rule.SelfCentered)
        {
            // These four skills really are areas. Preserve their resource radius,
            // range, cast kind and geometry; this fix does not rebalance them.
            skill.shape = 2;
            return;
        }
        // Enemy-single cast admission and auto-approach must use the same
        // world-pixel range as the original server (not an altered AoE range).
        if (rule.CanEnemy != 0) skill.dist = rule.Distance;
        skill.castkind = rule.CastKind;
        skill.shape = 0;
        skill.radius = 0;
        skill.rwidth = skill.rectl = skill.rectw = 0;
    }
}
}
