using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Protocol
{

public sealed partial class PrivateServerGateway : IGameServerGateway, IWorldRemoteEventSource,
    IWorldLocalStatusEffectSource, IWorldReceivePump, ICharacterWorkCommands, ICharacterSettingsCommands, IWorldReturnCommands, ICharacterReviveCommands, INewbieTipCommands, IHotbarWriteCommands, IPetForgeCommands, IAppliedPlayerLifeSink
{
    private const int MaximumPacketsBeforeExpectedResponse = 512;
    private const int MaximumRetainedChannelChatRows = 801;
    private const int MaximumRetainedPrivateChatRows = 801;
    private const int MaximumRetainedCombatEdges = 256;
    private const int MaximumRetainedNpcDialogTextEdges = 256;
    private const int MaximumDownlinkDiagnosticRows = 256;
    // Official 0x8001 acceptance bound: arm64 `cmp w8,#0x752 / b.hi` after `lsr w8,w19,#5` and
    // x86_64 `cmpl $0xea5f,%ebx / ja` are the same unsigned test, i.e. (uint)delta <= 59999.
    private const int MaxRoundTripMilliseconds = 59999;
    private readonly PrivateServerOptions _options;
    private readonly Action<string> _diagnosticSink;
    private readonly Func<float> _dispatchMillisecondsProvider;
    private readonly object _worldStateSync = new object();
    private readonly object _pendingDownlinkSync = new object();
    private readonly List<ProtocolPacket> _pendingDownlinks =
        new List<ProtocolPacket>();
    private readonly ConcurrentQueue<ProtocolPacket> _dispatchedDownlinks =
        new ConcurrentQueue<ProtocolPacket>();
    private readonly SemaphoreSlim _dispatchedReady = new SemaphoreSlim(0);
    private readonly SemaphoreSlim _sendOperation = new SemaphoreSlim(1, 1);
    private readonly Dictionary<int, WorldEntityState> _worldTraps = new Dictionary<int, WorldEntityState>();
    private readonly Dictionary<int, long> _trapIncarnations = new Dictionary<int, long>();
    private readonly NetClient _netClient;
    // Explicit host-thread execution dependency: everything that reaches the official
    // PaceTx / DrainTx / Close state (native NetClient.Send -> PaceTx, NetClient.Update ->
    // DrainTx and Ping, NetClient.Close) runs on the Unity main thread through this executor.
    // It is injected by the composition root; the gateway keeps no context of its own and
    // never blocks a caller on a cross-thread hand-off.
    private readonly IHostExecutor _host;
    // Official 0x8044 and 0x804c write their static owners while the Dispatch case body runs,
    // with no world-frame gate and no dependency on the world window being built. Protocol
    // stays Unity-free and never references Presentation, so the composition root binds the
    // one real owner here and every receive path - the in-world drain, the pre-world FIFO drain
    // and the startup reader - calls it at the same point of the same packet.
    private readonly IWorldReceiveOwner _worldReceiveOwner;
    private readonly Func<float> _realtimeSecondsProvider;
    // Official 0x8017 classifies the current map before its row loop with
    // CustomMaps.IsCustom(GameBootstrap.CurMapId), and both of those types belong to
    // Presentation. Protocol therefore receives that one host question as a Func<bool> probe
    // bound by the composition root, exactly like the host clock below; the probe is invoked
    // once per packet inside the read and nothing of its answer outlives that call.
    private readonly Func<bool> _customMapProvider;
    private CancellationTokenSource _connectionLifetime;
    // Host-thread-only terminal fence; ordinary Dispose still permits the title's reuse.
    private bool _hostShutdown;
    // Aliases of the native retained callback and its existing gateway lifetime. They do not
    // start/queue work or replace _connecting. They only contain an abandoned host connection
    // until its native completion is drained, so its late socket cannot enter a new session.
    private Action<bool> _pendingNativeConnect;
    private CancellationTokenSource _pendingNativeConnectLifetime;
    private CancellationToken _pendingNativeConnectCallerToken;
    private GatewayStage _stage;
    private IReadOnlyList<WorldTeleportState> _teleportStates =
        Array.Empty<WorldTeleportState>();
    private long _teleportStatesVersion;
    private long _teleportSequence;
    private WorldPlayerStatusState _playerStatusState;
    private long _playerStatusStateVersion;
    private WorldPlayerAppearanceState _playerAppearanceState;
    private long _playerAppearanceStateVersion;
    private WorldCharacterTitleState _characterTitleState =
        WorldCharacterTitleState.Empty;
    private long _characterTitleStateVersion;
    private WorldLocalPlayerLifeState _localPlayerLifeState;
    private long _localPlayerLifeStateVersion;
    private long _localPlayerLifeRevision;
    private readonly Queue<WorldEntityStatusEffect> _localPlayerStatusEffects =
        new Queue<WorldEntityStatusEffect>();
    private readonly Guid _localStatusEffectSourceId = Guid.NewGuid();
    private long _localStatusEffectConnectionGeneration;
    private long _localStatusEffectAcknowledgedRevision;
    private long _localPlayerStatusEffectsVersion;
    private long _localPlayerStatusEffectRevision;
    private IReadOnlyList<WorldSkillHitEffect> _skillHitEffects =
        Array.Empty<WorldSkillHitEffect>();
    private long _skillHitEffectsVersion;
    private long _skillHitEffectRevision;
    private WorldSelfPkState _selfPkState;
    private long _selfPkStateVersion;
    private WorldLocalFactionState _localFactionState;
    private long _localFactionStateVersion;
    private WorldRidingState _localRidingState;
    private long _localRidingStateVersion;
    private IReadOnlyDictionary<int, WorldRemoteRidingState> _remoteRidingStates =
        new ReadOnlyDictionary<int, WorldRemoteRidingState>(
            new Dictionary<int, WorldRemoteRidingState>());
    private long _remoteRidingStatesVersion;
    private IReadOnlyDictionary<int, WorldRemotePlayerState> _remotePlayers =
        new ReadOnlyDictionary<int, WorldRemotePlayerState>(
            new Dictionary<int, WorldRemotePlayerState>());
    private long _remotePlayersVersion;
    private IReadOnlyDictionary<int, WorldNpcState> _npcs =
        new ReadOnlyDictionary<int, WorldNpcState>(
            new Dictionary<int, WorldNpcState>());
    private long _npcsVersion;
    private WorldNpcChatterState _npcChatterState;
    private long _npcChatterStateVersion;
    private WorldNpcTaskOffersState _npcTaskOffersState;
    private long _npcTaskOffersStateVersion;
    private WorldShopState _shopState = WorldShopState.Empty;
    private long _shopStateVersion;
    private WorldTransportState _transportState = WorldTransportState.Empty;
    private long _transportStateVersion;
    private WorldWarehouseState _warehouseState = WorldWarehouseState.Empty;
    private long _warehouseStateVersion;
    private WorldTaskLogState _taskLogState = WorldTaskLogState.Empty;
    private long _taskLogStateVersion;
    private WorldNpcDialogTextState _npcDialogTextState;
    private long _npcDialogTextStateVersion;
    private WorldNpcDialogTextEdgeWindow _npcDialogTextEdgeWindow =
        WorldNpcDialogTextEdgeWindow.Empty;
    private long _npcDialogTextEdgeRevision;
    private IReadOnlyDictionary<int, WorldEntityState> _worldEntities =
        new ReadOnlyDictionary<int, WorldEntityState>(
            new Dictionary<int, WorldEntityState>());
    private long _worldEntitiesVersion;
    private WorldEntityPublicationState _worldEntityPublicationState =
        WorldEntityPublicationState.Empty;
    private WorldEntitySceneSnapshot _worldEntitySceneSnapshot =
        WorldEntitySceneSnapshot.Empty;
    private IReadOnlyDictionary<int, WorldGroundItemState> _groundItems =
        new ReadOnlyDictionary<int, WorldGroundItemState>(
            new Dictionary<int, WorldGroundItemState>());
    private long _groundItemsVersion;
    private WorldGroundLootAuthorityState _groundLootAuthorityState =
        WorldGroundLootAuthorityState.Empty;
    private long _groundLootAuthoritySequence;
    private IReadOnlyList<WorldCombatResultState> _combatResults =
        Array.Empty<WorldCombatResultState>();
    private long _combatResultsVersion;
    private long _combatResultRevision;
    private IReadOnlyList<WorldCombatNumberState> _combatNumbers =
        Array.Empty<WorldCombatNumberState>();
    private long _combatNumbersVersion;
    private long _combatNumberRevision;
    private WorldTargetStatusState _targetStatusState;
    private long _targetStatusStateVersion;
    private WorldBuffState _buffState = WorldBuffState.Empty;
    private long _buffStateVersion;
    private WorldInspectedPlayerEquipmentState _inspectedPlayerEquipmentState;
    private long _inspectedPlayerEquipmentStateVersion;
    private IReadOnlyDictionary<int, WorldRemotePathState> _remotePaths =
        new ReadOnlyDictionary<int, WorldRemotePathState>(
            new Dictionary<int, WorldRemotePathState>());
    private long _remotePathsVersion;
    private IReadOnlyDictionary<int, WorldRemotePathState> _monsterPaths =
        new ReadOnlyDictionary<int, WorldRemotePathState>(
            new Dictionary<int, WorldRemotePathState>());
    private long _monsterPathsVersion;
    private IReadOnlyDictionary<int, WorldRemoteActionState> _remoteActions =
        new ReadOnlyDictionary<int, WorldRemoteActionState>(
            new Dictionary<int, WorldRemoteActionState>());
    private long _remoteActionsVersion;
    private long _remoteMovementActionRevision;
    private readonly Guid _remoteEventSourceId = Guid.NewGuid();
    private readonly List<WorldRemoteEvent> _remoteEvents = new List<WorldRemoteEvent>();
    private readonly Dictionary<int, long> _remoteIncarnations = new Dictionary<int, long>();
    private readonly Dictionary<int, long> _entityIncarnations = new Dictionary<int, long>();
    private long _entityNextIncarnation;
    private long _remoteEventGeneration;
    private int _remoteEventMapId;
    private long _remoteEventSequence;
    private long _remoteAcknowledgedSequence;
    private long _remoteNextIncarnation;
    // Compatibility event observation only. The native typed registries own NPC rows/roots;
    // neither these edges nor a session snapshot may spawn or remove a root.
    private bool _remoteEventIncludesNpcEvents;
    private WorldRemoteSceneState _remoteEventBaseline =
        new WorldRemoteSceneState(null, null, null, null, WorldPartyState.Empty, null);
    private WorldSkillCastState _skillCastState;
    private IReadOnlyList<WorldSkillCastState> _skillCastEvents =
        Array.Empty<WorldSkillCastState>();
    private long _skillCastStateVersion;
    private long _skillCastRevision;
    private IReadOnlyDictionary<int, WorldRemoteChatBubbleState> _remoteChatBubbles =
        new ReadOnlyDictionary<int, WorldRemoteChatBubbleState>(
            new Dictionary<int, WorldRemoteChatBubbleState>());
    private long _remoteChatBubblesVersion;
    private long _remoteChatBubbleRevision;
    private IReadOnlyList<WorldChannelChatState> _channelChatRows =
        Array.Empty<WorldChannelChatState>();
    private long _channelChatRowsVersion;
    private long _channelChatRevision;
    private IReadOnlyList<WorldPrivateChatState> _privateChatRows =
        Array.Empty<WorldPrivateChatState>();
    private long _privateChatRowsVersion;
    private long _privateChatRevision;
    private WorldPartyState _partyState = WorldPartyState.Empty;
    private long _partyStateVersion;
    private int _accountHasSecurityCode;
    private int _accountSecurityWindowMilliseconds;
    private long _accountSecuritySeedVersion;
    private IReadOnlyDictionary<int, WorldStallOwnerState> _stallOwners =
        new ReadOnlyDictionary<int, WorldStallOwnerState>(
            new Dictionary<int, WorldStallOwnerState>());
    private long _stallOwnersVersion;
    private IReadOnlyList<WorldSystemRequestState> _systemRequests =
        Array.Empty<WorldSystemRequestState>();
    private long _systemRequestsVersion;
    private WorldInventoryState _inventoryState = WorldInventoryState.Empty;
    private long _inventoryStateVersion;
    private IWorldWardrobeObservation _wardrobeState = WorldWardrobeState.Empty;
    private long _wardrobeStateVersion;
    private WorldHotbarState _hotbarState = WorldHotbarState.Empty;
    private long _hotbarStateVersion;
    private WorldSkillBookState _skillBookState = WorldSkillBookState.Empty;
    private long _skillBookStateVersion;
    private WorldLifeSkillBookState _lifeSkillBookState = WorldLifeSkillBookState.Empty;
    private long _lifeSkillBookStateVersion;
    private WorldActiveTradeState _activeTradeState = WorldActiveTradeState.Empty;
    private long _activeTradeStateVersion;
    private WorldTradeEndState _tradeEndState;
    private long _tradeEndStateVersion;
    private int _currentWorldMapId;
    private int _currentWorldEntityId;
    private WorldPlayerRenameState _localRename;
    private int _downlinkDiagnosticRows;

    // realtimeSecondsProvider is the host clock the official NetClient rate limiter reads
    // (UnityEngine.Time.realtimeSinceStartup); Bootstrap/QA entries supply it so Protocol
    // stays Unity-free. See NetClient.PaceTx / NetClient.DrainTx.
    // receiveTrace and dispatchMillisecondsProvider are the two host capabilities the
    // recovered NetClient.Update (0x0600077D) reads every frame: the LoadTrace port and the
    // receive budget. The context-carrying Pump still prefers the budget of the live
    // IWorldReceiveBudgetContext; the provider is what the context-free Pump uses, so no
    // caller falls back to a hard-coded 6f budget. Both are bound by the composition root.
    // customMapProvider answers the one host question official 0x8017 asks before its row loop
    // - CustomMaps.IsCustom(GameBootstrap.CurMapId), field token 67110216 / 0x060001d0 - and is
    // bound the same way, because Presentation owns those types and Protocol may not name them.
    // loginEnvironment supplies lazy device/detector capabilities at the exact Login/Register
    // write positions. Bootstrap always binds the real platform; omitting it preserves only
    // the options-backed diagnostic constructors, not the production title environment.
    public PrivateServerGateway(
        PrivateServerOptions options,
        IHostExecutor hostExecutor,
        Func<float> realtimeSecondsProvider,
        IWorldReceiveTracePort receiveTrace = null,
        Func<float> dispatchMillisecondsProvider = null,
        IWorldReceiveOwner worldReceiveOwner = null,
        Func<bool> customMapProvider = null,
        NetClientLoginEnvironment loginEnvironment = null)
        : this(options, null, hostExecutor, realtimeSecondsProvider, receiveTrace,
            dispatchMillisecondsProvider, worldReceiveOwner, customMapProvider, loginEnvironment)
    {
    }

    public PrivateServerGateway(
        PrivateServerOptions options,
        Action<string> diagnosticSink,
        IHostExecutor hostExecutor,
        Func<float> realtimeSecondsProvider,
        IWorldReceiveTracePort receiveTrace = null,
        Func<float> dispatchMillisecondsProvider = null,
        IWorldReceiveOwner worldReceiveOwner = null,
        Func<bool> customMapProvider = null,
        NetClientLoginEnvironment loginEnvironment = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _diagnosticSink = diagnosticSink;
        _host = hostExecutor ?? throw new ArgumentNullException(nameof(hostExecutor));
        _worldReceiveOwner = worldReceiveOwner;
        _skillBookReceiveOwner = worldReceiveOwner as ISkillBookReceiveOwner;
        _petVitalsReceiveOwner = worldReceiveOwner as IWorldPetVitalsReceiveOwner;
        _realtimeSecondsProvider = realtimeSecondsProvider;
        _customMapProvider = customMapProvider;
        _dispatchMillisecondsProvider = dispatchMillisecondsProvider;
        // The official backlog warning goes through UnityEngine.Debug.LogWarning; the existing
        // diagnostic sink is that host capability at the composition root, so the formatted
        // official message is passed through unchanged and no branch is added here.
        _netClient = new NetClient(
            _options,
            realtimeSecondsProvider,
            diagnosticSink,
            receiveTrace,
            dispatchMillisecondsProvider,
            loginEnvironment);
    }

    public bool Connected => _netClient.Connected;

    // Forwarding members for the two NetClient members the recovered title notice reads and
    // subscribes. Official NetClient.OnDisconnected (+0x80, atlas offset 128) is assigned once,
    // with no Delegate.Combine and never unsubscribed, by the tail of GameBootstrap.Start
    // (0x060003ef, arm64 060003ef.c:268-282 / x86_64 :231-243) after a NetClient.I != null test;
    // Update (0x0600077D) invokes it once per detected drop, right after Close(), on the host
    // thread that drives Pump. LastDropReason (+0xb0) is the transport-owned volatile string this
    // gateway only passes through; nothing here decides, formats or caches it. The product anchor
    // of that one store is ClientBootstrap.Awake, not Start (see Bootstrap/ClientBootstrap.cs).
    public string LastDropReasonForIndependentRuntime => _netClient.LastDropReason;

    public void BindDisconnectedNotificationForIndependentRuntime(Action notification)
    {
        _netClient.OnDisconnected = () =>
        {
            // Preserve the gateway's existing lifetime/state cleanup on the true native drop
            // notification. Ordinary queued errors no longer reach that cleanup through Pump.
            // Native Update already called Close; this is the product graph cleanup before
            // the existing title/world notification, not another native error decision.
            DisposeConnectionCore();
            notification();
        };
    }

    public IReadOnlyList<WorldTeleportState> TeleportStates =>
        Volatile.Read(ref _teleportStates);

    public long TeleportStatesVersion =>
        Interlocked.Read(ref _teleportStatesVersion);

    public WorldPlayerStatusState PlayerStatusState =>
        ReadPlayerStatusState();

    public long PlayerStatusStateVersion =>
        Interlocked.Read(ref _playerStatusStateVersion);

    public WorldPlayerAppearanceState PlayerAppearanceState =>
        Volatile.Read(ref _playerAppearanceState);

    public long PlayerAppearanceStateVersion =>
        Interlocked.Read(ref _playerAppearanceStateVersion);

    public WorldCharacterTitleState CharacterTitleState =>
        Volatile.Read(ref _characterTitleState);

    public long CharacterTitleStateVersion =>
        Interlocked.Read(ref _characterTitleStateVersion);

    public WorldLocalPlayerLifeState LocalPlayerLifeState =>
        Volatile.Read(ref _localPlayerLifeState);

    public long LocalPlayerLifeStateVersion =>
        Interlocked.Read(ref _localPlayerLifeStateVersion);

    public IReadOnlyList<WorldEntityStatusEffect> LocalPlayerStatusEffects
    {
        get
        {
            lock (_worldStateSync)
                return new ReadOnlyCollection<WorldEntityStatusEffect>(_localPlayerStatusEffects.ToArray());
        }
    }

    public long LocalPlayerStatusEffectsVersion =>
        Interlocked.Read(ref _localPlayerStatusEffectsVersion);

    public WorldLocalStatusEffectWindow CaptureLocalStatusEffects()
    {
        lock (_worldStateSync)
            return new WorldLocalStatusEffectWindow(_localStatusEffectSourceId,
                _localStatusEffectConnectionGeneration, _localStatusEffectAcknowledgedRevision,
                _localPlayerStatusEffectRevision, _localPlayerStatusEffects);
    }

    private bool IsCurrentLocalStatusEffectWindowLocked(WorldLocalStatusEffectWindow window)
    {
        return window != null && window.IsFrom(_localStatusEffectSourceId) &&
            window.ConnectionGeneration == _localStatusEffectConnectionGeneration &&
            window.BaseRevision <= _localStatusEffectAcknowledgedRevision &&
            window.LastRevision <= _localPlayerStatusEffectRevision;
    }

    public bool IsCurrentLocalStatusEffect(WorldLocalStatusEffectWindow window, long revision)
    {
        lock (_worldStateSync)
        {
            if (!IsCurrentLocalStatusEffectWindowLocked(window) ||
                revision <= _localStatusEffectAcknowledgedRevision ||
                revision > window.LastRevision || _localPlayerStatusEffects.Count == 0)
                return false;
            WorldEntityStatusEffect next = _localPlayerStatusEffects.Peek();
            return next.Revision == revision && ReferenceEquals(next,
                window.Edges[checked((int)(revision - window.BaseRevision - 1))]);
        }
    }

    public bool AcknowledgeLocalStatusEffects(WorldLocalStatusEffectWindow window, long consumedThrough)
    {
        lock (_worldStateSync)
        {
            if (!IsCurrentLocalStatusEffectWindowLocked(window) ||
                consumedThrough < window.BaseRevision || consumedThrough > window.LastRevision)
                return false;
            if (consumedThrough <= _localStatusEffectAcknowledgedRevision) return true;
            int first = checked((int)(_localStatusEffectAcknowledgedRevision - window.BaseRevision));
            if (_localPlayerStatusEffects.Count == 0 ||
                !ReferenceEquals(_localPlayerStatusEffects.Peek(), window.Edges[first]))
                return false;
            while (_localPlayerStatusEffects.Count > 0 &&
                _localPlayerStatusEffects.Peek().Revision <= consumedThrough)
                _localPlayerStatusEffects.Dequeue();
            _localStatusEffectAcknowledgedRevision = consumedThrough;
            Interlocked.Increment(ref _localPlayerStatusEffectsVersion);
            return true;
        }
    }

    public IReadOnlyList<WorldSkillHitEffect> SkillHitEffects =>
        Volatile.Read(ref _skillHitEffects);

    public long SkillHitEffectsVersion =>
        Interlocked.Read(ref _skillHitEffectsVersion);

    public WorldSelfPkState SelfPkState =>
        Volatile.Read(ref _selfPkState);

    public long SelfPkStateVersion =>
        Interlocked.Read(ref _selfPkStateVersion);

    public WorldLocalFactionState LocalFactionState =>
        Volatile.Read(ref _localFactionState);

    public long LocalFactionStateVersion =>
        Interlocked.Read(ref _localFactionStateVersion);

    public WorldRidingState LocalRidingState =>
        Volatile.Read(ref _localRidingState);

    public long LocalRidingStateVersion =>
        Interlocked.Read(ref _localRidingStateVersion);

    public IReadOnlyDictionary<int, WorldRemoteRidingState> RemoteRidingStates =>
        Volatile.Read(ref _remoteRidingStates);

    public long RemoteRidingStatesVersion =>
        Interlocked.Read(ref _remoteRidingStatesVersion);

    public IReadOnlyDictionary<int, WorldRemotePlayerState> RemotePlayers =>
        Volatile.Read(ref _remotePlayers);

    public long RemotePlayersVersion =>
        Interlocked.Read(ref _remotePlayersVersion);

    public WorldRemoteEventWindow CaptureRemoteEvents()
    {
        lock (_worldStateSync)
        {
            return new WorldRemoteEventWindow(_remoteEventSourceId,
                _remoteEventGeneration, _remoteEventMapId, _remoteAcknowledgedSequence,
                _remoteEventBaseline, _remoteEvents, _remoteEventSequence,
                _remoteEventSequence == _remoteAcknowledgedSequence
                    ? _remoteEventBaseline : CaptureRemoteSceneLocked(), true, true, true,
                _remoteEventIncludesNpcEvents, _worldReceiveOwner is ICombatRegistryReceiveOwner);
        }
    }

    public bool AcknowledgeRemoteEvents(WorldRemoteEventWindow window)
    {
        if (window == null) return false;
        lock (_worldStateSync)
        {
            if (!window.IsFrom(_remoteEventSourceId) ||
                window.Generation != _remoteEventGeneration || window.MapId != _remoteEventMapId ||
                window.BaseSequence > _remoteAcknowledgedSequence ||
                window.LastSequence > _remoteEventSequence) return false;
            if (window.LastSequence <= _remoteAcknowledgedSequence) return true;
            _remoteAcknowledgedSequence = window.LastSequence;
            _remoteEventBaseline = window.Snapshot;
            _remoteEvents.RemoveAll(edge => edge.Sequence <= _remoteAcknowledgedSequence);
            return true;
        }
    }

    private WorldRemoteSceneState CaptureRemoteSceneLocked() =>
        new WorldRemoteSceneState(_remotePlayers, _remoteIncarnations, _remotePaths,
            _remoteActions, _partyState, _stallOwners, _localRename, _worldEntities, _entityIncarnations, _worldTraps, _trapIncarnations);

    private void ResetRemoteEventSceneLocked(int mapId)
    {
        _remoteEventGeneration = checked(_remoteEventGeneration + 1);
        _remoteEventMapId = mapId;
        _remoteEventSequence = 0;
        _remoteAcknowledgedSequence = 0;
        _remoteEventIncludesNpcEvents = false;
        _remoteEvents.Clear();
        _remoteIncarnations.Clear();
        _receivedRemoteIdentities.Clear();
        _entityIncarnations.Clear();
        _worldTraps.Clear();
        _trapIncarnations.Clear();
        _remoteEventBaseline = CaptureRemoteSceneLocked();
    }

    private void ClearRemoteSceneSnapshotsLocked()
    {
        Volatile.Write(ref _remotePlayers,
            new ReadOnlyDictionary<int, WorldRemotePlayerState>(new Dictionary<int, WorldRemotePlayerState>()));
        Volatile.Write(ref _remoteRidingStates,
            new ReadOnlyDictionary<int, WorldRemoteRidingState>(new Dictionary<int, WorldRemoteRidingState>()));
        Volatile.Write(ref _remotePaths,
            new ReadOnlyDictionary<int, WorldRemotePathState>(new Dictionary<int, WorldRemotePathState>()));
        Volatile.Write(ref _remoteActions,
            new ReadOnlyDictionary<int, WorldRemoteActionState>(new Dictionary<int, WorldRemoteActionState>()));
        Volatile.Write(ref _remoteChatBubbles,
            new ReadOnlyDictionary<int, WorldRemoteChatBubbleState>(new Dictionary<int, WorldRemoteChatBubbleState>()));
        Volatile.Write(ref _stallOwners,
            new ReadOnlyDictionary<int, WorldStallOwnerState>(new Dictionary<int, WorldStallOwnerState>()));
        Interlocked.Increment(ref _remotePlayersVersion);
        Interlocked.Increment(ref _remoteRidingStatesVersion);
        Interlocked.Increment(ref _remotePathsVersion);
        Interlocked.Increment(ref _remoteActionsVersion);
        Interlocked.Increment(ref _remoteChatBubblesVersion);
        Interlocked.Increment(ref _stallOwnersVersion);
    }

    private void RemoveRemotePlayerLocked(int entityId, byte reason, bool nativeRegistryHandled = false)
    {
        if (!_remotePlayers.ContainsKey(entityId)) return;
        long incarnation;
        _remoteIncarnations.TryGetValue(entityId, out incarnation);
        Dictionary<int, WorldRemotePlayerState> players = CopyRemotePlayers(_remotePlayers, 0);
        Dictionary<int, WorldRemoteRidingState> riding = CopyRemoteRidingStates(_remoteRidingStates, 0);
        players.Remove(entityId);
        riding.Remove(entityId);
        PublishRemoteWorldSnapshots(players, riding, true, true);
        ClearRemotePathLocked(entityId);
        ClearRemoteActionLocked(entityId);
        ClearRemoteChatBubbleLocked(entityId);
        _remoteIncarnations.Remove(entityId);
        _receivedRemoteIdentities.Remove(entityId);
        AppendRemoteEventLocked(WorldRemoteEventKind.Remove, entityId,
            removeReason: reason, incarnation: incarnation, nativeRegistryHandled: nativeRegistryHandled);
    }

    private void AppendEntityEventLocked(WorldRemoteEventKind kind, int entityId,
        WorldEntityState entity = null, WorldRemotePathState path = null,
        WorldEntityStatusEffect effect = null, byte reason = 0, int? hp = null, int ownerId = 0,
        bool nativeRegistryHandled = false, object receivedIdentity = null)
    {
        if (_remoteEventMapId == 0) return;
        var incarnations = entity != null && entity.Kind == WorldEntityKind.Trap
            ? _trapIncarnations : _entityIncarnations;
        incarnations.TryGetValue(entityId, out long incarnation);
        _remoteEvents.Add(new WorldRemoteEvent(checked(++_remoteEventSequence), kind,
            entityId, incarnation, path: path, removeReason: reason,
            statusEffect: effect, hitPoints: hp, entity: entity, ownerId: ownerId,
            nativeRegistryHandled: nativeRegistryHandled, receivedIdentity: receivedIdentity));
    }

    // Optional frame observations keep the existing common event sequence. Each native
    // OnNetNpcSpawn/OnNetNpcRemove has already run on the receipt thread before this edge.
    private void AppendNpcEventLocked(WorldRemoteEventKind kind, int entityId, WorldNpcState npc)
    {
        if (_remoteEventMapId == 0) return;
        _remoteEventIncludesNpcEvents = true;
        _remoteEvents.Add(new WorldRemoteEvent(checked(++_remoteEventSequence), kind,
            entityId, 0, npc: npc));
    }

    private void AppendRemoteEventLocked(WorldRemoteEventKind kind, int entityId = 0,
        WorldRemotePlayerState player = null, WorldPlayerAppearanceState appearance = null,
        WorldRemotePathState path = null, WorldRemoteActionState action = null,
        WorldRemoteRidingState riding = null,
        bool? alive = null, byte? pkState = null, WorldPartyState party = null,
        WorldStallOwnerState stall = null, byte removeReason = 0,
        bool repeatedSpawn = false, long incarnation = 0,
        WorldEntityStatusEffect statusEffect = null, int? hitPoints = null,
        WorldPlayerRenameState rename = null, WorldCombatResultState combat = null,
        string bubbleText = null, string bubbleTheme = null, int? emoteFace = null, WorldDuelHudState duelHud = null,
        bool? invisible = null, bool nativeRegistryHandled = false, object receivedIdentity = null)
    {
        if (_remoteEventMapId == 0) return;
        if (incarnation == 0) _remoteIncarnations.TryGetValue(entityId, out incarnation);
        _remoteEvents.Add(new WorldRemoteEvent(checked(++_remoteEventSequence), kind,
            entityId, incarnation, player, appearance, path, action,
            riding, alive, pkState, party, stall, removeReason, repeatedSpawn, statusEffect, hitPoints, rename, combat: combat, bubbleText: bubbleText, bubbleTheme: bubbleTheme, emoteFace: emoteFace, duelHud: duelHud, invisible: invisible,
            nativeRegistryHandled: nativeRegistryHandled, receivedIdentity: receivedIdentity));
    }

    public IReadOnlyDictionary<int, WorldNpcState> Npcs =>
        Volatile.Read(ref _npcs);

    public long NpcsVersion =>
        Interlocked.Read(ref _npcsVersion);

    public WorldNpcChatterState NpcChatterState =>
        Volatile.Read(ref _npcChatterState);

    public long NpcChatterStateVersion =>
        Interlocked.Read(ref _npcChatterStateVersion);

    public WorldNpcTaskOffersState NpcTaskOffersState =>
        ReadNpcTaskOffersState();

    public long NpcTaskOffersStateVersion =>
        Interlocked.Read(ref _npcTaskOffersStateVersion);

    public WorldShopState ShopState => Volatile.Read(ref _shopState);

    public long ShopStateVersion => Interlocked.Read(ref _shopStateVersion);

    public WorldTransportState TransportState => Volatile.Read(ref _transportState);

    public long TransportStateVersion => Interlocked.Read(ref _transportStateVersion);

    public WorldWarehouseState WarehouseState => Volatile.Read(ref _warehouseState);

    public long WarehouseStateVersion => Interlocked.Read(ref _warehouseStateVersion);

    public WorldTaskLogState TaskLogState => ReadTaskLogState();

    public long TaskLogStateVersion => Interlocked.Read(ref _taskLogStateVersion);

    public WorldNpcDialogTextState NpcDialogTextState =>
        Volatile.Read(ref _npcDialogTextState);

    public long NpcDialogTextStateVersion =>
        Interlocked.Read(ref _npcDialogTextStateVersion);

    public WorldNpcDialogTextEdgeWindow NpcDialogTextEdgeWindow =>
        Volatile.Read(ref _npcDialogTextEdgeWindow);

    public IReadOnlyDictionary<int, WorldEntityState> WorldEntities =>
        Volatile.Read(ref _worldEntities);

    public long WorldEntitiesVersion =>
        Interlocked.Read(ref _worldEntitiesVersion);

    public WorldEntityPublicationState WorldEntityPublicationState =>
        Volatile.Read(ref _worldEntityPublicationState);

    public WorldEntitySceneSnapshot WorldEntitySceneSnapshot =>
        Volatile.Read(ref _worldEntitySceneSnapshot);

    public IReadOnlyDictionary<int, WorldGroundItemState> GroundItems =>
        Volatile.Read(ref _groundItems);

    public long GroundItemsVersion =>
        Interlocked.Read(ref _groundItemsVersion);

    public WorldGroundLootAuthorityState GroundLootAuthorityState =>
        Volatile.Read(ref _groundLootAuthorityState);

    public IReadOnlyList<WorldCombatResultState> CombatResults =>
        Volatile.Read(ref _combatResults);

    public long CombatResultsVersion =>
        Interlocked.Read(ref _combatResultsVersion);

    public IReadOnlyList<WorldCombatNumberState> CombatNumbers =>
        Volatile.Read(ref _combatNumbers);

    public long CombatNumbersVersion =>
        Interlocked.Read(ref _combatNumbersVersion);

    // Receipt observation only; native target rows/time live in the injected host owner.
    public WorldTargetStatusState TargetStatusState =>
        ObserveTargetStatusState();

    public long TargetStatusStateVersion =>
        Interlocked.Read(ref _targetStatusStateVersion);

    public WorldBuffState BuffState => ObserveBuffState();

    public long BuffStateVersion => Interlocked.Read(ref _buffStateVersion);

    public WorldInspectedPlayerEquipmentState InspectedPlayerEquipmentState =>
        Volatile.Read(ref _inspectedPlayerEquipmentState);

    public long InspectedPlayerEquipmentStateVersion =>
        Interlocked.Read(ref _inspectedPlayerEquipmentStateVersion);

    public IReadOnlyDictionary<int, WorldRemotePathState> RemotePaths =>
        Volatile.Read(ref _remotePaths);

    public long RemotePathsVersion =>
        Interlocked.Read(ref _remotePathsVersion);

    public IReadOnlyDictionary<int, WorldRemotePathState> MonsterPaths =>
        Volatile.Read(ref _monsterPaths);

    public long MonsterPathsVersion =>
        Interlocked.Read(ref _monsterPathsVersion);

    public IReadOnlyDictionary<int, WorldRemoteActionState> RemoteActions =>
        Volatile.Read(ref _remoteActions);

    public long RemoteActionsVersion =>
        Interlocked.Read(ref _remoteActionsVersion);

    public WorldSkillCastState SkillCastState =>
        Volatile.Read(ref _skillCastState);

    public IReadOnlyList<WorldSkillCastState> SkillCastEvents =>
        Volatile.Read(ref _skillCastEvents);

    public long SkillCastStateVersion =>
        Interlocked.Read(ref _skillCastStateVersion);

    public IReadOnlyDictionary<int, WorldRemoteChatBubbleState> RemoteChatBubbles =>
        Volatile.Read(ref _remoteChatBubbles);

    public long RemoteChatBubblesVersion =>
        Interlocked.Read(ref _remoteChatBubblesVersion);

    public IReadOnlyList<WorldChannelChatState> ChannelChatRows =>
        Volatile.Read(ref _channelChatRows);

    public long ChannelChatRowsVersion =>
        Interlocked.Read(ref _channelChatRowsVersion);

    public IReadOnlyList<WorldPrivateChatState> PrivateChatRows =>
        Volatile.Read(ref _privateChatRows);

    public long PrivateChatRowsVersion =>
        Interlocked.Read(ref _privateChatRowsVersion);

    public WorldPartyState PartyState =>
        Volatile.Read(ref _partyState);

    public long PartyStateVersion =>
        Interlocked.Read(ref _partyStateVersion);

    public IReadOnlyDictionary<int, WorldStallOwnerState> StallOwners =>
        Volatile.Read(ref _stallOwners);

    public long StallOwnersVersion =>
        Interlocked.Read(ref _stallOwnersVersion);

    public IReadOnlyList<WorldSystemRequestState> SystemRequests =>
        ReadSystemRequests();

    public long SystemRequestsVersion =>
        Interlocked.Read(ref _systemRequestsVersion);

    public WorldInventoryState InventoryState =>
        Volatile.Read(ref _inventoryState);

    public long InventoryStateVersion =>
        Interlocked.Read(ref _inventoryStateVersion);

    public WorldWardrobeState WardrobeState =>
        Volatile.Read(ref _wardrobeState).State;

    public long WardrobeStateVersion =>
        Interlocked.Read(ref _wardrobeStateVersion);

    public WorldHotbarState HotbarState =>
        Volatile.Read(ref _hotbarState);

    public long HotbarStateVersion =>
        Interlocked.Read(ref _hotbarStateVersion);

    public WorldSkillBookState SkillBookState
    {
        get
        {
            lock (_worldStateSync)
                return _skillBookState ?? (_skillBookState = _skillBookObservation.ToSnapshot());
        }
    }

    public long SkillBookStateVersion =>
        Interlocked.Read(ref _skillBookStateVersion);

    public WorldLifeSkillBookState LifeSkillBookState =>
        ReadLifeSkillBookState();

    public long LifeSkillBookStateVersion =>
        Interlocked.Read(ref _lifeSkillBookStateVersion);

    public WorldActiveTradeState ActiveTradeState =>
        ReadActiveTradeState();

    public long ActiveTradeStateVersion =>
        Interlocked.Read(ref _activeTradeStateVersion);

    public WorldTradeEndState TradeEndState =>
        Volatile.Read(ref _tradeEndState);

    public long TradeEndStateVersion =>
        Interlocked.Read(ref _tradeEndStateVersion);

    /// <summary>
    /// Drains the transport on the host thread. The only scheduler is
    /// ClientRootView.Update -> ClientSessionCoordinator.Pump, i.e. one Unity frame-loop call
    /// per frame; Protocol is not a MonoBehaviour, so Unity never invokes NetClient.Update as a
    /// message and there is no second scheduling of this path. The argument-less
    /// NetClient.Update() is the recovered official body; the overload used here only binds the
    /// host capabilities that body reads and then runs it once. NetClient.Update runs DrainTx
    /// and the ping window, which therefore share the host thread with the sends.
    /// </summary>
    // Native _errs messages reach the true OnError title consumer. They are not transport
    // drop notifications; _dropped and the explicit packet/transport fault paths retain their
    // own cleanup. The product-only per-call error sink is therefore not supplied here.
    public void Pump()
    {
        if (_hostShutdown) return;
        SceneReceiverAtReceipt(null)?.ApplyPublishedWorldEvents();
        _netClient.Update((opcode, payload) =>
        {
            HoldInboxAfterEntrySelfData(opcode);
            Dispatch(opcode, payload);
            SceneReceiverAtReceipt(null)?.ApplyPublishedWorldEvents();
        }, null, _dispatchMillisecondsProvider, MayDequeueAfterEntry);
    }

    public void Pump(IWorldReceiveContext context)
    {
        if (context == null) throw new ArgumentNullException(nameof(context));
        if (_hostShutdown) return;
        BindSkillBookReceiveOwner(context as ISkillBookReceiveOwner);
        BindPetVitalsReceiveOwner(context as IWorldPetVitalsReceiveOwner);
        SceneReceiverAtReceipt(context)?.ApplyPublishedWorldEvents();
        _netClient.Update((opcode, payload) =>
        {
            HoldInboxAfterEntrySelfData(opcode);
            DispatchReceivedPacket(context, opcode, payload);
            SceneReceiverAtReceipt(context)?.ApplyPublishedWorldEvents();
        }, null, context is IWorldReceiveBudgetContext budgetContext
            ? () => budgetContext.DispatchMillisecondsPerFrame
            : _dispatchMillisecondsProvider, MayDequeueAfterEntry);
    }

    private void DispatchReceivedPacket(IWorldReceiveContext context, ushort opcode, byte[] payload)
    {
        if (TryReceiveCombatEntityTailPacket(opcode, payload)) return;
        if (TryReceiveCombatRegistryPacket(opcode, payload, context)) return;
        if (TryReceiveRemotePlayerTailPacket(opcode, payload, context)) return;
        if (TryReceiveCombatBubblePacket(opcode, payload, context)) return;
        if (TryReceiveWardrobePacket(opcode, payload,
                context as IWardrobeReceiveOwner ?? _worldReceiveOwner as IWardrobeReceiveOwner))
            return;
        if (TryReceiveCharacterListPacket(opcode, payload)) return;
        if (TryReceiveTitleResultPacket(opcode, payload)) return;
        if (TryReceiveAccountResult(opcode, payload)) return;
        if (TryReceiveInspectedEquipmentPacket(opcode, payload,
                context as IInspectedEquipmentReceiptOwner ?? _worldReceiveOwner as IInspectedEquipmentReceiptOwner))
            return;
        if (TryReceiveNetworkNpcPacket(opcode, payload)) return;
        if (TryReceiveTargetStatusPacket(opcode, payload,
                context as ITargetStatusReceiveOwner ?? _worldReceiveOwner as ITargetStatusReceiveOwner))
            return;
        if (TryReceiveSkillCastPacket(opcode, payload, SkillCastReceiverAtReceipt(context)))
            return;
        if (opcode == ProtocolOpcodes.DuelHud)
        {
            ReceiveDuelHud(payload, context as IDuelHudReceiveOwner);
            return;
        }
        if (opcode == ProtocolOpcodes.DuelList)
        {
            ReceiveDuelList(payload, context as IDuelListReceiveOwner);
            return;
        }
        if (TryReceiveFamilyPacket(opcode, payload,
                context as IFamilyReceiptOwner ?? _worldReceiveOwner as IFamilyReceiptOwner))
            return;
        if (TryReceiveMailPacket(opcode, payload,
                context as IMailReceiveOwner ?? _worldReceiveOwner as IMailReceiveOwner))
            return;
        if (TryReceiveRackShopPacket(opcode, payload,
                context as IRackShopReceiveOwner ?? _worldReceiveOwner as IRackShopReceiveOwner))
            return;
        if (TryReceiveHotbarPacket(opcode, payload,
                context as IHotbarReceiveOwner ?? _worldReceiveOwner as IHotbarReceiveOwner))
            return;
        if (TryReceiveNpcNoticePacket(opcode, payload,
                context as INpcNoticeReceiveOwner ?? _worldReceiveOwner as INpcNoticeReceiveOwner))
            return;
        if (opcode == ProtocolOpcodes.PrivateChat || opcode == ProtocolOpcodes.SystemRequests)
        {
            ReceiveSocialNotice(opcode, payload, context as ISocialNoticeReceiveOwner);
            return;
        }
        if (TryReceiveTradePacket(opcode, payload,
                context as ITradeReceiveOwner ?? _worldReceiveOwner as ITradeReceiveOwner))
            return;

        if (opcode == 0x8001)
        {
            // Consumed here and nowhere else. The 0x8001 case has no stage, world or map gate,
            // so the reply arrives in every stage the ping window of NetClient.Update runs in,
            // and returning stops the fall-through `else Dispatch(opcode, payload)` at the end of
            // this method from queueing it for a session-startup await as if it were a response.
            ReceiveRoundTripLatency(payload);
            return;
        }
        if (opcode == ProtocolOpcodes.OnlineCount)
        {
            // Same shape as the 0x8001 arm above: the count is a dispatch case, not the reply of
            // an await, so it is consumed here and the fall-through `else Dispatch(...)` at the
            // end of this method never queues it for a session-startup await. Keeping both a
            // dispatch arm and an await would read the same packet twice.
            ReceiveOnlineCount(payload);
            return;
        }
        if (opcode == ProtocolOpcodes.RenameCharacterResult && context is IRenameCharacterReceiveContext renameContext)
        {
            var reader = new NetClient.Buf(payload);
            int code = reader.I32();
            string message = reader.Str();
            string name = reader.Str();
            if (code == 0)
            {
                lock (_worldStateSync)
                    PublishPlayerRenameLocked(new WorldPlayerRenameState(_currentWorldEntityId, name),
                        _currentWorldEntityId);
            }
            renameContext.ReceiveRenameCharacterResult(code, message, name);
            return;
        }
        if (opcode == 0x8074 && context is IHornDialogReceiveContext hornContext)
        {
            var reader = new NetClient.Buf(payload);
            int itemId = reader.I32();
            int tier = reader.U8();
            int skin = reader.U8();
            hornContext.OpenHorn(itemId, tier, skin);
            return;
        }
        if (opcode == 0x803f && context is IPlayerHairReceiveContext hairContext)
        {
            ReceivePlayerHair(payload, hairContext);
            return;
        }
        if (context is IFurnaceReceiveContext furnaceContext)
        {
            if (opcode == 0x8020) { ReceiveFurnaceRecipes(payload, furnaceContext); return; }
            if (opcode == 0x8021) { ReceiveRefineInfo(payload, furnaceContext); return; }
            if (opcode == 0x8024) { ReceiveSocketInfo(payload, furnaceContext); return; }
        }
        if (opcode == ProtocolOpcodes.ChannelChat && context is IChannelChatReceiveContext channelContext)
        {
            ReceiveChannelChat(payload, channelContext);
            return;
        }
        if (opcode == ProtocolOpcodes.RemotePkState && context is IRemotePkStateReceiveContext remotePkContext)
        {
            // Official 0x8053 decides self versus remote inside the case, before any publish:
            // an id equal to NetClient.SelfEntityId stores the local byte and never reaches
            // CombatWorld.SetPlayerPkState. Only the remote arm re-enters the publish path.
            PrivateServerWorldStateDecoder.DecodeRemotePkState(
                    payload,
                    out int remotePkEntityId,
                    out byte remotePkState);

            if (!remotePkContext.ReceiveRemotePkState(remotePkEntityId, remotePkState))
            {
                DispatchPacket(new ProtocolPacket(opcode, payload));
            }
            return;
        }
        if (opcode == ProtocolOpcodes.SelfPkState)
        {
            ReceiveSelfPkPacket(payload, context as ISelfPkStateReceiveContext);
            return;
        }
        if (context is IPlayerLifeReceiveContext lifeContext)
        {
            if (opcode == ProtocolOpcodes.PlayerLifeState)
            {
                PrivateServerWorldStateDecoder.DecodePlayerLifeState(payload,
                        out int entityId, out bool alive, out int flags);
                PlayerLifeReceiveResult received = lifeContext.ReceivePlayerLife(entityId, alive, (byte)flags);
                if (received.Local)
                {
                    if (received.Applied)
                        RecordAppliedPlayerLife(received.EntityId, alive, (byte)flags);
                    return;
                }
                DispatchPacket(new ProtocolPacket(opcode, payload, remoteLifeAtReceive: true));
                return;
            }
        }
        if (context is IHistoryReceiveContext historyContext)
        {
            if (opcode == 0x8051)
            {
                ReceiveGmFeedbacks(payload, historyContext);
                return;
            }
            if (opcode == 0x8067)
            {
                ReceiveVoteOptions(payload, historyContext);
                return;
            }
        }
        if (context is IGroundItemReceiveContext groundContext)
        {
            if (opcode == ProtocolOpcodes.GroundItemSpawn)
            {
                ReceiveGroundSpawn(payload, groundContext);
                return;
            }
            if (opcode == ProtocolOpcodes.GroundItemRemove)
            {
                ReceiveGroundRemoval(payload, groundContext);
                return;
            }
        }
        if (context is IStallOwnerReceiveContext stallContext)
        {
            if (opcode == ProtocolOpcodes.StallOwnerUpdate)
            {
                // Publish the immutable gateway owner map before notifying the presentation
                // callback. The context path otherwise bypasses DispatchPacket entirely.
                DispatchPacket(new ProtocolPacket(opcode, payload));
                var reader = new NetClient.Buf(payload);
                int ownerId = reader.I32();
                reader.Str();
                byte type = (byte)reader.U8();
                string name = reader.Str();
                reader.I32();
                reader.I32();
                stallContext.ReceiveStallOwner(ownerId, type, name);
                return;
            }
            else if (opcode == 0x803E)
            {
                DispatchPacket(new ProtocolPacket(opcode, payload));
                stallContext.ReceiveStallClosed(new NetClient.Buf(payload).I32());
                return;
            }
        }
        if (context is IAccountSecurityReceiveContext securityContext)
        {
            if (opcode == 0x8065)
            {
                var reader = new NetClient.Buf(payload);
                bool hasCode = reader.U8() != 0;
                int windowMilliseconds = reader.I32();
                securityContext.SeedAccountSecurity(hasCode, windowMilliseconds);
                return;
            }
            if (opcode == 0x8066)
            {
                var reader = new NetClient.Buf(payload);
                int kind = reader.U8();
                int code = reader.U8();
                string message = reader.Str();
                int tab = reader.U8();
                int slot = reader.I32();
                int itemId = reader.I32();
                securityContext.ReceiveAccountSecurityResult(kind, code, message, tab, slot, itemId);
                return;
            }
        }
        if (opcode == 0x803C && context is IStallContentsReceiveContext stallContentsContext)
        {
            ReceiveStallContents(payload, stallContentsContext);
            return;
        }
        if (opcode == 0x801C && context is IShopItemsReceiveContext shopItemsContext)
        {
            ReceiveShopItems(payload, shopItemsContext);
            return;
        }
        if (opcode == 0x801D && context is IRepairReceiveContext repairContext)
        {
            ReceiveRepairQuote(payload, repairContext);
            return;
        }
        if (opcode == 0x804D && context is IItemIconOverrideReceiveContext iconOverrideContext)
        {
            ReceiveIconOverrides(payload, iconOverrideContext);
            return;
        }
        if (opcode == 0x804E && context is IAttributeWashReceiveContext attributeWashContext)
        {
            ReceiveAttributeWash(payload, attributeWashContext);
            return;
        }
        if (opcode == 0x8070 && context is IEquipmentFusionReceiveContext equipmentFusionContext)
        {
            ReceiveEquipmentFusion(payload, equipmentFusionContext);
            return;
        }
        if (opcode == 0x806D && context is IPetForgeReceiveContext forgeContext)
        {
            ReceivePetForge(payload, forgeContext);
            return;
        }
        if (opcode == ProtocolOpcodes.ZoneOverlayState && context is IZoneOverlayReceiveContext zoneContext)
        {
            ReceiveZoneOverlay(payload, zoneContext);
            return;
        }
        if (opcode == ProtocolOpcodes.ArenaDraft && context is IArenaDraftReceiveContext arenaDraftContext)
        {
            ReceiveArenaDraft(payload, arenaDraftContext);
            return;
        }
        if (opcode == ProtocolOpcodes.ArenaRoster && context is IArenaRosterReceiveContext arenaRosterContext)
        {
            ReceiveArenaRoster(payload, arenaRosterContext);
            return;
        }
        if (opcode == ProtocolOpcodes.ArenaDeathState && context is IArenaDeathReceiveContext arenaDeathContext)
        {
            ReceiveArenaDeath(payload, arenaDeathContext);
            return;
        }
        if (context is INewbieTipReceiveContext tipContext)
        {
            if (opcode == 0x8054)
            {
                var reader = new WireReader(payload);
                int count = reader.ReadUInt16();
                var ids = new List<int>(count);
                for (int i = 0; i < count; i++) ids.Add(reader.ReadInt32());
                tipContext.SeedNewbieTips(ids);
                return;
            }
            else if (opcode == ProtocolOpcodes.SkillSnapshot)
            {
                WorldSkillBookObservation skills = ReceiveSkillBookObservation(payload);
                if (!skills.HasValue)
                    throw new InvalidDataException("The server sent an invalid skill-book packet.");
                if (tipContext is INewbieSkillBookObservationContext observationContext)
                    observationContext.ReceiveNewbieSkillBook(skills);
                else
                    tipContext.ReceiveNewbieSkillBook(skills.ToSnapshot());
                DispatchPacket(new ProtocolPacket(opcode, payload,
                    receivedSkillBookObservation: skills, skillBookTipEvaluated: true));
                return;
            }
            else if (opcode == ProtocolOpcodes.PetSnapshot)
            {
                var petOwner = context as IWorldPetSnapshotReceiveOwner;
                WorldPlayerPetStatusState pet = petOwner != null
                    ? ReceivePetSnapshot(payload, petOwner, true)
                    : ReceivePetSnapshotVitals(payload);
                if (pet == null)
                    throw new InvalidDataException("The server sent an invalid player pet packet.");
                if (petOwner == null) tipContext.ReceiveNewbiePetSkills(pet);
                DispatchPacket(new ProtocolPacket(opcode, payload, petStatus: pet,
                    receivedPetStatus: pet));
                return;
            }
        }
        if (opcode == ProtocolOpcodes.EntitySpawn)
        {
            WorldEntityState spawn = PrivateServerWorldStateDecoder.DecodeWorldEntityState(payload, context);
            if (spawn == null) return;
            DispatchPacket(new ProtocolPacket(opcode, payload, spawn));
        }
        else Dispatch(opcode, payload);
    }

    // Account operations call this on the existing UI/Pump thread. Task-based login reaches
    // the same member via _host. The native owner alone decides connected/pending/not-ready
    // and creates its worker; this adapter publishes only the host lifetime/stage boundary.
    private bool BeginNativeConnection(Action<bool> onDone, CancellationToken callerToken,
        out CancellationTokenSource connection, out CancellationToken connectionToken)
    {
        callerToken.ThrowIfCancellationRequested();
        if (_hostShutdown) throw new ObjectDisposedException(nameof(PrivateServerGateway));
        connection = null;
        connectionToken = CancellationToken.None;

        // A native Close does not cancel/clear _connectCb. After host cancellation/Dispose,
        // do not adopt a socket the retired worker can publish before Update drains its result.
        // This exceptional host-lifetime guard is NOT the ordinary repeated-click latch: an
        // active native attempt, including its connected-before-callback window, still goes
        // through ConnectAsync unchanged. No request is queued for later replay.
        if (_pendingNativeConnect != null &&
            _netClient.OwnsConnectCallbackForIndependentRuntime(_pendingNativeConnect) &&
            (!ReferenceEquals(_pendingNativeConnectLifetime, _connectionLifetime) ||
             _pendingNativeConnectCallerToken.IsCancellationRequested))
            return false;

        connection = _connectionLifetime;
        if (connection == null)
        {
            connection = new CancellationTokenSource();
            _connectionLifetime = connection;
        }
        connectionToken = connection.Token;
        CancellationTokenSource identity = connection;
        CancellationToken identityToken = connectionToken;
        bool invoked = false;
        Action<bool> callback = null;
        callback = ok =>
        {
            invoked = true;
            if (ReferenceEquals(_pendingNativeConnect, callback))
            {
                _pendingNativeConnect = null;
                _pendingNativeConnectLifetime = null;
                _pendingNativeConnectCallerToken = CancellationToken.None;
            }

            // No stale callback may publish Connected or run an account/login continuation.
            // The admission guard above prevented replacement while this native callback was
            // retained, so a late published transport here belongs only to the retired work.
            if (!ReferenceEquals(_connectionLifetime, identity) || identityToken.IsCancellationRequested ||
                callerToken.IsCancellationRequested)
            {
                if (ReferenceEquals(_connectionLifetime, identity))
                    DisposeConnectionCore();
                else
                {
                    _netClient.Close();
                    _netClient.CloseIndependentRuntime();
                }
                return;
            }

            if (ok && _stage == GatewayStage.Disconnected)
                _stage = GatewayStage.Connected;
            onDone(ok);
        };

        _netClient.ConnectAsync(callback);
        bool retained = _netClient.OwnsConnectCallbackForIndependentRuntime(callback);
        if (retained)
        {
            _pendingNativeConnect = callback;
            _pendingNativeConnectLifetime = identity;
            _pendingNativeConnectCallerToken = callerToken;
        }
        return invoked || retained;
    }

    public async Task<Result<CancellationToken>> ConnectAsync(
        ServerEndpoint endpoint,
        CancellationToken cancellationToken)
    {
        if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));

        // The resolver and the explicit server-switch port already own host/port. This
        // endpoint is the existing login API's directory reference, not a second producer.
        // No leading Dispose, Task.Run, timeout worker or alternate EstablishConnection path.
        var completion = new TaskCompletionSource<Result<CancellationToken>>(TaskCreationOptions.RunContinuationsAsynchronously);
        CancellationTokenSource connection = null;
        CancellationToken connectionToken = CancellationToken.None;
        bool admitted = false;
        await _host.RunAsync(() =>
        {
            admitted = BeginNativeConnection(ok =>
            {
                // This is an explicit login admission, unlike the shared account callback.
                // A queued retry may reuse an already-authenticated native socket. Re-enter
                // the host's pre-auth stage only after that live connect succeeds, so the
                // unchanged AuthenticateAsync guard admits its real Login request and reply.
                if (ok) _stage = GatewayStage.Connected;
                completion.TrySetResult(ok
                    ? Result<CancellationToken>.Success(connectionToken)
                    : Result<CancellationToken>.Failure("transport.connect.failure",
                        "The game server connection could not be established."));
            }, cancellationToken, out connection, out connectionToken);
            if (!admitted)
                completion.TrySetResult(Result<CancellationToken>.Failure(
                    ClientSessionCoordinator.NativeConnectCallbackDroppedCode,
                    "The native connection owner did not retain this callback."));
        }, cancellationToken).ConfigureAwait(false);

        // Cancellation belongs to the host Task API, not native ConnectAsync. It releases the
        // waiting coordinator without fabricating a callback; only its still-current lifetime
        // is disposed. A suppressed login cannot cancel the account attempt that won the latch.
        using (cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken)))
        using (connectionToken.Register(() => completion.TrySetCanceled(connectionToken)))
        {
            try
            {
                return await completion.Task.ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                if (admitted && cancellationToken.IsCancellationRequested)
                    await _host.RunAsync(() =>
                    {
                        if (ReferenceEquals(_connectionLifetime, connection))
                            DisposeConnectionCore();
                    }, CancellationToken.None).ConfigureAwait(false);
                throw;
            }
        }
    }

    public async Task<Result> AuthenticateAsync(
        LoginCredentials credentials,
        CancellationToken cancellationToken)
    {
        if (credentials == null)
        {
            throw new ArgumentNullException(nameof(credentials));
        }

        if (_stage != GatewayStage.Connected)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The game connection is not ready for authentication.");
        }

        Interlocked.Increment(ref _authenticationWaiters);
        try
        {
            // Login is a direct NetClient write outside _sendOperation; only its thread
            // ownership moves to the host, it is not serialized behind the send semaphore.
            await _host.RunAsync(
                    () =>
                    {
                        // A later cancelled Task reply keeps its original Task observation
                        // path; absence of an active waiter alone never grants native UI ownership.
                        _titleLoginResultWithoutWaiter = false;
                        endgameAccount = credentials.AccountName;
                        _netClient.Login(credentials.AccountName, credentials.Password);
                    },
                    cancellationToken)
                .ConfigureAwait(false);

            ProtocolPacket packet = await ReadUntilAsync(
                ProtocolOpcodes.LoginResult,
                cancellationToken);
            WireReader reader = new WireReader(packet.Payload);
            int resultCode = reader.ReadInt32();
            string message = reader.ReadString();
            reader.EnsureFinished();

            if (resultCode != 0)
            {
                return Result.Failure(
                    "auth.rejected",
                    string.IsNullOrWhiteSpace(message)
                        ? "The server rejected the login."
                        : message);
            }

            // Connect's existing lifetime is linked by LoginAsync. Commit the host stage on
            // that same host, with the linked token's start check, so an old authentication
            // reply cannot authenticate a replacement connection after Dispose/drop/cancel.
            await _host.RunAsync(() => _stage = GatewayStage.Authenticated, cancellationToken)
                .ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await _host.RunAsync(DisposeConnectionCore, cancellationToken).ConfigureAwait(false);
            return Result.Failure(
                "auth.response.timeout",
                "The authentication response timed out.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            await _host.RunAsync(DisposeConnectionCore, cancellationToken).ConfigureAwait(false);
            return Result.Failure(
                "auth.response.invalid",
                "The authentication response was missing or invalid.");
        }
        finally
        {
            Interlocked.Decrement(ref _authenticationWaiters);
        }
    }

    public async Task<Result<IReadOnlyList<ServerCharacterRecord>>> GetCharacterRosterAsync(
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.Authenticated)
        {
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "session.stage.invalid",
                "The session is not authenticated.");
        }

        try
        {
            ProtocolPacket packet = await ReadUntilAsync(
                ProtocolOpcodes.CharacterList,
                cancellationToken);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Success(ReadReceivedCharacterRoster(packet));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await _host.RunAsync(DisposeConnectionCore, cancellationToken).ConfigureAwait(false);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "roster.response.timeout",
                "The character-list response timed out.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            await _host.RunAsync(DisposeConnectionCore, cancellationToken).ConfigureAwait(false);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "roster.response.invalid",
                "The character list was missing or invalid.");
        }
    }

    public async Task<Result<IReadOnlyList<ServerCharacterRecord>>> DeleteCharacterAsync(
        CharacterId characterId,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.Authenticated)
        {
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "session.stage.invalid",
                "The session is not ready to delete a character.");
        }

        try
        {
            // Official 1.5.8 NetClient.DeleteChar (0x060006c5) owns this frame: it allocates its own
            // display class and writes a single Buf.Str, so the request now leaves through the
            // recovered writer instead of a hand-built WireWriter payload. WireWriter.WriteString
            // threw on null where official Buf.Str substitutes the empty string.
            await SendAsync(
                () => _netClient.DeleteChar(characterId.Value),
                cancellationToken);

            ProtocolPacket resultPacket = await ReadUntilAsync(
                ProtocolOpcodes.CharacterMutationResult,
                cancellationToken);
            WireReader resultReader = new WireReader(resultPacket.Payload);
            int resultCode = resultReader.ReadInt32();
            string message = resultReader.ReadString();
            resultReader.EnsureFinished();
            if (resultCode != 0)
            {
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "character.delete.rejected",
                    string.IsNullOrWhiteSpace(message)
                        ? "The server rejected character deletion."
                        : message);
            }

            ProtocolPacket rosterPacket = await ReadUntilAsync(
                ProtocolOpcodes.CharacterList,
                cancellationToken);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Success(
                ReadReceivedCharacterRoster(rosterPacket));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "character.delete.response.timeout",
                "The character-deletion response timed out.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "character.delete.response.invalid",
                "The character-deletion response was missing or invalid.");
        }
    }

    public async Task<Result<IReadOnlyList<ServerCharacterRecord>>> CreateCharacterAsync(
        CharacterCreationRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_stage != GatewayStage.Authenticated)
        {
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "session.stage.invalid",
                "The session is not ready to create a character.");
        }

        try
        {
            // Official 1.5.8 NetClient.CreateChar (0x060006c3) owns this frame: it allocates its own
            // display class and writes Str + four U8 through Buf, so the request now leaves through
            // the recovered writer instead of a hand-built WireWriter payload. WireWriter.WriteString
            // threw on null and on UTF-8 sequences Encoding.UTF8 would replace, and the checked byte
            // casts threw where official Buf.U8 truncates; none of those three behaviours is official.
            await SendAsync(
                () => _netClient.CreateChar(
                    request.DisplayName,
                    request.Race,
                    request.Gender,
                    request.Head,
                    request.Hair),
                cancellationToken);

            ProtocolPacket resultPacket = await ReadUntilAsync(
                ProtocolOpcodes.CharacterMutationResult,
                cancellationToken);
            WireReader resultReader = new WireReader(resultPacket.Payload);
            int resultCode = resultReader.ReadInt32();
            string message = resultReader.ReadString();
            resultReader.EnsureFinished();
            if (resultCode != 0)
            {
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "character.create.rejected",
                    string.IsNullOrWhiteSpace(message)
                        ? "The server rejected character creation."
                        : message);
            }

            ProtocolPacket rosterPacket = await ReadUntilAsync(
                ProtocolOpcodes.CharacterList,
                cancellationToken);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Success(
                ReadReceivedCharacterRoster(rosterPacket));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "character.create.response.timeout",
                "The character-creation response timed out.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "character.create.response.invalid",
                "The character-creation response was missing or invalid.");
        }
    }

    // The existing title caller already runs on the host. Preserve its void call to native
    // SrvStatReq/Send, including native pacing, OnError and exception behavior. This route has
    // no operation/send wait, stage admission, cancellation token or manufactured Result.
    // OnlineCount still arrives only through the independent 0x805b receive owner.
    public void RequestServerOnlineCount() => _netClient.SrvStatReq();

    // Boundary marker (WBB-104): official 0x0600041d only null-checks NetClient.I before calling
    // 0x060006f8, so this hop is fire-and-forget - SendAsync runs the local send and never waits
    // for a reply. The scheduled compatibility path retains its product-only stage and
    // timeout/failure Results; the native host call reaches Send without a world-stage gate.
    // The entityId <= 0 drop that once sat here had no native counterpart and is gone.
    public Task<Result> QueryTargetStatusAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.QueryTargetStatus(entityId);
            return Task.FromResult(Result.Success());
        }

        return QueryTargetStatusScheduledAsync(entityId, cancellationToken);
    }

    private async Task<Result> QueryTargetStatusScheduledAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.QueryTargetStatus(entityId),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "target.status.send.timeout",
                "The target-status request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "target.status.send.failure",
                "The target-status request could not be sent.");
        }
    }

    public Task<Result> RequestNpcTaskOffersAsync(
        string npcName,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TaskNpcList(npcName);
            return Task.FromResult(Result.Success());
        }

        return RequestNpcTaskOffersScheduledAsync(npcName, cancellationToken);
    }

    private async Task<Result> RequestNpcTaskOffersScheduledAsync(
        string npcName,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(() => _netClient.TaskNpcList(npcName), cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "npc.tasks.send.timeout",
                "The NPC task request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "npc.tasks.send.failure",
                "The NPC task request could not be sent.");
        }
    }

    public Task<Result> OpenShopAsync(int shopId, CancellationToken cancellationToken)
    {
        if (shopId <= 0)
        {
            return Task.FromResult(Result.Failure("shop.open.invalid", "The shop id is invalid."));
        }

        return SendNpcFunctionAsync(
            ProtocolOpcodes.OpenShopRequest,
            writer => writer.WriteInt32(shopId),
            "shop.open",
            cancellationToken);
    }

    public Task<Result> BuyShopItemAsync(
        int itemId,
        int count,
        CancellationToken cancellationToken)
    {
        if (itemId <= 0 || count <= 0)
        {
            return Task.FromResult(Result.Failure("shop.buy.invalid", "The shop purchase is invalid."));
        }

        return SendNpcFunctionAsync(
            ProtocolOpcodes.BuyShopItemRequest,
            writer =>
            {
                writer.WriteInt32(itemId);
                writer.WriteInt32(count);
            },
            "shop.buy",
            cancellationToken);
    }

    public Task<Result> SellInventoryItemAsync(
        byte tab,
        int bagIndex,
        int count,
        int itemId,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        if (tab > 3 || bagIndex < 0 || count <= 0 || itemId <= 0)
        {
            return Task.FromResult(Result.Failure("shop.sell.invalid", "The shop sale is invalid."));
        }

        return SendNpcFunctionAsync(
            ProtocolOpcodes.SellInventoryItemRequest,
            writer =>
            {
                writer.WriteByte(tab);
                writer.WriteInt32(bagIndex);
                writer.WriteInt32(count);
                writer.WriteInt32(itemId);
                writer.WriteByte(confirmed ? (byte)1 : (byte)0);
            },
            "shop.sell",
            cancellationToken);
    }

    public Task<Result> OpenTransportAsync(
        int transportList,
        CancellationToken cancellationToken)
    {
        // The native DialogUI callback reaches the original sender synchronously. Its only
        // list predicate was at row registration; do not revalidate the live activation value.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.OpenTeleport(transportList);
            return Task.FromResult(Result.Success());
        }

        if (transportList <= 0)
        {
            return Task.FromResult(Result.Failure("transport.open.invalid", "The transport list is invalid."));
        }

        return SendNpcFunctionAsync(
            ProtocolOpcodes.OpenTransportRequest,
            writer => writer.WriteInt32(transportList),
            "transport.open",
            cancellationToken);
    }

    // Official NetClient.Teleport (0x060006e9): Send(0x1072) with the writer 0x060007c1, two
    // Buf.I32 - the transport list and the index of the row the player picked - in that order.
    // The window's own gates are "a row is selected and it is inside the received list"
    // (TransportUI.DoTeleport, 0x06000f96); the native command carries no argument check at all,
    // so no range or identity validation is added here. The index identifies a row of the list the
    // server sent for this NPC, never a map or a coordinate, and the map switch the client finally
    // performs still comes from the server's own 0x803a teleport downlink.
    public Task<Result> ChooseTransportAsync(
        int transportList,
        int destinationIndex,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Teleport(transportList, destinationIndex);
            return Task.FromResult(Result.Success());
        }

        return ChooseTransportScheduledAsync(transportList, destinationIndex, cancellationToken);
    }

    private async Task<Result> ChooseTransportScheduledAsync(
        int transportList,
        int destinationIndex,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.Teleport(transportList, destinationIndex),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "transport.choose.send.timeout",
                "The transport request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "transport.choose.send.failure",
                "The transport request could not be sent.");
        }
    }

    public Task<Result> OpenWarehouseAsync(CancellationToken cancellationToken)
    {
        return SendNpcFunctionAsync(
            ProtocolOpcodes.OpenWarehouseRequest,
            null,
            "warehouse.open",
            cancellationToken);
    }

    public Task<Result> MoveWarehouseItemAsync(
        byte direction,
        byte fromTab,
        int fromSlot,
        byte warehouseTab,
        int count,
        int toSlot,
        CancellationToken cancellationToken)
    {
        if (direction > 1 || fromTab > 3 || fromSlot < 0 || count <= 0 || toSlot < -1)
        {
            return Task.FromResult(Result.Failure("warehouse.move.invalid", "The warehouse move is invalid."));
        }

        return SendNpcFunctionAsync(
            ProtocolOpcodes.MoveWarehouseItemRequest,
            writer =>
            {
                writer.WriteByte(direction);
                writer.WriteByte(fromTab);
                writer.WriteInt32(fromSlot);
                writer.WriteByte(warehouseTab);
                writer.WriteInt32(count);
                writer.WriteInt32(toSlot);
            },
            "warehouse.move",
            cancellationToken);
    }

    private async Task<Result> SendNpcFunctionAsync(
        ushort opcode,
        Action<WireWriter> write,
        string errorPrefix,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure("session.stage.invalid", "The session is not in the world.");
        }

        try
        {
            WireWriter payload = new WireWriter();
            write?.Invoke(payload);
            await SendAsync(opcode, payload.ToArray(), cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(errorPrefix + ".send.timeout", "The NPC function request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(errorPrefix + ".send.failure", "The NPC function request could not be sent.");
        }
    }

    public Task<Result> AcceptTaskAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TaskAccept(taskId);
            return Task.FromResult(Result.Success());
        }

        return SendTaskCommandAsync(
            () => _netClient.TaskAccept(taskId),
            "task.accept",
            cancellationToken);
    }

    public Task<Result> AbandonTaskAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TaskAbandon(taskId);
            return Task.FromResult(Result.Success());
        }

        return SendTaskCommandAsync(
            () => _netClient.TaskAbandon(taskId),
            "task.abandon",
            cancellationToken);
    }

    public Task<Result> CompleteTaskAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TaskComplete(taskId);
            return Task.FromResult(Result.Success());
        }

        return SendTaskCommandAsync(
            () => _netClient.TaskComplete(taskId),
            "task.complete",
            cancellationToken);
    }

    private async Task<Result> SendTaskCommandAsync(
        Action send,
        string errorPrefix,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(send, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                errorPrefix + ".send.timeout",
                "The task command timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                errorPrefix + ".send.failure",
                "The task command could not be sent.");
        }
    }

    public Task<Result> InspectPlayerAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        // Keep the live menu's scalar writer and native return/throw on this call stack.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.InspectPlayer(entityId);
            return Task.FromResult(Result.Success());
        }

        return InspectPlayerCoreAsync(entityId, cancellationToken);
    }

    private async Task<Result> InspectPlayerCoreAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            WireWriter payload = new WireWriter();
            payload.WriteInt32(entityId);
            await SendAsync(
                ProtocolOpcodes.InspectPlayerRequest,
                payload.ToArray(),
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "player.inspect.send.timeout",
                "The inspect-player request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "player.inspect.send.failure",
                "The inspect-player request could not be sent.");
        }
    }

    public Task<Result> RequestTradeAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        // PlayerActionMenu.Fire returns only after the native send returns.
        // Keep its scalar callback and exceptions on the live host call stack.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TradeRequest(entityId);
            return Task.FromResult(Result.Success());
        }

        return RequestTradeCoreAsync(entityId, cancellationToken);
    }

    private async Task<Result> RequestTradeCoreAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            WireWriter payload = new WireWriter();
            payload.WriteInt32(entityId);
            await SendAsync(
                ProtocolOpcodes.TradeRequest,
                payload.ToArray(),
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "trade.request.send.timeout",
                "The trade invitation timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "trade.request.send.failure",
                "The trade invitation could not be sent.");
        }
    }

    public Task<Result> RespondSystemRequestAsync(
        int requestId,
        bool accept,
        CancellationToken cancellationToken) =>
        RespondSystemRequestAsync(requestId, accept ? (byte)1 : (byte)0, cancellationToken);

    public Task<Result> RespondSystemRequestAsync(
        int requestId,
        byte accept,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            // Notices can be answered before world entry. Native Send owns the connection
            // check and errors; the confirmation callback returns after it, including throws.
            _netClient.SysMsgRespond(requestId, accept);
            return Task.FromResult(Result.Success());
        }

        return RespondSystemRequestCoreAsync(requestId, accept, cancellationToken);
    }

    private async Task<Result> RespondSystemRequestCoreAsync(
        int requestId,
        byte accept,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerSystemRequestCodec.TryEncodeResponse(
                requestId,
                accept,
                out payload))
        {
            return Result.Failure(
                "system.request.response.invalid",
                "The system-request id is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.SystemRequestResponse,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "system.request.response.timeout",
                "The system-request response timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "system.request.response.failure",
                "The system-request response could not be sent.");
        }
    }

    public async Task<Result> UseInventoryItemAsync(
        int itemId,
        byte tab,
        int bagIndex,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerInventoryCodec.TryEncodeUse(
                itemId,
                tab,
                bagIndex,
                out payload))
        {
            return Result.Failure(
                "inventory.use.identity.invalid",
                "The inventory item identity is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.UseInventoryItemRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "inventory.use.timeout",
                "The inventory item-use request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "inventory.use.failure",
                "The inventory item-use request could not be sent.");
        }
    }

    public async Task<Result> SaveHotbarAsync(
        WorldHotbarState state,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerHotbarCodec.TryEncodeSave(state, out payload))
        {
            return Result.Failure(
                "hotbar.save.invalid",
                "The hotbar state is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.SaveHotbarRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "hotbar.save.timeout",
                "The hotbar save request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "hotbar.save.failure",
                "The hotbar save request could not be sent.");
        }
    }

    public Task<Result> AttackAsync(
        int targetEntityId,
        CancellationToken cancellationToken)
    {
        // Native combat continues with the swing/projectile only after Attack returns.
        // Keep the admitted host call and its exceptions on that same stack.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Attack(targetEntityId);
            return Task.FromResult(Result.Success());
        }

        return AttackCoreAsync(targetEntityId, cancellationToken);
    }

    private async Task<Result> AttackCoreAsync(
        int targetEntityId,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerCombatCodec.TryEncodeAttack(targetEntityId, out payload))
        {
            return Result.Failure(
                "combat.attack.target.invalid",
                "The normal-attack target identity is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.AttackRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "combat.attack.timeout",
                "The normal-attack request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "combat.attack.failure",
                "The normal-attack request could not be sent.");
        }
    }

    public async Task<Result> PickUpGroundItemAsync(
        int groundId,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerGroundItemCodec.TryEncodePickup(groundId, out payload))
        {
            return Result.Failure(
                "ground-item.pickup.target.invalid",
                "The ground-item identity is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.PickGroundItemRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "ground-item.pickup.timeout",
                "The ground-item pickup request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "ground-item.pickup.failure",
                "The ground-item pickup request could not be sent.");
        }
    }

    public async Task<Result> ReportPlayerMoveAsync(
        int x,
        int y,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure("session.stage.invalid", "The session is not in the world.");
        }
        try
        {
            // Native Send synchronously enters its ordered transmit queue. Keep this outside
            // _sendOperation so a skill's preceding Flush cannot queue behind that skill:
            // only the thread ownership changes, the position report is not serialized into
            // another semaphore.
            await _host.RunAsync(
                    () => _netClient.PlayerMove(x, y),
                    cancellationToken)
                .ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure("player.move.timeout", "The movement report timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "player.move.failure", "The movement report could not be sent.");
        }
    }
    public async Task<Result> SavePlayerPositionAsync(
        int mapId,
        int x,
        int y,
        bool entered,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        if (entered && mapId > 0)
        {
            // GameBootstrap has already built the target world at this exact official edge. The
            // private server treats SavePos(entered=1) as ClientMapReady and can publish target
            // scene frames as soon as the socket write begins. Advance the Protocol generation
            // synchronously so background RX never compares those frames to the previous map.
            // Nonpositive wire ids do not name a new projected scene; SavePos still forwards them.
            BeginWorldScenePublication(mapId, cancellationToken);
        }

        try
        {
            await SendAsync(
                () => _netClient.SavePos(mapId, x, y, entered),
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "player.position.save.timeout",
                "The position-save request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "player.position.save.failure",
                "The position-save request could not be sent.");
        }
    }

    public async Task<Result> RequestReviveAsync(
        WorldReviveOption option,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerReviveCodec.TryEncode(option, out payload))
        {
            return Result.Failure(
                "combat.revive.choice.invalid",
                "The revive choice is not part of the official ordinary dialog.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.ReviveRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "combat.revive.send.timeout",
                "The revive request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "combat.revive.send.failure",
                "The revive request could not be sent.");
        }
    }

    public Task<Result> BeginCharacterWorkAsync(int skill, CancellationToken cancellationToken)
    {
        // Native work state continues only after the sender returns normally.
        if (_stage == GatewayStage.InWorld && !cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.BeginWork(skill);
            return Task.FromResult(Result.Success());
        }

        return SendWorkCommandAsync(() => _netClient.BeginWork(skill), cancellationToken);
    }

    public Task<Result> GatherCharacterWorkAsync(int skill, CancellationToken cancellationToken)
    {
        if (_stage == GatewayStage.InWorld && !cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Gather(skill);
            return Task.FromResult(Result.Success());
        }

        return SendWorkCommandAsync(() => _netClient.Gather(skill), cancellationToken);
    }

    private async Task<Result> SendWorkCommandAsync(Action writer, CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
            return Result.Failure("session.stage.invalid", "The session is not in the world.");
        try
        {
            await SendAsync(writer, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("player.work.send.failure", "The work request could not be sent."); }
    }

    public Task<Result> EndCharacterWorkAsync(CancellationToken cancellationToken)
    {
        // CancelWork callers must not resume after a native sender exception.
        if (_stage == GatewayStage.InWorld && !cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.EndWork();
            return Task.FromResult(Result.Success());
        }

        return EndCharacterWorkScheduledAsync(cancellationToken);
    }

    private async Task<Result> EndCharacterWorkScheduledAsync(CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure("session.stage.invalid", "The session is not in the world.");
        }
        try
        {
            await SendAsync(() => _netClient.EndWork(), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("player.work.end.send.failure", "The end-work request could not be sent.");
        }
    }

    public Task<Result> SetPlayerSittingAsync(bool sitting, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.Sit(sitting);
            return Task.FromResult(Result.Success());
        }

        return SetPlayerSittingScheduledAsync(sitting, cancellationToken);
    }

    private async Task<Result> SetPlayerSittingScheduledAsync(bool sitting, CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure("session.stage.invalid", "The session is not in the world.");
        }
        try
        {
            await SendAsync(() => _netClient.Sit(sitting), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure("player.sit.send.failure", "The sitting state could not be sent.");
        }
    }

    public async Task<Result> UseCharacterSkillAsync(
        int skillId,
        int targetEntityId,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.WriteUseSkillForIndependentRuntime(skillId, targetEntityId),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "hotbar.skill.timeout",
                "The character-skill request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "hotbar.skill.failure",
                "The character-skill request could not be sent.");
        }
    }

    public async Task<Result> UseCharacterSkillAtAsync(
        int skillId,
        int x,
        int y,
        int targetEntityId,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.WriteUseSkillAtForIndependentRuntime(skillId, x, y, targetEntityId),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "hotbar.skill.timeout",
                "The character-skill request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "hotbar.skill.failure",
                "The character-skill request could not be sent.");
        }
    }

    // NetClient.PetUseSkill 0x06000737 is a recovered official member since WBD-124, so this
    // command takes the route NetClient.PetSetSkill 0x06000736 takes since WBD-120 instead of
    // encoding the payload here first: arm64 0x26e88c / x86_64 0x27ddf0 tail call
    // PetCmd 0x06000733 with subcommand 6 and the caller's Int32 unchanged, and the writer
    // <PetCmd>b__0 0x06000829 runs inside NetClient.Send 0x06000772 after its connected test, so
    // nothing is serialized while the socket is down. Encoding here produced a byte-identical
    // payload before that test - even while disconnected - and the identity failure
    // TryEncodeUseSkill could return has no native counterpart, so both are gone.
    // No skill validation is applied either: the two real callers pass a skill id
    // (GameHud.UseHotSlot 0x0600043c, Win05.DrawPet 0x06001043) and the server adjudicates it.
    // PrivateServerPetCommandCodec.TryEncodeUseSkill keeps the byte shape for comparison and has
    // no runtime caller - only the Editor byte audit reaches it, through
    // PrivateServerHotbarCodec.TryEncodePetSkill.
    public Task<Result> UsePetSkillAsync(int skillId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetUseSkill(skillId);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetUseSkill(skillId),
            "hotbar.pet-skill",
            cancellationToken);
    }

    // NetClient.PetAddPoint 0x06000734 and NetClient.PetRename 0x06000735 are official members now,
    // so these two commands take the same route NetClient.PetRecall 0x06000738 takes: the member
    // itself goes through the single send gate and <PetCmd>b__0 0x06000829 produces the bytes
    // inside NetClient.Send 0x06000772, after its connected test. The frozen bodies add no
    // validation - PetAddPoint forwards the Int32 unchanged and PetRename forwards the string
    // unchanged with the Int32 argument pinned to zero - so the attribute range test
    // AllocatePetPointAsync used to apply through WorldPetAttributeKind and the trim/length test
    // RenamePetAsync used to apply through the codec are gone here, exactly as the 0..9 ValidSlot
    // test is gone from RecallPetAsync since WBD-080. The private server already adjudicates both:
    // scene.AddPetPoint rejects unknown AttributeCode values and an empty free-point budget, and
    // scene.RenamePet trims and validates the name itself.
    // PrivateServerPetCommandCodec.TryEncodeAddPoint / TryEncodeRename keep the byte shape for
    // comparison and have no runtime caller.
    public Task<Result> AllocatePetPointAsync(
        int attributeCode,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetAddPoint(attributeCode);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetAddPoint(attributeCode),
            "pet.add-point",
            cancellationToken);
    }

    public Task<Result> RenamePetAsync(string name, CancellationToken cancellationToken)
    {
        // SubmitPetRename echoes the name and leaves edit mode only after this call returns.
        // Its original host/None binding must run the existing native sender on this stack.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetRename(name);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetRename(name),
            "pet.rename",
            cancellationToken);
    }

    // NetClient.PetSetSkill 0x06000736 is an official member since WBD-120, so this command takes
    // the same route NetClient.PetAddPoint 0x06000734 and NetClient.PetRename 0x06000735 take: the
    // member itself goes through the single send gate and <PetCmd>b__0 0x06000829 produces the
    // bytes inside NetClient.Send 0x06000772, after its connected test. The frozen body adds no
    // validation - the Int32 the caller hands in is forwarded unchanged, and the only caller,
    // Win05.DrawPet 0x06001043, passes the literal 0 - so the "skillId < 0" test
    // SetPetSkillAsync used to apply (both here and in ClientSessionCoordinator) is gone for the
    // same reason the attribute range test is gone from AllocatePetPointAsync: it has no native
    // counterpart and the server adjudicates the id. Nothing is serialized while the socket is
    // down, and no local state announces success - the visible selection stays the server's until
    // its next 0x800b snapshot replaces the echo the drawing arm makes.
    // PrivateServerPetCommandCodec.TryEncodeSetSkill keeps the byte shape for comparison and has
    // no runtime caller.
    public Task<Result> SetPetSkillAsync(int skillId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetSetSkill(skillId);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetSetSkill(skillId),
            "pet.set-skill",
            cancellationToken);
    }

    // CharData.SwitchPet 0x06000f22 is the only official caller of NetClient.PetCmd with
    // subcommand 5 - arm64 0x394b0c mov w1, #5 and x86_64 0x3a7e22 mov esi, 5, both with the
    // empty-string metadata slot (usage 10335, arm64 [0x149bb98] / x86_64 [rip + 0x10e81f1]) as
    // the text argument - so, exactly like NetClient.PetRecall 0x06000738 and NetClient.PetSwap
    // 0x0600073d, this command hands the recovered official member itself to the single send gate
    // instead of encoding a payload before it: the writer <PetCmd>b__0 0x06000829 runs inside
    // NetClient.Send 0x06000772 after its connected test, which is the only gate the official
    // body has. Nothing is serialized while the socket is down, and the slot travels as the
    // Int32 the loop index is. PrivateServerPetCommandCodec.TryEncodeSwitch keeps the byte shape
    // for comparison and has no runtime caller; the ValidSlot 0..9 test it applies is not a
    // native branch, and ClientSessionCoordinator.SwitchPetAsync drops its copy of it as well.
    public Task<Result> SwitchPetAsync(int slot, CancellationToken cancellationToken)
    {
        return SendPetCommandAsync(
            () => _netClient.PetCmd(5, slot, string.Empty),
            "pet.switch",
            cancellationToken);
    }

    // NetClient.PetRecall (0x06000738) is an official member now, so this command hands the member
    // itself to the single existing send path instead of encoding the payload here first: the
    // writer <PetCmd>b__0 (0x06000829) runs inside NetClient.Send (0x06000772) after its connected
    // test, which is the only gate the official body has. No slot range is checked here either:
    // since WBD-080 the 0..9 gate the product used to apply in
    // ClientSessionCoordinator.RecallPetAsync is gone for this command, because the frozen
    // 0x06000738 body forwards the Int32 unchanged and the server adjudicates the slot. Since
    // WBD-083 the same is true of DeployPetAsync and HatchPetAsync: the shared ValidPetSlot
    // helper no longer guards them either.
    public Task<Result> RecallPetAsync(int slot, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetRecall(slot);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetRecall(slot),
            "pet.recall",
            cancellationToken);
    }

    // NetClient.PetDeploy 0x06000739 is the same one-tail-call body as PetRecall - the frozen
    // arm64 0x26e954 / x86_64 0x27de90 windows set the subcommand to 8 and hand the bag slot and
    // the empty-string metadata slot (usage 10335) straight to PetCmd - so WBD-083 moves this
    // command onto the recovered official member as well instead of encoding the payload before
    // the gate. The bytes are then produced by <PetCmd>b__0 0x06000829 inside NetClient.Send
    // 0x06000772 after its connected test, and nothing is serialized while the socket is down.
    // No 0..9 range test is applied: the frozen body validates nothing and the server adjudicates
    // the slot. PrivateServerPetCommandCodec.TryEncodeDeploy keeps the byte shape for comparison
    // and has no runtime caller.
    public Task<Result> DeployPetAsync(int slot, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetDeploy(slot);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetDeploy(slot),
            "pet.deploy",
            cancellationToken);
    }

    // NetClient.PetHatch 0x0600073a, the pet-egg subcommand: arm64 0x26e9b8 / x86_64 0x27dee0 set
    // the subcommand to 9 and tail call PetCmd with the same empty-string slot, so this command
    // takes the same route as DeployPetAsync and PetRecall for the same reason. The frozen body
    // validates nothing, so no slot range is checked here either.
    public Task<Result> HatchPetAsync(int slot, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetHatch(slot);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetHatch(slot),
            "pet.hatch",
            cancellationToken);
    }

    // NetClient.PetShow 0x0600073b is a recovered official member since WBD-121, so this command
    // takes the same route as PetRecallAsync, DeployPetAsync and HatchPetAsync instead of
    // encoding a payload before the gate: arm64 0x26ea1c / x86_64 0x27df30 tail call
    // PetCmd 0x06000733 with subcommand 10 and the Boolean in the Int32 slot, and the writer
    // <PetCmd>b__0 0x06000829 runs inside NetClient.Send 0x06000772 after its connected test, so
    // nothing is serialized while the socket is down. No negation, toggle or local state happens
    // here either: the frozen body forwards its argument unchanged and the display flag it moves
    // is the server's - Win05.DrawPet 0x06001043 is the only caller and it is the side that
    // echoes into CharData.petShow before calling.
    public Task<Result> SetPetShownAsync(bool show, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetShow(show);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetShow(show),
            "pet.show",
            cancellationToken);
    }

    // NetClient.PetSwap (0x0600073d) is an official member now, for the same reason PetRecall is:
    // the decimal text is produced by Int32.ToString() inside the official body and written by
    // <PetCmd>b__0 inside NetClient.Send after the connected test, so nothing is serialized while
    // the socket is down. Since WBD-080 no equal-slot or range guard is applied on this path
    // either: the 0x0600073d body validates nothing, and the only call site already takes the
    // official target != held branch (Win05.DrawPet 06001043), so both arguments travel to the
    // server exactly as the official client sends them.
    public Task<Result> SwapPetAsync(
        int fromSlot,
        int toSlot,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.PetSwap(fromSlot, toSlot);
            return Task.FromResult(Result.Success());
        }

        return SendPetCommandAsync(
            () => _netClient.PetSwap(fromSlot, toSlot),
            "pet.swap",
            cancellationToken);
    }

    private async Task<Result> SendPetCommandAsync(
        byte[] payload,
        string codePrefix,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(ProtocolOpcodes.PetCommandRequest, payload, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                codePrefix + ".timeout",
                "The pet request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                codePrefix + ".failure",
                "The pet request could not be sent.");
        }
    }

    // Same single send gate as the payload overload above, but the command is the recovered
    // official NetClient member instead of a payload encoded before the gate, so the writer the
    // member hands to NetClient.Send is what produces the bytes - and only after Send's connected
    // test (SendAsync -> _sendOperation -> NetClient.Send). No second port, executor or writer is
    // added; only NetClient.PetRecall 0x06000738, NetClient.PetDeploy 0x06000739,
    // NetClient.PetHatch 0x0600073a, NetClient.PetShow 0x0600073b, NetClient.PetSwap 0x0600073d
    // and the
    // NetClient.PetCmd(5, slot, string.Empty) call CharData.SwitchPet 0x06000f22 makes reach this
    // helper, the other pet subcommands still take the payload overload.
    private async Task<Result> SendPetCommandAsync(
        Action send,
        string codePrefix,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(send, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                codePrefix + ".timeout",
                "The pet request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                codePrefix + ".failure",
                "The pet request could not be sent.");
        }
    }

    public Task<Result> SetPetViewAsync(
        int mask,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            if (mask < 0 || mask > 7)
            {
                return Task.FromResult(Result.Failure(
                    "pet.view.mask.invalid",
                    "The pet visibility mask is invalid."));
            }

            _netClient.SetPetView(mask);
            return Task.FromResult(Result.Success());
        }

        return SetPetViewScheduledAsync(mask, cancellationToken);
    }

    private async Task<Result> SetPetViewScheduledAsync(
        int mask,
        CancellationToken cancellationToken)
    {
        if (mask < 0 || mask > 7)
        {
            return Result.Failure(
                "pet.view.mask.invalid",
                "The pet visibility mask is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(() => _netClient.SetPetView(mask), cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "pet.view.timeout",
                "The pet visibility request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "pet.view.failure",
                "The pet visibility request could not be sent.");
        }
    }

    // NetClient.SetTitle 0x060006c7 is an official member since WBD-126, so the member itself goes
    // to the single send gate: <SetTitle>b__0 0x0600088a writes the string inside NetClient.Send
    // 0x06000772, after its connected test, instead of PrivateServerCharacterTitleCodec.TryEncode
    // building the payload here, before the gates and before the socket is known to be up. Null is
    // unchanged - TryEncode already wrote title ?? string.Empty; the Owns gate stays. The native
    // host callback has no world-stage gate; the scheduled compatibility path keeps its admission.
    public Task<Result> SetCharacterTitleAsync(
        string title,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            WorldCharacterTitleState state = Volatile.Read(ref _characterTitleState);
            if (state == null || !state.Owns(title))
            {
                return Task.FromResult(Result.Failure(
                    "character.title.not-owned",
                    "The character does not own that title."));
            }

            _netClient.SetTitle(title);
            return Task.FromResult(Result.Success());
        }

        return SetCharacterTitleScheduledAsync(title, cancellationToken);
    }

    private async Task<Result> SetCharacterTitleScheduledAsync(
        string title,
        CancellationToken cancellationToken)
    {
        WorldCharacterTitleState state = Volatile.Read(ref _characterTitleState);
        if (state == null || !state.Owns(title))
        {
            return Result.Failure(
                "character.title.not-owned",
                "The character does not own that title.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.SetTitle(title),
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "character.title.timeout",
                "The character-title request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "character.title.failure",
                "The character-title request could not be sent.");
        }
    }

    public async Task<Result> SetEquipmentEffectsMaskAsync(
        int hideMask,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.EquipmentEffectsMaskRequest,
                PrivateServerEquipmentEffectsCodec.Encode(hideMask),
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "equipment.effects.timeout",
                "The equipment-effect preference timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "equipment.effects.failure",
                "The equipment-effect preference could not be sent.");
        }
    }

    public Task<Result> StoreWardrobeItemAsync(byte bagTab, int bagSlot, CancellationToken cancellationToken)
    {
        // The original inventory confirmation writes before returning to its GUI caller.
        // Only its host-thread, noncancelable route bypasses the asynchronous adapter.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.WardrobeStore(bagTab, bagSlot);
            return Task.FromResult(Result.Success());
        }

        return StoreWardrobeItemCoreAsync(bagTab, bagSlot, cancellationToken);
    }

    private async Task<Result> StoreWardrobeItemCoreAsync(byte bagTab, int bagSlot, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(() => _netClient.WardrobeStore(bagTab, bagSlot), cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception exception) when (IsWireFailure(exception))
        { return Result.Failure("wardrobe.store.send.failure", "The wardrobe storage request could not be sent."); }
    }

    // Boundary marker (WBD-061): the official grid click (Win05.DrawMagicStore 0x0600104f) calls
    // NetClient.I.WardrobeWear inside the mouse-up branch and only null-checks NetClient.I, so
    // this adapter is reached from that hit point through the Presentation facade and must stay
    // fire-and-forget: SendAsync only runs the local send, it never waits for a server reply. The
    // InWorld stage gate below and the timeout/failure Results it can produce are product-only
    // additions that the official void send does not have; they are the same convention the store,
    // extract and move wardrobe commands already use, so they are marked here instead of being
    // removed. The private codec hop it replaced encoded the same single I32 but also added an
    // itemId <= 0 drop that the official body does not have.
    public Task<Result> ToggleWardrobeItemAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.WardrobeWear(itemId);
            return Task.FromResult(Result.Success());
        }

        return SendWardrobeCommandAsync(
            () => _netClient.WardrobeWear(itemId),
            "wardrobe.wear",
            cancellationToken);
    }

    // Boundary marker (WBD-058): the official extraction confirmation (0x06001071) sends
    // NetClient.WardrobeRemove inside the callback itself and only null-checks NetClient.I, so
    // this adapter is reached synchronously from that callback and must stay fire-and-forget:
    // SendAsync only runs the local send, it never waits for a server reply. The InWorld stage
    // gate below and the timeout/failure Results it can produce are product-only additions that
    // the official void send does not have; they are the same convention the store, toggle and
    // move wardrobe commands already use, so they are marked here instead of being removed.
    public Task<Result> ExtractWardrobeItemAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.WardrobeRemove(itemId);
            return Task.FromResult(Result.Success());
        }

        return ExtractWardrobeItemScheduledAsync(itemId, cancellationToken);
    }

    private async Task<Result> ExtractWardrobeItemScheduledAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.WardrobeRemove(itemId),
                cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "wardrobe.extract.timeout",
                "The wardrobe request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "wardrobe.extract.failure",
                "The wardrobe request could not be sent.");
        }
    }

    // Boundary marker (WBD-061): the official grid drop (Win05.DrawMagicStore 0x0600104f) calls
    // NetClient.I.WardrobeMove inside the drag mouse-up branch and only null-checks NetClient.I, so
    // this adapter is reached from that hit point through the Presentation facade and must stay
    // fire-and-forget. The InWorld stage gate below and the timeout/failure Results it can produce
    // are product-only additions that the official void send does not have; they are the same
    // convention the store, extract and wear wardrobe commands already use. The private codec hop
    // it replaced encoded the same two I32 values in the same order but also added itemId <= 0 and
    // category-index range drops that the official body does not have.
    public Task<Result> MoveWardrobeItemAsync(
        int itemId,
        int categoryIndex,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.WardrobeMove(itemId, categoryIndex);
            return Task.FromResult(Result.Success());
        }

        return SendWardrobeCommandAsync(
            () => _netClient.WardrobeMove(itemId, categoryIndex),
            "wardrobe.move",
            cancellationToken);
    }

    private async Task<Result> SendWardrobeCommandAsync(
        Action send,
        string codePrefix,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                codePrefix + ".timeout",
                "The wardrobe request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                codePrefix + ".failure",
                "The wardrobe request could not be sent.");
        }
    }

    public Task<Result> UpgradeSkillAsync(int skillId, CancellationToken cancellationToken)
    {
        if (Endgame.GrowthV5 && EndgameSkillRankView.IsProfessionSkill(skillId)) return UpgradeGrowthSkillAsync(skillId, cancellationToken);
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.UpgradeSkill(skillId);
            return Task.FromResult(Result.Success());
        }

        return SendSkillBookRequestAsync(() => _netClient.UpgradeSkill(skillId), "skill.upgrade", cancellationToken);
    }

    public Task<Result> LearnLifeSkillAsync(int skillId, CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.LearnLife(skillId);
            return Task.FromResult(Result.Success());
        }

        return SendSkillBookRequestAsync(() => _netClient.LearnLife(skillId), "life-skill.learn", cancellationToken);
    }

    private async Task<Result> SendSkillBookRequestAsync(
        Action send, string errorPrefix, CancellationToken cancellationToken)
    {
        try
        {
            await SendAsync(send, cancellationToken).ConfigureAwait(false);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                errorPrefix + ".timeout",
                "The skill-book request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                errorPrefix + ".failure",
                "The skill-book request could not be sent.");
        }
    }

    public async Task<Result> UseCaptureInventoryItemAsync(
        int targetEntityId,
        int itemId,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerInventoryCodec.TryEncodeCaptureUse(
                targetEntityId,
                itemId,
                out payload))
        {
            return Result.Failure(
                "inventory.capture.identity.invalid",
                "The capture target or item identity is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.CaptureInventoryItemRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "inventory.capture.timeout",
                "The capture-item request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "inventory.capture.failure",
                "The capture-item request could not be sent.");
        }
    }

    public Task<Result> UseInventoryToolOnAsync(
        int toolId,
        byte targetKind,
        byte tab,
        int slot,
        CancellationToken cancellationToken) =>
        UseItemOnAsync(toolId, targetKind, tab, slot, cancellationToken);

    public Task<Result> SplitInventoryItemAsync(
        int tab,
        int bagIndex,
        int count,
        CancellationToken cancellationToken)
    {
        // CommitSplit closes the modal only after the native send call returns.
        // Preserve that call stack, full scalar capture and synchronous failures.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SplitItem(tab, bagIndex, count);
            return Task.FromResult(Result.Success());
        }

        return SplitInventoryItemCoreAsync(unchecked((byte)tab), bagIndex, count, cancellationToken);
    }

    private async Task<Result> SplitInventoryItemCoreAsync(
        byte tab,
        int bagIndex,
        int count,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerInventoryCodec.TryEncodeSplit(
                tab,
                bagIndex,
                count,
                out payload))
        {
            return Result.Failure(
                "inventory.split.identity.invalid",
                "The inventory stack split identity is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.SplitInventoryItemRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "inventory.split.timeout",
                "The inventory stack-split request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "inventory.split.failure",
                "The inventory stack-split request could not be sent.");
        }
    }

    public Task<Result> EquipInventoryItemAsync(
        int bagIndex,
        CancellationToken cancellationToken)
    {
        // InventoryDlg's immediate and affirmative callbacks send on this call stack.
        // Native Send owns the connected gate, encoding, pacing and exception behavior.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.EquipItem(bagIndex);
            return Task.FromResult(Result.Success());
        }

        return EquipInventoryItemCoreAsync(bagIndex, cancellationToken);
    }

    private async Task<Result> EquipInventoryItemCoreAsync(
        int bagIndex,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerInventoryCodec.TryEncodeEquip(bagIndex, out payload))
        {
            return Result.Failure(
                "inventory.equip.slot.invalid",
                "The inventory equipment slot is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.EquipInventoryItemRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "inventory.equip.timeout",
                "The inventory equipment request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "inventory.equip.failure",
                "The inventory equipment request could not be sent.");
        }
    }

    // NetClient.UnequipItem 0x060006e6 is an official member since WBD-125, so the member itself
    // goes to the single send gate: <UnequipItem>b__0 0x060007bb writes the four bytes inside
    // NetClient.Send 0x06000772, after its connected test, instead of
    // PrivateServerInventoryCodec.TryEncodeUnequip encoding them before it. The 1..11 slot test
    // that codec applied is gone - the frozen body forwards the Int32 unchanged - and the codec
    // stays for the Editor byte audit only.
    public Task<Result> UnequipInventoryItemAsync(
        int slot,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.UnequipItem(slot);
            return Task.FromResult(Result.Success());
        }

        return UnequipInventoryItemScheduledAsync(slot, cancellationToken);
    }

    private async Task<Result> UnequipInventoryItemScheduledAsync(
        int slot,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.UnequipItem(slot),
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "inventory.unequip.timeout",
                "The inventory unequip request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "inventory.unequip.failure",
                "The inventory unequip request could not be sent.");
        }
    }

    // NetClient.AllocPoints 0x0600071a is an official member since WBD-125, so the member itself
    // goes to the single send gate: <AllocPoints>b__0 0x06000803 reads the seven values and writes
    // them inside NetClient.Send 0x06000772, after its connected test, instead of
    // PrivateServerInventoryCodec.TryEncodeAttributePoints reading them and building the payload
    // before it - so while the socket is down they are not read at all. The null, count-7 and
    // non-negative tests that codec applied are gone; the frozen writer keeps only the runtime
    // range check of its seven element accesses. The array travels by reference as the official
    // int[] argument, and the codec stays for the Editor byte audit only.
    public Task<Result> AllocateAttributePointsAsync(
        int[] points,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.AllocPoints(points);
            return Task.FromResult(Result.Success());
        }

        return AllocateAttributePointsScheduledAsync(points, cancellationToken);
    }

    private async Task<Result> AllocateAttributePointsScheduledAsync(
        int[] points,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                () => _netClient.AllocPoints(points),
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "attribute.points.timeout",
                "The attribute-point request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "attribute.points.failure",
                "The attribute-point request could not be sent.");
        }
    }

    public Task<Result> SortInventoryAsync(
        int tab,
        CancellationToken cancellationToken)
    {
        // The live inventory GUI continues only after the native send returns.
        // Keep the full scalar capture and let send failures interrupt that call stack.
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SortBag(tab);
            return Task.FromResult(Result.Success());
        }

        return SortInventoryCoreAsync(unchecked((byte)tab), cancellationToken);
    }

    private async Task<Result> SortInventoryCoreAsync(
        byte tab,
        CancellationToken cancellationToken)
    {
        byte[] payload;
        if (!PrivateServerInventoryCodec.TryEncodeSort(tab, out payload))
        {
            return Result.Failure(
                "inventory.sort.tab.invalid",
                "The inventory tab is invalid.");
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(
                ProtocolOpcodes.SortInventoryRequest,
                payload,
                cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "inventory.sort.timeout",
                "The inventory-sort request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "inventory.sort.failure",
                "The inventory-sort request could not be sent.");
        }
    }

    // NetClient.TradeOffer (0x06000751) captures its arguments and calls NetClient.Send
    // (0x06000772); the official writer 0x0600084d runs inside Send, AFTER the connected test, so
    // nothing is serialized - and a null offer list cannot raise - while the socket is down. The
    // command therefore hands the official member itself to the single existing send path instead
    // of encoding the payload here first. The offer list travels as the caller's real
    // List<(byte,int,int)>, so the writer enumerates it with the official struct enumerator.
    public Task<Result> SubmitTradeOfferAsync(
        long money,
        List<(byte Tab, int Slot, int Count)> items,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TradeOffer(money, items);
            return Task.FromResult(Result.Success());
        }

        return SendTradeCommandAsync(
            () => _netClient.TradeOffer(money, items),
            "trade.offer",
            cancellationToken);
    }

    public Task<Result> SetTradeLockAsync(
        bool locked,
        CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TradeLock(locked);
            return Task.FromResult(Result.Success());
        }

        return SendTradeCommandAsync(
            () => _netClient.TradeLock(locked),
            "trade.lock",
            cancellationToken);
    }

    // Official 0x060007aa (NetClient+<>c.<TradeConfirm>b__233_0) writes nothing: an empty payload.
    public Task<Result> ConfirmTradeAsync(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TradeConfirm();
            return Task.FromResult(Result.Success());
        }

        return SendTradeCommandAsync(
            () => _netClient.TradeConfirm(),
            "trade.confirm",
            cancellationToken);
    }

    // Official 0x060007ab (NetClient+<>c.<TradeCancel>b__234_0) writes nothing: an empty payload.
    public Task<Result> CancelTradeAsync(CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.TradeCancel();
            return Task.FromResult(Result.Success());
        }

        return SendTradeCommandAsync(
            () => _netClient.TradeCancel(),
            "trade.cancel",
            cancellationToken);
    }

    // The only gate on these four paths is the session-host lifecycle boundary the product needs to
    // own a world at all. The official connection check is the one inside NetClient.Send
    // (_tcp != null && _tcp.Connected -> write and send; otherwise OnError("未连接服务器") and
    // return), and the server still arbitrates every trade action, so no product trade-state
    // filter sits in front of it. This helper reaches only the four commands above.
    private async Task<Result> SendTradeCommandAsync(
        Action send,
        string codePrefix,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            await SendAsync(send, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                codePrefix + ".timeout",
                "The trade command timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                codePrefix + ".failure",
                "The trade command could not be sent.");
        }
    }

    public Task<Result> SendWorldChatAsync(
        WorldChatSendRequest request,
        CancellationToken cancellationToken)
    {
        // ChatUI clears its input only after the native channel sender returns normally.
        if (request != null && request.Kind == WorldChatSendKind.Channel &&
            _stage == GatewayStage.InWorld && !cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            _netClient.SendChat(request.Channel, request.Text, request.Shares);
            return Task.FromResult(Result.Success());
        }

        if (request != null && request.Kind == WorldChatSendKind.Whisper &&
            !cancellationToken.CanBeCanceled && _host.IsHostThread)
        {
            // Original Gossip is available to a receipt-created owner before a world exists.
            // The native Send connection gate remains authoritative; no stage or reply is made up.
            _netClient.Gossip(request.TargetName, request.Text, request.Shares);
            return Task.FromResult(Result.Success());
        }

        return SendWorldChatScheduledAsync(request, cancellationToken);
    }

    private async Task<Result> SendWorldChatScheduledAsync(
        WorldChatSendRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_stage != GatewayStage.InWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "The session is not in the world.");
        }

        try
        {
            // Every chat kind has a real Protocol owner now, so all three reach the wire through
            // a recovered official command instead of through the private-server chat codec:
            //   * 0x060006c8 NetClient.GmCmd       -> Send(0x1006, b => b.Str(cmd));
            //   * 0x06000771 NetClient.Gossip      -> Send(0x104c, b => { Str; Str; WriteShares });
            //   * 0x060006db NetClient.SendChat    -> Send(0x1058, b => { U8; Str; WriteShares;
            //                                                            I32 0; U8 0 }).
            // All three run behind this gateway's single send gate
            // (SendAsync -> _sendOperation -> NetClient.Send), so no second port is added and the
            // frame bytes are unchanged: WireWriter.WriteString and Buf.Str are both UTF-8 with a
            // little-endian UInt16 length prefix, WriteByte/WriteInt32 match Buf.U8/Buf.I32 and
            // the request text is never null (WorldChatSendRequest normalises it to
            // string.Empty).
            if (request.Kind == WorldChatSendKind.GmCommand)
            {
                await SendAsync(() => _netClient.GmCmd(request.Text), cancellationToken);
                return Result.Success();
            }

            // The share list is handed to the official command as the very instance the flush point
            // created: WorldChatSendRequest.Shares is already a List<(byte, byte, int)> - the type
            // official ChatUI.OutgoingShares carries and the type NetClient.Gossip (0x06000771) and
            // NetClient.SendChat (0x060006db) declare - so no projection, re-ordering or second
            // list exists between ChatUI.TakeOutgoing (0x060000de) and the frame. Nothing is
            // snapshotted here either: the argument is read inside the send lambda, i.e. after the
            // send gate is taken, and NetClient.WriteShares (0x060006dd) indexes the captured list
            // element by element while it writes, so clamping to four, the null-list count 0 and
            // the byte/byte/Int32 read order stay where the official body puts them.
            if (request.Kind == WorldChatSendKind.Whisper)
            {
                await SendAsync(
                    () => _netClient.Gossip(
                        request.TargetName,
                        request.Text,
                        request.Shares),
                    cancellationToken);
                return Result.Success();
            }

            if (request.Kind == WorldChatSendKind.Channel)
            {
                await SendAsync(
                    () => _netClient.SendChat(
                        request.Channel,
                        request.Text,
                        request.Shares),
                    cancellationToken);
                return Result.Success();
            }

            ushort opcode;
            byte[] payload;
            if (!PrivateServerChatCodec.TryEncode(request, out opcode, out payload))
            {
                return Result.Failure(
                    "chat.send.kind.invalid",
                    "The chat request kind is invalid.");
            }

            await SendAsync(opcode, payload, cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return Result.Failure(
                "chat.send.timeout",
                "The chat request timed out while being sent.");
        }
        catch (Exception exception) when (IsWireFailure(exception))
        {
            return Result.Failure(
                "chat.send.failure",
                "The chat request could not be sent.");
        }
    }

    public async Task<Result<WorldEntryRecord>> EnterWorldAsync(
        CharacterId characterId,
        int rosterLevel,
        CancellationToken cancellationToken)
    {
        if (_stage != GatewayStage.Authenticated)
        {
            return Result<WorldEntryRecord>.Failure(
                "session.stage.invalid",
                "The session is not ready to enter the world.");
        }

        WorldEntryAdmission admission = new WorldEntryAdmission(_connectionLifetime, cancellationToken);
        if (Interlocked.CompareExchange(ref _worldEntryAdmission, admission, null) != null)
            throw new InvalidOperationException("An earlier world entry has not completed its host handoff.");

        var receipt = new SelectedWorldEntryReceipt(characterId.Value, rosterLevel);
        try
        {
            // Official 1.5.8 NetClient.EnterGame (0x060006c4) owns this frame: it allocates its own
            // display class and writes a single Buf.Str, so the request now leaves through the
            // recovered writer instead of a hand-built WireWriter payload. WireWriter.WriteString
            // throws on null and on a byte length above ushort.MaxValue, where official Buf.Str
            // substitutes the empty string and truncates the U16 length.
            await SendAsync(
                () =>
                {
                    // Install the actual selected consumer before this send can produce a
                    // receipt. Both actions use the same existing host/send-operation turn.
                    admission.SelectedReceipt = receipt;
                    _netClient.EnterGame(characterId.Value);
                    ConsumePendingSelectedEntry(admission);
                    if (!receipt.Completion.Task.IsCompleted)
                        receipt.StartTimeout(CreateResponseTimeout(cancellationToken));
                },
                cancellationToken);

            return await receipt.Completion.Task;
        }
        catch (ServerRejectedException exception) when (!receipt.ReceiptFailed)
        {
            return Result<WorldEntryRecord>.Failure("world.enter.rejected", exception.Message);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (!receipt.ReceiptFailed)
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            return Result<WorldEntryRecord>.Failure(
                "world.enter.timeout",
                "The enter-world response timed out.");
        }
        // Keep send/startup-reader wire mapping, but a failure from the actual SelfData
        // callback must escape its receipt with its reached state. It is not an invalid
        // async response and must not trigger this observer's connection disposal.
        catch (Exception exception) when (!receipt.ReceiptFailed && IsWireFailure(exception))
        {
            await DisposeConnectionAsync().ConfigureAwait(false);
            return Result<WorldEntryRecord>.Failure(
                "world.enter.response.invalid",
                "The enter-world response was missing or invalid.");
        }
        finally
        {
            receipt.Dispose();
            // Rejection/send failure before SelfData never suspended the inbox. Retire only
            // this request's capability, so another entry on the same connection can start.
            if (!admission.SelfDataDequeued)
                Interlocked.CompareExchange(ref _worldEntryAdmission, null, admission);
        }
    }

    /// <summary>
    /// Synchronous dispose is a host-thread fast path. The product callers are the Unity
    /// lifecycle and the Unity UI (ClientBootstrap.OnDestroy, ClientSessionCoordinator.Dispose,
    /// ClientSessionCoordinator.ConfirmServerSelection, ClientSessionCoordinator.ReturnToLogin,
    /// and the Editor QA entries), all of which run on the captured host thread, so the close
    /// runs inline and is complete when this returns.
    /// An off-host synchronous dispose cannot wait for a posted close without blocking the
    /// host pump, and it must not post a close nobody observes, so it is refused instead of
    /// being faked: that caller has to await <see cref="DisposeAsync"/>.
    /// </summary>
    public void Dispose()
    {
        if (!_host.IsHostThread)
        {
            throw new InvalidOperationException(
                "The gateway can only be disposed synchronously on the host thread; " +
                "await DisposeAsync() from any other thread.");
        }

        DisposeConnectionCore();
    }

    // Direct capability of the same Protocol owner, invoked by the corresponding
    // Unity NetClient.OnDestroy. It remains valid after the host executor retires.
    public void CloseNativeForIndependentRuntime() => _netClient.Close();

    // Only terminal root release selects this path, when the exact bound Unity
    // owner exists (or has already been destroyed). No native-close latch is used.
    public void DisposeForNativeOwnerDestruction()
    {
        if (!_host.IsHostThread)
            throw new InvalidOperationException("Final gateway disposal must run on the host thread.");
        DisposeConnectionCore(false);
    }

    // Final composition-root handoff, after the existing session Dispose attempt and before
    // the executor is closed. No native callback is delivered after this terminal boundary;
    // late workers release their captured resources without needing another Pump or post.
    public void CompleteHostShutdownForIndependentRuntime()
    {
        if (!_host.IsHostThread)
            throw new InvalidOperationException("Final gateway shutdown must run on the host thread.");
        _hostShutdown = true;
        _netClient.RetireConnectionResourcesForIndependentRuntime();
    }

    /// <summary>
    /// Disposal for callers that are not the host thread. It completes when the host has
    /// really run the disposal core.
    /// </summary>
    public Task DisposeAsync()
    {
        return DisposeConnectionAsync();
    }

    private Task SendAsync(
        ushort opcode,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        return SendAsync(() => _netClient.SendPacket(opcode, payload), cancellationToken);
    }

    private async Task SendAsync(Action send, CancellationToken cancellationToken)
    {
        bool lockTaken = false;
        try
        {
            await _sendOperation.WaitAsync(cancellationToken).ConfigureAwait(false);
            lockTaken = true;
            // The native send body (NetClient.Send -> PaceTx -> _txPending/_txSecCount/
            // _txSecStart) is lock-free in the official body, so it runs on the host thread.
            // On the host this completes inline; off the host it is posted and awaited. The
            // semaphore is released only after the send really ran, and a send that faults or
            // is cancelled before it starts is reported as faulted or cancelled, never as a
            // silent success.
            await _host.RunAsync(send, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            if (lockTaken)
            {
                _sendOperation.Release();
            }
        }
    }

    private async Task<ProtocolPacket> ReadUntilAsync(
        ushort expectedOpcode,
        CancellationToken cancellationToken)
    {
        ProtocolPacket pending;
        if (TryTakePendingDownlink(expectedOpcode, out pending))
        {
            return pending;
        }

        for (int index = 0; index < MaximumPacketsBeforeExpectedResponse; index++)
        {
            ProtocolPacket packet = await ReadPacketAsync(cancellationToken);
            if (packet.Opcode == expectedOpcode)
            {
                return packet;
            }

            LogUnexpectedDownlink("session", packet, expectedOpcode);
            RetainPendingDownlink(packet);
        }

        throw new InvalidDataException(
            "The expected session response did not arrive within the packet bound.");
    }

    // The selected carrier calls this on the host for each already dispatched packet.
    // Its fields are the former reader locals; all branch order and decode/publication bodies
    // below are retained. Host actions complete inline, so no packet or startup observation
    // needs a second dispatch while SelfData enters the title callback on its own receipt.
    private async Task<EnterWorldResponse> ReadEnterWorldResponseAsync(
        SelectedWorldEntryReceipt receipt, ProtocolPacket packet,
        CancellationToken cancellationToken)
    {
        {
            if (packet.Opcode == ProtocolOpcodes.SelfData)
            {
                return new EnterWorldResponse(
                    packet,
                    receipt.remoteWorldStates,
                    receipt.groundItems,
                    receipt.channelChats,
                    receipt.selfPkState,
                    receipt.localFactionState,
                    receipt.partyState,
                    receipt.taskLogState,
                    receipt.npcChatterState,
                    receipt.inventoryState,
                    receipt.wardrobeState,
                    receipt.skillBookState,
                    receipt.lifeSkillBookState,
                    receipt.buffStatePayload,
                    receipt.buffStateNonEmpty,
                    receipt.attributeSnapshot,
                    receipt.petStatus);
            }

            if (packet.Opcode == ProtocolOpcodes.LoginResult)
            {
                WireReader rejection = new WireReader(packet.Payload);
                int resultCode = rejection.ReadInt32();
                string message = rejection.ReadString();
                rejection.EnsureFinished();
                if (resultCode != 0)
                {
                    throw new ServerRejectedException(
                        string.IsNullOrWhiteSpace(message)
                            ? "The server rejected world entry."
                            : message);
                }

                throw new InvalidDataException(
                    "The server sent an invalid enter-world result packet.");
            }

            if (packet.Opcode == ProtocolOpcodes.EntityRemove)
            {
                int removedId;
                byte reason;
                if (!PrivateServerWorldStateDecoder.TryDecodeWorldEntityRemoval(
                        packet.Payload, out removedId, out reason))
                    throw new InvalidDataException("The server sent an invalid startup entity removal.");
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Remove(removedId, reason));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.Emote)
            {
                var reader = new NetClient.Buf(packet.Payload);
                int id = reader.I32();
                int face = reader.I32();
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Emote(id, face));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.CombatState)
            {
                WorldCombatResultState combat;
                if (!PrivateServerWorldStateDecoder.TryDecodeCombatResult(packet.Payload, out combat))
                    throw new InvalidDataException("The server sent an invalid startup combat packet.");
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Combat(combat));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.SkillCastState)
            {
                WorldSkillCastState cast = packet.ReceivedSkillCast;
                if (cast == null)
                    throw new InvalidDataException("The server sent an invalid startup skill-cast packet.");
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Cast(cast));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.StallOwnerUpdate)
            {
                WorldStallOwnerState stall;
                if (!PrivateServerWorldStateDecoder.TryDecodeStallOwnerState(packet.Payload, out stall))
                    throw new InvalidDataException("The server sent an invalid startup stall-owner packet.");
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Stall(stall));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.StallClosed)
            {
                int ownerId;
                if (!PrivateServerWorldStateDecoder.TryDecodeStallClosedOwnerId(packet.Payload, out ownerId))
                    throw new InvalidDataException("The server sent an invalid startup stall-close packet.");
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.RemoveStall(ownerId));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.RemotePlayer)
            {
                WorldRemotePlayerState state;
                int mapIdGuard;
                if (!PrivateServerWorldStateDecoder.TryDecodeRemotePlayerState(
                        packet.Payload,
                        out state,
                        out mapIdGuard))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup remote-player packet.");
                }

                receipt.remoteWorldStates.Add(
                    PendingRemoteWorldState.Spawn(state, mapIdGuard));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.PlayerRename)
            {
                if (!PrivateServerWorldStateDecoder.TryDecodePlayerRename(packet.Payload, out var rename))
                    throw new InvalidDataException("The server sent an invalid startup player-rename packet.");
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Rename(rename));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.PlayerAppearance)
            {
                WorldPlayerAppearanceState state;
                if (!PrivateServerWorldStateDecoder.TryDecodePlayerAppearance(
                        packet.Payload,
                        out state))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup player-appearance packet.");
                }

                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Appearance(state));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.EntitySpawn)
            {
                WorldEntityState state = packet.EntitySpawn;
                if (state == null && !PrivateServerWorldStateDecoder.TryDecodeWorldEntityState(
                        packet.Payload,
                        out state))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup world-entity packet.");
                }

                receipt.remoteWorldStates.Add(PendingRemoteWorldState.GenericSpawn(state, packet.EntitySpawn != null));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.EntityStatusEffect)
            {
                int entityId;
                WorldEntityStatusEffect effect;
                if (!PrivateServerWorldStateDecoder.TryDecodeEntityStatusEffect(
                        packet.Payload,
                        out entityId,
                        out effect))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup entity status-effect packet.");
                }

                // Preserve the receive position; preceding queued operations determine which
                // native registry owns the identity when this packet is published.
                PendingEntityStatusEffect pendingEffect = new PendingEntityStatusEffect(
                    entityId, effect);
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.StatusFx(pendingEffect));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.EntityStatusIcons)
            {
                int entityId;
                IReadOnlyList<WorldEntityStatusIcon> icons;
                if (!PrivateServerWorldStateDecoder.TryDecodeEntityStatusIcons(
                        packet.Payload,
                        out entityId,
                        out icons))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup entity status-icon packet.");
                }

                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Icons(entityId, icons));

                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.GroundItemSpawn)
            {
                WorldGroundItemState state;
                if (!PrivateServerGroundItemCodec.TryDecodeSpawn(
                        packet.Payload,
                        out state))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup ground-item packet.");
                }

                receipt.groundItems.Add(state);
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.GroundItemRemove)
            {
                // The current normal and QA routes consume 0x8013/0x8014 through
                // IGroundItemReceiveContext before this fallback collector can see them.
                // If routing changes and this collector receives ground packets, preserve the
                // official arrival order: 0x8014 reads one i32 and removes the staged id.
                // PublishInitialGroundItems keeps the first row per id, so remove every row.
                // No length or sign guard is added because the official case body has none.
                int removedGroundId = new NetClient.Buf(packet.Payload).I32();
                for (int groundIndex = receipt.groundItems.Count - 1; groundIndex >= 0; groundIndex--)
                {
                    if (receipt.groundItems[groundIndex].GroundId == removedGroundId)
                    {
                        receipt.groundItems.RemoveAt(groundIndex);
                    }
                }

                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.RemoteRidingState)
            {
                WorldRemoteRidingState state;
                if (!PrivateServerWorldStateDecoder.TryDecodeRemoteRidingState(
                        packet.Payload,
                        out state))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup remote-riding packet.");
                }

                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Riding(state));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.EntityMovePath)
            {
                WorldRemotePathState state;
                if (!PrivateServerWorldStateDecoder.TryDecodeRemotePathState(
                        packet.Payload,
                        out state))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup entity movement-path packet.");
                }

                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Path(state));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.PlayerLifeState)
            {
                int entityId;
                bool alive;
                int reviveFlags;
                if (!PrivateServerWorldStateDecoder.TryDecodePlayerLifeState(
                        packet.Payload,
                        out entityId,
                        out alive,
                        out reviveFlags))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup player life-state packet.");
                }

                receipt.remoteWorldStates.Add(
                    PendingRemoteWorldState.Life(entityId, alive, reviveFlags, packet.RemoteLifeAtReceive));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.SelfPkState)
            {
                WorldSelfPkState nextSelfPkState;
                if (!PrivateServerWorldStateDecoder.TryDecodeSelfPkState(
                        packet.Payload,
                        out nextSelfPkState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup self PK-state packet.");
                }

                receipt.selfPkState = nextSelfPkState;
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.LocalFaction)
            {
                WorldLocalFactionState nextLocalFactionState;
                if (!PrivateServerWorldStateDecoder.TryDecodeLocalFactionState(
                        packet.Payload,
                        out nextLocalFactionState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup local-faction packet.");
                }

                receipt.localFactionState = nextLocalFactionState;
                // Official 0x8044 writes CombatWorld.LocalFaction in the case body, so a
                // packet read during session entry writes the owner here and not later, when
                // the world frame may still be unbuilt.
                // The selected receipt carrier runs on the host. Keep the same host-executor
                // action and its real result; it now completes inline before the next packet.
                // The retained snapshot and its publication order are unchanged.
                await ReceiveLocalFactionOnHostAsync(
                        nextLocalFactionState,
                        cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.CharacterTitleSnapshot)
            {
                // Official 0x804c is dispatched whenever it arrives, so a snapshot sent before
                // SelfData is a legal case. This reader had no arm for it: the packet fell
                // through to LogUnexpectedDownlink and was dropped without being retained, and
                // because ReadUntilAsync retains unexpected downlinks a snapshot that arrived
                // during an earlier session read was handed to this very loop and lost the same
                // way. One host-bound step runs the same decode and the same publication as the
                // in-world arm, so the owner stays the single source and no second receive
                // channel is introduced.
                await ReceiveCharacterTitleOnHostAsync(packet.Payload, cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.AttributeSnapshot)
            {
                WorldPlayerAttributeSnapshot nextAttributeSnapshot;
                if (!PrivateServerWorldStateDecoder.TryDecodePlayerAttributeSnapshot(
                        packet.Payload,
                        out nextAttributeSnapshot))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup player attribute packet.");
                }

                // Official case 0x8007 stores both CharData attribute arrays inside the case body,
                // once per received packet and in the order +0xb8 then +0xb0, with the +0xb8 store
                // skipped when the packet's second array is empty (arm64 0x250bec `cmp w19,#0x1` /
                // 0x250bf0 `b.lt 0x250c5c`, past the store at 0x250c50; x86_64 0x25f3cf
                // `testl %r15d,%r15d` / 0x25f3d2 `jle 0x25f441`, past the store at 0x25f42b).
                // Folding several pre-SelfData 0x8007 into one retained snapshot
                // is therefore not equivalent: an early packet whose second array is not empty
                // leaves totals behind that a later, empty-second-array packet must not erase.
                // The mirror root does not exist yet here, and that is not a reason to hold the
                // write back - the owner writes the real CharData statics, which are independent
                // of WorldPlayerStatusState. One awaited host step per packet, in arrival order,
                // with the packet's own write intent handed to the owner.
                bool hasStartupWeightLimit = TryTakeInventoryWeightLimit(
                    nextAttributeSnapshot.Current, out int startupMaximumWeight);
                await ReceivePlayerAttributesOnHostAsync(
                        nextAttributeSnapshot.Current,
                        nextAttributeSnapshot.Total.Count > 0 ? nextAttributeSnapshot.Total : null,
                        hasStartupWeightLimit ? startupMaximumWeight : (int?)null,
                        true,
                        cancellationToken)
                    .ConfigureAwait(false);
                receipt.attributeSnapshot = nextAttributeSnapshot;
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.InventorySnapshot)
            {
                WorldInventoryState nextInventoryState;
                if (!PrivateServerInventoryCodec.TryDecode(
                        packet.Payload,
                        out nextInventoryState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup inventory packet.");
                }

                receipt.inventoryState = nextInventoryState;
                // Official case 0x8006 stores Money (+0x10), CaiYu (+0x18), Weight (+0x20) and
                // MaxWeight (+0x24) inside its own case body, so a startup inventory packet
                // performs all four stores at its own arrival inside this loop and not once
                // after SelfData: a later startup 0x8007 that carries element 23 must still win,
                // which it cannot if the inventory maximum is written after every startup packet
                // has been read. One awaited host step per packet, in arrival order, the same
                // shape the startup 0x8007 arm uses.
                await ReceiveInventoryHeaderOnHostAsync(
                        nextInventoryState.Money,
                        nextInventoryState.CaiYu,
                        nextInventoryState.Weight,
                        nextInventoryState.MaximumWeight,
                        nextInventoryState.Honor,
                        cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.WardrobeSnapshot)
            {
                WorldWardrobeState nextWardrobeState;
                if (!PrivateServerWardrobeCodec.TryDecode(
                        packet.Payload,
                        out nextWardrobeState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup wardrobe packet.");
                }

                receipt.wardrobeState = nextWardrobeState;
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.SkillSnapshot)
            {
                WorldSkillBookObservation nextSkillBookState = packet.ReceivedSkillBookObservation;
                if (!nextSkillBookState.HasValue)
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup skill snapshot.");
                }

                // Official 0x8015 evaluates Push(7) once, after the row loop, when the filled
                // dictionary is nonempty (arm64 0x253478-0x2534e0 / x86_64 0x2625b8-0x262608).
                // The provenance bit is set only after the immediate tip callback returns.
                // Only packets without it need this host fallback; HasValue independently
                // carries receive success without materializing a DTO. Retaining this value
                // never grants this delayed reader state-write authority.
                if (!packet.SkillBookTipEvaluated)
                {
                    await ReceiveSkillBookTipOnHostAsync(nextSkillBookState, cancellationToken)
                        .ConfigureAwait(false);
                }
                receipt.skillBookState = nextSkillBookState;
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.LifeSkillSnapshot)
            {
                WorldLifeSkillBookState nextLifeSkillBookState;
                if (!PrivateServerSkillBookCodec.TryDecodeLifeSkills(
                        packet.Payload,
                        out nextLifeSkillBookState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup life-skill snapshot.");
                }

                receipt.lifeSkillBookState = nextLifeSkillBookState;
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.BuffSnapshot)
            {
                // Official 0x8017 reads the packet through one NetClient.Buf and stores the new
                // list into the CharData static inside the case body (arm64 statics +0xc0 /
                // x86_64 0xc0(%rdi)). The startup reader is a continuation, so the raw payload is
                // handed to one host action that performs that whole read and write before the
                // world frame may exist - and it is applied even if a second 0x8017 follows,
                // because every packet owns its own read.
                receipt.buffStateNonEmpty = await ReceiveBuffStateOnHostAsync(
                        packet.Payload,
                        cancellationToken)
                    .ConfigureAwait(false);
                receipt.buffStatePayload = packet.Payload;
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.PetSnapshot)
            {
                WorldPlayerPetStatusState nextPetStatus = packet.ReceivedPetStatus;
                if (nextPetStatus == null)
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup player pet packet.");
                }

                // Official 0x800b evaluates 51 for the first positive-level skill, then 52 for
                // the first id 13002 / level >= 1 skill (arm64 0x25e7b0 / 0x25e838, x86_64
                // 0x26d4f3 / 0x26d586). A non-null packet.PetStatus proves the immediate callback
                // already completed those searches, including when neither matched. Use the
                // original packet field to retain the context-free tip fallback even though
                // ReceivedPetStatus was decoded on the host. Optional state retention is unchanged.
                if (packet.PetStatus == null)
                {
                    await ReceiveNewbiePetSkillsTipOnHostAsync(nextPetStatus, cancellationToken)
                        .ConfigureAwait(false);
                }
                receipt.petStatus = nextPetStatus.WithRetainedOptionalState(receipt.petStatus);
                return null;
            }

            if (packet.Opcode == 0x8054)
            {
                // Official 0x8054 seeds the server-seen tip ids from inside the case body: one U16
                // count, that many signed I32 ids, then the NewbieTipBox.SeedServer tail call
                // (arm64 0x255070-0x25517c / x86_64 0x2645b2-0x264683), unconditionally on the
                // count - a zero-count packet still seeds the empty list it built. Before this arm
                // existed the packet fell through to LogUnexpectedDownlink and was dropped, so a
                // seed read before SelfData was lost and the ids it already carried stayed unseen.
                // One awaited host step per packet, in arrival order: SeedServer owns the
                // role-scoped PlayerPrefs write and resolves the seen set itself, so no second
                // persistence path, no deferred seed and no final-snapshot replay stands in for
                // the real arrival order.
                var reader = new WireReader(packet.Payload);
                int seedCount = reader.ReadUInt16();
                var seedIds = new List<int>(seedCount);
                for (int seedIndex = 0; seedIndex < seedCount; seedIndex++)
                {
                    seedIds.Add(reader.ReadInt32());
                }

                await ReceiveNewbieTipSeedOnHostAsync(seedIds, cancellationToken)
                    .ConfigureAwait(false);
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.RemotePkState)
            {
                int entityId;
                byte pkState;
                if (!PrivateServerWorldStateDecoder.TryDecodeRemotePkState(
                        packet.Payload,
                        out entityId,
                        out pkState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup remote PK-state packet.");
                }

                receipt.remoteWorldStates.Add(
                    PendingRemoteWorldState.Pk(entityId, pkState));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.ChannelChat)
            {
                PendingChannelChat chat = packet.ChannelChat ?? DecodeChannelChat(packet.Payload, true);
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.ChannelMessage(chat));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.PrivateChat)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Social(RetainSocialNotice(packet)));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.PartySnapshot)
            {
                WorldPartyState nextPartyState;
                if (!PrivateServerWorldStateDecoder.TryDecodePartyState(
                        packet.Payload,
                        out nextPartyState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup party packet.");
                }

                receipt.partyState = nextPartyState;
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Party(nextPartyState));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.FamilyStashSnapshot)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.FamilyStash(ReadFamilyStashState(packet.Payload)));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.SystemRequests)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Social(RetainSocialNotice(packet)));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.WarehouseSnapshot || packet.Opcode == ProtocolOpcodes.WarehouseDelta)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Warehouse(packet));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.TradeOpen || packet.Opcode == ProtocolOpcodes.TradeState || packet.Opcode == ProtocolOpcodes.TradeEnd)
            {
                // Successful trade receipts never enter startup. Retain only this failed
                // decode marker at the existing startup failure position; no owner replay.
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Trade(packet));
                return null;
            }

            if (packet.Opcode == 0x803b)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.RackGoods(ReadRackShopGoods(packet.Payload)));
                return null;
            }

            if (packet.Opcode == 0x8059)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.RackFitting(ReadRackShopFittingResponse(packet.Payload)));
                return null;
            }

            if (packet.Opcode == 0x8042)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.RackDeposit(ReadRackShopDepositInfo(packet.Payload)));
                return null;
            }

            if (packet.Opcode == 0x806c)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.RackRefunds(ReadRackShopRefunds(packet.Payload)));
                return null;
            }

            if (packet.Opcode == 0x806e)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.RackFittingDescriptions(ReadRackShopFittingDescriptions(packet.Payload)));
                return null;
            }

            if (packet.Opcode == 0x8073)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.ChangeSet(ReadChangeSet(packet.Payload)));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.FriendList)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Friend(ReadFriendState(packet.Payload)));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.ApprenticeSnapshot)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Apprentice(ReadApprenticeState(packet.Payload)));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.BlockList)
            {
                receipt.remoteWorldStates.Add(PendingRemoteWorldState.Block(ReadBlockState(packet.Payload)));
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.AccountSecuritySeed)
            {
                PublishAccountSecuritySeed(packet);
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.TaskSnapshot)
            {
                WorldTaskLogState nextTaskLogState;
                if (!PrivateServerWorldStateDecoder.TryDecodeTaskLog(
                        packet.Payload,
                        out nextTaskLogState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup task-log packet.");
                }

                // Official 0x8027 tail-calls TaskClient.SetState (0x06000e5d) in the case body,
                // so a packet read during session entry replaces the active and done collections
                // here and not later, when the world frame may still be unbuilt - and it is
                // applied even if a second 0x8027 follows in the same window, because every
                // packet is one whole replacement plus one OnStateChanged call.
                await ReceiveTaskLogOnHostAsync(
                        nextTaskLogState,
                        cancellationToken)
                    .ConfigureAwait(false);
                receipt.taskLogState = nextTaskLogState;
                return null;
            }

            if (packet.Opcode == ProtocolOpcodes.NpcChatter)
            {
                WorldNpcChatterState nextNpcChatterState;
                if (!PrivateServerWorldStateDecoder.TryDecodeNpcChatter(
                        packet.Payload,
                        out nextNpcChatterState))
                {
                    throw new InvalidDataException(
                        "The server sent an invalid startup NPC chatter packet.");
                }

                // Official 0x804b tail-calls NpcChatter.SetServerChatter in the case body, so a
                // packet read during session entry replaces the owner here and not later, when
                // the world frame may still be unbuilt - and it is applied even if a second
                // 0x804b follows in the same window, because SetServerChatter only overwrites a
                // timing for a positive value and the arrival order is what decides the result.
                await ReceiveNpcChatterOnHostAsync(
                        nextNpcChatterState,
                        cancellationToken)
                    .ConfigureAwait(false);
                receipt.npcChatterState = nextNpcChatterState;
                return null;
            }

            LogUnexpectedDownlink(
                "enter-world",
                packet,
                ProtocolOpcodes.SelfData);
        }
        return null;
    }

    /// <summary>
    /// Official 0x8044 step 2, run on the host thread when the caller is not the host.
    /// </summary>
    /// <remarks>
    /// The official case is a single unconditional store into the int32 static at
    /// CombatWorld +0x8 (arm64 0x25419c / x86_64 0x26341b) and the official client performs it
    /// on its one receive thread. The recovered reader that reaches this point is a task
    /// continuation instead, so the store is handed to the host executor: on the host it runs
    /// inline, off the host it is posted once and awaited. No lock is added and the host is
    /// never blocked - the awaited task reports the real outcome, including a cancellation that
    /// arrived before the write started.
    /// </remarks>
    private Task ReceiveLocalFactionOnHostAsync(
        WorldLocalFactionState state,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.ReceiveLocalFaction(state), cancellationToken);
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld title store, on the host thread when the caller is not.
    /// </summary>
    /// <remarks>
    /// The official store is unconditional and happens while case 0x8003 is dispatched, so the
    /// owner holds the entry title before any later 0x804c replaces it. This reader is a task
    /// continuation instead, so the store is handed to the host executor: one post, awaited,
    /// with the real outcome reported back and no lock added.
    /// </remarks>
    private Task ReceiveEntryTitleOnHostAsync(
        string title,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.SetCurrentTitle(title ?? string.Empty),
            cancellationToken);
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld entry store of <c>CharData.lastLogin</c>, on the host
    /// thread: one post, awaited, one <c>DateTime.Now</c> read and one string store.
    /// </summary>
    /// <remarks>
    /// The official callback runs while case 0x8003 is dispatched and stores local wall-clock
    /// time formatted as <c>yyyy-MM-dd HH:mm</c> (arm64 0x391e7c-0x391eb8 / x86_64
    /// 0x3a50f3-0x3a513f). It carries no packet argument, so nothing is read from the wire and
    /// there is nothing to normalize: the product's own shape of the same store lives in the
    /// owner so the presentation-side <c>CharData</c> stays the single owner of the static. No
    /// cache and no per-frame refresh are introduced - one store per entry, exactly like the
    /// official one, which is why the step is awaited here instead of being pushed into a frame
    /// update.
    /// </remarks>
    private Task ReceiveEntryLastLoginOnHostAsync(CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.ReceiveEntryLastLogin(), cancellationToken);
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld identity stores of <c>CharData.name</c> (+0x0) and
    /// <c>CharData.charId</c> (+0x8), on the host thread, immediately before this entry's
    /// <c>CharData.LoadNameMode()</c> step.
    /// </summary>
    /// <remarks>
    /// The official body performs both stores and then the zero-argument preference call with
    /// nothing in between (arm64 export lines 102/109/110, x86_64 lines 108/114/116), so one
    /// host action carries the two stores and the caller awaits it before posting the
    /// LoadNameMode step. <paramref name="characterId"/> is the entry's own persistent id -
    /// <c>WorldEntryRecord.PersistentCharacterId</c>, the I64 tail case 0x8003 reads at arm64
    /// 0x250948 and stores into <c>EnterInfo.charId</c> at 0x2585c8, or zero when that tail was
    /// absent (arm64 0x2585c4) - and not the network self entity id, which the same case keeps
    /// out of the struct. <paramref name="name"/> is the server display name of this entry, so
    /// the pair is the identity the server confirmed and not the roster row the selection
    /// mirror published before the request.
    /// </remarks>
    private Task ReceiveEntryIdentityOnHostAsync(
        string name,
        long characterId,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(
            () => owner.ReceiveEntryIdentity(name, characterId),
            cancellationToken);
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld call to <c>CharData.LoadNameMode()</c> (0x06000f2b), on
    /// the host thread, before this entry's own attribute steps.
    /// </summary>
    /// <remarks>
    /// The official call sits between the <c>CharData.charId</c> and <c>CharData.attrs</c> stores
    /// (arm64 0x391cd8 / x86_64 0x3a506b) and it is the only caller of 0x06000f2b the frozen
    /// index has, so it is one entry-time call and not a per-frame preference load: the callee
    /// itself refuses to re-read the preference while the derived suffix is unchanged. The key
    /// is derived inside the callee from <c>CharData.PrefSuffix</c>, so this step passes no
    /// argument and invents no name - it reads whatever identity
    /// <see cref="ReceiveEntryIdentityOnHostAsync"/> has just published, which is
    /// <c>namemode_&lt;charId&gt;</c> whenever this entry carried a persistent id and
    /// <c>namemode_&lt;server name&gt;</c> when it did not.
    /// </remarks>
    private Task LoadEntryNameModeOnHostAsync(CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.LoadEntryNameMode(), cancellationToken);
    }

    /// <summary>
    /// One official attribute-array store step run on the host thread when the caller is not the
    /// host: the 0x8003 enter callback's one store, and one store per startup 0x8007 packet that
    /// arrives before SelfData.
    /// </summary>
    /// <remarks>
    /// The official stores happen while the case is dispatched, before any frame runs, and the
    /// product frames are never guaranteed to reach the local-movement branch - a dead, mounted
    /// or working local player advances without it - so the arrays have to be published here.
    /// Startup runs on the continuation of the enter-world SendAsync, which is not the host
    /// thread, while Pump drives the drain and the recovered windows read the same statics on the
    /// host; the official write step therefore goes through the host executor: one post per
    /// packet, in arrival order, awaited, with the real outcome reported back and no lock added.
    /// A startup 0x8007 is stored even though the mirror status root does not exist yet - the
    /// mirror is a separate immutable projection and its absence says nothing about the real
    /// statics - and the arguments are the packet's own write intent, so an empty second array
    /// arrives as null and performs no +0xb8 store.
    /// <para>
    /// <paramref name="inventoryWeightLimit"/> is the same write intent for the one store of the
    /// case that leaves the CharData block: element 23 of the first array into
    /// <c>InventoryDlg.MaxWeight</c>, already gated on the packet's own first-array length, and
    /// null when this packet performs no such store. The 0x8003 enter root passes null because
    /// its case body performs no element-23 store at all - its array reaches
    /// <c>CharData.attrs</c> only through TitleFlow.DoEnterWorld (0x06000efd), whose body never
    /// loads the InventoryDlg metadata slot (arm64 0x148b4e8 / x86_64 0x147f960) and stores
    /// nothing at +0x24. The value is applied inside the same host action as the two array
    /// stores, after them, which is the official order inside the case body.
    /// </para>
    /// <para>
    /// <paramref name="storeAttributeFields"/> is the same write intent for the seven named
    /// scalars the 0x8007 case body stores from its first array: true for a real 0x8007, whose
    /// case performs those stores itself (arm64 0x250c9c-0x250d1c / x86_64 0x25f47c, after the
    /// two array stores and before the element-23 store), and false for the 0x8003 enter root,
    /// whose seven stores belong to TitleFlow.DoEnterWorld (0x06000efd) and have their own
    /// short-array shape - the enter root therefore awaits
    /// <see cref="ReceiveEnterAttributeFieldsOnHostAsync"/> instead and never reaches this
    /// parameter. Both write intents are the same one owner, so the seven fields have a single
    /// writing place and no per-frame writer is added next to it. The same true intent also
    /// permits the four ring-field stores after the weight-limit step; the enter callback's
    /// false intent never writes those 0x8007-only fields.
    /// </para>
    /// </remarks>
    private Task ReceivePlayerAttributesOnHostAsync(
        IReadOnlyList<int> attributes,
        IReadOnlyList<int> attributeTotals,
        int? inventoryWeightLimit,
        bool storeAttributeFields,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(
            () =>
            {
                owner.ReceivePlayerAttributes(attributes, attributeTotals);
                if (storeAttributeFields)
                {
                    owner.ReceivePlayerAttributeFields(attributes);
                }

                if (inventoryWeightLimit.HasValue)
                {
                    owner.ReceiveInventoryWeightLimit(inventoryWeightLimit.Value);
                }
                if (storeAttributeFields)
                {
                    owner.ReceivePlayerRingFields(attributes);
                }
            },
            cancellationToken);
    }

    /// <summary>
    /// The seven named scalars of the 0x8003 enter callback, on the host thread, issued after the
    /// one array store of the same callback so that the official order - <c>CharData.attrs</c>
    /// (+0xb0) first, then level/exp/expToNext/hp/maxHp/mp/maxMp - survives into the product.
    /// </summary>
    /// <remarks>
    /// The official writer is TitleFlow.DoEnterWorld (0x06000efd), which runs inside case 0x8003
    /// and stores the seven values from <c>EnterInfo.attrs</c> with its own short-array shape:
    /// a null or empty array performs no store and a length of one to six stores the level only,
    /// which is why this step exists next to the array step instead of inside it. The attributes
    /// are the enter packet's own array - the same instance the array store just published - and
    /// not a merged mirror snapshot, so a later 0x8007 still overwrites these values at its own
    /// arrival exactly as it does officially.
    /// </remarks>
    private Task ReceiveEnterAttributeFieldsOnHostAsync(
        IReadOnlyList<int> attributes,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(
            () => owner.ReceiveEnterAttributeFields(attributes),
            cancellationToken);
    }

    /// <summary>
    /// The seven named scalars the official enter gate can skip, read once through the receive port
    /// at the moment that gate runs.
    /// </summary>
    /// <remarks>
    /// The read runs inside one host action because every write of those statics already does, and
    /// it is issued before this entry's own host steps, so it observes the pre-entry content. It is
    /// a read only: no store, no copy kept in the gateway, and nothing to dispose - the port answers
    /// from the same statics the official callback writes, so a missing owner yields a null result,
    /// the six keepers fall back to the zero the official statics start from and level keeps the
    /// roster carrier, rather than to a value the gateway invented.
    /// </remarks>
    private async Task<WorldEntryRetainedScalars> ReadEntryRetainedScalarsAsync(
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return null;
        }

        WorldEntryRetainedScalars retained = null;
        await _host.RunAsync(
                () => retained = owner.ReadEntryRetainedScalars(),
                cancellationToken)
            .ConfigureAwait(false);
        return retained;
    }

    /// <summary>
    /// The 0x8006 / 0x8069 Money (+0x10), CaiYu (+0x18), Weight (+0x20) and MaxWeight (+0x24)
    /// stores followed by the same case body's CharData.honor (+0x70) store, issued on the host
    /// thread when the packet is decoded somewhere else: the startup 0x8006 that arrives before
    /// SelfData. One post per packet, in arrival order, awaited, with no lock added - the same
    /// shape the startup 0x8007 attribute arm uses, and the reason is the same: all five stores
    /// belong to the case body of the packet that carried the values, so they have to keep that
    /// packet's position in the arrival order instead of being folded into one write after the
    /// loop.
    /// </summary>
    private Task ReceiveInventoryHeaderOnHostAsync(
        long money,
        long caiYu,
        int weight,
        int maximumWeight,
        long honor,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(
            () => owner.ReceiveInventoryHeader(money, caiYu, weight, maximumWeight, honor),
            cancellationToken);
    }

    /// <summary>
    /// Host fallback for the official 0x8015 tip evaluation when receipt did not handle it.
    /// </summary>
    /// <remarks>
    /// The official case evaluates <c>NewbieTipBox.Push(7)</c> (0x060008d8) for a nonempty
    /// dictionary with no world or map gate. Contextual receipt already does that synchronously;
    /// the startup caller invokes this helper only without successful tip provenance. The host
    /// executor runs inline on the host or posts otherwise, and the reader awaits its real
    /// outcome before reading another packet itself. That await does not stop the separate
    /// transport pump. State was already applied/published at dispatch; this fallback cannot
    /// write it again, even when a newer receipt arrived during the awaited host step.
    /// Already-handled packets skip the redundant await and its scheduling/cancellation
    /// checkpoint; no replacement host hop or cross-packet deduplication is introduced.
    /// </remarks>
    private Task ReceiveSkillBookTipOnHostAsync(
        WorldSkillBookObservation state,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() =>
        {
            if (owner is ISkillBookStreamReceiveOwner nativeOwner)
                nativeOwner.ReceiveNewbieSkillBookTip(state);
            else
                owner.ReceiveNewbieSkillBookTip(state.ToSnapshot());
        }, cancellationToken);
    }

    /// <summary>
    /// Host fallback for the official 0x800b tip evaluations when receipt did not handle them.
    /// </summary>
    /// <remarks>
    /// The official case searches this packet's skills for <c>NewbieTipBox.Push(51)</c> and then
    /// <c>NewbieTipBox.Push(52)</c> (0x060008d8), without a world or map gate. Contextual receipt
    /// already performs those searches synchronously; the startup caller invokes this helper
    /// only when packet.PetStatus is null. The host executor runs inline on the host or posts
    /// otherwise, and the reader awaits its real outcome before reading another packet itself.
    /// That await does not stop the separate transport pump. Optional-state retention and
    /// snapshot publication stay independent of this fallback. Already-handled packets skip
    /// the redundant await and its scheduling/cancellation checkpoint; no replacement host
    /// hop or cross-packet deduplication is introduced.
    /// </remarks>
    private Task ReceiveNewbiePetSkillsTipOnHostAsync(
        WorldPlayerPetStatusState pet,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.ReceiveNewbiePetSkillsTip(pet), cancellationToken);
    }

    /// <summary>
    /// Official 0x8054 tail call, run on the host thread when the caller is not the host.
    /// </summary>
    /// <remarks>
    /// The official case tail-calls <c>NewbieTipBox.SeedServer</c> (0x060008d7) while it is
    /// dispatched - once per received packet, with no count, world, map or in-world gate - so a
    /// 0x8054 read before SelfData seeds at the moment it is read. This startup reader is a task
    /// continuation and not the host thread, while <c>SeedServer</c> resolves and writes the
    /// role-scoped PlayerPrefs, which belongs to the host; the step therefore goes through the host
    /// executor instead of running inline. One post per packet, in the order the packets were read,
    /// awaited, with the real outcome reported back and no lock, queue or second driver added. The
    /// list handed over is the one this packet's own count and rows produced: a truncated payload
    /// raises from the same <c>WireReader</c> the in-world 0x8054 arm already uses, before any
    /// <c>SeedServer</c> call, so a malformed packet leaves the previously seen set untouched and
    /// never seeds a partial list - the bounded intermediate state is the packets already applied
    /// in this window, and no second decoder is added for it. The in-world 0x8054 consumer is
    /// untouched, so an in-world packet still seeds exactly once through its own context.
    /// </remarks>
    private Task ReceiveNewbieTipSeedOnHostAsync(
        List<int> ids,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.ReceiveNewbieTipSeed(ids), cancellationToken);
    }

    /// <summary>
    /// Official 0x804b tail call, run on the host thread when the caller is not the host.
    /// </summary>
    /// <remarks>
    /// The official case tail-calls NpcChatter.SetServerChatter (0x0600090f) on its one receive
    /// thread, once per packet and with no gate, so a 0x804b read before SelfData is applied at
    /// the moment it is read and not when the world frame later happens to be built. This reader
    /// runs on the continuation of the enter-world SendAsync, which is not the host thread,
    /// while the recovered windows enumerate the same chatter statics from the host; the write
    /// therefore goes through the host executor instead of running inline. One post per packet and
    /// one post per whole replacement - the clear and every add stay inside the single owner call
    /// - in the order the packets were read, with the awaited task carrying the real outcome
    /// back, so two 0x804b in the same frame are applied twice and in arrival order.
    /// </remarks>
    private Task ReceiveNpcChatterOnHostAsync(
        WorldNpcChatterState state,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.ReceiveNpcChatter(state), cancellationToken);
    }

    /// <summary>
    /// Official 0x8027, run on the host thread when the caller is not the host.
    /// </summary>
    /// <remarks>
    /// The official case builds every row and then tail-calls <c>TaskClient.SetState</c>
    /// (0x06000e5d) on its one receive thread. The recovered startup reader is a task
    /// continuation instead, so the whole replacement is handed to the host executor: on the
    /// host it runs inline, off the host it is posted once and awaited. One action holds the
    /// clear, every add and the callback, because splitting them would make the owner
    /// observable with <c>Active</c> emptied but not refilled.
    /// </remarks>
    private Task ReceiveTaskLogOnHostAsync(
        WorldTaskLogState state,
        CancellationToken cancellationToken)
    {
        IWorldReceiveOwner owner = _worldReceiveOwner;
        if (owner == null)
        {
            return Task.CompletedTask;
        }

        return _host.RunAsync(() => owner.ReceiveTaskLog(state), cancellationToken);
    }

    /// <summary>
    /// Runs the original Buff read and writes in one existing host action. The retained startup
    /// carrier stores the existing payload and the all-row Empty identity flag, not a DTO.
    /// Snapshot construction belongs to explicit observation after its original publication point.
    /// </summary>
    private async Task<bool> ReceiveBuffStateOnHostAsync(
        byte[] payload,
        CancellationToken cancellationToken)
    {
        // The native pass has one Buf, one map read and one clock read per row. A later
        // explicit observer may read the stable payload again without native writes or clocks.
        bool nonEmpty = false;
        await _host.RunAsync(
                () => nonEmpty = ReadNativeBuffState(payload), cancellationToken)
            .ConfigureAwait(false);
        return nonEmpty;
    }

    /// <summary>
    /// The single host clock the official 0x8017 row stamp reads
    /// (<c>UnityEngine.Time.realtimeSinceStartup</c>). It is the same provider the composition
    /// root already hands the recovered NetClient rate limiter, so Protocol still holds no Unity
    /// reference of its own.
    /// </summary>
    private float HostRealtimeSeconds()
    {
        return _realtimeSecondsProvider();
    }

    /// <summary>
    /// The single host answer official 0x8017 needs before its row loop -
    /// <c>CustomMaps.IsCustom(GameBootstrap.CurMapId)</c>, GameBootstrap field token 67110216 and
    /// <c>CustomMaps.IsCustom</c> (0x060001d0). Both types live in Presentation, so the caller of
    /// this bridge stays Protocol-clean and the real reading happens in the bound implementation.
    /// </summary>
    /// <remarks>
    /// The probe is evaluated at the point this method is called - once per packet, inside the
    /// host action that owns the read - and not at construction time, so the map id that decides
    /// the drop test is always the one the host holds at that moment. There is deliberately no
    /// default value for an unbound probe: substituting <c>false</c> would assert "not a custom
    /// map" with no native backing, so a gateway built without the probe fails here instead.
    /// </remarks>
    private bool HostIsCustomMap()
    {
        if (_customMapProvider == null)
        {
            throw new InvalidOperationException(
                "The 0x8017 map classification has no bound host provider; the composition root " +
                "must bind CustomMaps.IsCustom(GameBootstrap.CurMapId).");
        }

        return _customMapProvider();
    }

    /// <summary>
    /// Official 0x804c steps 3 to 5 plus the publication, in one host action.
    /// </summary>
    /// <remarks>
    /// The official case empties the owned list, appends one entry per read and stores the
    /// current title inside one case body (arm64 0x254534 / x86_64 0x26387a). Those steps must
    /// not be split across several posts - the owner would then be observable with the list
    /// emptied but not refilled - so the whole sequence is one action, exactly as it is on the
    /// in-world path, and a primitive read error keeps propagating out of it after the entries
    /// that were already appended have stayed in the owner.
    /// </remarks>
    private Task ReceiveCharacterTitleOnHostAsync(
        byte[] payload,
        CancellationToken cancellationToken)
    {
        return _host.RunAsync(
            () =>
            {
                WorldCharacterTitleState next =
                    PrivateServerCharacterTitleCodec.Decode(payload, _worldReceiveOwner);
                PublishCharacterTitleState(next, cancellationToken);
            },
            cancellationToken);
    }

    /// <summary>
    /// Official 1.5.8 <c>NetClient.Dispatch</c> (0x0600077f) case 0x8001, the reply to the client's
    /// own 0x1001 ping, in the official order and with the official arithmetic:
    /// <c>Buf.I64()</c> (0x06000796) is read first and without any length gate - arm64 0x24be24
    /// <c>bl NetClient+Buf.I64</c> / x86_64 0x2596b8 - then <c>Environment.get_TickCount</c> is
    /// called and the two are subtracted as 32-bit integers - arm64 0x24be30
    /// <c>bl Environment.get_TickCount</c> / 0x24be34 <c>sub w19,w0,w19</c>, x86_64 0x2596c2 /
    /// 0x2596c9 <c>subl %r14d,%ebx</c> - so the low 32 bits of the I64 are what takes part in the
    /// subtraction and no 64-bit difference is ever formed.
    /// </summary>
    /// <remarks>
    /// The gate is one unsigned comparison and it is identical on both architectures: arm64
    /// 0x24be38 <c>lsr w8,w19,#5</c> / 0x24be3c <c>cmp w8,#0x752</c> / <c>b.hi</c> accepts
    /// <c>(uint)delta &gt;&gt; 5 &lt;= 1874</c> and x86_64 0x2596cc <c>cmpl $0xea5f,%ebx</c> /
    /// 0x2596d2 <c>ja</c> accepts <c>(uint)delta &lt;= 59999</c>; those are the same range, so a
    /// negative delta (a reply whose tick is ahead of the local one) and anything above 59999 ms
    /// is dropped and the previous value of the owner stays in place. The two architectures then
    /// store the accepted value into the same static - arm64 0x24be60 <c>ldr x8,[x0,#0xb8]</c> /
    /// 0x24be64 <c>str w19,[x8,#0x10]</c>, x86_64 0x2596f3 <c>movq 0xb8(%rdi),%rax</c> /
    /// 0x2596fa <c>movl %ebx,0x10(%rax)</c> - which is the only store to that slot in the frozen
    /// Dispatch body.
    /// Nothing here measures time of its own, starts a second ping or changes the existing ping
    /// cadence: the value is whatever the wire and the local tick counter produced, and a server
    /// that never answers 0x1001 leaves the owner at the -1 the class constructor wrote.
    /// </remarks>
    private void ReceiveRoundTripLatency(byte[] payload)
    {
        long sentTick = new NetClient.Buf(payload).I64();
        int latencyMs = unchecked(Environment.TickCount - (int)sentTick);
        if (unchecked((uint)latencyMs) > MaxRoundTripMilliseconds)
        {
            return;
        }

        _worldReceiveOwner?.ReceiveRoundTripLatency(latencyMs);
    }

    /// <summary>
    /// Official 1.5.8 <c>NetClient.Dispatch</c> (0x0600077f) case 0x805b, the reply to the
    /// client's own 0x107d <c>SrvStatReq</c> (0x060006d5): one <c>Buf.U16()</c> (0x06000793,
    /// arm64 0x24ccdc-0x24cce0 <c>mov x1,#0</c> / <c>bl 0x26b690</c>, x86_64 0x25a6db-0x25a6e0
    /// <c>xorl %esi,%esi</c> / <c>callq 0x27ad70</c>) is read after the null-Buf raise - arm64
    /// 0x24ccd8 <c>cbz x0,0x25c960</c>, x86_64 0x25a6d5 <c>je 0x26b60d</c> - and stored into the
    /// <c>NetClient.OnlineCount</c> static (System.Int32, atlas static offset 20): arm64 0x24cd10
    /// <c>str w0,[x8,#0x14]</c>, x86_64 0x25a70d <c>movl %eax,0x14(%rcx)</c>.
    /// </summary>
    /// <remarks>
    /// The case carries no length, stage, world or map gate, so the reply lands wherever the
    /// transport is running. Nothing here re-requests, caches or re-reads the count: the store to
    /// static offset 0x14 occurs exactly once in the whole Dispatch body on each architecture
    /// (the 123 KB x86_64 sweep 0x257ab0-0x2709c0 and the arm64 sweep of the same body both find
    /// a single instruction), so this is the only receive channel for 0x805b and the value reaches
    /// the owner exactly once per packet.
    /// <para>
    /// Cadence: the official five-second arm that produces those packets lives in
    /// <c>TitleFlow.DrawServer</c> (0x06000ef4) - arm64 0x3873e4 / 0x3873ec / 0x3873f0 then
    /// 0x38759c-0x3875a4, x86_64 0x39a288 / 0x39a291 / 0x39a298 then 0x39a324-0x39a330. The
    /// product carries that cadence in the recovered title surface, whose five-second arm is the
    /// only producer of the RefreshStatus action, so this arm receives a count once per window
    /// while that surface is drawn. The official gate is <c>NetClient.I != null &amp;&amp;
    /// NetClient.I.Connected</c> (0x060006b7); the product forwards the same property value, and
    /// the product-only <c>_serverStatusBusy</c> / stage / state doors still apply on top.
    /// </para>
    /// </remarks>
    private void ReceiveOnlineCount(byte[] payload)
    {
        _worldReceiveOwner?.ReceiveOnlineCount(new NetClient.Buf(payload).U16());
    }

    private void Dispatch(ushort opcode, byte[] payload)
    {
        if (TryReceiveEndgamePacket(opcode, payload)) return;
        if (TryReceiveCombatEntityTailPacket(opcode, payload)) return;
        if (TryReceiveCombatRegistryPacket(opcode, payload)) return;
        if (TryReceiveRemotePlayerTailPacket(opcode, payload)) return;
        if (TryReceiveCombatBubblePacket(opcode, payload)) return;
        if (TryReceiveWardrobePacket(opcode, payload, _worldReceiveOwner as IWardrobeReceiveOwner))
            return;
        if (TryReceiveWorldEntryWithoutWaiter(opcode, payload)) return;
        if (TryReceiveCharacterListPacket(opcode, payload)) return;
        if (TryReceiveTitleResultPacket(opcode, payload)) return;
        if (TryReceiveAccountResult(opcode, payload)) return;
        if (TryReceiveInspectedEquipmentPacket(opcode, payload, _worldReceiveOwner as IInspectedEquipmentReceiptOwner))
            return;
        if (TryReceiveNetworkNpcPacket(opcode, payload)) return;
        if (TryReceiveTargetStatusPacket(opcode, payload, _worldReceiveOwner as ITargetStatusReceiveOwner))
            return;
        if (TryReceiveSkillCastPacket(opcode, payload, SkillCastReceiverAtReceipt(null)))
            return;
        if (opcode == ProtocolOpcodes.DuelHud)
        {
            ReceiveDuelHud(payload);
            return;
        }
        if (opcode == ProtocolOpcodes.DuelList)
        {
            ReceiveDuelList(payload);
            return;
        }
        if (TryReceiveFamilyPacket(opcode, payload, _worldReceiveOwner as IFamilyReceiptOwner))
            return;
        if (TryReceiveMailPacket(opcode, payload, _worldReceiveOwner as IMailReceiveOwner))
            return;
        if (TryReceiveRackShopPacket(opcode, payload, _worldReceiveOwner as IRackShopReceiveOwner))
            return;
        if (TryReceiveHotbarPacket(opcode, payload, _worldReceiveOwner as IHotbarReceiveOwner))
            return;
        if (TryReceiveNpcNoticePacket(opcode, payload, _worldReceiveOwner as INpcNoticeReceiveOwner))
            return;
        if (opcode == ProtocolOpcodes.PrivateChat || opcode == ProtocolOpcodes.SystemRequests)
        {
            ReceiveSocialNotice(opcode, payload);
            return;
        }
        if (TryReceiveTradePacket(opcode, payload, _worldReceiveOwner as ITradeReceiveOwner))
            return;

        if (opcode == ProtocolOpcodes.SelfPkState)
        {
            ReceiveSelfPkPacket(payload);
            return;
        }

        // Pet-vitals roots are receipt-owned in both Pump forms and every startup stage.
        if (opcode == ProtocolOpcodes.PetVitals)
        {
            if (!ReceivePetVitals(payload))
                DispatchPacket(new ProtocolPacket(opcode, payload));
            return;
        }
        if (opcode == ProtocolOpcodes.PetSnapshot)
        {
            var petOwner = _worldReceiveOwner as IWorldPetSnapshotReceiveOwner;
            WorldPlayerPetStatusState pet = petOwner != null
                ? ReceivePetSnapshot(payload, petOwner, false)
                : ReceivePetSnapshotVitals(payload);
            // Keep the existing context-free startup tip fallback provenance. Real owner reads
            // now propagate directly with their native reached prefix; custom adapters retain
            // their existing null-result boundary.
            DispatchPacket(new ProtocolPacket(opcode, payload, receivedPetStatus: pet));
            return;
        }

        // Official 0x804a updates the gather owner at dispatch time, including
        // before world construction. Do not defer it into the world-stage queue.
        if (opcode == 0x804a)
        {
            ReceiveGatherSpots(payload);
            return;
        }

        if (opcode == ProtocolOpcodes.SpawnOnEffect)
        {
            // Official 1.5.8 case 0x800d (arm64 0x2514f0 / x86_64 0x260132): U8 then Str,
            // tail-call SpawnOn (060002a6) with both values unchanged. Both Pump paths reach
            // this host-thread entry before the stage-dependent arrival FIFO, so the owner
            // observes the current transform/riding state at this packet, never at a later
            // window tick. No WF predicate, target clamp, trailing read or queued replay.
            NetClient.Buf reader = new NetClient.Buf(payload);
            int target = reader.U8();
            string effectName = reader.Str();
            _worldReceiveOwner.ReceiveSpawnOnEffect(target, effectName);
            return;
        }

        if (opcode == ProtocolOpcodes.SkillSnapshot)
        {
            WorldSkillBookObservation skills = ReceiveSkillBookObservation(payload);
            // Preserve null tip provenance on the context-free route, including successful
            // reads. Malformed reads remain queued for the existing startup/world error path.
            DispatchPacket(new ProtocolPacket(opcode, payload, receivedSkillBookObservation: skills));
            return;
        }

        if (TryReceiveStaticSnapshotPacket(opcode, payload)) return;
        if (TryReceiveTaskPacket(opcode, payload)) return;

        // These restored static-owner cases have no session/world admission. Run their
        // existing bodies at this receipt, before an earlier response reader can retain
        // them. Consumed packets never enter the selected collector or replay at SelfData.
        if ((_worldReceiveOwner != null && opcode == ProtocolOpcodes.AttributeSnapshot) ||
            opcode == ProtocolOpcodes.CharacterTitleSnapshot ||
            opcode == ProtocolOpcodes.LocalFaction ||
            opcode == ProtocolOpcodes.BuffSnapshot ||
            opcode == ProtocolOpcodes.TaskSnapshot ||
            opcode == ProtocolOpcodes.NpcTaskOffers ||
            opcode == ProtocolOpcodes.NpcChatter ||
            opcode == 0x8063)
        {
            ProcessWorldPacket(new ProtocolPacket(opcode, payload), CancellationToken.None);
            return;
        }

        DispatchPacket(new ProtocolPacket(opcode, payload));
    }

    private void OnTransportError(string stableReason)
    {
        _diagnosticSink?.Invoke("protocol.transport.drop reason=" + stableReason);
        // Reached from NetClient.Update, which the Unity frame loop drives through Pump, so
        // this already is the host thread and the core runs inline.
        DisposeConnectionCore();
    }

    private async Task<ProtocolPacket> ReadPacketAsync(CancellationToken cancellationToken)
    {
        using (CancellationTokenSource timeout = CreateResponseTimeout(cancellationToken))
        {
            return await ReadPacketCoreAsync(timeout.Token);
        }
    }

    private async Task<ProtocolPacket> ReadPacketCoreAsync(CancellationToken cancellationToken)
    {
        while (true)
        {
            await _dispatchedReady.WaitAsync(cancellationToken);
            if (_dispatchedDownlinks.TryDequeue(out ProtocolPacket packet))
            {
                return packet;
            }
        }
    }

    private void ProcessWorldPacket(ProtocolPacket packet, CancellationToken cancellationToken)
    {
        if (packet.Opcode == ProtocolOpcodes.AttributeSnapshot)
        {
            WorldPlayerAttributeSnapshot next;
            if (!PrivateServerWorldStateDecoder.TryDecodePlayerAttributeSnapshot(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid player attribute packet.");
            }

            PublishPlayerAttributeSnapshot(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.CharacterTitleSnapshot)
        {
            // Official 0x804c empties CharData.ownedTitles, appends one entry per read and only
            // then stores CharData.title, all inside the case body. Decode runs that sequence
            // against the owner itself, so a packet that fails after the clear or after the
            // k-th append leaves exactly those entries in the owner and the primitive read
            // error keeps propagating; nothing here rolls the packet back.
            WorldCharacterTitleState next =
                PrivateServerCharacterTitleCodec.Decode(packet.Payload, _worldReceiveOwner);

            PublishCharacterTitleState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.PlayerRename)
        {
            if (!PrivateServerWorldStateDecoder.TryDecodePlayerRename(packet.Payload, out var rename))
                throw new InvalidDataException("The server sent an invalid player-rename packet.");
            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                PublishPlayerRenameLocked(rename, _currentWorldEntityId);
            }
        }
        else if (packet.Opcode == ProtocolOpcodes.PlayerAppearance)
        {
            WorldPlayerAppearanceState next;
            if (!PrivateServerWorldStateDecoder.TryDecodePlayerAppearance(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid player-appearance packet.");
            }

            PublishPlayerAppearanceState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.Teleport)
        {
            WorldTeleportState next;
            if (!PrivateServerTeleportCodec.TryDecode(packet.Payload, out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid teleport packet.");
            }

            PublishTeleportState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.PetSnapshot)
        {
            WorldPlayerPetStatusState next = packet.ReceivedPetStatus;
            if (next == null)
            {
                throw new InvalidDataException(
                    "The server sent an invalid player pet packet.");
            }

            PublishPlayerPetStatus(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.PetVitals)
        {
            // Only an explicitly supplied raw compatibility envelope reaches this branch.
            // Live failed receipts now propagate before retaining any packet.
            throw new InvalidDataException("The server sent an invalid pet vitals packet.");
        }
        else if (packet.Opcode == ProtocolOpcodes.SelfPkState)
        {
            // Raw compatibility envelopes reuse the same authority; live receipts never queue.
            ReceiveSelfPkPacket(packet.Payload);
        }
        else if (packet.Opcode == ProtocolOpcodes.LocalFaction)
        {
            WorldLocalFactionState next;
            PrivateServerWorldStateDecoder.DecodeLocalFactionState(
                    packet.Payload,
                    out next);

            // Official 0x8044 stores its I32 into CombatWorld.LocalFaction with no world-frame
            // gate (arm64 0x25419c str w19,[x8,#0x8] / x86_64 0x263422 movl %ebx,0x8(%rax)),
            // so the write happens on receipt whether or not the world window is already built:
            // this arm is reached from the in-world drain and from the retained FIFO drain that
            // runs at world entry, both of which precede any built projection.
            _worldReceiveOwner?.ReceiveLocalFaction(next);
            PublishLocalFactionState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.SelfRidingState)
        {
            WorldRidingState current = Volatile.Read(ref _localRidingState);
            WorldRidingState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeLocalRidingState(
                    packet.Payload,
                    current,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid self-riding state packet.");
            }

            lock (_worldStateSync)
            {
                cancellationToken.ThrowIfCancellationRequested();
                Volatile.Write(ref _localRidingState, next);
                Interlocked.Increment(ref _localRidingStateVersion);
            }
        }
        else if (packet.Opcode == ProtocolOpcodes.RemoteRidingState)
        {
            WorldRemoteRidingState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeRemoteRidingState(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid remote-riding state packet.");
            }

            PublishRemoteRidingState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.NpcChatter)
        {
            WorldNpcChatterState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeNpcChatter(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid NPC chatter packet.");
            }

            // Official 0x804b tail-calls NpcChatter.SetServerChatter inside the case body, once
            // per packet and with no world-frame gate, so the replacement runs here on receipt
            // and not from the per-frame projection. This arm is reached from the in-world drain
            // and from the retained FIFO drain at world entry, both of which run on the host, so
            // the whole table replacement - the clear and every add - happens in one step on the
            // same thread that enumerates the tables. PublishNpcChatterState only keeps the
            // snapshot and the version; it never applies the chatter a second time.
            _worldReceiveOwner?.ReceiveNpcChatter(next);
            PublishNpcChatterState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.TaskSnapshot)
        {
            WorldTaskLogState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeTaskLog(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid task-log packet.");
            }

            // Official 0x8027 tail-calls TaskClient.SetState (0x06000e5d) inside the case body,
            // once per packet and with no world-frame gate: Active and Done are cleared and
            // refilled and OnStateChanged fires at the moment the packet is read, so two 0x8027
            // in the same frame are applied twice and in arrival order. Publication only keeps
            // the snapshot and the version and never applies the state a second time.
            _worldReceiveOwner?.ReceiveTaskLog(next);
            PublishTaskLogState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.NpcTaskOffers)
        {
            WorldNpcTaskOffersState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeNpcTaskOffers(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid NPC task-offers packet.");
            }

            // Official 0x8028 tail-calls TaskClient.SetOffers (0x06000e5e) inside the case body,
            // once per packet and with no world-frame or visibility gate: the store of the NPC
            // name, the clear, the refills and the OnOffersChanged callback all run at the moment
            // the packet is read, so two 0x8028 in the same frame are applied twice and in
            // arrival order. Publishing only keeps the snapshot and the version and never applies
            // the offers a second time, which is why the per-frame projection no longer compares
            // NpcTaskOffersStateVersion before building the menu.
            _worldReceiveOwner?.ReceiveNpcTaskOffers(next);
            PublishNpcTaskOffersState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.ShopItems)
        {
            WorldShopState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeShopState(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid shop-items packet.");
            }

            PublishShopState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.TransportList)
        {
            WorldTransportState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeTransportState(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid transport-list packet.");
            }

            PublishTransportState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.WarehouseSnapshot)
        {
            WorldWarehouseState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeWarehouseState(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid warehouse packet.");
            }

            PublishWarehouseState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.WarehouseDelta)
        {
            if (!PrivateServerWorldStateDecoder.TryDecodeWarehouseDelta(packet.Payload, out WorldWarehouseState delta))
                throw new InvalidDataException("The server sent an invalid warehouse delta packet.");
            PublishWarehouseDelta(delta, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.EntitySpawn)
        {
            WorldEntityState next = packet.EntitySpawn;
            if (next == null && !PrivateServerWorldStateDecoder.TryDecodeWorldEntityState(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid world-entity packet.");
            }

            PublishWorldEntityState(next, cancellationToken,
                admittedOnMainThread: packet.EntitySpawn != null);
        }
        else if (packet.Opcode == ProtocolOpcodes.EntityRemove)
        {
            // Official case 0x8010 hands the decoded id and reason straight to
            // CombatWorld.Remove (0x06000144, arm64 0x241888 / x86_64 0x24f0e0); RemoveWorldEntityState
            // keeps the tombstone bookkeeping and the entity event that reaches that owner.
            int entityId;
            byte reason;
            PrivateServerWorldStateDecoder.DecodeWorldEntityRemoval(
                    packet.Payload,
                    out entityId,
                    out reason);

            RemoveWorldEntityState(entityId, cancellationToken, reason);
        }
        else if (packet.Opcode == ProtocolOpcodes.EntityOwner)
        {
            // Official 0x8043 -> CombatWorld.SetOwner (0x06000132). The write only lands on an id
            // CombatWorld.monsters already holds, so it travels as one ordered entity edge and is
            // consumed next to EntitySpawn/EntityRemove
            // (ReferenceFirstWorldInitialFrame.EntityEvents.cs): writing it here would run before
            // a queued or not-yet-applied spawn registers and would hit a removed-but-not-yet-
            // unregistered instance. Two 0x8043 are two edges; nothing deduplicates or caches.
            int entityId;
            int ownerId;
            PrivateServerWorldStateDecoder.DecodeWorldEntityOwner(
                    packet.Payload,
                    out entityId,
                    out ownerId);

            lock (_worldStateSync)
            {
                AppendEntityEventLocked(WorldRemoteEventKind.EntityOwner, entityId,
                    ownerId: ownerId);
            }
        }
        else if (packet.Opcode == ProtocolOpcodes.EntityStatusEffect)
        {
            int entityId;
            WorldEntityStatusEffect effect;
            if (!PrivateServerWorldStateDecoder.TryDecodeEntityStatusEffect(
                    packet.Payload,
                    out entityId,
                    out effect))
            {
                throw new InvalidDataException(
                    "The server sent an invalid entity status-effect packet.");
            }

            PublishWorldEntityStatusEffect(
                entityId,
                effect,
                cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.SkillHitEffect)
        {
            WorldSkillHitEffect effect;
            if (!PrivateServerWorldStateDecoder.TryDecodeSkillHitEffect(
                    packet.Payload,
                    out effect))
            {
                throw new InvalidDataException(
                    "The server sent an invalid skill hit-effect packet.");
            }

            PublishSkillHitEffect(effect, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.EntityStatusIcons)
        {
            int entityId;
            IReadOnlyList<WorldEntityStatusIcon> icons;
            if (!PrivateServerWorldStateDecoder.TryDecodeEntityStatusIcons(
                    packet.Payload,
                    out entityId,
                    out icons))
            {
                throw new InvalidDataException(
                    "The server sent an invalid entity status-icon packet.");
            }

            PublishWorldEntityStatusIcons(
                entityId,
                icons,
                cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.GroundItemSpawn)
        {
            WorldGroundItemState next;
            if (!PrivateServerGroundItemCodec.TryDecodeSpawn(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid ground-item packet.");
            }

            PublishGroundItemState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.GroundItemRemove)
        {
            int groundId;
            if (!PrivateServerGroundItemCodec.TryDecodeRemoval(
                    packet.Payload,
                    out groundId))
            {
                throw new InvalidDataException(
                    "The server sent an invalid ground-item removal packet.");
            }

            RemoveGroundItemState(groundId, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.InspectedPlayerEquipment)
        {
            // Successful packets are consumed exactly once by either host-thread pump.
            // Only failed decodes follow the unchanged startup/world error boundary here.
            throw new InvalidDataException(
                "The server sent an invalid inspected-player equipment packet.");
        }
        else if (packet.Opcode == ProtocolOpcodes.RemotePlayer)
        {
            WorldRemotePlayerState next;
            int mapIdGuard;
            if (!PrivateServerWorldStateDecoder.TryDecodeRemotePlayerState(
                    packet.Payload,
                    out next,
                    out mapIdGuard))
            {
                throw new InvalidDataException(
                    "The server sent an invalid remote-player packet.");
            }

            int currentMapId = Volatile.Read(ref _currentWorldMapId);
            if (mapIdGuard < 1 || currentMapId < 1 || mapIdGuard == currentMapId)
            {
                PublishRemotePlayerState(next, cancellationToken);
            }
        }
        else if (packet.Opcode == ProtocolOpcodes.Emote)
        {
            var reader = new NetClient.Buf(packet.Payload);
            int id = reader.I32();
            int face = reader.I32();
            PublishEmote(id, face, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.EntityBubble)
        {
            // Official 0x8019 (arm64 0x2523a8 / x86_64 0x26146a): one Buf, I32 id then Str text,
            // no Remaining gate, no optional tail, no filter and no try/catch - exactly like the
            // neighbouring 0x8025 Emote case. It is an independent opcode from 0x8039, so this
            // branch never deduplicates against or replaces the channel-chat bubble.
            var reader = new NetClient.Buf(packet.Payload);
            int id = reader.I32();
            string text = reader.Str();
            PublishEntityBubble(id, text, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.ChannelChat)
        {
            PendingChannelChat chat = packet.ChannelChat ?? DecodeChannelChat(packet.Payload, false);
            PublishChannelChat(chat.SenderEntityId, chat.SenderName, chat.BubbleThemeSelector,
                chat.ChannelCode, chat.Text, chat.Shares, cancellationToken, chat.SenderLevel, chat.SenderFlag);
            if (chat.ChannelCode == 0)
                PublishRemoteChatBubble(chat.SenderEntityId, chat.Text, chat.BubbleThemeSelector, cancellationToken, chat.NativeBubbleHandled);
        }
        else if (packet.Opcode == ProtocolOpcodes.PrivateChat)
        {
            DecodedSocialNotice notice = packet.SocialNotice ??
                DecodeSocialNotice(packet.Opcode, packet.Payload, false);
            PublishPrivateChat(notice.SenderName, notice.Text, notice.Shares,
                cancellationToken, packet.SocialNoticeOwnerApplied);
        }
        else if (packet.Opcode == ProtocolOpcodes.RemotePkState)
        {
            int entityId;
            byte pkState;
            PrivateServerWorldStateDecoder.DecodeRemotePkState(
                    packet.Payload,
                    out entityId,
                    out pkState);

            PublishRemotePlayerPkState(
                entityId,
                pkState,
                cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.EntityMovePath)
        {
            WorldRemotePathState next;
            PrivateServerWorldStateDecoder.DecodeRemotePathState(
                    packet.Payload,
                    out next);

            PublishRemotePathState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.CombatState)
        {
            WorldCombatResultState result;
            PrivateServerWorldStateDecoder.DecodeCombatResult(
                    packet.Payload,
                    out result);

            PublishCombatReceive(result, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.CombatNumber)
        {
            WorldCombatNumberState number;
            PrivateServerWorldStateDecoder.DecodeCombatNumber(
                    packet.Payload,
                    out number);

            PublishCombatNumber(number, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.SkillCastState)
        {
            WorldSkillCastState skillCast = packet.ReceivedSkillCast;
            if (skillCast == null)
            {
                throw new InvalidDataException(
                    "The server sent an invalid skill-cast packet.");
            }

            PublishSkillCastState(skillCast, cancellationToken);

            // Ready-world receipt already ran the single cast owner. The retained observation
            // shares its acknowledgement; only never-handled no-world receipts remain eligible
            // for the existing fallback, never a second PlayCast source.
        }
        else if (packet.Opcode == ProtocolOpcodes.PlayerLifeState)
        {
            int entityId;
            bool alive;
            int reviveFlags;
            PrivateServerWorldStateDecoder.DecodePlayerLifeState(
                    packet.Payload,
                    out entityId,
                    out alive,
                    out reviveFlags);

            PublishPlayerLifeState(
                entityId,
                alive,
                reviveFlags,
                cancellationToken, packet.RemoteLifeAtReceive);
        }
        else if (packet.Opcode == ProtocolOpcodes.PartySnapshot)
        {
            WorldPartyState next;
            if (!PrivateServerWorldStateDecoder.TryDecodePartyState(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid party packet.");
            }

            PublishPartyState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.FamilyStashSnapshot)
        {
            PublishFamilyStashState(ReadFamilyStashState(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.HelpTopics)
        {
            PublishHelpTopicsState(ReadHelpTopicsState(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == 0x803b)
        {
            PublishRackShopGoodsResponse(ReadRackShopGoods(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == 0x8059)
        {
            PublishRackShopFittingResponse(ReadRackShopFittingResponse(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == 0x8042)
        {
            PublishRackShopDepositInfo(ReadRackShopDepositInfo(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == 0x806c)
        {
            PublishRackShopRefundResponse(ReadRackShopRefunds(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == 0x806e)
        {
            PublishRackShopFittingDescriptions(ReadRackShopFittingDescriptions(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == 0x8048)
        {
            ReceiveMarquee(packet.Payload);
        }
        else if (packet.Opcode == 0x8063)
        {
            ReceiveArenaResult(packet.Payload);
        }
        else if (packet.Opcode == 0x8073)
        {
            PublishChangeSet(ReadChangeSet(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.FriendList)
        {
            PublishFriendState(ReadFriendState(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.ApprenticeSnapshot)
        {
            PublishApprenticeState(ReadApprenticeState(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.BlockList)
        {
            PublishBlockState(ReadBlockState(packet.Payload), cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.AccountSecuritySeed)
        {
            PublishAccountSecuritySeed(packet);
        }
        else if (packet.Opcode == ProtocolOpcodes.InventorySnapshot)
        {
            WorldInventoryState next;
            if (!PrivateServerInventoryCodec.TryDecode(packet.Payload, out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid inventory packet.");
            }

            PublishInventoryState(next, cancellationToken);
            // Official case 0x8006 stores Money (+0x10), CaiYu (+0x18), Weight (+0x20) and
            // MaxWeight (+0x24) inside the case body - arm64 0x25f110 / 0x25f118, x86_64
            // 0x26dfc6 / 0x26dfd1 / 0x26dfdb / 0x26dfe4 - once per received packet and in that
            // order, then the header's third i64 into CharData.honor (+0x70) - arm64 0x25f14c,
            // x86_64 0x26e01e - as the last store of the same case body. All five are published
            // here - on the host thread, at this packet's own arrival and not from a later
            // frame. This arm runs on the host thread: ProcessWorldPacket is only reached from
            // the in-world drain, which BeginWorldDrain starts through the host executor.
            _worldReceiveOwner?.ReceiveInventoryHeader(
                next.Money, next.CaiYu, next.Weight, next.MaximumWeight, next.Honor);
        }
        else if (packet.Opcode == ProtocolOpcodes.InventoryDelta)
        {
            // Compatibility route for an earlier retained packet or an unbound adapter.
            // The current concrete receipt is consumed by TryReceiveStaticSnapshotPacket.
            WorldInventoryState next = PrivateServerInventoryCodec.DecodeDelta(
                packet.Payload, Volatile.Read(ref _inventoryState), _worldReceiveOwner,
                out IWorldInventoryDeltaRows rows);
            rows?.Publish(next);
            PublishInventoryState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.WardrobeSnapshot)
        {
            WorldWardrobeState next;
            if (!PrivateServerWardrobeCodec.TryDecode(packet.Payload, out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid wardrobe packet.");
            }

            PublishWardrobeState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.SkillSnapshot)
        {
            WorldSkillBookObservation next = packet.ReceivedSkillBookObservation;
            if (!next.HasValue)
            {
                throw new InvalidDataException(
                    "The server sent an invalid skill snapshot.");
            }

            // Receipt owns both native writes and mirror publication. This check preserves
            // the old delivery cancellation boundary, but cannot replay an older receipt.
            cancellationToken.ThrowIfCancellationRequested();
        }
        else if (packet.Opcode == ProtocolOpcodes.LifeSkillSnapshot)
        {
            WorldLifeSkillBookState next;
            if (!PrivateServerSkillBookCodec.TryDecodeLifeSkills(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid life-skill snapshot.");
            }

            PublishLifeSkillBookState(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.BuffSnapshot)
        {
            // Official 0x8017 consumes the packet through one NetClient.Buf inside the case body
            // and writes the CharData statics as it reads, so the row stamp is the moment of the
            // read. The recovered reader owns that single Buf and is driven here, on the dispatch
            // thread the official case runs on, with the host clock bridge for the per-row stamp.
            // Retain this distinct, unmodified receipt payload only after native return. The
            // immutable all-row observation is requested separately and does not drive CharData.
            bool nonEmpty = ReadNativeBuffState(packet.Payload);
            PublishBuffPayload(packet.Payload, nonEmpty, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.TradeOpen ||
                 packet.Opcode == ProtocolOpcodes.TradeState ||
                 packet.Opcode == ProtocolOpcodes.TradeEnd)
        {
            // Only failed complete-message reads enter this queue. Keep their previous
            // startup/world failure boundary, without parsing twice or invoking an owner here.
            throw new InvalidDataException(packet.Opcode == ProtocolOpcodes.TradeOpen
                ? "The server sent an invalid trade-open packet."
                : packet.Opcode == ProtocolOpcodes.TradeState
                    ? "The server sent an invalid trade-state packet."
                    : "The server sent an invalid trade-end packet.");
        }
        else if (packet.Opcode == ProtocolOpcodes.SystemRequests)
        {
            PublishSystemRequests(packet, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.StallOwnerUpdate)
        {
            WorldStallOwnerState next;
            if (!PrivateServerWorldStateDecoder.TryDecodeStallOwnerState(
                    packet.Payload,
                    out next))
            {
                throw new InvalidDataException(
                    "The server sent an invalid stall-owner packet.");
            }

            PublishStallOwner(next, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.StallClosed)
        {
            int ownerEntityId;
            PrivateServerWorldStateDecoder.DecodeStallClosedOwnerId(
                    packet.Payload,
                    out ownerEntityId);

            RemoveStallOwner(ownerEntityId, cancellationToken);
        }
        else if (packet.Opcode == ProtocolOpcodes.SelfCounterWindow)
        {
            // Keep the existing arrival-ordered world drain. The receipt owner
            // stores each reached field before the next read and stamps only on completion.
            ReceiveCounterWindowPacket(packet.Payload);
        }
        else if (packet.Opcode == ProtocolOpcodes.RaidState)
        {
            // NetClient.Dispatch (0600077f) case 0x805c (arm64 0x24dae0 / x86_64 0x25b5cd), read
            // in its native order: Buf.U8 -> +0x91, Buf.I16 -> +0x94, Buf.I16 -> +0x98, Buf.I32 ->
            // +0x9c, Buf.I32 -> +0xa0, Buf.I16 -> +0xa4. No clamp, default, dedup or trailing
            // conditional read; a short payload raises out of Buf on both architectures. The
            // active byte is normalised with != 0 inside the case (arm64 0x24db64/0x24db6c, x86_64
            // 0x25b644/0x25b652), so no other byte value reaches the draw.
            // The clock is not sampled here: the official read stands after the sixth store inside
            // the same case body, so the owner takes it at this packet. Carriage boundary: this
            // arm belongs to ProcessWorldPacket, which only runs for packets that reach the
            // in-world drain - either directly or through the arrival FIFO BeginWorldDrain empties
            // in arrival order. The session and enter-world await loops do their own reading and
            // have no arm for this opcode, so a 0x805c that one of those loops reads is logged and
            // dropped; the target server cannot emit it before SelfData, see
            // docs/project/recovery-gaps/B-dispatch.json.
            NetClient.Buf reader = new NetClient.Buf(packet.Payload);
            bool active = reader.U8() != 0;
            int wave = reader.I16();
            int total = reader.I16();
            int nextSec = reader.I32();
            int remainSec = reader.I32();
            int alive = reader.I16();
            _worldReceiveOwner?.ReceiveRaidState(active, wave, total, nextSec, remainSec, alive);
        }
        else if (packet.Opcode == 0x805d)
        {
            // Native read order is I32 into the entity id and then U8 into the visibility byte;
            // the byte is compared with != 0 only after CombatWorld.FindPlayer has returned a
            // non-null identity, so both reads precede the lookup and a short payload raises out
            // of Buf on both architectures before anything is looked up. See
            // PublishRemotePlayerInvisibility for the case address evidence and for why an id
            // that is not spawned consumes nothing and is not retained for a future actor.
            int entityId;
            byte invisibleFlag;
            PrivateServerWorldStateDecoder.DecodeRemoteInvisibility(
                    packet.Payload,
                    out entityId,
                    out invisibleFlag);

            PublishRemotePlayerInvisibility(entityId, invisibleFlag != 0, cancellationToken);
        }
        else
        {
            LogIgnoredDownlink("world", packet);
        }
    }

    private void LogIgnoredDownlink(string stage, ProtocolPacket packet)
    {
        LogDownlinkDiagnostic("ignored", stage, packet, null);
    }

    private void LogUnexpectedDownlink(
        string stage,
        ProtocolPacket packet,
        ushort expectedOpcode)
    {
        LogDownlinkDiagnostic("unexpected", stage, packet, expectedOpcode);
    }

    private void LogDownlinkDiagnostic(
        string action,
        string stage,
        ProtocolPacket packet,
        ushort? expectedOpcode)
    {
        if (_diagnosticSink == null)
        {
            return;
        }

        int row = Interlocked.Increment(ref _downlinkDiagnosticRows);
        if (row > MaximumDownlinkDiagnosticRows + 1)
        {
            return;
        }

        string message;
        if (row == MaximumDownlinkDiagnosticRows + 1)
        {
            message = "[protocol.downlink] further diagnostic rows suppressed";
        }
        else
        {
            message = "[protocol.downlink] action=" + action +
                      " stage=" + stage +
                      " opcode=0x" + packet.Opcode.ToString("x4") +
                      " payloadBytes=" + packet.Payload.Length;
            if (expectedOpcode.HasValue)
            {
                message += " expected=0x" +
                           expectedOpcode.Value.ToString("x4");
            }
        }

        try
        {
            _diagnosticSink(message);
        }
        catch
        {
            // Diagnostics must never change protocol behavior.
        }
    }

    private void PublishTeleportState(
        WorldTeleportState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            long sequence = ++_teleportSequence;
            IReadOnlyList<WorldTeleportState> current =
                Volatile.Read(ref _teleportStates);
            List<WorldTeleportState> next =
                new List<WorldTeleportState>(current.Count + 1);
            next.AddRange(current);
            next.Add(new WorldTeleportState(sequence, state.MapId, state.X, state.Y));
            Volatile.Write(
                ref _teleportStates,
                new ReadOnlyCollection<WorldTeleportState>(next));
            Interlocked.Increment(ref _teleportStatesVersion);
        }
    }

    private void PublishSelfPkState(
        WorldSelfPkState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _selfPkState, state);
            Interlocked.Increment(ref _selfPkStateVersion);
        }
    }

    private void PublishLocalFactionState(
        WorldLocalFactionState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _localFactionState, state);
            Interlocked.Increment(ref _localFactionStateVersion);
        }
    }

    private void PublishInspectedPlayerEquipmentState(
        WorldInspectedPlayerEquipmentState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _inspectedPlayerEquipmentState, state);
            Interlocked.Increment(ref _inspectedPlayerEquipmentStateVersion);
        }
    }

    private void PublishPlayerStatusState(
        WorldPlayerStatusState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // A startup/full snapshot can arrive here after newer direct pet-vitals stores.
            // Project those current roots; this delayed publication never writes them back.
            Volatile.Write(ref _playerStatusState, state.WithPetVitals(CurrentPetVitals));
            Interlocked.Increment(ref _playerStatusStateVersion);
        }
    }

    private void PublishCharacterTitleState(
        WorldCharacterTitleState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _characterTitleState, state);
            Interlocked.Increment(ref _characterTitleStateVersion);
        }
    }

    private void PublishPlayerAttributeSnapshot(
        WorldPlayerAttributeSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        if (snapshot == null)
        {
            throw new ArgumentNullException(nameof(snapshot));
        }

        // Official 0x8007 stores both CharData attribute arrays inside the case body, once per
        // received packet and in the order +0xb8 then +0xb0; a death, mount-anchor or work frame
        // never re-publishes them, so the write has to happen here and not from a later frame.
        // Both the actual receipt and retained compatibility drain run this on the host.
        // The real static owner is written before an optional world-status observation.
        // The owner receives this packet's own write intent and not the merged mirror: the +0xb8
        // store is skipped when the packet's second array is empty (arm64 0x250bec `cmp w19,#0x1`
        // / 0x250bf0 `b.lt 0x250c5c`, past the store at 0x250c50; x86_64 0x25f3cf
        // `testl %r15d,%r15d` / 0x25f3d2 `jle 0x25f441`, past the store at 0x25f42b), so a null
        // second argument is passed instead of the mirror's retained previous totals - passing
        // those would copy the old array and write it back into +0xb8, which the official case
        // never does. The mirror itself keeps the previous immutable totals for the UI; it is
        // only kept away from the owner.
        IWorldReceiveOwner owner = _worldReceiveOwner;
        owner?.ReceivePlayerAttributes(
            snapshot.Current,
            snapshot.Total.Count > 0 ? snapshot.Total : null);
        // The same case body also stores the seven named scalars from that first array, between
        // the two array stores and the element-23 store: level = element 0 (+0x20), exp = 1
        // (+0x68), expToNext = 2 (+0x6c), hp = 3 (+0x58), maxHp = 4 (+0x5c), mp = 5 (+0x60) and
        // maxMp = 6 (+0x64) - arm64 0x250c9c-0x250d1c / x86_64 0x25f47c. It is the same owner the
        // 0x8003 enter callback uses for its own seven stores, so the player's later 0x8007
        // publishes overwrite the entry values at their own arrival instead of a second, per-frame
        // writer doing it from the mirror.
        owner?.ReceivePlayerAttributeFields(snapshot.Current);
        // Official case 0x8007 also stores element 23 of its FIRST array into
        // InventoryDlg.MaxWeight (arm64 0x259f94 / x86_64 0x268904, metadata usage ordinal 1929
        // TypeInfo:InventoryDlg), right after the two CharData array stores and inside the same
        // case body, so it is issued from the same host action and not from a later frame. The
        // store is reached only when the first array holds at least 24 elements (arm64 0x250d20
        // `cmp w8,#0x18` / 0x250d24 `b.ge 0x259f6c`; x86_64 0x25f51a `cmpl $0x18,%eax` /
        // 0x25f51d `jge 0x2688d8`), so a shorter first array performs no store at all - the
        // previous value stays and nothing is defaulted or clamped in its place.
        if (owner != null && TryTakeInventoryWeightLimit(snapshot.Current, out int maximumWeight))
        {
            owner.ReceiveInventoryWeightLimit(maximumWeight);
        }
        owner?.ReceivePlayerRingFields(snapshot.Current);

        WorldPlayerStatusState next;
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldPlayerStatusState current = Volatile.Read(ref _playerStatusState);
            if (current == null)
            {
                // The real CharData owner exists before a scene/status observation.
                // SelfData will publish its own attributes; never stage this packet for replay.
                return;
            }

            next = current.WithAttributeSnapshot(snapshot);
            Volatile.Write(ref _playerStatusState, next);
            Interlocked.Increment(ref _playerStatusStateVersion);
        }
    }

    /// <summary>
    /// The 0x8007 write intent for <c>InventoryDlg.MaxWeight</c>: element 23 of the packet's
    /// first array, and no store at all when that array is shorter than the 24 elements the
    /// official length gate requires.
    /// </summary>
    private static bool TryTakeInventoryWeightLimit(
        IReadOnlyList<int> current,
        out int maximumWeight)
    {
        maximumWeight = 0;
        if (current == null || current.Count < 24)
        {
            return false;
        }

        maximumWeight = current[23];
        return true;
    }

    private void PublishPlayerPetStatus(
        WorldPlayerPetStatusState pet,
        CancellationToken cancellationToken)
    {
        if (pet == null)
        {
            throw new ArgumentNullException(nameof(pet));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldPlayerStatusState current = Volatile.Read(ref _playerStatusState);
            if (current == null)
            {
                throw new InvalidDataException(
                    "A player pet packet arrived before the self-status root.");
            }

            Volatile.Write(
                ref _playerStatusState,
                current.WithPetSnapshot(pet.WithRetainedOptionalState(current.Pet))
                    .WithPetVitals(CurrentPetVitals));
            Interlocked.Increment(ref _playerStatusStateVersion);
        }
    }

    private void PublishRemotePlayerPkState(
        int entityId,
        byte pkState,
        CancellationToken cancellationToken, bool nativeRegistryHandled = false)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldRemotePlayerState> current =
                Volatile.Read(ref _remotePlayers);
            WorldRemotePlayerState player;
            if (!current.TryGetValue(entityId, out player))
            {
                // CombatWorld.SetPlayerPkState also ignores ids that are not currently spawned.
                return;
            }

            Dictionary<int, WorldRemotePlayerState> next =
                CopyRemotePlayers(current, 0);
            next[entityId] = player.WithPkState(
                pkState,
                checked(player.PkStateRevision + 1));
            Volatile.Write(
                ref _remotePlayers,
                new ReadOnlyDictionary<int, WorldRemotePlayerState>(next));
            Interlocked.Increment(ref _remotePlayersVersion);
            AppendRemoteEventLocked(WorldRemoteEventKind.Pk, entityId,
                player: next[entityId], pkState: pkState, nativeRegistryHandled: nativeRegistryHandled);
        }
    }

    // Official 1.5.8 NetClient.Dispatch (0x0600077f) case 0x805d, executed inside the in-world
    // drain. The whole case is: Buf.I32 entity id, Buf.U8 visibility byte, CombatWorld.FindPlayer
    // (0x0600013b, arm64 0x240250 / x86_64 0x24de20) with the id as its single argument
    // (arm64 mov x0,x20 / mov x1,#0 -> bl 0x240250; x86_64 mov edi,r14d / xor esi,esi ->
    // call 0x24de20), then - only for a non-null result - RemotePlayer.SetInvis (0x06000af9,
    // arm64 tail b 0x2da5d8 / x86_64 tail jmp 0x2eb700) with the byte compared against zero:
    // arm64 0x24c3c0 cmp w19,#0 / 0x24c3c4 cset w1,ne, x86_64 0x259dcb test ebx,ebx / 0x259dcd
    // setne cl. The null result jumps to the shared epilogue-only tail (arm64 cbz x0 -> 0x260d68,
    // x86_64 test rax,rax -> je 0x27009b), i.e. the case performs no write at all.
    // Live bound pumps now invoke this native lookup on receipt, before stage retention.
    // This publisher is observation-only for those receipts. Detached diagnostic inputs keep
    // their explicit deferred compatibility route; a mirror row never authorizes native work.
    private void PublishRemotePlayerInvisibility(
        int entityId,
        bool invisible,
        CancellationToken cancellationToken, bool nativeRegistryHandled = false)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldRemotePlayerState> current =
                Volatile.Read(ref _remotePlayers);
            if (!current.ContainsKey(entityId))
            {
                // CombatWorld.FindPlayer also returns null for an id that is not spawned, and the
                // case then does nothing. Nothing is queued for a future actor of this id: a
                // later incarnation gets its visibility from its own spawn row, exactly as an
                // official respawn does.
                return;
            }

            AppendRemoteEventLocked(WorldRemoteEventKind.Invis, entityId,
                invisible: invisible, nativeRegistryHandled: nativeRegistryHandled);
        }
    }

    private void PublishRemoteRidingState(
        WorldRemoteRidingState state,
        CancellationToken cancellationToken, bool nativeRegistryHandled = false)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldRemoteRidingState> currentRiding =
                Volatile.Read(ref _remoteRidingStates);
            Dictionary<int, WorldRemoteRidingState> nextRiding =
                CopyRemoteRidingStates(currentRiding, 1);
            nextRiding[state.EntityId] = state;

            IReadOnlyDictionary<int, WorldRemotePlayerState> currentPlayers =
                Volatile.Read(ref _remotePlayers);
            WorldRemotePlayerState currentPlayer;
            if (currentPlayers.TryGetValue(state.EntityId, out currentPlayer))
            {
                Dictionary<int, WorldRemotePlayerState> nextPlayers =
                    CopyRemotePlayers(currentPlayers, 0);
                nextPlayers[state.EntityId] = currentPlayer.WithRidingState(state);
                Volatile.Write(
                    ref _remotePlayers,
                    new ReadOnlyDictionary<int, WorldRemotePlayerState>(nextPlayers));
                Interlocked.Increment(ref _remotePlayersVersion);
                AppendRemoteEventLocked(WorldRemoteEventKind.Riding, state.EntityId,
                    player: nextPlayers[state.EntityId], riding: state, nativeRegistryHandled: nativeRegistryHandled);
            }

            Volatile.Write(
                ref _remoteRidingStates,
                new ReadOnlyDictionary<int, WorldRemoteRidingState>(nextRiding));
            Interlocked.Increment(ref _remoteRidingStatesVersion);
        }
    }

    private void PublishPlayerRenameLocked(WorldPlayerRenameState rename, int selfId)
    {
        WorldRemotePlayerState updated = null;
        if (rename.EntityId != 0 && _remotePlayers.TryGetValue(rename.EntityId, out var previous))
        {
            updated = previous.WithDisplayName(rename.Name);
            var players = CopyRemotePlayers(_remotePlayers, 0);
            players[rename.EntityId] = updated;
            Volatile.Write(ref _remotePlayers,
                new ReadOnlyDictionary<int, WorldRemotePlayerState>(players));
            Interlocked.Increment(ref _remotePlayersVersion);
        }
        if (rename.EntityId == selfId) _localRename = rename;
        if (updated != null || rename.EntityId == selfId)
            AppendRemoteEventLocked(WorldRemoteEventKind.Rename, rename.EntityId,
                player: updated, rename: rename);
    }

    private void PublishPlayerAppearanceState(
        WorldPlayerAppearanceState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishPlayerAppearanceStateLocked(state);
        }
    }

    private void PublishPlayerAppearanceStateLocked(WorldPlayerAppearanceState state, int? localEntityId = null,
        bool nativeRegistryHandled = false)
    {
        // Native 0x800a treats zero as the local-player alias; keep the raw wire identity.
        int selfId = localEntityId ?? Volatile.Read(ref _currentWorldEntityId);
        if (state.EntityId == 0 || state.EntityId == selfId)
        {
            Volatile.Write(ref _playerAppearanceState, state);
            Interlocked.Increment(ref _playerAppearanceStateVersion);
            return;
        }

        WorldRemotePlayerState previous;
        if (!_remotePlayers.TryGetValue(state.EntityId, out previous)) return;
        WorldRemotePlayerState updated = CopyRemoteAppearance(previous, state);
        Dictionary<int, WorldRemotePlayerState> next = CopyRemotePlayers(_remotePlayers, 0);
        next[state.EntityId] = updated;
        Volatile.Write(ref _remotePlayers,
            new ReadOnlyDictionary<int, WorldRemotePlayerState>(next));
        Interlocked.Increment(ref _remotePlayersVersion);
        AppendRemoteEventLocked(WorldRemoteEventKind.Appearance, state.EntityId,
            player: updated, appearance: state, nativeRegistryHandled: nativeRegistryHandled);
    }

    private readonly Dictionary<int, object> _receivedRemoteIdentities = new Dictionary<int, object>();

    private void PublishRemotePlayerState(
        WorldRemotePlayerState state,
        CancellationToken cancellationToken, bool nativeRegistryHandled = false, object receivedIdentity = null)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldRemotePlayerState> currentPlayers =
                Volatile.Read(ref _remotePlayers);
            WorldRemotePlayerState previous;
            bool repeated = currentPlayers.TryGetValue(state.EntityId, out previous);
            if (nativeRegistryHandled)
            {
                repeated = repeated && _receivedRemoteIdentities.TryGetValue(state.EntityId, out object prior) &&
                    ReferenceEquals(prior, receivedIdentity);
                _receivedRemoteIdentities[state.EntityId] = receivedIdentity;
            }
            if (repeated)
            {
                // SpawnPlayer's existing-player branch changes vitals only. Dispatch still
                // applies this packet's riding, invisibility and SetWeapon tails afterwards.
                state = CopyRepeatedRemoteSpawn(previous, state);
            }
            else
            {
                _remoteIncarnations[state.EntityId] = checked(++_remoteNextIncarnation);
            }

            Dictionary<int, WorldRemotePlayerState> nextPlayers =
                CopyRemotePlayers(currentPlayers, 1);
            Dictionary<int, WorldRemoteRidingState> nextRiding =
                CopyRemoteRidingStates(Volatile.Read(ref _remoteRidingStates), 1);
            nextPlayers[state.EntityId] = state;
            nextRiding[state.EntityId] = state.RidingState;
            PublishRemoteWorldSnapshots(nextPlayers, nextRiding, true, true);
            if (!repeated)
            {
                ClearRemotePathLocked(state.EntityId);
                ClearRemoteActionLocked(state.EntityId);
                ClearRemoteChatBubbleLocked(state.EntityId);
            }
            AppendRemoteEventLocked(WorldRemoteEventKind.Spawn, state.EntityId,
                player: state, repeatedSpawn: repeated, nativeRegistryHandled: nativeRegistryHandled,
                receivedIdentity: receivedIdentity);
        }
    }

    private static WorldRemotePlayerState CopyRemoteAppearance(
        WorldRemotePlayerState previous, WorldPlayerAppearanceState edge) =>
        new WorldRemotePlayerState(previous.EntityId, previous.DisplayName,
            previous.X, previous.Y, previous.Level, previous.Gender, previous.Head,
            previous.Hair, previous.Title, edge.Appearance, previous.Dead,
            edge.AvatarAura, edge.SoulEffects, previous.Faction, previous.HitPoints,
            previous.MaximumHitPoints, edge.TitleEffect, edge.GlowMode ?? previous.GlowMode,
            previous.Invisible, previous.RidingState, previous.LifeStateRevision,
            previous.PkState, previous.PkStateRevision);

    private static WorldRemotePlayerState CopyRepeatedRemoteSpawn(
        WorldRemotePlayerState previous, WorldRemotePlayerState received)
    {
        CharacterAppearanceRecord old = previous.Appearance;
        CharacterAppearanceRecord appearance = new CharacterAppearanceRecord(
            old.Body, old.Cap, old.Backpack, old.RightWeapon, old.LeftWeapon, old.Face,
            old.AttackVariant, received.Appearance.WeaponCategory, received.Appearance.AttackDistance);
        return new WorldRemotePlayerState(previous.EntityId, previous.DisplayName,
            previous.X, previous.Y, previous.Level, previous.Gender, previous.Head,
            previous.Hair, previous.Title, appearance, previous.Dead,
            previous.AvatarAura, previous.SoulEffects, received.Faction, received.HitPoints,
            received.MaximumHitPoints, previous.TitleEffect, previous.GlowMode,
            received.Invisible, received.RidingState, previous.LifeStateRevision,
            previous.PkState, previous.PkStateRevision);
    }

    private void PublishEmote(int entityId, int face, CancellationToken cancellationToken,
        bool nativeRegistryHandled = false)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendRemoteEventLocked(WorldRemoteEventKind.Emote, entityId, emoteFace: face,
                nativeRegistryHandled: nativeRegistryHandled);
        }
    }

    // Native receipt has already resolved the current registry. Its retained edge is only an
    // observation, even when the original owner found no live target. Detached inputs retain
    // the old explicit scene-consumer route.
    private void PublishEntityBubble(
        int entityId,
        string text,
        CancellationToken cancellationToken,
        bool nativeRegistryHandled = false)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            AppendRemoteEventLocked(WorldRemoteEventKind.EntityBubble, entityId, bubbleText: text,
                nativeRegistryHandled: nativeRegistryHandled);
        }
    }

    private void PublishRemoteChatBubble(
        int entityId,
        string text,
        byte themeSelector,
        CancellationToken cancellationToken,
        bool nativeRegistryHandled = false)
    {
        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            // Official 0x8039 calls CombatWorld.ShowPlayerBubble(senderEntityId, text, theme) for
            // channel 0 only, and the theme is metadata_usage_15642 "red" for the byte value 1 and
            // metadata_usage_15074 "huang" for every other value. The ordered event below is the
            // observation of receipt (or detached-input delivery) for every sender id:
            // CombatWorld.ShowPlayerBubble resolves the id through NetClient.SelfEntityId first
            // and reaches PlayerEmote.Local.ShowBubble without any remote-player lookup.
            AppendRemoteEventLocked(WorldRemoteEventKind.ChatBubble, entityId,
                bubbleText: text, bubbleTheme: themeSelector == 1 ? "red" : "huang",
                nativeRegistryHandled: nativeRegistryHandled);
            // The snapshot remains a compatibility observation for the remote-only view; source
            // consumers use ordered events, so a local id needs no snapshot row.
            if (!Volatile.Read(ref _remotePlayers).ContainsKey(entityId))
            {
                return;
            }

            WorldRemoteChatBubbleState state = new WorldRemoteChatBubbleState(
                entityId,
                text,
                themeSelector,
                checked(++_remoteChatBubbleRevision));
            Dictionary<int, WorldRemoteChatBubbleState> next =
                CopyRemoteChatBubbles(Volatile.Read(ref _remoteChatBubbles), 1);
            next[entityId] = state;
            Volatile.Write(
                ref _remoteChatBubbles,
                new ReadOnlyDictionary<int, WorldRemoteChatBubbleState>(next));
            Interlocked.Increment(ref _remoteChatBubblesVersion);
        }
    }

    private void PublishChannelChat(
        int senderEntityId,
        string senderName,
        byte bubbleThemeSelector,
        byte channelCode,
        string text,
        IReadOnlyList<WorldChatSharedEntryState> shares,
        CancellationToken cancellationToken, byte senderLevel = 0, byte senderFlag = 0)
    {
        if (senderName == null)
        {
            throw new ArgumentNullException(nameof(senderName));
        }

        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        // shares is the official ReadShares result and stays null for an absent or zero count;
        // only the sender name and the text are guaranteed non-null by the native branch.
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldChannelChatState state = new WorldChannelChatState(
                senderEntityId,
                senderName,
                bubbleThemeSelector,
                channelCode,
                text,
                shares,
                checked(++_channelChatRevision), senderLevel, senderFlag);
            IReadOnlyList<WorldChannelChatState> current =
                Volatile.Read(ref _channelChatRows);
            int retained = Math.Min(
                current.Count,
                MaximumRetainedChannelChatRows - 1);
            List<WorldChannelChatState> next =
                new List<WorldChannelChatState>(retained + 1);
            int first = current.Count - retained;
            for (int index = first; index < current.Count; index++)
            {
                next.Add(current[index]);
            }

            next.Add(state);
            Volatile.Write(
                ref _channelChatRows,
                new ReadOnlyCollection<WorldChannelChatState>(next));
            Interlocked.Increment(ref _channelChatRowsVersion);
        }
    }

    private void PublishPrivateChat(
        string senderName,
        string text,
        IReadOnlyList<WorldChatSharedEntryState> shares,
        CancellationToken cancellationToken,
        bool ownerApplied)
    {
        if (senderName == null)
        {
            throw new ArgumentNullException(nameof(senderName));
        }

        if (text == null)
        {
            throw new ArgumentNullException(nameof(text));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldPrivateChatState state = new WorldPrivateChatState(
                senderName,
                text,
                shares,
                checked(++_privateChatRevision));
            if (!ownerApplied)
                _socialUiUpdates.Enqueue(new WorldSocialUiUpdate(state));
            IReadOnlyList<WorldPrivateChatState> current =
                Volatile.Read(ref _privateChatRows);
            int retained = Math.Min(
                current.Count,
                MaximumRetainedPrivateChatRows - 1);
            List<WorldPrivateChatState> next =
                new List<WorldPrivateChatState>(retained + 1);
            int first = current.Count - retained;
            for (int index = first; index < current.Count; index++)
            {
                next.Add(current[index]);
            }

            next.Add(state);
            Volatile.Write(
                ref _privateChatRows,
                new ReadOnlyCollection<WorldPrivateChatState>(next));
            Interlocked.Increment(ref _privateChatRowsVersion);
        }
    }

    public void RecordAppliedPlayerLife(int entityId, bool alive, byte flags)
    {
        lock (_worldStateSync)
        {
            if (_stage != GatewayStage.InWorld) return;
            PublishLocalPlayerLifeStateLocked(entityId, alive, flags, true);
        }
    }

    private void PublishPlayerLifeState(
        int entityId,
        bool alive,
        int reviveFlags,
        CancellationToken cancellationToken, bool remoteAtReceive = false, bool nativeRegistryHandled = false)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int localEntityId = Volatile.Read(ref _currentWorldEntityId);
            if (!remoteAtReceive && (entityId == 0 || entityId == localEntityId))
            {
                PublishLocalPlayerLifeStateLocked(localEntityId, alive, reviveFlags);
                return;
            }

            PublishRemotePlayerLifeStateLocked(entityId, alive, nativeRegistryHandled);
        }
    }

    private void PublishLocalPlayerLifeStateLocked(
        int entityId,
        bool alive,
        int reviveFlags, bool nativeActionsHandled = false)
    {
        WorldLocalPlayerLifeState state = new WorldLocalPlayerLifeState(
            entityId,
            alive,
            reviveFlags,
            checked(++_localPlayerLifeRevision), nativeActionsHandled);
        Volatile.Write(ref _localPlayerLifeState, state);
        Interlocked.Increment(ref _localPlayerLifeStateVersion);
    }

    private void PublishRemotePlayerLifeStateLocked(
        int entityId,
        bool alive, bool nativeRegistryHandled = false)
    {
        IReadOnlyDictionary<int, WorldRemotePlayerState> current =
            Volatile.Read(ref _remotePlayers);
        WorldRemotePlayerState player;
        if (!current.TryGetValue(entityId, out player))
        {
            return;
        }

        Dictionary<int, WorldRemotePlayerState> next =
            CopyRemotePlayers(current, 0);
        next[entityId] = player.WithDeadState(!alive);
        Volatile.Write(
            ref _remotePlayers,
            new ReadOnlyDictionary<int, WorldRemotePlayerState>(next));
        Interlocked.Increment(ref _remotePlayersVersion);
        ClearRemoteActionLocked(entityId);
        AppendRemoteEventLocked(WorldRemoteEventKind.Life, entityId,
            player: next[entityId], alive: alive, nativeRegistryHandled: nativeRegistryHandled);
    }

    // Publishes one 0x8018 movement path. The official case calls CombatWorld.MovePath
    // (0x06000149, arm64 0x242ef4 / x86_64 0x250760) with the raw id, and that owner resolves the
    // entity itself in this order:
    //   1. id == NetClient.SelfEntityId and xs is not null and not zero-length - count MoveSelf and
    //      warp the local player to the last point of each array (arm64 0x242fd4..0x2431c8, tail
    //      call PlayerController.Warp at 0x2a9e30). A zero-length xs skips this branch and its
    //      counter (arm64 0x242fec) and continues with steps 3..5;
    //   2. CombatWorld.I is Unity-null (Object.op_Equality, arm64 0x243068) - count MoveNoWorld and
    //      return;
    //   3. I.players.TryGetValue(id) - a hit that is not Unity-null counts MovePlayer and goes to
    //      RemotePlayer.SetPath (arm64 0x2dbf3c);
    //   4. I.monsters.TryGetValue(id) - a hit that is not Unity-null counts MoveMob and goes to
    //      MonsterEntity.SetPath (arm64 0x48f734);
    //   5. neither dictionary yields a live target - CombatWorld.NoteNoTarget(id) (0x06000148).
    // A zero-count path still reaches steps 3..5 with two zero-length arrays, where both SetPath
    // owners return before any field write, so it is a consumed legal no-op rather than a failure.
    // This gateway only hands the decoded data over: a known remote player or monster also keeps a
    // receive snapshot and its event carries the revision assigned by
    // NextRemoteMovementActionRevisionLocked, so the event drain and the snapshot drain of one
    // message see the same WorldRemotePathState instance and the revision guard in the actors
    // (ReferenceWorldRemotePlayerActor.cs:523 and ReferenceWorldMonsterActor.cs:442) lets the
    // second one return before reaching the owner. An id that matches neither known set is
    // published as a path event only: it is appended before WithRevision runs, so its Revision is
    // 0 and no snapshot dictionary ever holds it - it is consumed exactly once by the window
    // sequence test in ReferenceFirstWorldInitialFrame.RemoteEvents.cs (180, 181 and 312).
    // The receive actors apply a path through CombatWorld.MovePath (the single application site),
    // and the drain in ReferenceFirstWorldInitialFrame.RemoteEvents.cs sends an id without an actor
    // to the same owner, so one message never reaches SetPath twice.
    private void PublishRemotePathState(
        WorldRemotePathState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (Volatile.Read(ref _remotePlayers).ContainsKey(state.EntityId))
            {
                state = state.WithRevision(NextRemoteMovementActionRevisionLocked());
                Dictionary<int, WorldRemotePathState> remotePaths =
                    CopyRemotePaths(Volatile.Read(ref _remotePaths), 1);
                remotePaths[state.EntityId] = state;
                Volatile.Write(
                    ref _remotePaths,
                    new ReadOnlyDictionary<int, WorldRemotePathState>(remotePaths));
                Interlocked.Increment(ref _remotePathsVersion);
                AppendRemoteEventLocked(WorldRemoteEventKind.Path, state.EntityId, path: state);
                return;
            }

            WorldEntityState entity;
            if (!Volatile.Read(ref _worldEntities).TryGetValue(state.EntityId, out entity) ||
                !IsMonsterStatusEffectKind(entity))
            {
                // No receive actor is known for this id. The official case never drops here: it
                // calls CombatWorld.MovePath with the raw id and that owner decides everything -
                // a local-player id warps, a registered player or monster reaches its own SetPath,
                // and an id no dictionary answers is recorded by CombatWorld.NoteNoTarget. The
                // decoded path is therefore handed over as data and nothing else is published for
                // it. This append runs before WithRevision, so this event carries Revision 0 and
                // never lands in a snapshot dictionary: its single application is guaranteed by
                // the window sequence test alone, which is the only mechanism this branch has and
                // is enough, so no revision is invented here for the sake of a uniform report.
                AppendRemoteEventLocked(WorldRemoteEventKind.Path, state.EntityId, path: state);
                return;
            }

            state = state.WithRevision(NextRemoteMovementActionRevisionLocked());
            Dictionary<int, WorldRemotePathState> monsterPaths =
                CopyRemotePaths(Volatile.Read(ref _monsterPaths), 1);
            monsterPaths[state.EntityId] = state;
            Volatile.Write(
                ref _monsterPaths,
                new ReadOnlyDictionary<int, WorldRemotePathState>(monsterPaths));
            Interlocked.Increment(ref _monsterPathsVersion);
            AppendEntityEventLocked(WorldRemoteEventKind.EntityPath, state.EntityId, path: state);
        }
    }

    private void PublishRemoteActionState(
        int entityId,
        WorldRemoteActionKind kind,
        int targetEntityId,
        CancellationToken cancellationToken, bool appendEvent = true)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!Volatile.Read(ref _remotePlayers).ContainsKey(entityId))
            {
                return;
            }

            WorldRemoteActionState state = new WorldRemoteActionState(
                entityId,
                kind,
                targetEntityId,
                NextRemoteMovementActionRevisionLocked());
            Dictionary<int, WorldRemoteActionState> next =
                CopyRemoteActions(Volatile.Read(ref _remoteActions), 1);
            next[entityId] = state;
            Volatile.Write(
                ref _remoteActions,
                new ReadOnlyDictionary<int, WorldRemoteActionState>(next));
            Interlocked.Increment(ref _remoteActionsVersion);
            if (appendEvent) AppendRemoteEventLocked(WorldRemoteEventKind.Action, entityId, action: state);
        }
    }

    private void PublishSkillCastState(
        WorldSkillCastState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldSkillCastState next = state.WithRevision(++_skillCastRevision);
            IReadOnlyList<WorldSkillCastState> current =
                Volatile.Read(ref _skillCastEvents);
            int retained = Math.Min(
                current.Count,
                MaximumRetainedCombatEdges - 1);
            List<WorldSkillCastState> events =
                new List<WorldSkillCastState>(retained + 1);
            int start = current.Count - retained;
            for (int index = start; index < current.Count; index++)
            {
                events.Add(current[index]);
            }
            events.Add(next);
            Volatile.Write(
                ref _skillCastEvents,
                new ReadOnlyCollection<WorldSkillCastState>(events));
            Volatile.Write(ref _skillCastState, next);
            Interlocked.Increment(ref _skillCastStateVersion);
            // The official 0x800e owner CombatWorld.PlayerCast is the single place that starts
            // RemotePlayer.PlayCast for a remote caster, so this publisher carries the decoded
            // cast state only. A second animation edge here would be two animation starts for
            // one original message and would need a product discard rule to hide it.
        }
    }

    private void PublishCombatReceive(WorldCombatResultState result,
        CancellationToken cancellationToken, int? localEntityId = null)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            PublishCombatResult(result, cancellationToken,
                localEntityId ?? Volatile.Read(ref _currentWorldEntityId));
            PublishRemoteActionState(result.AttackerEntityId, WorldRemoteActionKind.Attack,
                result.TargetEntityId, cancellationToken, false);
            AppendRemoteEventLocked(WorldRemoteEventKind.Combat, result.TargetEntityId, combat: result);
        }
    }

    private void PublishCombatResult(
        WorldCombatResultState state,
        CancellationToken cancellationToken,
        int localEntityId)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _entityIncarnations.TryGetValue(state.AttackerEntityId, out long attackerIncarnation);
            _entityIncarnations.TryGetValue(state.TargetEntityId, out long targetIncarnation);
            state = state.WithEntityIncarnations(attackerIncarnation, targetIncarnation, _remoteEventSequence)
                .WithRevision(++_combatResultRevision);

            // Immutable receive projection follows OnCombat target precedence. The ordered
            // raw event remains authoritative for live Unity registry/lifecycle decisions.
            bool selfTarget = state.TargetEntityId != 0 && state.TargetEntityId == localEntityId;
            if (!selfTarget && _remotePlayers.TryGetValue(state.TargetEntityId, out var remoteTarget))
            {
                if (!state.Missed)
                {
                    int hp = Math.Max(0, state.RawTargetHitPoints);
                    if (remoteTarget.MaximumHitPoints > 0) hp = Math.Min(hp, remoteTarget.MaximumHitPoints);
                    var players = CopyRemotePlayers(_remotePlayers, 0);
                    players[state.TargetEntityId] = remoteTarget.WithVitals(hp, remoteTarget.MaximumHitPoints);
                    Volatile.Write(ref _remotePlayers,
                        new ReadOnlyDictionary<int, WorldRemotePlayerState>(players));
                    Interlocked.Increment(ref _remotePlayersVersion);
                }
            }
            else if (!selfTarget && _worldEntities.TryGetValue(state.TargetEntityId, out var target) &&
                IsMonsterStatusEffectKind(target))
            {
                var nextEntities = CopyWorldEntities(_worldEntities, 0);
                nextEntities[state.TargetEntityId] = target.WithHitPoints(Math.Max(0, state.RawTargetHitPoints));
                RefreshWorldEntityPublicationSnapshotLocked(
                    new ReadOnlyDictionary<int, WorldEntityState>(nextEntities),
                    Interlocked.Increment(ref _worldEntitiesVersion), nextEntities.Count);
            }

            Volatile.Write(
                ref _combatResults,
                AppendRetainedCombatResult(
                    Volatile.Read(ref _combatResults),
                    state));
            Interlocked.Increment(ref _combatResultsVersion);
        }
    }

    private void PublishCombatNumber(
        WorldCombatNumberState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            state = state.WithRevision(++_combatNumberRevision);
            Volatile.Write(
                ref _combatNumbers,
                AppendRetainedCombatNumber(
                    Volatile.Read(ref _combatNumbers),
                    state));
            Interlocked.Increment(ref _combatNumbersVersion);
        }
    }

    private void PublishPartyState(
        WorldPartyState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _partyState, state);
            Interlocked.Increment(ref _partyStateVersion);
            AppendRemoteEventLocked(WorldRemoteEventKind.Party, party: state);
        }
    }

    private void PublishAccountSecuritySeed(
        bool hasSecurityCode,
        int windowMilliseconds)
    {
        Interlocked.Exchange(
            ref _accountHasSecurityCode,
            hasSecurityCode ? 1 : 0);
        Interlocked.Exchange(
            ref _accountSecurityWindowMilliseconds,
            windowMilliseconds);
        Interlocked.Increment(ref _accountSecuritySeedVersion);
    }

    private Task PublishInventoryStateOnHostAsync(WorldInventoryState state, CancellationToken cancellationToken) =>
        _host.RunAsync(() => PublishInventoryState(state, cancellationToken), cancellationToken);

    private void PublishInventoryState(
        WorldInventoryState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _inventoryState, state);
            Interlocked.Increment(ref _inventoryStateVersion);
            RecordGroundLootInventoryLocked();
        }
        // Receipt paths and the retained entry fallback publish on the host.
        // Endgame is host-owned, not protected by the observation snapshot lock.
        Endgame.NoteNativeInventoryPublished();
    }

    private void PublishWardrobeState(
        IWorldWardrobeObservation state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _wardrobeState, state);
            Interlocked.Increment(ref _wardrobeStateVersion);
        }
    }

    private void PublishHotbarState(WorldHotbarState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            Volatile.Write(ref _hotbarState, state);
            Interlocked.Increment(ref _hotbarStateVersion);
        }
    }

    private void PublishSkillBookState(WorldSkillBookState state)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        PublishSkillBookObservation(new WorldSkillBookObservation(state));
    }

    private void PublishLifeSkillBookState(
        WorldLifeSkillBookState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _lifeSkillBookState, state);
            _lifeSkillBookObservationPayload = null;
            Interlocked.Increment(ref _lifeSkillBookStateVersion);
        }
    }

    private void PublishBuffState(
        WorldBuffState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _buffObservedPayload = null;
            Volatile.Write(ref _buffState, state);
            Interlocked.Increment(ref _buffStateVersion);
        }
    }

    private void PublishStallOwner(
        WorldStallOwnerState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<int, WorldStallOwnerState> next =
                CopyStallOwners(Volatile.Read(ref _stallOwners), 1);
            next[state.EntityId] = state;
            Volatile.Write(
                ref _stallOwners,
                new ReadOnlyDictionary<int, WorldStallOwnerState>(next));
            Interlocked.Increment(ref _stallOwnersVersion);
            AppendRemoteEventLocked(WorldRemoteEventKind.Stall, state.EntityId, stall: state);
        }
    }

    private void RemoveStallOwner(int ownerEntityId, CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldStallOwnerState> current =
                Volatile.Read(ref _stallOwners);
            if (!current.ContainsKey(ownerEntityId))
            {
                AppendRemoteEventLocked(WorldRemoteEventKind.Stall, ownerEntityId);
                return;
            }

            Dictionary<int, WorldStallOwnerState> next = CopyStallOwners(current, 0);
            next.Remove(ownerEntityId);
            Volatile.Write(
                ref _stallOwners,
                new ReadOnlyDictionary<int, WorldStallOwnerState>(next));
            Interlocked.Increment(ref _stallOwnersVersion);
            AppendRemoteEventLocked(WorldRemoteEventKind.Stall, ownerEntityId);
        }
    }

    private bool TryReceiveNetworkNpcPacket(ushort opcode, byte[] payload)
    {
        INetworkNpcReceiveOwner owner = _worldReceiveOwner as INetworkNpcReceiveOwner;
        if (opcode == ProtocolOpcodes.NpcSpawn)
        {
            PrivateServerWorldStateDecoder.DecodeNpcState(payload, out WorldNpcState row);
            if (owner == null) throw new InvalidOperationException("The native NPC receive owner is not bound.");
            owner.ReceiveNetworkNpcSpawn(row);
            PublishNpcState(row, CancellationToken.None); // Compatibility observation only.
            return true;
        }
        if (opcode == ProtocolOpcodes.NpcRemove)
        {
            PrivateServerWorldStateDecoder.DecodeNpcRemoval(payload, out int id);
            if (owner == null) throw new InvalidOperationException("The native NPC receive owner is not bound.");
            owner.ReceiveNetworkNpcRemove(id);
            RemoveNpcState(id, CancellationToken.None);
            return true;
        }
        return false;
    }

    private void PublishNpcState(
        WorldNpcState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<int, WorldNpcState> next =
                CopyNpcs(Volatile.Read(ref _npcs), 1);
            next[state.EntityId] = state;
            Volatile.Write(
                ref _npcs,
                new ReadOnlyDictionary<int, WorldNpcState>(next));
            Interlocked.Increment(ref _npcsVersion);
            // The native row/root mutation already ran synchronously at receipt. This event
            // is only an observation for existing diagnostics and optional frame views.
            AppendNpcEventLocked(WorldRemoteEventKind.NpcSpawn, state.EntityId, state);
        }
    }

    private void RemoveNpcState(
        int entityId,
        CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldNpcState> current = Volatile.Read(ref _npcs);
            Dictionary<int, WorldNpcState> next = CopyNpcs(current, 0);
            next.Remove(entityId);
            Volatile.Write(
                ref _npcs,
                new ReadOnlyDictionary<int, WorldNpcState>(next));
            Interlocked.Increment(ref _npcsVersion);
            // Native OnNetNpcRemove already ran even when the compatibility row was absent.
            // Retain an observation for every received remove; this never destroys a root.
            AppendNpcEventLocked(WorldRemoteEventKind.NpcRemove, entityId, null);
        }
    }

    private void PublishNpcTaskOffersState(
        WorldNpcTaskOffersState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _npcTaskOffersObservationPayload = null;
            Volatile.Write(ref _npcTaskOffersState, state);
            Interlocked.Increment(ref _npcTaskOffersStateVersion);
        }
    }

    private void PublishShopState(
        WorldShopState state,
        CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _shopState, state);
            Interlocked.Increment(ref _shopStateVersion);
        }
    }

    private void PublishTransportState(
        WorldTransportState state,
        CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _transportState, state);
            Interlocked.Increment(ref _transportStateVersion);
        }
    }

    private void PublishWarehouseState(
        WorldWarehouseState state,
        CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _warehouseState, state);
            _warehouseUpdates.Enqueue(WorldWarehouseUpdate.SetData(state));
            Interlocked.Increment(ref _warehouseStateVersion);
        }
    }

    private void PublishNpcChatterState(
        WorldNpcChatterState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Volatile.Write(ref _npcChatterState, state);
            Interlocked.Increment(ref _npcChatterStateVersion);
        }
    }

    private void PublishTaskLogState(
        WorldTaskLogState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _taskLogObservationPayload = null;
            Volatile.Write(ref _taskLogState, state);
            Interlocked.Increment(ref _taskLogStateVersion);
        }
    }

    // Observation only: callers have completed the receipt-time effect before reaching this
    // retained history. No Presentation consumer may replay these observations as gameplay.
    private void PublishNpcDialogTextState(
        WorldNpcDialogTextState state,
        CancellationToken cancellationToken)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        AppendNpcDialogTextEdge(state, null, cancellationToken);
    }

    private void AppendNpcDialogTextEdge(
        WorldNpcDialogTextState state, WorldInvitationCodeResult invitationCodeResult,
        CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldNpcDialogTextEdgeWindow current =
                Volatile.Read(ref _npcDialogTextEdgeWindow);
            long revision = Interlocked.Increment(ref _npcDialogTextEdgeRevision);
            int retained = Math.Min(
                current.Edges.Count,
                MaximumRetainedNpcDialogTextEdges - 1);
            int first = current.Edges.Count - retained;
            long droppedThroughRevision = current.DroppedThroughRevision;
            if (first > 0)
            {
                droppedThroughRevision = current.Edges[first - 1].Revision;
            }

            List<WorldNpcDialogTextEdge> edges =
                new List<WorldNpcDialogTextEdge>(retained + 1);
            for (int index = first; index < current.Edges.Count; index++)
            {
                edges.Add(current.Edges[index]);
            }
            edges.Add(new WorldNpcDialogTextEdge(
                revision,
                state == null ? string.Empty : state.Text,
                state == null ? (byte)0 : state.DisplayMode,
                invitationCodeResult));

            Volatile.Write(
                ref _npcDialogTextEdgeWindow,
                new WorldNpcDialogTextEdgeWindow(
                    current.Version + 1,
                    droppedThroughRevision,
                    edges));
            if (state != null)
            {
                Volatile.Write(ref _npcDialogTextState, state);
                Interlocked.Increment(ref _npcDialogTextStateVersion);
            }
        }
    }

    private void BeginWorldScenePublication(
        int mapId,
        CancellationToken cancellationToken)
    {
        if (mapId < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(mapId));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            WorldEntityPublicationState currentPublication =
                Volatile.Read(ref _worldEntityPublicationState);
            // entered=1 follows a rebuilt client world, including a same-map teleport or
            // rollback. The map number alone is not the native scene-lifetime identity.
            Volatile.Write(ref _currentWorldMapId, mapId);
            ClearRemoteSceneSnapshotsLocked();
            IReadOnlyDictionary<int, WorldEntityState> emptyEntities =
                new ReadOnlyDictionary<int, WorldEntityState>(
                    new Dictionary<int, WorldEntityState>());
            long snapshotVersion = Interlocked.Increment(ref _worldEntitiesVersion);

            // GameBootstrap.BuildWorld releases the previous world through ClearNetNpcs before
            // the destination scene becomes visible. NPC spawn packets are scene-local and the
            // server is not required to emit one 0x8047 removal for every actor during a map
            // handoff, so never carry the previous map's immutable NPC snapshot forward.
            IReadOnlyDictionary<int, WorldNpcState> npcs = Volatile.Read(ref _npcs);
            if (npcs.Count > 0)
            {
                Volatile.Write(
                    ref _npcs,
                    new ReadOnlyDictionary<int, WorldNpcState>(
                        new Dictionary<int, WorldNpcState>()));
                Interlocked.Increment(ref _npcsVersion);
            }

            IReadOnlyDictionary<int, WorldRemotePathState> monsterPaths =
                Volatile.Read(ref _monsterPaths);
            if (monsterPaths.Count > 0)
            {
                Volatile.Write(
                    ref _monsterPaths,
                    new ReadOnlyDictionary<int, WorldRemotePathState>(
                        new Dictionary<int, WorldRemotePathState>()));
                Interlocked.Increment(ref _monsterPathsVersion);
            }

            WorldEntityPublicationState publication =
                new WorldEntityPublicationState(
                    currentPublication.SceneGeneration + 1,
                    mapId,
                    0,
                    0,
                    0,
                    0,
                    snapshotVersion,
                    0);
            PublishWorldEntitySceneSnapshotLocked(
                emptyEntities,
                snapshotVersion,
                publication);
            ResetRemoteEventSceneLocked(mapId);
        }
    }

    // Project only the original spawn/no-op; execute each optional tail through its router.
    // Trap receives have their own registry; a new nontrap root mirrors Init's final HP field.
    private static WorldEntityState ProjectWorldEntitySpawn(WorldEntityState incoming,
        WorldEntityState previous, out bool created)
    {
        bool trap = incoming.Kind == WorldEntityKind.Trap;
        bool sameRegistry = previous != null &&
            (previous.Kind == WorldEntityKind.Trap) == trap;
        created = !sameRegistry;
        WorldEntityState result = sameRegistry ? previous
            : incoming.WithStatusEffects(Array.Empty<WorldEntityStatusEffect>(), 0)
                .WithStatusIcons(Array.Empty<WorldEntityStatusIcon>(), 0);
        return created && !trap ? result.WithHitPoints(Math.Max(0, incoming.HitPointsOrRadiusPixels)) : result;
    }

    private void PublishWorldEntityState(
        WorldEntityState state,
        CancellationToken cancellationToken,
        int? resolvedSelfId = null, bool admittedOnMainThread = false,
        bool nativeRegistryHandled = false, object receivedIdentity = null)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int currentMapId = Volatile.Read(ref _currentWorldMapId);
            WorldEntityPublicationState publication =
                Volatile.Read(ref _worldEntityPublicationState);
            long decodedSpawnFrames = publication.DecodedSpawnFrameCount + 1;
            if (!admittedOnMainThread && state.MapIdGuard >= 1 && currentMapId >= 1 &&
                state.MapIdGuard != currentMapId)
            {
                IReadOnlyDictionary<int, WorldEntityState> currentEntities =
                    Volatile.Read(ref _worldEntities);
                long currentVersion = Interlocked.Read(ref _worldEntitiesVersion);
                WorldEntityPublicationState rejectedPublication =
                    new WorldEntityPublicationState(
                        publication.SceneGeneration,
                        currentMapId,
                        decodedSpawnFrames,
                        publication.AdmittedSpawnFrameCount,
                        publication.RejectedMapGuardSpawnFrameCount + 1,
                        publication.ReceivedRemovalFrameCount,
                        currentVersion,
                        currentEntities.Count);
                PublishWorldEntitySceneSnapshotLocked(
                    currentEntities,
                    currentVersion,
                    rejectedPublication);
                return;
            }

            if (state.Kind == WorldEntityKind.Trap)
            {
                if (!_worldTraps.ContainsKey(state.EntityId))
                {
                    _worldTraps[state.EntityId] = state;
                    _trapIncarnations[state.EntityId] = checked(++_entityNextIncarnation);
                }
                AppendEntityEventLocked(WorldRemoteEventKind.EntitySpawn, state.EntityId, entity: state,
                    nativeRegistryHandled: nativeRegistryHandled, receivedIdentity: receivedIdentity);
                PublishWorldEntitySceneSnapshotLocked(_worldEntities, _worldEntitiesVersion,
                    new WorldEntityPublicationState(publication.SceneGeneration, currentMapId,
                        decodedSpawnFrames, publication.AdmittedSpawnFrameCount + 1,
                        publication.RejectedMapGuardSpawnFrameCount, publication.ReceivedRemovalFrameCount,
                        _worldEntitiesVersion, _worldEntities.Count));
                return;
            }

            IReadOnlyDictionary<int, WorldEntityState> current =
                Volatile.Read(ref _worldEntities);
            current.TryGetValue(state.EntityId, out var previous);
            WorldEntityState incoming = state;
            state = ProjectWorldEntitySpawn(state, previous, out bool created);

            Dictionary<int, WorldEntityState> next =
                CopyWorldEntities(current, 1);
            next[state.EntityId] = state;
            IReadOnlyDictionary<int, WorldEntityState> publishedEntities =
                new ReadOnlyDictionary<int, WorldEntityState>(next);
            long snapshotVersion = Interlocked.Increment(ref _worldEntitiesVersion);
            if (created) ClearMonsterPathLocked(state.EntityId);
            WorldEntityPublicationState admittedPublication =
                new WorldEntityPublicationState(
                    publication.SceneGeneration,
                    currentMapId,
                    decodedSpawnFrames,
                    publication.AdmittedSpawnFrameCount + 1,
                    publication.RejectedMapGuardSpawnFrameCount,
                    publication.ReceivedRemovalFrameCount,
                    snapshotVersion,
                    next.Count);
            PublishWorldEntitySceneSnapshotLocked(
                publishedEntities,
                snapshotVersion,
                admittedPublication);
            if (created) _entityIncarnations[state.EntityId] = checked(++_entityNextIncarnation);
            AppendEntityEventLocked(WorldRemoteEventKind.EntitySpawn, incoming.EntityId,
                entity: incoming.WithStatusEffects(Array.Empty<WorldEntityStatusEffect>(), 0)
                    .WithStatusIcons(Array.Empty<WorldEntityStatusIcon>(), 0),
                nativeRegistryHandled: nativeRegistryHandled, receivedIdentity: receivedIdentity);
            if (incoming.Kind != WorldEntityKind.Trap)
            {
                foreach (WorldEntityStatusEffect effect in incoming.StatusEffects)
                    if (!string.IsNullOrEmpty(effect.Effect) && effect.DurationMilliseconds > 0)
                        PublishWorldEntityStatusEffect(incoming.EntityId, effect,
                            cancellationToken, resolvedSelfId, nativeRegistryHandled);
                if (incoming.HasStatusIconsUpdate)
                    PublishWorldEntityStatusIcons(incoming.EntityId, incoming.StatusIcons, cancellationToken, nativeRegistryHandled);
            }
        }
    }

    private void RemoveWorldEntityState(
        int entityId,
        CancellationToken cancellationToken,
        byte reason = 0)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (_worldTraps.TryGetValue(entityId, out var trap))
            {
                AppendEntityEventLocked(WorldRemoteEventKind.EntityRemove, entityId, entity: trap, reason: reason);
                _worldTraps.Remove(entityId);
                _trapIncarnations.Remove(entityId);
                WorldEntityPublicationState trapPublication = _worldEntityPublicationState;
                PublishWorldEntitySceneSnapshotLocked(_worldEntities, _worldEntitiesVersion,
                    new WorldEntityPublicationState(trapPublication.SceneGeneration, _currentWorldMapId,
                        trapPublication.DecodedSpawnFrameCount, trapPublication.AdmittedSpawnFrameCount,
                        trapPublication.RejectedMapGuardSpawnFrameCount, trapPublication.ReceivedRemovalFrameCount + 1,
                        _worldEntitiesVersion, _worldEntities.Count));
                return;
            }
            if (_remotePlayers.ContainsKey(entityId))
            {
                RemoveRemotePlayerLocked(entityId, reason);
                return;
            }
            WorldEntityPublicationState publication =
                Volatile.Read(ref _worldEntityPublicationState);
            long receivedRemovalFrames = publication.ReceivedRemovalFrameCount + 1;
            IReadOnlyDictionary<int, WorldEntityState> current =
                Volatile.Read(ref _worldEntities);
            if (!current.ContainsKey(entityId))
            {
                long currentVersion = Interlocked.Read(ref _worldEntitiesVersion);
                WorldEntityPublicationState missingPublication =
                    new WorldEntityPublicationState(
                        publication.SceneGeneration,
                        Volatile.Read(ref _currentWorldMapId),
                        publication.DecodedSpawnFrameCount,
                        publication.AdmittedSpawnFrameCount,
                        publication.RejectedMapGuardSpawnFrameCount,
                        receivedRemovalFrames,
                        currentVersion,
                        current.Count);
                PublishWorldEntitySceneSnapshotLocked(
                    current,
                    currentVersion,
                    missingPublication);
                return;
            }

            Dictionary<int, WorldEntityState> next = CopyWorldEntities(current, 0);
            next.Remove(entityId);
            AppendEntityEventLocked(WorldRemoteEventKind.EntityRemove, entityId,
                entity: current[entityId], reason: reason);
            _entityIncarnations.Remove(entityId);
            IReadOnlyDictionary<int, WorldEntityState> publishedEntities =
                new ReadOnlyDictionary<int, WorldEntityState>(next);
            long snapshotVersion = Interlocked.Increment(ref _worldEntitiesVersion);
            ClearMonsterPathLocked(entityId);
            WorldEntityPublicationState removedPublication =
                new WorldEntityPublicationState(
                    publication.SceneGeneration,
                    Volatile.Read(ref _currentWorldMapId),
                    publication.DecodedSpawnFrameCount,
                    publication.AdmittedSpawnFrameCount,
                    publication.RejectedMapGuardSpawnFrameCount,
                    receivedRemovalFrames,
                    snapshotVersion,
                    next.Count);
            PublishWorldEntitySceneSnapshotLocked(
                publishedEntities,
                snapshotVersion,
                removedPublication);
        }
    }

    private void PublishWorldEntityStatusIcons(
        int entityId,
        IReadOnlyList<WorldEntityStatusIcon> icons,
        CancellationToken cancellationToken, bool nativeRegistryHandled = false)
    {
        if (icons == null)
        {
            throw new ArgumentNullException(nameof(icons));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldEntityState> current =
                Volatile.Read(ref _worldEntities);
            WorldEntityState entity;
            if (!current.TryGetValue(entityId, out entity) || !IsMonsterStatusEffectKind(entity))
            {
                return;
            }

            Dictionary<int, WorldEntityState> next = CopyWorldEntities(current, 0);
            next[entityId] = entity.WithStatusIcons(icons);
            IReadOnlyDictionary<int, WorldEntityState> publishedEntities =
                new ReadOnlyDictionary<int, WorldEntityState>(next);
            long snapshotVersion = Interlocked.Increment(ref _worldEntitiesVersion);
            RefreshWorldEntityPublicationSnapshotLocked(
                publishedEntities,
                snapshotVersion,
                next.Count);
            AppendEntityEventLocked(WorldRemoteEventKind.EntityStatusIcons, entityId, entity: next[entityId],
                nativeRegistryHandled: nativeRegistryHandled);
        }
    }

    private void PublishWorldEntityStatusEffect(
        int entityId,
        WorldEntityStatusEffect effect,
        CancellationToken cancellationToken,
        int? resolvedSelfId = null, bool nativeRegistryHandled = false)
    {
        if (effect == null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            int localEntityId = resolvedSelfId ?? Volatile.Read(ref _currentWorldEntityId);
            if (entityId == localEntityId && localEntityId != 0)
            {
                // 1.5.8 resolves nonzero self before both dictionaries and calls SpawnOn only
                // when the effect starts with WF. Mode and duration do not affect this branch.
                if (!ShouldPublishLocalPlayerStatusEffect(effect))
                {
                    return;
                }

                if (!nativeRegistryHandled)
                {
                    EnqueueLocalPlayerStatusEffectLocked(effect);
                    Interlocked.Increment(ref _localPlayerStatusEffectsVersion);
                }
                return;
            }

            IReadOnlyDictionary<int, WorldEntityState> current =
                Volatile.Read(ref _worldEntities);
            WorldEntityState entity;
            if (!current.TryGetValue(entityId, out entity) || !IsMonsterStatusEffectKind(entity))
            {
                PublishRemotePlayerStatusEffectLocked(entityId, effect, nativeRegistryHandled);
                return;
            }

            WorldEntityState updated = ApplyWorldEntityStatusEffect(entity, effect);
            if (ReferenceEquals(updated, entity))
            {
                return;
            }

            Dictionary<int, WorldEntityState> next = CopyWorldEntities(current, 0);
            next[entityId] = updated;
            IReadOnlyDictionary<int, WorldEntityState> publishedEntities =
                new ReadOnlyDictionary<int, WorldEntityState>(next);
            long snapshotVersion = Interlocked.Increment(ref _worldEntitiesVersion);
            RefreshWorldEntityPublicationSnapshotLocked(
                publishedEntities,
                snapshotVersion,
                next.Count);
            AppendEntityEventLocked(WorldRemoteEventKind.EntityStatusFx, entityId,
                entity: updated, effect: effect, nativeRegistryHandled: nativeRegistryHandled);
        }
    }

    private void PublishSkillHitEffect(
        WorldSkillHitEffect effect,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (effect == null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        lock (_worldStateSync)
        {
            long revision = ++_skillHitEffectRevision;
            IReadOnlyList<WorldSkillHitEffect> current =
                Volatile.Read(ref _skillHitEffects);
            int retained = Math.Min(
                current.Count,
                MaximumRetainedCombatEdges - 1);
            List<WorldSkillHitEffect> next =
                new List<WorldSkillHitEffect>(retained + 1);
            int start = current.Count - retained;
            for (int index = start; index < current.Count; index++)
            {
                next.Add(current[index]);
            }
            next.Add(effect.WithRevision(revision));
            Volatile.Write(
                ref _skillHitEffects,
                new ReadOnlyCollection<WorldSkillHitEffect>(next));
            Interlocked.Increment(ref _skillHitEffectsVersion);
        }
    }

    private void RefreshWorldEntityPublicationSnapshotLocked(
        IReadOnlyDictionary<int, WorldEntityState> entities,
        long snapshotVersion,
        int entityCount)
    {
        WorldEntityPublicationState publication =
            Volatile.Read(ref _worldEntityPublicationState);
        WorldEntityPublicationState refreshed =
            new WorldEntityPublicationState(
                publication.SceneGeneration,
                Volatile.Read(ref _currentWorldMapId),
                publication.DecodedSpawnFrameCount,
                publication.AdmittedSpawnFrameCount,
                publication.RejectedMapGuardSpawnFrameCount,
                publication.ReceivedRemovalFrameCount,
                snapshotVersion,
                entityCount);
        PublishWorldEntitySceneSnapshotLocked(
            entities,
            snapshotVersion,
            refreshed);
    }

    private void PublishWorldEntitySceneSnapshotLocked(
        IReadOnlyDictionary<int, WorldEntityState> entities,
        long snapshotVersion,
        WorldEntityPublicationState publication)
    {
        if (entities == null)
        {
            throw new ArgumentNullException(nameof(entities));
        }

        Volatile.Write(ref _worldEntities, entities);
        Volatile.Write(ref _worldEntityPublicationState, publication);
        Volatile.Write(
            ref _worldEntitySceneSnapshot,
            new WorldEntitySceneSnapshot(
                publication.SceneGeneration,
                publication.MapId,
                snapshotVersion,
                entities,
                publication));
    }

    private void EnqueueLocalPlayerStatusEffectLocked(WorldEntityStatusEffect effect)
    {
        long revision = checked(_localPlayerStatusEffectRevision + 1);
        _localPlayerStatusEffects.Enqueue(effect.WithRevision(revision));
        _localPlayerStatusEffectRevision = revision;
    }

    // Legacy immutable helper remains available to historical Editor callers. Live delivery
    // uses the connection queue above, whose prefix is removed only by acknowledgment.
    internal static IReadOnlyList<WorldEntityStatusEffect>
        AppendLocalPlayerStatusEffect(
            IReadOnlyList<WorldEntityStatusEffect> source,
            WorldEntityStatusEffect effect,
            long revision)
    {
        if (effect == null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        if (revision < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(revision));
        }

        IReadOnlyList<WorldEntityStatusEffect> current =
            source ?? Array.Empty<WorldEntityStatusEffect>();
        List<WorldEntityStatusEffect> next =
            new List<WorldEntityStatusEffect>(current.Count + 1);
        for (int index = 0; index < current.Count; index++)
        {
            WorldEntityStatusEffect row = current[index];
            if (row == null)
            {
                throw new ArgumentException(
                    "Local status-effect edges cannot contain null rows.",
                    nameof(source));
            }

            next.Add(row);
        }

        next.Add(effect.WithRevision(revision));
        return new ReadOnlyCollection<WorldEntityStatusEffect>(next);
    }

    internal static bool ShouldPublishLocalPlayerStatusEffect(
        WorldEntityStatusEffect effect)
    {
        return effect.Effect.StartsWith("WF");
    }

    private static bool IsMonsterStatusEffectKind(WorldEntityState entity)
    {
        return entity.Kind != WorldEntityKind.Trap;
    }

    private void PublishRemotePlayerStatusEffectLocked(int entityId, WorldEntityStatusEffect effect,
        bool nativeRegistryHandled = false)
    {
        if (!_remotePlayers.ContainsKey(entityId)) return;
        AppendRemoteEventLocked(WorldRemoteEventKind.StatusFx, entityId, statusEffect: effect,
            nativeRegistryHandled: nativeRegistryHandled);
    }

    private static WorldEntityState ApplyWorldEntityStatusEffect(
        WorldEntityState entity,
        WorldEntityStatusEffect effect)
    {
        if (entity == null)
        {
            throw new ArgumentNullException(nameof(entity));
        }

        if (effect == null)
        {
            throw new ArgumentNullException(nameof(effect));
        }

        if (string.IsNullOrEmpty(effect.Effect))
        {
            return entity;
        }

        long revision = NextWorldStateRevision(entity.StatusEffectsRevision);
        WorldEntityStatusEffect revised = effect.WithRevision(revision);
        List<WorldEntityStatusEffect> rows =
            new List<WorldEntityStatusEffect>(entity.StatusEffects.Count + 1);
        bool replaced = false;
        for (int index = 0; index < entity.StatusEffects.Count; index++)
        {
            WorldEntityStatusEffect current = entity.StatusEffects[index];
            if (!replaced && string.Equals(
                    current.Effect,
                    effect.Effect,
                    StringComparison.Ordinal))
            {
                rows.Add(revised);
                replaced = true;
            }
            else
            {
                rows.Add(current);
            }
        }

        if (!replaced)
        {
            rows.Add(revised);
        }

        return entity.WithStatusEffects(rows, revision);
    }

    private static long NextWorldStateRevision(long revision)
    {
        return revision == long.MaxValue ? long.MaxValue : revision + 1;
    }

    private void PublishInitialGroundItems(
        IReadOnlyList<WorldGroundItemState> states,
        CancellationToken cancellationToken)
    {
        if (states == null)
        {
            throw new ArgumentNullException(nameof(states));
        }

        if (states.Count == 0)
        {
            return;
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Dictionary<int, WorldGroundItemState> next =
                CopyGroundItems(Volatile.Read(ref _groundItems), states.Count);
            int lastAddedGroundId = 0;
            foreach (WorldGroundItemState state in states)
            {
                if (state == null)
                {
                    throw new InvalidDataException(
                        "The staged ground-item snapshot contains a null row.");
                }

                // The official GroundItems.Spawn owner ignores a repeated ground id. Preserve
                // the first authoritative row instead of treating a retransmit as replacement.
                if (!next.ContainsKey(state.GroundId))
                {
                    next.Add(state.GroundId, state);
                    lastAddedGroundId = state.GroundId;
                }
            }

            Volatile.Write(
                ref _groundItems,
                new ReadOnlyDictionary<int, WorldGroundItemState>(next));
            Interlocked.Increment(ref _groundItemsVersion);
            if (lastAddedGroundId > 0)
            {
                RecordGroundLootSpawnLocked(lastAddedGroundId);
            }
        }
    }

    private void PublishGroundItemState(
        WorldGroundItemState state,
        CancellationToken cancellationToken,
        bool replaceExisting = false)
    {
        if (state == null)
        {
            throw new ArgumentNullException(nameof(state));
        }

        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldGroundItemState> current =
                Volatile.Read(ref _groundItems);
            if (!replaceExisting && current.ContainsKey(state.GroundId))
            {
                return;
            }

            Dictionary<int, WorldGroundItemState> next =
                CopyGroundItems(current, 1);
            next[state.GroundId] = state;
            Volatile.Write(
                ref _groundItems,
                new ReadOnlyDictionary<int, WorldGroundItemState>(next));
            Interlocked.Increment(ref _groundItemsVersion);
            RecordGroundLootSpawnLocked(state.GroundId);
        }
    }

    private void RemoveGroundItemState(
        int groundId,
        CancellationToken cancellationToken)
    {
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyDictionary<int, WorldGroundItemState> current =
                Volatile.Read(ref _groundItems);
            if (!current.ContainsKey(groundId))
            {
                return;
            }

            Dictionary<int, WorldGroundItemState> next = CopyGroundItems(current, 0);
            next.Remove(groundId);
            Volatile.Write(
                ref _groundItems,
                new ReadOnlyDictionary<int, WorldGroundItemState>(next));
            Interlocked.Increment(ref _groundItemsVersion);
            RecordGroundLootRemoveLocked(groundId);
        }
    }

    private void RecordGroundLootSpawnLocked(int groundId)
    {
        WorldGroundLootAuthorityState current =
            Volatile.Read(ref _groundLootAuthorityState);
        long sequence = NextGroundLootAuthoritySequenceLocked();
        Volatile.Write(
            ref _groundLootAuthorityState,
            new WorldGroundLootAuthorityState(
                sequence,
                sequence,
                groundId,
                current.LastRemoveSequence,
                current.LastRemoveGroundId,
                current.LastInventorySequence));
    }

    private void RecordGroundLootRemoveLocked(int groundId)
    {
        WorldGroundLootAuthorityState current =
            Volatile.Read(ref _groundLootAuthorityState);
        long sequence = NextGroundLootAuthoritySequenceLocked();
        Volatile.Write(
            ref _groundLootAuthorityState,
            new WorldGroundLootAuthorityState(
                sequence,
                current.LastSpawnSequence,
                current.LastSpawnGroundId,
                sequence,
                groundId,
                current.LastInventorySequence));
    }

    private void RecordGroundLootInventoryLocked()
    {
        WorldGroundLootAuthorityState current =
            Volatile.Read(ref _groundLootAuthorityState);
        long sequence = NextGroundLootAuthoritySequenceLocked();
        Volatile.Write(
            ref _groundLootAuthorityState,
            new WorldGroundLootAuthorityState(
                sequence,
                current.LastSpawnSequence,
                current.LastSpawnGroundId,
                current.LastRemoveSequence,
                current.LastRemoveGroundId,
                sequence));
    }

    private long NextGroundLootAuthoritySequenceLocked()
    {
        if (_groundLootAuthoritySequence < long.MaxValue)
        {
            _groundLootAuthoritySequence++;
        }

        return _groundLootAuthoritySequence;
    }

    private void PublishInitialRemoteWorldStates(
        IReadOnlyList<PendingRemoteWorldState> states,
        int mapId,
        int localEntityId,
        CancellationToken cancellationToken)
    {
        if (states == null) throw new ArgumentNullException(nameof(states));
        lock (_worldStateSync)
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (PendingRemoteWorldState pending in states)
            {
                if (pending.GenericState != null)
                {
                    PublishWorldEntityState(pending.GenericState, cancellationToken, localEntityId,
                        pending.GenericAdmittedOnMainThread);
                }
                else if (pending.IconRows != null)
                {
                    PublishWorldEntityStatusIcons(pending.IconsEntityId, pending.IconRows, cancellationToken);
                }
                else if (pending.PlayerState != null)
                {
                    if (pending.MapIdGuard <= 0 || pending.MapIdGuard == mapId)
                        PublishRemotePlayerState(pending.PlayerState, cancellationToken);
                }
                else if (pending.IsRemoval)
                {
                    RemoveWorldEntityState(pending.RemovedEntityId, cancellationToken, pending.RemoveReason);
                }
                else if (pending.RidingState != null)
                {
                    PublishRemoteRidingState(pending.RidingState, cancellationToken);
                }
                else if (pending.RenameState != null)
                {
                    PublishPlayerRenameLocked(pending.RenameState, localEntityId);
                }
                else if (pending.AppearanceState != null)
                {
                    PublishPlayerAppearanceStateLocked(pending.AppearanceState, localEntityId);
                }
                else if (pending.StatusEffect != null)
                {
                    PendingEntityStatusEffect effect = pending.StatusEffect;
                    PublishWorldEntityStatusEffect(effect.EntityId, effect.Effect,
                        cancellationToken, localEntityId);
                }
                else if (pending.Alive.HasValue)
                {
                    if (!pending.RemoteLifeAtReceive && (pending.LifeEntityId == 0 || pending.LifeEntityId == localEntityId))
                        PublishLocalPlayerLifeStateLocked(localEntityId,
                            pending.Alive.Value, pending.ReviveFlags);
                    else PublishRemotePlayerLifeStateLocked(pending.LifeEntityId, pending.Alive.Value);
                }
                else if (pending.PkState.HasValue)
                {
                    PublishRemotePlayerPkState(pending.PkEntityId, pending.PkState.Value, cancellationToken);
                }
                else if (pending.PathState != null)
                {
                    // Earlier queued spawn/remove operations now define the actual registry here.
                    PublishRemotePathState(pending.PathState, cancellationToken);
                }
                else if (pending.ChatState != null)
                {
                    PendingChannelChat chat = pending.ChatState;
                    PublishChannelChat(chat.SenderEntityId, chat.SenderName, chat.BubbleThemeSelector,
                        chat.ChannelCode, chat.Text, chat.Shares, cancellationToken, chat.SenderLevel, chat.SenderFlag);
                    if (chat.ChannelCode == 0)
                        PublishRemoteChatBubble(chat.SenderEntityId, chat.Text, chat.BubbleThemeSelector, cancellationToken, chat.NativeBubbleHandled);
                }
                else if (pending.EmoteFace.HasValue)
                {
                    PublishEmote(pending.EmoteEntityId, pending.EmoteFace.Value, cancellationToken);
                }
                else if (pending.CombatResult != null)
                {
                    PublishCombatReceive(pending.CombatResult, cancellationToken, localEntityId);
                }
                else if (pending.SkillCastState != null)
                {
                    PublishSkillCastState(pending.SkillCastState, cancellationToken);
                }
                else if (pending.PartyState != null)
                {
                    PublishPartyState(pending.PartyState, cancellationToken);
                }
                else if (pending.FamilyStashState != null)
                {
                    PublishFamilyStashState(pending.FamilyStashState, cancellationToken);
                }
                else if (pending.SocialPacket != null)
                {
                    ProcessWorldPacket(pending.SocialPacket.Value, cancellationToken);
                }
                else if (pending.WarehousePacket != null)
                {
                    ProcessWorldPacket(pending.WarehousePacket.Value, cancellationToken);
                }
                else if (pending.TradePacket != null)
                {
                    ProcessWorldPacket(pending.TradePacket.Value, cancellationToken);
                }
                else if (pending.RackGoodsResponse != null)
                {
                    PublishRackShopGoodsResponse(pending.RackGoodsResponse, cancellationToken);
                }
                else if (pending.RackFittingResponse != null)
                {
                    PublishRackShopFittingResponse(pending.RackFittingResponse, cancellationToken);
                }
                else if (pending.RackDepositInfo != null)
                {
                    PublishRackShopDepositInfo(pending.RackDepositInfo, cancellationToken);
                }
                else if (pending.RackRefundResponse != null)
                {
                    PublishRackShopRefundResponse(pending.RackRefundResponse, cancellationToken);
                }
                else if (pending.RackDescriptions != null)
                {
                    PublishRackShopFittingDescriptions(pending.RackDescriptions, cancellationToken);
                }
                else if (pending.ChangeSetItems != null)
                {
                    PublishChangeSet(pending.ChangeSetItems, cancellationToken);
                }
                else if (pending.FriendState != null)
                {
                    PublishFriendState(pending.FriendState, cancellationToken);
                }
                else if (pending.ApprenticeState != null)
                {
                    PublishApprenticeState(pending.ApprenticeState, cancellationToken);
                }
                else if (pending.BlockState != null)
                {
                    PublishBlockState(pending.BlockState, cancellationToken);
                }
                else if (pending.StallState != null)
                {
                    PublishStallOwner(pending.StallState, cancellationToken);
                }
                else if (pending.IsStallRemoval)
                {
                    RemoveStallOwner(pending.RemovedStallId, cancellationToken);
                }
            }
        }
    }

    private void PublishRemoteWorldSnapshots(
        Dictionary<int, WorldRemotePlayerState> players,
        Dictionary<int, WorldRemoteRidingState> riding,
        bool playersChanged,
        bool ridingChanged)
    {
        if (playersChanged)
        {
            Volatile.Write(
                ref _remotePlayers,
                new ReadOnlyDictionary<int, WorldRemotePlayerState>(players));
            Interlocked.Increment(ref _remotePlayersVersion);
        }

        if (ridingChanged)
        {
            Volatile.Write(
                ref _remoteRidingStates,
                new ReadOnlyDictionary<int, WorldRemoteRidingState>(riding));
            Interlocked.Increment(ref _remoteRidingStatesVersion);
        }
    }

    private static Dictionary<int, WorldRemotePlayerState> CopyRemotePlayers(
        IReadOnlyDictionary<int, WorldRemotePlayerState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldRemotePlayerState> copy =
            new Dictionary<int, WorldRemotePlayerState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldRemotePlayerState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldNpcState> CopyNpcs(
        IReadOnlyDictionary<int, WorldNpcState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldNpcState> copy =
            new Dictionary<int, WorldNpcState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldNpcState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldEntityState> CopyWorldEntities(
        IReadOnlyDictionary<int, WorldEntityState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldEntityState> copy =
            new Dictionary<int, WorldEntityState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldEntityState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldGroundItemState> CopyGroundItems(
        IReadOnlyDictionary<int, WorldGroundItemState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldGroundItemState> copy =
            new Dictionary<int, WorldGroundItemState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldGroundItemState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldRemoteRidingState> CopyRemoteRidingStates(
        IReadOnlyDictionary<int, WorldRemoteRidingState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldRemoteRidingState> copy =
            new Dictionary<int, WorldRemoteRidingState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldRemoteRidingState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldRemotePathState> CopyRemotePaths(
        IReadOnlyDictionary<int, WorldRemotePathState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldRemotePathState> copy =
            new Dictionary<int, WorldRemotePathState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldRemotePathState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldRemoteActionState> CopyRemoteActions(
        IReadOnlyDictionary<int, WorldRemoteActionState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldRemoteActionState> copy =
            new Dictionary<int, WorldRemoteActionState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldRemoteActionState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldRemoteChatBubbleState> CopyRemoteChatBubbles(
        IReadOnlyDictionary<int, WorldRemoteChatBubbleState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldRemoteChatBubbleState> copy =
            new Dictionary<int, WorldRemoteChatBubbleState>(
                source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldRemoteChatBubbleState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static Dictionary<int, WorldStallOwnerState> CopyStallOwners(
        IReadOnlyDictionary<int, WorldStallOwnerState> source,
        int additionalCapacity)
    {
        Dictionary<int, WorldStallOwnerState> copy =
            new Dictionary<int, WorldStallOwnerState>(source.Count + additionalCapacity);
        foreach (KeyValuePair<int, WorldStallOwnerState> pair in source)
        {
            copy[pair.Key] = pair.Value;
        }

        return copy;
    }

    private static IReadOnlyList<WorldCombatResultState> AppendRetainedCombatResult(
        IReadOnlyList<WorldCombatResultState> source,
        WorldCombatResultState value)
    {
        int skip = Math.Max(0, source.Count + 1 - MaximumRetainedCombatEdges);
        List<WorldCombatResultState> next = new List<WorldCombatResultState>(
            Math.Min(MaximumRetainedCombatEdges, source.Count + 1));
        for (int index = skip; index < source.Count; index++)
        {
            next.Add(source[index]);
        }

        next.Add(value);
        return new ReadOnlyCollection<WorldCombatResultState>(next);
    }

    private static IReadOnlyList<WorldCombatNumberState> AppendRetainedCombatNumber(
        IReadOnlyList<WorldCombatNumberState> source,
        WorldCombatNumberState value)
    {
        int skip = Math.Max(0, source.Count + 1 - MaximumRetainedCombatEdges);
        List<WorldCombatNumberState> next = new List<WorldCombatNumberState>(
            Math.Min(MaximumRetainedCombatEdges, source.Count + 1));
        for (int index = skip; index < source.Count; index++)
        {
            next.Add(source[index]);
        }

        next.Add(value);
        return new ReadOnlyCollection<WorldCombatNumberState>(next);
    }

    private void ClearRemotePathLocked(int entityId)
    {
        IReadOnlyDictionary<int, WorldRemotePathState> current =
            Volatile.Read(ref _remotePaths);
        if (!current.ContainsKey(entityId))
        {
            return;
        }

        Dictionary<int, WorldRemotePathState> next =
            CopyRemotePaths(current, 0);
        next.Remove(entityId);
        Volatile.Write(
            ref _remotePaths,
            new ReadOnlyDictionary<int, WorldRemotePathState>(next));
        Interlocked.Increment(ref _remotePathsVersion);
    }

    private void ClearMonsterPathLocked(int entityId)
    {
        IReadOnlyDictionary<int, WorldRemotePathState> current =
            Volatile.Read(ref _monsterPaths);
        if (!current.ContainsKey(entityId))
        {
            return;
        }

        Dictionary<int, WorldRemotePathState> next =
            CopyRemotePaths(current, 0);
        next.Remove(entityId);
        Volatile.Write(
            ref _monsterPaths,
            new ReadOnlyDictionary<int, WorldRemotePathState>(next));
        Interlocked.Increment(ref _monsterPathsVersion);
    }

    private void ClearRemoteActionLocked(int entityId)
    {
        IReadOnlyDictionary<int, WorldRemoteActionState> current =
            Volatile.Read(ref _remoteActions);
        if (!current.ContainsKey(entityId))
        {
            return;
        }

        Dictionary<int, WorldRemoteActionState> next =
            CopyRemoteActions(current, 0);
        next.Remove(entityId);
        Volatile.Write(
            ref _remoteActions,
            new ReadOnlyDictionary<int, WorldRemoteActionState>(next));
        Interlocked.Increment(ref _remoteActionsVersion);
    }

    private void ClearRemoteChatBubbleLocked(int entityId)
    {
        IReadOnlyDictionary<int, WorldRemoteChatBubbleState> current =
            Volatile.Read(ref _remoteChatBubbles);
        if (!current.ContainsKey(entityId))
        {
            return;
        }

        Dictionary<int, WorldRemoteChatBubbleState> next =
            CopyRemoteChatBubbles(current, 0);
        next.Remove(entityId);
        Volatile.Write(
            ref _remoteChatBubbles,
            new ReadOnlyDictionary<int, WorldRemoteChatBubbleState>(next));
        Interlocked.Increment(ref _remoteChatBubblesVersion);
    }

    private long NextRemoteMovementActionRevisionLocked()
    {
        _remoteMovementActionRevision = checked(_remoteMovementActionRevision + 1);
        return _remoteMovementActionRevision;
    }

    private CancellationTokenSource CreateResponseTimeout(CancellationToken cancellationToken)
    {
        CancellationTokenSource timeout =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(_options.ResponseTimeout);
        return timeout;
    }

    private static IReadOnlyList<ServerCharacterRecord> ReadRoster(ProtocolPacket packet)
    {
        // Official NetClient.Dispatch opcode 0x8004: the switch jump table maps 0x8004 to the
        // arm64 block 0x250df8 (F32 at 0x250ed4 and 0x250ee4) and to the x86_64 block 0x25f5e2
        // (F32 at 0x25f70e and 0x25f724). Both coordinates stay floats - neither branch contains
        // fcvtzs, cvttss2si or any other float-to-integer conversion, and arm64 stores them with
        // `str s0, [sp, #0x1090]` / `str s0, [sp, #0x1094]` inside the 0x68-byte row record that
        // is copied into the roster array - so the reader keeps them as floats. The row order,
        // the attribute count/loop and the trailing I64 are read by the same Buf cursor, and the
        // branch ends with no trailing-byte check after the last row.
        //
        // Adaptation boundary, not native Dispatch semantics: NetClient.Buf reads the payload
        // through the raw array indexer, so a truncated roster surfaces as
        // IndexOutOfRangeException instead of the EndOfStreamException the previous WireReader
        // raised. The adapter maps it back onto the same IOException so the roster callers keep
        // reporting an invalid response instead of letting it escape. The catch is typed, not a
        // blanket catch(Exception): the loop body only indexes the payload through the Buf
        // cursor (ReadAttributes allocates int[count] and fills indexes below count, and
        // ServerCharacterRecord is a value with inline appearance fields), so no business exception is captured.
        // The mapping preserves the old external contract of this helper; it is not evidence
        // that the official Dispatch drops a short roster packet.
        try
        {
            NetClient.Buf reader = new NetClient.Buf(packet.Payload);
            int count = reader.U8();
            ServerCharacterRecord[] characters = new ServerCharacterRecord[count];
            for (int index = 0; index < count; index++)
            {
                ServerCharacterRecord row = new ServerCharacterRecord
                {
                    name = reader.Str(),
                    race = reader.U8(),
                    gender = reader.U8(),
                    head = reader.U8(),
                    hair = reader.U8(),
                    level = reader.I32(),
                    mapId = reader.I32(),
                    x = reader.F32(),
                    y = reader.F32(),
                    lastLogin = reader.Str(),
                    apBody = reader.U16(),
                    apCap = reader.U16(),
                    apBackpack = reader.U16(),
                    apWeaponR = reader.U16(),
                    apWeaponL = reader.U16(),
                    apFace = reader.U16(),
                    atkVariant = reader.U8(),
                    weaponCType = reader.U8(),
                    atkDist = reader.U16(),
                    attrs = ReadAttributes(reader),
                    honor = reader.I64()
                };
                characters[index] = row;
            }

            return characters;
        }
        catch (IndexOutOfRangeException)
        {
            throw new EndOfStreamException("Wire payload ended unexpectedly.");
        }
    }

    private static CharacterAppearanceRecord ReadAppearance(NetClient.Buf reader)
    {
        return new CharacterAppearanceRecord(
            reader.U16(),
            reader.U16(),
            reader.U16(),
            reader.U16(),
            reader.U16(),
            reader.U16(),
            reader.U8(),
            reader.U8(),
            reader.U16());
    }

    private static int[] ReadAttributes(NetClient.Buf reader)
    {
        int count = reader.U16();
        int[] attributes = new int[count];
        for (int index = 0; index < count; index++)
        {
            attributes[index] = reader.I32();
        }

        return attributes;
    }

    // Official 1.5.8 NetClient.Dispatch 0x8065 case (arm64 0x24c87c, x86_64 0x25a2b7): the case
    // reads exactly two values from one NetClient.Buf - U8 then I32 - with no remainder check,
    // no value filter and no validation wrapper. The byte becomes the bool argument with `!= 0`
    // (arm64 24c8bc cmp w20,#0 / 24c8c0 cset w0,ne; x86 25a2f3 testl %r14d / 25a2f6 setne %al)
    // and the pair is passed to AccountSec.Seed (arm64 tail b 0x20d8bc, x86_64 jmp 0x219c90).
    // A null payload raises NullReferenceException (arm64 24c880 cbz x19 -> bl 0x177a74,
    // x86 25a2ba je -> callq 0x16c1b0) and a short payload raises from the Buf read itself;
    // neither is converted into a protocol-level failure, so an extra tail is ignored, any
    // non-zero byte means "has code" and a negative window is forwarded unchanged.
    private void PublishAccountSecuritySeed(ProtocolPacket packet)
    {
        var reader = new NetClient.Buf(packet.Payload);
        bool hasSecurityCode = reader.U8() != 0;
        int windowMilliseconds = reader.I32();

        PublishAccountSecuritySeed(hasSecurityCode, windowMilliseconds);
    }

    private bool TryTakePendingDownlink(
        ushort opcode,
        out ProtocolPacket packet)
    {
        lock (_pendingDownlinkSync)
        {
            for (int index = 0; index < _pendingDownlinks.Count; index++)
            {
                if (_pendingDownlinks[index].Opcode != opcode)
                {
                    continue;
                }

                packet = _pendingDownlinks[index];
                _pendingDownlinks.RemoveAt(index);
                return true;
            }
        }

        packet = default(ProtocolPacket);
        return false;
    }

    private bool TryTakeOldestPendingDownlink(out ProtocolPacket packet)
    {
        lock (_pendingDownlinkSync)
        {
            if (_pendingDownlinks.Count > 0)
            {
                packet = _pendingDownlinks[0];
                _pendingDownlinks.RemoveAt(0);
                return true;
            }
        }

        packet = default(ProtocolPacket);
        return false;
    }

    private void RetainPendingDownlink(ProtocolPacket packet)
    {
        ProtocolPacket dropped = default(ProtocolPacket);
        bool droppedOldest = false;
        lock (_pendingDownlinkSync)
        {
            if (_pendingDownlinks.Count >= MaximumPacketsBeforeExpectedResponse)
            {
                dropped = _pendingDownlinks[0];
                _pendingDownlinks.RemoveAt(0);
                droppedOldest = true;
            }

            _pendingDownlinks.Add(packet);
        }

        if (droppedOldest)
        {
            LogIgnoredDownlink("pending-overflow", dropped);
        }
    }

    private static bool IsWireFailure(Exception exception)
    {
        return exception is IOException ||
               exception is SocketException ||
               exception is CryptographicException ||
               exception is ObjectDisposedException ||
               exception is InvalidOperationException ||
               exception is ArgumentException;
    }

    /// <summary>
    /// Runs the disposal core on the host thread. Off the host the close is posted and the
    /// returned task completes when <see cref="DisposeConnectionCore"/> really ran, so no
    /// caller reports a closed transport before the native close happened.
    /// </summary>
    private Task DisposeConnectionAsync()
    {
        return _host.RunAsync(DisposeConnectionCore, CancellationToken.None);
    }

    /// <summary>
    /// Runs the native transport close plus the independent-runtime teardown on the host
    /// thread. Used by the connect timeout/cancel suite, which closes the in-flight client
    /// instead of running the whole session disposal: the official Close body keeps no
    /// handle to a connection that is still being established, so the in-flight abort lives
    /// in <see cref="NetClient.CloseIndependentRuntime"/>. Both run inside one host action,
    /// so the returned task still completes only after the close really happened.
    /// </summary>
    private Task CloseTransportAsync()
    {
        return _host.RunAsync(
            () =>
            {
                _netClient.Close();
                _netClient.CloseIndependentRuntime();
            },
            CancellationToken.None);
    }

    /// <summary>
    /// The real disposal body. It must run on the host thread: the native NetClient.Close it
    /// ends with clears _txPending and resets the transmit rate window, which the host-side
    /// DrainTx reads. Callers on the host thread (Pump -> Dispatch -> OnTransportError and
    /// the world drain) run it inline; every other caller goes through
    /// <see cref="DisposeConnectionAsync"/>.
    /// </summary>
    private void DisposeConnectionCore()
    {
        DisposeConnectionCore(true);
    }

    private void DisposeConnectionCore(bool closeNativeConnection)
    {
        ResetEndgameConnection();
        lock (_worldDrainSync)
        {
            _stage = GatewayStage.Disconnected;
            Interlocked.Exchange(ref _worldEntryAdmission, null);
            _worldDrainActive = false;
        }
        _ = ObserveWorldDrainStopAsync(StopWorldDrainAsync());
        lock (_pendingDownlinkSync)
        {
            _pendingDownlinks.Clear();
        }
        while (_dispatchedDownlinks.TryDequeue(out _))
        {
        }
        while (_dispatchedReady.Wait(0))
        {
        }
        CancellationTokenSource lifetime = Interlocked.Exchange(
            ref _connectionLifetime,
            null);
        lifetime?.Cancel();
        lock (_worldStateSync)
        {
            if (Volatile.Read(ref _teleportStates).Count > 0)
            {
                Volatile.Write(
                    ref _teleportStates,
                    Array.Empty<WorldTeleportState>());
                Interlocked.Increment(ref _teleportStatesVersion);
            }

            _teleportSequence = 0;

            if (Interlocked.Exchange(ref _playerStatusState, null) != null)
            {
                Interlocked.Increment(ref _playerStatusStateVersion);
            }

            if (Interlocked.Exchange(ref _playerAppearanceState, null) != null)
            {
                Interlocked.Increment(ref _playerAppearanceStateVersion);
            }

            if (!ReferenceEquals(
                    Volatile.Read(ref _characterTitleState),
                    WorldCharacterTitleState.Empty))
            {
                Volatile.Write(
                    ref _characterTitleState,
                    WorldCharacterTitleState.Empty);
                Interlocked.Increment(ref _characterTitleStateVersion);
            }

            if (Interlocked.Exchange(ref _localPlayerLifeState, null) != null)
            {
                Interlocked.Increment(ref _localPlayerLifeStateVersion);
            }

            _localPlayerLifeRevision = 0;

            _localStatusEffectConnectionGeneration = checked(_localStatusEffectConnectionGeneration + 1);
            if (_localPlayerStatusEffects.Count > 0)
            {
                _localPlayerStatusEffects.Clear();
                Interlocked.Increment(ref _localPlayerStatusEffectsVersion);
            }

            _localPlayerStatusEffectRevision = 0;
            _localStatusEffectAcknowledgedRevision = 0;

            if (Volatile.Read(ref _skillHitEffects).Count > 0)
            {
                Volatile.Write(
                    ref _skillHitEffects,
                    Array.Empty<WorldSkillHitEffect>());
                Interlocked.Increment(ref _skillHitEffectsVersion);
            }
            _skillHitEffectRevision = 0;

            if (Interlocked.Exchange(ref _selfPkState, null) != null)
            {
                Interlocked.Increment(ref _selfPkStateVersion);
            }

            if (Interlocked.Exchange(ref _localFactionState, null) != null)
            {
                Interlocked.Increment(ref _localFactionStateVersion);
            }

            if (Interlocked.Exchange(ref _localRidingState, null) != null)
            {
                Interlocked.Increment(ref _localRidingStateVersion);
            }

            // Drop only the session observation. This is not an 805e or a native HUD reset.
            if ((Interlocked.Exchange(ref _targetStatusState, null) != null) |
                (Interlocked.Exchange(ref _targetStatusObservedPayload, null) != null))
            {
                Interlocked.Increment(ref _targetStatusStateVersion);
            }

            if (_buffObservedPayload != null || !ReferenceEquals(Volatile.Read(ref _buffState), WorldBuffState.Empty))
            {
                _buffObservedPayload = null;
                Volatile.Write(ref _buffState, WorldBuffState.Empty);
                Interlocked.Increment(ref _buffStateVersion);
            }

            if (Interlocked.Exchange(ref _inspectedPlayerEquipmentState, null) != null)
            {
                Interlocked.Increment(ref _inspectedPlayerEquipmentStateVersion);
            }

            IReadOnlyDictionary<int, WorldRemoteRidingState> remote =
                Volatile.Read(ref _remoteRidingStates);
            if (remote.Count > 0)
            {
                Volatile.Write(
                    ref _remoteRidingStates,
                    new ReadOnlyDictionary<int, WorldRemoteRidingState>(
                        new Dictionary<int, WorldRemoteRidingState>()));
                Interlocked.Increment(ref _remoteRidingStatesVersion);
            }

            IReadOnlyDictionary<int, WorldRemotePlayerState> remotePlayers =
                Volatile.Read(ref _remotePlayers);
            if (remotePlayers.Count > 0)
            {
                Volatile.Write(
                    ref _remotePlayers,
                    new ReadOnlyDictionary<int, WorldRemotePlayerState>(
                        new Dictionary<int, WorldRemotePlayerState>()));
                Interlocked.Increment(ref _remotePlayersVersion);
            }

            IReadOnlyDictionary<int, WorldNpcState> npcs = Volatile.Read(ref _npcs);
            if (npcs.Count > 0)
            {
                Volatile.Write(
                    ref _npcs,
                    new ReadOnlyDictionary<int, WorldNpcState>(
                        new Dictionary<int, WorldNpcState>()));
                Interlocked.Increment(ref _npcsVersion);
            }

            bool pendingTaskOffers = _npcTaskOffersObservationPayload != null;
            _npcTaskOffersObservationPayload = null;
            if (Interlocked.Exchange(ref _npcTaskOffersState, null) != null || pendingTaskOffers)
            {
                Interlocked.Increment(ref _npcTaskOffersStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _shopState), WorldShopState.Empty))
            {
                Volatile.Write(ref _shopState, WorldShopState.Empty);
                Interlocked.Increment(ref _shopStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _transportState), WorldTransportState.Empty))
            {
                Volatile.Write(ref _transportState, WorldTransportState.Empty);
                Interlocked.Increment(ref _transportStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _warehouseState), WorldWarehouseState.Empty))
            {
                Volatile.Write(ref _warehouseState, WorldWarehouseState.Empty);
                Interlocked.Increment(ref _warehouseStateVersion);
            }

            if (Interlocked.Exchange(ref _npcChatterState, null) != null)
            {
                Interlocked.Increment(ref _npcChatterStateVersion);
            }

            WorldTaskLogState taskLog = Volatile.Read(ref _taskLogState);
            bool pendingTaskLog = _taskLogObservationPayload != null;
            bool taskLogHasEntries = pendingTaskLog
                ? _taskLogObservationHasEntries
                : taskLog.Active.Count > 0 || taskLog.Done.Count > 0;
            _taskLogObservationPayload = null;
            _taskLogObservationHasEntries = false;
            if (pendingTaskLog || taskLogHasEntries)
            {
                Volatile.Write(ref _taskLogState, WorldTaskLogState.Empty);
                if (taskLogHasEntries) Interlocked.Increment(ref _taskLogStateVersion);
            }

            if (Interlocked.Exchange(ref _npcDialogTextState, null) != null)
            {
                Interlocked.Increment(ref _npcDialogTextStateVersion);
            }

            WorldNpcDialogTextEdgeWindow dialogTextEdges =
                Volatile.Read(ref _npcDialogTextEdgeWindow);
            if (dialogTextEdges.Edges.Count > 0)
            {
                Volatile.Write(
                    ref _npcDialogTextEdgeWindow,
                    new WorldNpcDialogTextEdgeWindow(
                        dialogTextEdges.Version + 1,
                        dialogTextEdges.LastRevision,
                        Array.Empty<WorldNpcDialogTextEdge>()));
            }

            IReadOnlyDictionary<int, WorldEntityState> worldEntities =
                Volatile.Read(ref _worldEntities);
            IReadOnlyDictionary<int, WorldEntityState> emptyWorldEntities =
                new ReadOnlyDictionary<int, WorldEntityState>(
                    new Dictionary<int, WorldEntityState>());
            if (worldEntities.Count > 0)
            {
                Interlocked.Increment(ref _worldEntitiesVersion);
            }

            long snapshotVersion = Interlocked.Read(ref _worldEntitiesVersion);
            WorldEntityPublicationState disconnectedPublication =
                new WorldEntityPublicationState(
                    0,
                    0,
                    0,
                    0,
                    0,
                    0,
                    snapshotVersion,
                    0);
            PublishWorldEntitySceneSnapshotLocked(
                emptyWorldEntities,
                snapshotVersion,
                disconnectedPublication);

            IReadOnlyDictionary<int, WorldGroundItemState> groundItems =
                Volatile.Read(ref _groundItems);
            if (groundItems.Count > 0)
            {
                Volatile.Write(
                    ref _groundItems,
                    new ReadOnlyDictionary<int, WorldGroundItemState>(
                        new Dictionary<int, WorldGroundItemState>()));
                Interlocked.Increment(ref _groundItemsVersion);
            }

            Volatile.Write(
                ref _groundLootAuthorityState,
                WorldGroundLootAuthorityState.Empty);
            _groundLootAuthoritySequence = 0;

            if (Volatile.Read(ref _combatResults).Count > 0)
            {
                Volatile.Write(
                    ref _combatResults,
                    Array.Empty<WorldCombatResultState>());
                Interlocked.Increment(ref _combatResultsVersion);
            }

            ClearCombatNumberPresentationJournal();
            _combatResultRevision = 0;

            IReadOnlyDictionary<int, WorldRemotePathState> remotePaths =
                Volatile.Read(ref _remotePaths);
            if (remotePaths.Count > 0)
            {
                Volatile.Write(
                    ref _remotePaths,
                    new ReadOnlyDictionary<int, WorldRemotePathState>(
                        new Dictionary<int, WorldRemotePathState>()));
                Interlocked.Increment(ref _remotePathsVersion);
            }

            IReadOnlyDictionary<int, WorldRemoteActionState> remoteActions =
                Volatile.Read(ref _remoteActions);
            if (remoteActions.Count > 0)
            {
                Volatile.Write(
                    ref _remoteActions,
                    new ReadOnlyDictionary<int, WorldRemoteActionState>(
                        new Dictionary<int, WorldRemoteActionState>()));
                Interlocked.Increment(ref _remoteActionsVersion);
            }

            IReadOnlyDictionary<int, WorldRemotePathState> monsterPaths =
                Volatile.Read(ref _monsterPaths);
            if (monsterPaths.Count > 0)
            {
                Volatile.Write(
                    ref _monsterPaths,
                    new ReadOnlyDictionary<int, WorldRemotePathState>(
                        new Dictionary<int, WorldRemotePathState>()));
                Interlocked.Increment(ref _monsterPathsVersion);
            }

            _remoteMovementActionRevision = 0;

            if (Interlocked.Exchange(ref _skillCastState, null) != null)
            {
                Interlocked.Increment(ref _skillCastStateVersion);
            }

            if (Volatile.Read(ref _skillCastEvents).Count > 0)
            {
                Volatile.Write(
                    ref _skillCastEvents,
                    Array.Empty<WorldSkillCastState>());
                Interlocked.Increment(ref _skillCastStateVersion);
            }

            _skillCastRevision = 0;

            IReadOnlyDictionary<int, WorldRemoteChatBubbleState> remoteChatBubbles =
                Volatile.Read(ref _remoteChatBubbles);
            if (remoteChatBubbles.Count > 0)
            {
                Volatile.Write(
                    ref _remoteChatBubbles,
                    new ReadOnlyDictionary<int, WorldRemoteChatBubbleState>(
                        new Dictionary<int, WorldRemoteChatBubbleState>()));
                Interlocked.Increment(ref _remoteChatBubblesVersion);
            }

            _remoteChatBubbleRevision = 0;

            IReadOnlyList<WorldChannelChatState> channelChatRows =
                Volatile.Read(ref _channelChatRows);
            if (channelChatRows.Count > 0)
            {
                Volatile.Write(
                    ref _channelChatRows,
                    Array.Empty<WorldChannelChatState>());
                Interlocked.Increment(ref _channelChatRowsVersion);
            }

            _channelChatRevision = 0;

            IReadOnlyList<WorldPrivateChatState> privateChatRows =
                Volatile.Read(ref _privateChatRows);
            if (privateChatRows.Count > 0)
            {
                Volatile.Write(
                    ref _privateChatRows,
                    Array.Empty<WorldPrivateChatState>());
                Interlocked.Increment(ref _privateChatRowsVersion);
            }

            _privateChatRevision = 0;

            if (!ReferenceEquals(Volatile.Read(ref _familyStashState), WorldFamilyStashState.Empty))
            {
                Volatile.Write(ref _familyStashState, WorldFamilyStashState.Empty);
                Interlocked.Increment(ref _familyStashStateVersion);
            }
            if (!ReferenceEquals(Volatile.Read(ref _helpTopicsState), WorldHelpTopicsState.Empty))
            {
                Volatile.Write(ref _helpTopicsState, WorldHelpTopicsState.Empty);
                Interlocked.Increment(ref _helpTopicsStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _rackShopState), WorldRackShopState.Empty))
            {
                Volatile.Write(ref _rackShopState, WorldRackShopState.Empty);
                Interlocked.Increment(ref _rackShopGoodsVersion);
                Interlocked.Increment(ref _rackShopStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _duelState), WorldDuelState.Empty))
            {
                Volatile.Write(ref _duelState, WorldDuelState.Empty);
                Interlocked.Increment(ref _duelStateVersion);
            }

            ResetMailStateLocked();

            if (!ReferenceEquals(Volatile.Read(ref _friendState), WorldFriendState.Empty))
            {
                Volatile.Write(ref _friendState, WorldFriendState.Empty);
                Interlocked.Increment(ref _friendStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _apprenticeState), WorldApprenticeState.Empty))
            {
                Volatile.Write(ref _apprenticeState, WorldApprenticeState.Empty);
                Interlocked.Increment(ref _apprenticeStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _blockState), WorldBlockState.Empty))
            {
                Volatile.Write(ref _blockState, WorldBlockState.Empty);
                Interlocked.Increment(ref _blockStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _partyState), WorldPartyState.Empty))
            {
                Volatile.Write(ref _partyState, WorldPartyState.Empty);
                Interlocked.Increment(ref _partyStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _inventoryState), WorldInventoryState.Empty))
            {
                Volatile.Write(ref _inventoryState, WorldInventoryState.Empty);
                Interlocked.Increment(ref _inventoryStateVersion);
            }

            if (!ReferenceEquals(Volatile.Read(ref _wardrobeState), WorldWardrobeState.Empty))
            {
                Volatile.Write(ref _wardrobeState, WorldWardrobeState.Empty);
                Interlocked.Increment(ref _wardrobeStateVersion);
            }

            // WorldHotbarState is deliberately NOT reset to Empty here.
            // The only official caller of GameHud.LoadHotbar (0x06000445) in the frozen 1.5.8
            // slices is the 0x8016 arm of NetClient.Dispatch (0x0600077f): the frozen call
            // index records exactly one edge to it, the arm64 tail call 0x256f78
            // `b 0x44197c`, and no teardown body reaches it. GameBootstrap
            // .TeardownWorldUi (arm64 0x426a2c, 1628 bytes, disassembled in full) clears only
            // CharData +0x231 riding, +0x24c mountModel, +0x260 riderSeat, +0x264 mountTianmo
            // and +0x268 rideAnchorId (arm64 0x426bf4-0x426c08); it never writes CharData
            // .petSkillIcons (+0x280) and never touches a GameHud root. NetClient.Close
            // (0x060006be) leaves those static roots unchanged; NetClient.OnDestroy
            // (0x060006bf) is its tail caller, as the WBB-061 proof below records. Scope:
            // the teardown and disconnect bodies listed here plus the single LoadHotbar call
            // edge; no whole-binary sweep for writers of the hotbar statics was run, so this
            // is not a proof that no other official body ever writes them.
            // Publishing Empty here did reach the official statics: the version bump makes
            // ReferenceFirstWorldWindowOwner.SyncHotbarState apply it, and
            // ReferenceFirstWorldHotbar.ApplyState then called LoadHotbar with all thirty slots
            // zero, expanded false and, because LoadHotbar clears the row only when collapsed,
            // hotbarRow = 0. That is an official-side store the official client never performs
            // - the official hotbar keeps the last received snapshot until the next 0x8016 -
            // so the reset is removed and the published snapshot follows received packets only.
            // The second half of the same fix is that ApplyState no longer performs any official
            // store at all: it binds the UI mirror and leaves CharData.petSkillIcons (+0x280) and
            // the GameHud hotbar statics to the IHotbarReceiveOwner raw receipt operations,
            // which run only for a received 0x8016, never for a "no packet yet" sentinel.

            // WorldSkillBookState is deliberately NOT reset to Empty here, the same shape as the
            // hotbar root above. WBB-061 verified on both frozen 1.5.8 slices (arm64 + x86_64)
            // that no teardown or disconnect body writes the three CharData roots this state is
            // projected into (skillPoints +0x40, skills +0x48, skillDmgBonus +0x50):
            // TitleFlow.DoEnterWorld 0x06000efd, GameBootstrap.TeardownWorldUi 0x060003f3 and its
            // g__Purge|155_0 0x060003f7, NetClient.Close 0x060006be and NetClient.OnDestroy
            // 0x060006bf (a 4-byte arm64 / 16-byte x86_64 tail branch to Close, not an empty
            // body), plus every direct callee of those bodies and all 23 CharData members. Inside
            // that scope the only writers of the three roots are CharData..cctor 0x06000f2d
            // (empty-dictionary initial value) and the 0x8015 case of NetClient.Dispatch
            // 0x0600077f, which replaces both dictionaries whole. Scope of the claim: exactly
            // those bodies, their direct callees and the CharData members enumerated in the
            // WBB-061 report; no whole-binary sweep for other direct static writers was run, so
            // this is not a proof that no other official body ever writes them.
            // Publishing Empty here was an official-side store the official client never
            // performs: the version bump makes the window owner push Empty into
            // Win05.SkillDialog.skillBook and the per-frame projector then rebinds
            // CharData.skills / skillDmgBonus to empty and CharData.skillPoints to 0, so a
            // disconnect blanked the skill window until the next 0x8015. With the reset removed
            // the published snapshot follows received packets only.
            // The no-packet mirror starts Empty, matching the official cctor value, but is no
            // longer projected back into native roots. A real 0-row receipt still replaces both
            // tables; a failed required read replaces only the reached roots. The skill owner
            // binding and any unbound reached writes are likewise retained across disconnect.

            // Win05.lifeData (+0x38) and Win05.lifeCanLearn (+0x40) are deliberately not reset
            // here. Dual-architecture enumeration of the frozen 1.5.8 slices (arm64 + x86_64,
            // every method body of the managed assembly) finds exactly two writers of those two
            // static slots: Win05..cctor 0x0600105e (empty Dictionary / empty HashSet) and
            // Win05.SetLifeSkills 0x0600101d, whose only call site in both slices is the 0x8022
            // case of NetClient.Dispatch 0x0600077f. That case always passes the two collections
            // it just allocated, so the official clear of the learned dictionary is a real 0x8022
            // snapshot with no row at level >= 0 (no row at all, or every row at level < 0); the
            // learnable HashSet of that same packet is independent and keeps its eligible rows.
            // No disconnect, map-change or world-exit path writes either root.
            // Resetting them on teardown would publish an official-side store that never
            // happens (the window owner syncs it into the skill dialog on the next frame), so
            // the life-skill page follows received 0x8022 snapshots only.

            IReadOnlyDictionary<int, WorldStallOwnerState> stallOwners =
                Volatile.Read(ref _stallOwners);
            if (stallOwners.Count > 0)
            {
                Volatile.Write(
                    ref _stallOwners,
                    new ReadOnlyDictionary<int, WorldStallOwnerState>(
                        new Dictionary<int, WorldStallOwnerState>()));
                Interlocked.Increment(ref _stallOwnersVersion);
            }

            ResetSystemRequestsObservation();

            _warehouseUpdates.Clear();
            Volatile.Write(ref _changeSet, Array.Empty<WorldChangeSetItem>());
            Interlocked.Increment(ref _changeSetVersion);
            _socialUiUpdates.Clear();
            WorldActiveTradeState activeTrade = ReadActiveTradeState();
            _tradeStateObservationOwner = null;
            _tradeStateObservationDirty = false;
            if (activeTrade != null && activeTrade.Active)
            {
                Volatile.Write(ref _activeTradeState, TradeResetMirror(WorldActiveTradeState.Empty, activeTrade));
                Interlocked.Increment(ref _activeTradeStateVersion);
            }

            if (Interlocked.Exchange(ref _tradeEndState, null) != null)
            {
                Interlocked.Increment(ref _tradeEndStateVersion);
            }

            Volatile.Write(ref _currentWorldMapId, 0);
            Volatile.Write(ref _currentWorldEntityId, 0);
            _localRename = null;
            ResetRemoteEventSceneLocked(0);
        }

        // Ordinary disposal runs official 0x060006BE first. Terminal bookkeeping
        // leaves that call to the bound component's actual Unity OnDestroy.
        // The remaining product-only independent-runtime teardown is unchanged:
        // the in-flight connect handle, the Tx/Rx thread fields, the session keys, _sendSeq,
        // _nextPingAt and the inbox drain. The official body knows none of them, and only the
        // session teardown may drop them - the official Update/OnDestroy intent stays on
        // Close() alone.
        if (closeNativeConnection) _netClient.Close();
        _netClient.CloseIndependentRuntime();
        if (lifetime != null)
        {
            lifetime.Dispose();
        }
    }

    private sealed class EnterWorldResponse
    {
        public EnterWorldResponse(
            ProtocolPacket packet,
            IReadOnlyList<PendingRemoteWorldState> remoteWorldStates,
            IReadOnlyList<WorldGroundItemState> groundItems,
            IReadOnlyList<PendingChannelChat> channelChats,
            WorldSelfPkState selfPkState,
            WorldLocalFactionState localFactionState,
            WorldPartyState partyState,
            WorldTaskLogState taskLogState,
            WorldNpcChatterState npcChatterState,
            WorldInventoryState inventoryState,
            WorldWardrobeState wardrobeState,
            WorldSkillBookObservation skillBookState,
            WorldLifeSkillBookState lifeSkillBookState,
            byte[] buffStatePayload,
            bool buffStateNonEmpty,
            WorldPlayerAttributeSnapshot attributeSnapshot,
            WorldPlayerPetStatusState petStatus)
        {
            Packet = packet;
            RemoteWorldStates = remoteWorldStates ??
                throw new ArgumentNullException(nameof(remoteWorldStates));
            GroundItems = groundItems ??
                throw new ArgumentNullException(nameof(groundItems));
            ChannelChats = channelChats ??
                throw new ArgumentNullException(nameof(channelChats));
            SelfPkState = selfPkState;
            LocalFactionState = localFactionState;
            PartyState = partyState;
            TaskLogState = taskLogState;
            NpcChatterState = npcChatterState;
            InventoryState = inventoryState;
            WardrobeState = wardrobeState;
            SkillBookState = skillBookState;
            LifeSkillBookState = lifeSkillBookState;
            BuffStatePayload = buffStatePayload;
            BuffStateNonEmpty = buffStateNonEmpty;
            AttributeSnapshot = attributeSnapshot;
            PetStatus = petStatus;
        }

        public ProtocolPacket Packet { get; }

        public IReadOnlyList<PendingRemoteWorldState> RemoteWorldStates { get; }

        public IReadOnlyList<WorldGroundItemState> GroundItems { get; }

        public IReadOnlyList<PendingChannelChat> ChannelChats { get; }

        public WorldSelfPkState SelfPkState { get; }

        public WorldLocalFactionState LocalFactionState { get; }

        public WorldPartyState PartyState { get; }

        public WorldTaskLogState TaskLogState { get; }

        public WorldNpcChatterState NpcChatterState { get; }

        public WorldInventoryState InventoryState { get; }

        public WorldWardrobeState WardrobeState { get; }

        public WorldSkillBookObservation SkillBookState { get; }

        public WorldLifeSkillBookState LifeSkillBookState { get; }

        public byte[] BuffStatePayload { get; }

        public bool BuffStateNonEmpty { get; }

        public WorldPlayerAttributeSnapshot AttributeSnapshot { get; }

        public WorldPlayerPetStatusState PetStatus { get; }
    }

    private sealed class PendingEntityStatusEffect
    {
        public PendingEntityStatusEffect(int entityId, WorldEntityStatusEffect effect)
        {
            EntityId = entityId;
            Effect = effect ?? throw new ArgumentNullException(nameof(effect));
        }

        public int EntityId { get; }
        public WorldEntityStatusEffect Effect { get; }
    }

    internal sealed class PendingChannelChat
    {
        public PendingChannelChat(
            int senderEntityId,
            string senderName,
            byte bubbleThemeSelector,
            byte channelCode,
            string text,
            IReadOnlyList<WorldChatSharedEntryState> shares, byte senderLevel, byte senderFlag)
        {
            SenderLevel = senderLevel; SenderFlag = senderFlag;
            SenderEntityId = senderEntityId;
            SenderName = senderName ?? throw new ArgumentNullException(nameof(senderName));
            BubbleThemeSelector = bubbleThemeSelector;
            ChannelCode = channelCode;
            Text = text ?? throw new ArgumentNullException(nameof(text));
            // Native ReadShares returns null for an absent or zero count; PendingPrivateChat
            // already keeps that value and this carrier must not invent an empty list.
            Shares = shares;
        }

        // Set only after the native channel-0 bubble tail returned normally, before retention.
        public bool NativeBubbleHandled { get; set; }
        public byte SenderLevel { get; }
        public byte SenderFlag { get; }
        public int SenderEntityId { get; }

        public string SenderName { get; }

        public byte BubbleThemeSelector { get; }

        public byte ChannelCode { get; }

        public string Text { get; }

        public IReadOnlyList<WorldChatSharedEntryState> Shares { get; }
    }

    private sealed class PendingRemoteWorldState
    {
        private PendingRemoteWorldState(WorldCombatResultState combatResult = null,
            WorldSkillCastState skillCast = null, WorldPartyState party = null,
            WorldStallOwnerState stall = null, bool removeEntity = false,
            int entityId = 0, byte removeReason = 0, bool removeStall = false,
            PendingEntityStatusEffect statusEffect = null, WorldPlayerRenameState rename = null,
            WorldFamilyStashState familyStash = null,
            RackShopGoodsResponse rackGoodsResponse = null,
            WorldRackShopDepositInfo rackDepositInfo = null,
            ProtocolPacket? tradePacket = null, ProtocolPacket? warehousePacket = null,
            ProtocolPacket? socialPacket = null,
            WorldRackShopFittingResponse rackFittingResponse = null,
            RackShopRefundResponse rackRefundResponse = null,
            IReadOnlyList<WorldRackShopFittingDescription> rackDescriptions = null,
            IReadOnlyList<WorldChangeSetItem> changeSetItems = null,
            WorldFriendState friend = null, WorldApprenticeState apprentice = null,
            WorldBlockState block = null,
            WorldEntityState generic = null, IReadOnlyList<WorldEntityStatusIcon> icons = null,
            bool genericAdmittedOnMainThread = false, int? emoteFace = null, PendingChannelChat chatState = null)
        {
            CombatResult = combatResult;
            SkillCastState = skillCast;
            PartyState = party;
            RackGoodsResponse = rackGoodsResponse;
            RackDepositInfo = rackDepositInfo;
            TradePacket = tradePacket;
            WarehousePacket = warehousePacket;
            SocialPacket = socialPacket;
            RackFittingResponse = rackFittingResponse;
            RackRefundResponse = rackRefundResponse;
            RackDescriptions = rackDescriptions;
            ChangeSetItems = changeSetItems;
            FamilyStashState = familyStash;
            FriendState = friend;
            ApprenticeState = apprentice;
            BlockState = block;
            StallState = stall;
            IsRemoval = removeEntity;
            RemovedEntityId = entityId;
            RemoveReason = removeReason;
            IsStallRemoval = removeStall;
            RemovedStallId = entityId;
            StatusEffect = statusEffect;
            RenameState = rename;
            GenericState = generic;
            GenericAdmittedOnMainThread = genericAdmittedOnMainThread;
            IconRows = icons;
            IconsEntityId = entityId;
            EmoteEntityId = entityId;
            EmoteFace = emoteFace;
            ChatState = chatState;
        }

        private PendingRemoteWorldState(
            WorldRemotePlayerState playerState,
            WorldRemoteRidingState ridingState,
            WorldRemotePathState pathState,
            int mapIdGuard,
            int lifeEntityId,
            bool? alive,
            int reviveFlags,
            int pkEntityId,
            byte? pkState,
            WorldPlayerAppearanceState appearanceState = null, bool remoteLifeAtReceive = false)
        {
            PlayerState = playerState;
            RidingState = ridingState;
            PathState = pathState;
            MapIdGuard = mapIdGuard;
            LifeEntityId = lifeEntityId;
            RemoteLifeAtReceive = remoteLifeAtReceive;
            Alive = alive;
            ReviveFlags = reviveFlags;
            PkEntityId = pkEntityId;
            PkState = pkState;
            AppearanceState = appearanceState;
        }

        public WorldRemotePlayerState PlayerState { get; }

        public WorldRemoteRidingState RidingState { get; }

        public WorldRemotePathState PathState { get; }

        public int MapIdGuard { get; }

        public int LifeEntityId { get; }
        public bool RemoteLifeAtReceive { get; }

        public bool? Alive { get; }

        public int ReviveFlags { get; }

        public int PkEntityId { get; }

        public byte? PkState { get; }

        public WorldPlayerAppearanceState AppearanceState { get; }
        public WorldPlayerRenameState RenameState { get; }
        public WorldEntityState GenericState { get; }
        public bool GenericAdmittedOnMainThread { get; }
        public IReadOnlyList<WorldEntityStatusIcon> IconRows { get; }
        public int IconsEntityId { get; }
        public PendingChannelChat ChatState { get; }
        public static PendingRemoteWorldState ChannelMessage(PendingChannelChat chat) => new PendingRemoteWorldState(chatState: chat);
        public int EmoteEntityId { get; }
        public int? EmoteFace { get; }
        public static PendingRemoteWorldState Emote(int id, int face) => new PendingRemoteWorldState(entityId: id, emoteFace: face);
        public static PendingRemoteWorldState GenericSpawn(WorldEntityState state, bool admittedOnMainThread) =>
            new PendingRemoteWorldState(generic: state, genericAdmittedOnMainThread: admittedOnMainThread);
        public static PendingRemoteWorldState Icons(int entityId, IReadOnlyList<WorldEntityStatusIcon> rows) =>
            new PendingRemoteWorldState(entityId: entityId, icons: rows);

        public static PendingRemoteWorldState Rename(WorldPlayerRenameState state) =>
            new PendingRemoteWorldState(rename: state);
        public PendingEntityStatusEffect StatusEffect { get; }

        public WorldCombatResultState CombatResult { get; }
        public WorldSkillCastState SkillCastState { get; }
        public WorldPartyState PartyState { get; }
        public RackShopGoodsResponse RackGoodsResponse { get; }
        public WorldRackShopDepositInfo RackDepositInfo { get; }
        public ProtocolPacket? TradePacket { get; }
        public ProtocolPacket? WarehousePacket { get; }
        public ProtocolPacket? SocialPacket { get; }
        public WorldRackShopFittingResponse RackFittingResponse { get; }
        public RackShopRefundResponse RackRefundResponse { get; }
        public IReadOnlyList<WorldRackShopFittingDescription> RackDescriptions { get; }
        public IReadOnlyList<WorldChangeSetItem> ChangeSetItems { get; }
        public WorldFamilyStashState FamilyStashState { get; }
        public WorldFriendState FriendState { get; }
        public WorldApprenticeState ApprenticeState { get; }
        public WorldBlockState BlockState { get; }
        public WorldStallOwnerState StallState { get; }
        public bool IsRemoval { get; }
        public int RemovedEntityId { get; }
        public byte RemoveReason { get; }
        public bool IsStallRemoval { get; }
        public int RemovedStallId { get; }

        public static PendingRemoteWorldState Combat(WorldCombatResultState state) =>
            new PendingRemoteWorldState(combatResult: state);
        public static PendingRemoteWorldState Cast(WorldSkillCastState state) =>
            new PendingRemoteWorldState(skillCast: state);
        public static PendingRemoteWorldState Party(WorldPartyState state) =>
            new PendingRemoteWorldState(party: state);
        public static PendingRemoteWorldState Social(ProtocolPacket packet) => new PendingRemoteWorldState(socialPacket: packet);
        public static PendingRemoteWorldState Trade(ProtocolPacket packet) => new PendingRemoteWorldState(tradePacket: packet);
        public static PendingRemoteWorldState Warehouse(ProtocolPacket packet) => new PendingRemoteWorldState(warehousePacket: packet);
        public static PendingRemoteWorldState RackGoods(RackShopGoodsResponse response) =>
            new PendingRemoteWorldState(rackGoodsResponse: response);
        public static PendingRemoteWorldState RackFitting(WorldRackShopFittingResponse response) =>
            new PendingRemoteWorldState(rackFittingResponse: response);
        public static PendingRemoteWorldState RackDeposit(WorldRackShopDepositInfo receipt) =>
            new PendingRemoteWorldState(rackDepositInfo: receipt);
        public static PendingRemoteWorldState RackRefunds(RackShopRefundResponse response) =>
            new PendingRemoteWorldState(rackRefundResponse: response);
        public static PendingRemoteWorldState RackFittingDescriptions(IReadOnlyList<WorldRackShopFittingDescription> descriptions) =>
            new PendingRemoteWorldState(rackDescriptions: descriptions);
        public static PendingRemoteWorldState ChangeSet(IReadOnlyList<WorldChangeSetItem> items) =>
            new PendingRemoteWorldState(changeSetItems: items);
        public static PendingRemoteWorldState FamilyStash(WorldFamilyStashState state) =>
            new PendingRemoteWorldState(familyStash: state);
        public static PendingRemoteWorldState Friend(WorldFriendState state) =>
            new PendingRemoteWorldState(friend: state);
        public static PendingRemoteWorldState Apprentice(WorldApprenticeState state) =>
            new PendingRemoteWorldState(apprentice: state);
        public static PendingRemoteWorldState Block(WorldBlockState state) =>
            new PendingRemoteWorldState(block: state);
        public static PendingRemoteWorldState Stall(WorldStallOwnerState state) =>
            new PendingRemoteWorldState(stall: state);
        public static PendingRemoteWorldState Remove(int id, byte reason) =>
            new PendingRemoteWorldState(removeEntity: true, entityId: id, removeReason: reason);
        public static PendingRemoteWorldState RemoveStall(int id) =>
            new PendingRemoteWorldState(removeStall: true, entityId: id);
        public static PendingRemoteWorldState StatusFx(PendingEntityStatusEffect effect) =>
            new PendingRemoteWorldState(statusEffect: effect);

        public static PendingRemoteWorldState Spawn(
            WorldRemotePlayerState state,
            int mapIdGuard)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new PendingRemoteWorldState(
                state, null, null, mapIdGuard, 0, null, 0, 0, null);
        }

        public static PendingRemoteWorldState Riding(WorldRemoteRidingState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new PendingRemoteWorldState(
                null, state, null, 0, 0, null, 0, 0, null);
        }

        public static PendingRemoteWorldState Appearance(
            WorldPlayerAppearanceState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new PendingRemoteWorldState(
                null, null, null, 0, 0, null, 0, 0, null, state);
        }

        public static PendingRemoteWorldState Path(WorldRemotePathState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            return new PendingRemoteWorldState(
                null,
                null,
                state,
                0,
                0,
                null,
                0,
                0,
                null);
        }

        public static PendingRemoteWorldState Life(
            int entityId,
            bool alive,
            int reviveFlags, bool remoteAtReceive = false)
        {
            return new PendingRemoteWorldState(
                null, null, null, 0, entityId, alive, reviveFlags, 0, null, remoteLifeAtReceive: remoteAtReceive);
        }

        public static PendingRemoteWorldState Pk(int entityId, byte pkState)
        {
            return new PendingRemoteWorldState(
                null, null, null, 0, 0, null, 0, entityId, pkState);
        }
    }

    private enum GatewayStage
    {
        Disconnected,
        Connected,
        Authenticated,
        InWorld,
        LeavingWorld
    }

    private sealed class ServerRejectedException : Exception
    {
        public ServerRejectedException(string message)
            : base(message)
        {
        }
    }
}
}
