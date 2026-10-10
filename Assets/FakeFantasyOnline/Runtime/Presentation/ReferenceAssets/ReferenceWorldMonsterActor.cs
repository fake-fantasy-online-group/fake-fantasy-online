using System;
using System.Collections.Generic;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Source-owned construction boundary for package-backed map-6 monsters.
/// </summary>
/// <remarks>
/// This cone restores the official directional standing and walking rows, normal and additive
/// DIMG layers, map projection, pick box, blob shadow, hostile name and optional head HP feedback.
/// Actual MonsterEntity owns every live movement/action/hit/death/glow clock; this wrapper only
/// connects independent-build data and adjacent view lifetime.
/// </remarks>
internal sealed class ReferenceWorldMonsterActor : IDisposable
{
    public const int Model274Id = 274;
    public const int Map6TurtleModelId = 206;
    public const int Map6WoodenManModelId = 262;
    public const int DefaultStoredDirection = 4;
    public const string StandPath = "avatar/npc/npc27405.gsn";
    public const string StandGlowPath = "avatar/npc/npc27405e.gsn";
    public const string WalkPath = "avatar/npc/npc27406.gsn";
    public const string WalkGlowPath = "avatar/npc/npc27406e.gsn";
    public const string AttackPath = "avatar/npc/npc27401.gsn";
    public const string AttackGlowPath = "avatar/npc/npc27401e.gsn";
    public const string DeathPath = "avatar/npc/npc27403.gsn";
    public const string DeathGlowPath = "avatar/npc/npc27403e.gsn";
    public const uint RequiredDimgVersion = 65537;
    public const int RequiredFrameCount = 30;
    public const int RequiredDirectionCount = 5;
    public const int RequiredFramesPerDirection = 6;
    public const int RequiredStandFrameWidthPixels = 94;
    public const float StandIntervalMilliseconds = 170f;
    public const float WalkIntervalMilliseconds = 111f;
    public const int RequiredAttackFrameCount = 18;
    public const int RequiredAttackDirectionCount = 2;
    public const int RequiredAttackFramesPerDirection = 9;
    public const float AttackIntervalMilliseconds = 133f;
    public const float PrepareFinalHoldMilliseconds = 3000f;
    public const int RequiredDeathFrameCount = 20;
    public const int RequiredDeathDirectionCount = 2;
    public const int RequiredDeathFramesPerDirection = 10;
    public const float DeathIntervalMilliseconds = 148f;
    public const float DeathHoldSeconds = 0.35f;
    public const float HitTintSeconds = 0.12f;
    private readonly ReferenceGsmMap map;
    private readonly ReferenceResourceLoader resources;
    private readonly Transform parent;
    private WorldEntityState state;
    private readonly ReferenceWorldRemoteFeedbackAssets feedbackAssets;
    private readonly Camera worldCamera;
    private readonly int localPlayerLevel;
    private readonly Material suppliedGlowMaterial;
    private Sprite[,] standGrid;
    private Sprite[,] walkGrid;
    private Sprite[,] attackGrid;
    private Sprite[,] prepareGrid;
    private Sprite[,] deathGrid;
    private int standDirectionCount;
    private int standFramesPerDirection;
    private int walkDirectionCount;
    private int walkFramesPerDirection;
    private int attackDirectionCount;
    private int attackFramesPerDirection;
    private int prepareDirectionCount;
    private int prepareFramesPerDirection;
    private int deathDirectionCount;
    private int deathFramesPerDirection;
    private float standIntervalMilliseconds = StandIntervalMilliseconds;
    private float walkIntervalMilliseconds = WalkIntervalMilliseconds;
    private float attackIntervalMilliseconds = AttackIntervalMilliseconds;
    private float prepareIntervalMilliseconds;
    private float deathIntervalMilliseconds = DeathIntervalMilliseconds;
    private bool hasWalkAction;
    private bool hasAttackAction;
    private bool hasPrepareAction;
    private bool hasDeathAction;
    private int standActionId;
    private int walkActionId;
    private int attackActionId;
    private int prepareActionId;
    private int deathActionId;

    private GameObject rootObject;
    private MonsterEntity sourceOwner;
    private SpriteRenderer renderer;
    private SpriteRenderer glowRenderer;
    private BoxCollider2D pickBox;
    private ReferenceWorldBlobShadow shadow;
    private HeadHpBarView headHpBarView;
    private bool glowMaterialApplied;
    private float headTopUnits;
    private ReferenceWorldMonsterPathMotion pathMotion;
    private long lastMovementActionRevision;
    private int currentStoredDirection = DefaultStoredDirection;
    private bool flipX;
    private int currentFrame;
    private int standFrame;
    private int walkFrame;
    private float standElapsedMilliseconds;
    private float walkElapsedMilliseconds;
    private int attackFrame = -1;
    private float attackElapsedMilliseconds;
    private bool attackPlaying;
    private int prepareFrame = -1;
    private float prepareElapsedMilliseconds;
    private float prepareHeldMilliseconds;
    private bool preparePlaying;
    private float deathElapsedMilliseconds;
    private float deathHoldSeconds;
    private float hitTintSeconds;
    private bool dying;
    private bool deathHidden;
    private bool ownsNativeRoot;
    private bool built;
    private bool disposed;

