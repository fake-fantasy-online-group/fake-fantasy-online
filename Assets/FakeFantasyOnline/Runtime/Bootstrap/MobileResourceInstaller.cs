using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using FakeFantasyOnline.Foundation;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Bootstrap
{
/// <summary>Copies packaged resources to a versioned private directory before any file-based consumer starts.</summary>
public sealed class MobileResourceInstaller : MonoBehaviour
{
    private string status = "Preparing mobile resources…";
    public IEnumerator Install()
    {
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
        if (manifest.Offline)
        {
            // The APK manifest is the authority. Local transfer metadata cannot change expected hashes.
            // Keep this line machine-readable for the explicit-device deployment helper.
            Debug.Log("FFO_MOBILE_RESOURCE_ROOT=" + root);
            status = "离线测试资源待校验。请完成 USB 资源传输后重新启动游戏。\n" + root;
            Task validation = Task.Run(() => manifest.VerifyDirectory(root));
            while (!validation.IsCompleted) yield return null;
            validation.GetAwaiter().GetResult();
            ClientResourcePaths.Activate(root);
            File.Delete(manifestPath);
            status = null;
            yield break;
        }
        Directory.CreateDirectory(root);
        for (int i = 0; i < manifest.Entries.Count; i++)
        {
            var entry = manifest.Entries[i];
            status = "Checking mobile resources " + (i + 1) + "/" + manifest.Entries.Count;
            string destination = MobileResourceManifest.FilePath(root, entry.Destination);
            Task<bool> verification = Task.Run(() => MobileResourceManifest.Verify(destination, entry));
            while (!verification.IsCompleted) yield return null;
            if (verification.GetAwaiter().GetResult()) continue;
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            string temporary = destination + ".part";
            if (File.Exists(temporary)) File.Delete(temporary);
            status = "Installing mobile resources " + (i + 1) + "/" + manifest.Entries.Count;
            // DownloadHandlerFile streams jar/file bytes straight to disk, including large .pkg files.
            using (var request = Download(entry.Source, temporary))
            {
                yield return request.SendWebRequest();
                RequireSuccess(request);
            }
            Task commit = Task.Run(() => MobileResourceManifest.CommitFile(temporary, destination, entry));
            while (!commit.IsCompleted) yield return null;
            commit.GetAwaiter().GetResult();
        }
        // Activation is the commit boundary. Interrupted versions are never visible to loaders.
        ClientResourcePaths.Activate(root);
        File.Delete(manifestPath);
        status = null;
    }

    private static UnityWebRequest Download(string relative, string destination)
    {
        MobileResourceManifest.ValidateRelativePath(relative);
        string source = UnityEngine.Application.streamingAssetsPath.TrimEnd('/') + "/" + relative;
        if (!source.Contains("://")) source = new Uri(source).AbsoluteUri;
        var request = UnityWebRequest.Get(source);
        request.downloadHandler = new DownloadHandlerFile(destination) { removeFileOnAbort = true };
        return request;
    }

    private static void RequireSuccess(UnityWebRequest request)
    {
        if (request.result != UnityWebRequest.Result.Success)
            throw new IOException("Packaged mobile resource read failed: " + request.error);
    }

    public void Fail(Exception exception)
    {
        status = "Mobile startup stopped: " + exception.Message + "\nCheck packaged resources, free storage and mobile-dispatch.txt, then restart.";
        Debug.LogError(status);
    }

    private void OnGUI()
    {
        if (status != null) GUI.Label(new Rect(20, 20, Screen.width - 40, Screen.height - 40), status);
    }
}
}
