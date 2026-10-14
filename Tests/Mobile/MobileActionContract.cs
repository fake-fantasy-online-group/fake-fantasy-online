using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
class MobileActionContract
{
    static int checks;
    static void Check(bool value,string name) { if(!value)throw new Exception(name);checks++; }
    static int Main()
    {
        var pc=new PlayerController();
        MobileClientPresentation.Enabled=false;ClientInput.JoystickHeld=true;ClientInput.MovementAxis=new Vector2(1,0);
        pc.Tick();Check(pc.Requests==0,"desktop path untouched");
        MobileClientPresentation.Enabled=true;pc.Tick();
        Check(pc.Requests==1 && pc.Requested.x>500 && pc.Requested.y==500,"stick enters existing navigation");
        Check(pc.worldX==500 && pc.worldY==500,"adapter never teleports");
        Check(CombatWorld.Stops==1,"movement cancels old attack only at entry");
        pc.Tick();Check(pc.Requests==1,"bounded replanning");
        ClientInput.MovementAxis=new Vector2(0,1);pc.Tick();Check(pc.Requested.y<500,"screen up maps north");
        ClientInput.JoystickHeld=false;pc.Tick();Check(!pc.Moving,"release stops navigation");
        ClientInput.JoystickHeld=true;pc.Tick();pc.Pause();Check(!pc.Moving&&!ClientInput.JoystickHeld,"background cancels movement");
        GameHud.hotSkill[0]=42;SkillTargeting.NextAim=SkillTargeting.Aim.Circle;
        SkillTargeting.BeginMobileHotSlot(0);Check(GameHud.Uses==1&&SkillTargeting.Active,"aim begins on down");
        Check(!ClientInput.Confirmed,"down never confirms targeted cast");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(15,25));Check(ClientInput.Confirmed&&ClientInput.AimPosition.x==15,"release confirms latest position");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(30,40));Check(ClientInput.Confirmed&&ClientInput.AimPosition.x==15,"duplicate targeted release preserves first confirmation");
        SkillTargeting.CancelMobileHotSlot();Check(!SkillTargeting.Active&&!ClientInput.Confirmed,"cancel clears pending release");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2));Check(GameHud.Uses==1,"late release after cancellation ignored");
        SkillTargeting.NextAim=SkillTargeting.Aim.Instant;SkillTargeting.BeginMobileHotSlot(0);Check(GameHud.Uses==1,"instant down never casts");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2));Check(GameHud.Uses==2,"instant release casts once");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2));Check(GameHud.Uses==2,"duplicate release ignored");
        SkillTargeting.NextAim=SkillTargeting.Aim.Circle;GameHud.Reject=true;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2));
        Check(GameHud.Uses==3&&!ClientInput.MobileSkillGestureActive,"rejected skill never retried on release");
        Console.WriteLine("PASS mobile action adapters: "+checks+" checks");return 0;
    }
}