    public ReferenceWorldMonsterActor(
        ReferenceGsmMap map,
        ReferenceResourceLoader resources,
        Transform parent,
        WorldEntityState state,
        ReferenceWorldRemoteFeedbackAssets feedbackAssets,
        Camera worldCamera,
        int localPlayerLevel,
        Material glowMaterial = null)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.parent = parent == null
            ? throw new ArgumentNullException(nameof(parent))
            : parent;
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.feedbackAssets = feedbackAssets ??
            throw new ArgumentNullException(nameof(feedbackAssets));
        this.worldCamera = worldCamera;
        this.localPlayerLevel = localPlayerLevel;
        suppliedGlowMaterial = glowMaterial;
    }

    public bool Built => built;

    public WorldEntityState State => state;

    public GameObject RootObject => rootObject;

    public SpriteRenderer Renderer => renderer;

    public SpriteRenderer GlowRenderer => sourceOwner == null
        ? glowRenderer
        : sourceOwner.GlowRendererForIndependentRuntime;

    public BoxCollider2D PickBox => pickBox;

    public ReferenceWorldBlobShadow Shadow => shadow;

    // Historical Editor API; actual names are exposed through NameTagRoot/NameTagRenderer.
    public ReferenceWorldMonsterNameTagView NameTagView => null;
    public GameObject NameTagRoot => sourceOwner ? sourceOwner.NameTagRootForIndependentRuntime : null;
    public SpriteRenderer NameTagRenderer => sourceOwner ? sourceOwner.NameTagRendererForIndependentRuntime : null;

    // Historical Editor API; live effects are owned by MonsterEntity/ActorEffects.
    public ReferenceWorldMonsterStatusEffectView StatusEffectView => null;

    // Historical Editor API. Production status icons belong to MonsterEntity.
    public ReferenceWorldMonsterStatusIconView StatusIconView => null;

    public HeadHpBarView HeadHpBarView => headHpBarView;

    public EffectPlayer EliteRing => sourceOwner == null
        ? null
        : sourceOwner.EliteRingForIndependentRuntime;

    public float HeadTopUnits => headTopUnits;

    public int CurrentFrame => sourceOwner != null && sourceOwner.DyingForIndependentRuntime
        ? sourceOwner.DeathFrameForIndependentRuntime
        : currentFrame;

    public float StandElapsedMilliseconds => standElapsedMilliseconds;

    public float WalkElapsedMilliseconds => walkElapsedMilliseconds;

    public float WorldX => sourceOwner == null
        ? state.X
        : sourceOwner.WorldXForIndependentRuntime;

    public float WorldY => sourceOwner == null
        ? state.Y
        : sourceOwner.WorldYForIndependentRuntime;

    public bool Moving => sourceOwner != null && sourceOwner.MovingForIndependentRuntime;

    public int PathIndex => sourceOwner == null
        ? 0
        : sourceOwner.PathIndexForIndependentRuntime;

    public int PathCount => sourceOwner == null
        ? 0
        : sourceOwner.PathCountForIndependentRuntime;

    public float MoveSpeed => sourceOwner == null
        ? ReferenceWorldMonsterPathMotion.DefaultMoveSpeed
        : sourceOwner.MoveSpeedForIndependentRuntime;

    public float CatchUp => sourceOwner == null
        ? ReferenceWorldMonsterPathMotion.OrdinaryMultiplier
        : sourceOwner.CatchUpForIndependentRuntime;

    public int CurrentStoredDirection => sourceOwner == null
        ? currentStoredDirection
        : sourceOwner.DirectionForIndependentRuntime;

    public bool FlipX => sourceOwner == null
        ? flipX
        : sourceOwner.FlipForIndependentRuntime;

    public bool AttackPlaying => attackPlaying;

    public int AttackFrame => attackFrame;

    public float AttackElapsedMilliseconds => attackElapsedMilliseconds;

    public bool PreparePlaying => preparePlaying;

    public int PrepareFrame => prepareFrame;

    public float PrepareElapsedMilliseconds => prepareElapsedMilliseconds;

    public float PrepareHeldMilliseconds => prepareHeldMilliseconds;

    public bool Dying => sourceOwner != null
        ? sourceOwner.DyingForIndependentRuntime
        : dying;

    public bool DeathHidden => deathHidden;

    public float HitTintSecondsRemaining => sourceOwner == null
        ? hitTintSeconds
        : sourceOwner.FlashTimerForIndependentRuntime;

    internal MonsterEntity SelectableOwner => sourceOwner;

    internal bool SourceOwnerLive => sourceOwner != null &&
        sourceOwner.UnityLiveForIndependentRuntime;

    internal float DeathAlpha => sourceOwner == null
        ? 0f
        : sourceOwner.DeathAlphaForIndependentRuntime;

    public int StandActionId => standActionId;

    public int WalkActionId => walkActionId;

    public int AttackActionId => attackActionId;

    public int PrepareActionId => prepareActionId;

    public int DeathActionId => deathActionId;

    public bool Build(MonsterEntity initializedOwner = null)
    {
        ThrowIfDisposed();
        if (built || (initializedOwner == null && !IsMonsterState(state)) ||
            map.MapWidth < 1 || map.MapHeight < 1)
        {
            return false;
        }

        bool receivedOwner = initializedOwner != null;
        ownsNativeRoot = !receivedOwner;
        try
        {
            if (initializedOwner == null)
            {
                // Compatibility snapshot construction uses the same Init owner, but does not
                // borrow a registered object that its replacement caller still owns.
                bool ally = state.FriendlyFlag > 0;
                GameObject created = new GameObject(string.Format("{0}:{1}:{2}",
                    state.Kind == WorldEntityKind.Pet ? "Pet" : ally ? "Ally" : "Mon",
                    state.DisplayName, state.EntityId));
                rootObject = created;
                initializedOwner = created.AddComponent<MonsterEntity>();
                sourceOwner = initializedOwner;
                initializedOwner.ally = ally;
                initializedOwner.petOwnerId = state.Kind == WorldEntityKind.Pet ? state.OwnerEntityId : 0;
                initializedOwner.Init(state.EntityId, state.ModelOrSkillId, state.DisplayName, state.Level,
                    state.X, state.Y, state.HitPointsOrRadiusPixels, state.MaximumHitPoints,
                    ReferenceFirstWorldStaticComposite.PixelsPerUnit, map.MapWidth, map.MapHeight,
                    state.Kind == WorldEntityKind.Pet, state.EliteFlags);
                initializedOwner.lootOwnerId = state.Kind == WorldEntityKind.Pet ? 0 : state.OwnerEntityId;
            }
            if (!initializedOwner) return false;
            sourceOwner = initializedOwner;
            rootObject = sourceOwner.gameObject;
            renderer = sourceOwner.BodyForIndependentRuntime;
            pickBox = sourceOwner.ColliderForIndependentRuntime;
            shadow = sourceOwner.ShadowForIndependentRuntime;
            // Spawn/Init supplied the original enabled component. Adopting an optional view
            // does not change its Unity callbacks or any Init-owned renderer/collider state.
            state = state.WithHitPoints(sourceOwner.SelHp);
            standActionId = sourceOwner.StandActionForIndependentRuntime;
            walkActionId = sourceOwner.WalkActionForIndependentRuntime;
            attackActionId = sourceOwner.AttackActionForIndependentRuntime;
            prepareActionId = sourceOwner.PrepareActionForIndependentRuntime;
            deathActionId = sourceOwner.DeathActionForIndependentRuntime;
            standGrid = sourceOwner.StandFramesForIndependentRuntime;
            walkGrid = sourceOwner.WalkFramesForIndependentRuntime;
            hasWalkAction = walkActionId > 0;
            hasAttackAction = attackActionId > 0;
            hasPrepareAction = prepareActionId > 0;
            hasDeathAction = deathActionId > 0;
            standIntervalMilliseconds = sourceOwner.StandIntervalForIndependentRuntime;
            walkIntervalMilliseconds = hasWalkAction
                ? sourceOwner.WalkIntervalForIndependentRuntime
                : standIntervalMilliseconds;
            attackIntervalMilliseconds = hasAttackAction
                ? sourceOwner.AttackIntervalForIndependentRuntime
                : AttackIntervalMilliseconds;
            prepareIntervalMilliseconds = hasPrepareAction
                ? sourceOwner.PrepareIntervalForIndependentRuntime
                : 0f;
            deathIntervalMilliseconds = hasDeathAction
                ? sourceOwner.DeathIntervalForIndependentRuntime
                : DeathIntervalMilliseconds;
            standDirectionCount = standGrid == null ? 0 : standGrid.GetLength(0);
            standFramesPerDirection = standGrid == null ? 0 : standGrid.GetLength(1);
            walkDirectionCount = walkGrid == null ? 0 : walkGrid.GetLength(0);
            walkFramesPerDirection = walkGrid == null ? 0 : walkGrid.GetLength(1);
            attackDirectionCount = 0;
            attackFramesPerDirection = 0;
            prepareDirectionCount = 0;
            prepareFramesPerDirection = 0;
            deathDirectionCount = 0;
            deathFramesPerDirection = 0;

            currentFrame = 0;
            standFrame = 0;
            walkFrame = 0;
            standElapsedMilliseconds = 0f;
            walkElapsedMilliseconds = 0f;
            currentStoredDirection = sourceOwner.DirectionForIndependentRuntime;
            flipX = renderer && renderer.flipX;
            pathMotion = null;
            attackFrame = -1;
            attackElapsedMilliseconds = 0f;
            attackPlaying = false;
            prepareFrame = -1;
            prepareElapsedMilliseconds = 0f;
            prepareHeldMilliseconds = 0f;
            preparePlaying = false;
            deathElapsedMilliseconds = 0f;
            deathHoldSeconds = 0f;
            hitTintSeconds = 0f;
            dying = sourceOwner.IsDying;
            deathHidden = false;
            headTopUnits = sourceOwner.HeadTopForIndependentRuntime;

            if (!receivedOwner)
            {
                foreach (WorldEntityStatusEffect effect in state.StatusEffects)
                    sourceOwner.ShowStatusFx(effect.Effect, effect.PositionMode, effect.DurationMilliseconds);
                if (state.HasStatusIconsUpdate) ApplyNativeStatusIcons(state.StatusIcons);
            }
            headHpBarView = sourceOwner.HpBarForIndependentRuntime;
            if (!ObserveFeedbackFromOwner())
            {
                ReleaseOwnedState();
                return false;
            }

            glowRenderer = sourceOwner.GlowRendererForIndependentRuntime;

            built = true;
            return true;
        }
        catch
        {
            ReleaseOwnedState();
            return false;
        }
    }

    internal bool ObserveNativeFrameForIndependentRuntime()
    {
        ThrowIfDisposed();
        if (!built || !sourceOwner)
        {
            return false;
        }

        // Unity owns Update and LateUpdate. This observation may run before or after another
        // component's Update; it neither advances a clock nor imposes an engine callback order.
        glowRenderer = sourceOwner.GlowRendererForIndependentRuntime;
        currentStoredDirection = sourceOwner.DirectionForIndependentRuntime;
        flipX = sourceOwner.FlipForIndependentRuntime;
        attackFrame = sourceOwner.AttackFrameForIndependentRuntime;
        attackElapsedMilliseconds = sourceOwner.AttackTimerForIndependentRuntime;
        attackPlaying = attackFrame >= 0;
        prepareFrame = sourceOwner.PrepareFrameForIndependentRuntime;
        prepareElapsedMilliseconds = sourceOwner.PrepareTimerForIndependentRuntime;
        prepareHeldMilliseconds = sourceOwner.PrepareHeldForIndependentRuntime;
        preparePlaying = prepareFrame >= 0;
        // CombatWorld.MonsterPlayAttack and CombatWorld.MonsterPlayPrepare are the official
        // owners and start MonsterEntity.PlayAttack/PlayPrepare themselves, so this receive-side
        // mirror only picks the already decoded rows up. It is an observation, not a second
        // animation start, and it never decides when an action begins.
        if (attackPlaying && attackGrid == null) ObserveAttackGridFromOwner();
        if (preparePlaying && prepareGrid == null) ObservePrepareGridFromOwner();
        dying = sourceOwner.DyingForIndependentRuntime;
        currentFrame = dying
            ? sourceOwner.DeathFrameForIndependentRuntime
            : currentFrame;
        return ObserveFeedbackFromOwner();
    }

    private void ObserveAttackGridFromOwner()
    {
        if (!hasAttackAction || sourceOwner == null)
        {
            return;
        }

        attackGrid = sourceOwner.AttackFramesForIndependentRuntime;
        if (!HasDirectionalShape(attackGrid))
        {
            attackGrid = null;
            return;
        }

        attackDirectionCount = attackGrid.GetLength(0);
        attackFramesPerDirection = attackGrid.GetLength(1);
    }

    private void ObservePrepareGridFromOwner()
    {
        if (!hasPrepareAction || sourceOwner == null)
        {
            return;
        }

        prepareGrid = sourceOwner.PrepareFramesForIndependentRuntime;
        if (!HasDirectionalShape(prepareGrid))
        {
            prepareGrid = null;
            return;
        }

        prepareDirectionCount = prepareGrid.GetLength(0);
        prepareFramesPerDirection = prepareGrid.GetLength(1);
    }

    public bool TryApplyPath(WorldRemotePathState value)
    {
        ThrowIfDisposed();
        if (value != null && value.NativeMovementHandled) return true;
        if (!built || sourceOwner == null || value == null)
        {
            return false;
        }

        // Same revision guard the remote-player actor already uses: the receive event and the
        // retained snapshot carry the same WorldRemotePathState instance, so the second drain sees
        // a revision that is not newer and reaches the official owner only once.
        if (value.Revision > 0 && value.Revision <= lastMovementActionRevision)
        {
            return true;
        }

        int count = value.Points.Count;
        int[] xs = new int[count];
        int[] ys = new int[count];
        for (int index = 0; index < count; index++)
        {
            xs[index] = value.Points[index].X;
            ys[index] = value.Points[index].Y;
        }

        // CombatWorld.MovePath (0x06000149) is the official owner: a registered MonsterEntity
        // reaches MonsterEntity.SetPath, and an id no dictionary answers is recorded by
        // CombatWorld.NoteNoTarget instead of being dropped inside the protocol layer. Both drains
        // activate the frame's own CombatWorld first, so the lookup hits the world this monster was
        // registered in and the revision recorded below stands for a real dispatch - or for the
        // native legal no-op of a zero-count path - never for a message lost to another world.
        CombatWorld.MovePath(value.EntityId, xs, ys, value.Speed);
        if (value.Revision > 0)
        {
            lastMovementActionRevision = value.Revision;
        }

        return true;
    }

    public bool TryClearPath()
    {
        ThrowIfDisposed();
        if (!built || sourceOwner == null)
        {
            return false;
        }

        sourceOwner.ClearPathForIndependentRuntime();
        return true;
    }

    /// <summary>
    /// Applies MonsterEntity.PlayAttack without changing the actor's retained direction.
    /// </summary>
    public bool StartAttackAnimation()
    {
        ThrowIfDisposed();
        if (!built || dying || deathHidden)
        {
            return false;
        }

        attackGrid = sourceOwner == null
            ? null
            : sourceOwner.PlayAttackForIndependentRuntime();
        if (!hasAttackAction)
        {
            return true;
        }

        if (!HasDirectionalShape(attackGrid))
        {
            return false;
        }
        attackDirectionCount = attackGrid.GetLength(0);
        attackFramesPerDirection = attackGrid.GetLength(1);

        prepareFrame = sourceOwner.PrepareFrameForIndependentRuntime;
        prepareElapsedMilliseconds = sourceOwner.PrepareTimerForIndependentRuntime;
        prepareHeldMilliseconds = sourceOwner.PrepareHeldForIndependentRuntime;
        preparePlaying = prepareFrame >= 0;
        attackFrame = sourceOwner.AttackFrameForIndependentRuntime;
        attackElapsedMilliseconds = sourceOwner.AttackTimerForIndependentRuntime;
        attackPlaying = attackFrame >= 0;
        return true;
    }

    /// <summary>
    /// Applies MonsterEntity.PlayPrepare. Models without the source-selected caster preparation
    /// action keep their current action unchanged, matching the official no-op branch.
    /// </summary>
    public bool StartPrepareAnimation()
    {
        ThrowIfDisposed();
        if (!built || dying || deathHidden)
        {
            return false;
        }

        prepareGrid = sourceOwner == null
            ? null
            : sourceOwner.PlayPrepareForIndependentRuntime();
        if (!hasPrepareAction)
        {
            return true;
        }

        if (!HasDirectionalShape(prepareGrid))
        {
            return false;
        }
        prepareDirectionCount = prepareGrid.GetLength(0);
        prepareFramesPerDirection = prepareGrid.GetLength(1);

        attackFrame = sourceOwner.AttackFrameForIndependentRuntime;
        attackElapsedMilliseconds = sourceOwner.AttackTimerForIndependentRuntime;
        attackPlaying = attackFrame >= 0;
        prepareFrame = sourceOwner.PrepareFrameForIndependentRuntime;
        prepareElapsedMilliseconds = sourceOwner.PrepareTimerForIndependentRuntime;
        prepareHeldMilliseconds = sourceOwner.PrepareHeldForIndependentRuntime;
        preparePlaying = prepareFrame >= 0;
        return true;
    }

    public bool MatchesSpawnRoot(WorldEntityState value)
    {
        if (value == null ||
            state.EntityId != value.EntityId ||
            state.Kind != value.Kind ||
            state.ModelOrSkillId != value.ModelOrSkillId ||
            !string.Equals(state.DisplayName, value.DisplayName, StringComparison.Ordinal) ||
            state.Level != value.Level ||
            state.X != value.X || state.Y != value.Y ||
            state.MaximumHitPoints != value.MaximumHitPoints ||
            state.OwnerEntityId != value.OwnerEntityId ||
            state.EliteFlags != value.EliteFlags ||
            state.MapIdGuard != value.MapIdGuard ||
            state.FriendlyFlag != value.FriendlyFlag)
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies a live body-effect receive revision without rebuilding the monster actor.
    /// </summary>
    public bool TryApplyAuthoritativeStatusEffects(
        WorldEntityState value,
        float nowSeconds)
    {
        ThrowIfDisposed();
        if (!built || sourceOwner == null || !MatchesSpawnRoot(value) ||
            float.IsNaN(nowSeconds) || float.IsInfinity(nowSeconds))
        {
            return false;
        }

        if (state.StatusEffectsRevision == value.StatusEffectsRevision)
        {
            return true;
        }

        foreach (WorldEntityStatusEffect effect in value.StatusEffects)
        {
            bool alreadyApplied = false;
            foreach (WorldEntityStatusEffect previous in state.StatusEffects)
                if (previous.Effect == effect.Effect && previous.Revision == effect.Revision)
                { alreadyApplied = true; break; }
            if (!alreadyApplied)
                sourceOwner.ShowStatusFx(effect.Effect, effect.PositionMode, effect.DurationMilliseconds);
        }
        state = state.WithStatusEffects(value.StatusEffects, value.StatusEffectsRevision);
        return true;
    }

    /// <summary>
    /// Applies a live status-icon replacement without rebuilding the entity or reverting HP.
    /// </summary>
    private void ApplyNativeStatusIcons(IReadOnlyList<WorldEntityStatusIcon> rows)
    {
        // Compatibility snapshot construction builds its own MonsterEntity without registering
        // it, so this path keeps the direct call that needs no owner lookup.
        sourceOwner.SetStatusIcons(ConvertStatusIcons(rows));
    }

    internal static List<MonsterEntity.StatusIcon> ConvertStatusIcons(
        IReadOnlyList<WorldEntityStatusIcon> rows)
    {
        var nativeRows = new List<MonsterEntity.StatusIcon>(rows.Count);
        foreach (WorldEntityStatusIcon row in rows)
            nativeRows.Add(new MonsterEntity.StatusIcon
            {
                status = row.StatusId, icon = row.IconId,
                good = row.Beneficial != 0, remainMs = row.RemainingMilliseconds
            });
        return nativeRows;
    }

    internal void ObserveStatusEventForIndependentRuntime(WorldEntityState value)
    {
        state = state.WithStatusEffects(value.StatusEffects, value.StatusEffectsRevision)
            .WithStatusIcons(value.StatusIcons, value.StatusIconsRevision);
    }

    internal void ApplyStatusEffectEvent(WorldEntityStatusEffect effect, WorldEntityState snapshot)
    {
        // Official 0x06000158 owns the receive-time resolution: the id selects the local, monster
        // or remote-player arm and only the monster arm reaches MonsterEntity.ShowStatusFx. The
        // receive branch registers this actor's owner under this id through CombatWorld.Spawn
        // before it builds the actor, so the owner's lookup resolves to the same identity.
        CombatWorld.ShowEntityStatus(
            state.EntityId, effect.Effect, effect.PositionMode, effect.DurationMilliseconds);
        state = state.WithStatusEffects(snapshot.StatusEffects, snapshot.StatusEffectsRevision);
    }

    internal void ApplyStatusIconsEvent(WorldEntityState value)
    {
        state = state.WithStatusIcons(value.StatusIcons, value.StatusIconsRevision);
        // Official 0x06000159 resolves monsters only, so the replacement goes through the owner
        // instead of the actor's own reference.
        CombatWorld.SetEntityStatusIcons(state.EntityId, ConvertStatusIcons(value.StatusIcons));
    }

    internal void ApplyHitPointsEvent(int value)
    {
        sourceOwner.SetHpForIndependentRuntime(value);
        state = state.WithHitPoints(sourceOwner.SelHp);
    }

    public bool TryApplyAuthoritativeStatusIcons(
        WorldEntityState value,
        float nowSeconds)
    {
        ThrowIfDisposed();
        if (!built || sourceOwner == null || !MatchesSpawnRoot(value) ||
            float.IsNaN(nowSeconds) || float.IsInfinity(nowSeconds))
        {
            return false;
        }

        if (state.StatusIconsRevision == value.StatusIconsRevision)
        {
            return true;
        }

        state = state.WithStatusIcons(
            value.StatusIcons,
            value.StatusIconsRevision);
        ApplyNativeStatusIcons(state.StatusIcons);
        return ObserveFeedbackFromOwner();
    }

    internal void ObserveCombatStateForIndependentRuntime()
    {
        state = state.WithHitPoints(sourceOwner.SelHp);
        dying = sourceOwner.IsDying;
    }

    public bool TryApplyCombatResult(WorldCombatResultState result)
    {
        ThrowIfDisposed();
        if (!built || result == null || result.TargetEntityId != state.EntityId)
        {
            return false;
        }

        if (result.NativeCombatHandled)
        {
            ObserveCombatStateForIndependentRuntime();
            return true;
        }

        // OnCombat's 0x10 healing branch precedes miss handling and returns before BeginDie.
        if (result.Healing)
        {
            sourceOwner.SetHpForIndependentRuntime(result.RawTargetHitPoints);
            state = state.WithHitPoints(sourceOwner.SelHp);
            return true;
        }

        sourceOwner.SetHpForIndependentRuntime(result.RawTargetHitPoints);
        state = state.WithHitPoints(sourceOwner.SelHp);

        if ((result.Fatal || result.TargetHitPoints <= 0) &&
            !sourceOwner.DyingForIndependentRuntime &&
            !sourceOwner.BeginDieForIndependentRuntime())
        {
            return false;
        }

        dying = sourceOwner.DyingForIndependentRuntime;
        return true;
    }

    internal static int GlowVer => MonsterEntity.GlowVer;

    public static long GridCacheBytes => MonsterEntity.GridCacheBytes;

    private static Sprite SpriteAt(Sprite[,] grid, int direction, int frame)
    {
        if (grid == null || direction < 0 || frame < 0 ||
            direction >= grid.GetLength(0) || frame >= grid.GetLength(1))
        {
            return null;
        }

        return grid[direction, frame];
    }

    public static bool IsMonsterState(WorldEntityState value)
    {
        return value != null &&
               value.Kind == WorldEntityKind.Monster &&
               value.ModelOrSkillId > 0;
    }

    private static bool HasDirectionalShape(Sprite[,] grid)
    {
        return grid != null && grid.GetLength(0) >= 1 && grid.GetLength(1) >= 1;
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseOwnedState();
    }

    private static bool HasRequiredShape(Sprite[,] grid)
    {
        return grid != null && grid.GetLength(0) == RequiredDirectionCount &&
               grid.GetLength(1) == RequiredFramesPerDirection;
    }

    private static bool HasRequiredDeathShape(Sprite[,] grid)
    {
        return grid != null && grid.GetLength(0) == RequiredDeathDirectionCount &&
               grid.GetLength(1) == RequiredDeathFramesPerDirection;
    }

    private static bool HasRequiredAttackShape(Sprite[,] grid)
    {
        return grid != null && grid.GetLength(0) == RequiredAttackDirectionCount &&
               grid.GetLength(1) == RequiredAttackFramesPerDirection;
    }

    private Material ResolveGlowMaterial()
    {
        if (suppliedGlowMaterial != null)
        {
            return suppliedGlowMaterial;
        }

        return EffectPlayer.GlowLayerMat();
    }

    private void ApplyCurrentFrame()
    {
        int direction;
        int frame;
        Sprite[,] grid;
        Sprite normal;
        if (dying && hasDeathAction)
        {
            grid = deathGrid;
            direction = Math.Min(
                currentStoredDirection,
                deathDirectionCount - 1);
            frame = currentFrame;
        }
        else
        {
            currentFrame = standFrame;
            grid = standGrid;
            direction = Math.Min(
                currentStoredDirection,
                standDirectionCount - 1);
            frame = standFrame;
        }

        sourceOwner?.ArtHintForIndependentRuntime(false, direction);
        normal = !dying && sourceOwner != null
            ? sourceOwner.StandSpriteForIndependentRuntime(direction, frame)
            : SpriteAt(grid, direction, frame);
        ApplyFrameSprites(normal, MonsterEntity.GlowForIndependentRuntime(normal));
    }

    private void ApplyAttackFrame()
    {
        if (!hasAttackAction)
        {
            ApplyCurrentFrame();
            return;
        }

        int direction = Math.Min(
            currentStoredDirection,
            attackDirectionCount - 1);
        sourceOwner?.ArtHintForIndependentRuntime(false, direction);
        Sprite normal = SpriteAt(attackGrid, direction, attackFrame);
        if (normal == null)
        {
            // MonsterEntity.StepAtkFrame leaves the current body sprite untouched while the
            // official asynchronous action row has not decoded a usable frame yet.
            return;
        }
        ApplyFrameSprites(normal, MonsterEntity.GlowForIndependentRuntime(normal));
    }

    private void ApplyPrepareFrame()
    {
        if (!hasPrepareAction)
        {
            ApplyCurrentFrame();
            return;
        }

        int direction = Math.Min(
            currentStoredDirection,
            prepareDirectionCount - 1);
        sourceOwner?.ArtHintForIndependentRuntime(false, direction);
        Sprite normal = SpriteAt(prepareGrid, direction, prepareFrame);
        if (normal == null)
        {
            // MonsterEntity.StepPrepFrame has the same deferred-row behavior as StepAtkFrame.
            return;
        }
        ApplyFrameSprites(normal, MonsterEntity.GlowForIndependentRuntime(normal));
    }

    private void ApplyWalkFrame()
    {
        if (!hasWalkAction)
        {
            ApplyCurrentFrame();
            return;
        }

        currentFrame = walkFrame;
        int direction = Math.Min(
            currentStoredDirection,
            walkDirectionCount - 1);
        sourceOwner?.ArtHintForIndependentRuntime(true, direction);
        Sprite normal = SpriteAt(walkGrid, direction, walkFrame);
        ApplyFrameSprites(normal, MonsterEntity.GlowForIndependentRuntime(normal));
    }

    private void ApplyFrameSprites(Sprite normal, Sprite glow)
    {
        renderer.sprite = normal;
        renderer.flipX = flipX;
        if (glowRenderer != null)
        {
            glowRenderer.sprite = glow;
            glowRenderer.flipX = flipX;
            glowRenderer.enabled = glow != null && !deathHidden;
        }

        if (normal == null)
        {
            return;
        }

        if (shadow != null) shadow.SetSource(renderer);
    }

    private bool TryAdvancePath(float deltaTimeSeconds)
    {
        float previousWorldX = pathMotion.WorldX;
        float previousWorldY = pathMotion.WorldY;
        if (!pathMotion.TryAdvance(deltaTimeSeconds) || !ApplyMotionProjection())
        {
            return false;
        }

        float deltaX;
        float deltaY;
        if (pathMotion.TryGetFacingDelta(
                previousWorldX,
                previousWorldY,
                out deltaX,
                out deltaY))
        {
            int octant =
                (Mathf.RoundToInt(
                    Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg / 45f) + 8) % 8;
            ReferenceWorldPlayerFacing facing =
                ReferenceWorldPlayerMotionMath.FromOctant(octant);
            currentStoredDirection = facing.StoredDirection;
            flipX = facing.FlipX;
        }

        return true;
    }

    private bool ApplyMotionProjection()
    {
        if (rootObject == null || renderer == null || glowRenderer == null ||
            pathMotion == null)
        {
            return false;
        }

        float roundedX = Mathf.Round(pathMotion.WorldX);
        float roundedY = Mathf.Round(pathMotion.WorldY);
        rootObject.transform.position = new Vector3(
            (roundedX - map.MapWidth * 0.5f) /
                ReferenceFirstWorldStaticComposite.PixelsPerUnit,
            (map.MapHeight * 0.5f - roundedY) /
                ReferenceFirstWorldStaticComposite.PixelsPerUnit,
            0f);
        int sortingOrder = ReferenceWorldRemotePlayerActor.SortingBaseForMapY(
            pathMotion.WorldY);
        renderer.sortingOrder = sortingOrder;
        glowRenderer.sortingOrder = sortingOrder;
        return true;
    }

    private bool TryAdvanceWalkFrame(float deltaTimeSeconds)
    {
        if (!hasWalkAction)
        {
            ApplyCurrentFrame();
            return true;
        }

        walkElapsedMilliseconds += deltaTimeSeconds * 1000f;
        if (walkElapsedMilliseconds >= walkIntervalMilliseconds)
        {
            if (walkElapsedMilliseconds - walkIntervalMilliseconds <=
                walkIntervalMilliseconds)
            {
                walkElapsedMilliseconds -= walkIntervalMilliseconds;
            }
            else
            {
                walkElapsedMilliseconds = walkIntervalMilliseconds;
            }

            walkFrame = (walkFrame + 1) % walkFramesPerDirection;
        }

        ApplyWalkFrame();
        return true;
    }

    private bool BeginDeath()
    {
        if (!hasDeathAction)
        {
            return true;
        }

        deathGrid = sourceOwner == null
            ? null
            : sourceOwner.EnsureDeathForIndependentRuntime();
        if (!HasDirectionalShape(deathGrid))
        {
            return false;
        }
        deathDirectionCount = deathGrid.GetLength(0);
        deathFramesPerDirection = deathGrid.GetLength(1);
        preparePlaying = false;
        prepareFrame = -1;
        prepareElapsedMilliseconds = 0f;
        prepareHeldMilliseconds = 0f;
        attackPlaying = false;
        attackFrame = -1;
        attackElapsedMilliseconds = 0f;
        dying = hasDeathAction;
        deathHidden = false;
        currentFrame = 0;
        deathElapsedMilliseconds = 0f;
        deathHoldSeconds = 0f;
        hitTintSeconds = 0f;
        ApplyHitTint();
        ApplyCurrentFrame();
        return true;
    }

    private bool TryAdvanceAttack(float deltaTimeSeconds)
    {
        attackElapsedMilliseconds += deltaTimeSeconds * 1000f;
        while (attackElapsedMilliseconds >= attackIntervalMilliseconds &&
               attackFrame < attackFramesPerDirection)
        {
            attackElapsedMilliseconds -= attackIntervalMilliseconds;
            attackFrame++;
        }

        if (attackFrame >= attackFramesPerDirection)
        {
            attackPlaying = false;
            attackFrame = -1;
            ApplyCurrentFrame();
            return true;
        }

        ApplyAttackFrame();
        return true;
    }

    private bool TryAdvancePrepare(float deltaTimeSeconds)
    {
        prepareElapsedMilliseconds += deltaTimeSeconds * 1000f;
        int finalFrame = prepareFramesPerDirection - 1;
        while (prepareElapsedMilliseconds >= prepareIntervalMilliseconds &&
               prepareFrame < finalFrame)
        {
            prepareElapsedMilliseconds -= prepareIntervalMilliseconds;
            prepareFrame++;
        }

        if (prepareFrame >= finalFrame)
        {
            prepareHeldMilliseconds += deltaTimeSeconds * 1000f;
            if (prepareHeldMilliseconds >= PrepareFinalHoldMilliseconds)
            {
                preparePlaying = false;
                prepareFrame = -1;
                prepareHeldMilliseconds = 0f;
                ApplyCurrentFrame();
                return true;
            }
        }
        else
        {
            prepareHeldMilliseconds = 0f;
        }

        ApplyPrepareFrame();
        return true;
    }

    private bool TryAdvanceDeath(float deltaTimeSeconds)
    {
        if (deathHidden)
        {
            return true;
        }

        if (currentFrame < deathFramesPerDirection - 1)
        {
            deathElapsedMilliseconds += deltaTimeSeconds * 1000f;
            while (deathElapsedMilliseconds >= deathIntervalMilliseconds &&
                   currentFrame < deathFramesPerDirection - 1)
            {
                deathElapsedMilliseconds -= deathIntervalMilliseconds;
                currentFrame++;
            }

            ApplyCurrentFrame();
            return true;
        }

        deathHoldSeconds += deltaTimeSeconds;
        if (deathHoldSeconds >= DeathHoldSeconds)
        {
            deathHidden = true;
            renderer.enabled = false;
            glowRenderer.enabled = false;
            pickBox.enabled = false;
        }

        return true;
    }

    private bool TryAdvanceHitTint(float deltaTimeSeconds)
    {
        if (hitTintSeconds <= 0f)
        {
            return true;
        }

        hitTintSeconds = Mathf.Max(0f, hitTintSeconds - deltaTimeSeconds);
        ApplyHitTint();
        return renderer != null && glowRenderer != null;
    }

    private void ApplyHitTint()
    {
        if (renderer == null || glowRenderer == null)
        {
            return;
        }

        float fraction = Mathf.Clamp01(hitTintSeconds / HitTintSeconds);
        float channel = Mathf.Lerp(1f, 0.7f, fraction);
        Color tint = new Color(channel, channel, channel, 1f);
        renderer.color = tint;
        glowRenderer.color = tint;
    }

    private bool ObserveFeedbackFromOwner()
    {
        if (rootObject == null || sourceOwner == null || headHpBarView == null) return false;
        headTopUnits = sourceOwner.HeadTopForIndependentRuntime;
        return true;
    }

#if UNITY_EDITOR
    // Historical audit API only: supplied elapsed time cannot drive a second native frame.
    public bool TryAdvanceStanding(float deltaTimeSeconds)
    {
        return !float.IsNaN(deltaTimeSeconds) && !float.IsInfinity(deltaTimeSeconds) &&
            deltaTimeSeconds >= 0f && ObserveNativeFrameForIndependentRuntime();
    }

    internal bool CurrentSpriteMatchesAttackFrameForAudit
    {
        get
        {
            if (!attackPlaying || renderer == null || attackGrid == null)
            {
                return false;
            }

            int direction = Math.Min(
                currentStoredDirection,
                attackGrid.GetLength(0) - 1);
            return renderer.sprite == SpriteAt(attackGrid, direction, attackFrame);
        }
    }

    internal static int GridCacheCountForAudit =>
        MonsterEntity.GridCacheCountForAudit;

    internal static int GridReferenceCountForAudit(int actId)
    {
        return MonsterEntity.GridReferenceCountForAudit(actId);
    }

    internal static long GridBudgetBytesForAudit =>
        MonsterEntity.GridBudgetBytesForAudit;

    internal static int EvictedActionCountForAudit =>
        MonsterEntity.EvictedActionCountForAudit;

    internal static long EvictedBytesForAudit =>
        MonsterEntity.EvictedBytesForAudit;

    internal static void SetGridBudgetForAudit(long value)
    {
        MonsterEntity.SetGridBudgetForAudit(value);
    }

    internal static void AgeAllGridsForAudit(float ageSeconds)
    {
        MonsterEntity.AgeAllGridsForAudit(ageSeconds);
    }

    internal static void ResetEvictionTickForAudit()
    {
        MonsterEntity.ResetEvictionTickForAudit();
    }

    internal static void EvictGridsIfNeededForAudit()
    {
        MonsterEntity.EvictGridsIfNeededForAudit();
    }

    internal static void ClearGridCacheForAudit()
    {
        MonsterEntity.ClearGridCacheForAudit();
    }

    internal bool RefreshRecoveredArtForAudit()
    {
        if (!built)
        {
            return false;
        }

        ApplyCurrentFrame();
        sourceOwner.UpdateNameTagForIndependentRuntime();
        sourceOwner.PlaceStatusIconsForIndependentRuntime();
        sourceOwner.UpdateHpBarForIndependentRuntime();
        return ObserveFeedbackFromOwner();
    }
#endif

    private void ReleaseOwnedState()
    {
        built = false;
        // Native OnDestroy owns grids, rings, name/status textures and the HP view.
        headHpBarView = null;

        shadow = null;
        pickBox = null;
        glowRenderer = null;
        renderer = null;
        if (rootObject != null)
        {
            if (ownsNativeRoot) DestroyOwned(rootObject);
            rootObject = null;
        }
        sourceOwner = null;

        glowMaterialApplied = false;
        standGrid = null;
        walkGrid = null;
        attackGrid = null;
        prepareGrid = null;
        deathGrid = null;
        standDirectionCount = 0;
        standFramesPerDirection = 0;
        walkDirectionCount = 0;
        walkFramesPerDirection = 0;
        attackDirectionCount = 0;
        attackFramesPerDirection = 0;
        prepareDirectionCount = 0;
        prepareFramesPerDirection = 0;
        deathDirectionCount = 0;
        deathFramesPerDirection = 0;
        standIntervalMilliseconds = StandIntervalMilliseconds;
        walkIntervalMilliseconds = WalkIntervalMilliseconds;
        attackIntervalMilliseconds = AttackIntervalMilliseconds;
        prepareIntervalMilliseconds = 0f;
        deathIntervalMilliseconds = DeathIntervalMilliseconds;
        hasWalkAction = false;
        hasAttackAction = false;
        hasPrepareAction = false;
        hasDeathAction = false;
        standActionId = 0;
        walkActionId = 0;
        attackActionId = 0;
        prepareActionId = 0;
        deathActionId = 0;
        pathMotion = null;
        currentStoredDirection = DefaultStoredDirection;
        flipX = false;
        currentFrame = 0;
        standFrame = 0;
        walkFrame = 0;
        standElapsedMilliseconds = 0f;
        walkElapsedMilliseconds = 0f;
        attackFrame = -1;
        attackElapsedMilliseconds = 0f;
        attackPlaying = false;
        prepareFrame = -1;
        prepareElapsedMilliseconds = 0f;
        prepareHeldMilliseconds = 0f;
        preparePlaying = false;
        deathElapsedMilliseconds = 0f;
        deathHoldSeconds = 0f;
        hitTintSeconds = 0f;
        dying = false;
        deathHidden = false;
        headTopUnits = 0f;
    }

    private static void DestroyOwned(UnityEngine.Object value)
    {
        if (value == null)
        {
            return;
        }

        if (UnityEngine.Application.isPlaying)
        {
            UnityEngine.Object.Destroy(value);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(value);
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldMonsterActor));
        }
    }
}

}
