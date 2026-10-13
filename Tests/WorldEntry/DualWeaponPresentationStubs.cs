// Headless Unity boundary only. All avatar selection, parsing, layering and pose code is production.
using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;
namespace Unity.Collections
{
    public struct NativeArray<T>
    {
        internal T[] Values;
        public static void Copy(T[] source, int sourceIndex, NativeArray<T> target, int targetIndex, int length) =>
            Array.Copy(source, sourceIndex, target.Values, targetIndex, length);
    }
}
namespace UnityEngine
{
    public class Object { public static void Destroy(Object value) { } public static implicit operator bool(Object value)=>value!=null; }
    public struct Color32 { public byte r, g, b, a; public Color32(byte r, byte g, byte b, byte a) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public struct Vector2 { public float x,y; public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Vector2Int { public int x,y; public Vector2Int(int x,int y){this.x=x;this.y=y;} }
    public struct Rect { public float x,y,width,height; public Rect(float x,float y,float width,float height){this.x=x;this.y=y;this.width=width;this.height=height;} }
    public enum TextureFormat { RGBA32, ARGB4444 }
    public enum FilterMode { Point, Bilinear, Trilinear }
    public enum TextureWrapMode { Clamp }
    public class Texture2D : Object
    {
        public int width,height; public FilterMode filterMode; public TextureWrapMode wrapMode;
        readonly TextureFormat format; object pixels;
        public Texture2D(int width,int height,TextureFormat format,bool mipmaps){this.width=width;this.height=height;this.format=format;}
        public Unity.Collections.NativeArray<T> GetRawTextureData<T>()
        {
            if(pixels==null) pixels=new T[width*height*(typeof(T)==typeof(byte)?(format==TextureFormat.ARGB4444?2:4):1)];
            return new Unity.Collections.NativeArray<T>{Values=(T[])pixels};
        }
        public void SetPixelData<T>(T[] data,int mip,int source=0){pixels=(T[])data.Clone();}
        public void LoadRawTextureData(byte[] data){pixels=(byte[])data.Clone();}
        public void SetPixels32(Color32[] data){pixels=(Color32[])data.Clone();}
        public Color32[] GetPixels32()
        {
            if(pixels is Color32[] colors)return (Color32[])colors.Clone();
            var data=(byte[])pixels;var result=new Color32[width*height];
            for(int i=0;i<result.Length;i++) result[i]=format==TextureFormat.ARGB4444
                ?new Color32((byte)((data[i*2+1]&15)*17),(byte)((data[i*2]>>4)*17),(byte)((data[i*2]&15)*17),(byte)((data[i*2+1]>>4)*17))
                :new Color32(data[i*4],data[i*4+1],data[i*4+2],data[i*4+3]);
            return result;
        }
        public void Apply(bool mipmaps=true,bool unreadable=false){}
    }
    public class Sprite : Object
    {
        public string name;public Texture2D texture;public Vector2 pivot;public float pixelsPerUnit;
        public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float ppu)=>
            new Sprite{texture=texture,pivot=new Vector2(pivot.x*rect.width,pivot.y*rect.height),pixelsPerUnit=ppu};
    }
    public static class Mathf
    {
        public static int Clamp(int value,int min,int max)=>Math.Min(max,Math.Max(min,value));
        public static float Clamp01(float value)=>Math.Min(1f,Math.Max(0f,value));
        public static int Min(int a,int b)=>Math.Min(a,b);public static float Min(float a,float b)=>Math.Min(a,b);
        public static int Max(int a,int b)=>Math.Max(a,b);public static float Max(float a,float b)=>Math.Max(a,b);
        public static int RoundToInt(float value)=>(int)Math.Round(value);public static int CeilToInt(float value)=>(int)Math.Ceiling(value);
    }
    public struct Color
    {
        public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}
        public static void RGBToHSV(Color c,out float h,out float s,out float v)
        {
            float min=Math.Min(c.r,Math.Min(c.g,c.b));v=Math.Max(c.r,Math.Max(c.g,c.b));float d=v-min;s=v==0?0:d/v;
            h=d==0?0:(v==c.r?(c.g-c.b)/d+(c.g<c.b?6:0):v==c.g?(c.b-c.r)/d+2:(c.r-c.g)/d+4)/6;
        }
        public static Color HSVToRGB(float h,float s,float v,bool hdr)
        {
            float a=h*6;int sector=(int)Math.Floor(a);float f=a-sector,p=v*(1-s),q=v*(1-f*s),t=v*(1-(1-f)*s);
            switch(sector%6){case 0:return new Color(v,t,p,1);case 1:return new Color(q,v,p,1);case 2:return new Color(p,v,t,1);case 3:return new Color(p,q,v,1);case 4:return new Color(t,p,v,1);default:return new Color(v,p,q,1);}
        }
    }
    public static class Debug { public static void LogWarning(object value){Console.Error.WriteLine(value);} }
    public static class Time { public static float realtimeSinceStartup=>0; }
    public static class JsonUtility { public static T FromJson<T>(string value)=>new JavaScriptSerializer().Deserialize<T>(value); }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed class ResLoader
    {
        public static ResLoader I; public string Root;
        public byte[] ReadRes24First(string name)
        {
            string path=Path.Combine(Root,name);
            if(!File.Exists(path))
            {
                string directory=Path.GetDirectoryName(path);
                if(!Directory.Exists(directory))return null;
                foreach(string candidate in Directory.GetFiles(directory))
                    if(string.Equals(Path.GetFileName(candidate),Path.GetFileName(path),StringComparison.OrdinalIgnoreCase))return File.ReadAllBytes(candidate);
                return null;
            }
            return File.ReadAllBytes(path);
        }
    }
    public static class GameTables
    {
        public static string Root;
        public static string LoadText(string name)
        {
            if(name!="weapon_dual")return null;
            return Root==null?"{\"models\":[51,60,1282]}":File.ReadAllText(Path.Combine(Root,"weapon_dual.json"));
        }
    }
    public static class ArtDetail {public static float BudgetScale=>1;public static event Action OnBudgetScaleChanged;public static bool NeedMip(int scale)=>false;}
    public static class LoadTrace {public static bool On=>false;public static long Now=>0;public static float Ms(long value)=>0;public static void Decode(bool valid,int width,int height,float time){}}
    public static class GsaTextureScale {public static void Register(UnityEngine.Texture2D texture,int scale){}}
    public static class TexScale {public static void Register(UnityEngine.Texture2D texture,int scale){}}
}
