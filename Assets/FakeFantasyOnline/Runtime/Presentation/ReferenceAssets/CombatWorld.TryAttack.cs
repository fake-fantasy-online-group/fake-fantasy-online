using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // Official CombatWorld.TryAttack (06000170, arm64 0x249d2c / 1288, x86_64 0x2571a0 / 1216).
    // Both architectures agree instruction for instruction on the order below:
    //   1. *(byte*)(CharData + 0x231), the riding byte, is read first; a non-zero value returns
    //      before any other work. No target, client or range work happens while riding.
    //   2. UnityEngine.Object.op_Equality(target, null): a null target returns.
    //   3. UnityEngine.Object.op_Equality(NetClient.I, null): a null client returns.
    //   4. NetClient.get_Connected (060006b7): a disconnected client returns. The official owner is
    //      the Protocol NetClient property; it is read here through the read-only mirror so no
    //      second connection predicate is introduced.
    //   5. CombatWorld.InRange (0600016e): out of range returns. This is the frame's second
    //      InRange read - Update already gated on it before the line-of-sight branch, and the
    //      target may have moved between the two.
    //   6. NetClient.Attack (0600074d) with the target's int field at +0x30, i.e.
    //      MonsterEntity.id. The raw reference and NetClient.I are re-read immediately before the
    //      call and a zero reaches the non-returning helper (arm64 LAB_0024a230 -> FUN_00177a74,
    //      x86_64 LAB_0025765a -> FUN_0016c1b0): the IL2CPP null check for the call site, an
    //      exception rather than a skip.
    //   7. Only now, and only while PlayerController.I survives the Unity comparison, does the body
    //      call PlayerController.PlayAttack (060009c8) with MonsterEntity.CenterPos (0600065e) and
    //      CombatWorld.get_AtkInterval (0600012c). Every branch above returns first, so a failed
    //      gate produces no swing, no projectile and no request.
    //   8. AtkProjectile (0600012b) and the EffectPlayer 060002aa / 060002a7 pair follow outside
    //      that null check - see PlayAttackProjectile in CombatWorld.Projectile.cs.
    private void TryAttack()
    {
        if (CharData.riding) return;
        // Branches 2 and 3 are UnityEngine.Object.op_Equality against null, so a destroyed
        // (Unity-fake-null) target or client takes the early return.
        if (target == null) return;
        NetClient client = NetClient.I;
        if (client == null) return;
        if (!client.ConnectedForIndependentRuntime) return;
        // Branch 5 may observe live state: InRange reads CombatNumber's player Transform,
        // Transform.get_position on both sides and CombatWorld.get_AtkRange, so the earlier
        // reads of target and NetClient.I are not valid across it. The native body re-reads both
        // raw references immediately before the call - arm64
        // `if ((*(long*)(this + 0x30) != 0) && (**(long**)(NetClient static + 0xb8) != 0))` and
        // the identical x86_64 test - and a zero reaches the non-returning helper (arm64
        // LAB_0024a230 -> FUN_00177a74, x86_64 LAB_0025765a -> FUN_0016c1b0). That is a throw,
        // not a return, so the re-read values are dereferenced directly here rather than being
        // turned into another early return. Target is read first, then NetClient.I, matching the
        // native test order.
        if (!InRange()) return;
        int targetId = target.id;
        NetClient.I.AttackForIndependentRuntime(targetId);
        if (PlayerController.I != null)
        {
            // Step 7, arm64 lines 123-135 of the frozen export, in the official order:
            //   1 `if (*(long *)(param_4 + 0x30) == 0) goto LAB_0024a230` - the raw target slot is
            //     tested first; LAB_0024a230 is the non-returning helper, so this throws and is
            //     never a skip or an early return;
            //   2 `lVar7 = *(long *)(*(long *)(02196 + 0xb8) + 0x78)` - PlayerController.I is read
            //     again from the static slot into the call receiver;
            //   3 MonsterEntity.CenterPos (0600065e) produces the first argument;
            //   4 CombatWorld.get_AtkInterval (0600012c) produces the second;
            //   5 `if (lVar7 == 0) goto LAB_0024a230` - the receiver is tested, same throw;
            //   6 PlayerController.PlayAttack (060009c8).
            // C# fuses a call-site null check into the call it guards, so steps 1 and 3 are one
            // `target.CenterPos()`; the source writes step 1 out as the same raw-reference test
            // (Unity's operator == is the op_Equality of step 2, not this test) and keeps the
            // receiver read between them, exactly as native does. Step 5 stays the implicit check
            // of the call itself. Nothing here writes back to the caller: 060009c8 returns void.
            if ((object)target == null) throw new System.NullReferenceException();
            PlayerController player = PlayerController.I;
            Vector3 targetCenter = target.CenterPos();
            float swingSec = AtkInterval;
            player.PlayAttackForIndependentRuntime(targetCenter, swingSec);
        }
        // Step 8 onward is outside that null check in both architectures.
        PlayAttackProjectile();
    }

    internal void TryAttackForIndependentRuntime() => TryAttack();
}

}
