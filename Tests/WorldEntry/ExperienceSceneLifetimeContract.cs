using System;
using System.Collections.Generic;
using UnityEngine;
using FakeFantasyOnline.Presentation.ReferenceAssets;

class ExperienceSceneLifetimeContract
{
    static int checks;
    static void Check(bool value, string reason) { if (!value) throw new Exception(reason); checks++; }
    static void Main()
    {
        CombatNumber.GlyphsReady=true;
        Check(CombatNumber.TrySpawnForIndependentRuntime(Vector3.zero,10,4,100), "old scene XP renders");
        var old = CombatNumber.LastCreatedForTest;
        Check(old.gameObject.activeSelf && CombatNumber.ActiveExperienceForTest==1, "rendered XP is tracked");
        Check(CombatNumber.TrySpawnForIndependentRuntime(Vector3.zero,20,0,100), "ordinary damage renders");
        var ordinary = CombatNumber.LastCreatedForTest;
        CombatNumber.ReleaseExperienceForSceneTransition();
        Check(!old.gameObject.activeSelf && CombatNumber.ActiveExperienceForTest==0, "retirement removes already-visible old EXP object");
        Check(ordinary.gameObject.activeSelf, "experience cleanup does not alter ordinary damage animation");
        int pooled=CombatNumber.PooledForTest;
        CombatNumber.ReleaseExperienceForSceneTransition();
        Check(CombatNumber.PooledForTest==pooled, "repeated retirement cannot duplicate pool entries");
        Check(CombatNumber.TrySpawnForIndependentRuntime(Vector3.zero,30,4,100), "genuine new-map XP still spawns");
        Check(old.gameObject.activeSelf && CombatNumber.ActiveExperienceForTest==1, "new scene reuses pooled object normally");
        Time.deltaTime=2; old.TickForTest();
        Check(!old.gameObject.activeSelf && CombatNumber.ActiveExperienceForTest==0, "natural animation completion removes ownership");
        pooled=CombatNumber.PooledForTest; CombatNumber.ReleaseExperienceForSceneTransition();
        Check(CombatNumber.PooledForTest==pooled, "finished animation cannot be pooled twice by scene cleanup");
        CombatNumber.GlyphsReady=false; BitmapFont.Ready=true;
        Check(CombatNumber.TrySpawnForIndependentRuntime(Vector3.zero,40,4,100), "font-backed XP renders");
        Texture2D owned=old.OwnedTextureForTest; Sprite sprite=old.OwnedSpriteForTest;
        Check(owned && sprite, "font-backed popup owns generated artwork");
        CombatNumber.ReleaseExperienceForSceneTransition();
        Check(owned.destroyed && sprite.destroyed, "retirement disposes generated font artwork");
        BitmapFont.Ready=false; pooled=CombatNumber.PooledForTest;
        Check(!CombatNumber.TrySpawnForIndependentRuntime(Vector3.zero,50,4,100), "missing artwork is still reported as unshown");
        Check(CombatNumber.ActiveExperienceForTest==0 && CombatNumber.PooledForTest==pooled, "failed spawn is not tracked or leaked");
        CombatNumber.GlyphsReady=true;
        CombatNumber.RegisterPlayer(new Transform(),1,100); Time.time=1;
        CombatNumber.TrySpawnOnForIndependentRuntime(0,60,4);
        CombatNumber.TrySpawnOnForIndependentRuntime(0,61,4);
        Check(CombatNumber.StackForTest>0, "same-scene rapid XP stacks normally");
        CombatNumber.ReleaseExperienceForSceneTransition();
        Check(CombatNumber.StackForTest==0, "old-scene stack offsets are retired");
        CombatNumber.TrySpawnOnForIndependentRuntime(0,62,4);
        Check(CombatNumber.StackForTest==0 && CombatNumber.ActiveExperienceForTest==1, "first new-scene reward starts at correct anchor without old stack");
        var active=CombatNumber.FirstActiveForTest; active.DestroyForTest();
        Check(CombatNumber.ActiveExperienceForTest==0, "external destruction releases tracked owner");
        CombatNumber.ReleaseExperienceForSceneTransition();
        Check(CombatNumber.ActiveExperienceForTest==0, "scene release remains safe after destruction");
        CombatNumber.RegisterPlayer(new Transform(),1,100);
        CombatNumber.TrySpawnOnForIndependentRuntime(0,70,4);
        Check(CombatNumber.ActiveExperienceForTest==1, "reconnected local player can display fresh XP");
        CombatNumber.BindPlayerTransformForIndependentRuntime(null);
        Check(CombatNumber.ActiveExperienceForTest==0 && CombatNumber.PlayerTransform==null, "terminal world detach retires active XP on disconnect");
        CombatNumber.RegisterPlayer(new Transform(),1,100);
        Check(CombatNumber.TrySpawnOnForIndependentRuntime(0,71,4) && CombatNumber.ActiveExperienceForTest==1, "new connection does not inherit retired active objects");
        CombatNumber.ReleaseExperienceForSceneTransition();
        Console.WriteLine("PASS " + checks + " production experience object/pool/animation scene-lifetime assertions (managed substitutes)");
    }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public static class CharData { public static bool riding; }
    public static class BitmapFont
    {
        public static bool Ready;
        public static Texture2D BakeNameTag(string text,string title,Color a,Color b,int padding) => new Texture2D {width=20,height=12};
    }
    public partial class CombatNumber
    {
        public static bool GlyphsReady; private static int glyphScale=1;
        private static List<Sprite> BuildGlyphs(int value,int type) => GlyphsReady ? new List<Sprite>{new Sprite {rect=new Rect(0,0,10,10)}} : null;
        public static int ActiveExperienceForTest => activeExperience.Count;
        public static int PooledForTest => _pool.Count;
        public static int StackForTest => spawnStack;
        public static CombatNumber LastCreatedForTest => GameObject.LastCombatNumber;
        public static CombatNumber FirstActiveForTest {get {foreach(var value in activeExperience) return value; return null;}}
        public Texture2D OwnedTextureForTest => _ownedTex;
        public Sprite OwnedSpriteForTest => _ownedSp;
        public void TickForTest() => Update();
        public void DestroyForTest() {OnDestroy(); destroyed=true;}
    }
}
namespace UnityEngine
{
    public class Object { public bool destroyed; public static implicit operator bool(Object value) => value!=null && !value.destroyed; public static void Destroy(Object value) {if(value!=null)value.destroyed=true;} }
    public class Component : Object {public GameObject gameObject;public Transform transform=>gameObject.transform;}
    public class MonoBehaviour : Component { }
    public class GameObject : Object
    {
        public static CombatNumber LastCombatNumber;
        public bool activeSelf=true; public Transform transform=new Transform();
        public GameObject(string name) { }
        public void SetActive(bool active)=>activeSelf=active;
        public T AddComponent<T>() where T:Component,new() {var value=new T {gameObject=this}; if(value is CombatNumber number)LastCombatNumber=number; return value;}
    }
    public class Transform : Object {public Vector3 position,localPosition,localScale; public void SetParent(Transform parent,bool worldPositionStays) {}}
    public class Shader : Object {public static Shader Find(string path)=>new Shader();}
    public class Material : Object {public Material(Shader shader) {}}
    public class Texture2D : Object {public int width,height;}
    public class Sprite : Object {public Rect rect;public static Sprite Create(Texture2D texture,Rect rect,Vector2 pivot,float ppu)=>new Sprite {rect=rect};}
    public class SpriteRenderer : Component {public int sortingOrder;public Material sharedMaterial;public Sprite sprite;public Color color;}
    public struct Rect {public float width,height; public Rect(float x,float y,float w,float h){width=w;height=h;}}
    public struct Vector2 {public Vector2(float x,float y) {}}
    public struct Vector3 {public float x,y,z;public Vector3(float x,float y,float z){this.x=x;this.y=y;this.z=z;}public static Vector3 zero=>new Vector3();public static Vector3 one=>new Vector3(1,1,1);public static Vector3 operator+(Vector3 a,Vector3 b)=>new Vector3(a.x+b.x,a.y+b.y,a.z+b.z);}
    public struct Color {public float a;public Color(float r,float g,float b,float alpha=1){a=alpha;}public static Color white=>new Color(1,1,1);}
    public static class Time {public static float time,deltaTime;public static int frameCount;}
    public static class Mathf {public const float PI=(float)Math.PI;public static float Max(float a,float b)=>Math.Max(a,b);public static int Max(int a,int b)=>Math.Max(a,b);public static float Min(float a,float b)=>Math.Min(a,b);public static float Clamp01(float v)=>Math.Max(0,Math.Min(1,v));public static float Sin(float value)=>(float)Math.Sin(value);public static float Round(float value)=>(float)Math.Round(value);}
    public static class Random {public static float value=>.5f;}
}
