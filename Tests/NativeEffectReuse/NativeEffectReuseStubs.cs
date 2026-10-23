// Only Unity presentation and external I/O boundaries are substitutes. The runner
// compiles the complete production EffectPlayer and verbatim resource resolver methods.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public class Object
    {
        public static implicit operator bool(Object value) { return value != null; }
    }
    public class Component : Object
    {
        public GameObject gameObject;
        public Transform transform { get { return gameObject == null ? null : gameObject.transform; } }
    }
    public class MonoBehaviour : Component
    {
        protected static void Destroy(Object value) { }
        protected void StopAllCoroutines() { }
    }
    public class GameObject : Object
    {
        public string name;
        public readonly Transform transform = new Transform();
        public GameObject(string name) { this.name = name; }
        public void SetActive(bool value) { }
        public T AddComponent<T>() where T : Component, new() { return new T { gameObject = this }; }
    }
    public class Transform : Object
    {
        public Vector3 position, localPosition, localScale;
        public Quaternion localRotation;
        public void SetParent(Transform parent, bool worldPositionStays) { }
    }
    public class Sprite : Object { }
    public class Material : Object { }
    public class MaterialPropertyBlock { }
    public class SpriteRenderer : Component
    {
        public Sprite sprite;
        public bool enabled, flipX;
        public Color color;
        public int sortingOrder;
    }
    public struct Quaternion { public static Quaternion identity { get { return new Quaternion(); } } }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero { get { return new Vector2(); } }
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t)
        { return new Vector2(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t)); }
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero { get { return new Vector3(); } }
        public static Vector3 one { get { return new Vector3(1, 1, 1); } }
        public static Vector3 Lerp(Vector3 a, Vector3 b, float t)
        { return new Vector3(Mathf.Lerp(a.x, b.x, t), Mathf.Lerp(a.y, b.y, t), Mathf.Lerp(a.z, b.z, t)); }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
        public static Color Lerp(Color a, Color b, float t)
        { return new Color(Mathf.Lerp(a.r, b.r, t), Mathf.Lerp(a.g, b.g, t), Mathf.Lerp(a.b, b.b, t), Mathf.Lerp(a.a, b.a, t)); }
    }
    public static class Mathf
    {
        public static float Max(float a, float b) { return Math.Max(a, b); }
        public static float Lerp(float a, float b, float t) { return a + (b - a) * t; }
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal class ArtBudget<T> { public ArtBudget(string name, float budget, float max) { } }
    internal static class ArtDetail { public static event Action OnBudgetScaleChanged; }
    public partial class EffectPlayer
    {
        private static ArtBudget<string> _fxBudget;
        private static HashSet<string> _fxPinned;
        private const float FxBudgetBase = 1;
        public static string[] PrewarmList;
        private static void ApplyFxBudgetScale() { }
    }
    // Production is readonly false. Mutable only to exercise both ReadRes branches.
    internal static class SkillEffectDB { public static bool Use05Fx; }
    internal sealed class RecordingPackage
    {
        private readonly string label;
        public readonly Dictionary<string, byte[]> Data = new Dictionary<string, byte[]>();
        public RecordingPackage(string label) { this.label = label; }
        public byte[] Read(string path)
        {
            ResLoader.Calls.Add(label + ":" + path);
            byte[] value;
            return path != null && Data.TryGetValue(path, out value) ? value : null;
        }
    }
    internal partial class ResLoader
    {
        public static ResLoader I;
        public static readonly List<string> Calls = new List<string>();
        public readonly Dictionary<string, byte[]> Overlay = new Dictionary<string, byte[]>();
        public RecordingPackage Pkg = new RecordingPackage("05");
        public RecordingPackage Pkg24 = new RecordingPackage("q21");
        public RecordingPackage PkgFf21 = new RecordingPackage("ff21");
        private static readonly Dictionary<string, string> _res24Cache = new Dictionary<string, string>();
        private static string[] _res24Dirs;
        public static int Stat24Reads;
        public byte[] LoadRespatch(string path)
        {
            Calls.Add("overlay:" + path);
            byte[] value;
            return path != null && Overlay.TryGetValue(path, out value) ? value : null;
        }
        public static void Reset()
        {
            I = new ResLoader();
            Calls.Clear();
            _res24Cache.Clear();
            _res24Dirs = null;
            Stat24Reads = 0;
            SkillEffectDB.Use05Fx = false;
        }
    }
}
