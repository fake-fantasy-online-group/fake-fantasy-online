using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public partial class CombatWorld
{
    private readonly Dictionary<string, EffectPlayer> fixedGroundEffects = new Dictionary<string, EffectPlayer>();
    public void PlayGroundEffect(WorldSkillHitEffect edge)
    {
        if (edge == null || !edge.IsGround) return;
        string key = edge.EntityId.ToString() + ":" + edge.Key;
        EffectPlayer old;
        if (fixedGroundEffects.TryGetValue(key, out old)) { if (old) old.Stop(); fixedGroundEffects.Remove(key); }
        if (edge.Mode == 2) return;
        // Remove completed entries without retaining every past one-shot key forever.
        var expired = new List<string>();
        foreach (var item in fixedGroundEffects) if (!item.Value) expired.Add(item.Key);
        foreach (string item in expired) fixedGroundEffects.Remove(item);
        float scale = ppu > 0 ? ppu : 100f;
        Vector3 position = new Vector3((edge.X - worldW * 0.5f) / scale, (worldH * 0.5f - edge.Y) / scale, 0f);
        EffectPlayer effect = edge.Mode == 1
            ? EffectPlayer.PlaySustained(position, edge.Effect, scale, null, edge.DurationMS / 1000f)
            : EffectPlayer.PlayNamed(position, edge.Effect, scale, 1, -1, false, false);
        if (effect) fixedGroundEffects[key] = effect;
    }
    internal void ClearGroundEffects()
    {
        foreach (var effect in fixedGroundEffects.Values) if (effect) effect.Stop();
        fixedGroundEffects.Clear();
    }
}
}
