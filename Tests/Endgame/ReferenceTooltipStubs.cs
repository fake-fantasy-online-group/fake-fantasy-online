// Minimal deterministic substitutes used only to execute the diagnostic tooltip source.
// Native UITip palette/parser parity is checked separately by tooltip_source_contract.py.
namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new Vector2();
    }
    public struct Vector3 { public float x, y, z; }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x = x; this.y = y; this.width = width; this.height = height; }
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
    public static class Mathf { public static float Max(float x, float y) => System.Math.Max(x, y); }
    public static class Input { public static Vector3 mousePosition; }
    public static class Screen { public static int width = 1280, height = 900; }
    public class Event { public static Event current; public Vector2 mousePosition; }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    using UnityEngine;
    public sealed class ReferenceResourceLoader { }
    internal sealed class ReferenceWorldBitmapFont
    {
        public static bool TryCreate(ReferenceResourceLoader resources, out ReferenceWorldBitmapFont font)
        { font = new ReferenceWorldBitmapFont(); return true; }
        public float Measure(string text, float scale) => (text ?? string.Empty).Length * 6f * scale;
    }
    internal static class UITip
    {
        internal static string RequestedTextForIndependentRuntime;
        internal static string RequestedComparisonForIndependentRuntime;
        internal static float RequestedScaleForIndependentRuntime;
        internal static Vector2 RequestedMouseForIndependentRuntime;
        internal static void ShowAtForIndependentRuntime(string text, float scale, Vector2 mouse)
        {
            RequestedTextForIndependentRuntime = text;
            RequestedComparisonForIndependentRuntime = null;
            RequestedScaleForIndependentRuntime = scale;
            RequestedMouseForIndependentRuntime = mouse;
        }
        internal static void ShowCompareAtForIndependentRuntime(string text, string comparison, float scale, Vector2 mouse)
        {
            ShowAtForIndependentRuntime(text, scale, mouse);
            RequestedComparisonForIndependentRuntime = comparison;
        }
    }
}
