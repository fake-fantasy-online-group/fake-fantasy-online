using System;
using System.IO;
using Process = System.Diagnostics.Process;
using ProcessStartInfo = System.Diagnostics.ProcessStartInfo;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FakeFantasyOnline.Editor
{

public static class ClientBuild
{
    private const string ScenePath = "Assets/FakeFantasyOnline/Scenes/Client.unity";
    private const string MonoRuntimeDirectory = "lib/mono";

    [MenuItem("fake-fantasy-online/Prepare client scene")]
    public static void PrepareClientScene()
    {
        if (!File.Exists(ScenePath))
        {
            throw new FileNotFoundException("The source-owned client scene is missing.", ScenePath);
        }

        // The source scene supplies the recovered Main Camera and AudioListener.
        // Replacing it with an empty scene silently removes the world's render target.
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        AssetDatabase.SaveAssets();
    }

    public static void BuildMacOS()
    {
        PrepareClientScene();
        string configuredPath = Environment.GetEnvironmentVariable(
            "FAKE_FANTASY_ONLINE_BUILD_PATH");
        string outputPath = string.IsNullOrWhiteSpace(configuredPath)
            ? Path.GetFullPath("Build/fake-fantasy-online.app")
            : Path.GetFullPath(configuredPath);

        BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
        ScriptingImplementation previousBackend =
            PlayerSettings.GetScriptingBackend(targetGroup);
        try
        {
            PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.Mono2x);
            PlayerSettings.productName = "fake-fantasy-online";
            PlayerSettings.companyName = "fake-fantasy-online";
            PlayerSettings.bundleVersion = "0.1.0";
            PlayerSettings.defaultScreenWidth = 800;
            PlayerSettings.defaultScreenHeight = 600;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.macRetinaSupport = false;

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneOSX,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"macOS player build failed: {report.summary.result}");
            }

            string dataDirectory = Path.Combine(outputPath, "Contents", "Resources", "Data");
            InstallPackageLayout(dataDirectory);
            InstallMusicLayout(dataDirectory);
            InstallSoundLayout(dataDirectory);
            InstallCodePageAssembliesFromDirectory(
                dataDirectory,
                ResolveCodePageAssemblyDirectory(
                    EditorApplication.applicationContentsPath,
                    "macOS",
                    "unityjit-macos"));
            ApplyMacOSWindowMetadata(outputPath);
            Debug.Log($"fake-fantasy-online player built at {outputPath}");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(targetGroup, previousBackend);
        }
    }

    public static void BuildWindows64()
    {
        string editorContentsPath = EditorApplication.applicationContentsPath;
        string windowsSupportDirectory = Path.Combine(
            editorContentsPath,
            "PlaybackEngines",
            "WindowsStandaloneSupport");
        if (!Directory.Exists(windowsSupportDirectory))
        {
            throw new InvalidOperationException(
                "Unity Windows Standalone Support module is missing from the active Editor: " +
                windowsSupportDirectory +
                ". Install Windows Build Support (Mono) for this Unity version, then rebuild.");
        }

        string configuredPath = Environment.GetEnvironmentVariable(
            "FAKE_FANTASY_ONLINE_WINDOWS_BUILD_PATH");
        string outputPath = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? "Build/Windows/fake-fantasy-online.exe"
            : configuredPath);
        if (!string.Equals(
                Path.GetExtension(outputPath),
                ".exe",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException(
                "Windows player output path must end in .exe: " + outputPath,
                nameof(configuredPath));
        }
        string dataDirectory = Path.Combine(
            Path.GetDirectoryName(outputPath),
            Path.GetFileNameWithoutExtension(outputPath) + "_Data");
        string codePageSourceDirectory = ResolveCodePageAssemblyDirectory(
            editorContentsPath,
            "Windows",
            "unityjit-win32",
            "net_4_x-win32",
            "unity");

        PrepareClientScene();
        PlayerSettings.productName = "fake-fantasy-online";
        PlayerSettings.companyName = "fake-fantasy-online";
        PlayerSettings.bundleVersion = "0.1.0";
        PlayerSettings.defaultScreenWidth = 800;
        PlayerSettings.defaultScreenHeight = 600;
        PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
        PlayerSettings.resizableWindow = true;

        BuildTargetGroup targetGroup = BuildTargetGroup.Standalone;
        ScriptingImplementation previousBackend = PlayerSettings.GetScriptingBackend(targetGroup);
        try
        {
            PlayerSettings.SetScriptingBackend(targetGroup, ScriptingImplementation.Mono2x);
            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Windows player build failed: {report.summary.result}");
            }

            InstallPackageLayout(dataDirectory);
            InstallMusicLayout(dataDirectory);
            InstallSoundLayout(dataDirectory);
            InstallCodePageAssembliesFromDirectory(dataDirectory, codePageSourceDirectory);
            Debug.Log($"fake-fantasy-online Windows player built at {outputPath}");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(targetGroup, previousBackend);
        }
    }

    private static void InstallPackageLayout(string dataDirectory)
    {
        string streamingAssetsDirectory = Path.Combine(
            dataDirectory,
            "StreamingAssets");
        string packageDirectory = Path.Combine(
            streamingAssetsDirectory,
            "ReferenceClient",
            "packages");
        string officialDataDirectory = Path.Combine(streamingAssetsDirectory, "data");
        Directory.CreateDirectory(officialDataDirectory);

        string[] packageNames =
        {
            "zpatch_tower10.pkg",
            "zpatch_petart.pkg",
            "res_q21_000.pkg",
            "res_q21_001.pkg",
            "res_05diff.pkg",
            "res_ff21.pkg"
        };
        string[] packagedFiles = Directory.GetFiles(packageDirectory, "*.pkg");
        if (packagedFiles.Length != packageNames.Length)
        {
            throw new InvalidDataException(
                "The Player package staging directory is not the required six-package layout.");
        }

        string[] oldRuntimePackages = Directory.GetFiles(officialDataDirectory, "*.pkg");
        for (int index = 0; index < oldRuntimePackages.Length; index++)
        {
            File.Delete(oldRuntimePackages[index]);
        }

        for (int index = 0; index < packageNames.Length; index++)
        {
            string packageName = packageNames[index];
            string source = Path.Combine(packageDirectory, packageName);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException(
                    "The complete locally supplied resource package is missing.",
                    source);
            }

            string runtimePackage = Path.Combine(officialDataDirectory, packageName);
            File.Move(source, runtimePackage);
            if (!File.Exists(runtimePackage))
            {
                throw new IOException(
                    "Runtime package move failed: " + runtimePackage);
            }
        }

        Directory.Delete(packageDirectory, true);
        if (Directory.GetFiles(officialDataDirectory, "*.pkg").Length != packageNames.Length)
        {
            throw new InvalidDataException(
                "The built Player does not contain exactly six complete runtime packages.");
        }
    }

    private static void InstallCodePageAssembliesFromDirectory(
        string dataDirectory,
        string sourceDirectory)
    {
        string playerManagedDirectory = Path.Combine(dataDirectory, "Managed");
        string[] assemblyNames = { "I18N.dll", "I18N.CJK.dll" };
        Directory.CreateDirectory(playerManagedDirectory);
        for (int index = 0; index < assemblyNames.Length; index++)
        {
            string source = Path.Combine(sourceDirectory, assemblyNames[index]);
            if (!File.Exists(source))
            {
                throw new FileNotFoundException("Unity code-page assembly is missing.", source);
            }

            File.Copy(
                source,
                Path.Combine(playerManagedDirectory, assemblyNames[index]),
                true);
        }
    }

    private static string ResolveCodePageAssemblyDirectory(
        string editorContentsPath,
        string targetName,
        params string[] directoryNames)
    {
        string[] monoRoots =
        {
            Path.Combine(
                editorContentsPath,
                "Resources",
                "Scripting",
                "MonoBleedingEdge",
                MonoRuntimeDirectory),
            Path.Combine(
                editorContentsPath,
                "MonoBleedingEdge",
                MonoRuntimeDirectory)
        };
        for (int rootIndex = 0; rootIndex < monoRoots.Length; rootIndex++)
        {
            for (int candidateIndex = 0; candidateIndex < directoryNames.Length; candidateIndex++)
            {
                string candidate = Path.Combine(
                    monoRoots[rootIndex],
                    directoryNames[candidateIndex]);
                if (HasCodePageAssemblies(candidate))
                {
                    return candidate;
                }
            }
        }

        throw new FileNotFoundException(
            "The active Unity Editor has no complete " + targetName +
            " I18N assembly pair in its Mono runtime directories.",
            editorContentsPath);
    }

    private static bool HasCodePageAssemblies(string directory)
    {
        return File.Exists(Path.Combine(directory, "I18N.dll")) &&
               File.Exists(Path.Combine(directory, "I18N.CJK.dll"));
    }

    private static void InstallSoundLayout(string dataDirectory)
    {
        string streamingAssets = Path.Combine(dataDirectory, "StreamingAssets");
        foreach (string leaf in new[] { "sndeffect", "sndeffect24_pcm" })
        {
            // Unity copies user-supplied sound files directly from StreamingAssets.
            string canonical = Path.Combine(streamingAssets, leaf);
            if (Directory.Exists(canonical)) continue;
            if (leaf != "sndeffect") continue;
            throw new InvalidDataException("The source sound directory is missing from Player staging.");
        }
    }

    private static void InstallMusicLayout(string dataDirectory)
    {
        string streamingAssetsDirectory = Path.Combine(
            dataDirectory,
            "StreamingAssets");
        string sourceMusicDirectory = Path.Combine(
            streamingAssetsDirectory,
            "ReferenceClient",
            "music");
        string runtimeMusicDirectory = Path.Combine(
            streamingAssetsDirectory,
            "Music");
        string runtimeTableDirectory = Path.Combine(
            streamingAssetsDirectory,
            "gameassets",
            "tables");
        string runtimeTable = Path.Combine(runtimeTableDirectory, "mapmusic.json");

        string[] tracks = Directory.GetFiles(sourceMusicDirectory, "*.ogg");
        if (tracks.Length != 51)
        {
            throw new InvalidDataException(
                "The Player music staging layout is not the required 51-file layout.");
        }

        if (Directory.Exists(runtimeMusicDirectory))
        {
            Directory.Delete(runtimeMusicDirectory, true);
        }
        Directory.CreateDirectory(runtimeMusicDirectory);
        for (int index = 0; index < tracks.Length; index++)
        {
            string destination = Path.Combine(
                runtimeMusicDirectory,
                Path.GetFileName(tracks[index]));
            File.Move(tracks[index], destination);
        }

        // Both the Editor and Player read the tables supplied under gameassets/tables.
        if (!File.Exists(runtimeTable))
        {
            throw new InvalidDataException(
                "The built Player did not receive the gameassets tables directory.");
        }

        if (Directory.GetFiles(runtimeMusicDirectory, "*.ogg").Length != 51 ||
            !File.Exists(Path.Combine(runtimeMusicDirectory, "M001.ogg")) ||
            !File.Exists(Path.Combine(runtimeMusicDirectory, "M003.ogg")) ||
            Directory.GetFiles(sourceMusicDirectory, "*.ogg").Length != 0)
        {
            throw new InvalidDataException(
                "The built Player did not receive the required music layout.");
        }
    }

    private static void ApplyMacOSWindowMetadata(string appPath)
    {
        string infoPath = Path.Combine(appPath, "Contents", "Info.plist");
        RunMacTool(
            "/usr/libexec/PlistBuddy",
            "-c " + Quote("Delete :NSHighResolutionCapable") + " " + Quote(infoPath),
            true);
        RunMacTool(
            "/usr/libexec/PlistBuddy",
            "-c " + Quote("Add :NSHighResolutionCapable bool false") + " " + Quote(infoPath));
        RunMacTool("/usr/bin/codesign", "--force --deep --sign - " + Quote(appPath));
        RunMacTool("/usr/bin/codesign", "--verify --deep --strict " + Quote(appPath));
    }

    private static void RunMacTool(
        string executable,
        string arguments,
        bool allowFailure = false)
    {
        ProcessStartInfo start = new ProcessStartInfo(executable, arguments)
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        using (Process process = Process.Start(start))
        {
            if (process == null)
            {
                throw new InvalidOperationException("Could not start " + executable + ".");
            }

            string standardOutput = process.StandardOutput.ReadToEnd();
            string standardError = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.ExitCode != 0 && !allowFailure)
            {
                throw new InvalidOperationException(
                    executable + " failed: " + standardOutput + standardError);
            }
        }
    }

    private static string Quote(string value)
    {
        if (value.IndexOf('"') >= 0)
        {
            throw new ArgumentException("A macOS build path cannot contain a quote.", nameof(value));
        }

        return "\"" + value + "\"";
    }
}
}
