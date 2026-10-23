using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public partial class CombatWorld
{
    // Alive-PvP tail of 0600016c. The complete Update has already made the single target-kind
    // split and loss check. No extra target test, monster phase machine, local-death gate,
    // InRange call, or riding gate is inserted here.
    private void AdvancePvpAttackForIndependentRuntime()
    {
        if (!autoAttacking) return;
        // ARM 24921c B.LT and x86 256792 JB return on unordered as well as below.
        if (!(Time.time >= castPauseUntil)) return;

        bool inRange = false;
        Transform player = CombatNumber.PlayerTransform;
        if (player != null)
        {
            Vector3 playerPosition = player.position;
            Vector3 targetPosition = pvpTarget.transform.position;
            inRange = Vector2.Distance(
                          new Vector2(playerPosition.x, playerPosition.y),
                          new Vector2(targetPosition.x, targetPosition.y)) <= AtkRange;
        }

        bool notMoving = true;
        if (PlayerController.I != null) notMoving = !PlayerController.I.IsMoving;
        if (!inRange || !notMoving || !LosClear())
        {
            atkPhaseT = 0f;
            return;
        }

        atkPhaseT += Time.deltaTime;
        if (!(atkPhaseT >= AtkInterval)) return;
        atkPhaseT = 0f;

        // The connected/riding gate surrounds only the request. Receivers are read again after
        // their Unity comparison, as in the original, rather than retaining a pre-gate client.
        if (NetClient.I != null && NetClient.I.ConnectedForIndependentRuntime && !CharData.riding)
        {
            int targetId = pvpTarget.id;
            NetClient.I.AttackForIndependentRuntime(targetId);
        }

        if (PlayerController.I != null)
        {
            RemotePlayer swingTarget = pvpTarget;
            if ((object)swingTarget == null) throw new System.NullReferenceException();
            PlayerController playerController = PlayerController.I;
            Vector3 targetPosition = swingTarget.transform.position;
            float swingSeconds = AtkInterval;
            playerController.PlayAttackForIndependentRuntime(targetPosition, swingSeconds);
        }

        // Disconnected and riding frames still reach the swing above and projectile below.
        PlayPvpAttackProjectile();
    }

    private void PlayPvpAttackProjectile()
    {
        var projectile = AtkProjectile(CharData.weaponCType, CharData.AttackIdentityDistance);
        if (!projectile.HasValue) return;
        var specification = projectile.Value;
        Transform player = CombatNumber.PlayerTransform;
        // Both ARM 249788/24978c FCMP/B.LE and x86 256ba8/256bab UCOMISS/JBE take the
        // 100 fallback for nonpositive or NaN PPU, before testing the player's Unity null.
        float pixelsPerUnit = CombatNumber.Ppu > 0f ? CombatNumber.Ppu : 100f;
        if (player == null) return;
        Vector3 playerPosition = player.position;
        Vector3 targetPosition = pvpTarget.transform.position;
        float distance = Vector2.Distance(new Vector2(playerPosition.x, playerPosition.y),
            new Vector2(targetPosition.x, targetPosition.y));
        EffectPlayer.NextScale = specification.Item2;
        EffectPlayer effect = EffectPlayer.PlayNamed(player.position,
            specification.Item1, pixelsPerUnit, 1, -1, false, false);
        EffectPlayer.NextScale = 1f;
        if (effect != null)
        {
            // Re-read after PlayNamed; an exception leaves the preceding stores in place.
            Transform destination = pvpTarget.transform;
            float travelled = pixelsPerUnit * distance;
            // Raw correction to the older decompiled prose: ARM 2498d8/2498dc FCMP/FCSEL MI
            // and x86 256e58 MAXSS (product as second operand) propagate NaN, not the 80 floor.
            float milliseconds = travelled < 80f ? 80f : travelled;
            effect.MoveTo(destination, milliseconds);
        }
    }
}

}
