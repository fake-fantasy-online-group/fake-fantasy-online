using System;
namespace UnityEngine {
 public struct Vector3 { public float x,y,z; }
 public class Transform { public Vector3 position; }
 public static class Time { public static float deltaTime; }
 public static class Mathf { public const float Rad2Deg=57.29578f; public static float Abs(float x)=>Math.Abs(x); public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x); public static int RoundToInt(float x)=>(int)Math.Round(x); public static int Max(int a,int b)=>Math.Max(a,b);public static int Clamp(int x,int a,int b)=>Math.Max(a,Math.Min(b,x)); }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 internal static class CharData { public static int atkDist,weaponCType; public static int? nativeAtkDist; public static int AttackIdentityDistance=>nativeAtkDist??atkDist; public static int[] attrsTotal,attrs; }
 public partial class CombatWorld { public static float TestInterval=>AtkInterval; public static (string,float)? TestProjectile(int c,int d)=>AtkProjectile(c,d); }
 public class PlayerController { public static PlayerController I; public bool IsMoving; }
 public class PlayerAvatar {
  public struct Appear { public int Body,Cap,Backpack,WeaponR,WeaponL,Face; }
  public class Pose {}
  public bool ok=true; public int MagicCalls,AttackCalls;
  public Pose[,] MagicPose(out int n){MagicCalls++;n=1;return new Pose[1,1];}
  public Pose[,] AttackPose(int v,out int n){AttackCalls++;n=1;return new Pose[1,1];}
  public Pose[,] PoseFrames(int v,out int n){n=1;return new Pose[1,1];}
 }
 public partial class RemotePlayer {
  public PlayerAvatar avatar=new PlayerAvatar(); public UnityEngine.Transform transform=new UnityEngine.Transform();
  private int curDir,worldY;private bool flip,acting,moving,dead,body;
  private static int[] dirByOct=new int[8];private static bool[] flipByOct=new bool[8];
  private PlayerAvatar.Pose StandPose(int d)=>null; private int SortFor(int y)=>0;private void ApplyPose(PlayerAvatar.Pose p,bool f,int s){}
 }
}
