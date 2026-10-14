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
  view.Selection.Select(10);GUI.click="放入快捷栏";view.DrawActions();
  Check(!view.Selection.ChoosingSlot&&NetClient.I.writes==0&&NetClient.I.upgrades==0,"unlearned cannot bind or implicitly learn");
  CharData.skills[10]=1;GUI.click="放入快捷栏";view.DrawActions();
  Check(view.Selection.ChoosingSlot&&NetClient.I.writes==0,"explicit bind enters chooser only");
  GameHud.hotSkill[29]=888;GameHud.hotKind[29]=2;GameHud.hotSkill[0]=999;GameHud.hotKind[0]=1;
  for(int page=0;page<7;page++){GUI.click="下一页";view.DrawActions();}
  GUI.click="8页2";view.DrawActions();
  Check(GameHud.hotSkill[29]==10&&GameHud.hotKind[29]==0&&GameHud.hotSkill[0]==999&&GameHud.hotKind[0]==1,"last partial page maps to slot29; replaces item without touching pet slot");
  Check(NetClient.I.writes==1&&NetClient.I.upgrades==0,"real drop sync once, no upgrade");
  GUI.click="8页2";view.DrawActions();Check(NetClient.I.writes==1,"repeated slot release does not resend");
  GUI.click="放入快捷栏";view.DrawActions();GUI.click="取消选槽";view.DrawActions();Check(!view.Selection.ChoosingSlot&&NetClient.I.writes==1,"cancel leaves bindings");
  GUI.click="返回技能树";view.DrawActions();Check(view.Selection.Skill==0,"back clears selection");view.Selection.Select(10);
  Win05.info.type=19;GUI.click="放入快捷栏";view.DrawActions();Check(!view.Selection.ChoosingSlot,"passive cannot bind");Win05.info.type=0;
  Win05.info.tgt=18;GUI.click="放入快捷栏";view.DrawActions();Check(!view.Selection.ChoosingSlot,"non-hotbar target cannot bind");Win05.info.tgt=0;
  GUI.click="放入快捷栏";view.DrawActions();CharData.skills.Clear();GUI.click="1页1";view.DrawActions();Check(NetClient.I.writes==1,"revalidate learned state at commit");
  view.LoseFocus();Check(view.Selection.Skill==0&&!view.Selection.ChoosingSlot,"focus loss clears pending selection");
  view.Selection.Select(10);GUI.click="学习 / 升级";view.DrawActions();Check(NetClient.I.upgrades==1&&NetClient.I.writes==1,"only explicit upgrade sends upgrade");
  var state=new MobileSkillSelection();state.Select(10);state.Choose(true);Check(!state.Commit(30,true,(a,b)=>{throw new Exception();}),"invalid slot rejected");
  Console.WriteLine("PASS mobile skill selection and real binding call paths: "+checks+" checks");
 }
}
