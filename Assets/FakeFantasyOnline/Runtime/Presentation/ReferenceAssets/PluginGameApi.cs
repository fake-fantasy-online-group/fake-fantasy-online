using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.PluginApi;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
/// <summary>
/// Main-thread, transport-neutral view of the native game. Observations never acknowledge
/// gameplay queues. Each consumer keeps its own event cursor; all returned values are copies.
/// </summary>
public sealed partial class PluginGameApi : IGameApi, IGameObservationCatalog, IDisposable
{
    private readonly string apiInstanceId = Guid.NewGuid().ToString("N");
    private const int EventCapacity = 1024;
    private const int MaximumRows = 512;
    private const int MaximumCachedQueries = 64;
    private readonly Func<ClientSessionCoordinator> session;
    private readonly int ownerThread;
    private readonly Queue<GameEvent> events = new Queue<GameEvent>();
    private readonly Dictionary<string, string> observationTokens = new Dictionary<string, string>(StringComparer.Ordinal);
    private readonly Dictionary<string, long> observationRevisions = new Dictionary<string, long>(StringComparer.Ordinal);
    private readonly Dictionary<string, GameSnapshot> observationCache = new Dictionary<string, GameSnapshot>(StringComparer.Ordinal);
    private ClientSessionCoordinator lastSession;
    private ReferenceFirstWorldInitialFrame lastFrame;
    private long lastNativeSceneGeneration = long.MinValue;
    private int lastMapId = int.MinValue;
    private bool lastConnected;
    private long sequence;
    private long revision;
    private long sceneRevision;
    private DialogUI observedDialogue;
    private readonly List<ReferenceNpcDialogOption> observedChoices = new List<ReferenceNpcDialogOption>();
    private string observedGreeting;
    private string observedNpcName;
    private int observedNpcId;
    private int observedChoiceCount;
    private bool observedDialogueVisible;
    private long dialogueRevision;
    private bool disposed;

    public PluginGameApi(Func<ClientSessionCoordinator> session)
    {
        this.session = session ?? throw new ArgumentNullException(nameof(session));
        ownerThread = Thread.CurrentThread.ManagedThreadId;
    }

    public void Dispose()
    {
        if (disposed) return;
        RequireMainThread();
        DisposeActions();
        observationCache.Clear();
        observationTokens.Clear();
        observationRevisions.Clear();
        events.Clear();
        observedChoices.Clear();
        observedDialogue = null;
        lastFrame = null;
        lastSession = null;
        disposed = true;
    }

    private ClientSessionCoordinator Session => session();
    private ReferenceFirstWorldInitialFrame ObservationFrame => GameBootstrap.I?.Window?.Frame;

    public void Tick()
    {
        RequireMainThread();
        RefreshScene();
        RefreshObservationRevisions();
        TickActions();
    }

    private void RequireMainThread()
    {
        if (disposed) throw new ObjectDisposedException(nameof(PluginGameApi));
        if (Thread.CurrentThread.ManagedThreadId != ownerThread)
            throw new InvalidOperationException("Game API access must be dispatched to its owning Unity thread.");
    }

    private void RefreshScene()
    {
        RequireMainThread();
        ClientSessionCoordinator current = Session;
        ReferenceFirstWorldInitialFrame frame = ObservationFrame;
        long generation = current?.WorldEntitySceneSnapshot?.SceneGeneration ?? 0;
        int mapId = frame == null ? 0 : GameBootstrap.CurMapId;
        bool connected = current != null && current.Connected;
        if (ReferenceEquals(current, lastSession) && ReferenceEquals(frame, lastFrame) &&
            generation == lastNativeSceneGeneration && mapId == lastMapId && connected == lastConnected) return;
        lastSession = current;
        lastFrame = frame;
        lastNativeSceneGeneration = generation;
        lastMapId = mapId;
        lastConnected = connected;
        sceneRevision++;
        revision++;
        observationCache.Clear();
        observationTokens.Clear();
        observationRevisions.Clear();
        observedDialogue = null;
        observedChoices.Clear();
        dialogueRevision++;
        Emit("scene.changed", Values("mapId", mapId, "sceneRevision", sceneRevision,
            "connected", connected, "nativeSceneGeneration", generation));
    }

    private void Emit(string name, IReadOnlyDictionary<string, object> data)
    {
        RequireMainThread();
        events.Enqueue(new GameEvent(++sequence, sceneRevision, name, data));
        while (events.Count > EventCapacity) events.Dequeue();
    }

    public GameEventBatch ReadEvents(long afterSequence, int maxCount)
    {
        RequireMainThread();
        RefreshScene();
        RefreshObservationRevisions();
        if (afterSequence < 0) throw new ArgumentOutOfRangeException(nameof(afterSequence));
        int count = Math.Max(1, Math.Min(MaximumRows, maxCount));
        long oldest = events.Count == 0 ? sequence + 1 : events.Peek().Sequence;
        bool resync = afterSequence < oldest - 1 || afterSequence > sequence;
        long cursor = afterSequence > sequence ? 0 : afterSequence;
        var result = new List<GameEvent>(count);
        foreach (GameEvent item in events)
        {
            if (item.Sequence <= cursor) continue;
            if (result.Count == count) break;
            result.Add(item);
            if (item.Name == "scene.changed") resync = true;
        }
        return new GameEventBatch(result, result.Count == 0 ? sequence : result[result.Count - 1].Sequence, resync);
    }

