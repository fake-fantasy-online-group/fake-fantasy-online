using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned translation of PlayerTaskTrack progress and explicit text feedback.
/// </summary>
/// <remarks>
/// This owner only observes immutable server-authored task snapshots. It never advances a task,
/// marks one ready, or turns a successful request write into local task authority.
/// </remarks>
internal sealed class ReferenceWorldPlayerTaskTrack
{
    public const float ShowSeconds = 5f;
    public const float HeadOffsetPixels = 140f;
    public const float LineHeightUnits = 16f;
    public const float StackGapUnits = 8f;
    public const string SignatureSeparator = "|";
    public const string ProgressSeparator = "  ";
    public const string TaskNameOpen = "【";
    public const string TaskNameClose = "】";

    public static readonly Color ProgressColor =
        new Color(1f, 90f / 255f, 90f / 255f, 1f);

    private readonly ReferenceWorldNpcTaskFeedback taskDefinitions;
    private readonly ReferenceWorldBitmapFont font;
    private readonly Transform player;
    private readonly Camera camera;
    private readonly List<string> lines = new List<string>();
    private readonly Dictionary<int, string> previousSignatures =
        new Dictionary<int, string>();
    private readonly HashSet<int> seen = new HashSet<int>();
    private readonly HashSet<int> changedIds = new HashSet<int>();
    private readonly List<int> removedIds = new List<int>();

    private float showUntil = -1f;
    private readonly TaskPromptLayout layout = new TaskPromptLayout();

    private ReferenceWorldPlayerTaskTrack(
        ReferenceWorldNpcTaskFeedback taskDefinitions,
        ReferenceWorldBitmapFont font,
        Transform player,
        Camera camera)
    {
        this.taskDefinitions = taskDefinitions;
        this.font = font;
        this.player = player;
        this.camera = camera;
    }

    internal IReadOnlyList<string> Lines => lines;
    internal float ShowUntil => showUntil;
    internal int PreviousSignatureCount => previousSignatures.Count;

    public static bool TryCreate(
        ReferenceWorldNpcTaskFeedback taskDefinitions,
        ReferenceWorldBitmapFont font,
        Transform player,
        Camera camera,
        out ReferenceWorldPlayerTaskTrack track)
    {
        track = null;
        if (taskDefinitions == null || font == null ||
            player == null || camera == null)
        {
            return false;
        }

        track = new ReferenceWorldPlayerTaskTrack(
            taskDefinitions,
            font,
            player,
            camera);
        return true;
    }

    /// <summary>
    /// Translates PlayerTaskTrack.Update and BuildProgress for one authoritative snapshot.
    /// </summary>
    public bool Observe(WorldTaskLogState taskLog, float currentTime)
    {
        if (taskLog == null || !IsFinite(currentTime))
        {
            return false;
        }

        changedIds.Clear();
        seen.Clear();
        for (int index = 0; index < taskLog.Active.Count; index++)
        {
            WorldActiveTaskState active = taskLog.Active[index];
            seen.Add(active.TaskId);
            string signature = active.Step.ToString() + SignatureSeparator + active.Progress;
            string previous;
            if (previousSignatures.TryGetValue(active.TaskId, out previous) &&
                !string.Equals(signature, previous, StringComparison.Ordinal))
            {
                changedIds.Add(active.TaskId);
            }

            previousSignatures[active.TaskId] = signature;
        }

        removedIds.Clear();
        foreach (int taskId in previousSignatures.Keys)
        {
            if (!seen.Contains(taskId))
            {
                removedIds.Add(taskId);
            }
        }

        for (int index = 0; index < removedIds.Count; index++)
        {
            previousSignatures.Remove(removedIds[index]);
        }

        if (changedIds.Count > 0)
        {
            BuildProgress(taskLog);
            if (lines.Count > 0)
            {
                showUntil = currentTime + ShowSeconds;
            }
        }

        return true;
    }

