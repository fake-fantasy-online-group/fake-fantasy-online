using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    private float pendGroundRangeU;
    private float pendGroundXU;
    private float pendGroundYU;
    private float pendGroundUntil;

    private static void Ensure()
    {
        if (I == null) I = new GameObject("CombatWorld").AddComponent<CombatWorld>();
        RefreshDims();
    }

    private static void RefreshDims()
    {
        PlayerController player = PlayerController.I;
        if (player != null && I != null)
        {
            I.ppu = player.ppu;
            I.worldW = player.worldW;
            I.worldH = player.worldH;
        }
    }

    public static void RequestGroundCast(int skillId, int gx, int gy, float rangePx)
    {
        Ensure();
        float scale = I.ppu > 0f ? I.ppu : 100f;
        I.pendGroundSkill = skillId;
        I.pendGroundGx = gx;
        I.pendGroundGy = gy;
        I.pendGroundRangeU = (rangePx > 0f ? rangePx : AuthoritativeProfessionSkill.Uses(skillId) ? float.PositiveInfinity : 100f) / scale;
        I.pendGroundXU = (gx - I.worldW * 0.5f) / scale;
        I.pendGroundYU = (I.worldH * 0.5f - gy) / scale;
        I.pendGroundUntil = Time.time + 8f;
    }

    private bool PlayerWithinGround(float rangeU)
    {
        Transform player = CombatNumber.PlayerTransform;
        if (player == null) return false;
        Vector3 position = player.position;
        return Vector2.Distance(new Vector2(position.x, position.y),
            new Vector2(pendGroundXU, pendGroundYU)) <= rangeU;
    }

    // Complete Update calls this request stage once, between queued replay and target pending.
    private void AdvanceGroundCastForIndependentRuntime()
    {
        if (pendGroundSkill > 0)
        {
            if (Time.time > pendGroundUntil)
            {
                pendGroundSkill = 0;
            }
            else if (PlayerWithinGround(pendGroundRangeU) && NetClient.I != null)
            {
                NetClient.PendingCastSkill = pendGroundSkill;
                NetClient.PendingCastGround = new Vector3(pendGroundXU, pendGroundYU, 0f);
                NetClient.I.UseSkillAt(pendGroundSkill, pendGroundGx, pendGroundGy, 0);
                pendGroundSkill = 0;
                if (PlayerController.I != null) PlayerController.I.StopMove();
            }
        }
    }
}

}
