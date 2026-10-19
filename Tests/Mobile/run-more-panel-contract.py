#!/usr/bin/env python3
"""Compile production hit-test method bodies with lightweight window state, then exercise routing."""
from pathlib import Path
import os,re,subprocess,tempfile
root=Path(__file__).resolve().parents[2]
base=Path(os.environ.get('FFO_TEST_BASE', root))
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets';bp=base/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def source(f): return (p/f if (p/f).exists() else bp/f).read_text()
def body(f,signature):
 s=source(f);a=s.index(signature);b=s.index('{',a);depth=0
 for i in range(b,len(s)):
  depth+=(s[i]=='{')-(s[i]=='}')
  if not depth:return s[a:i+1]
classes={'TeamDlg':('TeamDlg.cs','internal static bool MobileContainsPointer',187,374), 'TradeDlg':('TradeDlg.cs','internal static bool MobileContainsPointer',569,426),'StoreDlg':('StoreDlg.cs','internal static bool MobileContainsPointer',423,501)}
for c,w,h in [('TaskDlg',495,397),('FigureDlg',345,476),('MailDlg',418,325),('ShopDlg',569,426),('FamilyDlg',494,325),('SysOptionDlg',192,207),('SysConfigDlg',267,445)]:
 f={'ShopDlg':'ShopDlg.Window.cs','SysConfigDlg':'SysConfigDlg.PickLayout.cs'}.get(c,c+'.cs');classes[c]=(f,'internal static bool MobileContainsPointer',w,h)
stubs=(base/'Tests/Mobile/MobileLayoutStubs.cs').read_text();extra=''
stubs=re.sub(r'public static class UIScale \{[^\n]*\}', '',stubs)
stubs=stubs.replace('public static class Mathf {', 'public static class Mathf {public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));')
if 'bool SplitOpen;' not in stubs: stubs=stubs.replace('public class InventoryDlg {', 'public class InventoryDlg {public static bool SplitOpen;')
for c,(f,sig,w,h) in classes.items():
 stubs=re.sub(r'public static class '+c+r' \{[^\n]*\}', '',stubs)
 # Settings hits share the production safe-area resolver with OnGUI. Compile
 # that dependency too; do not substitute a second layout implementation.
 helper=body(f,'private Rect MobileLayout') if c in ('SysConfigDlg','SysOptionDlg') else ''
 extra+='public class '+c+' {public static '+c+' I;public bool visible;public int _wdGen=0;private static bool PickRowOn()=>true;public static bool Visible=>I!=null&&I.visible;public float winX=float.NaN,winY;const float NW='+str(w)+'f;public static float NH='+str(h)+'f;public float Width='+str(w)+'f,Height='+str(h)+'f;public bool pickOpen,resOpen,needDock;const float RESY=362f;public static bool ComposeVisible,Picking;'+helper+body(f,sig).replace('get_Visible()', 'Visible')+'}\n'