    private void RefreshObservationRevisions()
    {
        ClientSessionCoordinator s = Session;
        ReferenceFirstWorldInitialFrame frame = ObservationFrame;
        var endgame = s?.EndgameClientForPlugins;
        string endgameToken = Token(endgame?.EndgameInWorld, endgame?.Endgame.Generation, endgame?.Endgame.Version);
        TrackObservation("endgame", endgameToken);
        TrackObservation("endgame.items", endgameToken);
        TrackObservation("endgame.catalog", endgameToken);
        TrackObservation("endgame.rift", endgameToken);
        TrackObservation("world", Token(s?.State, s?.Connected, GameBootstrap.CurMapId, frame?.Built));
        TrackObservation("dialogue", CurrentDialogueRevision().ToString(CultureInfo.InvariantCulture));
        // Static receipt roots use the native versions. No collection is walked or copied here.
        TrackObservation("inventory", Token(s?.InventoryStateVersion));
        TrackObservation("equipment", Token(s?.InventoryStateVersion));
        TrackObservation("npcs", Token(s?.NpcsVersion));
        TrackObservation("loot", Token(s?.GroundItemsVersion));
        TrackObservation("entities", Token(s?.WorldEntitiesVersion, s?.RemotePlayersVersion,
            s?.RemotePathsVersion, s?.MonsterPathsVersion));
        TrackObservation("buffs", Token(s?.BuffStateVersion));
        TrackObservation("party", PartyObservationToken(s?.PartyStateVersion, frame?.Player?.EntityId,
            CharData.charId, ObservationWorldReady(s)));
        TrackObservation("quests", Token(s?.TaskLogStateVersion, s?.NpcTaskOffersStateVersion));
        var player = frame?.Player;
        TrackObservation("player", Token(s?.PlayerStatusStateVersion, s?.LocalPlayerLifeStateVersion,
            s?.PlayerAppearanceStateVersion, s?.CharacterTitleStateVersion, player?.WorldX, player?.WorldY,
            player?.MovementActive, player?.ActionActive, player?.SourceControllerForIndependentRuntime?.InCombat));
        var selection = frame?.TargetSelection;
        TrackObservation("target", Token(s?.TargetStatusStateVersion, selection?.SelectedEntityId,
            s?.WorldEntitiesVersion, s?.RemotePlayersVersion, s?.PlayerStatusStateVersion,
            selection?.CurrentMonster?.WorldX, selection?.CurrentMonster?.WorldY,
            selection?.Current?.WorldX, selection?.Current?.WorldY,
            selection?.CurrentSelf?.worldX, selection?.CurrentSelf?.worldY));
        // Cooldown seconds and visible UI geometry are sampled only when requested, once per frame.
        TrackObservation("skills", Token(s?.SkillBookStateVersion, s?.SkillCastStateVersion,
            GameHud.SkillCooldownCountForIndependentRuntime > 0 ? ObservationFrameNumber : 0));
    }

    private void TrackObservation(string domain, string token)
    {
        if (observationTokens.TryGetValue(domain, out string old) && old == token) return;
        observationTokens[domain] = token;
        observationRevisions[domain] = ++revision;
        Emit("observation.changed", Values("domain", domain, "revision", revision));
    }

    private long CurrentDialogueRevision()
    {
        RequireMainThread();
        DialogUI dialog = DialogUI.I;
        IReadOnlyList<ReferenceNpcDialogOption> choices = dialog == null ? null : dialog.Options;
        int count = choices?.Count ?? 0;
        bool changed = !ReferenceEquals(dialog, observedDialogue) ||
            observedDialogueVisible != (dialog != null && dialog.Visible) ||
            observedNpcId != (dialog?.CurrentNpcEntityId ?? 0) || observedNpcName != dialog?.NpcName ||
            observedGreeting != dialog?.Greeting || observedChoiceCount != count ||
            observedChoices.Count != Math.Min(MaximumRows, count);
        for (int i = 0; !changed && i < observedChoices.Count; i++)
            changed = !ReferenceEquals(observedChoices[i], choices[i]);
        if (!changed) return dialogueRevision;
        observedDialogue = dialog;
        observedDialogueVisible = dialog != null && dialog.Visible;
        observedNpcId = dialog?.CurrentNpcEntityId ?? 0;
        observedChoiceCount = count;
        observedNpcName = dialog?.NpcName;
        observedGreeting = dialog?.Greeting;
        observedChoices.Clear();
        for (int i = 0; i < Math.Min(MaximumRows, count); i++) observedChoices.Add(choices[i]);
        return ++dialogueRevision;
    }

    private string EntityReference(string kind, int id) => kind + ":" + sceneRevision.ToString(CultureInfo.InvariantCulture) + ":" + id.ToString(CultureInfo.InvariantCulture);
    private static string PartyObservationToken(long? nativeVersion, int? selfEntityId, long selfCharacterId, bool worldReady) =>
        Token(nativeVersion, selfEntityId, selfCharacterId, worldReady);
    private static int ObservationFrameNumber
    {
        // Keep Unity's internal call out of the offline observer's JIT body. This accessor
        // runs only for a requested UI sample or live cooldown, never for headless invariants.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        get { return Time.frameCount; }
    }

    private static string Token(params object[] values)
    {
        var text = new System.Text.StringBuilder();
        foreach (object value in values)
            text.Append(value is float ? ((float)value).ToString("R", CultureInfo.InvariantCulture) :
                value is double ? ((double)value).ToString("R", CultureInfo.InvariantCulture) :
                Convert.ToString(value, CultureInfo.InvariantCulture)).Append('|');
        return text.ToString();
    }
    private static Dictionary<string, object> Values(params object[] values)
    {
        var result = new Dictionary<string, object>(StringComparer.Ordinal);
        for (int i = 0; i < values.Length; i += 2) result.Add((string)values[i], values[i + 1]);
        return result;
    }
}
}
