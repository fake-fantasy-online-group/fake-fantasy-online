using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    private readonly Dictionary<string, RiftEliteWorldEffect> riftEliteEffects = new Dictionary<string, RiftEliteWorldEffect>();
    private RiftEliteVisualAssets riftEliteAssets;
    private void UpdateRiftEliteVisuals(EndgameRiftView rift)
    {
        if (!State.CanUseRiftHazards(GameBootstrap.CurMapId, GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime)) { ClearRiftEliteVisuals(); return; }
        double clock = EndgameClientState.MonotonicMS;
        State.RiftHazards.Tick(clock);
        double serverNow = State.RiftHazards.ServerTimeMS(clock);
        if (riftEliteAssets == null) riftEliteAssets = new RiftEliteVisualAssets();
        var live = new HashSet<string>(StringComparer.Ordinal);
        foreach (EndgameRiftHazardView hazard in State.RiftHazards.CurrentHazards)
        {
            string key = "hazard:" + hazard.ID; live.Add(key);
            EliteEffect(key).Hazard(hazard, serverNow);
        }
        // Only an explicit server receipt admits a detonation. Missing hazards, local deadlines,
        // reconnects and source deaths never synthesize one. A late receipt starts at its current frame.
        foreach (EndgameRiftImpactView impact in rift.Impacts)
        {
            if (serverNow < impact.AtMS || serverNow >= impact.ExpiresMS) continue;
            string key = "impact:" + impact.ID; live.Add(key);
            EliteEffect(key).Impact(impact, serverNow);
        }
        var removed = new List<string>();
        foreach (var pair in riftEliteEffects) if (!live.Contains(pair.Key)) { pair.Value.Dispose(); removed.Add(pair.Key); }
        foreach (string key in removed) riftEliteEffects.Remove(key);
        foreach (EndgameRiftAuraView aura in rift.Auras)
        {
            if (!riftAuraObjects.TryGetValue(aura.NativeEntityID, out MonsterEntity monster) || monster == null) continue;
            bool agility = false;
            foreach (int id in aura.AbilityIDs) if (id == 5) agility = true;
            monster.UpdateRiftEliteState(riftEliteAssets, aura.GoldenUntilMS > serverNow,
                agility, !string.IsNullOrEmpty(aura.CloneOf), serverNow);
        }
    }
    private RiftEliteWorldEffect EliteEffect(string key)
    {
        if (!riftEliteEffects.TryGetValue(key, out RiftEliteWorldEffect effect))
        {
            effect = new RiftEliteWorldEffect(key, GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime, riftEliteAssets);
            riftEliteEffects.Add(key, effect);
        }
        return effect;
    }
    private void ClearRiftEliteVisuals()
    {
        foreach (var pair in riftEliteEffects) pair.Value.Dispose();
        riftEliteEffects.Clear();
        foreach (var pair in riftAuraObjects) if (pair.Value != null) pair.Value.ClearRiftEliteState();
        if (riftEliteAssets != null) riftEliteAssets.Dispose();
        riftEliteAssets = null;
    }
}
}
