using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
namespace UnityEngine
{
    public struct Vector2 { public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;} }
    public struct Rect
    {
        public float x,y,width,height;
        public Rect(float x,float y,float w,float h){this.x=x;this.y=y;width=w;height=h;}
        public float xMin=>x;public float xMax=>x+width;public float yMin=>y;public float yMax=>y+height;
    }
    public static class Screen { public static int width=960,height=540; }
    public static class Mathf
    {
        public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);
        public static float Min(float a,float b)=>Math.Min(a,b);public static int Min(int a,int b)=>Math.Min(a,b);
        public static int FloorToInt(float value)=>(int)Math.Floor(value);
        public static float Clamp(float value,float a,float b)=>Math.Max(a,Math.Min(b,value));
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class MobileClientPresentation { public static bool Enabled; public static Rect SafeAreaGui;public static float Scale; }
    public static class UIScale { public static float Factor=1; }
}
class PlayerMenuLayoutContract
{
    static int checks;
    static void Check(bool v,string message){if(!v)throw new Exception(message);checks++;}
    static bool Inside(Rect a,Rect b)=>a.xMin>=b.xMin-.01&&a.yMin>=b.yMin-.01&&a.xMax<=b.xMax+.01&&a.yMax<=b.yMax+.01;
    static int Main()
    {
        foreach(Rect safe in new[]{new Rect(0,0,960,540),new Rect(60,30,1800,1020),new Rect(24,0,764,360),new Rect(0,20,390,824)})
        {
            float scale=Math.Min(safe.height/540f,safe.width/960f);
            foreach(int count in new[]{1,9,18,30,50})
            foreach(Vector2 anchor in new[]{new Vector2(-50,-50),new Vector2(safe.xMax,safe.yMax),new Vector2(safe.x+30,safe.y+50)})
            {
                var layout=new MobilePlayerMenuLayout(safe,anchor,count,scale);
                Check(Math.Abs(layout.RowHeight-44*scale)<.001,"44 logical point touch rows");
                Check(Inside(layout.Box,safe),"panel clamps inside safe area");
                Check(Inside(layout.Header,layout.Box)&&Inside(layout.Previous,layout.Box)&&Inside(layout.Next,layout.Box),"close and pages stay inside panel");
                Check(layout.PageCount*layout.RowsPerPage>=count,"every original action remains reachable");
                for(int row=0;row<Math.Min(count,layout.RowsPerPage);row++)
                    Check(Inside(layout.Row(row),layout.Box)&&layout.Row(row).yMax<=layout.Previous.yMin+.01,"44 point rows never overlap footer");
            }
        }
        foreach(float scale in new[]{1f,2f,3f})
        foreach(int count in new[]{1,8,18})
        foreach(Vector2 anchor in new[]{new Vector2(10,20),new Vector2(950,530),new Vector2(-20,-30)})
        {
            UIScale.Factor=scale;
            var menu=new DesktopMenu(count,anchor);Rect box=menu.Box();
            float w=98*scale,h=(count*22+5)*scale;
            float x=Math.Max(0,Math.Min(anchor.x,Screen.width-w)),y=Math.Max(0,Math.Min(anchor.y,Screen.height-h));
            Check(box.x==x&&box.y==y&&box.width==w&&box.height==h,"original desktop 22-point layout unchanged");
        }
        Console.WriteLine("PASS mobile/desktop player menu layout: "+checks+" checks");return 0;
    }
}
