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
        public Color texel = Color.white;
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
            GUI.calls.Clear(); GUI.color = new Color(.2f,.3f,.4f,.5f);
            Win05.TestIcon(area, texture, learned, tree);
            Check(GUI.calls.Count == (texture == null ? 1 : 2), "clean inner well then current icon, regardless of learned state");
            Check(GUI.color.r == 1 && GUI.color.g == 1 && GUI.color.b == 1 && GUI.color.a == 1, "GUI color reset on every branch");
            var backing = GUI.calls[0];
            float inset = tree ? 2 : 0;
            Check(ReferenceEquals(backing.texture,Texture2D.whiteTexture) && backing.tint.a == 1, "opaque inner backing prevents missing/transparent icon leak");
            Check(Near(backing.area.x,area.x+inset) && Near(backing.area.y,area.y+inset) && Near(backing.area.width,36-2*inset) && Near(backing.area.height,36-2*inset), "only inner icon well is covered; original rim survives");
            if (texture == null) continue;
            var call = GUI.calls[1];
            Check(ReferenceEquals(call.texture, texture), "current contract icon used, never baked artwork");
            Check(Near(call.area.x,backing.area.x) && Near(call.area.y,backing.area.y) && Near(call.area.width,backing.area.width) && Near(call.area.height,backing.area.height), "same inner geometry when learned and unlearned");
            Check(call.tint.a == 1 && Near(call.tint.r,learned ? 1 : .55f) && Near(call.tint.g,call.tint.r) && Near(call.tint.b,call.tint.r), "unlearned dims RGB only, never alpha");
        }
        foreach (var node in new[] { new[] { 10316,39,181 }, new[] { 10318,39,267 }, new[] { 10321,155,10 } })
        foreach (float scale in new[] { .5f,1f,1.5f,2f })
        foreach (bool learned in new[] { false,true })
        {
            GUI.calls.Clear(); GUI.color = Color.white;
            var rect = new Rect(node[1]*scale,(node[2]-23)*scale,36*scale,36*scale);
            Win05.TestIcon(rect,icon,learned,true);
            Check(GUI.calls.Count == 2 && ReferenceEquals(GUI.calls[1].texture,icon), "restored apothecary node " + node[0] + " retains current identity at both ranks");
            Check(Near(GUI.calls[0].area.x,rect.x+2*scale) && Near(GUI.calls[0].area.width,32*scale), "inner rim scales with original viewport");
        }
        foreach (bool learned in new[] { false, true, false, true, false })
        {
            GUI.calls.Clear(); GUI.color = Color.white;
            Win05.TestIcon(area, icon, learned, true);
            Check(GUI.calls.Count == 2 && ReferenceEquals(GUI.calls[1].texture,icon), "learn/unlearn/reopen never switches to a different baked icon");
        }
    }
    private static Color Composite(float x, float y, Color background)
    {
        foreach (var draw in GUI.calls)
        {
            var r=draw.area;
            if (x<r.x || y<r.y || x>=r.x+r.width || y>=r.y+r.height) continue;
            var t=draw.texture.texel;
            float alpha=t.a*draw.tint.a;
            background=new Color(t.r*draw.tint.r*alpha+background.r*(1-alpha),
                t.g*draw.tint.g*alpha+background.g*(1-alpha),
                t.b*draw.tint.b*alpha+background.b*(1-alpha),1);
        }
        return background;
    }
    private static bool Same(Color a, Color b) => Near(a.r,b.r) && Near(a.g,b.g) && Near(a.b,b.b) && Near(a.a,b.a);
    private static void PixelIsolation()
    {
        var oldRed=new Color(1,0,0,1); var oldBlue=new Color(0,0,1,1);
        foreach (float scale in new[] { .5f,1f,2f })
        foreach (bool learned in new[] { false,true })
        foreach (float alpha in new[] { 0f,.5f,1f })
        foreach (var size in new[] { new[] { 30,30 },new[] { 60,30 },new[] { 30,60 } })
        {
            var area=new Rect(37*scale,(11-23)*scale,36*scale,36*scale);
            var icon=new Texture2D(size[0],size[1]) { texel=new Color(.8f,.6f,.4f,alpha) };
            GUI.calls.Clear(); Win05.TestIcon(area,icon,learned,true);
            float cx=area.x+18*scale,cy=area.y+18*scale;
            Check(Same(Composite(cx,cy,oldRed),Composite(cx,cy,oldBlue)), "current image/transparent pixel cannot expose old baked identity");
            Check(Same(Composite(area.x+3*scale,area.y+3*scale,oldRed),Composite(area.x+3*scale,area.y+3*scale,oldBlue)), "non-square fit margin cannot expose baked identity");
            Check(Same(Composite(area.x+scale,cy,oldRed),oldRed) && Same(Composite(area.x+35*scale,cy,oldBlue),oldBlue), "left/right original rim pixels unchanged");
            Check(Same(Composite(cx,area.y+scale,oldRed),oldRed) && Same(Composite(cx,area.y+35*scale,oldBlue),oldBlue), "top/bottom original rim pixels unchanged");
            Check(Same(Composite(cx,area.y+40*scale,oldRed),oldRed), "connecting arrow below node unchanged");
        }
    }
    public static void Main()
    {
        Geometry();
        Rendering();
        PixelIsolation();
        Console.WriteLine("PASS SkillIconContract: " + assertions + " production-method geometry/render assertions (not Unity visual acceptance)");
    }
}
