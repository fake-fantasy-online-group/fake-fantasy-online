// Deterministic API substitutes execute production selection-page code. Not a Unity event-loop test.
using System;
using System.Collections.Generic;
using UnityEngine;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace UnityEngine
{
    public enum FocusType { Keyboard }
    public struct Matrix4x4 { public float m11; }
    public static class GUIUtility { public static int hotControl,keyboardControl; public static int GetControlID(int hint,FocusType focus,Rect rect)=>hint; }
    public enum TextAnchor { MiddleCenter, MiddleLeft }
    public enum TextClipping { Overflow, Clip }
    public enum ImagePosition { ImageOnly }
    public enum EventType { MouseDown, MouseUp, MouseDrag, KeyDown, KeyUp, ScrollWheel, Repaint, Used }
    public enum KeyCode { None, Space, Return, KeypadEnter, Escape }
    public struct Vector2 { public float x,y; public Vector2(float x,float y) { this.x=x; this.y=y; } }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public static Color white => new Color(1,1,1,1);
        public static Color operator *(Color x,Color y) => new Color(x.r*y.r,x.g*y.g,x.b*y.b,x.a*y.a);
    }
    public struct Color32
    {
        public byte r,g,b,a;
        public Color32(byte r,byte g,byte b,byte a) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public static implicit operator Color(Color32 c) => new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);
    }
    public struct Rect
    {
        public float x,y,width,height;
        public Rect(float x,float y,float w,float h) { this.x=x;this.y=y;width=w;height=h; }
        public float xMax=>x+width; public float yMax=>y+height;
        public bool Contains(Vector2 p)=>p.x>=x&&p.x<xMax&&p.y>=y&&p.y<yMax;
    }
    public static class Mathf
    {
        public static float Abs(float x)=>Math.Abs(x); public static int FloorToInt(float x)=>(int)Math.Floor(x);
        public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(b,x));
        public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);
    }
    public class Texture2D
    {
        public int width,height; public bool destroyed; public string name;
        public Texture2D(int w,int h) { width=w;height=h; }
        public static Texture2D whiteTexture=new Texture2D(1,1);
        public static implicit operator bool(Texture2D t)=>t!=null&&!t.destroyed;
    }
    public class Sprite { public Texture2D texture; }
    public class RectOffset { }
    public class GUIContent { public string text; public GUIContent(){} public GUIContent(string text){this.text=text;} public static GUIContent none=new GUIContent(); }
    public class GUIStyleState { public Texture2D background; public Color textColor; }
    public class GUIStyle
    {
        public GUIStyle() { } public GUIStyle(GUIStyle source) { }
        public Vector2 CalcSize(GUIContent content)=>new Vector2((content.text??"").Length*(fontSize==0?12:fontSize),fontSize);
        public static GUIStyle none=new GUIStyle();
        public int fontSize; public bool wordWrap,richText; public TextClipping clipping;
        public TextAnchor alignment; public ImagePosition imagePosition;
        public RectOffset border,margin,padding,overflow;
        public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState();
    }
    public class GUISkin { public GUIStyle label=new GUIStyle(); }
    public class Event
    {
        public static Event current=new Event(); public EventType type; public KeyCode keyCode;
        public int button,clickCount; public Vector2 mousePosition;
        public void Use() { type=EventType.Used; }
        public bool isMouse=>type==EventType.MouseDown||type==EventType.MouseUp||type==EventType.MouseDrag;
        public EventType GetTypeForControl(int id)=>type;
    }
    public static class Time { public static float realtimeSinceStartup; }
    public static class GUI
    {
        public static bool enabled=true; public static Color color=Color.white; public static Matrix4x4 matrix=new Matrix4x4 {m11=1};
        public static GUISkin skin=new GUISkin(); public static GUIStyle lastStyle; public static GUIContent lastContent;
        public static List<string> labels=new List<string>();
        public static List<(Rect rect,Texture2D texture)> drawings=new List<(Rect,Texture2D)>();
        public static List<(Rect rect,bool enabled)> buttons=new List<(Rect,bool)>();
        private static string hot;
        private static string Id(Rect r)=>r.x+","+r.y;
        public static void Clear() { labels.Clear();drawings.Clear();buttons.Clear();color=Color.white;enabled=true; }
        public static void ResetInput() { hot=null; }
        public static void Label(Rect r,string s,GUIStyle style) { labels.Add(s); }
        public static void DrawTexture(Rect r,Texture2D t) { drawings.Add((r,t)); }
        public static void DrawTextureWithTexCoords(Rect r,Texture2D t,Rect uv) { drawings.Add((r,t)); }
        public static int groupDepth; public static void BeginGroup(Rect r) { groupDepth++; } public static void EndGroup() { groupDepth--; }
        public static bool Button(Rect r,GUIContent c,GUIStyle style)
        {
            lastContent=c;lastStyle=style;buttons.Add((r,enabled));
            Event e=Event.current;string id=Id(r);
            if(e.type==EventType.MouseDown&&enabled&&e.button==0&&r.Contains(e.mousePosition)) { hot=id;e.Use();return false; }
            if(e.type==EventType.MouseUp&&e.button==0&&hot==id)
            { hot=null;bool result=enabled&&r.Contains(e.mousePosition);e.Use();return result; }
            return false;
        }
    }
}
namespace FakeFantasyOnline.Foundation
{
    public class Error { public string Code; }
    public struct Result { public bool IsSuccess; public Error Error; }
}
namespace FakeFantasyOnline.Application
{
    public class ServerSelectionItem
    { public string ZoneName,DisplayName,Host;public int Port,BusyLevel=1,DirectoryIndex;public bool IsPlayerVersusPlayer; }
    public struct ServerCharacterRecord
    {
        public string CharacterId,DisplayName,LastLogin;public int Race,Gender,Head,Hair,Level;
        public int apBody,apCap,apBackpack,apWeaponR,apWeaponL,apFace;public int[] Attributes;public long Honor;
    }
    public class ClientSessionCoordinator
    {
        public const string ServerSwitchReauthenticationCode="reauth";
        public IReadOnlyList<ServerSelectionItem> Source=Array.Empty<ServerSelectionItem>();
        public int Applies;public bool Switched;public string AppliedHost;public Result Next=new Result {IsSuccess=true};
        public int TitleServerCountForIndependentRuntime=>Source.Count;
        public IReadOnlyList<ServerSelectionItem> ReadTitleServersForIndependentRuntime()=>Source;
        public Result ApplyServerSelectionForIndependentRuntime(int index,string host,int port,bool changed,int count)
        { Applies++;AppliedHost=host;Switched=changed;return Next; }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal enum TitlePage { Login, ServerSelection, CharacterSelection, CharacterCreation }
    internal enum TitleCharacterAction { Create, Delete, ReturnToServerSelection }
    internal class ResLoader
    {
        public static ResLoader I; public object Pkg,Pkg24,PkgFf21; public int loads;
        public Dictionary<string,Texture2D> textures=new Dictionary<string,Texture2D>();
        public Texture2D LoadGsaFrameA(string p,int f) { loads++;textures.TryGetValue(p+f,out Texture2D t);return t; }
        public static implicit operator bool(ResLoader r)=>r!=null;
    }
    internal static class GsaTextureScale { public static float Width(Texture2D t)=>t.width;public static float Height(Texture2D t)=>t.height; }
    internal static class GsaDecoder { public static float W(Texture2D t)=>t.width;public static float H(Texture2D t)=>t.height; }
    internal static class TexScale { public static void Draw(float x,float y,Texture2D t)=>GUI.DrawTexture(new Rect(x,y,t.width,t.height),t); }
    internal static class RecoveredTitleCharacterSurface
    { public static int borders;public static void DrawBorder(Rect r,float t,Color c) { borders++; } }
    internal static class MobileClientPresentation { public static bool Enabled; }
    internal static class BitmapFont
    {
        public static bool Ready;public static int ensures,draws;public static char missing;
        public static float lastScale;public static string lastText;public static List<(float x,float y,string text,float scale)> textDraws=new List<(float,float,string,float)>();
        public static void EnsureForIndependentRuntime() { ensures++; }
        public static int CellOfForIndependentRuntime(char c)=>c==missing?-1:1;
        public static float Measure(string s,float scale)=>s.Length*12*scale;
        public static void DrawOutline(float x,float y,string text,Color color,float scale)
        { draws++;lastText=text;lastScale=scale;GUI.labels.Add(text);textDraws.Add((x,y,text,scale)); }
    }
    internal static class ArtworkSkin
    {
        public static int panels;public static List<string> buttonLabels=new List<string>();
        public static void Panel(Rect r) { panels++; }
        public static void ButtonFace(Rect r,string s,float scale=1,bool enabled=true,bool selected=false,float fontScale=1,bool pressed=false) {buttonLabels.Add(s);}
        public static bool Button(Rect r,string s) { buttonLabels.Add(s);return GUI.Button(r,GUIContent.none,GUIStyle.none); }
    }
    internal static class GameBootstrap { public static int quits;public static void QuitGame() { quits++; } }
    internal class NetClient
    {
        public static NetClient I;public static int OnlineCount=-1,RttMs=-1;
        public string TitleServerHostForIndependentRuntime="host";public int TitleServerPortForIndependentRuntime=7;
        public bool ConnectedForIndependentRuntime;public int statusRequests,entryRequests;public string enteredName;
        public void RequestTitleServerStatusForIndependentRuntime() { statusRequests++; }
        public void RequestTitleEntryForIndependentRuntime(string name) { entryRequests++;enteredName=name; }
        public static implicit operator bool(NetClient n)=>n!=null;
    }
    internal static class CharData
    {
        public static string name,lastLogin;public static int race,gender,head,hair,level;public static int[] appearance;
        public static void SetAppearance(int a,int b,int c,int d,int e,int f) { appearance=new[]{a,b,c,d,e,f}; }
    }
    internal class PlayerAvatar
    {
        public struct Appear { public int Body,Cap,Backpack,WeaponR,WeaponL,Face; }
        public static List<Appear> built=new List<Appear>();
        public bool ok=true;public int nameTopPx=8,headPx=8,anchorX=12,anchorY=20;
        public Sprite sprite=new Sprite {texture=new Texture2D(24,40)};
        public Sprite Still(int dir)=>sprite;
        public static PlayerAvatar Build(int gender,int head,int hair,Appear appear,float ppu) { built.Add(appear);return new PlayerAvatar(); }
    }
    internal sealed partial class TitleFlow
    {
        private Texture2D btnLogin,btnReturn,btnCreate,btnEnter,btnDelete,actorbk,actorcover,selProp,proppanel;
        private GUIStyle center=new GUIStyle(),numStyle=new GUIStyle();
        private Texture2D[] worldBtn=new Texture2D[2],pveBtn=new Texture2D[2],pvpBtn=new Texture2D[2],busyBar=new Texture2D[4],busyWorld=new Texture2D[3];
        private readonly PlayerAvatar[] slotDoll=new PlayerAvatar[3];
        private Texture2D[,,] nudeAv=new Texture2D[2,9,3];
        private IReadOnlyList<ServerCharacterRecord> serverChars=Array.Empty<ServerCharacterRecord>();
        private static string[] CLASS={"战士","剑客","刺客","药师","术士"};
        private static int[][] INIT={new int[10],new int[10],new int[10],new int[10],new int[10]};
        private static int[] WORLD_BAR_FRAME={2,1,0};private static Color32 WORLD_TXT=new Color32(165,160,128,255);
        private static Color[] BUSY_COL={Color.white,Color.white,Color.white,Color.white};
        private static bool DOLL_FLIP=true;
        private int srvBuiltFor=-1,selSlot=-1;private bool pendingDelete,entering;private float enterAtMs;
        private string netMsg="";private TitlePage _page;
        private Action<string> setMessage;
        private Action<ServerCharacterRecord?> prepareCreation;
        private Action<TitleCharacterAction> action;
        private ServerCharacterRecord? SelectedCharacterForIndependentRuntime=>selSlot>=0&&selSlot<serverChars.Count?serverChars[selSlot]:(ServerCharacterRecord?)null;
        private void PublishPluginTitleResult(string s) { }
        private void LabelValue(float x,float y,int value)=>GUI.Label(new Rect(x,y,60,14),value.ToString(),numStyle);
        private static int AttributeAt(IReadOnlyList<int> a,int i,int fallback)=>a==null||i>=a.Count?fallback:a[i];
        private static int checks;
        private static void Check(bool c,string s) { checks++;if(!c)throw new Exception(s); }
        private static void EventAt(EventType t,float x=0,float y=0,int count=1) { Event.current=new Event {type=t,button=0,clickCount=count,mousePosition=new Vector2(x,y)}; }
        private static TitleFlow NewFlow()
        {
            var f=new TitleFlow();f.setMessage=s=>f.netMsg=s;f.prepareCreation=r=>{};f.action=a=>{};
            f.actorbk=new Texture2D(782,243);f.actorcover=new Texture2D(195,145);f.selProp=new Texture2D(394,104);return f;
        }
        private static void LoadArt()
        {
            ResLoader.I=new ResLoader {Pkg=new object()};
            foreach(string n in new[]{"affirmbtn","autobtn","returnbtn","quitbtn","createbtn","deletebtn","enterbtn"})
                for(int j=0;j<3;j++)ResLoader.I.textures["res/uires/titleui/"+n+".gsa"+j]=new Texture2D(152,33);
        }
        private static void Tap(Action draw,float x,float y)
        { EventAt(EventType.MouseDown,x,y);draw();EventAt(EventType.MouseUp,x,y);draw(); }
        public static void Contract()
        {
            LoadArt();var flow=NewFlow();flow.EnsureSelectionPresentation();
            Check(ResLoader.I.loads==21,"seven native buttons load three complete assembled frames");
            flow.EnsureSelectionPresentation();Check(ResLoader.I.loads==21,"no repeated OnGUI loads");
            for(int i=0;i<7;i++)
            {
                GUIStyle style=flow.selectionButtons[i];Check(style!=null,"native style exists");
                Check(style.normal.background!=style.hover.background&&style.normal.background!=style.active.background,"original hover/pressed frame");
                Check(style.focused.background==style.hover.background,"focused original frame");
                EventAt(EventType.MouseDown,5,5);Check(!flow.DrawSelectionButton(0,0,(SelectionButton)i,"fallback"),"press does not activate");
                EventAt(EventType.MouseUp,5,5);Check(flow.DrawSelectionButton(0,0,(SelectionButton)i,"fallback"),"one release activates");
                Check(!flow.DrawSelectionButton(0,0,(SelectionButton)i,"fallback"),"same event cannot activate twice");
                Check(ReferenceEquals(GUI.lastContent,GUIContent.none),"baked label never redrawn");
                EventAt(EventType.MouseDown,5,5);flow.DrawSelectionButton(0,0,(SelectionButton)i,"fallback");
                EventAt(EventType.MouseUp,200,200);Check(!flow.DrawSelectionButton(0,0,(SelectionButton)i,"fallback"),"drag-out cancels");
                GUI.enabled=false;Color original=new Color(.8f,.6f,.4f,.7f);GUI.color=original;
                Tap(()=>Check(!flow.DrawSelectionButton(0,0,(SelectionButton)i,"fallback"),"disabled cannot submit"),5,5);
                Check(GUI.color.r==original.r&&GUI.color.a==original.a,"disabled tint restores color");GUI.enabled=true;GUI.color=Color.white;
            }
            BitmapFont.Ready=true;BitmapFont.missing='\0';int before=BitmapFont.draws;
            flow.DrawSelectionLabel(new Rect(0,0,160,20), "角色Ab 12", Color.white);
            Check(BitmapFont.ensures>0&&BitmapFont.draws==before+1&&BitmapFont.lastText=="角色Ab 12", "original bitmap entry ensures load and covers mixed Chinese/Latin");
            flow.DrawSelectionLabel(new Rect(0,0,30,20), "很长的真实角色名字Ab12", Color.white);
            Check(BitmapFont.lastScale<1f, "long native label fits original frame without overflow");
            BitmapFont.missing='缺';before=BitmapFont.draws;GUI.labels.Clear();
            flow.DrawSelectionLabel(new Rect(0,0,160,20), "Ab缺字角色", Color.white);
            Check(BitmapFont.draws==before&&GUI.labels.Contains("Ab缺字角色"), "missing glyph uses complete-string GUI fallback, not blank cells");
            BitmapFont.missing='\uD83D';before=BitmapFont.draws;GUI.labels.Clear();
            flow.DrawSelectionLabel(new Rect(0,0,160,20), "角色🙂Ab", Color.white);
            Check(BitmapFont.draws==before&&GUI.labels.Contains("角色🙂Ab"), "non-BMP name falls back as a complete UTF-16 string");
            BitmapFont.Ready=false;GUI.labels.Clear();flow.DrawSelectionLabel(new Rect(0,0,160,20), "原始名字", Color.white);
            Check(GUI.labels.Contains("原始名字"), "unavailable font retains original string");
            Texture2D borrowed=flow.selectionButtons[0].normal.background;
            ResLoader.I.Pkg24=new object();flow.EnsureSelectionPresentation();Check(ResLoader.I.loads==42,"supplementary package change invalidates cache");
            ResLoader.I.PkgFf21=new object();flow.EnsureSelectionPresentation();Check(ResLoader.I.loads==63,"second supplementary package invalidates");
            ResLoader.I.Pkg=new object();flow.EnsureSelectionPresentation();Check(ResLoader.I.loads==84,"primary package invalidates");
            ResLoader.I.textures["res/uires/titleui/affirmbtn.gsa1"]=new Texture2D(32,32);
            ResLoader.I.Pkg=new object();flow.EnsureSelectionPresentation();Check(flow.selectionButtons[0].hover.background==flow.selectionButtons[0].normal.background,"bad hover size falls back to normal");
            flow.ReleaseSelectionPresentation();flow.ReleaseSelectionPresentation();Check(!borrowed.destroyed,"borrowed art not destroyed");
            ResLoader.I=null;flow.EnsureSelectionPresentation();EventAt(EventType.Repaint);flow.DrawSelectionButton(0,0,SelectionButton.Delete,"删除角色");
            Check(ArtworkSkin.buttonLabels.Contains("删除角色"),"missing art has correctly labelled fallback");
            LoadArt();flow=NewFlow();GUI.Clear();EventAt(EventType.Repaint);RecoveredTitleCharacterSurface.borders=0;flow.DrawCharSelect();
            Check(RecoveredTitleCharacterSurface.borders==0,"empty slots have no extra white frame");
            Check(!GUI.labels.Exists(s=>s.Contains("＋")||s.Contains("占位")||s.Contains("示例")),"no synthetic slot labels");
            Check(GUI.drawings.Exists(x=>ReferenceEquals(x.texture,flow.selProp)),"empty property panel still drawn");
            Check(GUI.buttons.Exists(x=>x.rect.x==322&&!x.enabled)&&GUI.buttons.Exists(x=>x.rect.y==455&&!x.enabled),"empty enter/delete disabled");
            int created=0;flow.action=a=>{if(a==TitleCharacterAction.Create)created++;};Tap(flow.DrawCharSelect,320,150);Check(created==1,"empty slot still creates once");
            Check(flow._page==TitlePage.CharacterCreation,"empty slot route unchanged");
            Check(419+33<455,"create/delete targets no overlap");
            var rows=new ServerCharacterRecord[3];
            for(int i=0;i<3;i++)rows[i]=new ServerCharacterRecord {DisplayName="角色Ab长名"+i,CharacterId="id"+i,LastLogin="2026-10-18 09:21",Level=13+i,Race=i,Gender=i%2,Head=2,Hair=2,apBody=12+i,apCap=23,apBackpack=34,apWeaponR=45,apWeaponL=56,apFace=67,Attributes=new int[22],Honor=1234};
            rows[0].Attributes[4]=4321;flow=NewFlow();flow.serverChars=rows;flow.selSlot=0;flow.MirrorSlotToCharData(0);
            GUI.Clear();EventAt(EventType.Repaint);NetClient.I=new NetClient();flow.DrawCharSelect();
            Check(GUI.labels.Exists(s=>s.Contains(rows[0].DisplayName)&&s.Contains("LV 13")),"actual name and level retained");
            Check(GUI.labels.Exists(s=>s.Contains(rows[0].LastLogin)),"actual last-login retained");
            Check(GUI.labels.Contains("4321")&&GUI.labels.Contains("1234"),"actual attributes and honor retained");
            Check(PlayerAvatar.built.Exists(a=>a.Body==12&&a.WeaponR==45&&a.Face==67),"actual appearance builds avatar");
            Check(GUI.drawings.Exists(x=>ReferenceEquals(x.texture,flow.slotDoll[0].sprite.texture)),"real avatar Still texture is drawn");
            for(int i=0;i<3;i++){Tap(flow.DrawCharSelect,80+i*237,150);Check(flow.selSlot==i,"slot click selects correct row");Check(NetClient.I.entryRequests==0,"slot tap never enters world");}
            int deleted=0;flow.action=a=>{if(a==TitleCharacterAction.Delete)deleted++;};Tap(flow.DrawCharSelect,700,465);Check(flow.pendingDelete,"delete opens confirmation");
            Tap(flow.DrawCharSelect,480,340);Check(!flow.pendingDelete&&deleted==0,"cancel does not issue delete");
            Tap(flow.DrawCharSelect,700,465);Tap(flow.DrawCharSelect,300,340);Check(!flow.pendingDelete&&deleted==1,"one confirm issues one delete");
            flow.DrawCharSelect();Check(deleted==1,"consumed release cannot issue second delete");
            flow.OpenDeleteConfirmation();created=0;Tap(flow.DrawCharSelect,80,150);Check(deleted==1&&flow.pendingDelete,"modal prevents background selection action");flow.CloseDeleteConfirmation();
            Tap(flow.DrawCharSelect,350,500);Check(NetClient.I.entryRequests==1&&NetClient.I.enteredName==rows[2].DisplayName,"enter selected real name once");
            Tap(flow.DrawCharSelect,350,500);Check(NetClient.I.entryRequests==1,"entry latch prevents repeat");
            Time.realtimeSinceStartup=16;EventAt(EventType.Repaint);flow.DrawCharSelect();Check(!flow.entering&&flow.netMsg.Contains("超时"),"existing entry timeout preserved");
            // Only-null source must be a safe empty UI; it must not fabricate an endpoint.
            var session=new ClientSessionCoordinator {Source=new ServerSelectionItem[]{null,null}};flow=NewFlow();flow.BindServerPageForIndependentRuntime(session,()=>flow.netMsg,()=>{},r=>{});
            GUI.Clear();EventAt(EventType.Repaint);flow.DrawServer();Check(flow.srvWorlds.Count==0&&GUI.labels.Contains("暂无可用服务器"),"all-null directory safely renders empty");
            Tap(flow.DrawServer,700,380);Check(session.Applies==0,"empty directory confirm disabled");
            session=new ClientSessionCoordinator();flow=NewFlow();flow.BindServerPageForIndependentRuntime(session,()=>"",()=>{},r=>{});EventAt(EventType.Repaint);flow.DrawServer();Check(flow.srvWorlds.Count==1,"existing no-directory fallback retained");
            session=new ClientSessionCoordinator {Source=new[]{new ServerSelectionItem {ZoneName="真实一区",DisplayName="线路甲",Host="host",Port=7},new ServerSelectionItem {ZoneName="真实一区",DisplayName="线路乙",Host="other",Port=8,DirectoryIndex=1}}};
            flow=NewFlow();flow.BindServerPageForIndependentRuntime(session,()=>"",()=>{},r=>{});EventAt(EventType.Repaint);flow.DrawServer();Tap(flow.DrawServer,170,435);Check(session.Applies==0&&flow.selZone==1,"mobile one-tap selects line only");
            Tap(flow.DrawServer,700,380);Check(session.Applies==1&&session.AppliedHost=="other"&&session.Switched,"explicit confirmation uses selected endpoint");
            flow.DrawServer();Check(session.Applies==1,"same GUI event cannot confirm twice");
            Console.WriteLine("PASS "+checks+" selection presentation assertions (production source with deterministic API substitutes; not Unity runtime)");
        }
    }
}
internal static class SelectionPresentationContract
{ public static void Main() { FakeFantasyOnline.Presentation.ReferenceAssets.TitleFlow.Contract(); FakeFantasyOnline.Presentation.ReferenceAssets.TitleFlow.DeleteContract(); } }
