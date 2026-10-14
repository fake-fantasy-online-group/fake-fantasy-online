using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
namespace FakeFantasyOnline.Protocol
{
public sealed partial class NetClient
{
    internal sealed class Buf { public void Bytes(byte[] payload) {} }
    private void Send(ushort op, Action<Buf> write) { write(new Buf()); }
}
}
namespace UnityEngine
{
public class Transform { public Vector3 position; }
public class TextAsset { public byte[] bytes; }
public struct Vector2 { public float x,y; public Vector2(float x,float y) {this.x=x;this.y=y;} }
public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) {this.x=x;this.y=y;this.z=z;} }
public struct Rect { public float xMin,yMin,xMax,yMax; }
public static class Mathf
{
    public static float Sqrt(float v)=>(float)Math.Sqrt(v);
    public static float Clamp(float v,float min,float max)=>Math.Max(min,Math.Min(max,v));
    public static float Round(float v)=>(float)Math.Round(v);
    public static float Max(float a,float b)=>Math.Max(a,b);
    public static float Min(float a,float b)=>Math.Min(a,b);
    public static int CeilToInt(float v)=>(int)Math.Ceiling(v);
}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
using UnityEngine;
public class EndgamePanel { public static EndgameClientState WorldState; }
public class GameBootstrap
{
    public static GameBootstrap I;
    public static int CurMapId;
    public bool Switching;
    public Transform WorldRootForMapConstructionForIndependentRuntime=new Transform();
}
public class ReferenceWorldCollisionMask
{
    public static ReferenceWorldCollisionMask FromDecodedForIndependentRuntime(byte[] a,int b,int c,int d,int e,int f)=>null;
}
public static class DuelHud { public static bool Blocks(float x,float y)=>false; }
public partial class PlayerController
{
    public static PlayerController I;
    public Transform transform=new Transform();
    public float worldW=2000,worldH=2000,ppu=1,Step=50;
    private bool sitting,moveToTarget;
    private int pathIdx,pendHops;
    private long pluginNavigationRevision;
    private List<Vector2> path=new List<Vector2>();
    private static readonly int[] DX8={1,0,-1,0,1,-1,-1,1};
    private static readonly int[] DY8={0,1,0,-1,1,1,-1,-1};
    internal float MovementStepForIndependentRuntime()=>Step;
    public bool HasPath=>moveToTarget;
    public long NavigationOwner=>pluginNavigationRevision;
    public void SetOpenMask(int width,int height,int cell)
    { maskW=width;maskH=height;maskCW=maskCH=cell;maskOff=0;maskBytes=new byte[width*height]; }
    public void ForceDirectTarget(float x,float y) { path.Clear();pathIdx=0;tgtX=x;tgtY=y;moveToTarget=true; }
    public bool PathSegmentsClear(EndgameRiftHazardState hazards)
    {
        float x=worldX,y=worldY;
        for(int i=pathIdx;i<path.Count;i++) {var point=path[i];if(hazards.Blocks(GameBootstrap.CurMapId,x,y,point.x,point.y,EndgameClientState.MonotonicMS))return false;x=point.x;y=point.y;}
        return true;
    }
}
}
