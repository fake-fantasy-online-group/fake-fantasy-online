using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
public static class MobileMinimapCloseContract
{
 static int checks;
 static void Check(bool value,string text){if(!value)throw new Exception(text);checks++;}
 static void Frame(params Touch[] touches){Time.frameCount++;Input.touches=touches;GameHud.I.Tick();ClientInput.Refresh();}
 static Touch At(int id,TouchPhase phase,float x,float y)=>new Touch{fingerId=id,phase=phase,position=new Vector2(x,1000-y)};
 static void Tap(float x,float y){Frame(At(12,TouchPhase.Began,x,y));Frame(At(12,TouchPhase.Ended,x,y));}
 public static void Main()
 {
  GameHud.I=new GameHud();GameHud.hotSkill[0]=1;NetClient.I=new NetClient();
  for(int n=0;n<3;n++){
   Tap(850,100);Check(GameHud.I.diagramOpen,"compact map opens expanded diagram");
   Frame();
   Tap(940,20);Check(!GameHud.I.diagramOpen,"existing header toggle closes expanded diagram after idle frame");
   Check(GameHud.I.Owner==-1,"close releases touch owner");
  }
  Tap(850,100);Check(GameHud.I.diagramOpen,"diagram reopens without restart");
  Tap(820,20);Check(GameHud.ZoomInCount==1,"plus while diagram open dispatches exactly once");
  Frame();Check(GameHud.ZoomInCount==1,"idle never duplicates zoom");
  Tap(860,20);Check(GameHud.ZoomOutCount==1,"minus dispatches once");
  Frame(At(12,TouchPhase.Began,940,20));Frame(At(12,TouchPhase.Canceled,940,20));
  Check(GameHud.I.diagramOpen,"canceled close keeps map open");
  Frame(At(12,TouchPhase.Began,940,20));Frame(At(12,TouchPhase.Ended,500,500));
  Check(GameHud.I.diagramOpen,"close release outside does not toggle");
  Frame(At(10,TouchPhase.Began,180,900));
  Check(ClientInput.JoystickHeld,"joystick outside diagram remains responsive");
  Frame(At(10,TouchPhase.Ended,180,900));
  Tap(880,20);Check(!GameHud.I.diagramOpen&&WorldMapDlg.Opens==1,"world-map route closes diagram exactly once");
  Tap(850,100);Check(GameHud.I.diagramOpen,"diagram opens again after world-map transition");
  Tap(940,20);Check(!GameHud.I.diagramOpen,"map closes again without restart");
  GameHud.I.showHotbar=false;
  Tap(850,100);Check(GameHud.I.diagramOpen,"hidden hotbar does not disable visible minimap opener");
  Tap(940,20);Check(!GameHud.I.diagramOpen,"hidden hotbar does not disable minimap close");
  Console.WriteLine("PASS minimap close/reopen: "+checks+" checks");
 }
}
