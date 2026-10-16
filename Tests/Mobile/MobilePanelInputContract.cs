using System;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
public static class MobilePanelInputContract
{
 static int checks;
 static void Check(bool v,string message){if(!v)throw new Exception(message);checks++;}
 static Touch At(int id,TouchPhase phase,Vector2 gui)=>new Touch{fingerId=id,phase=phase,position=new Vector2(gui.x,Screen.height-gui.y)};
 static void Frame(params Touch[] touches){Time.frameCount++;Input.touches=touches;GameHud.I.Tick();ClientInput.Refresh();}
 static void Tap(Vector2 p){Frame(At(2,TouchPhase.Began,p));Frame(At(2,TouchPhase.Ended,p));}
 static void Main(){
 GameHud.I=new GameHud();GameHud.hotSkill[0]=1;NetClient.I=new NetClient();TargetSelect.CurrentSourceForIndependentRuntime=new MonsterEntity();
 var bag=new InventoryDlg();InventoryDlg.I=bag;InventoryDlg.IsOpen=true;
 var rect=new Rect(290,10,330,350);MobileClientPresentation.RegisterPanel(bag,rect);
 var joy=MobileClientPresentation.JoystickRect.center+new Vector2(40,0);var attack=MobileClientPresentation.AttackRect.center;
 Check(!MobileClientPresentation.ModalOpen,"ordinary inventory is not global modal");
 Frame(At(1,TouchPhase.Began,joy),At(2,TouchPhase.Began,attack));
 Check(ClientInput.JoystickHeld&&ClientInput.MovementAxis.x>0,"bag open: joystick outside continues");Check(GameHud.I.Owner==2,"bag open: second attack finger owns HUD");
 Frame(At(1,TouchPhase.Stationary,joy),At(2,TouchPhase.Ended,attack));Check(CombatWorld.Attacks==1,"bag open: attack releases once");Check(ClientInput.JoystickHeld,"attack release does not stop stick");
 Frame(At(1,TouchPhase.Ended,joy));Check(!ClientInput.JoystickHeld,"stick release clears");
 Frame(At(3,TouchPhase.Began,rect.center));Check(!ClientInput.Pointer.Down&&!ClientInput.JoystickHeld&&GameHud.I.Owner==-1,"inside bag no world/HUD owner");
 Frame(At(3,TouchPhase.Moved,attack));Frame(At(3,TouchPhase.Ended,attack));Check(CombatWorld.Attacks==1,"panel origin cannot turn into attack on drag out");
 MobileClientPresentation.RegisterPanel(bag,MobileClientPresentation.AttackRect);Tap(attack);Check(CombatWorld.Attacks==1,"panel covering attack prevents clickthrough");
 MobileClientPresentation.RegisterPanel(bag,rect);Frame(At(2,TouchPhase.Began,attack));MobileClientPresentation.RegisterPanel(bag,MobileClientPresentation.AttackRect);Frame(At(2,TouchPhase.Ended,attack));Check(CombatWorld.Attacks==1&&GameHud.I.Owner==-1,"panel appearing over held attack cancels release");
 MobileClientPresentation.RegisterPanel(bag,rect);Frame(At(1,TouchPhase.Began,joy));MobileClientPresentation.RegisterPanel(bag,MobileClientPresentation.JoystickRect);Frame(At(1,TouchPhase.Stationary,joy));Check(!ClientInput.JoystickHeld,"panel appearing over stick cancels movement");
 MobileClientPresentation.RegisterPanel(bag,rect);Frame(At(1,TouchPhase.Moved,joy));Check(!ClientInput.JoystickHeld,"held canceled stick never reacquires");Frame(At(1,TouchPhase.Ended,joy));
 Frame(At(1,TouchPhase.Began,joy));Frame(At(1,TouchPhase.Canceled,joy));Check(!ClientInput.JoystickHeld,"OS cancellation clears stick");
 Frame(At(2,TouchPhase.Began,attack));GameHud.I.LoseFocus();Frame(At(2,TouchPhase.Ended,attack));Check(CombatWorld.Attacks==1&&GameHud.I.Owner==-1,"focus loss clears HUD gesture");
 Frame(At(2,TouchPhase.Began,attack));SysMsgBox.Visible=true;Frame(At(2,TouchPhase.Stationary,attack));Check(GameHud.I.Owner==-1&&!ClientInput.JoystickHeld,"true confirmation remains modal");SysMsgBox.Visible=false;Frame(At(2,TouchPhase.Ended,attack));Check(CombatWorld.Attacks==1,"modal close never revives held gesture");
 Frame(At(1,TouchPhase.Began,joy));Frame();Check(!ClientInput.JoystickHeld,"missing finger clears movement");
 MobileClientPresentation.RegisterPanel(bag,MobileClientPresentation.AttackRect);InventoryDlg.IsOpen=false;Tap(attack);MobileClientPresentation.RemovePanel(bag);Check(CombatWorld.Attacks==2,"close restores fresh gesture");
 GameHud.I.diagramOpen=true;
 Check(MobileClientPresentation.IsPanelPointer(new Vector2(400,Screen.height-150)),"diagram body owns actual point");
 Check(!MobileClientPresentation.IsPanelPointer(new Vector2(joy.x,Screen.height-joy.y)),"diagram does not own outside joystick");
 Check(!MobileClientPresentation.ModalOpen,"diagram is not global modal");
 var header=MobileClientPresentation.MapActionRect(3).center;
 GameHud.MobileDiagramRect=MobileClientPresentation.MinimapRect;
 Check(!MobileClientPresentation.IsPanelPointer(new Vector2(header.x,Screen.height-header.y)),"compact header remains reachable over diagram");
 InventoryDlg.IsOpen=true;MobileClientPresentation.RegisterPanel(bag,MobileClientPresentation.MinimapRect);
 Check(MobileClientPresentation.IsPanelPointer(new Vector2(header.x,Screen.height-header.y)),"unrelated bag still owns header overlap");
 InventoryDlg.IsOpen=false;MobileClientPresentation.RemovePanel(bag);GameHud.I.diagramOpen=false;
 Console.WriteLine("PASS actual mobile presentation + HUD + ClientInput panel routing: "+checks+" checks (Unity/platform adapters stubbed)");
 }
}
