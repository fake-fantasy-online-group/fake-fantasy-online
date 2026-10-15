using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using FakeFantasyOnline.Protocol;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using NativeNetClient = FakeFantasyOnline.Presentation.ReferenceAssets.NetClient;

namespace FakeFantasyOnline.Editor
{
/// <summary>
/// Editor-only, offline visual inspection of the actual native world/HUD. No login,
/// server receipts, authenticated state, plugins or transport owner are constructed.
/// Run in a graphical Editor (without -quit); captures contain local sample data.
/// </summary>
[InitializeOnLoad]
public static class MobileHudPreview
{
    private const string Key = "FakeFantasyOnline.MobileHudPreview.";
    private const BindingFlags Members = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static;
    private static readonly Vector2Int[] Sizes = { new Vector2Int(2616, 1212), new Vector2Int(2340, 1080) };
    private static GameBootstrap bootstrap;
    private static ClientSessionCoordinator session;
    private static PreviewHost host;
    private static EditorWindow gameView;
    private static int stage, settledFrame;
    private static double stageStarted;
    private static bool seeded, capturePending, finishing;
    private static string capturePath, failure;

    [Serializable] private sealed class SavedScenes { public SceneSetup[] scenes; }

    static MobileHudPreview()
    {
        EditorApplication.playModeStateChanged += OnPlayMode;
        if (SessionState.GetBool(Key + "Active", false))
            EditorApplication.update += Tick;
    }

    [MenuItem("Tools/Fake Fantasy Online/Capture Offline Mobile HUD")]
    public static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("Stop Play Mode before starting the offline HUD preview.");
        if (UnityEngine.Application.isBatchMode ||
            SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
            throw new InvalidOperationException("HUD capture needs the graphical Unity Editor, without -batchmode or -nographics.");
        for (int index = 0; index < SceneManager.sceneCount; index++)
            if (SceneManager.GetSceneAt(index).isDirty)
                throw new InvalidOperationException("Save or close modified scenes before preview capture; no scene will be overwritten.");

        string output = Argument("-ffoMobilePreviewOutput");
        if (string.IsNullOrEmpty(output))
            output = EditorUtility.OpenFolderPanel("Save offline mobile HUD screenshots", "", "");
        if (string.IsNullOrEmpty(output)) return;
        output = Path.GetFullPath(output);
        Directory.CreateDirectory(output);
        SessionState.SetString(Key + "Output", output);
        SessionState.SetBool(Key + "Exit", HasArgument("-ffoPreviewExit"));
        SessionState.SetBool(Key + "Inventory", HasArgument("-ffoPreviewInventory"));
        SessionState.SetString(Key + "Scenes", JsonUtility.ToJson(new SavedScenes
        { scenes = EditorSceneManager.GetSceneManagerSetup() }));
        SessionState.SetBool(Key + "OldOptionsEnabled", EditorSettings.enterPlayModeOptionsEnabled);
        SessionState.SetInt(Key + "OldOptions", (int)EditorSettings.enterPlayModeOptions);
        // A real domain/scene reload keeps all sample statics confined to this preview run.
        EditorSettings.enterPlayModeOptionsEnabled = false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Key + "Active", true);
        SessionState.SetInt(Key + "ExitCode", 1);
        try
        {
            SetGameViewSize(Sizes[0], saveOriginal: true);
            EditorApplication.EnterPlaymode();
        }
        catch
        {
            RestoreEditor();
            throw;
        }
    }

