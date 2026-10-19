using System;
using System.Collections;
using System.IO;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;

namespace FakeFantasyOnline.Bootstrap
{

public sealed class ClientBootstrap : MonoBehaviour
{
    private NativeNetClientHost _unboundNetwork;
    private INetClientInstanceHost _networkHost;
    private ClientRootView _view;
    private IDisposable _plugins;
    private bool _terminalQuitRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreateCompositionRoot()
    {
#if UNITY_EDITOR
        // Only the explicit Editor preview runner sets this session-scoped flag.
        // The isolated scene must never create networking or addon listeners.
        if (UnityEditor.SessionState.GetBool("FakeFantasyOnline.MobileHudPreview.Active", false))
            return;
#endif
        if (UnityEngine.Object.FindAnyObjectByType<ClientBootstrap>() != null)
        {
            return;
        }

        GameObject root = new GameObject("FakeFantasyOnline");
        DontDestroyOnLoad(root);
        root.AddComponent<ClientBootstrap>();
    }


    private void Awake()
    {
        if (UnityEngine.Application.isMobilePlatform)
        {
            // Apply before resource loading and title creation; only landscape is playable.
            Screen.autorotateToPortrait = false;
            Screen.autorotateToPortraitUpsideDown = false;
            Screen.autorotateToLandscapeLeft = true;
            Screen.autorotateToLandscapeRight = true;
            Screen.orientation = ScreenOrientation.AutoRotation;
        }
        ClientResourcePaths.UseBundled();
        ClientUpdateSession.Reset();
        StartCoroutine(InitializeWithUpdateGate());
    }

    private IEnumerator InitializeWithUpdateGate()
    {
        var installer = gameObject.AddComponent<MobileResourceInstaller>();
        IEnumerator installation = installer.Install();
        while (true)
        {
            object next;
            try
            {
                if (!installation.MoveNext()) break;
                next = installation.Current;
            }
            catch (Exception exception) { installer.Fail(exception); yield break; }
            yield return next;
        }
        // Show the loading phase before synchronous client/title initialization.
        yield return null;
        try
        {
            PrivateServerOptions options;
            if (installer.ResourceFree) options = PrivateServerOptions.MobileConfigured(installer.DispatchUrl);
            else if (UnityEngine.Application.isMobilePlatform)
                options = PrivateServerOptions.MobileConfigured(File.ReadAllText(
                    Path.Combine(ClientResourcePaths.InstalledRoot, "mobile-dispatch.txt")).Trim());
            else options = PrivateServerOptions.LocalDefault();
            InitializeClient(options, installer);
        }
        catch (Exception exception) { installer.Fail(exception); }
    }

    private void InitializeClient(PrivateServerOptions options, MobileResourceInstaller startup = null)
    {
        NoticeBoard.ConfigureEndpointForIndependentRuntime(options.NoticeUri.AbsoluteUri);
        // Before title creation the native test-world path still has its original
        // unbound session. SetupNet transfers this graph to its first actual owner.
        _unboundNetwork = new NativeNetClientHost(options);
        _view = gameObject.AddComponent<ClientRootView>();
        _view.Initialize(_unboundNetwork.Session, RequestTerminalQuit, options.NoticeUri);
        _unboundNetwork.ObserveUnboundView(_view);
        try { _plugins = OptionalPluginRuntime.Create(gameObject, _view); }
        catch (Exception exception)
        {
            // An optional addon failure must not abort the native title/network composition.
            Debug.LogWarning("Plugin runtime initialization failed: " + exception.Message);
            _plugins?.Dispose();
            _plugins = null;
        }
        _view.BindTitleNetworkSetupForIndependentRuntime(() =>
        {
            if (_terminalQuitRequested || this == null) return;
            EnsureNativeNetworkOwner(options);
            _view.SetupNativeTitleNetworkForIndependentRuntime();
        }, () => _networkHost.SendTitleWorldReturn());
        _view.WorldBootstrapForIndependentRuntime.BindTitleStartupForIndependentRuntime(() =>
        {
            if (_terminalQuitRequested || this == null) return;
            try
            {
                var title = _view.InitializeTitleForIndependentRuntime();
                if (_terminalQuitRequested || this == null) return;
                _view.BindTitleBootstrapForIndependentRuntime(title);
                if (startup != null) startup.Complete();
            }
            catch (Exception exception)
            {
                if (startup == null) throw;
                startup.Fail(exception);
            }
        });
    }

    private void EnsureNativeNetworkOwner(PrivateServerOptions options)
    {
        var owner = FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.I;
        NativeNetClientHost created = null;
        if (owner == null)
        {
            // Official SetupNet: Unity-null I -> new GameObject("NetClient") -> AddComponent.
            // The existing seven Protocol field initializers run for this new instance,
            // before the component's Awake publishes I. No old core is retargeted.
            GameObject host = new GameObject("NetClient");
            created = _unboundNetwork != null && !_unboundNetwork.HasOwner
                ? _unboundNetwork : new NativeNetClientHost(options);
            host.AddComponent<FakeFantasyOnline.Presentation.ReferenceAssets.NetClient>();
            owner = FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.I;
        }
        if (owner.InstanceHostForIndependentRuntime == null)
        {
            created = created ?? (_unboundNetwork != null && !_unboundNetwork.HasOwner
                ? _unboundNetwork : new NativeNetClientHost(options));
            created.BindOwner(owner, _view);
            if (ReferenceEquals(_unboundNetwork, created)) _unboundNetwork = null;
        }
        INetClientInstanceHost selected = owner.InstanceHostForIndependentRuntime;
        if (!ReferenceEquals(_networkHost, selected)) _networkHost?.DetachView(_view);
        _networkHost = selected;
        selected.AttachView(_view);
        // An already-live foreign owner keeps its own graph. The unused initial graph
        // has never owned a Unity component and can be released without closing I.
        _unboundNetwork?.ReleaseUnbound();
        _unboundNetwork = null;
    }

    private void RequestTerminalQuit(int exitCode)
    {
        if (_terminalQuitRequested) return;
        _terminalQuitRequested = true;
        _plugins?.Dispose();
        UnityEngine.Application.Quit(exitCode);
    }

    private void OnDestroy()
    {
        _plugins?.Dispose();
        _plugins = null;
        // Each NetClient owns its Close, coroutines, session and executor. Destroying
        // this view cannot release a persistent or foreign component's transport.
        _networkHost?.DetachView(_view);
        _unboundNetwork?.ReleaseUnbound();
        _unboundNetwork = null;
        _networkHost = null;
    }
}
}
