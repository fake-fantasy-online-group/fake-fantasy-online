using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
class MobileActionContract
{
    static int checks;
    static void Check(bool value,string name) { if(!value)throw new Exception(name);checks++; }
    static int Main()
    {
        var pc=new PlayerController();PlayerController.I=pc;
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
        SkillTargeting.BeginMobileHotSlot(0);Check(GameHud.Uses==0&&SkillTargeting.Active,"preview on down without cast");
        Check(!ClientInput.Confirmed,"down never confirms targeted cast");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(15,25),false);Check(CombatWorld.Grounds==1&&CombatWorld.RequiredTarget==5,"smart tap uses legal target");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(30,40),false);Check(CombatWorld.Grounds==1,"duplicate release ignored");
        SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.CancelMobileHotSlot();Check(!SkillTargeting.Active&&!ClientInput.Confirmed,"cancel clears preview");
        SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Grounds==1,"late release after cancellation ignored");
        GameSettings.SmartCast=false;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),false);Check(CombatWorld.Grounds==1,"smart off tap does not cast");
        SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Grounds==2&&CombatWorld.RequiredTarget==0,"smart off drag casts empty point");
        GameSettings.SmartCast=true;CombatWorld.HasTarget=false;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),false);Check(CombatWorld.Grounds==2,"smart tap no target no cast");
        SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Grounds==3,"smart on drag allows empty ground");
        SkillTargeting.NextAim=SkillTargeting.Aim.Single;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Units==1&&!CombatWorld.LastNearest,"single drag uses pointer target");
        SkillTargeting.NextAim=SkillTargeting.Aim.Trap;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),false);Check(CombatWorld.Traps==0&&CombatWorld.NearestTraps==1,"trap tap picks legal trap only");SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Traps==1,"trap drag uses trap API");
        SkillTargeting.NextAim=SkillTargeting.Aim.Circle;GameHud.Reject=true;SkillTargeting.BeginMobileHotSlot(0);GameHud.Reject=false;SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Grounds==3&&!ClientInput.MobileSkillGestureActive,"down rejection cannot retry on release");
        SkillTargeting.BeginMobileHotSlot(0);GameHud.Reject=true;SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Grounds==3,"release admission rechecked");GameHud.Reject=false;
        SkillTargeting.BeginMobileHotSlot(0);GameHud.hotSkill[0]=43;SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Grounds==3,"rebound slot cannot cast wrong skill");
        GameHud.hotKind[0]=2;GameSettings.SmartCast=false;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),false);Check(GameHud.Uses==1,"item tap preserved");
        GameHud.hotKind[0]=1;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),false);Check(GameHud.Uses==2,"pet tap preserved");
        GameHud.hotKind[0]=0;SkillTargeting.NextAim=SkillTargeting.Aim.Instant;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),false);Check(NetClient.I.Uses==1&&NetClient.I.LastTarget==0,"true self tap allowed with smart off without borrowed target");
        Win05.Info.canEnemy=0;Win05.Info.canFriendly=1;SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),false);Check(NetClient.I.Uses==1&&CombatWorld.Units==1,"legacy friendly requires drag when smart off");
        SkillTargeting.BeginMobileHotSlot(0);SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(CombatWorld.Units==2,"legacy friendly drag uses strict target API");
        SkillTargeting.BeginMobileHotSlot(0);GameHud.hotKind[0]=2;SkillTargeting.ReleaseMobileHotSlot(new Vector2(1,2),true);Check(GameHud.Uses==2&&CombatWorld.Units==2,"rebound slot kind cannot cast item during skill gesture");
        Console.WriteLine("PASS mobile action adapters: "+checks+" checks");return 0;
    }
}
