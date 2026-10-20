using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
namespace FakeFantasyOnline.Presentation { public static class EmbeddedClientBackground {public static void Preload(){}public static void Draw(UnityEngine.Rect r){}} }
namespace UnityEngine {
 public class Object {public static void Destroy(object o){}}
 public class MonoBehaviour:Object {public Transform transform=new Transform();public object StartCoroutine(IEnumerator e){return e;}}
 public class Transform {public void SetParent(Transform t,bool b){}}
 public class GameObject {public Transform transform=new Transform();public GameObject(string n){}public T AddComponent<T>()where T:new(){return new T();}}
 public class Camera {public GameObject gameObject;public CameraClearFlags clearFlags;public Color backgroundColor;public int cullingMask;public float depth;public bool useOcclusionCulling;}
 public enum CameraClearFlags {SolidColor} public enum TextureFormat {RGBA32} public enum TextAnchor {MiddleCenter,MiddleRight}
 public struct Rect {public float x,y,width,height;public float yMax=>y+height;public Rect(float a,float b,float c,float d){x=a;y=b;width=c;height=d;}}
 public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a=1){this.r=r;this.g=g;this.b=b;this.a=a;}public static Color white=>new Color(1,1,1);public static Color Lerp(Color a,Color b,float t){return a;}}
 public struct Vector2 {public float x,y;}public struct Vector3 {public Vector3(float a,float b,float c){}}
 public struct Quaternion {public static Quaternion identity=>new Quaternion();}public struct Matrix4x4 {public int value;public static Matrix4x4 identity=>new Matrix4x4();public static Matrix4x4 TRS(Vector3 a,Quaternion q,Vector3 b){return new Matrix4x4();}}
 public class Texture2D:Object {public static Texture2D whiteTexture=new Texture2D(1,1,TextureFormat.RGBA32,false);public Texture2D(int w,int h,TextureFormat f,bool b){}public void SetPixel(int x,int y,Color c){}public void Apply(bool a,bool b){}}
 public class RectOffset {public RectOffset(int a,int b,int c,int d){}}
 public class GUIStyleState {public Color textColor;public Texture2D background;}
 public class GUIStyle {public TextAnchor alignment;public bool wordWrap;public int fontSize;public RectOffset padding,border;public GUIStyleState normal=new GUIStyleState(),hover=new GUIStyleState(),active=new GUIStyleState();public GUIStyle(){}public GUIStyle(GUIStyle s){}public float CalcHeight(GUIContent c,float w){return 40;}}
 public class GUISkin {public GUIStyle label=new GUIStyle(),textField=new GUIStyle(),button=new GUIStyle();}
 public class GUIContent {public string text,tooltip;public GUIContent(string s){text=s;}public GUIContent(string s,string t){text=s;tooltip=t;}}
 public static class GUI {public static string Click,NextText;public static int depth;public static bool enabled=true;public static Color color;public static Matrix4x4 matrix;public class LabelRecord {public string text;public Color color;public Rect bounds;}public static List<LabelRecord> Labels=new List<LabelRecord>();public static GUISkin skin=new GUISkin();public static void DrawTexture(Rect r,Texture2D t){}public static void Label(Rect r,string s,GUIStyle g){Labels.Add(new LabelRecord{text=s,color=g.normal.textColor,bounds=r});}public static bool Button(Rect r,string s,GUIStyle g){return enabled&&Click==s;}public static bool Button(Rect r,GUIContent c,GUIStyle g){return enabled&&Click==c.tooltip;}public static string TextField(Rect r,string s,int n,GUIStyle g){return enabled&&NextText!=null?NextText:s;}public static Vector2 BeginScrollView(Rect a,Vector2 s,Rect b){return s;}public static void EndScrollView(){}}
 public static class Screen {public static int width=1920,height=1080;public static Rect safeArea=new Rect(0,0,1920,1080);}
 public static class Mathf {public static float Min(float a,float b)=>Math.Min(a,b);public static float Max(float a,float b)=>Math.Max(a,b);public static float Sqrt(float a)=>(float)Math.Sqrt(a);public static float Atan2(float a,float b)=>(float)Math.Atan2(a,b);public static float Cos(float a)=>(float)Math.Cos(a);public static float Abs(float a)=>Math.Abs(a);public static float Clamp01(float a)=>Math.Min(1,Math.Max(0,a));}
 public static class Application {public static string dataPath="/tmp/game/Game_Data",persistentDataPath="/tmp/update-gate-contract",temporaryCachePath="/tmp",streamingAssetsPath="/tmp/bundled",version="2.0.0";public static bool isMobilePlatform,isEditor,runInBackground;public static List<string> Opened=new List<string>();public static int Quits;public static void Quit(){Quits++;}public static void OpenURL(string s){Opened.Add(s);}}
 public static class Debug {public static bool isDebugBuild;public static void Log(object s){}public static void LogWarning(object s){}public static void LogError(object s){}}
 public static class JsonUtility {public static T FromJson<T>(string s){throw new Exception("JSON Unity stub must not execute");}}
 public static class PlayerPrefs {public static string GetString(string k,string d)=>d;}public class WaitForEndOfFrame {}
}
namespace UnityEngine.Networking {public class DownloadHandler {public string text;}public class DownloadHandlerFile:DownloadHandler{public bool removeFileOnAbort;public DownloadHandlerFile(string p){}}public class UnityWebRequest:IDisposable {public enum Result {Success}public Result result;public ulong downloadedBytes;public string error;public DownloadHandler downloadHandler;public static UnityWebRequest Get(string s){throw new Exception("Network forbidden in UI contract");}public Op SendWebRequest(){throw new Exception("Network forbidden in UI contract");}public void Dispose(){}public class Op {public bool isDone;}}}
