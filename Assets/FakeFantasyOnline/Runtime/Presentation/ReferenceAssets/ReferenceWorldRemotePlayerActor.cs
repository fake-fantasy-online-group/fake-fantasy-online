using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// One frame decision from RemotePlayer.UpdateIdle's special work-body clock.
/// </summary>
public struct ReferenceWorldRemoteWorkIdleAnimationPlan
{
    public ReferenceWorldRemoteWorkIdleAnimationPlan(float timerSeconds, int frame)
    {
        TimerSeconds = timerSeconds;
        Frame = frame;
    }

    public float TimerSeconds { get; }
    public int Frame { get; }
}

/// <summary>
/// Source-owned timing for body models 950..953 in RemotePlayer.UpdateIdle.
/// </summary>
public static class ReferenceWorldRemoteWorkIdleAnimationMath
{
    public const float SecondsPerFrame = 0.12f;

    public static ReferenceWorldRemoteWorkIdleAnimationPlan Advance(
        float timerSeconds,
        float deltaTime,
        int frame,
        int framesPerDirection)
    {
        ValidateFiniteNonnegative(timerSeconds, nameof(timerSeconds));
        ValidateFiniteNonnegative(deltaTime, nameof(deltaTime));
        if (frame < 0 || framesPerDirection < 2)
        {
            throw new ArgumentOutOfRangeException(
                frame < 0 ? nameof(frame) : nameof(framesPerDirection));
        }

        float nextTimer = timerSeconds + deltaTime;
        if (float.IsInfinity(nextTimer))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        int nextFrame = frame % framesPerDirection;
        // UpdateIdle uses one conditional, not a catch-up loop: even a long frame advances once.
        if (nextTimer >= SecondsPerFrame)
        {
            nextTimer -= SecondsPerFrame;
            nextFrame = (nextFrame + 1) % framesPerDirection;
        }

        return new ReferenceWorldRemoteWorkIdleAnimationPlan(nextTimer, nextFrame);
    }

