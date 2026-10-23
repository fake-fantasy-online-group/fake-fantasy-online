namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // The constants below are the official literals read by get_AtkInterval; they agree with the
    // already recovered ReferenceWorldBasicAttackController.AttackSpeedAttributeIndex,
    // MinimumAttackSpeedPercent and Melee/RangedIntervalSeconds.
    private const int AttackSpeedAttributeArrayIndex = 33;
    private const int MinimumAttackSpeedPercent = 50;
    // The native clamp is `if (percent < 0x33) percent = 0x32` (arm64) / `0x32 < percent ?
    // percent : 0x32` (x86_64): a percent below 51 becomes 50. For integer percents that is the
    // same result as `percent < 50 -> 50`, but the limit is written as the official one.
    private const int MinimumAttackSpeedPercentLimit = 51;
    private const int DefaultAttackSpeedPercent = 100;
    private const float OrdinaryAttackIntervalSeconds = 1f;
    private const float FastAttackIntervalSeconds = 0.5f;

    // Official CombatWorld.get_IsFastAttack (06000129, static System.Boolean
    // CombatWorld.get_IsFastAttack(), arm64 0x23dbc8 / 324 bytes, x86_64 0x24bb20 / 336 bytes).
    // Both decompilations reduce to the same two tests, in this order:
    //   if (0x96 < CharData.atkDist (+0x120)) { if (CharData.weaponCType (+0x11c) != 0x10) true; }
    //   return CharData.weaponCType == 8;
    // A long-reach weapon is therefore fast in every category EXCEPT 16, the magic category that
    // PlayerController.PlayMagicSwing takes instead of PlayAttackPose; category 8 is fast at any
    // reach. It is not the same predicate as "reach > 150" alone.
    // Negotiated servers supply original weapon reach separately from effective target reach.
    // Legacy servers retain the recovered predicate through AttackIdentityDistance fallback.
    private static bool IsFastAttack =>
        (CharData.AttackIdentityDistance > 150 && CharData.weaponCType != 16) || CharData.weaponCType == 8;

    // Official CombatWorld.get_AtkInterval (0600012c, static System.Single
    // CombatWorld.get_AtkInterval(), arm64 0x23ded4 / 384 bytes, x86_64 0x24be30 / 352 bytes).
    // Native order:
    //   1. get_IsFastAttack() is called first and only once;
    //   2. the attack-speed percent starts at 100, the attribute array is read from the CharData
    //      slot at +0xb8 and falls back to +0xb0 and then stays at 100 when neither is present;
    //      element 33 is read only while the array length is greater than 33 (`0x21 < max_length`),
    //      and a value below 1 is put back to 100 before anything else uses it - arm64 assigns
    //      inside the conjunct (`uVar3 = *(uint*)(array + 0xa4), (int)uVar3 < 1`) while x86_64
    //      keeps the constant unless `0 < (int)element`; the selected value is the same.
    //   3. base = IsFastAttack ? 0.5 : 1 (a data constant selected after the array read);
    //   4. arm64 clamps with `if (percent < 0x33) percent = 0x32` and x86_64 with
    //      `0x32 < percent ? percent : 0x32`; for integer percents those are the same rule,
    //      a percent of 50 or less becomes 50 and 51 stays 51.
    //   5. return base / (percent / 100).
    // The two CharData slots are pinned by the frozen 1.5.8 field atlas and not by any product
    // parameter name: CharData.attrs is System.Int32[] at instance offset 176 (+0xb0) and
    // CharData.attrsTotal is System.Int32[] at instance offset 184 (+0xb8). Native therefore
    // reads attrsTotal FIRST and only falls back to attrs when attrsTotal is null; when attrs is
    // null as well the percent stays at its 100 default. Array element 33 is `*(uint*)(array +
    // 0xa4)` - the IL2CPP array header is 0x20 bytes of object/bounds/max_length - and the
    // length test `0x21 < *(int*)(array + 0x18)` is `Length > 33`, so the length bound and the
    // element index are the same 33. The official order of the two rewrites is: a value below 1
    // becomes 100 FIRST, then a value below 51 becomes 50. CharData.attrsTotal is published with
    // CharData.attrs by the same host receive step, in the official order +0xb8 then +0xb0
    // (PrivateServerGateway -> IWorldReceiveOwner.ReceivePlayerAttributes ->
    // ReferenceWorldReceiveOwner), once per received 0x8007, so the formula below reads the
    // arrays the last packet stored even on a death, mount-anchor or work frame. Null-ness is
    // preserved exactly, so an absent totals array still falls back instead of defaulting.
    private static float AtkInterval
    {
        get
        {
            bool fast = IsFastAttack;
            int speedPercent = DefaultAttackSpeedPercent;
            int[] attributes = CharData.attrsTotal;
            if (attributes == null) attributes = CharData.attrs;
            if (attributes != null && attributes.Length > AttackSpeedAttributeArrayIndex)
            {
                speedPercent = attributes[AttackSpeedAttributeArrayIndex];
                if (speedPercent < 1) speedPercent = DefaultAttackSpeedPercent;
            }
            if (speedPercent < MinimumAttackSpeedPercentLimit) speedPercent = MinimumAttackSpeedPercent;
            return (fast ? FastAttackIntervalSeconds : OrdinaryAttackIntervalSeconds) /
                (speedPercent / 100f);
        }
    }

    internal static float AtkIntervalForIndependentRuntime => AtkInterval;
}
}
