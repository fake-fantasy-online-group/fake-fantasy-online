using System.Collections;
using FakeFantasyOnline.Application;
using UnityEngine;
using UnityEngine.Networking;

namespace FakeFantasyOnline.Presentation.ReferenceAssets
{
internal partial class NetClient
{
    // Official static +0x18: no initializer and no fabricated local-version value.
    public static string LatestVer;

    private string versionManifestUrlMacForIndependentRuntime;
    private string versionManifestUrlWinForIndependentRuntime;

    internal void BindVersionManifestsForIndependentRuntime(string manifestUrlMac, string manifestUrlWin)
    {
        versionManifestUrlMacForIndependentRuntime = manifestUrlMac;
        versionManifestUrlWinForIndependentRuntime = manifestUrlWin;
    }

    // Official 060006b9 and its 060008b9..060008bf iterator. The actual startup
    // caller binds this owner's private manifest URLs and gates the selected
    // platform before invoking the original no-argument iterator factory. Request
    // and using/yield behavior remains recovered; no official URL is a fallback.
    internal IEnumerator CheckVersion()
    {
        using (UnityWebRequest req = UnityWebRequest.Get(
            UnityEngine.Application.platform == RuntimePlatform.OSXPlayer ||
            UnityEngine.Application.platform == RuntimePlatform.OSXEditor
                ? versionManifestUrlMacForIndependentRuntime
                : versionManifestUrlWinForIndependentRuntime))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    ClientConfig.Manifest manifest =
                        JsonUtility.FromJson<ClientConfig.Manifest>(req.downloadHandler.text);
                    if (manifest != null && manifest.files != null && manifest.files.Length > 0 &&
                        !string.IsNullOrEmpty(manifest.version) && manifest.version != "0")
                        LatestVer = manifest.version;
                }
                catch
                {
                }
            }
        }
    }
}
}
