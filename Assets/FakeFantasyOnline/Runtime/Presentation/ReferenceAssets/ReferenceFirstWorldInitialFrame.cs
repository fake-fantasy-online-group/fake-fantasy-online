using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using FakeFantasyOnline.Application;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Owns the recovered first-map static composition and authoritative local-player stand frame.
/// </summary>
/// <remarks>
/// This is the source-owned composition boundary that gives CameraFollow the actual player
/// Transform. It admits ordinary remote paths, attack/magic action edges,
/// ordinary-idle and model-driven mount revision updates, the distinct remote death/revive precedence,
/// the official generic static NPC spawn/removal projection, and generic monster
/// standing/walking/name/head-HP projection from the entity stream, plus the local
/// follow and collision/SetPathTo/movement/Transform path for mounted and unmounted players.
/// It still excludes action visual/audio effects owned by adjacent combat slices,
/// pets/traps, the adjacent window-owned map-music lifecycle,
/// the complete HUD/input owner and every user-visible claim that requires a rendered reference-client
/// comparison. The adjacent actual-window owner supplies the HUD and selection routes.
/// </remarks>
public sealed partial class ReferenceFirstWorldInitialFrame : IDisposable
{
    public const int FirstMapId = 60;
    private const int SnapshotReadAttempts = 4;
    private static readonly IReadOnlyDictionary<int, WorldRemoteActionState>
        EmptyRemoteActions =
            new ReadOnlyDictionary<int, WorldRemoteActionState>(
                new Dictionary<int, WorldRemoteActionState>());
    private static readonly IReadOnlyDictionary<int, WorldRemoteChatBubbleState>
        EmptyRemoteChatBubbles =
            new ReadOnlyDictionary<int, WorldRemoteChatBubbleState>(
                new Dictionary<int, WorldRemoteChatBubbleState>());

    private readonly ReferenceGsmMap map;
    private readonly ReferenceTileImageLibrary tiles;
    private readonly ReferenceTerrainShapeLibrary shapes;
    private readonly ReferenceResourceLoader resources;
    private readonly Transform root;
    private readonly Camera camera;
    private readonly MapDef.Entry mapDescriptor;
    private WorldEntryRecord character;
    private readonly Material additiveMaterial;
    private bool hideOtherPets;
    private readonly ReferenceWorldCombatSound combatSound;
    private readonly ISet<string> processNpcNames;
    private string pendingFollowFeedback = string.Empty;
    private Action<string, Color> followMessageForIndependentRuntime;

    private ReferenceWorldLocalPlayerDoll player;
    private PlayerNameTag playerNameTag;
    private ReferenceWorldRemoteFeedbackAssets remoteFeedbackAssets;
    private ReferenceWorldNpcTaskFeedback npcTaskFeedback;
    private ReferenceNpcGreetingCatalog npcGreetingCatalog;
    private ReferenceItemIconCatalog itemIconCatalog;
    private ReferenceWorldTargetSelection targetSelection;
    private ReferenceWorldCombatFeedback combatFeedback;
    private CombatWorld combatWorld => CombatWorld.CurrentForIndependentRuntime;
    private Func<bool> beforeMovementTransfer;
    internal void BindMovementTransferSlotForIndependentRuntime(Func<bool> pickup) => beforeMovementTransfer = pickup;
    private bool chatTypingForIndependentRuntime;
    private bool gossipTypingForPickup;
    private Action<string, Color> pickupMessage;
    internal void BindPickupInputForIndependentRuntime(bool gossipTyping, Action<string, Color> message)
    {
        gossipTypingForPickup = gossipTyping;
        pickupMessage = message;
    }
    private byte immobileForIndependentRuntime;
    private int rideAnchorForIndependentRuntime;
    private bool ridingForIndependentRuntime;
    private System.Action<string> sitMessageForIndependentRuntime;
    private bool mouseFollowForIndependentRuntime;
    private System.Action stopAutoAttackForIndependentRuntime;
    private readonly Dictionary<int, ReferenceWorldRemotePlayerActor> remoteActors =
        new Dictionary<int, ReferenceWorldRemotePlayerActor>();
    private readonly Dictionary<int, ReferenceWorldNpcActor> npcActors =
        new Dictionary<int, ReferenceWorldNpcActor>();
    private readonly Dictionary<int, ReferenceWorldMonsterActor> monsterActors =
        new Dictionary<int, ReferenceWorldMonsterActor>();
    private readonly Dictionary<int, ReferenceWorldGroundItemActor> groundItemActors =
        new Dictionary<int, ReferenceWorldGroundItemActor>();
    private readonly Dictionary<int, WorldRemotePathState> appliedRemotePaths =
        new Dictionary<int, WorldRemotePathState>();
    private readonly Dictionary<int, WorldRemotePathState> appliedMonsterPaths =
        new Dictionary<int, WorldRemotePathState>();
    private readonly Dictionary<int, WorldRemoteActionState> appliedRemoteActions =
        new Dictionary<int, WorldRemoteActionState>();
    private readonly IReadOnlyDictionary<int, ReferenceWorldRemotePlayerActor>
        readOnlyRemoteActors;
    private readonly IReadOnlyDictionary<int, ReferenceWorldNpcActor> readOnlyNpcActors;
    private readonly IReadOnlyDictionary<int, ReferenceWorldMonsterActor>
        readOnlyMonsterActors;
    private readonly IReadOnlyDictionary<int, ReferenceWorldGroundItemActor>
        readOnlyGroundItemActors;
    private ReferenceFirstWorldStaticComposite staticComposite;
    private long lastLocalRidingStateVersion = -1;
    private long lastLocalPlayerLifeStateVersion = -1;
    private long lastRemotePlayersVersion = -1;
    private long lastNpcsVersion = -1;
    private long lastWorldEntitiesVersion = -1;
    private long lastWorldEntitySceneGeneration = -1;
    private readonly HashSet<int> terminalWorldEntityTombstones =
        new HashSet<int>();
    private long worldEntityActorRebuildGeneration;
    private int lastWorldEntitySnapshotMapId;
    private int lastWorldEntitySnapshotCount;
    private ReferenceWorldEntityReconciliationFailure
        lastWorldEntityReconciliationFailure;
    private long lastGroundItemsVersion = -1;
    private long lastCombatResultsVersion = -1;
    private long lastCombatNumbersVersion = -1;
    private long lastCombatResultRevision;
    private readonly WorldCombatNumberPresentation standaloneCombatNumbers =
        new WorldCombatNumberPresentation();
    private long lastRemotePathsVersion = -1;
    private long lastMonsterPathsVersion = -1;
    private long lastRemoteActionsVersion = -1;
    private long lastRemoteChatBubblesVersion = -1;
    private long lastPartyStateVersion = -1;
    private long lastStallOwnersVersion = -1;
    private int lastNameMode = -1;
    private bool localPlayerDead;
    private bool built;
    private bool disposed;

    public ReferenceFirstWorldInitialFrame(
        ReferenceGsmMap map,
        ReferenceTileImageLibrary tiles,
        ReferenceTerrainShapeLibrary shapes,
        ReferenceResourceLoader resources,
        Transform root,
        Camera camera,
        WorldEntryRecord character,
        Material additiveMaterial = null,
        bool hideOtherPets = false,
        ReferenceWorldCombatSound combatSound = null,
        ISet<string> processNpcNames = null)
        : this(
            map,
            tiles,
            shapes,
            resources,
            root,
            camera,
            new MapDef.Entry
            {
                id = character.MapId,
                file = "qw" + character.MapId.ToString("D4"),
                name = string.Empty,
                isCity = false
            },
            character,
            additiveMaterial,
            hideOtherPets,
            combatSound,
            processNpcNames)
    {
    }

    public ReferenceFirstWorldInitialFrame(
        ReferenceGsmMap map,
        ReferenceTileImageLibrary tiles,
        ReferenceTerrainShapeLibrary shapes,
        ReferenceResourceLoader resources,
        Transform root,
        Camera camera,
        MapDef.Entry mapDescriptor,
        WorldEntryRecord character,
        Material additiveMaterial = null,
        bool hideOtherPets = false,
        ReferenceWorldCombatSound combatSound = null,
        ISet<string> processNpcNames = null)
    {
        this.map = map ?? throw new ArgumentNullException(nameof(map));
        this.tiles = tiles;
        this.shapes = shapes;
        this.resources = resources ?? throw new ArgumentNullException(nameof(resources));
        this.root = root == null ? throw new ArgumentNullException(nameof(root)) : root;
        // BuildWorld continues when neither native camera selector finds a camera.
        this.camera = camera;
        this.mapDescriptor = mapDescriptor;
        this.character = character ?? throw new ArgumentNullException(nameof(character));
        this.additiveMaterial = additiveMaterial;
        this.hideOtherPets = hideOtherPets;
        this.combatSound = combatSound;
        this.processNpcNames = processNpcNames;
        readOnlyRemoteActors =
            new ReadOnlyDictionary<int, ReferenceWorldRemotePlayerActor>(remoteActors);
        readOnlyNpcActors =
            new ReadOnlyDictionary<int, ReferenceWorldNpcActor>(npcActors);
        readOnlyMonsterActors =
            new ReadOnlyDictionary<int, ReferenceWorldMonsterActor>(monsterActors);
        readOnlyGroundItemActors =
            new ReadOnlyDictionary<int, ReferenceWorldGroundItemActor>(groundItemActors);

        if (!map.Ok && !map.HasCover && !map.HasTextureBackgroundForIndependentRuntime)
        {
            throw new ArgumentException(
                "The first-world initial frame requires a parsed terrain map or cover.",
                nameof(map));
        }

        if (character.MapId != mapDescriptor.id)
        {
            throw new ArgumentException(
                "The reviewed initial-frame map does not match the authoritative map id.",
                nameof(character));
        }
    }

    public bool Built => built;
    public ReferenceWorldLocalPlayerDoll Player => player;
    internal CombatWorld CombatOwnerForIndependentRuntime => combatWorld;
    internal PlayerNameTag LocalPlayerNameTag => playerNameTag;
    // Retired diagnostic surface; product rendering is owned by PlayerController.
    internal ReferenceWorldLocalMountRider LocalMountRider => null;
    internal Action<PlayerController> WorkConfirmationForIndependentRuntime;
    private bool LocalRideActive => CharData.riding || CharData.rideAnchorId != 0;
    internal ReferenceWorldTargetSelection TargetSelection => targetSelection;
    internal IReadOnlyDictionary<int, ReferenceWorldRemotePlayerActor> RemoteActors =>
        readOnlyRemoteActors;
    internal IReadOnlyDictionary<int, ReferenceWorldNpcActor> NpcActors => readOnlyNpcActors;
    internal IReadOnlyDictionary<int, ReferenceWorldMonsterActor> MonsterActors =>
        readOnlyMonsterActors;
    internal IReadOnlyDictionary<int, ReferenceWorldGroundItemActor> GroundItemActors =>
        readOnlyGroundItemActors;
    public long LastLocalRidingStateVersion => lastLocalRidingStateVersion;
    public long LastLocalPlayerLifeStateVersion => lastLocalPlayerLifeStateVersion;
    public bool LocalPlayerDead => localPlayerDead;
    public long LastRemotePlayersVersion => lastRemotePlayersVersion;
    public long LastNpcsVersion => lastNpcsVersion;
    public long LastWorldEntitiesVersion => lastWorldEntitiesVersion;
    public long LastWorldEntitySceneGeneration => lastWorldEntitySceneGeneration;
    public long WorldEntityActorRebuildGeneration =>
        worldEntityActorRebuildGeneration;
    public int LastWorldEntitySnapshotMapId => lastWorldEntitySnapshotMapId;
    public int LastWorldEntitySnapshotCount => lastWorldEntitySnapshotCount;
    public ReferenceWorldEntityReconciliationFailure
        LastWorldEntityReconciliationFailure =>
            lastWorldEntityReconciliationFailure;
    public long LastGroundItemsVersion => lastGroundItemsVersion;
    public long LastCombatResultsVersion => lastCombatResultsVersion;
    public long LastCombatNumbersVersion => lastCombatNumbersVersion;
    internal long LastCombatResultRevisionForRealCombatTail =>
        lastCombatResultRevision;
    public long LastRemotePathsVersion => lastRemotePathsVersion;
    public long LastMonsterPathsVersion => lastMonsterPathsVersion;
    public long LastRemoteActionsVersion => lastRemoteActionsVersion;
    public long LastRemoteChatBubblesVersion => lastRemoteChatBubblesVersion;
    public long LastPartyStateVersion => lastPartyStateVersion;
    public long LastStallOwnersVersion => lastStallOwnersVersion;

