using System;
using System.IO;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
namespace UnityEngine {
 public class Object { public static int Destroyed; public static void Destroy(Object value){if(value!=null)Destroyed++;} }
 public enum TextureFormat {RGBA32} public enum FilterMode {Bilinear} public enum TextureWrapMode {Clamp} public enum HideFlags {HideAndDontSave}
 public struct Color32 {public byte r,g,b,a;public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}}
 public class Texture2D:Object {public string name;public FilterMode filterMode;public TextureWrapMode wrapMode;public HideFlags hideFlags;public Color32[] Pixels;public Texture2D(int w,int h,TextureFormat f,bool mip){} public void SetPixels32(Color32[] p){Pixels=p;} public void Apply(bool a,bool b){} }
 public static class Mathf {public static float Sqrt(float v)=>(float)Math.Sqrt(v);public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));public static float Abs(float v)=>Math.Abs(v);public static float Max(float a,float b)=>Math.Max(a,b);public static int RoundToInt(float v)=>(int)Math.Round(v);}
}
public static class MobileGroupWheelArtworkContract {
 static int checks;
 static void Check(bool value,string label){if(!value)throw new Exception(label);checks++;}
 static Color32 Pixel(Texture2D t,float x,float y)=>t.Pixels[(int)(191.5f-y*2)*384+(int)(191.5f+x*2)];
 public static void Main(string[] args){
  var idle=MobileGroupWheelArtwork.Face(-1);
  Check(ReferenceEquals(idle,MobileGroupWheelArtwork.Face(-2)),"invalid state shares idle cache");
  Check(Pixel(idle,95,95).a==0,"outside circular perimeter is transparent");
  Check(Pixel(idle,0,0).r==27,"cancel center is distinct flat disc");
  for(int state=0;state<4;state++){
   var t=MobileGroupWheelArtwork.Face(state);
   Check(ReferenceEquals(t,MobileGroupWheelArtwork.Face(state)),"pending state reused without per-frame allocation");
   for(int g=0;g<4;g++){
    MobileGroupWheel.Direction(g,out float x,out float y);var p=Pixel(t,x*65,y*65);
    Check(p.r==(g==state?99:19),"rendered sector matches production hit direction");
   }
   Check(Pixel(t,0,0).r==27,"pending sector never covers cancel center");
   Check(Pixel(t,0,95).a>0,"outer frame remains present");
  }
  if(args.Length>0){var p=MobileGroupWheelArtwork.Face(1).Pixels;var bytes=new byte[p.Length*4];for(int i=0;i<p.Length;i++){bytes[i*4]=p[i].r;bytes[i*4+1]=p[i].g;bytes[i*4+2]=p[i].b;bytes[i*4+3]=p[i].a;}File.WriteAllBytes(args[0],bytes);}
  MobileGroupWheelArtwork.Clear();Check(UnityEngine.Object.Destroyed==5,"all five owned textures destroyed");
  Check(!ReferenceEquals(idle,MobileGroupWheelArtwork.Face(-1)),"cache rebuilt after cleanup");
  MobileGroupWheelArtwork.Clear();Console.WriteLine("PASS mobile wheel artwork contract: "+checks+" checks (pixels/cache; not Unity rendering)");
 }
}
