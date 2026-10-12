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
class Texture2D { public static implicit operator bool(Texture2D t)=>t!=null; }
class ResLoader { public static ResLoader I; public bool ready; public int reads; public byte[] ReadRes(string key){reads++; return ready?new byte[]{1}:null;} public static implicit operator bool(ResLoader r)=>r!=null; }
class DimgDecoder { public class Result { public bool ok=true; public Texture2D tex=new Texture2D(); public int scale=1; } public static Result Decode(byte[] b,int offset)=>new Result(); }
class TexScale { public static void Register(Texture2D t,int scale){} }
class AuthoritativeProfessionSkill { public static bool enabled; public static Win05.SkInfo info; public static bool TryInfo(int id,out Win05.SkInfo value){value=info;return enabled;} }
class Win05 {
 public class SkInfo { public int icon; }
 static Dictionary<int,SkInfo> skInfo=new Dictionary<int,SkInfo>{{10307,new SkInfo{icon=5039}}};
 static void LoadStatic(){}
 SKILL
}
class CharData { public class BuffView { public int skillId,iconId; public string name; } public static List<BuffView> buffs; }
class Receive { RESOLVE }
class GameHud { static Dictionary<int,Texture2D> buffIcons=new Dictionary<int,Texture2D>(); BUFF }
class Time { public static float time; }
class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); }
class MonsterEntity { public struct StatusIcon { public int status,icon,remainMs; public bool good; }
 List<StatusIcon> sicons; float[] siExpire; void ApplyStatusIcons(){} public int FirstIcon()=>sicons.Count>0?sicons[0].icon:0;
 SIGNATURE SETTER }
class Test {
 static int checks; static void Check(bool ok,string msg){if(!ok)throw new Exception(msg);checks++;}
 static void Main(){
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
  Check(Receive.ResolveBuffIcon(12005,"衰弱",0)==7069,"zero refresh preserves same buff art");
  Check(Receive.ResolveBuffIcon(12005,"衰弱",5001)==5001,"positive replacement wins");
  Check(Receive.ResolveBuffIcon(12005,"另一状态",0)==0,"no inheritance across distinct statuses sharing source");
  Check(Receive.ResolveBuffIcon(10307,"强化",0)==5039,"first packet maps actual skill id");
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
  monster.SetStatusIcons(new List<MonsterEntity.StatusIcon>{new MonsterEntity.StatusIcon{status=1006,icon=0}});Check(monster.FirstIcon()==7069,"monster zero refresh preserves artwork");
  monster.SetStatusIcons(new List<MonsterEntity.StatusIcon>{new MonsterEntity.StatusIcon{status=1006,icon=5038}});Check(monster.FirstIcon()==5038,"monster positive replacement wins");
  monster.SetStatusIcons(new List<MonsterEntity.StatusIcon>());Check(monster.FirstIcon()==0,"monster empty snapshot clears");
  Console.WriteLine("PASS "+checks+" production status icon contracts");
 }
}
'''
for key,file,sig in [('SKILL','Win05.SkillData.cs','public static int SkillIconId('),('RESOLVE','ReferenceWorldReceiveOwner.BuffState.cs','internal static int ResolveBuffIcon('),('BUFF','GameHud.BuffIcon.cs','public static Texture2D BuffIcon('),('SIGNATURE','MonsterEntity.StatusIcons.cs','internal static int StatusIconSignature('),('SETTER','MonsterEntity.StatusIcons.cs','public void SetStatusIcons(')]:source=source.replace(key,method(file,sig))
mono=Path(os.environ['FFO_MONO_ROOT'])/'bin/mono'; csc=Path(os.environ['FFO_CSC']); fw=Path(os.environ['FFO_FRAMEWORK_DIR'])
with tempfile.TemporaryDirectory(prefix='status-icon-') as d:
    d=Path(d); (d/'test.cs').write_text(source)
    subprocess.run([str(mono),str(csc),'-nologo','-noconfig','-nostdlib+','-out:'+str(d/'test.exe')]+['-r:'+str(fw/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]+[str(d/'test.cs')],check=True)
    subprocess.run([str(mono),str(d/'test.exe')],check=True)
assert 'ResolveBuffIcon(row.skillId, row.name, row.iconId)' in (p/'ReferenceWorldReceiveOwner.BuffState.cs').read_text()
assert 'ResolveBuffIcon(skillId, name, iconId)' in (p/'ReferenceWorldReceiveOwner.cs').read_text()
print('PASS both native and compatibility buff receipt paths use icon resolver')
