using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Only Unity construction/art is substituted. Registry mutation, native ground
// spawn/removal, NPC receive/publication and all wire decoding are production code.
namespace UnityEngine
{
    public class MonoBehaviour : Object { }
    public partial class GameObject
    {
        readonly Dictionary<Type,object> components=new Dictionary<Type,object>();
        public T AddComponent<T>() where T:new() { var value=new T();components[typeof(T)]=value;return value; }
        public T GetComponent<T>() where T:class => components.TryGetValue(typeof(T),out object value)?value as T:null;
    }
    public partial struct Vector2
    {
        public static explicit operator Vector2(Vector3 value)=>default;
        public static Vector2 operator *(Vector2 value,float number)=>value;
    }
    public struct Vector3
    {
        public Vector3(float x,float y,float z) { }
        public static Vector3 operator *(Vector3 value,float number)=>value;
    }
    public struct Bounds { public Vector3 size; }
    public class Sprite { public Bounds bounds; }
    public class SpriteRenderer { public Sprite sprite; public int sortingOrder; }
    public class BoxCollider2D { public Vector2 size; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public sealed partial class PlayerController { public float ppu=100,worldW=1000,worldH=1000; }
    internal class GroundItemEntity
    {
        public int id,gx,gy;
        public void ConfigureUniqueDrop(int unique,bool fresh,long token) { }
        public void CancelUniqueDrop() { }
    }
    internal static class TexScale { public static Sprite MakeSprite(Texture2D value,float ppu,float x,float y)=>null; }
    internal partial class GroundItems
    {
        static Texture2D LoadIcon(int id)=>null;
        internal static IEnumerable<string> KeysForTest()=>I==null?Array.Empty<string>():I.items.Keys.Select(id=>"32787:"+id).ToArray();
        internal static GameObject ObjectForTest(int id)=>I!=null&&I.items.TryGetValue(id,out Item item)?item.go:null;
        internal static bool AllObjectsAliveForTest()=>I==null||I.items.Values.All(item=>item.go!=null&&!item.go.Destroyed);
    }
    public partial class GameBootstrap
    {
        public static GameBootstrap I;
        public GameObject TestLocalNpc;
        static readonly Dictionary<int,NpcEntry> _netNpcs=new Dictionary<int,NpcEntry>();
        static readonly Dictionary<int,GameObject> _netNpcGo=new Dictionary<int,GameObject>();
        static readonly List<Vector2> NpcPts=new List<Vector2>();
        GameObject SpawnNpcEntry(NpcEntry e,Transform parent,float w,float h,float ppu)=>new GameObject(e.name);
        public static void ResetNpcReceiptsForTest()=>ClearNetNpcs();
    }
}
