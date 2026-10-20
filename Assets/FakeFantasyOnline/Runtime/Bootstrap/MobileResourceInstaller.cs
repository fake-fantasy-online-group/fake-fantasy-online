using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Presentation;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Bootstrap
{
/// <summary>Cross-platform, asset-independent update gate. Keeps the title behind verified resources.</summary>
public sealed partial class MobileResourceInstaller : MonoBehaviour
{
    private string status = "准备更新";
    private string detail = "读取启动配置…";
    private long completedBytes, totalBytes;
    private bool measurable, failed, terminalFailure;
    private Camera clearCamera;
    private GUIStyle titleStyle, statusStyle, detailStyle, fieldStyle, buttonStyle;
    private bool resourceFree, awaitingChoice, downloadRequested, repairRequested, enterRequested, bundledRequested;
    private bool allowBundled, ready, configurationOpen, configurationRequired, allowDevelopmentHttp;
    private string draftDispatchUrl = "", configurationError = "", conciseError = "";
    private Texture2D buttonTexture, buttonHoverTexture, repairIcon, configureIcon;
    private readonly ClientDownloadRate downloadRate = new ClientDownloadRate();
    private double? downloadBytesPerSecond;
    private string dispatchUrl = "", verifiedDispatchUrl = "", verifiedRoot;
    private ClientUpdatePlan updatePlan;
    private CancellationTokenSource remoteCancellation;
    private readonly object remoteProgressLock = new object();
    private string remoteStatus, remoteDetail;
    private long remoteDone, remoteTotal;
    private bool resourceTransferProgress;
    public bool ResourceFree { get { return resourceFree; } }
    public string DispatchUrl { get; private set; }

    private void Awake()
    {
        // The boot scene has no camera. A label alone leaves the Android backbuffer
        // undefined while hashing; explicitly clear it from the first rendered frame.
        var host = new GameObject("Mobile startup clear camera");
        host.transform.SetParent(transform, false);
        clearCamera = host.AddComponent<Camera>();
        clearCamera.clearFlags = CameraClearFlags.SolidColor;
        clearCamera.backgroundColor = new Color(0.035f, 0.07f, 0.11f, 1);
        clearCamera.cullingMask = 0;
        clearCamera.depth = -1000;
        clearCamera.useOcclusionCulling = false;
        // Source-owned Player asset, also retained by resource-free builds. It is available
        // before any manifest, download, installed resource or game package is trusted.
        EmbeddedClientBackground.Preload();
    }

    private void Phase(string label, string description, long bytes)
    {
        status = label; detail = description;
        downloadBytesPerSecond = null;
        lock (remoteProgressLock) downloadRate.Reset();
        totalBytes = bytes;
        Interlocked.Exchange(ref completedBytes, 0);
        measurable = bytes > 0;
    }

    public IEnumerator Install()
    {
        // Let the background render before any filesystem/package work starts.
        yield return null;
        // An explicit, tiny build marker chooses remote mode. Missing marker keeps legacy
        // bundled/offline behavior; no game package is loaded to draw this first screen.
        string configText = null;
        using (var request = UnityWebRequest.Get(StreamingUrl(MobileResourceBootstrapConfig.FileName)))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                if (request.downloadedBytes > MobileResourceBootstrapConfig.MaxConfigBytes)
                    throw new InvalidDataException("Resource bootstrap configuration is too large.");
                configText = request.downloadHandler.text;
            }
        }
        if (configText != null)
        {
            var config = JsonUtility.FromJson<MobileResourceBootstrapConfig>(configText);
            if (config == null) throw new InvalidDataException("Missing resource bootstrap configuration.");
            config.Validate(Debug.isDebugBuild);
            if (config.appVersion != UnityEngine.Application.version)
                throw new InvalidDataException("Resource bootstrap application version does not match this client.");
            IEnumerator remote = InstallRemote(config);
            while (remote.MoveNext()) yield return remote.Current;
            yield break;
        }
        // Existing local packs are an explicit development choice, never an implicit network fallback.
        if (configText == null)
        {
            allowBundled = Debug.isDebugBuild || UnityEngine.Application.isEditor;
            var config = new MobileResourceBootstrapConfig { schemaVersion = 1, mode = "resource-free",
                appVersion = UnityEngine.Application.version, dispatchUrl = "" };
            IEnumerator configured = InstallRemote(config);
            while (configured.MoveNext()) yield return configured.Current;
            if (!bundledRequested) yield break;
            resourceFree = false;
            if (!UnityEngine.Application.isMobilePlatform)
            {
                ClientResourcePaths.UseBundled();
                BeginClientStartup();
                yield break;
            }
        }
        string storage = Path.Combine(UnityEngine.Application.persistentDataPath, "MobileResources");
        Directory.CreateDirectory(storage);
        string manifestPath = Path.Combine(storage, "manifest.part");
        using (var request = Download(MobileResourceManifest.FileName, manifestPath))
        {
            yield return request.SendWebRequest();
            RequireSuccess(request);
        }
        if (new FileInfo(manifestPath).Length > MobileResourceManifest.MaxManifestBytes)
            throw new InvalidDataException("Mobile resource manifest exceeds size limit.");
        var manifest = MobileResourceManifest.Parse(File.ReadAllText(manifestPath));
        string root = Path.Combine(storage, manifest.Version);
        if (manifest.Offline) Debug.Log("FFO_MOBILE_RESOURCE_ROOT=" + root);
        else Directory.CreateDirectory(root);
        Phase("Checking resource metadata", "Checking installed files...", 0);
        Task<MobileResourceVerification> preparation = Task.Run(() =>
            new MobileResourceVerification(manifest, root, root + ".verified"));
        while (!preparation.IsCompleted) yield return null;
        var validation = preparation.GetAwaiter().GetResult();
        long allBytes = validation.BytesToHash, verifiedBytes = 0;
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            var entry = manifest.Entries[i];
            if (!validation.NeedsHash(i)) continue;
            int index = i;
            string fileLabel = (i + 1) + "/" + manifest.Entries.Count + "  " + Path.GetFileName(entry.Destination);
            Phase("Checking resources", fileLabel, allBytes);
            long preceding = verifiedBytes;
            Interlocked.Exchange(ref completedBytes, preceding);
            string destination = MobileResourceManifest.FilePath(root, entry.Destination);
            Task<bool> verification = Task.Run(() => validation.Verify(index,
                bytes => Interlocked.Exchange(ref completedBytes, preceding + bytes)));
            while (!verification.IsCompleted) yield return null;
            bool valid = verification.GetAwaiter().GetResult();
            if (!valid)
            {
                if (manifest.Offline)
                    throw new InvalidDataException("Missing or corrupt resource: " + entry.Destination +
                        ". Complete the USB resource transfer and restart.");
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                string temporary = destination + ".part";
                if (File.Exists(temporary)) File.Delete(temporary);
                Phase("Copying packaged resource", fileLabel, entry.Length);
                // Stream jar/file bytes directly to disk; percentage is actual transferred bytes.
                using (var request = Download(entry.Source, temporary))
                {
                    var operation = request.SendWebRequest();
                    while (!operation.isDone)
                    {
                        Interlocked.Exchange(ref completedBytes, (long)request.downloadedBytes);
                        yield return null;
                    }
                    RequireSuccess(request);
                    Interlocked.Exchange(ref completedBytes, (long)request.downloadedBytes);
                }
                Phase("Verifying copied resource", fileLabel, entry.Length);
                Task commit = Task.Run(() => validation.CommitFile(index, temporary,
                    bytes => Interlocked.Exchange(ref completedBytes, bytes)));
                while (!commit.IsCompleted) yield return null;
                commit.GetAwaiter().GetResult();
            }
            verifiedBytes += entry.Length;
        }
        Phase("Finishing resource check", "Saving verified resource state...", 0);
        Task receipt = Task.Run(() => validation.CommitReceipt());
        while (!receipt.IsCompleted) yield return null;
        receipt.GetAwaiter().GetResult();
        // Activation is the commit boundary. Interrupted versions are never visible to loaders.
        ClientResourcePaths.Activate(root);
        File.Delete(manifestPath);
        BeginClientStartup();
    }

    private IEnumerator InstallRemote(MobileResourceBootstrapConfig config)
    {
        resourceFree = true;
        string addressDirectory = UnityEngine.Application.persistentDataPath;
        string saved = "";
        try { saved = ClientUpdateAddressStore.Read(addressDirectory); }
        catch (Exception) { } // Invalid saved configuration remains recoverable in the address field.
        string legacy = PlayerPrefs.GetString("FFO.ResourceDispatchUrl", "");
        // Only this release's explicit retired defaults migrate. Arbitrary user overrides stay untouched.
        // Save the selected value only after the complete dispatch validates below; a failed migration
        // must not destroy the previous persisted address or rewrite unrelated legacy preferences.
        dispatchUrl = ClientUpdateAddressStore.SelectDispatchUrl(saved, legacy, config.dispatchUrl, config.legacyDispatchUrls);
        configurationRequired = string.IsNullOrWhiteSpace(dispatchUrl);
        configurationOpen = configurationRequired;
        draftDispatchUrl = dispatchUrl;
        bool allowHttp = allowDevelopmentHttp = config.developmentHttp && Debug.isDebugBuild;
        // A configured endpoint is an explicit remote mode. First-run mode makes no request until confirmed.
        if (!configurationRequired) downloadRequested = true;
        string storage = Path.Combine(UnityEngine.Application.persistentDataPath, "MobileResources", "Remote");
        var transport = new MobileResourceHttpTransport();
        var installer = new MobileRemoteResourceInstall(storage, config.appVersion, allowHttp, transport, PlatformKey(),
            availableBytes: ClientStorageSpace.CreateAvailableBytesQuery());
        Phase("检查并更新", "填写 dispatch.json 地址。资源清单、客户端版本和下载信息均由此获取。", 0);
        while (true)
        {
            awaitingChoice = true;
            while (!downloadRequested && !repairRequested && !enterRequested && !bundledRequested && !clientPackageRequested && !clientPackageHandoffRequested) yield return null;
            awaitingChoice = false;
            if (bundledRequested)
            {
                if (allowBundled && (updatePlan == null || !updatePlan.Required)) yield break;
                bundledRequested = false;
                continue;
            }
            if (clientPackageRequested || clientPackageHandoffRequested)
            {
                IEnumerator package = ProcessClientPackage(config, transport);
                while (package.MoveNext()) yield return package.Current;
                continue;
            }
            if (enterRequested)
            {
                enterRequested = false;
                if (ready && verifiedRoot != null && verifiedDispatchUrl == dispatchUrl.Trim() &&
                    updatePlan != null && !updatePlan.Required && updatePlan.HasCompatibleResources)
                {
                    DispatchUrl = verifiedDispatchUrl;
                    ClientUpdateSession.Publish(updatePlan);
                    ClientResourcePaths.Activate(verifiedRoot);
                    BeginClientStartup();
                    yield break;
                }
                continue;
            }
            ClearClientPackageSelection();
            bool repair = repairRequested;
            downloadRequested = repairRequested = false;
            failed = ready = false;
            verifiedRoot = null;
            updatePlan = null;
            string selectedDispatch = dispatchUrl.Trim();
            Exception configurationError = null;
            try { MobileResourceBootstrapConfig.ValidateEndpoint(selectedDispatch, allowHttp); }
            catch (Exception exception) { configurationError = exception; }
            if (configurationError != null) { RemoteFailure(configurationError); continue; }
            Phase("读取更新配置", "正在从 dispatch.json 检查客户端与资源版本…", 0);
            lock (remoteProgressLock) { remoteStatus = null; }
            remoteCancellation = new CancellationTokenSource();
            CancellationToken cancellation = remoteCancellation.Token;
            string temporaryDispatch = Path.Combine(UnityEngine.Application.temporaryCachePath,
                "dispatch-" + Guid.NewGuid().ToString("N") + ".part");
            Task<ClientUpdatePlan> dispatchTask = Task.Run(() => ReadDispatch(selectedDispatch, config.appVersion,
                allowHttp, transport, cancellation, temporaryDispatch));
            while (!dispatchTask.IsCompleted) yield return null;
            Exception failure = null;
            ClientUpdatePlan plan = null;
            try { plan = dispatchTask.GetAwaiter().GetResult(); cancellation.ThrowIfCancellationRequested(); }
            catch (Exception exception) { failure = exception; }
            if (failure == null)
            {
                updatePlan = plan;
                // Persist migration/embedded defaults only after the complete dispatch is validated.
                try { ClientUpdateAddressStore.Save(addressDirectory, selectedDispatch, allowHttp); }
                catch (Exception exception) { failure = exception; }
                if (plan.Required)
                    Phase("需要更新客户端", "当前 v" + config.appVersion + "，最低要求 v" + plan.Artifact.MinVersion +
                        "。请下载并校验安装包，再按提示安装和重新启动。", 0);
                else if (failure == null && !plan.HasCompatibleResources)
                    failure = new InvalidDataException("No compatible resource manifest for " + PlatformKey() + " client v" + config.appVersion + ". Download a compatible client package or correct the dispatch mapping.");
                else if (failure == null)
                {
                    lock (remoteProgressLock) { resourceTransferProgress = true; downloadRate.Reset(); }
                    Task<string> operation = Task.Run(() => installer.Install(plan.ResourceManifestUrl, cancellation,
                        SetRemoteProgress, plan.ResourceManifestSizeBytes, plan.ResourceManifestSha256, repair,
                        SetResourceNetworkProgress, plan.ResourceArchive));
                    while (!operation.IsCompleted) { ReadRemoteProgress(); yield return null; }
                    ReadRemoteProgress();
                    lock (remoteProgressLock) resourceTransferProgress = false;
                    try { verifiedRoot = operation.GetAwaiter().GetResult(); cancellation.ThrowIfCancellationRequested(); }
                    catch (Exception exception) { failure = exception; }
                }
            }
            if (cancellation.IsCancellationRequested) failure = new OperationCanceledException("操作已暂停，可重试继续。");
            remoteCancellation.Dispose(); remoteCancellation = null;
            if (failure != null) { RemoteFailure(failure); continue; }
            verifiedDispatchUrl = selectedDispatch;
            if (plan.Required) continue;
            ready = true;
            Phase("准备就绪", plan.Available ? "资源已验证。另有客户端 v" + plan.Artifact.Version + " 可下载，当前版本仍可进入。" :
                "资源已验证并安全保存，可以进入游戏。", 0);
            // Reuse the same verified entry guard. This only opens the title/login page; it never submits credentials.
            if (!repair && !configurationOpen && !plan.Available) enterRequested = true;
        }
    }

    private static ClientUpdatePlan ReadDispatch(string address, string version, bool allowHttp,
        IMobileResourceTransport transport, CancellationToken cancellation, string temporary)
    {
        try
        {
            transport.Download(MobileResourceBootstrapConfig.ValidateEndpoint(address, allowHttp), temporary,
                ClientUpdatePlan.MaxDispatchBytes, cancellation, null);
            cancellation.ThrowIfCancellationRequested();
            return ClientUpdatePlan.Parse(new System.Text.UTF8Encoding(false, true).GetString(File.ReadAllBytes(temporary)),
                PlatformKey(), version, allowHttp);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    private static string PlatformKey()
    {
#if UNITY_ANDROID
        return "android";
#elif UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return "windows";
#elif UNITY_STANDALONE_OSX || UNITY_EDITOR_OSX
        return "macos";
#else
        return "linux";
#endif
    }

    private void SetRemoteProgress(string phase, string description, long done, long total)
    {
        lock (remoteProgressLock)
        {
            remoteStatus = phase; remoteDetail = description; remoteDone = done; remoteTotal = total;
            // Resource workers report actual newly received bytes separately. A
            // changing filename or a concurrent local hash must not reset the rate
            // or count retained/copy bytes as network traffic.
            if (!resourceTransferProgress)
                downloadRate.Observe(phase + "|" + description, done, MonotonicSeconds(), phase == "Downloading client package");
        }
    }

    private void SetResourceNetworkProgress(long receivedBytes)
    {
        lock (remoteProgressLock)
            downloadRate.Observe("resource-set", receivedBytes, MonotonicSeconds(), true);
    }

    private void ReadRemoteProgress()
    {
        lock (remoteProgressLock)
        {
            if (remoteStatus == null) return;
            status = FriendlyPhase(remoteStatus); detail = remoteDetail;
            totalBytes = remoteTotal; measurable = totalBytes > 0;
            Interlocked.Exchange(ref completedBytes, remoteDone);
            downloadBytesPerSecond = remoteStatus == "Downloading resources" || remoteStatus == "Downloading first-install archive" || remoteStatus == "Downloading client package"
                ? downloadRate.BytesPerSecond(MonotonicSeconds()) : null;
        }
    }

    private void RemoteFailure(Exception exception)
    {
        failed = true;
        conciseError = SafeFailureMessage(exception);
        Phase("更新未完成", exception.Message, 0);
        Debug.LogWarning("Remote resource setup stopped: " + exception);
    }

    private static string FriendlyPhase(string value)
    {
        switch (value)
        {
            case "Downloading client package": return "正在下载客户端";
            case "Checking client package": return "检查客户端安装包";
            case "Verifying client package": return "验证客户端安装包";
            case "Client package ready": return "客户端安装包已校验";
            case "Downloading resource manifest": return "读取资源清单";
            case "Checking installed resource metadata": return "检查本地资源记录";
            case "Preparing resource update": return "准备资源更新";
            case "Checking staged resources": return "检查已保存资源";
            case "Repairing resource verification": return "完整校验资源";
            case "Checking resources": case "Checking installed resources": return "校验本地资源";
            case "Verifying retained download": return "校验已保存下载";
            case "Verifying resumed prefix": return "校验续传数据";
            case "Verifying reused resource": return "校验可复用资源";
            case "Reusing installed resources": return "复用已安装资源";
            case "Downloading resources": return "正在下载资源";
            case "Downloading first-install archive": return "正在下载首次资源包";
            case "Extracting first-install archive": return "正在解包并验证资源";
            case "Verifying downloaded resource": return "验证下载内容";
            case "Finishing resource check": return "启用完整资源";
            default: return value;
        }
    }

    public void BeginClientStartup()
    {
        // Client/title initialization has no measurable byte total. Never invent a percentage.
        Phase("Loading game", "Resources verified. Preparing the title screen...", 0);
    }

    public void Complete()
    {
        if (!failed) StartCoroutine(ReleaseAfterTitleFrame());
    }

    private IEnumerator ReleaseAfterTitleFrame()
    {
        // Keep the opaque cover across the first title render to avoid a handoff flash.
        yield return new WaitForEndOfFrame();
        Destroy(this);
    }

    private static UnityWebRequest Download(string relative, string destination)
    {
        var request = UnityWebRequest.Get(StreamingUrl(relative));
        request.downloadHandler = new DownloadHandlerFile(destination) { removeFileOnAbort = true };
        return request;
    }

    private static string StreamingUrl(string relative)
    {
        MobileResourceManifest.ValidateRelativePath(relative);
        string source = UnityEngine.Application.streamingAssetsPath.TrimEnd('/') + "/" + relative;
        return source.Contains("://") ? source : new Uri(source).AbsoluteUri;
    }

    private static void RequireSuccess(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.Success)
            throw new IOException("Packaged mobile resource read failed: " + request.error);
    }

    public void Fail(Exception exception)
    {
        failed = terminalFailure = true;
        conciseError = "启动失败，请检查日志后手动重启";
        configurationOpen = false;
        ready = false;
        Phase("启动未完成", exception.Message + "\n请检查启动配置、资源和可用空间，然后手动重新启动客户端。", 0);
        Debug.LogError("Client startup stopped: " + exception);
    }

    private void OnGUI()
    {
        int oldDepth = GUI.depth;
        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;
        bool savedEnabled = GUI.enabled;
        try
        {
            GUI.depth = -10000;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            EmbeddedClientBackground.Draw(new Rect(0, 0, Screen.width, Screen.height));
            Rect safe = Screen.safeArea;
            float panelHeight = configurationOpen ? ClientUpdateLayout.ConfigHeight : ClientUpdateLayout.Height;
            float scale = Mathf.Min(1f, Mathf.Min((safe.width - 32) / ClientUpdateLayout.Width,
                (safe.height - ClientUpdateLayout.Bottom - 16) / panelHeight));
            scale = Mathf.Max(.1f, scale);
            float x = safe.x + (safe.width - ClientUpdateLayout.Width * scale) * .5f;
            float y = Screen.height - safe.yMax + safe.height - (panelHeight + ClientUpdateLayout.Bottom) * scale;
            GUI.matrix = Matrix4x4.TRS(new Vector3(x, y, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            float width = ClientUpdateLayout.Width, pad = ClientUpdateLayout.Padding, inner = width - pad * 2;
            Paint(new Rect(0, 0, width, panelHeight), new Color(.025f, .045f, .055f, .88f));
            Color edge = new Color(.65f, .52f, .29f, .85f);
            Paint(new Rect(0, 0, width, 1), edge); Paint(new Rect(0, panelHeight - 1, width, 1), edge);
            Paint(new Rect(0, 0, 1, panelHeight), edge); Paint(new Rect(width - 1, 0, 1, panelHeight), edge);
            EnsureStyles();
            titleStyle.normal.textColor = new Color(.96f, .86f, .63f);
            detailStyle.normal.textColor = new Color(.76f, .84f, .87f);
            GUI.Label(new Rect(pad, ClientUpdateLayout.TitleY, 350, 32), configurationOpen ? "配置更新地址" : "游戏更新", titleStyle);
            var right = new GUIStyle(detailStyle) { alignment = TextAnchor.MiddleRight };
            GUI.Label(new Rect(width - pad - 190, ClientUpdateLayout.TitleY, 190, 32), "客户端 v" + UnityEngine.Application.version, right);
            bool oldEnabled = GUI.enabled;
            if (configurationOpen)
            {
                GUI.Label(new Rect(pad, ClientUpdateLayout.AddressLabelY, inner, 23), "dispatch.json 地址", detailStyle);
                GUI.enabled = oldEnabled && resourceFree && awaitingChoice;
                draftDispatchUrl = GUI.TextField(new Rect(pad, ClientUpdateLayout.AddressY, inner, ClientUpdateLayout.AddressHeight), draftDispatchUrl, 4096, fieldStyle);
                var hint = new GUIStyle(detailStyle);
                hint.normal.textColor = string.IsNullOrEmpty(configurationError) ? detailStyle.normal.textColor : new Color(1, .39f, .32f);
                GUI.Label(new Rect(pad, ClientUpdateLayout.ConfigHintY, inner, 38), string.IsNullOrEmpty(configurationError) ?
                    "填写分发地址后确认，即可检查版本并准备游戏资源。" : configurationError, hint);
                if (GUI.Button(new Rect((width - 244) * .5f, ClientUpdateLayout.ConfirmY, 244, ClientUpdateLayout.ButtonHeight), "确认配置", buttonStyle)) ConfirmConfiguration();
                if (!configurationRequired && GUI.Button(new Rect(pad, ClientUpdateLayout.ConfirmY, 100, ClientUpdateLayout.ButtonHeight), "返回", detailStyle))
                    configurationOpen = false;
                GUI.enabled = oldEnabled && awaitingChoice && allowBundled && (updatePlan == null || !updatePlan.Required);
                if (allowBundled && GUI.Button(new Rect(width - pad - 132, ClientUpdateLayout.ConfirmY, 132, ClientUpdateLayout.ButtonHeight), "开发：本地资源", detailStyle)) bundledRequested = true;
                GUI.enabled = oldEnabled;
                return;
            }
            statusStyle.normal.textColor = failed ? new Color(1, .39f, .32f) : Color.white;
            GUI.Label(new Rect(pad, ClientUpdateLayout.StatusY, inner, 36), failed ? conciseError : status, statusStyle);
            Paint(new Rect(pad, ClientUpdateLayout.ProgressY, ClientUpdateLayout.ProgressWidth, 5), new Color(.025f, .05f, .075f, 1));
            string metrics = terminalFailure ? "已有资源已保留 · 详细原因见日志" : remoteCancellation != null ? "正在处理，请稍候…" :
                clientPackageMode && !string.IsNullOrEmpty(clientPackageNotice) ? clientPackageNotice :
                ready ? "资源已就绪，旅程即将开始" : failed ? "已有资源会保留，可重试或修改配置" :
                updatePlan != null && updatePlan.Required ? "最低客户端 v" + updatePlan.Artifact.MinVersion + " · 请按更新步骤安装并重启" :
                "检查版本和资源，准备进入游戏";
            if (measurable)
            {
                long bytes = Interlocked.Read(ref completedBytes);
                float fraction = Mathf.Clamp01((float)((double)bytes / totalBytes));
                Paint(new Rect(pad, ClientUpdateLayout.ProgressY, ClientUpdateLayout.ProgressWidth * fraction, 5), new Color(.86f, .70f, .36f, 1));
                metrics = androidPackageActive && AndroidInstallBusy ? Math.Floor(fraction * 100) + "%  ·  本地安装准备" :
                    Math.Floor(fraction * 100) + "%  ·  " + (bytes / 1048576d).ToString("F1") + " / " + (totalBytes / 1048576d).ToString("F1") + " MiB";
            }
            GUI.Label(new Rect(width - pad - ClientUpdateLayout.SpeedWidth, ClientUpdateLayout.ProgressY - 12, ClientUpdateLayout.SpeedWidth, 28), SpeedText(downloadBytesPerSecond), right);
            GUI.Label(new Rect(pad, ClientUpdateLayout.MetricsY, inner, 30), metrics, detailStyle);
            bool requiresClient = updatePlan != null && updatePlan.Required;
            string primary = terminalFailure ? "退出后手动重启" : remoteCancellation != null ? "暂停更新" : (requiresClient || clientPackageMode) ? ClientPackageButtonLabel() : ready ? "进入游戏" : failed ? "重试更新" : "检查更新";
            GUI.enabled = oldEnabled && (terminalFailure || (resourceFree && ClientPackageActionEnabled && (awaitingChoice || (remoteCancellation != null && !remoteCancellation.IsCancellationRequested))));
            if (GUI.Button(new Rect((width - 244) * .5f, ClientUpdateLayout.PrimaryY, 244, ClientUpdateLayout.ButtonHeight), primary, buttonStyle))
            {
                if (terminalFailure) UnityEngine.Application.Quit();
                else if (remoteCancellation != null)
                { remoteCancellation.Cancel(); downloadBytesPerSecond = null; lock (remoteProgressLock) downloadRate.Reset(); }
                else if (requiresClient || clientPackageMode) RequestClientPackageAction();
                else if (ready) enterRequested = true;
                else downloadRequested = true;
            }
            GUI.enabled = oldEnabled && resourceFree && awaitingChoice && !terminalFailure && !AndroidInstallBusy && !DesktopInstallBusy;
            if (IconButton(new Rect(ClientUpdateLayout.RepairX, ClientUpdateLayout.PrimaryY - 2, ClientUpdateLayout.IconSize, ClientUpdateLayout.IconSize), repairIcon, "修复资源")) repairRequested = true;
            if (IconButton(new Rect(ClientUpdateLayout.ConfigureX, ClientUpdateLayout.PrimaryY - 2, ClientUpdateLayout.IconSize, ClientUpdateLayout.IconSize), configureIcon, "配置更新地址"))
            { draftDispatchUrl = dispatchUrl; configurationError = ""; configurationOpen = true; }
            if (updatePlan != null && updatePlan.Available && !requiresClient)
            {
                var link = new GUIStyle(detailStyle) { alignment = TextAnchor.MiddleCenter };
                string caption = clientPackageMode ? "返回当前版本" : "下载新客户端 v" + updatePlan.Artifact.Version;
                if (GUI.Button(new Rect((width - 244) * .5f, 199, 244, 24), caption, link))
                { if (clientPackageMode) ReturnFromOptionalClientPackage(); else RequestClientPackageAction(); }
            }
            GUI.enabled = oldEnabled;
        }
        finally { GUI.depth = oldDepth; GUI.color = oldColor; GUI.matrix = oldMatrix; GUI.enabled = savedEnabled; }
    }

    private void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22 };
        statusStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 17 };
        detailStyle = new GUIStyle(GUI.skin.label) { wordWrap = true, fontSize = 13 };
        fieldStyle = new GUIStyle(GUI.skin.textField) { fontSize = 15, padding = new RectOffset(10, 10, 8, 8) };
        buttonTexture = MakeButtonTexture(false); buttonHoverTexture = MakeButtonTexture(true);
        repairIcon = ClientUpdateIcons.Create(false); configureIcon = ClientUpdateIcons.Create(true);
        buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, border = new RectOffset(1, 1, 1, 1) };
        buttonStyle.normal.background = buttonTexture; buttonStyle.hover.background = buttonHoverTexture;
        buttonStyle.active.background = buttonTexture;
        buttonStyle.normal.textColor = buttonStyle.hover.textColor = buttonStyle.active.textColor = new Color(1, .89f, .65f);
    }

    private bool IconButton(Rect bounds, Texture2D icon, string tooltip)
    {
        bool pressed = GUI.Button(bounds, new GUIContent("", tooltip), buttonStyle);
        Color old = GUI.color;
        GUI.color = GUI.enabled ? Color.white : new Color(1, 1, 1, .35f);
        GUI.DrawTexture(new Rect(bounds.x + 8, bounds.y + 8, 32, 32), icon);
        GUI.color = old;
        return pressed;
    }

    private void ConfirmConfiguration()
    {
        try
        {
            string selected = draftDispatchUrl.Trim();
            ClientUpdateAddressStore.Save(UnityEngine.Application.persistentDataPath, selected, allowDevelopmentHttp);
            dispatchUrl = selected;
            configurationError = ""; configurationRequired = configurationOpen = false;
            ClearClientPackageSelection();
            ready = failed = false; verifiedRoot = null; updatePlan = null;
            downloadRequested = true;
            downloadBytesPerSecond = null;
            lock (remoteProgressLock) downloadRate.Reset();
        }
        catch (Exception exception)
        {
            configurationError = SafeFailureMessage(exception);
            Debug.LogWarning("Update configuration rejected: " + exception);
        }
    }

    private static string SafeFailureMessage(Exception exception)
    {
        if (exception is OperationCanceledException) return "更新已暂停，可继续重试";
        if (exception is System.Net.WebException) return "网络连接失败，请检查网络后重试";
        if (exception is UnauthorizedAccessException) return "无法写入更新目录，请检查权限";
        string message = exception.Message.ToLowerInvariant();
        if (message.Contains("space") || message.Contains("disk")) return "可用空间不足，请清理后重试";
        if (message.Contains("client package") && (message.Contains("sha") || message.Contains("hash") || message.Contains("corrupt"))) return "客户端安装包校验失败，请重新下载";
        if (message.Contains("client") && (message.Contains("version") || message.Contains("platform"))) return "资源与当前客户端不兼容";
        if (message.Contains("sha") || message.Contains("hash") || message.Contains("corrupt")) return "资源校验失败，请重试或修复";
        if (message.Contains("url") || message.Contains("https") || message.Contains("host") || message.Contains("endpoint")) return "更新地址无效，请检查配置";
        if (exception is InvalidDataException || exception is FormatException) return "更新配置或资源清单无效，请检查后重试";
        return "更新未完成，请重试；详细原因已记入日志";
    }

    private static double MonotonicSeconds() => (double)System.Diagnostics.Stopwatch.GetTimestamp() / System.Diagnostics.Stopwatch.Frequency;
    private static string SpeedText(double? bytesPerSecond)
    {
        if (!bytesPerSecond.HasValue) return "—";
        double speed = Math.Max(0, bytesPerSecond.Value);
        return speed >= 1048576 ? (speed / 1048576).ToString("F1") + " MiB/s" : (speed / 1024).ToString("F0") + " KiB/s";
    }

    private static Texture2D MakeButtonTexture(bool hover)
    {
        var texture = new Texture2D(4, 32, TextureFormat.RGBA32, false);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 4; x++)
            texture.SetPixel(x, y, x == 0 || x == 3 || y == 0 || y == 31 ? new Color(.72f, .58f, .31f) :
                Color.Lerp(new Color(.15f, .17f, .15f), new Color(hover ? .34f : .26f, .27f, .19f), y / 31f));
        texture.Apply(false, true);
        return texture;
    }

    private static void Paint(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnApplicationPause(bool paused)
    {
        // Leaving the app pauses network/disk transfer. Do not cancel Android installer
        // sessions here: the required system permission/confirmation screens also pause Unity.
        if (!paused || remoteCancellation == null) return;
        remoteCancellation.Cancel();
        downloadBytesPerSecond = null;
        lock (remoteProgressLock) downloadRate.Reset();
    }

    private void OnDestroy()
    {
        // Abort an in-flight response when the bootstrap is destroyed; no partial set activates.
        remoteCancellation?.Cancel();
        CancelDesktopUpdate();
        if (androidPackageActive) AndroidClientPackageInstaller.Cancel();
        if (clearCamera != null) Destroy(clearCamera.gameObject);
        if (buttonTexture != null) Destroy(buttonTexture);
        if (buttonHoverTexture != null) Destroy(buttonHoverTexture);
        if (repairIcon != null) Destroy(repairIcon);
        if (configureIcon != null) Destroy(configureIcon);
    }
}
}
