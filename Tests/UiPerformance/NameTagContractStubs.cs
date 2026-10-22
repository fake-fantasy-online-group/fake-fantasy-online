using System;
using UnityEngine;

// Deterministic API substitutes, not a Unity or GPU implementation.
namespace UnityEngine
{
    public class Object
    {
        public bool Destroyed;
        public static implicit operator bool(Object value) => value != null && !value.Destroyed;
        public static void Destroy(Object value) { if (value != null) value.Destroyed = true; }
    }
    public class MonoBehaviour : Object { public readonly Transform transform = new Transform(); }
    public class Transform : Object { public void SetParent(Transform parent, bool worldPositionStays) { } }
    public class GameObject : Object
    {
        public bool activeSelf = true; public readonly Transform transform = new Transform();
        public GameObject(string name) { }
        public void SetActive(bool active) => activeSelf = active;
        public T AddComponent<T>() where T : new() => new T();
    }
    public class SpriteRenderer : Object
    {
        public Sprite sprite; public int sortingOrder; public Material sharedMaterial;
        private Color tint = Color.white;
        public static int ColorSets;
        public Color color { get => tint; set { ColorSets++; tint = value; } }
    }
    public class Shader : Object { public static Shader Find(string name) => new Shader(); }
    public class Material : Object { public Material(Shader shader) { } }
    public class Sprite : Object
    {
        public Texture2D texture; public static int Creates;
        public static Sprite Create(Texture2D texture, Rect rect, Vector2 pivot, float ppu)
        { Creates++; return new Sprite { texture = texture }; }
    }
    public enum FilterMode { Point }
    public enum TextureWrapMode { Clamp }
    public enum TextureFormat { RGBA32 }
    public class Texture2D : Object
    {
        public readonly int width, height;
        public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Color32[] Pixels;
        public bool FailRead;
        public int Reads;
        public static int Created, AllReads, Applies, Sets;
        public static bool FailNextApply;
        public Texture2D(int w, int h, TextureFormat format, bool mipmap)
        { width = w; height = h; Pixels = new Color32[w * h]; Created++; }
        public Color32[] GetPixels32()
        {
            Reads++; AllReads++;
            if (FailRead) throw new InvalidOperationException("Injected atlas read failure");
            return (Color32[])Pixels.Clone();
        }
        public void SetPixels32(Color32[] pixels) { Sets++; Pixels = (Color32[])pixels.Clone(); }
        public void Apply(bool mipmap, bool unreadable)
        {
            Applies++;
            if (FailNextApply) { FailNextApply = false; throw new InvalidOperationException("Injected texture upload failure"); }
        }
        public static void ResetCounters() { Created = AllReads = Applies = Sets = Sprite.Creates = SpriteRenderer.ColorSets = 0; }
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
    }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x = x; this.y = y; } }
    public struct Vector3 { }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color black => new Color(0, 0, 0, 1);
        public static Color white => new Color(1, 1, 1, 1);
        public static implicit operator Color(Color32 c) => new Color(c.r / 255f, c.g / 255f, c.b / 255f, c.a / 255f);
        public static bool operator ==(Color a, Color b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;
        public static bool operator !=(Color a, Color b) => !(a == b);
        public override bool Equals(object other) => other is Color c && this == c;
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        private static byte Byte(float value) => (byte)Math.Round(Math.Max(0, Math.Min(1, value)) * 255);
        public static implicit operator Color32(Color c) => new Color32(Byte(c.r), Byte(c.g), Byte(c.b), Byte(c.a));
    }
    public static class Mathf
    {
        public static float Round(float value) => (float)Math.Round(value);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(int a, int b) => Math.Max(a, b);
    }
    public static class GUI
    {
        public static Color color;
        public static void DrawTextureWithTexCoords(Rect rect, Texture2D texture, Rect uv) { }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public class ResLoader : UnityEngine.Object
    {
        public static ResLoader I;
        public Texture2D Atlas;
        public readonly ushort[] Cells = new ushort[65536];
        public byte[] Widths = new byte[16];
        public ResLoader(int pattern = 0)
        {
            Atlas = new Texture2D(48, 48, TextureFormat.RGBA32, false);
            Array.Fill(Cells, ushort.MaxValue);
            const string chars = "AB名字怪物顶称底｜012345";
            for (int i = 0; i < chars.Length; i++) { Cells[chars[i]] = (ushort)i; Widths[i] = (byte)(i % 2 == 0 ? 12 : 7); }
            byte[] alpha = { 0, 7, 8, 64, 127, 254, 255 };
            for (int i = 0; i < Atlas.Pixels.Length; i++)
                Atlas.Pixels[i] = new Color32((byte)(i % 255), (byte)(255 - i % 255), 17, alpha[(i * 5 + i / 48 + pattern) % alpha.Length]);
        }
        public Texture2D LoadUI(string path) => Atlas;
        public byte[] LoadUIBytes(string path)
        {
            if (path.EndsWith(".wid")) return Widths;
            byte[] bytes = new byte[Cells.Length * 2]; Buffer.BlockCopy(Cells, 0, bytes, 0, bytes.Length); return bytes;
        }
    }
    internal static class TexScale
    {
        public static int Of(Texture2D t) => 1;
        public static float W(Texture2D t) => t.width;
        public static float H(Texture2D t) => t.height;
    }
    internal static class NetClient { public static int SelfEntityId = 1; }
    internal static class CharData { public static int level = 50, nameMode; public static bool petShow = true; }
    internal static class SocialClient { public static bool InParty(int owner) => false; }
    internal static class StallDlg { public static bool IsStallOwner(int owner) => false; }
    internal static class GameSettings { public static bool ShowMonsterNames = true; public static void EnsureLoaded() { } }
    internal static class PlayerNameTag { public static void PlaceSharp(Transform t, float y, float ppu, float x) { } }
    public abstract class TestMonster : MonoBehaviour, ISelectable
    {
        public string mname = "怪物A";
        public int Level = 60;
        public bool friendly, ally, dying;
        public int petOwnerId;
        protected GameObject nameTagGo;
        protected SpriteRenderer nameTagSr;
        protected static Material nameTagMat;
        protected float headTopU = 1, ppu = 12;
        public Transform SelTransform => transform;
        public string SelName => mname;
        public int SelLevel => Level;
        public int SelHp => 1;
        public int SelMaxHp => 1;
        public bool SelFriendly => friendly;
        public int SelOwnerId => petOwnerId;
        public Vector3 SelFeetPos => new Vector3();
    }
    public partial class MonsterEntity : TestMonster { }
    public partial class LegacyMonsterEntity : TestMonster { }
#if NAME_TAG_COLOR_OVERRIDE
    public partial class GameHud
    {
        public static Color Override = Color.white;
        public static Color TargetNameColor(ISelectable target) => Override;
    }
#endif
}
