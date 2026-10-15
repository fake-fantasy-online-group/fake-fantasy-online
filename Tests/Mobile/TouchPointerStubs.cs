using System;
namespace UnityEngine
{
    public struct Vector2
    {
        public float x,y;
        public Vector2(float x,float y) { this.x=x;this.y=y; }
        public float sqrMagnitude => x*x+y*y;
        public float magnitude => (float)Math.Sqrt(sqrMagnitude);
        public static Vector2 operator +(Vector2 a,Vector2 b)=>new Vector2(a.x+b.x,a.y+b.y);
        public static Vector2 operator *(Vector2 a,float b)=>new Vector2(a.x*b,a.y*b);
        public static Vector2 zero => new Vector2(0,0);
        public static Vector2 operator -(Vector2 a,Vector2 b) => new Vector2(a.x-b.x,a.y-b.y);
        public static Vector2 operator /(Vector2 a,float f) => new Vector2(a.x/f,a.y/f);
        public static Vector2 ClampMagnitude(Vector2 v,float max)
        { float len=(float)Math.Sqrt(v.x*v.x+v.y*v.y); return len>max ? v/(len/max) : v; }
    }
}
