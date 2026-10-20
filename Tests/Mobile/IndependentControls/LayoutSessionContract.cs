using System;using System.IO;using UnityEngine;using FakeFantasyOnline.Presentation.ReferenceAssets;
class LayoutSessionContract {
 static int checks;static int geometryFailures;static void Check(bool c,string m){checks++;if(!c){if(m.StartsWith("default ")){if(geometryFailures++<30)Console.WriteLine("GEOMETRY FAIL "+m);return;}throw new Exception(m);}}
 static bool Same(Rect a,Rect b)=>Math.Abs(a.x-b.x)<.001&&Math.Abs(a.y-b.y)<.001&&Math.Abs(a.width-b.width)<.001&&Math.Abs(a.height-b.height)<.001;
 static bool Overlap(Rect a,Rect b)=>a.x<b.xMax-.01&&a.xMax>b.x+.01&&a.y<b.yMax-.01&&a.yMax>b.y+.01;
 static bool Within(Rect a,Rect b)=>a.x>=b.x-.01&&a.y>=b.y-.01&&a.xMax<=b.xMax+.01&&a.yMax<=b.yMax+.01;
 static void Main(){
 string dir=Path.Combine(Path.GetTempPath(),"mobile-session-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);Application.persistentDataPath=dir;
 try{
 MobileClientPresentation.Simulate=true;GameHud.I=new GameHud();
 Check(!MobileClientPresentation.LayoutPanelsOpen,"baseline layout gate clear");
 PlayerController.WorkTipOpen=true;Check(MobileClientPresentation.LayoutPanelsOpen,"native gather menu blocks editor admission and interrupts edit");PlayerController.WorkTipOpen=false;
 ChatUI.TypingNow=true;Check(MobileClientPresentation.LayoutPanelsOpen,"typing blocks editor");ChatUI.TypingNow=false;
 WinZ.AnyLayoutBlockingWindow=true;Check(MobileClientPresentation.LayoutPanelsOpen,"registered native window blocks editor");WinZ.AnyLayoutBlockingWindow=false;
 object owner=new object();MobileClientPresentation.RegisterPanel(owner,new Rect(10,10,20,20));Check(MobileClientPresentation.LayoutPanelsOpen,"registered panel blocks editor");MobileClientPresentation.RemovePanel(owner);
 var closedInventory=new InventoryDlg();MobileClientPresentation.RegisterPanel(closedInventory,new Rect(10,10,20,20));Check(!MobileClientPresentation.LayoutPanelsOpen,"closed inventory stale rect does not block editor");MobileClientPresentation.RemovePanel(closedInventory);
 Check(!MobileClientPresentation.LayoutPanelsOpen,"closing all panels restores admission");
 foreach(var dims in new[]{new[]{960,540},new[]{1280,720},new[]{812,375},new[]{2340,1080},new[]{1920,1200},new[]{640,360},new[]{320,240},new[]{375,812}})
 foreach(float preference in new[]{.6f,.8f,1f,1.2f,1.5f,2f}){
 Screen.width=dims[0];Screen.height=dims[1];Screen.safeArea=new Rect(24,12,dims[0]-48,dims[1]-30);UIScale.User=preference;var safe=MobileClientPresentation.SafeAreaGui;
 for(int i=0;i<14;i++){
 var r=MobileClientPresentation.ResolveControlRect((MobileControlId)i);Check(Within(r,safe),"default safe "+i+" size "+dims[0]+" pref "+preference);
 for(int j=i+1;j<14;j++)Check(!Overlap(r,MobileClientPresentation.ResolveControlRect((MobileControlId)j)),"default overlap "+i+","+j+" size "+dims[0]+" pref "+preference);
 Check(!Overlap(r,MobileClientPresentation.MinimapRect),"default map overlap "+i+" size "+dims[0]);
 Check(!Overlap(r,MobileClientPresentation.ChatRect),"default chat overlap "+i+" size "+dims[0]+" pref "+preference);
 }
 for(int k=0;k<6;k++)Check(Same(MobileClientPresentation.SkillRect(k),MobileClientPresentation.ResolveControlRect((MobileControlId)(4+k))),"draw hit skill source");
 Check(MobileClientPresentation.MountRect.xMax<MobileClientPresentation.ChatRect.x,"mount left of messages");Check(MobileClientPresentation.HealthPotionRect.y>=MobileClientPresentation.MinimapRect.yMax,"red beneath minimap");Check(MobileClientPresentation.ManaPotionRect.y>=MobileClientPresentation.MinimapRect.yMax,"blue beneath minimap");
 }
 Screen.width=960;Screen.height=540;Screen.safeArea=new Rect(0,0,960,540);UIScale.User=1;
 Rect original=MobileClientPresentation.JoystickRect;string path=Path.Combine(dir,MobileControlLayoutStore.FileName);
 Time.frameCount=1;MobileClientPresentation.BeginLayoutEdit();Check(MobileClientPresentation.LayoutEditing&&MobileClientPresentation.GameplayInputBlocked,"edit blocks game input");
 MobileClientPresentation.SetControlCenter(MobileControlId.Joystick,new Vector2(400,350));MobileClientPresentation.SetControlScale(MobileControlId.Joystick,1.4f);Rect changed=MobileClientPresentation.JoystickRect;Check(!Same(original,changed)&&!File.Exists(path),"drag resize is unsaved draft");
 MobileClientPresentation.CancelLayoutEdit();Check(!MobileClientPresentation.LayoutEditing&&MobileClientPresentation.GameplayInputBlocked&&Same(original,MobileClientPresentation.JoystickRect)&&!File.Exists(path),"cancel restores plus release shield");Time.frameCount++;Check(!MobileClientPresentation.GameplayInputBlocked,"shield ends next frame");
 MobileClientPresentation.BeginLayoutEdit();MobileClientPresentation.SetControlCenter(MobileControlId.Joystick,new Vector2(400,350));Check(MobileClientPresentation.SaveLayoutEdit(out string e)&&!MobileClientPresentation.LayoutEditing&&File.Exists(path),"save commits");string saved=File.ReadAllText(path);Rect savedRect=MobileClientPresentation.JoystickRect;
 Time.frameCount++;MobileClientPresentation.BeginLayoutEdit();MobileClientPresentation.ResetLayoutEdit();Check(Same(original,MobileClientPresentation.JoystickRect)&&File.ReadAllText(path)==saved,"reset remains draft");MobileClientPresentation.CancelLayoutEdit();Check(Same(savedRect,MobileClientPresentation.JoystickRect)&&File.ReadAllText(path)==saved,"cancel reset restores prior save");
 Time.frameCount++;MobileClientPresentation.BeginLayoutEdit();MobileClientPresentation.SetControlCenter(MobileControlId.Joystick,new Vector2(250,250));string bad=Path.Combine(dir,"file");File.WriteAllText(bad,"keep");Application.persistentDataPath=bad;Check(!MobileClientPresentation.SaveLayoutEdit(out e)&&MobileClientPresentation.LayoutEditing&&e.Length>0,"save failure stays editing");Application.persistentDataPath=dir;MobileClientPresentation.CancelLayoutEdit();Check(Same(savedRect,MobileClientPresentation.JoystickRect)&&File.ReadAllText(path)==saved,"failed save preserves saved layout");

 // Updated defaults affect unmodified controls and explicit Reset only. A saved
 // skill or group-wheel placement must survive unchanged, even at former grid positions.
 Time.frameCount++;MobileClientPresentation.BeginLayoutEdit();
 MobileClientPresentation.SetControlCenter(MobileControlId.Skill0,new Vector2(704,274));MobileClientPresentation.SetControlScale(MobileControlId.Skill0,1.25f);
 MobileClientPresentation.SetControlCenter(MobileControlId.GroupWheel,new Vector2(932,274));
 Check(MobileClientPresentation.SaveLayoutEdit(out e),"save custom skill and wheel fixture");
 Rect customSkill=MobileClientPresentation.SkillRect(0),customWheel=MobileClientPresentation.PageRect;string customFile=File.ReadAllText(path);
 var reloaded=MobileControlLayoutStore.Load(dir);
 Check(Same(customSkill,reloaded.ResolveRect(MobileControlId.Skill0,MobileClientPresentation.DefaultControlRect(MobileControlId.Skill0),MobileClientPresentation.SafeAreaGui,MobileClientPresentation.Scale)),"saved skill overrides restored arc after reload");
 Check(Same(customWheel,reloaded.ResolveRect(MobileControlId.GroupWheel,MobileClientPresentation.DefaultControlRect(MobileControlId.GroupWheel),MobileClientPresentation.SafeAreaGui,MobileClientPresentation.Scale)),"saved wheel overrides restored default after reload");
 Check(File.ReadAllText(path)==customFile,"resolving updated defaults never rewrites saved file");
 Time.frameCount++;MobileClientPresentation.BeginLayoutEdit();MobileClientPresentation.ResetLayoutEdit();
 var expectedArc=new[]{new Vector2(-177.18376f,35.76483f),new Vector2(-106.85673f,35.76483f),new Vector2(-102.74753f,-33.18696f),new Vector2(-35.82306f,-98.36820f),new Vector2(31.81393f,-166.51180f),new Vector2(37.04461f,-98.36820f)};
 for(int k=0;k<6;k++)Check((MobileClientPresentation.SkillRect(k).center-MobileClientPresentation.AttackRect.center-expectedArc[k]*MobileClientPresentation.DefaultControlScale(MobileControlId.Skill0)).sqrMagnitude<.0001f,"explicit Reset restores comfortable slot "+k);
 Check((MobileClientPresentation.PageRect.center-MobileClientPresentation.AttackRect.center-new Vector2(-99.79882f,-92.52787f)*MobileClientPresentation.DefaultControlScale(MobileControlId.GroupWheel)).sqrMagnitude<.0001f,"Reset restores comfortable wheel location");
 Check(File.ReadAllText(path)==customFile,"Reset does not write until Save");MobileClientPresentation.CancelLayoutEdit();
 Check(Same(customSkill,MobileClientPresentation.SkillRect(0))&&Same(customWheel,MobileClientPresentation.PageRect)&&File.ReadAllText(path)==customFile,"cancel Reset preserves custom skill and wheel");

 for(int i=1;i<14;i++){Time.frameCount++;MobileClientPresentation.BeginLayoutEdit();MobileClientPresentation.ResetLayoutEdit();var center=MobileClientPresentation.JoystickRect.center;MobileClientPresentation.SetControlCenter((MobileControlId)i,center);Check(MobileClientPresentation.SaveLayoutEdit(out e),"save overlapping fixture");Time.frameCount++;Check(!MobileClientPresentation.IsJoystickRegion(new Vector2(center.x,Screen.height-center.y)),"one pointer cannot steer and dispatch overlapping control "+i);}
 }finally{Directory.Delete(dir,true);}Check(geometryFailures==0,"geometry failures "+geometryFailures);Console.WriteLine("PASS independent actual presentation layout session "+checks+" assertions; no Unity/device.");
 }
}
