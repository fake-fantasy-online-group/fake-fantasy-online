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
/// <summary>Opt-in Android foundation build. Does not install SDKs, change signing or relax network policy.</summary>
public static class MobileClientBuild
{
    public static void BuildAndroid()
    {
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
            throw new InvalidOperationException("Install Android Build Support for this Unity Editor before building.");
        string output = Environment.GetEnvironmentVariable("FAKE_FANTASY_ONLINE_ANDROID_BUILD_PATH");
        output = Path.GetFullPath(string.IsNullOrWhiteSpace(output) ? "Build/Android/fake-fantasy-online.apk" : output);
        if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Android build output must end in .apk.");
        string root = UnityEngine.Application.streamingAssetsPath;
        string manifestPath = Path.Combine(root, MobileResourceManifest.FileName);
        byte[] previousManifest = File.Exists(manifestPath) ? File.ReadAllBytes(manifestPath) : null;
        var backend = PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android);
        var architectures = PlayerSettings.Android.targetArchitectures;
        bool appBundle = EditorUserBuildSettings.buildAppBundle;
        try
        {
            GenerateManifest(root);
            ClientBuild.PrepareClientScene();
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            EditorUserBuildSettings.buildAppBundle = false;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/FakeFantasyOnline/Scenes/Client.unity" },
                locationPathName = output,
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android build failed: " + report.summary.result);
            Debug.Log("Android foundation APK built at " + output + ". Device/resource/IL2CPP acceptance is still required.");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, backend);
            PlayerSettings.Android.targetArchitectures = architectures;
            EditorUserBuildSettings.buildAppBundle = appBundle;
            if (previousManifest == null) File.Delete(manifestPath);
            else File.WriteAllBytes(manifestPath, previousManifest);
            AssetDatabase.Refresh();
        }
    }

    public static void GenerateManifest(string root)
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
        var text = new StringBuilder("FFO-MOBILE-1\n");
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
