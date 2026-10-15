using System;
using System.Collections.Generic;
using UnityEngine;

// Engine-free draw/interaction recorders. This compiles the actual mobile chat
// renderer and collector; it is not a claim of Unity or native-IME execution.
namespace UnityEngine
{
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x,float y,float width,float height) { this.x=x; this.y=y; this.width=width; this.height=height; }
        public float xMax=>x+width; public float yMax=>y+height;
        public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;
    }
    public struct Color { public Color(float r,float g,float b,float a) {} }
    public struct Color32 { public Color32(byte r,byte g,byte b,byte a) {} }
    public class Texture2D {}
    public class Font {}
    public enum TextAnchor { MiddleCenter }
    public enum TextClipping { Clip }
    public class RectOffset { public RectOffset(int left,int right,int top,int bottom) {} }
    public class GUIStyleState { public Color textColor; }
    public class GUIStyle
    {
        public static GUIStyle none=new GUIStyle(); public Font font; public int fontSize;
        public TextAnchor alignment; public TextClipping clipping; public bool wordWrap; public RectOffset padding;
        public GUIStyleState normal=new GUIStyleState(); public GUIStyle() {} public GUIStyle(GUIStyle other) {}
    }
    public class GUIContent { public static GUIContent none=new GUIContent(); }
    public enum EventType { Repaint, MouseDown, MouseUp, KeyDown, Used }
    public enum KeyCode { None, Return, KeypadEnter }
    public class Event
    {
        public int button; public static Event current; public EventType type; public KeyCode keyCode; public Vector2 mousePosition;
        public void Use() { type=EventType.Used; }
    }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static int RoundToInt(float a)=>(int)Math.Round(a);
    }
    public static class Input { public static bool GetMouseButton(int n)=>false; }
    public static class GUI
    {
        public static readonly List<Rect> Buttons=new List<Rect>(), Textures=new List<Rect>();
        public static readonly List<string> Trace=new List<string>();
        public static string Focus; public static int Click=-1; public static GUIStyle LastLabelStyle;
        public static bool Button(Rect r,GUIContent content,GUIStyle style)
        {
            if (!ReferenceEquals(content,GUIContent.none)||!ReferenceEquals(style,GUIStyle.none))
                throw new Exception("Mobile chat must never use default skin buttons");
            int index=Buttons.Count; Buttons.Add(r); return index==Click;
        }
        public static void DrawTexture(Rect r,Texture2D t) { Textures.Add(r); }
        public static void Label(Rect r,string text,GUIStyle style) { LastLabelStyle=style; }
        public static string GetNameOfFocusedControl()=>Focus;
        public static void FocusControl(string name) { Trace.Add("focus:"+(name??"none")); Focus=name; }
        public static void Reset() { Buttons.Clear(); Textures.Clear(); Trace.Clear(); Click=-1; LastLabelStyle=null; }
    }
}

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class MobileClientPresentation
    {
        public static float Scale=1; public static Rect ChatRect; public static bool ChatExpanded, MenuExpanded, ModalOpen;
        public static void RegisterPanel(object owner,Rect panel) {} public static void RemovePanel(object owner) {}
    }
    internal static class Win05
    {
        public static string LastOpened; public static int Opens;
        public static void Open(string name) { LastOpened=name; Opens++; MobileClientPresentation.ModalOpen=true; }
    }
    internal static class GameHud { public static bool MobileShortcutPointerOwned; }
    internal static class ArtworkSkin
    {
        public static int Inputs, Icons;
        public static void Panel(Rect r,float scale) {} public static void Row(Rect r,float scale,bool selected) {}
        public static void Input(Rect r,float scale) { Inputs++; }
        public static void IconFace(Rect art,Texture2D[] frames,bool hover,bool pressed) { Icons++; }
    }
    internal static class BitmapFont
    {
        public static bool Ready=true; public static readonly List<string> Labels=new List<string>();
        public static float LastScale;
        public static float Measure(string text,float scale) { LastScale=scale; return text.Length*12*scale; }
        public static void CenterOutline(Rect rect,string text,Color32 color,float scale) { Labels.Add(text); }
    }
    internal static class NpcChatter { public static readonly Font Font=new Font(); public static Font CnFont()=>Font; }
    internal static class HornDialog { public static bool Typing; }
    internal static class DuelDlg { public static bool Typing; }
    internal static class UiInput
    {
        public static Rect FieldRect; public static string Result; public static bool ConsumeReturn;
        public static string Field(Rect rect,string name,string before,int maximum,GUIStyle style,float scale)
        {
            if(name!="chatinput"||maximum!=120) throw new Exception("Existing input identity/limit changed");
            GUI.Trace.Add("field"); FieldRect=rect;
            if(ConsumeReturn) Event.current.Use();
            return Result??before;
        }
    }
    internal partial class ChatUI
    {
        internal struct Line { public string ch, text; }
        private static readonly List<Line> chatLines=new List<Line>();
        private static readonly object pendingRefs=new object();
        private static readonly string[] Channels={"附近","队伍","家族","世界"};
        private Texture2D inputBar=new Texture2D();
        private Texture2D[] dropBtn={new Texture2D()}, sendBtn={new Texture2D()}, sizeBtn={new Texture2D()};
        private GUIStyle editSt=new GUIStyle(); private float s; private string inputText="",channel="附近",whisperTarget="";
        private bool chanOpen; private bool interactive; private int chIdx; private static bool _pendingFocus,TypingNow;
        private static int filters; private int panels,sends; private string sent,guardBefore; private Rect messageRect;
        private List<Line> drawnLines;
        private static void LoadFilter() { filters++; }
        private static bool ChanShown(string channel)=>channel!="hidden";
        private void DrawPanel(Rect rect) { panels++; }
        private void DrawLines(List<Line> lines,Rect rect,bool channels,bool interactive=true) { messageRect=rect; drawnLines=lines; this.interactive=interactive; }
        private void EnsureEditStyle() { editSt.fontSize=Mathf.RoundToInt(12*s); }
        private string GuardShareEdit(string before,string edited,object refs)
        {
            if(!ReferenceEquals(refs,pendingRefs)) throw new Exception("Share reference ownership changed");
            guardBefore=before; GUI.Trace.Add("guard"); return edited;
        }
        private void SendNow() { GUI.Trace.Add("send"); sends++; sent=inputText; inputText=""; }
        private static int assertions;
        private static void Check(bool value,string name) { assertions++; if(!value) throw new Exception(name); }
        private static bool Near(float a,float b)=>Math.Abs(a-b)<.01f;
        private static bool Overlap(Rect a,Rect b)=>a.x<b.xMax&&b.x<a.xMax&&a.y<b.yMax&&b.y<a.yMax;
        private static ChatUI Fresh(float scale=1)
        {
            GameHud.MobileShortcutPointerOwned=false; GUI.Reset(); GUI.Focus=null; BitmapFont.Ready=true; BitmapFont.Labels.Clear();
            ArtworkSkin.Inputs=ArtworkSkin.Icons=0; UiInput.Result=null; UiInput.ConsumeReturn=false;
            HornDialog.Typing=DuelDlg.Typing=false; _pendingFocus=TypingNow=false;
            Event.current=new Event { type=EventType.Repaint };
            MobileClientPresentation.Scale=scale; MobileClientPresentation.ChatExpanded=false;
            MobileClientPresentation.MenuExpanded=false; MobileClientPresentation.ModalOpen=false;
            Win05.LastOpened=null; Win05.Opens=0;
            MobileClientPresentation.ChatRect=new Rect(300*scale,400*scale,360*scale,112*scale);
            chatLines.Clear(); filters=0;
            return new ChatUI();
        }
        private static void Main()
        {
            foreach(float scale in new[]{.65f,1f,1.75f,2.5f})
            {
                ChatUI chat=Fresh(scale);
                chatLines.Add(new Line{ch="附近",text="first"});
                chatLines.Add(new Line{ch="系统",text="second"});
                chatLines.Add(new Line{ch="hidden",text="filtered"});
                chatLines.Add(new Line{ch="世界",text="third"});
                chat.DrawMobileChat();
                Check(chat.panels==0,"Collapsed messages never draw a giant enclosing frame");
                Check((int)Math.Floor((chat.messageRect.height-10*scale)/(18*scale)+.0001f)==3,"Three full original-font rows fit");
                Check(chat.drawnLines.Count==3&&chat.drawnLines[1].text=="second"&&chat.drawnLines[2].text=="third","Renderer uses filtered chronological messages");
                Check(filters==1,"Renderer loads the character filter");
                Check(GUI.Buttons.Count==2,"Exactly one channel and send target, no expansion button");
                Check(GUI.Buttons.TrueForAll(r=>Near(r.height,44*scale)&&r.width>=44*scale),"Touch targets retain at least 44 units");
                Check(Near(UiInput.FieldRect.height,44*scale)&&UiInput.FieldRect.width>=200*scale,"Composer is directly tappable at full touch height");
                foreach(Rect button in GUI.Buttons) Check(!Overlap(button,UiInput.FieldRect),"Input never overlaps channel/send/toggle target");
                Check(GUI.Textures.Count==1&&Near(GUI.Textures[0].height,32*scale),"Only slim original input-bar artwork is drawn");
                Check(ArtworkSkin.Icons==2,"Original channel/send sprites are used");
                Check(BitmapFont.Labels.Contains("附近")&&!BitmapFont.Labels.Contains("展开")&&BitmapFont.Labels.Contains("点击输入"),"Readable bitmap channel, expansion and empty-input affordance");
                Check(Near(BitmapFont.LastScale,scale)&&chat.editSt.fontSize==Mathf.RoundToInt(12*scale),"Bitmap labels and field follow mobile scale");
            }

            ChatUI c=Fresh(); c.inputText="before"; UiInput.Result="edited"; GUI.Click=1; c.DrawMobileChat();
            Check(c.sends==1&&c.sent=="edited"&&c.guardBefore=="before","Send uses guarded current edit through existing sender");
            Check(string.Join(",",GUI.Trace)=="field,guard,send,focus:none","Edit/share guard/send/focus release ordering is preserved");
            Check(!MobileClientPresentation.ChatExpanded,"Sending does not expand chat");

            c=Fresh(); c.inputText="return"; GUI.Focus="chatinput"; Event.current.type=EventType.KeyDown;
            Event.current.keyCode=KeyCode.Return; UiInput.ConsumeReturn=true; c.DrawMobileChat();
            Check(c.sends==1&&c.sent=="return"&&GUI.Focus==null,"Return sends once even when TextField consumes it");
            GUI.Reset(); c.DrawMobileChat(); Check(c.sends==1,"Consumed Return never sends again on repaint");

            c=Fresh(); c.inputText="draft"; GUI.Focus="horn"; HornDialog.Typing=true;
            Event.current.type=EventType.KeyDown; Event.current.keyCode=KeyCode.Return; c.DrawMobileChat();
            Check(TypingNow&&c.sends==0&&c.inputText=="draft"&&Event.current.type==EventType.KeyDown,"Horn typing does not submit or consume chat Return");
            c=Fresh(); DuelDlg.Typing=true; Event.current.type=EventType.KeyDown; Event.current.keyCode=KeyCode.KeypadEnter;
            c.DrawMobileChat(); Check(TypingNow&&c.sends==0,"Duel typing does not submit chat");

            c=Fresh(); _pendingFocus=true; c.DrawMobileChat();
            Check(GUI.Focus=="chatinput"&&!_pendingFocus&&TypingNow,"Pending whisper/share focus is restored once");
            Check(!BitmapFont.Labels.Contains("点击输入"),"Placeholder disappears while editing");

            c=Fresh(); c.channel="密语"; c.whisperTarget="recipient"; GUI.Click=0; c.DrawMobileChat();
            Check(c.chanOpen&&c.channel=="密语"&&c.whisperTarget=="recipient","Channel tap opens picker without cycling or losing whisper routing");
            Check(GUI.Buttons.Count==6,"Picker exposes four direct touch-sized choices");
            Check(GUI.Buttons[2].y<GUI.Buttons[0].y,"Channel picker opens upward");
            GUI.Reset(); GUI.Click=5; c.DrawMobileChat();
            Check(!c.chanOpen&&c.channel=="世界"&&c.chIdx==3&&c.whisperTarget=="","Choosing specific channel exits whisper routing");

            c=Fresh(); MobileClientPresentation.MenuExpanded=true;
            Event.current.type=EventType.MouseDown; Event.current.mousePosition=new Vector2(320,420); c.DrawMobileChat();
            Check(!c.interactive&&!MobileClientPresentation.ChatExpanded,"Collapsed rows cannot expose actions on press");
            GUI.Reset(); Event.current.type=EventType.MouseUp; c.DrawMobileChat();
            Check(Win05.LastOpened=="historybar"&&Win05.Opens==1&&!MobileClientPresentation.MenuExpanded,"Message tap uses original viewbtn historybar entry and closes function grid");
            Check(!MobileClientPresentation.ChatExpanded&&Event.current.type==EventType.Used,"Opening original details consumes release without enlarging HUD");
            GUI.Reset(); Event.current.type=EventType.Repaint; c.DrawMobileChat();
            Check(c.panels==0&&Near(c.messageRect.height,68)&&!c.interactive,"Details leave preview compact and display-only");
            GUI.Reset(); Event.current.type=EventType.MouseDown; c.DrawMobileChat();
            GUI.Reset(); Event.current.type=EventType.MouseUp; c.DrawMobileChat();
            Check(Win05.Opens==1,"Modal details block preview taps from reopening or stealing navigation");
            MobileClientPresentation.ModalOpen=false;
            GUI.Reset(); Event.current.type=EventType.MouseDown; c.DrawMobileChat();
            GUI.Reset(); Event.current.type=EventType.MouseUp; c.DrawMobileChat();
            Check(Win05.Opens==2&&Win05.LastOpened=="historybar","After closing details a new tap reopens the same original history window");
            c=Fresh(); Event.current.type=EventType.MouseDown; Event.current.mousePosition=new Vector2(320,420); c.DrawMobileChat();
            MobileClientPresentation.ModalOpen=true;
            GUI.Reset(); Event.current.type=EventType.MouseUp; c.DrawMobileChat();
            Check(Win05.Opens==0,"A modal opening during a press cancels the pending preview tap");
            c=Fresh(); Event.current.type=EventType.MouseDown; Event.current.button=1; Event.current.mousePosition=new Vector2(320,420); c.DrawMobileChat();
            Check(!c.interactive&&Win05.Opens==0,"Collapsed right-click / long press cannot expose copy menu");

            c=Fresh(); Event.current.type=EventType.MouseDown; Event.current.mousePosition=new Vector2(320,420); c.DrawMobileChat();
            GameHud.MobileShortcutPointerOwned=true; GUI.Reset(); Event.current.type=EventType.MouseUp; c.DrawMobileChat();
            GameHud.MobileShortcutPointerOwned=false; GUI.Reset(); c.DrawMobileChat();
            Check(Win05.Opens==0,"Shortcut ownership cancels any pending chat tap, including its release frame");
            c=Fresh(); c.chanOpen=true; Event.current.type=EventType.MouseDown; Event.current.mousePosition=new Vector2(50,50); c.DrawMobileChat();
            Check(!c.chanOpen&&c.channel=="附近","Outside tap closes picker without changing channel");

            c=Fresh(2); BitmapFont.Ready=false; c.inputBar=null; c.sendBtn=null; c.DrawMobileChat();
            Check(ArtworkSkin.Inputs==1,"Missing chat bar uses other original input artwork");
            Check(GUI.LastLabelStyle.font==NpcChatter.Font&&GUI.LastLabelStyle.fontSize==24,"Missing bitmap atlas gets scaled Chinese font, never generic Unity labels");
            Check(c.sends==0&&GUI.Buttons.Count==2,"Missing sprites keep controls active without automatic sends");
            Console.WriteLine("PASS mobile chat visuals: "+assertions+" assertions; actual renderer/collector, deterministic Unity/input recorders. No Unity or native IME execution.");
        }
    }
}
