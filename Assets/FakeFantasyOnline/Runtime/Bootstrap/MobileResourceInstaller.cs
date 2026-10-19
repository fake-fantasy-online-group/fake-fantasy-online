using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Bootstrap
{
/// <summary>Owns the mobile startup screen until the title has actually initialized.</summary>
public sealed class MobileResourceInstaller : MonoBehaviour
{
    private string status = "Reading resource manifest";
    private string detail = "Preparing your journey";
    private long completedBytes, totalBytes;
    private bool measurable, failed;
    private Camera clearCamera;
    private Texture2D background;
    private GUIStyle titleStyle, statusStyle, detailStyle, fieldStyle, buttonStyle;
    private bool resourceFree, awaitingChoice, downloadRequested, installedRequested;
    private string manifestUrl = "", dispatchUrl = "";
    private CancellationTokenSource remoteCancellation;
    private readonly object remoteProgressLock = new object();
    private string remoteStatus, remoteDetail;
    private long remoteDone, remoteTotal;
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
        // Optional small, locally supplied build asset; never depend on unverified packages.
        background = Resources.Load<Texture2D>("MobileStartupBackground");
    }

    private void Phase(string label, string description, long bytes)
    {
        status = label; detail = description;
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
        manifestUrl = string.IsNullOrWhiteSpace(config.resourceManifestUrl)
            ? PlayerPrefs.GetString("FFO.ResourceManifestUrl", "") : config.resourceManifestUrl;
        dispatchUrl = string.IsNullOrWhiteSpace(config.dispatchUrl)
            ? PlayerPrefs.GetString("FFO.ResourceDispatchUrl", "") : config.dispatchUrl;
        bool allowHttp = config.developmentHttp && Debug.isDebugBuild;
        string storage = Path.Combine(UnityEngine.Application.persistentDataPath, "MobileResources", "Remote");
        var installer = new MobileRemoteResourceInstall(storage, config.appVersion, allowHttp, new MobileResourceHttpTransport());
        Phase("Set up game resources", "Enter the resource manifest URL and the separate game dispatch URL. HTTPS is required unless this is an explicitly enabled LAN development build.", 0);
        while (true)
        {
            awaitingChoice = true;
            while (!downloadRequested && !installedRequested) yield return null;
            awaitingChoice = false;
            bool useInstalled = installedRequested;
            downloadRequested = installedRequested = false;
            failed = false;
            string selectedManifest = manifestUrl.Trim(), selectedDispatch = dispatchUrl.Trim();
            Exception configurationError = null;
            try
            {
                MobileResourceBootstrapConfig.ValidateEndpoint(selectedDispatch, allowHttp);
                if (!useInstalled) MobileResourceBootstrapConfig.ValidateEndpoint(selectedManifest, allowHttp);
            }
            catch (Exception exception) { configurationError = exception; }
            if (configurationError != null) { RemoteFailure(configurationError); continue; }
            // Persist only validated, credential-free URLs. Resource and game endpoints stay separate.
            PlayerPrefs.SetString("FFO.ResourceDispatchUrl", selectedDispatch);
            if (!useInstalled) PlayerPrefs.SetString("FFO.ResourceManifestUrl", selectedManifest);
            PlayerPrefs.Save();
            Phase("Preparing resources", "Completed verified files can be reused. An interrupted current file is downloaded again.", 0);
            lock (remoteProgressLock) { remoteStatus = null; }
            remoteCancellation = new CancellationTokenSource();
            CancellationToken cancellation = remoteCancellation.Token;
            Task<string> operation = Task.Run(() => useInstalled
                ? installer.UseInstalled(cancellation, SetRemoteProgress)
                : installer.Install(selectedManifest, cancellation, SetRemoteProgress));
            while (!operation.IsCompleted)
            {
                ReadRemoteProgress();
                yield return null;
            }
            ReadRemoteProgress();
            string root = null;
            Exception failure = null;
            try { root = operation.GetAwaiter().GetResult(); }
            catch (Exception exception) { failure = exception; }
            remoteCancellation.Dispose(); remoteCancellation = null;
            if (failure != null) { RemoteFailure(failure); continue; }
            DispatchUrl = selectedDispatch;
            ClientResourcePaths.Activate(root);
            BeginClientStartup();
            yield break;
        }
    }

    private void SetRemoteProgress(string phase, string description, long done, long total)
    {
        lock (remoteProgressLock)
        { remoteStatus = phase; remoteDetail = description; remoteDone = done; remoteTotal = total; }
    }

    private void ReadRemoteProgress()
    {
        lock (remoteProgressLock)
        {
            if (remoteStatus == null) return;
            status = remoteStatus; detail = remoteDetail;
            totalBytes = remoteTotal; measurable = totalBytes > 0;
            Interlocked.Exchange(ref completedBytes, remoteDone);
        }
    }

    private void RemoteFailure(Exception exception)
    {
        failed = true;
        Phase("Resource setup stopped", exception.Message + "\nCheck the URLs, network and free storage, then retry. Existing installed resources are kept.", 0);
        Debug.LogWarning("Remote resource setup stopped: " + exception.GetType().Name + ": " + exception.Message);
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
        failed = true;
        Phase("Startup stopped", exception.Message + (resourceFree
            ? "\nCheck configuration and free storage, then restart the client."
            : "\nCheck resources, free storage and mobile-dispatch.txt, then restart."), 0);
        Debug.LogError("Mobile startup stopped: " + exception);
    }

    private void OnGUI()
    {
        int oldDepth = GUI.depth;
        Color oldColor = GUI.color;
        Matrix4x4 oldMatrix = GUI.matrix;
        try
        {
            GUI.depth = -10000;
            GUI.matrix = Matrix4x4.identity;
            GUI.color = Color.white;
            // Asset-independent opaque fallback: no package, font download or shader required.
            Paint(new Rect(0, 0, Screen.width, Screen.height), new Color(.035f, .07f, .11f, 1));
            if (background != null)
                GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), background, ScaleMode.ScaleAndCrop);
            else
                for (int band = 0; band < 24; band++)
                    Paint(new Rect(0, Screen.height * band / 24f, Screen.width, Screen.height / 24f + 1),
                        Color.Lerp(new Color(.04f, .09f, .15f, 1), new Color(.10f, .22f, .25f, 1), band / 23f));
            // safeArea is bottom-left based; IMGUI is top-left based.
            Rect safe = Screen.safeArea;
            float scale = Mathf.Clamp(Mathf.Min(safe.width / 960f, safe.height / 540f), .6f, 3f);
            float width = Mathf.Min(safe.width - 48 * scale, 720 * scale);
            float x = safe.x + (safe.width - width) * .5f;
            float y = Screen.height - safe.yMax + safe.height * (resourceFree ? .28f : .53f);
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
                statusStyle = new GUIStyle(titleStyle) { wordWrap = true };
                detailStyle = new GUIStyle(statusStyle);
                fieldStyle = new GUIStyle(GUI.skin.textField);
                buttonStyle = new GUIStyle(GUI.skin.button);
            }
            titleStyle.fontSize = Mathf.RoundToInt(30 * scale);
            statusStyle.fontSize = Mathf.RoundToInt(20 * scale);
            detailStyle.fontSize = Mathf.RoundToInt(14 * scale);
            fieldStyle.fontSize = buttonStyle.fontSize = Mathf.RoundToInt(16 * scale);
            titleStyle.normal.textColor = new Color(.96f, .86f, .63f);
            statusStyle.normal.textColor = Color.white;
            detailStyle.normal.textColor = new Color(.76f, .84f, .87f);
            GUI.Label(new Rect(x, y - 110 * scale, width, 50 * scale), "FAKE FANTASY ONLINE", titleStyle);
            GUI.Label(new Rect(x, y - 50 * scale, width, 40 * scale), status, statusStyle);
            Paint(new Rect(x, y, width, 8 * scale), new Color(.025f, .05f, .075f, 1));
            if (measurable)
            {
                long bytes = Interlocked.Read(ref completedBytes);
                float fraction = Mathf.Clamp01((float)((double)bytes / totalBytes));
                Paint(new Rect(x, y, width * fraction, 8 * scale), new Color(.86f, .70f, .36f, 1));
                GUI.Label(new Rect(x, y + 15 * scale, width, 28 * scale),
                    Math.Floor(fraction * 100) + "%  ·  " + (bytes / 1048576d).ToString("F1") +
                    " / " + (totalBytes / 1048576d).ToString("F1") + " MiB", detailStyle);
            }
            GUI.Label(new Rect(x, y + 42 * scale, width, (resourceFree ? 56 * scale : safe.height * .3f)), detail, detailStyle);
            if (resourceFree)
            {
                bool oldEnabled = GUI.enabled;
                GUI.enabled = awaitingChoice;
                GUI.Label(new Rect(x, y + 102 * scale, width, 24 * scale), "Resource manifest URL (mobile-resources.tsv)", detailStyle);
                manifestUrl = GUI.TextField(new Rect(x, y + 128 * scale, width, 34 * scale), manifestUrl, 4096, fieldStyle);
                GUI.Label(new Rect(x, y + 167 * scale, width, 24 * scale), "Game dispatch URL (separate game server endpoint)", detailStyle);
                dispatchUrl = GUI.TextField(new Rect(x, y + 193 * scale, width, 34 * scale), dispatchUrl, 4096, fieldStyle);
                float buttonWidth = (width - 12 * scale) * .5f;
                if (GUI.Button(new Rect(x, y + 240 * scale, buttonWidth, 40 * scale), failed ? "Retry download" : "Download resources", buttonStyle))
                { downloadRequested = true; awaitingChoice = false; }
                if (GUI.Button(new Rect(x + buttonWidth + 12 * scale, y + 240 * scale, buttonWidth, 40 * scale), "Use installed resources", buttonStyle))
                { installedRequested = true; awaitingChoice = false; }
                GUI.enabled = oldEnabled;
            }
        }
        finally { GUI.depth = oldDepth; GUI.color = oldColor; GUI.matrix = oldMatrix; }
    }

    private static void Paint(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = Color.white;
    }

    private void OnDestroy()
    {
        // Abort an in-flight response when the bootstrap is destroyed; no partial set activates.
        remoteCancellation?.Cancel();
        if (clearCamera != null) Destroy(clearCamera.gameObject);
    }
}
}