    public ReferenceWorldRevisionFailure LastRevisionFailure { get; private set; }
    public int LastNameMode => lastNameMode;
    public ReferenceFirstWorldStaticComposite StaticComposite => staticComposite;
    public ReferenceWorldCombatSound CombatSound => combatSound;
    public MapDef.Entry MapDescriptor => mapDescriptor;
    public WorldEntryRecord Character => character;
    internal float MapWidthForIndependentRuntime => map.MapWidth;
    internal float MapHeightForIndependentRuntime => map.MapHeight;
    public CameraFollow CameraFollow =>
        staticComposite == null ? null : staticComposite.CameraFollow;
    public int FollowTargetEntityId => PlayerController.FollowIdForIndependentRuntime;
    internal ReferenceWorldNpcTaskFeedback NpcTaskFeedback => npcTaskFeedback;
    internal ReferenceNpcGreetingCatalog NpcGreetingCatalog => npcGreetingCatalog;
    internal ReferenceWorldBitmapFont BitmapFont =>
        remoteFeedbackAssets == null ? null : remoteFeedbackAssets.Font;

    public void ApplyPetViewMask(int mask)
    {
        hideOtherPets = ReferenceWorldPetViewSettings.HasBit(
            mask,
            ReferenceWorldPetViewSettings.HideOtherPetsBit);
    }

    /// <summary>
    /// Applies the authoritative local appearance root without rebuilding the world/camera graph.
    /// </summary>
    public bool ApplyLocalPlayerAppearance(
        WorldEntryRecord nextCharacter,
        Color32 tint)
    {
        ThrowIfDisposed();
        if (!built || player == null || nextCharacter == null ||
            nextCharacter.EntityId != character.EntityId ||
            !player.ApplyAppearance(nextCharacter, player.WorldY, tint))
        {
            return false;
        }

        character = nextCharacter;
        return true;
    }

#if UNITY_EDITOR
    internal static ReferenceFirstWorldInitialFrame CreateAuditFrame(
        ReferenceGsmMap map,
        ReferenceTileImageLibrary tiles,
        ReferenceTerrainShapeLibrary shapes,
        ReferenceResourceLoader resources,
        Transform root,
        Camera camera,
        int mapId,
        float x,
        float y,
        int gender,
        int head,
        int hair,
        int body,
        int cap,
        int backpack,
        int rightWeapon,
        int leftWeapon,
        int face)
    {
        WorldEntryRecord auditCharacter = new WorldEntryRecord(
            1,
            "audit",
            "audit",
            string.Empty,
            0,
            gender,
            head,
            hair,
            mapId,
            x,
            y,
            Array.Empty<int>(),
            new CharacterAppearanceRecord(
                body,
                cap,
                backpack,
                rightWeapon,
                leftWeapon,
                face,
                0,
                0,
                0),
            new WorldPetRecord(0, string.Empty, 0),
            null,
            null,
            null,
            null,
            null);
        MapDef.EnsureLoaded(resources);
        MapDef.Entry descriptor;
        if (!MapDef.TryGet(mapId, out descriptor))
        {
            descriptor = new MapDef.Entry
            {
                id = mapId,
                file = "qw" + mapId.ToString("D4"),
                name = string.Empty,
                isCity = false
            };
        }

        return new ReferenceFirstWorldInitialFrame(
            map,
            tiles,
            shapes,
            resources,
            root,
            camera,
            descriptor,
            auditCharacter);
    }
#endif

    internal Action<Transform, float, float, float> MapObjectsPlacedForIndependentRuntime { get; set; }
    internal GameBootstrap NpcSpawnOwnerForIndependentRuntime { get; set; }
    internal Action<int, bool, byte> PendingLifeAppliedForIndependentRuntime { get; set; }

    internal GameBootstrap.BuildWorldCheckpoints BuildCheckpointsForIndependentRuntime { get; set; }
    internal Action<PlayerController> LocalPlayerCreatedForIndependentRuntime { get; set; }

    internal void AdoptFloorForIndependentRuntime(ReferenceFirstWorldStaticComposite floor)
    {
        ThrowIfDisposed();
        if (built || staticComposite != null)
            throw new InvalidOperationException("The world frame already owns its floor.");
        staticComposite = floor ?? throw new ArgumentNullException(nameof(floor));
    }

    internal bool PrepareFloorForIndependentRuntime()
    {
        ThrowIfDisposed();
        if (built || staticComposite != null)
            return false;
        staticComposite = new ReferenceFirstWorldStaticComposite(
            map, tiles, shapes, resources, root, camera,
            NpcSpawnOwnerForIndependentRuntime ?? GameBootstrap.I);
        staticComposite.BuildCheckpointsForIndependentRuntime = BuildCheckpointsForIndependentRuntime;
        return staticComposite.BuildFloorForIndependentRuntime();
    }

    public bool BuildForViewport(int viewportWidth, int viewportHeight, float dpiScale)
    {
        ThrowIfDisposed();
        if (built)
        {
            return false;
        }

        try
        {
            if (staticComposite == null && !PrepareFloorForIndependentRuntime())
            {
                ReleaseOwnedState();
                return false;
            }

            player = new ReferenceWorldLocalPlayerDoll(
                map,
                resources,
                root,
                character,
                additiveMaterial);
            player.CallerOwnsConstructionFailureForIndependentRuntime =
                TransitionOwnsConstructionFailureForIndependentRuntime;
            player.NativeControllerCreatedForIndependentRuntime = LocalPlayerCreatedForIndependentRuntime;
            if (!player.Build() || player.Root == null)
            {
                ReleaseOwnedState();
                return false;
            }

            bool pendingLife = NetClient.ApplyPendingSelfStateForIndependentRuntime(out bool alive, out byte reviveFlags);
            ObserveNativeLifeForIndependentRuntime(PlayerController.I);
            if (pendingLife)
                PendingLifeAppliedForIndependentRuntime?.Invoke(character.EntityId, alive, reviveFlags);

            // BuildWorld reads the live controller coordinates after pending self-state,
            // then positions the new Player before positioning/binding its camera.
            player.Root.position = new Vector3(
                (player.WorldX - map.MapWidth * 0.5f) /
                    ReferenceFirstWorldStaticComposite.PixelsPerUnit,
                (map.MapHeight * 0.5f - player.WorldY) /
                    ReferenceFirstWorldStaticComposite.PixelsPerUnit,
                0f);

            if (!staticComposite.CompleteForPlayerForIndependentRuntime(player.Root))
            {
                ReleaseOwnedState();
                return false;
            }

            MapObjectsPlacedForIndependentRuntime?.Invoke(
                root, map.MapWidth, map.MapHeight, ReferenceFirstWorldStaticComposite.PixelsPerUnit);

            if (!ReferenceWorldRemoteFeedbackAssets.TryCreate(
                    resources,
                    out remoteFeedbackAssets))
            {
                ReleaseOwnedState();
                return false;
            }

            bool nativeStartup = NpcSpawnOwnerForIndependentRuntime != null &&
                NpcSpawnOwnerForIndependentRuntime.HasNativeStartupWorldForIndependentRuntime;
            WorldPlayerStatusState initialStatus = nativeStartup
                ? null : WorldPlayerStatusState.FromEntry(character);
            playerNameTag = player.Root.gameObject.GetComponent<PlayerNameTag>();
            if (playerNameTag == null || !playerNameTag.Configure(
                    player,
                    remoteFeedbackAssets,
                    character.DisplayName,
                    character.Title,
                    character.EntityId,
                    nativeStartup ? CharData.hp : initialStatus.HitPoints,
                    nativeStartup ? CharData.maxHp : initialStatus.MaximumHitPoints,
                    nativeStartup))
            {
                ReleaseOwnedState();
                return false;
            }

            if (!ReferenceWorldNpcTaskFeedback.TryCreate(
                    resources,
                    out npcTaskFeedback))
            {
                npcTaskFeedback = null;
            }

            if (!ReferenceNpcGreetingCatalog.TryCreate(
                    resources,
                    processNpcNames,
                    out npcGreetingCatalog))
            {
                npcGreetingCatalog = null;
            }

            itemIconCatalog = new ReferenceItemIconCatalog(resources);
            targetSelection = new ReferenceWorldTargetSelection();
            combatFeedback = new ReferenceWorldCombatFeedback(resources, root);
            // CombatWorld is an independent native scene owner. Spawn/target/skill
            // entry points create it through Ensure; a map merely borrows that owner.
            CharData.race = character.Race;
            CombatNumber.BindPlayerTransformForIndependentRuntime(player.Root);
            // GameBootstrap.PlayerT is the static CombatWorld.PlayEntityEffect reads for its id 0
            // and self-id arm. The official owner writes it inside GameBootstrap.BuildWorld from
            // the same doll root, and the independent-runtime frame entry is the path that stands
            // in for that graph, so it binds the same object here rather than leaving the owner's
            // local arm without a transform.
            GameBootstrap.PlayerT = player.Root;
            NetClient.SelfEntityId = character.EntityId;
            CharData.charId = character.PersistentCharacterId.GetValueOrDefault();
            selfIdentityBound = true;
            CharData.BindPreferenceIdentity(
                character.PersistentCharacterId,
                character.DisplayName);
            if (!nativeStartup)
            {
                WorldPlayerPetStatusState entryPet =
                    WorldPlayerPetStatusState.FromEntry(character.Pet);
                CharData.BindPetForIndependentRuntime(
                    entryPet.Show,
                    entryPet.ShowKnown,
                    entryPet.Ridable,
                    entryPet.Tianmo,
                    entryPet.ShowTianmo,
                    CharData.ShowMountRing,
                    entryPet.Model);
            }

            built = true;
            return true;
        }
        catch
        {
            if (!TransitionOwnsConstructionFailureForIndependentRuntime) ReleaseOwnedState();
            throw;
        }
    }

    internal bool TransitionOwnsConstructionFailureForIndependentRuntime { get; set; }

    /// <summary>Observe the live camera without requesting a second camera update.</summary>
    public Camera InspectCameraForDiagnostics()
    {
        ThrowIfDisposed();
        return staticComposite == null ? null : staticComposite.InspectCameraForDiagnostics();
    }