stubs+='\nnamespace FakeFantasyOnline.Presentation.ReferenceAssets {using UnityEngine;public static class WinDrag {public static int Gen;}public static class UIScale {public static float User=1f;public static float Factor=1f;}public static class GameSettings {public static int[] ResOpts=new int[25];}'+extra+'}'
checks=''
for c in classes:
 checks+='''{
 C.I=new C();C.I.visible=true;
 Check(!MobileClientPresentation.ModalOpen,"C ordinary not modal");
 Check(MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(480,270))),"C center hit before paint");
 Check(MobileClientPresentation.IsJoystickRegion(ScreenPoint(MobileClientPresentation.JoystickRect.center)),"C outside joystick available");
 C.I.winX=400;C.I.winY=90;
 Check(MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(401,91))),"C dragged geometry");
 Check(!MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(399,89))),"C outside dragged geometry");
 C.I.visible=false;
 Check(!MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(401,91))),"C close releases hit");
 if ("C" != "StoreDlg") { C.I.winX=900;C.I.winY=500;C.I._wdGen=-1;C.I.visible=true;
 Check(MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(480,270))),"C resized center");
 Check(!MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(901,501))),"C stale dragged bounds released");
 } C.I=null;
}'''.replace('C.',c+'.').replace('new C()', 'new '+c+'()').replace('"C ', '"'+c+' ').replace('"C"', '"'+c+'"')
program='''using System;using UnityEngine;using FakeFantasyOnline.Presentation.ReferenceAssets;
class Contract {static int checks;static void Check(bool ok,string name){checks++;if(!ok)throw new Exception(name);}static Vector2 ScreenPoint(Vector2 p)=>new Vector2(p.x,Screen.height-p.y);static void Main(){Screen.width=960;Screen.height=540;Screen.safeArea=new Rect(0,0,960,540);MobileClientPresentation.Simulate=true;
'''+checks+'''
MailDlg.ComposeVisible=true;Check(MobileClientPresentation.ModalOpen,"mail composition modal");MailDlg.ComposeVisible=false;
ShopDlg.I=new ShopDlg{visible=true};ShopDlg.Picking=true;Check(MobileClientPresentation.ModalOpen,"shop input modal");ShopDlg.I=null;ShopDlg.Picking=false;
InventoryDlg.SplitOpen=true;Check(MobileClientPresentation.ModalOpen,"warehouse quantity input modal without bag");InventoryDlg.SplitOpen=false;
SysMsgBox.Visible=true;Check(MobileClientPresentation.ModalOpen,"confirmation modal");SysMsgBox.Visible=false;
PasswdDlg.Visible=true;Check(MobileClientPresentation.ModalOpen,"password modal");PasswdDlg.Visible=false;
SysConfigDlg.I=new SysConfigDlg{visible=true,winX=400,winY=90,resOpen=true};Check(MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(510,50))),"resolution dropdown extends above main panel");SysConfigDlg.I.resOpen=false;Check(!MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(510,50))),"closed dropdown releases hit");SysConfigDlg.I=null;
MailDlg.I=new MailDlg{visible=true,winX=400,winY=90,Width=500,Height=400};Check(MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(890,480))),"mail uses actual artwork dimensions");MailDlg.I=null;
StoreDlg.I=new StoreDlg{visible=true,needDock=true};Check(MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(56,21))),"warehouse predicts pending dock");Check(!MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(600,270))),"warehouse avoids stale centered hit");StoreDlg.I=null;
UIScale.Factor=.5f;TaskDlg.I=new TaskDlg{visible=true};Check(MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(480,270))),"scaled center");Check(!MobileClientPresentation.IsPanelPointer(ScreenPoint(new Vector2(350,160))),"scaled outside");TaskDlg.I=null;
Console.WriteLine("PASS additional ordinary panel routing: "+checks+" checks");}}
'''
with tempfile.TemporaryDirectory() as temp:
 t=Path(temp);(t/'Stubs.cs').write_text(stubs);(t/'Contract.cs').write_text(program);(t/'framework.rsp').write_text('\n'.join('-r:'+str(f) for f in Path(os.environ['FFO_FRAMEWORK_DIR']).glob('*.dll')))
 cmd=[os.environ['FFO_MONO_ROOT']+'/bin/mono',os.environ['FFO_CSC'],'-noconfig','-nostdlib+','@'+str(t/'framework.rsp'),'-nologo','-langversion:9.0','-out:'+str(t/'Contract.exe'),str(p/'MobileClientPresentation.cs'),str(p/'MobileClientPresentation.Layout.cs'),str(p/'MobileControlLayout.cs'),str(p/'MobileControlLayoutStore.cs'),str(p/'MobileFixedAction.cs'),str(bp/'MobileGroupWheel.cs'),str(t/'Stubs.cs'),str(t/'Contract.cs')]
 subprocess.run(cmd,check=True);subprocess.run([os.environ['FFO_MONO_ROOT']+'/bin/mono',str(t/'Contract.exe')],check=True)
