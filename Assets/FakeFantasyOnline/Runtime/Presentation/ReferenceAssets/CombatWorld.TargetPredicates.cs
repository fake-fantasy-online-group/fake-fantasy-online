namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // Official 0600014e: Unity-alive owner, then Unity-alive monster target. Re-read I
    // after the first comparison, as in ARM64 243d48 and x86_64 25153f. No Ensure call.
    public static bool HasTarget => I != null && I.target != null;

    // Official 06000163: the pending ground skill must be strictly positive, not nonzero.
    public static bool HasGroundCast => I != null && I.pendGroundSkill > 0;

    // Official 06000166: check owner and target with Unity null semantics, then re-read
    // the target's dead flag (+0x81). IsDead is the existing direct getter of that field.
    public static bool HasPvpTarget => I != null && I.pvpTarget != null && !I.pvpTarget.IsDead;

    // Official 0600016f: a failed monster-range test still falls through to PvP. This
    // predicate does not apply HasPvpTarget's dead check, or any skill/LOS/attack gate.
    // C# evaluates the final I receiver before AtkRange and calls PvpWithin afterwards,
    // matching ARM64 24a420..24a43c and x86_64 2577d6..2577ee, including null throws.
    public static bool TargetInRange()
    {
        return I != null &&
            ((I.target != null && I.InRange()) ||
             (I.pvpTarget != null && I.PvpWithin(AtkRange)));
    }
}

}
