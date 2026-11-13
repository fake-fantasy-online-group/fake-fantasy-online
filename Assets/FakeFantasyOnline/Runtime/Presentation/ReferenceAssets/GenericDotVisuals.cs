using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Source-owned status art, not skill art or a repacked package. Six shared
/// textures/two sprites per process; each live effect owns exactly six renderers.</summary>
internal static class GenericDotVisuals
{
    private static readonly Texture2D[] icons = new Texture2D[4];
    private static readonly Texture2D[] textures = new Texture2D[2];
    private static readonly Sprite[] sprites = new Sprite[2];
    static GenericDotVisuals() { UnityEngine.Application.quitting += Release; }
    internal static Texture2D Icon(int id)
    {
        int k = id - 190001;
        if (k < 0 || k > 3) return null;
        if (!icons[k]) icons[k] = Make(k, true);
        return icons[k];
    }
    internal static Sprite Shape(int k)
    {
        if (!sprites[k])
        {
            textures[k] = Make(k == 0 ? 0 : 4, false);
            sprites[k] = Sprite.Create(textures[k], new Rect(0,0,32,32), new Vector2(.5f,.5f), 32f, 0, SpriteMeshType.FullRect);
            sprites[k].name = "DOT_shared_shape_" + k;
            sprites[k].hideFlags = HideFlags.HideAndDontSave;
        }
        return sprites[k];
    }
    private static Texture2D Make(int kind, bool icon)
    {
        var tex = new Texture2D(32,32,TextureFormat.RGBA32,false) { name = "DOT_" + (icon ? "icon_" : "particle_") + kind, filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, hideFlags = HideFlags.HideAndDontSave };
        var pixels = new Color32[1024];
        for (int y=0; y<32; y++) for (int x=0; x<32; x++)
        {
            float u = (x-15.5f)/16f, v = (y-15.5f)/16f;
            bool ink = false; Color c = Color.clear;
            if (icon)
            {
                c = new Color(.035f,.045f,.065f,.96f);
                if (x==1 || x==30 || y==1 || y==30) c = new Color(.40f,.36f,.28f,1);
            }
            switch(kind)
            {
                case 0: // A falling blood drop, convex lower bowl and a pointed upper tip.
                    ink = u*u+(v+.20f)*(v+.20f)<.43f*.43f || v>=-.12f && v<.81f && Mathf.Abs(u)<(.81f-v)*.48f;
                    c = ink ? new Color(.78f + .18f*Mathf.Clamp01(v), .025f,.055f,1) : c;
                    if (ink && u<-.09f && u>-.21f && v>-.18f && v<.18f) c = new Color(1,.40f,.43f,1);
                    break;
                case 1: // Poison vial: narrow neck, stopper and green liquid, not a skill glyph.
                    ink = Mathf.Abs(u)<.44f && v>-.65f && v<.20f || Mathf.Abs(u)<.18f && v>=.20f && v<.65f;
                    c = ink ? (v<-.10f ? new Color(.18f,.87f,.23f,1) : new Color(.55f,.76f,.58f,1)) : c;
                    if (Mathf.Abs(u)<.24f && v>=.63f && v<.79f) c = new Color(.55f,.40f,.24f,1);
                    break;
                case 2: // Flame silhouette with an asymmetrical tip and a bright inner flame.
                    ink = u*u+(v+.25f)*(v+.25f)<.46f*.46f || v>=-.15f && v<.83f && Mathf.Abs(u-.13f*v)<(.83f-v)*.48f;
                    c = ink ? new Color(1,.26f,.025f,1) : c;
                    if (ink && Mathf.Abs(u)<.19f && v<.18f && v>-.56f) c = new Color(1,.91f,.20f,1);
                    break;
                case 3: // A sustained light scorch: a four-point white/gold star and embers.
                    ink = Mathf.Abs(u)+Mathf.Abs(v)<.44f || Mathf.Abs(u)<.09f && Mathf.Abs(v)<.83f || Mathf.Abs(v)<.09f && Mathf.Abs(u)<.70f;
                    c = ink ? (Mathf.Abs(u)+Mathf.Abs(v)<.20f ? Color.white : new Color(1,.76f,.20f,1)) : c;
                    break;
                case 4: // A small irregular blood stain. It is reused and expires in <1 second.
                    ink = u*u/.50f + v*v/.075f<1 || (u+.35f)*(u+.35f)/.05f + (v-.16f)*(v-.16f)/.025f<1 || (u-.40f)*(u-.40f)/.035f + (v+.13f)*(v+.13f)/.018f<1;
                    c = ink ? new Color(.55f,.015f,.025f,.90f) : c;
                    break;
            }
            pixels[y*32+x] = c;
        }
        tex.SetPixels32(pixels); tex.Apply(false,!icon); return tex;
    }
    private static void Release()
    {
        foreach (var s in sprites) if (s) UnityEngine.Object.Destroy(s);
        foreach (var t in textures) if (t) UnityEngine.Object.Destroy(t);
        foreach (var t in icons) if (t) UnityEngine.Object.Destroy(t);
        Array.Clear(sprites,0,sprites.Length); Array.Clear(textures,0,textures.Length); Array.Clear(icons,0,icons.Length);
    }
}

