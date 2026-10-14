using System;
using System.Collections.Generic;
using System.Globalization;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
public sealed partial class EndgamePanel
{
    // Verified original magic5202.gso: twenty 100ms frames of a blue pearl.
    // TFX303 is the existing elite birth lightning, not a newly invented effect.
    private const int RiftGlobeMagic = 5202;
    private const string RiftArrivalLightning = "TFX303";
    private readonly Dictionary<string, GameObject> riftGlobeObjects = new Dictionary<string, GameObject>();
    private readonly Dictionary<int, MonsterEntity> riftAuraObjects = new Dictionary<int, MonsterEntity>();
    private readonly List<EffectPlayer> riftArrivalEffects = new List<EffectPlayer>();
    private string riftVisualRun = "", riftArrivalID = "";
    private int riftVisualFloor, riftArrivalNextStrike;
    private long riftVisualGeneration = -1;
    private GameObject riftArrivalAnchor;

    private bool RiftWorldVisualsReady(EndgameRiftView rift) => client != null && client.EndgameInWorld &&
        State.RiftContextKnown && State.RiftInInstance && rift != null && ActiveRun(rift) &&
        !rift.Transitioning && !ReferenceEquals(rift, riftBlockedView) && GameBootstrap.CurPpu > 0 &&
        GameBootstrap.I != null && !GameBootstrap.I.Switching && GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime != null &&
        (riftObservedRoot == null || riftObservedRoot == GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime);

    internal static Vector3 RiftWorldPoint(float x, float y) => new Vector3(
        (x - GameBootstrap.CurWorldW * .5f) / GameBootstrap.CurPpu,
        (GameBootstrap.CurWorldH * .5f - y) / GameBootstrap.CurPpu, 0);

    private void UpdateRiftWorldVisuals()
    {
        EndgameRiftView rift = State.Rift;
        if (!RiftWorldVisualsReady(rift)) { ClearRiftWorldVisuals(); return; }
        if (riftVisualRun != rift.RunID || riftVisualFloor != rift.FloorIndex || riftVisualGeneration != State.Generation)
        {
            ClearRiftWorldVisuals();
            riftVisualRun = rift.RunID; riftVisualFloor = rift.FloorIndex; riftVisualGeneration = State.Generation;
        }
        var liveAuras = new HashSet<int>();
        foreach (var aura in rift.Auras)
        {
            if (aura.Class < 0 || aura.Class > 3 || aura.Class == 0 && string.IsNullOrEmpty(aura.CloneOf)) continue;
            MonsterEntity monster = CombatWorld.EndgameMonster(aura.NativeEntityID);
            if (monster == null) continue; // Native spawn can arrive after the sidecar snapshot.
            liveAuras.Add(aura.NativeEntityID);
            if (riftAuraObjects.TryGetValue(aura.NativeEntityID, out MonsterEntity previous) && previous != null && previous != monster)
                previous.ClearRiftIdentity();
            riftAuraObjects[aura.NativeEntityID] = monster;
            monster.UpdateRiftIdentity(aura.Class, aura.OriginalName);
        }
        var removedAuras = new List<int>();
        foreach (var pair in riftAuraObjects)
            if (!liveAuras.Contains(pair.Key)) { if (pair.Value != null) pair.Value.ClearRiftIdentity(); removedAuras.Add(pair.Key); }
        foreach (int id in removedAuras) riftAuraObjects.Remove(id);

        var liveGlobes = new HashSet<string>();
        // Progress pickups are server-owned and collectible only in the active phase.
        // Never hide one on proximity or award progress before the server removes its ID.
        if (rift.Phase == "active") foreach (var globe in rift.Globes)
        {
            liveGlobes.Add(globe.EntityID);
            if (!riftGlobeObjects.TryGetValue(globe.EntityID, out GameObject root) || root == null)
            {
                root = CreateRiftMagicSprite("RiftGlobe:" + globe.EntityID, RiftGlobeMagic,
                    RiftWorldPoint(globe.X, globe.Y), 1.65f, (int)Math.Max(0, Math.Min(30000, globe.Y)) + 5);
                if (root != null) riftGlobeObjects[globe.EntityID] = root;
            }
        }
        var removedGlobes = new List<string>();
        foreach (var pair in riftGlobeObjects)
            if (!liveGlobes.Contains(pair.Key)) { if (pair.Value != null) ReleaseRiftWorldObject(pair.Value); removedGlobes.Add(pair.Key); }
        foreach (string id in removedGlobes) riftGlobeObjects.Remove(id);
        UpdateRiftEliteVisuals(rift);
        UpdateRiftArrival(rift);
        PruneRiftTelegraphArtwork(rift);
    }

    private static GameObject CreateRiftMagicSprite(string name, int magic, Vector3 position, float scale, int order)
    {
        // These two verified resources have one direction. The native all-frame
        // accessor pins its cache entry, protecting FrameAnimator borrowers from
        // the effect-only eviction scan (which cannot see standalone sprites).
        Sprite[] frames = EffectPlayer.GetMagicFramesAll(magic);
        if (frames == null || frames.Length == 0 || frames[0] == null) return null;
        var root = new GameObject(name);
        root.transform.position = position;
        if (GameBootstrap.I != null && GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime != null)
            root.transform.SetParent(GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime, true);
        root.transform.localScale = new Vector3(scale, scale, 1);
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = frames[0]; renderer.sortingOrder = order;
        // Match the existing .eff blend: pearl5202 is alpha, birth seal341 is additive.
        renderer.sharedMaterial = magic == 341 ? EffectPlayer.BorrowAdditiveMaterial() : EffectPlayer.BorrowAlphaMaterial();
        var animator = root.AddComponent<FrameAnimator>();
        animator.frameMs = 100; animator.loop = true; animator.loader = () => (frames, null);
        return root;
    }

