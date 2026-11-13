using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>Source-owned status art, not skill art or a repacked package. Six shared
/// textures/two sprites per process; each live effect owns exactly six renderers.</summary>
internal static class GenericDotVisuals
{
    private static readonly Texture2D[] icons = new Texture2D[4];
    static GenericDotVisuals() { UnityEngine.Application.quitting += Release; }
    internal static Texture2D Icon(int id)
    {
        int k = id - 190001;
        if (k < 0 || k > 3) return null;
        if (!icons[k]) icons[k] = Make(k, true);
        return icons[k];
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
            }
            pixels[y*32+x] = c;
        }
        tex.SetPixels32(pixels); tex.Apply(false,!icon); return tex;
    }
    private static void Release()
    {
        foreach (var t in icons) if (t) UnityEngine.Object.Destroy(t);
        Array.Clear(icons,0,icons.Length);
    }
}

public partial class EffectPlayer
{
    // User-chosen existing package FX, referenced in place. Do not retint,
    // retime, or rename the original assets. Scale and a low-frequency replay
    // only keep a short native burst readable as a sustained status.
    internal const string GenericBleedEffect = "TFX1231_70";   // effect/TFX1231_70.eff, curated 血-1 E0282
    internal const string GenericRadiantEffect = "lizi";       // effect/lizi.eff, curated 光-3 E0034
    internal const float GenericStatusDOTScale = 0.30f;
    internal const float GenericStatusDOTLoopDelayMs = 700f;

    private bool genericStatusDOT;
    private bool genericStatusDOTFirstFrame;
    internal static int GenericDOTLiveForAudit { get; private set; }
    internal static int GenericDOTRenderersForAudit { get; private set; }
    internal bool IsGenericDOTForAudit => genericStatusDOT;
    private static EffectPlayer CreateNativeStatusDOT(Vector3 position, string asset, float scale, float pixelsPerUnit, int facing, int direction, bool flip, bool follow)
    {
        NextScale = scale;
        var ep = Play(position, asset, pixelsPerUnit, facing, direction, flip, follow);
        NextScale = 1f;
        if (!ep) return null;
        ep.genericStatusDOT = true;
        ep.genericStatusDOTFirstFrame = true;
        ep.selfDestroy = false;
        ep.loopMode = 3;
        ep.loopDelay = GenericStatusDOTLoopDelayMs;
        GenericDOTLiveForAudit++;
        GenericDOTRenderersForAudit += ep.rends == null ? 0 : ep.rends.Length;
        return ep;
    }
    private void OnDestroy()
    {
        if (genericStatusDOT)
        {
            GenericDOTLiveForAudit--;
            GenericDOTRenderersForAudit -= rends == null ? 0 : rends.Length;
            genericStatusDOT=false;
        }
        // Shared icon textures are process-owned; borrowed materials are never destroyed.
    }
}
}
