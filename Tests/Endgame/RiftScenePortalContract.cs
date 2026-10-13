using System;
using System.Reflection;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

internal static class RiftScenePortalContract
{
    private static int checks;
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
    private static void Call(EndgamePanel panel, string method) => typeof(EndgamePanel).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(panel, null);
    private static GameObject Portal(EndgamePanel panel) => (GameObject)typeof(EndgamePanel).GetField("riftScenePortal", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(panel);
    private static void Tick(EndgamePanel panel) => Call(panel, "SyncRiftScenePortal");
    private static bool Near(float a, float b) => Math.Abs(a - b) < .0001f;
    private sealed class Client : IEndgameClient
    {
        public EndgameClientState Endgame { get; } = new EndgameClientState();
        public bool EndgameInWorld { get; set; } = true;
        public bool SendEndgame(EndgameRequest request) => Endgame.Begin(request);
        public bool RetryEndgame() => false;
        public bool EquipEndgameItem(EndgameItemView item) => false;
        public bool UnequipEndgameItem(EndgameItemView item) => false;
    }
    private static void Apply(Client c, string json) => c.Endgame.Apply(EndgameResponse.Parse(json));
    private static void Hello(Client c) { c.Endgame.Begin(new EndgameRequest("hello")); Apply(c, "{\"kind\":\"hello\",\"ok\":true,\"capabilities\":[\"endgame.v1\",\"endgame.rift-portals.v1\"]}"); }
    private static void Context(Client c, bool inside) => Apply(c, "{\"kind\":\"rift.context\",\"ok\":true,\"result\":{\"inInstance\":\"" + (inside ? "true" : "false") + "\"}}");
    private static void Rift(Client c, string phase = "active", int floor = 1, bool transitioning = false, int x = 720, int y = 960) => Apply(c, "{\"kind\":\"rift.state\",\"ok\":true,\"rift\":{\"runId\":\"scene-portal\",\"revision\":\"1\",\"phase\":\"" + phase + "\",\"floorIndex\":" + floor + ",\"exitX\":" + x + ",\"exitY\":" + y + ",\"transitioning\":" + (transitioning ? "true" : "false") + "}}");
    public static void Main()
    {
        var world = GameBootstrap.I = new GameBootstrap { WorldRootForMapConstructionForIndependentRuntime = new Transform() };
        GameBootstrap.CurWorldW = 2000; GameBootstrap.CurWorldH = 1500; GameBootstrap.CurPpu = 100;
        var resources = ResLoader.I = new ResLoader {
            Image = new ResLoader.ObjSprite { tex = new Texture2D(), nLeft = 2, nTop = 7, clipBy = 246, frames = 15 },
            Animation = new ResLoader.ObjAnim { frames = new[] { new Sprite(), new Sprite() }, nLeft = new[] { 2, 5 }, nTop = new[] { 7, 9 } }
        };
        var client = new Client(); var panel = new EndgamePanel(); panel.Bind(client); Hello(client); Rift(client);
        Tick(panel); Check(Portal(panel) == null && resources.Loads == 0, "unknown context never creates a portal");
        Context(client, false); Tick(panel); Check(Portal(panel) == null, "public saved run does not draw private portal");
        Context(client, true); Tick(panel); GameObject first = Portal(panel);
        Check(first != null && first.active && first.transform.parent == world.WorldRootForMapConstructionForIndependentRuntime, "portal belongs to settled private scene root");
        Check(resources.LastID == 0x013006a5 && resources.LastWidth == 219 && resources.LastHeight == 282, "verified native FY1701 asset identity and size");
        Check(Near(first.transform.position.x, -3.87f) && Near(first.transform.position.y, .29f), "frame0 native trim offset and centered projection");
        var renderer = (SpriteRenderer)first.components[typeof(SpriteRenderer)];
        Check(renderer.sprite == GameBootstrap.PortalSprite && renderer.sharedMaterial == GameBootstrap.PortalMaterial && renderer.sortingOrder == 960, "borrow source sprite material and positive ground-anchor order");
        var animator = (FrameAnimator)first.components[typeof(FrameAnimator)];
        Check(animator.frameMs == 200 && animator.loop, "original map-object animation timing");
        var frames = animator.loader();
        Check(ReferenceEquals(frames.Item1, resources.Animation.frames) && resources.AnimLoads == 1 && resources.LastPpu == 100, "borrow original loader animation frames");
        Check(Near(frames.Item2[0].x, first.transform.position.x) && Near(frames.Item2[0].y, first.transform.position.y) && Near(frames.Item2[1].x, -3.84f) && Near(frames.Item2[1].y, .27f), "per-frame trims preserve fixed authored gate anchor");
        Tick(panel); Rift(client); Tick(panel); Check(Portal(panel) == first && resources.Loads == 1, "idle and fresh same-floor refresh do not duplicate objects");
        world.Switching = true; Tick(panel); Check(Portal(panel) == null && !first.active && first.destroyed, "retire immediately before deferred destruction on switch");
        world.WorldRootForMapConstructionForIndependentRuntime = new Transform(); world.Switching = false; Tick(panel);
        Check(Portal(panel) == null, "old floor response cannot paint into a new map root");
        Rift(client, floor: 2); Tick(panel); GameObject second = Portal(panel);
        Check(second != null && second != first, "map-ready new floor refresh recreates exact gate");
        world.WorldRootForMapConstructionForIndependentRuntime = new Transform(); Tick(panel);
        Check(Portal(panel) == null && !second.active, "same-map root rebuild without observed switching still rejects stale view");
        Rift(client, floor: 2); Tick(panel); Check(Portal(panel) != null, "fresh same-floor reconnect state restores scene gate");
        foreach (string phase in new[] { "boss", "completed", "closed", "aborted", "failed", "reward_pending", "reserved" }) {
            Rift(client, phase); Tick(panel); Check(Portal(panel) == null, phase + " has no traversable seeded portal");
        }
        Rift(client, transitioning: true); Tick(panel); Check(Portal(panel) == null, "transition retires seeded portal");
        Rift(client); Tick(panel); GameObject beforePublic = Portal(panel); Context(client, false); Tick(panel);
        Check(Portal(panel) == null && !beforePublic.active && !GameBootstrap.PortalSprite.destroyed, "public scene clears only owned object, keeps shared sprite");
        Context(client, true); Rift(client); Tick(panel); client.EndgameInWorld = false; Tick(panel); Check(Portal(panel) == null, "disconnect/endgame world exit clears portal");
        client.EndgameInWorld = true; Tick(panel); GameObject beforeUnbind = Portal(panel); panel.Unbind(client);
        Check(Portal(panel) == null && !beforeUnbind.active, "unbind removes portal immediately");
        panel.Bind(client); Tick(panel); Call(panel, "OnDisable"); Check(Portal(panel) == null, "disabled panel cannot orphan portal");
        Tick(panel); Call(panel, "OnDestroy"); Check(Portal(panel) == null, "destroy cleans its gate");
        var position = (Vector3)typeof(EndgamePanel).GetMethod("RiftScreenPoint", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { new Camera(), 720f, 960f });
        Check(Near(position.x, -280) && Near(position.y, Screen.height + 210), "boss marker and warnings share native centered map projection");
        Check(!GameBootstrap.PortalSprite.destroyed && !GameBootstrap.PortalMaterial.destroyed, "scene lifecycle never destroys shared art/material");
        Console.WriteLine("PASS " + checks + " real-source rift portal placement/lifetime contracts; deterministic Unity substitutes, not a Unity render.");
    }
}