    private void UpdateRiftArrival(EndgameRiftView rift)
    {
        EndgameRiftGuardianArrivalView arrival = rift.GuardianArrival;
        if (rift.Phase != "boss_arriving" || arrival == null) { ClearRiftArrival(); return; }
        int remaining = RiftArrivalRemainingMS(rift, State.RiftReceivedUtc, DateTime.UtcNow);
        int elapsed = arrival.DurationMS - remaining;
        if (remaining <= 0) { ClearRiftArrival(); return; }
        if (riftArrivalID != arrival.ID)
        {
            ClearRiftArrival(); riftArrivalID = arrival.ID;
            // Reconnect/new observer starts at the current point; historical lightning
            // is never replayed. Subsequent snapshots cannot rewind this cursor.
            riftArrivalNextStrike = RiftArrivalFirstFutureStrike(elapsed);
            riftArrivalAnchor = CreateRiftMagicSprite("RiftGuardianArrival", 341,
                RiftWorldPoint(arrival.X, arrival.Y), 1.6f, -4400) ?? new GameObject("RiftGuardianArrival");
        }
        while (riftArrivalNextStrike < 12 && RiftArrivalStrikeMS(riftArrivalNextStrike) <= elapsed)
        {
            int strike = riftArrivalNextStrike++;
            // A long stall skips old effects instead of firing a backlog at once.
            if (elapsed - RiftArrivalStrikeMS(strike) > 180) continue;
            Vector2 offset = RiftArrivalStrikeOffset(arrival.Seed, strike);
            EffectPlayer effect;
            float pendingScale = EffectPlayer.NextScale;
            try
            {
                EffectPlayer.NextScale = 1f;
                effect = EffectPlayer.PlayNamed(RiftWorldPoint(arrival.X + offset.x, arrival.Y + offset.y),
                    RiftArrivalLightning, GameBootstrap.CurPpu);
            }
            finally { EffectPlayer.NextScale = pendingScale; }
            if (effect != null)
            {
                // Attach to an owned root to prevent native effect pooling from making
                // a retained reference point at a later unrelated skill effect.
                if (riftArrivalAnchor != null) effect.transform.SetParent(riftArrivalAnchor.transform, true);
                riftArrivalEffects.Add(effect);
            }
        }
    }
    internal static int RiftArrivalStrikeMS(int index) => 180 + index * 390;
    internal static int RiftArrivalFirstFutureStrike(int elapsed)
    {
        int index = 0; while (index < 12 && RiftArrivalStrikeMS(index) <= elapsed) index++; return index;
    }
    internal static Vector2 RiftArrivalStrikeOffset(string seed, int index)
    {
        ulong value; ulong.TryParse(seed, NumberStyles.None, CultureInfo.InvariantCulture, out value);
        // Stable unsigned mixing, independent of System.Random/runtime implementation.
        unchecked
        {
            value += 0x9e3779b97f4a7c15UL * (ulong)(index + 1);
            value = (value ^ value >> 30) * 0xbf58476d1ce4e5b9UL;
            value = (value ^ value >> 27) * 0x94d049bb133111ebUL; value ^= value >> 31;
        }
        double angle = (value & 65535) * (Math.PI * 2 / 65536);
        float radius = 36 + (int)((value >> 16) % 120);
        return new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius);
    }
    private static void ReleaseRiftWorldObject(GameObject root)
    {
        if (root == null) return;
        root.SetActive(false); // Destroy is deferred; never flash old markers during a map edge.
        Destroy(root);
    }
    private void ClearRiftArrival()
    {
        foreach (EffectPlayer effect in riftArrivalEffects) if (effect != null) ReleaseRiftWorldObject(effect.gameObject);
        riftArrivalEffects.Clear();
        if (riftArrivalAnchor != null) ReleaseRiftWorldObject(riftArrivalAnchor);
        riftArrivalAnchor = null; riftArrivalID = ""; riftArrivalNextStrike = 0;
    }
    private void ClearRiftWorldVisuals()
    {
        ClearRiftEliteVisuals();
        foreach (var pair in riftGlobeObjects) if (pair.Value != null) ReleaseRiftWorldObject(pair.Value);
        riftGlobeObjects.Clear();
        foreach (var pair in riftAuraObjects) if (pair.Value != null) pair.Value.ClearRiftIdentity();
        riftAuraObjects.Clear(); ClearRiftArrival(); ClearRiftTelegraphArtwork();
        riftVisualRun = ""; riftVisualFloor = 0; riftVisualGeneration = -1;
    }
    internal static bool IsRiftMonsterCast(int entityID)
    {
        EndgamePanel panel = Current; var rift = panel?.State.Rift;
        if (panel == null || !panel.RiftWorldVisualsReady(rift)) return false;
        foreach (var aura in rift.Auras) if (aura.NativeEntityID == entityID && aura.Class > 0) return true;
        return false;
    }
}
}
