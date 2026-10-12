// Test-only deterministic substitutes. Production GrowthSkills, SkillTipText and
// UITip are compiled unchanged; no layout, composition or render method is replaced.
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
    }
    public struct Vector3 { public float x, y, z; }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
        public float xMax => x + width;
        public float yMax => y + height;
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new Color(1f, 1f, 1f, 1f);
    }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a)
        { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static implicit operator Color(Color32 color) =>
            new Color(color.r / 255f, color.g / 255f, color.b / 255f, color.a / 255f);
    }
    public static class Mathf
    {
        public static int Max(int x, int y) => Math.Max(x, y);
        public static float Max(float x, float y) => Math.Max(x, y);
        public static int Min(int x, int y) => Math.Min(x, y);
        public static float Min(float x, float y) => Math.Min(x, y);
        public static float Clamp(float value, float minimum, float maximum) => Math.Min(maximum, Math.Max(minimum, value));
        public static float Round(float value) => (float)Math.Round(value);
    }
    public static class Input { public static Vector3 mousePosition; }
    public static class Screen { public static int width = 1280, height = 900; public static float dpi = 96f; }
    public static class PlayerPrefs
    {
        public static float GetFloat(string key, float fallback) => fallback;
        public static void SetFloat(string key, float value) { }
        public static void Save() { }
    }
    public enum EventType { Layout, Repaint, ScrollWheel, Used }
    public class Event { public static Event current; public Vector2 mousePosition, delta; public EventType type; public void Use() { type = EventType.Used; } }
    public class MonoBehaviour { }
    public class GameObject
    {
        public GameObject(string name) { }
        public T AddComponent<T>() => (T)Activator.CreateInstance(typeof(T), true);
    }
    public sealed class Texture2D
    {
        public readonly string Name;
        public Texture2D(string name) { Name = name; }
        public static readonly Texture2D whiteTexture = new Texture2D("white");
    }
    public static class GUI
    {
        public static Color color = Color.white;
        public static int depth;
        public sealed class TextureDraw
        {
            public Rect Area;
            public Texture2D Texture;
            public Color Color;
            public bool HasCoordinates;
        }
        public static readonly List<TextureDraw> Draws = new List<TextureDraw>();
        public static void DrawTexture(Rect area, Texture2D texture) =>
            Draws.Add(new TextureDraw { Area = area, Texture = texture, Color = color });
        public static void DrawTextureWithTexCoords(Rect area, Texture2D texture, Rect coordinates) =>
            Draws.Add(new TextureDraw { Area = area, Texture = texture, Color = color, HasCoordinates = true });
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    using UnityEngine;
    public static class EndgamePanel { public static EndgameClientState GrowthState; }
    public static class CharData { public static int skillPoints; }
    public static class GameSettings { public static bool ColorWeak; }
    public sealed class ResLoader
    {
        public static ResLoader I = new ResLoader();
        public static readonly List<string> Paths = new List<string>();
        public Texture2D LoadUI(string path) { Paths.Add(path); return new Texture2D(path); }
    }
    public static class BitmapFont
    {
        public static bool Ready = true;
        public sealed class TextDraw
        {
            public float X, Y, Scale;
            public string Text;
            public Color Color;
        }
        public static readonly List<TextDraw> Draws = new List<TextDraw>();
        // Six pixels per ASCII cell and twelve per non-ASCII character. This makes
        // ComposeTip's space padding and UITip's 150-pixel wrapping deterministic.
        public static float Measure(string text, float scale)
        {
            int cells = 0;
            foreach (char value in text ?? string.Empty) cells += value > 127 ? 2 : 1;
            return cells * 6f * scale;
        }
        public static void Draw(float x, float y, string text, Color color, float scale) =>
            Draws.Add(new TextDraw { X = x, Y = y, Text = text, Color = color, Scale = scale });
    }
    public partial class Win05
    {
        public class SkInfo { public int id; public string name; public bool usable = true; }
        private static bool SkillUsableForHotbar(SkInfo info) => info != null && info.usable;
        public static string TestNativeGrowthTip(int id = 10401, bool includeNext = true, string name = "火弹术", bool usable = true) =>
            BuildGrowthSkillTip(new SkInfo { id = id, name = name, usable = usable }, includeNext);
        public static bool TestUsesGrowthSkill(int id) => UsesGrowthSkill(id);
        public static int TestDisplayedSkillPoints => DisplayedSkillPoints;
    }
}