public partial class EffectPlayer
{
    private bool genericDOT;
    private bool genericNativeRadiant;
    private bool genericNativeRadiantFirstFrame;
    private bool genericFollowWasSet;
    private float genericClock;
    internal static int GenericDOTLiveForAudit { get; private set; }
    internal static int GenericDOTRenderersForAudit { get; private set; }
    internal bool IsGenericDOTForAudit => genericDOT || genericNativeRadiant;
    private static EffectPlayer CreateNativeRadiantDOT(Vector3 position, float pixelsPerUnit, int facing, int direction, bool flip, bool follow)
    {
        // Verified in the real Player: existing white/gold light ball + flare.
        // Keep original package frames, materials and full timing; only bound size
        // and repeat cadence for a sustained status. This is not a cast/charge FX.
        var ep = Play(position, "TFX1043(光球)", pixelsPerUnit, facing, direction, flip, follow);
        if (!ep) return null;
        ep.genericNativeRadiant = true;
        ep.genericNativeRadiantFirstFrame = true;
        ep.transform.localScale = new Vector3(.42f,.42f,1f);
        ep.selfDestroy = false;
        GenericDOTLiveForAudit++;
        GenericDOTRenderersForAudit += ep.rends.Length;
        return ep;
    }
    private static EffectPlayer CreateGenericDOT(Vector3 position, string name)
    {
        var go = new GameObject("DOT:" + name);
        go.transform.position = position;
        var ep = go.AddComponent<EffectPlayer>();
        ep.genericDOT = true;
        ep.rends = new SpriteRenderer[6];
        for (int i=0;i<6;i++)
        {
            var child = new GameObject(i<3 ? "Drop" : "TimedTrace"); child.transform.SetParent(go.transform,false);
            var sr = child.AddComponent<SpriteRenderer>();
            sr.sprite = GenericDotVisuals.Shape(i<3 ? 0 : 1);
            sr.sharedMaterial = BorrowAlphaMaterial();
            sr.color = Color.clear;
            ep.rends[i] = sr;
        }
        GenericDOTLiveForAudit++;
        GenericDOTRenderersForAudit += 6;
        return ep;
    }
    private void TickGenericDOT()
    {
        if (followTarget) { genericFollowWasSet = true; transform.position = followTarget.position; }
        else if (genericFollowWasSet) { Destroy(gameObject); return; }
        var actor = GetComponentInParent<ActorEffects>();
        var body = attachSort ? attachSort : actor ? actor.playerSr : null;
        float h = Mathf.Clamp(body && body.sprite ? body.bounds.size.y : actor ? actor.bodyHeightU : 1f, .55f, 1.6f);
        float foot = body && body.sprite ? body.bounds.min.y - transform.position.y : 0f;
        int order = body ? body.sortingOrder : 31000;
        genericClock += Time.deltaTime;
        // Three staggered drops and three independently fading traces. No Instantiate,
        // texture creation, material clone, coroutine or unbounded emission per frame.
        for (int i=0;i<3;i++)
        {
            float phase = Mathf.Repeat(genericClock+i*.43f,1.29f);
            var drop = rends[i]; var trace = rends[i+3];
            float x = (i-1)*h*.16f;
            float fall = Mathf.Clamp01(phase/.72f);
            float alpha = phase<.72f ? Mathf.Min(1f,phase/.06f) : 0f;
            drop.transform.localPosition = new Vector3(x,foot+h*(.62f-.58f*fall*fall),0);
            drop.transform.localScale = Vector3.one*h*.16f;
            drop.sortingOrder = order+20+i;
            drop.color = new Color(1,1,1,alpha*.95f);
            float age = phase-.68f;
            trace.transform.localPosition = new Vector3(x,foot+.012f,0);
            trace.transform.localScale = Vector3.one*h*.34f;
            trace.sortingOrder = order+8+i;
            trace.color = new Color(1,1,1, age>=0 ? Mathf.Clamp01(1-age/.61f)*.8f : 0f);
        }
    }
    private void OnDestroy()
    {
        if (genericDOT || genericNativeRadiant)
        {
            GenericDOTLiveForAudit--;
            GenericDOTRenderersForAudit -= rends == null ? 0 : rends.Length;
            genericDOT=false; genericNativeRadiant=false;
        }
        // Shared textures/sprites are process-owned; borrowed materials are never destroyed.
    }
}
}
