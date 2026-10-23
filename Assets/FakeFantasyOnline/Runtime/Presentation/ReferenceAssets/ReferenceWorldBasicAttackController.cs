using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal enum ReferenceWorldBasicAttackPhase
{
    Idle = 0,
    Windup = 1,
    Recovery = 2,
}

internal struct ReferenceWorldBasicAttackMovementPlan
{
    public ReferenceWorldBasicAttackMovementPlan(
        bool setPathToTarget,
        bool stopMovement,
        bool clearSelection,
        float targetX,
        float targetY)
    {
        SetPathToTarget = setPathToTarget;
        StopMovement = stopMovement;
        ClearSelection = clearSelection;
        TargetX = targetX;
        TargetY = targetY;
    }

    public bool SetPathToTarget { get; }
    public bool StopMovement { get; }
    public bool ClearSelection { get; }
    public float TargetX { get; }
    public float TargetY { get; }
}

internal struct ReferenceWorldBasicAttackPlan
{
    public ReferenceWorldBasicAttackPlan(
        bool requestAttack,
        bool startLocalSwing,
        bool clearSelection,
        int targetEntityId,
        float swingSeconds)
    {
        RequestAttack = requestAttack;
        StartLocalSwing = startLocalSwing;
        ClearSelection = clearSelection;
        TargetEntityId = targetEntityId;
        SwingSeconds = swingSeconds;
    }

    public bool RequestAttack { get; }
    public bool StartLocalSwing { get; }
    public bool ClearSelection { get; }
    public int TargetEntityId { get; }
    public float SwingSeconds { get; }
}

/// <summary>
/// Legacy ownerless audit model and numeric helpers. No runtime instance is constructed and
/// no CombatWorld state is writable here. Native CombatWorld.Update is the live phase owner.
/// </summary>
internal sealed class ReferenceWorldBasicAttackController
{
    internal const int DefaultAttackDistancePixels = 50;
    internal const int RangedAttackDistanceThresholdPixels = 150;
    internal const int AttackSpeedAttributeIndex = 33;
    internal const int MinimumAttackSpeedPercent = 50;
    internal const float MeleeIntervalSeconds = 1f;
    internal const float RangedIntervalSeconds = 0.5f;
    internal const int FastWeaponCategory = 8;
    internal const string RangedProjectileEffectName = "TFX302";
    internal const float RangedProjectileMinimumDurationMilliseconds = 80f;

    private int selectedEntityId;
    private bool autoAttacking;
    private ReferenceWorldBasicAttackPhase phase;
    private float phaseSeconds;

    public int SelectedEntityId => selectedEntityId;

    public bool AutoAttacking => autoAttacking;

    public ReferenceWorldBasicAttackPhase Phase => phase;

    public float PhaseSeconds => phaseSeconds;

    public bool SetTarget(int entityId, bool attack)
    {
        if (entityId <= 0)
        {
            return false;
        }

        selectedEntityId = entityId;
        autoAttacking = attack;
        ResetPhase();
        return true;
    }

    // Historical ownerless no-target observation. The product's corresponding phase clear
    // now lives only in CombatWorld.Update; this model has no link back to native state.
    private void AdoptOfficialNoTargetForIndependentRuntime()
    {
        selectedEntityId = 0;
        ResetPhase();
    }

    public void ClearTarget()
    {
        selectedEntityId = 0;
        autoAttacking = false;
        ResetPhase();
    }

    public void StopAttack()
    {
        autoAttacking = false;
        ResetPhase();
    }

    public bool TryPrepareMovement(
        WorldEntityState target,
        float targetX,
        float targetY,
        float playerX,
        float playerY,
        bool playerMoving,
        int attackDistancePixels,
        out ReferenceWorldBasicAttackMovementPlan plan,
        bool manualFollow = false)
    {
        plan = default(ReferenceWorldBasicAttackMovementPlan);
        if (!IsFinite(targetX) || !IsFinite(targetY) ||
            !IsFinite(playerX) || !IsFinite(playerY))
        {
            return false;
        }

        if (!autoAttacking || selectedEntityId <= 0)
        {
            return true;
        }

        // Keep the legacy ownerless no-target model for its existing Editor consumers.
        if (target == null)
        {
            AdoptOfficialNoTargetForIndependentRuntime();
            return true;
        }

        if (!IsLiveSelectedMonster(target))
        {
            ClearTarget();
            plan = new ReferenceWorldBasicAttackMovementPlan(
                false,
                playerMoving,
                true,
                0f,
                0f);
            return true;
        }

        if (!IsInRange(
                playerX,
                playerY,
                targetX,
                targetY,
                attackDistancePixels))
        {
            ResetPhase();
            plan = new ReferenceWorldBasicAttackMovementPlan(
                true,
                false,
                false,
                targetX,
                targetY);
            return true;
        }

        if (playerMoving)
        {
            plan = new ReferenceWorldBasicAttackMovementPlan(
                false,
                true,
                false,
                targetX,
                targetY);
        }

        return true;
    }

