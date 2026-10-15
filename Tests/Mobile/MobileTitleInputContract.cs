using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class MobileTitleInputContract
{
    static int checks;
    static void Check(bool ok, string label) { checks++; if (!ok) throw new Exception(label); }
    static bool Near(float a, float b) => Math.Abs(a-b) < .01f;
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
        float pan = MobileTitleTextInput.PanY(20, 1.2f, field, safe, new Rect(0,0,800,350));
        Check(pan+field.yMax*1.2f <= visible.yMax-12 && pan+field.y*1.2f >= visible.y+8, "Focused field above keyboard within safe top");
        float unknownPan = MobileTitleTextInput.PanY(20,1.2f,field,safe,new Rect());
        Check(Near(unknownPan+field.y*1.2f,safe.y+8), "Zero-area Android keyboard docks focused row at safe top");
        Check(MobileTitleTextInput.VisibleArea(safe,new Rect(),true).height == safe.height, "Unknown keyboard bounds are never guessed from screen percentage");
        Check(MobileTitleTextInput.VisibleArea(safe,new Rect(),false).height == safe.height, "Closed keyboard keeps layout");
        TestTitleFrame();
        TouchScreenKeyboard.isSupported = false;
        GUI.passwordCalls = 0;
        input.Draw("password", field,"fictional",40,true,style);
        Check(GUI.passwordCalls == 1, "Desktop simulation uses PasswordField without native keyboard");
        Console.WriteLine("PASS mobile title input: " + checks + " assertions; actual adapter with deterministic Unity substitutes, no device IME run.");
    }

    static void TestTitleFrame()
    {
        var title = new TitleFlow();
        MobileClientPresentation.Enabled = true;
        foreach (var size in new[] { new[] {1280,720}, new[] {812,375}, new[] {2340,1080}, new[] {1920,1200} })
        {
            Screen.width = size[0]; Screen.height = size[1];
            Rect safe = new Rect(24,16,size[0]-48,size[1]-32);
            MobileClientPresentation.SafeAreaGui = safe;
            float scale = Math.Min(safe.width/800f,safe.height/600f);
            float centeredY = safe.y+(safe.height-scale*600f)*.5f;
            TouchScreenKeyboard.area = new Rect();
            title.TestInput.Close();
            title.BeginGuiFrameForIndependentRuntime(false);
            Check(Near(GUI.matrix.y,centeredY) && Near(GUI.matrix.sy,scale), "Closed title uses original mobile scale and centering");
            foreach (Rect row in new[] {new Rect(327,392,134,22),new Rect(327,429,134,22),new Rect(306,68,135,22)})
            {
                GUI.click = true;
                title.TestField(row);
                Rect focused = title.TestInput.FocusRect;
                Check(Near(focused.y,row.y-4) && Near(focused.height,row.height+8), "Actual mobile field expansion retained");
                int opens = TouchScreenKeyboard.opens;
                Matrix4x4 previous = Matrix4x4.TRS(new Vector3(7,9,0),Quaternion.identity,new Vector3(2,2,1));
                GUI.matrix = previous;
                Matrix4x4 returned = title.BeginGuiFrameForIndependentRuntime(false);
                Check(Near(returned.x,7) && Near(returned.y,9) && Near(returned.sy,2), "Title frame returns caller matrix");
                Check(Near(GUI.matrix.sy,scale) && Near(GUI.matrix.x,safe.x+(safe.width-scale*800)*.5f), "Keyboard does not rescale or shift title horizontally");
                Check(Near(GUI.matrix.y+focused.y*scale,safe.y+8), "Actual title frame docks zero-area keyboard input above landscape IME");
                foreach (float covered in new[] {.45f,.65f,.85f})
                    Check(GUI.matrix.y+focused.yMax*scale < safe.y+safe.height*(1-covered), "Focused row clears tall zero-area landscape keyboard");
                for (int repaint=0;repaint<3;repaint++) title.BeginGuiFrameForIndependentRuntime(false);
                Check(TouchScreenKeyboard.opens == opens, "Layout and repaint do not reopen native IME");
                TouchScreenKeyboard.area = new Rect(0,0,Screen.width,Screen.height*.65f);
                title.BeginGuiFrameForIndependentRuntime(false);
                Check(GUI.matrix.y+focused.yMax*scale <= Screen.height-TouchScreenKeyboard.area.height-12+.01f, "Actual frame respects measured keyboard top");
                TouchScreenKeyboard.last.status = TouchScreenKeyboard.Status.Done;
                title.BeginGuiFrameForIndependentRuntime(false);
                Check(Near(GUI.matrix.y,centeredY), "Done restores original centered title before next draw");
                title.TestField(row);
                Check(!title.TestInput.Editing, "Done releases keyboard after title field draw");
                TouchScreenKeyboard.area = new Rect();
            }
            GUI.click = true; title.TestField(new Rect(327,429,134,22));
            TouchScreenKeyboard.last.status = TouchScreenKeyboard.Status.Canceled;
            title.TestField(new Rect(327,429,134,22));
            title.BeginGuiFrameForIndependentRuntime(false);
            Check(Near(GUI.matrix.y,centeredY), "Cancel restores centered title");
            GUI.click = true; title.TestField(new Rect(327,392,134,22));
            title.TestPause();
            title.BeginGuiFrameForIndependentRuntime(false);
            Check(!title.TestInput.Editing && Near(GUI.matrix.y,centeredY), "App pause releases keyboard and panning");
            GUI.click = true; title.TestField(new Rect(327,429,134,22));
            title.TestDisable();
            title.BeginGuiFrameForIndependentRuntime(false);
            Check(!title.TestInput.Editing && Near(GUI.matrix.y,centeredY), "Title disable releases keyboard and panning");
        }
        // Local API 31 emulator regression: 2340x1080 landscape with IME top at y=493.
        // The old 55%-height guess left the input bottom at y=582, under this keyboard.
        Screen.width=2340; Screen.height=1080;
        MobileClientPresentation.SafeAreaGui=new Rect(0,0,2340,1080);
        Check(1080*.55f-12 > 493, "Former percentage fallback reproduces recorded landscape occlusion");
        foreach (float y in new[] {392f,429f})
        {
            GUI.click=true; title.TestField(new Rect(327,y,134,22));
            title.BeginGuiFrameForIndependentRuntime(false);
            Rect focused=title.TestInput.FocusRect;
            Check(Near(GUI.matrix.y+focused.y*GUI.matrix.sy,8) &&
                GUI.matrix.y+focused.yMax*GUI.matrix.sy < 493, "Account/password clear recorded y493 IME without reported bounds");
            title.TestInput.Close();
        }
        Screen.width=1280; Screen.height=720;
        MobileClientPresentation.Enabled=false;
        title.BeginGuiFrameForIndependentRuntime(false);
        Check(Near(GUI.matrix.x,160) && Near(GUI.matrix.y,0) && Near(GUI.matrix.sx,1.2f) && Near(GUI.matrix.sy,1.2f), "Desktop original 800x600 transform unchanged");
        title.TestBackground();
        title.BeginGuiFrameForIndependentRuntime(false);
        Check(Near(GUI.backgroundDrawMatrix.x,0) && Near(GUI.backgroundDrawMatrix.y,0) && Near(GUI.backgroundDrawMatrix.sx,1), "Background still draws in screen coordinates");
        Check(Near(GUI.matrix.x,160) && Near(GUI.matrix.sy,1.2f), "Title transform restored after background");
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal sealed partial class TitleFlow
    {
        Texture2D selBg, cbg, bg, loginBg;
        internal MobileTitleTextInput TestInput => mobileTitleInput;
        internal string TestField(Rect rect) => MobileField("test-"+rect.y,rect,"fictional",40,true,new GUIStyle());
        internal void TestPause() => OnApplicationPause(true);
        internal void TestDisable() => OnDisable();
        internal void TestBackground() { loginBg = new Texture2D(); }
    }
    public static class MobileClientPresentation { public static bool Enabled; public static Rect SafeAreaGui; }
}
namespace UnityEngine
{
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
    public struct Quaternion { public static Quaternion identity => new Quaternion(); }
    public struct Matrix4x4 {
        public float x,y,sx,sy;
        public static Matrix4x4 identity => new Matrix4x4{sx=1,sy=1};
        public static Matrix4x4 TRS(Vector3 p,Quaternion q,Vector3 s)=>new Matrix4x4{x=p.x,y=p.y,sx=s.x,sy=s.y};
    }
    public class Texture2D { public static implicit operator bool(Texture2D value)=>value!=null; }
    public enum ScaleMode { ScaleAndCrop }
    public enum RuntimePlatform { Android, WindowsPlayer }
    public static class Application { public static RuntimePlatform platform; }
    public struct Rect {
        public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public float yMax => y+height;
        public bool Contains(Vector2 p)=>p.x>=x&&p.x<x+width&&p.y>=y&&p.y<yMax;
    }
    public static class Mathf { public static float Min(float a,float b)=>Math.Min(a,b); public static float Max(float a,float b)=>Math.Max(a,b); }
    public static class Screen { public static int width,height; }
    public enum TouchScreenKeyboardType { Default }
    public class TouchScreenKeyboard {
        public enum Status { Visible, Done, Canceled, LostFocus }
        public static bool isSupported = true; public static TouchScreenKeyboard last; public static int opens; public static Rect area;
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
        public static Matrix4x4 matrix=Matrix4x4.identity,backgroundDrawMatrix;
        public static void DrawTexture(Rect r,Texture2D t,ScaleMode s){backgroundDrawMatrix=matrix;}
        public static bool Button(Rect r,string s,GUIStyle style){lastDisplay=s;bool result=click;click=false;return result;}
        public static string PasswordField(Rect r,string s,char c,int max,GUIStyle style){passwordCalls++;return s;}
        public static string TextField(Rect r,string s,int max,GUIStyle style)=>s;
        public static void FocusControl(string s){}
    }
}
