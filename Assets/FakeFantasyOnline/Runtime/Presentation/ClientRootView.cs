using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Presentation
{

public sealed partial class ClientRootView : MonoBehaviour, IWorldReceiveContext, IWorldReceiveBudgetContext
{
    // Protocol reachability scaffold only. The product session screens are recovered from the
    // reference TitleFlow implementation. The login surface now uses that recovered composition;
    // world state below remains diagnostic and cannot satisfy M1 acceptance.
    bool IWorldReceiveContext.Switching => GameBootstrap.I != null && GameBootstrap.I.Switching;
    int IWorldReceiveContext.CurrentMapId => GameBootstrap.CurMapId;
    float IWorldReceiveBudgetContext.DispatchMillisecondsPerFrame => NetClient.DispatchMsPerFrame;

    private const float WindowWidth = 520f;
    internal ClientSessionCoordinator _session;
    // Read at call time: native network ownership may replace the attached session.
    public ClientSessionCoordinator CurrentSessionForPlugins => _session;
    internal Action _projectNativeState;
    public void BindNativeStateProjectionForIndependentRuntime(Action projection) =>
        _projectNativeState = projection;
    private CancellationTokenSource _lifetime;
    private Uri _noticeUri;
    private TitleFlow _titleLogin;
    private RecoveredTitleCharacterSurface _titleCharacter;
    internal RecoveredTitleCharacterCreationSurface _titleCharacterCreation;
    private MusicManager _music;
    private ReferenceWorldCombatSound _combatSound;
    private GameBootstrap _gameBootstrap;
    public GameBootstrap WorldBootstrapForIndependentRuntime => _gameBootstrap;
    private ReferenceFirstWorldWindowOwner _firstWorldWindow;
    private readonly HashSet<string> _processNpcChatterNames =
        new HashSet<string>(StringComparer.Ordinal);
    internal IReadOnlyList<ServerCharacterRecord> _characters =
        Array.Empty<ServerCharacterRecord>();
    private WorldEntryRecord _world;
    // Existing diagnostic credential storage is used only before a title exists.
    // Production GUI/commands own and use the actual component fields directly.
    private string _diagnosticAccountName = string.Empty;
    private string _diagnosticPassword = string.Empty;
    private ref string _accountName
    {
        get
        {
            if (!ReferenceEquals(_titleLogin, null)) return ref _titleLogin.account;
            return ref _diagnosticAccountName;
        }
    }
    private ref string _password
    {
        get
        {
            if (!ReferenceEquals(_titleLogin, null)) return ref _titleLogin.password;
            return ref _diagnosticPassword;
        }
    }
    private string _worldStatus = string.Empty;
    private string _status
    {
        get => _titleLogin != null ? _titleLogin.netMsg : _worldStatus;
        set
        {
            if (_titleLogin != null) _titleLogin.netMsg = value;
            else _worldStatus = value;
        }
    }
    // Existing host admission for the serialized create operation. Native login and entry
    // latches belong to the retained TitleFlow owner and never borrow this flag.
    internal bool _creatingCharacter;

    // Retained host-only diagnostic routing for Task cancellation/transport failures.
    // This is the former timestamp sentinel, not the native login latch or its clock.
    internal bool _loginDiagnosticsActive;
    private bool _worldBuildAttempted;
    private long _observedTeleportSequence;
    private ReferenceWorldRevisionFailure _lastReportedWorldRevisionFailure;
    internal bool _backgroundSessionCheckRequested;
    private bool _localAvatarSourceCheckRequested;
    private bool _localAvatarSourceCheckCompleted;
    private bool _localAvatarManualCheckRequested;
    private bool _localAvatarManualReadyReported;
    private string _localAvatarManualLastStage = string.Empty;
    private bool _inventoryEquipmentSourceCheckRequested;
    private bool _inventoryEquipmentSourceCheckCompleted;
    private ReferenceInventoryEquipmentSourceDriver _inventoryEquipmentSourceDriver;
    private bool _inventoryEquipmentManualCheckRequested;
    private bool _inventoryEquipmentManualReadyReported;
    private string _inventoryEquipmentManualLastStage = string.Empty;
    private bool _audioSourceCheckRequested;
    private bool _audioManualCheckRequested;
    private bool _audioSourceCheckCompleted;
    private bool _audioLoginStarted;
    private bool _audioMapSelected;
    private bool _audioMapStarted;
    private float _audioLoginStartedAt;
    private float _audioStartDeadline;
    private float _audioMapStartDeadline;
    private AudioClip _audioLoginClip;
    private float _audioBackgroundHoldStartedAt = -1f;
    private ReferenceFirstWorldWindowOwner _audioBackgroundWorldOwner;
    private bool _audioBackgroundRegressionCompleted;
    private bool _audioBackgroundNpcRequested;
    private long _audioBackgroundOffersVersion;
    private string _audioBackgroundNpcName = string.Empty;
    private float _audioBackgroundNpcDeadline;
    private bool _contentAuthorityCheckRequested;
    private bool _monsterRealCombatTailCheckRequested;
    private ReferenceMonsterRealCombatTailDriver _monsterRealCombatTailDriver;
    private bool _worldGroundLootCheckRequested;
    private ReferenceWorldGroundLootDriver _worldGroundLootDriver;
    private bool _npcTaskLifecycleCheckRequested;
    private ReferenceNpcTaskLifecycleJourneyDriver _npcTaskLifecycleDriver;
    private bool _npcTaskDialogueManualCheckRequested;
    private bool _npcTaskDialogueManualReadyReported;
    private bool _npcTaskDialogueManualDialogVisible;
    private bool _npcTaskDialogueManualDetailVisible;
    private Action<int> _requestTerminalQuit;
    private bool _npcTaskLifecycleTerminalStarted;
    private bool _monsterRealCombatTailBootstrapFailurePending;
    private bool _monsterRealCombatTailBootstrapWriteFailureReported;
    private ReferenceMonsterRealCombatTailExit _monsterRealCombatTailBootstrapFailure;
    private bool _backgroundMapTransitionCheckRequested;
    private bool _backgroundMapTransitionStarted;
    private float _backgroundMapTransitionDeadline;
    private long _backgroundMapTransitionNpcVersion;
    private long _backgroundMapTransitionEntityVersion;
    private long _backgroundMapTransitionObservedNpcVersion = -1;
    private long _backgroundMapTransitionObservedEntityVersion = -1;
    private float _backgroundMapTransitionStableSince = -1f;
    private bool _backgroundSessionCheckStarted;
    private bool _backgroundSessionCheckCompleted;
    private CancellationTokenSource _backgroundSessionTimeout;
    private float _backgroundWorldReadinessDeadline;
    private int _backgroundSessionObservedEntityCount = -1;
    private float _backgroundSessionEntityStableSince = -1f;
    private int _backgroundRosterCount;
    private bool _backgroundScreenshotRequested;
    private string _backgroundScreenshotPath = string.Empty;
    private float _backgroundScreenshotDeadline;
    private bool _backgroundScreenshotCompleted;
    private bool _backgroundScreenshotFailed;

    public MusicManager Music => _music;

    public ReferenceWorldCombatSound CombatSound => _combatSound;

    public void Initialize(ClientSessionCoordinator session, Uri noticeUri)
    {
        Initialize(session, exitCode => UnityEngine.Application.Quit(exitCode), noticeUri);
    }

    public void Initialize(
        ClientSessionCoordinator session,
        Action<int> requestTerminalQuit,
        Uri noticeUri)
    {
        if (_session != null)
        {
            throw new InvalidOperationException("The client view is already initialized.");
        }

        _noticeUri = noticeUri ?? throw new ArgumentNullException(nameof(noticeUri));
        _session = session ?? throw new ArgumentNullException(nameof(session));
        _requestTerminalQuit = requestTerminalQuit ??
            throw new ArgumentNullException(nameof(requestTerminalQuit));
        _lifetime = new CancellationTokenSource();
        string[] arguments = Environment.GetCommandLineArgs();
        _monsterRealCombatTailCheckRequested =
            ReferenceMonsterRealCombatTailCheck.IsRequested(arguments);
        _worldGroundLootCheckRequested =
            ReferenceWorldGroundLootCheck.IsRequested(arguments);
        _npcTaskLifecycleCheckRequested =
            ReferenceNpcTaskLifecycleJourneyCheck.IsRequested(arguments);
        _npcTaskDialogueManualCheckRequested =
            ReferenceNpcTaskDialogueManualCheck.IsRequested(arguments);
        _backgroundMapTransitionCheckRequested =
            ReferenceBackgroundMapTransitionCheck.IsRequested(arguments);
        _contentAuthorityCheckRequested =
            ReferenceContentAuthorityCheck.IsRequested(arguments);
        _localAvatarSourceCheckRequested =
            ReferenceLocalAvatarSourceCheck.IsRequested(arguments);
        _localAvatarManualCheckRequested =
            ReferenceLocalAvatarSourceCheck.IsManualRequested(arguments);
        _inventoryEquipmentSourceCheckRequested =
            ReferenceInventoryEquipmentSourceCheck.IsRequested(arguments);
        _inventoryEquipmentManualCheckRequested =
            ReferenceInventoryEquipmentSourceCheck.IsManualRequested(arguments);
        _audioManualCheckRequested =
            ReferenceAudioSourceCheck.IsManualRequested(arguments);
        _audioSourceCheckRequested =
            _audioManualCheckRequested ||
            ReferenceAudioSourceCheck.IsRequested(arguments);
        _backgroundSessionCheckRequested =
            _backgroundMapTransitionCheckRequested ||
            _contentAuthorityCheckRequested ||
            _localAvatarSourceCheckRequested ||
            _localAvatarManualCheckRequested ||
            _inventoryEquipmentSourceCheckRequested ||
            _inventoryEquipmentManualCheckRequested ||
            _audioSourceCheckRequested ||
            _monsterRealCombatTailCheckRequested ||
            _worldGroundLootCheckRequested ||
            _npcTaskLifecycleCheckRequested ||
            _npcTaskDialogueManualCheckRequested ||
            ReferenceBackgroundSessionCheck.IsRequested(arguments);
        // SoundManager owns the persistent, lazily created sound pool. Borrow an explicitly
        // supplied compatibility component without creating another pool on title entry.
        _combatSound = gameObject.GetComponent<ReferenceWorldCombatSound>();

        _gameBootstrap = gameObject.GetComponent<GameBootstrap>();
        if (_gameBootstrap == null)
        {
            _gameBootstrap = gameObject.AddComponent<GameBootstrap>();
        }
        _gameBootstrap.BindTitleReturnForIndependentRuntime(BeginReturnFromWorld);
        _gameBootstrap.BindNativeWorldHostForIndependentRuntime(
            _session, transform, _combatSound, _processNpcChatterNames);
    }

    private bool HasWorldForIndependentRuntime =>
        _session != null && _world != null &&
        (_session.State == ClientSessionState.InWorld ||
         (_gameBootstrap != null && _gameBootstrap.HasNativeStartupWorldForIndependentRuntime));

    private void AdoptNativeStartupWorldForIndependentRuntime()
    {
        if (_gameBootstrap == null || !_gameBootstrap.HasNativeStartupWorldForIndependentRuntime)
            return;
        _world = _gameBootstrap.CurrentEntry;
        _firstWorldWindow = _gameBootstrap.Window;
        _music = MusicManager.I;
        // Start has already made the one synchronous native BuildWorld attempt. This path
        // must neither replay the title builder nor complete a nonexistent server receipt.
        _worldBuildAttempted = true;
    }

    private void OnGUI()
    {
        AdoptNativeStartupWorldForIndependentRuntime();
        if (_session == null || !HasWorldForIndependentRuntime) return;
        _projectNativeState?.Invoke();
        if (_backgroundSessionCheckRequested)
        {
            if ((_backgroundScreenshotRequested ||
                 _npcTaskDialogueManualCheckRequested ||
                 _localAvatarManualCheckRequested ||
                 _inventoryEquipmentSourceCheckRequested ||
                 _inventoryEquipmentManualCheckRequested) &&
                _firstWorldWindow != null && _firstWorldWindow.Built)
                _firstWorldWindow.DrawHud(Time.time);
            return;
        }
        if (_firstWorldWindow != null && _firstWorldWindow.Built)
            _firstWorldWindow.DrawHud(Time.time);
        else
            DrawDiagnosticWindow(DrawWorld);
    }

    private void Update()
    {
        if (_networkInstanceHost == null) _session?.Pump(this);
        _projectNativeState?.Invoke();
        AdoptNativeStartupWorldForIndependentRuntime();

        if (_audioSourceCheckRequested &&
            !_backgroundSessionCheckStarted &&
            !_backgroundSessionCheckCompleted)
        {
            TryBeginAudioSessionCheck();
            return;
        }

        if (_monsterRealCombatTailBootstrapFailurePending)
        {
            TryFinishMonsterRealCombatTailBootstrapFailure();
            return;
        }

        if (!HasWorldForIndependentRuntime)
        {
            ReleaseFirstWorldWindow();
            return;
        }

        int viewportWidth = _backgroundSessionCheckRequested
            ? ReferenceBackgroundSessionCheck.ViewportWidth
            : Screen.width;
        int viewportHeight = _backgroundSessionCheckRequested
            ? ReferenceBackgroundSessionCheck.ViewportHeight
            : Screen.height;
        if (viewportWidth < 1 || viewportHeight < 1)
        {
            return;
        }

        if (_gameBootstrap != null &&
            !ReferenceEquals(_firstWorldWindow, _gameBootstrap.Window))
        {
            _firstWorldWindow = _gameBootstrap.Window;
            if (_gameBootstrap.CurrentEntry != null)
            {
                _world = _gameBootstrap.CurrentEntry;
            }
        }

        if (_firstWorldWindow == null && !_worldBuildAttempted &&
            !BuildTitleWorldEntry(viewportWidth, viewportHeight))
        {
            return;
        }

        if (_audioSourceCheckRequested && !_backgroundSessionCheckCompleted)
        {
            ObserveAudioSourceTransition();
        }

        DispatchOfficialTeleportEdge();

        bool manualForegroundInput =
            _npcTaskDialogueManualCheckRequested ||
            _localAvatarManualCheckRequested ||
            _inventoryEquipmentManualCheckRequested;
        bool driveOfficialUiShortcuts =
            _firstWorldWindow != null && _firstWorldWindow.Built &&
            (!_backgroundSessionCheckRequested || manualForegroundInput);

        // UIManager.Update (0x06000fa8) evaluates the TypingNow-only Alt+B branch before the
        // ordinary-key guards. Keep that first read separate so a focused chat input can open the
        // face panel while every ordinary shortcut below remains blocked.
        if (driveOfficialUiShortcuts)
            UIManager.UpdateFacePanelShortcutForIndependentRuntime(_firstWorldWindow.ChatUi);

        // Read the original static typing state, which can outlive a newly created chat carrier.
        // The outer drive boundary is retained; admitted manual/background input also obeys typing.
        bool ordinaryUiShortcuts = driveOfficialUiShortcuts &&
            !ChatUI.TypingNow &&
            !MailDlg.Typing &&
            !GossipDlg.Typing &&
            GUIUtility.keyboardControl == 0;
        // The live partial carrier follows the frozen native side-effect order for every slice it
        // owns: Q, C, I, T, N, O, M, F, P, L, X, D, F12, Insert, V, zoom in, zoom out, then Escape.
        // Each key stays an independent GetKeyDown IF, so a frame in which several of them go down
        // runs each matching action once; nothing outside this block reads those keys again.
        if (ordinaryUiShortcuts)
        {
            UIManager.UpdateCharacterWindowHotkeysForIndependentRuntime(
                _firstWorldWindow.ChatUi, _firstWorldWindow.GossipDialog,
                _firstWorldWindow.TeamDialog, _firstWorldWindow.LocalEquipmentDialog,
                _firstWorldWindow.InventoryDialog);
            UIManager.UpdateTaskShortcutForIndependentRuntime(
                _firstWorldWindow.ChatUi, _firstWorldWindow.GossipDialog);
            UIManager.UpdateSettingsForIndependentRuntime(
                _firstWorldWindow.ChatUi, _firstWorldWindow.GossipDialog);

            if (Input.GetKeyDown(KeyCode.M))
            {
                _firstWorldWindow.ToggleMapDiagram();
            }

            UIManager.UpdateFamilyAndPetHotkeysForIndependentRuntime(
                _firstWorldWindow.ChatUi, _firstWorldWindow.GossipDialog);
            _firstWorldWindow.UpdateMailAndDuelShortcuts();
            UIManager.UpdateHelpShortcutForIndependentRuntime();
            _firstWorldWindow.UpdateInsertAndChannelShortcuts();

            ReferenceFirstWorldMinimapHud minimap = _firstWorldWindow.MinimapHud;
            if (minimap != null &&
                (Input.GetKeyDown(KeyCode.Equals) ||
                 Input.GetKeyDown(KeyCode.KeypadPlus)))
            {
                minimap.ZoomMinimap(true);
            }
            if (minimap != null &&
                (Input.GetKeyDown(KeyCode.Minus) ||
                 Input.GetKeyDown(KeyCode.KeypadMinus)))
            {
                minimap.ZoomMinimap(false);
            }

            // The frozen 06000fa8 Escape branch is dominated by the four ordinary guards alone:
            // arm64 0x3a7afc/0x3a7b04 GUIUtility.get_keyboardControl followed by cbz w0,#0x3a7b1c,
            // and x86_64 0x3ba907 call followed by test eax,eax and je 0x3ba921; neither body reads
            // RenameDlg, and UiEsc.CloseTop (06000fca, 0x3a830c, 3812 bytes, 132 distinct callees)
            // contains no RenameDlg slot either. RenameDlg.OnGUI (06000a60, cmp w0,#0x1b at
            // 0x2d0c70) is that window's own native consumer, not a precondition of this host, so
            // testing it here blocked the official UiEsc chain and its tail for every Escape pressed
            // while the rename dialog was visible.
            // WBC-056 lane C correction: this comment used to say the same about SkillTargeting,
            // which was wrong. UiEsc.CloseTop does carry a SkillTargeting front gate, but as a
            // direct static-byte read - metadata usage 02410 = TypeInfo:SkillTargeting,
            // static_fields +0x0, arm64 06000fca.c:65-75 and x86_64 06000fca.c:61-68, return 1 and
            // nothing else - so no SkillTargeting member token appears among the body's callees.
            // That reserve is restored in
            // ReferenceFirstWorldWindowOwner.CloseUiEscTopForIndependentRuntime; the aim is still
            // ended by SkillTargeting.Update (06000d1d, GetKeyDown 27 at 0x322d20), which is the
            // official consumer and not a precondition of this host.
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                _firstWorldWindow.HandleEscape();
            }
        }
        bool worldTickAccepted = true;
        if (_firstWorldWindow != null)
        {
            worldTickAccepted = _firstWorldWindow.Tick(
                viewportWidth,
                viewportHeight,
                ReferenceWorldCameraFollow.DefaultDpiScale,
                Time.deltaTime,
                Time.time,
                false,
                !_backgroundSessionCheckRequested || manualForegroundInput);
        }

        if (_firstWorldWindow != null && worldTickAccepted &&
            _firstWorldWindow.LastWorldRevisionFailure !=
                ReferenceWorldRevisionFailure.None)
        {
            ReferenceWorldRevisionFailure revisionFailure =
                _firstWorldWindow.LastWorldRevisionFailure;
            _status = WorldRevisionFailureStatus(revisionFailure);
            if (_lastReportedWorldRevisionFailure != revisionFailure)
            {
                Debug.LogError(
                    ReferenceBackgroundSessionCheck.WorldRevisionFailureLine(
                        revisionFailure));
                _lastReportedWorldRevisionFailure = revisionFailure;
            }

            if (_backgroundSessionCheckRequested &&
                !_backgroundSessionCheckCompleted)
            {
                Debug.LogError(
                    ReferenceBackgroundSessionCheck.OwnerFailureLine(
                        ReferenceFirstWorldWindowFailure.WorldRevisionRejected));
                FinishBackgroundSessionCheck(
                    ReferenceBackgroundSessionCheckExit.WorldOwnerFailed);
            }
        }

        if (_firstWorldWindow != null && !worldTickAccepted)
        {
            _status = WorldFailureStatus(_firstWorldWindow.LastFailure);
            if (_backgroundSessionCheckRequested)
            {
                if (_firstWorldWindow.LastFailure ==
                        ReferenceFirstWorldWindowFailure.WorldRevisionRejected &&
                    _firstWorldWindow.Frame != null)
                {
                    Debug.LogError(
                        ReferenceBackgroundSessionCheck.WorldRevisionFailureLine(
                            _firstWorldWindow.Frame.LastRevisionFailure));
                }
                Debug.LogError(
                    ReferenceBackgroundSessionCheck.OwnerFailureLine(
                        _firstWorldWindow.LastFailure));
            }
            if (_backgroundSessionCheckRequested)
            {
                FinishBackgroundSessionCheck(
                    ReferenceBackgroundSessionCheckExit.WorldOwnerFailed);
            }
            else
            {
                Debug.LogError(
                    "[world.owner] stage=tick failure=" +
                    _firstWorldWindow.LastFailure);
                _gameBootstrap.ReleaseFailedWorld(_firstWorldWindow);
                _firstWorldWindow = null;
                _lastReportedWorldRevisionFailure = ReferenceWorldRevisionFailure.None;
            }
        }

        if (_npcTaskDialogueManualCheckRequested &&
            _npcTaskDialogueManualReadyReported &&
            _firstWorldWindow != null && _firstWorldWindow.Built)
        {
            ObserveNpcTaskDialogueManualStage();
        }

        if (_localAvatarManualCheckRequested &&
            _localAvatarManualReadyReported &&
            _firstWorldWindow != null && _firstWorldWindow.Built)
        {
            ObserveLocalAvatarManualStage();
        }

        if (_inventoryEquipmentManualCheckRequested &&
            _inventoryEquipmentManualReadyReported &&
            _firstWorldWindow != null && _firstWorldWindow.Built)
        {
            ObserveInventoryEquipmentManualStage();
        }

        if (_backgroundSessionCheckRequested &&
            !_backgroundSessionCheckCompleted &&
            _firstWorldWindow != null &&
            _firstWorldWindow.Built)
        {
            if (_npcTaskLifecycleDriver != null)
            {
                _npcTaskLifecycleDriver.Tick(_firstWorldWindow, _gameBootstrap);
                if (_npcTaskLifecycleDriver.Terminal)
                {
                    FinishNpcTaskLifecycleCheck(_npcTaskLifecycleDriver);
                }
            }
            else if (_worldGroundLootDriver != null)
            {
                _worldGroundLootDriver.Tick();
                if (_worldGroundLootDriver.Terminal)
                {
                    FinishWorldGroundLootCheck(_worldGroundLootDriver);
                }
            }
            else if (_monsterRealCombatTailDriver != null)
            {
                _monsterRealCombatTailDriver.Tick();
                if (_monsterRealCombatTailDriver.Terminal)
                {
                    FinishMonsterRealCombatTailCheck(_monsterRealCombatTailDriver);
                }
            }
            else if (_inventoryEquipmentSourceDriver != null)
            {
                _inventoryEquipmentSourceDriver.Tick();
                if (_inventoryEquipmentSourceDriver.Terminal)
                {
                    FinishInventoryEquipmentSourceCheck(
                        _inventoryEquipmentSourceDriver);
                }
            }
            else
            {
                TryFinishBackgroundWorldCheck();
            }
        }
    }

    private void ObserveNpcTaskDialogueManualStage()
    {
        ReferenceFirstWorldNpcDialog dialog = _firstWorldWindow.NpcDialog;
        if (dialog == null)
        {
            return;
        }

        bool visible = dialog.Visible;
        bool detail = visible && dialog.InSubView;
        if (visible && !_npcTaskDialogueManualDialogVisible)
        {
            Debug.Log(
                ReferenceNpcTaskDialogueManualCheck.StageLine(
                    "DIALOG_OPEN",
                    dialog.NpcName));
        }
        if (detail && !_npcTaskDialogueManualDetailVisible)
        {
            Debug.Log(
                ReferenceNpcTaskDialogueManualCheck.StageLine(
                    "DETAIL_OPEN",
                    dialog.NpcName));
        }
        if (!visible && _npcTaskDialogueManualDialogVisible)
        {
            Debug.Log(
                ReferenceNpcTaskDialogueManualCheck.StageLine(
                    "DIALOG_CLOSED",
                    dialog.NpcName));
        }

        _npcTaskDialogueManualDialogVisible = visible;
        _npcTaskDialogueManualDetailVisible = detail;
    }

    private void ObserveLocalAvatarManualStage()
    {
        ReferenceWorldLocalPlayerDoll player =
            _firstWorldWindow.Frame == null
                ? null
                : _firstWorldWindow.Frame.Player;
        string stage = ReferenceLocalAvatarSourceCheck.ManualStageLine(player);
        if (!string.Equals(
                stage,
                _localAvatarManualLastStage,
                StringComparison.Ordinal))
        {
            _localAvatarManualLastStage = stage;
            Debug.Log(stage);
        }
    }

    private void ObserveInventoryEquipmentManualStage()
    {
        string stage = ReferenceInventoryEquipmentSourceCheck.ManualStageLine(
            _firstWorldWindow);
        if (!string.Equals(
                stage,
                _inventoryEquipmentManualLastStage,
                StringComparison.Ordinal))
        {
            _inventoryEquipmentManualLastStage = stage;
            Debug.Log(stage);
        }
    }

    private void FinishInventoryEquipmentSourceCheck(
        ReferenceInventoryEquipmentSourceDriver driver)
    {
        if (_inventoryEquipmentSourceCheckCompleted)
        {
            return;
        }

        _inventoryEquipmentSourceDriver = null;
        _inventoryEquipmentSourceCheckCompleted = true;
        if (driver == null || !driver.Succeeded)
        {
            _backgroundSessionCheckCompleted = true;
            _accountName = string.Empty;
            _password = string.Empty;
            Debug.LogError(driver == null
                ? ReferenceInventoryEquipmentSourceCheck.FailureLine(
                    "result.missing")
                : driver.Result);
            UnityEngine.Application.Quit(79);
            return;
        }

        Debug.Log(driver.Result);
    }

    private void DispatchOfficialTeleportEdge()
    {
        IReadOnlyList<WorldTeleportState> states = _session.TeleportStates;
        Action<int, float, float> callback = GameBootstrap.DoTeleport;
        if (callback == null)
        {
            return;
        }

        for (int index = 0; index < states.Count; index++)
        {
            WorldTeleportState state = states[index];
            if (state.Sequence <= _observedTeleportSequence)
            {
                continue;
            }

            _observedTeleportSequence = state.Sequence;
            callback(state.MapId, state.X, state.Y);
        }
    }

    private void TryBeginAudioSessionCheck()
    {
        if (_music == null || _music.Source == null)
        {
            if (Time.realtimeSinceStartup >= _audioStartDeadline)
            {
                FinishAudioSourceCheck("login.owner_unavailable");
            }
            return;
        }

        AudioSource source = _music.Source;
        bool loginReady =
            _music.CurrentTrack == "M001" &&
            _music.CurrentClipName == "M001" &&
            source.clip != null && source.clip.name == "M001" &&
            source.isPlaying && source.loop && !source.playOnAwake &&
            _music.HasActiveRequest && !_music.HasInflightRequest;
        if (!loginReady)
        {
            if (Time.realtimeSinceStartup >= _audioStartDeadline)
            {
                FinishAudioSourceCheck("login.start_timeout");
            }
            return;
        }

        if (!_audioLoginStarted)
        {
            _audioLoginStarted = true;
            _audioLoginStartedAt = Time.realtimeSinceStartup;
            _audioLoginClip = source.clip;
            Debug.Log(ReferenceAudioSourceCheck.StageLine("LOGIN_STARTED"));
        }

        float hold = _audioManualCheckRequested
            ? ReferenceAudioSourceCheck.ManualLoginHoldSeconds
            : ReferenceAudioSourceCheck.UnattendedLoginHoldSeconds;
        if (Time.realtimeSinceStartup - _audioLoginStartedAt < hold)
        {
            return;
        }

        _audioMapStartDeadline = Time.realtimeSinceStartup +
            ReferenceAudioSourceCheck.MapStartTimeoutSeconds;
        BeginBackgroundSessionCheck();
    }

    private void ObserveAudioSourceTransition()
    {
        if (!_audioLoginStarted || _music == null || _music.Source == null)
        {
            return;
        }

        AudioSource source = _music.Source;
        if (!_audioMapSelected && _music.CurrentTrack == "M003")
        {
            if (source.clip == _audioLoginClip && source.isPlaying &&
                _music.HasInflightRequest)
            {
                _audioMapSelected = true;
                Debug.Log(ReferenceAudioSourceCheck.StageLine("MAP_SELECTED"));
            }
        }

        if (_audioMapSelected && !_audioMapStarted &&
            _music.CurrentTrack == "M003" &&
            _music.CurrentClipName == "M003" &&
            source.clip != null && source.clip.name == "M003" &&
            source.isPlaying && source.loop && !source.playOnAwake &&
            _music.HasActiveRequest && !_music.HasInflightRequest)
        {
            _audioMapStarted = true;
            Debug.Log(ReferenceAudioSourceCheck.StageLine("MAP_STARTED"));
        }
    }

    private void FinishAudioSourceCheck(string failureCode)
    {
        _backgroundSessionCheckCompleted = true;
        _accountName = string.Empty;
        _password = string.Empty;
        Debug.LogError(ReferenceAudioSourceCheck.FailureLine(failureCode));
        UnityEngine.Application.Quit(79);
    }

    private async void BeginBackgroundSessionCheck()
    {
        if (_backgroundSessionCheckStarted ||
            _backgroundSessionCheckCompleted ||
            _session == null ||
            _lifetime == null)
        {
            return;
        }

        _backgroundSessionCheckStarted = true;
        string account;
        string transientPassword;
        if (!RecoveredTitleLoginSurface.TryLoadRememberedCredentialPair(
                out account,
                out transientPassword) ||
            !ReferenceBackgroundSessionCheck.IsManagedAccount(account))
        {
            FinishBackgroundSessionCheck(
                ReferenceBackgroundSessionCheckExit.CredentialUnavailable);
            return;
        }

        _backgroundSessionTimeout =
            CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
        _backgroundSessionTimeout.CancelAfter(TimeSpan.FromSeconds(30));
        CancellationToken token = _backgroundSessionTimeout.Token;
        try
        {
            // Initialize may enter here before Bootstrap starts the native coroutine. Wait
            // only for its real ready/error output; ordinary interactive login stays immediate.
            await WaitForBackgroundAddressResult(token);
            token.ThrowIfCancellationRequested();
            Result<IReadOnlyList<ServerCharacterRecord>> login =
                await _session.LoginAsync(account, transientPassword, token);
            transientPassword = string.Empty;
            account = string.Empty;
            if (!login.IsSuccess)
            {
                FinishBackgroundSessionCheck(
                    token.IsCancellationRequested
                        ? ReferenceBackgroundSessionCheckExit.Timeout
                        : ReferenceBackgroundSessionCheckExit.LoginFailed);
                return;
            }

            _characters = login.Value;
            int connectedServerIndex = _session.ConnectedServerIndex;
            Result selection = _session.ConfirmServerSelection(connectedServerIndex);
            if (!selection.IsSuccess)
            {
                FinishBackgroundSessionCheck(
                    ReferenceBackgroundSessionCheckExit.ServerSelectionFailed);
                return;
            }

            if (_characters.Count == 0)
            {
                Result creationStart = _session.BeginCharacterCreation();
                if (!creationStart.IsSuccess)
                {
                    FinishBackgroundSessionCheck(
                        ReferenceBackgroundSessionCheckExit.CharacterProvisionFailed);
                    return;
                }

                Result<IReadOnlyList<ServerCharacterRecord>> creation =
                    await _session.CreateCharacterAsync(
                        ReferenceBackgroundSessionCheck.CreateManagedCharacterRequest(),
                        token);
                if (!creation.IsSuccess)
                {
                    FinishBackgroundSessionCheck(
                        ReferenceBackgroundSessionCheckExit.CharacterProvisionFailed);
                    return;
                }

                _characters = creation.Value;
            }

            _backgroundRosterCount = _characters.Count;
            if (_characters.Count == 0)
            {
                FinishBackgroundSessionCheck(
                    ReferenceBackgroundSessionCheckExit.RosterEmpty);
                return;
            }

            // The background entry has no slot click, so it publishes the same roster row it is
            // about to enter: official CharData holds the selected character before the
            // enter-world request (TitleFlow.MirrorSlotToCharData 0x06000efb). This existing
            // product-only row adapter preserves that projection without constructing a title
            // or inventing a native slot receiver for the background entry.
            RecoveredTitleCharacterSurface.MirrorSlotToCharData(_characters[0]);
            Result<WorldEntryRecord> entry = await _session.EnterWorldAsync(
                _characters[0].CharacterId,
                token);
            if (!entry.IsSuccess)
            {
                FinishBackgroundSessionCheck(
                    token.IsCancellationRequested
                        ? ReferenceBackgroundSessionCheckExit.Timeout
                        : ReferenceBackgroundSessionCheckExit.EnterWorldFailed);
                return;
            }

            // The installed title callback already consumed this exact selected receipt and
            // reached its builder return on the host. Awaiting observes it; never build twice.
            _backgroundWorldReadinessDeadline =
                Time.realtimeSinceStartup +
                ReferenceBackgroundSessionCheck.WorldReadinessTimeoutSeconds;
            _backgroundSessionObservedEntityCount = -1;
            _backgroundSessionEntityStableSince = -1f;
        }
        catch (OperationCanceledException)
        {
            // A destroyed view is no longer a QA result owner. The existing timeout still
            // reports Timeout while the live view/lifetime remains active.
            if (this == null || _lifetime == null || _lifetime.IsCancellationRequested) return;
            FinishBackgroundSessionCheck(
                ReferenceBackgroundSessionCheckExit.Timeout);
        }
        catch (Exception exception)
        {
            if (this == null || _lifetime == null || _lifetime.IsCancellationRequested) return;
            Debug.LogException(exception);
            FinishBackgroundSessionCheck(
                ReferenceBackgroundSessionCheckExit.Unexpected);
        }
        finally
        {
            transientPassword = string.Empty;
            account = string.Empty;
            if (_backgroundSessionTimeout != null)
            {
                _backgroundSessionTimeout.Dispose();
                _backgroundSessionTimeout = null;
            }
        }
    }

    private void TryFinishBackgroundWorldCheck()
    {
        int npcCount = _session.Npcs == null ? 0 : _session.Npcs.Count;
        int entityCount = _session.WorldEntities == null
            ? 0
            : _session.WorldEntities.Count;
        if (_backgroundMapTransitionCheckRequested)
        {
            TryFinishBackgroundMapTransitionCheck(npcCount, entityCount);
            return;
        }

        if (_npcTaskLifecycleCheckRequested)
        {
            if (_session.State == ClientSessionState.InWorld && _world != null &&
                _world.MapId == ReferenceNpcTaskLifecycleJourneyCheck.InitialMapId &&
                _session.PlayerStatusState != null && npcCount > 0 &&
                _firstWorldWindow != null && _firstWorldWindow.Built)
            {
                _npcTaskLifecycleDriver =
                    new ReferenceNpcTaskLifecycleJourneyDriver(_session);
                _npcTaskLifecycleDriver.Tick(_firstWorldWindow, _gameBootstrap);
                return;
            }

            if (Time.realtimeSinceStartup >= _backgroundWorldReadinessDeadline)
            {
                FinishNpcTaskLifecycleCheck(null);
            }
            return;
        }

        if (ReferenceBackgroundSessionCheck.IsWorldReady(
                _session.State,
                _world,
                _session.PlayerStatusState,
                npcCount,
                entityCount))
        {
            // Background evidence adapter only. The official capture waits for the startup
            // monster stream before reading CombatWorld.monsters; do the same without changing
            // the production gateway or world owner. A first-packet screenshot made the reported
            // count race between 1 and the complete snapshot. Count, rather than the revision,
            // is observed because existing entities can continue receiving state updates.
            float now = Time.realtimeSinceStartup;
            if (entityCount != _backgroundSessionObservedEntityCount)
            {
                _backgroundSessionObservedEntityCount = entityCount;
                _backgroundSessionEntityStableSince = now;
                return;
            }

            if (!ReferenceBackgroundSessionCheck.IsWorldEntitySnapshotStable(
                    _backgroundSessionObservedEntityCount,
                    entityCount,
                    _backgroundSessionEntityStableSince,
                    now))
            {
                if (now < _backgroundWorldReadinessDeadline)
                {
                    return;
                }

                FinishBackgroundSessionCheck(
                    ReferenceBackgroundSessionCheckExit.WorldStateIncomplete);
                return;
            }

            if (_audioSourceCheckRequested)
            {
                if (!_audioMapStarted)
                {
                    if (Time.realtimeSinceStartup >= _audioMapStartDeadline)
                    {
                        FinishAudioSourceCheck("map.start_timeout");
                    }
                    return;
                }

                if (!_audioManualCheckRequested &&
                    !_audioBackgroundRegressionCompleted)
                {
                    float backgroundNow = Time.realtimeSinceStartup;
                    if (_audioBackgroundHoldStartedAt < 0f)
                    {
                        _audioBackgroundHoldStartedAt = backgroundNow;
                        _audioBackgroundWorldOwner = _firstWorldWindow;
                        Debug.Log(
                            ReferenceAudioSourceCheck.StageLine(
                                "BACKGROUND_HOLD_STARTED"));
                        return;
                    }

                    if (backgroundNow - _audioBackgroundHoldStartedAt <
                        ReferenceAudioSourceCheck.UnattendedBackgroundHoldSeconds)
                    {
                        return;
                    }

                    if (_session.State != ClientSessionState.InWorld ||
                        _world == null || _world.MapId != 6 ||
                        _gameBootstrap == null ||
                        !ReferenceEquals(
                            _audioBackgroundWorldOwner,
                            _firstWorldWindow) ||
                        !ReferenceEquals(
                            _audioBackgroundWorldOwner,
                            _gameBootstrap.Window) ||
                        _firstWorldWindow == null || !_firstWorldWindow.Built ||
                        _firstWorldWindow.LastFailure !=
                            ReferenceFirstWorldWindowFailure.None ||
                        _firstWorldWindow.LastWorldRevisionFailure !=
                            ReferenceWorldRevisionFailure.None)
                    {
                        FinishAudioSourceCheck("background.world_owner_lost");
                        return;
                    }

                    AudioSource retainedAudio = _music == null ? null : _music.Source;
                    if (retainedAudio == null || !retainedAudio.isPlaying ||
                        !retainedAudio.loop || retainedAudio.clip == null ||
                        retainedAudio.clip.name != "M003" ||
                        _music.CurrentTrack != "M003")
                    {
                        FinishAudioSourceCheck("background.music_not_retained");
                        return;
                    }

                    if (!_audioBackgroundNpcRequested)
                    {
                        Debug.Log(ReferenceAudioSourceCheck.StageLine("BACKGROUND_HOLD_COMPLETED"));
                        _audioBackgroundOffersVersion = _session.NpcTaskOffersStateVersion;
                        if (!ReferenceAudioSourceCheck.TryOpenLiveNpcThroughPublicPointer(
                                _firstWorldWindow, out _audioBackgroundNpcName))
                        {
                            FinishAudioSourceCheck("background.npc_open_failed");
                            return;
                        }
                        _audioBackgroundNpcRequested = true;
                        _audioBackgroundNpcDeadline = backgroundNow +
                            ReferenceAudioSourceCheck.MapStartTimeoutSeconds;
                        return;
                    }

                    WorldNpcTaskOffersState offers = _session.NpcTaskOffersState;
                    if (_session.NpcTaskOffersStateVersion <= _audioBackgroundOffersVersion ||
                        offers == null || !string.Equals(offers.NpcName,
                            _audioBackgroundNpcName, StringComparison.Ordinal))
                    {
                        if (backgroundNow >= _audioBackgroundNpcDeadline)
                        {
                            FinishAudioSourceCheck("background.npc_authority_timeout");
                        }
                        return;
                    }

                    _audioBackgroundRegressionCompleted = true;
                    Debug.Log(
                        ReferenceAudioSourceCheck.StageLine(
                            "BACKGROUND_WORLD_NPC_READY"));
                }

                if (!_audioSourceCheckCompleted)
                {
                    _audioSourceCheckCompleted = true;
                    Debug.Log(_audioManualCheckRequested
                        ? ReferenceAudioSourceCheck.ReadyLine(_music.GetVolume())
                        : ReferenceAudioSourceCheck.SuccessLine(_music.GetVolume()));
                }

                if (_audioManualCheckRequested)
                {
                    return;
                }
            }

            if (_localAvatarManualCheckRequested)
            {
                if (!_localAvatarManualReadyReported)
                {
                    Debug.Log(
                        ReferenceLocalAvatarSourceCheck.ManualReadyLine(
                            _world.MapId));
                    _localAvatarManualReadyReported = true;
                }
                return;
            }

            if (_inventoryEquipmentManualCheckRequested)
            {
                if (!_inventoryEquipmentManualReadyReported)
                {
                    Debug.Log(
                        ReferenceInventoryEquipmentSourceCheck.ManualReadyLine(
                            _world.MapId));
                    _inventoryEquipmentManualReadyReported = true;
                }
                return;
            }

            if (_inventoryEquipmentSourceCheckRequested &&
                !_inventoryEquipmentSourceCheckCompleted)
            {
                if (_inventoryEquipmentSourceDriver == null)
                {
                    _inventoryEquipmentSourceDriver =
                        new ReferenceInventoryEquipmentSourceDriver(
                            _session,
                            _firstWorldWindow);
                }
                _inventoryEquipmentSourceDriver.Tick();
                if (_inventoryEquipmentSourceDriver.Terminal)
                {
                    FinishInventoryEquipmentSourceCheck(
                        _inventoryEquipmentSourceDriver);
                }
                return;
            }

            if (_localAvatarSourceCheckRequested &&
                !_localAvatarSourceCheckCompleted)
            {
                string localAvatarResult;
                if (!ReferenceLocalAvatarSourceCheck.TryVerify(
                        _firstWorldWindow,
                        _world,
                        out localAvatarResult))
                {
                    _localAvatarSourceCheckCompleted = true;
                    _backgroundSessionCheckCompleted = true;
                    _accountName = string.Empty;
                    _password = string.Empty;
                    Debug.LogError(localAvatarResult);
                    UnityEngine.Application.Quit(79);
                    return;
                }

                _localAvatarSourceCheckCompleted = true;
                Debug.Log(localAvatarResult);
            }

            if (_worldGroundLootCheckRequested)
            {
                _worldGroundLootDriver =
                    new ReferenceWorldGroundLootDriver(
                        _session,
                        _firstWorldWindow);
                _worldGroundLootDriver.Tick();
                return;
            }

            if (_npcTaskDialogueManualCheckRequested)
            {
                if (!_npcTaskDialogueManualReadyReported)
                {
                    Debug.Log(
                        ReferenceNpcTaskDialogueManualCheck.ReadyLine(
                            _world.MapId,
                            npcCount,
                            entityCount));
                    _npcTaskDialogueManualReadyReported = true;
                }
                return;
            }

            if (_monsterRealCombatTailCheckRequested)
            {
                _monsterRealCombatTailDriver =
                    new ReferenceMonsterRealCombatTailDriver(
                        _session,
                        _firstWorldWindow);
                _monsterRealCombatTailDriver.Tick();
                return;
            }

            if (!_backgroundScreenshotRequested)
            {
                if (TryRequestBackgroundScreenshot())
                {
                    return;
                }

                Debug.Log(
                    ReferenceBackgroundSessionCheck.ScreenshotLine(false));
            }
            else if (IsBackgroundScreenshotReady())
            {
                Debug.Log(
                    ReferenceBackgroundSessionCheck.ScreenshotLine(true));
            }
            else if (_backgroundScreenshotFailed)
            {
                Debug.Log(
                    ReferenceBackgroundSessionCheck.ScreenshotLine(false));
            }
            else if (Time.realtimeSinceStartup < _backgroundScreenshotDeadline)
            {
                return;
            }
            else
            {
                Debug.Log(
                    ReferenceBackgroundSessionCheck.ScreenshotLine(false));
            }

            FinishBackgroundSessionCheck(
                ReferenceBackgroundSessionCheckExit.Success,
                npcCount,
                entityCount);
            return;
        }

        if (Time.realtimeSinceStartup >= _backgroundWorldReadinessDeadline)
        {
            FinishBackgroundSessionCheck(
                ReferenceBackgroundSessionCheckExit.WorldStateIncomplete);
        }
    }

    private void TryFinishBackgroundMapTransitionCheck(int npcCount, int entityCount)
    {
        if (!_backgroundMapTransitionStarted)
        {
            if (!ReferenceBackgroundSessionCheck.IsWorldReady(
                    _session.State,
                    _world,
                    _session.PlayerStatusState,
                    npcCount,
                    entityCount))
            {
                if (Time.realtimeSinceStartup >= _backgroundWorldReadinessDeadline)
                {
                    FinishBackgroundSessionCheck(
                        ReferenceBackgroundSessionCheckExit.WorldStateIncomplete);
                }
                return;
            }

            if (_world.MapId != ReferenceBackgroundMapTransitionCheck.SourceMapId ||
                _gameBootstrap == null || _gameBootstrap.CurrentEntry == null ||
                _gameBootstrap.CurrentEntry.MapId !=
                    ReferenceBackgroundMapTransitionCheck.SourceMapId)
            {
                FinishBackgroundMapTransitionCheck(
                    ReferenceBackgroundMapTransitionCheckExit.SourceMapUnexpected);
                return;
            }

            _backgroundMapTransitionNpcVersion = _session.NpcsVersion;
            _backgroundMapTransitionEntityVersion = _session.WorldEntitiesVersion;
            _backgroundMapTransitionStarted = true;
            _backgroundMapTransitionDeadline =
                Time.realtimeSinceStartup +
                ReferenceBackgroundMapTransitionCheck.TargetReadinessTimeoutSeconds;
            _gameBootstrap.SwitchMapAuthoritative(
                ReferenceBackgroundMapTransitionCheck.TargetMapId,
                ReferenceBackgroundMapTransitionCheck.TargetX,
                ReferenceBackgroundMapTransitionCheck.TargetY);
            return;
        }

        int targetEntityCount = 0;
        foreach (WorldEntityState state in _session.WorldEntities.Values)
        {
            if (state.MapIdGuard == ReferenceBackgroundMapTransitionCheck.TargetMapId)
            {
                targetEntityCount++;
            }
        }

        int renderedNpcCount = _firstWorldWindow == null ||
            _firstWorldWindow.Frame == null
                ? 0
                : _firstWorldWindow.Frame.NpcActors.Count;

        if (ReferenceBackgroundMapTransitionCheck.IsTargetReady(
                _session.State,
                _gameBootstrap,
                _firstWorldWindow,
                _session.PlayerStatusState,
                renderedNpcCount,
                _session.NpcsVersion,
                _backgroundMapTransitionNpcVersion,
                _session.WorldEntitiesVersion,
                _backgroundMapTransitionEntityVersion))
        {
            if (_backgroundMapTransitionObservedNpcVersion != _session.NpcsVersion ||
                _backgroundMapTransitionObservedEntityVersion !=
                    _session.WorldEntitiesVersion)
            {
                _backgroundMapTransitionObservedNpcVersion = _session.NpcsVersion;
                _backgroundMapTransitionObservedEntityVersion =
                    _session.WorldEntitiesVersion;
                _backgroundMapTransitionStableSince = Time.realtimeSinceStartup;
                return;
            }

            if (_backgroundMapTransitionStableSince < 0f)
            {
                _backgroundMapTransitionStableSince = Time.realtimeSinceStartup;
                return;
            }

            if (Time.realtimeSinceStartup - _backgroundMapTransitionStableSince <
                ReferenceBackgroundMapTransitionCheck.TargetSnapshotStabilitySeconds)
            {
                return;
            }

            if (!_backgroundScreenshotRequested)
            {
                if (TryRequestBackgroundScreenshot())
                {
                    return;
                }

                Debug.Log(ReferenceBackgroundMapTransitionCheck.ScreenshotLine(false));
            }
            else if (IsBackgroundScreenshotReady())
            {
                Debug.Log(ReferenceBackgroundMapTransitionCheck.ScreenshotLine(true));
            }
            else if (_backgroundScreenshotFailed)
            {
                Debug.Log(ReferenceBackgroundMapTransitionCheck.ScreenshotLine(false));
            }
            else if (Time.realtimeSinceStartup < _backgroundScreenshotDeadline)
            {
                return;
            }
            else
            {
                Debug.Log(ReferenceBackgroundMapTransitionCheck.ScreenshotLine(false));
            }

            FinishBackgroundMapTransitionCheck(
                ReferenceBackgroundMapTransitionCheckExit.Success,
                renderedNpcCount,
                targetEntityCount);
            return;
        }

        _backgroundMapTransitionStableSince = -1f;

        if (Time.realtimeSinceStartup >= _backgroundMapTransitionDeadline)
        {
            Debug.Log(
                ReferenceBackgroundMapTransitionCheck.DiagnosticLine(
                    _session.State,
                    _gameBootstrap,
                    _firstWorldWindow,
                    renderedNpcCount,
                    targetEntityCount,
                    _session.NpcsVersion,
                    _backgroundMapTransitionNpcVersion,
                    _session.WorldEntitiesVersion,
                    _backgroundMapTransitionEntityVersion));
            FinishBackgroundMapTransitionCheck(
                ReferenceBackgroundMapTransitionCheckExit.TargetTimeout);
        }
    }

    private bool TryRequestBackgroundScreenshot()
    {
        try
        {
            _backgroundScreenshotPath = Path.GetFullPath(
                Path.Combine(
                    UnityEngine.Application.dataPath,
                    "../..",
                    _backgroundMapTransitionCheckRequested
                        ? ReferenceBackgroundMapTransitionCheck.ScreenshotFileName
                        : ReferenceBackgroundSessionCheck.ScreenshotFileName));
            if (File.Exists(_backgroundScreenshotPath))
            {
                File.Delete(_backgroundScreenshotPath);
            }

            _backgroundScreenshotRequested = true;
            _backgroundScreenshotDeadline =
                Time.realtimeSinceStartup +
                ReferenceBackgroundSessionCheck.ScreenshotTimeoutSeconds;
            StartCoroutine(CaptureBackgroundScreenshotAtEndOfFrame());
            return true;
        }
        catch (Exception)
        {
            _backgroundScreenshotPath = string.Empty;
            return false;
        }
    }

    private bool IsBackgroundScreenshotReady()
    {
        try
        {
            return _backgroundScreenshotCompleted &&
                   !string.IsNullOrEmpty(_backgroundScreenshotPath) &&
                   File.Exists(_backgroundScreenshotPath) &&
                   new FileInfo(_backgroundScreenshotPath).Length > 0;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private IEnumerator CaptureBackgroundScreenshotAtEndOfFrame()
    {
        yield return new WaitForEndOfFrame();
        if (Screen.width < 1 || Screen.height < 1)
        {
            _backgroundScreenshotFailed = true;
            yield break;
        }

        Texture2D screenshot = null;
        try
        {
            screenshot = new Texture2D(
                Screen.width,
                Screen.height,
                TextureFormat.RGB24,
                false,
                false);
            screenshot.ReadPixels(
                new Rect(0f, 0f, Screen.width, Screen.height),
                0,
                0,
                false);
            screenshot.Apply(false, false);
            if (!HasBackgroundScreenshotContent(screenshot.GetPixels32()))
            {
                _backgroundScreenshotFailed = true;
            }
            else
            {
                byte[] png = screenshot.EncodeToPNG();
                if (png == null || png.Length == 0)
                {
                    _backgroundScreenshotFailed = true;
                }
                else
                {
                    File.WriteAllBytes(_backgroundScreenshotPath, png);
                    _backgroundScreenshotCompleted = true;
                }
            }
        }
        catch (Exception)
        {
            _backgroundScreenshotFailed = true;
        }
        finally
        {
            if (screenshot != null)
            {
                Destroy(screenshot);
            }
        }
    }

    private static bool HasBackgroundScreenshotContent(Color32[] pixels)
    {
        if (pixels == null || pixels.Length == 0)
        {
            return false;
        }

        byte minimumRed = byte.MaxValue;
        byte minimumGreen = byte.MaxValue;
        byte minimumBlue = byte.MaxValue;
        byte maximumRed = byte.MinValue;
        byte maximumGreen = byte.MinValue;
        byte maximumBlue = byte.MinValue;
        for (int index = 0; index < pixels.Length; index++)
        {
            Color32 pixel = pixels[index];
            minimumRed = Math.Min(minimumRed, pixel.r);
            minimumGreen = Math.Min(minimumGreen, pixel.g);
            minimumBlue = Math.Min(minimumBlue, pixel.b);
            maximumRed = Math.Max(maximumRed, pixel.r);
            maximumGreen = Math.Max(maximumGreen, pixel.g);
            maximumBlue = Math.Max(maximumBlue, pixel.b);
        }

        return maximumRed - minimumRed >= 8 ||
               maximumGreen - minimumGreen >= 8 ||
               maximumBlue - minimumBlue >= 8;
    }

    private void FinishBackgroundSessionCheck(
        ReferenceBackgroundSessionCheckExit exit,
        int npcCount = 0,
        int entityCount = 0)
    {
        if (_backgroundSessionCheckCompleted)
        {
            return;
        }

        if (_worldGroundLootCheckRequested)
        {
            FinishWorldGroundLootCheck(null);
            return;
        }

        if (_npcTaskLifecycleCheckRequested)
        {
            FinishNpcTaskLifecycleCheck(
                _npcTaskLifecycleDriver,
                exit == ReferenceBackgroundSessionCheckExit.WorldOwnerFailed
                    ? ReferenceNpcTaskLifecycleJourneyExit.WorldRevisionRejected
                    : ReferenceNpcTaskLifecycleJourneyExit.InitialAuthorityUnavailable);
            return;
        }

        if (_monsterRealCombatTailCheckRequested)
        {
            FinishMonsterRealCombatTailCheck(null);
            return;
        }

        if (exit == ReferenceBackgroundSessionCheckExit.Success &&
            _localAvatarSourceCheckRequested &&
            !_localAvatarSourceCheckCompleted)
        {
            _backgroundSessionCheckCompleted = true;
            _accountName = string.Empty;
            _password = string.Empty;
            Debug.LogError(
                ReferenceLocalAvatarSourceCheck.FailureLine("result.missing"));
            UnityEngine.Application.Quit(79);
            return;
        }

        if (exit == ReferenceBackgroundSessionCheckExit.Success &&
            _inventoryEquipmentSourceCheckRequested &&
            !_inventoryEquipmentSourceCheckCompleted)
        {
            _backgroundSessionCheckCompleted = true;
            _accountName = string.Empty;
            _password = string.Empty;
            Debug.LogError(
                ReferenceInventoryEquipmentSourceCheck.FailureLine(
                    "result.missing"));
            UnityEngine.Application.Quit(79);
            return;
        }

        if (exit == ReferenceBackgroundSessionCheckExit.Success &&
            _audioSourceCheckRequested &&
            !_audioSourceCheckCompleted)
        {
            FinishAudioSourceCheck("result.missing");
            return;
        }

        if (exit == ReferenceBackgroundSessionCheckExit.Success &&
            _contentAuthorityCheckRequested)
        {
            string contentResult;
            using (ReferenceResourceLoader resources =
                ReferenceResourceLoader.OpenStreamingAssets())
            {
                if (!ReferenceContentAuthorityCheck.TryVerify(
                        resources,
                        out contentResult))
                {
                    _backgroundSessionCheckCompleted = true;
                    _accountName = string.Empty;
                    _password = string.Empty;
                    Debug.LogError(contentResult);
                    UnityEngine.Application.Quit(70);
                    return;
                }
            }

            Debug.Log(contentResult);
        }

        _backgroundSessionCheckCompleted = true;
        _accountName = string.Empty;
        _password = string.Empty;
        int exitCode = (int)exit;
        if (exit == ReferenceBackgroundSessionCheckExit.Success)
        {
            Debug.Log(
                ReferenceBackgroundSessionCheck.SuccessLine(
                    _world == null ? 0 : _world.MapId,
                    _backgroundRosterCount,
                    npcCount,
                    entityCount));
        }
        else
        {
            Debug.LogError(_backgroundMapTransitionCheckRequested
                ? ReferenceBackgroundMapTransitionCheck.CommonFailureLine(exit)
                : ReferenceBackgroundSessionCheck.FailureLine(exit));
        }

        UnityEngine.Application.Quit(exitCode);
    }

    private void FinishMonsterRealCombatTailCheck(
        ReferenceMonsterRealCombatTailDriver driver)
    {
        if (_backgroundSessionCheckCompleted)
        {
            return;
        }

        ReferenceMonsterRealCombatTailExit exit = driver == null
            ? ReferenceMonsterRealCombatTailExit.FixtureUnavailable
            : driver.Exit;
        if (driver == null)
        {
            _monsterRealCombatTailBootstrapFailurePending = true;
            _monsterRealCombatTailBootstrapWriteFailureReported = false;
            _monsterRealCombatTailBootstrapFailure = exit;
            TryFinishMonsterRealCombatTailBootstrapFailure();
            return;
        }

        CompleteMonsterRealCombatTailCheck(exit, driver);
    }

    private void FinishWorldGroundLootCheck(ReferenceWorldGroundLootDriver driver)
    {
        if (_backgroundSessionCheckCompleted)
        {
            return;
        }

        ReferenceWorldGroundLootExit exit = driver == null
            ? ReferenceWorldGroundLootExit.DropUnavailable
            : driver.Exit;
        if (driver == null)
        {
            ReferenceWorldGroundLootCheck.TryWriteBootstrapPartial(exit);
        }

        _backgroundSessionCheckCompleted = true;
        _accountName = string.Empty;
        _password = string.Empty;
        if (exit == ReferenceWorldGroundLootExit.Success)
        {
            Debug.Log(ReferenceWorldGroundLootCheck.ArtifactLine());
            Debug.Log(
                ReferenceWorldGroundLootCheck.SuccessLine(
                    driver == null ? 0 : driver.CheckpointCount));
        }
        else
        {
            Debug.Log(
                ReferenceWorldGroundLootCheck.PartialArtifactLine(
                    exit,
                    driver == null ? "BeforeDriver" : driver.PhaseName,
                    driver == null ? 0 : driver.CheckpointCount));
            Debug.LogError(ReferenceWorldGroundLootCheck.FailureLine(exit));
        }

        UnityEngine.Application.Quit((int)exit);
    }

    private async void FinishNpcTaskLifecycleCheck(
        ReferenceNpcTaskLifecycleJourneyDriver driver,
        ReferenceNpcTaskLifecycleJourneyExit requestedExit =
            ReferenceNpcTaskLifecycleJourneyExit.InitialAuthorityUnavailable)
    {
        if (_npcTaskLifecycleTerminalStarted)
        {
            return;
        }

        _npcTaskLifecycleTerminalStarted = true;
        _backgroundSessionCheckCompleted = true;
        ReferenceFirstWorldWindowOwner terminalWindow = _firstWorldWindow;
        if (driver != null && !driver.Terminal)
        {
            driver.FailFromWorldRevision(requestedExit);
        }

        Task<bool> cancellation = terminalWindow == null
            ? Task.FromResult(true)
            : terminalWindow.CancelAndAwaitNpcTaskLifecycleActionsAsync(
                ReferenceNpcTaskLifecycleJourneyCheck.TerminalCancellationTimeout);
        if (_backgroundSessionTimeout != null)
        {
            _backgroundSessionTimeout.Cancel();
        }
        ReleaseFirstWorldWindow();

        bool cancellationSettled;
        try
        {
            cancellationSettled = await cancellation;
        }
        catch (Exception)
        {
            cancellationSettled = false;
        }

        ReferenceNpcTaskLifecycleJourneyExit exit = driver == null
            ? (cancellationSettled
                ? requestedExit
                : ReferenceNpcTaskLifecycleJourneyExit.TerminalCancellationFailed)
            : driver.Exit;
        bool wrote = driver == null
            ? ReferenceNpcTaskLifecycleJourneyCheck.TryWriteBootstrapPartial(
                exit,
                cancellationSettled)
            : driver.TryWriteTerminalArtifact(cancellationSettled);
        if (driver != null)
        {
            exit = driver.Exit;
        }
        if (!wrote || !cancellationSettled)
        {
            exit = ReferenceNpcTaskLifecycleJourneyExit.TerminalCancellationFailed;
        }

        _accountName = string.Empty;
        _password = string.Empty;
        if (exit == ReferenceNpcTaskLifecycleJourneyExit.Success)
        {
            Debug.Log(ReferenceNpcTaskLifecycleJourneyCheck.ArtifactLine());
            Debug.Log(
                ReferenceNpcTaskLifecycleJourneyCheck.SuccessLine(
                    driver == null ? 0 : driver.CheckpointCount));
        }
        else
        {
            Debug.Log(
                ReferenceNpcTaskLifecycleJourneyCheck.PartialArtifactLine(
                    exit,
                    driver == null ? "BeforeDriver" : driver.PhaseName,
                    driver == null ? 0 : driver.CheckpointCount));
            Debug.LogError(ReferenceNpcTaskLifecycleJourneyCheck.FailureLine(exit));
        }

        _requestTerminalQuit?.Invoke((int)exit);
    }

    private void TryFinishMonsterRealCombatTailBootstrapFailure()
    {
        if (!_monsterRealCombatTailBootstrapFailurePending ||
            _backgroundSessionCheckCompleted)
        {
            return;
        }
        if (!ReferenceMonsterRealCombatTailCheck.TryWriteBootstrapPartial(
                _monsterRealCombatTailBootstrapFailure))
        {
            if (!_monsterRealCombatTailBootstrapWriteFailureReported)
            {
                _monsterRealCombatTailBootstrapWriteFailureReported = true;
                Debug.LogError(
                    ReferenceMonsterRealCombatTailCheck.Marker +
                    " PARTIAL_WRITE_FAILED path=" +
                    ReferenceMonsterRealCombatTailCheck.PartialArtifactFileName +
                    " phase=BeforeDriver");
            }
            return;
        }

        Debug.Log(
            ReferenceMonsterRealCombatTailCheck.PartialArtifactLine(
                _monsterRealCombatTailBootstrapFailure,
                "BeforeDriver",
                0));
        ReferenceMonsterRealCombatTailExit exit =
            _monsterRealCombatTailBootstrapFailure;
        _monsterRealCombatTailBootstrapFailurePending = false;
        CompleteMonsterRealCombatTailCheck(exit, null);
    }

    private void CompleteMonsterRealCombatTailCheck(
        ReferenceMonsterRealCombatTailExit exit,
        ReferenceMonsterRealCombatTailDriver driver)
    {
        _backgroundSessionCheckCompleted = true;
        _accountName = string.Empty;
        _password = string.Empty;
        if (exit == ReferenceMonsterRealCombatTailExit.Success)
        {
            Debug.Log(ReferenceMonsterRealCombatTailCheck.ArtifactLine());
            Debug.Log(
                ReferenceMonsterRealCombatTailCheck.SuccessLine(
                    driver.CheckpointCount));
        }
        else
        {
            Debug.LogError(
                ReferenceMonsterRealCombatTailCheck.FailureLine(exit));
        }

        UnityEngine.Application.Quit((int)exit);
    }

    private void FinishBackgroundMapTransitionCheck(
        ReferenceBackgroundMapTransitionCheckExit exit,
        int npcCount = 0,
        int entityCount = 0)
    {
        if (_backgroundSessionCheckCompleted)
        {
            return;
        }

        _backgroundSessionCheckCompleted = true;
        _accountName = string.Empty;
        _password = string.Empty;
        int exitCode = (int)exit;
        if (exit == ReferenceBackgroundMapTransitionCheckExit.Success)
        {
            Debug.Log(
                ReferenceBackgroundMapTransitionCheck.SuccessLine(
                    npcCount,
                    entityCount));
        }
        else
        {
            Debug.LogError(ReferenceBackgroundMapTransitionCheck.FailureLine(exit));
        }

        UnityEngine.Application.Quit(exitCode);
    }

    // The native page calls at the original control position. No end-of-page action collection,
    // extra busy gate, or second click-to-enter route sits between it and the existing writers.
    private void PerformCharacterSelectionAction(TitleCharacterAction action)
    {
        switch (action)
        {
            case TitleCharacterAction.Delete:
                // The native confirm arm invokes this writer before it closes the modal, using
                // the roster row the confirm click read: official 1.5.8 passes CharBrief +0x20
                // of the selected slot
                // (arm64 wbb-068-0x06000ef7-arm64-20260930/06000ef7.c:728-730), which is the
                // character name - the product roster's CharacterId is that same string, not a
                // separate account-scoped id.
                BeginDeleteCharacter(_titleCharacter.SelectedCharacterId);
                break;
            case TitleCharacterAction.ReturnToServerSelection:
                _session.ReturnToServerSelection();
                break;
            case TitleCharacterAction.Create:
                Result creation = _session.BeginCharacterCreation();
                if (!creation.IsSuccess)
                {
                    _status = SafeError(creation.Error);
                    break;
                }
                break;
        }
    }

    // Official 1.5.8 TitleFlow.NoteDisconnected (0x06000ec0), frozen bodies
    // Build/ReferenceReverseGhidra-1.5.8/queries/wbc119-copy-of-wbc116r-titleflow-arm64-20261002/
    // 06000ec0.c (0x381470, 404B) and .../x86_64-20261002/06000ec0.c (0x394520, 384B): one
    // straight line, three clear stores (state +0x28 = 0, loggingIn +0x258 = false, entering
    // +0x260 = false), then LastDropReason (+0xb0) behind Object.op_Inequality(I, null), then the
    // single netMsg (+0x250) store - usage 16972 + optional 10532 " (" + reason + 10857 ")";
    // field names/offsets and every literal are proved in Build/WBC-119-report.md §3. Runs on the
    // host thread inside the Update (0x0600077D) _dropped arm, after that same drain already ran
    // DisposeConnectionCore, and holds no lock, so it may touch session state safely.
    // The product split of this member is unchanged: the page/state store reaches the login page
    // only through the session transition, and the rest of the stores live in ApplyDisconnectedNote.
    internal void NoteDisconnectedForIndependentRuntime()
    {
        // GameBootstrap.OnNetDropped (0x060003f0) reaches this member on both of its arms. The two
        // frozen exports differ only in branch order, not in semantics: the body always builds its
        // own prompt from usage 16973 + optional 10391 "\n(" + reason + 10857 ")", then tests
        // worldRoot (+0xf8, atlas field GameBootstrap.worldRoot Transform @248) with
        // Object.op_Inequality — arm64 :74-88 goes to the non-null arm, x86_64 :60-78 tests equality
        // first and reaches the same non-null arm at :73-77 — and only when the test fails does it
        // GetComponent<TitleFlow>() and reach this member (usage 06975 MethodRef[44867],
        // arm64 :90-95 / x86_64 :66-70). The non-null arm hands two new Action(this,
        // DisconnectToLogin) (usage 07562 MethodDef GameBootstrap.DisconnectToLogin,
        // arm64 :83-86 / x86_64 :73-76) to
        // SysMsgBox.Show (0x06000e1c, static Void Show(String, Action, Action)) and then returns without touching the
        // title, so an in-world drop must do nothing here but open that box. Evidence:
        // Build/ReferenceReverseGhidra-1.5.8/queries/wb169-ondropped-{arm64,x86_64}/060003f0.c
        // (arm64 0x426534 / 664B, x86_64 0x43b8e0 / 576B); literal identities are the frozen atlas
        // usages 16973 = "与服务器的连接已断开，请重新登录。" and 10391 = "\n(" (exact).
        // This read and both concatenations precede the world/title receiver test in the
        // original, including the no-title arm. NoteDisconnected reads the reason again later.
        string reason = string.Empty;
        if (NetClient.I != null)
        {
            reason = NetClient.I.LastDropReasonForIndependentRuntime;
        }
        string message = string.Concat(
            "与服务器的连接已断开，请重新登录。",
            string.IsNullOrEmpty(reason) ? string.Empty : string.Concat("\n(", reason, ")"));

        // The original receiver is retained by the callback; only worldRoot uses Unity liveness.
        if (_gameBootstrap.HasWorldRootForIndependentRuntime)
        {
            SysMsgBox.Show(message, BeginDisconnectToLoginFromWorld, BeginDisconnectToLoginFromWorld);
            return;
        }

        // Resolve the actual component on the original GameBootstrap host. A retained
        // page adapter is not evidence that its Unity component still exists.
        TitleFlow title = _gameBootstrap.GetComponent<TitleFlow>();
        // Both native bodies test the returned managed reference directly, not Object equality.
        if (ReferenceEquals(title, null))
            return;

        // Store order is official: the page selector first, then the busy bytes, then the copy.
        _session?.NoteTransportDisconnected();
        ApplyDisconnectedNote(title);
    }

    // Both retained-title and newly added-title paths target the exact component.
    private void ApplyDisconnectedNote(TitleFlow title) => title.NoteDisconnected();

    private void DrawDiagnosticWindow(Action content)
    {
        Rect window = new Rect(
            Math.Max(16f, (Screen.width - WindowWidth) / 2f),
            Math.Max(16f, (Screen.height - 520f) / 2f),
            WindowWidth,
            520f);
        GUILayout.BeginArea(window, GUI.skin.window);
        GUILayout.Space(12f);
        GUILayout.Label("PROTOCOL DIAGNOSTIC ONLY — NOT PRODUCT UI", WarningStyle());
        GUILayout.Label("fake-fantasy-online", HeaderStyle());
        GUILayout.Label("Independent Unity client · local private-server target");
        GUILayout.Space(18f);
        content();
        GUILayout.FlexibleSpace();
        GUILayout.Space(12f);
        GUILayout.Label($"State: {_session.State}");
        GUILayout.TextArea(_status, GUILayout.MinHeight(58f));
        GUILayout.EndArea();
    }

    private void DrawCharacterAppearanceBlocked()
    {
        GUILayout.Label("Character surface blocked", HeaderStyle());
        GUILayout.Space(10f);
        GUILayout.Label(
            "The product character screen was not drawn because at least one server-provided " +
            "portrait cannot be recovered exactly from the reviewed assets.");
    }

    private void DrawWorld()
    {
        GUILayout.Label("Incomplete recovered world — diagnostic gate", HeaderStyle());
        GUILayout.Space(14f);
        GUILayout.Label($"Character: {_world.DisplayName}");
        GUILayout.Label($"Entity: {_world.EntityId}");
        GUILayout.Label($"Map: {_world.MapId}");
        GUILayout.Label($"Position: {_world.X:0.##}, {_world.Y:0.##}");
        GUILayout.Space(16f);
        GUILayout.Label(
            "The reviewed first-world source graph is unavailable. No substitute map or HUD " +
            "was invented.");
    }

    internal void BeginRefreshServerStatus()
    {
        // DrawServer owns the live receiver/Connected checks and writes nextStatAt first.
        // Its host-stack void call returns or throws through the existing native sender;
        // no view-lifetime admission, Task observation or second status message intervenes.
        _session.RequestServerOnlineCount();
    }

    internal void BeginEnterWorld(string characterName, Action<string> send)
    {
        // Preserve only the existing host diagnostic handoff. Native EnterGame already
        // owns the slot guards, entering latch, time and message at the button position.
        _loginDiagnosticsActive = false;
        send(characterName);
    }

    private async void BeginDeleteCharacter(string characterId)
    {
        if (_lifetime == null)
        {
            return;
        }

        // DrawCharSelect writes its delete message after invoking this existing command bridge.
        // It does not take entering/the shared busy flag. The session's single-reader operation
        // queue remains an explicit transport boundary for requests overlapping world entry.
        RecoveredTitleCharacterSurface source = _titleCharacter;
        _loginDiagnosticsActive = false; // Preserve the existing non-login-stage marker, without a busy latch.
        try
        {
            Result<IReadOnlyList<ServerCharacterRecord>> result =
                await _session.DeleteCharacterAsync(characterId, _lifetime.Token);
            if (!result.IsSuccess)
            {
                if (!_session.UsesNativeTitleResultReceipts ||
                    result.Error.Code != "character.delete.rejected")
                    _status = SafeError(result.Error);
                return;
            }

            ObserveTitleCharacterRosterResponse(result.Value, source);
            if (!_session.UsesNativeTitleResultReceipts) _status = string.Empty;
        }
        catch (OperationCanceledException)
        {
            _status = "Character deletion was cancelled.";
        }
        catch (Exception)
        {
            _status = "client.unexpected: Character deletion failed unexpectedly.";
        }
    }

    internal async void BeginCreateCharacter(CharacterCreationRequest request)
    {
        if (_creatingCharacter || _lifetime == null)
        {
            return;
        }

        RecoveredTitleCharacterSurface source = _titleCharacter;
        _creatingCharacter = true;
        // Preserve the existing host diagnostic route without changing native login state.
        _loginDiagnosticsActive = false;
        _status = "创建中...";
        try
        {
            Result<IReadOnlyList<ServerCharacterRecord>> result =
                await _session.CreateCharacterAsync(request, _lifetime.Token);
            if (!result.IsSuccess)
            {
                if (!_session.UsesNativeTitleResultReceipts ||
                    result.Error.Code != "character.create.rejected")
                    _status = result.Error.Message;
                return;
            }

            ObserveTitleCharacterRosterResponse(result.Value, source);
            if (!_session.UsesNativeTitleResultReceipts)
            {
                source.NativeOwnerForIndependentRuntime.SelectTitlePageForIndependentRuntime(5);
                _status = string.Empty;
            }
        }
        catch (OperationCanceledException)
        {
            _status = "创建角色已取消。";
        }
        catch (Exception)
        {
            _status = "创建角色失败。";
        }
        finally
        {
            _creatingCharacter = false;
        }
    }

    private void OnDestroy()
    {
        ReleaseFirstWorldWindow();
        if (_backgroundSessionTimeout != null)
        {
            _backgroundSessionTimeout.Cancel();
            _backgroundSessionTimeout.Dispose();
            _backgroundSessionTimeout = null;
        }
        _lifetime?.Cancel();
        _lifetime?.Dispose();
        _lifetime = null;
        _titleLogin?.ReleaseGeneratedLoginTexturesForIndependentRuntime();
        _titleLogin = null;
        // Unity destroys the TitleFlow attached to the GameBootstrap host and invokes
        // its callback/avatar cleanup; do not invoke that cleanup from this adapter.
        _titleCharacter = null;
        _titleCharacterCreation?.Dispose();
        _titleCharacterCreation = null;
    }

    private void ReleaseFirstWorldWindow()
    {
        if (_gameBootstrap != null)
        {
            if (_gameBootstrap.HasWorldStateForIndependentRuntime ||
                (_gameBootstrap.HasMailOwnerForIndependentRuntime && (_session == null || !_session.Connected)))
                _gameBootstrap.ReleaseWorld();
            _firstWorldWindow = null;
            _lastReportedWorldRevisionFailure = ReferenceWorldRevisionFailure.None;
        }
        else if (_firstWorldWindow != null)
        {
            _firstWorldWindow.Dispose();
            _firstWorldWindow = null;
            _lastReportedWorldRevisionFailure = ReferenceWorldRevisionFailure.None;
        }

        if (_session == null || _session.State != ClientSessionState.InWorld)
        {
            _worldBuildAttempted = false;
        }
    }

    private static string WorldFailureStatus(ReferenceFirstWorldWindowFailure failure)
    {
        return "world.reference.fail_closed." + failure +
               ": The reviewed first-world source owner stopped; no substitute world was shown.";
    }

    private static string WorldRevisionFailureStatus(
        ReferenceWorldRevisionFailure failure)
    {
        return "world.reference.revision_degraded." + failure +
               ": The last valid world remains visible; this revision is not accepted.";
    }

    internal static string SafeError(Error error)
    {
        return $"{error.Code}: {error.Message}";
    }

    /// <summary>
    /// True for the two codes only the transport connect produces ("transport.connect.failure"
    /// in PrivateServerGateway.ConnectAsync at its two return sites, "transport.connect.timeout"
    /// at its timeout site), i.e. for the failure stage that maps onto the ok == 0 arm of
    /// official &lt;DrawLogin&gt;b__0 (06000f0d), the arm that writes no netMsg at all. Codes
    /// from the stages around it — the dispatch lookup ("dispatch.*"), the login reply
    /// ("auth.*") and the roster ("roster.*") — have no native "writes nothing" evidence, so
    /// they keep the write.
    /// </summary>
    private static bool IsConnectStageFailure(Error error)
    {
        return error.Code == "transport.connect.failure" ||
               error.Code == "transport.connect.timeout";
    }

    private static GUIStyle HeaderStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        return style;
    }

    private static GUIStyle WarningStyle()
    {
        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            wordWrap = true
        };
        style.normal.textColor = new Color(1f, 0.72f, 0.2f, 1f);
        return style;
    }
}
}