    private static void ValidateFiniteNonnegative(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

/// <summary>
/// One RemotePlayer.StepAct clock decision.
/// </summary>
public struct ReferenceWorldRemoteActionAnimationPlan
{
    public ReferenceWorldRemoteActionAnimationPlan(
        float elapsedSeconds,
        int frame,
        bool active)
    {
        ElapsedSeconds = elapsedSeconds;
        Frame = frame;
        Active = active;
    }

    public float ElapsedSeconds { get; }
    public int Frame { get; }
    public bool Active { get; }
}

/// <summary>
/// Historical diagnostic arithmetic; live action decisions and clocks are owned by RemotePlayer.Actions.
/// </summary>
public static class ReferenceWorldRemoteActionAnimationMath
{
    public const float AttackFrameSeconds = 0.06f;
    public const float MagicFrameSeconds = 0.07f;

    public static int AttackPoseVariant(int attackVariant)
    {
        switch (attackVariant)
        {
            case 1:
                return 0;
            case 2:
            case 4:
                return 1;
            case 3:
                return 2;
            default:
                return 2;
        }
    }

    public static int NormalizeFramesPerDirection(int framesPerDirection)
    {
        return framesPerDirection < 2 ? 1 : framesPerDirection;
    }

    public static ReferenceWorldRemoteActionAnimationPlan Advance(
        float elapsedSeconds,
        float deltaTime,
        float frameSeconds,
        int framesPerDirection)
    {
        ValidateFiniteNonnegative(elapsedSeconds, nameof(elapsedSeconds));
        ValidateFiniteNonnegative(deltaTime, nameof(deltaTime));
        if (!(frameSeconds > 0f) || float.IsNaN(frameSeconds) ||
            float.IsInfinity(frameSeconds) || framesPerDirection < 1)
        {
            throw new ArgumentOutOfRangeException(
                !(frameSeconds > 0f) ? nameof(frameSeconds) : nameof(framesPerDirection));
        }

        float nextElapsed = elapsedSeconds + deltaTime;
        if (float.IsInfinity(nextElapsed))
        {
            throw new ArgumentOutOfRangeException(nameof(deltaTime));
        }

        float duration = frameSeconds * framesPerDirection;
        if (nextElapsed >= duration)
        {
            return new ReferenceWorldRemoteActionAnimationPlan(
                nextElapsed,
                framesPerDirection - 1,
                false);
        }

        int frame = Math.Max(
            0,
            Math.Min(framesPerDirection - 1, (int)(nextElapsed / frameSeconds)));
        return new ReferenceWorldRemoteActionAnimationPlan(nextElapsed, frame, true);
    }

    private static void ValidateFiniteNonnegative(float value, string parameterName)
    {
        if (float.IsNaN(value) || float.IsInfinity(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName);
        }
    }
}

/// <summary>
/// One exact BoxCollider2D shape selected by RemotePlayer.UpdatePickBox.
/// </summary>
public struct ReferenceWorldRemotePickBoxPlan
{
    public ReferenceWorldRemotePickBoxPlan(Vector2 size, Vector2 offset)
    {
        Size = size;
        Offset = offset;
    }

    public Vector2 Size { get; }
    public Vector2 Offset { get; }
}

/// <summary>
/// Source-owned geometry and update threshold for the remote-player pick box.
/// </summary>
public static class ReferenceWorldRemotePickBoxMath
{
    public const float ChangeThreshold = 0.02f;
    public const float RaisedThreshold = 0.01f;
    public const float FootWidth = 0.6f;
    public const float FootHeight = 1.4f;
    public const float FootOffsetY = 0.6f;
    public const float RaisedMinimumWidth = 0.9f;

    public static bool ShouldApply(
        float appliedRaise,
        float appliedSaddleX,
        float raise,
        float saddleX)
    {
        return IsFinite(appliedRaise) && IsFinite(appliedSaddleX) &&
               IsFinite(raise) && IsFinite(saddleX) &&
               (Mathf.Abs(raise - appliedRaise) >= ChangeThreshold ||
                Mathf.Abs(saddleX - appliedSaddleX) >= ChangeThreshold);
    }

    public static bool TryResolve(
        float raise,
        float saddleX,
        out ReferenceWorldRemotePickBoxPlan plan)
    {
        plan = default(ReferenceWorldRemotePickBoxPlan);
        if (!IsFinite(raise) || !IsFinite(saddleX))
        {
            return false;
        }

        if (raise <= RaisedThreshold)
        {
            plan = new ReferenceWorldRemotePickBoxPlan(
                new Vector2(FootWidth, FootHeight),
                new Vector2(0f, FootOffsetY));
            return true;
        }

        plan = new ReferenceWorldRemotePickBoxPlan(
            new Vector2(
                Mathf.Max(FootWidth, RaisedMinimumWidth),
                FootHeight + raise),
            new Vector2(saddleX * 0.5f, FootOffsetY + raise * 0.5f));
        return true;
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

/// <summary>
/// Observes the native CombatWorld-owned identity for one server-authored remote player.
/// </summary>
/// <remarks>
/// The existing hierarchy and receive snapshots connect to actual RemotePlayer path, pose and
/// action/animation members. Original Avatar caches and clocks remain in those owners. Full
/// Init/Rebuild/appearance/Update, mount/anchor, selection and feedback lifecycles remain separately
/// scoped recovery; this assembler does not certify their remaining legacy branches.
/// </remarks>
internal sealed class ReferenceWorldRemotePlayerActor :
    IDisposable,
    IReferenceWorldRemoteRiderDoll
{
    public const int LayerCapacity = 14;
    public const int MaximumSortingBase = 30000;

    private static readonly Color32 VisibleTint =
        new Color32(255, 255, 255, 255);
    private static readonly Color32 InvisibleTint =
        new Color32(64, 64, 128, 80);

    private readonly ReferenceWorldRemoteFeedbackAssets feedbackAssets;
    private WorldRemotePlayerState state;
    private readonly List<SpriteRenderer> layerRenderers =
        new List<SpriteRenderer>(LayerCapacity);

    private ReferenceWorldPlayerNormalPoses normalPoses;
    private PlayerAvatar viewedAvatar;
    private ReferenceWorldPlayerSittingPoses sittingPoses;
    private ReferenceWorldPlayerStandPose currentPose;
    private ReferenceWorldPlayerFacing detachedFacing =
        ReferenceWorldPlayerMotionMath.FromOctant(
            ReferenceWorldPlayerMotionMath.DefaultFacingOctant);
    private ReferenceWorldPlayerFacing currentFacing
    {
        get => sourceIdentity == null ? detachedFacing :
            ReferenceWorldPlayerMotionMath.FromOctant(ReferenceWorldPlayerMotionMath.FacingOctant(
                sourceIdentity.DirectionForIndependentRuntime, sourceIdentity.FlipForIndependentRuntime));
        set
        {
            if (sourceIdentity == null) detachedFacing = value;
            else
            {
                sourceIdentity.DirectionForIndependentRuntime = value.StoredDirection;
                sourceIdentity.FlipForIndependentRuntime = value.FlipX;
            }
        }
    }
    private WorldRemoteRidingState currentRidingState;
    private GameObject rootObject;
    private RemotePlayer sourceIdentity;
    private GameObject dollRootObject;
    private SpriteRenderer mainRenderer;
    private SpriteRenderer bodyRenderer;
    private BoxCollider2D pickBox;
    private ReferenceWorldBlobShadow shadow;
    private ReferenceWorldRemoteNameTagView nameTagView;
    private HeadHpBarView headHpBarView;
    private ReferenceWorldStallSignView stallSignView;
    private ReferenceWorldRemoteChatBubbleView chatBubbleView;
    private WorldStallOwnerState stallOwnerState;
    private bool feedbackVisibilityKnown;
    private int feedbackNameMode;
    private bool feedbackInParty;
    private string feedbackPartyName = string.Empty;
    private int feedbackPartyLeaderId;
    private bool hidden;
    private bool mountedArtAllowedByNameMode = true;
    private WorldRemoteActionKind actionKind;
    private int actionVariant;
    private long lastMovementActionRevision;
    private bool built;
    private bool disposed;

    public ReferenceWorldRemotePlayerActor(
        ReferenceGsmMap map,
        ReferenceResourceLoader resources,
        Transform parent,
        WorldRemotePlayerState state,
        ReferenceWorldRemoteFeedbackAssets feedbackAssets = null)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        // The loader is no longer consumed here: a remote player's avatar is produced by its own
        // RemotePlayer.Init/Rebuild owner, so this assembler never builds one. The argument is kept
        // (and still validated) so every existing caller keeps one construction signature.
        if (resources == null) throw new ArgumentNullException(nameof(resources));
        // Keep the existing assembly signature, but the native-created root is unparented.
        // CombatWorld's registry, including inactive-frame retirement, owns its lifetime.
        if (parent == null) throw new ArgumentNullException(nameof(parent));
        this.state = state ?? throw new ArgumentNullException(nameof(state));
        this.feedbackAssets = feedbackAssets;
        currentRidingState = state.RidingState;

        if (!map.Ok && !map.HasCover && !map.HasTextureBackgroundForIndependentRuntime)
        {
            throw new ArgumentException(
                "The remote-player actor requires a parsed legacy map.",
                nameof(map));
        }
    }

    public bool Built => built;
    public Transform Root => rootObject == null ? null : rootObject.transform;
    public Transform DollRoot => dollRootObject == null ? null : dollRootObject.transform;
    public SpriteRenderer MainRenderer => mainRenderer;
    public SpriteRenderer BodyRenderer => bodyRenderer;
    public BoxCollider2D PickBox => pickBox;
    public ReferenceWorldBlobShadow Shadow => shadow;
    internal ReferenceWorldRemoteNameTagView NameTagView => nameTagView;
    internal HeadHpBarView HeadHpBarView => sourceIdentity?.HeadHpViewForIndependentRuntime;
    internal ReferenceWorldStallSignView StallSignView => stallSignView;
    internal ReferenceWorldRemoteChatBubbleView ChatBubbleView => chatBubbleView;
    public WorldStallOwnerState StallOwnerState => stallOwnerState;
    public bool Hidden => hidden;
    public bool MountedArtVisible => sourceIdentity != null && sourceIdentity.MountVisibleForIndependentRuntime;
    public float MountArtFarSeconds => sourceIdentity == null ? 0f : sourceIdentity.MountFarSecondsForIndependentRuntime;
    public float WorldX => sourceIdentity == null ? state.X : sourceIdentity.worldX;
    public float WorldY => sourceIdentity == null ? state.Y : sourceIdentity.worldY;
    public float MoveSpeed => sourceIdentity == null ? 120f : sourceIdentity.MoveSpeedForIndependentRuntime;
    public float CatchUp => sourceIdentity == null ? 1f : sourceIdentity.CatchUpForIndependentRuntime;
    public int PathIndex => sourceIdentity == null ? 0 : sourceIdentity.PathIndexForIndependentRuntime;
    public int PathCount => sourceIdentity == null ? 0 : sourceIdentity.PathCountForIndependentRuntime;
    public bool Moving => sourceIdentity != null && sourceIdentity.MovingForIndependentRuntime;
    public bool Dead => sourceIdentity == null ? state.Dead : sourceIdentity.IsDead;
    public int CurrentWalkFrame => sourceIdentity == null ? 0 : sourceIdentity.WalkFrameForIndependentRuntime;
    public int CurrentWorkFrame => sourceIdentity == null ? 0 : sourceIdentity.WorkFrameForIndependentRuntime;
    public int CurrentDeathFrame => sourceIdentity == null ? 0 : sourceIdentity.DeathFrameForIndependentRuntime;
    public float WalkTimer => sourceIdentity == null ? 0 : sourceIdentity.WalkTimerForIndependentRuntime;
    public float WorkTimer => sourceIdentity == null ? 0 : sourceIdentity.WorkTimerForIndependentRuntime;
    public float DeathElapsedSeconds => sourceIdentity == null ? 0 : sourceIdentity.DeathElapsedForIndependentRuntime;
    public bool ActionActive => sourceIdentity != null && sourceIdentity.ActingForIndependentRuntime;
    public WorldRemoteActionKind ActionKind => actionKind;
    public int CurrentActionFrame => sourceIdentity == null ? 0 : sourceIdentity.ActionFrameForIndependentRuntime;
    public int CurrentActionVariant => actionVariant;
    public float ActionElapsedSeconds => sourceIdentity == null ? 0 : sourceIdentity.ActionElapsedForIndependentRuntime;
    public float ActionFrameSeconds => sourceIdentity == null ? 0 : sourceIdentity.ActionFrameSecondsForIndependentRuntime;
    public int ActionFramesPerDirection => sourceIdentity == null ? 0 : sourceIdentity.ActionFramesPerDirectionForIndependentRuntime;
    public long LastMovementActionRevision => lastMovementActionRevision;
    public float PickRaiseApplied => sourceIdentity == null ? 0f : sourceIdentity.PickRaiseForIndependentRuntime;
    public float PickSaddleApplied => sourceIdentity == null ? 0f : sourceIdentity.PickSaddleForIndependentRuntime;
    public ReferenceWorldPlayerFacing CurrentFacing => currentFacing;
    public WorldRemotePlayerState State => state;
    internal RemotePlayer SourceIdentityForIndependentRuntime => sourceIdentity;
    public WorldRemoteRidingState CurrentRidingState => currentRidingState;
    // Historical Editor inspection API; live rendering belongs to RemotePlayer.
    public ReferenceWorldRemoteMountRider MountRider => null;
    public IReadOnlyList<SpriteRenderer> LayerRenderers =>
        new ReadOnlyCollection<SpriteRenderer>(layerRenderers);
    internal ReferenceWorldPlayerNormalPoses NormalPoses => normalPoses;
    internal ReferenceWorldPlayerSittingPoses SittingPoses => sittingPoses;
    internal ReferenceWorldPlayerDeathPoses DeathPoses => sourceIdentity == null ? null :
        normalPoses?.DeathViewForIndependentRuntime(sourceIdentity.DeathFramesForIndependentRuntime,
            sourceIdentity.DeathFramesPerDirectionForIndependentRuntime);
    internal ReferenceWorldPlayerStandPose CurrentPose => currentPose;

    public bool Build(float standTimeSeconds, bool deferSpawnTail = false)
    {
        ThrowIfDisposed();
        if (built || !IsFinite(standTimeSeconds)) return false;
        if (!SpawnAndAdoptForIndependentRuntime(state)) return false;
        // The legacy deferSpawnTail argument now defers only this snapshot projection.
        // A consolidated ordinary/QA snapshot includes a separate 0x8053 PK state. The receive
        // path supplies that edge separately. Neither adaptation belongs to SpawnPlayer.
        if (!deferSpawnTail) ApplySnapshotPkForIndependentRuntime(state);
        return true;
    }

    private bool SpawnAndAdoptForIndependentRuntime(WorldRemotePlayerState value)
    {
        CharacterAppearanceRecord appearance = value.Appearance;
        // All 24 arguments are the already decoded row. Native SpawnPlayer alone decides
        // whether to retain a Unity-live identity or create, initialize and publish a new one.
        CombatWorld.SpawnPlayer(value.EntityId, value.DisplayName, value.Title, value.Level,
            value.X, value.Y, value.Gender, value.Head, value.Hair, appearance.AttackVariant,
            appearance.Body, appearance.Cap, appearance.Backpack, appearance.RightWeapon,
            appearance.LeftWeapon, appearance.Face, value.Dead, value.AvatarAura,
            value.SoulEffects, value.Faction, value.HitPoints, value.MaximumHitPoints,
            value.TitleEffect, value.GlowMode);
        return AdoptNativeIdentityForIndependentRuntime(
            CombatWorld.SpawnedPlayerForIndependentRuntime(value.EntityId));
    }

    internal bool AdoptReceivedIdentityForIndependentRuntime(WorldRemotePlayerState value, object receivedIdentity)
    {
        RemotePlayer identity = receivedIdentity as RemotePlayer;
        if (identity == null || !ReferenceEquals(CombatWorld.FindPlayer(value.EntityId), identity))
            return false;
        state = value;
        currentRidingState = value.RidingState;
        return AdoptNativeIdentityForIndependentRuntime(identity);
    }

    internal void ObserveRegistryEventForIndependentRuntime(WorldRemotePlayerState value)
    {
        if (sourceIdentity == null ||
            !ReferenceEquals(CombatWorld.FindPlayer(state.EntityId), sourceIdentity)) return;
        if (value != null)
        {
            state = value;
            currentRidingState = value.RidingState;
        }
        state = state.WithLifeState(sourceIdentity.IsDead, state.LifeStateRevision)
            .WithPkState(sourceIdentity.pkState, state.PkStateRevision);
        ObserveNativeVitals();
        ObserveNativePose();
    }

    private bool AdoptNativeIdentityForIndependentRuntime(RemotePlayer identity)
    {
        if (!ReferenceEquals(sourceIdentity, identity))
        {
            // Release only this observer's views. A successful native spawn owns its root and
            // registration even if later observer construction fails; no host rollback occurs.
            ReleaseOwnedState();
            sourceIdentity = identity;
            rootObject = identity.gameObject;
            mainRenderer = identity.BodyForIndependentRuntime;
            pickBox = identity.PickBoxForIndependentRuntime;
            Transform dollRoot = identity.DollRootForIndependentRuntime;
            shadow = identity.ShadowForIndependentRuntime;
            if (dollRoot == null || shadow == null) return false;
            dollRootObject = dollRoot.gameObject;
            SpriteRenderer[] dollLayers = identity.DollRenderersForIndependentRuntime;
            if (dollLayers != null) layerRenderers.AddRange(dollLayers);
        }

        if (dollRootObject == null || shadow == null) return false;
        // Init's hide/frame-budget gate may legitimately leave the avatar absent. Observe the
        // owner's pose without a second avatar build, stand-pose application or state binding.
        ObserveNativePose();
        ObserveNativeVitals();
        built = true;
        return true;
    }

    private void ApplySnapshotPkForIndependentRuntime(WorldRemotePlayerState value) =>
        CombatWorld.SetPlayerPkState(value.EntityId, value.PkState);

    public bool ApplyOrdinaryIdle(float standTimeSeconds)
    {
        return ApplyIdle(0f, standTimeSeconds);
    }

    private bool ApplyIdle(float deltaTime, float standTimeSeconds)
    {
        ThrowIfDisposed();
        if (!built || sourceIdentity == null || state.Dead || currentRidingState.Riding) return false;
        // RemotePlayer.Update (0x06000b21) takes this arm from _isStallOwner (+0x1b8), which the
        // update refreshes once per frame from StallDlg.IsStallOwner(id) before the dispatch. The
        // visibility row records the same owner map for the sign/name-tag seam, but the poll can
        // reject after StallDlg was already rebound, so that row is not the dispatch input.
        if (sourceIdentity.StallOwnerForIndependentRuntime) sourceIdentity.ApplyStallPoseForIndependentRuntime();
        else sourceIdentity.UpdateIdleForIndependentRuntime();
        ObserveNativePose();
        return true;
    }

    /// <summary>
    /// Applies the source-owned RemotePlayer.SetPath state for an ordinary remote player.
    /// </summary>
    public bool TryApplyPath(WorldRemotePathState value, float standTimeSeconds)
    {
        ThrowIfDisposed();
        if (value != null && value.NativeMovementHandled) return true;
        if (!built || sourceIdentity == null || value == null || value.EntityId != state.EntityId)
            return false;
        if (value.Revision > 0 && value.Revision <= lastMovementActionRevision) return true;
        int[] xs = new int[value.Points.Count];
        int[] ys = new int[value.Points.Count];
        for (int i = 0; i < xs.Length; i++)
        {
            xs[i] = value.Points[i].X;
            ys[i] = value.Points[i].Y;
        }
        // The official owner resolves the id itself (CombatWorld.MovePath, 0x06000149): the local
        // player id warps, a registered RemotePlayer reaches RemotePlayer.SetPath, a registered
        // MonsterEntity reaches MonsterEntity.SetPath and an id nobody owns is recorded by
        // CombatWorld.NoteNoTarget. This call is the single application site for a receive path,
        // so one 0x8018 message reaches the official owner once and no second channel repeats it.
        // The consuming frame activates its own CombatWorld before the drain
        // (ReferenceFirstWorldInitialFrame.RemoteEvents.cs), so the dictionary lookup below hits
        // the world this actor was registered in and marking the revision consumed at the end
        // records a real dispatch - or the native legal no-op of a zero-count path - instead of
        // dropping the message through a foreign world.
        CombatWorld.MovePath(state.EntityId, xs, ys, value.Speed);
        ObserveNativePose();
        if (value.Revision > 0) lastMovementActionRevision = value.Revision;
        return true;
    }

    /// <summary>
    /// Applies one authoritative CombatWorld action edge without inventing a target transform.
    /// </summary>
    public bool TryApplyAction(
        WorldRemoteActionState value,
        Vector3? faceTargetPosition)
    {
        ThrowIfDisposed();
        if (!built || sourceIdentity == null || value == null ||
            value.EntityId != state.EntityId ||
            (faceTargetPosition.HasValue &&
             (!IsFinite(faceTargetPosition.Value.x) ||
              !IsFinite(faceTargetPosition.Value.y))))
        {
            return false;
        }

        if (value.Revision <= lastMovementActionRevision)
        {
            return true;
        }

        // Receive actions have only the original owner/target presence boundary. Death takes
        // precedence in Update; it does not discard an authoritative action at receive time.
        if (value.Kind == WorldRemoteActionKind.Attack)
        {
            sourceIdentity.PlayAttack(faceTargetPosition);
            actionVariant = sourceIdentity.AttackVariantForIndependentRuntime;
        }
        else if (value.Kind == WorldRemoteActionKind.Magic)
        {
            sourceIdentity.PlayCast();
            actionVariant = 0;
        }
        else return false;
        actionKind = value.Kind; // Receive-event observation; it does not select the native actFrames.
        lastMovementActionRevision = value.Revision;
        return true;
    }

    // The 0x800e remote branch calls the official CombatWorld.PlayerCast owner, which starts
    // RemotePlayer.PlayCast itself, so no receive-side revision stamp is produced here.

    /// <summary>
    /// Advances the ordinary RemotePlayer AdvancePath/StepWalk/UpdateIdle precedence once.
    /// </summary>
    public bool TryAdvanceOrdinaryPath(
        float deltaTime,
        float standTimeSeconds)
    {
        ThrowIfDisposed();
        if (!built || sourceIdentity == null || state.Dead || currentRidingState.Riding ||
            !IsFinite(deltaTime) || deltaTime < 0f ||
            !IsFinite(standTimeSeconds))
        {
            return false;
        }

        // Original Update chooses the walking branch before AdvancePath can clear moving.
        if (sourceIdentity.MovingForIndependentRuntime)
        {
            if (!TryAdvancePathPosition(deltaTime)) return false;
            return ApplyOrdinaryWalk(deltaTime);
        }
        return ApplyIdle(deltaTime, standTimeSeconds);
    }

    /// <summary>
    /// Historical API: observes the Unity-driven RemotePlayer; it no longer advances a frame.
    /// Supplied delta/time values are retained only for caller compatibility, not simulation.
    /// </summary>
    public bool TryAdvanceRecoveredUpdate(
        float deltaTime,
        float standTimeSeconds,
        bool hideOtherPets,
        Camera worldCamera)
    {
        ThrowIfDisposed();
        if (!built || !IsFinite(deltaTime) || deltaTime < 0f ||
            !IsFinite(standTimeSeconds))
        {
            return false;
        }

        // Native Update runs independently on the root, even if this observer is absent or the
        // host's optional UI/projection work rejected its Tick. Never call that owner here.
        if (!sourceIdentity.UpdateBodyPresentForIndependentRuntime) return true;
        hidden = sourceIdentity.HiddenForIndependentRuntime;
        mountedArtAllowedByNameMode = CharData.nameMode < 3 || sourceIdentity.F11ExemptForIndependentRuntime;
        ObserveNativePose();
        if (!sourceIdentity.AvatarReadyForIndependentRuntime || sourceIdentity.IsDead) return true;
        return TryUpdateRemoteFeedback(worldCamera);
    }

    /// <summary>
    /// Advances RemotePlayer.StepDie before every riding/action/idle/walk branch.
    /// </summary>
    public bool TryAdvanceDeath(float deltaTime)
    {
        ThrowIfDisposed();
        if (!built || sourceIdentity == null || !sourceIdentity.IsDead) return false;
        sourceIdentity.StepDieForIndependentRuntime();
        ObserveNativePose();
        return true;
    }

    public bool CanApplyAuthoritativeState(WorldRemotePlayerState value)
    {
        return MatchesSpawnRoot(value) && CanApplyAuthoritativeRevisions(value);
    }

    private bool CanApplyAuthoritativeRevisions(WorldRemotePlayerState value)
    {
        if (value == null ||
            value.LifeStateRevision < state.LifeStateRevision ||
            value.PkStateRevision < state.PkStateRevision ||
            (value.PkStateRevision == state.PkStateRevision &&
             value.PkState != state.PkState) ||
            !CanApplyRidingState(value.RidingState))
        {
            return false;
        }

        bool ridingChanged = !RidingStateEquals(
            currentRidingState,
            value.RidingState);
        bool lifeChanged = value.Dead != state.Dead ||
                           value.LifeStateRevision != state.LifeStateRevision;
        if (ridingChanged && (state.Dead || lifeChanged))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Applies dynamic spawn-root revisions without replacing the actor or losing its path.
    /// </summary>
    public bool TryApplyAuthoritativeState(
        WorldRemotePlayerState value,
        float standTimeSeconds)
    {
        ThrowIfDisposed();
        if (!built || !IsFinite(standTimeSeconds) ||
            !CanApplyAuthoritativeState(value))
        {
            return false;
        }

        return ApplyAuthoritativeStateCore(value, standTimeSeconds);
    }

    /// <summary>
    /// Applies a repeated-spawn row whose root fields changed through their own official update
    /// owners, so the registered identity is kept and only the official existing-player range is
    /// restated instead of rebuilding the actor.
    /// </summary>
    public bool TryApplySpawnRootRevision(
        WorldRemotePlayerState value,
        float standTimeSeconds)
    {
        ThrowIfDisposed();
        if (!built || sourceIdentity == null || value == null ||
            !IsFinite(standTimeSeconds) || !CanApplyAuthoritativeRevisions(value))
        {
            return false;
        }

        // CombatWorld.SpawnPlayer (0x06000133) keeps an already registered id and only stores
        // faction and vitals. A root field that really changed arrives through its own edge and
        // has its own recovered owner: appearance and its effects through
        // CombatWorld.UpdatePlayerAppearance (ApplyAppearanceEvent), head and hair through
        // CombatWorld.UpdatePlayerHair (ApplyHairEvent), the name through CombatWorld.RenamePlayer
        // (ApplyRenameEvent) and invisibility through the SetInvis the Dispatch tail applies.
        // X, Y, level, gender and title have no such revision owner. Legal repeated rows retain
        // them; an unsupported row falls back to a new view, but SpawnPlayer still owns identity.
        if (value.X != state.X || value.Y != state.Y || value.Level != state.Level ||
            value.Gender != state.Gender || value.Title != state.Title)
        {
            return false;
        }

        // The owners below reach the identity through CombatWorld.I.players, the dictionary the
        // spawn publication writes. A row may only be claimed here when that lookup already
        // resolves to this identity; otherwise the caller must let the complete SpawnPlayer
        // resolve the live registry and adopt its result instead of claiming this revision.
        if (!ReferenceEquals(CombatWorld.FindPlayer(value.EntityId), sourceIdentity))
        {
            return false;
        }

        bool hairChanged = value.Head != state.Head || value.Hair != state.Hair;
        bool nameChanged = value.DisplayName != state.DisplayName;
        bool invisibleChanged = value.Invisible != state.Invisible;
        bool appearanceChanged =
            !AppearanceEquals(value.Appearance, state.Appearance) ||
            value.AvatarAura != state.AvatarAura ||
            value.SoulEffects != state.SoulEffects ||
            value.TitleEffect != state.TitleEffect ||
            value.GlowMode != state.GlowMode;
        try
        {
            if (appearanceChanged)
            {
                CharacterAppearanceRecord appearance = value.Appearance;
                CombatWorld.UpdatePlayerAppearance(
                    value.EntityId, appearance.Body, appearance.Cap, appearance.Backpack,
                    appearance.RightWeapon, appearance.LeftWeapon, appearance.Face,
                    appearance.AttackVariant, value.AvatarAura, value.SoulEffects,
                    value.TitleEffect, value.GlowMode);
                sourceIdentity.SetWeapon(appearance.WeaponCategory, appearance.AttackDistance);
            }
            if (hairChanged) CombatWorld.UpdatePlayerHair(value.EntityId, value.Head, value.Hair);
            if (nameChanged) CombatWorld.RenamePlayer(value.EntityId, value.DisplayName);
            if (invisibleChanged) sourceIdentity.SetInvis(value.Invisible);
        }
        finally
        {
            if (appearanceChanged || hairChanged) ObserveNativePose();
        }

        return ApplyAuthoritativeStateCore(value, standTimeSeconds);
    }

    private bool ApplyAuthoritativeStateCore(
        WorldRemotePlayerState value,
        float standTimeSeconds)
    {
        bool ridingChanged = !RidingStateEquals(
            currentRidingState,
            value.RidingState);
        bool lifeChanged = value.Dead != state.Dead ||
                           value.LifeStateRevision != state.LifeStateRevision;
        if (ridingChanged &&
            !TryApplyRidingState(value.RidingState, standTimeSeconds))
        {
            return false;
        }

        state = value;
        // The latest-value row preserves native identity/root fields on legal repeated spawns.
        // Execute the same complete native owner as the receive edge, then apply the separate
        // snapshot-only PK projection. No duplicate faction/HP branch is kept in the observer.
        if (!SpawnAndAdoptForIndependentRuntime(value)) return false;
        ApplySnapshotPkForIndependentRuntime(value);
        if (!lifeChanged)
        {
            return true;
        }

        if (value.Dead) sourceIdentity.PlayDie();
        else sourceIdentity.Revive();
        ObserveNativePose();
        return true;
    }

    internal void ApplyHairEvent(WorldRemotePlayerState updated)
    {
        state = updated;
        try { CombatWorld.UpdatePlayerHair(updated.EntityId, updated.Head, updated.Hair); }
        finally { ObserveNativePose(); }
    }

    internal void ApplyAppearanceEvent(WorldPlayerAppearanceState change, WorldRemotePlayerState updated)
    {
        state = updated;
        ObserveNativeVitals();
        CharacterAppearanceRecord a = change.Appearance;
        try
        {
            CombatWorld.UpdatePlayerAppearance(change.EntityId, a.Body, a.Cap, a.Backpack,
                a.RightWeapon, a.LeftWeapon, a.Face, a.AttackVariant, change.AvatarAura,
                change.SoulEffects, change.TitleEffect, change.GlowMode ?? -1);
            // Dispatch's final null-conditional is CLR null, after the wrapper's Unity guards.
            RemotePlayer player = CombatWorld.FindPlayer(change.EntityId);
            if (!ReferenceEquals(player, null)) player.SetWeapon(a.WeaponCategory, a.AttackDistance);
        }
        finally { ObserveNativePose(); }
    }

    internal void ApplyLifeEvent(bool alive, WorldRemotePlayerState updated)
    {
        state = updated;
        ObserveNativeVitals();
        CombatWorld.PlayerDeadState(updated.EntityId, !alive);
        ObserveNativePose();
    }

    internal void ApplyPkEvent(WorldRemotePlayerState updated)
    {
        state = updated;
        ObserveNativeVitals();
        // The remote arm of official case 0x8053 reaches this byte only through
        // CombatWorld.SetPlayerPkState (0x06000134). This actor used to store into its own
        // RemotePlayer reference here, which duplicated the dictionary lookup and the two Unity
        // null tests the owner makes, so the recovered owner below now performs the single write
        // that the case performs. ApplyRemoteReceiveWindow re-points CombatWorld.I at this frame's
        // world before it consumes any edge, and every id that reaches this method is registered
        // under the same key by AddRemoteReceiveActor, so the owner's lookup resolves to this
        // actor's identity; nothing else in the receive pump calls it again for one packet.
        CombatWorld.SetPlayerPkState(updated.EntityId, updated.PkState);
    }

    internal void ApplyInvisEvent(bool invisible)
    {
        // Official case 0x805d reaches this identity through CombatWorld.FindPlayer (0x0600013b)
        // and then calls RemotePlayer.SetInvis (0x06000af9) directly: there is no CombatWorld
        // wrapper, no value comparison of its own (SetInvis already short-circuits on an equal
        // value and clears hasLast) and nothing else in the case touches this actor. The row is
        // left as received - the case writes no row - so this edge never supplies a second
        // visibility cache for a later spawn row to contradict.
        sourceIdentity.SetInvis(invisible);
    }

    internal void ApplyStatusEffectEvent(WorldEntityStatusEffect effect)
    {
        // Official 0x06000158 owns the receive-time resolution and reaches RemotePlayer only
        // through its remote-player arm. AddRemoteReceiveActor registers this identity under this
        // id and ApplyRemoteReceiveWindow re-points CombatWorld.I at this frame's world before it
        // consumes any edge, so the owner's lookup resolves to this actor's identity.
        CombatWorld.ShowEntityStatus(
            state.EntityId, effect.Effect, effect.PositionMode, effect.DurationMilliseconds);
    }

    internal bool ApplyRepeatedSpawnCoreForIndependentRuntime(WorldRemotePlayerState value) =>
        SpawnAndAdoptForIndependentRuntime(value);

    internal void ApplyHpEvent(int hitPoints)
    {
        if (!sourceIdentity) return;
        sourceIdentity.SetHp(hitPoints);
        ObserveNativeVitals();
    }

    internal void ObserveCombatStateForIndependentRuntime() => ObserveNativeVitals();

    private void ObserveNativeVitals()
    {
        // Later life/appearance/riding/PK receive snapshots do not own the native HP fields.
        if (state.HitPoints != sourceIdentity.hp || state.MaximumHitPoints != sourceIdentity.maxHp)
            state = state.WithVitals(sourceIdentity.hp, sourceIdentity.maxHp);
    }

    internal void ApplySpawnDispatchTail(WorldRemotePlayerState value)
    {
        ApplyRidingEvent(value.RidingState, value);
        sourceIdentity.SetInvis(value.Invisible);
        sourceIdentity.SetWeapon(value.Appearance.WeaponCategory, value.Appearance.AttackDistance);
    }

    // Historical Editor entry retained as a forwarding seam. SpawnPlayer has already published
    // and run its own dead arm; only Dispatch's riding/invisibility/weapon tail remains here.
    internal void ApplySpawnPublicationTails(WorldRemotePlayerState value) =>
        ApplySpawnDispatchTail(value);

    internal void ApplyRidingEvent(WorldRemoteRidingState value, WorldRemotePlayerState updated)
    {
        state = updated;
        ObserveNativeVitals();
        currentRidingState = value;
        ApplyNativeRidingState(value);
        // Original StepRiding consumes this state on the next native update.
    }

    internal void ApplyRenameEvent(WorldPlayerRenameState rename, WorldRemotePlayerState updated)
    {
        CombatWorld.RenamePlayer(rename.EntityId, rename.Name);
        if (updated != null) state = updated;
        ObserveNativeVitals();
    }

    private void ApplyNativeRidingState(WorldRemoteRidingState value) =>
        sourceIdentity.SetRiding(value.Riding, value.MountModel, value.MountTianmo,
            value.RiderSeat, value.RideAnchorId, value.MountSeats);

    public bool TryApplyRidingState(WorldRemoteRidingState value, float standTimeSeconds)
    {
        ThrowIfDisposed();
        if (!built || !CanApplyRidingState(value)) return false;
        ApplyNativeRidingState(value);
        currentRidingState = value;
        return true;
    }

    public bool TryApplyVisibility(
        int nameMode,
        bool inParty,
        WorldStallOwnerState stallOwner)
    {
        return TryApplyVisibility(
            nameMode,
            inParty,
            string.Empty,
            0,
            stallOwner);
    }

    public bool TryApplyVisibility(
        int nameMode,
        bool inParty,
        string partyName,
        int partyLeaderId,
        WorldStallOwnerState stallOwner)
    {
        ThrowIfDisposed();
        if (!CanApplyVisibility() ||
            partyName == null ||
            (stallOwner != null && stallOwner.EntityId != state.EntityId))
        {
            return false;
        }

        stallOwnerState = stallOwner;
        feedbackVisibilityKnown = true;
        feedbackNameMode = nameMode;
        feedbackInParty = inParty;
        feedbackPartyName = partyName;
        feedbackPartyLeaderId = partyLeaderId;
        return true;
    }

    /// <summary>
    /// Applies one monotonic RemotePlayer.ShowChatBubble edge to the actor's root OnGUI owner.
    /// </summary>
    public bool TryApplyChatBubble(
        WorldRemoteChatBubbleState bubble,
        Camera worldCamera,
        float now)
    {
        ThrowIfDisposed();
        if (!built || feedbackAssets == null || bubble == null ||
            bubble.EntityId != state.EntityId || worldCamera == null)
        {
            return false;
        }

        if (chatBubbleView == null)
        {
            chatBubbleView = new ReferenceWorldRemoteChatBubbleView(
                feedbackAssets.ChatBubble,
                feedbackAssets.Font);
        }

        return chatBubbleView.TryShow(
            rootObject.transform,
            worldCamera,
            ResolveChatBubbleHeadHeight(),
            bubble,
            now);
    }

    public void ClearChatBubble()
    {
        ThrowIfDisposed();
        chatBubbleView?.Clear();
    }

    internal bool TryResolveHeadAnchor(out float localY, out float offX)
    {
        localY = 0f;
        offX = 0f;
        if (!built || sourceIdentity == null) return false;
        localY = sourceIdentity.HeadAnchorForIndependentRuntime(out offX);
        return true;
    }

    public bool CanApplyVisibility()
    {
        return built && dollRootObject != null && pickBox != null &&
               sourceIdentity != null;
    }

    public bool CanApplyRidingState(WorldRemoteRidingState value) =>
        value != null && value.EntityId == state.EntityId;

    public bool MatchesSpawnRoot(WorldRemotePlayerState value)
    {
        if (value == null || value.EntityId != state.EntityId)
        {
            return false;
        }

        return value.DisplayName == state.DisplayName &&
               value.X == state.X && value.Y == state.Y &&
               value.Level == state.Level && value.Gender == state.Gender &&
               value.Head == state.Head && value.Hair == state.Hair &&
               value.Title == state.Title &&
               AppearanceEquals(value.Appearance, state.Appearance) &&
               value.AvatarAura == state.AvatarAura &&
               value.SoulEffects == state.SoulEffects &&
               value.Faction == state.Faction &&
               value.TitleEffect == state.TitleEffect &&
               value.GlowMode == state.GlowMode &&
               value.Invisible == state.Invisible;
    }

    public bool TryAdvanceMount(bool moving, float deltaTime, float standTimeSeconds,
        bool hideOtherPets, Camera worldCamera)
    {
        ThrowIfDisposed();
        if (!built || sourceIdentity == null) return false;
        sourceIdentity.StepRidingForIndependentRuntime();
        ObserveNativePose();
        return true;
    }

    internal void RefreshShadowForAudit()
    {
        shadow?.RefreshForAudit();
    }

    public static int SortingBaseForMapY(float mapY)
    {
        if (!IsFinite(mapY))
        {
            return 0;
        }

        int truncated = (int)mapY;
        return Math.Max(0, Math.Min(MaximumSortingBase, truncated));
    }

    public static Color32 TintFor(bool invisible)
    {
        return invisible ? InvisibleTint : VisibleTint;
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

    bool IReferenceWorldRemoteRiderDoll.ApplyRemoteMountedRider(
        ReferenceMountRemoteComposition composition,
        float standTimeSeconds,
        Color32 tint)
    {
        if (!built || normalPoses == null || dollRootObject == null ||
            composition == null || !IsFinite(standTimeSeconds))
        {
            return false;
        }

        ReferenceWorldPlayerStandPose stand = normalPoses.StandAt(
            composition.Direction,
            standTimeSeconds);
        if (!ApplyPose(
                stand,
                composition.FlipX,
                composition.RiderSortingBase,
                tint))
        {
            return false;
        }

        dollRootObject.transform.localPosition = new Vector3(
            composition.RiderLocalPosition.x,
            composition.RiderLocalPosition.y,
            0f);
        if (!TryUpdatePickBox(
                composition.RiderLocalPosition.y,
                composition.RiderLocalPosition.x))
        {
            return false;
        }

        int octant = ReferenceWorldPlayerMotionMath.FacingOctant(
            composition.Direction,
            composition.FlipX);
        currentFacing = ReferenceWorldPlayerMotionMath.FromOctant(octant);

        return true;
    }

    private bool ApplyPose(ReferenceWorldPlayerStandPose value, bool flip, int baseSortingOrder, Color32 tint)
    {
        sourceIdentity.ApplyPoseForIndependentRuntime(value?.OriginalPose, flip, baseSortingOrder);
        ObserveNativePose();
        return true;
    }

    private void RefreshAvatarViews()
    {
        PlayerAvatar current = sourceIdentity.AvatarForIndependentRuntime;
        if (ReferenceEquals(viewedAvatar, current)) return;
        ReferenceWorldPlayerNormalPoses next;
        if (!ReferenceWorldPlayerStandPoseFactory.TryBuildNormalView(
                current,
                sourceIdentity.AppearanceForIndependentRuntime,
                out next))
        {
            next = null;
        }
        ReferenceWorldPlayerSittingPoses nextSitting;
        try { nextSitting = next?.SittingViewForIndependentRuntime(); }
        catch { next?.Dispose(); throw; }
        normalPoses?.Dispose();
        sittingPoses?.Dispose();
        normalPoses = next;
        sittingPoses = nextSitting;
        viewedAvatar = current;
    }

    private void ObserveNativePose()
    {
        RefreshAvatarViews();
        bodyRenderer = sourceIdentity.DollBodyRendererForIndependentRuntime;
        PlayerAvatar.Pose pf = sourceIdentity.LastPoseForIndependentRuntime;
        if (pf == null) currentPose = null;
        else if (normalPoses != null) currentPose = normalPoses.WrapSourceForIndependentRuntime(pf);
        // An unavailable replacement does not redraw or replace the retained native lastPf.
    }

    private bool TryUpdatePickBox(float raise, float saddleX)
    {
        if (sourceIdentity == null) return false;
        sourceIdentity.UpdatePickBoxForIndependentRuntime(raise, saddleX);
        return true;
    }

    private bool TryAdvancePathPosition(float deltaTime)
    {
        if (sourceIdentity == null) return false;
        sourceIdentity.AdvancePathForIndependentRuntime();
        return true;
    }

    private bool TryAdvanceAction(float deltaTime, float standTimeSeconds)
    {
        sourceIdentity.StepActForIndependentRuntime();
        ObserveNativePose();
        return true;
    }

    private bool ApplyOrdinaryWalk(float deltaTime)
    {
        sourceIdentity.StepWalkForIndependentRuntime();
        ObserveNativePose();
        return true;
    }

    private bool TryUpdateRemoteFeedback(Camera worldCamera)
    {
        if (feedbackAssets == null)
        {
            return true;
        }

        if (chatBubbleView != null &&
            !chatBubbleView.TryTick(
                rootObject.transform,
                worldCamera,
                ResolveChatBubbleHeadHeight()))
        {
            return false;
        }

        return true;
    }

    private float ResolveChatBubbleHeadHeight()
    {
        int pixels = normalPoses == null ? 0 : normalPoses.HeadPixels;
        return pixels < 1
            ? ReferenceWorldRemoteChatBubbleView.DefaultHeadHeight
            : pixels / ReferenceWorldLocalPlayerDoll.PixelsPerUnit +
              ReferenceWorldRemoteChatBubbleView.HeadPaddingWorld;
    }

    private void ReleaseOwnedState()
    {
        // This actor borrows the native root. CombatWorld.Remove/ClearAll (or the owning
        // instance's inactive-frame retirement) raise RemotePlayer.OnDestroy; view disposal
        // never destroys or unregisters that identity, including when native spawn or later
        // view admission throws.
        nameTagView?.Dispose();
        nameTagView = null;
        headHpBarView?.Dispose();
        headHpBarView = null;
        stallSignView?.Dispose();
        stallSignView = null;
        chatBubbleView?.Dispose();
        chatBubbleView = null;
        rootObject = null;
        sourceIdentity = null;
        dollRootObject = null;
        mainRenderer = null;
        bodyRenderer = null;
        pickBox = null;
        shadow = null;
        layerRenderers.Clear();

        normalPoses?.Dispose();
        normalPoses = null;
        viewedAvatar = null;
        sittingPoses?.Dispose();
        sittingPoses = null;
        currentPose = null;

        currentFacing = ReferenceWorldPlayerMotionMath.FromOctant(
            ReferenceWorldPlayerMotionMath.DefaultFacingOctant);
        currentRidingState = state.RidingState;
        hidden = false;
        stallOwnerState = null;
        feedbackVisibilityKnown = false;
        feedbackNameMode = 0;
        feedbackInParty = false;
        feedbackPartyName = string.Empty;
        feedbackPartyLeaderId = 0;
        mountedArtAllowedByNameMode = true;
        actionKind = default(WorldRemoteActionKind);
        actionVariant = 0;
        lastMovementActionRevision = 0;
        built = false;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceWorldRemotePlayerActor));
        }
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool RidingStateEquals(
        WorldRemoteRidingState left,
        WorldRemoteRidingState right)
    {
        return left != null && right != null &&
               left.EntityId == right.EntityId && left.Riding == right.Riding &&
               left.MountModel == right.MountModel &&
               left.MountTianmo == right.MountTianmo &&
               left.RiderSeat == right.RiderSeat &&
               left.RideAnchorId == right.RideAnchorId &&
               left.MountSeats == right.MountSeats;
    }

    private static bool AppearanceEquals(
        CharacterAppearanceRecord left,
        CharacterAppearanceRecord right)
    {
        return left != null && right != null &&
               left.Body == right.Body && left.Cap == right.Cap &&
               left.Backpack == right.Backpack &&
               left.RightWeapon == right.RightWeapon &&
               left.LeftWeapon == right.LeftWeapon && left.Face == right.Face &&
               left.AttackVariant == right.AttackVariant &&
               left.WeaponCategory == right.WeaponCategory &&
               left.AttackDistance == right.AttackDistance;
    }


}

}
