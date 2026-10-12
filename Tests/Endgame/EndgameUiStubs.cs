// Deliberately small deterministic API substitutes. These tests never invoke a Unity engine.
using System;
using System.Collections.Generic;
namespace UnityEngine
{
    public class Object { }
    public class MonoBehaviour : Object { public Transform transform = new Transform(); }
    public class Transform { public Vector3 position; }
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
    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public static class Mathf
    {
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static float Clamp01(float value) => Math.Min(1, Math.Max(0, value));
    }
    public static class Time { public static float unscaledTime; }
    public static class Screen { public static int width = 1280, height = 900; }
    public enum KeyCode { F8, Escape }
    public static class Input { public static KeyCode? pressed; public static bool GetKeyDown(KeyCode code) => pressed == code; }
    public enum EventType { Repaint, MouseDown, Used }
    public class Event
    {
        public static Event current = new Event(); public EventType type; public int button;
        public Vector2 mousePosition = new Vector2(-9000, -9000);
        public void Use() { type = EventType.Used; }
    }
    public class Camera { public static Camera main; public Vector3 WorldToScreenPoint(Vector3 p) => new Vector3(p.x * 100, p.y * 100, 1); }
    public class Font { }
    public enum FontStyle { Bold }
    public enum ScaleMode { ScaleToFit }
    public class Texture2D { public static Texture2D whiteTexture = new Texture2D(); }
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
        public static GUISkin skin = new GUISkin(); public static int depth; public static Color color; public static Matrix4x4 matrix;
        public static List<string> labels = new List<string>();
        public static List<Rect> textures = new List<Rect>();
        public static string nextText;
        public static void Label(Rect r, string text, GUIStyle style) { labels.Add(text); }
        public static string TextField(Rect r, string text, int n, GUIStyle style) { string value = nextText ?? text; nextText = null; return value; }
        public static Vector2 BeginScrollView(Rect r, Vector2 p, Rect inner) => p;
        public static void EndScrollView() { }
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
        public static Dictionary<string, bool> enableds = new Dictionary<string, bool>();
        public static bool Button(Rect r, string text, float scale = 1, bool enabled = true, bool selected = false, float fontScale = 1)
        { enableds[text] = enabled; if (click == text && enabled) { click = null; return true; } return false; }
        public static bool Tab(Rect r, string text, float scale = 1, bool enabled = true, bool selected = false, float fontScale = 1) => Button(r, text, scale, enabled, selected, fontScale);
        public static void ButtonFace(Rect r, string text, float scale = 1, bool enabled = true, bool selected = false, float fontScale = 1, bool pressed = false) { }
        public static void Panel(Rect r, float scale = 1, bool highlighted = false, float opacity = 1) { }
        public static void Input(Rect r, float scale = 1, bool focused = false) { }
        public static void Row(Rect r, float scale = 1, bool highlighted = false) { }
        public static Texture2D[] Frames(string path, int count = 4) => null;
    }
    internal static class NpcChatter { public static Font CnFont() => new Font(); }
    internal static class BitmapFont { public static List<string> labels = new List<string>(); public static void CenterOutline(Rect r, string text, Color c, float scale) { labels.Add(text); } }
    internal static class UIScale { public static float Factor = 1; }
    internal static class WinZ
    {
        public static int forgotten;
        public static (int, bool) Frame(object owner, Rect r, int depth) => (depth, false);
        public static void Forget(object owner) { forgotten++; }
    }
    internal static class UITip { public static string last; public static Vector2 mouse; public static float scale; public static void Show(string text, float s) { last = text; scale = s; } public static void ShowAtForIndependentRuntime(string text, float s, Vector2 pointer) { Show(text, s); mouse = pointer; } }
    internal static partial class TooltipContent { public static string QualityName(string name, int quality) => "\u0003" + quality + name; public static string Fmt(string name, string desc) => "\u0007" + name + "\n" + desc; }
    internal static class GameHud { public static string HotSlotTip(int index) => "hotbar:" + index; }
    internal static class PlayerController { public static void SuppressWorldClick() { } }
    internal static class InventoryDlg
    {
        public struct Item { public int id, bagIdx, quality; public string name, desc; }
        public static IEnumerable<Item>[] Items => new IEnumerable<Item>[] { rows.Values, rows.Values, rows.Values, rows.Values };
        public static Dictionary<int, Item> rows = new Dictionary<int, Item>();
        public static Item ItemAt(int tab, int slot) => rows.TryGetValue(slot, out var value) ? value : default;
    }
    internal static class EquipDlg
    {
        public struct EquippedItem { public int id, slot, quality; public string name, desc; }
        public static List<EquippedItem> Equipped = new List<EquippedItem>();
    }
    internal static class NetClient { public static Texture2D LoadItemIcon(int id) => null; }
    public class GameBootstrap { public static float CurPpu = 100, CurWorldH = 1000; }
    public partial class CombatWorld { private static CombatWorld I; private Dictionary<int, MonsterEntity> monsters = new Dictionary<int, MonsterEntity>(); }
    public class MonsterEntity { public Transform transform = new Transform(); }
}
