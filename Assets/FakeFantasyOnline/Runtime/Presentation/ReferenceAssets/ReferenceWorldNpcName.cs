using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Head-anchor geometry passed to NpcChatter.OnGUI; GameBootstrap owns the texture scan.
/// </summary>
internal static class ReferenceWorldNpcNameMath
{
    public const float HeadPaddingUnits = 0.05f;
    public const int GuiDepth = 100;

    public static int ResolveBakeScale(float uiScale)
    {
        return Math.Max(1, Mathf.RoundToInt(uiScale));
    }

    public static Vector3 ResolveHeadAnchor(
        Vector3 spritePosition,
        Texture2D tex,
        Sprite sprite)
    {
        if (!tex || !sprite)
        {
            throw new ArgumentException(
                "The NPC head anchor requires decoded art and its source-owned sprite.");
        }

        float centerFraction;
        float topFraction;
        GameBootstrap.ScanOpaqueForNpcAnchor(
            tex, out centerFraction, out topFraction);
        if (!(centerFraction > 0f))
        {
            centerFraction = 0.5f;
        }
        // SpawnNpcEntry uses the complete texture dimensions and its effective PPU.
        // Tight-mesh bounds must not crop the alpha-scan fractions a second time.
        return new Vector3(
            spritePosition.x + tex.width / sprite.pixelsPerUnit * centerFraction,
            spritePosition.y - tex.height / sprite.pixelsPerUnit * topFraction +
                HeadPaddingUnits,
            spritePosition.z);
    }

    public static bool TryResolveDrawRect(
        Vector3 screenPoint,
        int screenWidth,
        int screenHeight,
        int textureWidth,
        int textureHeight,
        out Rect rect)
    {
        rect = default(Rect);
        if (screenWidth < 1 || screenHeight < 1 ||
            textureWidth < 1 || textureHeight < 1 ||
            screenPoint.z <= 0f ||
            screenPoint.x < 0f || screenPoint.x > screenWidth ||
            screenPoint.y < 0f || screenPoint.y > screenHeight)
        {
            return false;
        }

        float anchorY = screenHeight - screenPoint.y;
        rect = new Rect(
            Mathf.Round(screenPoint.x - textureWidth * 0.5f),
            Mathf.Round(anchorY - textureHeight),
            textureWidth,
            textureHeight);
        return true;
    }
}

/// <summary>
/// Binds the recovered NpcChatter component to the NPC actor and publishes task-log edges.
/// Native NpcChatter owns its name texture, task icon and bubble drawing.
/// </summary>
internal sealed class ReferenceWorldNpcNameView : MonoBehaviour
{
    private ReferenceWorldRemoteFeedbackAssets assets;
    private NpcChatter native;
    private Camera worldCamera;
    private Transform mainPlayer;
    private Vector3 headAnchor;
    private string npcName;
    private Texture2D bakedName;
    private string bakedNameFor;
    private int bakedNameScale;
    private ReferenceWorldNpcTaskFeedback taskFeedback;
    private ReferenceWorldNpcAmbientChatter ambientChatter;
    private int playerLevel;
    private bool configured;

    public Texture2D BakedName => native != null ? native.BakedNameForIndependentRuntime : bakedName;
    public string BakedNameFor => native != null ? native.BakedNameKeyForIndependentRuntime : bakedNameFor;
    public int BakedNameScale => native != null ? native.BakedNameScaleForIndependentRuntime : bakedNameScale;
    public Vector3 HeadAnchor => headAnchor;
    public string NpcName => npcName;
    internal ReferenceNpcTaskSymbol TaskSymbol => taskFeedback != null
        ? taskFeedback.ResolveCurrent(npcName) : ReferenceNpcTaskSymbol.None;
    internal ReferenceWorldNpcAmbientChatter AmbientChatter => ambientChatter;

    public bool Configure(
        ReferenceWorldRemoteFeedbackAssets feedbackAssets,
        Camera camera,
        Transform mainPlayerTransform,
        Vector3 sourceHeadAnchor,
        string displayName,
        ReferenceWorldNpcTaskFeedback npcTaskFeedback,
        int sourcePlayerLevel,
        ReferenceNpcGreetingCatalog greetingCatalog = null,
        WorldNpcState npc = null)
    {
        if (configured || feedbackAssets == null || feedbackAssets.Font == null || camera == null ||
            mainPlayerTransform == null ||
            !feedbackAssets.Font.Ready || string.IsNullOrEmpty(displayName) ||
            npcTaskFeedback == null || sourcePlayerLevel < 0)
        {
            return false;
        }

        assets = feedbackAssets;
        worldCamera = camera;
        mainPlayer = mainPlayerTransform;
        headAnchor = sourceHeadAnchor;
        npcName = displayName;
        taskFeedback = npcTaskFeedback;
        playerLevel = sourcePlayerLevel;
        if (greetingCatalog != null || npc != null)
        {
            if (greetingCatalog == null || npc == null ||
                !string.Equals(displayName, npc.DisplayName, StringComparison.Ordinal) ||
                !ReferenceWorldNpcAmbientChatter.TryCreate(
                    gameObject,
                    greetingCatalog,
                    feedbackAssets,
                    npc,
                    out ambientChatter))
            {
                assets = null;
                worldCamera = null;
                mainPlayer = null;
                taskFeedback = null;
                ambientChatter = null;
                return false;
            }
        }

        native = GetComponent<NpcChatter>();
        configured = true;
        return true;
    }

    public bool ApplyTaskLog(WorldTaskLogState taskLog)
    {
        if (!configured || taskFeedback == null || taskLog == null)
        {
            return false;
        }

        return true;
    }

#if UNITY_EDITOR
    // Historical direct-audit seam; the player draws through NpcChatter.OnGUI.
    internal bool TryPrepare(float uiScale)
    {
        if (!configured || assets == null || !assets.Font.Ready ||
            string.IsNullOrEmpty(npcName))
        {
            return false;
        }

        int scale = ReferenceWorldNpcNameMath.ResolveBakeScale(uiScale);
        if (bakedName && bakedNameFor == npcName && bakedNameScale == scale)
        {
            return true;
        }

        Texture2D next = assets.Font.BakeNameTag(
            npcName,
            string.Empty,
            Color.green,
            Color.green,
            scale,
            null,
            null,
            null,
            null,
            null);
        if (!next)
        {
            return false;
        }

        next.name = "RecoveredNpcNameTexture";
        Texture2D previous = bakedName;
        bakedName = next;
        bakedNameFor = npcName;
        bakedNameScale = scale;
        DestroyOwned(previous);
        return true;
    }

#endif

    private void OnDestroy()
    {
        DestroyOwned(bakedName);
        bakedName = null;
        assets = null;
        worldCamera = null;
        mainPlayer = null;
        taskFeedback = null;
        ambientChatter = null;
        native = null;
        configured = false;
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
            UnityEngine.Object.DestroyImmediate(value);
            return;
        }
#endif
        UnityEngine.Object.Destroy(value);
    }
}

}
