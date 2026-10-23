using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    public void PlayChildSkillRelease(WorldSkillHitEffect edge)
    {
        if (edge == null || !edge.IsChildRelease) return;
        float scale = ppu > 0 ? ppu : 100f;
        Vector3 origin = new Vector3((edge.OriginX - worldW * 0.5f) / scale,
            (worldH * 0.5f - edge.OriginY) / scale, 0f);
        Vector3 center = new Vector3((edge.X - worldW * 0.5f) / scale,
            (worldH * 0.5f - edge.Y) / scale, 0f);
        Transform caster = ChildReleaseActor(edge.EntityId);
        Transform target = ChildReleaseActor(edge.TargetEntityId);
        SkillCaster.CastChildVisual(edge.SkillId, caster, target, scale, origin, center, edge.Mode == 1);
        if (edge.Mode == 0) SkillSoundDB.PlayFire(edge.SkillId);
    }

    private static Transform ChildReleaseActor(int id)
    {
        if (id == 0) return null;
        if (id == NetClient.SelfEntityId) return CombatNumber.PlayerTransform;
        RemotePlayer player = FindPlayer(id);
        return player != null ? player.transform : MonsterTransform(id);
    }
}
}
