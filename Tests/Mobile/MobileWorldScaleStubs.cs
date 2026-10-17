using System;
namespace UnityEngine {
 public class MonoBehaviour { public Camera TestCamera; public Transform transform = new Transform(); public T GetComponent<T>() where T:class { return TestCamera as T; } }
 public class Transform {public Vector3 position;}
 public class Camera { public float orthographicSize=5, aspect=2; public bool orthographic=true; }
 public struct Vector2 {public float x,y; public Vector2(float a,float b){x=a;y=b;} }
 public struct Vector3 {public float x,y,z; public Vector3(float a,float b,float c){x=a;y=b;z=c;} }
 public static class Screen {public static int width=1920,height=1080;}
 public static class Input {public static Vector2 mouseScrollDelta;}
 public static class Mathf {public static float Abs(float x)=>Math.Abs(x);public static float Clamp(float x,float lo,float hi)=>Math.Max(lo,Math.Min(hi,x)); public static int RoundToInt(float x)=>float.IsInfinity(x)?int.MinValue:(int)Math.Round(x);public static float Round(float x)=>(float)Math.Round(x);}
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets {
 public class GameHud {public static GameHud I;public bool MouseOverHud()=>false;}
 public static class GameSettings {public static int K=1;public static int DpiK()=>K;}
 public static class ArtDetail {public static int PixelScale;public static void SetPixelScale(int scale){PixelScale=scale;}}
 public static class DuelHud {public static bool Locked;public static UnityEngine.Vector3 Position;public static bool CameraLock(out UnityEngine.Vector3 p){p=Position;return Locked;}}
 public static class MobileClientPresentation {public static bool Enabled;}
}
