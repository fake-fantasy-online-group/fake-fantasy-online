using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    // The folded literal of the official range door. CombatWorld.MaxTabDistPx (static System.Single)
    // exists in the 1.5.8 atlas but this body never reads it: arm64 materialises the constant into
    // w25 (0x243584 mov w25,#0xc800 / 0x243588 movk w25,#0x49af,lsl #16, fmov s1,w25 at 0x243624)
    // and x86_64 compares against _DAT_01118a30, both = 1.44e+06f = 1200f * 1200f.
    private const float CycleNearbySqrRangeForIndependentRuntime = 1440000f;

    /// <summary>
    /// Recovered official CombatWorld.CycleNearbyMonster (0x0600014a, static System.Boolean
    /// CycleNearbyMonster(); arm64 0x2432f0 / 1752 bytes, x86_64 0x250b40 / 1728 bytes).
    /// </summary>
    /// <remarks>
    /// Doors, in the order both frozen bodies evaluate them (arm64 0x243418, x86_64 0x250b40+):
    /// UnityEngine.Object.op_Equality(CombatWorld.I, null) - a missing or destroyed I returns false
    /// (arm64 0x243440 tbz w0,#0 keeps going, x86_64 0x250b.. je); then the same operator on
    /// PlayerController.I (arm64 0x2434bc, x86_64 0x250c..) - without a local player there is no
    /// centre to measure from. Both are the Unity null test, not a raw pointer test, and both end in
    /// "return false", never in an exception.
    ///
    /// Candidates come from CombatWorld.I.monsters (+0x20, Dictionary&lt;int, MonsterEntity&gt;) in
    /// dictionary enumeration order: arm64 0x243540/0x243544 and x86_64 0x250b.. read the field and
    /// fall into the non-returning helper (arm64 FUN_00177a74 / x86_64 FUN_0016c1b0, both verified
    /// to raise and never to return) when it is zero, then take Dictionary.GetEnumerator
    /// (arm64 0x243558, De Ref via metadata usage 03629) and move with MoveNext
    /// (arm64 0x2435a4) / dispose with Dispose (arm64 0x243744). Current is the Value half of the
    /// enumerator (arm64 ldr x21,[sp,#0x58]), so the sort key below is duplicated per value and the
    /// key is never read.
    ///
    /// Four rejections, each a plain "continue" on both architectures:
    /// 1. op_Equality(me, null) - arm64 0x2435cc, tbnz w0,#0 back to the MoveNext head. The raw
    ///    cbz arm64 0x2435d4 / x86_64 equivalent is the IL2CPP instance-call null guard of
    ///    get_NoAttack and lands in the same non-returning helper; it is unreachable because
    ///    op_Equality already returned false.
    /// 2. MonsterEntity.get_NoAttack (0x0600061a, arm64 0x4891dc) - arm64 0x2435e0, tbnz w0,#0
    ///    continues. The product property is "friendly || ally", which is the same official body.
    /// 3. hp (+0x3c, System.Int32) - arm64 0x2435e8/0x2435ec "cmp w8,#1 / b.lt" and x86_64
    ///    0x250c..: the monster stays a candidate only while hp &gt;= 1. No death, invisibility,
    ///    faction, pet-owner or "already targeted" test appears anywhere in either body.
    /// 4. Component.get_gameObject then GameObject.get_activeInHierarchy - arm64 0x2435fc/0x243608,
    ///    tbz w0,#0 continues; the null guard before it (arm64 0x243600 cbz x0) is the same
    ///    non-returning helper.
    /// Then the squared planar distance: one 8-byte read of (worldX +0x64, worldY +0x68) on the
    /// monster and one of (worldX +0xe4, worldY +0xe8) on PlayerController.I, fsub v0.2s / fmul
    /// v0.2s / faddp s0 on arm64 (0x243610..0x243620, no sqrt) compared with 1.44e+06f
    /// (b.gt continues at 0x24362c). The key stored in the tuple is that same squared value: the
    /// ValueTuple ctor at arm64 0xbed7a4 is "str s0,[x0]; str x1,[x0,#8]", i.e. it stores the live
    /// s0 register, which still holds dx*dx+dy*dy. Sorting by the squared value is the same order
    /// as sorting by distance, so no square root is missing here.
    ///
    /// Ordering: List.Sort(Comparison&lt;ValueTuple&lt;float, MonsterEntity&gt;&gt;) - arm64 0x2437e8,
    /// x86_64 0x250c.. - with the compiler-cached lambda CombatWorld.&lt;&gt;c.&lt;CycleNearbyMonster&gt;
    /// b__68_0 (0x06000175), whose IL is "ldloca Item1; call System.Single::CompareTo(Item2's
    /// Item1)": ascending by the squared distance, first item nearest. It is List.Sort, not a
    /// stable OrderBy, so equal keys keep whatever order the unstable introspective sort leaves.
    /// An empty list returns false before the sort (arm64 0x24374c/0x243750 cbz w8 -&gt; 0x243848).
    ///
    /// Next-index arithmetic (arm64 0x24380c..0x2438e8, x86_64 0x250e..): op_Inequality(I.target,
    /// null) gates the search, and the search compares I.target with the tuple's Item2 by raw
    /// reference (arm64 0x2438ac/0x2438b4 "ldr x9,[x8,#0x30]; cmp x9,x21"), not by the Unity
    /// operator, re-reading I and I.target every iteration. A hit at position p yields
    /// (p + 1) % count; a miss yields -1 + 1 = 0, i.e. the nearest candidate. I.target is therefore
    /// read once per comparison and the comparison never treats a destroyed target as null.
    ///
    /// The single write is CombatWorld.SetTarget (0x0600014b) at arm64 0x243908 with "mov x0,x19;
    /// mov w1,#0" (x86_64 0x2510.. with the same zeroed second argument): attack = false, so a Tab
    /// cycle selects without starting auto-attack, and the returned bool is the constant 1
    /// (arm64 0x24390c mov w0,#1). SetTarget already performs the one TargetSelect.Select of this
    /// event, so this body must not select, bind or ring anything a second time. No pvpTarget,
    /// autoAttack-on, phase, pending-skill or packet state is written here.
    /// </remarks>
    public static bool CycleNearbyMonster()
    {
        if (!I) return false;
        PlayerController player = PlayerController.I;
        if (!player) return false;

        List<(float, MonsterEntity)> candidates = new List<(float, MonsterEntity)>();
        foreach (MonsterEntity me in I.monsters.Values)
        {
            if (!me) continue;
            if (me.NoAttack) continue;
            if (me.hp < 1) continue;
            if (!me.gameObject.activeInHierarchy) continue;
            float dx = me.worldX - player.worldX;
            float dy = me.worldY - player.worldY;
            float sqr = dx * dx + dy * dy;
            if (sqr > CycleNearbySqrRangeForIndependentRuntime) continue;
            candidates.Add((sqr, me));
        }

        if (candidates.Count == 0) return false;
        candidates.Sort((a, b) => a.Item1.CompareTo(b.Item1));

        // (found + 1) % count, with found = -1 when the current target is not a candidate.
        int index = 0;
        if (I.target)
        {
            int found = 0;
            while (found < candidates.Count)
            {
                if (ReferenceEquals(I.target, candidates[found].Item2)) break;
                found++;
            }
            if (found >= candidates.Count) found = -1;
            index = (found + 1) % candidates.Count;
        }

        SetTarget(candidates[index].Item2, false);
        return true;
    }
}
}
