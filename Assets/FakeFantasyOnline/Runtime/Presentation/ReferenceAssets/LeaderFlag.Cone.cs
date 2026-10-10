using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Official 1.5.8 LeaderFlag rendering cone: 06000584 Mat, 06000585 Tick and 06000586 OnDestroy.
/// 06000587 is the field-initializer constructor (ppu = 100f plus the base MonoBehaviour
/// constructor) and is recorded as an evidence-backed exclusion instead.
/// </summary>
/// <remarks>
/// The flag texture statics flagTex and flagTried already live in LeaderFlag.Texture.cs at the
/// frozen offsets 0 and 8, so this file only adds the fields the cone itself owns: the statics
/// ownMat at 16 and matTried at 24, and the instance fields ppu at 32, mat at 40, flagGo at 48
/// and flagSr at 56, declared in frozen field-table order.
/// </remarks>
public partial class LeaderFlag
{
    // Official private const System.Single LeaderFlag.HalfBodyPx. It is only observable through
    // the Tick localPosition x term, which both slices emit as -19.0 divided by ppu; whether the
    // official constant stored 19 or -19 cannot be resolved from the frozen exports because both
    // fold to the same literal, so the value is taken as 19 and negated at the use site.
    private const float HalfBodyPx = 19f;

    // Official instance fields, frozen field-table order: ppu 32, mat 40, flagGo 48, flagSr 56.
    // ppu carries the 100f initializer that 06000587 writes before the base constructor call.
    private float ppu = 100f;
    private Material mat;
    private GameObject flagGo;
    private SpriteRenderer flagSr;

    // Official static fields, frozen field-table order: ownMat 16, matTried 24.
    private static Material ownMat;
    private static bool matTried;

    // Official 0x06000584, instance UnityEngine.Material LeaderFlag.Mat(): return the instance
    // material when one is already set, otherwise lazily build the shared material from the
    // Overlay/NameTag shader at most once and hand that back even when the shader is missing.
    private Material Mat()
    {
        if (mat != null) return mat;

        if (!matTried)
        {
            matTried = true;
            Shader shader = Shader.Find("Overlay/NameTag");
            if (shader != null) ownMat = new Material(shader);
        }

        return ownMat;
    }

    // Official 0x06000585, instance System.Void LeaderFlag.Tick(System.Boolean, System.Single).
    public void Tick(bool show, float headTopY)
    {
        if (!show)
        {
            if (flagGo == null) return;
            if (!flagGo.activeSelf) return;
            flagGo.SetActive(false);
            return;
        }

        if (!flagTried)
        {
            flagTried = true;
            if (ResLoader.I != null)
            {
                flagTex = ResLoader.I.LoadGsa2021First("res/effect/paopao_tleaderflag.gsa");
            }
        }
        if (flagTex == null) return;

        if (flagGo == null)
        {
            flagGo = new GameObject("LeaderFlag");
            flagGo.transform.SetParent(transform, false);
            flagSr = flagGo.AddComponent<SpriteRenderer>();
            flagSr.sortingOrder = 32003;
            Material material = Mat();
            if (material != null) flagSr.sharedMaterial = material;
            flagSr.sprite = TexScale.MakeSprite(flagTex, ppu, 1f, 0.5f);
        }

        if (flagGo != null)
        {
            if (!flagGo.activeSelf) flagGo.SetActive(true);
            if (flagGo != null && flagGo.transform != null)
            {
                flagGo.transform.localPosition = new Vector3(-HalfBodyPx / ppu, headTopY, 0f);
            }
        }
    }

    // Official 0x06000586, instance System.Void LeaderFlag.OnDestroy(): destroy the sprite the
    // cone created. Both slices read flagSr.sprite twice, once for the guard and once for the
    // Destroy argument, each behind its own flagSr null check.
    private void OnDestroy()
    {
        if (flagSr != null && flagSr.sprite != null)
        {
            Object.Destroy(flagSr.sprite);
        }
    }
}
}
