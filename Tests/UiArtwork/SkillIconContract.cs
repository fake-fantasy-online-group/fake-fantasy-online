using System;
using System.Collections.Generic;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace UnityEngine
{
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float width, float height)
        { this.x=x; this.y=y; this.width=width; this.height=height; }
    }
    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a)
        { this.r=r; this.g=g; this.b=b; this.a=a; }
        public static Color white => new Color(1, 1, 1, 1);
    }
    public class Texture2D
    {
        public int width, height;
        public static readonly Texture2D whiteTexture = new Texture2D(1, 1);
        public Texture2D(int width, int height) { this.width=width; this.height=height; }
    }
    public static class Mathf { public static float Min(float a, float b) => Math.Min(a,b); }
    public static class GUI
    {
        public struct Draw { public Rect area; public Texture2D texture; public Color tint; }
        public static Color color = Color.white;
        public static readonly List<Draw> calls = new List<Draw>();
        public static void DrawTexture(Rect area, Texture2D texture)
        { calls.Add(new Draw { area=area, texture=texture, tint=color }); }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class Win05
    {
        public static void TestIcon(Rect area, Texture2D texture, bool learned, bool hasTreeArtwork) => DrawSkillIcon(area, texture, learned, hasTreeArtwork);
        public static Rect TestFit(Rect area, Texture2D texture) => Fit(area, texture);
    }
}
static class SkillIconContract
{
    private static int assertions;
    private static void Check(bool value, string label)
    { if (!value) throw new Exception(label); assertions++; }
    private static bool Near(float a, float b) => Math.Abs(a-b) < .0001f;
    private static void Geometry()
    {
        foreach (float scale in new[] { .5f, 1f, 1.5f, 2f })
        foreach (float scroll in new[] { 0f, 23f, 101f })
        {
            var area = new Rect(17*scale, (54-scroll)*scale, 36*scale, 36*scale);
            var square = Win05.TestFit(area, new Texture2D(72,72));
            Check(Near(square.x,area.x) && Near(square.y,area.y) && Near(square.width,area.width) && Near(square.height,area.height), "square fit preserves node/scroll/scale");
            var wide = Win05.TestFit(area, new Texture2D(72,36));
            Check(Near(wide.x,area.x) && Near(wide.y,area.y+9*scale) && Near(wide.width,36*scale) && Near(wide.height,18*scale), "wide fit preserves aspect and centered margins");
            var tall = Win05.TestFit(area, new Texture2D(36,72));
            Check(Near(tall.x,area.x+9*scale) && Near(tall.y,area.y) && Near(tall.width,18*scale) && Near(tall.height,36*scale), "tall fit preserves aspect and centered margins");
        }
    }
    private static void Rendering()
    {
        var area = new Rect(37, 11, 36, 36);
        var icon = new Texture2D(30,30);
        foreach (bool tree in new[] { false, true })
        foreach (bool learned in new[] { false, true })
        foreach (Texture2D texture in new[] { icon, null })
        {
            GUI.calls.Clear();
            GUI.color = new Color(.2f,.3f,.4f,.5f);
            Win05.TestIcon(area, texture, learned, tree);
            bool bakedOnly = tree && !learned;
            Check(GUI.calls.Count == (bakedOnly ? 0 : 1), "single authored inactive image; learned/fallback draw preserved");
            Check(GUI.color.r == 1 && GUI.color.g == 1 && GUI.color.b == 1 && GUI.color.a == 1, "GUI color reset on every branch");
            if (bakedOnly) continue;
            var call = GUI.calls[0];
            Check(ReferenceEquals(call.texture, texture ?? Texture2D.whiteTexture), "original live/fallback texture used");
            Check(Near(call.area.x,area.x) && Near(call.area.y,area.y) && Near(call.area.width,36) && Near(call.area.height,36), "no node geometry or frame changes");
            if (texture == null)
                Check(call.tint.r == 0 && call.tint.g == 0 && call.tint.b == 0 && Near(call.tint.a,.25f), "missing icon fallback unchanged");
            else if (!learned)
                Check(call.tint.r == 1 && call.tint.g == 1 && call.tint.b == 1 && Near(call.tint.a,.38f), "missing tree retains dimmed live fallback");
        }
        // Recovered apothecary nodes already exist in maintree4's authored art.
        // Server metadata supplies the learned image without a new inactive layer.
        foreach (var node in new[] { new[] { 10316,39,181 }, new[] { 10318,39,267 }, new[] { 10321,155,10 } })
        foreach (bool tree in new[] { false,true })
        foreach (bool learned in new[] { false,true })
        {
            GUI.calls.Clear(); GUI.color = Color.white;
            var rect = new Rect(node[1],node[2]-23,36,36);
            Win05.TestIcon(rect,icon,learned,tree);
            Check(GUI.calls.Count == (tree && !learned ? 0 : 1), "restored apothecary node " + node[0] + " retains exactly one inactive/live image");
        }
        // A transition must not carry a stale live draw across future GUI passes.
        foreach (bool learned in new[] { false, true, false, true, false })
        {
            GUI.calls.Clear(); GUI.color = Color.white;
            Win05.TestIcon(area, icon, learned, true);
            Check(GUI.calls.Count == (learned ? 1 : 0), "learn/unlearn/reopen repaint has no stale overlay");
            if (learned) Check(GUI.calls[0].tint.a == 1 && GUI.calls[0].tint.r == 1, "learned image stays full color");
        }
    }
    public static void Main()
    {
        Geometry();
        Rendering();
        Console.WriteLine("PASS SkillIconContract: " + assertions + " production-method geometry/render assertions (not Unity visual acceptance)");
    }
}
