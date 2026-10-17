using System;
using System.Reflection;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class MobileSettingsScaleContract {
 static int checks;
 static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance;
 static void Check(bool b,string label){checks++;if(!b)throw new Exception(label);}
 static bool Near(float a,float b)=>Math.Abs(a-b)<.001f;
 static T Get<T>(string n)=>(T)typeof(SysConfigDlg).GetField(n,Flags).GetValue(SysConfigDlg.I);
 static void Set(string n,object v)=>typeof(SysConfigDlg).GetField(n,Flags).SetValue(SysConfigDlg.I,v);
 static void Frame(EventType type=EventType.Repaint,Vector2 point=default(Vector2)){
  Input.mousePosition=new Vector2(point.x,Screen.height-point.y);
  Event.current=new Event{type=type,mousePosition=point};
  typeof(SysConfigDlg).GetMethod("OnGUI",Flags).Invoke(SysConfigDlg.I,null);
 }
 static Rect Local(float x,float y,float w,float h){float s=Get<float>("s");return new Rect(Get<float>("winX")+x*s,Get<float>("winY")+y*s,w*s,h*s);}
 static float Extension=>CharData.petSkills==null?0:26;
 static void Open(){SysConfigDlg.I=new SysConfigDlg();SysConfigDlg.Toggle();Frame();}
 static bool Inside(Rect outer,Rect inner)=>inner.x>=outer.x-.001f&&inner.y>=outer.y-.001f&&inner.xMax<=outer.xMax+.001f&&inner.yMax<=outer.yMax+.001f;
 static void CheckPanel(){
  Rect safe=MobileClientPresentation.SafeAreaGui;
  Check(Inside(safe,Local(0,0,267,445+Extension)),"whole artwork fits safe area");
  foreach(Rect control in new[]{Local(31,419+Extension,90,22),Local(167,419+Extension,70,22),Local(241,6,20,19),Local(100,325,146,14)}){
   Check(Inside(safe,control),"Apply/Cancel/Close/slider reachable");
   Check(SysConfigDlg.MobileContainsPointer(control.center),"physical hit shares rendered transform");
   Input.mousePosition=new Vector2(control.center.x,Screen.height-control.center.y);
   Check(SysConfigDlg.MouseOver(),"legacy mouse hit shares mobile transform");
  }
 }
 static void Main(){
  foreach(var size in new[]{(640,360),(960,540),(1280,720),(2400,1080),(360,640),(320,480)})
  foreach(float user in new[]{.6f,1f,2f}) foreach(bool pet in new[]{false,true}){
   Screen.width=size.Item1;Screen.height=size.Item2;Screen.safeArea=new Rect(17,11,size.Item1-49,size.Item2-37);
   UIScale.User=user;CharData.petSkills=pet?new[]{new CharData.PetSkill{id=13084,level=1}}:null;
   Open();CheckPanel();
   // Native title dragging cannot strand the fitted panel or bottom actions.
   Frame(EventType.MouseDown,Local(40,15,1,1).center);
   Frame(EventType.MouseDrag,new Vector2(-500,-500));Frame(EventType.MouseUp);Frame();CheckPanel();
   Frame(EventType.MouseDown,Local(40,15,1,1).center);
   Frame(EventType.MouseDrag,new Vector2(Screen.width+500,Screen.height+500));Frame(EventType.MouseUp);Frame();CheckPanel();
   // Safe area and user preference changes while open use the current transform on hits and draw.
   Screen.safeArea=new Rect(29,19,size.Item1-61,size.Item2-53);UIScale.User=user==2?.6f:2f;Frame();CheckPanel();
   Set("resOpen",true);Frame();Check(SysConfigDlg.MobileContainsPointer(Local(104,292,122,68).center),"resolution dropdown owns fitted point");Set("resOpen",false);
   if(pet){Set("pickOpen",true);Frame();Check(SysConfigDlg.MobileContainsPointer(Local(104,303,122,108).center),"pet dropdown owns fitted point");Set("pickOpen",false);}
   Frame();float before=UIScale.User;
   Rect slider=Local(100,325,146,14);
   Frame(EventType.MouseDown,new Vector2(slider.x+1,slider.center.y));Check(Near(Get<float[]>("sndVal")[1],0),"tap reaches minimum without drag");
   Frame(EventType.MouseUp);Check(Get<int>("dragSlider")==-1,"release ends slider capture");
   Frame(EventType.MouseDown,new Vector2(slider.xMax-1,slider.center.y));Check(Near(Get<float[]>("sndVal")[1],100),"tap reaches maximum without drag");
   Frame(EventType.MouseDrag,new Vector2(slider.center.x,slider.center.y));Check(Near(Get<float[]>("sndVal")[1],50),"drag maps center correctly");
   Frame(EventType.MouseUp);Frame(EventType.MouseDrag,new Vector2(slider.x,slider.center.y));Check(Near(Get<float[]>("sndVal")[1],50),"released drag does not mutate value");
   Frame(EventType.MouseDown,Local(167,419+Extension,70,22).center);Check(!SysConfigDlg.Visible&&Near(UIScale.User,before),"Cancel dismisses without saving scale");
   SysConfigDlg.Toggle();Frame();Check(Near(Get<float[]>("sndVal")[1],(before-.6f)/1.4f*100),"Cancel reloads persisted scale");
   foreach(float end in new[]{0f,100f}){
    slider=Local(100,325,146,14);Frame(EventType.MouseDown,new Vector2(end==0?slider.x+1:slider.xMax-1,slider.center.y));Frame(EventType.MouseUp);
    Frame(EventType.MouseDown,Local(31,419+Extension,90,22).center);
    Check(!SysConfigDlg.Visible&&Near(UIScale.User,end==0?.6f:2f),"Apply persists exact supported endpoint and dismisses");
    SysConfigDlg.Toggle();Frame();CheckPanel();
   }
  }
  // Orientation changes while this same window stays open invalidate its old origin.
  Screen.width=360;Screen.height=640;Screen.safeArea=new Rect(9,20,340,600);Frame();CheckPanel();
  Screen.width=640;Screen.height=360;Screen.safeArea=new Rect(20,9,600,340);Frame();CheckPanel();
  foreach(int audio in new[]{0,2}){
   Rect slider=Local(100,audio==0?305:345,146,14);
   Frame(EventType.MouseDown,new Vector2(slider.x+1,slider.center.y));Check(Near(Get<float[]>("sndVal")[audio],1),"audio retains lower endpoint");Frame(EventType.MouseUp);
   Frame(EventType.MouseDown,new Vector2(slider.xMax-1,slider.center.y));Check(Near(Get<float[]>("sndVal")[audio],100),"audio retains upper endpoint");Frame(EventType.MouseUp);
  }
  MobileClientPresentation.Enabled=false;Screen.width=1280;Screen.height=720;Screen.safeArea=new Rect(70,20,1000,600);CharData.petSkills=null;UIScale.User=2;Open();
  Check(Near(Get<float>("s"),UIScale.Factor),"desktop scale unchanged");Check(Near(Get<float>("winX"),(Screen.width-267*UIScale.Factor)/2),"desktop full screen center unchanged");
  Rect desktopSlider=Local(100,325,146,14);float old=Get<float[]>("sndVal")[1];Frame(EventType.MouseDown,new Vector2(desktopSlider.x+1,desktopSlider.center.y));Check(Near(Get<float[]>("sndVal")[1],old),"desktop initial press behavior retained");Frame(EventType.MouseDrag,new Vector2(desktopSlider.x+1,desktopSlider.center.y));Check(Near(Get<float[]>("sndVal")[1],1),"desktop range retained");
  Console.WriteLine("PASS settings fit, actual OnGUI/input/Apply/Cancel: "+checks+" checks (Unity/service adapters stubbed)");
 }
}
