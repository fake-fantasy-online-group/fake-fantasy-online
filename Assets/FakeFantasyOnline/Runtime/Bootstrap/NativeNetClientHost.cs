using System;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Threading;
using FakeFantasyOnline.Application;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;
using FakeFantasyOnline.Presentation;
using FakeFantasyOnline.Presentation.ReferenceAssets;
using UnityEngine;
using NativeNetClient = FakeFantasyOnline.Presentation.ReferenceAssets.NetClient;

namespace FakeFantasyOnline.Bootstrap
{
// One Unity owner, one immutable gateway/core/session, one frame driver. A new owner
// gets a fresh graph; retaining an old owner never redirects its writes or Close.
internal sealed class NativeNetClientHost : INetClientInstanceHost
{
    private readonly PrivateServerOptions _options;
    private readonly PrivateServerGateway _gateway;
    private readonly SynchronizationContextHostExecutor _hostExecutor;
    private NativeNetClient _owner;
    private ClientRootView _view;
    private ReferenceCharacterSkillUsePort _characterSkillUse;
    private IEnumerator _addressIterator;
    private Coroutine _addressCoroutine;
    private IEnumerator _versionIterator;
    private Coroutine _versionCoroutine;
    private bool _released;

    public ClientSessionCoordinator Session { get; }
    internal bool HasOwner => !ReferenceEquals(_owner, null);

    internal NativeNetClientHost(PrivateServerOptions options)
    {
        _options = options;
        _hostExecutor = new SynchronizationContextHostExecutor(
            SynchronizationContext.Current, Thread.CurrentThread.ManagedThreadId);
        _gateway = new PrivateServerGateway(
            options,
            message => Debug.LogWarning(message),
            _hostExecutor,
            () => UnityEngine.Time.realtimeSinceStartup,
            new LoadTraceReceivePort(),
            () => FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.DispatchMsPerFrame,
            // Official 0x8044 and 0x804c write their static owners inside the Dispatch case
            // body. The owner adapter is stateless - it only writes CombatWorld.LocalFaction and
            // the CharData title statics - so it can be bound before the world view exists.
            // It also supplies ISkillBookReceiveOwner: both Pump paths therefore write the
            // same CharData skill roots at receipt, including before SelfData/world creation.
            // Notice receipt resolves the current host dialog lazily; the context-free Pump
            // therefore uses the same live owner as ClientRootView, including world replacement.
            new FakeFantasyOnline.Presentation.ReferenceAssets.ReferenceWorldReceiveOwner(
                () => _view != null ? _view.NpcDialogAtReceiptForIndependentRuntime() : null)
            {
                MailOwnerAtReceiptForIndependentRuntime = () => _view,
                SkillCastOwnerAtReceiptForIndependentRuntime = () => _view
            },
            // Official 0x8017 asks the map question once per packet, after its U16 count and
            // before the row loop: CustomMaps.IsCustom (0x060001d0) over GameBootstrap.CurMapId
            // (field token 67110216). Presentation owns both members, so this lambda is the one
            // real implementation and Protocol receives only the Func<bool> probe. Nothing
            // evaluates it here: the read invokes it inside its own host action, so the drop
            // test always sees the map id the host holds at that moment - including the startup
            // 0x8017 that is applied before GameBootstrap.BuildWorld assigns any value.
            () => CustomMaps.IsCustom(GameBootstrap.CurMapId),
            // Only bind capabilities here. Login/Register invoke them on the existing host
            // executor after Send's connected gate, in native payload order. No value is
            // sampled during Awake, and detector reasons never leave the writer's local.
            new NetClientLoginEnvironment(
                () => SystemInfo.deviceUniqueIdentifier,
                VmDetect.Detect,
                DebugDetect.Detect));
        _gateway.BindAddressResolutionForIndependentRuntime(
            () => new UnityAddressRequest(options.DispatchUri.AbsoluteUri),
            JsonUtility.FromJson<ClientConfig.Dispatch>,
            seconds => new WaitForSeconds(seconds),
            () => FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.Servers,
            servers => FakeFantasyOnline.Presentation.ReferenceAssets.NetClient.Servers = servers);
        _gateway.ConfigureLocalServerForIndependentRuntime(options.LocalServer);
        IServerAddressDiscovery discovery = _gateway.CreateAddressDiscoveryForIndependentRuntime();
        Session = new ClientSessionCoordinator(discovery, _gateway, new InProcessEventBus());

    }

    internal void ObserveUnboundView(ClientRootView view)
    {
        _view = view;
        view.BindStartupAddressResultForIndependentRuntime(
            () => _gateway.HasStartupAddressResultForIndependentRuntime);
    }

