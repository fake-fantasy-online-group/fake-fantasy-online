using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine
{
    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 operator -(Vector2 a, Vector2 b) => new Vector2(a.x - b.x, a.y - b.y);
    }
    public struct Rect
    {
        public float x, y, width, height;
        public Rect(float x, float y, float w, float h) { this.x = x; this.y = y; width = w; height = h; }
        public bool Contains(Vector2 p) => p.x >= x && p.x < x + width && p.y >= y && p.y < y + height;
        public float xMax => x + width; public float yMax => y + height;
        public Vector2 center => new Vector2(x+width/2,y+height/2);
        public static bool operator ==(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height;
        public static bool operator !=(Rect a,Rect b)=>!(a==b);
        public override bool Equals(object other)=>other is Rect r&&this==r;
        public override int GetHashCode()=>x.GetHashCode()^y.GetHashCode()^width.GetHashCode()^height.GetHashCode();
    }
    public struct Color
    {
        public Color(float r, float g, float b, float a = 1) { }
        public static Color white => new Color();
    }
    public class Texture2D { public int ID; public float width = 30, height = 30; public static Texture2D whiteTexture = new Texture2D(); }
    public class Sprite { public Texture2D texture; public Rect textureRect; }
    public enum EventType { MouseDown, MouseDrag, MouseUp, KeyDown, Repaint, Used }
    public enum KeyCode { Escape }
    [Flags] public enum EventModifiers { None=0,Control=1,Command=2,Shift=4 }
    public enum TouchPhase { Began,Moved,Stationary,Ended,Canceled }
    public struct Touch { public int fingerId;public Vector2 position;public TouchPhase phase; }
    public static class Input { public static Touch[] touches=new Touch[0];public static int touchCount=>touches.Length;public static Touch GetTouch(int i)=>touches[i]; }
    public static class Time { public static int frameCount;public static float unscaledTime; }
    public static class Application { public static bool isMobilePlatform; }
    public class Event
    {
        public static Event current;
        public EventType type; public int button; public KeyCode keyCode; public Vector2 mousePosition;public EventModifiers modifiers;
        public void Use() { type = EventType.Used; }
    }
    public static class Screen { public static int width = 1024, height = 768; }
    public static class Mathf
    {
        public static int Clamp(int n, int min, int max) => Math.Min(Math.Max(n, min), max);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static float Max(float a,float b)=>Math.Max(a,b);
        public static float Clamp(float n,float min,float max)=>Math.Min(Math.Max(n,min),max);
    }
    public class GUIContent { public static GUIContent none = new GUIContent(); }
    public class GUIStyle { public static GUIStyle none = new GUIStyle(); }
    public static class GUI
    {
        public static int depth; public static Color color;public static bool enabled=true;
        private static Rect pressed;private static bool pressing;
        public static void ResetButton(){pressing=false;}
        public static void DrawTexture(Rect r, Texture2D t) { }
        public static void DrawTextureWithTexCoords(Rect r, Texture2D t, Rect uv) { }
        public static bool Button(Rect r, GUIContent c, GUIStyle s)
        {
            Event ev=Event.current;
            if(!enabled||ev.button!=0)return false;
            if(ev.type==EventType.MouseDown&&r.Contains(ev.mousePosition)){pressed=r;pressing=true;ev.Use();return false;}
            if(ev.type==EventType.MouseUp&&pressing&&r==pressed)
            {pressing=false;bool hit=r.Contains(ev.mousePosition);if(hit)ev.Use();return hit;}
            return false;
        }
        public static void Label(Rect r, string text) { }
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal partial class EquipDlg
    {
        public struct EquippedItem { public int slot, id, quality, avatar; public string name, desc; public Texture2D icon; }
        public static readonly List<EquippedItem> equipped = new List<EquippedItem>();
        private bool open = true, loaded = true, placed = true, titleOpen, taskOpen, dragging;
        private int tab, dollVer, dollTry;
        private float s, winX, winY;
        private Vector2 dragOff;
        private PlayerAvatar doll;
        private Texture2D bk = new Texture2D(), attrPage;
        private Texture2D[] checkbox, titleBtn = new Texture2D[2], closeBtn = new Texture2D[2], helpBtn = new Texture2D[2];
        private int[] pend = new int[7];
        private static string[] RaceNames = { "", "", "", "", "" };
        public bool AdditionalOpen => additionalEquipmentOpen;
        public bool Open { get => open; set => open = value; }
        public int Tab { get => tab; set => tab = value; }
        public float X { get=>winX;set => winX = value; }
        public float Y { get=>winY;set => winY = value; }
        public float Scale=>s;public int MobileSlot=>mobileEquipmentSlot;public bool MobileActions=>mobileEquipmentActions;public bool MobileBinding=>mobileEquipmentBinding;
        public Rect ActionRect=>mobileEquipmentActionsRect;
        public void Frame(float x, float y, int button)
        {
            Frame(new Vector2(x,y),button<0?EventType.Repaint:EventType.MouseDown,0.02f,button<0?0:button);
        }
        public void Frame(Vector2 point,EventType type,float elapsed=.02f,int button=0)
        {
            Time.frameCount++;Time.unscaledTime+=elapsed;ItemIconUI.Drawn.Clear();ArtworkSkin.Buttons.Clear();
            Event.current=new Event {type=type,button=button,mousePosition=point};OnGUI();
        }
        private void Load() { }
        private bool Btn(Rect r, Texture2D[] frames, Event ev)
        {
            if (ev.type != EventType.MouseDown || ev.button != 0 || !r.Contains(ev.mousePosition)) return false;
            ev.Use(); return true;
        }
        private bool VTab(Rect r, string label, bool selected, bool blue) => false;
        private void Patch9(Rect r) { }
        private void Border(Rect r, Color c) { }
        private void DrawPoints(GuiFrame f, Event ev) { }
        private void DrawTitles(GuiFrame f, Event ev) { }
        private int At(int i) => 0;
    }
    internal static class UIScale { public static float Factor = 1; }
    internal static class WinZ { public static (int, bool) Frame(object owner, Rect rect, int z) => (z, false); }
    internal static class BitmapFont
    {
        public static bool Ready = true;
        public static void CenterOutline(Rect r, string text, Color c, float s) { }
        public static void DrawOutline(float x, float y, string text, Color c, float s) { }
        public static void Draw(float x, float y, string text, Color c, float s) { }
    }
    internal static class Win05
    {
        public const string ChangeEquipmentDialogName = "", MagicWardrobeDialogName = "";
        public static void Toggle(string s) { }
        public static bool IsShowing(string s) => false;
    }
    internal static class CharData
    {
        public static int race, level, atkDist, weaponCType, apVersion, gender, head, hair;
        public static string name, title;
        public static int[] attrsTotal, attrs;
        public static Dictionary<int, int> skills;
        public static PlayerAvatar.Appear CombinedAppear() => new PlayerAvatar.Appear();
        public static bool EquipShown(int p, int k) => false;
        public static void SetEquipShown(int p, int k, bool show) { }
        public static void SaveEquipFx() { }
    }
    internal class PlayerAvatar
    {
        public struct Appear { }
        public bool ok;
        public void Dispose() { }
        public Sprite Still(int f) => null;
        public static PlayerAvatar Build(int g, int h, int hair, Appear a, float scale) => null;
    }
    internal class PlayerController { public static PlayerController I; public PlayerAvatar Doll;public static int Suppressions; public static void SuppressWorldClick(){Suppressions++;} public void RefreshSoulFx() { } }
    internal static class ItemIconUI { public static List<int> Drawn = new List<int>(); public static void Draw(Rect r, Texture2D t, float s) { Drawn.Add(t.ID); } }
    internal static class MobileClientPresentation
    {
        public static bool Enabled;public static float Scale=1;public static Rect SafeAreaGui=new Rect(0,0,1024,768);
        public static object Owner;public static Rect Registered;
        public static void RegisterPanel(object owner,Rect r){Owner=owner;Registered=r;}
        public static void RemovePanel(object o){if(Owner==o)Owner=null;}
    }
    internal static class ArtworkSkin
    {
        public static List<(Rect,string,bool)> Buttons=new List<(Rect,string,bool)>();
        public static void ButtonFace(Rect r,string label,float scale,bool enabled,bool hovered,bool pressed){Buttons.Add((r,label,enabled));}
    }
    internal static class UITip
    {
        public static bool MobileContainsPointer(Vector2 p) => false;
        public static string Last;
        public static Color QualityColor(int q) => Color.white;
        public static void Show(string text, float s) { Last = text; }
    }
    internal static class AttrWashDlg
    {
        public static bool Picking, Over; public static int Target;
        public static bool MouseOver() => Over;
        public static void SetTarget(int kind, int tab, int slot, string name, Texture2D icon) { Target = slot; }
        public static void Open() { }
    }
    internal static class EndgamePanel { public static string EquippedTooltip(EquipDlg.EquippedItem i, string original) => original; }
    internal static class TooltipContent { public static string QualityName(string n, int q) => n; public static string Fmt(string n, string d) => n + "\n" + d; }
    internal static class AvatarFuse
    {
        public static bool Active;
        public static void PickTarget(int slot, string name, bool avatar) { }
        public static void Cancel() { }
    }
    internal static class ToolTarget
    {
        public static bool Active; public static int Target; public static string Verb = "使用工具";
        public static void PickEquip(int slot, string name, bool avatar) { Target = slot; }
        public static void Cancel() { }
    }
    internal static class RepairDlg { public static bool Picking; public static int Target; public static void PickSlot(int slot) { Target = slot; } public static void CancelPick() { } }
    internal static class InventoryDlg
    {
        public static bool Sharing, IsOpen; public static int SharedSlot;
        public static void Toggle() { }
        public static bool ShareModHeld(Event e) => Sharing||(e.modifiers&(EventModifiers.Control|EventModifiers.Command))!=0;
        public static void ShareToChat(int kind, int tab, int slot, string name) { SharedSlot = slot; }
    }
    internal static class GameHud
    {
        public static int Pickups,Bindings,BoundSlot=-1,BoundSkill;
        public static void PickupSkill(int skill,Texture2D icon,bool b){Pickups++;}
        public static bool BindMobileBookSkill(int slot,int skill,Texture2D icon){Bindings++;BoundSlot=slot;BoundSkill=skill;return true;}
    }
    internal class NetClient
    {
        public static NetClient I; public int LastUnequipped,UnequipCount;
        public void UnequipItemForIndependentRuntime(int slot) { LastUnequipped = slot;UnequipCount++; }
        public void SendEquipFx() { }
        public static void RefreshTransmogFx() { }
    }
}