    public void Show(string text)
    {
        // PlayerTaskTrack.Show returns before clearing the current stack for null/empty text.
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        lines.Clear();
        foreach (string line in text.Replace("\r", string.Empty).Split('\n'))
        {
            if (line.Length > 0)
            {
                lines.Add(line);
            }
        }

        if (lines.Count > 0)
        {
            showUntil = Time.time + ShowSeconds;
        }
    }

    public bool Draw(float currentTime, float uiScale, int screenWidth, int screenHeight)
    {
        if (!IsFinite(currentTime) || !IsPositiveFinite(uiScale) ||
            screenWidth < 1 || screenHeight < 1)
        {
            return false;
        }

        if (showUntil < currentTime || lines.Count == 0 ||
            player == null || camera == null || font == null || !font.Ready)
        {
            return true;
        }

        Vector3 anchor = player.position +
                         Vector3.up * (HeadOffsetPixels /
                                       ReferenceWorldLocalPlayerDoll.PixelsPerUnit);
        Vector3 screenPoint = camera.WorldToScreenPoint(anchor);
        float margin = 12f * uiScale;
        float maxWidth = Mathf.Max(1f, Mathf.Min(320f * uiScale, screenWidth - 2f * margin));
        layout.Update(lines, maxWidth, uiScale, text => font.Measure(text, uiScale));
        float firstLineTop;
        if (!TryResolveFirstLineTop(
                screenPoint,
                screenHeight,
                uiScale,
                layout.Rows.Count,
                out firstLineTop))
        {
            return true;
        }

        float lineTop = Mathf.Max(8f * uiScale, firstLineTop);
        float lineHeight = uiScale * LineHeightUnits;
        for (int index = 0; index < layout.Rows.Count; index++)
        {
            string line = layout.Rows[index];
            float width = layout.Widths[index];
            font.DrawOutline(
                Mathf.Max(margin, Mathf.Min(screenPoint.x - width * 0.5f, screenWidth - margin - width)),
                lineTop,
                line,
                ProgressColor,
                uiScale);
            lineTop += lineHeight;
        }

        return true;
    }

    internal static bool TryResolveFirstLineTop(
        Vector3 screenPoint,
        int screenHeight,
        float uiScale,
        int lineCount,
        out float firstLineTop)
    {
        firstLineTop = 0f;
        if (screenHeight < 1 || !IsPositiveFinite(uiScale) || lineCount < 1 ||
            !IsFinite(screenPoint.x) || !IsFinite(screenPoint.y) ||
            !IsFinite(screenPoint.z) || screenPoint.z < 0f)
        {
            return false;
        }

        float lineHeight = uiScale * LineHeightUnits;
        firstLineTop = screenHeight - screenPoint.y - lineHeight * lineCount -
                       uiScale * StackGapUnits;
        return IsFinite(firstLineTop);
    }

    private void BuildProgress(WorldTaskLogState taskLog)
    {
        lines.Clear();
        List<WorldActiveTaskState> changed = new List<WorldActiveTaskState>();
        for (int index = 0; index < taskLog.Active.Count; index++)
        {
            WorldActiveTaskState active = taskLog.Active[index];
            if (changedIds.Contains(active.TaskId) &&
                !string.IsNullOrEmpty(active.Progress))
            {
                changed.Add(active);
            }
        }

        bool includeTaskName = changed.Count >= 2;
        for (int index = 0; index < changed.Count; index++)
        {
            WorldActiveTaskState active = changed[index];
            string taskName = includeTaskName
                ? taskDefinitions.TaskName(active.TaskId)
                : null;
            string[] progressLines = active.Progress.Split(
                new[] { ProgressSeparator },
                StringSplitOptions.RemoveEmptyEntries);
            for (int lineIndex = 0; lineIndex < progressLines.Length; lineIndex++)
            {
                string line = progressLines[lineIndex].Trim();
                lines.Add(string.IsNullOrEmpty(taskName)
                    ? line
                    : TaskNameOpen + taskName + TaskNameClose + line);
            }
        }
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && IsFinite(value);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
