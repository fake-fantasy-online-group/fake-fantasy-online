using System;
using System.Collections.Generic;
using System.IO;

namespace FakeFantasyOnline.Foundation
{
/// <summary>
/// Optional, manifest-bound first-install transport. This is not a resource manifest:
/// only the selected manifest decides destination names and installed content.
/// </summary>
public sealed class MobileResourceArchive
{
    public const long MaxArchiveBytes = 64L * 1024 * 1024 * 1024;
    public const string AllowedRootPrefix = "Assets/StreamingAssets/";
    public string Url { get; private set; }
    public string Format { get; private set; }
    public long SizeBytes { get; private set; }
    public string Sha256 { get; private set; }
    public string RootPrefix { get; private set; }
    private MobileResourceArchive() { }

    internal static MobileResourceArchive Parse(object value, bool developmentHttp)
    {
        var descriptor = Object(value, "resource archive");
        Fields(descriptor, "url", "format", "sizeBytes", "sha256", "rootPrefix");
        var archive = new MobileResourceArchive {
            Url = Text(descriptor, "url"),
            Format = Text(descriptor, "format"),
            SizeBytes = Size(descriptor, "sizeBytes"),
            Sha256 = Digest(descriptor, "sha256"),
            RootPrefix = Text(descriptor, "rootPrefix")
        };
        MobileResourceBootstrapConfig.ValidateEndpoint(archive.Url, developmentHttp);
        if (archive.Format != "tar") throw new InvalidDataException("Unsupported resource archive format.");
        if (archive.RootPrefix != AllowedRootPrefix) throw new InvalidDataException("Unsupported resource archive root prefix.");
        return archive;
    }

    private static Dictionary<string, object> Object(object value, string name)
    { return value as Dictionary<string, object> ?? throw new InvalidDataException("Expected object: " + name); }
    private static void Fields(Dictionary<string, object> obj, params string[] allowed)
    {
        foreach (string field in obj.Keys)
            if (Array.IndexOf(allowed, field) < 0) throw new InvalidDataException("Unknown resource archive field: " + field);
    }
    private static string Text(Dictionary<string, object> obj, string key)
    {
        object raw;
        if (!obj.TryGetValue(key, out raw)) throw new InvalidDataException("Missing resource archive " + key);
        string value = raw as string;
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim()) throw new InvalidDataException("Invalid resource archive " + key);
        return value;
    }
    private static long Size(Dictionary<string, object> obj, string key)
    {
        object raw;
        if (!obj.TryGetValue(key, out raw) || !(raw is long) ||
            (long)raw <= 0 || (long)raw > MaxArchiveBytes)
            throw new InvalidDataException("Invalid bounded resource archive " + key);
        return (long)raw;
    }
    private static string Digest(Dictionary<string, object> obj, string key)
    {
        string value = Text(obj, key);
        if (value.Length != 64) throw new InvalidDataException("Invalid resource archive SHA-256: " + key);
        foreach (char c in value)
            if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F')))
                throw new InvalidDataException("Invalid resource archive SHA-256: " + key);
        return value.ToLowerInvariant();
    }
}
}
