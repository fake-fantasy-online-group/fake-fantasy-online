using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Product versions only. Never reuse these values as login/authentication markers.</summary>
public static class ClientProductVersion
{
    public static int Compare(string left, string right)
    {
        string[] a = Parts(left), b = Parts(right);
        for (int i = 0; i < 3; i++)
        {
            int result = a[i].Length.CompareTo(b[i].Length);
            if (result == 0) result = string.CompareOrdinal(a[i], b[i]);
            if (result != 0) return result;
        }
        return 0;
    }
    public static bool IsValid(string value)
    { try { Parts(value); return true; } catch (InvalidDataException) { return false; } }
    private static string[] Parts(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > 64) throw new InvalidDataException("Application version must be MAJOR.MINOR.PATCH.");
        string[] parts = value.Split('.');
        if (parts.Length != 3) throw new InvalidDataException("Application version must be MAJOR.MINOR.PATCH.");
        foreach (string part in parts)
        {
            if (part.Length == 0 || (part.Length > 1 && part[0] == '0')) throw new InvalidDataException("Invalid application version.");
            foreach (char c in part) if (c < '0' || c > '9') throw new InvalidDataException("Invalid application version.");
            uint number;
            if (!uint.TryParse(part, NumberStyles.None, CultureInfo.InvariantCulture, out number))
                throw new InvalidDataException("Application version component exceeds UInt32.");
        }
        return parts;
    }
}

/// <summary>Bounded dispatch extension. Existing servers are left for the existing login decoder.</summary>
public sealed class ClientUpdatePlan
{
    public const int MaxDispatchBytes = 256 * 1024;
    public string ResourceManifestUrl { get; private set; }
    public string ResourceManifestSha256 { get; private set; }
    public long ResourceManifestSizeBytes { get; private set; }
    // False still permits a mandatory package upgrade; the resource gate must
    // check Required first and must not download or enter without a match.
    public bool HasCompatibleResources { get { return ResourceManifestUrl != null; } }
    public string HotupdateManifestUrl { get; private set; }
    public ClientArtifact Artifact { get; private set; }
    public bool Required { get; private set; }
    public bool Available { get; private set; }
    public string CurrentVersion { get; private set; }

    public sealed class ClientArtifact
    {
        public string Version { get; internal set; }
        public string MinVersion { get; internal set; }
        public string Url { get; internal set; }
        public string Sha256 { get; internal set; }
        public long SizeBytes { get; internal set; }
    }

