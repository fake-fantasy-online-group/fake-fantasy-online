namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official CombatWorld monster targeting entry: a monster click writes the target, drops
/// any player target and resets the attack clock through the same owner fields.
/// </summary>
/// <remarks>
/// Official CombatWorld.SetTarget (0x0600014b, arm64 0x2439c8 / x86_64 0x251200) runs Ensure first
/// and then, in one order on both architectures, writes target (+0x30) = me, pvpTarget (+0x38) = 0,
/// autoAttacking (+0x78) = attack, one 8-byte store of zero over +0x40 (atkState +0x40 together with
/// atkPhaseT +0x44, arm64 0600014b.c:50 and x86_64 0600014b.c:36) and finally TargetSelect.Select(me).
/// WB-061 carries the atkPhaseT half of that single store, which this body previously omitted while
/// SetPvpTarget already had it; both target switches are the same owner fields, so leaving one of the
/// two half-reset kept the phase of the previous target alive across a switch.
/// </remarks>
public partial class CombatWorld
{
    public static void SetTarget(MonsterEntity me, bool attack = true)
    {
        Ensure();
        I.target = me;
        I.pvpTarget = null;
        I.autoAttacking = attack;
        I.atkState = AtkState.Idle;
        I.atkPhaseT = 0f;
        TargetSelect.Select(me);
    }

    /// <summary>
    /// Recovered official CombatWorld PvP targeting entry (0x0600014c, arm64 0x243aac / 228 bytes,
    /// x86_64 0x2512d0 / 192 bytes): the only owner that writes the PvP target slot.
    /// </summary>
    /// <remarks>
    /// WB-143 restores the official member identity of this carrier. The official 1.5.8 declaration
    /// is `.method public hidebysig static void SetPvpTarget(class RemotePlayer rp)` (managed IL
    /// body begins at RVA 0x8be6c, token 0x0600014c; the atlas alias group for that token contains
    /// only this member itself), while WB-060/WB-061 carried the same body under the product-only
    /// name `SetPvpTargetForIndependentRuntime` with `internal` visibility, so the carrier is now
    /// named and made public as the official member. The body itself is unchanged.
    /// Both frozen exports run the same five writes in the same order after
    /// CombatWorld.Ensure (0x0600012d): pvpTarget (+0x38) = rp, target (+0x30) = null,
    /// autoAttacking (+0x78) = true, then one 8-byte store of zero over +0x40, which is
    /// atkState (+0x40) and atkPhaseT (+0x44) together - the phase reset is official, not a
    /// product choice. CombatWorld.SetTarget (0x0600014b) is the exact mirror: target first,
    /// pvpTarget = null, autoAttacking = attack, the same 8-byte zero over +0x40.
    /// The official body ends with TargetSelect.Select (0x06000e54) on the same RemotePlayer, and
    /// WB-061 now carries it: this is the one and only selection of that click event, so the caller
    /// must not select or bind a second object for it. Choosing a different product object here would
    /// leave TargetSelect.Current and the object the selection owner keeps pointing at two different
    /// identities, which is what the earlier form did by selecting a wrapper afterwards.
    /// A Unity-null I cannot occur here because Ensure runs first; the official body throws
    /// through the non-returning helper (arm64 FUN_00177a74 / x86_64 FUN_0016c1b0) if it does.
    /// </remarks>
    public static void SetPvpTarget(RemotePlayer rp)
    {
        Ensure();
        I.pvpTarget = rp;
        I.target = null;
        I.autoAttacking = true;
        I.atkState = AtkState.Idle;
        I.atkPhaseT = 0f;
        TargetSelect.Select(rp);
    }

    /// <summary>
    /// Recovered official CombatWorld PvP/monster target clearing entry (0x0600014d, arm64 0x243b90
    /// / 344 bytes, x86_64 0x251390 / 304 bytes): the whole clear runs inside the "I is alive" door
    /// and ends with the one TargetSelect.Clear of this arm.
    /// </summary>
    /// <remarks>
    /// Both frozen exports run the same writes in the same order and neither calls Ensure. The door
    /// is UnityEngine.Object.op_Inequality on the CombatWorld singleton: an absent or destroyed I
    /// returns without touching anything, while an I that Unity reports alive and that is then raw
    /// zero falls into the non-returning helper (arm64 FUN_00177a74 / x86_64 FUN_0016c1b0), i.e. an
    /// IL2CPP throw, not a skip. Inside the door the official order is target (+0x30) = null, then
    /// pvpTarget (+0x38) = null, then the single 8-byte zero over +0x40 that covers atkState (+0x40)
    /// and atkPhaseT (+0x44) together, then three 4-byte zeros over pendSkillId (+0x80), queuedSkillId
    /// (+0x8c) and pendGroundSkill (+0x94), and finally TargetSelect.Clear (0x06000e55), which is
    /// what clears TargetSelect.Current and unbinds/hides the TargetRing. The five ids/flags that
    /// are not touched here are official too: autoAttacking (+0x78), castPauseUntil (+0x7c),
    /// pendSkillRangeU (+0x84), pendAwaitUntil (+0x88), queuedRangePx (+0x90) and the whole
    /// pendGround coordinate block (+0x98..+0xac) keep their values, so this body clears exactly
    /// the six writes above and nothing else. It takes no argument and never tests the current
    /// target first - unlike CombatWorld.Remove (0x06000144), which clears the target only when it
    /// is the removed monster - because the official caller has no target to compare against.
    /// </remarks>
    public static void ClearTarget()
    {
        if (!I) return;
        I.target = null;
        I.pvpTarget = null;
        I.atkState = AtkState.Idle;
        I.atkPhaseT = 0f;
        I.pendSkillId = 0;
        I.queuedSkillId = 0;
        I.pendGroundSkill = 0;
        TargetSelect.Clear();
    }
}

}
