// Engine-free substitutes only: this test does not validate Unity rendering or assets.
using System.Collections.Generic;
namespace UnityEngine {
 public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;} }
 public class Transform { }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public partial class CombatWorld { public float ppu=100,worldW=1000,worldH=800; }
 public class EffectPlayer {
  public static readonly List<EffectPlayer> Created=new List<EffectPlayer>();
  public UnityEngine.Vector3 Position;public UnityEngine.Transform Follow;public bool Sustained,Stopped;public float Duration;
  public static implicit operator bool(EffectPlayer p){return p!=null&&!p.Stopped;}
  public void Stop(){Stopped=true;}
  public static EffectPlayer PlayNamed(UnityEngine.Vector3 p,string e,float s,int f,int d,bool flip,bool follow) {var x=new EffectPlayer{Position=p};Created.Add(x);return x;}
  public static EffectPlayer PlaySustained(UnityEngine.Vector3 p,string e,float s,UnityEngine.Transform follow,float duration) {var x=new EffectPlayer{Position=p,Sustained=true,Follow=follow,Duration=duration};Created.Add(x);return x;}
 }
}
