using System;
using System.IO;
using System.Text;
using System.Xml;
using FakeFantasyOnline.Foundation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace FakeFantasyOnline.Editor
{
/// <summary>Local, resource-free desktop packaging. OS signing/notarization is a separate release gate.</summary>
public static class DesktopResourceFreeBuild
{
    public static void BuildWindows() { Build("windows", BuildTarget.StandaloneWindows64); }
    public static void BuildMacOS() { Build("macos", BuildTarget.StandaloneOSX); }

    private static void Build(string platform, BuildTarget target)
    {
        var profile = new ResourceFreeBuildProfile(platform, PlayerSettings.bundleVersion, Environment.GetEnvironmentVariable);
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new InvalidOperationException("Install the selected desktop Build Support module in this Unity Editor.");
        string configuredOutput = Environment.GetEnvironmentVariable("FFO_BUILD_OUTPUT");
        string output = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredOutput)
            ? (platform == "windows" ? "Build/ResourceFree/windows/fake-fantasy-online/fake-fantasy-online.exe" :
                "Build/ResourceFree/macos/fake-fantasy-online.app") : configuredOutput);
        if (!output.EndsWith(platform == "windows" ? ".exe" : ".app", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Desktop build output extension does not match the selected platform.");
        string installRoot = platform == "windows" ? Path.GetDirectoryName(output) : output;
        if (Directory.Exists(installRoot) || File.Exists(installRoot))
            throw new IOException("Resource-free desktop output must use a fresh dedicated directory; stale files must not enter an update package.");
        string appId = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Standalone);
        string teamId = Environment.GetEnvironmentVariable("FFO_MAC_TEAM_ID") ?? "";
        string signer = Environment.GetEnvironmentVariable("FFO_WINDOWS_SIGNER_SHA256") ?? "";
        DesktopBuildPayload.ValidateIdentity(platform, !profile.Development, appId, teamId, signer);
        string helpers = Environment.GetEnvironmentVariable("FFO_DESKTOP_UPDATER_DIRECTORY");
        DesktopBuildPayload.ValidateHelpers(helpers, platform);
        string dataDirectory = platform == "windows"
            ? Path.Combine(installRoot, Path.GetFileNameWithoutExtension(output) + "_Data")
            : Path.Combine(output, "Contents/Resources/Data");
        string codePages = platform == "windows"
            ? ClientBuild.ResolveCodePageAssemblyDirectory(EditorApplication.applicationContentsPath, "Windows", "unityjit-win32", "net_4_x-win32", "unity")
            : ClientBuild.ResolveCodePageAssemblyDirectory(EditorApplication.applicationContentsPath, "macOS", "unityjit-macos");
        var backend = PlayerSettings.GetScriptingBackend(NamedBuildTarget.Standalone);
        var httpPolicy = PlayerSettings.insecureHttpOption;
        int architecture = PlayerSettings.GetArchitecture(NamedBuildTarget.Standalone);
        var defaultIcons = new BuildIconSnapshot(NamedBuildTarget.Unknown);
        var desktopIcons = new BuildIconSnapshot(NamedBuildTarget.Standalone);
        IDisposable stage = null;
        try
        {
            stage = new ResourceFreeBuildStage(UnityEngine.Application.dataPath,
                Path.GetFullPath("LocalMobileBuild/ResourceFree-" + platform),
                MobileResourceBootstrapConfig.FileName, JsonUtility.ToJson(profile.Bootstrap, true));
            GbkBuildTable.WriteTo(Path.Combine(UnityEngine.Application.dataPath, "Resources"));
            DesktopBuildPayload.CopyHelpers(helpers, Path.Combine(UnityEngine.Application.streamingAssetsPath, "DesktopUpdater"), platform);
            PlayerSettings.insecureHttpOption = profile.Bootstrap.developmentHttp ? InsecureHttpOption.DevelopmentOnly : InsecureHttpOption.NotAllowed;
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
            if (platform == "macos") PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, 2); // Intel + Apple silicon universal.
            AssetDatabase.Refresh();
            ClientBuild.ConfigureResourceFreeBranding(NamedBuildTarget.Standalone);
            ClientBuild.PrepareClientScene(true);
            Directory.CreateDirectory(Path.GetDirectoryName(output));
            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/FakeFantasyOnline/Scenes/Client.unity" },
                locationPathName = output,
                target = target,
                options = profile.Development ? BuildOptions.Development : BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Resource-free desktop build failed: " + report.summary.result);
            ClientBuild.InstallCodePageAssembliesFromDirectory(dataDirectory, codePages);
            if (platform == "macos")
                foreach (string helper in new[] { "DesktopUpdater-osx-x64", "DesktopUpdater-osx-arm64" })
                    ClientBuild.RunMacTool("/bin/chmod", "u+x " + ClientBuild.Quote(Path.Combine(dataDirectory, "StreamingAssets/DesktopUpdater", helper)));
            var marker = new DesktopBuildPayload.InstallMarker
            {
                schemaVersion = 1, platform = platform, version = profile.Bootstrap.appVersion,
                installId = appId, macTeamId = platform == "macos" ? teamId : "",
                windowsSignerSha256 = platform == "windows" ? signer.ToLowerInvariant() : "",
                executable = platform == "windows" ? Path.GetFileName(output) : "Contents/MacOS/" + ReadMacExecutable(output)
            };
            string markerRoot = platform == "windows" ? installRoot : Path.Combine(installRoot, "Contents/Resources");
            File.WriteAllText(Path.Combine(markerRoot, "ffo-desktop-install.json"), JsonUtility.ToJson(marker, true), new UTF8Encoding(false));
            // Never replace an existing Developer ID signature with the legacy ad-hoc signing flow.
            Debug.Log("Resource-free " + platform + " Player built at " + output +
                ". OS signing/notarization, archive validation, startup and update acceptance remain required before distribution.");
        }
        finally
        {
            try { stage?.Dispose(); }
            finally
            {
                PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, backend);
                PlayerSettings.SetArchitecture(NamedBuildTarget.Standalone, architecture);
                PlayerSettings.insecureHttpOption = httpPolicy;
                AssetDatabase.Refresh();
                defaultIcons.Restore();
                desktopIcons.Restore();
            }
        }
    }

    private static string ReadMacExecutable(string app)
    {
        var document = new XmlDocument { XmlResolver = null };
        using (var reader = XmlReader.Create(Path.Combine(app, "Contents/Info.plist"),
            new XmlReaderSettings { DtdProcessing = DtdProcessing.Ignore, XmlResolver = null })) document.Load(reader);
        XmlNode node = document.SelectSingleNode("/plist/dict/key[text()='CFBundleExecutable']/following-sibling::*[1][self::string]");
        string name = node?.InnerText;
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) || name.IndexOf('\\') >= 0 ||
            !File.Exists(Path.Combine(app, "Contents/MacOS", name)))
            throw new InvalidDataException("Built macOS app must declare its existing executable in Info.plist.");
        return name;
    }
}
}
