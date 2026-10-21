#!/usr/bin/env python3
"""Compile actual production aura sync with thin Unity doubles, no Player required."""
from pathlib import Path
import os, shutil, subprocess, tempfile
root=Path(__file__).resolve().parents[2]
p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
s=(p/'ActorEffects.Sync.cs').read_text()
methods=s[s.index('    internal static string EquipmentAuraAsset'):s.index('    // 0x06000028')]
u=(p/'EffectPlayer.Update.cs').read_text()
clock=u[u.index('        if (loopMode == 3)'):u.index('        if (!(selfDestroy')]
place=(p/'ActorEffects.cs').read_text();place=place[place.index('    private void Place('):place.rindex('\n}\n\n}')];
mono=os.environ.get('FFO_MONO') or shutil.which('mono')
csc=os.environ.get('FFO_CSC') or shutil.which('csc')
if not mono or not csc: raise SystemExit('Set FFO_MONO and FFO_CSC for the cloud managed toolchain.')
refs=['-noconfig']; framework=os.environ.get('FFO_FRAMEWORK_DIR')
if framework: refs+=['-nostdlib+']+['-r:'+str(Path(framework)/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
harness=r'''
using System; using System.Collections.Generic;
struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}}
struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}}
class Mathf {public const float PI=3.14159265f;public static float Sin(float v)=>(float)Math.Sin(v);public static float Cos(float v)=>(float)Math.Cos(v);}
class Transform { public int position;public Vector3 localPosition; public void SetParent(Transform t,bool b) {} }
class Object { public bool active=true; public void SetActive(bool v){active=v;} }
class SpriteRenderer { public int sortingOrder; public static implicit operator bool(SpriteRenderer v){return v!=null;} }
class PlayerController { public static string AuraEffName(string k){return k;} }
class EffectPlayer {
 public static int created; public string asset; public int loopMode,settled,stopped; public bool selfDestroy,attachUnder; public SpriteRenderer attachSort; public Object gameObject=new Object(); public Transform transform=new Transform();
 public static implicit operator bool(EffectPlayer e){return e!=null;}
 public static EffectPlayer PlayNamed(int pos,string key,float ppu,int a,int b,bool c,bool d){created++;return new EffectPlayer{asset=key};}
 public void SetBehind(bool b,int n){} public void SettleToEnd(){settled++;} public void Stop(){stopped++;}
}
class Actor {
 enum Kind{Aura} class Inst { public EffectPlayer ep; public int mode,slot,idx; public float groundOffsetY; public Kind kind; }
 Dictionary<string,Inst> auras=new Dictionary<string,Inst>(); List<string> _goneBuf=new List<string>();
 Transform transform=new Transform(); float ppu=100; SpriteRenderer playerSr=new SpriteRenderer(); bool _hidden; float bodyHeightU=1.4f;Vector2 mountOffsetU;const float OrbitRX=30,OrbitRY=10;
__METHODS__
__PLACE__
 static int checks; static void Check(bool x,string n){checks++;if(!x)throw new Exception(n);}
 class Template { public float lifeMs; } static Template tpl=new Template(); static float t,loopDelay=0;static int loopMode=1;
 static void Tick(float delta){t+=delta;
__CLOCK__
 }
 static void Main(){
 var keys=new[]{"套装光环·战士","套装光环·剑客","套装光环·刺客","套装光环·术士","套装光环·药师"};
 var assets=new[]{"TFX074_40","TFX028_40","TFX7012_40","TFX7015_40","TFX7018_40"};
 var lives=new[]{2110,1600,1500,1700,2100}; var actor=new Actor();var want=new Dictionary<string,int>();
 for(int i=0;i<5;i++)want[keys[i]]=0;actor.SyncAuras(want);
 for(int i=0;i<5;i++){var x=actor.auras[keys[i]];Check(x.ep.asset==assets[i],"selected art");Check(x.ep.loopMode==1&&!x.ep.selfDestroy&&x.ep.settled==0,"full loop not settled");Check(x.mode==2,"set-only ground attachment excludes saddle/head");Check(x.ep.attachSort==actor.playerSr,"actor sorting");tpl.lifeMs=lives[i];t=0;for(int n=0;n<86400;n++){Tick(1000);Check(t>=0&&t<tpl.lifeMs,"24h bounded clock");}}
 var offsets=new[]{-47f,-1.5f,0f,11f,4.4f};
 var labels=new[]{"勇气之刃","胜利王座","夜色拥抱","哲理之石","信仰之翼"};
 for(int j=0;j<5;j++) {Check(EquipmentAuraLoops(labels[j])&&EquipmentAuraAsset(labels[j])==assets[j],"display name visual compatibility");Check(EquipmentAuraGroundOffsetY(labels[j])==offsets[j],"display name placement compatibility");}
 // All actors use this same Place code. Body height, saddle offset, time/facing and
 // independent world roots cannot change these local ground-root translations.
 foreach(float height in new[]{.7f,1.4f,3f})foreach(float saddle in new[]{0f,.5f,2f}) {
 actor.bodyHeightU=height;actor.mountOffsetU=new Vector2(.3f,saddle);
 for(int j=0;j<5;j++){actor.transform.position=j*123;var x=actor.auras[keys[j]];actor.Place(x,17.2f);Check(x.ep.transform.localPosition.x==0&&Math.Abs(x.ep.transform.localPosition.y-offsets[j]/100)<.00001,"ground anchor independent of actor/head/saddle");}}
 var ordinary=new Inst{mode=3,ep=new EffectPlayer()};actor.Place(ordinary,1);Check(ordinary.ep.transform.localPosition.y==2,"ordinary TFX still follows saddle");
 var other=new Actor();other.mountOffsetU=new Vector2(5,7);other.bodyHeightU=9;var named=new Dictionary<string,int>();foreach(var label in labels)named[label]=4;other.SyncAuras(named);
 for(int j=0;j<5;j++){var x=other.auras[labels[j]];other.Place(x,123);Check(x.mode==2&&Math.Abs(x.ep.transform.localPosition.y-offsets[j]/100)<.00001,"other actor and stale head mode remain ground anchored");}
 var createdBeforeRenew=EffectPlayer.created; var original=actor.auras[keys[0]].ep; actor.SyncAuras(want);Check(EffectPlayer.created==createdBeforeRenew&&actor.auras[keys[0]].ep==original,"renewal cannot duplicate/restart");
 want.Remove(keys[0]);actor.SyncAuras(want);Check(original.stopped==1&&!actor.auras.ContainsKey(keys[0]),"source removal destroys emitter");
 foreach(var k in new[]{"TFX074_40","TFX028_40","圣羽护佑","玄武镇门","冰之盾","原技能"}){want.Clear();want[k]=0;actor.SyncAuras(want);var x=actor.auras[k];Check(!EquipmentAuraLoops(k)&&x.ep.loopMode==0&&x.ep.settled==1,"ordinary buff end-state unchanged");}
 actor._hidden=true;want.Clear();want[keys[1]]=0;actor.SyncAuras(want);Check(!actor.auras[keys[1]].ep.gameObject.active,"hidden actor preserved");
 want.Clear();actor.SyncAuras(want);Check(actor.auras.Count==0,"empty snapshot clears all");
 Console.WriteLine("PASS "+checks+" production contracts: selected art, bounded loop, attach/sort, renew/remove, ordinary buffs, hidden actor");
 }
}
'''.replace('__METHODS__',methods).replace('__CLOCK__',clock).replace('__PLACE__',place)
with tempfile.TemporaryDirectory(prefix='class-set-aura-') as d:
 d=Path(d);(d/'Test.cs').write_text(harness)
 compiler=[mono,csc] if csc.endswith('.exe') else [csc]
 subprocess.run(compiler+refs+['-nologo','-out:'+str(d/'Test.exe'),str(d/'Test.cs')],check=True)
 subprocess.run([mono,str(d/'Test.exe')],check=True)
