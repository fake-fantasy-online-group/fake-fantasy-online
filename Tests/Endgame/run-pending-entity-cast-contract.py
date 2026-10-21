#!/usr/bin/env python3
"""Execute production request, Update pending stage and target dispatch together.
Thin scene/Unity/skill metadata doubles; actual UseSkill invokes bound network writer.
"""
from pathlib import Path
import os,subprocess,tempfile
root=Path(__file__).resolve().parents[2];p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def method(file,sig):
 s=(p/file).read_text()
 if file=='CombatWorld.TargetCast.cs' and os.environ.get('FFO_CAST_BASELINE'):
  s=subprocess.check_output(['git','show',os.environ['FFO_CAST_BASELINE']+':'+str((p/file).relative_to(root))],cwd=root,text=True)
 start=s.index(sig);i=s.index('{',start)+1;depth=1
 while depth:depth+=(s[i]=='{')-(s[i]=='}');i+=1
 return s[start:i]
request=method('CombatWorld.SkillRequests.cs','public static void RequestSkill(')
cast=method('CombatWorld.TargetCast.cs','private void CastAtTarget(')
u=(p/'CombatWorld.Update.cs').read_text();tick=u[u.index('        if (queuedSkillId > 0'):u.index('        // One target-kind split')]
use=method('NetClient.SkillCommands.cs','public void UseSkill(')
useat=method('NetClient.SkillCommands.cs','public void UseSkillAt(')
ground=method('CombatWorld.GroundRequest.cs','public static void RequestGroundCast(')
advance=method('CombatWorld.GroundRequest.cs','private void AdvanceGroundCastForIndependentRuntime()')
h=r'''
using System;using UnityEngine;
namespace UnityEngine{class Time{public static float time;}class MonoBehaviour{}struct Vector3{public Vector3(float x,float y,float z){}} }
class Behavior{public int CastDistance=150;public string InputMode="entity";public bool PointRequired;}
class Monster {public int id=42;public bool IsDead;}
class RemotePlayer{public int id;public bool IsDead;}
class TargetSelect{public static object Current,CurrentSourceForIndependentRuntime;}
class SkillTargeting{public static string ValidateTarget(int id,object x)=>null;}
class Win05{public class SkInfo{public int canFriendly;}public static SkInfo SkillInfo(int x)=>null;}
class UIScale{public static float Factor=1;}
class CharData{public static int race;}
class GameBootstrap{public static void FlushMoveReport(){}}
class PlayerController{public static PlayerController I;public float worldW=1000,worldH=1000,ppu=100;public void BreakSit(){}public void StopMove(){}}
class AuthoritativeProfessionSkill{public static Behavior B=new Behavior();public static bool Learned=true,Friendly;public static int FriendlyID;public static bool Uses(int id)=>true;public static bool LearnedActive(int id)=>Learned;public static Behavior Current(int id)=>B;public static void Waiting(){}public static bool TryFriendlyOrSelf(int id,out int f,out string err){f=FriendlyID;err=null;return Friendly;}public static string ValidateTarget(int id,Monster m)=>m.IsDead?"dead":null;}
class NetClient{public static NetClient I=new NetClient();public static int PendingCastSkill;public static Vector3? PendingCastGround;public Action<int,int> writeSkill;public Action<int,int,int,int> writeSkillAt;public Action<string,float> showTip=(s,f)=>{};bool LocalNotEnoughMp(int s)=>false;
__USE__
__USEAT__}
class CombatWorld{
 static CombatWorld I;bool pluginResumingQueuedSkill,autoAttacking;int pluginSkillIntentRevision,queuedSkillId,pendSkillId;float castPauseUntil,queuedRangePx,pendSkillRangeU,pendAwaitUntil,ppu=100;RemotePlayer pvpTarget;Monster target;float distance=1;int pendGroundSkill,pendGroundGx,pendGroundGy;float pendGroundRangeU,pendGroundXU,pendGroundYU,pendGroundUntil,worldW=1000,worldH=1000;
 Action<string,float> skillTipForIndependentRuntime=(s,f)=>{};static void Ensure(){if(I==null)I=new CombatWorld();}
 static bool TargetWorldPx(out int x,out int y){x=y=0;return false;}__GROUND__
 bool TargetWithin(float r)=>distance<=r;void ResumeQueuedSkillForPlugins(int s,float r)=>RequestSkill(s,r);bool PlayerWithinGround(float r)=>distance<=r;__ADVANCE__
 public static void NotifyCast(float s){I.castPauseUntil=Time.time+s;}
__REQUEST__
__CAST__
void Tick(){__TICK__}
static void Check(bool b,string x){if(!b)throw new Exception(x);}
static void Main(){int sends=0,lastTarget=0;NetClient.I.writeSkill=(s,t)=>{sends++;lastTarget=t;};NetClient.I.writeSkillAt=(s,x,y,t)=>{sends++;lastTarget=t;};
for(int profession=0;profession<5;profession++){
 CharData.race=profession;I=new CombatWorld{target=new Monster()};Time.time=0;sends=0;RequestSkill(10002,150);Check(sends==0&&I.pendSkillId==10002,"UI request queued");I.Tick();Check(sends==1&&lastTarget==42&&I.pendSkillId==0,"in-range request must send once, not requeue then clear");I.Tick();Check(sends==1,"no duplicate");
 I=new CombatWorld{target=new Monster(),distance=4};sends=0;RequestSkill(10002,150);I.Tick();Check(sends==0,"out of range waits");I.distance=1;I.Tick();Check(sends==1,"approach then send");
 I=new CombatWorld{target=new Monster(),castPauseUntil=2};sends=0;Time.time=0;RequestSkill(10002,150);I.Tick();Check(sends==0,"busy queues");Time.time=2;I.Tick();Check(sends==1,"busy expiry replays then sends");
 I=new CombatWorld{target=new Monster{IsDead=true}};sends=0;RequestSkill(10002,150);I.Tick();Check(sends==0,"invalid target preserved");
 I=new CombatWorld{target=new Monster()};sends=0;AuthoritativeProfessionSkill.Learned=false;RequestSkill(10002,150);I.Tick();Check(sends==0,"unlearned preserved");AuthoritativeProfessionSkill.Learned=true;
}
I=new CombatWorld();sends=0;AuthoritativeProfessionSkill.Friendly=true;AuthoritativeProfessionSkill.FriendlyID=7;RequestSkill(10301,150);I.Tick();Check(sends==1&&lastTarget==7,"friendly direct request once");
I=new CombatWorld();sends=0;AuthoritativeProfessionSkill.FriendlyID=0;RequestSkill(10301,150);I.Tick();Check(sends==1&&lastTarget==0,"self request once");AuthoritativeProfessionSkill.Friendly=false;
I=new CombatWorld{distance=4};sends=0;AuthoritativeProfessionSkill.B.InputMode="point";AuthoritativeProfessionSkill.B.PointRequired=true;RequestGroundCast(10402,100,100,150);I.Tick();Check(sends==0,"ground out of range waits");I.distance=1;I.Tick();Check(sends==1&&lastTarget==0,"ground approach then actual point writer");I.Tick();Check(sends==1,"ground does not double send");
Console.WriteLine("PASS five professions: production RequestSkill -> Update pending stage -> CastAtTarget -> UseSkill bound writer; in-range, approach, pause/replay, single send, dead target, unlearned");}
}
'''
for k,v in [('REQUEST',request),('CAST',cast),('TICK',tick),('USE',use),('USEAT',useat),('GROUND',ground),('ADVANCE',advance)]:h=h.replace('__'+k+'__',v)
fw=Path(os.environ['FFO_FRAMEWORK_DIR']);mono=os.environ['FFO_MONO'];csc=os.environ['FFO_CSC']
with tempfile.TemporaryDirectory(prefix='pending-entity-') as d:
 d=Path(d);(d/'Test.cs').write_text(h);subprocess.run([mono,csc,'-nologo','-noconfig','-nostdlib+']+['-r:'+str(fw/n) for n in ['mscorlib.dll','System.dll','System.Core.dll']]+['-out:'+str(d/'Test.exe'),str(d/'Test.cs')],check=True);subprocess.run([mono,str(d/'Test.exe')],check=True)

# Both real desktop key and mouse-release paths call the same admission method.
hotbar=(p/'ReferenceFirstWorldHotbar.cs').read_text()
assert 'GameHud.UseHotSlot(keySlot)' in hotbar and 'GameHud.UseHotSlot(slotIndex)' in hotbar
admission=(p/'GameHud.HotbarUseAdmission.cs').read_text()
assert 'CombatWorld.RequestSkill(skillId, range)' in admission
print('PASS F-key/mouse common admission wiring; friendly/self direct and ground point paths remain distinct')
