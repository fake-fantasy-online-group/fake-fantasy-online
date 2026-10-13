#!/usr/bin/env python3
"""Execute production icon resolution/cache/signature methods. No Unity/Player."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
def method(file, signature):
    s=(p/file).read_text(); start=s.index(signature); i=s.index('{',start); depth=1; j=i+1
    while depth:
        depth += (s[j]=='{')-(s[j]=='}'); j+=1
    return s[start:j]
source='''
using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Protocol;
namespace FakeFantasyOnline.Application { public interface ISkillBookReader {} }
class Texture2D { public static implicit operator bool(Texture2D t)=>t!=null; }
class ResLoader { public static ResLoader I; public bool ready; public int reads; public string lastKey; public byte[] ReadRes(string key){lastKey=key;reads++; return ready?new byte[]{1}:null;} public static implicit operator bool(ResLoader r)=>r!=null; }
class DimgDecoder { public class Result { public bool ok=true; public Texture2D tex=new Texture2D(); public int scale=1; } public static Result Decode(byte[] b,int offset)=>new Result(); }
class TexScale { public static void Register(Texture2D t,int scale){} }
class AuthoritativeProfessionSkill { public static bool enabled; public static Win05.SkInfo info; public static bool TryInfo(int id,out Win05.SkInfo value){value=info;return enabled;} }
class Win05 {
 public class SkInfo { public int icon; }
 static Dictionary<int,SkInfo> skInfo=new Dictionary<int,SkInfo>{{10307,new SkInfo{icon=5039}}};
 static void LoadStatic(){}
 SKILL
}
class CharData { public class BuffView { public int skillId,iconId,remainSec; public string name,desc; public bool good; public sbyte posMode; public float recvTime; } public static List<BuffView> buffs; public static bool invisible,doubleExp; public static byte immobile; }
class Receive { RESOLVE RECEIVE }
class CustomMaps {public static bool IsCustom(int id)=>false;}
class GameBootstrap {public static int CurMapId;}
class BuffHud {Texture2D LoadStatusIcon(int id)=>GameHud.BuffIcon(id); FINITE public Texture2D Load(CharData.BuffView row)=>LoadFiniteStatusIcon(row);}
class GameHud { static Dictionary<int,Texture2D> buffIcons=new Dictionary<int,Texture2D>(); BUFF }
class Time { public static float time,realtimeSinceStartup; }
class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); }
class MonsterEntity { public struct StatusIcon { public int status,icon,remainMs; public bool good; }
 List<StatusIcon> sicons; float[] siExpire; void ApplyStatusIcons(bool retryMissingIcons=false){} public int FirstIcon()=>sicons.Count>0?sicons[0].icon:0;
 SIGNATURE SETTER }
class Test {
 static int checks; static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks++;}
 static void Main(string[] args){
  AuthoritativeProfessionSkill.enabled=true;
  Check(Win05.SkillIconId(10307)==5039,"pending contract retains installed art");
  AuthoritativeProfessionSkill.info=new Win05.SkInfo{icon=0};
  Check(Win05.SkillIconId(10307)==5039,"zero contract retains installed art");
  AuthoritativeProfessionSkill.info.icon=7069;
  Check(Win05.SkillIconId(10307)==7069,"explicit server art wins");
  Check(Win05.SkillIconId(9999)==7069,"server resource id used literally");
  AuthoritativeProfessionSkill.enabled=false;
  Check(Win05.SkillIconId(9999)==0,"unknown skill is not treated as resource id");
  CharData.buffs=new List<CharData.BuffView>{new CharData.BuffView{skillId=12005,iconId=7069,name="衰弱"}};
  Check(Receive.ResolveBuffIcon(12005,"衰弱",0)==0,"authored zero clears previous buff art");
  Check(Receive.ResolveBuffIcon(12005,"衰弱",5001)==5001,"positive replacement wins");
  Check(Receive.ResolveBuffIcon(12005,"另一状态",0)==0,"no inheritance across distinct statuses sharing source");
  Check(Receive.ResolveBuffIcon(10307,"强化",0)==0,"authored zero never borrows source skill art");
  CharData.buffs.Clear();Check(Receive.ResolveBuffIcon(12005,"衰弱",0)==0,"empty snapshot drops previous icon state");
  Check(GameHud.BuffIcon(7069)==null,"resource owner not yet ready");
  ResLoader.I=new ResLoader();Check(GameHud.BuffIcon(7069)==null,"asset not yet ready");
  ResLoader.I.ready=true;var icon=GameHud.BuffIcon(7069);Check(icon!=null,"same icon recovers after resource initialization");
  int reads=ResLoader.I.reads;Check(GameHud.BuffIcon(7069)==icon && ResLoader.I.reads==reads,"successful texture is cached");
  var rows=new List<MonsterEntity.StatusIcon>{new MonsterEntity.StatusIcon{status=1006,icon=0,good=false}};
  int first=MonsterEntity.StatusIconSignature(rows);rows[0]=new MonsterEntity.StatusIcon{status=1006,icon=7069,good=false};
  int second=MonsterEntity.StatusIconSignature(rows);Check(first!=second,"same status new icon invalidates baked texture");
  rows[0]=new MonsterEntity.StatusIcon{status=1006,icon=7069,good=true};Check(second!=MonsterEntity.StatusIconSignature(rows),"polarity invalidates border");
  Check(MonsterEntity.StatusIconSignature(new List<MonsterEntity.StatusIcon>())==0,"empty status signature");
  var monster=new MonsterEntity();monster.SetStatusIcons(new List<MonsterEntity.StatusIcon>{new MonsterEntity.StatusIcon{status=1006,icon=7069}});
  monster.SetStatusIcons(new List<MonsterEntity.StatusIcon>{new MonsterEntity.StatusIcon{status=1006,icon=0}});Check(monster.FirstIcon()==0,"monster authored zero clears previous artwork");
  monster.SetStatusIcons(new List<MonsterEntity.StatusIcon>{new MonsterEntity.StatusIcon{status=1006,icon=5038}});Check(monster.FirstIcon()==5038,"monster positive replacement wins");
  monster.SetStatusIcons(new List<MonsterEntity.StatusIcon>());Check(monster.FirstIcon()==0,"monster empty snapshot clears");
  var hud=new BuffHud();int beforeReads=ResLoader.I.reads;
  Check(hud.Load(new CharData.BuffView{skillId=10307,iconId=0})==null && ResLoader.I.reads==beforeReads,"HUD authored zero makes no source-skill resource lookup");
  Check(Receive.ResolveBuffIcon(1018,"使毒",7037)==7037,"native carrier uses7037 rather than poison5030");
  Check(Receive.ResolveBuffIcon(1036,"毒牙",10179)==10179,"exact level artwork survives receipt");
  if(args.Length>0) {
   byte[] wire=System.IO.File.ReadAllBytes(args[0]);var reader=new NetClient.Buf(wire);var receive=new Receive();
   Check(receive.ReceiveBuffState(reader,()=>12f,()=>false),"actual monster poison packet published");
   Check(reader.Remaining==0 && CharData.buffs.Count==1,"actual wire packet read fully");
   var row=CharData.buffs[0];Check(row.skillId==1001 && row.iconId==5030 && row.name=="中毒" && !row.good && row.remainSec==10,"actual server native poison identity/resource/polarity/duration");
   Check(hud.Load(row)!=null && ResLoader.I.lastKey=="item/item/item5030.gso","actual receipt reaches exact original HUD resource path");
   Array.Clear(wire,6,4);receive.ReceiveBuffState(new NetClient.Buf(wire),()=>13f,()=>false);
   Check(CharData.buffs[0].iconId==0 && hud.Load(CharData.buffs[0])==null,"zero update cannot retain last packet artwork");
  }
  Console.WriteLine("PASS "+checks+" production status icon contracts");
 }
}
'''
for key,file,sig in [('SKILL','Win05.SkillData.cs','public static int SkillIconId('),('RESOLVE','ReferenceWorldReceiveOwner.BuffState.cs','internal static int ResolveBuffIcon('),('RECEIVE','ReferenceWorldReceiveOwner.BuffState.cs','public bool ReceiveBuffState('),('FINITE','ReferenceFirstWorldBuffHud.cs','private Texture2D LoadFiniteStatusIcon('),('BUFF','GameHud.BuffIcon.cs','public static Texture2D BuffIcon('),('SIGNATURE','MonsterEntity.StatusIcons.cs','internal static int StatusIconSignature('),('SETTER','MonsterEntity.StatusIcons.cs','public void SetStatusIcons(')]:source=source.replace(key,method(file,sig))
mono=Path(os.environ['FFO_MONO_ROOT'])/'bin/mono'; csc=Path(os.environ['FFO_CSC']); fw=Path(os.environ['FFO_FRAMEWORK_DIR'])
with tempfile.TemporaryDirectory(prefix='status-icon-') as d:
    d=Path(d); (d/'test.cs').write_text(source)
    subprocess.run([str(mono),str(csc),'-nologo','-noconfig','-nostdlib+','-out:'+str(d/'test.exe')]+['-r:'+str(fw/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]+[str(d/'test.cs'), str(root/'Assets/FakeFantasyOnline/Runtime/Application/IWorldBuffReceiveOwner.cs'), str(root/'Assets/FakeFantasyOnline/Runtime/Protocol/NetClient.Buf.Read.cs'), str(root/'Assets/FakeFantasyOnline/Runtime/Protocol/NetClient.Buf.BuffState.cs')],check=True)
    wire=os.environ.get('FFO_STATUS_ICON_WIRE_FIXTURE')
    subprocess.run([str(mono),str(d/'test.exe')]+([wire] if wire else []),check=True)
assert 'ResolveBuffIcon(row.skillId, row.name, row.iconId)' in (p/'ReferenceWorldReceiveOwner.BuffState.cs').read_text()
assert 'ResolveBuffIcon(skillId, name, iconId)' in (p/'ReferenceWorldReceiveOwner.cs').read_text()
print('PASS both native and compatibility buff receipt paths use icon resolver')
