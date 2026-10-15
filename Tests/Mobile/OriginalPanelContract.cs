using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
internal static class OriginalPanelContract
{
    private static int checks;
    private static void Check(bool value, string name) { checks++; if (!value) throw new Exception(name); }
    internal static void Run()
    {
        Rect safe = new Rect(30, 10, 740, 330);
        foreach (Rect design in new[] { new Rect(0,0,423,501), new Rect(-26,0,313,506), new Rect(-26,0,536,506), new Rect(0,0,423,506) })
        {
            float s = 2, x = -500, y = 500;
            Rect result = MobilePanelLayout.Fit(design, safe, ref s, ref x, ref y);
            Check(result.x >= safe.x-.001f && result.y >= safe.y-.001f && result.xMax <= safe.xMax+.001f && result.yMax <= safe.yMax+.001f, "native full panel/side-flyouts fit inset safe area");
            Check(Math.Abs(result.width/result.height-design.width/design.height)<.001f, "uniform scale preserves native proportions");
        }
        MobileClientPresentation.SafeAreaGui=safe;
        Rect menu=MobilePanelLayout.Actions(new Vector2(799,359),7,out float ms);
        Check(menu.x>=safe.x&&menu.y>=safe.y&&menu.xMax<=safe.xMax+.001f&&menu.yMax<=safe.yMax+.001f,"action chooser stays inside safe area");
        var gesture = new MobilePanelTouch();
        Func<Vector2,int> hit = p=>p.x>=0&&p.x<100&&p.y>=0&&p.y<100?9:-1;
        Event ev = new Event {type=EventType.MouseDown,mousePosition=new Vector2(20,20)};
        Time.unscaledTime=0;Input.touchCount=0;Application.isMobilePlatform=false;
        Check(!gesture.Read(ev,1,hit,out int slot,out bool held)&&ev.type==EventType.Used,"tap down never activates and owns synthetic mouse");
        ev=new Event{type=EventType.MouseUp,mousePosition=new Vector2(20,20)};
        Check(gesture.Read(ev,1,hit,out slot,out held)&&slot==9&&!held&&ev.type==EventType.Used,"tap release selects native tooltip once");
        Check(!gesture.Read(ev,1,hit,out slot,out held),"used release cannot select again");
        ev=new Event{type=EventType.MouseDown,mousePosition=new Vector2(120,120)};
        gesture.Read(ev,1,hit,out slot,out held);
        Check(gesture.Started&&gesture.StartPoint.x==120&&gesture.Anchor.x==20&&gesture.Anchor.y==20,"action/menu click never relocates selected item anchor");
        ev=new Event{type=EventType.MouseDown,mousePosition=new Vector2(20,20)};gesture.Read(ev,1,hit,out slot,out held);
        Time.unscaledTime=.5f;ev=new Event{type=EventType.Repaint,mousePosition=new Vector2(20,20)};
        Check(gesture.Read(ev,1,hit,out slot,out held)&&held,"stationary hold requests action menu");
        ev=new Event{type=EventType.MouseUp,mousePosition=new Vector2(20,20)};
        Check(!gesture.Read(ev,1,hit,out slot,out held)&&ev.type==EventType.Used,"hold release cannot press first action");
        Time.unscaledTime=1;ev=new Event{type=EventType.MouseDown,mousePosition=new Vector2(20,20)};gesture.Read(ev,1,hit,out slot,out held);
        ev=new Event{type=EventType.MouseDrag,mousePosition=new Vector2(60,20)};gesture.Read(ev,1,hit,out slot,out held);
        Time.unscaledTime=2;ev=new Event{type=EventType.MouseUp,mousePosition=new Vector2(20,20)};
        Check(!gesture.Read(ev,1,hit,out slot,out held),"drag out and back cannot select or act");
        Application.isMobilePlatform=true;Input.touchCount=1;Input.touch=new Touch{fingerId=4,phase=TouchPhase.Began,position=new Vector2(20,Screen.height-20)};Time.frameCount++;
        ev=new Event{type=EventType.Layout};gesture.Read(ev,1,hit,out slot,out held);
        Input.touch.phase=TouchPhase.Ended;Time.frameCount++;ev=new Event{type=EventType.Layout};
        Check(gesture.Read(ev,1,hit,out slot,out held)&&slot==9&&!held,"real raw touch release selects once");
        Check(!gesture.Read(ev,1,hit,out slot,out held),"same touch frame is not replayed by layout/repaint");
        Input.touch.phase=TouchPhase.Began;Time.frameCount++;gesture.Read(ev,1,hit,out slot,out held);
        Input.touchCount=2;Time.frameCount++;Check(!gesture.Read(ev,1,hit,out slot,out held)&&gesture.Canceled,"second touch cancels pending action");
        Input.touchCount=1;Input.touch.phase=TouchPhase.Ended;Time.frameCount++;Check(!gesture.Read(ev,1,hit,out slot,out held),"release after multitouch cannot act");
        Input.touch.phase=TouchPhase.Began;Time.frameCount++;gesture.Read(ev,1,hit,out slot,out held);
        Time.unscaledTime=3;Input.touch.phase=TouchPhase.Canceled;Time.frameCount++;
        Check(!gesture.Read(ev,1,hit,out slot,out held)&&gesture.Canceled,"cancellation wins over elapsed hold time");
        Input.touch.phase=TouchPhase.Began;Time.frameCount++;gesture.Read(ev,1,hit,out slot,out held);
        Input.touchCount=0;Time.frameCount++;Check(!gesture.Read(ev,1,hit,out slot,out held)&&gesture.Canceled,"missing contact clears owner");
        Input.touchCount=1;Input.touch.fingerId=5;Input.touch.phase=TouchPhase.Began;Time.frameCount++;gesture.Read(ev,1,hit,out slot,out held);
        Input.touch.phase=TouchPhase.Ended;Time.frameCount++;Check(gesture.Read(ev,1,hit,out slot,out held),"next finger works after interrupted owner");
        Input.touchCount=0;Application.isMobilePlatform=false;
        Console.WriteLine("PASS original panel geometry and actual touch adapter: "+checks+" checks");
    }
}
