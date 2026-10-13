using System;
using System.Collections.Generic;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

partial class EndgameUiContract
{
    static void RiftWorldVisualContracts()
    {
        var source = new Color32[16 * 16];
        for (int i = 0; i < source.Length; i++) source[i] = new Color32((byte)(i % 256), 80, 140, (byte)(i % 255));
        var circle = EndgamePanel.BuildRiftTelegraphPixels(source, 16, 16, 256, "circle", 200, 0, 0);
        var ring = EndgamePanel.BuildRiftTelegraphPixels(source, 16, 16, 256, "ring", 200, 80, 0);
        var line = EndgamePanel.BuildRiftTelegraphPixels(source, 16, 16, 256, "line", 35, 0, 500);
        var zeroLine = EndgamePanel.BuildRiftTelegraphPixels(source, 16, 16, 256, "line", 35, 0, 0);
        bool safe = true, danger = true, outside = true, rectangle = true, zero = true; int distinct = 0;
        var colors = new HashSet<byte>();
        for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
        {
            int i = y * 256 + x; double nx = (x + .5) / 128 - 1, ny = (y + .5) / 128 - 1, r = Math.Sqrt(nx * nx + ny * ny);
            if (r < .4) safe &= ring[i].a == 0;
            if (r > .42 && r < .99) danger &= ring[i].a > 0;
            if (r > 1) outside &= circle[i].a == 0 && ring[i].a == 0;
            rectangle &= line[i].a > 0; zero &= zeroLine[i].a == 0; colors.Add(circle[i].r);
        }
        distinct = colors.Count;
        Check(safe && danger && outside, "authored rune mask keeps exact circular exterior and conservatively clear donut safe center");
        Check(rectangle && zero, "line mask covers the whole flat-ended server rectangle; zero-length line is empty");
        Check(distinct > 10, "telegraph contains source-authored texture variation, not a solid white primitive");
        Check(circle[128 * 256 + 128].a > 0 && ring[128 * 256 + 128].a == 0, "circle danger center and donut safe center cannot be confused");
        Check(EndgamePanel.RiftArrivalFirstFutureStrike(0) == 0 && EndgamePanel.RiftArrivalFirstFutureStrike(180) == 1 && EndgamePanel.RiftArrivalFirstFutureStrike(5000) == 12,
            "arrival cursor excludes every elapsed strike on reconnect, including exact boundary");
        for (int i = 0; i < 12; i++)
        {
            Vector2 a = EndgamePanel.RiftArrivalStrikeOffset("18446744073709551615", i), b = EndgamePanel.RiftArrivalStrikeOffset("18446744073709551615", i);
            double radius = Math.Sqrt(a.x * a.x + a.y * a.y);
            Check(a.x == b.x && a.y == b.y && radius >= 35.99 && radius <= 155.01, "arrival strike is deterministic and surrounds future guardian " + i);
        }
        var oldWorld = GameBootstrap.I;
        GameBootstrap.I = new GameBootstrap { WorldRootForMapConstructionForIndependentRuntime = new Transform() };
        var c = new Client(); var p = new EndgamePanel(); p.Bind(c); QuietNegotiate(c);
        EffectPlayer.MagicFrames = new Sprite[20]; for (int i = 0; i < 20; i++) EffectPlayer.MagicFrames[i] = new Sprite();
        string globe = ",\"floorIndex\":1,\"globes\":[{\"entityId\":\"900\",\"x\":1000,\"y\":500,\"value\":20}]";
        Rift(c, "visual-run", "active", extra: globe); Call(p, "UpdateRiftWorldVisuals");
        var globes = (Dictionary<string, GameObject>)Get(p, "riftGlobeObjects");
        Check(globes.Count == 0, "unknown instance context never shows collectible-looking globes");
        RiftContext(c, true); Call(p, "UpdateRiftWorldVisuals");
        Check(globes.Count == 1, "authoritative active snapshot creates one real pearl sprite");
        GameObject pearl = globes["900"];
        Check(pearl.components.ContainsKey(typeof(SpriteRenderer)) && pearl.components.ContainsKey(typeof(FrameAnimator)), "globe uses a world sprite and native animator rather than text");
        var pearlRenderer = (SpriteRenderer)pearl.components[typeof(SpriteRenderer)];
        Check(ReferenceEquals(pearlRenderer.sharedMaterial, EffectPlayer.AlphaMaterial) && pearlRenderer.sortingOrder == 505 && pearl.transform.position.z == 0 && ReferenceEquals(pearl.transform.parent, GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime), "pearl uses native alpha material, world root, native Y sorting and zero depth");
        var animator = (FrameAnimator)pearl.components[typeof(FrameAnimator)];
        Check(animator.loader().Item1.Length == 20 && animator.frameMs == 100 && animator.loop, "globe plays all twenty verified original frames at native frame interval");
        Call(p, "UpdateRiftWorldVisuals"); Check(ReferenceEquals(pearl, globes["900"]), "repeat snapshot does not duplicate globe or reset its animation");
        Rift(c, "visual-run", "active", extra: ",\"floorIndex\":1"); Call(p, "UpdateRiftWorldVisuals");
        Check(globes.Count == 0 && pearl.destroyed && !pearl.active, "server pickup removal destroys exact globe immediately");
        Rift(c, "visual-run", "active", extra: globe); Call(p, "UpdateRiftWorldVisuals"); pearl = globes["900"];
        Rift(c, "visual-run", "active", extra: ",\"floorIndex\":2"); Call(p, "UpdateRiftWorldVisuals");
        Check(globes.Count == 0 && pearl.destroyed, "floor transition discards source-floor globe objects");
        string arrival = ",\"floorIndex\":2,\"guardianArrival\":" + ArrivalJson;
        EffectPlayer.Played.Clear(); Rift(c, "visual-run", "boss_arriving", extra: arrival); Call(p, "UpdateRiftWorldVisuals");
        Check(EffectPlayer.Played.Count == 0 && Get(p, "riftArrivalAnchor") != null, "arrival baseline creates original seal without replaying past lightning");
        typeof(EndgameClientState).GetField("<RiftReceivedUtc>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(c.Endgame, DateTime.UtcNow.AddMilliseconds(-220));
        EffectPlayer.NextScale = 2.75f; Call(p, "UpdateRiftWorldVisuals"); Check(EffectPlayer.NextScale == 2.75f, "arrival lightning never consumes another skill receipt scale"); EffectPlayer.NextScale = 1;
        Check(EffectPlayer.Played.Count == 1 && EffectPlayer.Played[0].gameObject.name == "TFX303", "native elite lightning is played once at future scheduled strike");
        Call(p, "UpdateRiftWorldVisuals"); Check(EffectPlayer.Played.Count == 1, "repeat update cannot replay same lightning");
        Call(p, "ClearRiftWorldVisuals");
        Check(EffectPlayer.Played[0].gameObject.destroyed, "disconnect clears owned effect instead of leaking it onto another map");
        Call(p, "UpdateRiftWorldVisuals"); Check(EffectPlayer.Played.Count == 1 && (int)Get(p, "riftArrivalNextStrike") == 1, "reconnect keeps elapsed strike skipped");
        typeof(EndgameClientState).GetField("<RiftReceivedUtc>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(c.Endgame, DateTime.UtcNow.AddMilliseconds(-3200));
        Call(p, "UpdateRiftWorldVisuals"); Check(EffectPlayer.Played.Count <= 2, "long frame stall skips obsolete lightning instead of bursting backlog");
        Rift(c, "visual-run", "boss", extra: ",\"floorIndex\":2"); Call(p, "UpdateRiftWorldVisuals");
        Check(Get(p, "riftArrivalAnchor") == null && (string)Get(p, "riftArrivalID") == "", "guardian spawn clears countdown seal and pre-arrival effects");
        Rift(c, "visual-run", "active", extra: globe); Call(p, "UpdateRiftWorldVisuals"); pearl = globes["900"];
        c.EndgameInWorld = false; Call(p, "UpdateRiftWorldVisuals");
        Check(globes.Count == 0 && pearl.destroyed, "world disconnect destroys globe even before a new sidecar snapshot");
        c.EndgameInWorld = true; Rift(c, "visual-run", "active", extra: globe);
        Call(p, "SyncRiftScenePortal"); Call(p, "UpdateRiftWorldVisuals");
        Check(globes.Count == 1, "fresh map snapshot may render world globe");
        GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime = new Transform();
        Call(p, "SyncRiftScenePortal"); Call(p, "UpdateRiftWorldVisuals");
        Check(globes.Count == 0, "old sidecar snapshot cannot repaint its globe into a replacement map root");
        Rift(c, "visual-run", "active", extra: globe); Call(p, "SyncRiftScenePortal"); Call(p, "UpdateRiftWorldVisuals");
        Check(globes.Count == 1, "fresh post-map snapshot resumes world markers");
        p.Unbind(c); EffectPlayer.MagicFrames = null; GameBootstrap.I = oldWorld;
    }
}
