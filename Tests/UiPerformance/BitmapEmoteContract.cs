using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

// Draw recorders exercise the actual complete BitmapFont and EmoteText sources.
// The oracle uses the old run-string algorithm; neither route invokes Unity.
namespace UnityEngine
{
    public class Object { public static implicit operator bool(Object value)=>value!=null; }
    public enum FilterMode { Point } public enum TextureWrapMode { Clamp } public enum TextureFormat { RGBA32 }
    public class Texture2D:Object
    {
        public int width,height; public FilterMode filterMode; public TextureWrapMode wrapMode;
        public Texture2D(int w,int h,TextureFormat f,bool mip){width=w;height=h;}
        public Color32[] GetPixels32()=>new Color32[width*height];
        public void SetPixels32(Color32[] pixels){} public void Apply(bool a,bool b){}
    }
    public struct Rect { public float x,y,width,height; public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;} }
    public struct Color
    {
        public float r,g,b,a; public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static Color black=>new Color(0,0,0,1);
        public static implicit operator Color(Color32 c)=>new Color(c.r/255f,c.g/255f,c.b/255f,c.a/255f);
    }
    public struct Color32
    {
        public byte r,g,b,a; public Color32(byte r,byte g,byte b,byte a){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static implicit operator Color32(Color c)=>new Color32((byte)(c.r*255),(byte)(c.g*255),(byte)(c.b*255),(byte)(c.a*255));
    }
    public static class Mathf
    {
        public static float Round(float value)=>(float)Math.Round(value);
        public static float Max(float a,float b)=>Math.Max(a,b); public static int Max(int a,int b)=>Math.Max(a,b);
    }
    public static class Time { public static float time; }
    public static class GUI
    {
        public struct Call { public Rect rect,uv; public Texture2D texture; public Color color; public bool coords; }
        public static Color color=new Color(1,1,1,1);
        public static readonly List<Call> Calls=new List<Call>(4096);
        public static void DrawTextureWithTexCoords(Rect rect,Texture2D texture,Rect uv)=>Calls.Add(new Call{rect=rect,texture=texture,uv=uv,color=color,coords=true});
        public static void DrawTexture(Rect rect,Texture2D texture)=>Calls.Add(new Call{rect=rect,texture=texture,color=color});
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public class ResLoader:UnityEngine.Object
    {
        public static ResLoader I;
        public readonly Texture2D Atlas=new Texture2D(120,156,TextureFormat.RGBA32,false);
        public readonly ushort[] Cells=new ushort[65536]; public readonly byte[] Widths=new byte[130];
        public readonly Texture2D[] Frames={new Texture2D(16,16,TextureFormat.RGBA32,false),new Texture2D(16,16,TextureFormat.RGBA32,false),null};
        public ResLoader()
        {
            for(int i=0;i<Cells.Length;i++)Cells[i]=ushort.MaxValue;
            for(int i=0;i<127;i++)Cells[i]=(ushort)i;
            Cells['中']=1;for(int i=0;i<Widths.Length;i++)Widths[i]=(byte)(6+i%7);
        }
        public Texture2D LoadUI(string path)=>Atlas;
        public byte[] LoadUIBytes(string path)
        {
            if(path.EndsWith(".wid"))return Widths;
            var bytes=new byte[Cells.Length*2];Buffer.BlockCopy(Cells,0,bytes,0,bytes.Length);return bytes;
        }
        public int GsaFrameCount(string path)=>Frames.Length;
        public Texture2D LoadGsaFrame(string path,int index)=>Frames[index];
    }
    internal static class TexScale { public static float W(Texture2D t)=>t.width;public static float H(Texture2D t)=>t.height;public static int Of(Texture2D t)=>1; }
    internal static class BitmapEmoteContract
    {
        private static int assertions;
        private static readonly Func<long> AllocatedBytes=(Func<long>)Delegate.CreateDelegate(typeof(Func<long>),typeof(GC).GetMethod("GetAllocatedBytesForCurrentThread"));
        private static void Check(bool value,string label){assertions++;if(!value)throw new Exception(label);}
        private static bool Same(Rect a,Rect b)=>a.x==b.x&&a.y==b.y&&a.width==b.width&&a.height==b.height;
        private static bool Same(Color a,Color b)=>a.r==b.r&&a.g==b.g&&a.b==b.b&&a.a==b.a;
        private static bool Emote(string text,int i,out int id)
        {
            id=0;if(i+3>=text.Length||text[i]!='\\'||text[i+1]!='e'||!char.IsDigit(text[i+2])||!char.IsDigit(text[i+3]))return false;
            id=(text[i+2]-'0')*10+text[i+3]-'0';return (uint)(id-1)<96;
        }
        private static float Advance(char c)
        {
            if(c==' ')return 6;
            int cell=ResLoader.I.Cells[c];return cell==ushort.MaxValue?12:ResLoader.I.Widths[cell];
        }
        private static float LegacyMeasure(string text,float scale)
        {
            float width=0;foreach(char c in text)width+=Advance(c)*scale;return width;
        }
        private static void LegacyBitmapDraw(float x,float y,string text,Color color,float scale)
        {
            x=Mathf.Round(x);y=Mathf.Round(y);var atlas=ResLoader.I.Atlas;var previous=GUI.color;GUI.color=color;
            foreach(char c in text)
            {
                if(c==' '){x+=6*scale;continue;}
                int cell=ResLoader.I.Cells[c];
                if(cell!=ushort.MaxValue)GUI.DrawTextureWithTexCoords(new Rect(x,y,12*scale,12*scale),atlas,new Rect((cell%10)*12/(float)atlas.width,1f-(cell/10+1)*12/(float)atlas.height,12/(float)atlas.width,12/(float)atlas.height));
                x+=Advance(c)*scale;
            }
            GUI.color=previous;
        }
        private static void LegacyOutline(float x,float y,string text,Color color,float scale,bool outline)
        {
            if(!outline){LegacyBitmapDraw(x,y,text,color,scale);return;}
            x=Mathf.Round(x);y=Mathf.Round(y);float offset=Mathf.Max(1,scale);
            for(int dx=-1;dx<=1;dx++)for(int dy=-1;dy<=1;dy++)if(dx!=0||dy!=0)LegacyBitmapDraw(x+dx*offset,y+dy*offset,text,Color.black,scale);
            LegacyBitmapDraw(x,y,text,color,scale);
        }
        private static float LegacyEmote(float x,float y,string text,Color color,float scale,bool outline,bool draw)
        {
            if(string.IsNullOrEmpty(text))return 0;
            x=Mathf.Round(x);y=Mathf.Round(y);float width=0,size=16*scale;var buffer=new StringBuilder();
            void Flush()
            {
                if(buffer.Length==0)return;
                string run=buffer.ToString();if(draw)LegacyOutline(x,y,run,color,scale,outline);
                float advance=LegacyMeasure(run,scale);x=x+advance;width+=advance;buffer.Clear();
            }
            for(int i=0;i<text.Length;)
            {
                if(Emote(text,i,out int id))
                {
                    Flush();var frames=ResLoader.I.Frames;var frame=frames[(int)(Time.time/.2f)%frames.Length]??frames[0];
                    if(draw)GUI.DrawTexture(new Rect(x,y+scale*(-3f),size,size),frame);
                    x=x+(size+scale);width=17*scale+width;i+=4;
                }
                else {buffer.Append(text[i]);i++;}
            }
            Flush();return width;
        }
        private static void CompareDraw(string text,float scale,float time,bool outline)
        {
            Time.time=time;var color=new Color(.4f,.7f,.9f,.5f);var previous=new Color(.3f,.2f,.8f,.6f);GUI.color=previous;GUI.Calls.Clear();
            LegacyEmote(-.3f,4.4f,text,color,scale,outline,true);var expected=GUI.Calls.ToArray();GUI.Calls.Clear();
            EmoteText.DrawColored(-.3f,4.4f,text,color,scale,outline);
            Check(GUI.Calls.Count==expected.Length,"Glyph/outline/emoji draw count");
            for(int i=0;i<expected.Length;i++)
            {
                var a=expected[i];var b=GUI.Calls[i];Check(Same(a.rect,b.rect)&&Same(a.uv,b.uv)&&Same(a.color,b.color)&&a.texture==b.texture&&a.coords==b.coords,"Exact draw order, position, UV, color and live frame");
            }
            Check(Same(GUI.color,previous),"GUI color restored");
            Check(EmoteText.Measure(text,scale)==LegacyEmote(0,0,text,color,scale,outline,false),"Measure retains exact floating-point operation order");
        }
        private static void ResetFont()
        {
            var flags=BindingFlags.Static|BindingFlags.NonPublic;
            typeof(BitmapFont).GetField("atlas",flags).SetValue(null,null);
            typeof(BitmapFont).GetField("idx",flags).SetValue(null,null);
            typeof(BitmapFont).GetField("tried",flags).SetValue(null,false);
        }
        private static List<string> LegacyWrap(string text,float width,float scale)
        {
            var rows=new List<string>();var row=new StringBuilder();float advance=0;
            if(string.IsNullOrEmpty(text)){rows.Add(text??"");return rows;}
            for(int i=0;i<text.Length;)
            {
                bool emote=Emote(text,i,out int id);int length=emote?4:1;float token=emote?17*scale:Advance(text[i])*scale;
                if(advance+token>width&&row.Length>0){rows.Add(row.ToString());row.Clear();advance=0;}
                row.Append(text,i,length);advance=token+advance;i+=length;
            }
            if(row.Length>0||rows.Count==0)rows.Add(row.ToString());return rows;
        }
        private static void Main()
        {
            ResLoader.I=new ResLoader();BitmapFont.Measure("warm",1);
            string[] texts={null,"","plain text","A\\e01B\\e96中","\\e02\\e01","before\\e00/\\e97/\\e0x/\\e１1/\\e","missing🙂 glyph\nend","\\e01final"};
            foreach(string text in texts)foreach(float scale in new[]{.65f,1f,1.5f,2.5f})foreach(float time in new[]{0f,.2f,.4f})foreach(bool outline in new[]{false,true})CompareDraw(text,scale,time,outline);
            GUI.Calls.Clear();EmoteText.DrawOutline(1,2,"A",new Color(1,1,1,1),1);Check(GUI.Calls.Count==9,"One glyph still has eight outline samples and one fill");
            foreach(string text in texts)foreach(float scale in new[]{.65f,1f,1.5f,2.5f})foreach(float width in new[]{0f,11.9f,12.1f,24f,100f})
            {
                var expected=LegacyWrap(text,width,scale);var actual=EmoteText.Wrap(text,width,scale);
                Check(string.Join("|",actual)==string.Join("|",expected),"Wrapping matches actual metrics and exact layout boundary");
                if(!string.IsNullOrEmpty(text))Check(ReferenceEquals(actual,EmoteText.Wrap(text,width,scale)),"Repeated exact wrapping inputs reuse rows");
            }
            Check(EmoteText.Wrap("AA",15.9f,1).Count==2&&EmoteText.Wrap("AA",16.1f,1).Count==1,"Widths that used to share a rounded cache key reflow across a real glyph boundary");
            Check(EmoteText.Wrap("AA",16,1).Count==1&&EmoteText.Wrap("AA",16,1.001f).Count==2,"Small scale changes reflow at the exact boundary");
            var before=EmoteText.Wrap("AAAA",30,1);var oldFont=BitmapFont.LayoutIdentity;
            ResLoader.I=new ResLoader();ResLoader.I.Widths['A']=12;ResetFont();BitmapFont.Measure("warm",1);
            var after=EmoteText.Wrap("AAAA",30,1);Check(!ReferenceEquals(oldFont,BitmapFont.LayoutIdentity)&&!ReferenceEquals(before,after)&&after.Count==2&&before[0]=="AAA"&&after[0]=="AA","Reloaded font metrics invalidate wrap entries");
            ResetFont();var loader=ResLoader.I;ResLoader.I=null;var absent=EmoteText.Wrap("AAAA",10,1);Check(absent.Count==1,"Missing font preserves original unwrapped fallback");
            ResLoader.I=loader;BitmapFont.Measure("warm",1);Check(EmoteText.Wrap("AAAA",10,1).Count==4,"Late font readiness does not reuse an unmeasured fallback");
            for(int i=0;i<600;i++)EmoteText.Wrap("bounded"+i,100,1);
            var cache=(IDictionary)typeof(EmoteText).GetField("wrapCache",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);Check(cache.Count<=512,"Wrap cache is bounded to 512 entries");
            const string stable="A\\e01B\\e96中 spaces";Time.time=.2f;
            for(int i=0;i<50;i++){GUI.Calls.Clear();EmoteText.DrawOutline(1,2,stable,new Color(1,1,1,1),1.5f);EmoteText.Measure(stable,1.5f);}
            long allocated=AllocatedBytes();
            for(int i=0;i<1000;i++){GUI.Calls.Clear();EmoteText.DrawOutline(1,2,stable,new Color(1,1,1,1),1.5f);EmoteText.Measure(stable,1.5f);}
            long bytes=AllocatedBytes()-allocated;Check(bytes==0,"Steady emote draw and measure allocate zero managed bytes");
            Console.WriteLine("PASS bitmap/emote: "+assertions+" assertions; exact recorded legacy draw equivalence across text, scale, outline and animation cases. 1000 mixed-emoji draw+measure passes: 0 managed bytes. No Unity/device execution.");
        }
    }
}
