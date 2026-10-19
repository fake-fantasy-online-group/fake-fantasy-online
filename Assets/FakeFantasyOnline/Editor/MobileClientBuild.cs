using System;
using System.IO;
using System.Linq;
using System.Text;
using FakeFantasyOnline.Foundation;
using FakeFantasyOnline.Protocol;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FakeFantasyOnline.Editor
{
/// <summary>Opt-in Android foundation build. Does not install SDKs or change signing. Development HTTP requires explicit build opt-in.</summary>
public static class MobileClientBuild
{
    public static void BuildAndroid()
    {
        BuildAndroid(Environment.GetEnvironmentVariable("FAKE_FANTASY_ONLINE_ANDROID_RESOURCE_MODE") ?? "offline");
    }

    public static void BuildAndroidResourceFree()
    {
        BuildAndroid("resource-free");
    }

    private static void BuildAndroid(string mode)
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support for this Unity Editor before building.");
        string output = Environment.GetEnvironmentVariable("FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH");
        output = Path.GetFullPath(string.IsNullOrWhiteSpace(output) ? "Build/Android/fake-fantasy-online.apk" : output);
        if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Android build output must end in .apk.");
        if (mode != "offline" && mode != "bundled" && mode != "resource-free")
            throw new ArgumentException("Resource mode must be offline, bundled, or resource-free.");
        bool offline = mode == "offline";
        bool resourceFree = mode == "resource-free";
        bool development = offline || resourceFree;
        bool developmentHttp = Environment.GetEnvironmentVariable("FAKE_FANTASY_ONLINE_ANDROID_DEVELOPMENT_HTTP") == "1";
        if (developmentHttp && !development) throw new InvalidOperationException("HTTP opt-in is restricted to development builds.");
        string root = UnityEngine.Application.streamingAssetsPath;
        string backup = Path.GetFullPath("LocalMobileBuild/OriginalStreamingAssets");
        if (Directory.Exists(backup)) throw new IOException("Interrupted mobile staging: restore " + backup + " to StreamingAssets before retrying.");
        IDisposable stage = null;
        string manifestPath = Path.Combine(root, MobileResourceManifest.FileName);
        byte[] previousManifest = !resourceFree && File.Exists(manifestPath) ? File.ReadAllBytes(manifestPath) : null;
        var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        var architectures = PlayerSettings.Android.targetArchitectures;
        bool appBundle = EditorUserBuildSettings.buildAppBundle;
        bool customKeystore = PlayerSettings.Android.useCustomKeystore;
        string applicationId = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Android);
        var previousHttpPolicy = PlayerSettings.insecureHttpOption;
        Texture2D[] defaultIcons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any);
        Texture2D[] androidIcons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Android, IconKind.Any);
        try
        {
            if (resourceFree)
            {
                var config = new MobileResourceBootstrapConfig
                {
                    schemaVersion = 1,
                    mode = "resource-free",
                    appVersion = PlayerSettings.bundleVersion,
                    dispatchUrl = Environment.GetEnvironmentVariable("FFO_RESOURCE_DISPATCH_URL") ?? "",
                    developmentHttp = developmentHttp
                };
                config.Validate(development);
                // Never generate or hash a game-resource manifest for this build mode.
                stage = new ResourceFreeBuildStage(UnityEngine.Application.dataPath,
                    Path.GetFullPath("LocalMobileBuild/ResourceFree"),
                    MobileResourceBootstrapConfig.FileName, JsonUtility.ToJson(config, true));
                GbkBuildTable.WriteTo(Path.Combine(UnityEngine.Application.dataPath, "Resources"));
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, new Texture2D[0], IconKind.Any);
                PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, new Texture2D[0], IconKind.Any);
                PlayerSettings.Android.useCustomKeystore = false;
                // Resource delivery mode must not change the application identity.
                // Preserve the project identifier (including Unity's resolved default).
                PlayerSettings.insecureHttpOption = developmentHttp ? InsecureHttpOption.DevelopmentOnly : InsecureHttpOption.NotAllowed;
                AssetDatabase.Refresh();
            }
            else
            {
                // Preserve the existing locally supplied offline/bundled resource workflow.
                if (developmentHttp) PlayerSettings.insecureHttpOption = InsecureHttpOption.DevelopmentOnly;
                GenerateManifest(root, offline);
                if (!offline && Directory.GetFiles(root, "*", SearchOption.AllDirectories).Sum(f => new FileInfo(f).Length) > 1024L * 1024 * 1024)
                    throw new InvalidDataException("Bundled resources exceed conservative 1 GiB limit; use offline development mode.");
            }
            ClientBuild.PrepareClientScene(resourceFree);
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            if (offline)
            {
                string manifest = File.ReadAllText(manifestPath);
                File.WriteAllText(output + ".resources.tsv", manifest, new UTF8Encoding(false));
                stage = new MobileStreamingAssetsStage(root, backup, manifest);
                AssetDatabase.Refresh();
            }
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/FakeFantasyOnline/Scenes/Client.unity" },
                locationPathName = output,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            Debug.Log("Android " + mode + " APK built at " + output + "; applicationId=" + applicationId +
                "; version=" + PlayerSettings.bundleVersion + "; versionCode=" + PlayerSettings.Android.bundleVersionCode +
                ". Actual APK identity/signature and device/resource/IL2CPP acceptance are still required.");
        }
        finally
        {
            try { stage?.Dispose(); }
            finally
            {
                PlayerSettings.insecureHttpOption = previousHttpPolicy;
                PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, backend);
                PlayerSettings.Android.targetArchitectures = architectures;
                PlayerSettings.Android.useCustomKeystore = customKeystore;
                EditorUserBuildSettings.buildAppBundle = appBundle;
                if (resourceFree)
                {
                    PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, defaultIcons, IconKind.Any);
                    PlayerSettings.SetIcons(UnityEditor.Build.NamedBuildTarget.Android, androidIcons, IconKind.Any);
                }
                else if (previousManifest == null) File.Delete(manifestPath);
                else File.WriteAllBytes(manifestPath, previousManifest);
                AssetDatabase.Refresh();
            }
        }
    }

    public static void GenerateManifest(string root, bool offline = false)
    {
        // Endpoint contains no credentials and is bundled in the app; never place secrets here.
        PrivateServerOptions.MobileConfigured(File.ReadAllText(Path.Combine(root, "mobile-dispatch.txt")).Trim());
        string[] packages = { "zpatch_tower10.pkg", "zpatch_petart.pkg", "res_q21_000.pkg", "res_q21_001.pkg", "res_05diff.pkg", "res_ff21.pkg" };
        foreach (string package in packages)
            RequireFile(root, "ReferenceClient/packages/" + package);
        if (Directory.GetFiles(Path.Combine(root, "ReferenceClient/packages"), "*.pkg").Length != packages.Length)
            throw new InvalidDataException("Mobile package staging requires exactly the six supported packages.");
        RequireFile(root, "ReferenceClient/music/M001.ogg");
        RequireFile(root, "ReferenceClient/music/M003.ogg");
        RequireFile(root, "gameassets/tables/mapmusic.json");
        if (!Directory.Exists(Path.Combine(root, "sndeffect")))
            throw new InvalidDataException("Locally supplied sndeffect directory is required.");
        if (!Directory.Exists(Path.Combine(root, "ReferenceClient/music")) ||
            Directory.GetFiles(Path.Combine(root, "ReferenceClient/music"), "*.ogg").Length != 51)
            throw new InvalidDataException("The locally supplied 51-track music layout is required.");
        var text = new StringBuilder(offline ? "FFO-MOBILE-OFFLINE-1\n" : "FFO-MOBILE-1\n");
        foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories).OrderBy(p => p, StringComparer.Ordinal))
        {
            string relative = file.Substring(root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Length + 1).Replace('\\', '/');
            if (relative.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) || relative == MobileResourceManifest.FileName) continue;
            // Explicit packaged content allowlist avoids accidentally bundling local unrelated files.
            if (!(relative.StartsWith("ReferenceClient/", StringComparison.Ordinal) || relative.StartsWith("gameassets/", StringComparison.Ordinal) ||
                relative.StartsWith("sndeffect/", StringComparison.Ordinal) || relative.StartsWith("sndeffect24_pcm/", StringComparison.Ordinal) ||
                relative.StartsWith("AddOns/", StringComparison.Ordinal) || relative == "mobile-dispatch.txt"))
                throw new InvalidDataException("Unexpected StreamingAssets content; review before mobile packaging: " + relative);
            string destination = relative;
            if (relative.StartsWith("ReferenceClient/packages/", StringComparison.Ordinal)) destination = "data/" + relative.Substring("ReferenceClient/packages/".Length);
            else if (relative.StartsWith("ReferenceClient/music/", StringComparison.Ordinal)) destination = "Music/" + relative.Substring("ReferenceClient/music/".Length);
            text.Append(relative).Append('\t').Append(destination).Append('\t').Append(new FileInfo(file).Length)
                .Append('\t').Append(MobileResourceManifest.HashFile(file)).Append('\n');
        }
        MobileResourceManifest.Parse(text.ToString());
        File.WriteAllText(Path.Combine(root, MobileResourceManifest.FileName), text.ToString(), new UTF8Encoding(false));
        AssetDatabase.Refresh();
    }

    private static void RequireFile(string root, string relative)
    {
        if (!File.Exists(Path.Combine(root, relative))) throw new FileNotFoundException("Missing local mobile resource: " + relative);
    }
}
}