    private static void OnPlayMode(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Key + "Active", false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            stageStarted = EditorApplication.timeSinceStartup;
            EditorApplication.update -= Tick;
            EditorApplication.update += Tick;
            UnityEngine.Application.logMessageReceived += OnLog;
            SetGameViewSize(Sizes[0], saveOriginal: false);
        }
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            EditorApplication.update -= Tick;
            UnityEngine.Application.logMessageReceived -= OnLog;
            host?.AllowDisposal();
            session?.Dispose();
            session = null;
            MobileClientPresentation.Simulate = false;
            MobileClientPresentation.MenuExpanded = false;
            MobileClientPresentation.ChatExpanded = false;
        }
        if (state == PlayModeStateChange.EnteredEditMode) RestoreEditor();
    }

    private static void Tick()
    {
        if (!EditorApplication.isPlaying || finishing) return;
        try
        {
            if (!string.IsNullOrEmpty(failure)) throw new InvalidOperationException(failure);
            if (EditorApplication.timeSinceStartup - stageStarted > 240)
                throw new TimeoutException("Preview did not produce a ready rendered frame: " +
                    (bootstrap == null ? "waiting for exact Game View size" : bootstrap.LastBuildFailure.ToString()));
            if (NativeNetClient.I != null || UnityEngine.Object.FindAnyObjectByType<ClientRootView>() != null && bootstrap == null)
                throw new InvalidOperationException("A non-preview client/network owner exists; stopping before capture.");
            gameView?.Repaint();
            EditorApplication.QueuePlayerLoopUpdate();
            Vector2Int size = Sizes[stage / CapturesPerSize];
            if (Screen.width != size.x || Screen.height != size.y) return;

            if (bootstrap == null)
            {
                CreateOfflineWorld();
                return;
            }
            if (session.Connected || session.State != ClientSessionState.Offline)
                throw new InvalidOperationException("Preview session must remain offline and unauthenticated.");
            if (bootstrap.Window == null || !bootstrap.Window.Built || GameHud.I == null) return;
            if (!seeded)
            {
                SeedVisibleSamples();
                seeded = true;
                PrepareStage();
                return;
            }
            if (Time.frameCount - settledFrame < 30 || EditorApplication.timeSinceStartup - stageStarted < 2) return;
            if (!capturePending)
            {
                string state = stage % CapturesPerSize == 0 ? "collapsed" :
                    stage % CapturesPerSize == 1 ? "expanded" : "inventory";
                capturePath = Path.Combine(SessionState.GetString(Key + "Output", ""),
                    "mobile-hud-preview-" + size.x + "x" + size.y + "-" + state + ".png");
                if (File.Exists(capturePath))
                    throw new IOException("Capture already exists; choose an empty output directory: " + capturePath);
                ScreenCapture.CaptureScreenshot(capturePath);
                capturePending = true;
                return;
            }
            if (!File.Exists(capturePath) || new FileInfo(capturePath).Length < 24) return;
            byte[] png = File.ReadAllBytes(capturePath);
            if (PngInt(png, 16) != size.x || PngInt(png, 20) != size.y)
                throw new InvalidOperationException("Captured PNG size differs from the requested Game View size.");
            Debug.Log("FFO_MOBILE_PREVIEW_CAPTURE=" + capturePath);
            stage++;
            if (stage == Sizes.Length * CapturesPerSize)
            {
                File.WriteAllText(Path.Combine(SessionState.GetString(Key + "Output", ""), "preview-info.txt"),
                    "Actual Unity Game View rendering of the current native HUD and local assets.\n" +
                    "Offline visual preview with sample avatar, skills and chat; not connected gameplay or a server/account test.\n" +
                    "No login, dispatch request, network owner or plugin runtime was started.\n");
                Finish(0);
            }
            else PrepareStage();
        }
        catch (Exception exception)
        {
            Debug.LogError("FFO_MOBILE_PREVIEW_FAILED: " + exception);
            Finish(1);
        }
    }

    private static int CapturesPerSize => SessionState.GetBool(Key + "Inventory", false) ? 3 : 2;

    private static void CreateOfflineWorld()
    {
        MobileClientPresentation.Simulate = true;
        host = new PreviewHost();
        // This gateway supplies its genuine empty read models. It never receives packets,
        // owns a Unity NetClient, or gets a host capable of executing a connection/send.
        var options = new PrivateServerOptions(new Uri("http://127.0.0.1:1/offline-preview"), "LOCAL-UI-PREVIEW");
        var gateway = new PrivateServerGateway(options, host, () => Time.realtimeSinceStartup);
        session = new ClientSessionCoordinator(new NoDiscovery(), gateway, new InProcessEventBus());
        var root = new GameObject("Offline mobile UI preview (sample data)");
        var view = root.AddComponent<ClientRootView>();
        view.Initialize(session, code => { failure = "Unexpected terminal request: " + code; }, options.NoticeUri);
        bootstrap = view.WorldBootstrapForIndependentRuntime;
        bootstrap.testAccount = true;
        bootstrap.testName = "本地预览侠客";
        bootstrap.testLevel = 42;
        GameBootstrap.CurMapId = 7;
        // Editor-only direct presentation setup, never a forged server snapshot/receipt.
        SetCharacter("hp", 8240); SetCharacter("maxHp", 10000);
        SetCharacter("mp", 2640); SetCharacter("maxMp", 3200);
        SetCharacter("exp", 4200); SetCharacter("expToNext", 10000);
        SetCharacter("attrs", new int[64]);
        SetCharacter("attrsTotal", new int[64]);
    }

    private static void SeedVisibleSamples()
    {
        int[] ids = { 10002, 10003, 10004, 10005 };
        SetCharacter("skills", new Dictionary<int, int>
        { { ids[0], 1 }, { ids[1], 1 }, { ids[2], 1 }, { ids[3], 1 } });
        GameHud.LoadHotbar(ids, new int[4], false);
        Type chat = typeof(GameHud).Assembly.GetType("FakeFantasyOnline.Presentation.ReferenceAssets.ChatUI", true);
        MethodInfo add = chat.GetMethod("Add", Members);
        add.Invoke(null, new object[] { "系统", "本地样本预览（非联网实玩）", new Color(1f, .90f, .63f), "", null });
        add.Invoke(null, new object[] { "附近", "欢迎来到桃花源", Color.white, "预览旅人", null });
        add.Invoke(null, new object[] { "队伍", "技能与聊天为本地界面样本", new Color(.55f, 1f, .65f), "预览队友", null });
    }

    private static void PrepareStage()
    {
        int state = stage % CapturesPerSize;
        MobileClientPresentation.MenuExpanded = state == 1;
        MobileClientPresentation.ChatExpanded = false;
        if (bootstrap.Window.InventoryDialog.Visible) bootstrap.Window.InventoryDialog.Toggle();
        if (state == 2) bootstrap.Window.InventoryDialog.Toggle();
        SetGameViewSize(Sizes[stage / CapturesPerSize], saveOriginal: false);
        capturePending = false;
        settledFrame = Time.frameCount;
        stageStarted = EditorApplication.timeSinceStartup;
    }

    private static void SetCharacter(string name, object value)
    {
        typeof(GameHud).Assembly.GetType("FakeFantasyOnline.Presentation.ReferenceAssets.CharData", true)
            .GetField(name, Members).SetValue(null, value);
    }

    // Unity exposes no public fixed-resolution Game View API. Reflection is limited to
    // its Editor types; the resulting Screen and PNG dimensions are checked before use.
    private static void SetGameViewSize(Vector2Int size, bool saveOriginal)
    {
        Assembly editor = typeof(EditorWindow).Assembly;
        Type viewType = editor.GetType("UnityEditor.GameView", true);
        gameView = EditorWindow.GetWindow(viewType);
        PropertyInfo selected = viewType.GetProperty("selectedSizeIndex", Members);
        if (saveOriginal) SessionState.SetInt(Key + "OldSize", (int)selected.GetValue(gameView));
        Type sizesType = editor.GetType("UnityEditor.GameViewSizes", true);
        object sizes = typeof(ScriptableSingleton<>).MakeGenericType(sizesType)
            .GetProperty("instance", Members).GetValue(null);
        Type groupType = editor.GetType("UnityEditor.GameViewSizeGroupType", true);
        object groupId = sizesType.GetProperty("currentGroupType", Members).GetValue(sizes);
        object group = sizesType.GetMethod("GetGroup", Members, null, new[] { groupType }, null)
            .Invoke(sizes, new[] { groupId });
        Type groupClass = group.GetType();
        int count = (int)groupClass.GetMethod("GetTotalCount", Members).Invoke(group, null);
        int index = -1;
        for (int i = 0; i < count; i++)
        {
            object candidate = groupClass.GetMethod("GetGameViewSize", Members).Invoke(group, new object[] { i });
            Type type = candidate.GetType();
            if ((int)type.GetProperty("width", Members).GetValue(candidate) == size.x &&
                (int)type.GetProperty("height", Members).GetValue(candidate) == size.y)
            { index = i; break; }
        }
        if (index < 0)
        {
            Type sizeType = editor.GetType("UnityEditor.GameViewSize", true);
            Type kind = editor.GetType("UnityEditor.GameViewSizeType", true);
            object fixedSize = Activator.CreateInstance(sizeType, Members, null,
                new[] { Enum.Parse(kind, "FixedResolution"), (object)size.x, size.y, "Offline mobile HUD preview" }, null);
            groupClass.GetMethod("AddCustomSize", Members).Invoke(group, new[] { fixedSize });
            index = count;
        }
        selected.SetValue(gameView, index);
        gameView.Show(); gameView.Focus(); gameView.Repaint();
    }

    private static void Finish(int exitCode)
    {
        if (finishing) return;
        finishing = true;
        SessionState.SetInt(Key + "ExitCode", exitCode);
        EditorApplication.ExitPlaymode();
    }

    private static void RestoreEditor()
    {
        bool exit = SessionState.GetBool(Key + "Exit", false);
        int code = SessionState.GetInt(Key + "ExitCode", 1);
        SessionState.SetBool(Key + "Active", false);
        EditorApplication.update -= Tick;
        EditorSettings.enterPlayModeOptions = (EnterPlayModeOptions)SessionState.GetInt(Key + "OldOptions", 0);
        EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + "OldOptionsEnabled", false);
        var saved = JsonUtility.FromJson<SavedScenes>(SessionState.GetString(Key + "Scenes", "{}"));
        if (saved?.scenes != null && saved.scenes.Length > 0 && !string.IsNullOrEmpty(saved.scenes[0].path))
            EditorSceneManager.RestoreSceneManagerSetup(saved.scenes);
        else EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Type viewType = typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
        EditorWindow view = EditorWindow.GetWindow(viewType);
        viewType.GetProperty("selectedSizeIndex", Members).SetValue(view, SessionState.GetInt(Key + "OldSize", 0));
        if (exit) EditorApplication.delayCall += () => EditorApplication.Exit(code);
    }

    private static int PngInt(byte[] png, int offset) =>
        (png[offset] << 24) | (png[offset + 1] << 16) | (png[offset + 2] << 8) | png[offset + 3];
    private static void OnLog(string text, string trace, LogType type)
    { if (type == LogType.Exception || type == LogType.Error) failure = text; }
    private static bool HasArgument(string name) => Array.IndexOf(Environment.GetCommandLineArgs(), name) >= 0;
    private static string Argument(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, name);
        return index >= 0 && index + 1 < args.Length ? args[index + 1] : null;
    }

    private sealed class NoDiscovery : IServerAddressDiscovery
    {
        public Task<Result<IReadOnlyList<ServerDirectoryEntry>>> DiscoverAsync(CancellationToken token) =>
            throw new InvalidOperationException("Server discovery is forbidden in the offline visual preview.");
    }
    private sealed class PreviewHost : IHostExecutor
    {
        private bool disposing;
        public bool IsHostThread => disposing;
        public void AllowDisposal() { disposing = true; }
        public Task RunAsync(Action action, CancellationToken token) =>
            throw new InvalidOperationException("Connection/action execution is forbidden in the offline visual preview.");
    }
}
}
