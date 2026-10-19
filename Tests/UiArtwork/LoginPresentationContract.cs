using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace UnityEngine
{
    public enum FilterMode { Point }
    public enum TextureWrapMode { Clamp }
    public enum TextureFormat { RGBA32 }
    public enum FontStyle { Normal }
    public enum TextAnchor { MiddleLeft }
    public enum ImagePosition { ImageOnly }
    public enum FocusType { Keyboard }
    public enum EventType { MouseDown, MouseUp, KeyDown, Repaint, Used }
    public enum KeyCode { None, Space }
    public struct Color { public Color(float r, float g, float b, float a) { } }
    public struct Color32
    {
        public byte r, g, b, a;
        public Color32(byte r, byte g, byte b, byte a) { this.r = r; this.g = g; this.b = b; this.a = a; }
    }
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x=x; this.y=y; width=w; height=h; }
        public bool Contains(Vector2 p) => p.x >= x && p.x < x+width && p.y >= y && p.y < y+height;
    }
    public class RectOffset { }
    public class Texture2D
    {
        public readonly int width, height;
        public bool isReadable=true, destroyed;
        public string name;
        public FilterMode filterMode;
        public TextureWrapMode wrapMode;
        private Color32[] pixels;
        public Texture2D(int width, int height, TextureFormat format, bool mip) { this.width=width; this.height=height; pixels=new Color32[width*height]; }
        public static readonly Texture2D whiteTexture = new Texture2D(1,1,TextureFormat.RGBA32,false);
        public Color32[] GetPixels32() => (Color32[])pixels.Clone();
        public void SetPixels32(Color32[] value) { pixels=(Color32[])value.Clone(); }
        public void Apply(bool mip, bool unreadable) { isReadable=!unreadable; }
        public static implicit operator bool(Texture2D t) => t != null && !t.destroyed;
    }
    public class GUIStyleState { public Texture2D background; public Color textColor; }
    public class GUIStyle
    {
        public GUIStyle() { }
        public GUIStyle(GUIStyle copy) { }
        public int fontSize; public FontStyle fontStyle; public TextAnchor alignment;
        public RectOffset border, margin, padding, overflow;
        public float fixedWidth, fixedHeight; public ImagePosition imagePosition;
        public GUIStyleState normal=new GUIStyleState(), hover=new GUIStyleState(), active=new GUIStyleState(), focused=new GUIStyleState();
    }
    public class GUIContent { public static readonly GUIContent none=new GUIContent(); }
    public class GUISkin { public GUIStyle label=new GUIStyle(); }
    public static class GUI
    {
        public static bool enabled=true; public static GUISkin skin=new GUISkin();
        public static GUIStyle lastStyle; public static object lastContent;
        public static void DrawTexture(Rect rect, Texture2D t) { }
        public static void Box(Rect rect, GUIContent c, GUIStyle style) { }
        public static void Label(Rect rect, string s, GUIStyle style) { }
        public static bool Button(Rect rect, GUIContent c, GUIStyle style) { lastStyle=style; lastContent=c; return false; }
        public static bool Button(Rect rect, string c, GUIStyle style) { lastStyle=style; lastContent=c; return false; }
    }
    public static class GUIUtility { public static int keyboardControl; public static int GetControlID(FocusType f, Rect r) => (int)r.x; }
    public class Event
    {
        public static Event current;
        public EventType type; public int button; public KeyCode keyCode; public Vector2 mousePosition;
        public void Use() { type=EventType.Used; }
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal class ResLoader
    {
        public static ResLoader I;
        public object Pkg;
        public int loads;
        public Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public Texture2D LoadGsaFrameA(string path,int frame) { loads++; textures.TryGetValue(path+frame,out Texture2D value); return value; }
    }
    internal static class GsaTextureScale
    {
        private static readonly Dictionary<Texture2D,int> scales=new Dictionary<Texture2D,int>();
        public static int Of(Texture2D t) => scales.TryGetValue(t,out int scale) ? scale : 1;
        public static float Width(Texture2D t) => (float)t.width / Of(t);
        public static float Height(Texture2D t) => (float)t.height / Of(t);
        public static void Register(Texture2D t,int scale) { scales[t]=scale; }
    }
    internal static class RecoveredTitleLoginSurface
    {
        public static void DestroyTexture(Texture2D t) { if (t != null) t.destroyed=true; }
    }
    internal sealed partial class TitleFlow
    {
        private const float ButtonWidth=152, ButtonHeight=33;
        private Texture2D _loginPanel;
        private GUIStyle _regButtonStyle=new GUIStyle(), _panelStyle=new GUIStyle(), _inputStyle=new GUIStyle();
        private static int assertions;
        private static void Check(bool condition,string reason) { assertions++; if(!condition) throw new Exception(reason); }
        private static Texture2D Texture(int w,int h) => new Texture2D(w,h,TextureFormat.RGBA32,false);
        private static void EventAt(EventType type,int button,float x,float y) { Event.current=new Event {type=type,button=button,mousePosition=new Vector2(x,y)}; }
        public static void Contract(string[] args)
        {
            Check(BuildNativeMemoryPanel(null)==null,"missing panel");
            Check(BuildNativeMemoryPanel(Texture(325,178))==null,"old geometry must not leak hidden-login text");
            Texture2D source=Texture(317,131); source.isReadable=false;
            Check(BuildNativeMemoryPanel(source)==null,"unreadable art fallback"); source.isReadable=true;
            Color32[] pixels=new Color32[317*131];
            for(int y=0;y<131;y++) for(int x=0;x<317;x++) pixels[(130-y)*317+x]=new Color32(0,0,0,(byte)y);
            for(int y=0;y<11;y++)
            {
                pixels[(130-103-y)*317+100]=new Color32((byte)(170-y),(byte)(170-y),(byte)(170-y),255);
                pixels[(130-66-y)*317+157]=new Color32(255,153,34,255);
            }
            source.SetPixels32(pixels);
            Texture2D panel=BuildNativeMemoryPanel(source);
            Check(panel!=null && panel.width==317 && panel.height==131,"native panel size");
            Color32[] result=panel.GetPixels32();
            for(int y=101;y<117;y++) Check(result[(130-y)*317+232].a==y,"same-row alpha retained");
            for(int y=0;y<11;y++)
            {
                Check(result[(130-103-y)*317+182].r==170-y,"native 记忆 color");
                Check(result[(130-103-y)*317+206].r==170-y,"native 密码 gray recolor");
                Check(result[(130-103-y)*317+205].r==0 && result[(130-103-y)*317+205].a==255,"one-pixel black edge");
            }
            Check(source.GetPixels32()[(130-103)*317+182].a==103,"source artwork not mutated");

            TitleFlow flow=new TitleFlow { _loginPanel=source };
            ResLoader.I=new ResLoader { Pkg=new object() };
            foreach(string name in new[]{"loginbtn","candidatebtn","quitbtn"})
                for(int f=0;f<3;f++) ResLoader.I.textures["res/uires/titleui/"+name+".gsa"+f]=Texture(152,33);
            ResLoader.I.textures["res/uires/titleui/check.gsa0"]=Texture(12,12);
            ResLoader.I.textures["res/uires/titleui/check.gsa1"]=Texture(12,12);
            flow.EnsureNativeLoginPresentation();
            Check(ResLoader.I.loads==11,"three states per button and two checkbox frames");
            flow.EnsureNativeLoginPresentation(); Check(ResLoader.I.loads==11,"no per-event reload");
            for(int i=0;i<3;i++)
            {
                GUIStyle style=flow._nativeLoginButtons[i];
                Check(style!=null && style.normal.background!=style.hover.background && style.active.background!=style.normal.background,"three original states");
                Check(style.focused.background==style.hover.background,"keyboard focus uses highlight");
                flow.NativeLoginButton(new Rect(),i,"fallback");
                Check(ReferenceEquals(GUI.lastContent,GUIContent.none),"no text redrawn over baked button");
            }
            Texture2D previous=flow._nativeLoginPanel;
            ResLoader.I.Pkg=new object(); flow.EnsureNativeLoginPresentation();
            Check(previous.destroyed && ResLoader.I.loads==22,"package identity invalidates only owned presentation");
            Rect hit=new Rect(321,450,64,14);
            EventAt(EventType.MouseDown,0,380,454);
            Check(flow.NativeLoginCheck(hit,false),"label click toggles");
            Check(Event.current.type==EventType.Used && GUIUtility.keyboardControl==321,"press consumes/focuses");
            EventAt(EventType.MouseUp,0,322,451); Check(!flow.NativeLoginCheck(hit,false),"release must not toggle twice");
            EventAt(EventType.MouseDown,1,322,451); Check(!flow.NativeLoginCheck(hit,false),"right click ignored");
            EventAt(EventType.MouseDown,0,500,451); Check(!flow.NativeLoginCheck(hit,false),"outside target ignored");
            GUI.enabled=false; EventAt(EventType.MouseDown,0,322,451); Check(!flow.NativeLoginCheck(hit,false),"disabled control"); GUI.enabled=true;
            Event.current=new Event { type=EventType.KeyDown,keyCode=KeyCode.Space };
            Check(flow.NativeLoginCheck(hit,false),"space toggles focused control");
            Event.current=new Event { type=EventType.KeyDown,keyCode=KeyCode.Space }; GUIUtility.keyboardControl=403;
            Check(!flow.NativeLoginCheck(hit,false),"other checkbox focus isolated");
            previous=flow._nativeLoginPanel; Texture2D borrowed=flow._nativeLoginButtons[0].normal.background;
            flow.ReleaseNativeLoginPresentation(); flow.ReleaseNativeLoginPresentation();
            Check(previous.destroyed && !borrowed.destroyed,"idempotent disposal does not destroy resource cache");

            // Optional real-artwork pixel proof. Fixture/output remain in a caller-specified
            // external directory; the repository never includes the copyrighted source art.
            if(args.Length==2)
            {
                byte[] bytes=File.ReadAllBytes(args[0]); Check(bytes.Length==317*131*4,"fixture RGBA dimensions");
                Color32[] original=new Color32[317*131];
                for(int y=0;y<131;y++) for(int x=0;x<317;x++)
                {
                    int p=(y*317+x)*4; original[(130-y)*317+x]=new Color32(bytes[p],bytes[p+1],bytes[p+2],bytes[p+3]);
                }
                source=Texture(317,131); source.SetPixels32(original); result=BuildNativeMemoryPanel(source).GetPixels32();
                for(int y=0;y<131;y++) for(int x=0;x<317;x++)
                {
                    Color32 c=result[(130-y)*317+x]; int p=(y*317+x)*4;
                    bytes[p]=c.r; bytes[p+1]=c.g; bytes[p+2]=c.b; bytes[p+3]=c.a;
                }
                File.WriteAllBytes(args[1],bytes);
            }
            Console.WriteLine("PASS "+assertions+" login presentation assertions (Unity API substitutes, no Unity execution)");
        }
    }
}

internal static class LoginPresentationContract
{
    public static void Main(string[] args) { FakeFantasyOnline.Presentation.ReferenceAssets.TitleFlow.Contract(args); }
}
