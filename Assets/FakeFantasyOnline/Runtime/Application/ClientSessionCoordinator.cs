using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Domain;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Application
{

public class CharacterSelectionItem
{
    // Standalone input for the existing auxiliary portrait/audit compositor.
    // The live decoded roster uses ServerCharacterRecord values directly.
    public CharacterSelectionItem(
        string characterId,
        string displayName,
        int race,
        int gender,
        int head,
        int hair,
        int level,
        string lastLogin,
        CharacterAppearanceRecord appearance,
        IReadOnlyList<int> attributes,
        long honor)
    {
        CharacterId = characterId ?? throw new ArgumentNullException(nameof(characterId));
        DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        Race = race;
        Gender = gender;
        Head = head;
        Hair = hair;
        Level = level;
        LastLogin = lastLogin ?? string.Empty;
        Appearance = appearance ?? throw new ArgumentNullException(nameof(appearance));
        Attributes = attributes ?? Array.Empty<int>();
        Honor = honor;
    }

    public string CharacterId { get; private protected set; }

    public string DisplayName { get; private protected set; }

    public int Race { get; private protected set; }

    public int Gender { get; private protected set; }

    public int Head { get; private protected set; }

    public int Hair { get; private protected set; }

    public int Level { get; private protected set; }

    public string LastLogin { get; private protected set; }

    public CharacterAppearanceRecord Appearance { get; private protected set; }

    public IReadOnlyList<int> Attributes { get; private protected set; }

    public long Honor { get; private protected set; }
}

public sealed class ServerSelectionItem
{
    public ServerSelectionItem(
        int directoryIndex,
        string displayName,
        string zoneName,
        bool isPlayerVersusPlayer,
        int busyLevel)
        : this(directoryIndex, displayName, zoneName, isPlayerVersusPlayer, busyLevel, null, 0)
    {
    }

    public ServerSelectionItem(int directoryIndex, string displayName, string zoneName,
        bool isPlayerVersusPlayer, int busyLevel, string host, int port)
    {
        DirectoryIndex = directoryIndex;
        DisplayName = displayName ?? string.Empty;
        ZoneName = zoneName ?? string.Empty;
        IsPlayerVersusPlayer = isPlayerVersusPlayer;
        BusyLevel = busyLevel;
        Host = host;
        Port = port;
    }

    public string Host { get; }
    public int Port { get; }

    public int DirectoryIndex { get; }

    public string DisplayName { get; }

    public string ZoneName { get; }

    public bool IsPlayerVersusPlayer { get; }

    public int BusyLevel { get; }
}

public sealed partial class ClientSessionCoordinator : IDisposable
{
    private readonly IServerAddressDiscovery _addressDiscovery;
    private readonly IGameServerGateway _gateway;
    private ICharacterSkillUsePort _characterSkillUse;
    private readonly IEventBus _events;
    private readonly SemaphoreSlim _operation = new SemaphoreSlim(1, 1);
    private ClientSessionState _state = ClientSessionState.Offline;
    private IReadOnlyList<ServerDirectoryEntry> _directory =
        Array.Empty<ServerDirectoryEntry>();
    private IReadOnlyList<ServerSelectionItem> _servers =
        Array.Empty<ServerSelectionItem>();
    private IReadOnlyList<ServerCharacterRecord> _characters =
        Array.Empty<ServerCharacterRecord>();
    private int _preferredServerIndex;
    private int _connectedServerIndex = -1;
    private long _localPlayerStatusEffectConsumedRevision;

    public ClientSessionCoordinator(
        IServerAddressDiscovery addressDiscovery,
        IGameServerGateway gateway,
        IEventBus events)
    {
        _addressDiscovery = addressDiscovery ?? throw new ArgumentNullException(nameof(addressDiscovery));
        _gateway = gateway ?? throw new ArgumentNullException(nameof(gateway));
        _events = events ?? throw new ArgumentNullException(nameof(events));
    }

    public ClientSessionState State => _state;

    public bool Connected => _gateway.Connected;
    // Shared native owner for addon/agent endgame commands; never a parallel game state.
    public IEndgameClient EndgameClientForPlugins => _gateway as IEndgameClient;

    internal IPartyCommands PartyCommands => _gateway;
    internal IFamilyCommands FamilyCommands => _gateway;
    public WorldFamilyState FamilyState => (_gateway as IFamilyStateSource)?.FamilyState ?? WorldFamilyState.Empty;
    public long FamilyStateVersion => (_gateway as IFamilyStateSource)?.FamilyStateVersion ?? 0;
    public WorldFamilyBrowseState FamilyBrowseState => (_gateway as IFamilyStateSource)?.FamilyBrowseState ?? WorldFamilyBrowseState.Empty;
    public long FamilyBrowseStateVersion => (_gateway as IFamilyStateSource)?.FamilyBrowseStateVersion ?? 0;
    public WorldRackShopState RackShopState => (_gateway as IRackShopStateSource)?.RackShopState ?? WorldRackShopState.Empty;
    public long RackShopStateVersion => (_gateway as IRackShopStateSource)?.RackShopStateVersion ?? 0;
    public long RackShopGoodsVersion => (_gateway as IRackShopStateSource)?.RackShopGoodsVersion ?? 0;
    public IRackShopStateSource RackShopStateSource => _gateway as IRackShopStateSource;
    public WorldFamilyPositionsState FamilyPositionsState => (_gateway as IFamilyStateSource)?.FamilyPositionsState ?? WorldFamilyPositionsState.Empty;
    public long FamilyPositionsStateVersion => (_gateway as IFamilyStateSource)?.FamilyPositionsStateVersion ?? 0;
    public WorldFamilyStashState FamilyStashState => (_gateway as IFamilyStashStateSource)?.FamilyStashState ?? WorldFamilyStashState.Empty;
    public long FamilyStashStateVersion => (_gateway as IFamilyStashStateSource)?.FamilyStashStateVersion ?? 0;
    public WorldHelpTopicsState HelpTopicsState => (_gateway as IHelpTopicsStateSource)?.HelpTopicsState ?? WorldHelpTopicsState.Empty;
    public long HelpTopicsStateVersion => (_gateway as IHelpTopicsStateSource)?.HelpTopicsStateVersion ?? 0;
    public WorldFriendState FriendState => (_gateway as ISocialFeedStateSource)?.FriendState ?? WorldFriendState.Empty;
    public long FriendStateVersion => (_gateway as ISocialFeedStateSource)?.FriendStateVersion ?? 0;
    public WorldApprenticeState ApprenticeState => (_gateway as ISocialFeedStateSource)?.ApprenticeState ?? WorldApprenticeState.Empty;
    public long ApprenticeStateVersion => (_gateway as ISocialFeedStateSource)?.ApprenticeStateVersion ?? 0;
    public WorldBlockState BlockState => (_gateway as ISocialFeedStateSource)?.BlockState ?? WorldBlockState.Empty;
    public long BlockStateVersion => (_gateway as ISocialFeedStateSource)?.BlockStateVersion ?? 0;
    public WorldMailState MailState => (_gateway as IMailStateSource)?.MailState ?? WorldMailState.Empty;
    public long MailStateVersion => (_gateway as IMailStateSource)?.MailStateVersion ?? 0;
    public IMailStateSource MailStateSource => _gateway as IMailStateSource;

    internal IFriendCommands FriendCommands => _gateway;
    internal IMailCommands MailCommands => _gateway;
    internal IDuelCommands DuelCommands => _gateway;
    public IDuelStateSource DuelStateSource => _gateway as IDuelStateSource;
    internal IBlockCommands BlockCommands => _gateway;
    internal IRackCommands RackCommands => _gateway;
    internal IShopCommands ShopCommands => _gateway;
    internal IRepairCommands RepairCommands => _gateway;
    internal IHistoryCommands HistoryCommands => _gateway;
    internal IItemUseCommands ItemUseCommands => _gateway;
    internal IItemToolCommands ItemToolCommands => _gateway;
    internal IItemLockCommands ItemLockCommands => _gateway;
    internal ICaptureCommands CaptureCommands => _gateway;
    internal IFurnaceCommands FurnaceCommands => _gateway;
    internal IEquipmentFusionCommands EquipmentFusionCommands => _gateway;
    internal IAvatarFusionCommands AvatarFusionCommands => _gateway;
    internal IAttributeWashCommands AttributeWashCommands => _gateway;
    internal IHotbarWriteCommands HotbarWriteCommands => _gateway;
    internal IPetForgeCommands PetForgeCommands => _gateway;
    internal IGroundPickupCommands GroundPickupCommands => _gateway;
    internal INewbieTipCommands NewbieTipCommands => _gateway;
    internal IWorldReturnCommands WorldReturnCommands => _gateway;
    internal ICharacterWorkCommands WorkCommands => _gateway;
    internal ICharacterReviveCommands ReviveCommands => _gateway;
    internal ICharacterSettingsCommands CharacterSettingsCommands => _gateway;
    internal IStallCommands StallCommands => _gateway;
    internal IRidingCommands RidingCommands => _gateway;
    internal IApprenticeCommands ApprenticeCommands => _gateway;

    public void BindCharacterSkillUsePort(ICharacterSkillUsePort owner)
    {
        if (owner == null)
        {
            throw new ArgumentNullException(nameof(owner));
        }
        if (_characterSkillUse != null && !ReferenceEquals(_characterSkillUse, owner))
        {
            throw new InvalidOperationException("The character skill-use owner is already bound.");
        }
        _characterSkillUse = owner;
    }

    public void UnbindCharacterSkillUsePort(ICharacterSkillUsePort owner)
    {
        if (ReferenceEquals(_characterSkillUse, owner))
        {
            _characterSkillUse = null;
        }
    }

    public IReadOnlyList<ServerSelectionItem> Servers => _servers;

    public IReadOnlyList<ServerCharacterRecord> Characters => _characters;

    public int ConnectedServerIndex => _connectedServerIndex;

    private WorldEntryRecord _entryAwaitingHostBuild;
    private Action _entryBuildReturned;

    /// <summary>Complete only the selected entry's normally returned host BuildWorld attempt.</summary>
    public void CompleteWorldEntryBuildForIndependentRuntime(WorldEntryRecord selectedEntry)
    {
        if (!ReferenceEquals(selectedEntry, _entryAwaitingHostBuild)) return;
        Action completion = _entryBuildReturned;
        _entryAwaitingHostBuild = null;
        _entryBuildReturned = null;
        completion?.Invoke();
    }

    public void Pump()
    {
        _gateway.Pump();
    }

    public void Pump(IWorldReceiveContext context)
    {
        if (_gateway is IWorldReceivePump receivePump) receivePump.Pump(context);
        else _gateway.Pump();
    }

    /// <summary>
    /// Ordered immutable arguments from the official NetClient.OnTeleport edge.
    /// </summary>
    public IReadOnlyList<WorldTeleportState> TeleportStates => _gateway.TeleportStates;

    /// <summary>
    /// Monotonically changes whenever the teleport callback edge is published or cleared.
    /// </summary>
    public long TeleportStatesVersion => _gateway.TeleportStatesVersion;

    /// <summary>
    /// The latest immutable server-authored self attribute/pet status root.
    /// </summary>
    public WorldPlayerStatusState PlayerStatusState => _gateway.PlayerStatusState;

    /// <summary>
    /// Monotonically changes whenever self status is published or cleared.
    /// </summary>
    public long PlayerStatusStateVersion => _gateway.PlayerStatusStateVersion;

    /// <summary>
    /// Latest complete server-authored local appearance edge.
    /// </summary>
    public WorldPlayerAppearanceState PlayerAppearanceState =>
        _gateway.PlayerAppearanceState;

    /// <summary>
    /// Monotonically changes whenever local appearance is published or cleared.
    /// </summary>
    public long PlayerAppearanceStateVersion =>
        _gateway.PlayerAppearanceStateVersion;

    /// <summary>
    /// Latest complete server-authored current/owned character-title replacement.
    /// </summary>
    public WorldCharacterTitleState CharacterTitleState =>
        _gateway.CharacterTitleState;

    public long CharacterTitleStateVersion =>
        _gateway.CharacterTitleStateVersion;

    /// <summary>
    /// Latest exact self life-state projection, kept separate from attribute-owned hit points.
    /// </summary>
    public WorldLocalPlayerLifeState LocalPlayerLifeState =>
        _gateway.LocalPlayerLifeState;

    /// <summary>
    /// Monotonically changes whenever the self life edge is published or cleared.
    /// </summary>
    public long LocalPlayerLifeStateVersion =>
        _gateway.LocalPlayerLifeStateVersion;

    /// <summary>
    /// Ordered official body-effect edges addressed to the local player. Presentation consumes these
    /// as one-shot EffectPlayer.SpawnOn requests rather than inferring effects from Buff rows.
    /// </summary>
    public IReadOnlyList<WorldEntityStatusEffect> LocalPlayerStatusEffects =>
        _gateway.LocalPlayerStatusEffects;

    /// <summary>
    /// Monotonically changes whenever a local-player body-effect edge is published or cleared.
    /// </summary>
    public long LocalPlayerStatusEffectsVersion =>
        _gateway.LocalPlayerStatusEffectsVersion;

    public WorldLocalStatusEffectWindow CaptureLocalStatusEffects() =>
        (_gateway as IWorldLocalStatusEffectSource)?.CaptureLocalStatusEffects();

    public bool IsCurrentLocalStatusEffect(WorldLocalStatusEffectWindow window, long revision) =>
        (_gateway as IWorldLocalStatusEffectSource)?.IsCurrentLocalStatusEffect(window, revision) ?? false;

    public bool AcknowledgeLocalStatusEffects(WorldLocalStatusEffectWindow window, long consumedThrough) =>
        (_gateway as IWorldLocalStatusEffectSource)?.AcknowledgeLocalStatusEffects(window, consumedThrough) ?? false;

    /// <summary>Legacy gateway fallback; capable gateways own their captured prefix and ACK.</summary>
    public long LocalPlayerStatusEffectConsumedRevision =>
        Interlocked.Read(ref _localPlayerStatusEffectConsumedRevision);

    public void AcknowledgeLocalPlayerStatusEffect(long revision)
    {
        if (revision < 1) throw new ArgumentOutOfRangeException(nameof(revision));
        long current = Interlocked.Read(ref _localPlayerStatusEffectConsumedRevision);
        while (revision > current)
        {
            long previous = Interlocked.CompareExchange(
                ref _localPlayerStatusEffectConsumedRevision, revision, current);
            if (previous == current) return;
            current = previous;
        }
    }

    public IReadOnlyList<WorldSkillHitEffect> SkillHitEffects =>
        _gateway.SkillHitEffects;

    public long SkillHitEffectsVersion =>
        _gateway.SkillHitEffectsVersion;

    /// <summary>
    /// The latest complete server-authored self PK state, or null before Protocol publishes it.
    /// </summary>
    public WorldSelfPkState SelfPkState => _gateway.SelfPkState;

    /// <summary>
    /// Monotonically changes whenever self PK state is published or cleared.
    /// </summary>
    public long SelfPkStateVersion => _gateway.SelfPkStateVersion;

    /// <summary>
    /// The latest server-authored CombatWorld LocalFaction root, or null until it is published.
    /// </summary>
    public WorldLocalFactionState LocalFactionState => _gateway.LocalFactionState;

    /// <summary>
    /// Monotonically changes whenever the local-faction root is published or cleared.
    /// </summary>
    public long LocalFactionStateVersion => _gateway.LocalFactionStateVersion;

    /// <summary>
    /// The latest server-authored self-riding state, or null until a dedicated update arrives.
    /// </summary>
    public WorldRidingState LocalRidingState => _gateway.LocalRidingState;

    /// <summary>
    /// Monotonically changes whenever the connection publishes or clears self-riding state.
    /// Presentation may poll this value on Unity's main thread without mutating Unity objects
    /// from the Protocol receive loop.
    /// </summary>
    public long LocalRidingStateVersion => _gateway.LocalRidingStateVersion;

    /// <summary>
    /// Immutable remote-player riding states published from spawn and riding-update packets.
    /// </summary>
    public IReadOnlyDictionary<int, WorldRemoteRidingState> RemoteRidingStates =>
        _gateway.RemoteRidingStates;

    /// <summary>
    /// Monotonically changes whenever the remote-player riding snapshot changes or is cleared.
    /// </summary>
    public long RemoteRidingStatesVersion => _gateway.RemoteRidingStatesVersion;

    /// <summary>
    /// Immutable complete remote-player spawn snapshots keyed by server entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldRemotePlayerState> RemotePlayers =>
        _gateway.RemotePlayers;

    /// <summary>
    /// Monotonically changes whenever the complete remote-player snapshot changes or is cleared.
    /// </summary>
    public long RemotePlayersVersion => _gateway.RemotePlayersVersion;

    // Optional receive-event capability; existing gateway implementations and mocks stay valid.
    public WorldRemoteEventWindow CaptureRemoteEvents() =>
        (_gateway as IWorldRemoteEventSource)?.CaptureRemoteEvents();

    // Remote skill-cast animation is owned by CombatWorld.PlayerCast, so a receive-event
    // capability no longer switches it on or off.

    public bool AcknowledgeRemoteEvents(WorldRemoteEventWindow window) =>
        (_gateway as IWorldRemoteEventSource)?.AcknowledgeRemoteEvents(window) ?? false;

    /// <summary>
    /// Immutable complete NPC spawn snapshots keyed by server entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldNpcState> Npcs => _gateway.Npcs;

    /// <summary>
    /// Monotonically changes whenever the NPC snapshot changes or is cleared.
    /// </summary>
    public long NpcsVersion => _gateway.NpcsVersion;

    /// <summary>
    /// Latest complete server-authored NpcChatter replacement, including bubble timing.
    /// </summary>
    public WorldNpcChatterState NpcChatterState => _gateway.NpcChatterState;

    /// <summary>
    /// Monotonically changes whenever Protocol publishes or clears NPC chatter.
    /// </summary>
    public long NpcChatterStateVersion => _gateway.NpcChatterStateVersion;

    /// <summary>
    /// Latest complete task-offer replacement for the NPC named in the server reply.
    /// </summary>
    public WorldNpcTaskOffersState NpcTaskOffersState => _gateway.NpcTaskOffersState;

    /// <summary>
    /// Monotonically changes whenever the server publishes or clears NPC task offers.
    /// </summary>
    public long NpcTaskOffersStateVersion => _gateway.NpcTaskOffersStateVersion;

    public WorldShopState ShopState => _gateway.ShopState;

    public long ShopStateVersion => _gateway.ShopStateVersion;

    public WorldTransportState TransportState => _gateway.TransportState;

    public long TransportStateVersion => _gateway.TransportStateVersion;

    public WorldWarehouseState WarehouseState => _gateway.WarehouseState;

    public long WarehouseStateVersion => _gateway.WarehouseStateVersion;

    /// <summary>
    /// Latest complete server-authored active/done task replacement.
    /// </summary>
    public WorldTaskLogState TaskLogState => _gateway.TaskLogState;

    /// <summary>
    /// Monotonically changes whenever Protocol publishes or clears the task log.
    /// </summary>
    public long TaskLogStateVersion => _gateway.TaskLogStateVersion;

    /// <summary>
    /// Latest server-authored NPC/dialog text. The display-mode byte determines its owner.
    /// </summary>
    public WorldNpcDialogTextState NpcDialogTextState => _gateway.NpcDialogTextState;

    /// <summary>
    /// Monotonically changes whenever Protocol publishes or clears server dialog text.
    /// </summary>
    public long NpcDialogTextStateVersion => _gateway.NpcDialogTextStateVersion;

    /// <summary>
    /// Atomic bounded ordered server dialog-text edges and their overflow boundary.
    /// </summary>
    public WorldNpcDialogTextEdgeWindow NpcDialogTextEdgeWindow =>
        _gateway.NpcDialogTextEdgeWindow;

    /// <summary>
    /// Immutable complete generic monster, pet and trap spawn snapshots keyed by server entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldEntityState> WorldEntities =>
        _gateway.WorldEntities;

    /// <summary>
    /// Monotonically changes whenever the generic world-entity snapshot changes or is cleared.
    /// </summary>
    public long WorldEntitiesVersion => _gateway.WorldEntitiesVersion;

    /// <summary>
    /// Aggregate receive, map-guard and snapshot facts for the current world scene generation.
    /// </summary>
    public WorldEntityPublicationState WorldEntityPublicationState =>
        _gateway.WorldEntityPublicationState;

    /// <summary>
    /// Atomic scene generation, map, immutable entity snapshot, version and publication facts.
    /// </summary>
    public WorldEntitySceneSnapshot WorldEntitySceneSnapshot =>
        _gateway.WorldEntitySceneSnapshot;

    /// <summary>
    /// Immutable complete ground-item spawn snapshots keyed by ground entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldGroundItemState> GroundItems =>
        _gateway.GroundItems;

    /// <summary>
    /// Monotonically changes whenever the authoritative ground-item snapshot changes or clears.
    /// </summary>
    public long GroundItemsVersion => _gateway.GroundItemsVersion;

    /// <summary>
    /// Receive-order evidence for accepted ground spawn/remove and inventory packets.
    /// </summary>
    public WorldGroundLootAuthorityState GroundLootAuthorityState =>
        _gateway.GroundLootAuthorityState;

    /// <summary>
    /// Bounded ordered journal of authoritative combat-result edges.
    /// </summary>
    public IReadOnlyList<WorldCombatResultState> CombatResults =>
        _gateway.CombatResults;

    public long CombatResultsVersion => _gateway.CombatResultsVersion;

    /// <summary>
    /// Bounded ordered journal of server-requested world numbers such as experience gain.
    /// </summary>
    public IReadOnlyList<WorldCombatNumberState> CombatNumbers =>
        _gateway.CombatNumbers;

    public long CombatNumbersVersion => _gateway.CombatNumbersVersion;

    /// <summary>
    /// Latest immutable target-status response. Presentation must still match EntityId to its
    /// current target before displaying it.
    /// </summary>
    public WorldTargetStatusState TargetStatusState => _gateway.TargetStatusState;

    /// <summary>
    /// Monotonically changes whenever Protocol publishes or clears a target-status response.
    /// </summary>
    public long TargetStatusStateVersion => _gateway.TargetStatusStateVersion;

    /// <summary>
    /// Latest complete server-authored local-player Buff/Debuff replacement.
    /// </summary>
    public WorldBuffState BuffState => _gateway.BuffState;

    /// <summary>
    /// Monotonically changes whenever Protocol publishes or clears the local Buff snapshot.
    /// </summary>
    public long BuffStateVersion => _gateway.BuffStateVersion;

    public WorldInspectedPlayerEquipmentState InspectedPlayerEquipmentState =>
        _gateway.InspectedPlayerEquipmentState;

    public long InspectedPlayerEquipmentStateVersion =>
        _gateway.InspectedPlayerEquipmentStateVersion;

    /// <summary>
    /// Immutable latest remote-player movement paths keyed by server entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldRemotePathState> RemotePaths =>
        _gateway.RemotePaths;

    /// <summary>
    /// Monotonically changes whenever a remote-player path is published, replaced, or cleared.
    /// </summary>
    public long RemotePathsVersion => _gateway.RemotePathsVersion;

    /// <summary>
    /// Immutable latest monster movement paths keyed by server entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldRemotePathState> MonsterPaths =>
        _gateway.MonsterPaths;

    /// <summary>
    /// Monotonically changes whenever a monster path is published, replaced, or cleared.
    /// </summary>
    public long MonsterPathsVersion => _gateway.MonsterPathsVersion;

    /// <summary>
    /// Immutable latest remote-player action edges keyed by server entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldRemoteActionState> RemoteActions =>
        _gateway.RemoteActions;

    /// <summary>
    /// Monotonically changes whenever a remote-player action is published or cleared.
    /// </summary>
    public long RemoteActionsVersion => _gateway.RemoteActionsVersion;

    /// <summary>
    /// Latest complete server-authored skill-cast edge, including every optional wire tail.
    /// </summary>
    public WorldSkillCastState SkillCastState => _gateway.SkillCastState;

    /// <summary>
    /// Bounded immutable skill-cast event window in exact wire order.
    /// </summary>
    public IReadOnlyList<WorldSkillCastState> SkillCastEvents =>
        _gateway.SkillCastEvents;

    /// <summary>
    /// Monotonically changes whenever Protocol publishes or clears the skill-cast edge.
    /// </summary>
    public long SkillCastStateVersion => _gateway.SkillCastStateVersion;

    /// <summary>
    /// Immutable latest channel-zero chat edges keyed by remote-player entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldRemoteChatBubbleState> RemoteChatBubbles =>
        _gateway.RemoteChatBubbles;

    /// <summary>
    /// Monotonically changes whenever a remote chat edge is published, replaced, or cleared.
    /// </summary>
    public long RemoteChatBubblesVersion => _gateway.RemoteChatBubblesVersion;

    /// <summary>
    /// Bounded server-authored channel-chat history in exact receive order.
    /// </summary>
    public IReadOnlyList<WorldChannelChatState> ChannelChatRows =>
        _gateway.ChannelChatRows;

    /// <summary>
    /// Monotonically changes whenever Protocol appends or clears channel-chat rows.
    /// </summary>
    public long ChannelChatRowsVersion => _gateway.ChannelChatRowsVersion;

    /// <summary>
    /// Bounded server-authored private-chat history in exact receive order.
    /// </summary>
    public IReadOnlyList<WorldPrivateChatState> PrivateChatRows =>
        _gateway.PrivateChatRows;

    /// <summary>
    /// Monotonically changes whenever Protocol appends or clears private-chat rows.
    /// </summary>
    public long PrivateChatRowsVersion => _gateway.PrivateChatRowsVersion;

    /// <summary>
    /// Immutable current party snapshot used by social UI and remote-player visibility policy.
    /// </summary>
    public WorldPartyState PartyState => _gateway.PartyState;

    /// <summary>
    /// Monotonically changes whenever the complete party snapshot changes or is cleared.
    /// </summary>
    public long PartyStateVersion => _gateway.PartyStateVersion;

    /// <summary>
    /// Immutable live stall-owner rows keyed by the server entity id.
    /// </summary>
    public IReadOnlyDictionary<int, WorldStallOwnerState> StallOwners =>
        _gateway.StallOwners;

    /// <summary>
    /// Monotonically changes whenever a stall owner is published, removed, or cleared.
    /// </summary>
    public long StallOwnersVersion => _gateway.StallOwnersVersion;

    /// <summary>
    /// Latest complete server-authored system-request queue in wire order.
    /// </summary>
    public IReadOnlyList<WorldSystemRequestState> SystemRequests =>
        _gateway.SystemRequests;

    /// <summary>
    /// Monotonically changes whenever the server replaces or the connection clears the queue.
    /// </summary>
    public long SystemRequestsVersion => _gateway.SystemRequestsVersion;

    /// <summary>
    /// Latest complete server-authored inventory, equipment and currency snapshot.
    /// </summary>
    public WorldInventoryState InventoryState => _gateway.InventoryState;

    public long InventoryStateVersion => _gateway.InventoryStateVersion;

    /// <summary>
    /// Latest complete server-authored magic wardrobe in server wire order.
    /// </summary>
    public WorldWardrobeState WardrobeState => _gateway.WardrobeState;

    public IWorldWardrobeObservation WardrobeObservation =>
        _gateway is IWorldWardrobeObservationSource source
            ? source.WardrobeObservation
            : _gateway.WardrobeState;

    public long WardrobeStateVersion => _gateway.WardrobeStateVersion;

    /// <summary>
    /// Latest server-authored thirty-slot hotbar projection, including optional wire extension.
    /// </summary>
    public WorldHotbarState HotbarState => _gateway.HotbarState;

    public long HotbarStateVersion => _gateway.HotbarStateVersion;

    public WorldSkillBookState SkillBookState => _gateway.SkillBookState;

    internal WorldSkillBookObservation SkillBookObservation =>
        _gateway is IWorldSkillBookObservationSource source
            ? source.SkillBookObservation
            : new WorldSkillBookObservation(_gateway.SkillBookState ?? WorldSkillBookState.Empty);

    public long SkillBookStateVersion => _gateway.SkillBookStateVersion;

    public WorldLifeSkillBookState LifeSkillBookState => _gateway.LifeSkillBookState;

    public long LifeSkillBookStateVersion => _gateway.LifeSkillBookStateVersion;

    /// <summary>
    /// Latest immutable trade snapshot. Only the server's open/state/end packets mutate it.
    /// </summary>
    public ISocialUiUpdateSource SocialUiUpdateSource => _gateway as ISocialUiUpdateSource;
    public IWarehouseUpdateSource WarehouseUpdateSource => _gateway as IWarehouseUpdateSource;
    public IChangeSetSource ChangeSetSource => _gateway as IChangeSetSource;
    public IChangeSetCommands ChangeSetCommands => _gateway as IChangeSetCommands;
    public WorldActiveTradeState ActiveTradeState => _gateway.ActiveTradeState;

    public long ActiveTradeStateVersion => _gateway.ActiveTradeStateVersion;

    public WorldTradeEndState TradeEndState => _gateway.TradeEndState;

    public long TradeEndStateVersion => _gateway.TradeEndStateVersion;

    public async Task<Result<IReadOnlyList<ServerCharacterRecord>>> LoginAsync(
        string accountName,
        string password,
        CancellationToken cancellationToken)
    {
        bool lockTaken = false;
        try
        {
            if (!await _operation.WaitAsync(0, cancellationToken))
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "session.login.busy", "A session operation is already in progress.");
            lockTaken = true;
            _characters = Array.Empty<ServerCharacterRecord>();
            TransitionTo(ClientSessionState.DiscoveringAddress, string.Empty);
            Result<IReadOnlyList<ServerDirectoryEntry>> discovery =
                await _addressDiscovery.DiscoverAsync(cancellationToken);
            if (!discovery.IsSuccess)
            {
                return FailRoster(discovery.Error);
            }

            _directory = discovery.Value;
            if (_preferredServerIndex < 0 || _preferredServerIndex >= _directory.Count)
            {
                _preferredServerIndex = 0;
            }

            List<ServerSelectionItem> serverSelections = new List<ServerSelectionItem>();
            for (int index = 0; index < _directory.Count; index++)
            {
                ServerDirectoryEntry entry = _directory[index];
                if (entry == null)
                {
                    // Native dispatch preserves null rows; BuildSrvTree skips them while its
                    // length cache and directory identities retain the original array positions.
                    serverSelections.Add(null);
                    continue;
                }
                serverSelections.Add(
                    new ServerSelectionItem(
                        index,
                        entry.DisplayName,
                        entry.ZoneName,
                        entry.IsPlayerVersusPlayer,
                        entry.BusyLevel,
                        entry.Endpoint.Host,
                        entry.Endpoint.Port));
            }

            _servers = new ReadOnlyCollection<ServerSelectionItem>(serverSelections);
            _connectedServerIndex = _preferredServerIndex;

            TransitionTo(ClientSessionState.Connecting, string.Empty);
            // Login and the three account operations share the native connection admission.
            // The gateway retires any previous authentication before admitting a new login.
            Interlocked.Exchange(ref _localPlayerStatusEffectConsumedRevision, 0);
            if (!(_gateway is IWorldCombatNumberStreamSource)) ResetCombatNumberPresentation();
            Result<CancellationToken> connection = await _gateway.ConnectAsync(
                _directory[_connectedServerIndex].Endpoint,
                cancellationToken);
            if (!connection.IsSuccess)
            {
                if (connection.Error.Code == NativeConnectCallbackDroppedCode)
                    return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                        connection.Error.Code, connection.Error.Message);
                return FailRoster(connection.Error);
            }

            using (CancellationTokenSource loginLifetime = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, connection.Value))
            {
                CancellationToken loginToken = loginLifetime.Token;
                loginToken.ThrowIfCancellationRequested();
                TransitionTo(ClientSessionState.Authenticating, string.Empty);
                LoginCredentials credentials = new LoginCredentials(accountName, password);
                Result authentication = await _gateway.AuthenticateAsync(credentials, loginToken);
                if (!authentication.IsSuccess)
                {
                    // The live native callback already retained the current page on rejection.
                    if (UsesNativeTitleResultReceipts && authentication.Error.Code == "auth.rejected")
                        return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                            authentication.Error.Code, authentication.Error.Message);
                    return FailRoster(authentication.Error);
                }
                loginToken.ThrowIfCancellationRequested();

                Result<IReadOnlyList<ServerCharacterRecord>> roster =
                    await _gateway.GetCharacterRosterAsync(loginToken);
                if (!roster.IsSuccess)
                {
                    return FailRoster(roster.Error);
                }
                loginToken.ThrowIfCancellationRequested();

                IReadOnlyList<ServerCharacterRecord> response = ObserveCharacterRosterResponse(roster.Value);
                if (!UsesNativeTitleResultReceipts)
                    TransitionTo(ClientSessionState.ServerSelection, string.Empty);
                return Result<IReadOnlyList<ServerCharacterRecord>>.Success(response);
            }
        }
        catch (OperationCanceledException)
        {
            TransitionTo(ClientSessionState.Offline, "session.cancelled");
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "session.cancelled",
                "The session operation was cancelled.");
        }
        finally
        {
            if (lockTaken)
            {
                _operation.Release();
            }
        }
    }

    /// <summary>
    /// Official TitleFlow.ConfirmServer (06000EF5) switch arm: NetClient.Close (060006BE), the new
    /// endpoint, then state (+0x28) = State.Login (0) and netMsg (+0x250) = the switch tip. The
    /// host writes that tip for this code only; every other failure is an adapter result, not the
    /// switch arm, and keeps its own code/message presentation.
    /// </summary>
    public const string ServerSwitchReauthenticationCode =
        "server.selection.reauthentication.required";

    public Result ConfirmServerSelection(int directoryIndex)
    {
        if (_state != ClientSessionState.ServerSelection)
        {
            return Result.Failure(
                "session.stage.invalid",
                "Server selection is not active.");
        }

        if (directoryIndex < 0 || directoryIndex >= _directory.Count)
        {
            return Result.Failure(
                "server.selection.invalid",
                "The selected server does not exist.");
        }

        ServerDirectoryEntry selected = _directory[directoryIndex];
        bool switchEndpoint = !string.Equals(selected.Endpoint.Host,
            ServerHostForIndependentRuntime, StringComparison.Ordinal) ||
            selected.Endpoint.Port != ServerPortForIndependentRuntime;
        return ApplyServerSelectionForIndependentRuntime(directoryIndex, selected.Endpoint.Host,
            selected.Endpoint.Port, switchEndpoint);
    }

    /// <summary>
    /// Forwards the title host's void 0x107d <c>SrvStatReq</c> call (0x060006d5).
    /// DrawServer owns admission; the sender owns return/failure and its native pacing queue.
    /// The count arrives independently through the 0x805b dispatch case.
    /// </summary>
    public void RequestServerOnlineCount() => _gateway.RequestServerOnlineCount();

    /// <summary>
    /// Sends the source-recovered in-world target-status query without mutating local target
    /// state. The server-authored response is published through TargetStatusState.
    /// </summary>
    public Task<Result> QueryTargetStatusAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        return _gateway.QueryTargetStatusAsync(entityId, cancellationToken);
    }

    /// <summary>
    /// Sends the official TaskNpcList request. Success means only that the request frame was
    /// written; the authoritative replacement arrives through NpcTaskOffersState.
    /// </summary>
    public Task<Result> RequestNpcTaskOffersAsync(
        string npcName,
        CancellationToken cancellationToken)
    {
        return _gateway.RequestNpcTaskOffersAsync(npcName, cancellationToken);
    }

    public Task<Result> OpenShopAsync(int shopId, CancellationToken cancellationToken)
    {
        return InWorldOrFailure(
            () => _gateway.OpenShopAsync(shopId, cancellationToken),
            "NPC shops can only be opened in the world.");
    }

    public Task<Result> BuyShopItemAsync(
        int itemId,
        int count,
        CancellationToken cancellationToken)
    {
        return InWorldOrFailure(
            () => _gateway.BuyShopItemAsync(itemId, count, cancellationToken),
            "Shop items can only be bought in the world.");
    }

    public Task<Result> SellInventoryItemAsync(
        byte tab,
        int bagIndex,
        int count,
        int itemId,
        bool confirmed,
        CancellationToken cancellationToken)
    {
        return InWorldOrFailure(
            () => _gateway.SellInventoryItemAsync(
                tab,
                bagIndex,
                count,
                itemId,
                confirmed,
                cancellationToken),
            "Inventory items can only be sold in the world.");
    }

    public Task<Result> OpenTransportAsync(
        int transportList,
        CancellationToken cancellationToken)
    {
        // DialogUI's original callback reads NetClient.I directly, without a session-stage gate.
        if (!cancellationToken.CanBeCanceled)
            return _gateway.OpenTransportAsync(transportList, cancellationToken);
        return InWorldOrFailure(
            () => _gateway.OpenTransportAsync(transportList, cancellationToken),
            "Transport can only be opened in the world.");
    }

    public Task<Result> ChooseTransportAsync(
        int transportList,
        int destinationIndex,
        CancellationToken cancellationToken)
    {
        return _gateway.ChooseTransportAsync(
            transportList,
            destinationIndex,
            cancellationToken);
    }

    public Task<Result> OpenWarehouseAsync(CancellationToken cancellationToken)
    {
        return InWorldOrFailure(
            () => _gateway.OpenWarehouseAsync(cancellationToken),
            "The warehouse can only be opened in the world.");
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
        return InWorldOrFailure(
            () => _gateway.MoveWarehouseItemAsync(
                direction,
                fromTab,
                fromSlot,
                warehouseTab,
                count,
                toSlot,
                cancellationToken),
            "Warehouse items can only be moved in the world.");
    }

    public Task<Result> RequestWarehouseMoneyAsync(int mode, long amount, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? _gateway.WarehouseMoneyAsync(mode, amount, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Warehouse actions can only be used in the world."));

    public Task<Result> RequestWarehouseSplitAsync(int tab, int slot, int count, CancellationToken ct) =>
        _gateway.WarehouseSplitAsync(tab, slot, count, ct);

    public Task<Result> RequestWarehouseRearrangeAsync(int fromTab, int fromSlot, int toTab, int toSlot, CancellationToken ct) =>
        _gateway.WarehouseRearrangeAsync(fromTab, fromSlot, toTab, toSlot, ct);

    /// <summary>
    /// Sends the official TaskAccept command. A successful write is not task acceptance; the
    /// authoritative result arrives through TaskLogState and a refreshed NPC offer list.
    /// </summary>
    public Task<Result> AcceptTaskAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        return _gateway.AcceptTaskAsync(taskId, cancellationToken);
    }

    /// <summary>
    /// Sends the official TaskAbandon command without removing local task state optimistically.
    /// </summary>
    public Task<Result> AbandonTaskAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        return _gateway.AbandonTaskAsync(taskId, cancellationToken);
    }

    /// <summary>
    /// Sends the official TaskComplete command. Multi-step advance and final reward remain the
    /// server's decision and are observed only through later authoritative packets.
    /// </summary>
    public Task<Result> CompleteTaskAsync(
        int taskId,
        CancellationToken cancellationToken)
    {
        return _gateway.CompleteTaskAsync(taskId, cancellationToken);
    }

    /// <summary>
    /// Sends the official inspect-player request. The UI remains closed until the server-authored
    /// equipment snapshot advances InspectedPlayerEquipmentStateVersion.
    /// </summary>
    public Task<Result> InspectPlayerAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Players can only be inspected in the world."));
        }

        return _gateway.InspectPlayerAsync(entityId, cancellationToken);
    }

    /// <summary>
    /// Sends the official trade invitation. Success means only that the request frame was written;
    /// target delivery, acceptance and an active trade remain server-authored transitions.
    /// </summary>
    public Task<Result> RequestTradeAsync(
        int entityId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Trade invitations can only be sent in the world."));
        }

        return _gateway.RequestTradeAsync(entityId, cancellationToken);
    }

    public Task<Result> RequestPartyInviteAsync(int entityId, string name, CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
            return Task.FromResult(Result.Failure("session.stage.invalid", "Party invitations can only be sent in the world."));
        return PartyCommands.InviteAsync(entityId, name, cancellationToken);
    }

    public Task<Result> CreatePartyAndInviteAsync(string name, IReadOnlyList<PartyInviteTarget> targets, CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
            return Task.FromResult(Result.Failure("session.stage.invalid", "Party creation can only be used in the world."));
        return PartyCommands.CreateAndInviteAsync(name, targets, cancellationToken);
    }

    public Task<Result> RequestFamilyInviteAsync(int entityId, string name, CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
            return Task.FromResult(Result.Failure("session.stage.invalid", "Family invitations can only be sent in the world."));
        return FamilyCommands.InviteAsync(entityId, name, cancellationToken);
    }
    public Task<Result> RequestFamilyListAsync(CancellationToken cancellationToken) =>
        FamilyCommands.ListAsync(cancellationToken);
    public Task<Result> RequestFamilyLeaveAsync(CancellationToken ct) =>
        FamilyCommands.LeaveAsync(ct);
    public Task<Result> RequestFamilyCreateAsync(string name, string proclaim, CancellationToken ct) =>
        FamilyCommands.CreateAsync(name, proclaim, ct);
    public Task<Result> RequestFamilyApplyAsync(string name, CancellationToken ct) =>
        FamilyCommands.ApplyAsync(name, ct);
    public Task<Result> RequestFamilyManageAsync(byte op, string name, int id, string text, CancellationToken ct) =>
        FamilyCommands.ManageAsync(op, name, id, text, ct);
    public Task<Result> RequestFamilyMailAsync(string title, string body, CancellationToken ct) =>
        FamilyCommands.MailAsync(title, body, ct);
    public Task<Result> RequestFamilyPositionsAsync(CancellationToken ct) =>
        FamilyCommands.PositionsAsync(ct);
    public Task<Result> RequestFamilyRefreshAsync(CancellationToken ct) =>
        FamilyCommands.RefreshAsync(ct);
    public Task<Result> RequestFamilyPositionNameAsync(int id, string name, CancellationToken ct) =>
        FamilyCommands.PositionNameAsync(id, name, ct);
    public Task<Result> RequestFamilyStashAsync(int op, int bagTab, int bagSlot, int index, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? FamilyCommands.StashAsync(op, bagTab, bagSlot, index, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Family actions can only be used in the world."));
    public Task<Result> RequestFamilyStashPermissionAsync(int pos, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? FamilyCommands.StashPermissionAsync(pos, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Family actions can only be used in the world."));

    public Task<Result> RequestPartyKickAsync(int entityId, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? PartyCommands.KickAsync(entityId, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Party actions can only be used in the world."));
    public Task<Result> RequestPartyLeaderAsync(int entityId, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? PartyCommands.SetLeaderAsync(entityId, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Party actions can only be used in the world."));
    public Task<Result> RequestPartyJoinAsync(int entityId, string name, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? PartyCommands.JoinAsync(entityId, name, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Party actions can only be used in the world."));
    public Task<Result> RequestPartyLeaveAsync(CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? PartyCommands.LeaveAsync(ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Party actions can only be used in the world."));
    public Task<Result> RequestPartyDismissAsync(CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? PartyCommands.DismissAsync(ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Party actions can only be used in the world."));
    public Task<Result> RequestMailOpenAsync(CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? MailCommands.OpenMailAsync(ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Mail actions can only be used in the world."));
    public Task<Result> RequestMailActionAsync(byte action, long mailId, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? MailCommands.ActionAsync(action, mailId, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Mail actions can only be used in the world."));
    public Task<Result> RequestMailSendAsync(string to, string title, string body, long money, long caiyu, byte type,
        List<(byte, int, int)> attachments, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? MailCommands.SendAsync(to, title, body, money, caiyu, type, attachments, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Mail actions can only be used in the world."));
    public Task<Result> RequestRackOpenAsync(CancellationToken ct) =>
        RackCommands.OpenRackAsync(ct);
    public Task<Result> RequestRackBuyAsync(int itemId, byte source, int count, CancellationToken ct) =>
        RackCommands.BuyAsync(itemId, source, count, ct);
    public Task<Result> RequestRackFittingCatalogAsync(CancellationToken ct) =>
        _state != ClientSessionState.InWorld ? Task.FromResult(Result.Failure("session.stage.invalid", "Rack actions can only be used in the world.")) :
        _gateway is IRackFittingCommands fitting ? fitting.RequestFittingCatalogAsync(ct) : Task.FromResult(Result.Failure("rack.fitting.unsupported", "Fitting commands are unavailable."));
    public Task<Result> RequestRackFittingPageAsync(byte kind, string part, string search, int page, int perPage, CancellationToken ct) =>
        _gateway is IRackFittingCommands fitting ? fitting.RequestFittingPageAsync(kind, part, search, page, perPage, ct) : Task.FromResult(Result.Failure("rack.fitting.unsupported", "Fitting commands are unavailable."));
    public Task<Result> RequestRackFittingDescriptionsAsync(IReadOnlyList<int> ids, CancellationToken ct) =>
        _gateway is IRackFittingCommands fitting ? fitting.RequestFittingDescriptionsAsync(ids, ct) : Task.FromResult(Result.Failure("rack.fitting.unsupported", "Fitting commands are unavailable."));
    public Task<Result> RequestRackDepositAsync(string chain, int amount, CancellationToken ct) =>
        RackCommands.RequestDepositAsync(chain, amount, ct);
    public Task<Result> RequestRackRefundListAsync(CancellationToken ct) =>
        RackCommands.RequestRefundListAsync(ct);
    public Task<Result> RequestRackRefundAsync(int itemId, int unitPrice, int bundle, int day, int shares, CancellationToken ct) =>
        RackCommands.RefundAsync(itemId, unitPrice, bundle, day, shares, ct);
    public Task<Result> RequestRackHelpAsync(CancellationToken ct) =>
        RackCommands.HelpAsync(ct);
    public Task<Result> RequestFriendAddAsync(string name, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? FriendCommands.AddAsync(name, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Friend actions can only be used in the world."));
    public Task<Result> RequestFriendDeleteAsync(string name, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? FriendCommands.DeleteAsync(name, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Friend actions can only be used in the world."));
    public Task<Result> RequestApprenticeAsync(int id, byte role, string name, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? ApprenticeCommands.RequestAsync(id, role, name, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Apprentice actions can only be used in the world."));
    public Task<Result> RequestRideTogetherAsync(int id, CancellationToken ct) =>
        _state == ClientSessionState.InWorld ? RidingCommands.RideTogetherAsync(id, ct) : Task.FromResult(Result.Failure("session.stage.invalid", "Riding actions can only be used in the world."));

    /// <summary>
    /// Sends one response to a server-issued system request. Presentation must not remove the
    /// request locally; the next authoritative queue snapshot owns that transition.
    /// </summary>
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
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "System requests can only be answered in the world."));
        }

        return _gateway.RespondSystemRequestAsync(
            requestId,
            accept,
            cancellationToken);
    }

    /// <summary>
    /// Sends the source-recovered inventory-sort intent. The current inventory remains unchanged
    /// until Protocol publishes the server's next complete replacement snapshot.
    /// </summary>
    public Task<Result> SortInventoryAsync(
        byte tab,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Inventory can only be sorted while in the world."));
        }

        return _gateway.SortInventoryAsync(tab, cancellationToken);
    }

    /// <summary>
    /// Sends the source-recovered ordinary item-use intent. The local cooldown is visual only;
    /// item count and gameplay effects remain unchanged until server-authored snapshots arrive.
    /// </summary>
    public Task<Result> UseInventoryItemAsync(
        int itemId,
        byte tab,
        int bagIndex,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Inventory items can only be used while in the world."));
        }

        return _gateway.UseInventoryItemAsync(
            itemId,
            tab,
            bagIndex,
            cancellationToken);
    }

    /// <summary>
    /// Persists the exact twenty slot identities and expanded state. The visible state is not
    /// changed until the server echoes its sanitized authoritative snapshot.
    /// </summary>
    public Task<Result> SaveHotbarAsync(
        WorldHotbarState state,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "The hotbar can only be saved while in the world."));
        }

        return _gateway.SaveHotbarAsync(state, cancellationToken);
    }

    /// <summary>
    /// Sends one official normal-attack intent. Hit points, damage, death and rewards remain
    /// exclusively server-authored.
    /// </summary>
    public Task<Result> AttackAsync(
        int targetEntityId,
        CancellationToken cancellationToken)
    {
        return _gateway.AttackAsync(targetEntityId, cancellationToken);
    }

    /// <summary>
    /// Sends one source-recovered ground-item pickup intent. The item remains visible and the
    /// inventory remains unchanged until Protocol receives authoritative server packets.
    /// </summary>
    public Task<Result> PickUpGroundItemAsync(
        int groundId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Ground items can only be picked up while in the world."));
        }

        if (groundId <= 0)
        {
            return Task.FromResult(
                Result.Failure(
                    "ground-item.pickup.target.invalid",
                    "The ground-item identity is invalid."));
        }

        return _gateway.PickUpGroundItemAsync(groundId, cancellationToken);
    }

    /// <summary>
    /// Reports the locally predicted map-pixel position; server validation remains authoritative.
    /// </summary>
    public Task<Result> ReportPlayerMoveAsync(
        int x,
        int y,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Movement can only be reported while in the world."));
        }

        return _gateway.ReportPlayerMoveAsync(x, y, cancellationToken);
    }

    /// <summary>
    /// Writes the official periodic position-save root without treating send success as acceptance.
    /// </summary>
    public Task<Result> SavePlayerPositionAsync(
        int mapId,
        int x,
        int y,
        bool entered,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Position can only be saved while in the world."));
        }

        return _gateway.SavePlayerPositionAsync(
            mapId,
            x,
            y,
            entered,
            cancellationToken);
    }

    /// <summary>
    /// Sends one source-recovered revive choice. Only a later server life edge may revive the
    /// local actor; the official dialog is free to hide after this request is queued.
    /// </summary>
    public Task<Result> RequestReviveAsync(
        WorldReviveOption option,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Revive choices can only be used while in the world."));
        }

        WorldLocalPlayerLifeState life = _gateway.LocalPlayerLifeState;
        if (life == null || life.Alive || !life.Allows(option))
        {
            return Task.FromResult(
                Result.Failure(
                    "combat.revive.choice.unavailable",
                    "The selected revive choice is not available for the current death edge."));
        }

        return _gateway.RequestReviveAsync(option, cancellationToken);
    }

    /// <summary>
    /// Sends the official direct-target character-skill intent. Damage, effects and cooldowns
    /// remain server-authored.
    /// </summary>
    public Task<Result> UseCharacterSkillAsync(
        int skillId,
        int targetEntityId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Skills can only be used while in the world."));
        }

        ICharacterSkillUsePort owner = _characterSkillUse;
        if (owner == null)
        {
            return Task.FromResult(Result.Failure("skill.owner.unavailable", "The character skill-use owner is unavailable."));
        }
        return owner.UseSkillAsync(
            skillId,
            targetEntityId,
            cancellationToken);
    }

    /// <summary>Handles native request/queue state on the client main thread; sending is observed separately.</summary>
    public bool RequestCharacterSkill(int skillId, float rangePixels, CancellationToken cancellationToken) =>
        _state == ClientSessionState.InWorld && _characterSkillUse != null &&
        _characterSkillUse.RequestSkill(skillId, rangePixels, cancellationToken);

    /// <summary>Invokes the native current-target skill entry on the client main thread.</summary>
    public Task<Result> CastCharacterSkillAtTargetAsync(int skillId, CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
            return Task.FromResult(Result.Failure("session.stage.invalid", "Skills require an active world session."));
        ICharacterSkillUsePort owner = _characterSkillUse;
        if (owner == null)
            return Task.FromResult(Result.Failure("skill.owner.unavailable", "The character skill-use owner is unavailable."));
        return owner.CastAtTargetAsync(skillId, cancellationToken);
    }

    /// <summary>
    /// Sends the official coordinate-bearing character-skill intent. Native target casting also
    /// uses this wire form; resolution and combat outcomes remain server-authored.
    /// </summary>
    public Task<Result> UseCharacterSkillAtAsync(
        int skillId,
        int x,
        int y,
        int targetEntityId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Skills can only be used while in the world."));
        }

        ICharacterSkillUsePort owner = _characterSkillUse;
        if (owner == null)
        {
            return Task.FromResult(Result.Failure("skill.owner.unavailable", "The character skill-use owner is unavailable."));
        }
        return owner.UseSkillAtAsync(
            skillId,
            x,
            y,
            targetEntityId,
            cancellationToken);
    }

    /// <summary>
    /// Sends the official current-pet skill intent. Pet state and effects remain server-authored.
    /// </summary>
    public Task<Result> UsePetSkillAsync(
        int skillId,
        CancellationToken cancellationToken)
    {
        return _gateway.UsePetSkillAsync(skillId, cancellationToken);
    }

    // Official NetClient.PetAddPoint 0x06000734: one tail call to PetCmd(this, 2, arg, "") after
    // the metadata-usage guard - no range test, no amount argument, no local free-point
    // decrement. Since WBD-119 the validity test this command used to apply through
    // WorldPetAttributeKind is gone for the same reason the 0..9 slot test is gone from
    // RecallPetAsync: it has no native counterpart and the server adjudicates the code.
    // The automatic native caller reaches the same sender without an added world-stage gate.
    public Task<Result> AllocatePetPointAsync(
        int attributeCode,
        CancellationToken cancellationToken)
    {
        return _gateway.AllocatePetPointAsync(attributeCode, cancellationToken);
    }

    // Official NetClient.PetRename 0x06000735 is 16 bytes of argument shuffling and a tail call to
    // PetCmd(this, 3, 0, text) - no trim, no length test, no null substitution. Those belong to the
    // only caller, Win05.<DrawPet>g__SubmitName|87_1 0x06001060, which trims the field once and
    // skips empty results before it ever reaches this member; duplicating them here would be a
    // second, later decision on a value the official client has already sent. The lifecycle gate
    // stays for the same reason it does above.
    public Task<Result> RenamePetAsync(
        string name,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return PetCommandStageFailure();
        }
        return _gateway.RenamePetAsync(name, cancellationToken);
    }

    // Official NetClient.PetSetSkill 0x06000736 is one tail call to PetCmd(this, 4, skillId, "") -
    // arm64 0x26e828 / x86_64 0x27dda0, no test of any kind. Since WBD-120 the "skillId < 0"
    // identity test this command used to share with the hotbar pet-skill route is gone for the
    // same reason the attribute range test is gone from AllocatePetPointAsync: it is not a native
    // branch and the server adjudicates the id. The only caller, Win05.DrawPet 0x06001043, passes
    // the literal 0, so the wire value is decided at the hit site, not here. No additional
    // world-stage decision intervenes between the automatic native caller and the sender.
    public Task<Result> SetPetSkillAsync(
        int skillId,
        CancellationToken cancellationToken)
    {
        return _gateway.SetPetSkillAsync(skillId, cancellationToken);
    }

    // Official CharData.SwitchPet 0x06000f22 tests only -1 < idx before it calls
    // NetClient.PetCmd(NetClient.I, 5, idx, string.Empty): arm64 0x394a98 tbnz w19, #0x1f ->
    // 0x394b5c and x86_64 0x3a7da0-0x3a7da2 test/js are the whole validation, and the only
    // caller is the double-click arm of Win05.DrawPet 06001043, which passes the ten-slot loop
    // index. The 0..9 ValidPetSlot range test this command used to share with
    // DeployPetAsync/HatchPetAsync through SendPetSlotCommandAsync is therefore not a native
    // branch and is gone here; since WBD-083 those two drop it as well. Slot identity stays
    // server-adjudicated: a
    // double click on an empty or locked cell sends the request the official client sends and
    // the server decides. The only gate left is the lifecycle one, _state != InWorld, and it is
    // dominated by the draw path - ClientRootView.OnGUI (ClientRootView.cs:254) and Update
    // (ClientRootView.cs:303) run the world window, and so Win05.DrawPet 06001043, only while
    // _session.State == ClientSessionState.InWorld - so it cannot drop a command the official
    // NetClient.Send would have written.
    public Task<Result> SwitchPetAsync(int slot, CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return PetCommandStageFailure();
        }
        return _gateway.SwitchPetAsync(slot, cancellationToken);
    }

    // Official NetClient.PetRecall 0x06000738 is PetCmd(this, 7, slot, string.Empty) with no
    // validation: the frozen arm64 0x26e8f0 / x86_64 0x27de40 bodies forward the Int32 unchanged
    // and hand it to the writer <PetCmd>b__0 0x06000829, which writes U8 7 / I32 slot / Str ""
    // inside NetClient.Send 0x06000772 after its connected test. The 0..9 range gate this command
    // used to share with SwitchPetAsync/DeployPetAsync/HatchPetAsync through SendPetSlotCommandAsync
    // is therefore not a native branch and is gone here; since WBD-083 DeployPetAsync and
    // HatchPetAsync drop it too and the shared helper is gone. Slot identity stays
    // server-adjudicated:
    // a click on an empty or locked slot sends the same request the official client sends and the
    // server decides. The existing native caller reaches the sender without an additional
    // world-stage decision; the sender still owns its real connection check.
    public Task<Result> RecallPetAsync(int slot, CancellationToken cancellationToken)
    {
        return _gateway.RecallPetAsync(slot, cancellationToken);
    }

    // Official NetClient.PetDeploy 0x06000739: the frozen arm64 0x26e954 / x86_64 0x27de90 bodies
    // are one tail call - PetCmd(this, 8, storeIdx, string.Empty) - and validate nothing, so the
    // 0..9 ValidPetSlot range test this command used to share with HatchPetAsync, SwitchPetAsync
    // and RecallPetAsync through SendPetSlotCommandAsync is not a native branch and is gone here
    // since WBD-083, exactly as it already is for the other three. The slot identity stays
    // server-adjudicated. The automatic InventoryDlg caller reaches this sender without an
    // additional world-stage decision; the sender still owns its real connection check.
    public Task<Result> DeployPetAsync(int slot, CancellationToken cancellationToken)
    {
        return _gateway.DeployPetAsync(slot, cancellationToken);
    }

    // Official NetClient.PetHatch 0x0600073a is the same one-tail-call body with subcommand 9
    // (arm64 0x26e9b8 / x86_64 0x27dee0), so the same reasoning removes the 0..9 range test here.
    public Task<Result> HatchPetAsync(int slot, CancellationToken cancellationToken)
    {
        return _gateway.HatchPetAsync(slot, cancellationToken);
    }

    public Task<Result> SetPetShownAsync(
        bool show,
        CancellationToken cancellationToken)
    {
        return _gateway.SetPetShownAsync(show, cancellationToken);
    }

    public Task<Result> SwapPetAsync(
        int fromSlot,
        int toSlot,
        CancellationToken cancellationToken)
    {
        // Official NetClient.PetSwap 0x0600073d validates nothing either: arm64 0x26eae8 /
        // x86_64 0x27dfe0 convert the second argument with the culture-sensitive
        // Int32.ToString() and tail call PetCmd(this, 12, from, <that string>). The two business
        // gates that used to sit here are gone: the 0..9 range test has no native counterpart,
        // and the fromSlot == toSlot interception duplicates the official branch the only call
        // site already takes - Win05.DrawPet 06001043 tests target != held before
        // NetClient.I.PetSwap(held, target) (arm64 0x3bca6c-0x3bca70 / x86_64 0x3d0712-0x3d0714).
        // Both arguments keep travelling to the server as sent; reordering legality is the
        // server's decision, as it is in the official client. There is no additional world-stage
        // gate between this actual native caller and the same sender.
        return _gateway.SwapPetAsync(fromSlot, toSlot, cancellationToken);
    }

    private static Task<Result> PetCommandStageFailure()
    {
        return Task.FromResult(Result.Failure(
            "session.stage.invalid",
            "Pet commands can only be used while in the world."));
    }

    public Task<Result> SetPetViewAsync(
        int mask,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Pet visibility can only be changed while in the world."));
        }

        if (mask < 0 || mask > 7)
        {
            return Task.FromResult(
                Result.Failure(
                    "pet.view.mask.invalid",
                    "The pet visibility mask is invalid."));
        }

        return _gateway.SetPetViewAsync(mask, cancellationToken);
    }

    public Task<Result> SetCharacterTitleAsync(
        string title,
        CancellationToken cancellationToken)
    {
        string candidate = title ?? string.Empty;
        WorldCharacterTitleState state = _gateway.CharacterTitleState;
        if (state == null || !state.Owns(candidate))
        {
            return Task.FromResult(Result.Failure(
                "character.title.not-owned",
                "The character does not own that title."));
        }

        return _gateway.SetCharacterTitleAsync(candidate, cancellationToken);
    }

    public Task<Result> SetEquipmentEffectsMaskAsync(
        int hideMask,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(Result.Failure(
                "session.stage.invalid",
                "Equipment-effect preferences can only be changed in the world."));
        }

        return _gateway.SetEquipmentEffectsMaskAsync(
            hideMask,
            cancellationToken);
    }

    public Task<Result> StoreWardrobeItemAsync(
        byte bagTab,
        int bagSlot,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(Result.Failure(
                "session.stage.invalid",
                "Wardrobe items can only be stored while in the world."));
        }

        return _gateway.StoreWardrobeItemAsync(
            bagTab,
            bagSlot,
            cancellationToken);
    }

    public Task<Result> ToggleWardrobeItemAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(Result.Failure(
                "session.stage.invalid",
                "Wardrobe appearances can only be changed while in the world."));
        }

        return _gateway.ToggleWardrobeItemAsync(itemId, cancellationToken);
    }

    // Boundary marker (WBD-058): reached synchronously from the official extraction confirmation
    // (0x06001071) through NetClient.I.WardrobeRemove, not from a deferred intent pump. The
    // InWorld gate below is a product-only convention shared with the other wardrobe commands;
    // the official body has no stage gate.
    public Task<Result> ExtractWardrobeItemAsync(
        int itemId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(Result.Failure(
                "session.stage.invalid",
                "Wardrobe items can only be extracted while in the world."));
        }

        return _gateway.ExtractWardrobeItemAsync(itemId, cancellationToken);
    }

    public Task<Result> MoveWardrobeItemAsync(
        int itemId,
        int categoryIndex,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(Result.Failure(
                "session.stage.invalid",
                "Wardrobe items can only be reordered while in the world."));
        }

        return _gateway.MoveWardrobeItemAsync(
            itemId,
            categoryIndex,
            cancellationToken);
    }

    public Task<Result> UpgradeSkillAsync(
        int skillId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Skills can only be upgraded while in the world."));
        }

        return _gateway.UpgradeSkillAsync(skillId, cancellationToken);
    }

    public Task<Result> LearnLifeSkillAsync(
        int skillId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Life skills can only be requested while in the world."));
        }

        return _gateway.LearnLifeSkillAsync(skillId, cancellationToken);
    }

    /// <summary>
    /// Sends the source-recovered capture-item intent. A zero target is preserved because the
    /// five basic official capture tools still submit that value; visible state remains server
    /// authoritative.
    /// </summary>
    public Task<Result> UseCaptureInventoryItemAsync(
        int targetEntityId,
        int itemId,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Capture items can only be used while in the world."));
        }

        return _gateway.UseCaptureInventoryItemAsync(
            targetEntityId,
            itemId,
            cancellationToken);
    }

    /// <summary>
    /// Sends the source-recovered target-tool intent. Binding, fused appearance, equipment and
    /// inventory remain unchanged until later server-authored snapshots arrive.
    /// </summary>
    public Task<Result> UseInventoryToolOnAsync(
        int toolId,
        byte targetKind,
        byte tab,
        int slot,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Inventory tools can only be used while in the world."));
        }

        return _gateway.UseInventoryToolOnAsync(
            toolId,
            targetKind,
            tab,
            slot,
            cancellationToken);
    }

    /// <summary>
    /// Sends the source-recovered stack-split intent. The immutable inventory is not changed
    /// until Protocol publishes a later complete server-authored replacement snapshot.
    /// </summary>
    public Task<Result> SplitInventoryItemAsync(
        byte tab,
        int bagIndex,
        int count,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Inventory stacks can only be split while in the world."));
        }

        return _gateway.SplitInventoryItemAsync(
            tab,
            bagIndex,
            count,
            cancellationToken);
    }

    /// <summary>
    /// Sends the source-recovered equipment intent. The current bag, worn set, attributes and
    /// appearance remain unchanged until Protocol publishes the server's later snapshots.
    /// </summary>
    public Task<Result> EquipInventoryItemAsync(
        int bagIndex,
        CancellationToken cancellationToken)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Inventory items can only be equipped while in the world."));
        }

        return _gateway.EquipInventoryItemAsync(bagIndex, cancellationToken);
    }

    /// <summary>
    /// Sends the source-recovered unequip intent. Worn equipment, bag contents, attributes and
    /// appearance remain unchanged until Protocol publishes the server's later snapshots.
    /// </summary>
    public Task<Result> UnequipInventoryItemAsync(
        int slot,
        CancellationToken cancellationToken)
    {
        return _gateway.UnequipInventoryItemAsync(slot, cancellationToken);
    }

    /// <summary>
    /// Sends seven source-recovered positional attribute deltas. The displayed attributes remain
    /// server-authored and change only after the later complete attribute snapshot. The array is
    /// forwarded by reference, as the official NetClient.AllocPoints 0x0600071a argument is.
    /// </summary>
    public Task<Result> AllocateAttributePointsAsync(
        int[] points,
        CancellationToken cancellationToken)
    {
        return _gateway.AllocateAttributePointsAsync(points, cancellationToken);
    }

    /// <summary>
    /// Writes one offer intent. The UI changes only when the server publishes a trade state.
    /// The offer list is forwarded as the same List instance: the official writer reads it inside
    /// NetClient.Send, after the connection gate, so no projection happens on this path.
    /// </summary>
    public Task<Result> SubmitTradeOfferAsync(
        long money,
        List<(byte Tab, int Slot, int Count)> items,
        CancellationToken cancellationToken)
    {
        return _gateway.SubmitTradeOfferAsync(money, items, cancellationToken);
    }

    public Task<Result> SetTradeLockAsync(
        bool locked,
        CancellationToken cancellationToken)
    {
        return _gateway.SetTradeLockAsync(locked, cancellationToken);
    }

    // Official NetClient.ArenaRevive (0x06000746) -> opcode 0x1084, one U8 (0 revive / 1 surrender).
    // The server decides what either click actually does; the coordinator adds no arena or
    // world-stage admission before the existing sender.
    public Task<Result> ArenaReviveAsync(
        bool surrender,
        CancellationToken cancellationToken)
    {
        return _gateway.ArenaReviveAsync(surrender, cancellationToken);
    }

    // Official NetClient.ArenaPick (0x06000744) -> opcode 0x1083, one U8 holding the clicked card
    // slot. Which talent that slot holds is server-authored; the coordinator adds no card or
    // world-stage admission before the existing sender.
    public Task<Result> ArenaPickAsync(
        int index,
        CancellationToken cancellationToken)
    {
        return _gateway.ArenaPickAsync(index, cancellationToken);
    }

    // Official NetClient.ArenaReroll (0x06000745) -> opcode 0x1086 with no payload at all. The
    // reroll price and the free-reroll count are server-authored, so nothing here checks balance.
    public Task<Result> ArenaRerollAsync(CancellationToken cancellationToken)
    {
        return _gateway.ArenaRerollAsync(cancellationToken);
    }

    public Task<Result> ConfirmTradeAsync(CancellationToken cancellationToken)
    {
        return _gateway.ConfirmTradeAsync(cancellationToken);
    }

    public Task<Result> CancelTradeAsync(CancellationToken cancellationToken)
    {
        return _gateway.CancelTradeAsync(cancellationToken);
    }

    /// <summary>
    /// Writes one source-recovered chat request while retaining server authority over outcomes.
    /// </summary>
    public Task<Result> SendWorldChatAsync(
        WorldChatSendRequest request,
        CancellationToken cancellationToken)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure(
                    "session.stage.invalid",
                    "Chat can only be sent while in the world."));
        }

        return _gateway.SendWorldChatAsync(request, cancellationToken);
    }

    public void ReturnToLogin()
    {
        if (_state != ClientSessionState.ServerSelection &&
            _state != ClientSessionState.CharacterSelection)
        {
            return;
        }

        _gateway.Dispose();
        Interlocked.Exchange(ref _localPlayerStatusEffectConsumedRevision, 0);
        ResetCombatNumberPresentation();
        _connectedServerIndex = -1;
        _characters = Array.Empty<ServerCharacterRecord>();
        TransitionTo(ClientSessionState.Offline, string.Empty);
    }

    /// <summary>
    /// Routing half of TitleFlow.NoteDisconnected (0x06000ec0), whose first store
    /// unconditionally selects Login. OnNetDropped (0x060003f0) has already selected
    /// this path using the actual worldRoot and resolved a non-null TitleFlow receiver.
    /// A session-stage check here would add a second, different condition to that native branch.
    /// Keep the existing projection and observation; do not dispose the gateway, clear
    /// the roster or cancel an operation that may later publish its own state.
    /// </summary>
    public void NoteTransportDisconnected()
    {
        ResetCombatNumberPresentation();
        TransitionTo(ClientSessionState.Offline, string.Empty);
    }

    /// <summary>
    /// The same unconditional Login store after DisconnectToLogin (0x060003f1) has
    /// torn down the world, added its fresh TitleFlow and assigned boot. The prompt may
    /// outlive the session stage that was current when OnNetDropped displayed it.
    /// This projection therefore follows the actual callback, without an InWorld gate.
    /// As on the retained-title path, there is no extra transport disposal, roster clear
    /// or cancellation; the existing TitleFlow receiver performs the remaining stores.
    /// </summary>
    public void NoteTransportDisconnectedFromWorld()
    {
        ResetCombatNumberPresentation();
        TransitionTo(ClientSessionState.Offline, string.Empty);
    }

    // DrawCharSelect (06000ef7) Return writes state=4 (ServerSelect) only. It neither
    // closes the authenticated connection nor clears the roster nor cancels an awaited entry.
    public void ReturnToServerSelection()
    {
        if (_state == ClientSessionState.CharacterSelection ||
            _state == ClientSessionState.EnteringWorld)
            TransitionTo(ClientSessionState.ServerSelection, string.Empty);
    }

    public Result BeginCharacterCreation()
    {
        if (_state != ClientSessionState.CharacterSelection &&
            _state != ClientSessionState.EnteringWorld)
        {
            return Result.Failure(
                "session.stage.invalid",
                "Character selection is not active.");
        }

        if (_characters.Count >= 3)
        {
            return Result.Failure(
                "character.create.roster.full",
                "The account already has the maximum number of characters.");
        }

        TransitionTo(ClientSessionState.CharacterCreation, string.Empty);
        return Result.Success();
    }

    public void ReturnToCharacterSelection()
    {
        if (_state == ClientSessionState.CharacterCreation)
        {
            TransitionTo(ClientSessionState.CharacterSelection, string.Empty);
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

        bool lockTaken = false;
        try
        {
            await _operation.WaitAsync(cancellationToken);
            lockTaken = true;
            if (_state != ClientSessionState.CharacterCreation)
            {
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "session.stage.invalid",
                    "Character creation is not active.");
            }

            Result<IReadOnlyList<ServerCharacterRecord>> creation =
                await _gateway.CreateCharacterAsync(request, cancellationToken);
            if (!creation.IsSuccess)
            {
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    creation.Error.Code,
                    creation.Error.Message);
            }

            IReadOnlyList<ServerCharacterRecord> response = ObserveCharacterRosterResponse(creation.Value);
            if (!UsesNativeTitleResultReceipts)
                TransitionTo(ClientSessionState.CharacterSelection, string.Empty);
            return Result<IReadOnlyList<ServerCharacterRecord>>.Success(response);
        }
        catch (OperationCanceledException)
        {
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "session.cancelled",
                "Character creation was cancelled.");
        }
        finally
        {
            if (lockTaken)
            {
                _operation.Release();
            }
        }
    }

    public async Task<Result<IReadOnlyList<ServerCharacterRecord>>> DeleteCharacterAsync(
        string characterId,
        CancellationToken cancellationToken)
    {
        bool lockTaken = false;
        try
        {
            await _operation.WaitAsync(cancellationToken);
            lockTaken = true;
            if (_state != ClientSessionState.CharacterSelection)
            {
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    "session.stage.invalid",
                    "Character selection is not active.");
            }

            CharacterId selected = new CharacterId(characterId);
            Result<IReadOnlyList<ServerCharacterRecord>> deletion =
                await _gateway.DeleteCharacterAsync(selected, cancellationToken);
            if (!deletion.IsSuccess)
            {
                return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                    deletion.Error.Code,
                    deletion.Error.Message);
            }

            return Result<IReadOnlyList<ServerCharacterRecord>>.Success(
                ObserveCharacterRosterResponse(deletion.Value));
        }
        catch (OperationCanceledException)
        {
            return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(
                "session.cancelled",
                "Character deletion was cancelled.");
        }
        finally
        {
            if (lockTaken)
            {
                _operation.Release();
            }
        }
    }

    public async Task<Result<WorldEntryRecord>> EnterWorldAsync(
        string characterId,
        CancellationToken cancellationToken)
    {
        bool lockTaken = false;
        try
        {
            await _operation.WaitAsync(cancellationToken);
            lockTaken = true;
            if (_state != ClientSessionState.CharacterSelection)
            {
                return Result<WorldEntryRecord>.Failure(
                    "session.stage.invalid",
                    "Character selection is not active.");
            }

            ServerCharacterRecord? selectedCharacter = null;
            for (int index = 0; index < _characters.Count; index++)
            {
                if (string.Equals(
                        _characters[index].CharacterId,
                        characterId,
                        StringComparison.Ordinal))
                {
                    selectedCharacter = _characters[index];
                    break;
                }
            }

            if (selectedCharacter == null)
            {
                return Result<WorldEntryRecord>.Failure(
                    "session.character.invalid",
                    "The selected character does not exist in the authoritative roster.");
            }

            _selectedEntryRequestActive = true;
            _selectedEntryAtReceipt = null;
            _selectedEntryReceiptFailure = null;
            TransitionTo(ClientSessionState.EnteringWorld, string.Empty);
            Result<WorldEntryRecord> entry = await _gateway.EnterWorldAsync(
                new CharacterId(characterId),
                selectedCharacter.Value.Level,
                cancellationToken);
            if (!entry.IsSuccess)
            {
                TransitionTo(ClientSessionState.Failed, entry.Error.Code);
                return entry;
            }

            return Result<WorldEntryRecord>.Success(
                _selectedEntryAtReceipt ?? SelectWorldEntryForHostBuild(entry.Value));
        }
        catch (OperationCanceledException) when (!lockTaken || _selectedEntryReceiptFailure == null)
        {
            TransitionTo(ClientSessionState.CharacterSelection, "session.cancelled");
            return Result<WorldEntryRecord>.Failure(
                "session.cancelled",
                "World entry was cancelled.");
        }
        finally
        {
            if (lockTaken)
            {
                // The receipt can synchronously request terminal session disposal. Release
                // only a still-live operation; disposal and this finally share that boundary.
                lock (_operation)
                {
                    _selectedEntryRequestActive = false;
                    _selectedEntryAtReceipt = null;
                    _selectedEntryReceiptFailure = null;
                    if (!_selectedEntryOperationDisposed) _operation.Release();
                }
            }
        }
    }

    public void Dispose()
    {
        DisposeWithGatewayReleaseForIndependentRuntime(_gateway.Dispose);
    }

    // The composition root substitutes only terminal gateway bookkeeping when
    // an actual Unity network component owns Close. Ordinary Dispose is unchanged.
    public void DisposeWithGatewayReleaseForIndependentRuntime(Action releaseGateway)
    {
        releaseGateway();
        Interlocked.Exchange(ref _localPlayerStatusEffectConsumedRevision, 0);
        ResetCombatNumberPresentation();
        if (_addressDiscovery is IDisposable disposableDiscovery)
        {
            disposableDiscovery.Dispose();
        }
        lock (_operation)
        {
            _selectedEntryOperationDisposed = true;
            _operation.Dispose();
        }
    }

    private Result<IReadOnlyList<ServerCharacterRecord>> FailRoster(Error error)
    {
        TransitionTo(ClientSessionState.Failed, error.Code);
        return Result<IReadOnlyList<ServerCharacterRecord>>.Failure(error.Code, error.Message);
    }

    private Task<Result> InWorldOrFailure(
        Func<Task<Result>> action,
        string message)
    {
        if (_state != ClientSessionState.InWorld)
        {
            return Task.FromResult(
                Result.Failure("session.stage.invalid", message));
        }

        return action();
    }

    /// <summary>
    /// Official TitleFlow.DoEnterWorld (0x06000efd, arm64 0x391b34 / x86_64 0x3a4e70) step 8: the
    /// level the enter callback publishes comes from the enter packet itself,
    /// <c>EnterInfo.attrs[0]</c>, and not from the roster the selection screen showed - arm64
    /// <c>0x391d0c ldr w21,[x21,#0x20]</c> / <c>0x391d28 str w21,[x8,#0x20]</c> on the short-array
    /// branch and <c>0x391dcc ldr w24,[x21,#0x20]</c> / <c>0x391dec str w24,[x9,#0x20]</c> on the
    /// seven-element branch; x86_64 0x3a50b4 / 0x3a50d5 and 0x3a55a5 / 0x3a55ca are the same two
    /// stores. The array element is the server's current level, so a level the server changed
    /// after the roster was listed is the one the world starts with.
    /// </summary>
    /// <remarks>
    /// A null or empty attrs array performs no level store at all - arm64 <c>0x391cf4 cbz x21</c>
    /// and <c>0x391d04 cbz x8</c>, x86_64 <c>0x3a5095</c> and <c>0x3a50aa</c> - so the value the
    /// official client keeps is the one <c>CharData.level</c> already holds, which the selection
    /// mirror TitleFlow.MirrorSlotToCharData (0x06000efb) wrote from the roster unless a startup
    /// 0x8007 with at least seven elements was dispatched before this entry and overwrote it.
    /// <c>PrivateServerGateway.EnterWorldAsync</c> already reads that static through
    /// <c>IWorldReceiveOwner.ReadEntryRetainedScalars</c> and carries the resolved value in this
    /// exact returned record before merging earlier startup snapshots into its separate status
    /// projection. Keep that entry value when the store was skipped; replacing it with the
    /// selected roster level would discard the official retained scalar. A missing receive
    /// owner keeps the gateway's existing roster fallback. No native store or second read is
    /// introduced here.
    /// </remarks>
    private static int ResolveEnterWorldLevel(WorldEntryRecord entry)
    {
        IReadOnlyList<int> attributes = entry.Attributes;
        if (attributes != null && attributes.Count >= 1)
        {
            return attributes[0];
        }

        return entry.Level;
    }

    private static IReadOnlyList<ServerCharacterRecord> MapRoster(
        IReadOnlyList<ServerCharacterRecord> roster)
    {
        // f03 stores the completed decoder array itself before releasing avatars.
        // Both sides carry the same value-row type; no covariance or row/list allocation.
        // The native null-payload arm alone allocates a fresh empty roster.
        return roster ?? new ServerCharacterRecord[0];
    }

    private void TransitionTo(ClientSessionState next, string reasonCode)
    {
        ClientSessionState previous = _state;
        _state = next;
        _events.Publish(new SessionStateChanged(previous, next, reasonCode));
    }
}
}
