namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Narrow navigation surface consumed by the recovered PlayerController follow branch.
/// </summary>
public interface IReferenceWorldFollowNavigation
{
    float WorldX { get; }

    float WorldY { get; }

    bool MovementActive { get; }

    bool TrySetPathTo(
        ReferenceWorldCollisionMask mask,
        float targetX,
        float targetY);
}

/// <summary>
/// Historical diagnostic projection of the follow branch. Live behavior and fields are now
/// owned by PlayerController.Follow and CombatWorld.MatePosition.
/// </summary>
/// <remarks>
/// Follow selection is local navigation state, not a server mutation. The selected target is
/// toggled off when selected twice. While present, a target farther than 110 map pixels is
/// replanned at most once every 0.3 seconds. A missing target first consumes its last observed
/// position, then cancels only after eight stationary seconds with the exact official feedback.
/// </remarks>
public sealed class ReferenceWorldPlayerFollow
{
    public const float RepathDistancePixels = 110f;
    public const float RepathDistanceSquared =
        RepathDistancePixels * RepathDistancePixels;
    public const float RepathIntervalSeconds = 0.3f;
    public const float MissingTargetTimeoutSeconds = 8f;
    public const string MissingTargetFeedback =
        "跟随目标离得太远(或已离开本地图), 已取消跟随。";

    private int targetEntityId;
    private float missingSinceSeconds;
    private float lastTargetX;
    private float lastTargetY;
    private bool hasLastTargetPosition;
    private float nextRepathTimeSeconds;
    private string pendingFeedback = string.Empty;

    public int TargetEntityId => targetEntityId;

    public float MissingSinceSeconds => missingSinceSeconds;

    public float LastTargetX => lastTargetX;

    public float LastTargetY => lastTargetY;

    public bool HasLastTargetPosition => hasLastTargetPosition;

    public float NextRepathTimeSeconds => nextRepathTimeSeconds;

    public bool Toggle(int entityId)
    {
        if (entityId <= 0)
        {
            return false;
        }

        if (targetEntityId == entityId)
        {
            ClearTarget();
            return true;
        }

        targetEntityId = entityId;
        missingSinceSeconds = 0f;
        hasLastTargetPosition = false;
        pendingFeedback = string.Empty;
        return true;
    }

    public bool TryAdvance(
        float timeSeconds,
        bool targetAvailable,
        float targetX,
        float targetY,
        IReferenceWorldFollowNavigation navigation,
        ReferenceWorldCollisionMask mask)
    {
        if (!IsNonNegativeFinite(timeSeconds) || navigation == null ||
            !IsFinite(navigation.WorldX) || !IsFinite(navigation.WorldY))
        {
            return false;
        }

        if (targetEntityId == 0)
        {
            return true;
        }

        if (targetAvailable)
        {
            if (!IsFinite(targetX) || !IsFinite(targetY))
            {
                return false;
            }

            missingSinceSeconds = 0f;
            lastTargetX = targetX;
            lastTargetY = targetY;
            hasLastTargetPosition = true;

            float deltaX = targetX - navigation.WorldX;
            float deltaY = targetY - navigation.WorldY;
            if (deltaX * deltaX + deltaY * deltaY > RepathDistanceSquared &&
                nextRepathTimeSeconds <= timeSeconds)
            {
                nextRepathTimeSeconds = timeSeconds + RepathIntervalSeconds;
                return navigation.TrySetPathTo(mask, targetX, targetY);
            }

            return true;
        }

        if (hasLastTargetPosition && !navigation.MovementActive)
        {
            hasLastTargetPosition = false;
            if (!navigation.TrySetPathTo(mask, lastTargetX, lastTargetY))
            {
                return false;
            }
        }

        if (navigation.MovementActive)
        {
            missingSinceSeconds = 0f;
            return true;
        }

        if (missingSinceSeconds == 0f)
        {
            missingSinceSeconds = timeSeconds;
            return true;
        }

        if (timeSeconds - missingSinceSeconds > MissingTargetTimeoutSeconds)
        {
            ClearTarget();
            pendingFeedback = MissingTargetFeedback;
        }

        return true;
    }

    public void ResetRepathTime()
    {
        nextRepathTimeSeconds = 0f;
    }

    public string ConsumeFeedback()
    {
        string value = pendingFeedback;
        pendingFeedback = string.Empty;
        return value;
    }

    public void Clear()
    {
        ClearTarget();
        nextRepathTimeSeconds = 0f;
        pendingFeedback = string.Empty;
    }

    private void ClearTarget()
    {
        targetEntityId = 0;
        missingSinceSeconds = 0f;
        hasLastTargetPosition = false;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsNonNegativeFinite(float value)
    {
        return value >= 0f && IsFinite(value);
    }
}

}
