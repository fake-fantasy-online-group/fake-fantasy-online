using System;
using System.Collections.Generic;

// Deterministic Unity/resource substitutes. The unchanged production owner under
// test is compiled separately; this does not prove Unity or Player rendering.
namespace UnityEngine
{
    public class Object
    {
        public bool destroyed;
        public static implicit operator bool(Object value) => value != null && !value.destroyed;
        public static void Destroy(Object value) { if (value != null) value.destroyed = true; }
    }
    public class Transform { public void SetParent(Transform parent, bool worldPositionStays) { } }
    public class GameObject : Object
    {
        public Transform transform = new Transform(); public bool activeSelf = true;
        public GameObject(string name) { } public void SetActive(bool active) => activeSelf = active;
        public T AddComponent<T>() where T : new() => new T();
    }
    public class SpriteRenderer : Object { public int sortingOrder; public Material sharedMaterial; public Sprite sprite; }
    public class Material : Object { public Material(Shader shader) { } }
    public class Shader : Object { public static Shader Find(string name) => new Shader(); }
    public class Texture2D : Object
    {
        public static int created; public int width, height; public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Color32[] pixels;
        public Texture2D(int width, int height, TextureFormat format, bool mipmap) { this.width = width; this.height = height; created++; }
        public void SetPixels32(Color32[] value) => pixels = value;
        public void Apply(bool updateMipmaps, bool makeNoLongerReadable) { }
        public Color GetPixelBilinear(float x, float y) => new Color { a = 1 };
    }
    public struct Color { public float a; }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static explicit operator Color32(Color color) => new Color32(255, 255, 255, (byte)(255 * color.a));
    }
    public enum TextureFormat { RGBA32 } public enum FilterMode { Point } public enum TextureWrapMode { Clamp }
    public struct Rect { public Rect(float x, float y, float width, float height) { } }
    public struct Vector2 { public Vector2(float x, float y) { } }
    public class Sprite : Object
    {
        public Texture2D texture;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float pixelsPerUnit) => new Sprite { texture = texture };
    }
    public static class Mathf { public static float Max(float a, float b) => Math.Max(a, b); }
    public static class Time { public static float time; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    using UnityEngine;
    public class ResLoader : Object { public static ResLoader I; public object Pkg, Pkg24, PkgFf21; }
    public partial class MonsterEntity
    {
        public Transform transform = new Transform(); private float ppu = 100, headTopU = 0; private bool dying = false;
        public void TickForTest() => TickStatusIcons();
        public Sprite SpriteForTest => siSr == null ? null : siSr.sprite;
        public bool ActiveForTest => siGo && siGo.activeSelf;
    }
    public static class PlayerNameTag { public static void PlaceSharp(Transform transform, float y, float ppu) { } }
    public static class GameHud
    {
        public static bool ready; public static int calls; public static int lastId;
        private static readonly Texture2D original = new Texture2D(16, 16, TextureFormat.RGBA32, false);
        public static Texture2D BuffIcon(int id) { calls++; lastId = id; return ready ? original : null; }
        public static bool OriginalDestroyed => original.destroyed;
    }
    internal static class MonsterStatusIconReadinessContract
    {
        private static int checks;
        private static readonly Func<long> AllocatedBytes = (Func<long>)Delegate.CreateDelegate(
            typeof(Func<long>), typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
        private static void Check(bool value, string message) { if (!value) throw new Exception(message); checks++; }
        private static List<MonsterEntity.StatusIcon> Rows(int icon = 5030, int milliseconds = -1) =>
            new List<MonsterEntity.StatusIcon> { new MonsterEntity.StatusIcon { status = 1001, icon = icon, remainMs = milliseconds } };
        private static bool Filled(MonsterEntity actor) => actor.SpriteForTest.texture.pixels[17].a > 0;
        private static MonsterEntity Missing(bool loader = true)
        {
            ResLoader.I = loader ? new ResLoader() : null; GameHud.ready = false; Time.time = 0;
            var actor = new MonsterEntity(); actor.SetStatusIcons(Rows()); Check(!Filled(actor), "initial miss stays empty, never substitute art");
            return actor;
        }
        private static void StableTicks(MonsterEntity actor)
        {
            int calls = GameHud.calls, textures = Texture2D.created;
            actor.TickForTest(); // Warm the branch before measuring managed allocations.
            long allocated = AllocatedBytes();
            for (int i = 0; i < 1000; i++) actor.TickForTest();
            long tickAllocations = AllocatedBytes() - allocated;
            Check(tickAllocations == 0, "stable ticks do not allocate managed objects");
            Check(GameHud.calls == calls, "stable owner/package epoch does not poll resources");
            Check(Texture2D.created == textures, "stable ticks do not allocate compositions");
        }
        public static int Main()
        {
            var actor = Missing(); var first = actor.SpriteForTest; int calls = GameHud.calls;
            StableTicks(actor); GameHud.ready = true; actor.SetStatusIcons(Rows());
            Check(GameHud.calls == calls + 1 && Filled(actor), "same-signature authoritative packet retries exact resource");
            Check(GameHud.lastId == 5030, "native poison resource identity is preserved");
            Check(first.destroyed && first.texture.destroyed, "replaced border composition is disposed");
            var cached = actor.SpriteForTest; calls = GameHud.calls;
            actor.SetStatusIcons(Rows()); StableTicks(actor);
            Check(actor.SpriteForTest == cached && GameHud.calls == calls, "successful identical packet keeps composition");
            Check(!GameHud.OriginalDestroyed, "borrowed original resource is never destroyed");

            actor = Missing(false); GameHud.ready = true; ResLoader.I = new ResLoader(); actor.TickForTest();
            Check(Filled(actor), "new loader readiness recovers without another packet"); StableTicks(actor);
            foreach (int package in new[] { 0, 1, 2 })
            {
                actor = Missing(); calls = GameHud.calls;
                if (package == 0) ResLoader.I.Pkg = new object();
                else if (package == 1) ResLoader.I.Pkg24 = new object();
                else ResLoader.I.PkgFf21 = new object();
                actor.TickForTest(); Check(GameHud.calls == calls + 1, "one attempt per package identity change");
                StableTicks(actor); GameHud.ready = true;
                if (package == 0) ResLoader.I.Pkg = new object();
                else if (package == 1) ResLoader.I.Pkg24 = new object();
                else ResLoader.I.PkgFf21 = new object();
                actor.TickForTest(); Check(Filled(actor), "each package readiness can recover exact artwork");
            }
            actor = Missing(); GameHud.ready = true; ResLoader.I = new ResLoader(); actor.TickForTest();
            Check(Filled(actor), "replacement owner recovers exact artwork");

            actor = new MonsterEntity(); calls = GameHud.calls; actor.SetStatusIcons(Rows(0));
            ResLoader.I = new ResLoader(); actor.TickForTest(); actor.SetStatusIcons(Rows(0));
            Check(GameHud.calls == calls && !Filled(actor), "authored zero is not loaded, substituted or retried");
            actor.SetStatusIcons(Rows()); Check(Filled(actor), "positive update replaces authored zero");
            cached = actor.SpriteForTest; actor.SetStatusIcons(Rows(0));
            Check(!Filled(actor) && cached.destroyed, "zero refresh discards old positive artwork");

            actor = Missing(); var mixed = Rows(5030, 100); mixed.Add(new MonsterEntity.StatusIcon { status = 1004, icon = 7087, remainMs = -1 });
            actor.SetStatusIcons(mixed); Time.time = 0.2f; actor.TickForTest();
            Check(actor.SpriteForTest.texture.width == 16, "expiry composes only surviving status");
            StableTicks(actor); GameHud.ready = true; actor.SetStatusIcons(Rows(7087));
            Check(Filled(actor) && GameHud.lastId == 7087, "status event after expiry can recover surviving native art");
            actor.SetStatusIcons(Rows(5030, 100)); Time.time = 1; actor.TickForTest();
            Check(!actor.ActiveForTest, "last expiry hides status view"); StableTicks(actor);
            actor.SetStatusIcons(Rows()); Check(actor.ActiveForTest, "new publication after expiry restores visibility");
            cached = actor.SpriteForTest; actor.ReleaseStatusIconsForIndependentRuntime();
            Check(cached.destroyed && cached.texture.destroyed && actor.SpriteForTest == null, "release disposes owned sprite and composition");
            Check(!actor.ActiveForTest, "release hides root");
            ResLoader.I = new ResLoader(); StableTicks(actor);
            actor.ReleaseStatusIconsForIndependentRuntime(); Check(!GameHud.OriginalDestroyed, "repeat release never destroys shared source");
            actor.SetStatusIcons(Rows()); Check(Filled(actor), "released owner can rebuild a later authoritative row");
            actor.SetStatusIcons(new List<MonsterEntity.StatusIcon>()); Check(!actor.ActiveForTest, "empty publication hides row");
            StableTicks(actor);
            Console.WriteLine("PASS " + checks + " monster status readiness, exact-art, bounded retry, expiry and disposal assertions (managed substitutes)");
            return 0;
        }
    }
}
