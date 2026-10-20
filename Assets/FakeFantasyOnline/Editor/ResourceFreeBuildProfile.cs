using System;
using System.IO;
using System.Text.RegularExpressions;
using FakeFantasyOnline.Foundation;

namespace FakeFantasyOnline.Editor
{
/// <summary>Public, non-secret inputs for a local resource-free build. Never changes project identity or credentials.</summary>
public sealed class ResourceFreeBuildProfile
{
    public readonly string Platform;
    public readonly bool Development;
    public readonly MobileResourceBootstrapConfig Bootstrap;

    public ResourceFreeBuildProfile(string platform, string projectVersion, Func<string, string> environment,
        string defaultConfiguration = "formal")
    {
        if (platform != "android" && platform != "windows" && platform != "macos")
            throw new ArgumentException("Resource-free platform must be android, windows, or macos.");
        Platform = platform;
        string selectedPlatform = environment("FFO_BUILD_PLATFORM");
        if (!string.IsNullOrEmpty(selectedPlatform) && selectedPlatform != platform)
            throw new InvalidOperationException("Selected platform does not match the Unity build entry point.");
        string configuration = environment("FFO_BUILD_CONFIGURATION") ?? defaultConfiguration;
        if (configuration != "formal" && configuration != "development")
            throw new ArgumentException("Build configuration must be formal or development.");
        Development = configuration == "development";
        ValidateVersion(projectVersion);
        string requestedVersion = environment("FFO_BUILD_VERSION");
        if (!string.IsNullOrEmpty(requestedVersion) && requestedVersion != projectVersion)
            throw new InvalidOperationException("Requested version must match bundleVersion in ProjectSettings; the build never rewrites it.");
        string http = environment("FFO_BUILD_DEVELOPMENT_HTTP") ??
            environment("FAKE_FANTASY_ONLINE_ANDROID_DEVELOPMENT_HTTP");
        if (!string.IsNullOrEmpty(http) && http != "0" && http != "1")
            throw new ArgumentException("Development HTTP must be 0 or 1.");
        Bootstrap = new MobileResourceBootstrapConfig
        {
            schemaVersion = 1,
            mode = "resource-free",
            appVersion = projectVersion,
            dispatchUrl = environment("FFO_RESOURCE_DISPATCH_URL") ?? "",
            legacyDispatchUrls = ParseLegacyDispatchUrls(environment("FFO_RESOURCE_LEGACY_DISPATCH_URLS")),
            developmentHttp = http == "1"
        };
        if (!Development && string.IsNullOrWhiteSpace(Bootstrap.dispatchUrl))
            throw new InvalidOperationException("Formal resource-free builds require an explicit public FFO_RESOURCE_DISPATCH_URL.");
        Bootstrap.Validate(Development);
    }

    private static string[] ParseLegacyDispatchUrls(string value)
    {
        if (string.IsNullOrEmpty(value)) return new string[0];
        // Newline-delimited input keeps the existing environment-only Unity build interface.
        // Do not trim or discard empty entries: malformed profiles must fail, not silently change meaning.
        return value.Replace("\r\n", "\n").Split('\n');
    }

    public static void ValidateVersion(string version)
    {
        if (version == null || !Regex.IsMatch(version, @"\A(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\z"))
            throw new ArgumentException("Application version must be a stable MAJOR.MINOR.PATCH.");
        foreach (string component in version.Split('.'))
            if (!UInt32.TryParse(component, out _))
                throw new ArgumentException("Application version component exceeds UInt32.");
    }

    public void ValidateAndroidSigning(bool useCustomKeystore, string keystoreName, string keyAliasName,
        string applicationId, int versionCode)
    {
        if (Development) return;
        // Only inspect the configured path/alias and file existence, never key bytes or passwords.
        // Unity itself must unlock/sign with the user's preconfigured credentials and fails if it cannot.
        if (!useCustomKeystore || string.IsNullOrWhiteSpace(keystoreName) ||
            string.IsNullOrWhiteSpace(keyAliasName) || !File.Exists(keystoreName))
            throw new InvalidOperationException("Formal Android builds require an existing custom keystore and alias configured in Unity. No debug-signing fallback is permitted.");
        if (string.IsNullOrWhiteSpace(applicationId) ||
            !Regex.IsMatch(applicationId, @"\A[A-Za-z][A-Za-z0-9_]*(\.[A-Za-z][A-Za-z0-9_]*)+\z") ||
            applicationId.Equals("com.Company.ProductName", StringComparison.OrdinalIgnoreCase) || versionCode <= 0)
            throw new InvalidOperationException("Formal Android builds require a stable configured application ID and a positive allocated versionCode.");
    }
}
}
