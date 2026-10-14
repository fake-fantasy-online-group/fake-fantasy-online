using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class MobileLayoutContract {
static int checks;
static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
static bool Within(Rect a,Rect b)=>a.x>=b.x-.01f&&a.y>=b.y-.01f&&a.xMax<=b.xMax+.01f&&a.yMax<=b.yMax+.01f;
static bool Overlap(Rect a,Rect b)=>a.x<b.xMax&&a.xMax>b.x&&a.y<b.yMax&&a.yMax>b.y;
static void Main(){
Check(!MobileClientPresentation.Enabled,"Desktop default unchanged");MobileClientPresentation.Simulate=true;GameHud.I=new GameHud();
foreach(var size in new[]{new[]{1280,720},new[]{812,375},new[]{2340,1080},new[]{1920,1200}}){
Screen.width=size[0];Screen.height=size[1];Screen.safeArea=new Rect(24,16,size[0]-48,size[1]-32);
Rect safe=MobileClientPresentation.SafeAreaGui;
var controls=new[]{MobileClientPresentation.JoystickRect,MobileClientPresentation.AttackRect,MobileClientPresentation.SkillRect(0),MobileClientPresentation.SkillRect(1),MobileClientPresentation.SkillRect(2),MobileClientPresentation.SkillRect(3),MobileClientPresentation.PageRect,MobileClientPresentation.MenuToggleRect,MobileClientPresentation.MenuRect,MobileClientPresentation.MinimapRect};
for(int i=0;i<controls.Length;i++){Check(Within(controls[i],safe),"Safe bounds "+i);for(int j=i+1;j<controls.Length;j++)Check(!Overlap(controls[i],controls[j]),"Control overlap "+i+","+j);}
MobileClientPresentation.ChatExpanded=false;Check(Within(MobileClientPresentation.ChatRect,safe),"Compact chat safe");
MobileClientPresentation.ChatExpanded=true;Rect chat=MobileClientPresentation.ChatRect;Check(Math.Abs(chat.center.x-safe.center.x)<.01&&Math.Abs(chat.center.y-safe.center.y)<.01,"Expanded chat centered");Check(Within(chat,safe),"Expanded chat safe");
Vector2 joy=MobileClientPresentation.JoystickRect.center;joy.y=Screen.height-joy.y;Check(MobileClientPresentation.IsJoystickRegion(joy),"Joystick region");SysMsgBox.Visible=true;Check(!MobileClientPresentation.IsJoystickRegion(joy),"Modal blocks joystick");Check(MobileClientPresentation.IsPointerOverUi(new Vector2(400,250)),"Modal blocks world");SysMsgBox.Visible=false;
object owner=new object();MobileClientPresentation.RegisterPanel(owner,safe);Check(MobileClientPresentation.IsPanelPointer(joy),"Panel registers");Check(!MobileClientPresentation.IsJoystickRegion(joy),"Panel owns pointer");MobileClientPresentation.RemovePanel(owner);Check(!MobileClientPresentation.IsPanelPointer(joy),"Panel removed");
}
foreach(var type in typeof(MobileClientPresentation).Assembly.GetTypes())
if(type.Namespace=="FakeFantasyOnline.Presentation.ReferenceAssets" && type.IsAbstract && type.IsSealed && type!=typeof(MobileClientPresentation))
foreach(var field in type.GetFields(System.Reflection.BindingFlags.Public|System.Reflection.BindingFlags.Static))
if(field.FieldType==typeof(bool)){field.SetValue(null,true);Check(MobileClientPresentation.ModalOpen,"Modal coverage "+type.Name);field.SetValue(null,false);}
Console.WriteLine("PASS mobile layout: "+checks+" assertions; geometry/stub-only, no Unity rendering.");
}}
