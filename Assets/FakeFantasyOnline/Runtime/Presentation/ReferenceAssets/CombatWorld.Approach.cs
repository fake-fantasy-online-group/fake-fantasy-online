using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    public static bool Engaged => I != null &&
        (I.autoAttacking || I.pendSkillId > 0 || I.pendGroundSkill > 0);

    public static bool AwaitingTarget => I != null && I.pendSkillId > 0 &&
        I.target == null && Time.time <= I.pendAwaitUntil;

    public static void CancelPending()
    {
        if (I != null)
        {
            I.pendSkillId = 0;
            I.queuedSkillId = 0;
            I.pendGroundSkill = 0;
        }
    }

    private static float Inset(float rangeU)
    {
        float scale = CombatNumber.Ppu > 0f ? CombatNumber.Ppu : 100f;
        float scaled = rangeU * 0.9f;
        float offset = -2f / scale;
        return Mathf.Max(0.01f, scaled + offset);
    }

    private static float AtkRange
    {
        get
        {
            // WB-058: restored to the official predicate after re-reading both frozen exports for
            // this branch (a-06000160/06000160.c line 36 and 47, x-06000160 likewise). Official
            // get_AtkRange guards the ppu with `fVar2 <= 0.0 -> fVar2 = 100.0`, not with the
            // previous `ppu > 0 ? ppu : 100`. The two agree for every finite ppu and differ only
            // for a NaN ppu: arm64 re-reads regPpu on the false branch, so NaN stays NaN, while
            // x86_64 starts from DAT_01118a20 (100.0) and replaces it only under `0.0 < ppu`, so
            // NaN becomes 100. The arm64 polarity is the one installed here because it is the
            // literal branch in both 06000160 exports' first read and because this property is the
            // range source the restored alive-pvpTarget branch of CombatWorld.Update (0600016c)
            // reads at arm64 line 320 / x86_64 line 274; the x86_64 NaN answer is a recorded
            // one-bit difference that no finite ppu can reach.
            float scale = CombatNumber.Ppu;
            if (scale <= 0f) scale = 100f;
            float pixels = CharData.atkDist > 0 ? CharData.atkDist : 50f;
            return pixels / scale;
        }
    }

    private bool PvpWithin(float rangeU)
    {
        Transform player = CombatNumber.PlayerTransform;
        if (player == null || pvpTarget == null) return false;
        Vector3 position = player.position;
        Vector3 targetPosition = pvpTarget.transform.position;
        return Vector2.Distance(new Vector2(position.x, position.y),
            new Vector2(targetPosition.x, targetPosition.y)) <= rangeU;
    }

    public static bool WantsApproach()
    {
        if (I == null) return false;
        if (Time.time < I.castPauseUntil) return false;
        if (I.pendGroundSkill > 0) return !I.PlayerWithinGround(Inset(I.pendGroundRangeU));
        if (I.pvpTarget != null) return I.autoAttacking && !I.PvpWithin(AtkRange);
        if (I.target == null) return false;
        if (I.pendSkillId > 0) return !I.TargetWithin(Inset(I.pendSkillRangeU));
        return I.autoAttacking && !I.TargetWithin(AtkRange);
    }
}

}
