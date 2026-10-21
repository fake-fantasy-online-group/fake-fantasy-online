using System;
using System.Collections.Generic;
using System.IO;
using FakeFantasyOnline.Editor;
using FakeFantasyOnline.Foundation;

public static class ResourceFreeBuildProfileContract
{
    static int checks;
    static void Check(bool condition) { checks++; if (!condition) throw new Exception("Assertion " + checks); }
    static void Reject(Action action) { try { action(); } catch (ArgumentException) { checks++; return; } catch (InvalidOperationException) { checks++; return; } catch (InvalidDataException) { checks++; return; } catch (IOException) { checks++; return; } throw new Exception("Expected rejection"); }
    public static void Main()
    {
        var environment = new Dictionary<string, string>();
        Func<string, string> get = key => environment.TryGetValue(key, out var value) ? value : null;
        Reject(() => new ResourceFreeBuildProfile("android", "1.0.0", get));
        environment["FFO_RESOURCE_DISPATCH_URL"] = "https://example.com/dispatch.json";
        foreach (string platform in new[] { "android", "windows", "macos" })
        {
            var profile = new ResourceFreeBuildProfile(platform, "1.0.0", get);
            Check(!profile.Development && profile.Bootstrap.dispatchUrl == environment["FFO_RESOURCE_DISPATCH_URL"]);
            Check(profile.Bootstrap.appVersion == "1.0.0" && !profile.Bootstrap.developmentHttp);
        }
        string retired = "https://retired.example.test/dispatch.json", current = environment["FFO_RESOURCE_DISPATCH_URL"];
        string custom = "https://custom.example.test/dispatch.json";
        environment["FFO_RESOURCE_LEGACY_DISPATCH_URLS"] = retired + "\r\nhttps://older.example.test/dispatch.json";
        foreach (string platform in new[] { "android", "windows", "macos" })
        {
            var profile = new ResourceFreeBuildProfile(platform, "1.0.0", get);
            Check(profile.Bootstrap.legacyDispatchUrls.Length == 2 && profile.Bootstrap.legacyDispatchUrls[0] == retired);
            Check(ClientUpdateAddressStore.SelectDispatchUrl(retired, custom, current, profile.Bootstrap.legacyDispatchUrls) == current);
            Check(ClientUpdateAddressStore.SelectDispatchUrl("", retired, current, profile.Bootstrap.legacyDispatchUrls) == current);
            Check(ClientUpdateAddressStore.SelectDispatchUrl(custom, retired, current, profile.Bootstrap.legacyDispatchUrls) == custom);
            Check(ClientUpdateAddressStore.SelectDispatchUrl("", custom, current, profile.Bootstrap.legacyDispatchUrls) == custom);
            Check(ClientUpdateAddressStore.SelectDispatchUrl("", "", current, profile.Bootstrap.legacyDispatchUrls) == current);
            Check(ClientUpdateAddressStore.SelectDispatchUrl(current, retired, current, profile.Bootstrap.legacyDispatchUrls) == current);
            Check(ClientUpdateAddressStore.SelectDispatchUrl(retired + "/other", "", current, profile.Bootstrap.legacyDispatchUrls) == retired + "/other");
        }
        Check(ClientUpdateAddressStore.SelectDispatchUrl("", "", null, null) == "");
        Check(ClientUpdateAddressStore.SelectDispatchUrl(custom, "", "", null) == custom);
        Check(ClientUpdateAddressStore.SelectDispatchUrl(retired, "", current, null) == retired);
        Check(ClientUpdateAddressStore.MigrateKnownDefault("", "", current) == "");
        Check(ClientUpdateAddressStore.MigrateKnownDefault(retired, retired, "") == retired);
        foreach (string invalid in new[] { current, retired + "\n" + retired, retired + "\n", " " + retired,
            "https://user:secret@example.test/d", "https://example.test/d?token=x", "http://localhost/d",
            string.Join("\n", new[] { retired, "https://e.test/2", "https://e.test/3", "https://e.test/4", "https://e.test/5", "https://e.test/6", "https://e.test/7", "https://e.test/8", "https://e.test/9" }) })
        {
            environment["FFO_RESOURCE_LEGACY_DISPATCH_URLS"] = invalid;
            Reject(() => new ResourceFreeBuildProfile("android", "1.0.0", get));
        }
        environment["FFO_RESOURCE_LEGACY_DISPATCH_URLS"] = retired;
        environment.Remove("FFO_RESOURCE_DISPATCH_URL");
        environment["FFO_BUILD_CONFIGURATION"] = "development";
        Reject(() => new ResourceFreeBuildProfile("android", "1.0.0", get));
        environment.Remove("FFO_RESOURCE_LEGACY_DISPATCH_URLS");
        var unconfigured = new ResourceFreeBuildProfile("android", "1.0.0", get);
        Check(unconfigured.Bootstrap.dispatchUrl == "" && unconfigured.Bootstrap.legacyDispatchUrls.Length == 0);
        environment["FFO_RESOURCE_DISPATCH_URL"] = current;
        environment.Remove("FFO_BUILD_CONFIGURATION");
        foreach (string version in new[] { "1.0", "01.0.0", "1.0.0-rc.1", "1.0.0+build", "4294967296.0.0" })
            Reject(() => new ResourceFreeBuildProfile("android", version, get));
        environment["FFO_BUILD_PLATFORM"] = "macos";
        Reject(() => new ResourceFreeBuildProfile("android", "1.0.0", get));
        environment.Remove("FFO_BUILD_PLATFORM");
        environment["FFO_BUILD_VERSION"] = "1.0.1";
        Reject(() => new ResourceFreeBuildProfile("android", "1.0.0", get));
        environment.Remove("FFO_BUILD_VERSION");
        environment["FFO_BUILD_DEVELOPMENT_HTTP"] = "1";
        Reject(() => new ResourceFreeBuildProfile("android", "1.0.0", get));
        environment["FFO_BUILD_CONFIGURATION"] = "development";
        environment["FFO_RESOURCE_DISPATCH_URL"] = "http://192.168.1.20/dispatch.json";
        Check(new ResourceFreeBuildProfile("android", "1.0.0", get).Development);
        foreach (string local in new[] { "http://localhost:8088/dispatch.json", "http://127.0.0.1:8088/dispatch.json", "http://[::1]:8088/dispatch.json" })
        {
            environment["FFO_RESOURCE_DISPATCH_URL"] = local;
            foreach (string platform in new[] { "android", "windows", "macos" })
                Check(new ResourceFreeBuildProfile(platform, "1.0.0", get).Development);
            environment["FFO_BUILD_DEVELOPMENT_HTTP"] = "0";
            Reject(() => new ResourceFreeBuildProfile("macos", "1.0.0", get));
            environment["FFO_BUILD_DEVELOPMENT_HTTP"] = "1";
            environment["FFO_BUILD_CONFIGURATION"] = "formal";
            Reject(() => new ResourceFreeBuildProfile("macos", "1.0.0", get));
            environment["FFO_BUILD_CONFIGURATION"] = "development";
        }
        foreach (string badLocal in new[] { "http://0.0.0.0/d", "http://[::]/d", "http://sub.localhost/d", "http://127.0.0.2/d", "http://127.1/d", "http://user:pass@localhost/d", "http://localhost/d?token=x" })
        {
            environment["FFO_RESOURCE_DISPATCH_URL"] = badLocal;
            Reject(() => new ResourceFreeBuildProfile("macos", "1.0.0", get));
        }
        environment["FFO_BUILD_CONFIGURATION"] = "formal";
        environment["FFO_BUILD_DEVELOPMENT_HTTP"] = "0";
        environment["FFO_RESOURCE_DISPATCH_URL"] = "https://example.com/dispatch.json";
        var formal = new ResourceFreeBuildProfile("android", "1.0.0", get);
        string temp = Path.Combine(Path.GetTempPath(), "ffo-build-profile-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            // Synthetic non-key bytes. The validator only checks existence; it must never read the key.
            string key = Path.Combine(temp, "synthetic-keystore.fixture");
            File.WriteAllText(key, "not a signing credential");
            Reject(() => formal.ValidateAndroidSigning(false, key, "alias", "org.example.client", 2));
            Reject(() => formal.ValidateAndroidSigning(true, "missing", "alias", "org.example.client", 2));
            Reject(() => formal.ValidateAndroidSigning(true, key, "", "org.example.client", 2));
            Reject(() => formal.ValidateAndroidSigning(true, key, "alias", "com.Company.ProductName", 2));
            Reject(() => formal.ValidateAndroidSigning(true, key, "alias", "org.example.client", 0));
            formal.ValidateAndroidSigning(true, key, "alias", "org.example.client", 2); checks++;
            Reject(() => DesktopBuildPayload.ValidateIdentity("windows", true, "org.example.client", "", ""));
            Reject(() => DesktopBuildPayload.ValidateIdentity("macos", true, "org.example.client", "", ""));
            DesktopBuildPayload.ValidateIdentity("windows", true, "org.example.client", "", new string('a', 64)); checks++;
            DesktopBuildPayload.ValidateIdentity("macos", true, "org.example.client", "ABCDEFGHIJ", ""); checks++;
            string helpers = Path.Combine(temp, "helpers"); Directory.CreateDirectory(helpers);
            File.WriteAllText(Path.Combine(helpers, "DesktopUpdater.exe"), "synthetic executable fixture");
            DesktopBuildPayload.ValidateHelpers(helpers, "windows"); checks++;
            DesktopBuildPayload.CopyHelpers(helpers, Path.Combine(temp, "stage"), "windows");
            Check(File.ReadAllText(Path.Combine(temp, "stage/DesktopUpdater.exe")) == "synthetic executable fixture");
            File.WriteAllText(Path.Combine(helpers, "user.config"), "synthetic user settings");
            Reject(() => DesktopBuildPayload.ValidateHelpers(helpers, "windows"));
            File.Delete(Path.Combine(helpers, "user.config"));
            Reject(() => DesktopBuildPayload.ValidateHelpers(helpers, "macos"));
        }
        finally { Directory.Delete(temp, true); }
        Console.WriteLine("PASS resource-free build profile/payload: " + checks + " assertions (no Unity/Player build or real signing)");
    }
}
