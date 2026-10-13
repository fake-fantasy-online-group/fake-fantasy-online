// Focused execution of the production rift identity/glow owner without Unity.
using System;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
namespace UnityEngine
{
    public class Object { public bool destroyed; public static void Destroy(Object value) { value.destroyed = true; } }
    public class MonoBehaviour : Object { public Transform transform = new Transform(); }
    public class Transform { public Vector3 localScale; public Transform parent; public void SetParent(Transform value, bool worldPositionStays) { parent = value; } }
    public struct Vector3 { public float x,y,z; public Vector3(float x,float y,float z) { this.x=x;this.y=y;this.z=z; } }
    public struct Color { public float r,g,b,a; public Color(float r,float g,float b,float a) { this.r=r;this.g=g;this.b=b;this.a=a; } }
    public class GameObject : Object { public bool active = true; public void SetActive(bool value) { active = value; } public Transform transform = new Transform(); public GameObject(string name) { } public T AddComponent<T>() where T:new() { var value = new T(); if (value is SpriteRenderer) (value as SpriteRenderer).gameObject = this; return value; } }
    public class Sprite { }
    public class Material { public int mode; }
    public class SpriteRenderer : Object { public GameObject gameObject = new GameObject("sprite"); public Transform transform => gameObject.transform; public Sprite sprite; public bool flipX, enabled = true; public int sortingOrder, sortingLayerID; public Material sharedMaterial; public Color color; }
}
namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
    public partial class MonsterEntity : MonoBehaviour
    {
        public string mname = "寒林狼"; public SpriteRenderer body = new SpriteRenderer(); public bool dying; public EffectPlayer eliteRing = new EffectPlayer();
        internal SpriteRenderer Glow => riftBodyGlow;
    }
    public partial class EffectPlayer
    {
        public SpriteRenderer[] rends = { new SpriteRenderer { sharedMaterial = new Material { mode = 0 } } };
        public static Material GlowLayerMat(int mode) => new Material { mode = mode };
    }
}
static class RiftMonsterVisualContract
{
    static int count;
    static void Check(bool condition, string text) { if (!condition) throw new Exception(text); count++; }
    public static void Main()
    {
        var m = new MonsterEntity(); m.body.sprite = new Sprite(); m.body.sortingOrder = 100; m.body.sortingLayerID = 7; m.body.flipX = true;
        Material original = m.eliteRing.rends[0].sharedMaterial;
        m.UpdateRiftIdentity(1, "寒林狼");
        Check(m.mname == "寒林狼（精英）" && m.RiftClass == 1, "elite preserves original name and uses exact suffix");
        Check(m.Glow.sharedMaterial.mode == 8 && m.eliteRing.rends[0].sharedMaterial.mode == 8, "elite model and native ring glow blue");
        Check(m.Glow.sprite == m.body.sprite && m.Glow.flipX && m.Glow.sortingOrder == 102 && m.Glow.sortingLayerID == 7 && m.Glow.transform.parent == m.body.transform, "glow reuses real current monster frame, facing and order");
        var glow = m.Glow; m.UpdateRiftIdentity(1, "寒林狼"); Check(ReferenceEquals(glow,m.Glow) && m.mname == "寒林狼（精英）", "polling does not duplicate glow or suffix");
        m.UpdateRiftIdentity(2, "寒林狼"); Check(m.mname == "寒林狼（首领）" && m.Glow.sharedMaterial.mode == 5 && m.eliteRing.rends[0].sharedMaterial.mode == 5, "leader is exact original name with gold glow");
        m.dying = true; m.UpdateRiftIdentity(2, "寒林狼"); Check(!m.Glow.enabled, "dead monster never leaves live identity glow");
        m.ClearRiftIdentity(); Check(m.mname == "寒林狼" && m.RiftClass == 0 && m.Glow == null && glow.gameObject.destroyed, "context loss restores native name and destroys owned glow");
        Check(ReferenceEquals(original,m.eliteRing.rends[0].sharedMaterial), "context loss restores native ring material exactly");
        m.UpdateRiftIdentity(3, "古林石妖"); Check(m.mname == "古林石妖（镇境劫影）", "guardian uses actual template name even if native spawn used legacy generic name");
        Check(MonsterEntity.RiftMonsterName("寒林狼（精英）",1) == "寒林狼（精英）", "already suffixed native name stays single");
        m.ClearRiftIdentity(); m.ClearRiftIdentity(); Check(m.mname == "寒林狼", "cleanup is idempotent");
        Console.WriteLine("PASS " + count + " production monster name/glow lifecycle contracts; no Unity render.");
    }
}