    public bool TryAdvance(
        WorldEntityState target,
        float targetX,
        float targetY,
        float playerX,
        float playerY,
        bool playerMoving,
        bool lineOfSight,
        int attackDistancePixels,
        int weaponCategory,
        IReadOnlyList<int> attributeTotals,
        IReadOnlyList<int> attributes,
        float deltaTime,
        out ReferenceWorldBasicAttackPlan plan,
        int? nativeAttackDistancePixels = null)
    {
        plan = default(ReferenceWorldBasicAttackPlan);
        if (!IsFinite(targetX) || !IsFinite(targetY) ||
            !IsFinite(playerX) || !IsFinite(playerY) ||
            !IsFinite(deltaTime) || deltaTime < 0f)
        {
            return false;
        }

        if (!autoAttacking || selectedEntityId <= 0)
        {
            ResetPhase();
            return true;
        }

        // Same ownerless model as TryPrepareMovement; no live CombatWorld is referenced.
        if (target == null)
        {
            AdoptOfficialNoTargetForIndependentRuntime();
            return true;
        }

        if (!IsLiveSelectedMonster(target))
        {
            ClearTarget();
            plan = new ReferenceWorldBasicAttackPlan(
                false,
                false,
                true,
                0,
                0f);
            return true;
        }

        if (!lineOfSight)
        {
            ResetPhase();
            return true;
        }

        phaseSeconds += deltaTime;
        if (phase == ReferenceWorldBasicAttackPhase.Idle)
        {
            phase = ReferenceWorldBasicAttackPhase.Windup;
            phaseSeconds = 0f;
            return true;
        }

        float interval = AttackIntervalSeconds(
            attackDistancePixels, weaponCategory, attributeTotals, attributes, nativeAttackDistancePixels);
        if (phase == ReferenceWorldBasicAttackPhase.Windup)
        {
            PlayerController player = PlayerController.I;
            if (player != null && player.IsMoving)
            {
                phaseSeconds = 0f;
                return true;
            }
            if (!(phaseSeconds >= 0f))
            {
                return true;
            }
            phase = ReferenceWorldBasicAttackPhase.Recovery;
            phaseSeconds = 0f;
            plan = new ReferenceWorldBasicAttackPlan(
                true,
                true,
                false,
                selectedEntityId,
                interval);
            return true;
        }

        if (phase == ReferenceWorldBasicAttackPhase.Recovery &&
            phaseSeconds >= interval)
        {
            ResetPhase();
        }

        return true;
    }

    internal static int AttackRangePixels(int attackDistancePixels)
    {
        return attackDistancePixels > 0
            ? attackDistancePixels
            : DefaultAttackDistancePixels;
    }

    internal static bool IsRanged(int attackDistancePixels)
    {
        return attackDistancePixels > RangedAttackDistanceThresholdPixels;
    }

    internal static float RangedProjectileDurationMilliseconds(
        float worldDistance,
        float pixelsPerUnit)
    {
        return Math.Max(
            RangedProjectileMinimumDurationMilliseconds,
            pixelsPerUnit * worldDistance);
    }

    // Historical detached audit helper, not the native rhythm or a product phase authority.
    // Its percent/base predicates retain their old differences from CombatWorld.AtkInterval.
    internal static float AttackIntervalSeconds(
        int attackDistancePixels,
        int weaponCategory,
        IReadOnlyList<int> attributeTotals,
        IReadOnlyList<int> attributes,
        int? nativeAttackDistancePixels = null)
    {
        float baseInterval = IsRanged(nativeAttackDistancePixels ?? attackDistancePixels) ||
                             weaponCategory == FastWeaponCategory
            ? RangedIntervalSeconds
            : MeleeIntervalSeconds;
        int speedPercent = HasAttribute(attributeTotals, AttackSpeedAttributeIndex)
            ? attributeTotals[AttackSpeedAttributeIndex]
            : AttributeOrDefault(
                attributes,
                AttackSpeedAttributeIndex,
                100);

        speedPercent = Math.Max(MinimumAttackSpeedPercent, speedPercent);
        return baseInterval / (speedPercent / 100f);
    }

    internal static bool IsInRange(
        float playerX,
        float playerY,
        float targetX,
        float targetY,
        int attackDistancePixels)
    {
        if (!IsFinite(playerX) || !IsFinite(playerY) ||
            !IsFinite(targetX) || !IsFinite(targetY))
        {
            return false;
        }

        float deltaX = targetX - playerX;
        float deltaY = targetY - playerY;
        float range = AttackRangePixels(attackDistancePixels);
        return deltaX * deltaX + deltaY * deltaY <= range * range;
    }

    private bool IsLiveSelectedMonster(WorldEntityState target)
    {
        return target != null &&
               target.EntityId == selectedEntityId &&
               target.Kind == WorldEntityKind.Monster &&
               target.FriendlyFlag == 0 &&
               target.HitPointsOrRadiusPixels > 0;
    }

    private void ResetPhase()
    {
        phase = ReferenceWorldBasicAttackPhase.Idle;
        phaseSeconds = 0f;
    }

    private static int AttributeOrDefault(
        IReadOnlyList<int> values,
        int index,
        int fallback)
    {
        return values != null && index >= 0 && index < values.Count
            ? values[index]
            : fallback;
    }

    private static bool HasAttribute(IReadOnlyList<int> values, int index)
    {
        return values != null && index >= 0 && index < values.Count;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

}