    internal void BindOwner(NativeNetClient owner, ClientRootView view)
    {
        if (HasOwner) throw new InvalidOperationException("The network graph already has an owner.");
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        // Bind Close first so a later adapter allocation failure cannot orphan the core.
        owner.BindCloseForIndependentRuntime(CloseAndRelease);
        owner.BindInstanceForIndependentRuntime(_gateway, this, Pump);
        _characterSkillUse = new ReferenceCharacterSkillUsePort(
            owner, Session, _gateway, () => _view != null ? _view.WorldBootstrapForIndependentRuntime : null);
        Session.BindCharacterSkillUsePort(_characterSkillUse);
        owner.BindTitleLoginForIndependentRuntime(
            _gateway.ConnectTitleLoginForIndependentRuntime, _gateway.SendTitleLoginForIndependentRuntime,
            _gateway.CancelTitleLoginForIndependentRuntime);
        Action<string> sendEntry = _gateway.SendTitleEntryForIndependentRuntime;
        owner.BindTitleEntryForIndependentRuntime(name =>
        {
            if (_view != null) _view.BeginEnterWorld(name, sendEntry);
            else sendEntry(name);
        });
        owner.BindTitleEntryPrefixForIndependentRuntime(
            _gateway.ClearTitleEntryNoticeForIndependentRuntime,
            _gateway.SendTitleEntryCapsForIndependentRuntime);
        owner.BindLastDropReasonReaderForIndependentRuntime(
            () => _gateway.LastDropReasonForIndependentRuntime);
        _view = null;
        AttachView(view);

        // Retain the established startup order on this exact owner.
        _addressIterator = _gateway.ResolveServerAddressForIndependentRuntime();
        _addressCoroutine = owner.StartCoroutine(_addressIterator);
        Uri versionManifestUri =
            UnityEngine.Application.platform == RuntimePlatform.OSXPlayer ||
            UnityEngine.Application.platform == RuntimePlatform.OSXEditor
                ? _options.VersionManifestUriMac : _options.VersionManifestUriWin;
        if (versionManifestUri != null)
        {
            owner.BindVersionManifestsForIndependentRuntime(
                _options.VersionManifestUriMac?.AbsoluteUri,
                _options.VersionManifestUriWin?.AbsoluteUri);
            _versionIterator = owner.CheckVersion();
            _versionCoroutine = owner.StartCoroutine(_versionIterator);
        }
    }

    public void AttachView(ClientRootView view)
    {
        if (_released) throw new ObjectDisposedException(nameof(NativeNetClientHost));
        if (ReferenceEquals(_view, view)) return;
        if (_view != null) _view.GetComponent<EndgamePanel>()?.Unbind(_gateway);
        _view = view ?? throw new ArgumentNullException(nameof(view));
        var endgame = view.GetComponent<EndgamePanel>();
        if (endgame == null) endgame = view.gameObject.AddComponent<EndgamePanel>();
        endgame.Bind(_gateway);
        view.BindNetworkInstanceForIndependentRuntime(
            this, _characterSkillUse.ProjectCurrentStateForIndependentRuntime,
            () => _gateway.HasStartupAddressResultForIndependentRuntime);
        _gateway.BindDisconnectedNotificationForIndependentRuntime(
            view.NoteDisconnectedForIndependentRuntime);
    }

    public void DetachView(ClientRootView view)
    {
        // A different composition root may already have borrowed the live owner.
        if (ReferenceEquals(_view, view)) { view.GetComponent<EndgamePanel>()?.Unbind(_gateway); _view = null; }
    }

    public void SendTitleWorldReturn() => _gateway.SendTitleWorldReturnForIndependentRuntime();

    private void Pump()
    {
        if (_released) return;
        if (_view != null) Session.Pump(_view);
        else Session.Pump();
        _gateway.TickEndgameForIndependentRuntime();
    }

    private void CloseAndRelease()
    {
        if (_released) return;
        _released = true;
        Exception failure = null;
        try { _gateway.CloseNativeForIndependentRuntime(); }
        catch (Exception exception) { failure = exception; }
        Release(ref failure, true);
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    internal void ReleaseUnbound()
    {
        if (HasOwner || _released) return;
        _released = true;
        Exception failure = null;
        Release(ref failure, false);
        if (failure != null) ExceptionDispatchInfo.Capture(failure).Throw();
    }

    private void Release(ref Exception failure, bool nativeCloseOwnedByComponent)
    {
        try
        {
            if (_owner != null && _addressCoroutine != null) _owner.StopCoroutine(_addressCoroutine);
        }
        catch (Exception exception) { if (failure == null) failure = exception; }
        try { (_addressIterator as IDisposable)?.Dispose(); }
        catch (Exception exception) { if (failure == null) failure = exception; }
        _addressCoroutine = null;
        _addressIterator = null;
        try
        {
            if (_owner != null && _versionCoroutine != null) _owner.StopCoroutine(_versionCoroutine);
        }
        catch (Exception exception) { if (failure == null) failure = exception; }
        try { (_versionIterator as IDisposable)?.Dispose(); }
        catch (Exception exception) { if (failure == null) failure = exception; }
        _versionCoroutine = null;
        _versionIterator = null;
        try { _characterSkillUse?.Dispose(); }
        catch (Exception exception) { if (failure == null) failure = exception; }
        _characterSkillUse = null;
        try
        {
            if (nativeCloseOwnedByComponent)
                Session.DisposeWithGatewayReleaseForIndependentRuntime(_gateway.DisposeForNativeOwnerDestruction);
            else Session.Dispose();
        }
        catch (Exception exception) { if (failure == null) failure = exception; }
        try { _gateway.CompleteHostShutdownForIndependentRuntime(); }
        catch (Exception exception) { if (failure == null) failure = exception; }
        try { _hostExecutor.Dispose(); }
        catch (Exception exception) { if (failure == null) failure = exception; }
        _view = null;
    }
}
}
