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
    // retime, or rename the original assets. Playback follows each template's
    // authored lifeMs; scale and chest height follow the host actor.
    internal const string GenericBleedEffect = "TFX1231_70";   // effect/TFX1231_70.eff, curated 血-1 E0282
    internal const string GenericRadiantEffect = "lizi";       // effect/lizi.eff, curated 光-3 E0034
    // Catalog lifeMs of the chosen templates (E0282=1900, E0034=1085). Fallback
    // only until tpl.lifeMs is available; not a user-specified delay.
    internal const float GenericBleedLifeMs = 1900f;
    internal const float GenericRadiantLifeMs = 1085f;
    internal const float GenericStatusDOTRestFraction = 0.18f;
    internal const float GenericStatusDOTRestMinMs = 180f;
    internal const float GenericStatusDOTChestFactor = 0.45f;
    internal const float GenericStatusDOTScaleMin = 0.60f;
    internal const float GenericStatusDOTScaleMax = 1.10f;
    internal const float GenericStatusDOTHeightScale = 0.50f;
    internal const float GenericStatusDOTHeightBias = 0.25f;

    private bool genericStatusDOT;
    private bool genericStatusDOTFirstFrame;
    internal static int GenericDOTLiveForAudit { get; private set; }
    internal static int GenericDOTRenderersForAudit { get; private set; }
    internal static int GenericDOTCreatedForAudit { get; private set; }
    internal static float GenericDOTScaleForAudit { get; private set; }
    internal static float GenericDOTLoopDelayForAudit { get; private set; }
    internal static float GenericDOTBodyHeightForAudit { get; private set; }
    internal bool IsGenericDOTForAudit => genericStatusDOT;

    internal static float RestDelayMs(float lifeMs)
    {
        if (lifeMs <= 0f) lifeMs = GenericBleedLifeMs;
        return Mathf.Max(GenericStatusDOTRestMinMs, lifeMs * GenericStatusDOTRestFraction);
    }

    internal static float ScaleForBody(float bodyHeightU)
    {
        float height = Mathf.Max(bodyHeightU, 0.8f);
        return Mathf.Clamp(GenericStatusDOTHeightScale * height + GenericStatusDOTHeightBias,
            GenericStatusDOTScaleMin, GenericStatusDOTScaleMax);
    }

    internal static float FallbackLifeMs(string asset)
    {
        return asset == GenericRadiantEffect ? GenericRadiantLifeMs : GenericBleedLifeMs;
    }

    internal float ChestLocalY(float bodyHeightU, float mountY)
    {
        return mountY + Mathf.Max(bodyHeightU, 0.8f) * GenericStatusDOTChestFactor;
    }

    internal void ConfigureGenericStatusPresentation(float bodyHeightU)
    {
        if (!genericStatusDOT) return;
        float life = tpl != null && tpl.lifeMs > 0f ? tpl.lifeMs : GenericBleedLifeMs;
        float scale = ScaleForBody(bodyHeightU);
        transform.localScale = new Vector3(scale, scale, 1f);
        loopMode = 3;
        loopDelay = RestDelayMs(life);
        selfDestroy = false;
        attachUnder = false;
        GenericDOTScaleForAudit = scale;
        GenericDOTLoopDelayForAudit = loopDelay;
        GenericDOTBodyHeightForAudit = bodyHeightU;
    }

    private static EffectPlayer CreateNativeStatusDOT(Vector3 position, string asset, float pixelsPerUnit, int facing, int direction, bool flip, bool follow)
    {
        // Provisional mid scale; Sync immediately retunes to the host body.
        NextScale = ScaleForBody(1.2f);
        var ep = Play(position, asset, pixelsPerUnit, facing, direction, flip, follow);
        NextScale = 1f;
        if (!ep) return null;
        ep.genericStatusDOT = true;
        ep.genericStatusDOTFirstFrame = true;
        ep.selfDestroy = false;
        ep.loopMode = 3;
        float life = ep.tpl != null && ep.tpl.lifeMs > 0f ? ep.tpl.lifeMs : FallbackLifeMs(asset);
        ep.loopDelay = RestDelayMs(life);
        GenericDOTLiveForAudit++;
        GenericDOTCreatedForAudit++;
        GenericDOTRenderersForAudit += ep.rends == null ? 0 : ep.rends.Length;
        GenericDOTLoopDelayForAudit = ep.loopDelay;
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
