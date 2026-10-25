// Deliberately small deterministic API substitutes. These tests never invoke a Unity engine.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { public bool destroyed; public static void Destroy(Object value) { if (value != null) value.destroyed = true; } }
    public class MonoBehaviour : Object { public bool isActiveAndEnabled = true; public Transform transform = new Transform(); public GameObject gameObject; }
    public class Transform { public Vector3 position, localPosition; public Transform parent; public Vector3 localScale = new Vector3(1,1,1); public Quaternion localRotation, rotation; public Vector3 right = new Vector3(1,0,0), up = new Vector3(0,1,0); public void SetParent(Transform value, bool worldPositionStays) { parent = value; } }
    public class GameObject : Object { public readonly string name; public Transform transform = new Transform(); public bool active = true; public readonly Dictionary<Type, object> components = new Dictionary<Type, object>(); public GameObject(string name) { this.name = name; } public void SetActive(bool value) { active = value; } public T AddComponent<T>() where T : new() { var result = new T(); components[typeof(T)] = result;
            if (result is MonoBehaviour mono) { mono.gameObject = this; mono.transform = transform; }
            if (result is SpriteRenderer sprite) { sprite.gameObject = this; }
            return result; } }
    public class Sprite : Object
    {
        public Texture2D texture = new Texture2D(); public Vector2 pivot; public float pixelsPerUnit = 100;
        public Bounds bounds => new Bounds { max = new Vector3((texture.width-pivot.x)/pixelsPerUnit,(texture.height-pivot.y)/pixelsPerUnit,0) };
        public static Sprite Create(Texture2D t, Rect r, Vector2 p, float ppu) => new Sprite { texture=t, pivot=new Vector2(p.x*t.width,p.y*t.height), pixelsPerUnit=ppu };
    }
    public struct Bounds { public Vector3 max; }
    public class Material : Object
    {
        public Texture2D mainTexture; public readonly Dictionary<string,float> floats = new Dictionary<string,float>(); public readonly Dictionary<string,Color> colors = new Dictionary<string,Color>();
        public Material() { } public Material(Material source) { foreach(var pair in source.floats) floats[pair.Key]=pair.Value; foreach(var pair in source.colors) colors[pair.Key]=pair.Value; }
        public void SetFloat(string key,float value) { floats[key]=value; } public void SetColor(string key,Color value) { colors[key]=value; }
    }
    public class SpriteRenderer : Object
    {
        public GameObject gameObject = new GameObject("sprite"); public Transform transform => gameObject.transform;
        public Sprite sprite; public Material sharedMaterial; public int sortingOrder, sortingLayerID; public bool enabled = true, flipX, flipY; public Color color = new Color(1,1,1,1);
    }
    public class Mesh : Object { public Vector3[] vertices; public Vector2[] uv; public Color[] colors; public int[] triangles; public void Clear() { } public void RecalculateBounds() { } }
    public class MeshFilter { public Mesh sharedMesh; }
    public class MeshRenderer { public Material sharedMaterial; public int sortingOrder, sortingLayerID; }
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2();
    }
    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 up => new Vector3(0, 1, 0);
        public static Vector3 operator +(Vector3 a, Vector3 b) => new Vector3(a.x + b.x, a.y + b.y, a.z + b.z);
        public static Vector3 operator *(Vector3 a, float b) => new Vector3(a.x * b, a.y * b, a.z * b);
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public float xMax => x + width;
        public float yMax => y + height;
        public bool Contains(Vector2 p) => p.x >= x && p.x < xMax && p.y >= y && p.y < yMax;
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public struct Matrix4x4 { public static Matrix4x4 TRS(Vector3 a, Quaternion b, Vector3 c) => new Matrix4x4(); }
    public struct Quaternion { public float z; public static Quaternion identity => new Quaternion(); public static Quaternion Euler(float x,float y,float z) => new Quaternion { z=z }; }
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Clamp01(float value) => Math.Min(1, Math.Max(0, value));
    }
    public static class Time { public static float unscaledTime; }
    public static class Screen { public static int width = 1280, height = 900; }
    public enum KeyCode { F8, Escape, Return, KeypadEnter, Home, End, UpArrow, DownArrow, PageUp, PageDown }
    public static class Input { public static Vector3 mousePosition; public static KeyCode? pressed; public static bool GetKeyDown(KeyCode code) => pressed == code; }
    public enum EventType { Repaint, MouseDown, MouseUp, MouseDrag, ScrollWheel, KeyDown, Used }
    public class Event
    {
        public static Event current = new Event(); public EventType type; public int button; public KeyCode keyCode; public Vector2 delta;
        public Vector2 mousePosition = new Vector2(-9000, -9000);
        public void Use() { type = EventType.Used; }
    }
    public class Camera { public static Camera main; public Transform transform = new Transform(); public Vector3 WorldToScreenPoint(Vector3 p) => new Vector3(p.x * 100, p.y * 100, 1); }
    public class Font { }
    public enum FontStyle { Bold }
    public enum ScaleMode { ScaleToFit }
    public struct Color32 { public byte r, g, b, a; public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; } }
    public enum TextureFormat { RGBA32 }
    public enum FilterMode { Point }
    public enum TextureWrapMode { Clamp }
    public class Texture2D : Object
    {
        public int width = 1, height = 1; public Color32[] pixels = new[] { new Color32(1, 200, 5, 255) };
        public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Texture2D() { }
        public Texture2D(int w, int h, TextureFormat format, bool mipmap, bool linear) { width = w; height = h; pixels = new Color32[w * h]; }
        public Color32[] GetPixels32() => (Color32[])pixels.Clone();
        public void SetPixels32(Color32[] value) { pixels = (Color32[])value.Clone(); }
        public void Apply(bool mipmap, bool unreadable) { }
        public static Texture2D whiteTexture = new Texture2D();
    }
    public class RectOffset { public RectOffset(int a, int b, int c, int d) { } }
    public class GUIContent { public string text; public GUIContent(string text) { this.text = text; } }
    public class GUIStyleState { public Color textColor; }
    public class GUIStyle
    {
        public GUIStyle() { } public GUIStyle(GUIStyle s) { }
        public Font font; public int fontSize; public bool wordWrap, richText; public RectOffset padding; public FontStyle fontStyle;
        public GUIStyleState normal = new GUIStyleState();
        public float CalcHeight(GUIContent c, float width) => 40 + c.text.Length / 5;
    }
    public class GUISkin { public GUIStyle label = new GUIStyle(), textField = new GUIStyle(); }
    public static class GUIUtility { public static void RotateAroundPivot(float angle, Vector2 pivot) { } }
    public static class GUI
    {
        public static bool enabled = true; public static GUISkin skin = new GUISkin(); public static int depth; public static Color color; public static Matrix4x4 matrix;
        public static List<string> labels = new List<string>();
        public static List<Rect> textures = new List<Rect>();
        public static string nextText;
        public static void Label(Rect r, string text, GUIStyle style) { labels.Add(text); }
        public static string TextField(Rect r, string text, int n, GUIStyle style) { string value = nextText ?? text; nextText = null; return value; }
        public static Vector2 BeginScrollView(Rect r, Vector2 p, Rect inner) => p;
        public static void EndScrollView() { }
        public static List<Tuple<Rect, Texture2D, Rect>> uvTextures = new List<Tuple<Rect, Texture2D, Rect>>();
        public static List<bool> uvAlphaBlends = new List<bool>();
        public static void DrawTextureWithTexCoords(Rect r, Texture2D t, Rect uv, bool alphaBlend) { DrawTextureWithTexCoords(r, t, uv); uvAlphaBlends[uvAlphaBlends.Count - 1] = alphaBlend; }
        public static void DrawTextureWithTexCoords(Rect r, Texture2D t, Rect uv) { textures.Add(r); uvTextures.Add(Tuple.Create(r, t, uv)); uvAlphaBlends.Add(true); }
        public static void DrawTexture(Rect r, Texture2D t) { textures.Add(r); }
        public static void DrawTexture(Rect r, Texture2D t, ScaleMode s) { }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    using UnityEngine;
    internal static class ArtworkSkin
    {
        public static string click;
        public static bool respectEventUse;
        public static Dictionary<string, bool> enableds = new Dictionary<string, bool>();
        public static bool Button(Rect r, string text, float scale = 1, bool enabled = true, bool selected = false, float fontScale = 1)
        { enableds[text] = enabled; if (respectEventUse && Event.current.type == EventType.Used) return false; if (click == text && enabled) { click = null; return true; } return false; }
        public static bool Tab(Rect r, string text, float scale = 1, bool enabled = true, bool selected = false, float fontScale = 1) => Button(r, text, scale, enabled, selected, fontScale);
        public static void ButtonFace(Rect r, string text, float scale = 1, bool enabled = true, bool selected = false, float fontScale = 1, bool pressed = false) { }
        public static void Panel(Rect r, float scale = 1, bool highlighted = false, float opacity = 1) { }
        public static void Input(Rect r, float scale = 1, bool focused = false) { }
        public static void Row(Rect r, float scale = 1, bool highlighted = false) { }
        public static Dictionary<string, Texture2D[]> frameTextures = new Dictionary<string, Texture2D[]>();
        public static Texture2D[] Frames(string path, int count = 4) => frameTextures.TryGetValue(path, out var value) ? value : null;
    }
    internal static class SysMsgBox
    {
        public static bool Visible; public static string text; public static Action confirm, cancel;
        public static void Show(string value, Action yes, Action no) { text = value; confirm = yes; cancel = no; Visible = true; }
        public static void Finish(bool yes) { var action = yes ? confirm : cancel; Visible = false; confirm = cancel = null; action?.Invoke(); }
    }
    internal static class CraftUI
    {
        public static bool Click(Rect rect, Vector2 mouse)
        {
            if (SysMsgBox.Visible || Event.current.type != EventType.MouseDown || Event.current.button != 0 || !rect.Contains(mouse)) return false;
            Event.current.Use(); return true;
        }
    }
    internal static class NativeScroll
    {
        public static int calls;
        public static void BarRows(object owner, Rect track, int visible, int total, ref int scroll) { calls++; }
    }
    internal static class NpcChatter { public static Font CnFont() => new Font(); }
    internal static class NewbieTipBox
    {
        public static readonly List<string> notices = new List<string>();
        public static void PushText(string text) { notices.Add(text); }
    }
    internal static class BitmapFont { public static bool Ready => true; public static string Elide(string text, float width, float scale) => text; public static List<string> labels = new List<string>(); public static void CenterOutline(Rect r, string text, Color c, float scale) { labels.Add(text); } }
    internal static class UIScale { public static float Factor = 1; }
    internal static class WinZ
    {
        public static int forgotten; public static Rect lastFrame;
        public static (int, bool) Frame(object owner, Rect r, int depth) { lastFrame = r; return (depth, false); }
        public static void Forget(object owner) { forgotten++; }
    }
    internal static class WinDrag
    {
        public static int calls, forgotten;
        public static void Forget(object owner) { forgotten++; }
        public static bool Stale(float x, ref int generation) { int next = Screen.width * 65536 + Screen.height; bool stale = float.IsNaN(x) || generation != next; generation = next; return stale; }
        public static void Handle(object owner, Rect rect, ref float x, ref float y) { calls++; }
    }
    internal static class UITip { public static string last; public static Vector2 mouse; public static float scale; public static void Show(string text, float s) { last = text; scale = s; } public static void ShowAtForIndependentRuntime(string text, float s, Vector2 pointer) { Show(text, s); mouse = pointer; } }
    internal static partial class TooltipContent { public static string QualityName(string name, int quality) => "\u0003" + quality + name; public static string Fmt(string name, string desc) => "\u0007" + name + "\n" + desc; }
    public partial class GameHud { public static GameHud I; public float uiScale; public bool showAvatar = true; public static string HotSlotTip(int index) => "hotbar:" + index; }
    internal static class PlayerController { public static void SuppressWorldClick() { } }
    internal static class MobileClientPresentation { public static bool Enabled; }
    internal static class InventoryDlg
    {
        public struct Item { public int id, bagIdx, quality, count; public string name, desc; }
        public static IEnumerable<Item>[] Items => new IEnumerable<Item>[] { rows.Values, rows.Values, rows.Values, rows.Values };
        public static Dictionary<int, Item> rows = new Dictionary<int, Item>();
        // Existing tooltip fixtures use tab-agnostic rows; entry fixtures opt into an exact native tab.
        public static Dictionary<int, int> rowTabs = new Dictionary<int, int>();
        public static bool IsOpen, HasHeld; public static bool HasHeldAny => HasHeld; public static int HeldTab, HeldBagSlot;
        public static bool IsLocked(byte tab, int slot) => locked.Contains(slot);
        public static HashSet<int> locked = new HashSet<int>();
        public static void Toggle() { IsOpen = !IsOpen; }
        public static void ClearHeld() { HasHeld = false; }
        public static Item ItemAt(int tab, int slot) => tab >= 0 && tab < 4 && slot >= 0 &&
            (!rowTabs.TryGetValue(slot, out var actualTab) || actualTab == tab) && rows.TryGetValue(slot, out var value) ? value : default;
    }
    internal static class EquipDlg
    {
        public struct EquippedItem { public int id, slot, quality; public string name, desc; }
        public static List<EquippedItem> Equipped = new List<EquippedItem>();
    }
    internal static class NetClient { public static Texture2D LoadItemIcon(int id) => null; }
    public partial class GameBootstrap { public static void FlushMoveReport(bool force = false) { } public static GameBootstrap I; public static int CurMapId; public static float CurPpu = 100, CurWorldW = 2000, CurWorldH = 1000; public bool Switching; public Transform WorldRootForMapConstructionForIndependentRuntime; public static readonly Material PortalMaterial = new Material(); public static readonly Sprite PortalSprite = new Sprite(); public static Material IsoYObjMat() => PortalMaterial; public Sprite GetObjectSpriteForIndependentRuntime(int id, ResLoader.ObjSprite image, float ppu) => PortalSprite; }
    public class ResLoader { public static ResLoader I; public byte[] ReadRes(string path) => DimgDecoder.Bytes; public int Loads, AnimLoads, LastID, LastWidth, LastHeight; public float LastPpu; public ObjSprite Image; public ObjAnim Animation; public struct ObjSprite { public Texture2D tex; public int nLeft, nTop, clipBy, frames; } public struct ObjAnim { public Sprite[] frames; public int[] nLeft, nTop; } public ObjSprite LoadObj(int id, int w, int h) { Loads++; LastID = id; LastWidth = w; LastHeight = h; return Image; } public ObjAnim LoadObjAnim(int id, float ppu) { AnimLoads++; LastID = id; LastPpu = ppu; return Animation; } }
    public static class DimgDecoder
    {
        public static byte[] Bytes;
        public struct FrameTex { public Texture2D tex; public int nLeft,nTop; }
        public struct Anim { public bool ok; public FrameTex[] f; public int boundCx,boundCy,scale; }
        public static Func<Anim> Factory;
        public static Anim DecodeAnim(byte[] bytes) => Factory == null ? default(Anim) : Factory();
    }
    public class FrameAnimator { public float frameMs; public bool loop; public Func<(Sprite[], Vector3[])> loader; }
    public partial class CombatWorld { private static CombatWorld I; private Dictionary<int, MonsterEntity> monsters = new Dictionary<int, MonsterEntity>(); }
    public class MonsterEntity { public Transform transform = new Transform(); public int RiftKind, Clears; public string RiftName; public void UpdateRiftIdentity(int kind, string name) { RiftKind = kind; RiftName = name; } public void ClearRiftIdentity() { RiftKind = 0; Clears++; }
        internal void UpdateRiftEliteState(RiftEliteVisualAssets assets, bool golden, bool agility, bool clone, double now) { } internal void ClearRiftEliteState() { } }
    public class EffectPlayer : MonoBehaviour { public static readonly List<EffectPlayer> Played = new List<EffectPlayer>(); public static readonly Material AlphaMaterial = new Material(), AdditiveMaterial = new Material(); public static Material BorrowAlphaMaterial() => AlphaMaterial; public static Material BorrowAdditiveMaterial() => AdditiveMaterial; public static float NextScale = 1f; public static Sprite[] MagicFrames; public static Sprite[] GetMagicFramesAll(int id) => MagicFrames; public static Sprite[] GetMagicFrames(int id) => MagicFrames; public static EffectPlayer PlayNamed(Vector3 position, string effect, float ppu) { var result = new EffectPlayer { gameObject = new GameObject(effect) }; result.transform.position = position; Played.Add(result); return result; } }
}
