using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // Official CombatWorld.get_AtkRange (06000160, arm64 0x2447c4 / 492, x86_64 0x251e90 / 512) is
    // the only range source read by InRange on both architectures. It is the existing AtkRange
    // property in CombatWorld.Approach.cs: pixels = CharData.atkDist when that int is >= 1 else 50
    // (arm64 `if (iVar1 < 1) return 50.0 / fVar2`, x86_64 the analogous test over DAT_01118a38),
    // divided by the ppu, where a ppu <= 0 is read back as 100 (WB-058 restored that guard to the
    // official `<= 0` polarity - see the note on the property itself).
    // Nothing else is queried; no weapon, skill or per-monster distance participates.
    // Throttle slot for the blocked-line-of-sight tip, official field offset +0xb0, written only
    // by the LosClear failure branch as Time.time + 1.5 and read only by that branch.
    private float losTipUntil;

    // Official CombatWorld.InRange (0600016e, arm64 0x249ae0 / 588, x86_64 0x256fa0 / 512).
    // Both architectures agree instruction for instruction:
    //   target (+0x30) is read first and UnityEngine.Object.op_Equality(target, null) returns
    //     false immediately - a null target is NOT in range;
    //   then the shared player Transform (CombatNumber's static slot, +0x18) is read and
    //     op_Equality(transform, null) likewise returns false;
    //   only then are Transform.get_position(playerTransform) and
    //     Component.get_transform(target).position read, in that order, and compared as a 2D
    //     (x, y) distance against CombatWorld.get_AtkRange().
    // A null transform on a non-null target reaches the non-returning helper (arm64 FUN_00177a74
    // / x86_64 FUN_0016c1b0), which is the IL2CPP null check for the field read: an exception,
    // not a false result. It is therefore not guarded here.
    private bool InRange()
    {
        MonsterEntity current = target;
        if (current == null) return false;
        Transform player = CombatNumber.PlayerTransform;
        if (player == null) return false;
        Vector3 position = player.position;
        Vector3 targetPosition = current.transform.position;
        return Vector2.Distance(new Vector2(position.x, position.y),
            new Vector2(targetPosition.x, targetPosition.y)) <= AtkRange;
    }

    internal bool InRangeForIndependentRuntime() => InRange();

    // The official LosClear failure branch of CombatWorld.Update (0600016c, arm64 0x248cf8 /
    // 3204, x86_64 0x2562a0 / 3040) - arm64 lines 203-217 and x86_64 lines 164-179 of the frozen
    // exports, reached only after the InRange gate has passed. After one 8-byte store clears
    // atkState (+0x40) and
    // atkPhaseT (+0x44) together it reads Time.time once, returns without a tip while that value
    // is below the +0xb0 slot, otherwise shows the single frozen string literal
    // StringLiteral[17607] "无法攻击到目标" through UITip.Show with UIScale.Factor and re-reads
    // Time.time to store now + 1.5 (arm64 literal 1.5, x86_64 DAT_01118a3c) into +0xb0. Both
    // architectures return from Update on this branch: no attack state advances and no request
    // is sent while the line of sight is blocked.
    internal bool TryShowBlockedSightTipForIndependentRuntime(float scale)
    {
        float now = Time.time;
        if (now < losTipUntil) return false;
        skillTipForIndependentRuntime("无法攻击到目标", scale);
        losTipUntil = Time.time + 1.5f;
        return true;
    }
}

}
