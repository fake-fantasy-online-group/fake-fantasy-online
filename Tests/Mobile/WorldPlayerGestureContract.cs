using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine { public static class Time { public static float time=1,unscaledTime; } }
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class MobileClientPresentation { public static float Scale=1; }
    public class RemotePlayer { public int id=7; public string pname="peer"; public bool HostileToLocal; }
    public static class CombatWorld
    {
        public static int Attacks,Stops;
        public static void SetPvpTarget(RemotePlayer p) { Attacks++; }
        public static void StopAttack() { Stops++; }
    }
    public static class TargetSelect { public static int Selects; public static void Select(RemotePlayer p) { Selects++; } }
    public static class PlayerActionMenu
    {
        public static int Opens,Id; public static Vector2 Anchor;
        public static void OpenAtScreen(int id,string name,Vector2 position) { Opens++;Id=id;Anchor=position; }
    }
    public partial class PlayerController
    {
        static float suppressWorldClickUntil;
        public RemotePlayer Picked;
        public int Stopped;
        object PickSelectable(Vector2 point)=>Picked;
        void StopForNpcPointerForIndependentRuntime() { Stopped++; }
        static void SuppressWorldClick() { suppressWorldClickUntil=Time.time+.35f; }
        public bool Sample(ClientPointerFrame p,bool ui=false)=>HandleMobilePlayerPointer(p,ui,Vector2.zero);
        public void Reset()=>ResetMobilePlayerGesture();
        public void LoseTarget() { mobilePlayerTarget=null; }
    }
}
class WorldPlayerGestureContract
{
    static int checks;
    static void Check(bool value,string name) { if(!value)throw new Exception(name);checks++; }
    static ClientPointerFrame P(float time,bool down=false,bool up=false,bool cancel=false,float x=10)
    {
        Time.time=10+time;Time.unscaledTime=time;
        return new ClientPointerFrame{Position=new Vector2(x,20),Down=down,Up=up,Held=!up&&!cancel,Cancelled=cancel,Available=true};
    }
    static int Main()
    {
        var pc=new PlayerController{Picked=new RemotePlayer{HostileToLocal=true}};
        Check(pc.Sample(P(0,down:true)),"player down captured");
        Check(CombatWorld.Attacks==0&&PlayerActionMenu.Opens==0,"down does not attack or open");
        pc.Sample(P(.2f,up:true));Check(CombatWorld.Attacks==1,"hostile short tap attacks once");
        Check(!pc.Sample(P(.21f,up:true))&&CombatWorld.Attacks==1,"duplicate release no attack");
        pc.Sample(P(1,down:true));pc.Sample(P(1.5f));
        Check(PlayerActionMenu.Opens==1&&PlayerActionMenu.Id==7,"long press uses existing player menu");
        Check(PlayerActionMenu.Anchor.x==10&&PlayerActionMenu.Anchor.y==20,"menu gets owned finger position");
        pc.Sample(P(1.6f));pc.Sample(P(1.7f,up:true));
        Check(PlayerActionMenu.Opens==1&&CombatWorld.Attacks==1,"hold and release never repeat menu or attack");
        Check(pc.Stopped==1&&CombatWorld.Stops==1,"long menu stops active navigation and attack");
        pc.Sample(P(2,down:true));pc.Sample(P(2.2f,x:40));pc.Sample(P(2.8f,up:true));
        Check(PlayerActionMenu.Opens==1&&CombatWorld.Attacks==1,"drag out and back cancels both actions");
        pc.Sample(P(3,down:true));pc.Sample(P(3.6f,cancel:true));
        Check(PlayerActionMenu.Opens==1,"cancel has priority over elapsed hold");
        pc.Sample(P(4,down:true));pc.Reset();pc.Sample(P(4.8f,up:true));
        Check(PlayerActionMenu.Opens==1&&CombatWorld.Attacks==1,"map/focus reset prevents late release");
        Check(!pc.Sample(P(5,down:true),ui:true),"UI start cannot capture world player");
        pc.Sample(P(6,down:true));pc.Sample(P(6.6f),ui:true);Check(pc.Sample(P(6.7f,up:true)),"cancelled UI crossing still consumes release");
        Check(PlayerActionMenu.Opens==1&&CombatWorld.Attacks==1,"UI crossing cancels captured world action");
        pc.Sample(P(7,down:true));pc.LoseTarget();pc.Sample(P(7.7f,up:true));
        Check(PlayerActionMenu.Opens==1,"missing target cannot open menu");
        pc.Picked.HostileToLocal=false;pc.Sample(P(8,down:true));pc.Sample(P(8.1f,up:true));
        Check(TargetSelect.Selects==1&&CombatWorld.Attacks==1,"friendly short tap only selects");
        pc.Picked=null;Check(!pc.Sample(P(9,down:true)),"non-player keeps existing world click route");
        Console.WriteLine("PASS mobile world player long press: "+checks+" checks");return 0;
    }
}
