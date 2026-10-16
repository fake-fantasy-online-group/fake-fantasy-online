using System;
using System.IO;
using System.Security.Cryptography;
using FakeFantasyOnline.Presentation.ReferenceAssets;

namespace UnityEngine {
public class TextAsset { public byte[] bytes; }
public static class Resources { public static string TablePath; public static T Load<T>(string name) where T:class => new TextAsset{bytes=File.ReadAllBytes(TablePath)} as T; }
public static class Debug { public static void LogWarning(string value){Console.Error.WriteLine(value);} }
public enum TextureFormat { RGBA32 }
public enum FilterMode { Point,Trilinear }
public enum TextureWrapMode { Clamp }
public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} }
public class Texture2D { public int width,height; public Color32[] pixels; public FilterMode filterMode; public TextureWrapMode wrapMode; public Texture2D(int width,int height,TextureFormat format,bool mip){this.width=width;this.height=height;} public void SetPixels32(Color32[] value){pixels=value;} public void Apply(bool mip,bool unreadable){} }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
internal static class LoadTrace { public static long Now=>0; public static bool On=>false; public static float Ms(long time)=>0; public static void Pkg(string name,string source,int packed,int original,float read,float inflate){} }
internal static class GsaTextureScale { public static void Register(UnityEngine.Texture2D texture,int scale){} public static int Of(UnityEngine.Texture2D texture)=>1; public static float Width(UnityEngine.Texture2D texture)=>texture.width; public static float Height(UnityEngine.Texture2D texture)=>texture.height; }
}
class MobileHudResourceContract {
static int checks;
static void Check(bool value,string label){checks++;if(!value)throw new Exception(label);}
static string Hash(byte[] bytes){using(var hash=SHA256.Create())return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();}
static void Main(string[] args){
 string assets=Path.Combine(args[0],"Assets");
 UnityEngine.Resources.TablePath=Path.Combine(assets,"Resources/gbk_table.bytes");
 var archive=new PkgArchive();
 Check(archive.MountPkgs(new[]{Path.Combine(assets,"StreamingAssets/ReferenceClient/packages/res_q21_001.pkg")}),"Local package mounts through production PkgArchive");
 var specs=new[]{
  ("res/uires/petCircle/choosepetbtn.gsa",42,47,"cb47b54d832a3fe1fb61f51a40a0a5a9d04feff2bab1c16d4db617d94ef0d8b4"),
  ("res/uires/badgen/hurttype0.gsa",40,40,"4445e666d9b3fe1a87ac24cbb1010646855ef8e135b1077bfa99270657b628fa"),
  ("res/uires/composedlg/sculphole.gsa",40,41,"23a6d6879b7fccb29e57f11e2cc068f4d5a5c34abb953ac56ba5025678950be7")
 };
 foreach(var spec in specs){
  byte[] bytes=archive.Read(spec.Item1);
  Check(bytes!=null&&bytes.Length>36,"Exact resource path resolves: "+spec.Item1);
  Check(Hash(bytes)==spec.Item4,"Resource matches inspected original artwork: "+spec.Item1);
  Check(Hash(archive.Read(spec.Item1.ToLowerInvariant()))==spec.Item4,"Case-insensitive package lookup: "+spec.Item1);
  Check(GsaDecoder.FrameCount(bytes)==1,"Expected original frame count: "+spec.Item1);
  var frame=GsaDecoder.DecodeFrameAssembled(bytes,0);
  Check(frame!=null&&frame.width==spec.Item2&&frame.height==spec.Item3,"Production assembled decode dimensions: "+spec.Item1);
  int opaque=0;bool[] quadrant=new bool[4];
  for(int y=0;y<frame.height;y++)for(int x=0;x<frame.width;x++)if(frame.pixels[y*frame.width+x].a>32){opaque++;quadrant[(x<frame.width/2?0:1)+(y<frame.height/2?0:2)]=true;}
  Check(opaque>100,"Decoded artwork is visibly nonempty: "+spec.Item1);
  foreach(bool present in quadrant)Check(present,"Inspected artwork covers all four quadrants: "+spec.Item1);
  Console.WriteLine("Verified local original frame: "+spec.Item1+" "+frame.width+"x"+frame.height);
 }
 foreach(string name in new[]{"AttackH","AttackM","AttackL","Fetch"}) {
  string path="res/uires/cursor/"+name+".gsa"; byte[] bytes=archive.Read(path);
  Check(bytes!=null,"Original cursor present "+name);var frame=GsaDecoder.DecodeFrameAssembled(bytes,0);
  Check(frame!=null,"Original cursor decoded "+name); int opaque=0;long red=0,green=0,blue=0;
  foreach(var pixel in frame.pixels)if(pixel.a>32){opaque++;red+=pixel.r;green+=pixel.g;blue+=pixel.b;}
  Check(opaque>30,"Original cursor visibly nonempty "+name);
  if(name=="AttackM")Check(red>green && green>blue*2,"Original middle cursor is yellow/gold");
  if(name=="AttackH")Check(red>green*2,"Original higher cursor is red");
  if(name=="AttackL")Check(green>red && green>blue*2,"Original lower cursor is green");
  Console.WriteLine("Verified original cursor "+name+" "+frame.width+"x"+frame.height+" sha256="+Hash(bytes));
 }
 Console.WriteLine("PASS mobile HUD package resources: "+checks+" checks using production PkgArchive, Gbk and GsaDecoder. Texture storage is stubbed; no Unity screenshot.");
}
}
