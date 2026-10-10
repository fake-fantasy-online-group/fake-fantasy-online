using System.Collections.Generic;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Recovered official skill target classifier and direction/circle targeting surface.
/// </summary>
public sealed partial class SkillTargeting : MonoBehaviour
{
    public enum Aim
    {
        Instant,
        Single,
        Circle,
        Direction,
    }

    private const float ArrowScale = 1f;
    private const float OutlineWidthPx = 4f;
    private const int FanArcSegs = 24;
    private const float AimerFps = 10f;
    private const float DefaultRange = 400f;
    private const float DefaultRadius = 140f;
    private const float ServerSingleRangeFallback = 600f;
    private const int TargetingSortingOrder = -4900;
    private const int AimerSortingOrder = -4999;

    private static SkillTargeting I;

    private int skillId;
    private float rangePx;
    private float radiusPx;
    private float fanDeg;
    private float rectLen;
    private float rectWid;
    private Aim mode;
    private bool noCircle;
    private SpriteRenderer aimerSR;
    private Sprite[] aimerFrames;
    private int aimerIdx;
    private float aimerTimer;
    private float aimerTexW = 207f;
    private int aimerHdScale = 1;
    private SpriteRenderer arrowSR;
    private Sprite[] arrowFrames;
    private LineRenderer selLine;
    private List<Sprite> ownedAimerSprites;
    private Material selLineMaterial;

    public static bool Active { get; private set; }

    internal Aim ModeForAudit => mode;
    internal float RangePixelsForAudit => rangePx;
    internal float RadiusPixelsForAudit => radiusPx;

    internal static SkillTargeting InstanceForIndependentRuntime => I;

    internal static void CancelForIndependentRuntime()
    {
        if (I != null) I.End();
    }

    // Explicit diagnostics may advance outside play mode or on an inactive/disabled
    // copy. The live component keeps its own Unity Update and is never advanced twice.
    internal void AdvanceForIndependentRuntime()
    {
        if (!UnityEngine.Application.isPlaying || !isActiveAndEnabled) Update();
    }

    internal void ReleaseForIndependentRuntime()
    {
        if (I == this)
        {
            End();
            I = null;
        }
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (I == this)
        {
            End();
            I = null;
        }

        if (ownedAimerSprites != null)
        {
            for (int index = 0; index < ownedAimerSprites.Count; index++)
            {
                DestroyOwned(ownedAimerSprites[index]);
            }
            ownedAimerSprites.Clear();
        }
        DestroyOwned(selLineMaterial);
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (!value)
        {
            return;
        }
#if UNITY_EDITOR
        if (!UnityEngine.Application.isPlaying)
        {
            DestroyImmediate(value);
            return;
        }
#endif
        Destroy(value);
    }
}

}
