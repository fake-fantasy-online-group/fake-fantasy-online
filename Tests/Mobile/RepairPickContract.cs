using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
namespace UnityEngine
{
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
    public struct Rect {public float x,y,width,height;public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}public Vector2 center=>new Vector2(x+width/2,y+height/2);public bool Contains(Vector2 p)=>p.x>=x&&p.x<=x+width&&p.y>=y&&p.y<=y+height;}
    public static class GUI { public static int depth; }
    public enum EventType {Repaint,MouseDown,MouseUp,Used}
    public class Event { public static Event current=new Event();public EventType type;public Vector2 mousePosition;public void Use(){type=EventType.Used;} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    internal static class MobileClientPresentation { public static bool Enabled;public static float Scale=1; public static Rect SafeAreaGui=new Rect(10,20,800,400);public static object Owner; public static void RegisterPanel(object o,Rect r){Owner=o;}public static void RemovePanel(object o){if(Owner==o)Owner=null;} }
    internal static class SysMsgBox {public static bool Visible;}
    internal static class WinZ {public static object Owner;public static int Depth;public static void Raise(object o){Owner=o;}public static (int,bool) Frame(object o,Rect r,int d){Depth=d;return(d,false);}public static void Forget(object o){if(Owner==o)Owner=null;} }
    internal static class ArtworkSkin {public static bool Click;public static int Draws;public static Rect Last;public static bool Button(Rect r,string t,float s,bool enabled){Draws++;Last=r;return Click&&enabled;} }
    internal static class PlayerController {public static int Suppress;public static void SuppressWorldClick(){Suppress++;}}
    internal partial class RepairDlg {public static bool Picking;public static int Canceled;public static void CancelPick(){Picking=false;Canceled++;}public void Draw()=>DrawMobilePickCancel();}
}
class RepairPickContract
{
    static int checks;static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
    static void Main()
    {
        var d=new RepairDlg();RepairDlg.Picking=true;d.Draw();Check(ArtworkSkin.Draws==0&&RepairDlg.Canceled==0,"desktop unaffected");
        MobileClientPresentation.Enabled=true;Event.current.mousePosition=new Vector2(400,30);Event.current.type=EventType.MouseDown;d.Draw();
        Check(ArtworkSkin.Draws==1&&WinZ.Depth<0&&WinZ.Owner!=null,"cancel rendered above full-screen bag with independent hit owner");
        Check(Event.current.type==EventType.Used,"cancel area down does not reach item or world");
        Check(RepairDlg.Picking&&RepairDlg.Canceled==0,"down without activation does not cancel");
        ArtworkSkin.Click=true;Event.current.type=EventType.MouseUp;d.Draw();Check(!RepairDlg.Picking&&RepairDlg.Canceled==1&&PlayerController.Suppress==1,"explicit activation calls original cancellation");
        Check(MobileClientPresentation.Owner==null&&WinZ.Owner==null,"cancel removes hit ownership immediately");
        d.Draw();Check(RepairDlg.Canceled==1,"repeat release cannot repeat cancel");
        RepairDlg.Picking=true;SysMsgBox.Visible=true;int draws=ArtworkSkin.Draws;d.Draw();Check(ArtworkSkin.Draws==draws&&RepairDlg.Picking,"confirmation modal remains exclusive");
        SysMsgBox.Visible=false;ArtworkSkin.Click=false;d.Draw();MobileClientPresentation.Enabled=false;d.Draw();Check(MobileClientPresentation.Owner==null&&WinZ.Owner==null,"mode change cleans registered panel");
        Console.WriteLine("PASS mobile repair cancel adapter: "+checks+" checks");
    }
}