    public static ClientUpdatePlan Parse(string json, string platform, string currentVersion, bool developmentHttp)
    {
        if (json == null || Encoding.UTF8.GetByteCount(json) > MaxDispatchBytes)
            throw new InvalidDataException("Dispatch exceeds its size limit.");
        if (!ClientProductVersion.IsValid(currentVersion)) throw new InvalidDataException("This client's application version is invalid.");
        if (!SupportedPlatform(platform)) throw new InvalidDataException("Unsupported update platform.");
        var root = Object(BoundedJson.Parse(json), "dispatch");
        object raw;
        if (!root.TryGetValue("servers", out raw) || !(raw is List<object>) || ((List<object>)raw).Count == 0)
            throw new InvalidDataException("Dispatch has no game servers.");
        if (!root.TryGetValue("update", out raw) || raw == null)
            throw new InvalidDataException("This dispatch does not publish update information yet.");
        var update = Object(raw, "update");
        if (Integer(update, "schemaVersion", false) != 1) throw new InvalidDataException("Unsupported dispatch update schema.");
        var plan = new ClientUpdatePlan { CurrentVersion = currentVersion };
        bool hasResourceMap = update.ContainsKey("resourceManifests");
        // Validate legacy fields when supplied, but never use them as a fallback
        // when the exact-platform/version map is present (even an empty map).
        plan.ResourceManifestUrl = Endpoint(update, "resourceManifestUrl", developmentHttp, hasResourceMap);
        plan.ResourceManifestSha256 = Digest(update, "resourceManifestSha256", true);
        plan.ResourceManifestSizeBytes = Integer(update, "resourceManifestSizeBytes", true);
        if ((plan.ResourceManifestSha256 != null) != (plan.ResourceManifestSizeBytes > 0) ||
            plan.ResourceManifestSizeBytes > MobileResourceManifest.MaxManifestBytes ||
            (plan.ResourceManifestUrl == null && plan.ResourceManifestSha256 != null))
            throw new InvalidDataException("Resource manifest URL, hash and bounded positive size must be supplied together.");
        if (hasResourceMap)
        {
            plan.ResourceManifestUrl = plan.ResourceManifestSha256 = null;
            plan.ResourceManifestSizeBytes = 0;
            var platforms = Object(update["resourceManifests"], "resourceManifests");
            foreach (var platformEntry in platforms)
            {
                if (!SupportedPlatform(platformEntry.Key)) throw new InvalidDataException("Unsupported resource manifest platform.");
                var versions = Object(platformEntry.Value, "resourceManifests." + platformEntry.Key);
                foreach (var versionEntry in versions)
                {
                    if (!ClientProductVersion.IsValid(versionEntry.Key)) throw new InvalidDataException("Invalid resource manifest application version.");
                    var entry = Object(versionEntry.Value, "resource manifest");
                    foreach (string field in entry.Keys)
                        if (field != "manifestUrl" && field != "sha256" && field != "sizeBytes")
                            throw new InvalidDataException("Unknown resource manifest field: " + field);
                    string url = Endpoint(entry, "manifestUrl", developmentHttp, false);
                    string hash = Digest(entry, "sha256", false);
                    long size = Integer(entry, "sizeBytes", false);
                    if (size > MobileResourceManifest.MaxManifestBytes)
                        throw new InvalidDataException("Resource manifest exceeds its size limit.");
                    if (platformEntry.Key == platform && versionEntry.Key == currentVersion)
                    {
                        plan.ResourceManifestUrl = url;
                        plan.ResourceManifestSha256 = hash;
                        plan.ResourceManifestSizeBytes = size;
                    }
                }
            }
        }
        plan.HotupdateManifestUrl = Endpoint(update, "hotupdateManifestUrl", developmentHttp, true);
        if (update.TryGetValue("clients", out raw))
        {
            var clients = Object(raw, "clients");
            foreach (var pair in clients)
            {
                if (!SupportedPlatform(pair.Key)) throw new InvalidDataException("Unsupported client artifact platform.");
                var entry = Object(pair.Value, pair.Key);
                var artifact = new ClientArtifact {
                    Version = Text(entry, "version", false), MinVersion = Text(entry, "minVersion", true),
                    Url = Endpoint(entry, "artifactUrl", developmentHttp, false), Sha256 = Digest(entry, "sha256", false),
                    SizeBytes = Integer(entry, "sizeBytes", false)
                };
                if (!ClientProductVersion.IsValid(artifact.Version) || artifact.SizeBytes <= 0 ||
                    (artifact.MinVersion != null && (!ClientProductVersion.IsValid(artifact.MinVersion) ||
                    ClientProductVersion.Compare(artifact.MinVersion, artifact.Version) > 0)))
                    throw new InvalidDataException("Invalid client artifact version, minimum version, or size.");
                if (pair.Key == platform) plan.Artifact = artifact;
            }
        }
        if (plan.Artifact != null)
        {
            plan.Available = ClientProductVersion.Compare(currentVersion, plan.Artifact.Version) < 0;
            plan.Required = plan.Artifact.MinVersion != null && ClientProductVersion.Compare(currentVersion, plan.Artifact.MinVersion) < 0;
        }
        return plan;
    }

    private static bool SupportedPlatform(string value)
    { return value == "android" || value == "windows" || value == "macos" || value == "linux"; }
    private static Dictionary<string, object> Object(object value, string name)
    { return value as Dictionary<string, object> ?? throw new InvalidDataException("Expected object: " + name); }
    private static string Text(Dictionary<string, object> obj, string key, bool optional)
    {
        object value;
        if (!obj.TryGetValue(key, out value)) { if (optional) return null; throw new InvalidDataException("Missing " + key); }
        string text = value as string;
        if (string.IsNullOrWhiteSpace(text) || text != text.Trim()) throw new InvalidDataException("Invalid " + key);
        return text;
    }
    private static string Endpoint(Dictionary<string, object> obj, string key, bool developmentHttp, bool optional)
    {
        string value = Text(obj, key, optional);
        if (value != null) MobileResourceBootstrapConfig.ValidateEndpoint(value, developmentHttp);
        return value;
    }
    private static string Digest(Dictionary<string, object> obj, string key, bool optional)
    {
        string value = Text(obj, key, optional);
        if (value == null) return null;
        if (value.Length != 64) throw new InvalidDataException("Invalid SHA-256: " + key);
        foreach (char c in value) if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
            throw new InvalidDataException("Invalid SHA-256: " + key);
        return value.ToLowerInvariant();
    }
    private static long Integer(Dictionary<string, object> obj, string key, bool optional)
    {
        object value;
        if (!obj.TryGetValue(key, out value)) { if (optional) return 0; throw new InvalidDataException("Missing " + key); }
        // BoundedJson returns an integer as Int64, a fractional/exponent value as Double.
        if (!(value is long) || (long)value <= 0) throw new InvalidDataException("Expected positive integer: " + key);
        return (long)value;
    }
}

/// <summary>Publication from the pre-login gate, separate from legacy wire/version announcements.</summary>
public static class ClientUpdateSession
{
    public static ClientUpdatePlan Plan { get; private set; }
    public static bool UnifiedGate { get; private set; }
    public static void Reset() { Plan = null; UnifiedGate = false; }
    public static void Publish(ClientUpdatePlan plan) { Plan = plan; UnifiedGate = true; }
}
}
