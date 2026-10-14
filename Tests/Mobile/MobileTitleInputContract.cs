using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class MobileTitleInputContract
{
    static int checks;
    static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    static void Main()
    {
        var input = new MobileTitleTextInput();
        var field = new Rect(100, 400, 140, 30);
        var style = new GUIStyle();
        input.BeginPage(0);
        GUI.click = true;
        string value = input.Draw("password", field, "fictional", 40, true, style);
        var first = TouchScreenKeyboard.last;
        input.FocusChanged(false, true);
        Check(input.Editing && first.active, "Android keyboard focus loss must not dismiss its own keyboard");
        input.FocusChanged(true, true);
        Check(input.Editing, "Focus restoration never reopens or dismisses editing");
        Check(first.secure && !first.autocorrection && !first.multiline, "Secure keyboard options");
        Check(first.type == TouchScreenKeyboardType.Default, "IME-capable keyboard");
        Check(GUI.lastDisplay == new string('\u25cf', 9), "Password display is masked");
        Check(input.Editing && TouchScreenKeyboard.opens == 1, "Opened once");
        first.text = "fic-edited";
        value = input.Draw("password", field, value, 40, true, style);
        Check(value == "fic-edited", "Actual native text replaces value instead of mask reconstruction");
        input.Draw("password", field, value, 40, true, style);
        Check(TouchScreenKeyboard.opens == 1, "Repaint never reopens keyboard");
        GUI.click = true;
        input.Draw("password", field, value, 40, true, style);
        Check(TouchScreenKeyboard.opens == 1, "Repeated focused tap never reopens keyboard");
        first.status = TouchScreenKeyboard.Status.Canceled;
        value = input.Draw("password", field, value, 40, true, style);
        Check(value == "fictional" && !input.Editing && !first.active, "Cancel restores entry snapshot and closes");
        GUI.click = true;
        value = input.Draw("name", field, "江湖", 30, false, style);
        var name = TouchScreenKeyboard.last;
        name.text = "江湖侠客";
        name.status = TouchScreenKeyboard.Status.Done;
        value = input.Draw("name", field, value, 30, false, style);
        Check(value == "江湖侠客" && !input.Editing, "IME committed full Chinese name accepted on Done");
        GUI.click = true;
        input.Draw("name", field, value, 30, false, style);
        var outside = TouchScreenKeyboard.last;
        outside.text = "另一个名字";
        Event.current = new Event { type = EventType.MouseUp, mousePosition = new Vector2(0,0) };
        value = input.Draw("name", field, value, 30, false, style);
        Check(value == "另一个名字" && !outside.active, "Outside release keeps latest edit and dismisses");
        Event.current = null;
        GUI.click = true;
        input.Draw("name", field, value, 30, false, style);
        var switching = TouchScreenKeyboard.last;
        GUI.click = true;
        input.Draw("other", field, "", 20, false, style);
        Check(!switching.active && input.Editing, "Switching field closes previous keyboard");
        var paged = TouchScreenKeyboard.last;
        input.BeginPage(1);
        Check(!paged.active && !input.Editing, "Changing page releases keyboard");
        GUI.click = true;
        input.Draw("name", field, value, 30, false, style);
        var lost = TouchScreenKeyboard.last;
        lost.text = "失焦提交";
        lost.status = TouchScreenKeyboard.Status.LostFocus;
        value = input.Draw("name", field, value, 30, false, style);
        Check(value == "失焦提交" && !lost.active, "Native LostFocus preserves latest text and releases keyboard");
        GUI.click = true;
        input.Draw("name", field, value, 30, false, style);
        var pressed = TouchScreenKeyboard.last;
        Event.current = new Event { type = EventType.MouseDown, mousePosition = new Vector2(0,0) };
        input.Draw("name", field, value, 30, false, style);
        Check(pressed.active, "Outside press does not move layout before button release");
        Event.current = null;
        input.FocusChanged(false, false);
        Check(!pressed.active && !input.Editing, "Non-Android app focus loss closes keyboard");
        input.Close(); input.Close();
        Check(!input.Editing, "Repeated dismissal safe");
        Check(MobileTitleTextInput.LimitText("中文名",2)=="中文", "Existing UTF16 limit retained");
        Check(MobileTitleTextInput.LimitText("a\U0001F600b",2)=="a", "Truncation does not split surrogate");
        Screen.height = 900;
        var safe = new Rect(30, 20, 760, 850);
        var visible = MobileTitleTextInput.VisibleArea(safe,new Rect(0,0,800,350),true);
        Check(visible.yMax == 550 && visible.x == 30 && visible.width == 760, "Keyboard intersection respects safe area");
        float pan = MobileTitleTextInput.PanY(20, 1.2f, field, visible);
        Check(pan+field.yMax*1.2f <= visible.yMax-12 && pan+field.y*1.2f >= visible.y+8, "Focused field above keyboard within safe top");
        Check(MobileTitleTextInput.VisibleArea(safe,new Rect(),true).height < safe.height, "Zero-area Android fallback reserves editing area");
        Check(MobileTitleTextInput.VisibleArea(safe,new Rect(),false).height == safe.height, "Closed keyboard keeps layout");
        TouchScreenKeyboard.isSupported = false;
        GUI.passwordCalls = 0;
        input.Draw("password", field,"fictional",40,true,style);
        Check(GUI.passwordCalls == 1, "Desktop simulation uses PasswordField without native keyboard");
        Console.WriteLine("PASS mobile title input: " + checks + " assertions; actual adapter with deterministic Unity substitutes, no device IME run.");
    }
}
namespace UnityEngine
{
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Rect {
        public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public float yMax => y+height;
        public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<yMax;
    }
    public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); }
    public static class Screen { public static int height; }
    public enum TouchScreenKeyboardType { Default }
    public class TouchScreenKeyboard {
        public enum Status { Visible, Done, Canceled, LostFocus }
        public static bool isSupported = true; public static TouchScreenKeyboard last; public static int opens;
        public string text; public Status status=Status.Visible; public bool active=true,secure,autocorrection,multiline; public TouchScreenKeyboardType type;
        public static TouchScreenKeyboard Open(string value,TouchScreenKeyboardType type,bool correction,bool multiline,bool secure,bool alert,string placeholder,int limit) {
            opens++; return last = new TouchScreenKeyboard{text=value,type=type,autocorrection=correction,multiline=multiline,secure=secure};
        }
    }
    public enum EventType { MouseDown, MouseUp }
    public class Event { public static Event current; public EventType type; public Vector2 mousePosition; }
    public class GUIStyle {}
    public static class GUI {
        public static bool click; public static string lastDisplay; public static int passwordCalls;
        public static bool Button(Rect r,string s,GUIStyle style){lastDisplay=s;bool result=click;click=false;return result;}
        public static string PasswordField(Rect r,string s,char c,int max,GUIStyle style){passwordCalls++;return s;}
        public static string TextField(Rect r,string s,int max,GUIStyle style)=>s;
        public static void FocusControl(string s){}
    }
}
