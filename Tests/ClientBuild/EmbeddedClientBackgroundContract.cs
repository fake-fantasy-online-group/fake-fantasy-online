using System;
using System.Reflection;
using FakeFantasyOnline.Presentation;
using UnityEngine;

public static class EmbeddedClientBackgroundContract
{
    private static int assertions;
    private static void Check(bool value, string message)
    {
        assertions++;
        if (!value) throw new Exception(message);
    }
    private static void Reset()
    {
        typeof(EmbeddedClientBackground).GetMethod("ResetForPlaySession", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, null);
        Resources.Loads = Debug.Warnings = GUI.Draws = 0;
        GUI.ThrowOnDraw = false;
    }
    public static void Main()
    {
        Reset();
        EmbeddedClientBackground.Draw(new Rect(0, 0, 0, 600));
        EmbeddedClientBackground.Draw(new Rect(0, 0, 800, -1));
        Check(Resources.Loads == 0 && GUI.Draws == 0, "Empty viewports must not load or draw");
        Resources.Image = new Texture2D();
        EmbeddedClientBackground.Preload();
        EmbeddedClientBackground.Preload();
        Check(Resources.Loads == 1, "Preload must load the bundled texture only once");
        Check(Resources.LastPath == EmbeddedClientBackground.ResourcePath, "Load must use the package-owned resource key");
        foreach (var viewport in new[] { new Rect(0, 0, 800, 600), new Rect(0, 0, 1920, 1080), new Rect(7, 9, 2340, 1080), new Rect(0, 0, 600, 900) })
        {
            var original = new Color(.2f, .3f, .4f, .5f);
            GUI.color = original;
            EmbeddedClientBackground.Draw(viewport);
            Check(ReferenceEquals(GUI.Texture, Resources.Image), "Draw uses the shared cached texture");
            Check(GUI.Mode == ScaleMode.ScaleAndCrop && !GUI.AlphaBlend, "Every aspect ratio uses opaque aspect cover");
            Check(GUI.Viewport.Equals(viewport), "Draw retains the requested viewport and lets Unity crop rather than stretch");
            Check(GUI.ColorAtDraw.Equals(Color.white), "Caller tint must not darken bundled artwork");
            Check(GUI.color.Equals(original), "Draw restores caller tint");
        }
        Check(Resources.Loads == 1 && Debug.Warnings == 0, "Repeated layout/repaint must not reload or warn");
        GUI.ThrowOnDraw = true;
        var beforeFailure = GUI.color;
        try { EmbeddedClientBackground.Draw(new Rect(0, 0, 800, 600)); }
        catch (InvalidOperationException) { }
        Check(GUI.color.Equals(beforeFailure), "GUI color is restored even if drawing fails");
        Reset();
        Resources.Image = null;
        EmbeddedClientBackground.Preload();
        for (int frame = 0; frame < 8; frame++) EmbeddedClientBackground.Draw(new Rect(0, 0, 800, 600));
        Check(Resources.Loads == 1 && Debug.Warnings == 1, "Missing assets are not retried or logged every frame");
        Check(ReferenceEquals(GUI.Texture, Texture2D.whiteTexture), "Missing artwork falls back without downloaded resources");
        Check(GUI.ColorAtDraw.a == 1 && !GUI.AlphaBlend, "Fallback covers an undefined startup framebuffer opaquely");
        Check(GUI.ColorAtDraw.r < .1f && GUI.ColorAtDraw.b < .2f, "Fallback preserves readable startup contrast");
        Reset();
        Resources.Image = new Texture2D();
        EmbeddedClientBackground.Draw(new Rect(0, 0, 800, 600));
        Check(Resources.Loads == 1 && ReferenceEquals(GUI.Texture, Resources.Image), "A new Play session clears stale missing-asset state");
        Console.WriteLine("PASS bundled background contract: " + assertions + " assertions (Unity substitutes; no Player rendering)");
    }
}

namespace UnityEngine
{
    public enum RuntimeInitializeLoadType { SubsystemRegistration }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class RuntimeInitializeOnLoadMethodAttribute : Attribute
    { public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { } }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height) { this.x = x; this.y = y; this.width = width; this.height = height; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white { get { return new Color(1, 1, 1, 1); } }
    }
    public sealed class Texture2D { public static readonly Texture2D whiteTexture = new Texture2D(); }
    public static class Resources
    {
        public static Texture2D Image;
        public static int Loads;
        public static string LastPath;
        public static T Load<T>(string path) where T : class { Loads++; LastPath = path; return Image as T; }
    }
    public static class Debug { public static int Warnings; public static void LogWarning(string message) { Warnings++; } }
    public enum ScaleMode { StretchToFill, ScaleAndCrop }
    public static class GUI
    {
        public static Color color = Color.white, ColorAtDraw;
        public static Rect Viewport;
        public static Texture2D Texture;
        public static ScaleMode Mode;
        public static bool AlphaBlend, ThrowOnDraw;
        public static int Draws;
        public static void DrawTexture(Rect rect, Texture2D texture, ScaleMode mode, bool alphaBlend)
        {
            if (ThrowOnDraw) throw new InvalidOperationException("Synthetic drawing failure");
            Draws++; Viewport = rect; Texture = texture; Mode = mode; AlphaBlend = alphaBlend; ColorAtDraw = color;
        }
    }
}
