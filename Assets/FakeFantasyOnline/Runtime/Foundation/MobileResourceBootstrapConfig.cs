using System;
using System.IO;
using System.Net;

namespace FakeFantasyOnline.Foundation
{
/// <summary>Small public bootstrap configuration, independent of game assets and login protocol.</summary>
[Serializable]
public sealed class MobileResourceBootstrapConfig
{
    public const string FileName = "resource-bootstrap.json";
    public const int MaxConfigBytes = 16384;
    public const int MaxEndpointBytes = 4096;
    public const int MaxLegacyDispatchUrls = 8;
    public int schemaVersion;
    public string mode, appVersion, dispatchUrl;
    // Release-specific retired defaults, supplied only by the build profile. Never a global migration list.
    public string[] legacyDispatchUrls;
    // Legacy serialized field, ignored by the unified gate. Only dispatchUrl selects remote metadata.
    public string resourceManifestUrl;
    public bool developmentHttp;

    public void Validate(bool isDevelopmentBuild)
    {
        if (schemaVersion != 1 || mode != "resource-free" || string.IsNullOrWhiteSpace(appVersion) ||
            appVersion.Length > 64 || appVersion.IndexOfAny(new[] { '\t', '\r', '\n', '/', '\\' }) >= 0)
            throw new InvalidDataException("Unsupported resource bootstrap configuration.");
        if (developmentHttp && !isDevelopmentBuild)
            throw new InvalidDataException("Development HTTP requires an explicit development build.");
        if (!string.IsNullOrWhiteSpace(dispatchUrl)) ValidateEndpoint(dispatchUrl, developmentHttp && isDevelopmentBuild);
        if (legacyDispatchUrls == null || legacyDispatchUrls.Length == 0) return;
        if (legacyDispatchUrls.Length > MaxLegacyDispatchUrls || string.IsNullOrWhiteSpace(dispatchUrl))
            throw new InvalidDataException("Legacy dispatch migration requires a current default and at most eight retired URLs.");
        var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal) { dispatchUrl };
        foreach (string legacy in legacyDispatchUrls)
        {
            ValidateEndpoint(legacy, developmentHttp && isDevelopmentBuild);
            if (!seen.Add(legacy)) throw new InvalidDataException("Legacy dispatch URLs must be distinct from each other and the current default.");
        }
    }

    public static Uri ValidateEndpoint(string value, bool allowDevelopmentLanHttp)
    {
        Uri uri;
        if (value != null) foreach (char c in value) if (char.IsControl(c)) throw new InvalidDataException("Control characters are not allowed in URLs.");
        if (string.IsNullOrWhiteSpace(value) || value != value.Trim() || System.Text.Encoding.UTF8.GetByteCount(value) > MaxEndpointBytes ||
            !Uri.TryCreate(value, UriKind.Absolute, out uri) || string.IsNullOrEmpty(uri.Host) ||
            !string.IsNullOrEmpty(uri.UserInfo) || !string.IsNullOrEmpty(uri.Query) || !string.IsNullOrEmpty(uri.Fragment) ||
            ((uri.IsLoopback || IsLocalhost(uri.Host)) && !(allowDevelopmentLanHttp && IsDevelopmentLoopback(uri.Host))) || uri.Port < 1 || uri.Port > 65535 || uri.Host == "0.0.0.0" || (uri.Host == "::" || uri.Host == "[::]") || value.IndexOf('\\') >= 0 ||
            (uri.Scheme != Uri.UriSchemeHttps && !(uri.Scheme == Uri.UriSchemeHttp && allowDevelopmentLanHttp && (IsPrivateLan(uri.Host) || IsDevelopmentLoopback(uri.Host)))))
            throw new InvalidDataException("Use a credential-free HTTPS URL. HTTP is allowed only for explicitly enabled development LAN IPs or local test loopback.");
        // Reject scoped IPv6 and legacy numeric IPv4 spellings rather than normalizing them into a different host.
        int authorityStart = value.IndexOf("://", StringComparison.Ordinal) + 3;
        string authority = value.Substring(authorityStart).Split('/')[0];
        if (authority.IndexOf('%') >= 0) throw new InvalidDataException("Scoped or escaped URL hosts are not supported.");
        string originalHost = authority.StartsWith("[", StringComparison.Ordinal) ? authority : authority.Split(':')[0];
        if (uri.HostNameType == UriHostNameType.IPv4 && originalHost != uri.Host)
            throw new InvalidDataException("Use canonical dotted-decimal IPv4 addresses.");
        return uri;
    }

    private static bool IsDevelopmentLoopback(string host)
    {
        return host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            host == "127.0.0.1" || host == "::1" || host == "[::1]";
    }

    private static bool IsLocalhost(string host)
    {
        string normalized = host.TrimEnd('.');
        return normalized.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
            normalized.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsPrivateLan(string host)
    {
        IPAddress address;
        if (!IPAddress.TryParse(host, out address)) return false;
        byte[] b = address.GetAddressBytes();
        return b.Length == 4 && (b[0] == 10 || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168));
    }
}
}
