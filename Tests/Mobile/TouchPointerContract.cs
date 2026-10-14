using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
class TouchPointerContract
{
    static int checks;
    static void Check(bool value,string name) { if(!value) throw new Exception(name); checks++; }
    static Vector2 P(float x,float y=0) => new Vector2(x,y);
    static void Feed(TouchPointerRouter r,int id,ClientTouchPhase phase,bool ui=false,bool stick=false,float x=10)
        => r.Feed(id,P(x),phase,ui,stick,P(0),10);
    static int Main()
    {
        var r=new TouchPointerRouter();
        r.BeginFrame(); Feed(r,1,ClientTouchPhase.Began,ui:true); r.EndFrame();
        Check(!r.Pointer.Down && !r.JoystickHeld,"UI captures begin");
        r.BeginFrame(); Feed(r,1,ClientTouchPhase.Moved); r.EndFrame();
        Check(!r.Pointer.Held,"UI drag cannot become world");
        r.BeginFrame(); Feed(r,1,ClientTouchPhase.Ended); r.EndFrame();
        Check(!r.Pointer.Up,"UI release cannot click world");
        r.BeginFrame(); Feed(r,1,ClientTouchPhase.Began,ui:true,stick:true); Feed(r,2,ClientTouchPhase.Began); r.EndFrame();
        Check(r.JoystickHeld && r.MovementAxis.x==1,"stick axis normalized");
        Check(r.Pointer.Down && r.Pointer.Held,"second finger independent world");
        r.BeginFrame(); Feed(r,1,ClientTouchPhase.Stationary,ui:true,stick:true); Feed(r,2,ClientTouchPhase.Stationary); Feed(r,3,ClientTouchPhase.Began,x:99); r.EndFrame();
        Check(!r.Pointer.Down && r.Pointer.Position.x==10,"third finger cannot steal world");
        r.BeginFrame(); Feed(r,1,ClientTouchPhase.Moved,ui:false,stick:false,x:100); Feed(r,2,ClientTouchPhase.Cancelled); Feed(r,3,ClientTouchPhase.Stationary); r.EndFrame();
        Check(r.Pointer.Cancelled && !r.Pointer.Up && !r.Pointer.Held,"cancel never confirms");
        Check(r.JoystickHeld && r.MovementAxis.x==1,"stick retains ownership outside rect");
        r.BeginFrame(); Feed(r,1,ClientTouchPhase.Ended); Feed(r,3,ClientTouchPhase.Moved); r.EndFrame();
        Check(!r.JoystickHeld && r.MovementAxis.x==0,"stick release stops");
        Check(!r.Pointer.Held && !r.Pointer.Down,"blocked third finger never promoted");
        r.CancelAll();
        r.BeginFrame(); Feed(r,3,ClientTouchPhase.Moved); r.EndFrame();
        Check(!r.Pointer.Available,"old finger ignored after map/focus reset");
        r.BeginFrame(); Feed(r,4,ClientTouchPhase.Began); r.EndFrame();
        Check(r.Pointer.Down,"fresh begin works after reset");
        r.BeginFrame(); Feed(r,4,ClientTouchPhase.Ended); r.EndFrame();
        Check(r.Pointer.Up && !r.Pointer.Held,"world release once");
        r.BeginFrame(); r.EndFrame(); Check(!r.Pointer.Up && !r.Pointer.Available,"release not replayed");
        r.BeginFrame(); Feed(r,5,ClientTouchPhase.Began,stick:true); Feed(r,6,ClientTouchPhase.Began); r.EndFrame();
        r.BeginFrame(); Feed(r,5,ClientTouchPhase.Stationary,stick:true); r.EndFrame();
        Check(r.Pointer.Cancelled && r.JoystickHeld,"lost world touch cancels even while stick remains");
        r.BeginFrame(); r.EndFrame(); Check(!r.JoystickHeld && r.MovementAxis.x==0,"lost stick touch cancels");
        Console.WriteLine("PASS mobile touch ownership: "+checks+" checks"); return 0;
    }
}
