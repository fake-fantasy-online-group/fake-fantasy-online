// Executes production menu dispatch, visibility and button handlers against explicit Unity doubles.
// Does not claim Unity event ordering, rendering, or device validation.
using System;
using System.Reflection;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
static class Contract
{
    static int checks;
    static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    static void Click(float x, float y)
    {
        Event.current = new Event { type = EventType.MouseDown, button = 0, mousePosition = new Vector2(304 + x,196.5f + y) };
        typeof(SysOptionDlg).GetMethod("OnGUI", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(SysOptionDlg.I, null);
    }
    static void Main()
    {
        GameHud.ActivateControlButtonForIndependentRuntime(0);
        Check(SysOptionDlg.Visible && !SysConfigDlg.Visible, "shared PC/mobile settings opens menu");
        GameHud.ActivateControlButtonForIndependentRuntime(0);
        Check(!SysOptionDlg.Visible, "repeated entry closes menu");
        Input.Pressed = true;
        UIManager.UpdateSettingsForIndependentRuntime(new ReferenceFirstWorldChatUi(), null);
        Check(SysOptionDlg.Visible && !SysConfigDlg.Visible, "O opens menu");
        Input.Pressed = false;
        Click(50,140);
        Check(SysConfigDlg.Visible && !SysOptionDlg.Visible, "existing system settings button opens details");
        GameHud.ActivateControlButtonForIndependentRuntime(0);
        Check(SysOptionDlg.Visible && !SysConfigDlg.Visible, "settings entry from details replaces details");
        Click(50,40);
        Check(!SysOptionDlg.Visible && !SysConfigDlg.Visible, "return resumes without hidden detail overlay");
        for(int i=0;i<3;i++) { SysOptionDlg.Open(); Click(50,140); SysConfigDlg.Toggle(); Check(!SysConfigDlg.Visible && !SysOptionDlg.Visible,"detail escape close cycle"); }
        SysOptionDlg.Open(); SysMsgBox.Visible = true; Click(50,180);
        Check(GameBootstrap.QuitCount == 0 && SysOptionDlg.Visible,"confirmation blocks menu input");
        SysMsgBox.Visible = false; Click(50,180); Check(GameBootstrap.QuitCount == 1,"exit retains existing owner");
        SysOptionDlg.Open(); Click(50,70); Check(GameBootstrap.I.LastCharacter == true && !SysOptionDlg.Visible,"character selection retains existing owner");
        SysOptionDlg.Open(); Click(50,105); Check(GameBootstrap.I.LastCharacter == false && !SysOptionDlg.Visible,"server selection retains existing owner");
        PlayerController.I = new PlayerController { IsDead = true }; SysOptionDlg.Open(); Click(50,40);
        Check(NetClient.I.ReviveCount == 1 && !SysOptionDlg.Visible,"death menu revive preserved");
        MobileClientPresentation.Enabled = true; UIScale.Factor = 4;
        MobileClientPresentation.SafeAreaGui = new Rect(30,20,300,180); SysOptionDlg.Open();
        object[] args = { 0f };
        Rect fit = (Rect)typeof(SysOptionDlg).GetMethod("MobileLayout", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(SysOptionDlg.I,args);
        Check(fit.x >= 30 && fit.y >= 20 && fit.xMax <= 330 && fit.yMax <= 200,"menu fits narrow safe area at maximum scale");
        Check(SysOptionDlg.MobileContainsPointer(new Vector2(fit.x+1,fit.y+1)),"mobile hits use fitted menu");
        Check(!SysOptionDlg.MobileContainsPointer(new Vector2(0,0)),"outside safe area is not menu");
        Check(UIScale.Factor == 4,"fit does not change saved UI scale");
        Console.WriteLine("PASS " + checks + " settings-menu production assertions (Unity doubles)");
    }
}
namespace UnityEngine
{
    public class MonoBehaviour { protected static void DontDestroyOnLoad(object x) {} }
    public class GameObject { public GameObject(string n) {} public T AddComponent<T>() where T:new() => new T(); }
    public class Texture2D { public static Texture2D whiteTexture = new Texture2D(); }
    public struct Vector2 { public float x,y; public Vector2(float x,float y) { this.x=x;this.y=y; } }
    public struct Rect { public float x,y,width,height; public Rect(float x,float y,float w,float h) {this.x=x;this.y=y;width=w;height=h;} public float xMax=>x+width; public float yMax=>y+height; public bool Contains(Vector2 p)=>p.x>=x&&p.y>=y&&p.x<x+width&&p.y<y+height; }
    public struct Color { public Color(float r,float g,float b,float a=1) {} public static Color white=>new Color(); }
    public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Clamp(float x,float a,float b)=>Math.Max(a,Math.Min(x,b)); }
    public static class Screen { public static int width=800,height=600; }
    public enum KeyCode { O }
    public static class Input { public static bool Pressed; public static Vector2 mousePosition; public static bool GetKeyDown(KeyCode k)=>Pressed; }
    public enum EventType { MouseDown, Used }
    public class Event { public static Event current = new Event(); public EventType type; public int button; public Vector2 mousePosition; public void Use() {type=EventType.Used;} }
    public static class GUI { public static int depth; public static Color color; public static void DrawTexture(Rect r,Texture2D t) {} }
    public static class GUIUtility { public static int keyboardControl; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class GameHud { static readonly (string,float,string,string)[] CtrlBtns = { ("optionbtn",0,"O","设置") }; }
    public partial class UIManager { public static UIManager I; public void Toggle(string s) {} }
    public class ReferenceFirstWorldChatUi { public bool TypingNow; }
    public class ReferenceFirstWorldGossipDialog { public bool Typing; }
    public static class EquipDlg { public static void Toggle() {} }
    public static class InventoryDlg { public static void Toggle() {} }
    public static class Win05 { public const string SkillDialogName="skilldlg"; public static void Toggle(string n) {} }
    public static class TeamDlg { public static void Toggle() {} }
    public static class SocialClient { public static string FamilyName; }
    public static class FamilyDlg { public static void Toggle() {} }
    public static class FamilyDetailDlg { public static void Toggle() {} }
    public static class TaskDlg { public static void Toggle() {} }
    public static class MailDlg { public static bool Typing; public static void Toggle() {} }
    public static class RackShopDlg { public static void Toggle() {} }
    public static class MobileClientPresentation { public static bool Enabled; public static Rect SafeAreaGui=new Rect(0,0,800,600); }
    public static class UIScale { public static float Factor=1; }
    public static class WinDrag { public static int Gen; public static bool Stale(float x,ref int g) { if(!float.IsNaN(x)&&g==Gen)return false;g=Gen;return true;} public static void Handle(object o,Rect r,ref float x,ref float y) {} }
    public class ResLoader { public static ResLoader I; public Texture2D LoadGsa(string s)=>null; public Texture2D LoadGsaFrame(string s,int i)=>null; }
    public static class ChatUI { public static void Add(string c,string t,Color col,string w,object list) {} }
    public static class BitmapFont { public static bool Ready; public static void CenterOutline(Rect r,string t,Color c,float s) {} }
    public class PlayerController { public static PlayerController I; public bool IsDead; }
    public static class SysMsgBox { public static bool Visible; }
    public class GameBootstrap { public static GameBootstrap I=new GameBootstrap(); public static int QuitCount; public bool LastCharacter; public void ReturnToTitle(bool c) {LastCharacter=c;} public static void QuitGame() {QuitCount++;} }
    public class NetClient { public static NetClient I=new NetClient(); public int ReviveCount; public void ReviveForIndependentRuntime(int i) {ReviveCount++;} }
}
