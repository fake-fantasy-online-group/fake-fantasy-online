using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using UnityEngine;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{

/// <summary>
/// Official-code-first recovery of the reference GameBootstrap map-transition owner.
/// </summary>
/// <remarks>
/// The public/private game-state fields and SwitchMapAuthoritative/SwitchMap/TransitionTo/Fade
/// control flow retain the official owner. The ConfigureWorld/Window members are isolated
/// independent-runtime adapters for the source client's Application boundary and the already
/// recovered BuildWorld graph. Independent-runtime coroutine serialization keeps a map
/// transition owned through its final fade while preserving the native entity-receive gate.
/// </remarks>
public partial class GameBootstrap : MonoBehaviour
{
    private const float MaxFadeStep = 0.05f;

    // Keep initialized original fields together: partial-file order must not reorder
    // native allocation/publication or which later writes occur after a failure.
    // Instance sequence: official 1.5.8 .ctor 060003f4.
    private readonly List<GameObject> redBridgeGOs = new List<GameObject>();
    private readonly List<int> redBridgeIds = new List<int>();
    private int _lastReportX = int.MinValue;
    private int _lastReportY = int.MinValue;
    private readonly List<Occ> occluders = new List<Occ>();
    private readonly List<SpriteRenderer> npcSRs = new List<SpriteRenderer>();
    private readonly List<FadeObj> fadeRegs = new List<FadeObj>();
    public float occFadeAlpha = 0.4f;
    public float occBodyUp = 1.2f;
    public float fadeInSec = 0.35f;
    public float fadeOutSec = 0.45f;
    public float nameHoldSec = 1.6f;
    public float nameFadeSec = .7f;
    private readonly Dictionary<int, Sprite> objCache = new Dictionary<int, Sprite>();
    private readonly ArtBudget<int> _mapObjBudget = new ArtBudget<int>("\u5730\u56fe\u7269\u4ef6", 469762048L, 900f);
    private readonly HashSet<int> _mapObjNoted = new HashSet<int>();
    private readonly Dictionary<int, List<(Sprite, int, int)>> sliceCache =
        new Dictionary<int, List<(Sprite, int, int)>>();
    private readonly Dictionary<int, (Sprite, Sprite, int, int, int)> treeCache =
        new Dictionary<int, (Sprite, Sprite, int, int, int)>();
    public string mapName = "qw0007";
    public float pixelsPerUnit = ReferenceWorldCameraFollow.DefaultPixelsPerUnit;
    public float spawnX = 9211f;
    public float spawnY = 5371f;
    public bool testAccount;
    public string testName = "测试侠客";
    public int testGender = 1;
    public int testRace;
    public int testHead = 1;
    public int testHair = 1;
    public int testLevel = 1;

    // Static sequence: official 1.5.8 .cctor 060003f5.
    private static float _srScanAt = -99f;
    private static readonly Dictionary<int, (int, int, int, int)> _opaqueBoundsCache =
        new Dictionary<int, (int, int, int, int)>();
    public static bool EnableOccFade = false;
    public static bool DebugSeam = false;
    private static int _seamDumpCount = 0;
    private static readonly Dictionary<int, NpcEntry> _netNpcs = new Dictionary<int, NpcEntry>();
    private static readonly Dictionary<int, GameObject> _netNpcGo = new Dictionary<int, GameObject>();
    private static readonly List<MapLink.GatherZone> _netGatherZones =
        new List<MapLink.GatherZone>();
    private static Color _pendingSky = Color.black;
    private static readonly Dictionary<Texture2D, (float, float)> _scanOpaqueCache =
        new Dictionary<Texture2D, (float, float)>();
    public static string CurMapName = string.Empty;
    public static float CurPpu = 100f;
    public static readonly List<Vector2> NpcPts = new List<Vector2>();
    public static bool UseIsoDepth = false;
    public static bool UseIsoY = false;

    private float fadeAlpha;
    private Texture2D fadeTex;
    private string _prevMapName;

    public static Action<int, float, float> DoTeleport;

    private (int, float, float)? _queuedSwitch;

    private Transform worldRoot;
    private ReferenceWorldLocalPlayerDoll pcRef;

    public static float CurWorldW;
    public static float CurWorldH;
    public static int CurMapId;
    public static Transform PlayerT;
    public static GameBootstrap I;

    private bool switching;
    private int _pendingMapId;

    // Independent-runtime adapter state. No entry below this line decides official map behavior.
    private ClientSessionCoordinator session;
    private Transform worldParent;
    private WorldEntryRecord entry;
    private MusicManager sharedMusic;
    private ReferenceWorldCombatSound sharedCombatSound;
    private System.Collections.Generic.ISet<string> processNpcNames;
    private ReferenceFirstWorldWindowOwner window;
    private ReferenceFirstWorldWindowOwner constructingWindow;
    private ReferenceFirstWorldGossipDialog persistentGossip;
    private ReferenceFirstWorldDealDialog persistentDeal;
    private ReferenceFirstWorldDuelDialog persistentDuel;
    private CancellationTokenSource positionReportLifetime;
    private int viewportWidth;
    private int viewportHeight;
    private float dpiScale;
    private long windowBuildGeneration;
    private bool configured;
    private bool nativeStartupWorld;
    private Action titleStartupForIndependentRuntime;
    // The native switching flag also gates incoming entity receipts and opens as soon
    // as BuildWorld starts. Keep coroutine ownership separate: fades still yield after
    // that point, so neither a portal nor an authoritative reply may start a second one.
    private bool mapTransitionInProgress;
    private (int, float, float, PlayerController)? queuedPortalSwitch;

    public bool Switching => switching;

    internal bool HasWorldStateForIndependentRuntime =>
        worldRoot != null || window != null || constructingWindow != null;
    internal bool HasNativeStartupWorldForIndependentRuntime =>
        nativeStartupWorld && configured && HasWorldStateForIndependentRuntime;

    public ReferenceFirstWorldWindowOwner Window => window;
    internal ReferenceFirstWorldGossipDialog PersistentGossipForIndependentRuntime => persistentGossip;
    internal ReferenceFirstWorldDealDialog PersistentDealForIndependentRuntime => persistentDeal;

    internal ReferenceFirstWorldDuelDialog AcquireDuelForIndependentRuntime(ClientSessionCoordinator valueSession)
    {
        if (persistentDuel == null)
            persistentDuel = new ReferenceFirstWorldDuelDialog(valueSession.DuelCommands, valueSession.DuelStateSource);
        persistentDuel.RebindWindow();
        return persistentDuel;
    }

    public WorldEntryRecord CurrentEntry => entry;
    internal void ProjectPlayerHairForIndependentRuntime(int head, int hair)
    {
        if (entry != null) entry = entry.WithHair(head, hair);
        if (Window != null) Window.ProjectPlayerHairForIndependentRuntime(head, hair);
    }

    public ReferenceFirstWorldWindowFailure LastBuildFailure { get; private set; }

    internal void BindTitleStartupForIndependentRuntime(Action destination)
    {
        titleStartupForIndependentRuntime = destination ?? throw new ArgumentNullException(nameof(destination));
    }

    internal void BindNativeWorldHostForIndependentRuntime(
        ClientSessionCoordinator hostSession, Transform hostParent,
        ReferenceWorldCombatSound hostCombatSound, ISet<string> hostNpcNames)
    {
        // Supplies the existing renderer's host capabilities, not a server/world admission.
        session = hostSession ?? throw new ArgumentNullException(nameof(hostSession));
        worldParent = hostParent == null
            ? throw new ArgumentNullException(nameof(hostParent)) : hostParent;
        sharedCombatSound = hostCombatSound;
        processNpcNames = hostNpcNames;
    }

    private void Start()
    {
        // Official 1.5.8 GameBootstrap.Start owns this application-lifecycle decision.
        // The surrounding independent-runtime adapters do not narrow it to QA arguments.
        I = this;
        UnityEngine.Application.runInBackground = true;
        GameSettings.ApplyFrameRate();
        // The active Game scene component owns the initial supplier order in 060003ef:
        // ResLoader, MusicManager, CursorManager, chat binding, then the title branch.
        // Keep the existing package-layout adapter as the sole ResLoader supplier.
        ReferenceResourceLoader resources = null;
        if (ResLoader.I == null)
            resources = ReferenceResourceLoader.OpenStreamingAssets();
        if (MusicManager.I == null)
            new GameObject("MusicManager").AddComponent<MusicManager>();
        if (CursorManager.I == null)
            CursorManager.EnsureForIndependentRuntime(resources?.RootDirectory);
        BindNativeChatSendForIndependentRuntime();
        if (testAccount)
        {
            CharData.name = string.IsNullOrEmpty(testName) ? "测试侠客" : testName;
            CharData.gender = testGender > 0 ? 1 : 0;
            CharData.race = Mathf.Clamp(testRace, 0, 5);
            CharData.head = Mathf.Clamp(testHead, 1, 8);
            CharData.hair = Mathf.Clamp(testHair, 1, 2);
            CharData.level = Mathf.Max(1, testLevel);
            CharData.lastLogin = "测试账号";
            // The native branch builds directly, without title/login/network admission.
            PrepareNativeStartupWorldForIndependentRuntime();
            BuildWorld();
            return;
        }

        // The composition root binds the existing title host; Start supplies its original
        // activation edge. The serialized GameBootstrap has testAccount=false.
        titleStartupForIndependentRuntime?.Invoke();
    }

    private void PrepareNativeStartupWorldForIndependentRuntime()
    {
        // Standalone callers still need an actual host. The product binds it before Start.
        if (session == null || worldParent == null) return;
        sharedMusic = MusicManager.I;
        viewportWidth = Screen.width;
        viewportHeight = Screen.height;
        dpiScale = ReferenceWorldCameraFollow.DefaultDpiScale;
        positionReportLifetime = new CancellationTokenSource();
        nativeStartupWorld = true;
        configured = true;
    }

    private WorldEntryRecord CaptureNativeWorldEntryForIndependentRuntime()
    {
        // This immutable carrier reads the same original owners as the renderer. It is
        // never published as a packet, selected roster entry, or authenticated session.
        return new WorldEntryRecord(
            NetClient.SelfEntityId, CharData.name, CharData.name, CharData.title,
            CharData.race, CharData.gender, CharData.head, CharData.hair,
            _pendingMapId != 0 ? _pendingMapId : CurMapId, spawnX, spawnY,
            CharData.attrs,
            new CharacterAppearanceRecord(CharData.apBody, CharData.apCap,
                CharData.apBackpack, CharData.apWeaponR, CharData.apWeaponL,
                CharData.apFace, CharData.atkVariant, CharData.weaponCType, CharData.atkDist),
            new WorldPetRecord(CharData.petModel, CharData.petName, CharData.petLevel),
            CharData.charId, CharData.glowMode, GameSettings.SmartCast,
            CharData.glowUnlock, GameSettings.PetViewMask, CharData.level);
    }

    public bool ConfigureWorld(
        ClientSessionCoordinator valueSession,
        Transform valueParent,
        WorldEntryRecord valueEntry,
        MusicManager valueMusic,
        ReferenceWorldCombatSound valueCombatSound,
        System.Collections.Generic.ISet<string> valueProcessNpcNames,
        int valueViewportWidth,
        int valueViewportHeight,
        float valueDpiScale)
    {
        if (valueSession == null || valueParent == null || valueEntry == null ||
            !(valueDpiScale > 0f) || float.IsNaN(valueDpiScale) ||
            float.IsInfinity(valueDpiScale))
        {
            return false;
        }

        // First admission preserves each independently receipt-created owner.
        // Pump may have created these owners before any world exists.
        ReleaseWorld(preserveReceiptOverlay: !HasWorldStateForIndependentRuntime &&
            ChatCopyOverlay.I != null && ChatCopyOverlay.I.enabled,
            // Gossip/SystemInfo are independently receipt-created DDOL owners. Their first
            // world admission must not destroy the data just received before window creation.
            preserveReceiptSocialNotices: !HasWorldStateForIndependentRuntime &&
                (GossipDlg.I != null || SystemInfoBar.I != null),
            preserveReceiptMail: !HasWorldStateForIndependentRuntime,
            preserveReceiptTrade: !HasWorldStateForIndependentRuntime && DealDlg.I != null,
            preserveReceiptFamilyWindows: !HasWorldStateForIndependentRuntime,
            retireNativeWorld: HasWorldStateForIndependentRuntime);
        // A new entry replaces the prior host binding; world teardown alone does not.
        ReleaseWorldContextForIndependentRuntime();
        session = valueSession;
        AcquireDuelForIndependentRuntime(valueSession);
        worldParent = valueParent;
        entry = valueEntry;
        CharData.name = valueEntry.DisplayName;
        sharedMusic = valueMusic;
        sharedCombatSound = valueCombatSound;
        processNpcNames = valueProcessNpcNames;
        viewportWidth = valueViewportWidth;
        viewportHeight = valueViewportHeight;
        dpiScale = valueDpiScale;
        positionReportLifetime = new CancellationTokenSource();

        // DoEnterWorld has already written map/spawn on its captured boot before retiring
        // the title. Carry its nonnegative logical ID through the host preparation above;
        // nonpositive entry IDs must retain that boot's existing map name and coordinates.
        _pendingMapId = Math.Max(0, valueEntry.MapId);
        configured = true;
        BindNativeChatSendForIndependentRuntime();
        I = this;
        return true;
    }

    internal sealed class BuildWorldCheckpoints
    {
        internal readonly System.Diagnostics.Stopwatch Clock;
        internal long GsmParsedMs;
        internal long FloorMs;
        internal long LocalPlayerMs;
        internal long ObjectsMs;

        internal BuildWorldCheckpoints(System.Diagnostics.Stopwatch clock)
        {
            Clock = clock;
        }
    }

    // The transition already reads the target GSM to preserve the old world on a
    // missing/corrupt resource. Hand that exact buffer to its one construction read
    // instead of allocating/reading/inflating the same map a second time. This is
    // not a map cache: a different loader/path, rollback or later build reads fresh.
    internal sealed class PreparedMapSource
    {
        private readonly ResLoader loader;
        private readonly string path;
        private byte[] bytes;

        internal PreparedMapSource(ResLoader loader, string path)
        {
            this.loader = loader;
            this.path = path;
            bytes = loader.ReadMap(path);
        }

        internal bool HasBytes => bytes != null;

        internal byte[] ReadOnce(ResLoader constructionLoader, string constructionPath)
        {
            byte[] prepared = bytes;
            bytes = null;
            return prepared != null && ReferenceEquals(loader, constructionLoader) &&
                string.Equals(path, constructionPath, StringComparison.Ordinal)
                ? prepared : constructionLoader.ReadMap(constructionPath);
        }

        internal void Clear() => bytes = null;
    }

    /// <summary>
    /// Calls the recovered BuildWorld graph through the independent-runtime window adapter.
    /// The official method is void; a rejected graph is exposed through pcRef/window and the
    /// diagnostic LastBuildFailure property instead of substituting a world.
    /// </summary>
    public void BuildWorld() => BuildWorldCore(transitionOwnsFailure: false);

    private void BuildWorldCore(bool transitionOwnsFailure, PreparedMapSource preparedMapSource = null)
    {
        switching = false;
        // Ordinary direct-world map rebuilds also observe current native appearance/state.
        if (nativeStartupWorld) entry = CaptureNativeWorldEntryForIndependentRuntime();
        System.Diagnostics.Stopwatch buildClock = System.Diagnostics.Stopwatch.StartNew();
        BuildWorldCheckpoints checkpoints = new BuildWorldCheckpoints(buildClock);
        ClearNetNpcs();
        LastBuildFailure = ReferenceFirstWorldWindowFailure.None;
        if (!configured || session == null || worldParent == null || entry == null)
        {
            LastBuildFailure = ReferenceFirstWorldWindowFailure.UnexpectedConstructionFailure;
            return;
        }

        // Original BuildWorld provisions DialogUI before its world/UI construction suffix.
        EnsureDialogUIForIndependentRuntime();
        // PlayMap is void: a missing mapping or audio file never rejects construction.
        // The existing persistent singleton owns playback before the map root is created.
        if (MusicManager.I != null) MusicManager.I.PlayMap(mapName);
        npcSRs.Clear();
        fadeRegs.Clear();
        occluders.Clear();
        CombatWorld.ClearAll();
        // Original BuildWorld publishes its root before reading/parsing the map,
        // including initial entry. Window remains the completed host.
        worldRoot = new GameObject("WorldRoot").transform;
        worldRoot.SetParent(worldParent, false);
        // Original get_CurLogicalMapId (060003e9), inlined by BuildWorld: zero alone
        // selects the current logical map, never the entry snapshot or source filename.
        int mapId = _pendingMapId != 0 ? _pendingMapId : CurMapId;
        WorldEntryRecord projected = entry.WithMapPosition(mapId, spawnX, spawnY);
        CurMapId = mapId;
        ReferenceFirstWorldWindowOwner candidate =
            new ReferenceFirstWorldWindowOwner(
                session,
                worldParent,
                projected,
                sharedMusic,
                sharedCombatSound,
                processNpcNames,
                this,
                persistentGossip, persistentDeal);
        constructingWindow = candidate;
        candidate.AdoptConstructionRootForIndependentRuntime(worldRoot, transitionOwnsFailure);
        candidate.BuildCheckpointsForIndependentRuntime = checkpoints;
        candidate.PreparedMapSourceForIndependentRuntime = preparedMapSource;
        if (!candidate.BuildForViewport(
                viewportWidth,
                viewportHeight,
                dpiScale))
        {
            LastBuildFailure = candidate.LastFailure;
            // WindowOwner already tried the native Texture2D fallback once. If both
            // sources are absent, retain the published root/private candidate for the
            // native normal return and existing later release, without a built Window.
            if (LastBuildFailure == ReferenceFirstWorldWindowFailure.MissingSourceAsset)
                return;
            candidate.Dispose();
            constructingWindow = null;
            worldRoot = null;
            return;
        }

        windowBuildGeneration++;
        candidate.SetBuildGenerationForIndependentRuntime(windowBuildGeneration);
        window = candidate;
        if (ReferenceEquals(constructingWindow, candidate)) constructingWindow = null;
        if (persistentGossip == null) persistentGossip = candidate.GossipDialog;
        if (persistentDeal == null) persistentDeal = candidate.DealDialog;
        entry = projected;
        worldRoot = candidate.WorldRoot;

        // The real world graph supplies the original parent and map dimensions. Normal
        // earlier BuildWorld returns do not reach this publication/replay suffix.
        PublishNetNpcSceneForIndependentRuntime(candidate.Frame.MapWidthForIndependentRuntime,
            candidate.Frame.MapHeightForIndependentRuntime, pixelsPerUnit);
        ApplyNetGatherZones();
        ComputeOccFloors();
        if (LoadTrace.On)
        {
            LoadTrace.Phase("建图·gsm解析", (float)checkpoints.GsmParsedMs);
            LoadTrace.Phase("建图·地板", (float)(checkpoints.FloorMs - checkpoints.GsmParsedMs));
            LoadTrace.Phase("建图·本地玩家", (float)(checkpoints.LocalPlayerMs - checkpoints.FloorMs));
            LoadTrace.Phase("建图·地图物件", (float)(checkpoints.ObjectsMs - checkpoints.LocalPlayerMs));
            LoadTrace.Phase("  └物件·LoadObj查表", ObjLoadMs);
            LoadTrace.Phase("  └物件·GetSlices切片", ObjSliceMs);
            LoadTrace.Phase("  └物件·建GameObject", ObjSpawnMs);
            LoadTrace.Phase("  └物件·淡化登记", ObjFadeMs);
            LoadTrace.Line(string.Format("物件件数: 总{0} 切片式{1}片 动画{2} 单精灵{3}",
                ObjN, ObjSliceN, ObjAnimN, ObjPlainN));
            LoadTrace.Phase("建图·NPC与收尾", (float)(buildClock.ElapsedMilliseconds - checkpoints.ObjectsMs));
            LoadTrace.Phase("建图 合计", (float)buildClock.ElapsedMilliseconds);
            LoadTrace.Line(string.Format("建图完成 {0} 托管内存{1}MB", mapName,
                GC.GetTotalMemory(false) / 1048576L));
        }

        // The official BuildWorld stores this same compiler-generated callback in
        // GameBootstrap.DoTeleport and NetClient.OnTeleport. The independent socket adapter
        // forwards its immutable edge through DoTeleport on Unity's main thread.
        DoTeleport = (mid, x, y) => SwitchMapAuthoritative(mid, x, y);
        candidate.Frame?.ObserveNativeNpcsForIndependentRuntime();
    }

    // The real controller is available before HUD/mask/transfer construction. Keep the
    // existing private borrower in that same publication slot even if a later call throws.
    internal void PublishConstructionPlayerForIndependentRuntime(ReferenceWorldLocalPlayerDoll player)
    {
        pcRef = player;
    }

    public void SwitchMapAuthoritative(int mapId, float toX, float toY)
    {
        if (mapTransitionInProgress)
        {
            _queuedSwitch = (mapId, toX, toY);
        }
        else
        {
            SwitchMap(mapId, toX, toY);
        }
    }

    public void SwitchMap(int mapId, float toX, float toY)
    {
        if (mapTransitionInProgress)
        {
            // The newly built player can reach another gate during fade-out. Its native
            // transfer callback has already latched, so retain that request instead of
            // stranding the player. Old-world callbacks during fade-in are still dropped.
            PlayerController player = pcRef?.SourceControllerForIndependentRuntime;
            if (!switching && window != null && window.Built && player != null)
                queuedPortalSwitch = (mapId, toX, toY, player);
            return;
        }

        PlayerController originalPlayer = pcRef?.SourceControllerForIndependentRuntime;
        mapTransitionInProgress = true;
        switching = true;
        try
        {
            if (ResLoader.I != null)
                MapDef.EnsureLoaded(ResLoader.I.Pkg);
            string targetName = MapSourceName(mapId);
            _prevMapName = mapName;
            _pendingMapId = mapId;
            mapName = targetName;
            spawnX = toX;
            spawnY = toY;
            StartCoroutine(TransitionTo());
        }
        catch
        {
            ResetMapTransitionForIndependentRuntime();
            RearmRejectedPortalForIndependentRuntime(originalPlayer);
            throw;
        }
    }

    private void RearmRejectedPortalForIndependentRuntime(PlayerController rejectedPlayer)
    {
        PlayerController player = pcRef?.SourceControllerForIndependentRuntime;
        if (worldRoot == null || window == null || !window.Built || player == null ||
            !ReferenceEquals(player, rejectedPlayer) || !player.TransferredForIndependentRuntime) return;
        // Resource rejection leaves this player alive. Allow leaving/re-entering the
        // gate to retry, without retrying a missing map on every frame inside the gate.
        player.TransferredForIndependentRuntime = false;
        player.WasInsideTransferForIndependentRuntime = true;
    }

    private IEnumerator TransitionTo()
    {
        bool completed = false;
        IEnumerator transition = TransitionToCore();
        try
        {
            // Drive the body here so exceptions and cancellation dispose this owner,
            // including failures before the first yield or after world construction.
            while (transition.MoveNext()) yield return transition.Current;
            completed = true;
        }
        finally
        {
            (transition as IDisposable)?.Dispose();
            ResetMapTransitionForIndependentRuntime();
            if (!completed)
            {
                _queuedSwitch = null;
                queuedPortalSwitch = null;
            }
        }

        ReplayQueuedMapSwitchForIndependentRuntime();
    }

    private void ResetMapTransitionForIndependentRuntime()
    {
        mapTransitionInProgress = false;
        switching = false;
        fadeAlpha = 0f;
        ApplyPendingSky();
    }

    private void CancelMapTransitionForIndependentRuntime()
    {
        _queuedSwitch = null;
        queuedPortalSwitch = null;
        // This bootstrap owns only the map coroutine (and its nested fades).
        StopAllCoroutines();
        ResetMapTransitionForIndependentRuntime();
    }

    private void ReplayQueuedMapSwitchForIndependentRuntime()
    {
        (int, float, float, PlayerController)? portal = queuedPortalSwitch;
        queuedPortalSwitch = null;
        if (_queuedSwitch.HasValue)
        {
            (int, float, float) queued = _queuedSwitch.Value;
            _queuedSwitch = null;
            PlayerController player = pcRef?.SourceControllerForIndependentRuntime;
            if (worldRoot != null && window != null && window.Built && player != null &&
                queued.Item1 == CurMapId && (int)queued.Item2 == (int)player.worldX &&
                (int)queued.Item3 == (int)player.worldY)
            {
                // This is the already-built destination, not another load. The server may
                // still be waiting for its map-ready acknowledgement; never silently drop it.
                ReportPos(true);
            }
            else
            {
                // A server correction takes priority over a portal in the superseded world.
                Debug.Log(string.Format("[切图] 补执行切图期间到达的服务端强切 → 图{0}({1},{2})",
                    queued.Item1, (int)queued.Item2, (int)queued.Item3));
                SwitchMap(queued.Item1, queued.Item2, queued.Item3);
                return;
            }
        }

        if (portal.HasValue && worldRoot != null && window != null && window.Built &&
            ReferenceEquals(portal.Value.Item4, pcRef?.SourceControllerForIndependentRuntime))
            SwitchMap(portal.Value.Item1, portal.Value.Item2, portal.Value.Item3);
    }

    private IEnumerator TransitionToCore()
    {
        string backName = string.IsNullOrEmpty(_prevMapName)
            ? mapName
            : _prevMapName;
        int backCurMap = CurMapId;
        PlayerController backPlayer = pcRef?.SourceControllerForIndependentRuntime;
        float backX = backPlayer != null ? backPlayer.worldX : spawnX;
        float backY = backPlayer != null ? backPlayer.worldY : spawnY;

        // Include the synchronous preflight, which happens before the first fade
        // frame. Previously this potentially visible stall was outside the trace.
        if (LoadTrace.AutoOnMapSwitch) LoadTrace.Begin(backName + "到" + mapName);
        LoadTrace.Line(string.Format("切图开始 {0}(图{1}) → {2}(图{3}) 落点({4},{5})",
            backName, backCurMap, mapName, _pendingMapId, (int)spawnX, (int)spawnY));
        System.Diagnostics.Stopwatch traceClock = System.Diagnostics.Stopwatch.StartNew();
        long traceMark = 0L;

        PreparedMapSource preparedMapSource = null;
        bool sourceAvailable;
        try
        {
            if (ResLoader.I != null)
                preparedMapSource = new PreparedMapSource(ResLoader.I, "map/" + mapName + ".gsm");
            sourceAvailable = ResLoader.I == null ||
                preparedMapSource.HasBytes ||
                Resources.Load<Texture2D>(mapName) != null;
        }
        catch (Exception exception)
        {
            Debug.LogError("[切图] 读取目标地图失败 " + mapName + ": " + exception);
            sourceAvailable = false;
        }
        long sourceReadMs = traceClock.ElapsedMilliseconds;
        if (LoadTrace.On) LoadTrace.Phase("目标图预读(淡入前)", (float)sourceReadMs);
        traceMark = traceClock.ElapsedMilliseconds;
        if (!sourceAvailable)
        {
            Debug.LogError(string.Format("[\u5207\u56fe] \u2605\u76ee\u6807\u56fe\u8d44\u6e90\u7f3a\u5931, \u53d6\u6d88\u5207\u56fe: {0}(\u56fe{1}) \u2014\u2014 \u670d\u52a1\u7aef\u53ef\u80fd\u5df2\u6309\u65b0\u56fe\u8bb0\u8d26, \u73b0\u56de\u6eda\u56fe\u540d\u5e76\u8865\u6267\u884c\u6392\u961f\u4e2d\u7684\u670d\u52a1\u7aef\u5f3a\u5207", mapName, _pendingMapId));
            mapName = backName;
            _pendingMapId = backCurMap;
            spawnX = backX;
            spawnY = backY;
            switching = false;
            RearmRejectedPortalForIndependentRuntime(backPlayer);
            yield break;
        }

        _holdSkyBlack = true;
        yield return Fade(0f, 1f, fadeInSec);
        long fadeInMs = traceClock.ElapsedMilliseconds - traceMark;
        if (LoadTrace.On) LoadTrace.Phase("淡入黑屏", (float)fadeInMs);
        traceMark = traceClock.ElapsedMilliseconds;

        if (worldRoot != null)
        {
            Destroy(worldRoot.gameObject);
            worldRoot = null;
        }
        GroundItems.ClearAll();
        CombatWorld.ClearAll();
        StallDlg.Owners.Clear();
        ReleaseBuiltWorld(sceneRegistriesReleased: true, preservePlayerTransform: true);
        long releaseMs = traceClock.ElapsedMilliseconds - traceMark;
        if (LoadTrace.On) LoadTrace.Phase("销毁旧图+清怪宠美术", (float)releaseMs);
        traceMark = traceClock.ElapsedMilliseconds;
        yield return null;
        bool switchedMapForTrace = mapName != backName || _pendingMapId != backCurMap;
        int evictedMapObjects = switchedMapForTrace ? EvictMapObjectsAtSafePoint() : 0;
        if (LoadTrace.On)
            LoadTrace.Line(string.Format("物件美术按预算淘汰 {0} 件 → {1}",
                evictedMapObjects, MapObjBudgetStat()));
        _scanOpaqueCache.Clear();
        ArtCaches.EvictAtSafePoint();
        long safePointMs = traceClock.ElapsedMilliseconds - traceMark;
        if (LoadTrace.On)
        {
            LoadTrace.Phase(switchedMapForTrace
                ? "安全点淘汰+扔物件缓存" : "安全点淘汰(同图重进)",
                (float)safePointMs);
        }
        traceMark = traceClock.ElapsedMilliseconds;
        GC.Collect();
        long gcMs = traceClock.ElapsedMilliseconds - traceMark;
        if (LoadTrace.On) LoadTrace.Phase("GC.Collect", (float)gcMs);
        traceMark = traceClock.ElapsedMilliseconds;
        yield return Resources.UnloadUnusedAssets();
        long unloadMs = traceClock.ElapsedMilliseconds - traceMark;
        if (LoadTrace.On) LoadTrace.Phase("UnloadUnusedAssets", (float)unloadMs);
        traceMark = traceClock.ElapsedMilliseconds;

        // A server landing correction received while fading/loading the same target
        // can be used for the first build, avoiding a second load just to change spawn.
        (int, float, float)? appliedLanding = null;
        if (_queuedSwitch.HasValue && _queuedSwitch.Value.Item1 == _pendingMapId)
        {
            appliedLanding = _queuedSwitch;
            spawnX = appliedLanding.Value.Item2;
            spawnY = appliedLanding.Value.Item3;
        }

        bool built = false;
        try
        {
            BuildWorldCore(transitionOwnsFailure: true, preparedMapSource: preparedMapSource);
            built = worldRoot != null && window != null && window.Built;
        }
        catch (Exception exception)
        {
            Debug.LogError("[切图] 建图失败 " + backName + "→" + mapName + ": " + exception);
        }
        finally
        {
            // Also release the preflight buffer when construction returns/throws
            // before its map read, rather than retaining it through fade-out.
            preparedMapSource?.Clear();
            preparedMapSource = null;
        }

        if (built)
        {
            // A failed build must retain this authoritative target for the queued retry
            // after rollback. A newer receipt must also survive this completion edge.
            if (appliedLanding.HasValue && _queuedSwitch == appliedLanding)
                _queuedSwitch = null;
            CurMapId = _pendingMapId;
            ReportPos(true);
        }
        else
        {
            Debug.LogError("[切图] 回滚到 " + backName + "(" + (int)backX + "," +
                (int)backY + ") 救场");
            Transform failedRoot = worldRoot;
            if (failedRoot != null)
            {
                Destroy(failedRoot.gameObject);
                worldRoot = null;
            }
            // Native rollback retires only its failed root here. The next BuildWorld
            // owns the registry prefix; adapter disposal must not run final-world cleanup.
            ReleaseBuiltWorld(sceneRegistriesReleased: true, releasedRoot: failedRoot,
                preservePlayerTransform: true);
            mapName = backName;
            spawnX = backX;
            spawnY = backY;
            _pendingMapId = backCurMap;
            try
            {
                BuildWorldCore(transitionOwnsFailure: true);
                if (worldRoot != null && window != null && window.Built)
                {
                    CurMapId = backCurMap;
                    ReportPos(true);
                }
            }
            catch (Exception exception)
            {
                Debug.LogError("[切图] 回滚也失败(只能重登): " + exception);
            }
            switching = false;
        }

        long buildMs = traceClock.ElapsedMilliseconds - traceMark;
        if (LoadTrace.On) LoadTrace.Phase("BuildWorld(含回滚判定)", (float)buildMs);
        traceMark = traceClock.ElapsedMilliseconds;
        yield return null;
        yield return Fade(1f, 0f, fadeOutSec);
        fadeAlpha = 0f;
        ApplyPendingSky();
        long fadeOutMs = traceClock.ElapsedMilliseconds - traceMark;
        if (LoadTrace.On)
        {
            LoadTrace.Phase("淡出", (float)fadeOutMs);
            LoadTrace.Line("★黑屏结束, 以下都是玩家看得见的卡顿");
        }
        // One summary per completed transition, including without the optional
        // detailed trace. Yielded phases are wall time, not measured CPU stalls.
        Debug.Log(string.Format("[切图耗时] {0}(图{1})→{2}(图{3}) 总计{4}ms " +
            "预读{5} 淡入{6} 清旧{7} 安全点(含等帧){8} GC{9} 卸载(含等待){10} " +
            "建图(含回滚){11} 淡出(含等帧){12}ms 目标建成={13}",
            backName, backCurMap, mapName, CurMapId, traceClock.ElapsedMilliseconds,
            sourceReadMs, fadeInMs, releaseMs, safePointMs, gcMs, unloadMs, buildMs, fadeOutMs, built));
    }

    private IEnumerator Fade(float from, float to, float dur)
    {
        if (dur <= 0f)
        {
            fadeAlpha = to;
            yield break;
        }

        float t = 0f;
        while (t < dur)
        {
            t += Mathf.Min(Time.unscaledDeltaTime, MaxFadeStep);
            fadeAlpha = from + ((to - from) * Mathf.Clamp01(t / dur));
            yield return null;
        }

        fadeAlpha = to;
    }

    private void ReportPos(bool entered = false)
    {
        if (pcRef == null || pcRef.Root == null || session == null || !session.Connected)
        {
            return;
        }

        int mapId = CurMapId;
        if (mapId <= 0 && !int.TryParse(
                mapName.Length > 2 ? mapName.Substring(2) : "0",
                out mapId))
        {
            return;
        }

        SendPositionForIndependentRuntime(
            mapId,
            (int)pcRef.WorldX,
            (int)pcRef.WorldY,
            entered);
    }

    // Official 0x060003bd: Unity application-quit lifecycle callback. Both architectures tail-call
    // ReportPos(this, false), flushing the local player's position with entered=false before exit.
    private void OnApplicationQuit() => ReportPos(false);

    private async void SendPositionForIndependentRuntime(int mapId, int x, int y, bool entered)
    {
        if (positionReportLifetime == null)
        {
            return;
        }

        try
        {
            Task<Result> pending = session.SavePlayerPositionAsync(
                mapId,
                x,
                y,
                entered,
                positionReportLifetime.Token);
            window?.TrackPositionReportForIndependentRuntime(pending);
            await pending;
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
    }

    // Host bindings support the original bootstrap even after its world root is released.
    // Retire them only when a new entry replaces the binding or this actual owner dies.
    private void ReleaseWorldContextForIndependentRuntime()
    {
        // The original NetClient connection outlives world UI; retain its Duel commands
        // until the host binding is replaced or the actual bootstrap is destroyed.
        if (persistentDuel != null) { persistentDuel.Dispose(); persistentDuel = null; }
        if (positionReportLifetime != null)
        {
            positionReportLifetime.Cancel();
            positionReportLifetime.Dispose();
            positionReportLifetime = null;
        }

        configured = false;
        nativeStartupWorld = false;
        windowBuildGeneration = 0;
        session = null;
        worldParent = null;
        entry = null;
        processNpcNames = null;
    }

    public void ReleaseWorld() => ReleaseWorld(preserveReceiptOverlay: false,
        preserveReceiptSocialNotices: false, preserveReceiptMail: false,
        preserveReceiptTrade: false, preserveReceiptFamilyWindows: false,
        retireNativeWorld: true);

    private void ReleaseWorld(bool preserveReceiptOverlay, bool preserveReceiptSocialNotices,
        bool preserveReceiptMail, bool preserveReceiptTrade, bool preserveReceiptFamilyWindows,
        bool retireNativeWorld)
    {
        // Final exit must not leave a yielded transition able to resurrect this world.
        CancelMapTransitionForIndependentRuntime();
        bool hadWorldState = HasWorldStateForIndependentRuntime;
        bool hadTradeOwner = DealDlg.I != null;
        // 060003f3 destroys the root and clears these references before owner cleanup.
        // Retain the released root only for the existing borrower's single-destroy handoff.
        Transform releasedRoot = worldRoot;
        if (releasedRoot != null) Destroy(releasedRoot.gameObject);
        worldRoot = null;
        PlayerT = null;
        pcRef = null;
        if (retireNativeWorld)
        {
            // 060003f3 continues directly from the cleared references through this sequence.
            // Adapter cancellation/disposal cannot run before these native calls and stores.
            CombatWorld.ClearAll();
            GroundItems.ClearAll();
            ClearMapObjectCaches();
            GC.Collect();
            Resources.UnloadUnusedAssets();
            CurMapId = 0;
            StallConfigDlg.ResetLocal();
            StallDlg.Owners.Clear();
            PlayerController.ClearFollow();
            CharData.ClearRidingForWorldExit();
            SocialClient.Party.Clear();
            MailDlg.HasNew = false;
            PurgeWorldRootsForIndependentRuntime();
        }

        // Native root purge owns the shop and fitting-room lifetime. Retire only the
        // map borrower here; first-admission host preparation keeps its existing cleanup.
        if (hadWorldState) ReleaseRackShopForIndependentRuntime(detachOnly: retireNativeWorld);
        // The native registries and roots have already been retired on final exit. Reuse the
        // same detach path as an already-cleared map, without clearing them or charId again.
        ReleaseBuiltWorld(sceneRegistriesReleased: retireNativeWorld, releasedRoot: releasedRoot,
            preservePlayerTransform: retireNativeWorld);
        if (!preserveReceiptMail)
            ReleaseMailForIndependentRuntime(nativeNotificationCleared: retireNativeWorld);
        if (persistentGossip != null)
        {
            persistentGossip.Dispose();
            persistentGossip = null;
        }
        if (persistentDeal != null)
        {
            persistentDeal.Dispose(detachOnly: retireNativeWorld);
            persistentDeal = null;
        }
        if (!retireNativeWorld && !preserveReceiptTrade && hadTradeOwner)
        {
            // Non-native host preparation retains its existing cleanup policy.
            // Native final teardown only queues the root; it does not reset trade statics.
            if (DealDlg.I != null)
            {
                DealDlg.I.ReleaseForIndependentRuntime();
                SocialClient.ResetTrade();
            }
        }
        if (!preserveReceiptSocialNotices)
        {
            // Native root purge already requested GossipDlg destruction. Its fields and
            // automatic callbacks remain Unity-owned until that destruction takes effect.
            // Native root purge owns SystemInfoBar destruction; keep its lists, static
            // reference and automatic callbacks until Unity completes that destruction.
        }
        if (!preserveReceiptFamilyWindows) ReleaseFamilyWindowsForIndependentRuntime();
        // Native final teardown only queues these roots for destruction; their visible
        // state and keyboard focus remain owned by the components until Unity retires them.
        // Immediate hiding belongs only to the independent first-admission preparation.
        if (!retireNativeWorld)
        {
            TaskDlg.Hide();
            if (!preserveReceiptOverlay) ChatCopyOverlay.Hide();
        }
        StoreDlg.ReleaseIndependentRuntime();
        // Native final teardown already queued the message-box root for deferred destruction.
        // Do not close it or clear its live singleton before Unity performs that destruction.
        // Keep the existing immediate cleanup only for non-native host preparation.
        if (!retireNativeWorld && SysMsgBox.I != null)
        {
            SysMsgBox box = SysMsgBox.I;
            SysMsgBox.Close();
            SysMsgBox.I = null;
            Destroy(box.gameObject);
        }
        if (!retireNativeWorld)
        {
            // ConfigureWorld's first admission has no previous native world. Preserve its
            // existing host preparation and receipt-owned state; this is not TeardownWorldUi.
            ClearMapObjectCaches();
            CurMapId = 0;
            PlayerController.ClearFollow();
            CharData.ClearRidingForWorldExit();
        }
        WeatherFX.ReleaseSceneRootForIndependentRuntime();
    }

    private void ReleaseBuiltWorld(bool sceneRegistriesReleased = false,
        Transform releasedRoot = null, bool preservePlayerTransform = false)
    {
        ReferenceFirstWorldWindowOwner pendingConstruction = constructingWindow;
        // The original root is already published when the adapter constructor runs.
        // A constructor failure can therefore leave a root with no managed candidate.
        if (window == null && pendingConstruction == null && worldRoot != null)
            Destroy(worldRoot.gameObject);
        if (window != null)
        {
            bool sameOwner = ReferenceEquals(window, pendingConstruction);
            window.Dispose(sceneRegistriesReleased,
                worldRootReleased: releasedRoot != null && window.WorldRoot == releasedRoot,
                preserveNativePlayerState: preservePlayerTransform);
            window = null;
            if (sameOwner)
            {
                constructingWindow = null;
                pendingConstruction = null;
            }
        }
        if (pendingConstruction != null)
        {
            pendingConstruction.Dispose(sceneRegistriesReleased,
                worldRootReleased: releasedRoot != null && pendingConstruction.WorldRoot == releasedRoot,
                preserveNativePlayerState: preservePlayerTransform);
            if (ReferenceEquals(constructingWindow, pendingConstruction)) constructingWindow = null;
        }

        worldRoot = null;
        // TransitionTo leaves both native player references until construction replaces
        // them. Its retired doll must still expose the live controller until Unity
        // completes the already-requested root destruction.
        if (!preservePlayerTransform)
        {
            pcRef = null;
            PlayerT = null;
        }
    }

    internal void ReleaseFailedWorld(ReferenceFirstWorldWindowOwner failedWindow)
    {
        if (failedWindow != null && ReferenceEquals(window, failedWindow))
        {
            ReleaseBuiltWorld();
        }
    }

    internal static string MapSourceName(int mapId)
    {
        int normalized = mapId >= 100000 ? mapId / 1000 : mapId;
        int sourceId = CustomMaps.SrcOf(normalized);
        MapDef.Entry descriptor;
        return MapDef.TryGet(sourceId, out descriptor) &&
               !string.IsNullOrEmpty(descriptor.file)
            ? descriptor.file
            : "qw" + sourceId.ToString("D4");
    }

    private void OnDestroy()
    {
        // Host destruction without a world keeps its existing adapter-only cleanup. An
        // explicit ReleaseWorld above is the native teardown entry even when its root is null.
        ReleaseWorld(preserveReceiptOverlay: false, preserveReceiptSocialNotices: false,
            preserveReceiptMail: false, preserveReceiptTrade: false,
            preserveReceiptFamilyWindows: false,
            retireNativeWorld: HasWorldStateForIndependentRuntime);
        StopAllCoroutines();
        switching = false;
        _queuedSwitch = null;
        ReleaseWorldContextForIndependentRuntime();
        // Start's send subscription also serves receipt-created Gossip on the title. Retire
        // it with this actual host, not with an individual world/map borrower.
        ReleaseNativeChatSendForIndependentRuntime();
        // Native world teardown retains the live bootstrap and teleport callback.
        // The independent host clears them only when their actual owner is destroyed.
        if (ReferenceEquals(I, this))
        {
            I = null;
            DoTeleport = null;
        }
        if (fadeTex != null)
        {
            Destroy(fadeTex);
            fadeTex = null;
        }
    }
}

}
