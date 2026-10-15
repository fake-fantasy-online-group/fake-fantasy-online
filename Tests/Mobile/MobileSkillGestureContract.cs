using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
public static class MobileSkillGestureContract
{
    private static void Check(bool value,string name) { if(!value) throw new Exception(name); }
    public static void Main()
    {
        foreach(float scale in new[]{.1f,.5f,1f,2f,4f})
        {
            var gesture=new MobileSkillGesture();
            gesture.Begin(100,200,scale); gesture.Sample(100+11*scale,200);
            Check(!gesture.Dragged,"scaled tap slop");
            Check(!gesture.AllowsRelease(false,false,false),"manual mode rejects taps");
            Check(gesture.AllowsRelease(true,false,false),"smart tap eligible for target check");
            Check(gesture.AllowsRelease(false,true,false),"self tap always eligible");
            gesture.Sample(100+13*scale,200); Check(gesture.Dragged,"drag detected");
            gesture.Sample(100,200); Check(gesture.Dragged,"drag stays latched on return");
            Check(gesture.AllowsRelease(false,false,false),"manual drag eligible");
            Check(!gesture.AllowsRelease(true,true,true),"cancel overrides all modes");
            gesture.Reset(); Check(!gesture.AllowsRelease(true,true,false),"reset blocks stale release");
            gesture.Begin(0,0,scale); Check(!gesture.Dragged,"new gesture clears history");
        }
        Console.WriteLine("Mobile skill gesture contract passed (scaled slop, latch, smart/manual/self, cancellation).");
    }
}
