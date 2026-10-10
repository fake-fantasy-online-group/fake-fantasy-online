using System;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Immutable switches recovered from the locked 1.3.4 GameBootstrap static defaults.
/// </summary>
public sealed class ReferenceWorldRenderOptions
{
    public ReferenceWorldRenderOptions(
        bool useIsoDepth,
        bool useIsoY,
        bool enableOcclusionFade)
    {
        UseIsoDepth = useIsoDepth;
        UseIsoY = useIsoY;
        EnableOcclusionFade = enableOcclusionFade;
    }

    public bool UseIsoDepth { get; }
    public bool UseIsoY { get; }
    public bool EnableOcclusionFade { get; }
}

/// <summary>
/// Source-owned translation of the recovered first-world material setup boundary.
/// </summary>
/// <remarks>
/// The normal 1.3.4 path leaves IsoDepth, IsoY, and occlusion fade disabled. Therefore a null
/// object material is intentional: SpriteRenderer keeps its normal default material. The dormant
/// IsoY branch is retained here because it is part of the original source behavior, but callers
/// must opt in explicitly and own the returned Material.
/// </remarks>
public static class ReferenceWorldRenderSettings
{
    public const bool DefaultUseIsoDepth = false;
    public const bool DefaultUseIsoY = false;
    public const bool DefaultEnableOcclusionFade = false;
    public const string IsoYShaderName = "Custom/IsoDepthY";
    public const string IsoYResourcePath = "FakeFantasyOnline/ReferenceIsoDepthY";
    public const string IsoYObjectMaterialName = "IsoYObj";
    public const string IsoYCharacterMaterialName = "IsoYChar";
    public const string OcclusionFadeMaterialName = "OccFade(shared)";

    private const float IsoYRangeMargin = 30f;
    private const float CharacterBias = -0.4f;
    private const float CharacterCutoff = 0.3f;
    private const float LessEqualCompare = 4f;
    private const float GreaterCompare = 7f;

    public static ReferenceWorldRenderOptions OfficialDefaults()
    {
        return new ReferenceWorldRenderOptions(
            DefaultUseIsoDepth,
            DefaultUseIsoY,
            DefaultEnableOcclusionFade);
    }

    public static float IsoDepthCompareValue()
    {
        return SystemInfo.usesReversedZBuffer ? GreaterCompare : LessEqualCompare;
    }

    public static float ConfigureIsoYGlobals(float worldHeight, float pixelsPerUnit)
    {
        // Official 1.5.8 SetIsoYGlobals (060003ee) has no input guards.
        // Keep its floating-point results and unconditional global-write order.
        float range = worldHeight / (2f * pixelsPerUnit) + IsoYRangeMargin;
        Shader.SetGlobalFloat("_IsoYMin", -range);
        Shader.SetGlobalFloat("_IsoYMax", range);
        Shader.SetGlobalFloat(
            "_IsoReversed",
            SystemInfo.usesReversedZBuffer ? 1f : 0f);
        return range;
    }

    public static Material CreateIsoYObjectMaterial(bool enabled)
    {
        if (!enabled)
        {
            return null;
        }

        Material material = CreateIsoYMaterial(IsoYObjectMaterialName);
        material.SetFloat("_Bias", 0f);
        return material;
    }

    public static Material CreateIsoYCharacterMaterial(bool enabled)
    {
        if (!enabled)
        {
            return null;
        }

        Material material = CreateIsoYMaterial(IsoYCharacterMaterialName);
        material.SetFloat("_Bias", CharacterBias);
        material.SetFloat("_Cutoff", CharacterCutoff);
        return material;
    }

    public static Material CreateOcclusionFadeMaterial(bool enabled)
    {
        if (!enabled)
        {
            return null;
        }

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            throw new InvalidOperationException("Required built-in Sprites/Default shader is missing.");
        }

        Material material = new Material(shader);
        material.name = OcclusionFadeMaterialName;
        return material;
    }

    private static Material CreateIsoYMaterial(string name)
    {
        Shader shader = Resources.Load<Shader>(IsoYResourcePath);
        if (shader == null)
        {
            shader = Shader.Find(IsoYShaderName);
        }

        if (shader == null || shader.name != IsoYShaderName)
        {
            throw new InvalidOperationException(
                "Required source-owned Custom/IsoDepthY shader is missing.");
        }

        Material material = new Material(shader);
        material.name = name;
        material.SetFloat("_ZTestMode", IsoDepthCompareValue());
        return material;
    }
}

}
