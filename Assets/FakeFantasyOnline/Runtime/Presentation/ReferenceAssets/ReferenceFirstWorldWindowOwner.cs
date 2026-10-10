using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using UnityEngine;
using NativePlayerActionMenu = FakeFantasyOnline.Presentation.ReferenceAssets.PlayerActionMenu;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

internal enum ReferenceFirstWorldPointerWindow
{
    None = 0,
    ChatOverlay = 1,
    SystemMessageBox = 2,
    ReviveChoice = 3,
    NpcDialog = 4,
    PlayerActionMenu = 5,
    DealDialog = 6,
    InventoryDialog = 7,
    LocalEquipmentDialog = 8,
    InspectedEquipmentDialog = 9,
    SkillDialog = 10,
    GossipDialog = 11,
    LocalMapDiagram = 12,
    ChatUi = 13,
    TeamConfig = 14,
    DuelDialog = 15,
    MailDialog = 16,
    RackShopDialog = 17,
    NativeDialogUI = 18,
}

/// <summary>
/// Owns the reviewed first-world source graph inside the actual client window.
/// </summary>
/// <remarks>
/// This is the runtime bridge from the server-authored entry snapshot to
/// <see cref="ReferenceFirstWorldInitialFrame"/>. It deliberately remains visibly diagnostic
/// until the complete world-object and HUD cones are recovered; constructing this owner is not
/// product acceptance.
/// </remarks>
public sealed partial class ReferenceFirstWorldWindowOwner : IDisposable
{
    /// <summary>
    /// The committed byte-exact copy of the official <c>Effect/circle</c> texture, extracted from
    /// the official Resources payload. The official member reads it through Resources in the
    /// official client; this checkout keeps the same pixels at this reviewed path, so it stays the
    /// only sprite bound into <see cref="PlayerController.EmitRing"/>.
    /// </summary>
    internal const string ClickRingTexturePath = "unity/effect/circle.png";

    private readonly ClientSessionCoordinator session;
    private readonly Transform parent;
    private WorldEntryRecord character;
    private readonly MusicManager sharedMusic;
    private readonly ReferenceWorldCombatSound sharedCombatSound;
    private readonly System.Collections.Generic.ISet<string> processNpcNames;
    private readonly GameBootstrap gameBootstrap;

    private ReferenceResourceLoader resources;
    private MapDef.Entry mapDescriptor;
    private System.Collections.Generic.List<MapLink.MapProc> mapLinks;
    private ReferenceWorldCollisionMask collisionMask;
    private GameObject worldRootObject;
    private Camera worldCamera;
    private ReferenceFirstWorldInitialFrame frame;
    private ReferenceFirstWorldStaticComposite preparedStaticComposite;
    private Texture2D preparedCoverTexture;
    private MusicManager mapMusic;
    private ReferenceWorldCombatSound combatSound;
    private ReferenceWorldNameModeController nameModeController;
    private GameHud nativeHud;
    private bool borrowedNativeHudViews;
    private bool nativeHudViewsBound;
    private ReferenceFirstWorldHudResources hudResources;
    private ReferenceFirstWorldPlayerStatusHud playerStatusHud;
    private ReferenceFirstWorldTargetHud targetHud;
    private ReferenceFirstWorldBuffHud buffHud;
    private ReferenceFirstWorldMinimapHud minimapHud;
    private ReferenceFirstWorldControlBar controlBar;
    private ReferenceFirstWorldHotbar hotbar;
    private Win05 skillDialog;
    private ReferenceFirstWorldPlayerActionMenu playerActionMenu;
    private ReferenceFirstWorldTeamConfigDialog teamConfigDialog;
    private ReferenceFirstWorldTeamDialog teamDialog;
    private ReferenceFirstWorldDuelDialog duelDialog;
    private bool ownsDuelDialog;
    private ReferenceFirstWorldMailDialog mailDialog;
    private bool ownsMailDialog;
    private ReferenceFirstWorldRackShopDialog rackShopDialog;
    private ReferenceFirstWorldChatUi chatUi;
    private ReferenceFirstWorldGossipDialog gossipDialog;
    private ReferenceFirstWorldNpcDialog npcDialog;
    private ReferenceTransportUI transportDialog;
    private ReferenceStoreDlg storeDialog;
    private PlayerTaskTrack playerTaskTrack;
    private ReferenceFirstWorldInspectedEquipmentDialog inspectedEquipmentDialog;
    private ReferenceFirstWorldUiTip uiTip;
    private ReferenceFirstWorldSystemInfoBar systemInfoBar;
    private ReferenceReviveDialogBinding reviveChoiceDialog;
    private ReferenceGameHudItemCooldowns itemCooldowns;
    private ReferenceGameHudSkillCooldowns skillCooldowns;
    private CursorManager cursorManager;
    private ReferenceFirstWorldInventoryDialog inventoryDialog;
    private ReferenceFirstWorldLocalEquipmentDialog localEquipmentDialog;
    private ReferenceFirstWorldDealDialog dealDialog;
    private ReferenceWorldDealingMarks dealingMarks;
    private Sprite ownedClickRingSprite;
    private CancellationTokenSource chatLifetime;
    private CancellationTokenSource inspectLifetime;
    private CancellationTokenSource tradeRequestLifetime;
    private CancellationTokenSource systemRequestLifetime;
    private CancellationTokenSource inventoryLifetime;
    private CancellationTokenSource hotbarLifetime;
    private CancellationTokenSource npcTaskLifetime;
    private bool npcTaskActionReady = true;
    private CancellationTokenSource npcFunctionLifetime;
    private CancellationTokenSource basicAttackLifetime;
    private CancellationTokenSource groundPickupLifetime;
    private Task<Result> pendingInspectRequest;
    private Task<Result> pendingTradeRequest;
    private readonly System.Collections.Generic.List<Task<Result>> pendingChatSends =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingSystemRequestResponses =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingInventoryCommands =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingHotbarCommands =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingNpcTaskRequests =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingNpcFunctionRequests =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.Dictionary<
        Task<Result>,
        ReferenceNpcTaskMutationKind> pendingNpcTaskMutationKinds =
            new System.Collections.Generic.Dictionary<
                Task<Result>,
                ReferenceNpcTaskMutationKind>();
    private readonly System.Collections.Generic.HashSet<Task<Result>>
        pendingNpcTaskAbandonRequests =
            new System.Collections.Generic.HashSet<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingBasicAttackRequests =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingReviveChoiceRequests =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingGroundPickupRequests =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly System.Collections.Generic.List<Task<Result>> pendingPositionReports =
        new System.Collections.Generic.List<Task<Result>>();
    private readonly ReferenceWorldRightClickTracker rightClickTracker =
        new ReferenceWorldRightClickTracker();
    private Event routedPointerEvent;
    private ReferenceFirstWorldPointerWindow routedPointerWindow;
    private ReferenceFirstWorldPointerWindow capturedPointerWindow;
    private bool heldSkillReleaseBlockedByWindow;

    private readonly ReferenceWorldManualMovementController manualMovementController =
        new ReferenceWorldManualMovementController();
    private long playerStatusVersion;
    private long playerAppearanceStateVersion = long.MinValue;
    private long selfPkStateVersion;
    private long buffStateVersion = long.MinValue;
    private long inventoryStateVersion = long.MinValue;
    private long wardrobeStateVersion = long.MinValue;
    private int changeSetStateVersion = int.MinValue;
    private long hotbarStateVersion = long.MinValue;
    private long skillBookStateVersion = long.MinValue;
    private long lifeSkillBookStateVersion = long.MinValue;
    private long skillHitEffectsVersion = long.MinValue;
    private long skillHitEffectCursor;
    private long localPlayerStatusEffectsVersion = long.MinValue;
    private long activeTradeStateVersion = long.MinValue;
    private long transportStateVersion;
    private long warehouseStateVersion;
    private long familyStashStateVersion = long.MinValue;
    private long helpTopicsStateVersion = long.MinValue;
    private long friendStateVersion = long.MinValue;
    private long apprenticeStateVersion = long.MinValue;
    private long blockStateVersion = long.MinValue;
    private long partyStateVersion = long.MinValue;
    private long taskLogStateVersion;
    private long npcDialogTextEdgeWindowVersion;
    private long npcDialogTextEdgeCursor;
#if UNITY_EDITOR
    private readonly System.Collections.Generic.List<WorldNpcDialogTextEdge>
        pendingNewbieTipEdges =
            new System.Collections.Generic.List<WorldNpcDialogTextEdge>();
#endif
    private long localPlayerLifeStateVersion = long.MinValue;
    // Live request ids share the native StopAttack storage; the remaining request method
    // recovery retains its existing coordinate/range/deadline adapters below.
    private int pendingGroundSkillId
    {
        get => frame?.CombatOwnerForIndependentRuntime?.PendingGroundSkillForIndependentRuntime ?? 0;
        set
        {
            CombatWorld owner = frame?.CombatOwnerForIndependentRuntime;
            if (owner != null) owner.PendingGroundSkillForIndependentRuntime = value;
        }
    }
    private int pendingTargetSkillId
    {
        get => frame?.CombatOwnerForIndependentRuntime?.PendingSkillForIndependentRuntime ?? 0;
        set
        {
            CombatWorld owner = frame?.CombatOwnerForIndependentRuntime;
            if (owner != null) owner.PendingSkillForIndependentRuntime = value;
        }
    }
    private ReferenceWorldRevisionFailure lastWorldRevisionFailure;
    private long worldRevisionRejectionCount;
    private long basicAttackRequestIssuedCount;
    private long basicAttackRequestSucceededCount;
    private long groundPickupRequestIssuedCount;
    private long groundPickupRequestSucceededCount;
    private long npcTaskAcceptRequestIssuedCount;
    private long npcTaskAcceptRequestSucceededCount;
    private long npcTaskAbandonRequestIssuedCount;
    private long npcTaskAbandonRequestSucceededCount;
    private long npcTaskCompleteRequestIssuedCount;
    private long npcTaskCompleteRequestSucceededCount;
    private int lastGroundPickupRequestedIdForGroundLoot;
    private bool usingEntryStatusFallback;
    private bool built;
    private bool disposed;
    // Release handle for the UiEsc.CloseTop (0x06000fca) callable this owner publishes on
    // ReferenceFirstWorldEscapeDispatch while its world is built; invoked once on dispose.
    private Action releaseEscapeCloseTopBinding;

    public ReferenceFirstWorldWindowOwner(
        ClientSessionCoordinator session,
        Transform parent,
        WorldEntryRecord character)
        : this(session, parent, character, null, null, null, null)
    {
    }

    public ReferenceFirstWorldWindowOwner(
        ClientSessionCoordinator session,
        Transform parent,
        WorldEntryRecord character,
        MusicManager sharedMusic)
        : this(session, parent, character, sharedMusic, null, null, null)
    {
    }

    public ReferenceFirstWorldWindowOwner(
        ClientSessionCoordinator session,
        Transform parent,
        WorldEntryRecord character,
        MusicManager sharedMusic,
        ReferenceWorldCombatSound sharedCombatSound)
        : this(session, parent, character, sharedMusic, sharedCombatSound, null, null)
    {
    }

    public ReferenceFirstWorldWindowOwner(
        ClientSessionCoordinator session,
        Transform parent,
        WorldEntryRecord character,
        MusicManager sharedMusic,
        ReferenceWorldCombatSound sharedCombatSound,
        System.Collections.Generic.ISet<string> processNpcNames)
        : this(
            session,
            parent,
            character,
            sharedMusic,
            sharedCombatSound,
            processNpcNames,
            null)
    {
    }

    public ReferenceFirstWorldWindowOwner(
        ClientSessionCoordinator session,
        Transform parent,
        WorldEntryRecord character,
        MusicManager sharedMusic,
        ReferenceWorldCombatSound sharedCombatSound,
        System.Collections.Generic.ISet<string> processNpcNames,
        GameBootstrap gameBootstrap,
        ReferenceFirstWorldGossipDialog preservedGossip = null,
        ReferenceFirstWorldDealDialog preservedDeal = null)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        this.parent = parent == null
            ? throw new ArgumentNullException(nameof(parent))
            : parent;
        this.character = character ?? throw new ArgumentNullException(nameof(character));
        this.sharedMusic = sharedMusic;
        this.sharedCombatSound = sharedCombatSound;
        this.processNpcNames = processNpcNames ??
            new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
        this.gameBootstrap = gameBootstrap;
        dealDialog = preservedDeal;
        gossipDialog = preservedGossip;
        if (gossipDialog != null && !gossipDialog.Ready)
            gossipDialog = null;


    }

    public bool Built => built;

    public ReferenceFirstWorldWindowFailure LastFailure { get; private set; }

    /// <summary>
    /// The most recent optional world-revision branch that could not be projected exactly.
    /// </summary>
    /// <remarks>
    /// A rejected post-entry revision is diagnostic and keeps the last constructed world graph
    /// alive. It is still a failed acceptance condition and is surfaced to the background gate.
    /// </remarks>
    public ReferenceWorldRevisionFailure LastWorldRevisionFailure =>
        lastWorldRevisionFailure;

    public long WorldRevisionRejectionCount => worldRevisionRejectionCount;

    internal bool ApplyPublishedWorldEventsForIndependentRuntime()
    {
        if (disposed || !built || frame == null || nameModeController == null) return false;
        // The receive pump calls this after each synchronous packet, before the next one.
        WorldInventoryState inventoryPublication = session.InventoryState ?? WorldInventoryState.Empty;
        InventoryDlg.ObservePublicationForIndependentRuntime(inventoryPublication);
        EquipDlg.ObservePublicationForIndependentRuntime(inventoryPublication);
        if (!frame.ApplyPublishedWorldEventsForIndependentRuntime(session, nameModeController.Mode))
        {
            lastWorldRevisionFailure = frame.LastRevisionFailure;
            worldRevisionRejectionCount++;
            return false;
        }
        return true;
    }

    public long TickCount { get; private set; }

    public long BuildGeneration { get; private set; }

    public int NameMode => nameModeController == null ? 0 : nameModeController.Mode;

    public string LastNameModeFeedback { get; private set; } = string.Empty;

    public string LastFollowFeedback { get; private set; } = string.Empty;

    internal long BasicAttackRequestIssuedCountForRealCombatTail =>
        basicAttackRequestIssuedCount;

    internal long BasicAttackRequestSucceededCountForRealCombatTail =>
        basicAttackRequestSucceededCount;

    internal int LocalEntityIdForRealCombatTail => character.EntityId;

    internal long GroundPickupRequestIssuedCountForGroundLoot =>
        groundPickupRequestIssuedCount;

    internal long GroundPickupRequestSucceededCountForGroundLoot =>
        groundPickupRequestSucceededCount;

    internal int LastGroundPickupRequestedIdForGroundLoot =>
        lastGroundPickupRequestedIdForGroundLoot;

    internal long NpcTaskAcceptRequestIssuedCountForLifecycle =>
        npcTaskAcceptRequestIssuedCount;

    internal long NpcTaskAcceptRequestSucceededCountForLifecycle =>
        npcTaskAcceptRequestSucceededCount;

    internal long NpcTaskAbandonRequestIssuedCountForLifecycle =>
        npcTaskAbandonRequestIssuedCount;

    internal long NpcTaskAbandonRequestSucceededCountForLifecycle =>
        npcTaskAbandonRequestSucceededCount;

    internal long NpcTaskCompleteRequestIssuedCountForLifecycle =>
        npcTaskCompleteRequestIssuedCount;

    internal long NpcTaskCompleteRequestSucceededCountForLifecycle =>
        npcTaskCompleteRequestSucceededCount;

    internal long NpcDialogTextEdgeCursorForLifecycle =>
        npcDialogTextEdgeCursor;

    internal long NpcDialogTextEdgeWindowVersionForLifecycle =>
        npcDialogTextEdgeWindowVersion;

#if UNITY_EDITOR
    internal System.Collections.Generic.IReadOnlyList<WorldNpcDialogTextEdge>
        PendingNewbieTipEdgesForLifecycle => pendingNewbieTipEdges;
#endif

    public ReferenceFirstWorldInitialFrame Frame => frame;

    internal void SetBuildGenerationForIndependentRuntime(long generation)
    {
        if (generation < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(generation));
        }

        BuildGeneration = generation;
    }

    /// <summary>
    /// Cancels every production action the fixed task journey can leave pending and waits for
    /// their already-issued tasks to settle within one bounded terminal budget.
    /// </summary>
    internal async Task<bool> CancelAndAwaitNpcTaskLifecycleActionsAsync(
        TimeSpan timeout)
    {
        if (!(timeout > TimeSpan.Zero))
        {
            throw new ArgumentOutOfRangeException(nameof(timeout));
        }

        npcTaskLifetime?.Cancel();
        basicAttackLifetime?.Cancel();
        groundPickupLifetime?.Cancel();
        gameBootstrap?.CancelPositionReportsForIndependentRuntime();
        frame?.ClearTargetSelection();
        frame?.Player?.StopMovementForCombat();
        CombatWorld.StopAttack();

        System.Collections.Generic.List<Task> pending =
            new System.Collections.Generic.List<Task>(
                pendingNpcTaskRequests.Count +
                pendingBasicAttackRequests.Count +
                pendingGroundPickupRequests.Count +
                pendingPositionReports.Count);
        AddPendingTasks(pending, pendingNpcTaskRequests);
        AddPendingTasks(pending, pendingBasicAttackRequests);
        AddPendingTasks(pending, pendingGroundPickupRequests);
        AddPendingTasks(pending, pendingPositionReports);
        if (pending.Count == 0)
        {
            return true;
        }

        Task settlement = Task.WhenAll(pending.ToArray());
        Task deadline = Task.Delay(timeout);
        Task completed = await Task.WhenAny(settlement, deadline);
        if (!ReferenceEquals(completed, settlement))
        {
            return false;
        }

        try
        {
            await settlement;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception)
        {
            // A faulted already-issued action is settled. Its ordinary observer remains the
            // authority for the action result; terminal lifetime only requires no continuation.
        }
        return true;
    }

    private static void AddPendingTasks(
        System.Collections.Generic.ICollection<Task> destination,
        System.Collections.Generic.IReadOnlyList<Task<Result>> source)
    {
        for (int index = 0; index < source.Count; index++)
        {
            Task<Result> task = source[index];
            if (task != null && !task.IsCompleted)
            {
                destination.Add(task);
            }
        }
    }

    public MapDef.Entry MapDescriptor => mapDescriptor;

    public ReferenceWorldCollisionMask CollisionMask => collisionMask;

    public System.Collections.Generic.IReadOnlyList<MapLink.MapProc> MapLinks =>
        mapLinks ?? (System.Collections.Generic.IReadOnlyList<MapLink.MapProc>)
            Array.Empty<MapLink.MapProc>();

    public MusicManager MapMusic => mapMusic;

    public ReferenceWorldCombatSound CombatSound => combatSound;

    public ReferenceFirstWorldPlayerStatusHud PlayerStatusHud => playerStatusHud;

    public ReferenceFirstWorldTargetHud TargetHud => targetHud;

    public ReferenceFirstWorldBuffHud BuffHud => buffHud;

    public ReferenceFirstWorldMinimapHud MinimapHud => minimapHud;

    public ReferenceFirstWorldControlBar ControlBar => controlBar;

    public ReferenceFirstWorldHotbar Hotbar => hotbar;

    public Win05 SkillDialog => skillDialog;

    public ReferenceFirstWorldPlayerActionMenu PlayerActionMenu => playerActionMenu;

    public ReferenceFirstWorldChatUi ChatUi => chatUi;

    public ReferenceFirstWorldGossipDialog GossipDialog => gossipDialog;

    public ReferenceFirstWorldNpcDialog NpcDialog => npcDialog;

    public FamilyStashDlg FamilyStashDialog => FamilyStashDlg.I;
    public ReferenceFirstWorldTeamDialog TeamDialog => teamDialog;
    public ReferenceFirstWorldDuelDialog DuelDialog => duelDialog;
    public ReferenceFirstWorldMailDialog MailDialog => mailDialog;

    public bool DuelOpenRequested { get; private set; }
    public int DuelTargetEntityId { get; private set; }
    public string DuelTargetName { get; private set; } = string.Empty;


    public ReferenceTransportUI TransportDialog => transportDialog;

    public ReferenceStoreDlg StoreDialog => storeDialog;

    internal PlayerTaskTrack PlayerTaskTrack => playerTaskTrack;

    public ReferenceFirstWorldInspectedEquipmentDialog InspectedEquipmentDialog =>
        inspectedEquipmentDialog;

    public string LastChatSendFailureCode { get; private set; } = string.Empty;

    public string LastInspectFailureCode { get; private set; } = string.Empty;

    public string LastTradeRequestFailureCode { get; private set; } = string.Empty;

    public string LastSystemRequestResponseFailureCode { get; private set; } = string.Empty;

    public string LastInventoryCommandFailureCode { get; private set; } = string.Empty;

    public string LastHotbarCommandFailureCode { get; private set; } = string.Empty;

    public string LastLocalSkillFeedbackWarning { get; private set; } = string.Empty;

    public string LastNpcTaskFailureCode { get; private set; } = string.Empty;

    public string LastNpcFunctionFailureCode { get; private set; } = string.Empty;

    public string LastBasicAttackFailureCode { get; private set; } = string.Empty;

    public string LastReviveChoiceFailureCode { get; private set; } = string.Empty;

    public string LastGroundPickupFailureCode { get; private set; } = string.Empty;

    public string LastPositionReportFailureCode { get; private set; } = string.Empty;

    public int PendingGroundPickupId => frame?.Player?.SourceControllerForIndependentRuntime?.PendingPickupIdForIndependentRuntime ?? -1;

    public ReferenceFirstWorldUiTip UiTip => uiTip;

    public ReferenceFirstWorldSystemInfoBar SystemInfoBar => systemInfoBar;

    public ReferenceReviveDialogBinding ReviveChoiceDialog =>
        reviveChoiceDialog;

    public ReferenceFirstWorldInventoryDialog InventoryDialog => inventoryDialog;

    public ReferenceFirstWorldLocalEquipmentDialog LocalEquipmentDialog =>
        localEquipmentDialog;

    public ReferenceGameHudItemCooldowns ItemCooldowns => itemCooldowns;

    public ReferenceGameHudSkillCooldowns SkillCooldowns => skillCooldowns;

    internal SkillTargeting SkillTargetingForAudit => SkillTargeting.InstanceForIndependentRuntime;

    internal bool TryResolveSkillTargetWorldPixelForAudit(
        int entityId,
        out float x,
        out float y)
    {
        return TryResolveSkillTargetWorldPixel(entityId, out x, out y);
    }

#if UNITY_EDITOR
    // The historical 1.3.4 weaken audit has no live 1.5.8 consumer.
    internal ReferenceWorldLocalStatusEffect LocalStatusEffect => null;
