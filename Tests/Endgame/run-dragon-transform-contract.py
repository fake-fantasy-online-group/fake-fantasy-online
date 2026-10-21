#!/usr/bin/env python3
"""Execute production Build and action-pose methods with lightweight asset/Unity doubles."""
from pathlib import Path
import os, subprocess, tempfile
root=Path(__file__).resolve().parents[2];p=root/'Assets/FakeFantasyOnline/Runtime/Presentation/ReferenceAssets'
mono=os.environ['FFO_MONO'];csc=os.environ['FFO_CSC'];fw=Path(os.environ['FFO_FRAMEWORK_DIR'])
stub=r'''
using System;using System.Collections.Generic;
namespace UnityEngine {
 public struct Color32 {public byte r,g,b,a; public Color32(byte R,byte G,byte B,byte A){r=R;g=G;b=B;a=A;}}
 public struct Color {public float r,g,b;public Color(float R,float G,float B,float A){r=R;g=G;b=B;}public static void RGBToHSV(Color c,out float h,out float s,out float v){h=s=v=0;}public static Color HSVToRGB(float a,float b,float c,bool d){return new Color();}}
 public class Mathf {public static int Clamp(int n,int a,int b)=>Math.Max(a,Math.Min(n,b));public static float Clamp01(float a)=>a;public static float Min(float a,float b)=>Math.Min(a,b);}
 public class Debug{public static void LogWarning(string s){}}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 using UnityEngine;
 class ReferenceAvatarWeaponPresentation{public static int LeftModel(int r,int l)=>l;}
 class AvatarDepth{public static void EnsureLoaded(){}}
 class SuitFxDB{public static bool IsStandAnimWeaponModel(int n)=>false;public static bool IsStandAnimBackpackModel(int n)=>false;}
 class DimgDecoder{
 public struct Raw{public bool ok,hasBind;public Color32[] px;public byte[] px16;public int w,h,boundCy,nTop,fcy,bind0y;}
 public static Raw DecodeRaw(byte[] b,int n,bool pixels=true)=>new Raw{ok=true,hasBind=true,px=new Color32[1],boundCy=160,nTop=10,fcy=80,bind0y=15};
 public static Color32[] Expand16(byte[] b,int w,int h)=>new Color32[1];public static int FramesPerDir(byte[] b)=>8;public static int BoundWidthOf(byte[] b)=>200;
 }
 public partial class PlayerAvatar {
 public struct Appear{public int Body,Cap,Backpack,WeaponR,WeaponL,Face;}
 public class Pose {} private class Layer{}
 static List<WeakReference> _liveAvatars=new List<WeakReference>();static int _avatarPurgeAt=64;static Color32[] HairRGB=new Color32[8];
 float _ppu;int _bodyModel,_fallbackBody;bool _female;DimgDecoder.Raw[] _headFr;Appear _ap;string _headKey;Dictionary<int,Pose[]> _poseCache;Dictionary<int,Pose[,]> _poseFramesCache;Dictionary<int,int> _poseFpdCache;
 public int standBodyFpd,standWingFpd,bodyBoundW,headPx,nameTopPx,sitTopPx;public bool ok;public Pose[] stand,sit,ride;public Pose[][] standAnim;public Pose[,] walk;
 bool[] _atkBuilt=new bool[3];int[] _atkFpd=new int[3];Pose[][,] _atk=new Pose[3][,];bool _magBuilt;int _magFpd;Pose[,] _mag;
 static List<string> reads=new List<string>();static bool missingOptional,missingHead;static int headsDrawn,partsDrawn;
 static byte[] ReadPkg(string path){reads.Add(path);if(path.Contains("/head/")&&missingHead)return null;if(missingOptional&&path.Contains("npc1371")&&!path.EndsWith("01.gsn")&&!path.EndsWith("02.gsn"))return null;return new byte[]{1};}
 static void LoadWeaponDepth(){} static int Key(int d,int f)=>d*100+f;
 List<(int,string,byte[],byte[],byte[],byte[])> PartsFor(int action=0){var p=new List<(int,string,byte[],byte[],byte[],byte[])>();foreach(int v in new[]{_ap.Cap,_ap.Backpack,_ap.WeaponR,_ap.WeaponL,_ap.Face})if(v>0)p.Add((v,"",null,null,null,null));return p;}
 List<(int,string,byte[],byte[],byte[],byte[])> GlowPartsFor(int a=0)=>PartsFor(a);
 static Dictionary<int,List<Layer>> FramesOf(byte[] b,string k,DimgDecoder.Raw[] h,string hk,int n,List<(int,string,byte[],byte[],byte[],byte[])> parts,int action,string pose,int stride=-1,bool glow=false,bool adv=true,int wing=1,bool wa=true){foreach(var x in h)if(x.ok)headsDrawn++;partsDrawn+=parts.Count;var r=new Dictionary<int,List<Layer>>();for(int d=0;d<5;d++)for(int f=0;f<n;f++)r[Key(d,f)]=new List<Layer>();return r;}
 static Pose MakePose(List<Layer> l,float p)=>new Pose();static void MergeGlow(Dictionary<int,List<Layer>> a,Dictionary<int,List<Layer>> b){}
 static void Check(bool b,string s){if(!b)throw new Exception(s);}
 public static void Main(){
 var original=new Appear{Body=1371,Cap=123,Face=456,Backpack=789,WeaponR=11,WeaponL=22};missingHead=true;
 var a=Build(1,8,8,original,100);Check(a.ok,"complete model needs no head file");Check(original.Cap==123&&original.WeaponR==11,"input immutable");Check(headsDrawn==0&&partsDrawn==0,"no human head or equipment layer");Check(!reads.Exists(x=>x.Contains("/head/")),"no human head reads");Check(a.headPx==150,"complete body nameplate height");
 for(int d=0;d<5;d++){Check(a.stand[d]!=null&&a.sit[d]!=null&&a.ride[d]!=null,"five direction base poses");for(int f=0;f<8;f++)Check(a.walk[d,f]!=null,"five direction run");}
 int n;for(int v=0;v<3;v++){Check(a.AttackPose(v,out n)!=null&&n==8,"attack variant");Check(reads.Contains("avatar/npc/npc1371"+(v+5).ToString("D2")+".gsn"),"attack file");}
 Check(a.MagicPose(out n)!=null,"magic");for(int action=0;action<13;action++)Check(a.PoseFrames(action,out n)!=null,"all action frames");Check(headsDrawn==0&&partsDrawn==0,"actions have no overlays");
 reads.Clear();missingOptional=true;var sparse=Build(0,1,1,original,100);sparse.PoseFrames(3,out n);Check(!reads.Exists(x=>x.Contains("npc141")||x.Contains("npc142")),"missing action cannot become human");
 missingHead=false;missingOptional=false;headsDrawn=partsDrawn=0;var restored=Build(1,2,3,new Appear{Body=141,Cap=123},100);Check(restored.ok&&headsDrawn>0&&partsDrawn>0,"normal appearance rebuild restores head/equipment");
 Check(!IsCompleteTransformation(1372)&&!IsCompleteTransformation(141),"exact approved model only");Console.WriteLine("PASS dragon: immutable appearance, body-only build, five directions, run, attack 05-07, magic, all action slots, missing-action isolation, normal restore");
 }
 }
}
'''
with tempfile.TemporaryDirectory(prefix='dragon-contract-') as d:
 d=Path(d);(d/'Stub.cs').write_text(stub);refs=['-r:'+str(fw/n) for n in ('mscorlib.dll','System.dll','System.Core.dll')]
 subprocess.run([mono,csc,'-nologo','-noconfig','-nostdlib+']+refs+['-out:'+str(d/'Test.exe'),str(d/'Stub.cs'),str(p/'PlayerAvatar.Build.cs'),str(p/'PlayerAvatar.Poses.cs')],check=True)
 subprocess.run([mono,str(d/'Test.exe')],check=True)
frames=(p/'PlayerAvatar.Frames.cs').read_text();assert 'if (head.ok && body.hasBind)' in frames
for file in ['PlayerController.LoadFrames.cs','RemotePlayer.Appearance.cs','ReferencePlayerAvatarRuntime.cs']:assert 'PlayerAvatar.Build(' in (p/file).read_text()
print('PASS shared local/remote/runtime Build ownership and head-layer eligibility guard')