    /// <summary>
    /// Applies a dedicated server-authored self-riding state on Unity's main thread.
    /// </summary>
    /// <remarks>
    /// A null state is unknown and fails closed. The ordinary branch accepts package-backed,
    /// single-seat, non-anchor and non-tianmo mounts with no rider-pose override; an authoritative
    /// false state restores the normal stand path. The caller owns revision polling from
    /// ClientSessionCoordinator.
    /// </remarks>
    public bool ApplyLocalRidingState(WorldRidingState state, float standTimeSeconds, float mapY, Color32 riderTint)
    {
        ThrowIfDisposed();
        if (!built || player == null || state == null) return false;
        player.BindNativeRidingForIndependentRuntime(state);
        playerNameTag?.BindNativeRidingForIndependentRuntime();
        return true;
    }

    public bool AdvanceLocalMount(bool moving, float deltaTime, float mapY, Color32 riderTint)
    {
        if (!built || player == null) return false;
        if (!player.AdvanceNativeMountForIndependentRuntime(moving, localRideDeltaX, localRideDeltaY, riderTint)) return false;
        playerNameTag?.BindNativeRidingForIndependentRuntime();
        return true;
    }

    internal bool ApplyLocalPlayerStatus(
        WorldPlayerStatusState status,
        WorldSelfPkState pkState)
    {
        ThrowIfDisposed();
        return built && playerNameTag != null && status != null &&
               playerNameTag.ApplyAuthoritativeStatus(
                   status.HitPoints,
                   status.MaximumHitPoints,
                   pkState == null ? (byte?)null : pkState.State);
    }

    /// <summary>
    /// Projects the official <c>CharData.title</c> owner into the local head title.
    /// </summary>
    /// <remarks>
    /// The value comes from the very static 0x804c writes, so a title received before the world
    /// frame is built or during a map switch is shown as soon as the name tag exists, and a
    /// packet that failed mid-read is shown with the entries the owner actually holds rather
    /// than with a rolled-back snapshot.
    /// </remarks>
    internal void ApplyLocalCharacterTitleForIndependentRuntime(string title)
    {
        if (disposed || !built)
        {
            return;
        }

        playerNameTag?.ApplyReceivedTitleForIndependentRuntime(title);
    }

    private float localRideDeltaX;
    private float localRideDeltaY;

    /// <summary>
    /// Source-owned equivalent of CombatWorld.SelectPlayer for an already published remote actor.
    /// </summary>
    internal bool TrySelectRemotePlayer(
        int entityId,
        int localFaction,
        bool openWorldHostile)
    {
        ThrowIfDisposed();
        ReferenceWorldRemotePlayerActor actor;
        return built && targetSelection != null &&
               remoteActors.TryGetValue(entityId, out actor) &&
               targetSelection.TrySelect(actor, localFaction, openWorldHostile);
    }

    // The right-click counterpart of TrySelectRemotePlayer: official PlayerController.Update
    // (0x06000a00) selects the clicked RemotePlayer itself for a short right click and does not
    // classify factions or touch the attack state.
    internal bool TrySelectRemotePlayerIdentity(int entityId)
    {
        ThrowIfDisposed();
        ReferenceWorldRemotePlayerActor actor;
        return built && targetSelection != null &&
               remoteActors.TryGetValue(entityId, out actor) &&
               targetSelection.TrySelectRemoteIdentityForIndependentRuntime(actor);
    }

    internal bool TrySelectMonster(int entityId)
    {
        ThrowIfDisposed();
        ReferenceWorldMonsterActor actor;
        return built && targetSelection != null &&
               monsterActors.TryGetValue(entityId, out actor) &&
               targetSelection.TrySelect(actor);
    }

    // The 0x800e monster branch no longer uses a product adapter: ReferenceFirstWorldWindowOwner
    // calls the official CombatWorld.MonsterPlayPrepare / CombatWorld.MonsterPlayAttack owners,
    // which resolve I.monsters themselves and call MonsterEntity.PlayPrepare / PlayAttack.

    // The right-click counterpart of TrySelectMonster: official PlayerController.Update
    // (0x06000a00) splits a picked MonsterEntity on its own friendly flag instead of arming combat -
    // a friendly one is only selected, every other one goes through CombatWorld.SetTarget with
    // attack disabled, and neither arm clears a previous target or moves the local player.
    internal bool TrySelectMonsterIdentity(int entityId)
    {
        ThrowIfDisposed();
        ReferenceWorldMonsterActor actor;
        return built && targetSelection != null &&
               monsterActors.TryGetValue(entityId, out actor) &&
               targetSelection.TrySelectMonsterIdentityForIndependentRuntime(actor);
    }

    internal bool TryPickSelectable(
        Vector2 worldPoint,
        out ReferenceWorldPointerTargetKind kind,
        out int entityId)
    {
        ThrowIfDisposed();
        kind = ReferenceWorldPointerTargetKind.None;
        entityId = 0;
        if (!built)
        {
            return false;
        }

        ISelectable selected = player.SourceControllerForIndependentRuntime.PickSelectableForIndependentRuntime(worldPoint);
        return DescribePointerTargetForIndependentRuntime(selected, out kind, out entityId);
    }

    // Classification is diagnostic only. The local frame keeps the native picked object even
    // when an optional presentation wrapper has not been admitted.
    internal bool DescribePointerTargetForIndependentRuntime(
        ISelectable selected, out ReferenceWorldPointerTargetKind kind, out int entityId)
    {
        kind = ReferenceWorldPointerTargetKind.None;
        entityId = 0;
        if (selected is PlayerController)
        {
            kind = ReferenceWorldPointerTargetKind.LocalPlayer;
            entityId = character.EntityId;
            return true;
        }
        if (selected is RemotePlayer remote)
        {
            kind = ReferenceWorldPointerTargetKind.RemotePlayer;
            entityId = remote.id;
            return true;
        }
        if (selected is MonsterEntity monster)
        {
            kind = ReferenceWorldPointerTargetKind.Monster;
            entityId = monster.id;
            return true;
        }
        if (selected is NpcInteract npc)
        {
            kind = ReferenceWorldPointerTargetKind.Npc;
            foreach (KeyValuePair<int, ReferenceWorldNpcActor> pair in npcActors)
                if (ReferenceEquals(pair.Value.SelectableOwner, npc))
                { entityId = pair.Key; break; }
            return true;
        }
        return selected != null;
    }

    internal void AdoptCurrentPointerSelectionForIndependentRuntime() =>
        targetSelection?.AdoptCurrentNativeSelectionForIndependentRuntime(remoteActors, monsterActors, npcActors);

    internal bool TryPickGroundItem(Vector2 worldPoint, out int groundId)
    {
        ThrowIfDisposed();
        groundId = 0;
        if (!built)
        {
            return false;
        }

        float nearestSquared = float.PositiveInfinity;
        foreach (KeyValuePair<int, ReferenceWorldGroundItemActor> pair in groundItemActors)
        {
            ReferenceWorldGroundItemActor actor = pair.Value;
            if (actor == null || actor.Root == null || actor.PickBox == null ||
                !actor.PickBox.enabled || !actor.PickBox.OverlapPoint(worldPoint))
            {
                continue;
            }

            float squared = ((Vector2)actor.Root.transform.position - worldPoint).sqrMagnitude;
            if (squared < nearestSquared)
            {
                nearestSquared = squared;
                groundId = pair.Key;
            }
        }

        return groundId > 0;
    }

    internal bool TryGetGroundItemState(int groundId, out WorldGroundItemState state)
    {
        ThrowIfDisposed();
        state = null;
        ReferenceWorldGroundItemActor actor;
        if (!built || !groundItemActors.TryGetValue(groundId, out actor) || actor == null)
        {
            return false;
        }

        state = actor.State;
        return state != null;
    }

    /// <summary>
    /// Preserves the exact OverUI/faction hostility branch and returns the hostile combat intent.
    /// </summary>
    /// <remarks>
    /// The current diagnostic world has no recovered GameHud input owner, so this method is not
    /// called from product mouse input yet. A PlayerVsPlayerSelection result must later be consumed
    /// by the combat owner; this presentation boundary never fabricates attack acceptance.
    /// </remarks>
    internal bool TryHandleRemotePointerDown(
        int entityId,
        bool pointerOverUi,
        int localFaction,
        bool openWorldHostile,
        out ReferenceWorldRemotePointerSelection result)
    {
        ThrowIfDisposed();
        result = ReferenceWorldRemotePointerSelection.None;
        ReferenceWorldRemotePlayerActor actor;
        return built && targetSelection != null &&
               remoteActors.TryGetValue(entityId, out actor) &&
               targetSelection.TryHandlePointerDown(
                   actor,
                   pointerOverUi,
                   localFaction,
                   openWorldHostile,
                   out result);
    }

    internal void ClearTargetSelection()
    {
        ThrowIfDisposed();
        targetSelection?.Clear();
    }

    internal void AdoptOfficialTargetClearForIndependentRuntime()
    {
        ThrowIfDisposed();
        targetSelection?.AdoptOfficialTargetClearForIndependentRuntime();
    }

    /// <summary>
    /// Polls the immutable Application snapshots and applies only newer authoritative revisions.
    /// </summary>
    public bool PollAuthoritativeWorldRevisions(
        ClientSessionCoordinator session,
        float standTimeSeconds,
        Color32 localRiderTint,
        int nameMode)
    {
        ThrowIfDisposed();
        if (session == null)
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.CaptureRejected;
            return false;
        }

        WorldRemoteEventWindow receiveWindow = PrepareWorldReceiveContext(session, nameMode);

