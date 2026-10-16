using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class MobileLayoutContract {
static int checks;
static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
static bool Within(Rect a,Rect b)=>a.x>=b.x-.01f&&a.y>=b.y-.01f&&a.xMax<=b.xMax+.01f&&a.yMax<=b.yMax+.01f;
static bool Overlap(Rect a,Rect b)=>a.x<b.xMax-.01f&&a.xMax>b.x+.01f&&a.y<b.yMax-.01f&&a.yMax>b.y+.01f;
static void Main(){
Check(!MobileClientPresentation.Enabled,"Desktop default unchanged");MobileClientPresentation.Simulate=true;GameHud.I=new GameHud();
foreach(var size in new[]{new[]{1280,720},new[]{812,375},new[]{2340,1080},new[]{1920,1200}}){
Screen.width=size[0];Screen.height=size[1];Screen.safeArea=new Rect(24,16,size[0]-48,size[1]-32);
Rect safe=MobileClientPresentation.SafeAreaGui;
Check(Math.Abs(MobileClientPresentation.AttackRect.center.x+MobileClientPresentation.JoystickRect.center.x-2*safe.center.x)<.01f && Math.Abs(MobileClientPresentation.AttackRect.center.y-MobileClientPresentation.JoystickRect.center.y)<.01f,"Thumb centers mirror exactly in safe area");
Check(MobileClientPresentation.AvatarRect.x==safe.x && MobileClientPresentation.AvatarRect.y==safe.y,"Avatar starts at actual safe area without added gutter");
Check(MobileClientPresentation.IsPointerOverUi(new Vector2(MobileClientPresentation.PickupRect.center.x,Screen.height-MobileClientPresentation.PickupRect.center.y)),"Pickup excludes world pointer");
Check(MobileClientPresentation.PageRect.width==MobileClientPresentation.PageRect.height,"Page control is square");
float ms=MobileClientPresentation.Scale, right=safe.x+MobileClientPresentation.MinimapViewportWidth;
var nativeFaces=new[]{new Rect(right-29*ms,safe.y+57*ms,26*ms,26*ms),new Rect(right-24*ms,safe.y+83*ms,26*ms,26*ms),new Rect(right-39*ms,safe.y+30*ms,26*ms,26*ms),new Rect(right-20*ms,safe.y,20*ms,19*ms)};
for(int i=0;i<4;i++){
 Rect hit=MobileClientPresentation.MapActionRect(i);
 Check(Within(hit,safe),"Map action safe "+i);
 Check(Within(nativeFaces[i],hit),"Native map face inside mobile action "+i);
 Check(Within(hit,MobileClientPresentation.MinimapRect),"Map hit inside exclusion "+i);
 for(int j=0;j<i;j++)Check(!Overlap(hit,MobileClientPresentation.MapActionRect(j)),"Disjoint native map actions "+i+","+j);
}
var controls=new[]{MobileClientPresentation.PickupRect,MobileClientPresentation.JoystickRect,MobileClientPresentation.AttackRect,MobileClientPresentation.SkillRect(0),MobileClientPresentation.SkillRect(1),MobileClientPresentation.SkillRect(2),MobileClientPresentation.SkillRect(3),MobileClientPresentation.SkillRect(4),MobileClientPresentation.SkillRect(5),MobileClientPresentation.PageRect,MobileClientPresentation.MenuToggleRect,MobileClientPresentation.MenuRect,MobileClientPresentation.MinimapRect};
for(int i=0;i<controls.Length;i++){Check(Within(controls[i],safe),"Safe bounds "+i);for(int j=i+1;j<controls.Length;j++)Check(!Overlap(controls[i],controls[j]),"Control overlap "+i+","+j);}
Check(MobileClientPresentation.MenuToggleRect.yMax<safe.y+100f*MobileClientPresentation.Scale,"Menu switch stays in top band");
Check(MobileClientPresentation.MenuToggleRect.x>=safe.x+171f*MobileClientPresentation.Scale,"Menu avoids avatar");
Check(MobileClientPresentation.MenuRect.xMax<=MobileClientPresentation.MinimapRect.x-12f*MobileClientPresentation.Scale+.01f,"Menu avoids map");
for(int i=0;i<9;i++){Rect entry=MobileClientPresentation.MenuItemRect(i);Check(Within(entry,MobileClientPresentation.MenuRect),"Entry within menu "+i);Check(!Overlap(entry,MobileClientPresentation.MenuToggleRect),"Entry avoids switch "+i);for(int j=0;j<i;j++)Check(!Overlap(entry,MobileClientPresentation.MenuItemRect(j)),"Distinct menu entry "+i+","+j);}
MobileClientPresentation.ChatExpanded=false;Check(Within(MobileClientPresentation.ChatRect,safe),"Compact chat safe");
Check(Math.Abs(MobileClientPresentation.ChatRect.height-112f*MobileClientPresentation.Scale)<.01f,"Compact chat height");
Check(!Overlap(MobileClientPresentation.ChatRect,MobileClientPresentation.JoystickRect),"Chat avoids joystick");
Check(!Overlap(MobileClientPresentation.ChatRect,MobileClientPresentation.AttackRect),"Chat avoids attack");
for(int i=0;i<6;i++)Check(!Overlap(MobileClientPresentation.ChatRect,MobileClientPresentation.SkillRect(i)),"Chat avoids skill "+i);
MobileClientPresentation.ChatExpanded=true;Rect chat=MobileClientPresentation.ChatRect;Check(Math.Abs(chat.center.x-safe.center.x)<.01&&Math.Abs(chat.yMax-(safe.yMax-12f*MobileClientPresentation.Scale))<.01,"Expanded chat bottom anchored");Check(Math.Abs(chat.height-196f*MobileClientPresentation.Scale)<.01,"Expanded chat compact height");Check(Within(chat,safe),"Expanded chat safe");
for(int button=0;button<6;button++){
MobileClientPresentation.ActiveSkillButton=button;Rect cancel=MobileClientPresentation.CancelRect;
Check((cancel.center-MobileClientPresentation.SkillRect(button).center).sqrMagnitude >= (127.9f*ms)*(127.9f*ms),"Cancel has deliberate minimum travel");
Check(Within(cancel,safe),"Cancel safe area "+button);
foreach(Rect control in controls)Check(!Overlap(cancel,control),"Cancel avoids controls "+button);
Check(!Overlap(cancel,MobileClientPresentation.ChatRect),"Cancel avoids expanded chat "+button);
Check(MobileClientPresentation.CancelContains(cancel.center),"Cancel center accepted "+button);
Check(!MobileClientPresentation.CancelContains(new Vector2(cancel.x,cancel.y)),"Cancel bounding corner rejected "+button);
Check((cancel.center-MobileClientPresentation.SkillRect(button).center).sqrMagnitude<=(200f*ms)*(200f*ms),"Cancel remains within thumb reach "+button);
}
MobileClientPresentation.ActiveSkillButton=-1;Check(!MobileClientPresentation.CancelContains(MobileClientPresentation.CancelRect.center),"Inactive cancel has no hit area");
Vector2 joy=MobileClientPresentation.JoystickRect.center;joy.y=Screen.height-joy.y;Check(MobileClientPresentation.IsJoystickRegion(joy),"Joystick region");SysMsgBox.Visible=true;Check(!MobileClientPresentation.IsJoystickRegion(joy),"Modal blocks joystick");Check(MobileClientPresentation.IsPointerOverUi(new Vector2(400,250)),"Modal blocks world");SysMsgBox.Visible=false;
object owner=new object();MobileClientPresentation.RegisterPanel(owner,safe);Check(MobileClientPresentation.IsPanelPointer(joy),"Panel registers");Check(!MobileClientPresentation.IsJoystickRegion(joy),"Panel owns pointer");MobileClientPresentation.RemovePanel(owner);Check(!MobileClientPresentation.IsPanelPointer(joy),"Panel removed");
}
// Actual production wheel layout across landscape, portrait, and compact safe areas.
foreach(var size in new[]{new[]{1280,720},new[]{812,375},new[]{375,812},new[]{320,240},new[]{240,320}}){
 Screen.width=size[0];Screen.height=size[1];Screen.safeArea=new Rect(12,12,size[0]-24,size[1]-24);
 Rect safe=MobileClientPresentation.SafeAreaGui;float scale=MobileClientPresentation.Scale;
 Vector2 center=MobileClientPresentation.GroupWheelCenter;
 for(int group=0;group<4;group++){
  MobileGroupWheel.Direction(group,out float dx,out float dy);
  Vector2 position=center+new Vector2(dx,dy)*MobileClientPresentation.GroupWheelRadius;
  Check(Within(new Rect(position.x-24*scale,position.y-24*scale,48*scale,48*scale),safe),"all wheel labels inside safe area");
  Check(MobileGroupWheel.Hit(position.x-center.x,position.y-center.y,MobileClientPresentation.GroupWheelDeadZone)==group,"drawn label equals production hit direction");
 }
 MobileClientPresentation.GroupWheelOpen=true;
 Check(MobileClientPresentation.GroupWheelContains(center),"open center captured");
 Check(MobileClientPresentation.IsPointerOverUi(new Vector2(center.x,Screen.height-center.y)),"open wheel excludes world");
 MobileClientPresentation.GroupWheelOpen=false;GameHud.MobileGroupWheelOpening=true;
 Check(MobileClientPresentation.GroupWheelContains(center),"opening frame also captured");GameHud.MobileGroupWheelOpening=false;
}
Check(MobileClientPresentation.MenuColumnsForWidth(496f,1f,9)==9,"Full-width row has all nine entries");
Check(MobileClientPresentation.MenuColumnsForWidth(272f,1f,9)==5,"Narrow menu wraps after fifth entry");
Check(MobileClientPresentation.MenuColumnsForWidth(48f,1f,9)==1,"Small menu retains one full touch column");
// Ordinary windows own only their explicit per-pointer rectangles. The companion
// run-more-panel-contract.py compiles actual window getters, including resize/dock geometry.
Screen.width=960;Screen.height=540;Screen.safeArea=new Rect(0,0,960,540);
var ordinary=new[]{typeof(FamilyDlg),typeof(FigureDlg),typeof(MailDlg),typeof(ShopDlg),typeof(StoreDlg),typeof(SysConfigDlg),typeof(SysOptionDlg),typeof(TaskDlg),typeof(TeamDlg),typeof(TradeDlg)};
foreach(var type in ordinary){
 var visibility=type.GetField(type==typeof(FamilyDlg)?"Value":"Visible");
 type.GetField("Bounds").SetValue(null,new Rect(400,120,160,180));visibility.SetValue(null,true);
 Check(!MobileClientPresentation.ModalOpen,"Ordinary window is not global modal "+type.Name);
 Check(MobileClientPresentation.IsPanelPointer(new Vector2(480,540-200)),"Ordinary window captures inside pointer "+type.Name);
 Check(!MobileClientPresentation.IsPanelPointer(new Vector2(399,540-200)),"Ordinary window releases outside pointer "+type.Name);
 var joy=MobileClientPresentation.JoystickRect.center;
 Check(MobileClientPresentation.IsJoystickRegion(new Vector2(joy.x,540-joy.y)),"Ordinary window permits outside joystick "+type.Name);
 visibility.SetValue(null,false);
 Check(!MobileClientPresentation.IsPanelPointer(new Vector2(480,540-200)),"Closed ordinary window releases rectangle "+type.Name);
}
foreach(var type in typeof(MobileClientPresentation).Assembly.GetTypes())
if(type.Namespace=="FakeFantasyOnline.Presentation.ReferenceAssets" && type.IsAbstract && type.IsSealed && type!=typeof(MobileClientPresentation))
foreach(var field in type.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static))
if(field.FieldType==typeof(bool)){
 if(Array.IndexOf(ordinary,type)>=0 && (field.Name=="Visible" || field.Name=="Value")) continue; // Covered above with inside/outside/close assertions.
 if(type==typeof(ShopDlg) && field.Name=="Picking") ShopDlg.Visible=true;
 field.SetValue(null,true);Check(MobileClientPresentation.ModalOpen,"Modal coverage "+type.Name+"."+field.Name);field.SetValue(null,false);
 if(type==typeof(ShopDlg)) ShopDlg.Visible=false;
}
InventoryDlg.SplitOpen=true;Check(MobileClientPresentation.ModalOpen,"Standalone warehouse quantity modal");InventoryDlg.SplitOpen=false;
Console.WriteLine("PASS mobile layout: "+checks+" assertions; geometry/stub-only, no Unity rendering.");
}}
