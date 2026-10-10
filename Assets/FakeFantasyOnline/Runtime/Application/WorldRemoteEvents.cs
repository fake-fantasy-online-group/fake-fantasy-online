using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace FakeFantasyOnline.Application
{

/// <summary>The ordered rename payload retains its wire identity and name verbatim.</summary>
public sealed class WorldPlayerRenameState
{
    public WorldPlayerRenameState(int entityId, string name)
    {
        EntityId = entityId;
        Name = name;
    }

    public int EntityId { get; }
    public string Name { get; }
}

public interface IWorldRemoteEventSource
{
    WorldRemoteEventWindow CaptureRemoteEvents();
    bool AcknowledgeRemoteEvents(WorldRemoteEventWindow window);
}

public enum WorldRemoteEventKind
{
    Spawn,
    Remove,
    Appearance,
    Path,
    Action,
    Life,
    Riding,
    Pk,
    Party,
    Stall,
    StatusFx,
    Hp,
    Rename,
    EntitySpawn,
    EntityRemove,
    EntityPath,
    EntityStatusFx,
    EntityStatusIcons,
    EntityHp,
    Combat,
    ChatBubble,
    // Official 0x8019; consumed by CombatWorld.ShowBubble (0x06000139), not by
    // CombatWorld.ShowPlayerBubble (0x0600013a).
    EntityBubble,
    Emote,
    Hair,
    DuelHud,
    // Official 0x8046 (GameBootstrap.OnNetNpcSpawn, arm64 b 0x423a68 / x86_64 jmp 0x438d50) and
    // 0x8047 (GameBootstrap.OnNetNpcRemove, arm64 b 0x423c24 / x86_64 jmp 0x438ee0). One edge per
    // received packet, so two identical 0x8046 packets for one id are two rebuilds.
    NpcSpawn,
    NpcRemove,
    // Official 0x8043: one ordered loot-owner write onto an already registered monster. It rides
    // the same window as EntitySpawn/EntityRemove because CombatWorld.SetOwner (0x06000132) only
    // writes to an id CombatWorld.monsters already holds.
    EntityOwner,
    // Official 0x805d: one ordered remote invisibility write. Native case order is Buf.I32 id,
    // Buf.U8, CombatWorld.FindPlayer (0x0600013b) and only then RemotePlayer.SetInvis
    // (0x06000af9) - there is no CombatWorld wrapper and no second cache, so the payload is the
    // single compared byte and nothing else. An id with no live identity consumes nothing at the
    // producer, exactly like the null lookup that ends the official case.
    Invis
}

/// <summary>One ordered, decoded receive operation; unused payload alternatives are null.</summary>
public sealed class WorldRemoteEvent
{
    public WorldRemoteEvent(long sequence, WorldRemoteEventKind kind, int entityId,
        long incarnation, WorldRemotePlayerState player = null,
        WorldPlayerAppearanceState appearance = null, WorldRemotePathState path = null,
        WorldRemoteActionState action = null,
        WorldRemoteRidingState riding = null, bool? alive = null, byte? pkState = null,
        WorldPartyState party = null, WorldStallOwnerState stall = null,
        byte removeReason = 0, bool repeatedSpawn = false,
        WorldEntityStatusEffect statusEffect = null, int? hitPoints = null,
        WorldPlayerRenameState rename = null, WorldEntityState entity = null,
        WorldCombatResultState combat = null, string bubbleText = null, string bubbleTheme = null, int? emoteFace = null, WorldDuelHudState duelHud = null,
        WorldNpcState npc = null, int ownerId = 0, bool? invisible = null,
        bool nativeRegistryHandled = false, object receivedIdentity = null)
    {
        NativeRegistryHandled = nativeRegistryHandled;
        ReceivedIdentity = receivedIdentity;
        DuelHud = duelHud;
        Npc = npc;
        OwnerId = ownerId;
        Sequence = sequence;
        Kind = kind;
        EntityId = entityId;
        Incarnation = incarnation;
        Player = player;
        Appearance = appearance;
        Path = path;
        Action = action;
        Riding = riding;
        Alive = alive;
        PkState = pkState;
        Party = party;
        Stall = stall;
        RemoveReason = removeReason;
        RepeatedSpawn = repeatedSpawn;
        StatusEffect = statusEffect;
        HitPoints = hitPoints;
        Rename = rename;
        Entity = entity;
        Combat = combat;
        BubbleText = bubbleText; BubbleTheme = bubbleTheme; EmoteFace = emoteFace;
        Invisible = invisible;
    }

    public bool NativeRegistryHandled { get; }
    public object ReceivedIdentity { get; }
    public WorldDuelHudState DuelHud { get; }
    public long Sequence { get; }
    public WorldRemoteEventKind Kind { get; }
    public int EntityId { get; }
    public long Incarnation { get; }
    public WorldRemotePlayerState Player { get; }
    public WorldPlayerAppearanceState Appearance { get; }
    public WorldRemotePathState Path { get; }
    public WorldRemoteActionState Action { get; }
    public WorldRemoteRidingState Riding { get; }
    public bool? Alive { get; }
    public byte? PkState { get; }
    public WorldPartyState Party { get; }
    // A null Stall payload removes EntityId from the stall-owner set.
    public WorldStallOwnerState Stall { get; }
    public byte RemoveReason { get; }
    public bool RepeatedSpawn { get; }
    public WorldEntityStatusEffect StatusEffect { get; }
    public int? HitPoints { get; }
    public WorldPlayerRenameState Rename { get; }
    public WorldEntityState Entity { get; }
    public WorldCombatResultState Combat { get; }
    public string BubbleText { get; }
    public string BubbleTheme { get; }
    public int? EmoteFace { get; }
    // Only NpcSpawn carries a row; NpcRemove carries the id alone, exactly like 0x8047.
    public WorldNpcState Npc { get; }
    // Only EntityOwner carries the 0x8043 second I32: the loot owner id, stored unconverted.
    public int OwnerId { get; }
    // Only Invis carries the 0x805d U8 already compared with != 0 by the receive arm.
    public bool? Invisible { get; }
}

/// <summary>Receive-state checkpoint, separate from the ordered method invocations.</summary>
public sealed class WorldRemoteSceneState
{
    public WorldRemoteSceneState(
        IReadOnlyDictionary<int, WorldRemotePlayerState> players,
        IReadOnlyDictionary<int, long> incarnations,
        IReadOnlyDictionary<int, WorldRemotePathState> paths,
        IReadOnlyDictionary<int, WorldRemoteActionState> actions,
        WorldPartyState party,
        IReadOnlyDictionary<int, WorldStallOwnerState> stalls,
        WorldPlayerRenameState localRename = null,
        IReadOnlyDictionary<int, WorldEntityState> entities = null,
        IReadOnlyDictionary<int, long> entityIncarnations = null,
        IReadOnlyDictionary<int, WorldEntityState> traps = null,
        IReadOnlyDictionary<int, long> trapIncarnations = null)
    {
        Players = Copy(players);
        Incarnations = Copy(incarnations);
        Paths = Copy(paths);
        Actions = Copy(actions);
        Party = party ?? WorldPartyState.Empty;
        Stalls = Copy(stalls);
        LocalRename = localRename;
        Entities = Copy(entities);
        EntityIncarnations = Copy(entityIncarnations);
        Traps = Copy(traps);
        TrapIncarnations = Copy(trapIncarnations);
    }

    public IReadOnlyDictionary<int, WorldRemotePlayerState> Players { get; }
    public IReadOnlyDictionary<int, long> Incarnations { get; }
    public IReadOnlyDictionary<int, WorldRemotePathState> Paths { get; }
    public IReadOnlyDictionary<int, WorldRemoteActionState> Actions { get; }
    public WorldPartyState Party { get; }
    public IReadOnlyDictionary<int, WorldStallOwnerState> Stalls { get; }
    public WorldPlayerRenameState LocalRename { get; }
    public IReadOnlyDictionary<int, WorldEntityState> Entities { get; }
    public IReadOnlyDictionary<int, long> EntityIncarnations { get; }
    public IReadOnlyDictionary<int, WorldEntityState> Traps { get; }
    public IReadOnlyDictionary<int, long> TrapIncarnations { get; }

    private static IReadOnlyDictionary<int, T> Copy<T>(IReadOnlyDictionary<int, T> source)
    {
        Dictionary<int, T> result = new Dictionary<int, T>();
        if (source != null)
            foreach (KeyValuePair<int, T> pair in source) result.Add(pair.Key, pair.Value);
        return new ReadOnlyDictionary<int, T>(result);
    }
}

/// <summary>
/// One immutable capture: acknowledged baseline, every later unacknowledged operation, and the
/// receive snapshot through LastSequence. Acknowledgment never consumes events received later.
/// </summary>
public sealed class WorldRemoteEventWindow
{
    private readonly Guid sourceId;

    public WorldRemoteEventWindow(Guid sourceId, long generation, int mapId,
        long baseSequence, WorldRemoteSceneState baseline,
        IReadOnlyList<WorldRemoteEvent> events, long lastSequence,
        WorldRemoteSceneState snapshot, bool includesEntityEvents = false, bool includesCombatEvents = false, bool includesChatEvents = false,
        bool includesNpcEvents = false, bool nativeRegistryAtReceipt = false)
    {
        this.sourceId = sourceId;
        IncludesEntityEvents = includesEntityEvents;
        IncludesCombatEvents = includesCombatEvents;
        IncludesChatEvents = includesChatEvents;
        IncludesNpcEvents = includesNpcEvents;
        NativeRegistryAtReceipt = nativeRegistryAtReceipt;
        Generation = generation;
        MapId = mapId;
        BaseSequence = baseSequence;
        Baseline = baseline ?? throw new ArgumentNullException(nameof(baseline));
        if (events == null) throw new ArgumentNullException(nameof(events));
        List<WorldRemoteEvent> copy = new List<WorldRemoteEvent>(events.Count);
        foreach (WorldRemoteEvent edge in events) copy.Add(edge);
        Events = new ReadOnlyCollection<WorldRemoteEvent>(copy);
        LastSequence = lastSequence;
        Snapshot = snapshot ?? throw new ArgumentNullException(nameof(snapshot));
    }

    public long Generation { get; }
    public int MapId { get; }
    public long BaseSequence { get; }
    public WorldRemoteSceneState Baseline { get; }
    public IReadOnlyList<WorldRemoteEvent> Events { get; }
    public long LastSequence { get; }
    public WorldRemoteSceneState Snapshot { get; }
    public bool IncludesEntityEvents { get; }
    public bool IncludesCombatEvents { get; }
    public bool IncludesChatEvents { get; }
    /// <summary>
    /// True once Protocol publishes one NpcSpawn/NpcRemove edge per received 0x8046/0x8047 packet
    /// in this scene generation. While it holds, the NPC state dictionary only carries state and
    /// the ordered edges own per-id construction, rebuild and removal. It is false again after a
    /// scene reset, so a rebuilt window can still adopt the current dictionary once.
    /// </summary>
    public bool IncludesNpcEvents { get; }
    public bool NativeRegistryAtReceipt { get; }
    public bool IsFrom(Guid id) => sourceId == id;
}

}
