using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
static class Contract
{
 static int checks;
 static void Check(bool ok,string label){checks++;if(!ok)throw new Exception(label);}
 static void Main()
 {
  var view=new Win05();
  var safe=new Rect(30,10,740,330);var bounds=view.Prepare(safe);var bottom=view.BottomButtons;
  Check(bounds.x>=safe.x&&bounds.y>=safe.y&&bounds.x+bounds.width<=safe.x+safe.width&&bounds.y+bounds.height<=safe.y+safe.height+.001f,"800x360 inset safe area contains complete real window");
  Check(bottom.y+bottom.height<=safe.y+safe.height,"footer binding and learning fully reachable");
  view.Selection.Select(10);view.Selection.Choose(true);view.Prepare(new Rect(0,20,360,760));
  Check(view.Selection.Skill==0&&!view.Selection.ChoosingSlot,"rotation resets detail and pending chooser");
  view.Selection.Select(10);Input.touchCount=2;view.Prepare(new Rect(0,20,360,760));
  Check(view.Selection.Skill==0,"window-entry multi-touch cancels even detail path");Input.touchCount=0;
  view.Prepare(new Rect(0,0,960,540));
  Check(!view.Pointer(EventType.MouseDown,10,10),"down does not select");
  Check(view.Pointer(EventType.MouseUp,10,10),"release selects");
  view.Pointer(EventType.MouseDown,10,150);view.Pointer(EventType.MouseDrag,10,100);
  Check(Win05.skillScroll==50,"drag scrolls actual owner");
  Check(!view.Pointer(EventType.MouseUp,10,150),"return drag never selects");
  view.Pointer(EventType.MouseDown,10,10);Input.touchCount=1;Input.touch=new Touch{phase=TouchPhase.Canceled};
  Check(!view.Pointer(EventType.MouseUp,10,10),"canceled never selects");Input.touchCount=0;
  view.Pointer(EventType.MouseDown,10,10);Input.touchCount=2;
  Check(!view.Pointer(EventType.MouseUp,10,10),"second touch cancels tree tap");Input.touchCount=0;
  view.Pointer(EventType.MouseDown,10,10);Check(!view.Pointer(EventType.MouseUp,400,400),"outside release cancels");
  view.SelectForActions(10);GUI.click="放入快捷栏";view.DrawActions();
  Check(!view.Selection.ChoosingSlot&&NetClient.I.writes==0&&NetClient.I.upgrades==0,"unlearned cannot bind or implicitly learn");
  CharData.skills[10]=1;view.SelectForActions(10);GUI.click="放入快捷栏";view.DrawActions();
  Check(view.Selection.ChoosingSlot&&NetClient.I.writes==0,"explicit bind enters chooser only");
  for(int i=24;i<30;i++){GameHud.hotSkill[i]=859+i;GameHud.hotKind[i]=2;}
  GameHud.hotSkill[29]=888;GameHud.hotKind[29]=2;GameHud.hotSkill[0]=999;GameHud.hotKind[0]=1;
  for(int page=0;page<3;page++){GUI.click="下一页";view.DrawActions();}
  GUI.click="4页6";view.DrawActions();
  Check(GameHud.hotSkill[23]==10&&GameHud.hotKind[23]==0&&GameHud.hotSkill[29]==888&&GameHud.hotKind[29]==2&&GameHud.hotSkill[0]==999&&GameHud.hotKind[0]==1,"fourth group sixth button maps to slot23 and preserves desktop slot29");
  Check(NetClient.I.writes==1&&NetClient.I.upgrades==0,"real drop sync once, no upgrade");
  for(int i=24;i<30;i++)Check(GameHud.hotSkill[i]==859+i&&GameHud.hotKind[i]==2,"all six unexposed desktop bindings preserved");
  foreach(int invalid in new[]{-1,24,29,30,int.MaxValue})GameHud.BindMobileBookSkill(invalid,10,null);
  Check(NetClient.I.writes==1,"invalid direct mobile binding requests neither mutate nor sync");
  GUI.click="4页6";view.DrawActions();Check(NetClient.I.writes==1,"repeated slot release does not resend");
  view.SelectForActions(10);GUI.click="放入快捷栏";view.DrawActions();GUI.click="取消";view.DrawActions();Check(!view.Selection.ChoosingSlot&&NetClient.I.writes==1,"cancel leaves bindings");
  GUI.click="取消";view.DrawActions();Check(view.Selection.Skill==0,"back clears selection");view.SelectForActions(10);
  Win05.info.type=19;GUI.click="放入快捷栏";view.DrawActions();Check(!view.Selection.ChoosingSlot,"passive cannot bind");Win05.info.type=0;
  Win05.info.tgt=18;GUI.click="放入快捷栏";view.DrawActions();Check(!view.Selection.ChoosingSlot,"non-hotbar target cannot bind");Win05.info.tgt=0;
  GUI.click="放入快捷栏";view.DrawActions();CharData.skills.Clear();GUI.click="1页1";view.DrawActions();Check(NetClient.I.writes==1,"revalidate learned state at commit");
  view.LoseFocus();Check(view.Selection.Skill==0&&!view.Selection.ChoosingSlot,"focus loss clears pending selection");
  view.SelectForActions(10);GUI.click="学习 / 升级";view.DrawActions();Check(NetClient.I.upgrades==1&&NetClient.I.writes==1,"only explicit upgrade sends upgrade");
  var state=new MobileSkillSelection();state.Select(10);state.Choose(true);Check(!state.Commit(24,true,(a,b)=>{throw new Exception();}),"invalid slot rejected");
  Time.unscaledTime=0;view.Pointer(EventType.MouseDown,10,10);Time.unscaledTime=.5f;
  Check(view.Pointer(EventType.Repaint,10,10)&&view.Holding,"hold selects actions only after threshold");
  Check(!view.Pointer(EventType.MouseUp,10,10),"hold release cannot tap or duplicate actions");
  Time.unscaledTime=1;Input.touchCount=1;Input.touch=new Touch{fingerId=22,phase=TouchPhase.Began};
  view.Pointer(EventType.MouseDown,10,10);Input.touchCount=0;Time.unscaledTime=2;
  Check(!view.Pointer(EventType.Repaint,10,10)&&view.Selection.Skill==0,"lost skill finger cancels rather than becoming a late hold");
  Input.touchCount=1;Input.touch=new Touch{fingerId=23,phase=TouchPhase.Began};view.Pointer(EventType.MouseDown,10,10);
  Input.touch.fingerId=24;Check(!view.Pointer(EventType.Repaint,10,10),"replacement finger cannot inherit pending skill hold");Input.touchCount=0;
  foreach (int kind in new[]{0,1,2})
  {
   int writes=NetClient.I.writes;GameHud.BindMobileShortcut(23,200+kind,null,kind);
   Check(GameHud.hotSkill[23]==200+kind&&GameHud.hotKind[23]==kind&&NetClient.I.writes==writes+1,"all shortcut kinds persist once");
  }
  var savedIds=NetClient.I.savedIds;var savedKinds=NetClient.I.savedKinds;
  GameHud.hotSkill[23]=0;GameHud.LoadHotbar(savedIds,savedKinds,false);
  Check(GameHud.hotSkill[23]==202&&GameHud.hotKind[23]==2,"item binding roundtrips existing persistence format");
  Check(MobileShortcutEligibility.Bag(0,20,false,0)&&MobileShortcutEligibility.Bag(1,20,false,0),"ordinary item tabs allowed");
  Check(!MobileShortcutEligibility.Bag(2,20,false,0)&&!MobileShortcutEligibility.Bag(0,20,true,10),"equipment and pet cards blocked");
  Check(MobileShortcutEligibility.Bag(2,20,false,14001),"fabao retains skill mapping");
  CharData.petSkills=new[]{new CharData.PetSkill{id=13001,level=1,active=true}};
  view.PetActions(13001);GUI.click="放入快捷栏";view.DrawActions();
  Check(view.Selection.ChoosingSlot,"learned active PET enters same chooser");
  int petWrites=NetClient.I.writes;GUI.click="1页2";view.DrawActions();
  Check(GameHud.hotSkill[1]==13001&&GameHud.hotKind[1]==1&&NetClient.I.writes==petWrites+1,"PET stores original kind 1");
  view.PetActions(13001);GUI.click="放入快捷栏";view.DrawActions();CharData.petSkills=new CharData.PetSkill[0];GUI.click="1页3";view.DrawActions();
  Check(NetClient.I.writes==petWrites+1&&view.Selection.Skill==0,"removed PET skill invalidates pending chooser");
  CharData.petSkills=new[]{new CharData.PetSkill{id=13001,level=1,active=false}};
  view.PetActions(13001);GUI.click="放入快捷栏";view.DrawActions();Check(!view.Selection.ChoosingSlot,"passive PET cannot bind");
  view.LoseFocus();Time.unscaledTime=0;Time.frameCount++;Input.touchCount=1;Input.touch=new Touch{fingerId=30,phase=TouchPhase.Began,position=new Vector2(20,520)};
  view.ReadPet(new Event{type=EventType.MouseDown});Time.unscaledTime=.6f;Time.frameCount++;Input.touch.phase=TouchPhase.Ended;
  view.ReadPet(new Event{type=EventType.MouseUp});Check(view.Selection.Skill==10,"PET raw hold selects without desktop pickup");
  view.LoseFocus();view.Cover(true);Time.frameCount++;Input.touch.phase=TouchPhase.Began;view.ReadPet(new Event{type=EventType.MouseDown});
  Time.unscaledTime=1.2f;Time.frameCount++;Input.touch.phase=TouchPhase.Ended;view.ReadPet(new Event{type=EventType.MouseUp});Check(view.Selection.Skill==0,"covered PET window ignores raw touches");
  view.Cover(false);Input.touchCount=0;
  BagBindingContract.Run();
  OriginalPanelContract.Run();
  MobileFixedActionContract.Run();
  Console.WriteLine("PASS mobile skill selection and real binding call paths: "+checks+" checks");
 }
}
