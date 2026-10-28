using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
namespace UnityEngine {
 public struct Color { public byte r,g,b,a; public static Color white => new Color{r=255,g=255,b=255,a=255}; }
 public struct Color32 { public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;} public static implicit operator Color(Color32 c)=>new Color{r=c.r,g=c.g,b=c.b,a=c.a}; }
 public static class Mathf { public static float Max(float a,float b)=>Math.Max(a,b); }
}
static class GameSettings { public static bool ColorWeak; }
static class EquipmentTooltipColors { public static UnityEngine.Color32 ActiveBlue=>new UnityEngine.Color32(82,150,252,255); }
static partial class BitmapFont {
 const int CELL=12; static ushort[] idx; static byte[] wid; static bool Ready=>true; static void Ensure(){}
 public static void Load(string dir){byte[] bytes=File.ReadAllBytes(Path.Combine(dir,"gbk12.idx"));idx=new ushort[bytes.Length/2];Buffer.BlockCopy(bytes,0,idx,0,bytes.Length);wid=File.ReadAllBytes(Path.Combine(dir,"gbk12.wid"));}
}
static partial class UITip {
 static int checks;
 static void Check(bool pass,string name){checks++;if(!pass)throw new Exception(name);}
 static void Token(string token,float scale) {
  string prefix="威能直接伤害提高";while(BitmapFont.Measure(prefix+token,scale)<=150*scale)prefix+="伤";
  string text=prefix+token+"。";var rows=WrapByWidth(text,150*scale,scale);
  Check(string.Concat(rows)==text,"lossless "+token);Check(rows.Any(x=>x.Contains(token)),"unsplit token "+token);
  foreach(string row in rows)Check(BitmapFont.Measure(row,scale)<=150*scale,"measured glyph width "+token);
 }
 public static void Main(){
  BitmapFont.Load(Environment.GetEnvironmentVariable("FFO_NATIVE_FONT_DIRECTORY"));
  foreach(float scale in new[]{.4f,1f,1.5f,2f})foreach(string token in new[]{"48.73%","12345","-48.73%","+48.73%","52%～70%","52～70%","-52.5%～-30%","52-70%"})Token(token,scale);
  string longNumber=new string('8',70)+"%";var longRows=WrapByWidth(longNumber,150,1);Check(string.Concat(longRows)==longNumber && longRows.Count>1,"overlong token fallback lossless");foreach(string row in longRows)Check(BitmapFont.Measure(row,1)<=150,"overlong token stays inside frame");
  var chinese=WrapByWidth("卷中威能可以附着在暗金装备或羽翼之上",150,1);Check(string.Concat(chinese)=="卷中威能可以附着在暗金装备或羽翼之上","Chinese text preserved");
  var layout=Layout("\u0007\u00035威能卷轴\n使用说明\n\u0006威能直接伤害提高48.73%。\n\u0006下一段52%～70%。",1);
  Check(layout.Item1[0].Item1=="威能卷轴" && layout.Item1[0].Item2.r==183 && layout.Item1[0].Item3,"native title color and centering preserved");
  Check(layout.Item1[1].Item1=="使用说明" && layout.Item1[1].Item2.r==255 && layout.Item1[1].Item2.g==255,"usage stays white");
  foreach(var row in layout.Item1.Skip(2))Check(row.Item2.r==255 && row.Item2.g==136 && row.Item2.b==48 && !row.Item1.Contains("\u0006"),"wrapped effect keeps original orange without control glyphs");
  Check(layout.Item1.Any(x=>x.Item1.StartsWith("下一段")),"hard newline not merged with previous paragraph");
  Check(WrapByWidth("",150,1).Count==1 && WrapByWidth("12%",0,1)[0]=="12%","empty and unbounded contracts unchanged");
  Console.WriteLine("PASS "+checks+" native tooltip numeric-wrap/layout assertions with actual gbk12 glyph widths; no Unity runtime.");
 }
}
