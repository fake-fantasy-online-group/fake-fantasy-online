using System;using UnityEngine;
namespace UnityEngine {
 public enum KeyCode {F11} public static class Input {public static bool GetKeyDown(KeyCode key)=>true;public static bool GetMouseButtonDown(int b)=>true;}
 public static class GUIUtility {public static int keyboardControl;}
 public struct Color {public Color(float a,float b,float c,float d){}}
 public class SpriteRenderer {public int sortingOrder;public static implicit operator bool(SpriteRenderer s)=>s!=null;}
 public static class Time {public static float time;}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public static class MobileClientPresentation {public static bool GameplayInputBlocked;}
 public static class CharData {public static int apVersion=1,nameMode,NAMEMODE_COUNT=3,rideAnchorId,immobile;public static string[] NAMEMODE_TIPS={"a","b","c"};public static bool riding;public static int prefs;public static void SetPref(string k,int v){prefs++;}}
 public static class ChatUI {public static bool TypingNow;public static int messages;public static void Add(string c,string m,Color col,string f,object x){messages++;}}
 public static class Win05 {public static bool GmTyping;}
 public static class SysMsgBox {public static bool Visible;public static int shown;public static void Show(string s,Action a,object x){shown++;}}
 public static class CombatWorld {public static int stops;public static void StopAttack(){stops++;}}
 public struct DisplacementObservation {public float X,Y;public bool Moving;}
 public sealed partial class PlayerController {
  int _apVersion;public bool dead;public int workSkill;bool moveToTarget,followMode,sitting;int followId,pathIdx,pendHops;public bool IsMoving;SpriteRenderer sr=new SpriteRenderer();System.Collections.Generic.List<int> path=new System.Collections.Generic.List<int>();
  public int inputs,clocks,cancels,working,following,deadTicks,appearance,tint;
  void LoadFrames(){appearance++;_apVersion=CharData.apVersion;}void UpdateFrameTintForIndependentRuntime(){tint++;}void CancelMobileInput(){cancels++;}void UpdateDeadMovementForIndependentRuntime(){deadTicks++;}void UpdateAnim(float a,float b,bool c){clocks++;}void UpdateBuffAura(){clocks++;}void FollowRideAnchor(){following++;}int GroupBase()=>0;void UpdateWork(){working++;}bool OverAnyUI()=>false;void CancelWork(){}
  void HandleSitKey(){inputs++;}void UpdateLocalStallForIndependentRuntime(){clocks++;}void HandleGlowToggleKeyForIndependentRuntime(){inputs++;}void HandleTitleToggleKey(){inputs++;}void HandlePickupKey(){inputs++;}void UpdateSittingForIndependentRuntime(){clocks++;}void UpdatePointerFrame(){inputs++;}void UpdateMobileMovement(){clocks++;}void ApplyChantMovementForIndependentRuntime(){clocks++;}void ApplyApproachForIndependentRuntime(){clocks++;}void AdvanceFollowForIndependentRuntime(){clocks++;}void StopApproachForIndependentRuntime(){clocks++;}DisplacementObservation AdvanceDisplacementForIndependentRuntime(){clocks++;return default;}void AdvancePickupForIndependentRuntime(){clocks++;}bool AdvanceTransferForIndependentRuntime(){clocks++;return false;}void ProjectMovementForIndependentRuntime(){clocks++;}void PublishMovingForIndependentRuntime(DisplacementObservation s){clocks++;}
  public void Tick()=>Update();
 }
 public class FrameGateContract {
  static int checks;static void Check(bool c,string m){checks++;if(!c)throw new Exception(m);}
  static void Main(){
   foreach(bool blocked in new[]{true,false}){MobileClientPresentation.GameplayInputBlocked=blocked;CharData.nameMode=0;CharData.prefs=ChatUI.messages=SysMsgBox.shown=0;var p=new PlayerController();p.Tick();Check(p.inputs==(blocked?0:5),"all keyboard and pointer branches "+blocked);Check(p.clocks==14&&p.appearance==1&&p.tint==1,"state displacement animation continue "+blocked);Check(CharData.prefs==(blocked?0:1)&&ChatUI.messages==(blocked?0:1),"F11 blocked "+blocked);p=new PlayerController{workSkill=1};p.Tick();Check(p.working==1&&p.clocks==1&&SysMsgBox.shown==(blocked?0:1),"work update preserved rightclick gated "+blocked);}
   MobileClientPresentation.GameplayInputBlocked=true;var dead=new PlayerController{dead=true};dead.Tick();Check(dead.deadTicks==1&&dead.cancels==1&&dead.inputs==0&&dead.clocks==2,"dead system branch intact");
   CharData.rideAnchorId=99;var rider=new PlayerController();rider.Tick();Check(rider.following==1&&rider.cancels==1&&rider.inputs==0&&rider.clocks==2,"passenger follows server anchor");CharData.rideAnchorId=0;
   CharData.immobile=1;var immobile=new PlayerController();immobile.Tick();Check(immobile.cancels==1&&immobile.inputs==0&&immobile.clocks==2&&CombatWorld.stops==1,"immobile state preserved");CharData.immobile=0;
   Console.WriteLine("PASS independent actual PlayerController.Frame input gate "+checks+" assertions; other branches are counter stubs.");
  }
 }
}
