using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class PlayerController
{
    // Official PlayerController.PlayAttack (060009c8, instance System.Void
    // PlayAttack(UnityEngine.Vector3 targetWorldPos, System.Single swingSec),
    // arm64 0x2a3954 / 388 bytes, x86_64 0x2b43e0 / 288 bytes). Both frozen exports agree
    // statement for statement:
    //   1. CharData.riding (+0x231) is read first and anything truthy returns with nothing played:
    //      no facing change, no pose, no sound. arm64 tests the low bit ((bVar2 & 1) == 0) and
    //      x86_64 the whole signed byte (cVar2 == '\0'); for the single riding bool those agree.
    //   2. FaceTowards (060009e2) with targetWorldPos. It is the only use of that Vector3.
    //   3. PlayMagicSwing (060009df) with swingSec, and only when it returns false the sibling
    //      PlayAttackPose (060009e0) with the same swingSec. They are alternatives, not a pair.
    //   4. CombatWorld.SwingSnd (0600015e) with CharData.weaponCType (+0x11c), read after the pose
    //      has already been started.
    //   5. SoundManager.Play (06000d67) with that id, only while it is positive (arm64 returns at
    //      `iVar3 < 1`, x86_64 calls under `0 < iVar3`).
    // There is no repeat/re-entry guard, no Unity comparison of its own, no timing written back
    // to the caller and no second sound: the two null checks in this body live in the callees.
    private void PlayAttack(Vector3 targetWorldPos, float swingSec)
    {
        if (CharData.riding) return;
        FaceTowards(targetWorldPos);
        if (!PlayMagicSwing(swingSec)) PlayAttackPose(swingSec);
        int swingSound = ReferenceWorldCombatSound.SwingSoundId(CharData.weaponCType);
        if (swingSound > 0) SoundManager.Play(swingSound);
    }

    // Official PlayerController.PlayMagicSwing (060009df, instance System.Boolean
    // PlayMagicSwing(System.Single swingSec), arm64 0x2a3cc8 / 416 bytes,
    // x86_64 0x2b46f0 / 384 bytes). Both exports agree:
    //   1. the fpd local is zeroed, then avatar (+0x168) null returns false - a plain return.
    //   2. CombatWorld.BasicAtkUsesMagicPose (0600012a) with CharData.weaponCType (+0x11c) then
    //      CharData.atkDist (+0x120), both re-read for this call in that order; false returns.
    //   3. inside that branch the avatar slot is read again as the IL2CPP call-site check, and a
    //      zero reaches the non-returning helper (arm64 FUN_00177a74, x86_64 FUN_0016c1b0): that
    //      one throws instead of returning, unlike the guard above.
    //   4. avatar.MagicPose (060009a1) supplies the frames. When it yields nothing the body returns
    //      false without writing any action field, so the caller still falls back to the physical
    //      PlayAttackPose.
    //   5. otherwise count = fpd < 2 ? 1 : fpd, frameSec = swingSec / count with 0.07 substituted
    //      while swingSec <= 0, then exactly the eight writes PlayAttackPose performs through
    //      StartActPose: actAction = 1, actFrames, actFpd, actElapsed = 0, actDur = count *
    //      frameSec, actFrameSec = frameSec, actLoop = false, actIsAtkSwing = swingSec > 0;
    //      returns true.
    // The 0.07 default is this member's own and is not interchangeable with PlayAttackPose's 0.06.
    // Known divergence: arm64 divides first and then overrides with 0.07 when param_1 <= 0, while
    // x86_64 selects the constant unless 0 < param_1. Only a NaN swingSec separates them; the
    // product follows the x86_64 shape, like the existing PlayAttackPose beside it.
    private bool PlayMagicSwing(float swingSec)
    {
        if (avatar == null) return false;
        if (!CombatWorld.BasicAtkUsesMagicPose(CharData.weaponCType, CharData.AttackIdentityDistance)) return false;
        int fpd;
        PlayerAvatar.Pose[,] frames = avatar.MagicPose(out fpd);
        if (frames == null) return false;
        int count = fpd < 2 ? 1 : fpd;
        float frameSec = swingSec > 0f ? swingSec / count : 0.07f;
        StartActPose(frames, count, frameSec, false, swingSec > 0f);
        return true;
    }

    internal void PlayAttackForIndependentRuntime(Vector3 targetWorldPos, float swingSec) =>
        PlayAttack(targetWorldPos, swingSec);
}
}
