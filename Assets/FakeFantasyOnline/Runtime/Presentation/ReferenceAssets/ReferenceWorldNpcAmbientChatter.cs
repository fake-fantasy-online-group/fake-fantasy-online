using System;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Legacy bubble renderer reading recovered NpcChatter state. Deterministic state code is Editor-only.
/// </summary>
internal sealed class ReferenceWorldNpcAmbientChatter
{
    public const string BubbleTheme = "huang";
    public const float InitialBubbleUntil = -999f;

    private readonly ReferenceNpcGreetingCatalog catalog;
    private readonly ReferenceWorldChatBubbleAssets bubbleAssets;
    private readonly ReferenceWorldBitmapFont font;
    private readonly string npcName;
    private readonly string role;

    private readonly NpcChatter native;
    private string auditBubble;
    private float auditBubbleUntil;
    private float accumulatorMilliseconds;
    private int lineIndex;

    private ReferenceWorldNpcAmbientChatter(
        NpcChatter source, ReferenceWorldRemoteFeedbackAssets feedbackAssets)
    {
        native = source;
        bubbleAssets = feedbackAssets.ChatBubble;
        font = feedbackAssets.Font;
    }

#if UNITY_EDITOR
    private ReferenceWorldNpcAmbientChatter(
        ReferenceNpcGreetingCatalog sourceCatalog,
        ReferenceWorldChatBubbleAssets sourceBubbleAssets,
        ReferenceWorldBitmapFont sourceFont,
        WorldNpcState npc,
        float initialAccumulatorMilliseconds)
    {
        catalog = sourceCatalog;
        bubbleAssets = sourceBubbleAssets;
        font = sourceFont;
        npcName = npc.DisplayName;
        role = catalog.RoleOf(npc);
        accumulatorMilliseconds = initialAccumulatorMilliseconds;
        BubbleUntil = InitialBubbleUntil;
        catalog.RegisterNpcName(npcName);
    }

#endif

    internal NpcChatter NativeOwnerForIndependentRuntime => native;

    public string NpcName => native != null ? native.npcName : npcName;

    public string Role => native != null ? native.RoleForIndependentRuntime : role;

    public string Bubble
    {
        get => native != null ? native.BubbleForIndependentRuntime : auditBubble;
        private set => auditBubble = value;
    }

    public float BubbleUntil
    {
        get => native != null ? native.BubbleUntilForIndependentRuntime : auditBubbleUntil;
        private set => auditBubbleUntil = value;
    }

    public float AccumulatorMilliseconds => native != null ? native.AccumulatorForIndependentRuntime : accumulatorMilliseconds;

    public int LineIndex => native != null ? native.LineIndexForIndependentRuntime : lineIndex;

    public bool VisibleAt(float now)
    {
        return !string.IsNullOrEmpty(Bubble) && now <= BubbleUntil;
    }

    public static bool TryCreate(
        GameObject root,
        ReferenceNpcGreetingCatalog catalog,
        ReferenceWorldRemoteFeedbackAssets feedbackAssets,
        WorldNpcState npc,
        out ReferenceWorldNpcAmbientChatter owner)
    {
        owner = null;
        if (root == null || catalog == null || feedbackAssets == null || npc == null ||
            feedbackAssets.ChatBubble == null || feedbackAssets.Font == null ||
            string.IsNullOrEmpty(npc.DisplayName))
        {
            return false;
        }

        NpcChatter source = root.GetComponent<NpcChatter>();
        if (source == null) return false;
        owner = new ReferenceWorldNpcAmbientChatter(source, feedbackAssets);
        return true;
    }

#if UNITY_EDITOR
    internal static bool TryCreateForAudit(
        ReferenceNpcGreetingCatalog catalog,
        ReferenceWorldRemoteFeedbackAssets feedbackAssets,
        WorldNpcState npc,
        float initialAccumulatorMilliseconds,
        out ReferenceWorldNpcAmbientChatter owner)
    {
        owner = null;
        if (catalog == null || feedbackAssets == null || npc == null ||
            feedbackAssets.ChatBubble == null || feedbackAssets.Font == null ||
            string.IsNullOrEmpty(npc.DisplayName) ||
            !IsFinite(initialAccumulatorMilliseconds) ||
            initialAccumulatorMilliseconds < 0f)
        {
            return false;
        }

        owner = new ReferenceWorldNpcAmbientChatter(
            catalog,
            feedbackAssets.ChatBubble,
            feedbackAssets.Font,
            npc,
            initialAccumulatorMilliseconds);
        return true;
    }
#endif

#if UNITY_EDITOR
    /// <summary>
    /// Historical deterministic audit seam; normal gameplay uses NpcChatter.Update.
    /// Returns true for a valid clock step. bubbleChanged is true only when PickLine produced a
    /// new visible edge; failed/empty selection leaves the previous expired string untouched.
    /// </summary>
    public bool TryAdvance(float now, float deltaTime, out bool bubbleChanged)
    {
        bubbleChanged = false;
        if (!IsFinite(now) || !IsFinite(deltaTime) || deltaTime < 0f)
        {
            return false;
        }

        if (!catalog.ChatterEnabled || now <= BubbleUntil)
        {
            return true;
        }

        accumulatorMilliseconds += deltaTime * 1000f;
        if (accumulatorMilliseconds < catalog.CycleMilliseconds)
        {
            return true;
        }

        accumulatorMilliseconds = 0f;
        string next = catalog.PickAmbientLine(npcName, role, ref lineIndex);
        if (next == null)
        {
            return true;
        }

        Bubble = next;
        BubbleUntil = now + catalog.ShowSeconds;
        bubbleChanged = true;
        return true;
    }

#endif

#if UNITY_EDITOR
    // Historical direct-audit seam; the player draws through NpcChatter.OnGUI.
    public bool TryDraw(
        Vector3 screenPoint,
        int screenWidth,
        int screenHeight,
        float uiScale,
        float now)
    {
        if (!VisibleAt(now) || screenPoint.z <= 0f ||
            screenWidth < 1 || screenHeight < 1 ||
            screenPoint.x < 0f || screenPoint.x > screenWidth ||
            screenPoint.y < 0f || screenPoint.y > screenHeight ||
            !IsFinite(uiScale) || !(uiScale > 0f))
        {
            return false;
        }

        return ReferenceWorldChatBubbleRenderer.TryDraw(
            screenPoint.x,
            screenHeight - screenPoint.y,
            Bubble,
            uiScale,
            now,
            bubbleAssets.Yellow,
            font,
            bubbleAssets);
    }

#endif

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
