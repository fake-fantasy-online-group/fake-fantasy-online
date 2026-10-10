using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official EffectPlayer material and glow-mode owners.
/// </summary>
public partial class EffectPlayer : MonoBehaviour
{
    private static Material AlphaMat()
    {
        if (alphaMat != null)
        {
            return alphaMat;
        }

        Shader shader = Shader.Find("fake-fantasy-online/EffectGL");
        if (shader == null)
        {
            alphaMat = new Material(Shader.Find("Sprites/Default"));
            return alphaMat;
        }

        alphaMat = new Material(shader);
        alphaMat.SetFloat("_SrcF", 5f);
        alphaMat.SetFloat("_DstF", 10f);
        alphaMat.SetFloat("_Add", 0f);
        return alphaMat;
    }

    private static Material AddMat()
    {
        if (addMat != null)
        {
            return addMat;
        }

        Shader shader = Shader.Find("fake-fantasy-online/EffectGL");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Additive");
                if (shader == null)
                {
                    shader = Shader.Find("Sprites/Default");
                }
            }

            addMat = new Material(shader);
            if (addMat.HasProperty("_TintColor"))
            {
                addMat.SetColor("_TintColor", Color.white);
            }
            return addMat;
        }

        addMat = new Material(shader);
        addMat.SetFloat("_SrcF", 5f);
        addMat.SetFloat("_DstF", 1f);
        addMat.SetFloat("_Add", 0f);
        return addMat;
    }

    public static int GlowHueOf(int mode)
    {
        if (mode <= 2)
        {
            return -1;
        }

        return mode > 9 ? mode - 10 : mode - 3;
    }

    public static bool GlowUsable(int mode, int unlockMask)
    {
        if (mode < 0 || mode >= 17)
        {
            return false;
        }

        if (mode <= 2)
        {
            return mode != 0;
        }

        return (unlockMask & (1 << GlowHueOf(mode))) != 0;
    }

    public static int GlowUsableCount(int unlockMask)
    {
        int count = 2;
        for (int i = 0; i < 7; i++)
        {
            if ((unlockMask & (1 << i)) != 0)
            {
                count += 2;
            }
        }
        return count;
    }

    public static int GlowNextMode(int cur, int unlockMask)
    {
        int next = cur % 17 + 18;
        for (int i = 0; i < 17; i++)
        {
            int mode = next % 17;
            if (GlowUsable(mode, unlockMask))
            {
                return mode;
            }
            next++;
        }
        return 1;
    }

    public static Material GlowLayerMat(int mode, bool premul)
    {
        if (!premul) return GlowLayerMat(mode);
        int normalized = mode >= 1 && mode <= 16 ? mode : 1;
        if (normalized != 2 && normalized < 10) return GlowLayerMat(mode);
        if (glowMatsPre[normalized] != null) return glowMatsPre[normalized];
        Material source = GlowLayerMat(normalized);
        if (source.shader == null || source.shader.name != "fake-fantasy-online/EffectGL") return source;
        Material result = new Material(source);
        result.SetFloat("_PreMul", 1f);
        glowMatsPre[normalized] = result;
        return result;
    }

    public static Material GlowLayerMat(int mode)
    {
        if (mode < 1 || mode > 16)
        {
            mode = 1;
        }

        if (glowMats[mode] != null)
        {
            return glowMats[mode];
        }

        Shader shader = Shader.Find("fake-fantasy-online/EffectGL");
        if (shader == null)
        {
            Material fallback = new Material(Shader.Find("Sprites/Default"));
            glowMats[mode] = fallback;
            LastGlowDiag = "★EffectGL 没找到(build里被剥掉?) → 退 Sprites/Default, 光效档全无效!";
            Debug.LogWarning("[GlowLayerMat] " + LastGlowDiag);
            return fallback;
        }

        Material material = new Material(shader);
        // APPROVED-DEVIATION: GLOW-B-001; this factory alone enables the approved behavior.
        material.SetFloat("_ApprovedGlowB", 1f);
        bool max = mode == 2 || mode > 9;
        float src = max ? 1f : 5f;
        const float dst = 1f;
        float op = max ? 4f : 0f;
        float add = max ? 1f : 0f;
        bool tint = mode > 2;
        material.SetFloat("_SrcF", src);
        material.SetFloat("_DstF", dst);
        material.SetFloat("_Op", op);
        material.SetFloat("_Add", add);
        material.SetFloat("_TintMode", tint ? 1f : 0f);
        material.SetColor("_Tint", tint ? GlowTint[GlowHueOf(mode)] : Color.white);
        LastGlowDiag = string.Format(
            "EffectGL ok 档{0}({1}) src={2} dst={3} op={4} add={5} tint={6}",
            mode,
            GlowModeName[mode],
            src,
            dst,
            op,
            add,
            tint ? 1 : 0);
        glowMats[mode] = material;
        return material;
    }

    public static Material GlowLayerMat()
    {
        return GlowLayerMat(1);
    }

    public static Material NpcGlowLayerMat()
    {
        if (npcGlowMat != null)
        {
            return npcGlowMat;
        }

        Shader shader = Shader.Find("fake-fantasy-online/EffectGL");
        npcGlowMat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        if (shader != null)
        {
            npcGlowMat.SetFloat("_SrcF", 5f);
            npcGlowMat.SetFloat("_DstF", 1f);
            npcGlowMat.SetFloat("_Op", 0f);
            npcGlowMat.SetFloat("_Add", 0f);
            npcGlowMat.SetFloat("_TintMode", 0f);
            npcGlowMat.SetColor("_Tint", Color.white);
        }
        return npcGlowMat;
    }

    public static Material LayerAddMat()
    {
        if (layerAddMat != null)
        {
            return layerAddMat;
        }

        Shader shader = Shader.Find("fake-fantasy-online/EffectGL");
        layerAddMat = new Material(shader != null ? shader : Shader.Find("Sprites/Default"));
        if (shader != null)
        {
            layerAddMat.SetFloat("_SrcF", 5f);
            layerAddMat.SetFloat("_DstF", 1f);
            layerAddMat.SetFloat("_Op", 0f);
            layerAddMat.SetFloat("_Add", 0f);
        }
        return layerAddMat;
    }

    private static Material MaxMat()
    {
        if (maxMat != null)
        {
            return maxMat;
        }

        Shader shader = Shader.Find("fake-fantasy-online/EffectGL");
        if (shader == null)
        {
            maxMat = AddMat();
            return maxMat;
        }

        maxMat = new Material(shader);
        maxMat.SetFloat("_SrcF", 1f);
        maxMat.SetFloat("_DstF", 1f);
        maxMat.SetFloat("_Op", 4f);
        maxMat.SetFloat("_Add", 0f);
        return maxMat;
    }

    private static Material ZeroMat()
    {
        if (zeroMat != null)
        {
            return zeroMat;
        }

        Shader shader = Shader.Find("fake-fantasy-online/EffectGL");
        if (shader == null)
        {
            zeroMat = AlphaMat();
            return zeroMat;
        }

        zeroMat = new Material(shader);
        zeroMat.SetFloat("_SrcF", 5f);
        zeroMat.SetFloat("_DstF", 0f);
        zeroMat.SetFloat("_Add", 0f);
        return zeroMat;
    }
}

}
