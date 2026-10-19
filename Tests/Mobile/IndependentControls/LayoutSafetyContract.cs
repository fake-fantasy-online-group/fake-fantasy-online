using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;
class LayoutSafetyContract {
 static int checks;
 static void Check(bool v,string m){checks++;if(!v)throw new Exception(m);}
 static bool Finite(float x)=>!float.IsNaN(x)&&!float.IsInfinity(x);
 static void Safe(Rect r,Rect s,string msg){Check(Finite(r.x)&&Finite(r.y)&&Finite(r.width)&&Finite(r.height)&&r.width>0&&r.height>0,msg+" finite"); Check(r.x>=s.x-.001f&&r.y>=s.y-.001f&&r.xMax<=s.xMax+.001f&&r.yMax<=s.yMax+.001f,msg+" safe");}
 static void Reject(string text){bool bad=false;try{MobileControlLayoutStore.Parse(text);}catch(InvalidDataException){bad=true;}Check(bad,"reject "+text);}
 static void Main(){
 var safe=new Rect(44,24,728,327);var baseline=new Rect(100,100,64,64);var layout=new MobileControlLayout();
 Check(layout.Count==0,"defaults empty");
 foreach(var text in new[]{"","version=0","version=2","version=1\nversion=1","version=1\nskill0=NaN,0.5,1","version=1\nskill0=0.5,Infinity,1","version=1\nskill0=0.5,0.5,-Infinity","version=1\nskill0=0.5,0.5,0.64","version=1\nskill0=0.5,0.5,1.51","version=1\nskill0=-0.001,0.5,1","version=1\nskill0=0.5,1.001,1","version=1\nskill6=0.5,0.5,1","version=1\nmount=0.5,0.5,1\nmount=0.1,0.2,1","version=1\nskill0=0.5,0.5","version=1\nskill0=1e90,0.5,1",new string('x',4097)}) Reject(text);
 Check(MobileControlLayoutStore.Parse("version=1\n").Count==0,"missing placements use defaults");
 foreach(MobileControlId id in Enum.GetValues(typeof(MobileControlId))) Check(layout.Set(id,new Vector2(200+(int)id,160),1,baseline,safe,1),"valid control "+id);
 Check(layout.Count==MobileControlLayout.ControlCount,"all stable ids");
 string original=MobileControlLayoutStore.Serialize(layout); CultureInfo.CurrentCulture=CultureInfo.GetCultureInfo("fr-FR");
 Check(MobileControlLayoutStore.Serialize(MobileControlLayoutStore.Parse(original))==original,"invariant exact roundtrip");
 var clone=layout.Clone();clone.Clear();Check(clone.Count==0&&layout.Count==MobileControlLayout.ControlCount,"clone reset isolated");
 Check(!layout.Set((MobileControlId)999,new Vector2(1,1),1,baseline,safe,1),"unknown ID rejected");
 foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity}){Check(!layout.Set(MobileControlId.Mount,new Vector2(bad,1),1,baseline,safe,1),"invalid x");Check(!layout.Set(MobileControlId.Mount,new Vector2(1,bad),1,baseline,safe,1),"invalid y");Check(!layout.Set(MobileControlId.Mount,new Vector2(1,1),bad,baseline,safe,1),"invalid scale");}
 Check(MobileControlLayoutStore.Serialize(layout)==original,"invalid samples preserve complete arrangement");
 var random=new Random(53221);
 for(int i=0;i<4000;i++){
  float w=12+(float)random.NextDouble()*2300,h=12+(float)random.NextDouble()*1400,x=(float)random.NextDouble()*100,y=(float)random.NextDouble()*100;
  var region=new Rect(x,y,w,h);var d=new Rect(x+w*.8f,y+h*.8f,20+(float)random.NextDouble()*200,20+(float)random.NextDouble()*200);float s=.01f+(float)random.NextDouble()*3;
  var id=(MobileControlId)(i%MobileControlLayout.ControlCount);
  Check(layout.Set(id,new Vector2(x-300+(float)random.NextDouble()*(w+600),y-300+(float)random.NextDouble()*(h+600)),.1f+(float)random.NextDouble()*3,d,region,s),"random set");
  Safe(layout.ResolveRect(id,d,region,s),region,"random rect");
  var rotated=new Rect(y,x,h,w);Safe(layout.ResolveRect(id,new Rect(0,0,120*s,160*s),rotated,s),rotated,"rotated");
 }
 string snapshot=MobileControlLayoutStore.Serialize(layout);
 foreach(float ui in new[]{float.NaN,float.PositiveInfinity,0,-1,1e30f}) Safe(layout.ResolveRect(MobileControlId.Mount,baseline,safe,ui),safe,"pathological scale");
 Check(snapshot==MobileControlLayoutStore.Serialize(layout),"resolving viewports has no persistence mutation");
 string dir=Path.Combine(Path.GetTempPath(),"mobile-independent-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
 try{
  string path=Path.Combine(dir,MobileControlLayoutStore.FileName);Check(MobileControlLayoutStore.Load(dir).Count==0&&!File.Exists(path),"missing file read no write");
  Check(MobileControlLayoutStore.TrySave(dir,layout),"initial save");Check(File.ReadAllText(path)==snapshot,"saved exact");
  Check(MobileControlLayoutStore.Serialize(MobileControlLayoutStore.Load(dir))==snapshot,"reload exact");
  var reset=new MobileControlLayout();Check(MobileControlLayoutStore.TrySave(dir,reset),"atomic replacement");Check(File.ReadAllText(path)=="version=1\n","reset persisted");
  byte[] invalid={0xff,0xfe,0xff};File.WriteAllBytes(path,invalid);Check(MobileControlLayoutStore.Load(dir).Count==0&&File.ReadAllBytes(path).Length==invalid.Length,"invalid UTF8 no destruction");
  string oversized=new string('x',5000);File.WriteAllText(path,oversized);Check(MobileControlLayoutStore.Load(dir).Count==0&&File.ReadAllText(path)==oversized,"oversize fallback retains original");
  File.WriteAllText(path,"version=1\nskill0=NaN,0.5,1");Check(MobileControlLayoutStore.Load(dir).Count==0&&File.ReadAllText(path).Contains("NaN"),"corrupt fallback retains original");
  string occupied=Path.Combine(dir,"occupied");File.WriteAllText(occupied,"keep");Check(!MobileControlLayoutStore.TrySave(occupied,layout,out string error)&&!string.IsNullOrEmpty(error)&&File.ReadAllText(occupied)=="keep","failed save safe and explained");
  Check(Directory.GetFiles(dir,"*.tmp").Length==0,"no orphan temp files");
 }finally{Directory.Delete(dir,true);}
 Console.WriteLine("PASS independent layout safety "+checks+" assertions; deterministic value and file tests, no Unity/device.");
 }
}
