// Boundary stubs only. DrawHistory, ChatTabs/cache state, BuildMenu, both
// ActivateOption overloads, option binding/storage and family routing are production code.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector2 { public float x, y; public Vector2(float x, float y) { this.x=x; this.y=y; } }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x,float y,float w,float h) { this.x=x; this.y=y; width=w; height=h; }
        public bool Contains(Vector2 p) => p.x>=x && p.y>=y && p.x<x+width && p.y<y+height;
    }
    public struct Color
    {
        public float r,g,b,a;
        public Color(float r,float g,float b,float a=1) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public static Color white => new Color(1,1,1);
    }
    public struct Color32
    {
        public byte r,g,b,a;
        public Color32(byte r,byte g,byte b,byte a) { this.r=r;this.g=g;this.b=b;this.a=a; }
        public static implicit operator Color(Color32 c) => new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);
    }
    public class Texture2D { public static Texture2D whiteTexture=new Texture2D(); }
    public class Font { }
    public enum TextAnchor { UpperLeft,MiddleLeft }
    public class GUIStyleState { public Color textColor; }
    public class GUIStyle
    {
        public static GUIStyle none=new GUIStyle();
        public GUIStyle() { } public GUIStyle(GUIStyle other) { }
        public Font font; public TextAnchor alignment; public bool wordWrap,richText; public int fontSize;
        public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState(),focused=new GUIStyleState();
    }
    public enum EventType { Repaint,MouseDown,Used }
    public class Event
    {
        public static Event current=new Event(); public EventType type; public int button; public Vector2 mousePosition;
        public void Use() => type=EventType.Used;
    }
    public static class GUI
    {
        public static Color color=Color.white;
        public static void DrawTexture(Rect r,Texture2D t) { }
        public static Vector2 BeginScrollView(Rect view,Vector2 scroll,Rect content,GUIStyle h,GUIStyle v) => scroll;
        public static void EndScrollView() { }
    }
    public static class Mathf
    {
        public static bool Approximately(float a,float b) => Math.Abs(a-b)<.00001f;
        public static int RoundToInt(float v) => (int)Math.Round(v);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int Max(int a,int b) => Math.Max(a,b);
        public static float Max(float a,float b) => Math.Max(a,b);
        public static int Min(int a,int b) => Math.Min(a,b);
    }
    public static class Random { public static int Range(int a,int b) => a; }
}
namespace FakeFantasyOnline.Application
{
    public class WorldNpcTaskOfferState
    {
        public int TaskId,State; public string Title;
        public WorldNpcTaskOfferState(int id,int state,string title,string desc,string award) { TaskId=id;State=state;Title=title; }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    using FakeFantasyOnline.Application;
    public class Share { }
    public static class NpcChatter { public static Font CnFont() => null; }
    public class ResLoader
    {
        public static ResLoader I=new ResLoader(); public object Pkg=new object();
        public Texture2D LoadGsa(string path) => new Texture2D();
    }
    public static class GsaDecoder { public static float W(Texture2D t) => 550; }
    public static class ArtworkSkin { public static void Panel(Rect r,float s,bool selected) { } }
    public static class BitmapFont
    {
        public static bool Ready=true; public static List<string> Text=new List<string>();
        public static void Draw(float x,float y,string t,Color c,float s) => Text.Add(t);
        public static void CenterOutline(Rect r,string t,Color c,float s) => Text.Add(t);
    }
    public static class EmoteText { public static List<string> Wrap(string s,float w,float scale) => new List<string>{s}; }
    public static class ChatUI
    {
        public static int Ver; public static readonly List<(string,string,Color,string,List<Share>)> Lines=new List<(string,string,Color,string,List<Share>)>();
        public static readonly List<string> Drawn=new List<string>();
        public static List<(string,string,Color,string,List<Share>)> Snapshot() => Lines;
        public static void DrawShareRow(float x,float y,string text,Color c,List<Share> shares,float scale,bool wrap) => Drawn.Add(text);
        public static void ShareHover(float x,float y,string text,List<Share> shares,float scale) { }
        public static string RecentText(int count) => "";
        public static void Add(string channel,string text,Color color) { }
    }
    public static class CharData { public static string name="self"; }
    public static class CombatWorld { public static int FindPlayerIdByName(string name) => 1; }
    public static class PlayerActionMenu { public static void Open(int id,string who,string text,string recent) { } }
    public static class NativeScroll { public static void BarAt(object owner,Rect r,float h,float content,ref Vector2 scroll) { } }
    public partial class Win05
    {
        private class Def { public float w=550; }
        private static readonly Dictionary<string,Def> Defs=new Dictionary<string,Def>{{"historybar",new Def()}};
        private float s=1;
        public static int NoticeCalls;
        private Texture2D Bk(Def d) => null;
        private void DrawNoticeTab(Func<float,float,float,float,Rect> l,Def d,Event ev) => NoticeCalls++;
        public void Paint(float scale) { s=scale; DrawHistory((x,y,w,h)=>new Rect(x*s,y*s,w*s,h*s),HistoryDefinition); }
        public static int Selected => chatTab;
        public static string[] Labels => ChatTabs.Select(x=>x.Item2).ToArray();
        public static void OpenLifeLearn() { }
    }
    public static class GameBootstrap { public static int CurMapId; public static Action<int,int,int> DoTeleport; }
    public class NetClient { public static NetClient I=new NetClient(); public void BuyWarehouseForIndependentRuntime() { } }
    public static class FamilyCreateDlg
    {
        public static bool Visible; public static int Toggles;
        public static bool get_Visible() => Visible;
        public static void Toggle() { Toggles++;Visible=!Visible; }
    }
    public static class ShopDlg { public static int Last;public static bool Selling; public static void Open(int id,bool selling) { Last=id;Selling=selling; } }
    public static class SocialClient { public static string FamilyName=""; }
    public static class StoreDlg { public static void Toggle() { } }
    public static class FamilyStashDlg { public static void Open() { } }
    public static class PetArousalDlg { public static void Open() { } }
    public static class AccountSecDlg { public static void Open(int mode) { } }
    public static class AccountSec { public static bool HasSecCode; }
    public static class EndgamePanel { public static bool WorldEntryAvailable; }
    public static class SysMsgBox { public static void Show(string text,Action ok) { } }
    public static class TaskClient
    {
        public class Offer { public int id,state; public string title,desc,award; }
        public static string OfferNpcName;
        public static List<Offer> Offers=new List<Offer>();
        public static void Accept(int id) { } public static void Complete(int id) { }
    }
    public sealed partial class DialogUI
    {
        public string NpcName; public bool Visible,InSubView; public string Greeting;
        public int CurrentSellList; private int curTrans;
        private readonly List<string> optLabels=new List<string>();
        private readonly List<Action> optActs=new List<Action>();
        private const string LeaveLabel="离开";
        private static readonly (string name,int map,int x,int y)[] NoviceVillages={ ("东",1,1,1),("西",2,1,1) };
        private ReferenceNpcTaskMutationIntent LastTaskMutationIntent;
        public string[] Labels => optLabels.ToArray();
        public void Build() => BuildMenu();
        public bool Click(int index) => ActivateOption(index);
        public Action Captured(int index) => optActs[index];
        // Unrelated services are boundary substitutes; the tested production menu
        // performs all ordering, matching, leave-row and captured callback decisions.
        private void ShowVillageMenu(string region,int east,int west) { }
        private void ShowAscendMenu() { } private void ShowTransferMenu() { }
        private void AddDungeonEntryOptions() { } private void AddHairSalonOptions() { }
        private void AddActivityOptions() { } private void AddTrialOption(string l,string c) { }
        private void AddRiftEntranceOption() { } private void AddCubeWorkbenchOption() { }
        private void AddSpiritWorkbenchOptions() { } private void AddRiftExitOptions() { }
        private void AddDungeonExitAndHardOptions() { } private void AddTianshenOptions() { }
        private void ShowSay(string text) { }
        private bool ShowOffer(WorldNpcTaskOfferState row) => true;
        private void ShowOffer(TaskClient.Offer row) { }
        private static string PrefixFor(WorldNpcTaskOfferState row) => "任务：";
    }
    internal static class NativeControlsContract
    {
        private static int checks;
        private static void Check(bool ok,string why) { checks++; if(!ok) throw new Exception(why); }
        private static void EventAt(EventType type,float x=-1,float y=-1) => Event.current=new Event{type=type,mousePosition=new Vector2(x,y)};
        private static void Paint(Win05 history,float scale) { ChatUI.Drawn.Clear();BitmapFont.Text.Clear();history.Paint(scale); }
        public static void Main()
        {
            var history=new Win05();
            string[] expected={"系统","GM","队伍","广播","家族","私聊","附近","世界","公告"};
            Check(Win05.Labels.SequenceEqual(expected),"actual tab table changed or retained vote");
            Check(Win05.Selected==6,"actual default is not Nearby after removing vote");
            foreach(string channel in expected.Take(8)) ChatUI.Lines.Add((channel,channel+" message",Color.white,"sender",null));
            ChatUI.Lines.Add(("帮派","帮派 alias",Color.white,"sender",null));
            ChatUI.Lines.Add(("联盟","联盟 alias",Color.white,"sender",null));
            ChatUI.Lines.Add(("","system blank",Color.white,"sender",null));
            ChatUI.Ver++;
            foreach(float scale in new[]{.75f,1f,2f})
            {
                for(int i=0;i<expected.Length;i++)
                {
                    EventAt(EventType.MouseDown,(17+i*50+10)*scale,50*scale);
                    int before=Win05.NoticeCalls;Paint(history,scale);
                    Check(Event.current.type==EventType.Used && Win05.Selected==i,"actual history tab click not consumed/selected");
                    if(i==8) Check(Win05.NoticeCalls==before+1 && ChatUI.Drawn.Count==0,"notice did not use native special branch");
                    else
                    {
                        string[] want=i==0?new[]{"系统 message","system blank"}:i==4?new[]{"家族 message","帮派 alias","联盟 alias"}:new[]{expected[i]+" message"};
                        Check(ChatUI.Drawn.SequenceEqual(want),"actual MatchTab/cache displayed wrong channel: "+expected[i]);
                        Check(Win05.NoticeCalls==before,"ordinary channel invoked notice");
                    }
                    EventAt(EventType.Repaint);Paint(history,scale);
                    Check(Win05.Selected==i,"repaint changed active channel");
                }
            }
            EventAt(EventType.MouseDown,77,50);Paint(history,1);
            ChatUI.Lines.Add(("GM","new GM message",Color.white,"sender",null));ChatUI.Ver++;
            EventAt(EventType.Repaint);Paint(history,1);
            Check(ChatUI.Drawn.SequenceEqual(new[]{"GM message","new GM message"}),"actual GM history cache did not refresh");
            ChatUI.Lines.Clear();ChatUI.Ver++;Paint(history,1);
            Check(ChatUI.Drawn.Count==0 && BitmapFont.Text.Contains("【GM】暂无聊天记录"),"empty GM tab did not retain original history surface");
            int[] maps={7,19,32,58,86,154,178,247};
            string[] names={"城主成隍","执事官徐福","玉皇大帝","监城官哈亚图","无泪城主绝聆","城主六翼羽宸","上古城主海煌","猫猫城主大王"};
            for(int i=0;i<maps.Length;i++)
            {
                GameBootstrap.CurMapId=maps[i];FamilyCreateDlg.Visible=false;
                var dialog=new DialogUI{NpcName=names[i],Visible=true,InSubView=true,CurrentSellList=123};
                TaskClient.OfferNpcName=names[i];TaskClient.Offers=new List<TaskClient.Offer>{new TaskClient.Offer{id=1,title="original task"}};
                dialog.Build();
                Check(!dialog.InSubView && dialog.Labels.SequenceEqual(new[]{"购买物品","出售物品","创建家族","任务：original task","离开"}),"real BuildMenu lost original service/task/leave or leader route");
                dialog.Build();Check(dialog.Labels.Count(x=>x=="创建家族")==1,"rebuilding duplicates family row");
                Check(dialog.Click(0) && !dialog.Visible && ShopDlg.Last==123 && !ShopDlg.Selling,"existing merchant callback broken");
                dialog.Visible=true;int toggles=FamilyCreateDlg.Toggles;
                Check(dialog.Click(2) && !dialog.Visible && FamilyCreateDlg.Visible,"actual bound option dispatch did not open form/hide NPC");
                dialog.Click(2);Check(FamilyCreateDlg.Visible && FamilyCreateDlg.Toggles==toggles+1,"repeat activation toggles form closed");
                FamilyCreateDlg.Visible=false;dialog.Visible=true;
                Check(dialog.Click(4) && !dialog.Visible && !FamilyCreateDlg.Visible,"actual leave dispatch opened family form");
                Check(!dialog.Click(-1) && !dialog.Click(99),"out-of-range native option admitted");
                GameBootstrap.CurMapId=maps[i]+1000;dialog.Build();Check(!dialog.Labels.Contains("创建家族"),"same-name actor on wrong map admitted by real menu");
                GameBootstrap.CurMapId=maps[i];dialog.NpcName=names[i]+"幻影";dialog.Build();Check(!dialog.Labels.Contains("创建家族"),"substring actor admitted by real menu");
                dialog.NpcName=names[i];dialog.Build();Action stale=dialog.Captured(2);GameBootstrap.CurMapId=maps[i]+1000;stale();
                Check(!FamilyCreateDlg.Visible,"captured native action opened after map change");
                GameBootstrap.CurMapId=maps[i];dialog.NpcName="普通居民";stale();Check(!FamilyCreateDlg.Visible,"captured native action opened after NPC change");
            }
            var travel=new DialogUI{NpcName="传送仙子"};travel.Build();
            Check(travel.Labels.SequenceEqual(new[]{"随机传送","桃源","长乐"}),"real travel-menu early return changed");
            Console.WriteLine("PASS "+checks+" actual DrawHistory/BuildMenu/option-dispatch assertions (boundary stubs; no Unity execution)");
        }
    }
}
