using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

public sealed partial class ReferenceFirstWorldWindowOwner
{
    private long skillCastStateVersion = long.MinValue;

    private void ResetSkillCastObservationForIndependentRuntime()
    {
        skillCastStateVersion = long.MinValue;
    }

    // Cast history is an observation. Receipt-time native execution is independent of this
    // window, HUD readiness, frame clock and actor availability; it is never replayed here.
    private bool SyncLocalSkillCast(bool force, float now)
    {
        skillCastStateVersion = session.SkillCastStateVersion;
        return true;
    }

    private void SyncLocalSkillFacingComponent()
    {
        if (frame == null || frame.Player == null || frame.Player.Root == null)
        {
            return;
        }

        // The actual component owns its identity and facing. A borrowed snapshot must
        // neither recreate it nor write an earlier observed direction back into its frame.
        frame.CombatOwnerForIndependentRuntime?.ActivateForIndependentRuntime();
        frame.Player.ObserveNativeFacingForIndependentRuntime();
    }

    private ActorEffects EnsureSkillActorEffects(
        Transform owner,
        SpriteRenderer renderer,
        Func<(int, bool)> facing,
        float explicitHeight = -1f)
    {
        if (!owner)
        {
            return null;
        }

        RemotePlayer remote = owner.GetComponent<RemotePlayer>();
        if (remote) return remote.EnsureEffectsForIndependentRuntime();
        PlayerController local = owner.GetComponent<PlayerController>();
        if (local) return local.EnsureEffectsForIndependentRuntime();

        ActorEffects effects = owner.GetComponent<ActorEffects>();
        if (!effects)
        {
            effects = owner.gameObject.AddComponent<ActorEffects>();
        }

        float height = explicitHeight >= 0f
            ? explicitHeight
            : renderer && renderer.sprite
                ? renderer.sprite.bounds.size.y
                : 0f;
        float ppu = frame != null && frame.CameraFollow != null
            ? frame.CameraFollow.PixelsPerUnit
            : ReferenceWorldLocalPlayerDoll.PixelsPerUnit;
        effects.ConfigureForIndependentRuntime(
            ppu,
            height,
            Vector2.zero,
            renderer,
            facing);
        return effects;
    }

}

}
