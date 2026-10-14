using System;
using System.Collections;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

partial class EndgameUiContract
{
    private static EndgameRiftHazardView EliteHazard(string mechanism, int ability, long expiry, string extra = "")
        => new EndgameRiftHazardView(EndgameData.Object(BoundedJson.Parse("{\"id\":\"elite-h1\",\"sourceId\":\"900\",\"mechanism\":\"" + mechanism +
            "\",\"abilityId\":" + ability + ",\"x\":1000,\"y\":500,\"radius\":140,\"bornMS\":1000,\"expiresMS\":" + expiry +
            ",\"remainingMS\":" + (expiry - 1000) + extra + "}")));
    private static EndgameRiftImpactView EliteImpact(string mechanism)
        => new EndgameRiftImpactView(EndgameData.Object(BoundedJson.Parse("{\"id\":\"elite-h1\",\"mechanism\":\"" + mechanism +
            "\",\"x\":1000,\"y\":500,\"radius\":140,\"atMS\":4000,\"expiresMS\":4700}")));
    private static object ElitePart(RiftEliteWorldEffect effect, string key) => ((IDictionary)Get(effect, "parts"))[key];
    private static GameObject ElitePartRoot(RiftEliteWorldEffect effect, string key) => (GameObject)Get(ElitePart(effect, key), "Root");
    private static SpriteRenderer ElitePartSprite(RiftEliteWorldEffect effect, string key) => (SpriteRenderer)Get(ElitePart(effect, key), "Sprite");
    static void RiftEliteVisualContracts()
    {
        Check(RiftEliteVisualPlan.OnceFrame(-1, 70, 10) == -1 && RiftEliteVisualPlan.OnceFrame(0, 70, 10) == 0 &&
            RiftEliteVisualPlan.OnceFrame(699, 70, 10) == 9 && RiftEliteVisualPlan.OnceFrame(700, 70, 10) == -1,
            "lava native ten-frame burst is bounded and never loops after late delivery");
        Check(RiftEliteVisualPlan.OnceFrame(180 - 180, 60, 8) == 0 && RiftEliteVisualPlan.OnceFrame(659 - 180, 60, 8) == 7 &&
            RiftEliteVisualPlan.OnceFrame(660 - 180, 60, 8) == -1, "ice fragments use all eight native frames after exact180ms delay");
        Check(Math.Abs(RiftEliteVisualPlan.IceChargeScale(0) - .46) < .0001 && Math.Abs(RiftEliteVisualPlan.IceChargeScale(500) - .8) < .0001 &&
            Math.Abs(RiftEliteVisualPlan.IceChargeScale(1000) - .46) < .0001, "ice first native frame pulses uniformly without rotation or mirroring");
        Check(Math.Abs(RiftEliteVisualPlan.LavaCoreScale(140, 256) * 256 - 84) < .0001 &&
            Math.Abs(RiftEliteVisualPlan.DiameterScale(140, 150) * 150 - 280) < .0001, "lava peak diameter equals server diameter and idle core never exceeds30percent");
        Check(!RiftEliteVisualPlan.Ring(999, 1000, 2000, 800, 220, 40, out _, out _, out _), "no lightning pulse before authoritative first pulse");
        Check(RiftEliteVisualPlan.Ring(1400, 1000, 2000, 800, 220, 40, out float inner, out float outer, out double pulse) && inner == 70 && outer == 110 && pulse == 400,
            "lightning current annulus uses exact propagation radius and safe inner radius");
        Check(!RiftEliteVisualPlan.Ring(1801, 1000, 2000, 800, 220, 40, out _, out _, out _), "electric inter-pulse gap never replays old ring");
        RiftEliteVisualPlan.Wall(10, 20, 10, 116, 20, out double cx, out double cy, out double length, out double angle);
        Check(cx == 10 && cy == 68 && length == 96 && angle == -90, "wall center/axis derives from actual server segment in down-positive map coordinates");
        var oldLoader = ResLoader.I; var oldWorld = GameBootstrap.I; var oldCamera = Camera.main;
        try
        {
            ResLoader.I = new ResLoader(); DimgDecoder.Bytes = new byte[] { 1 }; Camera.main = null;
            GameBootstrap.I = new GameBootstrap { WorldRootForMapConstructionForIndependentRuntime = new Transform() };
            DimgDecoder.Factory = () =>
            {
                var native = new DimgDecoder.FrameTex[41];
                for (int i = 0; i < native.Length; i++)
                {
                    var texture = new Texture2D(8, 6, TextureFormat.RGBA32, false, false);
                    var pixels = new Color32[48]; for (int p = 0; p < pixels.Length; p++) pixels[p] = new Color32((byte)i, 200, 240, 255);
                    texture.SetPixels32(pixels); native[i] = new DimgDecoder.FrameTex { tex = texture, nLeft = 2, nTop = 3 };
                }
                return new DimgDecoder.Anim { ok = true, f = native, scale = 1, boundCx = 5, boundCy = 8 };
            };
            var assets = new RiftEliteVisualAssets();
            var lava = new RiftEliteWorldEffect("lava-test", GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime, assets);
            var warning = EliteHazard("death_lava", 2, 4000);
            lava.Hazard(warning, 3900);
            Check(ElitePartRoot(lava, "core").active && ((IDictionary)Get(lava, "parts")).Count == 1,
                "unresolved death warning creates only native4149 core and never a guessed burst");
            GameObject core = ElitePartRoot(lava, "core"); lava.Hazard(warning, 4000);
            Check(!core.active && ((IDictionary)Get(lava, "parts")).Count == 1, "expiry hides charge without fabricating an impact");
            lava.Impact(EliteImpact("death_lava"), 4350);
            Check(ElitePartSprite(lava, "burst").sprite == assets.Magic(420).Frames[5], "late impact renders current native frame five and skips historical frames");
            var burst = ElitePartRoot(lava, "burst"); lava.Impact(EliteImpact("death_lava"), 4350);
            Check(ReferenceEquals(burst, ElitePartRoot(lava, "burst")), "duplicate receipts reuse a single world effect");
            lava.Impact(EliteImpact("death_lava"), 4700); Check(!burst.active, "burst stops at authoritative700ms receipt expiry");
            var ice = new RiftEliteWorldEffect("ice-test", GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime, assets);
            ice.Hazard(EliteHazard("ice_orb", 4, 4000), 2500);
            var iceRenderer = ElitePartSprite(ice, "ice");
            Check(iceRenderer.sprite == assets.Magic(147).Frames[0] && !iceRenderer.flipX && !iceRenderer.flipY,
                "ice charge always samples native first frame in its original orientation");
            ice.Impact(EliteImpact("ice_orb"), 4180);
            Check(ElitePartSprite(ice, "ice").sprite == assets.Magic(147).Frames[3] && ElitePartSprite(ice, "shards").sprite == assets.Magic(43).Frames[0],
                "confirmed ice impact aligns crystal frame3 with fragments frame0");
            var electricity = new RiftEliteWorldEffect("electric-test", GameBootstrap.I.WorldRootForMapConstructionForIndependentRuntime, assets);
            electricity.Hazard(EliteHazard("lightning_orb", 3, 11000, ",\"width\":40,\"firstPulseMS\":1000,\"pulseMS\":2000,\"propagationMS\":800"), 2400);
            var mesh = (Mesh)Get(ElitePart(electricity, "discharge"), "Mesh");
            Check(mesh.vertices.Length == 258 && mesh.triangles.Length == 768, "ring uses world annular topology with no filled safe center");
            foreach (Vector3 v in mesh.vertices)
            {
                double radius = Math.Sqrt(v.x * v.x + v.y * v.y) * GameBootstrap.CurPpu;
                Check(Math.Abs(radius - 30) < .0001 || Math.Abs(radius - 70) < .0001, "electric mesh vertex agrees with current inner/outer radius");
            }
            var nativeBody = new GameObject("nativeActor").AddComponent<SpriteRenderer>(); nativeBody.sprite = assets.Magic(147).Frames[0];
            nativeBody.sharedMaterial = new Material(); var original = nativeBody.sharedMaterial;
            var actor = new GameObject("actorEffects").AddComponent<RiftEliteActorVisual>(); actor.Bind(nativeBody, assets);
            actor.SetState(true, false, false, 4200);
            Check(!ReferenceEquals(nativeBody.sharedMaterial, original) && EffectPlayer.AlphaMaterial.floats.Count == 0,
                "gold uses owned tint material and never mutates borrowed native material");
            Check(Get(actor, "goldenRing") != null && Get(actor, "shield") != null, "gold active state includes distinct0201 rune and0080 shield");
            actor.SetState(false, false, true, 4300);
            Check(Math.Abs(nativeBody.color.a - .62) < .0001 && ((Material)Get(actor, "tintMaterial")).colors["_Tint"].b == 1,
                "native clone actor becomes cool desaturated and translucent");
            actor.Release(); actor.Release(); Check(ReferenceEquals(nativeBody.sharedMaterial, original), "actor cleanup restores original material once");
            lava.Dispose(); lava.Dispose(); ice.Dispose(); electricity.Dispose();
            Sprite owned = assets.Magic(420).Frames[0]; assets.Dispose(); assets.Dispose();
            Check(owned.destroyed && owned.texture.destroyed, "scene-owned decoded sprites/textures are released without cache pins");
        }
        finally { ResLoader.I = oldLoader; GameBootstrap.I = oldWorld; Camera.main = oldCamera; DimgDecoder.Bytes = null; DimgDecoder.Factory = null; }
    }
}