#endif

    public ReferenceFirstWorldDealDialog DealDialog => dealDialog;

    public ReferenceWorldDealingMarks DealingMarks => dealingMarks;

    public ReferenceFirstWorldControlBarAction LastControlBarAction { get; private set; }

    public ReferenceFirstWorldEscapeAction LastEscapeAction { get; private set; }

    public bool LastWorldMapIntent { get; private set; }

    public ReferenceFirstWorldTargetHudAction LastTargetHudAction { get; private set; }

    public ReferencePlayerActionMenuAction LastPlayerActionMenuAction =>
        NativePlayerActionMenu.I != null
            ? (ReferencePlayerActionMenuAction)NativePlayerActionMenu.I.LastActionForIndependentRuntime
            : ReferencePlayerActionMenuAction.None;

    public bool TeamConfigOpenRequested { get; private set; }
    public bool TeamDialogOpenRequested { get; private set; }
    public int TeamDialogTab { get; private set; } = 1;

    public string TeamConfigDefaultName { get; private set; } = string.Empty;

    public int TeamConfigInviteTargetId { get; private set; }

    public bool TryConsumeTeamConfigOpenIntent(out string defaultName, out int inviteTargetId)
    {
        defaultName = TeamConfigDefaultName;
        inviteTargetId = TeamConfigInviteTargetId;
        if (!TeamConfigOpenRequested)
        {
            defaultName = string.Empty;
            inviteTargetId = 0;
            return false;
        }
        TeamConfigOpenRequested = false;
        TeamConfigDefaultName = string.Empty;
        TeamConfigInviteTargetId = 0;
        return true;
    }

    public ReferenceFirstWorldPointerAction LastPointerAction { get; private set; }

    internal string LastPointerPickedTargetKind { get; private set; } = "None";

    internal int LastPointerPickedTargetId { get; private set; }

    internal Vector3 LastPointerScreenPosition { get; private set; }

    internal Vector3 LastPointerWorldPosition { get; private set; }

    internal Vector2 LastPointerMapPosition { get; private set; }

    internal bool LastPointerOverHud { get; private set; }

    public Camera Camera => worldCamera;

    public Transform WorldRoot => worldRootObject == null ? null : worldRootObject.transform;

    internal GameBootstrap.BuildWorldCheckpoints BuildCheckpointsForIndependentRuntime { get; set; }

    private bool bootstrapOwnsConstructionRoot;
    private bool transitionOwnsConstructionFailure;

    internal void AdoptConstructionRootForIndependentRuntime(Transform root, bool transitionOwnsFailure)
    {
        bootstrapOwnsConstructionRoot = true;
        transitionOwnsConstructionFailure = transitionOwnsFailure;
        worldRootObject = root.gameObject;
    }

    public bool BuildForViewport(int viewportWidth, int viewportHeight, float dpiScale)
    {
        ThrowIfDisposed();
        if (built)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.AlreadyBuilt;
            return false;
        }

        // BuildWorld constructs before CameraFollow observes the live viewport. Raw
        // dimensions do not reject this prefix or force a synthetic camera refresh.
        if (!IsPositiveFinite(dpiScale))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidViewport;
            return false;
        }

        try
        {
            // Construct command lifetimes before any window can capture their tokens.
            chatLifetime = new CancellationTokenSource();
            inspectLifetime = new CancellationTokenSource();
            tradeRequestLifetime = new CancellationTokenSource();
            systemRequestLifetime = new CancellationTokenSource();
            inventoryLifetime = new CancellationTokenSource();
            hotbarLifetime = new CancellationTokenSource();
            npcTaskLifetime = new CancellationTokenSource();
            npcFunctionLifetime = new CancellationTokenSource();
            basicAttackLifetime = new CancellationTokenSource();
            groundPickupLifetime = new CancellationTokenSource();
            resources = ReferenceResourceLoader.OpenStreamingAssets();
            MapDef.EnsureLoaded(resources);
            if (!MapDef.TryGet(character.MapId, out mapDescriptor))
            {
                mapDescriptor = new MapDef.Entry
                {
                    id = character.MapId,
                    file = "qw" + character.MapId.ToString("D4"),
                    name = string.Empty,
                    isCity = false
                };
            }

            // BuildWorld reads its own mapName. In particular, a nonpositive entry ID
            // leaves the captured boot's existing name intact; logical ID is separate.
            mapDescriptor.file = ReferenceEquals(gameBootstrap, null)
                ? GameBootstrap.MapSourceName(character.MapId)
                : gameBootstrap.mapName;
            byte[] mapBytes = resources.Core.ReadMap("map/" + mapDescriptor.file + ".gsm");
            // BuildWorld consumes the actual returned owner, including an ordinary
            // partial parse. A parser exception follows the existing owner handler.
            GsmMap sourceMap = GsmMap.Parse(mapBytes);
            if (BuildCheckpointsForIndependentRuntime != null)
                BuildCheckpointsForIndependentRuntime.GsmParsedMs =
                    BuildCheckpointsForIndependentRuntime.Clock.ElapsedMilliseconds;
            ReferenceGsmMap map;
            if (!sourceMap.ok && sourceMap.hasCover)
                preparedCoverTexture = CoverFloor.Build(sourceMap);
            if (!sourceMap.ok && (!sourceMap.hasCover || preparedCoverTexture == null))
            {
                // A cover block may produce no bitmap. The same native resource fallback
                // then applies, while a cover-bearing map keeps its own scene dimensions.
                Texture2D background = Resources.Load<Texture2D>(gameBootstrap == null
                    ? mapDescriptor.file : gameBootstrap.mapName);
                if (background == null)
                {
                    LastFailure = ReferenceFirstWorldWindowFailure.MissingSourceAsset;
                    // The original void builder returns normally with its published root.
                    // Its existing owner keeps this private candidate until later release.
                    if (!bootstrapOwnsConstructionRoot) ReleaseOwnedState();
                    return false;
                }
                map = new ReferenceGsmMap(sourceMap, background,
                    sourceMap.hasCover ? sourceMap.mapWidth : background.width,
                    sourceMap.hasCover ? sourceMap.mapHeight : background.height);
            }
            else
            {
                map = new ReferenceGsmMap(sourceMap);
            }

            // Native BuildWorld writes the shader globals, then publishes the map name
            // and dimensions before camera/floor/player construction can fail.
            ReferenceWorldRenderSettings.ConfigureIsoYGlobals(
                map.MapHeight, ReferenceFirstWorldStaticComposite.PixelsPerUnit);
            if (!ReferenceEquals(gameBootstrap, null))
            {
                GameBootstrap.CurMapName = gameBootstrap.mapName;
                GameBootstrap.CurWorldW = map.MapWidth;
                GameBootstrap.CurWorldH = map.MapHeight;
                GameBootstrap.CurPpu = gameBootstrap.pixelsPerUnit;
            }

            ReferenceTileImageLibrary tiles = null;
            ReferenceTerrainShapeLibrary shapes = null;
            // FloorRenderer.Init loads the original tile libraries from the primary package
            // with their native Pkg24 fallback and retained-state rules.

            // The map root owns its floor and actors. GameHud is created independently
            // at the player-publication callback below, before mask/transfer reads.
            if (!bootstrapOwnsConstructionRoot)
            {
                worldRootObject = new GameObject("RecoveredFirstWorldRoot");
                worldRootObject.transform.SetParent(parent, false);
            }
            // Original BuildWorld borrows an existing camera. The true branch reads
            // Camera.main again; no match still admits the remaining world construction.
            Camera camera = UnityEngine.Camera.main != null
                ? UnityEngine.Camera.main
                : UnityEngine.Object.FindAnyObjectByType<Camera>();
            worldCamera = camera;
            preparedStaticComposite = new ReferenceFirstWorldStaticComposite(
                map, tiles, shapes, resources, worldRootObject.transform, camera, gameBootstrap);
            if (preparedCoverTexture != null)
            {
                preparedStaticComposite.AdoptCoverTextureForIndependentRuntime(preparedCoverTexture);
                preparedCoverTexture = null;
            }
            preparedStaticComposite.BuildCheckpointsForIndependentRuntime = BuildCheckpointsForIndependentRuntime;
            if (!preparedStaticComposite.BuildFloorForIndependentRuntime(globalsAlreadyConfigured: true))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InitialFrameRejected;
                ReleaseOwnedState();
                return false;
            }

            if (!ReferenceFirstWorldPlayerActionMenu.TryCreate(
                    resources,
                    out playerActionMenu) ||
                playerActionMenu == null || !playerActionMenu.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerActionMenu;
                ReleaseOwnedState();
                return false;
            }

            teamConfigDialog = new ReferenceFirstWorldTeamConfigDialog(
                resources,
                session.PartyCommands,
                BuildNearbyPartyTargets);
            if (!teamConfigDialog.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerActionMenu;
                ReleaseOwnedState();
                return false;
            }
            teamDialog = new ReferenceFirstWorldTeamDialog(new TeamDialogRuntime
            {
                PartyCommands = session.PartyCommands,
                FriendCommands = session.FriendCommands,
                BlockCommands = session.BlockCommands,
                ApprenticeCommands = session.ApprenticeCommands,
                ToggleTeamConfig = TeamConfigDlg.Toggle,
                OpenTeamConfig = TeamConfigDlg.Open,
                OpenGossip = GossipDlg.Open,
                LocalName = () => CharData.name,
                SelectPlayer = CombatWorld.SelectPlayer
            });
            TeamBar.Ensure();
            ownsDuelDialog = gameBootstrap == null;
            duelDialog = ownsDuelDialog
                ? new ReferenceFirstWorldDuelDialog(session.DuelCommands, session.DuelStateSource)
                : gameBootstrap.AcquireDuelForIndependentRuntime(session);
            ownsMailDialog = gameBootstrap == null;
            mailDialog = ownsMailDialog
                ? new ReferenceFirstWorldMailDialog(session.MailCommands, session.MailStateSource)
                : gameBootstrap.AcquireMailForIndependentRuntime(session);
            mailDialog.RebindWindow(
                () => inventoryDialog?.HeldItem, () => inventoryDialog?.ClearHeld(),
                () => session.InventoryState);

            rackShopDialog = gameBootstrap != null
                ? gameBootstrap.BorrowRackShopForIndependentRuntime()
                : new ReferenceFirstWorldRackShopDialog();

            FamilyDlg.BindGuiDriverForIndependentRuntime(this);

            if (!ReferenceFirstWorldChatUi.TryCreate(resources, out chatUi) ||
                chatUi == null || !chatUi.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidChatUi;
                ReleaseOwnedState();
                return false;
            }

            if (gossipDialog == null &&
                (!ReferenceFirstWorldGossipDialog.TryCreate(resources, out gossipDialog) ||
                 gossipDialog == null || !gossipDialog.Ready))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidGossipDialog;
                ReleaseOwnedState();
                return false;
            }

            if (!ReferenceTransportUI.TryCreate(resources, out transportDialog) ||
                transportDialog == null || !transportDialog.Ready ||
                !ReferenceStoreDlg.TryCreate(resources, out storeDialog) ||
                storeDialog == null || !storeDialog.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
                ReleaseOwnedState();
                return false;
            }

            // Standalone composition uses the same original creation block. In production
            // GameBootstrap.BuildWorld already ran it before constructing this map adapter.
            if (gameBootstrap == null) GameBootstrap.EnsureDialogUIForIndependentRuntime();
            if (!ReferenceFirstWorldNpcDialog.TryCreate(
                    resources,
                    OpenShopFromNpcDialog,
                    OpenTransportFromNpcDialog,
                    OpenWarehouseFromNpcDialog,
                    OpenFamilyStashFromNpcDialog,
                    out npcDialog,
                    SendMenuCommandFromNpcDialog) ||
                npcDialog == null || !npcDialog.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
                ReleaseOwnedState();
                return false;
            }

            npcDialog.BindTaskRequestsForIndependentRuntime(
                RequestNpcTaskOffersFromDialog, RequestNpcTaskMutationFromDialog);

            if (!ReferenceFirstWorldInspectedEquipmentDialog.TryCreate(
                    resources,
                    out inspectedEquipmentDialog) ||
                inspectedEquipmentDialog == null || !inspectedEquipmentDialog.Ready)
            {
                LastFailure =
                    ReferenceFirstWorldWindowFailure.InvalidInspectedEquipmentDialog;
                ReleaseOwnedState();
                return false;
            }

            if (!ReferenceFirstWorldUiTip.TryCreate(resources, out uiTip) ||
                uiTip == null || !uiTip.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidUiTip;
                ReleaseOwnedState();
                return false;
            }

            if (!ReferenceFirstWorldSystemInfoBar.TryCreate(
                    resources,
                    out systemInfoBar) ||
                systemInfoBar == null || !systemInfoBar.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidSystemInfoBar;
                ReleaseOwnedState();
                return false;
            }

            reviveChoiceDialog = new ReferenceReviveDialogBinding();
            if (!reviveChoiceDialog.Ready)
            {
                LastFailure =
                    ReferenceFirstWorldWindowFailure.InvalidReviveChoiceDialog;
                ReleaseOwnedState();
                return false;
            }

            if (!ReferenceGameHudItemCooldowns.TryCreate(
                    resources,
                    out itemCooldowns) ||
                itemCooldowns == null || !itemCooldowns.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidInventoryItemUse;
                ReleaseOwnedState();
                return false;
            }

            // Skill tables belong to Win05 and SkillEffectDB. Their existing consumers
            // load them at the native read sites; an eager catalog is not a world-entry gate.
            skillCooldowns = new ReferenceGameHudSkillCooldowns();

            if (!ReferenceFirstWorldInventoryDialog.TryCreate(
                    resources,
                    systemInfoBar.SharedMessageBox,
                    itemCooldowns,
                    GameHud.I != null ? GameHud.I.CooldownFramesForIndependentRuntime : null,
                    out inventoryDialog) ||
                inventoryDialog == null || !inventoryDialog.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidInventoryDialog;
                ReleaseOwnedState();
                return false;
            }

            if (!ReferenceFirstWorldLocalEquipmentDialog.TryCreate(
                    resources,
                    character,
                    inventoryDialog.ToolTarget,
                    out localEquipmentDialog) ||
                localEquipmentDialog == null || !localEquipmentDialog.Ready)
            {
                LastFailure =
                    ReferenceFirstWorldWindowFailure.InvalidLocalEquipmentDialog;
                ReleaseOwnedState();
                return false;
            }

            inventoryDialog.ToolTarget.BindEquipmentOpeningForIndependentRuntime(localEquipmentDialog.EnsureOpen);

            transportStateVersion = session.TransportStateVersion;
            SynchronizeChangeSet();
            storeDialog.Synchronize(session.WarehouseUpdateSource);
            warehouseStateVersion = session.WarehouseStateVersion;
            familyStashStateVersion = session.FamilyStashStateVersion;
            ApplyFamilyStashSnapshot(session.FamilyStashState);
            helpTopicsStateVersion = session.HelpTopicsStateVersion;
            ApplyHelpTopicsSnapshot(session.HelpTopicsState);
            partyStateVersion = session.PartyStateVersion;
            teamDialog.ApplyState(session.PartyState, partyStateVersion);
            taskLogStateVersion = session.TaskLogStateVersion;
            WorldNpcDialogTextEdgeWindow dialogTextEdges =
                session.NpcDialogTextEdgeWindow ??
                WorldNpcDialogTextEdgeWindow.Empty;
            npcDialogTextEdgeWindowVersion = dialogTextEdges.Version;
            npcDialogTextEdgeCursor = dialogTextEdges.LastRevision;
            // No chatter application here: official 0x804b already ran
            // NpcChatter.SetServerChatter on receipt, once per packet, so the window owner only
            // observes the result through the owner statics the chatter code reads.
            if (!npcDialog.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
                ReleaseOwnedState();
                return false;
            }

            // GameHud.Load is the single ChatUI producer. Its conditional unparented
            // creation occurs on first GUI entry; borrow an existing singleton now, if any.
            if (ChatUI.I != null) chatUi.BindChatOwnerForIndependentRuntime(ChatUI.I);
            chatUi.BindHistoryMenuForIndependentRuntime((id, name, text, block, pointer) =>
                playerActionMenu.Open(CreatePlayerActionMenuSnapshot(id, name, text, block), pointer));
            // Real world playback reaches SoundManager through the recovered combat,
            // player and SkillSoundDB callers. Its persistent pool and paths are initialized
            // by the first original sound/volume call, never by world construction.
            // Keep the standalone snapshot adapter only for its explicit legacy callers.
            if (ReferenceEquals(gameBootstrap, null))
            {
                combatSound = sharedCombatSound != null
                    ? sharedCombatSound
                    : worldRootObject.AddComponent<ReferenceWorldCombatSound>();
                if (!combatSound.Configure(resources.RootDirectory))
                {
                    LastFailure = ReferenceFirstWorldWindowFailure.InvalidCombatSoundAsset;
                    ReleaseOwnedState();
                    return false;
                }
            }

            nameModeController = new ReferenceWorldNameModeController();
            nameModeController.Load(
                character.PersistentCharacterId.GetValueOrDefault(),
                character.DisplayName);
            frame = new ReferenceFirstWorldInitialFrame(
                map,
                tiles,
                shapes,
                resources,
                worldRootObject.transform,
                camera,
                mapDescriptor,
                character,
                null,
                GameSettings.HideOtherPets,
                combatSound,
                processNpcNames);
            frame.NpcSpawnOwnerForIndependentRuntime = gameBootstrap;
            frame.TransitionOwnsConstructionFailureForIndependentRuntime =
                bootstrapOwnsConstructionRoot || transitionOwnsConstructionFailure;
            frame.PendingLifeAppliedForIndependentRuntime = session.RecordAppliedPlayerLife;
            if (gameBootstrap != null)
                frame.MapObjectsPlacedForIndependentRuntime = gameBootstrap.MapObjectsPlacedForIndependentRuntime;
            frame.BuildCheckpointsForIndependentRuntime = BuildCheckpointsForIndependentRuntime;
            frame.AdoptFloorForIndependentRuntime(preparedStaticComposite);
            preparedStaticComposite = null;
            nativeHudViewsBound = false;
            frame.LocalPlayerCreatedForIndependentRuntime = nativePlayer =>
            {
                // Native BuildWorld publishes the newly created controller transform, then
                // conditionally creates the independent GameHud, before mask/transfer reads.
                if (!ReferenceEquals(gameBootstrap, null))
                    gameBootstrap.PublishConstructionPlayerForIndependentRuntime(frame.Player);
                GameBootstrap.PlayerT = nativePlayer.transform;
                nativeHudViewsBound = PrepareNativeHudViewsForIndependentRuntime();
                // Native BuildWorld supplies dimensions and spawn coordinates after the HUD
                // producer and before mask reads. A thrown earlier producer leaves defaults.
                nativePlayer.worldW = map.MapWidth;
                nativePlayer.worldH = map.MapHeight;
                nativePlayer.ppu = ReferenceFirstWorldStaticComposite.PixelsPerUnit;
                nativePlayer.worldX = character.X;
                nativePlayer.worldY = character.Y;
                byte[] maskBytes = resources.ReadMap("map/" + mapDescriptor.file + ".msk");
                nativePlayer.maskData = maskBytes;
                if (maskBytes == null)
                    nativePlayer.maskAsset = Resources.Load<TextAsset>(mapDescriptor.file + "_mask");

                // GameBootstrap.BuildWorld (060003d4) skips ordinary transfer polygons for a
                // custom logical map. Keep the raw identity; IsCustom normalizes instance IDs.
                mapLinks = CustomMaps.IsCustom(character.MapId)
                    ? null
                    : MapLink.Load(resources, mapDescriptor.file);

                // GameBootstrap.BuildWorld (0x060003d4) assigns the official MapProc list and the
                // transfer callback to PlayerController before Unity's first Start/Update edge, in
                // this order: +0x1f0 = MapLink.Load (0x060005b8), +0x1f8 = MapLink.LoadGather
                // (0x060005b6), +0x200 = a fresh Action<int, float, float> over the GameBootstrap
                // instance and metadata method 7565, which resolves to GameBootstrap.SwitchMap
                // (0x060003de) on both architectures - not SwitchMapAuthoritative (0x060003dd),
                // which is the queueing wrapper the official client keeps for server-driven
                // teleports. The polygon trigger therefore drops a transfer raised while a switch
                // is already running instead of queueing it.
                frame.Player.transferProcs = mapLinks;
                PkgArchive gatherPkg = ResLoader.I != null ? ResLoader.I.Pkg : null;
                PkgArchive gatherPkg24 = ResLoader.I != null ? ResLoader.I.Pkg24 : null;
                nativePlayer.gatherZones = MapLink.LoadGather(gatherPkg, gatherPkg24, mapDescriptor.file);
                frame.Player.onTransfer = gameBootstrap == null
                    ? null
                    : gameBootstrap.SwitchMap;
            };
            if (!frame.BuildForViewport(viewportWidth, viewportHeight, dpiScale))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InitialFrameRejected;
                ReleaseOwnedState();
                return false;
            }

            if (!nativeHudViewsBound)
            {
                ReleaseOwnedState();
                return false;
            }

            if (dealDialog != null) dealDialog.RebindInventory(inventoryDialog);
            else if (!ReferenceFirstWorldDealDialog.TryCreate(resources, hudResources, inventoryDialog,
                         systemInfoBar.SharedMessageBox, out dealDialog) || dealDialog == null || !dealDialog.Ready)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidDealDialog;
                ReleaseOwnedState();
                return false;
            }

            if (!PollPlayerStatusRevisions(true) ||
                !SyncInventoryState(true) ||
                !SyncCharacterTitleState() ||
                !SyncHotbarState(true) ||
                !SyncBuffState(true, 0f) ||
                !SyncTradeState(true))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerStatusHud;
                ReleaseOwnedState();
                return false;
            }

            skillDialog = GameBootstrap.AcquireWin05ForIndependentRuntime();
            // Short-circuit order is unchanged; each step is recorded so a failure names itself in
            // Player.log instead of silently disabling the three Win05 windows (skills, pets,
            // character/wardrobe), which are the ones these steps feed.
            bool skillDialogInit = skillDialog != null &&
                skillDialog.TryInitializeSkillDialog(
                    resources,
                    uiTip,
                    character,
                    systemInfoBar.SharedMessageBox);
            bool skillDialogPet = skillDialogInit &&
                (session.PlayerStatusState == null ||
                 skillDialog.ApplyPetStatus(session.PlayerStatusState.Pet));
            bool skillDialogWardrobe = skillDialogPet &&
                skillDialog.ApplyMagicWardrobeInventory(
                    session.InventoryState ?? WorldInventoryState.Empty);
            bool skillDialogWardrobeSync = skillDialogWardrobe && SyncWardrobeState(true);
            bool skillDialogBookSync = skillDialogWardrobeSync && SyncSkillBookState(true);
            if (!skillDialogBookSync)
            {
                UnityEngine.Debug.LogError(
                    "[owner] Win05 gate failed: addComponent=" + (skillDialog != null) +
                    " init=" + skillDialogInit + " pet=" + skillDialogPet +
                    " wardrobe=" + skillDialogWardrobe +
                    " wardrobeSync=" + skillDialogWardrobeSync +
                    " skillBookSync=" + skillDialogBookSync +
                    " petState=" + (session.PlayerStatusState != null) +
                    " inventoryState=" + (session.InventoryState != null));
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidSkillDialog;
                ReleaseOwnedState();
                return false;
            }
            SyncLocalSkillFacingComponent();
            PlayerController playerController = frame.Player.Root.GetComponent<PlayerController>();
            frame.WorkConfirmationForIndependentRuntime = ConfirmWorkCancellationForIndependentRuntime;
            collisionMask = playerController.CaptureMaskViewForIndependentRuntime();
            EnsureSkillActorEffects(
                frame.Player.Root,
                frame.Player.MainRenderer,
                () =>
                {
                    ReferenceWorldPlayerFacing facing = frame.Player.CurrentFacing;
                    return (facing.StoredDirection, facing.FlipX);
                });
            cursorManager = CursorManager.EnsureForIndependentRuntime(resources.RootDirectory);

            playerTaskTrack = frame.Player.Root.GetComponent<PlayerTaskTrack>();
            // No task seeding and no re-application of the published snapshot here. Official
            // 0x8027 tail-calls TaskClient.SetState (0x06000e5d) inside the case body, so
            // TaskClient.Active / Done already hold the rows the last reply built - or the rows
            // of the previous session when no 0x8027 has arrived yet, which is the official
            // process-wide static behaviour. Re-applying the publication from the build would
            // add one replacement and one OnStateChanged that no packet caused, would stamp every
            // row again from a later frame, and would clear the owner on every world build
            // because a disposed session publishes WorldTaskLogState.Empty. The window's first
            // frame reads the owner as it is (PlayerTaskTrack, TaskDlg, the NPC dialog text).

            manualMovementController.BindOwnerForIndependentRuntime(frame.Player.Root.GetComponent<PlayerController>());
            // Native CombatWorld may not exist until its first real Ensure caller.
            // Its tooltip route belongs to UITip, not this disposable map adapter.
            // Native combat owns this call stack: a send exception must stop the following
            // swing/projectile and phase stores. None selects the synchronous host sender;
            // completed request observation and the issued counter remain at this boundary.
            NetClient.I?.BindAttackWriterForIndependentRuntime(
                id =>
                {
                    pendingBasicAttackRequests.Add(
                        session.AttackAsync(id, CancellationToken.None));
                    basicAttackRequestIssuedCount++;
                });
            // NetClient.get_Connected (060006b7) already exists in Protocol and is forwarded by
            // PrivateServerGateway.Connected and ClientSessionCoordinator.Connected; this only
            // points the Presentation mirror at that same real owner.
            NetClient.I?.BindConnectedReaderForIndependentRuntime(() => session.Connected);
            frame.BindMovementTransferSlotForIndependentRuntime(
                () => AdvancePendingGroundPickup());
            BindClickRingSpriteForIndependentRuntime(resources);

            if (!ReferenceEquals(gameBootstrap, null))
            {
                // The original bootstrap already invoked this live singleton in its prefix.
                // Borrow it without a second play, a map-owned producer or resource admission.
                mapMusic = MusicManager.I;
            }
            else if (sharedMusic != null)
            {
                mapMusic = sharedMusic;
                if (!mapMusic.TryPlayMap(mapDescriptor.file))
                {
                    LastFailure = ReferenceFirstWorldWindowFailure.InvalidMusicAsset;
                    ReleaseOwnedState();
                    return false;
                }
            }
            else
            {
                mapMusic = worldRootObject.AddComponent<MusicManager>();
                if (!mapMusic.ConfigureIndependentRuntime(resources.RootDirectory) ||
                    !mapMusic.TryPlayMap(mapDescriptor.file))
                {
                    LastFailure = ReferenceFirstWorldWindowFailure.InvalidMusicAsset;
                    ReleaseOwnedState();
                    return false;
                }
            }

            built = true;
            if (!SyncReviveChoiceState(true))
            {
                LastFailure =
                    ReferenceFirstWorldWindowFailure.InvalidReviveChoiceDialog;
                ReleaseOwnedState();
                return false;
            }

            if (!SyncLocalSkillCast(true, Time.time))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidLocalSkillCast;
                ReleaseOwnedState();
                return false;
            }

            if (!SyncSkillHitEffects(true))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidLocalSkillCast;
                ReleaseOwnedState();
                return false;
            }

            // Official CharData.invisible (field token 67114234, statics +0xd8): the aggregate
            // the 0x8017 case wrote at receipt, not a per-frame re-read of the published rows.
            if (!SyncPlayerAppearance(
                    true,
                    ReferenceWorldLocalPlayerDoll.VisibilityTint(
                        CharData.invisible)))
            {
                LastFailure =
                    ReferenceFirstWorldWindowFailure.InvalidPlayerAppearance;
                ReleaseOwnedState();
                return false;
            }

            if (!SyncLocalStatusEffects(true))
            {
                LastFailure =
                    ReferenceFirstWorldWindowFailure.InvalidLocalStatusEffect;
                ReleaseOwnedState();
                return false;
            }

            // Borrow live wrapper fields for the complete UiEsc body. No visibility snapshot or
            // alternate close decision is published, and a superseded host cannot clear this binding.
            releaseEscapeCloseTopBinding?.Invoke();
            releaseEscapeCloseTopBinding =
                ReferenceFirstWorldEscapeDispatch.BindOwnersForIndependentRuntime(
                    () => npcDialog, () => inspectedEquipmentDialog);
            BindHudWindowsForIndependentRuntime(nativeHud);
            nativeHud.BindGuiHostForIndependentRuntime(this, ObserveHotbarActivationForIndependentRuntime);
            LastFailure = ReferenceFirstWorldWindowFailure.None;
            return true;
        }
        catch (Exception exception)
        {
            // Both original entries propagate construction exceptions. The bootstrap keeps
            // the partial graph; TransitionTo supplies its own two outer catch boundaries.
            if (bootstrapOwnsConstructionRoot || transitionOwnsConstructionFailure) throw;
            // Exception messages may contain server data; only record type and call sites.
            Debug.LogError("[world.construct] exception=" + exception.GetType().FullName + "\n" + exception.StackTrace);
            LastFailure = ReferenceFirstWorldWindowFailure.UnexpectedConstructionFailure;
            ReleaseOwnedState();
            return false;
        }
    }

    /// <summary>
    /// Polls server-authored revisions and advances only the already reviewed actor clocks.
    /// </summary>
    public bool Tick(
        int viewportWidth,
        int viewportHeight,
        float dpiScale,
        float deltaTime,
        float standTimeSeconds)
    {
        return Tick(
            viewportWidth,
            viewportHeight,
            dpiScale,
            deltaTime,
            standTimeSeconds,
            false);
    }

    /// <summary>
    /// Polls the world and optionally applies the official F11 name-mode cycle before remote
    /// actors consume the new mode in this frame.
    /// </summary>
    public bool Tick(
        int viewportWidth,
        int viewportHeight,
        float dpiScale,
        float deltaTime,
        float standTimeSeconds,
        bool cycleNameMode,
        bool advancePointerInput = false,
        Func<bool> nameModeInput = null)
    {
        ThrowIfDisposed();
        // The source-owned remote tick is explicitly driven here. The original frame guard
        // makes this boundary idempotent with GameBootstrap/Pump/Mount callers.
        ArtUploadClock.BeginFrame();
        if (!built || frame == null)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.NotBuilt;
            return false;
        }

        if (viewportWidth < 1 || viewportHeight < 1 ||
            !IsPositiveFinite(dpiScale) ||
            !IsNonNegativeFinite(deltaTime) ||
            !IsNonNegativeFinite(standTimeSeconds))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidTickInput;
            return false;
        }

        // Native Start owns mask initialization. This only observes its current decoded
        // fields; the optional window never drives Start or substitutes mask state.
        PlayerController nativePlayer = frame.Player?.SourceControllerForIndependentRuntime;
        if (collisionMask == null && nativePlayer != null)
            collisionMask = nativePlayer.CaptureMaskViewForIndependentRuntime();

        if (nameModeController == null)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.RevisionRejected;
            return false;
        }

        if (!PollPlayerStatusRevisions(false))
        {
            LastFailure =
                ReferenceFirstWorldWindowFailure.InvalidPlayerStatusRevision;
            return false;
        }

        if (systemInfoBar == null ||
            !systemInfoBar.Update(deltaTime) || !ObserveSystemRequestResponses() ||
            !SyncInventoryState(false) || !SyncWardrobeState(false) ||
            !ObserveInventoryCommands() ||
            !SyncCharacterTitleState() ||
            !SyncHotbarState(false) || !ObserveHotbarCommands() ||
            !SyncTradeState(false) ||
            !ObserveBasicAttackRequests() ||
            !ObserveGroundPickupRequests() || !ObservePositionReports())
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidSystemInfoBar;
            return false;
        }

        if (!SyncReviveChoiceState(false) || !ObserveReviveChoiceRequests())
        {
            LastFailure =
                ReferenceFirstWorldWindowFailure.InvalidReviveChoiceDialog;
            return false;
        }

        if (!SyncNpcDialogState() || !ObserveNpcTaskRequests())
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
            return false;
        }

        if (!SyncBuffState(false, standTimeSeconds))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerStatusHud;
            return false;
        }

        // Same owner as the build arm: the 0x8017 aggregate, not a second row scan.
        Color32 riderTint = ReferenceWorldLocalPlayerDoll.VisibilityTint(
            CharData.invisible);
        if (!SyncPlayerAppearance(false, riderTint))
        {
            LastFailure =
                ReferenceFirstWorldWindowFailure.InvalidPlayerAppearance;
            return false;
        }

        // Only explicit diagnostic callers request this old F11 operation. The live
        // component reads F11 itself after its native appearance/tint prefix.
        if (nameModeInput != null) cycleNameMode = nameModeInput();
        if (cycleNameMode)
        {
            LastNameModeFeedback = nameModeController.Cycle();
        }

        if (!frame.PollAuthoritativeWorldRevisions(
                session,
                standTimeSeconds,
                riderTint,
                nameModeController.Mode))
        {
            // One incomplete optional projection must not destroy the already valid map, player
            // and HUD graph. Keep the last source-owned state visible, but retain a monotonic,
            // machine-readable rejection so acceptance automation still fails honestly.
            lastWorldRevisionFailure = frame.LastRevisionFailure;
            worldRevisionRejectionCount++;
        }
        else
        {
            lastWorldRevisionFailure = ReferenceWorldRevisionFailure.None;
        }

        if (!SyncLocalSkillCast(false, standTimeSeconds) ||
            !SyncSkillHitEffects(false))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidLocalSkillCast;
            return false;
        }

        if (!SyncLocalStatusEffects(false))
        {
            LastFailure =
                ReferenceFirstWorldWindowFailure.InvalidLocalStatusEffect;
            return false;
        }

        // Native task/name owners read TaskClient directly. The old task-feedback
        // adapter only checked borrowed-view readiness and consumed no task contents.

        // CombatWorld.Update runs once through Unity's component lifecycle. No combat phase
        // or request stage is driven by this host Tick, including a Tick that fails projection.

        // WBD-074 lane D: the pet tail used to be re-bound here once per frame while the rows were
        // bound once per published version in PollPlayerStatusRevisions. One authority for both is
        // the single version gate above, which runs before this point and from the same snapshot,
        // so nothing has to be re-stated here and no frame can end with one half of the pair.
        // WBD-075 lane D: the old split could only surface where a Tick that stops below this
        // point still leaves the window drawable, i.e. the background-session-check draw path at
        // ClientRootView.cs:238-251; on the foreground path ClientRootView.cs:531-561 releases
        // and clears the window on a failed Tick, before any later OnGUI can draw the pet panel.
        if (!frame.AdvanceAuthoritativeWorldActors(deltaTime, standTimeSeconds, riderTint))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.ActorAdvanceRejected;
            return false;
        }

        // The official CameraFollow and FloorRenderer each own one LateUpdate. Do not drive either
        // lifecycle again from this Update/Tick path; doing so refreshed the camera twice and moved
        // floor culling to the wrong phase.
        TickCount++;
        LastFailure = ReferenceFirstWorldWindowFailure.None;
        return true;
    }

    internal static ReferenceFirstWorldPointerWindow ChoosePointerWindow(
        bool chatOverlay,
        bool systemMessageBox,
        bool reviveChoice,
        bool npcDialog,
        bool playerActionMenu,
        bool dealDialog,
        bool inventoryDialog,
        bool localEquipmentDialog,
        bool inspectedEquipmentDialog,
        bool gossipDialog,
        bool localMapDiagram,
        bool chatUi)
    {
        return ChoosePointerWindow(
            chatOverlay,
            systemMessageBox,
            reviveChoice,
            npcDialog,
            playerActionMenu,
            dealDialog,
            inventoryDialog,
            localEquipmentDialog,
            inspectedEquipmentDialog,
            false,
            gossipDialog,
            localMapDiagram,
            chatUi);
    }

    internal static ReferenceFirstWorldPointerWindow ChoosePointerWindow(
        bool chatOverlay,
        bool systemMessageBox,
        bool reviveChoice,
        bool npcDialog,
        bool playerActionMenu,
        bool dealDialog,
        bool inventoryDialog,
        bool localEquipmentDialog,
        bool inspectedEquipmentDialog,
        bool skillDialog,
        bool gossipDialog,
        bool localMapDiagram,
        bool chatUi,
        bool duelDialog = false,
        bool mailDialog = false,
        bool teamConfigDialog = false,
        bool rackShopDialog = false)
    {
        if (chatOverlay)
        {
            return ReferenceFirstWorldPointerWindow.ChatOverlay;
        }
        if (systemMessageBox)
        {
            return ReferenceFirstWorldPointerWindow.SystemMessageBox;
        }
        if (reviveChoice)
        {
            return ReferenceFirstWorldPointerWindow.ReviveChoice;
        }
        if (playerActionMenu)
        {
            return ReferenceFirstWorldPointerWindow.PlayerActionMenu;
        }
        if (teamConfigDialog)
        {
            return ReferenceFirstWorldPointerWindow.TeamConfig;
        }
        if (rackShopDialog)
        {
            return ReferenceFirstWorldPointerWindow.RackShopDialog;
        }
        if (npcDialog)
        {
            return ReferenceFirstWorldPointerWindow.NpcDialog;
        }
        if (duelDialog)
        {
            return ReferenceFirstWorldPointerWindow.DuelDialog;
        }
        if (mailDialog)
        {
            return ReferenceFirstWorldPointerWindow.MailDialog;
        }
        if (dealDialog)
        {
            return ReferenceFirstWorldPointerWindow.DealDialog;
        }
        if (inventoryDialog)
        {
            return ReferenceFirstWorldPointerWindow.InventoryDialog;
        }
        if (localEquipmentDialog)
        {
            return ReferenceFirstWorldPointerWindow.LocalEquipmentDialog;
        }
        if (inspectedEquipmentDialog)
        {
            return ReferenceFirstWorldPointerWindow.InspectedEquipmentDialog;
        }
        if (skillDialog)
        {
            return ReferenceFirstWorldPointerWindow.SkillDialog;
        }
        if (gossipDialog)
        {
            return ReferenceFirstWorldPointerWindow.GossipDialog;
        }
        if (localMapDiagram)
        {
            return ReferenceFirstWorldPointerWindow.LocalMapDiagram;
        }
        return chatUi
            ? ReferenceFirstWorldPointerWindow.ChatUi
            : ReferenceFirstWorldPointerWindow.None;
    }

    private void PreparePointerWindowRouting(float scale, NativeAutomaticWindowEventScope automaticWindowEvents)
    {
        routedPointerEvent = null;
        routedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        heldSkillReleaseBlockedByWindow = false;
        Event current = Event.current;
        if (current == null || !IsRoutablePointerEvent(current.type))
        {
            return;
        }

        // Equipment and inventory receive Unity's original event in their own callbacks. Preserve it before
        // legacy priority routing can select an overlapping hosted window and call Use.
        // That window still receives a live copy and applies its own native WinZ mask.
        // Cover outside-window drag/release too; the native drag state outlives map borrowers.
        // Team's four persistent roots also drive themselves between map borrowers.
        // ChatUI also owns an automatic callback, including its input menu. Preserve its
        // original event before the legacy router consumes only the hosted copy.
        // Receipt-created SystemInfoBar likewise processes its own automatic callback.
        // TransportUI reads that original event in its own enabled Unity callback as well.
        if (EquipDlg.Opened || InventoryDlg.IsOpen || TeamDlg.Visible || TeamConfigDlg.Visible ||
            global::FakeFantasyOnline.Presentation.ReferenceAssets.SystemInfoBar.I != null ||
            RenameDlg.Visible ||
            TeamBar.I != null ||
            HelpWinDlg.Visible ||
            FamilyStashDlg.Visible ||
            HornDialog.Visible ||
            DuelDlg.Visible ||
            NativePlayerActionMenu.Visible ||
            JobOfferDlg.get_Visible() ||
            FamilyRelationDlg.get_Visible() ||
            FamilyCreateDlg.get_Visible() ||
            FamilyMailDlg.get_Visible() ||
            JobCustomDlg.get_Visible() ||
            JobRightDlg.get_Visible() ||
            TransportUI.VisibleS ||
            FamilyDetailDlg.get_Visible() ||
            FamilyDlg.get_Visible() ||
            GossipDlg.Visible ||
            TaskDlg.Visible ||
            RackShopDlg.Visible ||
            StoreDlg.Visible ||
            MailResistDlg.Visible ||
            DealDlg.Visible ||
            MailDlg.Visible ||
            MailConfigDlg.Visible ||
            FriendConfigDlg.Visible || MasterInfoDlg.Visible || ChatUI.I != null || GameHud.I != null)
        {
            automaticWindowEvents.PreserveCurrent();
            current = Event.current;
        }

        if (current.type == EventType.MouseDown)
        {
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        }

        Vector2 pointer = current.mousePosition;
        bool chatOverlay = chatUi != null &&
            (chatUi.InputMenuOpen || chatUi.HistoryVisible);
        bool systemMessageBox = systemInfoBar != null &&
            systemInfoBar.MessageBoxVisible;
        bool reviveChoice = reviveChoiceDialog != null && reviveChoiceDialog.Visible && ReviveDlg.MouseOver();
        DialogUI nativeDialog = DialogUI.I;
        bool nativeDialogHit = nativeDialog != null && nativeDialog.Visible &&
            nativeDialog.MouseOver(pointer, Screen.width, Screen.height, scale);
        bool npcHit = nativeDialogHit;
        npcHit = npcHit || ComposeDlg.MouseOver() || SculpDlg.MouseOver() || ShopDlg.I != null && ShopDlg.I.MouseOver() ||
            transportDialog != null && transportDialog.MouseOver(pointer, scale) ||
            storeDialog != null && storeDialog.MouseOver(pointer, scale) ||
            FamilyDlg.MouseOver() || FamilyCreateDlg.MouseOver() || FamilyDetailDlg.MouseOver() ||
            FamilyRelationDlg.MouseOver() || JobRightDlg.MouseOver() || JobCustomDlg.MouseOver() ||
            JobOfferDlg.MouseOver() || FamilyMailDlg.MouseOver() ||
            teamDialog != null && teamDialog.MouseOver(pointer, scale);
        bool playerAction = playerActionMenu != null && playerActionMenu.Visible;
        bool duelHit = duelDialog != null && duelDialog.MouseOver(pointer, scale);
        bool mailHit = mailDialog != null && mailDialog.MouseOver(pointer, scale);
        bool teamConfigHit = teamConfigDialog != null && teamConfigDialog.MouseOver(pointer, scale);
        bool rackShopHit = rackShopDialog != null && rackShopDialog.MouseOver();
        bool dealHit = dealDialog != null && dealDialog.MouseOver(pointer, scale);
        bool inventoryCapture = inventoryDialog != null && inventoryDialog.MouseOver();
        // The held-pet drop destination is not sampled here. Official Win05.DrawPet 06001043 reads
        // Event.current.mousePosition itself at arm64 0x3bcac8 and x86_64 0x3d076e and calls
        // InventoryDlg.OverWindow 060004fd at arm64 0x3bcaf0 and x86_64 0x3d078e, and by then this
        // window's own WinZ.Frame mask may have replaced that position with (-9999, -9999), so a
        // sample taken before the draw pass would report the inventory under an occluding window
        // that the official body rejects. No mask, area or second poll is added here.
        bool holdingPet = skillDialog != null && skillDialog.HasHeldPet;
        bool inventoryHit = inventoryCapture && !holdingPet;
        bool localEquipmentHit = EquipDlg.I != null && EquipDlg.I.MouseOver();
        bool inspectedHit = inspectedEquipmentDialog != null &&
            inspectedEquipmentDialog.MouseOver(pointer, scale);
        Win05 nativeSkillWindow = Win05.I;
        bool skillHit = nativeSkillWindow != null && nativeSkillWindow.MouseOver();
        if (skillHit && (nativeSkillWindow.ChangeEquipmentDialogVisible ||
                        nativeSkillWindow.MagicWardrobeDialogVisible))
        {
            // The newly toggled Win05 is the active WinZ window above its EquipDlg source.
            localEquipmentHit = false;
        }
        bool gossipHit = gossipDialog != null && gossipDialog.MouseOver(pointer, scale);
        bool diagramHit = minimapHud != null && minimapHud.DiagramOpen &&
            minimapHud.MouseOver(pointer, Screen.width, scale);
        bool chatHit = chatUi != null && chatUi.MouseOver(
            pointer,
            Screen.height,
            scale);
        ReferenceFirstWorldPointerWindow pointerHit = ChoosePointerWindow(
            chatOverlay,
            systemMessageBox,
            reviveChoice,
            npcHit,
            playerAction,
            dealHit,
            inventoryHit,
            localEquipmentHit,
            inspectedHit,
            skillHit,
            gossipHit,
            diagramHit,
            chatHit,
            duelDialog: duelHit,
            mailDialog: mailHit,
            teamConfigDialog: teamConfigHit,
            rackShopDialog: rackShopHit);

        // Keep the existing NPC group's priority, but distinguish its independent native
        // owner from the remaining NPC-group routing for event-copy isolation.
        if (pointerHit == ReferenceFirstWorldPointerWindow.NpcDialog && nativeDialogHit)
            pointerHit = ReferenceFirstWorldPointerWindow.NativeDialogUI;

        // GameHud.OnGUI owns the global held-skill MouseUp tail. A MouseDown that
        // originated in Win05 may capture that window for dragging, but forwarding
        // the release only to Win05 prevents both the hotbar drop and the mandatory
        // clear-before-DrawDragIcon branch from observing it. Retain the actual
        // top-window hit so a covered hotbar slot cannot accept the release.
        if (current.type == EventType.MouseUp && skillDialog != null &&
            skillDialog.HeldSkillId > 0)
        {
            heldSkillReleaseBlockedByWindow =
                pointerHit != ReferenceFirstWorldPointerWindow.None;
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
            return;
        }

        // SplitInput owns the click/drag while its native popup is open, including outside
        // clicks that close it. Do not route that event to the underlying warehouse grid.
        if (InventoryDlg.IsOpen && InventoryDlg.SplitOpen)
        {
            automaticWindowEvents.MaskCurrent();
            return;
        }

        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.InventoryDialog &&
            !InventoryDlg.IsOpen)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.LocalEquipmentDialog &&
            !EquipDlg.Opened)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.InspectedEquipmentDialog &&
            !ExEquipDlg.IsOpen)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.NativeDialogUI &&
            !DialogUI.VisibleS)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;

        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.TeamConfig && !TeamConfigDlg.Visible)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.DealDialog && !DealDlg.Visible)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;

        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.RackShopDialog && !RackShopDlg.Visible)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.GossipDialog && !GossipDlg.Visible)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        if (capturedPointerWindow == ReferenceFirstWorldPointerWindow.PlayerActionMenu && !NativePlayerActionMenu.Visible)
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;

        ReferenceFirstWorldPointerWindow hit =
            current.type == EventType.MouseDrag || current.type == EventType.MouseUp
                ? capturedPointerWindow
                : ReferenceFirstWorldPointerWindow.None;
        if (hit == ReferenceFirstWorldPointerWindow.None)
        {
            hit = pointerHit;
        }

        if (hit == ReferenceFirstWorldPointerWindow.None)
        {
            if (current.type == EventType.MouseUp)
            {
                capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
            }
            return;
        }

        if (hit == ReferenceFirstWorldPointerWindow.InventoryDialog ||
            hit == ReferenceFirstWorldPointerWindow.DuelDialog ||
            hit == ReferenceFirstWorldPointerWindow.PlayerActionMenu ||
            hit == ReferenceFirstWorldPointerWindow.GossipDialog ||
            hit == ReferenceFirstWorldPointerWindow.RackShopDialog ||
            hit == ReferenceFirstWorldPointerWindow.DealDialog ||
            hit == ReferenceFirstWorldPointerWindow.LocalEquipmentDialog ||
            hit == ReferenceFirstWorldPointerWindow.InspectedEquipmentDialog ||
            hit == ReferenceFirstWorldPointerWindow.SkillDialog ||
            hit == ReferenceFirstWorldPointerWindow.NativeDialogUI ||
            hit == ReferenceFirstWorldPointerWindow.TeamConfig)
        {
            if (current.type == EventType.MouseDown) capturedPointerWindow = hit;
            else if (current.type == EventType.MouseUp)
                capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
            if (hit == ReferenceFirstWorldPointerWindow.SkillDialog)
            {
                // The existing inventory-carry consumer must share the real event with Win05.
                // Its native Use remains visible to the automatic callback, in either order.
                routedPointerEvent = automaticWindowEvents.NativeEvent;
                routedPointerWindow = hit;
            }
            // InventoryDlg, EquipDlg, ExEquip, Win05, DialogUI, DealDlg and Team own automatic Unity callbacks. Consume only a copy for the
            // InventoryDlg, EquipDlg, ExEquip, Win05, DialogUI, Team and RackShop own automatic Unity callbacks. Consume only a copy for the
            // InventoryDlg, EquipDlg, ExEquip, Win05, DialogUI, Team and Gossip own automatic Unity callbacks. Consume only a copy for the
            // hosted remainder; the scope restores the original, including real native Use.
            automaticWindowEvents.MaskCurrent();
            return;
        }

        // Automatic owners remain in the NPC group for routing. Any overlapping hosted
        // owner must receive this live copy and apply its own native input rules.
        // PreserveCurrent protects the automatic original;
        // the scope propagates only real hosted consumption back to that original.
        routedPointerEvent = new Event(current);
        routedPointerWindow = hit;
        if (current.type == EventType.MouseDown)
        {
            capturedPointerWindow = hit;
        }
        else if (current.type == EventType.MouseUp)
        {
            capturedPointerWindow = ReferenceFirstWorldPointerWindow.None;
        }
        automaticWindowEvents.UseForRouting(routedPointerEvent);
    }

    private sealed class NativeModalEventScope : IDisposable
    {
        private readonly Event original;
        private readonly bool masked;

        internal NativeModalEventScope()
        {
            original = Event.current;
            masked = original != null && SysMsgBox.Visible &&
                (IsRoutablePointerEvent(original.type) || original.type == EventType.KeyDown ||
                 original.type == EventType.KeyUp);
            if (masked)
            {
                Event copy = new Event(original);
                copy.Use();
                Event.current = copy;
            }
        }

        public void Dispose()
        {
            if (masked) Event.current = original;
        }
    }

    private sealed class NativeAutomaticWindowEventScope : IDisposable
    {
        private Event original;
        private Event hosted;
        private Event routed;
        private bool preserved;
        private bool routingMasked;

        internal Event NativeEvent => preserved ? original : Event.current;

        internal void PreserveCurrent()
        {
            if (preserved) return;
            original = Event.current;
            hosted = new Event(original);
            Event.current = hosted;
            preserved = true;
        }

        internal void UseForRouting(Event nativeConsumerEvent)
        {
            if (preserved)
            {
                routed = nativeConsumerEvent;
                routingMasked = true;
            }
            Event.current.Use();
        }

        internal void MaskCurrent()
        {
            PreserveCurrent();
            routingMasked = true;
            Event.current.Use();
        }

        public void Dispose()
        {
            if (!preserved) return;
            // Discard only the router's synthetic Use. A real hosted consumer may have
            // consumed its live routed event (or the unmasked hosted event); retain that
            // consumption for later automatic callbacks, just as for the Win05 shared original.
            if ((!routingMasked && hosted.type == EventType.Used) ||
                (routed != null && routed.type == EventType.Used))
                original.Use();
            Event.current = original;
        }
    }

    private Event BeginPointerWindow(ReferenceFirstWorldPointerWindow owner)
    {
        Event previous = Event.current;
        if (routedPointerEvent != null && routedPointerWindow == owner)
        {
            Event.current = routedPointerEvent;
        }
        return previous;
    }

    private static void EndPointerWindow(Event previous)
    {
        Event.current = previous;
    }

    private static bool IsRoutablePointerEvent(EventType type)
    {
        return type == EventType.MouseDown || type == EventType.MouseUp ||
            type == EventType.MouseDrag || type == EventType.MouseMove ||
            type == EventType.ScrollWheel || type == EventType.ContextClick;
    }

    /// <summary>
    /// Observes the native HUD and drives the remaining map-hosted windows.
    /// </summary>
    public bool DrawHud(float elapsedSeconds)
    {
        ThrowIfDisposed();
        if (!built) return false;
        // GameHud owns its original automatic OnGUI. This map only observes state and
        // drives the remaining hosted windows; it never replays the native HUD message.
        if (ChatUI.I != null) chatUi.BindChatOwnerForIndependentRuntime(ChatUI.I);
        // Build/Release own adapter lifetime. Observation failures are diagnostic results,
        // not extra native HUD admission rules. Evaluate every observer even after a failure.
        bool hostStateReady = ObserveSystemRequestResponses();
        hostStateReady &= SyncReviveChoiceState(false);
        hostStateReady &= ObserveReviveChoiceRequests();
        hostStateReady &= SyncInventoryState(false);
        hostStateReady &= ObserveInventoryCommands();
        bool hudReady = SyncHotbarState(false);
        hostStateReady &= SyncWardrobeState(false);
        hostStateReady &= SyncSkillBookState(false);
        hudReady &= hotbar.ApplyTooltipSources(
            session.InventoryState ?? WorldInventoryState.Empty,
            session.PlayerStatusState == null ? null : session.PlayerStatusState.Pet,
            skillDialog);
        hudReady &= ObserveHotbarCommands();
        hudReady &= SyncBuffState(false, elapsedSeconds);
        hostStateReady &= SyncTradeState(false);
        hostStateReady &= SyncNpcDialogState();
        hostStateReady &= ObserveNpcTaskRequests();
        hostStateReady &= SyncNpcFunctionDialogs();
        hostStateReady &= ObserveNpcFunctionRequests();

        if (teamDialog != null && session.PartyStateVersion != partyStateVersion)
        {
            partyStateVersion = session.PartyStateVersion;
            teamDialog.ApplyState(session.PartyState, partyStateVersion);
        }

        // The actual SysMsgBox.OnGUI receives Unity's event. Legacy window routing only sees
        // a consumed copy while that modal is open, and cannot steal the native button event.
        using var nativeModalEvents = new NativeModalEventScope();
        // These recovered OnGUI bodies consume the original event under their own WinZ gates.
        // Run them before the legacy router copies/consumes events for its hosted adapters.
        float scale = UIScale.Factor;
        // Restore held-target dispatch before merged widgets consume the same click. The
        // original inventory tail, not a new per-window admission rule, decides the destination.
        inventoryDialog.DispatchHeldTargetClickForIndependentRuntime(Event.current, scale);
        using var automaticWindowEvents = new NativeAutomaticWindowEventScope();
        PreparePointerWindowRouting(scale, automaticWindowEvents);
        hudReady &= nativeHud.LastGuiDrawSucceededForIndependentRuntime;

        // Keep actual supplier/observer failures in the return value without turning
        // them into an extra GUI admission gate for independent owners or later slices.
        bool windowsReady = hudReady && hostStateReady;

        // The shared PlayerActionMenu owns its automatic GUI and direct native dispatch.
        // LastPlayerActionMenuAction is a read-only observation, never a replay command.

        string teamName;
        int teamInvite;
        if (TryConsumeTeamConfigOpenIntent(out teamName, out teamInvite))
            teamConfigDialog.Open(teamName, teamInvite);
        Event previousTeamEvent = BeginPointerWindow(ReferenceFirstWorldPointerWindow.NpcDialog);
        bool teamDrawn;
        try { teamDrawn = teamDialog != null && teamDialog.Draw(scale); }
        finally { EndPointerWindow(previousTeamEvent); }
        if (!teamDrawn)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerActionMenu;
            windowsReady = false;
        }
        Event previousTeamConfigEvent = BeginPointerWindow(ReferenceFirstWorldPointerWindow.TeamConfig);
        bool teamConfigReady;
        try { teamConfigReady = teamConfigDialog.Draw(scale); }
        finally { EndPointerWindow(previousTeamConfigEvent); }
        if (!teamConfigReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerActionMenu;
            windowsReady = false;
        }

        // DuelDlg owns its automatic callback, including between map borrowers.
        if (duelDialog == null || !duelDialog.Ready)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerActionMenu;
            windowsReady = false;
        }
        Event previousMailEvent = BeginPointerWindow(ReferenceFirstWorldPointerWindow.MailDialog);
        bool mailReady;
        try { mailReady = mailDialog != null && mailDialog.Draw(scale); }
        finally { EndPointerWindow(previousMailEvent); }
        if (!mailReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerActionMenu;
            windowsReady = false;
        }

        // RackShopDlg.OnGUI is invoked by Unity, including when there is no map borrower.
        bool rackReady = rackShopDialog != null && rackShopDialog.Ready;
        if (!rackReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidPlayerActionMenu;
            windowsReady = false;
        }

        bool chatStateReady = ObserveChatSends();
        if (!chatStateReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidChatUi;
            windowsReady = false;
        }
        Event previousChatEvent = BeginPointerWindow(
            routedPointerWindow == ReferenceFirstWorldPointerWindow.ChatOverlay
                ? ReferenceFirstWorldPointerWindow.ChatOverlay
                : ReferenceFirstWorldPointerWindow.ChatUi);
        try
        {
            // Retain only the separate copy-overlay host. ChatUI.OnGUI is now the sole
            // chat renderer and receives Unity callbacks independently of this map borrower.
            if (ChatUI.I != null)
            {
                chatUi.DrawCopyOverlayForIndependentRuntime(scale);
            }
        }
        finally
        {
            EndPointerWindow(previousChatEvent);
        }
        // GossipDlg receives its own Unity callback, including before/between world hosts.
        if (gossipDialog == null || !gossipDialog.Ready)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidGossipDialog;
            windowsReady = false;
        }

        // DialogUI receives its independent Unity callback even if this HUD pass returns
        // early. This latch observes an already-run request callback and never sends again.
        if (!npcTaskActionReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
            windowsReady = false;
            npcTaskActionReady = true;
        }

        Event previousNpcFunctionEvent = BeginPointerWindow(
            ReferenceFirstWorldPointerWindow.NpcDialog);
        bool npcFunctionsReady;
        try
        {
            // StoreDlg.OnGUI receives the original Unity event independently of this host.
            // TransportUI.OnGUI receives Unity's original event independently of this host.
            npcFunctionsReady = transportDialog.Ready && storeDialog.Ready;
        }
        finally
        {
            EndPointerWindow(previousNpcFunctionEvent);
        }
        if (!npcFunctionsReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
            windowsReady = false;
        }

        Event previousFamilyEvent = BeginPointerWindow(ReferenceFirstWorldPointerWindow.NpcDialog);
        bool familyReady;
        try
        {
            bool ownsFamilyGui = FamilyDlg.IsGuiDriverForIndependentRuntime(this);
            familyReady = true;
        }
        finally { EndPointerWindow(previousFamilyEvent); }
        if (!familyReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
            windowsReady = false;
        }

        // Win05 is independently Unity-driven. This map host only consumes its existing
        // host-view intents; no manual OnGUI call is installed.
        if (!ConsumeSkillDialogIntents())
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidSkillDialog;
            windowsReady = false;
        }

        bool inspectedStateReady = ObserveTradeRequest() &&
            ObserveInspectRequest();
        if (!inspectedStateReady)
        {
            LastFailure =
                ReferenceFirstWorldWindowFailure.InvalidInspectedEquipmentDialog;
            windowsReady = false;
        }
        // ExEquipDlg is an independent automatic OnGUI root. This world borrows only its
        // visibility, pointer and CloseNow, so unrelated HUD gates cannot suppress its callback.

        // InventoryDlg is an independent automatic OnGUI root. Its original body reads
        // native Items and sends commands at the click; no host Draw or intent pump remains.
        // EquipDlg is an independent automatic OnGUI root. Its live native controls
        // start commands at their original call sites, independently of hosted HUD gates.

        // DealDlg owns its automatic Update/OnGUI and sends through the existing native
        // NetClient command bindings at the click. The map only observes its state.

        // Gossip native owners now send and open menus through their original callbacks.

        // Control shortcuts are drawn by the recovered control branch above the button;
        // they do not also enqueue a floating UITip request.
        // HUD owners submit to UITip at their native hover sites, before later windows.
        // Only the still-hosted non-HUD requesters remain in this compatibility aggregate.
        string tipText = string.Empty;
        string tipComparison = string.Empty;
        if (!string.IsNullOrEmpty(chatUi.HoverTooltip))
        {
            tipText = chatUi.HoverTooltip;
        }

        if (!string.IsNullOrEmpty(gossipDialog.HoverTooltip))
        {
            tipText = gossipDialog.HoverTooltip;
        }

        if (!string.IsNullOrEmpty(localEquipmentDialog.HoverTooltip))
        {
            tipText = localEquipmentDialog.HoverTooltip;
            tipComparison = string.Empty;
        }

        if (!string.IsNullOrEmpty(dealDialog.HoverTooltip))
        {
            tipText = dealDialog.HoverTooltip;
            tipComparison = string.Empty;
        }

        // Inventory publishes directly to the original UITip/PetInfoTooltip owners.
        if (!string.IsNullOrEmpty(tipText))
        {
            bool requested = string.IsNullOrEmpty(tipComparison)
                ? uiTip.Request(tipText, scale)
                : uiTip.RequestComparison(tipText, tipComparison, scale);
            if (!requested)
            {
                return false;
            }
        }

        bool tipDrawn = uiTip.DrawRequested();
        return windowsReady && tipDrawn;
    }

    /// <summary>
    /// Routes only source-recovered PlayerActionMenu destinations to their real UI owner.
    /// </summary>
    /// <remarks>
    /// Unrecovered destinations remain recorded intents and are not converted into network
    /// success. Action 0 writes only a trade invitation, actions 3/4 open their recovered chat
    /// owners, actions 14/25 route only to local follow navigation, and actions 50..52 route only
    /// to the recovered local clipboard/ChatCopyOverlay owners. None invents a server-side result.
    /// </remarks>
    // Retained compatibility entry for historical host callers/audits. The actual shared
    // PlayerActionMenu dispatches its original actions itself and never routes through here.
    public bool ApplyPlayerActionMenuDestination(
        ReferencePlayerActionMenuAction action,
        string targetName)
    {
        return ApplyPlayerActionMenuDestination(action, 0, targetName);
    }

    /// <summary>
    /// Routes source-recovered destinations that require the selected entity identity.
    /// </summary>
    public bool ApplyPlayerActionMenuDestination(
        ReferencePlayerActionMenuAction action,
        int targetEntityId,
        string targetName)
    {
        return ApplyPlayerActionMenuDestination(action, targetEntityId, targetName, null);
    }

    private bool ApplyPlayerActionMenuDestination(
        ReferencePlayerActionMenuAction action,
        int targetEntityId,
        string targetName,
        global::FakeFantasyOnline.Presentation.ReferenceAssets.PlayerActionMenu sourceMenu)
    {
        ThrowIfDisposed();
        if (action == ReferencePlayerActionMenuAction.BrowseStall)
        {
            ReferenceFirstWorldPlayerActionMenu.FireBrowseStall(targetEntityId);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.PartyInvite)
        {
            _ = session.RequestPartyInviteAsync(targetEntityId, targetName, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.PartyCreate)
        {
            TeamConfigOpenRequested = true;
            TeamConfigDefaultName = (string.IsNullOrEmpty(character?.DisplayName)
                ? "我" : character.DisplayName) + "的小队";
            TeamConfigInviteTargetId = targetEntityId;
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.FamilyInvite)
        {
            _ = session.RequestFamilyInviteAsync(targetEntityId, targetName, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.DuelArena)
        {
            DuelOpenRequested = true;
            DuelTargetEntityId = targetEntityId;
            DuelTargetName = targetName ?? string.Empty;
            duelDialog?.Open(targetEntityId, targetName, 0);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.RideTogether)
        {
            _ = session.RequestRideTogetherAsync(targetEntityId, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.DismountTogether)
        {
            _ = session.RequestRideTogetherAsync(0, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.RideInvite)
        {
            _ = session.RidingCommands.InviteAsync(targetEntityId, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.PartyKick)
        {
            _ = session.RequestPartyKickAsync(targetEntityId, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.PartyLeader)
        {
            _ = session.RequestPartyLeaderAsync(targetEntityId, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.PartyJoin)
        {
            _ = session.RequestPartyJoinAsync(targetEntityId, targetName, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.Apprentice ||
            action == ReferencePlayerActionMenuAction.Mentor)
        {
            byte role = action == ReferencePlayerActionMenuAction.Apprentice ? (byte)0 : (byte)1;
            _ = session.RequestApprenticeAsync(targetEntityId, role, targetName, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.ApprenticeDissolve)
        {
            IApprenticeCommands commands = session.ApprenticeCommands;
            SysMsgBox.Show(
                "确定解除与【" + (targetName ?? string.Empty) + "】的师徒关系吗？",
                () =>
                {
                    // The native confirmation reads NetClient.I when invoked. SysMsgBox can
                    // survive a map rebuild, so it must not borrow the old map's chat lifetime.
                    if ((object)NetClient.I != null)
                        _ = commands.RequestAsync(
                            (object)sourceMenu != null ? sourceMenu.TargetIdForIndependentRuntime : targetEntityId,
                            2, targetName, CancellationToken.None);
                },
                null);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.FriendAdd)
        {
            _ = session.RequestFriendAddAsync(targetName, chatLifetime.Token);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.FriendDelete)
        {
            IFriendCommands commands = session.FriendCommands;
            SysMsgBox.Show(
                "确定删除好友【" + (targetName ?? string.Empty) + "】吗？",
                () =>
                {
                    if ((object)NetClient.I != null)
                        _ = commands.DeleteAsync(targetName, CancellationToken.None);
                },
                null);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.BlockRemove)
        {
            // Official Fire captures the first matching block's persistent character id;
            // targetEntityId is the transient world entity id and must not be used here.
            long blockedCharacterId = 0;
            foreach (var entry in session.BlockState.Entries)
            {
                if (entry.Item2 != targetName) continue;
                blockedCharacterId = entry.Item1;
                break;
            }
            if (blockedCharacterId == 0) return true;
            IBlockCommands commands = session.BlockCommands;
            SysMsgBox.Show(
                "解除对【" + targetName + "】的屏蔽？\n解除后又能收到他的聊天和各种请求。",
                () =>
                {
                    if ((object)NetClient.I != null)
                        _ = commands.DeleteAsync(blockedCharacterId, CancellationToken.None);
                },
                null);
            return true;
        }
        if (action == ReferencePlayerActionMenuAction.BlockChat ||
            action == ReferencePlayerActionMenuAction.BlockParty)
        {
            IBlockCommands commands = session.BlockCommands;
            byte scope = action == ReferencePlayerActionMenuAction.BlockChat ? (byte)0 : (byte)1;
            string suffix = scope == 0
                ? "】？\n他的世界/附近/队伍/家族发言、私聊、组队交易等请求、邮件都不会再打扰你。\n(喇叭广播是付费道具, 仍然会收到)\n对方不会收到任何提示。"
                : "】的所有角色？\n他换成同一个账号下的任何角色上线，都不会再打扰你。\n(喇叭广播是付费道具, 仍然会收到)\n对方不会收到任何提示。";
            SysMsgBox.Show(
                "屏蔽【" + targetName + suffix,
                () =>
                {
                    if ((object)NetClient.I != null)
                        _ = commands.AddAsync(targetName, scope, CancellationToken.None);
                },
                null);
            return true;
        }
        if (!built || chatUi == null || !chatUi.Ready ||
            gossipDialog == null || !gossipDialog.Ready ||
            inspectedEquipmentDialog == null || !inspectedEquipmentDialog.Ready)
        {
            return false;
        }

        if (action == ReferencePlayerActionMenuAction.Whisper)
        {
            return chatUi.StartWhisper(targetName);
        }

        if (action == ReferencePlayerActionMenuAction.Gossip)
        {
            return gossipDialog.Open(targetName);
        }

        if (action == ReferencePlayerActionMenuAction.Trade)
        {
            if (!ObserveTradeRequest())
            {
                return false;
            }

            if (NetClient.I == null || tradeRequestLifetime == null)
            {
                return true;
            }

            if (pendingTradeRequest != null && !pendingTradeRequest.IsCompleted)
            {
                return true;
            }

            pendingTradeRequest = session.RequestTradeAsync(
                targetEntityId,
                tradeRequestLifetime.IsCancellationRequested
                    ? tradeRequestLifetime.Token : CancellationToken.None);
            return ObserveTradeRequest();
        }

        if (action == ReferencePlayerActionMenuAction.FollowPlayer)
        {
            return frame != null && frame.ToggleFollowPlayer(targetEntityId);
        }

        if (action == ReferencePlayerActionMenuAction.FollowLeader)
        {
            WorldPartyState party = session.PartyState ?? WorldPartyState.Empty;
            return party.LeaderEntityId == 0 ||
                (frame != null && frame.ToggleFollowLeader(party.LeaderEntityId));
        }

        if (action == ReferencePlayerActionMenuAction.Inspect)
        {
            if (NetClient.I == null || inspectLifetime == null)
            {
                return true;
            }

            if (pendingInspectRequest != null && !pendingInspectRequest.IsCompleted)
            {
                return true;
            }

            pendingInspectRequest = session.InspectPlayerAsync(
                targetEntityId,
                inspectLifetime.IsCancellationRequested
                    ? inspectLifetime.Token : CancellationToken.None);
            return true;
        }

        return true;
    }

    private bool ApplyPlayerActionMenuDestination(
        ReferencePlayerActionMenuAction action,
        int targetEntityId,
        string targetName,
        string copyLine,
        string copyBlock,
        global::FakeFantasyOnline.Presentation.ReferenceAssets.PlayerActionMenu sourceMenu = null)
    {
        if (action == ReferencePlayerActionMenuAction.CopyLine ||
            action == ReferencePlayerActionMenuAction.CopyBlock ||
            action == ReferencePlayerActionMenuAction.SelectCopy)
        {
            return chatUi != null && uiTip != null &&
                chatUi.ApplyPlayerActionCopyDestination(
                    action,
                    targetName,
                    copyLine,
                    copyBlock,
                    uiTip);
        }

        return ApplyPlayerActionMenuDestination(action, targetEntityId, targetName, sourceMenu);
    }

    public void UpdateMailAndDuelShortcuts()
    {
        UIManager.UpdateMailAndDuelShortcutsForIndependentRuntime(
            chatUi, gossipDialog, () => mailDialog?.Toggle(), () => duelDialog?.Toggle());
    }

    // One thin host entry for the adjacent late slices of UIManager.Update: Insert, then V. The
    // ClientRootView driver invokes it after F12 and before the two minimap zoom keys.
    public void UpdateInsertAndChannelShortcuts()
    {
        UIManager.UpdateHeadHpBarForIndependentRuntime(chatUi, gossipDialog);
        UIManager.UpdateChannelListShortcutForIndependentRuntime(chatUi, gossipDialog);
    }

    public void ToggleMapDiagram()
    {
        ThrowIfDisposed();
        if (built && minimapHud != null)
        {
            minimapHud.ToggleDiagram();
        }
    }

    /// <summary>
    /// Applies the complete <c>UiEsc.CloseTop</c> body, then the ordinary SysOption / ArenaEsc
    /// tail only when no native slot handles the key. The generic UIManager stack retains its
    /// separate lifecycle boundary. This is the single per-frame Escape host: ClientRootView calls it once per pressed
    /// Escape and it closes at most one window.
    /// </summary>
    public ReferenceFirstWorldEscapeAction HandleEscape()
    {
        ThrowIfDisposed();
        if (!built)
        {
            LastEscapeAction = ReferenceFirstWorldEscapeAction.None;
            return LastEscapeAction;
        }
        if (CloseUiEscTopForIndependentRuntime())
        {
            return LastEscapeAction;
        }
        ApplyUIManagerEscapeTailForIndependentRuntime();
        return LastEscapeAction;
    }

    /// <summary>Calls the complete original body; the host only records its completed action.</summary>
    private bool CloseUiEscTopForIndependentRuntime()
    {
        ThrowIfDisposed();
        bool handled = UiEsc.CloseTop();
        LastEscapeAction = ReferenceFirstWorldEscapeDispatch.LastAction;
        if (LastEscapeAction == ReferenceFirstWorldEscapeAction.TeamDialog)
            TeamDialogOpenRequested = false;
        return handled;
    }

    /// <summary>
    /// The <c>UIManager.Update</c> (0x06000fa8) tail, not a <c>UiEsc.CloseTop</c> slot: it runs only
    /// after the reviewed UiEsc order closed nothing. It stays outside
    /// <see cref="CloseUiEscTopForIndependentRuntime"/> so Update - which performs this same tail
    /// itself after that call - cannot toggle the ordinary menu twice for one press.
    /// </summary>
    private void ApplyUIManagerEscapeTailForIndependentRuntime()
    {
        // Reached only after all 75 original slots (and approved FamilyMail) returned false.
        // In particular, the original final SysOption close has already been tested by UiEsc.
        if (ArenaEscDlg.ShouldTakeOver)
        {
            ArenaEscDlg.Toggle();
            LastEscapeAction = ReferenceFirstWorldEscapeAction.ArenaEsc;
            return;
        }
        SysOptionDlg.Toggle();
        LastEscapeAction = ReferenceFirstWorldEscapeAction.SystemMenu;
    }

    private bool PrepareNativeHudViewsForIndependentRuntime()
    {
        nativeHud = GameHud.I;
        if (nativeHud == null) nativeHud = new GameObject("GameHud").AddComponent<GameHud>();
        borrowedNativeHudViews = true;
        if (!nativeHud.InitializeGuiViewsForIndependentRuntime(resources, character, out var hudFailure))
        {
            LastFailure = hudFailure;
            return false;
        }
        hudResources = nativeHud.GuiResourcesForIndependentRuntime;
        playerStatusHud = nativeHud.PlayerStatusForIndependentRuntime;
        controlBar = nativeHud.ControlsForIndependentRuntime;
        targetHud = nativeHud.TargetForIndependentRuntime;
        buffHud = nativeHud.BuffsForIndependentRuntime;
        minimapHud = nativeHud.MinimapForIndependentRuntime;
        hotbar = nativeHud.HotbarForIndependentRuntime;
        return true;
    }

    private void BindHudWindowsForIndependentRuntime(GameHud hud)
    {
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Skills, () => Win05.I != null && Win05.I.MouseOver());
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Task, TaskDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Horn, HornDialog.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Rename, RenameDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.HairSalon, () => HairSalonDlg.Active);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.PetArousal, PetArousalDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.AccountSec, AccountSecDlg.MouseOver);
        // Official GameHud.MouseOverHud (06000417) asks ComposeDlg.get_Visible (06000181) and
        // SculpDlg.get_Visible (06000195), not a shaped MouseOver: while either workshop window is
        // open the aggregate hit test owns the pointer everywhere, not only over its own rectangle.
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Compose, () => ComposeDlg.Visible);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Sculp, () => SculpDlg.Visible);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Store, StoreDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.MoneyInput, MoneyInputDlg.MouseOver);
        // Original GameHud.MouseOverHud uses Mail visibility, and geometry for these other windows.
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Mail, () => MailDlg.Visible);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.RackShop, RackShopDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Duel, DuelDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Team, TeamDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.TeamBar, TeamBar.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.TeamConfig, TeamConfigDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.FriendConfig, FriendConfigDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.MasterInfo, MasterInfoDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.MailConfig, MailConfigDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.MailResist, MailResistDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Family, FamilyDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.FamilyCreate, FamilyCreateDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.FamilyDetail, FamilyDetailDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.FamilyRelation, FamilyRelationDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.JobCustom, JobCustomDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.JobOffer, JobOfferDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.JobRight, JobRightDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.SystemInfo, FakeFantasyOnline.Presentation.ReferenceAssets.SystemInfoBar.MouseOver);
        // Official GameHud.MouseOverHud (06000417) reaches these windows as well. The arm64 and
        // x86_64 bodies emit the same address-ordered call sequence for GarageDlg (06000ff4),
        // PasswdDlg (06001000), FriendInfoDlg (06000d36), GroceryConfigDlg (0600047f),
        // GroceryDetailDlg (06000497), QueryDlg (060001a8), SelectPopupDlg (060001af),
        // FigureDlg (06000a85), ReportDlg (06000a7e), VoiceSetDlg (06000a77), ColorDlg (06000a64),
        // FaceDlg (06000a6b), FindModeDlg (06000a71), MasterListDlg (060005c6) and DifficultyBox
        // (0600026a, the final returned term), so each one answers the slot the aggregate chain
        // already reserved for it.
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Garage, GarageDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Passwd, PasswdDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.FriendInfo, FriendInfoDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.GroceryConfig, GroceryConfigDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.GroceryDetail, GroceryDetailDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Query, QueryDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.SelectPopup, SelectPopupDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Figure, FigureDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Report, ReportDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.VoiceSet, VoiceSetDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Color, ColorDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Face, FaceDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.FindMode, FindModeDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.MasterList, MasterListDlg.MouseOver);
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Difficulty, DifficultyBox.MouseOver);
        // Official GameHud.MouseOverHud (06000417) tests WorldMapDlg.get_Visible (0600108f) as its
        // first term, before every geometry test, so the whole dialog - not a shaped rectangle -
        // owns the pointer while it is open.
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.WorldMap, () => WorldMapDlg.Visible);
        // HudWindowSlot.Trade is the official GameHud.MouseOverHud (06000417) caller of
        // TradeDlg.MouseOver (06000f62); the window body it hit-tests is recovered, so the slot now
        // answers from that real member instead of staying unbound.
        hud.BindWindowHitForIndependentRuntime(HudWindowSlot.Trade, TradeDlg.MouseOver);
        // HudWindowSlot.Help would duplicate the direct HelpWinDlg.MouseOver() the chain already
        // performs at that position.
    }

    // The original OverAnyUI owner already reaches the currently bound native HUD/window
    // consumers. The old wrapper union duplicated those terms and added unsupported world
    // gates (notably FamilyMail and Revive). Only the explicit diagnostic rectangle is extra.
    private bool IsOverExistingWorldUiForIndependentRuntime(PlayerController controller, Vector2 topOriginPointer, float scale, Rect externalDiagnosticRect)
    {
        if (externalDiagnosticRect.Contains(topOriginPointer)) return true;
        PlayerController owner = controller != null ? controller : PlayerController.I;
        return owner != null && owner.OverAnyUI();
    }

    private void ConfirmWorkCancellationForIndependentRuntime(PlayerController controller)
    {
        if (!Input.GetMouseButtonDown(1) || systemInfoBar.MessageBoxVisible) return;
        Vector2 point = new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y);
        // Official PlayerController.Update (0x06000a00) work branch enters the prompt with exactly
        // three terms: GetMouseButtonDown(1), !SysMsgBox.get_Visible (0x06000e1b) and
        // !PlayerController.OverAnyUI (0x060009ff) - arm64 :360-363 / x86_64 :375-378, immediately
        // after UpdateWork (0x060009dd). systemInfoBar.MessageBoxVisible is the host's name for
        // SysMsgBox.Visible, so the three-term door is the official one.
        // WB-141 correction: the frozen OverAnyUI body does carry a SkillTargeting term - metadata
        // usage 02410 (TypeInfo:SkillTargeting) static +0x0, i.e. <Active>k__BackingField - so the
        // previous "no SkillTargeting term" note was wrong and the door now keeps it: a right click
        // made while a skill is being aimed no longer opens the 是否要结束打工？ prompt.
        if (IsOverExistingWorldUiForIndependentRuntime(controller, point, UIScale.Factor, new Rect())) return;
        systemInfoBar.SharedMessageBox.Show("是否要结束打工？", controller.CancelWork, null);
    }

    // Retained for explicit diagnostic pointer callers only. The actual SkillTargeting
    // scene component receives its own Unity Update, independent of this optional window.
    internal void AdvanceSkillTargetingForIndependentRuntime()
    {
        ThrowIfDisposed();
        SkillTargeting skillTargeting = SkillTargeting.InstanceForIndependentRuntime;
        if (skillTargeting != null)
        {
            try { skillTargeting.AdvanceForIndependentRuntime(); }
            finally { frame?.Player?.ObserveNativeFacingForIndependentRuntime(); }
        }
    }

    /// <summary>
    /// Applies the recovered GameHud/PlayerController world-pointer ownership for this frame.
    /// </summary>
    /// <remarks>
    /// A hostile-monster primary selection starts the source-owned normal-attack command owner.
    /// Damage, hit points, death and rewards remain authoritative server outcomes.
    /// </remarks>
    public bool HandlePointerInput(
        Vector3 screenPosition,
        bool leftButtonDown,
        bool leftButtonHeld,
        bool leftButtonUp,
        bool rightButtonDown,
        bool rightButtonUp,
        float unscaledTime,
        float worldTime,
        Rect externalDiagnosticRect,
        bool advanceSkillTargeting = true,
        bool readLiveInput = false)
    {
        // Inputs are read where the recovered branch consumes them. Explicit callers use
        // the same branch body with their supplied values; the live entry captures no frame replay.
        bool LeftDown() => readLiveInput ? Input.GetMouseButtonDown(0) : leftButtonDown;
        bool LeftHeld() => readLiveInput ? Input.GetMouseButton(0) : leftButtonHeld;
        bool LeftUp() => readLiveInput ? Input.GetMouseButtonUp(0) : leftButtonUp;
        bool RightDown() => readLiveInput ? Input.GetMouseButtonDown(1) : rightButtonDown;
        bool RightUp() => readLiveInput ? Input.GetMouseButtonUp(1) : rightButtonUp;
        float WorldTime() => readLiveInput ? Time.time : worldTime;
        Vector3 ScreenPoint() => readLiveInput ? Input.mousePosition : screenPosition;

        ThrowIfDisposed();
        LastPointerAction = ReferenceFirstWorldPointerAction.None;
        LastPointerPickedTargetKind = "None";
        LastPointerPickedTargetId = 0;
        LastPointerScreenPosition = Vector3.zero;
        LastPointerWorldPosition = Vector3.zero;
        LastPointerMapPosition = Vector2.zero;
        LastPointerOverHud = false;
        bool targetingOwnedFrame = SkillTargeting.Active;
        if (advanceSkillTargeting) AdvanceSkillTargetingForIndependentRuntime();
        if (!built || frame == null ||
            (!readLiveInput && (float.IsNaN(unscaledTime) || float.IsInfinity(unscaledTime) ||
            float.IsNaN(screenPosition.x) || float.IsInfinity(screenPosition.x) ||
            float.IsNaN(screenPosition.y) || float.IsInfinity(screenPosition.y) ||
            !IsNonNegativeFinite(worldTime))))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.PointerInputRejected;
            return false;
        }

        // A missing camera skips this region in the native Update; the caller still advances
        // chant/approach/movement. It is not a failed world or a successful pointer selection.
        if (Camera == null)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.None;
            return true;
        }

        PlayerController pointerController = frame.Player.Root.GetComponent<PlayerController>();
        bool playerPointerEligible = pointerController != null &&
            pointerController.CanHandleWorldPointerForIndependentRuntime(
                session.LocalRidingState?.RideAnchorId ?? 0);
        // The same aggregate as the sitting binding: the maximum raw byte is non-zero exactly
        // when some row of the last 0x8017 carried one, dropped rows included.
        if (CharData.immobile != 0) playerPointerEligible = false;

        float scale = UIScale.Factor;
        Vector2 topOriginPointer = new Vector2(
            ScreenPoint().x,
            Screen.height - ScreenPoint().y);
        bool overHud = IsOverExistingWorldUiForIndependentRuntime(pointerController, topOriginPointer, scale, externalDiagnosticRect);
        Vector3 projectedScreen = ScreenPoint();
        LastPointerScreenPosition = projectedScreen;
        Vector3 world = Camera.ScreenToWorldPoint(projectedScreen);
        // PlayerController.Update synchronizes moved Collider2D transforms here,
        // before its overlap/pick queries, including standing NPC animation frames.
        Physics2D.SyncTransforms();
        LastPointerWorldPosition = world;
        CameraFollow cameraFollow = frame.CameraFollow;
        if (cameraFollow == null || !(cameraFollow.PixelsPerUnit > 0f))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidManualMovement;
            return false;
        }

        Vector2 targetMap;
        if (!ReferenceWorldPointerProjection.TrySceneToMap(
                world,
                cameraFollow.PixelsPerUnit,
                cameraFollow.WorldHalfWidth,
                cameraFollow.WorldHalfHeight,
                out targetMap))
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidManualMovement;
            return false;
        }

        targetMap.x = Mathf.Clamp(
            targetMap.x,
            0f,
            cameraFollow.WorldHalfWidth * cameraFollow.PixelsPerUnit * 2f);
        targetMap.y = Mathf.Clamp(
            targetMap.y,
            0f,
            cameraFollow.WorldHalfHeight * cameraFollow.PixelsPerUnit * 2f);
        LastPointerMapPosition = targetMap;
        if (LeftDown() && playerPointerEligible)
            pointerController.LatchPrimaryPressForIndependentRuntime(overHud || targetingOwnedFrame);

        if (playerPointerEligible && (overHud || targetingOwnedFrame) && CursorManager.I != null)
        {
            CursorManager.I.SetAttackHover(-2);
            CursorManager.I.SetGatherHover(0);
        }

        if (targetingOwnedFrame)
        {
            LastPointerOverHud = overHud;
            LastPointerPickedTargetKind = overHud ? "Hud" : "SkillTargeting";
            if (!playerPointerEligible)
            {
                LastFailure = ReferenceFirstWorldWindowFailure.None;
                return true;
            }
            return CompleteLocalPointerFrameForIndependentRuntime(pointerController, null, true, targetMap);
        }

        // SkillTargeting has its own Update and does not inherit PlayerController's early exits.
        if (!playerPointerEligible)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.None;
            return true;
        }

        if (overHud)
        {
            LastPointerPickedTargetKind = "Hud";
            LastPointerOverHud = true;
            ReferenceWorldManualMovementPlan uiPlan;
            if (!manualMovementController.TryAdvance(
                    LeftDown(),
                    LeftHeld(),
                    LeftUp(),
                    true,
                    false,
                    false,
                    false,
                    false,
                    frame.Player.MovementActive,
                    frame.Player.MovementPathCount,
                    WorldTime(),
                    targetMap.x,
                    targetMap.y,
                    out uiPlan, advanceFollowTail: false) || !ApplyManualMovementPlan(uiPlan))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidManualMovement;
                return false;
            }

            return CompleteLocalPointerFrameForIndependentRuntime(pointerController, null, true, targetMap);
        }

        if (StallConfigDlg.LocalStalling)
        {
            if (LeftDown()) pointerController.BrowseOwnStallAtForIndependentRuntime(new Vector2(world.x, world.y));
            ReferenceWorldManualMovementPlan stallPlan;
            if (!manualMovementController.TryAdvance(LeftDown(), LeftHeld(), LeftUp(),
                    false, false, false, false, false, frame.Player.MovementActive,
                    frame.Player.MovementPathCount, WorldTime(), targetMap.x, targetMap.y,
                    out stallPlan, processWorldInput: false, advanceFollowTail: false) || !ApplyManualMovementPlan(stallPlan))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidManualMovement;
                return false;
            }
            return CompleteLocalPointerFrameForIndependentRuntime(pointerController, null, false, targetMap);
        }

        // ScreenToWorldPoint returns Unity scene units. PlayerController.SetPathTo consumes the
        // original top-left map-pixel space, so invert the exact projection used by every actor.
        // Passing scene units through here made ordinary clicks target roughly (-70, 35) instead
        // of the visible point on a 14464x7232 map, leaving the player apparently immobile.
        float targetMapX = targetMap.x;
        float targetMapY = targetMap.y;

        ReferenceWorldPointerTargetKind gatherPickKind;
        int gatherPickId;
        ISelectable selected = pointerController.PickSelectableForIndependentRuntime(new Vector2(world.x, world.y));
        bool gatherPicked = frame.DescribePointerTargetForIndependentRuntime(selected, out gatherPickKind, out gatherPickId);
        MonsterEntity selectedMonster = selected as MonsterEntity;
        MonsterEntity attackableMonster = selectedMonster;
        if (attackableMonster != null && attackableMonster.NoAttack) attackableMonster = null;
        NpcInteract pointerNpc = selected as NpcInteract;
        RemotePlayer pointerRemote = selected as RemotePlayer;
        bool hostileRemote = pointerRemote != null && pointerRemote.HostileToLocal;
        bool selectedAttackable = attackableMonster != null || hostileRemote;
        if (LeftDown()) pointerController.MouseDownOverMobForIndependentRuntime = selectedAttackable;
        StallSignClick stallSign = pointerController.PickStallSignForIndependentRuntime(
            new Vector2(world.x, world.y), pointerNpc != null);
        if (LeftDown() && stallSign != null) pointerController.MouseDownOverSignForIndependentRuntime = true;
        // This owner publishes attack and NPC hover; CursorManager only consumes the values.
        if (CursorManager.I != null)
        {
            CursorManager attackCursor = CursorManager.I;
            int attackKind = attackableMonster != null
                ? Math.Sign(unchecked(attackableMonster.level - CharData.level))
                : hostileRemote || CombatWorld.AwaitingTarget ? 0 : -2;
            attackCursor.SetAttackHover(attackKind);
        }
        bool gatherAllowed = selected == null || selected is PlayerController ||
            (selectedMonster != null && selectedMonster.friendly);
        int gatherKind = pointerController.GatherKindForIndependentRuntime(targetMapX, targetMapY, gatherAllowed);
        if (CursorManager.I != null) CursorManager.I.SetGatherHover(gatherKind);
        bool workTipOpened = gatherKind != 0 && LeftDown() && !pointerController.MouseDownOverUIForIndependentRuntime;
        if (workTipOpened)
        {
            pointerController.OpenWorkTipForIndependentRuntime(gatherKind);
        }

        bool browsedStall = !workTipOpened && LeftDown() && stallSign != null &&
            !pointerController.MouseDownOverUIForIndependentRuntime;
        if (browsedStall) pointerController.BrowseStallForIndependentRuntime(stallSign);

        ReferenceWorldPointerTargetKind kind = gatherPickKind;
        int entityId = gatherPickId;
        bool picked = gatherPicked;
        if (picked)
        {
            LastPointerPickedTargetKind = kind.ToString();
            LastPointerPickedTargetId = entityId;
        }

        bool directLocalLeft = LeftDown() && !pointerController.MouseDownOverUIForIndependentRuntime &&
            (pointerNpc != null || attackableMonster != null);
        if (!workTipOpened && !browsedStall && !directLocalLeft)
        {
            bool downOverMob = LeftDown() && selectedAttackable;
            ReferenceWorldManualMovementPlan movementPlan;
            if (!manualMovementController.TryAdvance(
                    LeftDown(),
                    LeftHeld(),
                    LeftUp(),
                    false,
                    pointerNpc != null,
                    attackableMonster != null,
                    downOverMob,
                    LeftDown() && stallSign != null,
                    frame.Player.MovementActive,
                    frame.Player.MovementPathCount,
                    WorldTime(),
                    targetMapX,
                    targetMapY,
                    out movementPlan, advanceFollowTail: false) ||
                !ApplyManualMovementPlan(movementPlan))
            {
                LastFailure = ReferenceFirstWorldWindowFailure.InvalidManualMovement;
                return false;
            }
        }

        if (!browsedStall && LeftDown() && !workTipOpened &&
            !pointerController.MouseDownOverUIForIndependentRuntime)
        {
            if (pointerNpc != null)
            {
                pointerNpc.Show();
                pointerController.StopForNpcPointerForIndependentRuntime();
            }
            else
            {
                if (attackableMonster != null)
                    pointerController.ApproachMonsterPointerForIndependentRuntime(targetMapX, targetMapY);
                // Live MonsterEntity and RemotePlayer callbacks belong to Unity and do not
                // inherit this pointer branch's gates. Explicit diagnostic input retains its
                // direct entry; ordinary frame observations adopt the eventual native selection.
                if (!readLiveInput && selectedMonster != null)
                    TryApplyLeftSelection(kind, entityId, selectedMonster);
            }
        }

        // Official PlayerController.Update (0x06000a00) latches the short right click inside the
        // same guarded block that later runs the release arm. A press that begins on UI leaves
        // the previous latch unchanged; a later release still tests that retained latch.
        // The block is entered only
        // when the one PlayerController.OverAnyUI (0x060009ff) value read at arm64 :762 /
        // x86_64 :762 is false - arm64 :910 / x86_64 :907 - and only while StallConfigDlg.Active
        // is false (TypeInfo:StallConfigDlg static +0x40, metadata usage 02445; arm64 :911-915 /
        // x86_64 :911). GetMouseButtonDown(1) at arm64 :1371 / x86_64 :1321 writes +0x1a8 inside
        // that block and nowhere else in the 13404-byte body, so the latch and the action share
        // one gate. overHud is the same aggregate the left-button branches already use; no new
        // guard is added and no click reaches the world that the frozen body would refuse.
        // The dominating UI/stall branches above already selected this native region.
        // Do not resample stall state after the preceding left-click callbacks.
        if (RightDown())
        {
            if (readLiveInput) rightClickTracker.Press();
            else rightClickTracker.Press(WorldTime(), ScreenPoint());
        }

        bool shortRightClick = RightUp() &&
            (readLiveInput ? rightClickTracker.Release() :
                rightClickTracker.Release(WorldTime(), ScreenPoint()));
        // The original right-click region follows every successful left branch. It is
        // independent of left-held/up, and its unchanged action is followed by the common tail.
        if (!shortRightClick || PlayerActionMenu.Visible)
            return CompleteLocalPointerFrameForIndependentRuntime(pointerController, pointerNpc, false, targetMap);

        // Official PlayerController.Update short right-click AwaitingTarget arm (0x06000a00, arm64
        // :1425-1431 / x86_64 :1368-1374). CombatWorld.get_AwaitingTarget (0x06000150) is read once
        // and only its false arm reaches PlayerController.PickSelectable (0x060009fb): a click that
        // cancels a pending cast never also picks, selects, clears or moves, and the two arms are
        // the two sides of one test, not two separate paths. The true arm is two calls in this
        // order - CombatWorld.CancelPending (0x06000164) first, then UITip.Show (0x06000fbc) - with
        // no NetClient, UseSkill, Attack or SetPathTo call in between or after, so nothing is sent.
        // The text is the 1.5.8 string literal 6854 "取消释放": metadata usage ordinal 17184, kind 5,
        // decode_state exact, and the same usage slot is what both frozen exports load (arm64
        // 0x14a91a0 through adrp/add at 0x2afa48-0x2afa4c then ldr x0 at 0x2afa50; x86_64 0x149d618
        // through lea rax at 0x2bfbc6 then mov rdi at 0x2bfbc9). The scale is 0.8f: arm64 movz/movk
        // 0x3f4ccccd at 0x2afa54-0x2afa58 into fmov s0 at 0x2afa5c, x86_64 movss xmm0 from the 0.8f
        // constant at 0x2bfbcc - so text is the first parameter and the scale the second, matching
        // the official signature UITip.Show(System.String, System.Single) and its zero method slot.
        // CancelPending is also the whole field cleanup: it clears pendSkillId, queuedSkillId and
        // pendGroundSkill and writes no target, pvpTarget, autoAttacking or attack phase, so no
        // second clearing is added here and the ESC path is not borrowed.
        if (CombatWorld.AwaitingTarget)
        {
            CombatWorld.CancelPending();
            UITip.Show("取消释放", 0.8f);
            return CompleteLocalPointerFrameForIndependentRuntime(pointerController, pointerNpc, false, targetMap);
        }

        ISelectable rightSelected = pointerController.PickSelectableForIndependentRuntime(new Vector2(world.x, world.y));
        frame.DescribePointerTargetForIndependentRuntime(rightSelected, out kind, out entityId);
        LastPointerPickedTargetKind = kind.ToString();
        LastPointerPickedTargetId = entityId;
        if (rightSelected == null)
        {
            // Official PlayerController.Update short right-click arm (0x06000a00) reaching
            // CombatWorld.ClearTarget (0x0600014d) - the PickSelectable-result-null arm at
            // arm64 :1409 / x86_64 :1352. That static owner is the entire official clear of this
            // arm: target (+0x30), pvpTarget (+0x38), the one 8-byte zero over atkState/atkPhaseT
            // (+0x40/+0x44) and the three pending ids (+0x80/+0x8c/+0x94), in that order, ending in
            // the single TargetSelect.Clear (0x06000e55) that clears TargetSelect.Current and
            // unbinds/hides the TargetRing; it writes no autoAttacking (+0x78) and takes no argument.
            // Only the HUD projection is adopted afterward. Combat diagnostics read the native
            // owner directly; no adapter phase or entity-id cache can write back to it.
            CombatWorld.ClearTarget();
            frame.AdoptOfficialTargetClearForIndependentRuntime();
            LastPointerAction = ReferenceFirstWorldPointerAction.TargetCleared;
            return CompleteLocalPointerFrameForIndependentRuntime(pointerController, pointerNpc, false, targetMap);
        }

        // Keep the exact picked identity. The original right-click branch does not need a
        // presentation actor admission or convert the native void operation into a failure.
        if (rightSelected is MonsterEntity rightMonster && !rightMonster.friendly)
            CombatWorld.SetTarget(rightMonster, false);
        else
            TargetSelect.Select(rightSelected);
        frame.AdoptCurrentPointerSelectionForIndependentRuntime();

        if (kind != ReferenceWorldPointerTargetKind.Npc)
        {
            LastPointerAction = ReferenceFirstWorldPointerAction.TargetSelected;
        }
        return CompleteLocalPointerFrameForIndependentRuntime(pointerController, pointerNpc, false, targetMap);
    }

    // PlayerController.Update's common pointer tail. Local UI/stall and successful interaction
    // branches converge here; genuine adapter failures still propagate at their original site.
    private bool CompleteLocalPointerFrameForIndependentRuntime(
        PlayerController controller, NpcInteract npc, bool overUi, Vector2 targetMap)
    {
        bool emitRing = controller.AdvancePointerFollowTailForIndependentRuntime(npc != null, targetMap.x, targetMap.y);
        if (CursorManager.I != null) CursorManager.I.SetNpcHover(npc != null && !overUi);
        if (emitRing) controller.EmitRing(targetMap.x, targetMap.y);
        LastFailure = ReferenceFirstWorldWindowFailure.None;
        return true;
    }

    public void Dispose() => Dispose(sceneRegistriesReleased: false);

    internal void Dispose(bool sceneRegistriesReleased, bool worldRootReleased = false,
        bool preserveNativePlayerState = false)
    {
        if (disposed)
        {
            return;
        }

        if (!sceneRegistriesReleased)
        {
            PetArousalDlg.CloseForIndependentRuntime();
            AccountSecDlg.CloseNow();
            HairSalonDlg.CloseNow();
        }
        disposed = true;
        releaseEscapeCloseTopBinding?.Invoke();
        ReleaseOwnedState(sceneRegistriesReleased, worldRootReleased, completingDisposal: true,
            preserveNativePlayerState: preserveNativePlayerState);
    }

    private void ReleaseOwnedState(bool sceneRegistriesReleased = false,
        bool worldRootReleased = false, bool completingDisposal = false,
        bool preserveNativePlayerState = false)
    {
        // A bootstrap-owned graph survives a real construction exception, including first
        // entry. Existing explicit rejection/final release still invokes its one Dispose.
        if ((bootstrapOwnsConstructionRoot || transitionOwnsConstructionFailure) &&
            !completingDisposal) return;
        if (borrowedNativeHudViews && !ReferenceEquals(nativeHud, null))
            nativeHud.UnbindGuiHostForIndependentRuntime(this);
        // Final/default disposal closes these windows. The already-cleared ordinary
        // map path only detaches its borrower and preserves their independent owners.
        if (!sceneRegistriesReleased)
        {
            if (ComposeDlg.Visible) ComposeDlg.Toggle();
            if (SculpDlg.Visible) SculpDlg.Toggle();
        }

        TeamConfigOpenRequested = false;
        TeamDialogOpenRequested = false;
        TeamDialogTab = 1;
        TeamConfigDefaultName = string.Empty;
        TeamConfigInviteTargetId = 0;
        // Native map/world retirement does not cancel commands already handed to the
        // send path. Detach these token sources while their pending calls finish; default
        // host disposal keeps its existing cancellation and disposal behavior.
        if (chatLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                chatLifetime.Cancel();
                chatLifetime.Dispose();
            }
            chatLifetime = null;
        }

        if (inspectLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                inspectLifetime.Cancel();
                inspectLifetime.Dispose();
            }
            inspectLifetime = null;
        }

        if (pendingInspectRequest != null && pendingInspectRequest.IsFaulted)
        {
            _ = pendingInspectRequest.Exception;
        }

        pendingInspectRequest = null;

        if (tradeRequestLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                tradeRequestLifetime.Cancel();
                tradeRequestLifetime.Dispose();
            }
            tradeRequestLifetime = null;
        }

        if (pendingTradeRequest != null && pendingTradeRequest.IsFaulted)
        {
            _ = pendingTradeRequest.Exception;
        }

        pendingTradeRequest = null;

        if (systemRequestLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                systemRequestLifetime.Cancel();
                systemRequestLifetime.Dispose();
            }
            systemRequestLifetime = null;
        }

        for (int index = 0; index < pendingSystemRequestResponses.Count; index++)
        {
            Task<Result> pending = pendingSystemRequestResponses[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingSystemRequestResponses.Clear();

        if (inventoryLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                inventoryLifetime.Cancel();
                inventoryLifetime.Dispose();
            }
            inventoryLifetime = null;
        }

        for (int index = 0; index < pendingInventoryCommands.Count; index++)
        {
            Task<Result> pending = pendingInventoryCommands[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingInventoryCommands.Clear();

        if (hotbarLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                hotbarLifetime.Cancel();
                hotbarLifetime.Dispose();
            }
            hotbarLifetime = null;
        }

        for (int index = 0; index < pendingHotbarCommands.Count; index++)
        {
            Task<Result> pending = pendingHotbarCommands[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingHotbarCommands.Clear();

        if (npcTaskLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                npcTaskLifetime.Cancel();
                npcTaskLifetime.Dispose();
            }
            npcTaskLifetime = null;
        }

        for (int index = 0; index < pendingNpcTaskRequests.Count; index++)
        {
            Task<Result> pending = pendingNpcTaskRequests[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingNpcTaskRequests.Clear();
        pendingNpcTaskMutationKinds.Clear();
        pendingNpcTaskAbandonRequests.Clear();

        if (npcFunctionLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                npcFunctionLifetime.Cancel();
                npcFunctionLifetime.Dispose();
            }
            npcFunctionLifetime = null;
        }

        for (int index = 0; index < pendingNpcFunctionRequests.Count; index++)
        {
            Task<Result> pending = pendingNpcFunctionRequests[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }
        pendingNpcFunctionRequests.Clear();

        if (basicAttackLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                basicAttackLifetime.Cancel();
                basicAttackLifetime.Dispose();
            }
            basicAttackLifetime = null;
        }

        for (int index = 0; index < pendingBasicAttackRequests.Count; index++)
        {
            Task<Result> pending = pendingBasicAttackRequests[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingBasicAttackRequests.Clear();

        for (int index = 0; index < pendingReviveChoiceRequests.Count; index++)
        {
            Task<Result> pending = pendingReviveChoiceRequests[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingReviveChoiceRequests.Clear();

        if (groundPickupLifetime != null)
        {
            if (!preserveNativePlayerState)
            {
                groundPickupLifetime.Cancel();
                groundPickupLifetime.Dispose();
            }
            groundPickupLifetime = null;
        }

        for (int index = 0; index < pendingGroundPickupRequests.Count; index++)
        {
            Task<Result> pending = pendingGroundPickupRequests[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingGroundPickupRequests.Clear();
        if (!preserveNativePlayerState)
            frame?.Player?.SourceControllerForIndependentRuntime?.CancelPickup();

        for (int index = 0; index < pendingPositionReports.Count; index++)
        {
            Task<Result> pending = pendingPositionReports[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingPositionReports.Clear();
        if (!preserveNativePlayerState) manualMovementController.Reset();

        for (int index = 0; index < pendingChatSends.Count; index++)
        {
            Task<Result> pending = pendingChatSends[index];
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
            }
        }

        pendingChatSends.Clear();
        LastChatSendFailureCode = string.Empty;
        LastInspectFailureCode = string.Empty;
        LastTradeRequestFailureCode = string.Empty;
        LastSystemRequestResponseFailureCode = string.Empty;
        LastGroundPickupFailureCode = string.Empty;
        LastPositionReportFailureCode = string.Empty;
        LastInventoryCommandFailureCode = string.Empty;
        LastHotbarCommandFailureCode = string.Empty;
        LastNpcTaskFailureCode = string.Empty;
        LastNpcFunctionFailureCode = string.Empty;
        LastBasicAttackFailureCode = string.Empty;
        LastPointerPickedTargetKind = "None";
        LastPointerPickedTargetId = 0;
        LastPointerScreenPosition = Vector3.zero;
        LastPointerWorldPosition = Vector3.zero;
        LastPointerMapPosition = Vector2.zero;
        LastPointerOverHud = false;
        basicAttackRequestIssuedCount = 0;
        basicAttackRequestSucceededCount = 0;
        groundPickupRequestIssuedCount = 0;
        groundPickupRequestSucceededCount = 0;
        npcTaskAcceptRequestIssuedCount = 0;
        npcTaskAcceptRequestSucceededCount = 0;
        npcTaskAbandonRequestIssuedCount = 0;
        npcTaskAbandonRequestSucceededCount = 0;
        npcTaskCompleteRequestIssuedCount = 0;
        npcTaskCompleteRequestSucceededCount = 0;
        npcTaskActionReady = true;
        lastGroundPickupRequestedIdForGroundLoot = 0;
        LastReviveChoiceFailureCode = string.Empty;
        LastWorldMapIntent = false;
        inventoryStateVersion = long.MinValue;
        wardrobeStateVersion = long.MinValue;
        hotbarStateVersion = long.MinValue;
        skillBookStateVersion = long.MinValue;
        lifeSkillBookStateVersion = long.MinValue;
        ResetSkillCastObservationForIndependentRuntime();
        localPlayerStatusEffectsVersion = long.MinValue;
        buffStateVersion = long.MinValue;
        activeTradeStateVersion = long.MinValue;
        taskLogStateVersion = 0;
        familyStashStateVersion = long.MinValue;
        helpTopicsStateVersion = long.MinValue;
        friendStateVersion = long.MinValue;
        apprenticeStateVersion = long.MinValue;
        blockStateVersion = long.MinValue;
        partyStateVersion = long.MinValue;
        DuelOpenRequested = false;
        DuelTargetEntityId = 0;
        DuelTargetName = string.Empty;
        npcDialogTextEdgeWindowVersion = 0;
        npcDialogTextEdgeCursor = 0;
#if UNITY_EDITOR
        pendingNewbieTipEdges.Clear();
#endif
        localPlayerLifeStateVersion = long.MinValue;
        lastWorldRevisionFailure = ReferenceWorldRevisionFailure.None;
        worldRevisionRejectionCount = 0;

        if (dealDialog != null)
        {
            if (gameBootstrap == null || !ReferenceEquals(dealDialog, gameBootstrap.PersistentDealForIndependentRuntime))
                dealDialog.Dispose(detachOnly: !ReferenceEquals(gameBootstrap, null));
            // This map borrows persistent windows; it never retains them after release.
            dealDialog = null;
        }

        // Ordinary maps borrow the four receipt-connected DDOL owners, then return drawing.
        FamilyDlg.UnbindGuiDriverForIndependentRuntime(this);
        if (teamDialog != null)
        {
            teamDialog.Dispose();
            teamDialog = null;
        }
        if (duelDialog != null)
        {
            if (ownsDuelDialog) duelDialog.Dispose();
            duelDialog = null;
            ownsDuelDialog = false;
        }
        if (rackShopDialog != null)
        {
            if (gameBootstrap == null) rackShopDialog.Dispose();
            // Ordinary map release only detaches the single GUI borrower.
            rackShopDialog = null;
        }
        if (mailDialog != null)
        {
            if (ownsMailDialog) mailDialog.Dispose();
            else mailDialog.DetachWindow();
            mailDialog = null;
            ownsMailDialog = false;
        }

        if (dealingMarks != null)
        {
            dealingMarks.Dispose();
            dealingMarks = null;
        }

        if (localEquipmentDialog != null)
        {
            localEquipmentDialog.Dispose();
            localEquipmentDialog = null;
        }

        if (inventoryDialog != null)
        {
            inventoryDialog.Dispose();
            inventoryDialog = null;
        }

        if (itemCooldowns != null)
        {
            itemCooldowns.Dispose();
            itemCooldowns = null;
        }

        if (skillCooldowns != null)
        {
            skillCooldowns.Dispose();
            skillCooldowns = null;
        }

        // SkillTargeting is an independent scene root. Ordinary map release does not
        // own it; final GameBootstrap world-root purge reaches its OnDestroy cleanup.
        cursorManager = null;
        pendingGroundSkillId = 0;
        pendingTargetSkillId = 0;

        if (teamConfigDialog != null)
        {
            teamConfigDialog.Dispose();
            teamConfigDialog = null;
        }

        if (hotbar != null)
        {
            if (!borrowedNativeHudViews) hotbar.Dispose();
            hotbar = null;
        }

        if (skillDialog != null)
        {
            // Release only borrowed adapters. The independent root survives ordinary maps.
            skillDialog.DisposeSkillDialog();
            skillDialog = null;
        }

        if (systemInfoBar != null)
        {
            systemInfoBar.Dispose();
            systemInfoBar = null;
        }

        if (reviveChoiceDialog != null)
        {
            reviveChoiceDialog.Dispose();
            reviveChoiceDialog = null;
        }

        if (inspectedEquipmentDialog != null)
        {
            inspectedEquipmentDialog.Dispose();
            inspectedEquipmentDialog = null;
        }

        if (gossipDialog != null)
        {
            if (gameBootstrap == null || !ReferenceEquals(gossipDialog, gameBootstrap.PersistentGossipForIndependentRuntime))
                gossipDialog.Dispose();
            // This map borrows persistent windows; it never retains them after release.
            gossipDialog = null;
        }

        if (npcDialog != null)
        {
            npcDialog.Dispose();
            npcDialog = null;
        }


        if (transportDialog != null)
        {
            transportDialog.Dispose();
            transportDialog = null;
        }

        if (storeDialog != null)
        {
            storeDialog.Dispose();
            storeDialog = null;
        }

        if (chatUi != null)
        {
            chatUi.Dispose();
            chatUi = null;
        }

        if (playerActionMenu != null)
        {
            playerActionMenu.Dispose();
            playerActionMenu = null;
        }

        if (uiTip != null)
        {
            uiTip.Dispose();
            uiTip = null;
        }

        if (controlBar != null)
        {
            if (!borrowedNativeHudViews) controlBar.Dispose();
            controlBar = null;
        }

        if (targetHud != null)
        {
            if (!borrowedNativeHudViews) targetHud.Dispose();
            targetHud = null;
        }

        if (buffHud != null)
        {
            buffHud.UnbindObservationForIndependentRuntime(this);
            if (!borrowedNativeHudViews) buffHud.Dispose();
            buffHud = null;
        }

        if (minimapHud != null)
        {
            if (!borrowedNativeHudViews) minimapHud.Dispose();
            minimapHud = null;
        }

        if (playerStatusHud != null)
        {
            if (!borrowedNativeHudViews) playerStatusHud.Dispose();
            playerStatusHud = null;
        }

        if (hudResources != null)
        {
            if (!borrowedNativeHudViews) hudResources.Dispose();
            hudResources = null;
        }

        if (frame != null)
        {
            // DuelHud is an independent native scene root. Map/frame retirement cannot
            // clear its static state or destroy it; final native teardown purges the root.
            frame.Dispose(sceneRegistriesReleased, preserveNativePlayerState);
            frame = null;
        }

        if (preparedStaticComposite != null)
        {
            preparedStaticComposite.Dispose();
            preparedStaticComposite = null;
        }

        DestroyOwned(preparedCoverTexture);
        preparedCoverTexture = null;

        playerTaskTrack = null;

        mapMusic = null;
        combatSound = null;

        // The selected scene camera is borrowed. An ordinary map/candidate release
        // must not destroy it; final teardown keeps its existing native root-purge rules.
        if (!sceneRegistriesReleased && !worldRootReleased) DestroyOwned(worldRootObject);
        DestroyOwned(ownedClickRingSprite);
        ownedClickRingSprite = null;
        worldCamera = null;
        worldRootObject = null;
        nativeHud = null;
        borrowedNativeHudViews = false;
        nativeHudViewsBound = false;

        if (resources != null)
        {
            resources.Dispose();
            resources = null;
        }

        nameModeController = null;
        mapDescriptor = default(MapDef.Entry);
        mapLinks = null;
        collisionMask = null;
        built = false;
        TickCount = 0;
        LastNameModeFeedback = string.Empty;
        LastFollowFeedback = string.Empty;
        playerStatusVersion = 0;
        selfPkStateVersion = 0;
        skillHitEffectsVersion = long.MinValue;
        skillHitEffectCursor = 0;
        usingEntryStatusFallback = false;
        LastControlBarAction = ReferenceFirstWorldControlBarAction.None;
        LastTargetHudAction = ReferenceFirstWorldTargetHudAction.None;
        LastPointerAction = ReferenceFirstWorldPointerAction.None;
        rightClickTracker.Reset();
        if (!preserveNativePlayerState) manualMovementController.Reset();
    }

    private bool TryApplyLeftSelection(
        ReferenceWorldPointerTargetKind kind,
        int entityId,
        MonsterEntity pickedMonster = null)
    {
        if (kind == ReferenceWorldPointerTargetKind.Npc)
        {
            // WBD-012-R1: the official left-click NPC branch (arm64 06000a00.c:1252-1265, x86_64
            // 06000a00.c:1288-1298) does NpcInteract.Show and then only clears moveToTarget/followMode
            // before jumping past every later left-click branch (arm64 LAB_002af93c at :1370, x86_64
            // LAB_002bfaa1 at :1320). CombatWorld.StopAttack appears nowhere in that taken path and
            // CombatWorld.ClearTarget (0x0600014d) is called exactly once in Update, in the short
            // right-click branch for a click that picked nothing (arm64 :1409, x86_64 :1352). A
            // product-only ClearTarget here would cancel an attack/selected target the official
            // client keeps while the dialog opens, so it is removed rather than narrowed.
            // Official PlayerController.Update drives NpcInteract.Show (0x06000926), which forwards
            // itself to the dialog singleton; this tree keeps the same two hops so the open path has
            // one owner.
            ReferenceWorldNpcActor npcActor;
            if (frame == null ||
                !frame.NpcActors.TryGetValue(entityId, out npcActor) ||
                npcActor == null ||
                npcActor.SelectableOwner == null)
            {
                return false;
            }

            // Native Show is void. Its observation may remain None when the dialog declines
            // to open; that does not abort PlayerController's movement clear or pointer tail.
            LastPointerAction = ReferenceFirstWorldPointerAction.None;
            npcActor.SelectableOwner.Show();
            return true;
        }

        if (kind == ReferenceWorldPointerTargetKind.Monster)
        {
            frame.MonsterActors.TryGetValue(entityId, out ReferenceWorldMonsterActor actor);
            MonsterEntity owner = pickedMonster;
            if (ReferenceEquals(owner, null))
            {
                // The explicit entity-id diagnostic keeps its existing admission checks.
                if (actor == null || actor.State == null ||
                    actor.State.HitPointsOrRadiusPixels <= 0 || !actor.SelectableOwner)
                    return false;
                owner = actor.SelectableOwner;
            }

            owner.OnMouseDownForDiagnostic();
            if (actor != null && ReferenceEquals(actor.SelectableOwner, owner))
            {
                if (!frame.TargetSelection.ObserveMonsterClickForIndependentRuntime(actor))
                    return false;
            }
            else
            {
                // Native selection has already happened. Only its optional HUD observation
                // is adopted here; no second selection or combat write is performed.
                frame.AdoptCurrentPointerSelectionForIndependentRuntime();
                if (!ReferenceEquals(TargetSelect.Current, owner)) return false;
            }

            if (owner.NoAttack)
            {
                LastPointerAction = ReferenceFirstWorldPointerAction.TargetSelected;
            }
            else
            {
                LastPointerAction = ReferenceFirstWorldPointerAction.MonsterAttackIntentSelected;
            }
            return true;
        }

        if (kind != ReferenceWorldPointerTargetKind.RemotePlayer)
        {
            return false;
        }

        int localFaction;
        bool openWorldHostile;
        if (!TryResolveRemoteHostility(
                entityId,
                out localFaction,
                out openWorldHostile))
        {
            return false;
        }

        ReferenceWorldRemotePointerSelection result;
        if (!frame.TryHandleRemotePointerDown(
                entityId,
                false,
                localFaction,
                openWorldHostile,
                out result))
        {
            return false;
        }

        LastPointerAction = result == ReferenceWorldRemotePointerSelection.FriendlySelection
            ? ReferenceFirstWorldPointerAction.FriendlyRemoteSelected
            : ReferenceFirstWorldPointerAction.PlayerVsPlayerIntentSelected;
        return result != ReferenceWorldRemotePointerSelection.None;
    }

    internal bool TryBeginMonsterRealCombatTail(int entityId)
    {
        ReferenceWorldMonsterActor actor;
        if (!built || frame == null || entityId <= 0 ||
            !frame.MonsterActors.TryGetValue(entityId, out actor) ||
            actor == null || actor.State == null ||
            actor.State.FriendlyFlag != 0 || actor.State.OwnerEntityId != 0 ||
            actor.State.HitPointsOrRadiusPixels <= 0)
        {
            return false;
        }

        return TryApplyLeftSelection(
            ReferenceWorldPointerTargetKind.Monster,
            entityId);
    }

    internal void ClearMonsterRealCombatTailSelection()
    {
        if (frame != null)
        {
            frame.ClearTargetSelection();
        }
        CombatWorld.StopAttack();
    }

    internal ReferenceMonsterRealCombatTailDiagnostic
        CaptureMonsterRealCombatTailDiagnostic(int entityId)
    {
        ReferenceMonsterRealCombatTailDiagnostic result =
            new ReferenceMonsterRealCombatTailDiagnostic
            {
                windowTickCount = TickCount,
                windowLastFailure = LastFailure.ToString(),
                windowLastWorldRevisionFailure = LastWorldRevisionFailure.ToString(),
                lastBasicAttackFailureCode = LastBasicAttackFailureCode ?? string.Empty,
            };

        CombatWorld combatOwner = frame?.CombatOwnerForIndependentRuntime;
        if (combatOwner != null)
        {
            MonsterEntity currentTarget = combatOwner.TargetForIndependentRuntime;
            result.basicAttackSelectedEntityId = currentTarget == null ? 0 : currentTarget.id;
            result.basicAttackAutoAttacking = combatOwner.AutoAttackingForIndependentRuntime;
            result.basicAttackPhase = ((ReferenceWorldBasicAttackPhase)
                combatOwner.AttackStateForIndependentRuntime).ToString();
            result.basicAttackPhaseSeconds = combatOwner.AttackPhaseTimeForIndependentRuntime;
        }

        ReferenceWorldLocalPlayerDoll player = frame == null ? null : frame.Player;
        if (player != null)
        {
            result.playerAvailable = true;
            result.playerWorldX = player.WorldX;
            result.playerWorldY = player.WorldY;
            result.playerMovementActive = player.MovementActive;
        }

        ReferenceWorldMonsterActor target = null;
        if (frame != null)
        {
            result.targetFrameContains = frame.MonsterActors.TryGetValue(
                entityId,
                out target);
        }
        result.targetCombatContains =
            CombatWorld.ContainsMonsterForIndependentRuntime(entityId);
        if (target != null)
        {
            result.targetSourceOwnerLive = target.SourceOwnerLive;
            result.targetWorldX = target.WorldX;
            result.targetWorldY = target.WorldY;
            result.targetHitPoints = target.State == null
                ? 0
                : target.State.HitPointsOrRadiusPixels;
            result.targetDying = target.Dying;
            result.targetColliderEnabled =
                target.PickBox != null && target.PickBox.enabled;
        }

        ISelectable current = TargetSelect.Current;
        result.targetSelectType = current == null
            ? string.Empty
            : current.GetType().Name;
        if (current != null && frame != null)
        {
            foreach (System.Collections.Generic.KeyValuePair<
                         int,
                         ReferenceWorldMonsterActor> pair in frame.MonsterActors)
            {
                if (pair.Value != null && pair.Value.SelectableOwner == current)
                {
                    result.targetSelectEntityId = pair.Key;
                    break;
                }
            }
        }

        result.attackRange = ReferenceWorldBasicAttackController.AttackRangePixels(
            character.Appearance.AttackDistance);
        if (player != null && target != null)
        {
            float deltaX = target.WorldX - player.WorldX;
            float deltaY = target.WorldY - player.WorldY;
            result.distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
            result.inRange = ReferenceWorldBasicAttackController.IsInRange(
                player.WorldX,
                player.WorldY,
                target.WorldX,
                target.WorldY,
                character.Appearance.AttackDistance);
            result.lineOfSight = HasBasicAttackLineOfSight(
                player.WorldX,
                player.WorldY,
                target.WorldX,
                target.WorldY);

            ReferenceWorldApproachEvaluation approach;
            if (TryEvaluateMonsterRealCombatTailApproach(entityId, out approach))
            {
                result.approachEvaluated = approach.Evaluated;
                result.approachAlreadyInRange = approach.AlreadyInRange;
                result.approachOutcome = approach.Outcome.ToString();
                result.approachMoveToTarget = approach.MoveToTarget;
                result.approachWaypointCount = approach.WaypointCount;
                result.approachFinalX = approach.FinalX;
                result.approachFinalY = approach.FinalY;
                result.approachFinalDistance = approach.FinalDistance;
                result.approachCurrentLineOfSight = approach.CurrentLineOfSight;
                result.approachEndpointLineOfSight = approach.EndpointLineOfSight;
                result.approachAdmissible = approach.Admissible;
            }
        }

        return result;
    }

    internal bool TryEvaluateMonsterRealCombatTailApproach(
        int entityId,
        out ReferenceWorldApproachEvaluation evaluation)
    {
        evaluation = default(ReferenceWorldApproachEvaluation);
        ReferenceWorldMonsterActor target;
        return built && frame != null && frame.Player != null &&
               collisionMask != null &&
               frame.MonsterActors.TryGetValue(entityId, out target) &&
               target != null && target.State != null && target.SourceOwnerLive &&
               target.State.FriendlyFlag == 0 && target.State.OwnerEntityId == 0 &&
               target.State.HitPointsOrRadiusPixels > 0 &&
               ReferenceWorldSetPathToRealCombatTailAdapter.TryEvaluateApproach(
                   collisionMask,
                   frame.Player.WorldX,
                   frame.Player.WorldY,
                   target.WorldX,
                   target.WorldY,
                   character.Appearance.AttackDistance,
                   out evaluation);
    }

    internal bool TryEvaluateNpcTaskLifecycleApproach(
        int entityId,
        out ReferenceWorldApproachEvaluation evaluation)
    {
        evaluation = default(ReferenceWorldApproachEvaluation);
        ReferenceWorldMonsterActor target;
        return built && frame != null && frame.Player != null &&
               collisionMask != null &&
               frame.MonsterActors.TryGetValue(entityId, out target) &&
               target != null && target.State != null && target.SourceOwnerLive &&
               target.State.FriendlyFlag == 0 && target.State.OwnerEntityId == 0 &&
               target.State.HitPointsOrRadiusPixels > 0 &&
               ReferenceWorldSetPathToRealCombatTailAdapter.TryEvaluateApproach(
                   collisionMask,
                   frame.Player.WorldX,
                   frame.Player.WorldY,
                   target.WorldX,
                   target.WorldY,
                   character.Appearance.AttackDistance,
                   out evaluation);
    }

    private bool TryApplyDirectSelection(
        ReferenceWorldPointerTargetKind kind,
        int entityId)
    {
        if (kind == ReferenceWorldPointerTargetKind.LocalPlayer)
            return frame.TargetSelection.TrySelectSelf(frame.Player.SourceControllerForIndependentRuntime);

        if (kind == ReferenceWorldPointerTargetKind.Npc)
        {
            return frame.NpcActors.TryGetValue(entityId, out ReferenceWorldNpcActor npc) &&
                frame.TargetSelection.TrySelectNpc(npc);
        }

        if (kind == ReferenceWorldPointerTargetKind.Monster)
        {
            // Official 0x06000a00 short right-click monster arm: CombatWorld.SetTarget (0x0600014b)
            // with attack = false owns target/pvpTarget/autoAttacking/atkState/atkPhaseT and the one
            // Select of this click, and a friendly MonsterEntity is only selected. The product-only
            // pair that used to run here - frame.TrySelectMonster, which selected first and then
            // wrote I.target alone for every monster, followed by basicAttackController.SetTarget,
            // a second writer of the attack state - is gone: a right click starts no attack, clears
            // no previous target and moves nothing, so this owner keeps no projection of its own.
            return frame.TrySelectMonsterIdentity(entityId);
        }

        if (kind != ReferenceWorldPointerTargetKind.RemotePlayer)
        {
            return false;
        }

        // Official PlayerController.Update short right-click branch: one TargetSelect.Select on the
        // clicked RemotePlayer and nothing else. The product-only basicAttackController.ClearTarget
        // that used to run here, and the migration wrapper faction projection, are both gone - the
        // official branch stops no attack and writes no CombatWorld target for a picked remote.
        return frame.TrySelectRemotePlayerIdentity(entityId);
    }

    // Official 06000926 forwards the interactable to the dialog singleton without owning the open
    // path. The identity walk is only the adapter from the recovered NpcInteract back to the
    // entity id this owner keys its actors by.
    internal void ObserveNpcDialogShownForIndependentRuntime(NpcInteract npc)
    {
        // Observation only: the native singleton already performed Show and its one request.
        if (frame == null) return;
        foreach (KeyValuePair<int, ReferenceWorldNpcActor> pair in frame.NpcActors)
            if (pair.Value != null && ReferenceEquals(pair.Value.SelectableOwner, npc))
            {
                if (DialogUI.I != null)
                    DialogUI.I.ObserveNpcEntityForIndependentRuntime(pair.Key);
                LastPointerAction = ReferenceFirstWorldPointerAction.NpcDialogOpened;
                return;
            }
    }

    private bool TryResolveRemoteHostility(
        int entityId,
        out int localFaction,
        out bool openWorldHostile)
    {
        localFaction = 0;
        openWorldHostile = false;
        ReferenceWorldRemotePlayerActor actor;
        if (frame == null ||
            !frame.RemoteActors.TryGetValue(entityId, out actor) ||
            actor == null)
        {
            return false;
        }

        localFaction = CombatWorld.LocalFaction;
        openWorldHostile = actor.SourceIdentityForIndependentRuntime.OpenWorldHostile;
        return true;
    }

    private bool ObserveChatSends()
    {
        for (int index = pendingChatSends.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingChatSends[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastChatSendFailureCode = "chat.send.exception";
            }
            else if (pending.IsCanceled)
            {
                LastChatSendFailureCode = "chat.send.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastChatSendFailureCode = pending.Result.Error.Code;
            }

            pendingChatSends.RemoveAt(index);
        }

        return true;
    }

    internal void BeginRepairSelectionForIndependentRuntime(byte mode)
    {
        RepairDlg.BeginPick(mode);
    }

    internal void SendMenuCommandFromNpcDialog(string command)
    {
        pendingChatSends.Add(session.SendWorldChatAsync(
            WorldChatSendRequest.ForGmCommand(command), chatLifetime.Token));
    }

    internal void OpenShopFromNpcDialog(int shopId, bool sell)
    {
        ShopDlg.Ensure();
        ShopDlg.I.BindRepairSelectionForIndependentRuntime(BeginRepairSelectionForIndependentRuntime);
        ShopDlg.Open(shopId, sell);
    }

    internal void OpenTransportFromNpcDialog(int transportList)
    {
        if (transportDialog == null || npcFunctionLifetime == null ||
            npcFunctionLifetime.IsCancellationRequested)
        {
            return;
        }

        storeDialog?.Close();
        transportDialog.Open(transportList);
        WorldTransportState cached = session.TransportState;
        if (cached != null && cached.TransportList == transportList)
        {
            transportDialog.ApplyState(cached);
        }
        pendingNpcFunctionRequests.Add(
            session.OpenTransportAsync(transportList, npcFunctionLifetime.Token));
    }

    internal void OpenWarehouseFromNpcDialog()
    {
        if (storeDialog == null || npcFunctionLifetime == null ||
            npcFunctionLifetime.IsCancellationRequested)
        {
            return;
        }

        transportDialog?.Close();
        storeDialog.Toggle();
    }

    internal void OpenFamilyStashFromNpcDialog()
    {
        if (npcFunctionLifetime == null || npcFunctionLifetime.IsCancellationRequested)
        {
            return;
        }

        ApplyFamilyStashSnapshot(session.FamilyStashState);
        FamilyStashDlg.Open();
    }

    private static void ApplyFamilyStashSnapshot(WorldFamilyStashState state)
    {
        WorldFamilyStashState source = state ?? WorldFamilyStashState.Empty;
        var entries = new List<FamilyStashDlg.Entry>(source.Items.Count);
        foreach (WorldFamilyStashItem item in source.Items)
        {
            entries.Add(new FamilyStashDlg.Entry
            {
                id = item.ItemId,
                count = item.Count,
                curDur = item.CurrentDurability,
                maxDur = item.MaximumDurability,
                quality = item.Quality,
                name = item.Name,
                desc = item.Description,
                by = item.DepositedBy
            });
        }
        FamilyStashDlg.SetData(source.Capacity, source.TakePosition,
            source.CanTake, source.Leader, entries);
    }

    private static void ApplyHelpTopicsSnapshot(WorldHelpTopicsState state)
    {
        HelpWinDlg.Topics.Clear();
        foreach (WorldHelpTopic topic in (state ?? WorldHelpTopicsState.Empty).Topics)
            HelpWinDlg.Topics.Add((topic.Title, topic.Content));
    }

    private void SynchronizeChangeSet()
    {
        IChangeSetSource source = session.ChangeSetSource;
        if (source == null) return;
        int version = source.ChangeSetVersion;
        if (version == changeSetStateVersion) return;
        var items = new System.Collections.Generic.List<Win05.ChgItem>();
        foreach (WorldChangeSetItem item in source.ChangeSetSnapshot)
            items.Add(new Win05.ChgItem { cell = item.Cell, id = item.ItemId, name = item.Name,
                desc = item.Description, curDur = item.CurrentDurability, maxDur = item.MaximumDurability,
                refine = item.Refine, quality = item.Quality, blocked = item.Blocked,
                icon = NetClient.LoadItemIcon(item.ItemId) });
        Win05.SetChangeSet(items);
        changeSetStateVersion = version;
    }

    private bool SyncNpcFunctionDialogs()
    {
        if (transportDialog == null || storeDialog == null)
        {
            return false;
        }

        long nextTransportVersion = session.TransportStateVersion;
        if (nextTransportVersion != transportStateVersion)
        {
            transportStateVersion = nextTransportVersion;
            WorldTransportState next = session.TransportState;
            // Original 0x8049 opens from the received list. The request callback neither
            // pre-opens an empty window nor replays the previous response.
            if (next != null && !ReferenceEquals(next, WorldTransportState.Empty) &&
                !transportDialog.ApplyState(next))
            {
                return false;
            }
        }

        SynchronizeChangeSet();
        storeDialog.Synchronize(session.WarehouseUpdateSource);
        long nextWarehouseVersion = session.WarehouseStateVersion;
        if (nextWarehouseVersion != warehouseStateVersion)
        {
            warehouseStateVersion = nextWarehouseVersion;
            WorldWarehouseState next = session.WarehouseState;
            if (session.WarehouseUpdateSource == null && next != null &&
                !storeDialog.ApplyState(next))
            {
                return false;
            }
        }

        long nextFamilyStashVersion = session.FamilyStashStateVersion;
        if (nextFamilyStashVersion != familyStashStateVersion)
        {
            familyStashStateVersion = nextFamilyStashVersion;
            ApplyFamilyStashSnapshot(session.FamilyStashState);
        }
        long nextHelpTopicsVersion = session.HelpTopicsStateVersion;
        if (nextHelpTopicsVersion != helpTopicsStateVersion)
        {
            helpTopicsStateVersion = nextHelpTopicsVersion;
            ApplyHelpTopicsSnapshot(session.HelpTopicsState);
        }

        long nextFriendVersion = session.FriendStateVersion;
        if (nextFriendVersion != friendStateVersion)
        {
            friendStateVersion = nextFriendVersion;
            SocialClient.BindFriendStateForIndependentRuntime(session.FriendState);
        }
        long nextApprenticeVersion = session.ApprenticeStateVersion;
        if (nextApprenticeVersion != apprenticeStateVersion)
        {
            apprenticeStateVersion = nextApprenticeVersion;
            SocialClient.BindApprenticeStateForIndependentRuntime(session.ApprenticeState);
        }
        long nextBlockVersion = session.BlockStateVersion;
        if (nextBlockVersion != blockStateVersion)
        {
            blockStateVersion = nextBlockVersion;
            SocialClient.BindBlockStateForIndependentRuntime(session.BlockState);
        }

        return true;
    }

    private bool ObserveNpcFunctionRequests()
    {
        for (int index = pendingNpcFunctionRequests.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingNpcFunctionRequests[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastNpcFunctionFailureCode = "npc.function.exception";
            }
            else if (pending.IsCanceled)
            {
                LastNpcFunctionFailureCode = "npc.function.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastNpcFunctionFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastNpcFunctionFailureCode = string.Empty;
            }

            pendingNpcFunctionRequests.RemoveAt(index);
        }
        return true;
    }

    private bool SyncNpcDialogState()
    {
        if (npcDialog == null || chatUi == null)
        {
            return false;
        }

        // No chatter arm: official 0x804b tail-calls NpcChatter.SetServerChatter inside the case
        // body, so each packet already replaced the tables on receipt, in arrival order. A
        // version comparison here would apply only the last packet of a frame and would lose the
        // "first packet positive, second packet zero" timing that only positive values overwrite.

        // No task-log arm: official 0x8027 tail-calls TaskClient.SetState (0x06000e5d) inside the
        // case body, so each packet already cleared and refilled Active and Done and fired
        // OnStateChanged on receipt, in arrival order. A version comparison that also applied the
        // state would turn N replies in one frame into N + 1 replacements and one extra callback,
        // which is not the official behaviour. This compatibility observation updates no native
        // state: DialogUI's menu/detail query TaskClient.IsTerminal directly at their callsites.

        long nextTaskLogVersion = session.TaskLogStateVersion;
        if (nextTaskLogVersion != taskLogStateVersion)
        {
            taskLogStateVersion = nextTaskLogVersion;
            if (!npcDialog.Ready)
            {
                return false;
            }
        }

        // No offers arm: official 0x8028 tail-calls TaskClient.SetOffers inside the case body, so
        // each packet already replaced the offers and fired OnOffersChanged on receipt, in arrival
        // order, and DialogUI.OnTaskOffers (0x06000212) rebuilt the menu once per packet. A
        // version comparison here would apply only the last packet of a frame, which is not the
        // official behaviour: N replies in one frame are N replacements and N menu rebuilds, and
        // every rebuild also resets InSubView and the current selection.

        // 0x8008 and 0x8072 have already executed their native case tails, in arrival order,
        // before the next packet was dispatched. This retained window is observation-only:
        // no dialog/chat/tip/clipboard call or visibility decision belongs in this frame pass.
        WorldNpcDialogTextEdgeWindow nextTextEdges = session.NpcDialogTextEdgeWindow;
        if (nextTextEdges != null)
        {
#if UNITY_EDITOR
            foreach (WorldNpcDialogTextEdge edge in nextTextEdges.Edges)
                if (edge.Revision > npcDialogTextEdgeCursor &&
                    edge.InvitationCodeResult == null && edge.DisplayMode == 5)
                    pendingNewbieTipEdges.Add(edge);
#endif
            // Retention overflow cannot block gameplay. Missing history was already delivered.
            npcDialogTextEdgeCursor = nextTextEdges.LastRevision;
            npcDialogTextEdgeWindowVersion = nextTextEdges.Version;
        }

        return true;
    }

    private void RequestNpcTaskOffersFromDialog(string npcName)
    {
        if (npcTaskLifetime == null)
        {
            npcTaskActionReady = false;
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
            return;
        }
        CancellationToken cancellationToken = npcTaskLifetime.Token;
        // A live native Show request finishes its send before the dialog observation continues.
        // Preserve the existing already-cancelled admission without deferring a live GUI request.
        pendingNpcTaskRequests.Add(session.RequestNpcTaskOffersAsync(npcName,
            cancellationToken.IsCancellationRequested ? cancellationToken : CancellationToken.None));
    }

    private void RequestNpcTaskMutationFromDialog(ReferenceNpcTaskMutationIntent intent)
    {
        // Existing task adapters now execute at the native row-callback boundary, before
        // the decoration tail. The token and asynchronous result observation are unchanged.
        if (npcTaskLifetime == null || npcTaskLifetime.IsCancellationRequested)
        {
            npcTaskActionReady = false;
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
            return;
        }
        Task<Result> pending;
        if (intent.Kind == ReferenceNpcTaskMutationKind.Accept)
        {
            pending = session.AcceptTaskAsync(intent.TaskId, npcTaskLifetime.Token);
            npcTaskAcceptRequestIssuedCount++;
            npcDialog.Close();
        }
        else if (intent.Kind == ReferenceNpcTaskMutationKind.Complete)
        {
            pending = session.CompleteTaskAsync(intent.TaskId, npcTaskLifetime.Token);
            npcTaskCompleteRequestIssuedCount++;
        }
        else return;
        pendingNpcTaskRequests.Add(pending);
        pendingNpcTaskMutationKinds[pending] = intent.Kind;
    }

    private bool ObserveNpcTaskRequests()
    {
        for (int index = pendingNpcTaskRequests.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingNpcTaskRequests[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            ReferenceNpcTaskMutationKind mutationKind;
            bool isMutation = pendingNpcTaskMutationKinds.TryGetValue(
                pending,
                out mutationKind);
            bool isBaselineAbandon = pendingNpcTaskAbandonRequests.Contains(pending);
            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastNpcTaskFailureCode = "npc.tasks.exception";
            }
            else if (pending.IsCanceled)
            {
                LastNpcTaskFailureCode = "npc.tasks.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastNpcTaskFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastNpcTaskFailureCode = string.Empty;
                if (isMutation && mutationKind == ReferenceNpcTaskMutationKind.Accept)
                {
                    npcTaskAcceptRequestSucceededCount++;
                }
                else if (isBaselineAbandon)
                {
                    npcTaskAbandonRequestSucceededCount++;
                }
                else if (isMutation &&
                         mutationKind == ReferenceNpcTaskMutationKind.Complete)
                {
                    npcTaskCompleteRequestSucceededCount++;
                }
            }

            pendingNpcTaskMutationKinds.Remove(pending);
            pendingNpcTaskAbandonRequests.Remove(pending);
            pendingNpcTaskRequests.RemoveAt(index);
        }

        return true;
    }

    /// <summary>
    /// Sends the existing production TaskAbandon action for one exact run-owned active baseline.
    /// </summary>
    /// <remarks>
    /// The journey driver derives the task identity from the live same-NPC offer. This adapter
    /// queues only the formal Application action; the later task-log and offer replacements remain
    /// the sole authority that the task became available.
    /// </remarks>
    internal bool TryAbandonNpcTaskLifecycleBaseline(int taskId)
    {
        if (!built || taskId <= 0 || npcTaskLifetime == null || session == null)
        {
            return false;
        }

        Task<Result> pending = session.AbandonTaskAsync(
            taskId,
            npcTaskLifetime.Token);
        npcTaskAbandonRequestIssuedCount++;
        pendingNpcTaskRequests.Add(pending);
        pendingNpcTaskAbandonRequests.Add(pending);
        return true;
    }

    /// <summary>
    /// Supplies a fixed QA travel destination to the recovered PlayerController.SetPathTo owner.
    /// </summary>
    internal bool TrySetNpcTaskLifecyclePath(float x, float y)
    {
        if (!built || frame == null || frame.Player == null ||
            float.IsNaN(x) || float.IsInfinity(x) ||
            float.IsNaN(y) || float.IsInfinity(y))
        {
            return false;
        }

        manualMovementController.CancelForWorldInteraction();
        frame.ClearTargetSelection();
        CombatWorld.StopAttack();
        return frame.Player.TrySetPathTo(collisionMask, x, y);
    }

    /// <summary>
    /// Feeds one primary-down through DialogUI's exact option hit rows and production request owner.
    /// </summary>
    internal bool HandleNpcTaskLifecycleDialogPrimaryDown(
        Vector2 topOriginPointer,
        float scale)
    {
        npcTaskActionReady = true;
        if (npcDialog == null ||
            !npcDialog.HandlePrimaryDown(
                topOriginPointer,
                Screen.width,
                Screen.height,
                scale) || !npcTaskActionReady)
        {
            LastFailure = ReferenceFirstWorldWindowFailure.InvalidNpcDialog;
            return false;
        }

        LastFailure = ReferenceFirstWorldWindowFailure.None;
        return true;
    }

    private bool ObserveInspectRequest()
    {
        if (pendingInspectRequest == null || !pendingInspectRequest.IsCompleted)
        {
            return true;
        }

        if (pendingInspectRequest.IsFaulted)
        {
            _ = pendingInspectRequest.Exception;
            LastInspectFailureCode = "player.inspect.exception";
        }
        else if (pendingInspectRequest.IsCanceled)
        {
            LastInspectFailureCode = "player.inspect.cancelled";
        }
        else if (!pendingInspectRequest.Result.IsSuccess)
        {
            LastInspectFailureCode = pendingInspectRequest.Result.Error.Code;
        }
        else
        {
            LastInspectFailureCode = string.Empty;
        }

        pendingInspectRequest = null;
        return true;
    }

    private bool ObserveTradeRequest()
    {
        if (pendingTradeRequest == null || !pendingTradeRequest.IsCompleted)
        {
            return true;
        }

        if (pendingTradeRequest.IsFaulted)
        {
            _ = pendingTradeRequest.Exception;
            LastTradeRequestFailureCode = "trade.request.exception";
        }
        else if (pendingTradeRequest.IsCanceled)
        {
            LastTradeRequestFailureCode = "trade.request.cancelled";
        }
        else if (!pendingTradeRequest.Result.IsSuccess)
        {
            LastTradeRequestFailureCode = pendingTradeRequest.Result.Error.Code;
        }
        else
        {
            LastTradeRequestFailureCode = string.Empty;
        }

        pendingTradeRequest = null;
        return true;
    }

    private bool ObserveSystemRequestResponses()
    {
        for (int index = pendingSystemRequestResponses.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingSystemRequestResponses[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastSystemRequestResponseFailureCode =
                    "system.request.response.exception";
            }
            else if (pending.IsCanceled)
            {
                LastSystemRequestResponseFailureCode =
                    "system.request.response.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastSystemRequestResponseFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastSystemRequestResponseFailureCode = string.Empty;
            }

            pendingSystemRequestResponses.RemoveAt(index);
        }

        return true;
    }


    private bool SyncInventoryState(bool force)
    {
        if (inventoryDialog == null || localEquipmentDialog == null || session == null)
        {
            return false;
        }

        long nextVersion = session.InventoryStateVersion;
        if (!force && nextVersion == inventoryStateVersion)
        {
            return true;
        }

        WorldInventoryState next = session.InventoryState ?? WorldInventoryState.Empty;
        if (!inventoryDialog.ApplyState(next) ||
            !localEquipmentDialog.ApplyInventory(next) ||
            (skillDialog != null &&
             !skillDialog.ApplyMagicWardrobeInventory(next)))
        {
            return false;
        }

        if (force) InventoryDlg.BindFieldsForIndependentRuntime(next);
        else InventoryDlg.ObservePublicationForIndependentRuntime(next);
        if (force) EquipDlg.BindEquippedForIndependentRuntime(next);
        else EquipDlg.ObservePublicationForIndependentRuntime(next);
        if (!ReferenceEquals(next.EquipmentSnapshotItems, WorldInventoryState.Empty.EquipmentSnapshotItems))
            NetClient.I?.ApplyEquipmentPublicationForIndependentRuntime(next.EquipmentSnapshotItems);
        inventoryStateVersion = nextVersion;
        return true;
    }

    private bool SyncWardrobeState(bool force)
    {
        if (session == null)
        {
            return false;
        }

        long nextVersion = session.WardrobeStateVersion;
        if (!force && nextVersion == wardrobeStateVersion)
        {
            return true;
        }

        IWorldWardrobeObservation next = session.WardrobeObservation ?? WorldWardrobeState.Empty;
        if (skillDialog != null && !skillDialog.ApplyMagicWardrobeObservation(next))
        {
            return false;
        }

        wardrobeStateVersion = nextVersion;
        return true;
    }

    private bool SyncCharacterTitleState()
    {
        if (localEquipmentDialog == null || session == null)
        {
            return false;
        }

        // A title received before construction must reach the new name tag, and later
        // receipt writes must remain visible. PlayerNameTag.LateUpdate only rebuilds
        // the baked tag when the string differs, so keep this live projection each pass.
        if (CharData.HasReceivedTitleForIndependentRuntime)
        {
            frame?.ApplyLocalCharacterTitleForIndependentRuntime(CharData.title);
        }

        // EquipDlg draws and clamps its title selection from the live CharData list.
        // ApplyTitles only checked a newly allocated snapshot for null; capturing that
        // unused copy here added a construction/receipt-time allocation and exception
        // boundary before the later world gates, without updating the native window.
        // Partial 0x804c writes are already visible to its next original OnGUI call.
        return true;
    }

    // Equipment commands keep the existing per-map cancellation and Task<Result>
    // observation, but enter the real writer during native OnGUI rather than a later queue.
    internal CancellationToken EquipmentCommandCancellationForIndependentRuntime =>
        inventoryLifetime == null ? CancellationToken.None : inventoryLifetime.Token;
    internal void TrackEquipmentCommandForIndependentRuntime(Task<Result> pending) =>
        pendingInventoryCommands.Add(pending);

    private bool ConsumeSkillDialogIntents()
    {
        if (skillDialog == null || hotbarLifetime == null)
        {
            return false;
        }

        int petViewMask;
        while (skillDialog.TryConsumePetViewMask(out petViewMask))
        {
            // The native click callback already sent the command; this queue only updates the host view.
            frame?.ApplyPetViewMask(petViewMask);
        }

        // NetClient.PetAddPoint 06000734 and NetClient.PetRename 06000735 leave the list below
        // since WBD-119, and NetClient.PetSetSkill 06000736 / NetClient.PetUseSkill 06000737 leave
        // it since WBD-120: Win05.DrawPet 06001043 sends all four from the drawing arm at the hit
        // itself (the skill row's right-click arm, arm64 :1942-2019 / x86_64 :2135-2206), so the
        // four queues and their drain arms are gone and no product step sits between the hit and
        // the start of the send. That removes our own delay, not the gateway's: SendAsync still
        // awaits the _sendOperation semaphore and _host.RunAsync, so the wire write need not happen
        // inside this frame (partial, same as the other pet commands). The local echo
        // SubmitPetRename makes into CharData.petName, and the one the skill arm makes into
        // CharData.petActiveSkill, already happened before this point, in the same OnGUI frame.

        // Official Win05.DrawPet 06001043 sends the pet-bar switch, recall, swap and share itself
        // at the click, so no arm drains them here any more: CharData.SwitchPet 06000f22,
        // NetClient.PetRecall 06000738, NetClient.PetSwap 0600073d and InventoryDlg.ShareToChat
        // 06000511 are called inside the drawing arm the moment the slot is hit. Dequeuing them
        // later would be a second send and a later send time than the native one.

        // NetClient.PetShow 0x0600073b leaves this list with WBD-121 for the same reason: the
        // display checkbox arm of Win05.DrawPet 06001043 echoes into CharData.petShow and calls
        // the member inside the drawing arm at the hit (arm64 :2222-2242 / x86_64 :2401-2421), so
        // draining it here would be a second, later send of the same click. The pet view mask
        // above is not a pet command and stays a host view update.

        if (inventoryLifetime == null)
        {
            return false;
        }

        byte wardrobeTab;
        int wardrobeSlot;
        while (skillDialog.TryConsumeWardrobeStore(
            out wardrobeTab,
            out wardrobeSlot))
        {
            pendingInventoryCommands.Add(
                session.StoreWardrobeItemAsync(
                    wardrobeTab,
                    wardrobeSlot,
                    inventoryLifetime.Token));
        }

        // The wardrobe wear and move actions are not pumped here either: the official grid input
        // branch (Win05.DrawMagicStore 0x0600104f) sends NetClient.WardrobeWear /
        // NetClient.WardrobeMove itself inside the mouse-up handling, exactly like the extraction
        // confirmation, so a dequeue arm would send a second time.

        return true;
    }

    private bool ObserveInventoryCommands()
    {
        for (int index = pendingInventoryCommands.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingInventoryCommands[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastInventoryCommandFailureCode = "inventory.command.exception";
            }
            else if (pending.IsCanceled)
            {
                LastInventoryCommandFailureCode = "inventory.command.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastInventoryCommandFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastInventoryCommandFailureCode = string.Empty;
            }

            pendingInventoryCommands.RemoveAt(index);
        }

        return true;
    }

    private bool SyncHotbarState(bool force)
    {
        if (hotbar == null || session == null)
        {
            return false;
        }

        long nextVersion = session.HotbarStateVersion;
        if (!force && nextVersion == hotbarStateVersion)
        {
            return true;
        }

        WorldHotbarState next = session.HotbarState ?? WorldHotbarState.Empty;
        if (!hotbar.ApplyState(next))
        {
            return false;
        }

        hotbarStateVersion = nextVersion;
        return true;
    }

    private bool SyncSkillBookState(bool force)
    {
        if (skillDialog == null || session == null)
        {
            return false;
        }

        long nextSkillVersion = session.SkillBookStateVersion;
        if (force || nextSkillVersion != skillBookStateVersion)
        {
            WorldSkillBookObservation skillBook = session.SkillBookObservation;
            if (!skillDialog.ApplySkillBookObservation(skillBook) ||
                localEquipmentDialog == null ||
                !localEquipmentDialog.ApplySkillBookObservation(skillBook))
            {
                return false;
            }

            skillBookStateVersion = nextSkillVersion;
        }

        long nextLifeVersion = session.LifeSkillBookStateVersion;
        if (force || nextLifeVersion != lifeSkillBookStateVersion)
        {
            if (!skillDialog.ApplyLifeSkillBook(
                    session.LifeSkillBookState ?? WorldLifeSkillBookState.Empty))
            {
                return false;
            }

            lifeSkillBookStateVersion = nextLifeVersion;
        }

        // Win05 reads TaskClient.Done directly; its compatibility ApplyTaskLog is a no-op.
        WorldPlayerStatusState currentStatus = session.PlayerStatusState;
        skillDialog.ApplyPlayerLevel(
            currentStatus == null ? character.Level : currentStatus.Level);
        return true;
    }

    private bool SyncBuffState(bool force, float elapsedSeconds)
    {
        if (buffHud == null || session == null || !IsNonNegativeFinite(elapsedSeconds))
        {
            return false;
        }

        long nextVersion = session.BuffStateVersion;
        if (!force && nextVersion == buffStateVersion)
        {
            return true;
        }

        if (!buffHud.BindObservationForIndependentRuntime(session, elapsedSeconds, this))
        {
            return false;
        }

        // No buff-list arm: official 0x8017 already built the list and stored it into the
        // CharData static inside the case body, with every row stamped at the moment it was read.
        // Rebuilding it here from the published snapshot would stamp again from a later frame and
        // would turn N replies in one frame into one list, which is not the official behaviour;
        // Only explicit BuffHud.State access requests the compatibility observation.
        buffStateVersion = nextVersion;
        return true;
    }

    private bool SyncSkillHitEffects(bool force)
    {
        if (session == null || frame == null)
        {
            return false;
        }

        long nextVersion = session.SkillHitEffectsVersion;
        if (!force && nextVersion == skillHitEffectsVersion)
        {
            return true;
        }

        System.Collections.Generic.IReadOnlyList<WorldSkillHitEffect> edges =
            session.SkillHitEffects ?? Array.Empty<WorldSkillHitEffect>();
        for (int index = 0; index < edges.Count; index++)
        {
            WorldSkillHitEffect edge = edges[index];
            if (edge == null || edge.Revision <= skillHitEffectCursor)
            {
                continue;
            }

            // Official 0x0600015a owns this whole edge. It resolves monster, then remote player,
            // then the id 0 / self fallback on GameBootstrap.PlayerT, and plays once with
            // CombatWorld.I.ppu, so this event has no second dispatch of its own. The shared
            // TryResolveSkillActor stays untouched for the skill-cast callers that need its
            // renderer and facing; it is not an official arm of this packet, it resolves local
            // before monster before remote, and its ppu comes from CameraFollow instead of the
            // owner.
            CombatWorld owner = frame.CombatOwnerForIndependentRuntime;
            if (owner != null)
            {
                owner.ActivateForIndependentRuntime();
                CombatWorld.PlayEntityEffect(edge.EntityId, edge.Effect);
            }

            skillHitEffectCursor = edge.Revision;
        }

        skillHitEffectsVersion = nextVersion;
        return true;
    }

    private bool SyncLocalStatusEffects(bool force)
    {
        if (session == null)
        {
            return false;
        }

        WorldLocalStatusEffectWindow window = session.CaptureLocalStatusEffects();
        if (window != null)
        {
            // This Unity entry is synchronous on the main thread: no world/connection binding
            // can interleave a new player during SpawnOn. Background disconnect may invalidate
            // the capture; recheck after each call without holding Protocol locks around Unity.
            foreach (WorldEntityStatusEffect edge in window.Edges)
            {
                if (!session.IsCurrentLocalStatusEffect(window, edge.Revision)) return false;
                EffectPlayer.SpawnOn(0, edge.Effect);
                if (!session.AcknowledgeLocalStatusEffects(window, edge.Revision)) return false;
            }
            return session.AcknowledgeLocalStatusEffects(window, window.LastRevision);
        }

        // Older gateways and Editor fixtures have no capture capability.
        long nextVersion = session.LocalPlayerStatusEffectsVersion;
        if (!force && nextVersion == localPlayerStatusEffectsVersion)
        {
            return true;
        }

        System.Collections.Generic.IReadOnlyList<WorldEntityStatusEffect> edges =
            session.LocalPlayerStatusEffects ??
            Array.Empty<WorldEntityStatusEffect>();
        foreach (WorldEntityStatusEffect edge in edges)
        {
            if (edge.Revision <= session.LocalPlayerStatusEffectConsumedRevision) continue;
            EffectPlayer.SpawnOn(0, edge.Effect);
            session.AcknowledgeLocalPlayerStatusEffect(edge.Revision);
        }

        localPlayerStatusEffectsVersion = nextVersion;
        return true;
    }

    private bool TryResolveSkillTargetWorldPixel(
        int entityId,
        out float x,
        out float y)
    {
        x = 0f;
        y = 0f;
        if (entityId <= 0 || frame == null || frame.Player == null)
        {
            return false;
        }

        if (entityId == character.EntityId)
        {
            x = frame.Player.WorldX;
            y = frame.Player.WorldY;
            return true;
        }

        ReferenceWorldRemotePlayerActor remote;
        if (frame.RemoteActors.TryGetValue(entityId, out remote) && remote != null)
        {
            x = remote.WorldX;
            y = remote.WorldY;
            return true;
        }

        ReferenceWorldMonsterActor monster;
        if (frame.MonsterActors.TryGetValue(entityId, out monster) &&
            monster != null && monster.State != null && monster.SourceOwnerLive)
        {
            // Official CombatWorld.TargetWithin reads the selected component's live Transform.
            // The immutable spawn snapshot does not advance while MonsterEntity interpolates an
            // authoritative path, so combat navigation must consume the actor's current position.
            x = monster.WorldX;
            y = monster.WorldY;
            return true;
        }

        ReferenceWorldNpcActor npc;
        if (frame.NpcActors.TryGetValue(entityId, out npc) &&
            npc != null && npc.State != null)
        {
            x = npc.State.X;
            y = npc.State.Y;
            return true;
        }

        return false;
    }

    internal void TrackSkillSideEffectForIndependentRuntime(Task<Result> pending)
    {
        pendingHotbarCommands.Add(pending);
    }

    internal void ShowSkillTipForIndependentRuntime(string text, float scale)
    {
        uiTip?.Request(text, scale);
    }

    internal void ObserveNativeTargetHudForIndependentRuntime(ReferenceFirstWorldTargetHudAction action)
    {
        if (action != ReferenceFirstWorldTargetHudAction.None) LastTargetHudAction = action;
    }

    internal void ObserveNativeHudFailureForIndependentRuntime(ReferenceFirstWorldWindowFailure failure)
    {
        LastFailure = failure;
    }

    internal void ObserveNativeWorldMapRequestForIndependentRuntime() => LastWorldMapIntent = true;

    internal void ObserveNativeControlHudForIndependentRuntime(ReferenceFirstWorldControlBarAction action)
    {
        if (action != ReferenceFirstWorldControlBarAction.None) LastControlBarAction = action;
        if (action == ReferenceFirstWorldControlBarAction.Team)
        {
            TeamDialogOpenRequested = teamDialog.Visible;
            if (teamDialog.Visible) TeamDialogTab = 1;
        }
    }

    private void ObserveHotbarActivationForIndependentRuntime()
    {
        // Native UseHotSlot has already run at the slot/key, before Event.Use. These
        // host projections also run after an exception, as at the former delayed caller.
        frame?.Player?.ObserveNativeFacingForIndependentRuntime();
    }

    // Native Update and direct native UI sends take the current map's command lifetime at
    // the writer boundary; they never inherit a cancellation token from an earlier command.
    internal CancellationToken SkillSideEffectCancellationForIndependentRuntime =>
        hotbarLifetime?.Token ?? CancellationToken.None;

    private void CancelPendingSkillRequests()
    {
        frame?.CombatOwnerForIndependentRuntime?.ActivateForIndependentRuntime();
        CombatWorld.CancelPending();
    }

    private bool ObserveHotbarCommands()
    {
        for (int index = pendingHotbarCommands.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingHotbarCommands[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastHotbarCommandFailureCode = "hotbar.command.exception";
            }
            else if (pending.IsCanceled)
            {
                LastHotbarCommandFailureCode = "hotbar.command.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastHotbarCommandFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastHotbarCommandFailureCode = string.Empty;
            }

            pendingHotbarCommands.RemoveAt(index);
        }

        return true;
    }

    private bool SyncTradeState(bool force)
    {
        if (dealDialog == null || session == null)
        {
            return false;
        }

        // Observation only. Every successful trade receipt already ran its original owner
        // sequence; frame/GUI sync and forced rebuild cannot mutate or replay that sequence.
        long nextVersion = session.ActiveTradeStateVersion;
        if (!force && nextVersion == activeTradeStateVersion)
        {
            return true;
        }

        // The native dialog already consumes SocialClient. Keep its public observation lazy:
        // copying every item/pet/skill here would add an allocation boundary to each host update.
        dealDialog.ObservationSession = session;
        activeTradeStateVersion = nextVersion;
        return true;
    }

    private bool PollPlayerStatusRevisions(bool force)
    {
        if (playerStatusHud == null || session == null)
        {
            return false;
        }

        // Direct GameBootstrap startup already owns live CharData. Native HUD/equipment/pet
        // bodies read it themselves; an absent server snapshot must not replay entry defaults
        // into that state. This does not create a receipt or change the session's state.
        if (gameBootstrap != null && gameBootstrap.HasNativeStartupWorldForIndependentRuntime)
            return true;

        long nextPlayerStatusVersion = session.PlayerStatusStateVersion;
        WorldPlayerStatusState status = session.PlayerStatusState;
        if (status == null)
        {
            if (!force)
            {
                return usingEntryStatusFallback &&
                    nextPlayerStatusVersion == playerStatusVersion;
            }

            // The owner is also constructed by background source audits without a network
            // journey. Its initial HUD was already created from this immutable entry root.
            status = WorldPlayerStatusState.FromEntry(character);
            usingEntryStatusFallback = true;
        }
        else
        {
            usingEntryStatusFallback = false;
        }

        if (force || nextPlayerStatusVersion != playerStatusVersion)
        {
            if (!playerStatusHud.ApplyAuthoritativeStatus(status) ||
                (localEquipmentDialog != null &&
                 !localEquipmentDialog.ApplyStatus(status)) ||
                (skillDialog != null &&
                 !skillDialog.ApplyPetStatus(status.Pet)))
            {
                return false;
            }

            if (inventoryDialog != null)
            {
                inventoryDialog.CurrentPetBound = status.Pet.Bound;
            }

            // Official Win05.DrawPet 06001043 and GossipDlg.BuildLocalShares 060005e4 both read
            // CharData.petSlots (CharData static +0x180) by list index, so the list has to carry the
            // authoritative rows rather than a mirror some other owner refreshes on its own
            // schedule. This is where this owner applies a new authoritative status version, i.e.
            // one update per published pet snapshot; the projection itself is unchanged.
            CharData.BindPetSlotsForIndependentRuntime(status.Pet.Slots);
            // WBD-074 lane D moved the pet tail into this version gate; WBD-075 lane D corrected
            // this comment. The three roots leave this block from one status.Pet instance with no
            // read between the three calls, so no consumer can see the rows of one publish next to
            // the tail of another. What the frozen 0x800b tail actually evidences is only that
            // petSlots +0x180 is stored before petActive +0x178 inside one case body; petModel
            // +0x150 is not part of that tail and its store position is unproven, and the product
            // writes it inside BindPetSnapshotTailForIndependentRuntime and again, from the same
            // Model value, in BindPetForIndependentRuntime. This block is the only snapshot writer
            // of the triple; the entry BindPet remains at ReferenceFirstWorldInitialFrame.
            // The Gossip observation adapter no longer owns a teardown Empty write. On the
            // foreground path a Tick that stops below this gate releases the window before the
            // next draw, so the old split of rows and tail was not visible there.
            CharData.BindPetSnapshotTailForIndependentRuntime(status.Pet);
            CharData.BindPetForIndependentRuntime(
                status.Pet.Show,
                status.Pet.ShowKnown,
                status.Pet.Ridable,
                status.Pet.Tianmo,
                status.Pet.ShowTianmo,
                CharData.ShowMountRing,
                status.Pet.Model);

            playerStatusVersion = nextPlayerStatusVersion;
        }

        long nextSelfPkStateVersion = session.SelfPkStateVersion;
        WorldSelfPkState selfPkState = session.SelfPkState;
        if (selfPkState != null &&
            (force || nextSelfPkStateVersion != selfPkStateVersion))
        {
            if (!playerStatusHud.ApplyAuthoritativeSelfPkState(status, selfPkState))
            {
                return false;
            }
        }

        if (frame != null &&
            !frame.ApplyLocalPlayerStatus(status, selfPkState))
        {
            return false;
        }

        selfPkStateVersion = nextSelfPkStateVersion;
        return true;
    }

    internal void ProjectPlayerHairForIndependentRuntime(int head, int hair)
    {
        if (character != null) character = character.WithHair(head, hair);
    }

    private bool SyncPlayerAppearance(bool force, Color32 tint)
    {
        if (frame == null || localEquipmentDialog == null || session == null)
        {
            return false;
        }

        long nextVersion = -1;
        WorldPlayerAppearanceState next = null;
        for (int attempt = 0; attempt < 3; attempt++)
        {
            long before = session.PlayerAppearanceStateVersion;
            WorldPlayerAppearanceState candidate = session.PlayerAppearanceState;
            long after = session.PlayerAppearanceStateVersion;
            if (before == after)
            {
                nextVersion = after;
                next = candidate;
                break;
            }
        }

        if (nextVersion < 0)
        {
            return false;
        }

        if (!force && nextVersion == playerAppearanceStateVersion)
        {
            return true;
        }

        if (next == null)
        {
            playerAppearanceStateVersion = nextVersion;
            return true;
        }

        if (next.EntityId != 0 && next.EntityId != character.EntityId)
        {
            return false;
        }

        // Native appearance/effect/pref writes have already finished at receipt. This
        // retained snapshot updates only the old frame and equipment observations.
        WorldEntryRecord nextCharacter = character.WithAppearance(
            next.Appearance,
            next.GlowMode,
            next.GlowUnlock);
        if (!localEquipmentDialog.CanApplyAppearance(nextCharacter) ||
            !frame.ApplyLocalPlayerAppearance(nextCharacter, tint) ||
            !localEquipmentDialog.ApplyAppearance(nextCharacter))
        {
            return false;
        }

        character = nextCharacter;
        playerAppearanceStateVersion = nextVersion;
        return true;
    }

    // Official CombatWorld.NearbyPlayers (0x0600013e) is the single source of the invite dialog's
    // nearby list: it enumerates CombatWorld.I.players - the same dictionary every remote actor is
    // registered into by AddRemoteReceiveActor - and applies the one official filter, the Unity-null
    // test of each RemotePlayer. The session snapshot is not a second source, and no self-exclusion,
    // sort or de-duplication is applied here or anywhere else on this path.
    private IReadOnlyList<PartyInviteTarget> BuildNearbyPartyTargets()
    {
        List<PartyInviteTarget> result = new List<PartyInviteTarget>();
        List<(int, string)> nearby = CombatWorld.NearbyPlayers();
        for (int index = 0; index < nearby.Count; index++)
            result.Add(new PartyInviteTarget(nearby[index].Item1, nearby[index].Item2));
        return result;
    }

    private ReferencePlayerActionMenuSnapshot CreatePlayerActionMenuSnapshot(
        ReferenceGossipPlayerMenuIntent intent)
    {
        if (intent == null)
        {
            return null;
        }

        int targetEntityId = 0;
        if (!string.IsNullOrEmpty(intent.TargetName) && frame != null)
        {
            foreach (System.Collections.Generic.KeyValuePair<
                         int,
                         ReferenceWorldRemotePlayerActor> pair in frame.RemoteActors)
            {
                ReferenceWorldRemotePlayerActor actor = pair.Value;
                if (actor != null && actor.State != null &&
                    string.Equals(
                        actor.State.DisplayName,
                        intent.TargetName,
                        StringComparison.Ordinal))
                {
                    targetEntityId = pair.Key;
                    break;
                }
            }
        }

        return CreatePlayerActionMenuSnapshot(
            targetEntityId,
            intent.TargetName,
            intent.CopyLine,
            intent.CopyBlock);
    }

    private ReferencePlayerActionMenuSnapshot CreatePlayerActionMenuSnapshot(
        int targetEntityId,
        string targetName,
        string copyLine,
        string copyBlock)
    {
        WorldPartyState party = session.PartyState ?? WorldPartyState.Empty;
        WorldFamilyState family = SocialClient.FamilyStateForIndependentRuntime;
        WorldStallOwnerState stallOwner;
        bool targetIsStallOwner = session.StallOwners != null &&
            session.StallOwners.TryGetValue(targetEntityId, out stallOwner) &&
            stallOwner != null;

        WorldRidingState localRiding = session.LocalRidingState;
        int rideTogetherTarget = localRiding == null ? 0 : localRiding.RideAnchorId;
        bool localCanRideTogether = localRiding == null || !localRiding.Riding;
        // Official PlayerActionMenu.Build (0x06000974) reaches this row through
        // CombatWorld.FindPlayer (0x0600013b) and the Unity instance-null test, and only then
        // calls RemotePlayer.get_CanCarryPassenger (0x06000b25): arm64 LAB_002924c0..LAB_0029262c
        // uses UnityEngine.Object.op_Inequality, x86_64 LAB_002a12aa..LAB_002a1492 uses the same
        // overload, and both dereference the instance right after the test and fall into a
        // non-returning throw helper (arm64 FUN_00177a74 / x86_64 FUN_0016c1b0) on a raw-null
        // pointer - an instance test, never a silent skip. CombatWorld.I.players is not a second
        // copy of the actor cache: AddRemoteReceiveActor and the authoritative replacement
        // register the actor's own SourceIdentityForIndependentRuntime into it
        // through the original SpawnPlayer/Ensure path. Frames borrow that same current
        // independent owner; they do not construct a competing per-map component.
        // A target with no live remote owner therefore
        // answers null here, which is also the official answer.
        RemotePlayer remoteIdentity = CombatWorld.FindPlayer(targetEntityId);
        bool targetCanCarryPassenger =
            remoteIdentity != null && remoteIdentity.CanCarryPassenger;

        return new ReferencePlayerActionMenuSnapshot(
            targetEntityId,
            targetName,
            character.EntityId,
            targetIsStallOwner,
            // Official 0600097b is only member.Item1 == this.targetId. ContainsEntity's
            // extra zero-id rejection is not part of this menu predicate.
            party.Members.Any(member => member.EntityId == targetEntityId),
            party.Members.Count,
            party.LeaderEntityId,
            session.FriendState.Entries.Any(entry => entry.Item1 == targetName),
            rideTogetherTarget,
            localCanRideTogether,
            targetCanCarryPassenger,
            family.MyPosition == 0 || family.CanInvite,
            copyLine,
            copyBlock,
            localRiding == null ? 0 : localRiding.RideSeats,
            session.BlockState.Entries.Any(entry => entry.Item2 == targetName));
    }

    private bool IsPointerAttackableMob(
        ReferenceWorldPointerTargetKind kind,
        int entityId)
    {
        if (kind == ReferenceWorldPointerTargetKind.Monster)
        {
            ReferenceWorldMonsterActor monster;
            return frame != null &&
                   frame.MonsterActors.TryGetValue(entityId, out monster) &&
                   monster != null && monster.State != null &&
                   monster.State.FriendlyFlag == 0 &&
                   monster.State.HitPointsOrRadiusPixels > 0;
        }

        if (kind != ReferenceWorldPointerTargetKind.RemotePlayer)
        {
            return false;
        }

        int localFaction;
        bool hostile;
        return TryResolveRemoteHostility(entityId, out localFaction, out hostile) && hostile;
    }

    private bool ApplyManualMovementPlan(ReferenceWorldManualMovementPlan plan)
    {
        if (frame == null || frame.Player == null)
        {
            return false;
        }

        if (plan.StopAttack)
        {
            CombatWorld.StopAttack();
        }

        bool pathReady = !plan.SetPath || frame.Player.TrySetPathTo(
            collisionMask, plan.TargetX, plan.TargetY);
        if (plan.StandAfterPath) PlayerController.I?.BreakSit();
        if (!pathReady) return false;

        // EmitRing is void in the official body: a throttled or sprite-less frame emits nothing
        // and never turns this pointer pass into a failure.
        if (plan.EmitRing) PlayerController.I?.EmitRing(plan.TargetX, plan.TargetY);
        return true;
    }

    /// <summary>
    /// Single binding of the click-ring sprite into the official <c>PlayerController</c> field.
    /// A missing texture leaves that field null, which silences emission exactly as the official
    /// body does; it is not a manual-movement failure and never aborts world entry.
    /// </summary>
    /// <remarks>
    /// The Sprite this host builds is the adapter's own Unity object: nothing else owns or destroys
    /// it. Destroying the world root releases the <c>PlayerController</c> component whose field
    /// holds it, but Unity never destroys assets with their holder, so this owner keeps the
    /// reference and releases it with the world, exactly as the retired emitter did. The reviewed
    /// texture behind it stays owned by the ReferenceResourceLoader adapter cache and is not
    /// touched here.
    /// </remarks>
    private void BindClickRingSpriteForIndependentRuntime(ReferenceResourceLoader resourceLoader)
    {
        PlayerController player = PlayerController.I;
        if (player == null)
        {
            return;
        }

        Texture2D texture = resourceLoader.LoadReviewedPng(ClickRingTexturePath);
        if (texture == null)
        {
            return;
        }

        Sprite sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            player.ppu);
        ownedClickRingSprite = sprite;
        player.SetRingSpriteForIndependentRuntime(sprite);
    }

    internal void TrackPositionReportForIndependentRuntime(Task<Result> pending)
    {
        pendingPositionReports.Add(pending);
    }

    private bool ObservePositionReports()
    {
        for (int index = pendingPositionReports.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingPositionReports[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastPositionReportFailureCode = "player.position.report.exception";
            }
            else if (pending.IsCanceled)
            {
                LastPositionReportFailureCode = "player.position.report.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastPositionReportFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastPositionReportFailureCode = string.Empty;
            }

            pendingPositionReports.RemoveAt(index);
        }

        return true;
    }

    private bool AdvancePendingGroundPickup()
    {
        if (frame == null || frame.Player == null) return false;
        int requested = frame.Player.SourceControllerForIndependentRuntime.AdvancePickupForIndependentRuntime();
        if (requested >= 0)
        {
            lastGroundPickupRequestedIdForGroundLoot = requested;
            groundPickupRequestIssuedCount++;
        }
        return true;
    }

    private bool ObserveGroundPickupRequests()
    {
        for (int index = pendingGroundPickupRequests.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingGroundPickupRequests[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastGroundPickupFailureCode = "ground-item.pickup.exception";
            }
            else if (pending.IsCanceled)
            {
                LastGroundPickupFailureCode = "ground-item.pickup.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastGroundPickupFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastGroundPickupFailureCode = string.Empty;
                groundPickupRequestSucceededCount++;
            }

            pendingGroundPickupRequests.RemoveAt(index);
        }

        return true;
    }

    private bool HasBasicAttackLineOfSight(
        float fromX,
        float fromY,
        float toX,
        float toY)
    {
        return PlayerController.LineOfSight(fromX, fromY, toX, toY);
    }

    private bool ObserveBasicAttackRequests()
    {
        for (int index = pendingBasicAttackRequests.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingBasicAttackRequests[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastBasicAttackFailureCode = "combat.attack.exception";
            }
            else if (pending.IsCanceled)
            {
                LastBasicAttackFailureCode = "combat.attack.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastBasicAttackFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastBasicAttackFailureCode = string.Empty;
                basicAttackRequestSucceededCount++;
            }

            pendingBasicAttackRequests.RemoveAt(index);
        }

        return true;
    }

    private bool SyncReviveChoiceState(bool force)
    {
        if (reviveChoiceDialog == null || !reviveChoiceDialog.Ready)
        {
            return false;
        }

        long version = session.LocalPlayerLifeStateVersion;
        if (!force && version == localPlayerLifeStateVersion)
        {
            return true;
        }

        WorldLocalPlayerLifeState state = session.LocalPlayerLifeState;
        localPlayerLifeStateVersion = version;
        if (!reviveChoiceDialog.ObserveReceipt(state))
            return true;
        if (state != null && state.NativeActionsHandled)
            return true;
        if (state == null || state.EntityId != character.EntityId || state.Alive)
        {
            reviveChoiceDialog.Hide();
            return true;
        }

        if (CustomMaps.IsCustom(character.MapId))
            return true;
        return reviveChoiceDialog.Open(state.ReviveFlags);
    }

    internal void TrackReviveRequestForIndependentRuntime(Task<Result> pending)
    {
        if (!disposed && pending != null)
            pendingReviveChoiceRequests.Add(pending);
    }

    private bool ObserveReviveChoiceRequests()
    {
        for (int index = pendingReviveChoiceRequests.Count - 1; index >= 0; index--)
        {
            Task<Result> pending = pendingReviveChoiceRequests[index];
            if (!pending.IsCompleted)
            {
                continue;
            }

            if (pending.IsFaulted)
            {
                _ = pending.Exception;
                LastReviveChoiceFailureCode = "combat.revive.choice.exception";
            }
            else if (pending.IsCanceled)
            {
                LastReviveChoiceFailureCode = "combat.revive.choice.cancelled";
            }
            else if (!pending.Result.IsSuccess)
            {
                LastReviveChoiceFailureCode = pending.Result.Error.Code;
            }
            else
            {
                LastReviveChoiceFailureCode = string.Empty;
            }

            pendingReviveChoiceRequests.RemoveAt(index);
        }

        return true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceFirstWorldWindowOwner));
        }
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsNaN(value) && !float.IsInfinity(value);
    }

    private static bool IsNonNegativeFinite(float value)
    {
        return value >= 0f && !float.IsNaN(value) && !float.IsInfinity(value);
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
}

public enum ReferenceFirstWorldWindowFailure
{
    None = 0,
    AlreadyBuilt = 1,
    UnsupportedMap = 2,
    InvalidViewport = 3,
    MissingSourceAsset = 4,
    InvalidMapAsset = 5,
    InvalidTileAsset = 6,
    InvalidShapeAsset = 7,
    InitialFrameRejected = 8,
    UnexpectedConstructionFailure = 9,
    NotBuilt = 10,
    InvalidTickInput = 11,
    RevisionRejected = 12,
    ActorAdvanceRejected = 13,
    InvalidMaskAsset = 14,
    InvalidMusicAsset = 15,
    InvalidHudResources = 16,
    InvalidPlayerStatusHud = 17,
    InvalidControlBar = 18,
    InvalidTargetHud = 19,
    PointerInputRejected = 20,
    InvalidUiTip = 21,
    InvalidPlayerActionMenu = 22,
    InvalidChatUi = 23,
    InvalidGossipDialog = 24,
    InvalidInspectedEquipmentDialog = 25,
    InvalidSystemInfoBar = 26,
    InvalidDealDialog = 27,
    InvalidInventoryDialog = 28,
    InvalidDealingMark = 29,
    InvalidInventoryItemUse = 30,
    InvalidLocalEquipmentDialog = 31,
    InvalidHotbar = 32,
    InvalidBuffHud = 33,
    InvalidMinimapHud = 34,
    InvalidNpcDialog = 35,
    InvalidBasicAttack = 36,
    InvalidCombatSoundAsset = 37,
    InvalidReviveChoiceDialog = 38,
    InvalidGroundPickup = 39,
    InvalidPlayerTaskTrack = 40,
    InvalidManualMovement = 41,
    InvalidLocalSkillCast = 42,
    InvalidLocalStatusEffect = 43,
    InvalidPlayerStatusRevision = 44,
    WorldRevisionRejected = 45,
    InvalidMapLinks = 46,
    InvalidSkillDialog = 47,
    InvalidPlayerAppearance = 48
}

}