        WorldRidingState localRidingState;
        long localRidingVersion;
        WorldLocalPlayerLifeState localPlayerLifeState;
        long localPlayerLifeStateVersion;
        IReadOnlyDictionary<int, WorldRemotePlayerState> remotePlayers;
        long remotePlayersVersion;
        IReadOnlyDictionary<int, WorldNpcState> npcs;
        long npcsVersion;
        IReadOnlyDictionary<int, WorldEntityState> worldEntities;
        long worldEntitiesVersion;
        long worldEntitySceneGeneration;
        int worldEntitySceneMapId;
        IReadOnlyDictionary<int, WorldGroundItemState> groundItems;
        long groundItemsVersion;
        IReadOnlyList<WorldCombatResultState> combatResults;
        long combatResultsVersion;
        IReadOnlyList<WorldCombatNumberState> combatNumbers;
        long combatNumbersVersion;
        IReadOnlyDictionary<int, WorldRemotePathState> remotePaths;
        long remotePathsVersion;
        IReadOnlyDictionary<int, WorldRemotePathState> monsterPaths;
        long monsterPathsVersion;
        IReadOnlyDictionary<int, WorldRemoteActionState> remoteActions;
        long remoteActionsVersion;
        IReadOnlyDictionary<int, WorldRemoteChatBubbleState> remoteChatBubbles;
        long remoteChatBubblesVersion;
        WorldPartyState partyState;
        long partyStateVersion;
        IReadOnlyDictionary<int, WorldStallOwnerState> stallOwners;
        long stallOwnersVersion;
        if (!TryCaptureLocalRiding(
                session,
                out localRidingState,
                out localRidingVersion) ||
            !TryCaptureLocalPlayerLife(
                session,
                out localPlayerLifeState,
                out localPlayerLifeStateVersion) ||
            !TryCaptureRemotePlayers(
                session,
                out remotePlayers,
                out remotePlayersVersion) ||
            !TryCaptureNpcs(
                session,
                out npcs,
                out npcsVersion) ||
            !TryCaptureWorldEntities(
                session,
                out worldEntities,
                out worldEntitiesVersion,
                out worldEntitySceneGeneration,
                out worldEntitySceneMapId) ||
            !TryCaptureGroundItems(
                session,
                out groundItems,
                out groundItemsVersion) ||
            !TryCaptureCombatResults(
                session,
                out combatResults,
                out combatResultsVersion) ||
            !TryCaptureCombatNumbers(
                session,
                out combatNumbers,
                out combatNumbersVersion) ||
            !TryCaptureRemotePaths(
                session,
                out remotePaths,
                out remotePathsVersion) ||
            !TryCaptureMonsterPaths(
                session,
                out monsterPaths,
                out monsterPathsVersion) ||
            !TryCaptureRemoteActions(
                session,
                out remoteActions,
                out remoteActionsVersion) ||
            !TryCaptureRemoteChatBubbles(
                session,
                out remoteChatBubbles,
                out remoteChatBubblesVersion) ||
            !TryCapturePartyState(
                session,
                out partyState,
                out partyStateVersion) ||
            !TryCaptureStallOwners(
                session,
                out stallOwners,
                out stallOwnersVersion))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.CaptureRejected;
            return false;
        }

        if (!ApplyAuthoritativeWorldRevisions(
                localRidingState,
                localRidingVersion,
                remotePlayers,
                remotePlayersVersion,
                remotePaths,
                remotePathsVersion,
                remoteActions,
                remoteActionsVersion,
                remoteChatBubbles,
                remoteChatBubblesVersion,
                standTimeSeconds,
                localRiderTint))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.PlayerRevisionRejected;
            return false;
        }

        // Native receipt already changed the authoritative row/object dictionaries. This
        // frame only borrows their produced roots, including after a partial native failure.
        bool npcRevisionsOwnedByReceiveEvents =
            receiveWindow != null && receiveWindow.IncludesNpcEvents;
        ObserveNativeNpcsForIndependentRuntime();
        if (npcsVersion > lastNpcsVersion) lastNpcsVersion = npcsVersion;

        if (!(receiveWindow?.IncludesEntityEvents ?? false) && !ApplyAuthoritativeWorldEntityRevision(
                worldEntities,
                worldEntitiesVersion,
                worldEntitySceneGeneration,
                worldEntitySceneMapId))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.EntityRevisionRejected;
            return false;
        }

        if (!(receiveWindow?.IncludesEntityEvents ?? false) &&
            !ApplyAuthoritativeMonsterPathRevision(monsterPaths, monsterPathsVersion))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.MonsterPathRevisionRejected;
            return false;
        }

        if (!ApplyAuthoritativeGroundItemRevision(
                groundItems,
                groundItemsVersion,
                Time.realtimeSinceStartup))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.GroundItemRevisionRejected;
            return false;
        }

        if (!ApplyLocalPlayerLifeRevision(
                localPlayerLifeState,
                localPlayerLifeStateVersion))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.LocalLifeRevisionRejected;
            return false;
        }

        if (receiveWindow != null)
        {
            if (!ApplyRemoteReceiveWindow(session, receiveWindow, standTimeSeconds))
            {
                LastRevisionFailure = ReferenceWorldRevisionFailure.CaptureRejected;
                return false;
            }
            // The window advanced every NPC change of this poll; nothing in the dictionary is
            // left unapplied, so acknowledging it also acknowledges that revision number.
            if (npcRevisionsOwnedByReceiveEvents && npcsVersion > lastNpcsVersion)
            {
                lastNpcsVersion = npcsVersion;
            }

            if (!receiveWindow.IncludesChatEvents && remoteReceiveMapMatches && remoteChatBubblesVersion != lastRemoteChatBubblesVersion)
            {
                if (!ApplyRemoteChatBubbleSnapshot(remoteChatBubbles, Time.time)) return false;
                lastRemoteChatBubblesVersion = remoteChatBubblesVersion;
            }
            partyState = remoteReceiveParty;
            partyStateVersion = remoteReceiveSequence;
            stallOwners = remoteReceiveStalls;
            stallOwnersVersion = remoteReceiveSequence;
        }
        else
        {
            SocialClient.BindPartyForIndependentRuntime(partyState);
            StallDlg.BindOwnersForIndependentRuntime(stallOwners);
        }

        if (!ApplyAuthoritativeCombatFeedbackRevision(
                combatResults,
                combatResultsVersion,
                combatNumbers,
                combatNumbersVersion,
                session))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.CombatRevisionRejected;
            return false;
        }

        if (!ApplyRemoteVisibility(
                partyState,
                partyStateVersion,
                stallOwners,
                stallOwnersVersion,
                nameMode))
        {
            LastRevisionFailure = ReferenceWorldRevisionFailure.VisibilityRevisionRejected;
            return false;
        }

        LastRevisionFailure = ReferenceWorldRevisionFailure.None;
        return true;
    }

    /// <summary>
    /// Reconciles one captured set of server-authored revisions on Unity's main thread.
    /// </summary>
    internal bool ApplyAuthoritativeWorldRevisions(
        WorldRidingState localRidingState,
        long localRidingVersion,
        IReadOnlyDictionary<int, WorldRemotePlayerState> remotePlayers,
        long remotePlayersVersion,
        IReadOnlyDictionary<int, WorldRemotePathState> remotePaths,
        long remotePathsVersion,
        float standTimeSeconds,
        Color32 localRiderTint)
    {
        return ApplyAuthoritativeWorldRevisions(
            localRidingState,
            localRidingVersion,
            remotePlayers,
            remotePlayersVersion,
            remotePaths,
            remotePathsVersion,
            EmptyRemoteActions,
            Math.Max(0, lastRemoteActionsVersion),
            EmptyRemoteChatBubbles,
            Math.Max(0, lastRemoteChatBubblesVersion),
            standTimeSeconds,
            localRiderTint);
    }

    internal bool ApplyAuthoritativeWorldRevisions(
        WorldRidingState localRidingState,
        long localRidingVersion,
        IReadOnlyDictionary<int, WorldRemotePlayerState> remotePlayers,
        long remotePlayersVersion,
        IReadOnlyDictionary<int, WorldRemotePathState> remotePaths,
        long remotePathsVersion,
        IReadOnlyDictionary<int, WorldRemoteActionState> remoteActions,
        long remoteActionsVersion,
        float standTimeSeconds,
        Color32 localRiderTint)
    {
        return ApplyAuthoritativeWorldRevisions(
            localRidingState,
            localRidingVersion,
            remotePlayers,
            remotePlayersVersion,
            remotePaths,
            remotePathsVersion,
            remoteActions,
            remoteActionsVersion,
            EmptyRemoteChatBubbles,
            Math.Max(0, lastRemoteChatBubblesVersion),
            standTimeSeconds,
            localRiderTint);
    }

    internal bool ApplyAuthoritativeWorldRevisions(
        WorldRidingState localRidingState,
        long localRidingVersion,
        IReadOnlyDictionary<int, WorldRemotePlayerState> remotePlayers,
        long remotePlayersVersion,
        IReadOnlyDictionary<int, WorldRemotePathState> remotePaths,
        long remotePathsVersion,
        IReadOnlyDictionary<int, WorldRemoteActionState> remoteActions,
        long remoteActionsVersion,
        IReadOnlyDictionary<int, WorldRemoteChatBubbleState> remoteChatBubbles,
        long remoteChatBubblesVersion,
        float standTimeSeconds,
        Color32 localRiderTint)
    {
        ThrowIfDisposed();
        if (!built || remotePlayers == null || remotePaths == null ||
            remoteActions == null || remoteChatBubbles == null ||
            localRidingVersion < 0 || remotePlayersVersion < 0 ||
            remotePathsVersion < 0 || remoteActionsVersion < 0 ||
            remoteChatBubblesVersion < 0 ||
            localRidingVersion < lastLocalRidingStateVersion ||
            remotePlayersVersion < lastRemotePlayersVersion ||
            remotePathsVersion < lastRemotePathsVersion ||
            remoteActionsVersion < lastRemoteActionsVersion ||
            remoteChatBubblesVersion < lastRemoteChatBubblesVersion)
        {
            return false;
        }

        if (localRidingVersion != lastLocalRidingStateVersion)
        {
            if (localRidingState == null)
            {
                if (!ClearLocalRidingPresentation(
                        standTimeSeconds,
                        localRiderTint))
                {
                    return false;
                }
            }
            else if (!ApplyLocalRidingState(
                         localRidingState,
                         standTimeSeconds,
                         character.Y,
                         localRiderTint))
            {
                return false;
            }

            lastLocalRidingStateVersion = localRidingVersion;
        }

        if (!remoteReceiveEnabled && remotePlayersVersion != lastRemotePlayersVersion)
        {
            if (!ApplyRemotePlayerSnapshot(
                    remotePlayers,
                    standTimeSeconds))
            {
                return false;
            }

            lastRemotePlayersVersion = remotePlayersVersion;
        }

        if (!remoteReceiveEnabled && remoteChatBubblesVersion != lastRemoteChatBubblesVersion)
        {
            if (!ApplyRemoteChatBubbleSnapshot(remoteChatBubbles, Time.time))
            {
                return false;
            }

            lastRemoteChatBubblesVersion = remoteChatBubblesVersion;
        }

        if (!remoteReceiveEnabled && remotePathsVersion != lastRemotePathsVersion)
        {
            if (!ApplyRemotePathSnapshot(remotePaths, standTimeSeconds))
            {
                return false;
            }

            lastRemotePathsVersion = remotePathsVersion;
        }

        if (!remoteReceiveEnabled && remoteActionsVersion != lastRemoteActionsVersion)
        {
            if (!ApplyRemoteActionSnapshot(remoteActions))
            {
                return false;
            }

            lastRemoteActionsVersion = remoteActionsVersion;
        }

        return true;
    }

    private bool ClearLocalRidingPresentation(
        float standTimeSeconds,
        Color32 riderTint)
    {
        if (player == null) return false;
        player.BindNativeRidingForIndependentRuntime(null);
        playerNameTag?.BindNativeRidingForIndependentRuntime();
        return true;
    }

    private static bool TryCaptureLocalRiding(
        ClientSessionCoordinator session,
        out WorldRidingState state,
        out long version)
    {
        state = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.LocalRidingStateVersion;
            WorldRidingState candidate = session.LocalRidingState;
            long after = session.LocalRidingStateVersion;
            if (before == after)
            {
                state = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureLocalPlayerLife(
        ClientSessionCoordinator session,
        out WorldLocalPlayerLifeState state,
        out long version)
    {
        state = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.LocalPlayerLifeStateVersion;
            WorldLocalPlayerLifeState candidate = session.LocalPlayerLifeState;
            long after = session.LocalPlayerLifeStateVersion;
            if (before == after)
            {
                state = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureRemotePlayers(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldRemotePlayerState> players,
        out long version)
    {
        players = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.RemotePlayersVersion;
            IReadOnlyDictionary<int, WorldRemotePlayerState> candidate =
                session.RemotePlayers;
            long after = session.RemotePlayersVersion;
            if (before == after)
            {
                players = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureNpcs(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldNpcState> npcs,
        out long version)
    {
        npcs = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.NpcsVersion;
            IReadOnlyDictionary<int, WorldNpcState> candidate = session.Npcs;
            long after = session.NpcsVersion;
            if (before == after)
            {
                npcs = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureWorldEntities(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldEntityState> entities,
        out long version,
        out long sceneGeneration,
        out int mapId)
    {
        WorldEntitySceneSnapshot snapshot = session.WorldEntitySceneSnapshot;
        entities = snapshot == null ? null : snapshot.Entities;
        version = snapshot == null ? -1 : snapshot.SnapshotVersion;
        sceneGeneration = snapshot == null ? -1 : snapshot.SceneGeneration;
        mapId = snapshot == null ? -1 : snapshot.MapId;
        return snapshot != null && snapshot.Publication != null &&
               snapshot.Publication.SceneGeneration == sceneGeneration &&
               snapshot.Publication.MapId == mapId &&
               snapshot.Publication.SnapshotVersion == version &&
               entities != null &&
               snapshot.Publication.EntityCount == entities.Count;
    }

    private static bool TryCaptureGroundItems(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldGroundItemState> groundItems,
        out long version)
    {
        groundItems = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.GroundItemsVersion;
            IReadOnlyDictionary<int, WorldGroundItemState> candidate = session.GroundItems;
            long after = session.GroundItemsVersion;
            if (before == after)
            {
                groundItems = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureCombatResults(
        ClientSessionCoordinator session,
        out IReadOnlyList<WorldCombatResultState> results,
        out long version)
    {
        results = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.CombatResultsVersion;
            IReadOnlyList<WorldCombatResultState> candidate = session.CombatResults;
            long after = session.CombatResultsVersion;
            if (before == after)
            {
                results = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureCombatNumbers(
        ClientSessionCoordinator session,
        out IReadOnlyList<WorldCombatNumberState> numbers,
        out long version)
    {
        numbers = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.CombatNumbersVersion;
            IReadOnlyList<WorldCombatNumberState> candidate = session.CombatNumbers;
            long after = session.CombatNumbersVersion;
            if (before == after)
            {
                numbers = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureRemotePaths(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldRemotePathState> paths,
        out long version)
    {
        paths = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.RemotePathsVersion;
            IReadOnlyDictionary<int, WorldRemotePathState> candidate =
                session.RemotePaths;
            long after = session.RemotePathsVersion;
            if (before == after)
            {
                paths = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureRemoteActions(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldRemoteActionState> actions,
        out long version)
    {
        actions = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.RemoteActionsVersion;
            IReadOnlyDictionary<int, WorldRemoteActionState> candidate =
                session.RemoteActions;
            long after = session.RemoteActionsVersion;
            if (before == after)
            {
                actions = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureMonsterPaths(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldRemotePathState> paths,
        out long version)
    {
        paths = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.MonsterPathsVersion;
            IReadOnlyDictionary<int, WorldRemotePathState> candidate =
                session.MonsterPaths;
            long after = session.MonsterPathsVersion;
            if (before == after)
            {
                paths = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureRemoteChatBubbles(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldRemoteChatBubbleState> bubbles,
        out long version)
    {
        bubbles = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.RemoteChatBubblesVersion;
            IReadOnlyDictionary<int, WorldRemoteChatBubbleState> candidate =
                session.RemoteChatBubbles;
            long after = session.RemoteChatBubblesVersion;
            if (before == after)
            {
                bubbles = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCapturePartyState(
        ClientSessionCoordinator session,
        out WorldPartyState partyState,
        out long version)
    {
        partyState = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.PartyStateVersion;
            WorldPartyState candidate = session.PartyState;
            long after = session.PartyStateVersion;
            if (before == after)
            {
                partyState = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    private static bool TryCaptureStallOwners(
        ClientSessionCoordinator session,
        out IReadOnlyDictionary<int, WorldStallOwnerState> stallOwners,
        out long version)
    {
        stallOwners = null;
        version = -1;
        for (int attempt = 0; attempt < SnapshotReadAttempts; attempt++)
        {
            long before = session.StallOwnersVersion;
            IReadOnlyDictionary<int, WorldStallOwnerState> candidate =
                session.StallOwners;
            long after = session.StallOwnersVersion;
            if (before == after)
            {
                stallOwners = candidate;
                version = after;
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Applies live SocialClient membership inputs to the recovered Doll and mounted-art seams.
    /// </summary>
    internal bool ApplyRemoteVisibility(
        WorldPartyState partyState,
        long partyStateVersion,
        IReadOnlyDictionary<int, WorldStallOwnerState> stallOwners,
        long stallOwnersVersion,
        int nameMode)
    {
        ThrowIfDisposed();
        if (!built || partyState == null || stallOwners == null ||
            partyStateVersion < 0 || stallOwnersVersion < 0 ||
            partyStateVersion < lastPartyStateVersion ||
            stallOwnersVersion < lastStallOwnersVersion ||
            nameMode < 0 || nameMode >= ReferenceWorldNameModeController.ModeCount)
        {
            return false;
        }

        foreach (KeyValuePair<int, WorldStallOwnerState> pair in stallOwners)
        {
            if (pair.Value == null || pair.Key != pair.Value.EntityId)
            {
                return false;
            }
        }

        foreach (ReferenceWorldRemotePlayerActor actor in remoteActors.Values)
        {
            if (!actor.CanApplyVisibility())
            {
                return false;
            }
        }

        foreach (ReferenceWorldRemotePlayerActor actor in remoteActors.Values)
        {
            WorldStallOwnerState stallOwner;
            stallOwners.TryGetValue(actor.State.EntityId, out stallOwner);
            if (!actor.TryApplyVisibility(
                    nameMode,
                    partyState.ContainsEntity(actor.State.EntityId),
                    partyState.PartyName,
                    partyState.LeaderEntityId,
                    stallOwner))
            {
                return false;
            }
        }

        WorldStallOwnerState localStall;
        bool localStalling = stallOwners.TryGetValue(
            character.EntityId,
            out localStall);
        if (playerNameTag == null || !playerNameTag.ApplySocialState(
                partyState.ContainsEntity(character.EntityId),
                SocialClient.TeamName(),
                partyState.LeaderEntityId,
                localStalling))
        {
            return false;
        }

        lastPartyStateVersion = partyStateVersion;
        lastStallOwnersVersion = stallOwnersVersion;
        lastNameMode = nameMode;
        return true;
    }

    /// <summary>
    /// Advances the admitted remote death-before-ride/path precedence and reviewed clocks.
    /// </summary>
    internal void BindSittingInputForIndependentRuntime(bool chatTyping, byte immobile,
        int rideAnchor, bool riding, System.Action<string> systemMessage)
    {
        chatTypingForIndependentRuntime = chatTyping;
        immobileForIndependentRuntime = immobile;
        rideAnchorForIndependentRuntime = rideAnchor;
        ridingForIndependentRuntime = riding;
        sitMessageForIndependentRuntime = systemMessage;
    }

    internal void BindCombatInputForIndependentRuntime(bool mouseFollow, System.Action stopAutoAttack)
    {
        mouseFollowForIndependentRuntime = mouseFollow;
        stopAutoAttackForIndependentRuntime = stopAutoAttack;
    }

    public bool AdvanceAuthoritativeWorldActors(
        float deltaTime,
        float standTimeSeconds,
        Color32 localRiderTint)
    {
        return AdvanceAuthoritativeWorldActorsCore(
            deltaTime,
            standTimeSeconds,
            localRiderTint,
            null,
            null,
            null,
            false,
            null,
            false);
    }

    /// <summary>
    /// Advances reviewed actors plus the product-wired local follow and movement branch.
    /// </summary>
    public bool AdvanceAuthoritativeWorldActors(
        float deltaTime,
        float standTimeSeconds,
        Color32 localRiderTint,
        ReferenceWorldCollisionMask collisionMask,
        IReadOnlyList<int> localAttributes,
        IReadOnlyList<int> localAttributeTotals = null,
        Func<bool> localPointerInput = null,
        bool localPrefixPrepared = false)
    {
        return AdvanceAuthoritativeWorldActorsCore(
            deltaTime,
            standTimeSeconds,
            localRiderTint,
            collisionMask,
            localAttributes,
            localAttributeTotals,
            true,
            localPointerInput,
            localPrefixPrepared);
    }

    // Retained explicit diagnostic prefix. Production uses PlayerController.Update.
    internal bool PrepareLocalPlayerFramePrefixForIndependentRuntime(Color32 tint)
    {
        ThrowIfDisposed();
        return built && (player == null || player.RefreshAppearanceVersionForIndependentRuntime(tint));
    }

    public bool ToggleFollowPlayer(int entityId)
    {
        ThrowIfDisposed();
        if (!built) return false;
        if (NetClient.I != null && PlayerController.I != null)
            PlayerController.I.FollowMate(entityId);
        return true;
    }

    public bool ToggleFollowLeader(int entityId)
    {
        ThrowIfDisposed();
        if (!built) return false;
        if (entityId != 0) return ToggleFollowPlayer(entityId);
        return true;
    }

    internal void BindFollowMessageForIndependentRuntime(Action<string, Color> message) =>
        followMessageForIndependentRuntime = message;

    public string ConsumeFollowFeedback()
    {
        ThrowIfDisposed();
        string result = pendingFollowFeedback;
        pendingFollowFeedback = string.Empty;
        return result;
    }

    private bool AdvanceAuthoritativeWorldActorsCore(
        float deltaTime,
        float standTimeSeconds,
        Color32 localRiderTint,
        ReferenceWorldCollisionMask collisionMask,
        IReadOnlyList<int> localAttributes,
        IReadOnlyList<int> localAttributeTotals,
        bool advanceLocalMovement,
        Func<bool> localPointerInput,
        bool localPrefixPrepared)
    {
        ThrowIfDisposed();
        if (!built)
        {
            return false;
        }

        foreach (ReferenceWorldRemotePlayerActor actor in remoteActors.Values)
        {
            if (!actor.TryAdvanceRecoveredUpdate(
                    deltaTime,
                    standTimeSeconds,
                    hideOtherPets,
                    camera))
            {
                return false;
            }
        }

        // Both player components own their real Unity Update messages. This optional
        // world view only adopts local state; it never advances a second local frame.
        if (player != null && !player.ObserveNativeFrameForIndependentRuntime()) return false;
        targetSelection?.AdoptCurrentNativeSelectionForIndependentRuntime(remoteActors, monsterActors, npcActors);

        playerNameTag?.BindNativeRidingForIndependentRuntime();
        List<int> lifecycleRemoved = null;
        foreach (KeyValuePair<int, ReferenceWorldMonsterActor> pair in monsterActors)
        {
            ReferenceWorldMonsterActor actor = pair.Value;
            if (actor.SourceOwnerLive && !actor.ObserveNativeFrameForIndependentRuntime())
            {
                return false;
            }
            if (!actor.SourceOwnerLive)
            {
                if (lifecycleRemoved == null)
                {
                    lifecycleRemoved = new List<int>();
                }
                lifecycleRemoved.Add(pair.Key);
            }
        }

        if (lifecycleRemoved != null)
        {
            for (int index = 0; index < lifecycleRemoved.Count; index++)
            {
                int entityId = lifecycleRemoved[index];
                ReferenceWorldMonsterActor actor = monsterActors[entityId];
                targetSelection?.OnGone(actor, observationOnly: true);
                actor.Dispose();
                monsterActors.Remove(entityId);
                entityReceiveIncarnations.Remove(entityId);
                entityReceiveBirthSequences.Remove(entityId);
                appliedMonsterPaths.Remove(entityId);
                terminalWorldEntityTombstones.Add(entityId);
            }
        }

        foreach (ReferenceWorldGroundItemActor actor in groundItemActors.Values)
        {
            if (!actor.TryAdvance(deltaTime))
            {
                return false;
            }
        }

        if (combatFeedback != null && !combatFeedback.TryAdvance(deltaTime))
        {
            return false;
        }

        return targetSelection == null || targetSelection.TryTick();
    }

    public void Dispose() => Dispose(sceneRegistriesReleased: false);

    internal void Dispose(bool sceneRegistriesReleased, bool preserveNativePlayerState = false)
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        ReleaseOwnedState(sceneRegistriesReleased, completingDisposal: true,
            preserveNativePlayerState: preserveNativePlayerState);
    }

    private GroundItems groundItemsOwner;

    private bool selfIdentityBound;

    private void ReleaseOwnedState(bool sceneRegistriesReleased = false,
        bool completingDisposal = false, bool preserveNativePlayerState = false)
    {
        if (TransitionOwnsConstructionFailureForIndependentRuntime && !completingDisposal) return;
        if (selfIdentityBound)
        {
            if (!sceneRegistriesReleased)
            {
                CharData.charId = 0;
                SocialClient.BindPartyForIndependentRuntime(WorldPartyState.Empty);
            }
            selfIdentityBound = false;
        }
        // A failed unpublished candidate or retired frame cannot clear the current
        // borrowed singleton. The bootstrap keeps its current Window until disposal ends.
        bool currentFrame = NpcSpawnOwnerForIndependentRuntime == null ||
            ReferenceEquals(NpcSpawnOwnerForIndependentRuntime.Window?.Frame, this);
        if (!sceneRegistriesReleased && currentFrame && combatWorld != null)
            combatWorld.ClearForTeardownForIndependentRuntime();
        if (groundItemsOwner != null && groundItemsOwner.ClearFrameForIndependentRuntime(
                this, clearItems: !sceneRegistriesReleased))
            foreach (ReferenceWorldGroundItemActor actor in groundItemActors.Values)
                actor.NoteNativeReleasedForIndependentRuntime();
        groundItemsOwner = null;
        if (combatFeedback != null)
        {
            combatFeedback.Dispose();
            combatFeedback = null;
        }

        if (targetSelection != null)
        {
            targetSelection.Dispose();
            targetSelection = null;
        }

        // ClearAll above owns native players; these adapters only forget their views.
        foreach (ReferenceWorldRemotePlayerActor actor in remoteActors.Values)
        {
            actor.Dispose();
        }

        remoteActors.Clear();
        foreach (ReferenceWorldNpcActor actor in npcActors.Values)
        {
            actor.Dispose();
        }

        npcActors.Clear();
        foreach (ReferenceWorldMonsterActor actor in monsterActors.Values)
        {
            actor.Dispose();
        }

        monsterActors.Clear();
        foreach (ReferenceWorldGroundItemActor actor in groundItemActors.Values)
        {
            actor.Dispose();
        }

        groundItemActors.Clear();
        appliedRemotePaths.Clear();
        appliedMonsterPaths.Clear();
        appliedRemoteActions.Clear();


        playerNameTag = null;

        if (remoteFeedbackAssets != null)
        {
            remoteFeedbackAssets.Dispose();
            remoteFeedbackAssets = null;
        }

        npcTaskFeedback = null;
        npcGreetingCatalog = null;

        itemIconCatalog = null;

        if (staticComposite != null)
        {
            staticComposite.Dispose();
            staticComposite = null;
        }

        if (player != null)
        {
            if (!preserveNativePlayerState && CombatNumber.PlayerTransform == player.Root)
                CombatNumber.BindPlayerTransformForIndependentRuntime(null);
            player.Dispose(preserveNativePlayerState);
            player = null;
        }

        pendingFollowFeedback = string.Empty;
        followMessageForIndependentRuntime = null;
        // Ordinary map disposal never disables or destroys the independent CombatWorld.
        // Final world exit reclaims its scene root through the bootstrap's existing purge.
        beforeMovementTransfer = null;
        pickupMessage = null;

        lastLocalRidingStateVersion = -1;
        lastLocalPlayerLifeStateVersion = -1;
        lastRemotePlayersVersion = -1;
        lastNpcsVersion = -1;
        lastWorldEntitiesVersion = -1;
        lastWorldEntitySceneGeneration = -1;
        terminalWorldEntityTombstones.Clear();
        worldEntityActorRebuildGeneration = 0;
        lastWorldEntitySnapshotMapId = 0;
        lastWorldEntitySnapshotCount = 0;
        lastWorldEntityReconciliationFailure =
            ReferenceWorldEntityReconciliationFailure.None;
        lastGroundItemsVersion = -1;
        lastCombatResultsVersion = -1;
        lastCombatNumbersVersion = -1;
        lastCombatResultRevision = 0;
        standaloneCombatNumbers.Clear();
        lastRemotePathsVersion = -1;
        lastMonsterPathsVersion = -1;
        lastRemoteActionsVersion = -1;
        lastRemoteChatBubblesVersion = -1;
        lastPartyStateVersion = -1;
        lastStallOwnersVersion = -1;
        lastNameMode = -1;
        localPlayerDead = false;
        built = false;
    }

    internal bool ApplyAuthoritativeNpcRevision(
        IReadOnlyDictionary<int, WorldNpcState> snapshot,
        long version)
    {
        ThrowIfDisposed();
        if (!built || snapshot == null || version < 0 || version < lastNpcsVersion)
        {
            return false;
        }

        if (version == lastNpcsVersion)
        {
            return true;
        }

        if (!ApplyNpcSnapshot(snapshot))
        {
            return false;
        }

        lastNpcsVersion = version;
        return true;
    }

    internal bool ApplyAuthoritativeMonsterPathRevision(
        IReadOnlyDictionary<int, WorldRemotePathState> snapshot,
        long version)
    {
        ThrowIfDisposed();
        if (!built || snapshot == null || version < 0 ||
            version < lastMonsterPathsVersion)
        {
            return false;
        }

        if (version == lastMonsterPathsVersion)
        {
            return true;
        }

        if (!ApplyMonsterPathSnapshot(snapshot))
        {
            return false;
        }

        lastMonsterPathsVersion = version;
        return true;
    }

    private bool ApplyNpcSnapshot(IReadOnlyDictionary<int, WorldNpcState> snapshot)
    {
        // Compatibility snapshots are diagnostic observations, never a second spawn authority.
        ObserveNativeNpcsForIndependentRuntime();
        return true;
    }

    internal void ObserveNativeNpcsForIndependentRuntime()
    {
        if (!built || disposed) return;
        IReadOnlyDictionary<int, GameObject> objects = GameBootstrap.NetNpcObjectsForIndependentRuntime;
        foreach (int id in new List<int>(npcActors.Keys))
        {
            if (!objects.TryGetValue(id, out GameObject rootObject) || rootObject == null ||
                !ReferenceEquals(npcActors[id].RootObject, rootObject))
                RemoveNpcReceiveActor(id);
        }
        foreach (KeyValuePair<int, GameObject> pair in objects)
        {
            // A failed replacement may deliberately leave the destroyed old key registered.
            // Do not repair or remove it, and do not ask SpawnNpcEntry to replace it here.
            if (pair.Value == null || npcActors.ContainsKey(pair.Key)) continue;
            if (!GameBootstrap.NetNpcRowsForIndependentRuntime.TryGetValue(pair.Key, out NpcEntry row) ||
                row == null) continue;
            WorldNpcState state = new WorldNpcState(pair.Key, (int)row.x, (int)row.y,
                row.sprite, row.name, row.label, row.portrait, row.script,
                row.sell, row.trans, row.dir);
            ReferenceWorldNpcActor actor = new ReferenceWorldNpcActor(
                map, resources, root, state, remoteFeedbackAssets, camera, player.Root,
                npcTaskFeedback, character.Level, npcGreetingCatalog,
                NpcSpawnOwnerForIndependentRuntime, pair.Value);
            if (actor.Build()) npcActors[pair.Key] = actor;
            else actor.Dispose();
        }
    }

    private void RemoveNpcReceiveActor(int entityId)
    {
        if (npcActors.TryGetValue(entityId, out ReferenceWorldNpcActor actor))
        {
            actor.Dispose(); // Releases only borrowed-view state, never the native root.
            npcActors.Remove(entityId);
        }
    }

    internal bool ApplyNpcReceiveEvent(WorldRemoteEvent edge)
    {
        if (edge.Kind != WorldRemoteEventKind.NpcRemove && edge.Npc == null) return false;
        ObserveNativeNpcsForIndependentRuntime();
        return true;
    }

    internal bool ApplyNpcTaskFeedback(WorldTaskLogState taskLog)
    {
        ThrowIfDisposed();
        if (!built || taskLog == null)
        {
            return false;
        }

        if (npcTaskFeedback == null) return true;

        foreach (ReferenceWorldNpcActor actor in npcActors.Values)
        {
            if (actor.NameView != null && !actor.NameView.ApplyTaskLog(taskLog))
            {
                return false;
            }
        }

        return true;
    }

    // Official 0x804b has no frame-side projection: the case body tail-calls
    // NpcChatter.SetServerChatter on receipt, so the chatter tables are replaced by the receive
    // owner (IWorldReceiveOwner.ReceiveNpcChatter) and this frame deliberately has no
    // ApplyNpcChatter member. A per-frame re-application would run the side effect a second time
    // for the same packet and would collapse two packets that arrive in the same frame into one.

    internal bool ApplyAuthoritativeWorldEntityRevision(
        IReadOnlyDictionary<int, WorldEntityState> snapshot,
        long version)
    {
        return ApplyAuthoritativeWorldEntityRevision(
            snapshot,
            version,
            lastWorldEntitySceneGeneration < 0 ? 0 : lastWorldEntitySceneGeneration,
            mapDescriptor.id);
    }

    internal bool ApplyAuthoritativeWorldEntityRevision(
        IReadOnlyDictionary<int, WorldEntityState> snapshot,
        long version,
        long sceneGeneration,
        int sceneMapId)
    {
        ThrowIfDisposed();
        lastWorldEntityReconciliationFailure =
            ReferenceWorldEntityReconciliationFailure.None;
        if (!built || snapshot == null || version < 0 || sceneGeneration < 0 ||
            sceneMapId < 0)
        {
            lastWorldEntityReconciliationFailure =
                ReferenceWorldEntityReconciliationFailure.InvalidRow;
            return false;
        }

        if (sceneGeneration < lastWorldEntitySceneGeneration)
        {
            lastWorldEntityReconciliationFailure =
                ReferenceWorldEntityReconciliationFailure.SceneGenerationRegressed;
            return false;
        }

        if (sceneGeneration == lastWorldEntitySceneGeneration &&
            version < lastWorldEntitiesVersion)
        {
            lastWorldEntityReconciliationFailure =
                ReferenceWorldEntityReconciliationFailure.VersionRegressed;
            return false;
        }

        if (sceneMapId != mapDescriptor.id)
        {
            lastWorldEntityReconciliationFailure =
                ReferenceWorldEntityReconciliationFailure.SceneMapMismatch;
            return false;
        }

        if (sceneGeneration > lastWorldEntitySceneGeneration)
        {
            terminalWorldEntityTombstones.Clear();
        }

        if (sceneGeneration == lastWorldEntitySceneGeneration &&
            version == lastWorldEntitiesVersion)
        {
            return true;
        }

        if (!ApplyWorldEntitySnapshot(snapshot))
        {
            return false;
        }

        lastWorldEntitiesVersion = version;
        lastWorldEntitySceneGeneration = sceneGeneration;
        worldEntityActorRebuildGeneration++;
        lastWorldEntitySnapshotMapId = sceneMapId;
        lastWorldEntitySnapshotCount = snapshot.Count;
        return true;
    }

    internal bool ApplyAuthoritativeGroundItemRevision(
        IReadOnlyDictionary<int, WorldGroundItemState> snapshot,
        long version,
        float receiveRealtime)
    {
        ThrowIfDisposed();
        if (!built || snapshot == null || version < 0 ||
            version < lastGroundItemsVersion || float.IsNaN(receiveRealtime) ||
            float.IsInfinity(receiveRealtime))
        {
            return false;
        }

        if (version == lastGroundItemsVersion)
        {
            return true;
        }

        var present = new HashSet<int>();
        foreach (KeyValuePair<int, WorldGroundItemState> pair in snapshot)
        {
            WorldGroundItemState state = pair.Value;
            if (state == null || pair.Key != state.GroundId) return false;
            if (!GroundItems.TryGetForIndependentRuntime(pair.Key, out GroundItems owner, out GroundItems.Item item)) continue;
            present.Add(pair.Key);
            owner.BindExistingFrameForIndependentRuntime(this);
            groundItemsOwner = owner;
            if (groundItemActors.TryGetValue(pair.Key, out ReferenceWorldGroundItemActor current))
            {
                if (current.ReferencesForIndependentRuntime(item)) continue;
                current.NoteNativeReleasedForIndependentRuntime();
                current.Dispose();
            }
            groundItemActors[pair.Key] = ReferenceWorldGroundItemActor.AdoptForIndependentRuntime(state, owner, item);
        }
        var removed = new List<int>();
        foreach (int id in groundItemActors.Keys)
            if (!present.Contains(id)) removed.Add(id);
        foreach (int id in removed)
        {
            groundItemActors[id].NoteNativeReleasedForIndependentRuntime();
            groundItemActors[id].Dispose();
            groundItemActors.Remove(id);
        }
        lastGroundItemsVersion = version;
        return true;
    }

    internal bool ApplyAuthoritativeCombatFeedbackRevision(
        IReadOnlyList<WorldCombatResultState> results,
        long resultsVersion,
        IReadOnlyList<WorldCombatNumberState> numbers,
        long numbersVersion,
        ClientSessionCoordinator session = null)
    {
        ThrowIfDisposed();
        if (!built || combatFeedback == null || results == null || numbers == null ||
            resultsVersion < 0 || numbersVersion < 0 ||
            resultsVersion < lastCombatResultsVersion ||
            numbersVersion < lastCombatNumbersVersion)
        {
            return false;
        }

        if (resultsVersion != lastCombatResultsVersion)
        {
            for (int index = 0; index < results.Count; index++)
            {
                WorldCombatResultState result = results[index];
                if (result == null || result.Revision <= lastCombatResultRevision)
                {
                    continue;
                }

                if (nativeCombatReceiveEnabled || result.NativeCombatHandled)
                {
                    lastCombatResultRevision = result.Revision;
                    continue;
                }

                bool projected = false;
                ReferenceWorldMonsterActor actor;
                if (monsterActors.TryGetValue(result.TargetEntityId, out actor) &&
                    (!entityReceiveEnabled || MatchesEntityReceiveIncarnation(result.TargetEntityId, result.TargetIncarnation, result.ReceiveSequence)))
                {
                    if (result.Healing)
                    {
                        if (!combatFeedback.TrySpawnResult(actor, result) ||
                            !actor.TryApplyCombatResult(result)) return false;
                    }
                    else if (!actor.TryApplyCombatResult(result) ||
                             !combatFeedback.TrySpawnResult(actor, result))
                    {
                        return false;
                    }

                    // The native healing branch returns before impact effects and hit sound.
                    projected = !result.Healing;
                }

                if (result.TargetEntityId == character.EntityId)
                {
                    ReferenceWorldMonsterActor attacker;
                    if (monsterActors.TryGetValue(result.AttackerEntityId, out attacker) &&
                        (!entityReceiveEnabled || MatchesEntityReceiveIncarnation(result.AttackerEntityId, result.AttackerIncarnation, result.ReceiveSequence)) &&
                        !attacker.StartAttackAnimation())
                    {
                        return false;
                    }

                    if ((!result.Missed && !localPlayerDead &&
                         !player.StartHurtAnimation()) ||
                        !combatFeedback.TrySpawnLocalResult(player, result))
                    {
                        return false;
                    }

                    projected = true;
                }

                if (projected && combatSound != null &&
                    ReferenceWorldCombatSound.ShouldPlayHit(result.Flags) &&
                    !combatSound.TryPlayHit(
                        character.Appearance.WeaponCategory,
                        result.Critical))
                {
                    return false;
                }

                lastCombatResultRevision = result.Revision;
            }

            lastCombatResultsVersion = resultsVersion;
        }

        // Frames are replaced on each map load; the session retains successful consumption
        // and retries only unresolved edges. Poll pending entries even with an unchanged
        // receive version, and never skip to the journal tail on a new map.
        WorldCombatNumberPresentation numberPresentation =
            session != null ? session.CombatNumberPresentation : standaloneCombatNumbers;
        // A dropped connection clears its journal before the disconnect prompt is dismissed.
        // Do not retry old rewards on the frozen player while that prompt remains visible.
        if (session != null && !session.Connected)
        {
            numberPresentation.Clear();
            return true;
        }
        numberPresentation.TryPresent(
            numbers, Time.unscaledTime, TrySpawnCombatNumberForCurrentWorld,
            OwnsUnshownExperienceForCurrentWorld());
        if (numberPresentation.TakeBacklogWarning())
            Debug.LogWarning("[飘字] 未显示经验提示超过256条，已淘汰最旧提示；实际经验不受影响。");
        lastCombatNumbersVersion = numbersVersion;

        return true;
    }

    private bool OwnsUnshownExperienceForCurrentWorld()
    {
        // Ownership is separate from resource/anchor readiness. A live frame may
        // own an unshown visual even when its glyphs or local anchor are missing.
        // A receipt first seen while switching or on a superseded frame is unknown;
        // do not assume it came from the old map or discard a real new-map reward.
        GameBootstrap owner = NpcSpawnOwnerForIndependentRuntime;
        return owner == null || (!owner.Switching &&
            ReferenceEquals(owner.Window?.Frame, this));
    }

    private bool TrySpawnCombatNumberForCurrentWorld(WorldCombatNumberState number)
    {
        if (number.Type == WorldCombatNumberState.ExperienceType)
        {
            // During fade/loading the old frame can still tick and its transform can remain
            // registered. Queue experience until the live, current frame owns the new player.
            GameBootstrap owner = NpcSpawnOwnerForIndependentRuntime;
            if (owner != null && (owner.Switching ||
                !ReferenceEquals(owner.Window?.Frame, this))) return false;
            if (player == null || player.Root == null ||
                CombatNumber.PlayerTransform != player.Root) return false;
        }
        return combatFeedback.TrySpawnWorldNumber(number);
    }

    internal bool ObserveNativeLifeForIndependentRuntime(PlayerController owner)
    {
        if (disposed || player == null || !player.ObserveNativeLifeForIndependentRuntime(owner))
            return false;
        localPlayerDead = owner.IsDead;
        return true;
    }

    internal bool ApplyLocalPlayerLifeRevision(
        WorldLocalPlayerLifeState state,
        long version)
    {
        ThrowIfDisposed();
        if (!built || version < 0 || version < lastLocalPlayerLifeStateVersion ||
            (state != null && state.EntityId != character.EntityId))
        {
            return false;
        }

        if (version == lastLocalPlayerLifeStateVersion)
        {
            return true;
        }

        if (state != null && state.NativeActionsHandled)
        {
            lastLocalPlayerLifeStateVersion = version;
            return true;
        }

        bool nextDead = state != null && !state.Alive;
        if (state != null && nextDead)
        {
            if (player == null || !player.StartDeathState())
            {
                return false;
            }
        }
        else if (state != null && (player == null || !player.ReviveFromDeath()))
        {
            return false;
        }
        else if (state == null && localPlayerDead)
        {
            if (player == null || !player.ReviveFromDeath())
            {
                return false;
            }
        }

        localPlayerDead = nextDead;
        lastLocalPlayerLifeStateVersion = version;
        return true;
    }

    private bool ApplyWorldEntitySnapshot(
        IReadOnlyDictionary<int, WorldEntityState> snapshot)
    {
        Dictionary<int, ReferenceWorldMonsterActor> replacements =
            new Dictionary<int, ReferenceWorldMonsterActor>();
        try
        {
            foreach (KeyValuePair<int, WorldEntityState> pair in snapshot)
            {
                WorldEntityState state = pair.Value;
                if (state == null || pair.Key != state.EntityId)
                {
                    lastWorldEntityReconciliationFailure =
                        ReferenceWorldEntityReconciliationFailure.InvalidRow;
                    return false;
                }

                if (!IsInitialMonster(state))
                {
                    continue;
                }

                ReferenceWorldMonsterActor current;
                if (monsterActors.TryGetValue(pair.Key, out current) &&
                    current.MatchesSpawnRoot(state))
                {
                    float nowSeconds = Time.time;
                    if (!current.TryApplyAuthoritativeStatusEffects(
                            state,
                            nowSeconds) ||
                        !current.TryApplyAuthoritativeStatusIcons(
                            state,
                            nowSeconds))
                    {
                        lastWorldEntityReconciliationFailure =
                            ReferenceWorldEntityReconciliationFailure.ExistingUpdateFailed;
                        return false;
                    }

                    continue;
                }

                if (current == null && state.HitPointsOrRadiusPixels <= 0)
                {
                    // Official death completion owns actor removal before the server necessarily
                    // removes the terminal row. A later snapshot revision for another entity must
                    // not rehydrate this zero-HP root. A positive-HP spawn below clears the same-
                    // generation tombstone and may construct it once again.
                    terminalWorldEntityTombstones.Add(pair.Key);
                    continue;
                }

                if (state.HitPointsOrRadiusPixels > 0)
                {
                    terminalWorldEntityTombstones.Remove(pair.Key);
                }

                ReferenceWorldMonsterActor replacement =
                    new ReferenceWorldMonsterActor(
                        map,
                        resources,
                        root,
                        state,
                        remoteFeedbackAssets,
                        camera,
                        character.Level,
                        additiveMaterial);
                if (!replacement.Build())
                {
                    replacement.Dispose();
                    lastWorldEntityReconciliationFailure =
                        ReferenceWorldEntityReconciliationFailure.ReplacementBuildFailed;
                    return false;
                }

                replacements[pair.Key] = replacement;
            }

            List<int> removed = new List<int>();
            foreach (int entityId in monsterActors.Keys)
            {
                WorldEntityState state;
                if (!snapshot.TryGetValue(entityId, out state) ||
                    !IsInitialMonster(state))
                {
                    removed.Add(entityId);
                }
            }

            foreach (int entityId in removed)
            {
                targetSelection?.OnGone(monsterActors[entityId]);
                CombatWorld.Remove(entityId, 0);
                monsterActors[entityId].Dispose();
                monsterActors.Remove(entityId);
            }

            foreach (KeyValuePair<int, ReferenceWorldMonsterActor> pair in replacements)
            {
                ReferenceWorldMonsterActor previous;
                if (monsterActors.TryGetValue(pair.Key, out previous))
                {
                    targetSelection?.OnGone(previous);
                    CombatWorld.Remove(pair.Key, 0);
                    previous.Dispose();
                }

                monsterActors[pair.Key] = pair.Value;
                CombatWorld.RegisterSnapshotMonsterForIndependentRuntime(pair.Value.SelectableOwner);
            }

            replacements.Clear();
            return true;
        }
        finally
        {
            foreach (ReferenceWorldMonsterActor replacement in replacements.Values)
            {
                replacement.Dispose();
            }
        }
    }

    private bool IsInitialMonster(WorldEntityState state)
    {
        return ReferenceWorldMonsterActor.IsMonsterState(state) &&
               (state.MapIdGuard < 1 || state.MapIdGuard == mapDescriptor.id);
    }

    private bool ApplyRemotePlayerSnapshot(
        IReadOnlyDictionary<int, WorldRemotePlayerState> snapshot,
        float standTimeSeconds)
    {
        Dictionary<int, ReferenceWorldRemotePlayerActor> replacements =
            new Dictionary<int, ReferenceWorldRemotePlayerActor>();
        try
        {
            // A root revision is applied through the official owners CombatWorld.UpdatePlayerAppearance,
            // UpdatePlayerHair and RenamePlayer, and every one of them resolves the id through
            // CombatWorld.I.players, the same dictionary native SpawnPlayer publishes into. The snapshot
            // drain never runs the local-movement activation branch, so the owner is pointed at
            // this frame here too - the same re-point ApplyRemotePathSnapshot makes for
            // CombatWorld.MovePath.
            combatWorld?.ActivateForIndependentRuntime();

            foreach (KeyValuePair<int, WorldRemotePlayerState> pair in snapshot)
            {
                WorldRemotePlayerState state = pair.Value;
                if (state == null || pair.Key != state.EntityId)
                {
                    return false;
                }

                ReferenceWorldRemotePlayerActor current;
                if (remoteActors.TryGetValue(pair.Key, out current))
                {
                    if (current.MatchesSpawnRoot(state))
                    {
                        if (!current.CanApplyAuthoritativeState(state))
                        {
                            return false;
                        }

                        continue;
                    }

                    // CombatWorld.SpawnPlayer (0x06000133) keeps an already registered id, so a
                    // changed root field is revised through its own official owner instead of
                    // rebuilding the identity. A row requiring another observer falls through;
                    // the native dictionary still decides whether a new root is needed.
                    if (current.TryApplySpawnRootRevision(state, standTimeSeconds))
                    {
                        continue;
                    }
                }

                ReferenceWorldRemotePlayerActor replacement =
                    new ReferenceWorldRemotePlayerActor(
                        map,
                        resources,
                        root,
                        state,
                        remoteFeedbackAssets);
                if (!replacement.Build(standTimeSeconds))
                {
                    replacement.Dispose();
                    return false;
                }

                replacements[pair.Key] = replacement;
            }

            foreach (KeyValuePair<int, WorldRemotePlayerState> pair in snapshot)
            {
                ReferenceWorldRemotePlayerActor current;
                if (!remoteActors.TryGetValue(pair.Key, out current) ||
                    replacements.ContainsKey(pair.Key) ||
                    ReferenceEquals(current.State, pair.Value))
                {
                    // The last arm is a root revision that already applied this exact row.
                    continue;
                }

                if (!current.TryApplyAuthoritativeState(
                        pair.Value,
                        standTimeSeconds))
                {
                    return false;
                }
            }

            List<int> removed = new List<int>();
            foreach (int entityId in remoteActors.Keys)
            {
                if (!snapshot.ContainsKey(entityId))
                {
                    removed.Add(entityId);
                }
            }

            foreach (int entityId in removed)
            {
                targetSelection?.OnGone(remoteActors[entityId]);
                CombatWorld.Remove(entityId, 0);
                remoteActors[entityId].Dispose();
                remoteActors.Remove(entityId);
            }

            foreach (KeyValuePair<int, ReferenceWorldRemotePlayerActor> pair in replacements)
            {
                ReferenceWorldRemotePlayerActor previous;
                if (remoteActors.TryGetValue(pair.Key, out previous))
                {
                    targetSelection?.OnGone(previous);
                    previous.Dispose();
                }

                remoteActors[pair.Key] = pair.Value;
                // Build already executed native SpawnPlayer, including publication and death.
                // Swapping observer views never unregisters or destroys its retained identity.
                pair.Value.ApplySpawnDispatchTail(pair.Value.State);
            }

            replacements.Clear();
            return true;
        }
        finally
        {
            foreach (ReferenceWorldRemotePlayerActor replacement in replacements.Values)
            {
                replacement.Dispose();
            }
        }
    }

    private bool ApplyRemoteChatBubbleSnapshot(
        IReadOnlyDictionary<int, WorldRemoteChatBubbleState> snapshot,
        float now)
    {
        if (float.IsNaN(now) || float.IsInfinity(now))
        {
            return false;
        }

        foreach (KeyValuePair<int, WorldRemoteChatBubbleState> pair in snapshot)
        {
            if (pair.Value == null || pair.Key != pair.Value.EntityId)
            {
                return false;
            }
        }

        foreach (KeyValuePair<int, ReferenceWorldRemotePlayerActor> pair in remoteActors)
        {
            WorldRemoteChatBubbleState bubble;
            if (!snapshot.TryGetValue(pair.Key, out bubble))
            {
                pair.Value.ClearChatBubble();
                continue;
            }

            ReferenceWorldRemoteChatBubbleView view = pair.Value.ChatBubbleView;
            if ((view == null || bubble.Revision > view.LastRevision) &&
                !pair.Value.TryApplyChatBubble(bubble, camera, now))
            {
                return false;
            }
        }

        return true;
    }

    private bool ApplyRemotePathSnapshot(
        IReadOnlyDictionary<int, WorldRemotePathState> snapshot,
        float standTimeSeconds)
    {
        foreach (KeyValuePair<int, WorldRemotePathState> pair in snapshot)
        {
            if (pair.Value == null || pair.Key != pair.Value.EntityId)
            {
                return false;
            }
        }

        // The snapshot drain never runs the local-movement activation branch, so the owner has to
        // be pointed at this frame here too: the actors below hand their decoded arrays to
        // CombatWorld.MovePath, which resolves the id through CombatWorld.I.players.
        combatWorld?.ActivateForIndependentRuntime();

        foreach (KeyValuePair<int, WorldRemotePathState> pair in snapshot)
        {
            WorldRemotePathState previous;
            if (appliedRemotePaths.TryGetValue(pair.Key, out previous) &&
                ReferenceEquals(previous, pair.Value))
            {
                continue;
            }

            ReferenceWorldRemotePlayerActor actor;
            if (remoteActors.TryGetValue(pair.Key, out actor) &&
                !actor.TryApplyPath(pair.Value, standTimeSeconds))
            {
                return false;
            }

            appliedRemotePaths[pair.Key] = pair.Value;
        }

        List<int> removed = new List<int>();
        foreach (int entityId in appliedRemotePaths.Keys)
        {
            if (!snapshot.ContainsKey(entityId))
            {
                removed.Add(entityId);
            }
        }

        foreach (int entityId in removed)
        {
            // Removing a retained receive snapshot is not a server path-cancellation command.
            appliedRemotePaths.Remove(entityId);
        }

        return true;
    }

    private bool ApplyMonsterPathSnapshot(
        IReadOnlyDictionary<int, WorldRemotePathState> snapshot)
    {
        foreach (KeyValuePair<int, WorldRemotePathState> pair in snapshot)
        {
            if (pair.Value == null || pair.Key != pair.Value.EntityId)
            {
                return false;
            }
        }

        // Same lifecycle wiring as the remote-player snapshot: the monster actors registered in
        // this frame's combatWorld must be the ones CombatWorld.MovePath looks up through
        // CombatWorld.I.monsters.
        combatWorld?.ActivateForIndependentRuntime();

        foreach (KeyValuePair<int, WorldRemotePathState> pair in snapshot)
        {
            WorldRemotePathState previous;
            if (appliedMonsterPaths.TryGetValue(pair.Key, out previous) &&
                ReferenceEquals(previous, pair.Value))
            {
                continue;
            }

            ReferenceWorldMonsterActor actor;
            if (monsterActors.TryGetValue(pair.Key, out actor) &&
                !actor.TryApplyPath(pair.Value))
            {
                return false;
            }

            appliedMonsterPaths[pair.Key] = pair.Value;
        }

        List<int> removed = new List<int>();
        foreach (int entityId in appliedMonsterPaths.Keys)
        {
            if (!snapshot.ContainsKey(entityId))
            {
                removed.Add(entityId);
            }
        }

        foreach (int entityId in removed)
        {
            ReferenceWorldMonsterActor actor;
            if (monsterActors.TryGetValue(entityId, out actor) &&
                !actor.TryClearPath())
            {
                return false;
            }

            appliedMonsterPaths.Remove(entityId);
        }

        return true;
    }

    private bool ApplyRemoteActionSnapshot(
        IReadOnlyDictionary<int, WorldRemoteActionState> snapshot)
    {
        foreach (KeyValuePair<int, WorldRemoteActionState> pair in snapshot)
        {
            if (pair.Value == null || pair.Key != pair.Value.EntityId ||
                pair.Value.Revision < 1)
            {
                return false;
            }
        }

        foreach (KeyValuePair<int, WorldRemoteActionState> pair in snapshot)
        {
            WorldRemoteActionState previous;
            if (appliedRemoteActions.TryGetValue(pair.Key, out previous) &&
                ReferenceEquals(previous, pair.Value))
            {
                continue;
            }

            ReferenceWorldRemotePlayerActor actor;
            if (remoteActors.TryGetValue(pair.Key, out actor))
            {
                Vector3? faceTargetPosition = null;
                if (pair.Value.Kind == WorldRemoteActionKind.Attack)
                {
                    if (pair.Value.TargetEntityId == character.EntityId &&
                        player != null && player.Root != null)
                    {
                        faceTargetPosition = player.Root.position;
                    }
                    else
                    {
                        ReferenceWorldMonsterActor monster;
                        if (monsterActors.TryGetValue(pair.Value.TargetEntityId, out monster) &&
                            monster.RootObject != null)
                        {
                            faceTargetPosition = monster.RootObject.transform.position;
                        }
                        else
                        {
                            ReferenceWorldRemotePlayerActor target;
                            if (remoteActors.TryGetValue(pair.Value.TargetEntityId, out target) && target.Root != null)
                                faceTargetPosition = target.Root.position;
                        }
                    }
                }

                if (!actor.TryApplyAction(pair.Value, faceTargetPosition))
                {
                    return false;
                }
            }

            appliedRemoteActions[pair.Key] = pair.Value;
        }

        List<int> removed = new List<int>();
        foreach (int entityId in appliedRemoteActions.Keys)
        {
            if (!snapshot.ContainsKey(entityId))
            {
                removed.Add(entityId);
            }
        }

        foreach (int entityId in removed)
        {
            appliedRemoteActions.Remove(entityId);
        }

        return true;
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(ReferenceFirstWorldInitialFrame));
        }
    }
}

public enum ReferenceWorldRevisionFailure
{
    None = 0,
    CaptureRejected = 1,
    PlayerRevisionRejected = 2,
    NpcRevisionRejected = 3,
    EntityRevisionRejected = 4,
    MonsterPathRevisionRejected = 5,
    GroundItemRevisionRejected = 6,
    CombatRevisionRejected = 7,
    LocalLifeRevisionRejected = 8,
    VisibilityRevisionRejected = 9
}

public enum ReferenceWorldEntityReconciliationFailure
{
    None = 0,
    VersionRegressed = 1,
    SceneGenerationRegressed = 2,
    SceneMapMismatch = 3,
    InvalidRow = 4,
    ExistingUpdateFailed = 5,
    ReplacementBuildFailed = 6,
}

}
