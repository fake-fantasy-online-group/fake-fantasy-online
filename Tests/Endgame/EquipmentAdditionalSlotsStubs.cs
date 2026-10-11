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
    public class Event
    {
        public static Event current;
        public EventType type; public int button; public KeyCode keyCode; public Vector2 mousePosition;
        public void Use() { type = EventType.Used; }
    }
    public static class Screen { public static int width = 1024, height = 768; }
    public static class Mathf
    {
        public static int Clamp(int n, int min, int max) => Math.Min(Math.Max(n, min), max);
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
    }
    public class GUIContent { public static GUIContent none = new GUIContent(); }
    public class GUIStyle { public static GUIStyle none = new GUIStyle(); }
    public static class GUI
    {
        public static int depth; public static Color color;
        public static void DrawTexture(Rect r, Texture2D t) { }
        public static void DrawTextureWithTexCoords(Rect r, Texture2D t, Rect uv) { }
        public static bool Button(Rect r, GUIContent c, GUIStyle s) => false;
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
        public float X { set => winX = value; }
        public float Y { set => winY = value; }
        public void Frame(float x, float y, int button)
        {
            ItemIconUI.Drawn.Clear();
            Event.current = new Event { type = button < 0 ? EventType.Repaint : EventType.MouseDown, button = button, mousePosition = new Vector2(x, y) };
            OnGUI();
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
    internal class PlayerController { public static PlayerController I; public PlayerAvatar Doll; public void RefreshSoulFx() { } }
    internal static class ItemIconUI { public static List<int> Drawn = new List<int>(); public static void Draw(Rect r, Texture2D t, float s) { Drawn.Add(t.ID); } }
    internal static class UITip
    {
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
        public static bool ShareModHeld(Event e) => Sharing;
        public static void ShareToChat(int kind, int tab, int slot, string name) { SharedSlot = slot; }
    }
    internal static class GameHud { public static void PickupSkill(int skill, Texture2D icon, bool b) { } }
    internal class NetClient
    {
        public static NetClient I; public int LastUnequipped;
        public void UnequipItemForIndependentRuntime(int slot) { LastUnequipped = slot; }
        public void SendEquipFx() { }
        public static void RefreshTransmogFx() { }
    }
}
